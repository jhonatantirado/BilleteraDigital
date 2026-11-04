namespace BilleteraDigital.Application.Interfaces;

/// <summary>
/// Envío de SMS. En el curso la implementación es SIMULADA (escribe en la consola
/// de la API y en logs/sms_simulados.log); en producción sería Twilio, AWS SNS, etc.
/// </summary>
public interface IEnviadorSms
{
    Task EnviarAsync(string telefonoE164, string mensaje);
}
