using EnergyPC.Data.Entities;

namespace EnergyPC.Data;

/// <summary>
/// Pré-peuplement des types de capteurs attendus par l'application.
/// Idempotent : n'ajoute que les codes absents.
/// </summary>
public static class SeedData
{
    public static readonly (string Code, string Libelle,
        string Unite, string Famille)[] Defauts =
    {
        ("CPU_LOAD",     "Charge CPU",        "%",    "CPU"),
        ("CPU_POWER",    "Puissance CPU",     "W",    "CPU"),
        ("CPU_TEMP",     "Température CPU",    "°C",   "CPU"),
        ("GPU_LOAD",     "Charge GPU",        "%",    "GPU"),
        ("GPU_POWER",    "Puissance GPU",     "W",    "GPU"),
        ("GPU_TEMP",     "Température GPU",    "°C",   "GPU"),
        ("RAM_USED",     "Mémoire utilisée",  "Mo",   "RAM"),
        ("DISK_READ",    "Lecture disque",    "Mo/s", "DISK"),
        ("DISK_WRITE",   "Écriture disque",   "Mo/s", "DISK"),
        ("BATTERY_PCT",  "Batterie",          "%",    "BATTERY"),
        ("BATTERY_DRAIN","Décharge batterie", "W",    "BATTERY"),
        ("SYSTEM_POWER", "Puissance totale",  "W",    "SYSTEM"),
    };

    public static void Initialize(EnergyDbContext db)
    {
        foreach (var (code, lib, u, fam) in Defauts)
            if (!db.SensorTypes.Any(s => s.Code == code))
                db.SensorTypes.Add(new SensorType
                { Code = code, Libelle = lib, Unite = u, Famille = fam });

        db.SaveChanges();
    }
}
