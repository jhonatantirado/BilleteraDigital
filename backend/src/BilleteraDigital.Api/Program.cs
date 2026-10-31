using System.Text;
using System.Text.Json.Serialization;
using BilleteraDigital.Api.Configuracion;
using BilleteraDigital.Api.Middleware;
using BilleteraDigital.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ---------- Registro de dependencias por capa ----------
builder.Services.AddAplicacion();                               // Capa de Aplicación
builder.Services.AddInfraestructura(builder.Configuration);     // Capa de Infraestructura

// ---------- Seguridad: autenticación JWT ----------
var jwt = builder.Configuration.GetSection("Jwt");
var claveJwt = jwt["Clave"]
    ?? throw new InvalidOperationException("Falta Jwt:Clave en la configuración (appsettings / user-secrets).");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(o =>
    {
        o.MapInboundClaims = false; // conserva "sub" y "billetera_id" con su nombre original
        o.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt["Emisor"],
            ValidateAudience = true,
            ValidAudience = jwt["Audiencia"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

// Seguro por defecto: todo endpoint exige token, salvo los marcados con [AllowAnonymous]
builder.Services.AddAuthorization(o =>
    o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

// ---------- Capa de Presentación (MVC) ----------
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    // Botón "Authorize" en Swagger para pegar el token
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Description = "Pega aquí el token devuelto por /api/clientes/login"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// CORS: permite que la app Flutter (web/emulador) consuma la API
builder.Services.AddCors(o => o.AddPolicy("Flutter", p =>
    p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

app.SuscribirObservadores();                 // Patrón Observer
app.UseMiddleware<ManejoErroresMiddleware>(); // Errores → ProblemDetails

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Flutter");
app.UseAuthentication();   // lee y valida el token "Authorization: Bearer ..."
app.UseAuthorization();    // aplica [Authorize] / política por defecto
app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).AllowAnonymous().ExcludeFromDescription();

app.Run();
