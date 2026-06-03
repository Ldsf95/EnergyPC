namespace EnergyPC.WinForms.Helpers;

/// <summary>Helpers de formatage d'affichage (W, Wh, %, valeurs absentes).</summary>
public static class Format
{
    public static string Valeur(double? v) => v.HasValue ? $"{v.Value:N1}" : "—";
    public static string Watts(double? w) => w.HasValue ? $"{w.Value:N1} W" : "—";
    public static string Wh(double wh) => $"{wh:N1} Wh";
    public static string Kwh(double wh) => $"{wh / 1000:N2} kWh";
    public static string Pourcent(double? p) => p.HasValue ? $"{p.Value:N0} %" : "—";
}
