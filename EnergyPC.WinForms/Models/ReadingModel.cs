namespace EnergyPC.WinForms.Models;

/// <summary>Miroir client du ReadingDto renvoyé par l'API.</summary>
public record ReadingModel(string Code, string Libelle, string Unite,
    string Famille, double? Valeur, DateTime? Horodatage);
