namespace EnergyPC.Data.Entities;

/// <summary>
/// Une valeur mesurée et horodatée pour un <see cref="SensorType"/>.
/// </summary>
public class Reading
{
    public long Id { get; set; }

    /// <summary>Instant de la mesure (UTC).</summary>
    public DateTime Horodatage { get; set; }

    /// <summary>Valeur mesurée (dans l'unité du SensorType).</summary>
    public double Valeur { get; set; }

    public int SensorTypeId { get; set; }
    public SensorType? SensorType { get; set; }
}
