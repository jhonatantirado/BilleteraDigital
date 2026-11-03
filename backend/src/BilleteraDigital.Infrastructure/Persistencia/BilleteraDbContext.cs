using BilleteraDigital.Application.Idempotencia;
using BilleteraDigital.Domain.Entidades;
using BilleteraDigital.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BilleteraDigital.Infrastructure.Persistencia;

/// <summary>
/// Contexto de Entity Framework Core para PostgreSQL.
/// El mapeo (Fluent API) traduce las entidades de dominio a las tablas
/// creadas por el script database/01_esquema.sql. Así el Dominio queda
/// libre de atributos de persistencia ([Table], [Column], ...).
/// </summary>
public class BilleteraDbContext : DbContext
{
    public BilleteraDbContext(DbContextOptions<BilleteraDbContext> options) : base(options) { }

    public DbSet<Cliente> Clientes => Set<Cliente>();
    public DbSet<Billetera> Billeteras => Set<Billetera>();
    public DbSet<Transaccion> Transacciones => Set<Transaccion>();
    public DbSet<MovimientoPuntos> MovimientosPuntos => Set<MovimientoPuntos>();
    public DbSet<RegistroIdempotencia> Idempotencia => Set<RegistroIdempotencia>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.Entity<Cliente>(e =>
        {
            e.ToTable("clientes");
            e.HasKey(c => c.Id);
            e.Property(c => c.Id).HasColumnName("id");
            e.Property(c => c.Nombres).HasColumnName("nombres").HasMaxLength(80).IsRequired();
            e.Property(c => c.Apellidos).HasColumnName("apellidos").HasMaxLength(80).IsRequired();
            e.Property(c => c.Dni).HasColumnName("dni").HasMaxLength(8).IsRequired();
            e.Property(c => c.Email).HasColumnName("email").HasMaxLength(120).IsRequired();
            e.Property(c => c.Telefono).HasColumnName("telefono").HasMaxLength(15);
            e.Property(c => c.PasswordHash).HasColumnName("password_hash").IsRequired();
            e.Property(c => c.FechaRegistro).HasColumnName("fecha_registro");
            e.HasIndex(c => c.Dni).IsUnique();
            e.HasIndex(c => c.Email).IsUnique();
            e.Ignore(c => c.NombreCompleto);

            e.HasOne(c => c.Billetera)
             .WithOne(b => b.Cliente)
             .HasForeignKey<Billetera>(b => b.ClienteId);
        });

        mb.Entity<Billetera>(e =>
        {
            e.ToTable("billeteras");
            e.HasKey(b => b.Id);
            e.Property(b => b.Id).HasColumnName("id");
            e.Property(b => b.NumeroCuenta).HasColumnName("numero_cuenta").HasMaxLength(10).IsRequired();
            e.Property(b => b.ClienteId).HasColumnName("cliente_id");
            e.Property(b => b.Saldo).HasColumnName("saldo").HasColumnType("numeric(14,2)");
            e.Property(b => b.Moneda).HasColumnName("moneda").HasMaxLength(3);
            e.Property(b => b.FechaCreacion).HasColumnName("fecha_creacion");
            e.HasIndex(b => b.NumeroCuenta).IsUnique();
        });

        mb.Entity<Transaccion>(e =>
        {
            e.ToTable("transacciones");
            e.HasKey(t => t.Id);
            e.Property(t => t.Id).HasColumnName("id");
            e.Property(t => t.BilleteraId).HasColumnName("billetera_id");
            e.Property(t => t.Tipo).HasColumnName("tipo")
             .HasConversion(v => v.ToString(), v => Enum.Parse<TipoTransaccion>(v))
             .HasMaxLength(30);
            e.Property(t => t.Monto).HasColumnName("monto").HasColumnType("numeric(14,2)");
            e.Property(t => t.Comision).HasColumnName("comision").HasColumnType("numeric(14,2)");
            e.Property(t => t.SaldoResultante).HasColumnName("saldo_resultante").HasColumnType("numeric(14,2)");
            e.Property(t => t.BilleteraContraparteId).HasColumnName("billetera_contraparte_id");
            e.Property(t => t.Descripcion).HasColumnName("descripcion").HasMaxLength(140);
            e.Property(t => t.Referencia).HasColumnName("referencia");
            e.Property(t => t.Fecha).HasColumnName("fecha");

            e.HasOne<Billetera>().WithMany().HasForeignKey(t => t.BilleteraId);
            e.HasOne(t => t.BilleteraContraparte).WithMany().HasForeignKey(t => t.BilleteraContraparteId);
            e.HasIndex(t => new { t.BilleteraId, t.Fecha });
        });

        mb.Entity<MovimientoPuntos>(e =>
        {
            e.ToTable("puntos_fidelidad");
            e.HasKey(p => p.Id);
            e.Property(p => p.Id).HasColumnName("id");
            e.Property(p => p.ClienteId).HasColumnName("cliente_id");
            e.Property(p => p.Referencia).HasColumnName("referencia");
            e.Property(p => p.TipoOperacion).HasColumnName("tipo_operacion")
             .HasConversion(v => v.ToString(), v => Enum.Parse<TipoTransaccion>(v))
             .HasMaxLength(30);
            e.Property(p => p.Monto).HasColumnName("monto").HasColumnType("numeric(14,2)");
            e.Property(p => p.Puntos).HasColumnName("puntos");
            e.Property(p => p.Fecha).HasColumnName("fecha");

            e.HasOne<Cliente>().WithMany().HasForeignKey(p => p.ClienteId);
            e.HasIndex(p => p.Referencia).IsUnique();       // idempotencia
            e.HasIndex(p => new { p.ClienteId, p.Fecha });
        });

        mb.Entity<RegistroIdempotencia>(e =>
        {
            e.ToTable("idempotencia");
            e.HasKey(r => new { r.ClienteId, r.Clave });     // ← la garantía contra la carrera
            e.Property(r => r.ClienteId).HasColumnName("cliente_id");
            e.Property(r => r.Clave).HasColumnName("clave").HasMaxLength(64);
            e.Property(r => r.Ruta).HasColumnName("ruta").HasMaxLength(100);
            e.Property(r => r.HashCuerpo).HasColumnName("hash_cuerpo").HasMaxLength(64);
            e.Property(r => r.Estado).HasColumnName("estado")
             .HasConversion(v => v.ToString(), v => Enum.Parse<EstadoIdempotencia>(v))
             .HasMaxLength(12);
            e.Property(r => r.CodigoHttp).HasColumnName("codigo_http");
            e.Property(r => r.Respuesta).HasColumnName("respuesta").HasColumnType("jsonb");
            e.Property(r => r.TipoContenido).HasColumnName("tipo_contenido").HasMaxLength(60);
            e.Property(r => r.FechaCreacion).HasColumnName("fecha_creacion");
        });
    }
}
