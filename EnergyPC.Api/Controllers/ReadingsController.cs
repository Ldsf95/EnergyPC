using EnergyPC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnergyPC.Api.Controllers;

/// <summary>Lecture des relevés : temps réel, historique et énergie cumulée.</summary>
[ApiController]
[Route("api/[controller]")]
public class ReadingsController : ControllerBase
{
    private readonly EnergyDbContext _db;
    public ReadingsController(EnergyDbContext db) => _db = db;

    /// <summary>Dernier relevé pour chaque capteur (snapshot temps réel).</summary>
    [HttpGet("latest")]
    public async Task<ActionResult<IEnumerable<ReadingDto>>> Latest()
    {
        var result = await _db.SensorTypes
            .Select(st => new ReadingDto(
                st.Code, st.Libelle, st.Unite, st.Famille,
                st.Readings.OrderByDescending(r => r.Horodatage)
                    .Select(r => (double?)r.Valeur).FirstOrDefault(),
                st.Readings.OrderByDescending(r => r.Horodatage)
                    .Select(r => (DateTime?)r.Horodatage).FirstOrDefault()))
            .ToListAsync();

        return result;
    }

    /// <summary>Historique brut d'un capteur sur une fenêtre glissante (minutes).</summary>
    [HttpGet("history")]
    public async Task<ActionResult<IEnumerable<PointDto>>> History(
        [FromQuery] string code,
        [FromQuery] int minutes = 5)
    {
        var debut = DateTime.UtcNow.AddMinutes(-minutes);
        return await _db.Readings
            .Where(r => r.SensorType!.Code == code && r.Horodatage >= debut)
            .OrderBy(r => r.Horodatage)
            .Select(r => new PointDto(r.Horodatage, r.Valeur))
            .ToListAsync();
    }

    /// <summary>
    /// Historique « downsamplé » à la minute (moyennes) — utile pour les
    /// fenêtres longues (24 h) afin d'éviter de renvoyer des dizaines de
    /// milliers de points.
    /// </summary>
    [HttpGet("history-minute")]
    public async Task<ActionResult<IEnumerable<PointDto>>> HistoryMinute(
        [FromQuery] string code,
        [FromQuery] int minutes = 24 * 60)
    {
        var debut = DateTime.UtcNow.AddMinutes(-minutes);
        return await _db.Readings
            .Where(r => r.SensorType!.Code == code && r.Horodatage >= debut)
            .GroupBy(r => new DateTime(
                r.Horodatage.Year, r.Horodatage.Month, r.Horodatage.Day,
                r.Horodatage.Hour, r.Horodatage.Minute, 0))
            .Select(g => new PointDto(g.Key, g.Average(x => x.Valeur)))
            .OrderBy(p => p.Horodatage)
            .ToListAsync();
    }

    /// <summary>
    /// Énergie cumulée (Wh) sur une fenêtre, calculée par intégration
    /// (méthode des trapèzes) à partir de la puissance (W).
    /// </summary>
    [HttpGet("energie")]
    public async Task<ActionResult<EnergieDto>> Energie(
        [FromQuery] string code, [FromQuery] int minutes = 60)
    {
        var debut = DateTime.UtcNow.AddMinutes(-minutes);
        var points = await _db.Readings
            .Where(r => r.SensorType!.Code == code && r.Horodatage >= debut)
            .OrderBy(r => r.Horodatage)
            .Select(r => new { r.Horodatage, r.Valeur })
            .ToListAsync();

        double wh = 0;
        for (int i = 1; i < points.Count; i++)
        {
            var dt = (points[i].Horodatage - points[i - 1].Horodatage).TotalHours;
            var moy = (points[i].Valeur + points[i - 1].Valeur) / 2.0;
            wh += moy * dt; // intégration trapèze
        }

        return new EnergieDto(code, minutes, wh);
    }
}
