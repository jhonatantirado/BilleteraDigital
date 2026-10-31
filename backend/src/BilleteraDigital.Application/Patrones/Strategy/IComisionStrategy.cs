namespace BilleteraDigital.Application.Patrones.Strategy;

/// <summary>
/// PATRÓN STRATEGY (comportamiento - GoF)
/// Problema: la comisión de una transferencia se calcula de forma distinta
/// según el monto (y mañana podría depender del tipo de cliente, una promoción, etc.).
/// Solución: encapsular cada algoritmo de cálculo en su propia clase que
/// implementa esta interfaz. El contexto (<see cref="CalculadoraComision"/>)
/// trabaja contra la interfaz y no sabe qué algoritmo concreto se usa.
/// Agregar una nueva política = crear una nueva clase (principio Abierto/Cerrado).
/// </summary>
public interface IComisionStrategy
{
    /// <summary>Nombre legible de la estrategia (se muestra al usuario).</summary>
    string Nombre { get; }

    /// <summary>Prioridad de evaluación: menor número = se evalúa antes.</summary>
    int Prioridad { get; }

    /// <summary>¿Esta estrategia es aplicable al monto dado?</summary>
    bool Aplica(decimal monto);

    /// <summary>El algoritmo encapsulado.</summary>
    decimal Calcular(decimal monto);
}
