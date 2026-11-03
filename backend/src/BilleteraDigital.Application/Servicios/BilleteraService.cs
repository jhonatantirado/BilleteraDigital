using BilleteraDigital.Application.DTOs;
using BilleteraDigital.Application.Excepciones;
using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Application.Patrones.FactoryMethod;
using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Application.Patrones.Strategy;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Enums;
using BilleteraDigital.Domain.Excepciones;
using BilleteraDigital.Domain.Interfaces;

namespace BilleteraDigital.Application.Servicios;

/// <summary>
/// Caso de uso central: recargas, transferencias e historial.
/// Aquí se ven trabajando juntos 4 de los 5 patrones:
///   Repository + Unit of Work → persistencia atómica
///   Strategy                  → cálculo de la comisión
///   Factory Method            → creación de los movimientos
///   Observer                  → notificaciones posteriores a la operación
/// </summary>
public class BilleteraService : IBilleteraService
{
    public const decimal MontoMinimoTransferencia = 1m;
    public const decimal MontoMaximoTransferencia = 10_000m;
    public const decimal MontoMaximoRecarga = 5_000m;

    private readonly IBilleteraRepository _billeteras;
    private readonly ITransaccionRepository _transacciones;
    private readonly IClienteRepository _clientes;
    private readonly IUnitOfWork _unidadDeTrabajo;
    private readonly CalculadoraComision _calculadora;
    private readonly ProveedorCreadores _creadores;
    private readonly ITransaccionSubject _subject;
    private readonly TimeProvider _reloj;

    public BilleteraService(IBilleteraRepository billeteras, ITransaccionRepository transacciones,
        IClienteRepository clientes, IUnitOfWork unidadDeTrabajo, CalculadoraComision calculadora,
        ProveedorCreadores creadores, ITransaccionSubject subject, TimeProvider reloj)
    {
        _reloj = reloj;
        _billeteras = billeteras;
        _transacciones = transacciones;
        _clientes = clientes;
        _unidadDeTrabajo = unidadDeTrabajo;
        _calculadora = calculadora;
        _creadores = creadores;
        _subject = subject;
    }

    public async Task<BilleteraResponse> ObtenerAsync(Guid billeteraId)
    {
        var b = await _billeteras.ObtenerPorIdAsync(billeteraId)
                ?? throw new NoEncontradoException("Billetera no encontrada.");
        return Mapeador.ABilleteraResponse(b);
    }

    public async Task<DestinatarioResponse> BuscarDestinatarioAsync(string numeroCuenta)
    {
        var b = await _billeteras.ObtenerPorNumeroCuentaAsync(numeroCuenta)
                ?? throw new NoEncontradoException("No existe una billetera con ese número de cuenta.");
        var cliente = await _clientes.ObtenerPorIdAsync(b.ClienteId);
        // Solo se expone nombre + inicial del apellido (privacidad)
        var nombre = cliente is null ? "—" : $"{cliente.Nombres} {cliente.Apellidos[..1]}.";
        return new DestinatarioResponse(b.NumeroCuenta, nombre);
    }

    public async Task<ComisionResponse> CalcularComisionAsync(Guid billeteraOrigenId, decimal monto)
    {
        ValidarMonto(monto, MontoMinimoTransferencia, MontoMaximoTransferencia);
        var emisor = await ObtenerTitularAsync(billeteraOrigenId);
        var (comision, estrategia) = _calculadora.Calcular(CrearContexto(monto, emisor));
        return new ComisionResponse(monto, comision, monto + comision, estrategia);
    }

    // ------------------------------------------------------------------ RECARGA
    public async Task<TransaccionResponse> RecargarAsync(RecargaRequest r)
    {
        ValidarMonto(r.Monto, 1m, MontoMaximoRecarga);

        Transaccion movimiento;
        Billetera billetera;

        await _unidadDeTrabajo.IniciarTransaccionAsync();
        try
        {
            billetera = await _billeteras.ObtenerPorIdParaActualizarAsync(r.BilleteraId)
                        ?? throw new NoEncontradoException("Billetera no encontrada.");

            billetera.Acreditar(r.Monto);

            // FACTORY METHOD: el servicio no sabe cómo se arma una "recarga"
            movimiento = _creadores.Para(TipoTransaccion.Recarga)
                .Registrar(new DatosTransaccion(billetera, r.Monto, 0m, Guid.NewGuid()));

            await _transacciones.AgregarAsync(movimiento);
            await _unidadDeTrabajo.ConfirmarAsync();
        }
        catch
        {
            await _unidadDeTrabajo.RevertirAsync();
            throw;
        }

        var titular = await _clientes.ObtenerPorIdAsync(billetera.ClienteId);

        // OBSERVER: se notifica DESPUÉS del COMMIT
        await _subject.NotificarAsync(new TransaccionEvento(
            TipoTransaccion.Recarga, movimiento.Referencia, billetera.NumeroCuenta,
            titular?.Email ?? "", null, null, r.Monto, 0m, billetera.Saldo, movimiento.Fecha,
            billetera.Id, null));

        return Mapeador.ATransaccionResponse(movimiento);
    }

    // ------------------------------------------------------------ TRANSFERENCIA
    public async Task<TransferenciaResponse> TransferirAsync(TransferenciaRequest r)
    {
        ValidarMonto(r.Monto, MontoMinimoTransferencia, MontoMaximoTransferencia);

        // Lectura previa SIN bloqueo, solo para conocer el Id del destino
        var destinoInfo = await _billeteras.ObtenerPorNumeroCuentaAsync(r.NumeroCuentaDestino?.Trim() ?? "")
                          ?? throw new NoEncontradoException("La cuenta destino no existe.");
        if (destinoInfo.Id == r.BilleteraOrigenId)
            throw new ReglaDeNegocioException("No puede transferirse dinero a su propia billetera.");

        // STRATEGY: la calculadora elige el algoritmo según el monto y la antigüedad del emisor
        var emisor = await ObtenerTitularAsync(r.BilleteraOrigenId);
        var (comision, estrategia) = _calculadora.Calcular(CrearContexto(r.Monto, emisor));
        var referencia = Guid.NewGuid();

        Billetera origen, destino;
        Transaccion enviada;

        await _unidadDeTrabajo.IniciarTransaccionAsync();
        try
        {
            // Bloqueamos las dos filas SIEMPRE en el mismo orden (por Id)
            // para evitar interbloqueos (deadlocks) entre transferencias cruzadas A→B y B→A.
            var ids = new[] { r.BilleteraOrigenId, destinoInfo.Id }.OrderBy(id => id).ToArray();
            var bloqueadas = new Dictionary<Guid, Billetera>();
            foreach (var id in ids)
            {
                bloqueadas[id] = await _billeteras.ObtenerPorIdParaActualizarAsync(id)
                                 ?? throw new NoEncontradoException("Billetera no encontrada.");
            }
            origen = bloqueadas[r.BilleteraOrigenId];
            destino = bloqueadas[destinoInfo.Id];

            // Reglas de dominio: el emisor paga monto + comisión
            origen.Debitar(r.Monto + comision);
            destino.Acreditar(r.Monto);

            // FACTORY METHOD: dos movimientos con la misma referencia
            enviada = _creadores.Para(TipoTransaccion.TransferenciaEnviada)
                .Registrar(new DatosTransaccion(origen, r.Monto, comision, referencia, destino, r.Descripcion));
            var recibida = _creadores.Para(TipoTransaccion.TransferenciaRecibida)
                .Registrar(new DatosTransaccion(destino, r.Monto, 0m, referencia, origen, r.Descripcion));

            await _transacciones.AgregarAsync(enviada);
            await _transacciones.AgregarAsync(recibida);

            // UNIT OF WORK: débito + crédito + 2 movimientos = un solo COMMIT
            await _unidadDeTrabajo.ConfirmarAsync();
        }
        catch
        {
            await _unidadDeTrabajo.RevertirAsync();
            throw;
        }

        var receptor = await _clientes.ObtenerPorIdAsync(destino.ClienteId);

        // OBSERVER: auditoría, email y alerta de fraude reaccionan sin que este servicio los conozca
        await _subject.NotificarAsync(new TransaccionEvento(
            TipoTransaccion.TransferenciaEnviada, referencia, origen.NumeroCuenta, emisor.Email,
            destino.NumeroCuenta, receptor?.Email, r.Monto, comision, origen.Saldo, enviada.Fecha,
            origen.Id, destino.Id));

        return new TransferenciaResponse(referencia, r.Monto, comision, estrategia, origen.Saldo,
            destino.NumeroCuenta, receptor?.NombreCompleto ?? "", enviada.Fecha);
    }

    // ---------------------------------------------------------------- HISTORIAL
    public async Task<PaginaResponse<TransaccionResponse>> ObtenerHistorialAsync(
        Guid billeteraId, int pagina, int tamanoPagina, TipoTransaccion? tipo)
    {
        if (await _billeteras.ObtenerPorIdAsync(billeteraId) is null)
            throw new NoEncontradoException("Billetera no encontrada.");

        pagina = Math.Max(1, pagina);
        tamanoPagina = Math.Clamp(tamanoPagina, 1, 50);

        var (items, total) = await _transacciones.ObtenerHistorialAsync(billeteraId, pagina, tamanoPagina, tipo);
        return new PaginaResponse<TransaccionResponse>(
            items.Select(Mapeador.ATransaccionResponse).ToList(), pagina, tamanoPagina, total);
    }

    /// <summary>Titular (cliente) de una billetera; lectura sin bloqueo.</summary>
    private async Task<Cliente> ObtenerTitularAsync(Guid billeteraId)
    {
        var billetera = await _billeteras.ObtenerPorIdAsync(billeteraId)
                        ?? throw new NoEncontradoException("Billetera no encontrada.");
        return await _clientes.ObtenerPorIdAsync(billetera.ClienteId)
               ?? throw new NoEncontradoException("Titular de la billetera no encontrado.");
    }

    private ContextoComision CrearContexto(decimal monto, Cliente emisor) =>
        new(monto, emisor.FechaRegistro, _reloj.GetUtcNow().UtcDateTime);

    private static void ValidarMonto(decimal monto, decimal minimo, decimal maximo)
    {
        if (monto < minimo || monto > maximo)
            throw new ReglaDeNegocioException($"El monto debe estar entre S/ {minimo:N2} y S/ {maximo:N2}.");
        if (decimal.Round(monto, 2) != monto)
            throw new ReglaDeNegocioException("El monto admite como máximo 2 decimales.");
    }
}
