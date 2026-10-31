using System.Text.Json.Serialization;
using BilleteraDigital.Api.Configuracion;
using BilleteraDigital.Api.Middleware;
using BilleteraDigital.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// ---------- Registro de dependencias por capa ----------
builder.Services.AddAplicacion();                               // Capa de Aplicación
builder.Services.AddInfraestructura(builder.Configuration);     // Capa de Infraestructura

// ---------- Capa de Presentación (MVC) ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS: permite que la app Flutter (web/emulador) consuma la API
builder.Services.AddCors(o => o.AddPolicy("Flutter", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.SuscribirObservadores();                 // Patrón Observer
app.UseMiddleware<ManejoErroresMiddleware>(); // Errores → ProblemDetails

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Flutter");
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();
