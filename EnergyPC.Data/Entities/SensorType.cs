namespace EnergyPC.Data.Entities;

/// <summary>
/// Décrit une grandeur mesurée (ce que l'on mesure).
/// Modèle générique : ajouter une grandeur ne change pas le schéma.
/// </summary>
public class SensorType
{
    public int Id { get; set; }

    /// <summary>Code technique unique, ex. CPU_POWER.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Libellé lisible, ex. Puissance CPU.</summary>
    public string Libelle { get; set; } = string.Empty;

    /// <summary>Unité de mesure : W, %, °C, Mo…</summary>
    public string Unite { get; set; } = string.Empty;

    /// <summary>Famille de capteur : CPU, GPU, RAM, BATTERY…</summary>
    public string Famille { get; set; } = string.Empty;

    /// <summary>Relevés horodatés rattachés à ce type de capteur.</summary>
    public ICollection<Reading> Readings { get; set; } = new List<Reading>();
}
