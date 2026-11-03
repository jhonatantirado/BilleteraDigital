using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace BilleteraDigital.Infrastructure.Observadores;

/// <summary>
/// OBSERVADOR CONCRETO 5: guarda en la base de datos los puntos de fidelidad
/// de cada operación. Se agregó SIN modificar BilleteraService.
///
/// Decisiones de diseño (para discutir en clase):
/// 1. Ciclo de vida: el observador es Singleton, pero el DbContext es Scoped
///    (uno por petición). Por eso no recibe repositorios en el constructor: crea
///    su PROPIO scope por cada evento con IServiceScopeFactory.
/// 2. Consistencia eventual: se ejecuta DESPUÉS del COMMIT de la operación y en
///    otra transacción. Si falla, la operación ya ocurrió y los puntos no se
///    otorgan (el Subject atrapa el error). La solución "de producción" sería el
///    patrón Transactional Outbox.
/// 3. Idempotencia: la referencia de la operación es única en puntos_fidelidad,
///    así un mismo evento procesado dos veces no duplica los puntos.
/// </summary>
public class PuntosFidelidadObserver : ITransaccionObserver
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<PuntosFidelidadObserver> _log;

    public PuntosFidelidadObserver(IServiceScopeFactory scopes, ILogger<PuntosFidelidadObserver> log)
    {
        _scopes = scopes;
        _log = log;
    }

    public string Nombre => "Puntos de fidelidad";

    public async Task ActualizarAsync(TransaccionEvento e)
    {
        // Si la operación no otorga puntos (retiro, recibida...), ni se abre conexión
        if (MovimientoPuntos.Calcular(e.Tipo, e.Monto) == 0) return;

        await using var scope = _scopes.CreateAsyncScope();
        var billeteras = scope.ServiceProvider.GetRequiredService<IBilleteraRepository>();
        var puntos = scope.ServiceProvider.GetRequiredService<IPuntosFidelidadRepository>();
        var unidadDeTrabajo = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        if (await puntos.ExisteReferenciaAsync(e.Referencia))
        {
            _log.LogInformation("[PUNTOS] La operación {Ref} ya tenía puntos otorgados; se ignora.", e.Referencia);
            return;
        }

        // El evento trae la billetera; los puntos pertenecen a su titular
        var billetera = await billeteras.ObtenerPorIdAsync(e.BilleteraOrigenId)
                        ?? throw new InvalidOperationException($"Billetera {e.BilleteraOrigenId} no encontrada.");

        var movimiento = MovimientoPuntos.Otorgar(billetera.ClienteId, e.Referencia, e.Tipo, e.Monto, e.Fecha)!;
        await puntos.AgregarAsync(movimiento);

        try
        {
            await unidadDeTrabajo.GuardarCambiosAsync();
            _log.LogInformation("[PUNTOS → {Email}] +{Puntos} puntos por {Tipo} de S/ {Monto:N2}",
                e.EmailTitular, movimiento.Puntos, e.Tipo, e.Monto);
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // Dos procesos otorgaron puntos a la vez: la restricción UNIQUE ganó. No es un error.
            _log.LogInformation("[PUNTOS] Puntos de la operación {Ref} ya registrados (UNIQUE).", e.Referencia);
        }
    }
}
