using EnergyPC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnergyPC.Api.Controllers;

/// <summary>Maintenance de la base : purge des relevés anciens (Numérique Responsable).</summary>
[ApiController]
[Route("api/[controller]")]
public class MaintenanceController : ControllerBase
{
    /// <summary>
    /// Supprime les relevés de plus de <paramref name="jours"/> jours afin
    /// d'empêcher la base de gonfler indéfiniment.
    /// </summary>
    [HttpDelete("purge")]
    public async Task<IActionResult> Purger([FromServices] EnergyDbContext db,
        [FromQuery] int jours = 7)
    {
        var limite = DateTime.UtcNow.AddDays(-jours);
        var n = await db.Readings.Where(r => r.Horodatage < limite)
            .ExecuteDeleteAsync();
        return Ok(new { Supprimees = n });
    }
}
