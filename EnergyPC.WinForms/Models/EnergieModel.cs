namespace EnergyPC.WinForms.Models;

/// <summary>Énergie cumulée renvoyée par l'API (miroir de EnergieDto).</summary>
public record EnergieModel(string Code, int FenetreMinutes, double Wh);
