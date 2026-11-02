namespace BilleteraDigital.Application.Patrones.Strategy;

/// <summary>
/// CONTEXTO del patrón Strategy.
/// Recibe (por inyección de dependencias) todas las estrategias registradas,
/// elige la que corresponde al monto y le delega el cálculo.
/// Observe que esta clase NO contiene ningún "if monto &lt; 500 ..." ni "if antigüedad ..." con fórmulas:
/// las fórmulas viven en cada estrategia.
/// </summary>
public class CalculadoraComision
{
    private readonly IReadOnlyList<IComisionStrategy> _estrategias;

    public CalculadoraComision(IEnumerable<IComisionStrategy> estrategias)
    {
        _estrategias = estrategias.OrderBy(e => e.Prioridad).ToList();
        if (_estrategias.Count == 0)
            throw new InvalidOperationException("No hay estrategias de comisión registradas.");
    }

    public IComisionStrategy SeleccionarEstrategia(ContextoComision contexto) =>
        _estrategias.FirstOrDefault(e => e.Aplica(contexto))
        ?? throw new InvalidOperationException($"Ninguna estrategia aplica para el monto {contexto.Monto}.");

    public (decimal Comision, string Estrategia) Calcular(ContextoComision contexto)
    {
        var estrategia = SeleccionarEstrategia(contexto);
        return (estrategia.Calcular(contexto), estrategia.Nombre);
    }
}
