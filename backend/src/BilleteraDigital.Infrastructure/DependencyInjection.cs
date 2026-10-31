using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Observadores;
using BilleteraDigital.Infrastructure.Persistencia;
using BilleteraDigital.Infrastructure.Repositorios;
using BilleteraDigital.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BilleteraDigital.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registra las implementaciones concretas de la capa de Infraestructura.</summary>
    public static IServiceCollection AddInfraestructura(this IServiceCollection services, IConfiguration config)
    {
        var cadena = config.GetConnectionString("Postgres")
                     ?? throw new InvalidOperationException("Falta ConnectionStrings:Postgres en appsettings.json");

        // Scoped: un DbContext por petición HTTP, compartido por repositorios y Unit of Work
        services.AddDbContext<BilleteraDbContext>(o => o.UseNpgsql(cadena));

        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IBilleteraRepository, BilleteraRepository>();
        services.AddScoped<ITransaccionRepository, TransaccionRepository>();
        services.AddScoped<IUnitOfWork, UnidadDeTrabajo>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        // Observadores concretos (se suscriben al Subject al iniciar la API)
        services.AddSingleton<ITransaccionObserver, AuditoriaArchivoObserver>();
        services.AddSingleton<ITransaccionObserver, NotificacionEmailObserver>();
        services.AddSingleton<ITransaccionObserver, AlertaFraudeObserver>();

        return services;
    }
}
