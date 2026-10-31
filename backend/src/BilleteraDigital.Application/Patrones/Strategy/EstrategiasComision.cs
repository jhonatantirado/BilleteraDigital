namespace BilleteraDigital.Application.Patrones.Strategy;

/// <summary>Estrategia concreta 1: transferencias pequeñas (hasta S/ 500) no pagan comisión.</summary>
public sealed class SinComisionStrategy : IComisionStrategy
{
    public const decimal Limite = 500m;
    public string Nombre => "Sin comisión (hasta S/ 500)";
    public int Prioridad => 1;
    public bool Aplica(decimal monto) => monto <= Limite;
    public decimal Calcular(decimal monto) => 0m;
}

/// <summary>Estrategia concreta 2: entre S/ 500.01 y S/ 5,000 se cobra el 0.5 % del monto.</summary>
public sealed class ComisionPorcentualStrategy : IComisionStrategy
{
    public const decimal Porcentaje = 0.005m;
    public const decimal Limite = 5000m;
    public string Nombre => "Porcentual 0.5 %";
    public int Prioridad => 2;
    public bool Aplica(decimal monto) => monto > SinComisionStrategy.Limite && monto <= Limite;
    public decimal Calcular(decimal monto) =>
        Math.Round(monto * Porcentaje, 2, MidpointRounding.AwayFromZero);
}

/// <summary>Estrategia concreta 3: montos mayores a S/ 5,000 pagan una tarifa plana (tope) de S/ 25.</summary>
public sealed class TarifaPlanaStrategy : IComisionStrategy
{
    public const decimal Tarifa = 25m;
    public string Nombre => "Tarifa plana S/ 25";
    public int Prioridad => 3;
    public bool Aplica(decimal monto) => monto > ComisionPorcentualStrategy.Limite;
    public decimal Calcular(decimal monto) => Tarifa;
}
