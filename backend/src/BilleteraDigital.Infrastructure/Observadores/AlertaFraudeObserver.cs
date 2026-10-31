using BilleteraDigital.Application.Patrones.Observer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BilleteraDigital.Infrastructure.Observadores;

/// <summary>
/// OBSERVADOR CONCRETO 3: marca operaciones de monto alto para revisión.
/// Demuestra que se pueden agregar reacciones nuevas SIN tocar BilleteraService.
/// </summary>
public class AlertaFraudeObserver : ITransaccionObserver
{
    private readonly ILogger<AlertaFraudeObserver> _log;
    private readonly decimal _umbral;

    public AlertaFraudeObserver(ILogger<AlertaFraudeObserver> log, IConfiguration config)
    {
        _log = log;
        _umbral = decimal.TryParse(config["Observadores:UmbralAlerta"], out var u) ? u : 3000m;
    }

    public string Nombre => "Alerta de montos altos";

    public Task ActualizarAsync(TransaccionEvento e)
    {
        if (e.Monto >= _umbral)
            _log.LogWarning("[ALERTA] Operación {Tipo} de S/ {Monto:N2} desde {Cuenta} supera el umbral de S/ {Umbral:N2}. Ref: {Ref}",
                e.Tipo, e.Monto, e.CuentaOrigen, _umbral, e.Referencia);
        return Task.CompletedTask;
    }
}
