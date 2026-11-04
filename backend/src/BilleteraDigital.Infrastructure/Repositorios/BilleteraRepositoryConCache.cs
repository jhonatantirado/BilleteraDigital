using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Cache;
using Microsoft.Extensions.Logging;

namespace BilleteraDigital.Infrastructure.Repositorios;

/// <summary>
/// PATRÓN DECORATOR (estructural - GoF)
/// "Añade responsabilidades a un objeto de forma dinámica, envolviéndolo
///  con otro objeto que implementa la MISMA interfaz."
///
/// - Component:          <see cref="IBilleteraRepository"/>
/// - ConcreteComponent:  <see cref="BilleteraRepository"/> (EF Core → PostgreSQL)
/// - Decorator:          esta clase; recibe el componente real y le agrega caché.
///
/// BilleteraService no cambia: sigue pidiendo un IBilleteraRepository y no sabe
/// que ahora recibe el decorador (principio Abierto/Cerrado). El único cambio
/// está en el registro de dependencias (DependencyInjection.cs).
///
/// Qué se cachea y qué NO:
///   ✔ ObtenerPorId / PorNumeroCuenta / PorClienteId / PorTelefono (lecturas frecuentes)
///   ✘ ObtenerPorIdParaActualizar → debe ir SIEMPRE a la BD para tomar el
///     bloqueo FOR UPDATE y leer el saldo real (protege las transferencias).
///   ✘ ExisteNumeroCuenta → cachear un "no existe" podría duplicar cuentas.
///   ✘ Agregar → es una escritura.
/// </summary>
public class BilleteraRepositoryConCache : IBilleteraRepository
{
    private readonly IBilleteraRepository _interno;   // el objeto decorado
    private readonly CacheBilleteras _cache;
    private readonly ILogger<BilleteraRepositoryConCache> _log;

    public BilleteraRepositoryConCache(IBilleteraRepository interno, CacheBilleteras cache,
                                       ILogger<BilleteraRepositoryConCache> log)
    {
        _interno = interno;
        _cache = cache;
        _log = log;
    }

    // ------------------------------------------------------- con caché
    public async Task<Billetera?> ObtenerPorIdAsync(Guid id)
    {
        if (_cache.TryObtener(id, out var enCache))
        {
            _log.LogDebug("Caché HIT billetera {Id}", id);
            return enCache;
        }

        _log.LogDebug("Caché MISS billetera {Id}", id);
        return Guardar(await _interno.ObtenerPorIdAsync(id));
    }

    public async Task<Billetera?> ObtenerPorNumeroCuentaAsync(string numeroCuenta)
    {
        // El índice cuenta → Id es inmutable; el saldo se busca por Id
        if (_cache.TryObtenerIdPorCuenta(numeroCuenta, out var id))
            return await ObtenerPorIdAsync(id);

        return Guardar(await _interno.ObtenerPorNumeroCuentaAsync(numeroCuenta));
    }

    public async Task<Billetera?> ObtenerPorClienteIdAsync(Guid clienteId)
    {
        if (_cache.TryObtenerIdPorCliente(clienteId, out var id))
            return await ObtenerPorIdAsync(id);

        return Guardar(await _interno.ObtenerPorClienteIdAsync(clienteId));
    }

    public async Task<Billetera?> ObtenerPorTelefonoAsync(string telefono)
    {
        if (_cache.TryObtenerIdPorTelefono(telefono, out var id))
            return await ObtenerPorIdAsync(id);

        var billetera = Guardar(await _interno.ObtenerPorTelefonoAsync(telefono));
        if (billetera is not null) _cache.GuardarIndiceTelefono(telefono, billetera.Id);
        return billetera;
    }

    // ------------------------------------------- sin caché: solo delegan
    public Task<Billetera?> ObtenerPorIdParaActualizarAsync(Guid id) =>
        _interno.ObtenerPorIdParaActualizarAsync(id);

    public Task<bool> ExisteNumeroCuentaAsync(string numeroCuenta) =>
        _interno.ExisteNumeroCuentaAsync(numeroCuenta);

    public Task AgregarAsync(Billetera billetera) => _interno.AgregarAsync(billetera);

    /// <summary>Guarda en caché solo resultados encontrados: nunca un "no existe".</summary>
    private Billetera? Guardar(Billetera? billetera)
    {
        if (billetera is not null) _cache.Guardar(billetera);
        return billetera;
    }
}
