namespace BilleteraDigital.Application.Excepciones;

/// <summary>Recurso inexistente → HTTP 404.</summary>
public class NoEncontradoException : Exception
{
    public NoEncontradoException(string mensaje) : base(mensaje) { }
}

/// <summary>Conflicto con el estado actual (p. ej. email duplicado) → HTTP 409.</summary>
public class ConflictoException : Exception
{
    public ConflictoException(string mensaje) : base(mensaje) { }
}

/// <summary>Credenciales inválidas → HTTP 401.</summary>
public class CredencialesInvalidasException : Exception
{
    public CredencialesInvalidasException() : base("Email o contraseña incorrectos.") { }
}

/// <summary>El usuario autenticado intenta acceder a un recurso ajeno → HTTP 403.</summary>
public class AccesoDenegadoException : Exception
{
    public AccesoDenegadoException(string mensaje = "No tienes acceso a este recurso.") : base(mensaje) { }
}
