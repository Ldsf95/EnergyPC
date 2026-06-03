#nullable enable
using System.Drawing;
using System.Windows.Forms;

namespace EnergyPC.WinForms.Forms;

partial class FormDashboard
{
    private System.ComponentModel.IContainer? components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && components != null) components.Dispose();
        base.Dispose(disposing);
    }

    /// <summary>
    /// Composition de l'écran en trois zones :
    /// 1) rangée de cartes KPI, 2) graphique temps réel, 3) bandeau énergie + statut.
    /// </summary>
    private void InitializeComponent()
    {
        Text = "Energy PC - Dashboard";
        WindowState = FormWindowState.Maximized;
        Font = new Font("Segoe UI", 10);
        BackColor = Color.FromArgb(245, 245, 248);

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(8)
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 150));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 70));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 80));
        Controls.Add(layout);

        _kpiRow = BuildKpiRow();
        _chart = BuildChart();
        _energyRow = BuildEnergyRow();

        layout.Controls.Add(_kpiRow, 0, 0);
        layout.Controls.Add(_chart, 0, 1);
        layout.Controls.Add(_energyRow, 0, 2);

        Load += FormDashboard_Load;
    }
}
