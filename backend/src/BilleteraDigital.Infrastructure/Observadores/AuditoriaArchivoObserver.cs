using System.Text.Json;
using System.Text.Json.Serialization;
using BilleteraDigital.Application.Patrones.Observer;
using Microsoft.Extensions.Configuration;

namespace BilleteraDigital.Infrastructure.Observadores;

/// <summary>
/// OBSERVADOR CONCRETO 1: deja rastro de auditoría de cada operación
/// en un archivo JSON Lines (una línea por evento).
/// </summary>
public class AuditoriaArchivoObserver : ITransaccionObserver
{
    private readonly string _ruta;
    private readonly SemaphoreSlim _candado = new(1, 1);
    private static readonly JsonSerializerOptions Opciones = new() { Converters = { new JsonStringEnumConverter() } };

    public AuditoriaArchivoObserver(IConfiguration config)
    {
        _ruta = config["Observadores:RutaAuditoria"] ?? "logs/auditoria.jsonl";
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(_ruta))!);
    }

    public string Nombre => "Auditoría en archivo";

    public async Task ActualizarAsync(TransaccionEvento evento)
    {
        var linea = JsonSerializer.Serialize(evento, Opciones) + Environment.NewLine;
        await _candado.WaitAsync();
        try { await File.AppendAllTextAsync(_ruta, linea); }
        finally { _candado.Release(); }
    }
}
