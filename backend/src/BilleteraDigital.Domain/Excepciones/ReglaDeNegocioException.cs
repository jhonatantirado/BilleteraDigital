namespace BilleteraDigital.Domain.Excepciones;

/// <summary>Se lanza cuando se viola una regla del negocio (HTTP 422).</summary>
public class ReglaDeNegocioException : Exception
{
    public ReglaDeNegocioException(string mensaje) : base(mensaje) { }
}
