using Microsoft.EntityFrameworkCore;

namespace TarjetaSUBE;

public class SubeDbContext : DbContext
{
    public SubeDbContext() { }

    public SubeDbContext(DbContextOptions<SubeDbContext> opciones) : base(opciones) { }

    public DbSet<Tarjeta> Tarjetas => Set<Tarjeta>();
    public DbSet<TarjetaTipo> TarjetasTipo => Set<TarjetaTipo>();
    public DbSet<Colectivo> Colectivos => Set<Colectivo>();
    public DbSet<Boleto> Boletos => Set<Boleto>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite("Data Source=sube.db");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tarjeta>()
            .HasOne(t => t.TarjetaTipo)
            .WithMany(t => t.Tarjetas)
            .HasForeignKey(t => t.TarjetaTipoId);

        modelBuilder.Entity<Tarjeta>()
            .Property(t => t.Saldo)
            .HasPrecision(18, 2);

        modelBuilder.Entity<TarjetaTipo>()
            .Property(t => t.Proporcion)
            .HasPrecision(18, 2);

        modelBuilder.Entity<Boleto>()
            .Property(b => b.Monto)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Boleto>()
            .Property(b => b.Saldo)
            .HasPrecision(18, 2);
        modelBuilder.Entity<Boleto>()
            .Property(b => b.SaldoRestante)
            .HasPrecision(18, 2);
    }
}
