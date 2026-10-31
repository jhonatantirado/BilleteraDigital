namespace BilleteraDigital.Application.Patrones.Observer;

/// <summary>
/// SUJETO CONCRETO: mantiene la lista de observadores y les notifica cada evento.
/// Se registra como Singleton (una sola instancia para toda la aplicación).
/// Si un observador falla, los demás igual son notificados y la transferencia
/// (ya confirmada en BD) no se revierte.
/// </summary>
public class TransaccionSubject : ITransaccionSubject
{
    private readonly List<ITransaccionObserver> _observadores = new();
    private readonly object _lock = new();

    public IReadOnlyCollection<string> Observadores
    {
        get { lock (_lock) return _observadores.Select(o => o.Nombre).ToList(); }
    }

    public void Suscribir(ITransaccionObserver observador)
    {
        lock (_lock)
        {
            if (!_observadores.Contains(observador)) _observadores.Add(observador);
        }
    }

    public void Desuscribir(ITransaccionObserver observador)
    {
        lock (_lock) _observadores.Remove(observador);
    }

    public async Task NotificarAsync(TransaccionEvento evento)
    {
        List<ITransaccionObserver> copia;
        lock (_lock) copia = _observadores.ToList();

        foreach (var observador in copia)
        {
            try
            {
                await observador.ActualizarAsync(evento);
            }
            catch (Exception ex)
            {
                // Un observador defectuoso no debe romper el flujo principal.
                Console.Error.WriteLine($"[Observer] {observador.Nombre} falló: {ex.Message}");
            }
        }
    }
}
