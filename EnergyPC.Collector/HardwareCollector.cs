using LibreHardwareMonitor.Hardware;
using DataSensorType = EnergyPC.Data.Entities.SensorType;

namespace EnergyPC.Collector;

/// <summary>
/// Lecture cyclique des capteurs matériels via LibreHardwareMonitorLib.
/// Encapsule la librairie bas niveau et expose une liste de relevés
/// prêts à insérer en base.
/// </summary>
/// <remarks>
/// Nécessite des privilèges administrateur pour accéder à certains capteurs
/// (MSR du CPU, SuperI/O de la carte mère).
/// </remarks>
public class HardwareCollector : IHardwareCollector
{
    private readonly Computer _pc;

    public HardwareCollector()
    {
        _pc = new Computer
        {
            IsCpuEnabled = true,
            IsGpuEnabled = true,
            IsMemoryEnabled = true,
            IsStorageEnabled = true,
            IsBatteryEnabled = true,
            IsMotherboardEnabled = true,
        };
        _pc.Open();
    }

    /// <inheritdoc />
    public IEnumerable<(string Code, double Valeur)> Lire()
    {
        foreach (var hw in _pc.Hardware)
        {
            hw.Update();
            foreach (var sub in hw.SubHardware) sub.Update();

            foreach (var capt in hw.Sensors)
            {
                if (!capt.Value.HasValue) continue;

                var code = MapCode(hw.HardwareType, capt);
                if (code is not null)
                    yield return (code, capt.Value!.Value);
            }
        }
    }

    /// <summary>
    /// Fait correspondre un capteur LibreHardwareMonitor à un Code SensorType.
    /// Les noms exacts ("CPU Total", "CPU Package"…) varient selon la marque ;
    /// adapter au besoin après avoir loggé les capteurs réels de la machine.
    /// </summary>
    private static string? MapCode(HardwareType type, ISensor s)
    {
        return (type, s.SensorType, s.Name) switch
        {
            (HardwareType.Cpu, LibreHardwareMonitor.Hardware.SensorType.Load, "CPU Total")
                => "CPU_LOAD",
            (HardwareType.Cpu, LibreHardwareMonitor.Hardware.SensorType.Power, "CPU Package")
                => "CPU_POWER",
            (HardwareType.Cpu, LibreHardwareMonitor.Hardware.SensorType.Temperature, "CPU Package")
                => "CPU_TEMP",

            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel,
                LibreHardwareMonitor.Hardware.SensorType.Load, "GPU Core")
                => "GPU_LOAD",
            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel,
                LibreHardwareMonitor.Hardware.SensorType.Power, _)
                => "GPU_POWER",
            (HardwareType.GpuNvidia or HardwareType.GpuAmd or HardwareType.GpuIntel,
                LibreHardwareMonitor.Hardware.SensorType.Temperature, "GPU Core")
                => "GPU_TEMP",

            (HardwareType.Memory, LibreHardwareMonitor.Hardware.SensorType.Data, "Memory Used")
                => "RAM_USED",

            (HardwareType.Battery, LibreHardwareMonitor.Hardware.SensorType.Level, _)
                => "BATTERY_PCT",
            (HardwareType.Battery, LibreHardwareMonitor.Hardware.SensorType.Power, _)
                => "BATTERY_DRAIN",

            _ => null
        };
    }

    public void Dispose() => _pc.Close();
}
