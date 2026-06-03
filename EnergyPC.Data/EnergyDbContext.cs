using EnergyPC.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnergyPC.Data;

/// <summary>
/// Contexte EF Core (Code First) de la série temporelle énergétique.
/// </summary>
public class EnergyDbContext : DbContext
{
    public EnergyDbContext(DbContextOptions<EnergyDbContext> options)
        : base(options) { }

    public DbSet<SensorType> SensorTypes => Set<SensorType>();
    public DbSet<Reading> Readings => Set<Reading>();

    protected override void OnModelCreating(ModelBuilder mb)
    {
        // Code capteur unique.
        mb.Entity<SensorType>().HasIndex(s => s.Code).IsUnique();

        // Index composite pour accélérer les requêtes "dernier / historique".
        mb.Entity<Reading>()
          .HasIndex(r => new { r.SensorTypeId, r.Horodatage });
    }
}
