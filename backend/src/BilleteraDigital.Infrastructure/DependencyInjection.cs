using BilleteraDigital.Application.Idempotencia;
using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Domain.Interfaces;
using BilleteraDigital.Infrastructure.Cache;
using BilleteraDigital.Infrastructure.Mantenimiento;
using BilleteraDigital.Infrastructure.Mensajeria;
using BilleteraDigital.Infrastructure.Observadores;
using BilleteraDigital.Infrastructure.Persistencia;
using BilleteraDigital.Infrastructure.Repositorios;
using BilleteraDigital.Infrastructure.Seguridad;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // DECORATOR: quien pide IBilleteraRepository recibe la versión con caché,
        // que a su vez envuelve al repositorio real de EF Core.
        services.AddMemoryCache();                    // IMemoryCache: Singleton, compartida entre peticiones
        services.AddSingleton<CacheBilleteras>();
        services.AddScoped<BilleteraRepository>();    // componente real (Scoped, igual que el DbContext)
        services.AddScoped<IBilleteraRepository>(sp => new BilleteraRepositoryConCache(
            sp.GetRequiredService<BilleteraRepository>(),
            sp.GetRequiredService<CacheBilleteras>(),
            sp.GetRequiredService<ILogger<BilleteraRepositoryConCache>>()));

        services.AddScoped<ITransaccionRepository, TransaccionRepository>();
        services.AddScoped<IPuntosFidelidadRepository, PuntosFidelidadRepository>();

        // CELULAR: verificación con código (SMS simulado)
        services.AddScoped<IVerificacionTelefonoRepository, VerificacionTelefonoRepository>();
        services.AddSingleton<IEnviadorSms, SmsSimuladoEnviador>();

        // IDEMPOTENCIA: almacén de claves + limpieza periódica de claves vencidas
        services.AddScoped<IIdempotenciaRepository, IdempotenciaRepository>();
        services.AddHostedService<LimpiezaIdempotenciaService>();
        services.AddScoped<IUnitOfWork, UnidadDeTrabajo>();

        services.AddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();
        services.AddSingleton<IGeneradorToken, JwtGeneradorToken>();

        // Observadores concretos (se suscriben al Subject al iniciar la API)
        services.AddSingleton<ITransaccionObserver, AuditoriaArchivoObserver>();
        services.AddSingleton<ITransaccionObserver, NotificacionEmailObserver>();
        services.AddSingleton<ITransaccionObserver, AlertaFraudeObserver>();
        services.AddSingleton<ITransaccionObserver, CacheBilleterasObserver>();
        services.AddSingleton<ITransaccionObserver, PuntosFidelidadObserver>();

        return services;
    }
}
