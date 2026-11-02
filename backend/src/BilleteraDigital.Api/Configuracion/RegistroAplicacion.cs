using BilleteraDigital.Application.Interfaces;
using BilleteraDigital.Application.Patrones.FactoryMethod;
using BilleteraDigital.Application.Patrones.Observer;
using BilleteraDigital.Application.Patrones.Strategy;
using BilleteraDigital.Application.Servicios;

namespace BilleteraDigital.Api.Configuracion;

/// <summary>
/// RAÍZ DE COMPOSICIÓN: el único lugar donde se "conectan" interfaces con
/// implementaciones. Las capas internas nunca hacen "new" de sus dependencias.
/// </summary>
public static class RegistroAplicacion
{
    public static IServiceCollection AddAplicacion(this IServiceCollection services)
    {
        // Servicios de caso de uso (uno por petición)
        services.AddScoped<IClienteService, ClienteService>();
        services.AddScoped<IBilleteraService, BilleteraService>();

        // Reloj del sistema (abstracción de .NET 8): en las pruebas se reemplaza por una fecha fija
        services.AddSingleton(TimeProvider.System);

        // STRATEGY: todas las estrategias + el contexto que elige entre ellas
        services.AddSingleton<IComisionStrategy, ClienteAntiguoSinComisionStrategy>();
        services.AddSingleton<IComisionStrategy, SinComisionStrategy>();
        services.AddSingleton<IComisionStrategy, ComisionPorcentualStrategy>();
        services.AddSingleton<IComisionStrategy, TarifaPlanaStrategy>();
        services.AddSingleton<CalculadoraComision>();

        // FACTORY METHOD: un creador concreto por tipo de transacción
        services.AddSingleton<CreadorTransaccion, CreadorRecarga>();
        services.AddSingleton<CreadorTransaccion, CreadorTransferenciaEnviada>();
        services.AddSingleton<CreadorTransaccion, CreadorTransferenciaRecibida>();
        services.AddSingleton<ProveedorCreadores>();

        // OBSERVER + SINGLETON: un único Subject compartido por toda la aplicación
        services.AddSingleton<ITransaccionSubject, TransaccionSubject>();

        return services;
    }

    /// <summary>Suscribe al Subject todos los observadores registrados en el contenedor.</summary>
    public static WebApplication SuscribirObservadores(this WebApplication app)
    {
        var subject = app.Services.GetRequiredService<ITransaccionSubject>();
        foreach (var observador in app.Services.GetServices<ITransaccionObserver>())
            subject.Suscribir(observador);

        app.Logger.LogInformation("Observadores suscritos: {Lista}", string.Join(", ", subject.Observadores));
        return app;
    }
}
