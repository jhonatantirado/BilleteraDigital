using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Domain.Enums;
using Microsoft.Extensions.Logging;

namespace BilleteraDigital.Infrastructure.Observadores;

/// <summary>
/// OBSERVADOR CONCRETO 2: notificación por correo (simulada en consola).
/// En producción aquí se usaría un servicio SMTP, SendGrid, Amazon SES, etc.
/// </summary>
public class NotificacionEmailObserver : ITransaccionObserver
{
    private readonly ILogger<NotificacionEmailObserver> _log;
    public NotificacionEmailObserver(ILogger<NotificacionEmailObserver> log) => _log = log;

    public string Nombre => "Notificación por email";

    public Task ActualizarAsync(TransaccionEvento e)
    {
        if (e.Tipo == TipoTransaccion.Recarga)
        {
            _log.LogInformation("[EMAIL → {Email}] Recargaste S/ {Monto:N2}. Saldo: S/ {Saldo:N2}",
                e.EmailTitular, e.Monto, e.SaldoResultante);
        }
        else
        {
            _log.LogInformation("[EMAIL → {Email}] Enviaste S/ {Monto:N2} a la cuenta {Destino} (comisión S/ {Comision:N2})",
                e.EmailTitular, e.Monto, e.CuentaDestino, e.Comision);
            _log.LogInformation("[EMAIL → {Email}] Recibiste S/ {Monto:N2} de la cuenta {Origen}",
                e.EmailDestinatario, e.Monto, e.CuentaOrigen);
        }
        return Task.CompletedTask;
    }
}
