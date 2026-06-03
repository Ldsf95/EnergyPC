using EnergyPC.Api;
using EnergyPC.Api.Controllers;
using EnergyPC.Data;
using EnergyPC.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnergyPC.Tests;

public class ReadingsControllerTests
{
    private static EnergyDbContext NouvelleBase()
    {
        var opts = new DbContextOptionsBuilder<EnergyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new EnergyDbContext(opts);
        SeedData.Initialize(db);
        return db;
    }

    [Fact]
    public async Task Latest_RetourneTousLesSensorTypes()
    {
        using var db = NouvelleBase();
        var ctrl = new ReadingsController(db);

        var res = await ctrl.Latest();

        Assert.NotNull(res.Value);
        Assert.True(res.Value!.Count() >= 10);
    }

    [Fact]
    public async Task Energie_IntegrationTrapeze_Correct()
    {
        using var db = NouvelleBase();
        var t0 = DateTime.UtcNow.AddMinutes(-30);
        var st = db.SensorTypes.First(s => s.Code == "SYSTEM_POWER");

        // 60 minutes à 50 W constants -> 50 Wh attendus.
        for (int i = 0; i <= 60; i++)
            db.Readings.Add(new Reading
            {
                SensorTypeId = st.Id,
                Horodatage = t0.AddMinutes(i),
                Valeur = 50.0
            });
        await db.SaveChangesAsync();

        var ctrl = new ReadingsController(db);
        var res = await ctrl.Energie("SYSTEM_POWER", 120);

        Assert.NotNull(res.Value);
        Assert.InRange(res.Value!.Wh, 49.5, 50.5);
    }

    [Fact]
    public async Task SensorTypes_Get_RetourneLes12Defauts()
    {
        using var db = NouvelleBase();
        var ctrl = new SensorTypesController(db);

        var res = await ctrl.Get();

        Assert.NotNull(res.Value);
        Assert.Equal(SeedData.Defauts.Length, res.Value!.Count());
    }
}
