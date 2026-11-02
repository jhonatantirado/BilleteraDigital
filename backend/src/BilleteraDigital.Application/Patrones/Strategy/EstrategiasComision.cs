namespace BilleteraDigital.Application.Patrones.Strategy;

/// <summary>
/// Estrategia concreta 0 (máxima prioridad): clientes con MÁS de 6 meses de
/// antigüedad no pagan comisión, sin importar el monto (programa de fidelidad).
/// </summary>
public sealed class ClienteAntiguoSinComisionStrategy : IComisionStrategy
{
    public const int MesesAntiguedad = 6;
    public string Nombre => $"Cliente con más de {MesesAntiguedad} meses: sin comisión";
    public int Prioridad => 0;
    public bool Aplica(ContextoComision c) => c.ClienteConMasDe(MesesAntiguedad);
    public decimal Calcular(ContextoComision c) => 0m;
}

/// <summary>Estrategia concreta 1: transferencias pequeñas (hasta S/ 500) no pagan comisión.</summary>
public sealed class SinComisionStrategy : IComisionStrategy
{
    public const decimal Limite = 500m;
    public string Nombre => "Sin comisión (hasta S/ 500)";
    public int Prioridad => 1;
    public bool Aplica(ContextoComision c) => c.Monto <= Limite;
    public decimal Calcular(ContextoComision c) => 0m;
}

/// <summary>Estrategia concreta 2: entre S/ 500.01 y S/ 5,000 se cobra el 0.5 % del monto.</summary>
public sealed class ComisionPorcentualStrategy : IComisionStrategy
{
    public const decimal Porcentaje = 0.005m;
    public const decimal Limite = 5000m;
    public string Nombre => "Porcentual 0.5 %";
    public int Prioridad => 2;
    public bool Aplica(ContextoComision c) => c.Monto > SinComisionStrategy.Limite && c.Monto <= Limite;
    public decimal Calcular(ContextoComision c) =>
        Math.Round(c.Monto * Porcentaje, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Estrategia concreta 3: montos mayores a S/ 5,000 pagan una tarifa plana (tope) de S/ 25.</summary>
public sealed class TarifaPlanaStrategy : IComisionStrategy
{
    public const decimal Tarifa = 25m;
    public string Nombre => "Tarifa plana S/ 25";
    public int Prioridad => 3;
    public bool Aplica(ContextoComision c) => c.Monto > ComisionPorcentualStrategy.Limite;
    public decimal Calcular(ContextoComision c) => Tarifa;
}
