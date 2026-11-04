using BilleteraDigital.Domain.Excepciones;

namespace BilleteraDigital.Domain.ValueObjects;

/// <summary>
/// VALUE OBJECT (DDD): un celular peruano válido y normalizado.
/// - Se valida al crearse: un Telefono inválido no puede existir.
/// - Es inmutable y se compara por su valor (record).
/// - Siempre se guarda en formato E.164 ("+51987654321"), así lo que escribe
///   quien se registra y lo que escribe quien busca siempre coinciden.
/// Para admitir otros países bastaría con cambiar esta clase.
/// </summary>
public sealed record Telefono
{
    public const string Prefijo = "+51";

    /// <summary>Formato E.164, p. ej. "+51987654321".</summary>
    public string Valor { get; }

    private Telefono(string valor) => Valor = valor;

    /// <summary>Acepta "987654321", "987 654 321", "+51 987-654-321", "51987654321"…</summary>
    public static Telefono Crear(string? texto)
    {
        var digitos = new string((texto ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length == 11 && digitos.StartsWith("51")) digitos = digitos[2..];

        if (digitos.Length != 9 || digitos[0] != '9')
            throw new ReglaDeNegocioException("El celular debe tener 9 dígitos y empezar con 9.");

        return new Telefono(Prefijo + digitos);
    }

    /// <summary>Para mostrar sin revelar el número completo: "*** *** 678".</summary>
    public string Enmascarado => Enmascarar(Valor);

    public static string Enmascarar(string valorE164) =>
        valorE164.Length >= 3 ? $"*** *** {valorE164[^3..]}" : "***";

    public override string ToString() => Valor;
}
