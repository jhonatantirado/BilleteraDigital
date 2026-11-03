using BilleteraDigital.Application.Idempotencia;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BilleteraDigital.Infrastructure.Mantenimiento;

/// <summary>
/// Tarea en segundo plano: cada hora borra las claves de idempotencia de más de
/// 24 horas. Pasado ese tiempo, un reintento con la misma clave se trata como nuevo.
/// </summary>
public class LimpiezaIdempotenciaService : BackgroundService
{
    public static readonly TimeSpan Vigencia = TimeSpan.FromHours(24);
    private static readonly TimeSpan Intervalo = TimeSpan.FromHours(1);

    private readonly IServiceScopeFactory _scopes;
    private readonly TimeProvider _reloj;
    private readonly ILogger<LimpiezaIdempotenciaService> _log;

    public LimpiezaIdempotenciaService(IServiceScopeFactory scopes, TimeProvider reloj,
                                       ILogger<LimpiezaIdempotenciaService> log)
    {
        _scopes = scopes;
        _reloj = reloj;
        _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken detener)
    {
        using var temporizador = new PeriodicTimer(Intervalo, _reloj);
        do
        {
            try
            {
                await using var scope = _scopes.CreateAsyncScope();
                var repo = scope.ServiceProvider.GetRequiredService<IIdempotenciaRepository>();
                var borradas = await repo.EliminarAnterioresAAsync(_reloj.GetUtcNow().UtcDateTime - Vigencia);
                if (borradas > 0) _log.LogInformation("Idempotencia: {N} claves vencidas eliminadas", borradas);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Si la BD no está disponible, se reintenta en la próxima vuelta
                _log.LogWarning(ex, "No se pudieron limpiar las claves de idempotencia");
            }
        } while (await temporizador.WaitForNextTickAsync(detener));
    }
}
