namespace BilleteraDigital.Application.Patrones.Observer;

/// <summary>
/// PATRÓN OBSERVER (comportamiento - GoF) — interfaz del OBSERVADOR.
/// Cualquier componente interesado en "ocurrió una transacción" la implementa:
/// auditoría, notificación por email, detección de fraude, puntos de fidelidad...
/// El servicio que transfiere dinero NO conoce a ninguno de ellos (bajo acoplamiento).
/// </summary>
public interface ITransaccionObserver
{
    string Nombre { get; }
    Task ActualizarAsync(TransaccionEvento evento);
}

/// <summary>Interfaz del SUJETO (Subject / Publisher).</summary>
public interface ITransaccionSubject
{
    void Suscribir(ITransaccionObserver observador);
    void Desuscribir(ITransaccionObserver observador);
    Task NotificarAsync(TransaccionEvento evento);
    IReadOnlyCollection<string> Observadores { get; }
}
