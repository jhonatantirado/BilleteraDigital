using BilleteraDigital.Domain.Entidades;
using Microsoft.Extensions.Caching.Memory;

namespace BilleteraDigital.Infrastructure.Cache;

/// <summary>
/// Encapsula la caché en memoria de billeteras: nombres de las claves,
/// tiempos de vida e invalidación. La comparten el decorador
/// (<see cref="Repositorios.BilleteraRepositoryConCache"/>), que la llena, y el
/// observador (<see cref="Observadores.CacheBilleterasObserver"/>), que la invalida.
///
/// Diseño de las claves:
///   billetera:id:{id}            → la Billetera completa (incluye Saldo: CAMBIA)
///   billetera:cuenta:{numero}    → solo el Id (dato inmutable)
///   billetera:cliente:{id}       → solo el Id (dato inmutable)
/// Como los índices por cuenta y por cliente apuntan al Id, para invalidar
/// una billetera basta con borrar UNA sola entrada: la que guarda el saldo.
/// </summary>
public class CacheBilleteras
{
    /// <summary>Red de seguridad: aunque nadie invalide, el saldo nunca queda viejo más de esto.</summary>
    public static readonly TimeSpan DuracionBilletera = TimeSpan.FromSeconds(30);

    /// <summary>Los índices (cuenta → Id, cliente → Id) nunca cambian; se guardan más tiempo.</summary>
    public static readonly TimeSpan DuracionIndice = TimeSpan.FromMinutes(30);

    private readonly IMemoryCache _cache;

    public CacheBilleteras(IMemoryCache cache) => _cache = cache;

    private static string ClavePorId(Guid id) => $"billetera:id:{id}";
    private static string ClavePorCuenta(string numeroCuenta) => $"billetera:cuenta:{numeroCuenta}";
    private static string ClavePorCliente(Guid clienteId) => $"billetera:cliente:{clienteId}";

    public bool TryObtener(Guid id, out Billetera? billetera) =>
        _cache.TryGetValue(ClavePorId(id), out billetera);

    public bool TryObtenerIdPorCuenta(string numeroCuenta, out Guid id) =>
        _cache.TryGetValue(ClavePorCuenta(numeroCuenta), out id);

    public bool TryObtenerIdPorCliente(Guid clienteId, out Guid id) =>
        _cache.TryGetValue(ClavePorCliente(clienteId), out id);

    /// <summary>Guarda la billetera y sus índices por número de cuenta y por cliente.</summary>
    public void Guardar(Billetera billetera)
    {
        _cache.Set(ClavePorId(billetera.Id), billetera, DuracionBilletera);
        _cache.Set(ClavePorCuenta(billetera.NumeroCuenta), billetera.Id, DuracionIndice);
        _cache.Set(ClavePorCliente(billetera.ClienteId), billetera.Id, DuracionIndice);
    }

    /// <summary>Descarta la copia en caché (y con ello su saldo) de una billetera.</summary>
    public void Invalidar(Guid billeteraId) => _cache.Remove(ClavePorId(billeteraId));
}
