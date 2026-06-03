using System.Drawing;
using System.Windows.Forms;

namespace EnergyPC.WinForms.Controls;

/// <summary>
/// Carte d'indicateur réutilisable : un titre, une grande valeur et une unité.
/// Peut passer en état "alerte" (fond rouge) lorsqu'un seuil est dépassé.
/// </summary>
public class KpiCard : Panel
{
    private static readonly Color FondNormal = Color.White;
    private static readonly Color FondAlerte = Color.FromArgb(255, 230, 230);

    private readonly Label _titre = new() { Font = new Font("Segoe UI", 9) };
    private readonly Label _valeur = new() { Font = new Font("Segoe UI", 22, FontStyle.Bold) };
    private readonly Label _unite = new() { Font = new Font("Segoe UI", 9) };

    public KpiCard(string titre)
    {
        BackColor = FondNormal;
        BorderStyle = BorderStyle.FixedSingle;
        Padding = new Padding(10);
        Margin = new Padding(6);

        _titre.Text = titre;
        _titre.AutoSize = _valeur.AutoSize = _unite.AutoSize = true;

        Controls.AddRange(new Control[] { _titre, _valeur, _unite });
        Layout += (_, __) => Positionner();
    }

    /// <summary>Met à jour la valeur affichée et son unité.</summary>
    public void Mettre(double? valeur, string unite)
    {
        _valeur.Text = valeur.HasValue ? $"{valeur.Value:N1}" : "—";
        _unite.Text = unite;
    }

    /// <summary>Active ou non l'état d'alerte (dépassement de seuil).</summary>
    public void Alerte(bool actif)
    {
        BackColor = actif ? FondAlerte : FondNormal;
        _valeur.ForeColor = actif ? Color.Firebrick : Color.Black;
    }

    private void Positionner()
    {
        _titre.Location = new Point(10, 8);
        _valeur.Location = new Point(10, 34);
        _unite.Location = new Point(10, Height - 28);
    }
}
