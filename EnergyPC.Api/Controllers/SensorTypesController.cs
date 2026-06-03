using EnergyPC.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace EnergyPC.Api.Controllers;

/// <summary>Liste des grandeurs mesurées (types de capteurs).</summary>
[ApiController]
[Route("api/[controller]")]
public class SensorTypesController : ControllerBase
{
    private readonly EnergyDbContext _db;
    public SensorTypesController(EnergyDbContext db) => _db = db;

    /// <summary>Retourne tous les types de capteurs configurés.</summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<SensorTypeDto>>> Get()
        => await _db.SensorTypes
            .Select(s => new SensorTypeDto(s.Id, s.Code, s.Libelle, s.Unite, s.Famille))
            .ToListAsync();
}
