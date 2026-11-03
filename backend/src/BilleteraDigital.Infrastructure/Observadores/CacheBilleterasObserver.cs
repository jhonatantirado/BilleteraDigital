using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Infrastructure.Cache;

namespace BilleteraDigital.Infrastructure.Observadores;

/// <summary>
/// OBSERVADOR CONCRETO 4: invalida en la caché las billeteras cuyo saldo cambió.
/// El Subject notifica DESPUÉS del COMMIT, así que no hay riesgo de que otra
/// petición vuelva a guardar en caché el saldo anterior mientras la transacción
/// sigue abierta (lo que sí pasaría si se invalidara antes de confirmar).
/// Decorator + Observer + Repository trabajando juntos.
/// </summary>
public class CacheBilleterasObserver : ITransaccionObserver
{
    private readonly CacheBilleteras _cache;

    public CacheBilleterasObserver(CacheBilleteras cache) => _cache = cache;

    public string Nombre => "Invalidación de caché";

    public Task ActualizarAsync(TransaccionEvento evento)
    {
        _cache.Invalidar(evento.BilleteraOrigenId);
        if (evento.BilleteraDestinoId is { } destino)
            _cache.Invalidar(destino);
        return Task.CompletedTask;
    }
}
