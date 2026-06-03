namespace EnergyPC.Collector;

/// <summary>
/// Abstraction de la source matérielle. Permet de remplacer la lecture réelle
/// par un mock dans les tests (séparation des préoccupations).
/// </summary>
public interface IHardwareCollector : IDisposable
{
    /// <summary>
    /// Retourne les relevés disponibles à l'instant t sous forme
    /// (Code SensorType, Valeur). Les capteurs absents sont simplement omis
    /// (aucune exception levée).
    /// </summary>
    IEnumerable<(string Code, double Valeur)> Lire();
}
