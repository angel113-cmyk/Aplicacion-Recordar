using Microsoft.EntityFrameworkCore;

namespace Suelta.Api.Datos;

/// <summary>Una persona con respaldo. Se identifica solo por la huella de su código de recuperación (HT-09).</summary>
public class Cuenta
{
    public Guid Id { get; set; }

    /// <summary>SHA-256 del código. El código en sí nunca se guarda.</summary>
    public string CodigoHash { get; set; } = "";

    public DateTime CreadaEnUtc { get; set; }
}

/// <summary>El espejo en el servidor de un recordatorio del teléfono (HT-10).</summary>
public class RecordatorioRemoto
{
    public Guid CuentaId { get; set; }
    public Guid Id { get; set; }

    public string Titulo { get; set; } = "";
    public int Categoria { get; set; }
    public DateTime? FechaHora { get; set; }
    public bool SinHora { get; set; }
    public int Repeticion { get; set; }
    public int? AnclaDia { get; set; }
    public decimal? Monto { get; set; }
    public string Dijiste { get; set; } = "";
    public bool Hecho { get; set; }
    public DateTime? PospuestoHasta { get; set; }

    public DateTime CreadoEnUtc { get; set; }
    public DateTime ActualizadoEnUtc { get; set; }

    /// <summary>Marca de que se eliminó: así los demás teléfonos se enteran.</summary>
    public DateTime? EliminadoEnUtc { get; set; }

    /// <summary>Cuándo lo recibió el servidor (reloj del servidor). Sirve para pedir "lo nuevo desde...".</summary>
    public DateTime RecibidoEnUtc { get; set; }
}

public class ApiDb(DbContextOptions<ApiDb> opciones) : DbContext(opciones)
{
    public DbSet<Cuenta> Cuentas => Set<Cuenta>();
    public DbSet<RecordatorioRemoto> Recordatorios => Set<RecordatorioRemoto>();

    protected override void OnModelCreating(ModelBuilder modelo)
    {
        modelo.Entity<Cuenta>(c =>
        {
            c.HasKey(x => x.Id);
            c.HasIndex(x => x.CodigoHash).IsUnique();
            c.Property(x => x.CodigoHash).HasMaxLength(64);
        });

        modelo.Entity<RecordatorioRemoto>(r =>
        {
            r.HasKey(x => new { x.CuentaId, x.Id });
            r.HasIndex(x => new { x.CuentaId, x.FechaHora });
            r.HasIndex(x => new { x.CuentaId, x.RecibidoEnUtc });
            r.Property(x => x.Titulo).HasMaxLength(Limites.Titulo);
            r.Property(x => x.Dijiste).HasMaxLength(Limites.Dijiste);
            r.HasOne<Cuenta>().WithMany().HasForeignKey(x => x.CuentaId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}

public static class Limites
{
    public const int Titulo = 200;
    public const int Dijiste = 1000;
    public const int CambiosPorPeticion = 2000;
}
