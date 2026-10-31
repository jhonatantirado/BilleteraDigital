namespace BilleteraDigital.Application.Patrones.Strategy;

/// <summary>
/// CONTEXTO del patrón Strategy.
/// Recibe (por inyección de dependencias) todas las estrategias registradas,
/// elige la que corresponde al monto y le delega el cálculo.
/// Observe que esta clase NO contiene ningún "if monto &lt; 500 ..." con fórmulas:
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

    public IComisionStrategy SeleccionarEstrategia(decimal monto) =>
        _estrategias.FirstOrDefault(e => e.Aplica(monto))
        ?? throw new InvalidOperationException($"Ninguna estrategia aplica para el monto {monto}.");

    public (decimal Comision, string Estrategia) Calcular(decimal monto)
    {
        var estrategia = SeleccionarEstrategia(monto);
        return (estrategia.Calcular(monto), estrategia.Nombre);
    }
}
