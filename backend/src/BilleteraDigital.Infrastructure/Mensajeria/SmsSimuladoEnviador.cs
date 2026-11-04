using BilleteraDigital.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BilleteraDigital.Infrastructure.Mensajeria;

/// <summary>
/// SMS SIMULADO para el curso: el mensaje aparece en la consola de la API y en
/// logs/sms_simulados.log (allí el estudiante lee el código de verificación).
/// En producción se reemplazaría por Twilio, AWS SNS, etc. sin tocar los casos
/// de uso: basta con registrar otra implementación de IEnviadorSms.
/// </summary>
public class SmsSimuladoEnviador : IEnviadorSms
{
    private readonly ILogger<SmsSimuladoEnviador> _log;
    private readonly string _ruta;
    private readonly SemaphoreSlim _candado = new(1, 1);

    public SmsSimuladoEnviador(ILogger<SmsSimuladoEnviador> log, IConfiguration config)
    {
        _log = log;
        _ruta = config["Sms:RutaSimulados"] ?? "logs/sms_simulados.log";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_ruta))!);
    }

    public async Task EnviarAsync(string telefonoE164, string mensaje)
    {
        _log.LogWarning("[SMS SIMULADO → {Telefono}] {Mensaje}", telefonoE164, mensaje);

        var linea = $"{DateTime.UtcNow:O}  {telefonoE164}  {mensaje}{Environment.NewLine}";
        await _candado.WaitAsync();
        try { await File.AppendAllTextAsync(_ruta, linea); }
        finally { _candado.Release(); }
    }
}
