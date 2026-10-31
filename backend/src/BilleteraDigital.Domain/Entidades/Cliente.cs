namespace BilleteraDigital.Domain.Entidades;

/// <summary>
/// Entidad de dominio: un cliente registrado en la billetera digital.
/// Las entidades encapsulan su estado: los setters son privados y la
/// creación se hace mediante un método que valida las reglas de negocio.
/// </summary>
public class Cliente
{
    public Guid Id { get; private set; }
    public string Nombres { get; private set; } = string.Empty;
    public string Apellidos { get; private set; } = string.Empty;
    public string Dni { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;
    public string Telefono { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public DateTime FechaRegistro { get; private set; }

    public Billetera? Billetera { get; private set; }

    // Requerido por EF Core
    private Cliente() { }

    public static Cliente Crear(string nombres, string apellidos, string dni,
                                string email, string telefono, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(nombres)) throw new Excepciones.ReglaDeNegocioException("Los nombres son obligatorios.");
        if (string.IsNullOrWhiteSpace(apellidos)) throw new Excepciones.ReglaDeNegocioException("Los apellidos son obligatorios.");
        if (dni is null || dni.Length != 8 || !dni.All(char.IsDigit))
            throw new Excepciones.ReglaDeNegocioException("El DNI debe tener 8 dígitos.");
        if (string.IsNullOrWhiteSpace(email) || !email.Contains('@'))
            throw new Excepciones.ReglaDeNegocioException("El email no es válido.");

        return new Cliente
        {
            Id = Guid.NewGuid(),
            Nombres = nombres.Trim(),
            Apellidos = apellidos.Trim(),
            Dni = dni,
            Email = email.Trim().ToLowerInvariant(),
            Telefono = telefono?.Trim() ?? string.Empty,
            PasswordHash = passwordHash,
            FechaRegistro = DateTime.UtcNow
        };
    }

    public string NombreCompleto => $"{Nombres} {Apellidos}";

    public void AsignarBilletera(Billetera billetera) => Billetera = billetera;
}
