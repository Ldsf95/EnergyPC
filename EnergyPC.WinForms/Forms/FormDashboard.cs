using System.Collections.ObjectModel;
using System.Drawing;
using System.Windows.Forms;
using EnergyPC.WinForms.Controls;
using EnergyPC.WinForms.Helpers;
using EnergyPC.WinForms.Models;
using EnergyPC.WinForms.Services;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.WinForms;

namespace EnergyPC.WinForms.Forms;

/// <summary>
/// Écran principal du dashboard temps réel : cartes KPI, graphique défilant
/// (60–120 derniers points) et compteurs d'énergie cumulée.
/// </summary>
public partial class FormDashboard : Form
{
    private readonly EnergyService _service;
    private readonly double _seuilSystemPower;

    private TableLayoutPanel _kpiRow = null!;
    private CartesianChart _chart = null!;
    private Panel _energyRow = null!;

    private Dictionary<string, KpiCard> _kpis = new();

    private readonly ObservableCollection<DateTimePoint> _serieCpu = new();
    private readonly ObservableCollection<DateTimePoint> _serieGpu = new();

    private readonly System.Windows.Forms.Timer _timer = new() { Interval = 1000 };
    private readonly System.Windows.Forms.Timer _timerEnergie = new() { Interval = 60_000 };
    private readonly DateTime _debutSession = DateTime.UtcNow;

    private Label _lblStatus = null!;
    private Label _lblWhHeure = null!;
    private Label _lblWhJour = null!;

    public FormDashboard(EnergyService service, double seuilSystemPower)
    {
        _service = service;
        _seuilSystemPower = seuilSystemPower;
        InitializeComponent();
    }

    // --- Zone 1 : cartes KPI -------------------------------------------------
    private TableLayoutPanel BuildKpiRow()
    {
        var row = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 7,
            RowCount = 1
        };
        for (int i = 0; i < 7; i++)
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / 7));

        _kpis = new Dictionary<string, KpiCard>
        {
            ["CPU_LOAD"]    = new KpiCard("Charge CPU"),
            ["CPU_POWER"]   = new KpiCard("Puissance CPU"),
            ["GPU_LOAD"]    = new KpiCard("Charge GPU"),
            ["GPU_POWER"]   = new KpiCard("Puissance GPU"),
            ["RAM_USED"]    = new KpiCard("Mémoire"),
            ["BATTERY_PCT"] = new KpiCard("Batterie"),
            ["SYSTEM_POWER"]= new KpiCard("Total estimé"),
        };

        foreach (var k in _kpis.Values)
        {
            k.Dock = DockStyle.Fill;
            row.Controls.Add(k);
        }
        return row;
    }

    // --- Zone 2 : graphique temps réel --------------------------------------
    private CartesianChart BuildChart()
    {
        var c = new CartesianChart { Dock = DockStyle.Fill };
        c.Series = new ISeries[]
        {
            new LineSeries<DateTimePoint> { Values = _serieCpu, Name = "CPU (W)" },
            new LineSeries<DateTimePoint> { Values = _serieGpu, Name = "GPU (W)" }
        };
        c.XAxes = new[]
        {
            new Axis
            {
                Labeler = v => new DateTime((long)v).ToString("HH:mm:ss"),
                LabelsRotation = 0
            }
        };
        return c;
    }

    // --- Zone 3 : bandeau énergie + statut ----------------------------------
    private Panel BuildEnergyRow()
    {
        var panel = new Panel { Dock = DockStyle.Fill };

        _lblWhHeure = new Label
        {
            Text = "1h : —",
            Location = new Point(12, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        _lblWhJour = new Label
        {
            Text = "24h : —",
            Location = new Point(220, 10),
            AutoSize = true,
            Font = new Font("Segoe UI", 12, FontStyle.Bold)
        };
        _lblStatus = new Label
        {
            Text = "Connexion à l'API…",
            Location = new Point(12, 44),
            AutoSize = true,
            ForeColor = Color.DimGray
        };

        panel.Controls.AddRange(new Control[] { _lblWhHeure, _lblWhJour, _lblStatus });
        return panel;
    }

    // --- Cycle de vie / rafraîchissement ------------------------------------
    private async void FormDashboard_Load(object? s, EventArgs e)
    {
        _timer.Tick += async (_, __) => await RafraichirAsync();
        _timer.Start();

        _timerEnergie.Tick += (_, __) => MajEnergie();
        _timerEnergie.Start();

        await RafraichirAsync();
        MajEnergie();
    }

    /// <summary>Mise à jour des KPI et du graphique (1 Hz, sur le thread UI).</summary>
    private async Task RafraichirAsync()
    {
        try
        {
            var latest = await _service.GetLatestAsync() ?? new();

            foreach (var r in latest)
                if (_kpis.TryGetValue(r.Code, out var card))
                    card.Mettre(r.Valeur, r.Unite);

            // Détection de pic par seuil sur la puissance totale.
            var sys = latest.FirstOrDefault(r => r.Code == "SYSTEM_POWER");
            if (_kpis.TryGetValue("SYSTEM_POWER", out var sysCard))
                sysCard.Alerte(sys?.Valeur is double sv && sv > _seuilSystemPower);

            // Gestion de l'absence de batterie : on masque la carte.
            var bat = latest.FirstOrDefault(r => r.Code == "BATTERY_PCT");
            if (_kpis.TryGetValue("BATTERY_PCT", out var batCard))
                batCard.Visible = bat?.Valeur is not null;

            // Alimentation du graphique défilant.
            var cpu = latest.FirstOrDefault(r => r.Code == "CPU_POWER");
            if (cpu?.Valeur is double cv)
                AjouterPoint(_serieCpu, cpu.Horodatage ?? DateTime.Now, cv);

            var gpu = latest.FirstOrDefault(r => r.Code == "GPU_POWER");
            if (gpu?.Valeur is double gv)
                AjouterPoint(_serieGpu, gpu.Horodatage ?? DateTime.Now, gv);

            _lblStatus.Text = $"Dernière mise à jour : {DateTime.Now:HH:mm:ss}";
            _lblStatus.ForeColor = Color.DimGray;
        }
        catch (Exception ex)
        {
            // L'UI reste utilisable même si l'API est arrêtée.
            _lblStatus.Text = $"API injoignable : {ex.Message}";
            _lblStatus.ForeColor = Color.Firebrick;
        }
    }

    /// <summary>Ajoute un point à une série et tronque aux 120 derniers (rolling).</summary>
    private static void AjouterPoint(ObservableCollection<DateTimePoint> serie,
        DateTime t, double v)
    {
        serie.Add(new DateTimePoint(t, v));
        while (serie.Count > 120) serie.RemoveAt(0);
    }

    /// <summary>Met à jour les compteurs d'énergie (1h et 24h) toutes les minutes.</summary>
    private async void MajEnergie()
    {
        try
        {
            var heure = await _service.GetEnergieAsync("SYSTEM_POWER", 60);
            var jour = await _service.GetEnergieAsync("SYSTEM_POWER", 24 * 60);

            _lblWhHeure.Text = $"1h : {Format.Wh(heure?.Wh ?? 0)}";
            _lblWhJour.Text = $"24h : {Format.Kwh(jour?.Wh ?? 0)}";
        }
        catch
        {
            // Silencieux : le bandeau de statut signale déjà l'indisponibilité.
        }
    }
}
