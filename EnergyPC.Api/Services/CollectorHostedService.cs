using System.Diagnostics;
using EnergyPC.Collector;
using EnergyPC.Data;
using EnergyPC.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnergyPC.Api.Services;

/// <summary>
/// Service de collecte hébergé : tourne en boucle pendant toute la durée de vie
/// de l'API. À chaque intervalle, lit les capteurs et persiste les relevés.
/// Robustesse : chaque tour est isolé dans un try/catch qui loggue sans propager.
/// </summary>
public class CollectorHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly ILogger<CollectorHostedService> _log;
    private readonly TimeSpan _intervalle;

    public CollectorHostedService(IServiceScopeFactory scopes,
        IConfiguration cfg, ILogger<CollectorHostedService> log)
    {
        _scopes = scopes;
        _log = log;
        _intervalle = TimeSpan.FromSeconds(
            cfg.GetValue("Collector:IntervalSeconds", 2));
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        // L'instance Computer (via HardwareCollector) est créée une fois puis
        // réutilisée. Elle est (re)créée paresseusement dans la boucle afin que
        // l'échec d'initialisation matérielle ne fasse jamais tomber l'API.
        IHardwareCollector? collector = null;
        try
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    collector ??= CreerCollecteur();
                    await CollectOnceAsync(collector, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break; // arrêt normal
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, "Collecte échouée — réinitialisation du collecteur au prochain tour");
                    // Recréation du collecteur si l'instance est dans un état incohérent
                    // (ex. après une mise en veille prolongée, ou capteur indisponible).
                    try { collector?.Dispose(); } catch { /* ignore */ }
                    collector = null;
                }

                try { await Task.Delay(_intervalle, ct); }
                catch (OperationCanceledException) { break; }
            }
        }
        finally
        {
            collector?.Dispose();
        }
    }

    /// <summary>Point d'extension : surchargé dans les tests pour injecter un mock.</summary>
    protected virtual IHardwareCollector CreerCollecteur() => new HardwareCollector();

    private async Task CollectOnceAsync(IHardwareCollector c, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();

        using var scope = _scopes.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<EnergyDbContext>();

        var types = await db.SensorTypes.ToDictionaryAsync(t => t.Code, ct);
        var maintenant = DateTime.UtcNow;

        int n = 0;
        foreach (var (code, val) in c.Lire())
        {
            if (types.TryGetValue(code, out var st))
            {
                db.Readings.Add(new Reading
                {
                    Horodatage = maintenant,
                    SensorTypeId = st.Id,
                    Valeur = val
                });
                n++;
            }
        }

        await db.SaveChangesAsync(ct);
        sw.Stop();

        _log.LogInformation("Tour de collecte : {N} valeurs en {Ms} ms",
            n, sw.ElapsedMilliseconds);
    }
}
