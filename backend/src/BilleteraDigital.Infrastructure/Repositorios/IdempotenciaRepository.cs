using BilleteraDigital.Application.Idempotencia;
using BilleteraDigital.Infrastructure.Persistencia;
using Microsoft.EntityFrameworkCore;

namespace BilleteraDigital.Infrastructure.Repositorios;

/// <summary>
/// Claves de idempotencia en PostgreSQL. Todas las operaciones se ejecutan de
/// inmediato (fuera del Unit of Work): la reserva tiene que ser visible para
/// otras peticiones ANTES de empezar a mover dinero.
/// </summary>
public class IdempotenciaRepository : IIdempotenciaRepository
{
    private readonly BilleteraDbContext _ctx;
    public IdempotenciaRepository(BilleteraDbContext ctx) => _ctx = ctx;

    public async Task<bool> IntentarReservarAsync(Guid clienteId, string clave, string ruta, string hashCuerpo, DateTime fecha)
    {
        // ON CONFLICT DO NOTHING: si dos peticiones llegan a la vez, PostgreSQL
        // inserta solo una (clave primaria) y a la otra le devuelve 0 filas.
        var filas = await _ctx.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO idempotencia (cliente_id, clave, ruta, hash_cuerpo, estado, fecha_creacion)
            VALUES ({clienteId}, {clave}, {ruta}, {hashCuerpo}, 'EnProceso', {fecha})
            ON CONFLICT (cliente_id, clave) DO NOTHING
            """);
        return filas == 1;
    }

    public Task<RegistroIdempotencia?> ObtenerAsync(Guid clienteId, string clave) =>
        _ctx.Idempotencia.AsNoTracking().FirstOrDefaultAsync(r => r.ClienteId == clienteId && r.Clave == clave);

    public Task CompletarAsync(Guid clienteId, string clave, int codigoHttp, string respuesta, string tipoContenido) =>
        _ctx.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE idempotencia
               SET estado = 'Completada', codigo_http = {codigoHttp},
                   respuesta = CAST({respuesta} AS jsonb), tipo_contenido = {tipoContenido}
             WHERE cliente_id = {clienteId} AND clave = {clave}
            """);

    public Task LiberarAsync(Guid clienteId, string clave) =>
        _ctx.Idempotencia.Where(r => r.ClienteId == clienteId && r.Clave == clave).ExecuteDeleteAsync();

    public Task<int> EliminarAnterioresAAsync(DateTime limite) =>
        _ctx.Idempotencia.Where(r => r.FechaCreacion < limite).ExecuteDeleteAsync();
}
