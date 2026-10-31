using BilleteraDigital.Domain.Enums;

namespace BilleteraDigital.Application.Patrones.FactoryMethod;

/// <summary>
/// Entrega el creador concreto adecuado para cada tipo de transacción.
/// Los creadores se registran en el contenedor de dependencias; para agregar
/// un nuevo tipo (p. ej. "Retiro") basta con crear y registrar un nuevo creador.
/// </summary>
public class ProveedorCreadores
{
    private readonly Dictionary<TipoTransaccion, CreadorTransaccion> _creadores;

    public ProveedorCreadores(IEnumerable<CreadorTransaccion> creadores)
    {
        _creadores = creadores.ToDictionary(c => c.Tipo);
    }

    public CreadorTransaccion Para(TipoTransaccion tipo) =>
        _creadores.TryGetValue(tipo, out var creador)
            ? creador
            : throw new NotSupportedException($"No hay creador registrado para {tipo}.");
}
