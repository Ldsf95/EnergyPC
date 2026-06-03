using EnergyPC.Data;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace EnergyPC.Tests;

public class SeedDataTests
{
    [Fact]
    public void Initialize_EstIdempotent()
    {
        var opts = new DbContextOptionsBuilder<EnergyDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        using var db = new EnergyDbContext(opts);

        // Deux initialisations successives ne doivent pas dupliquer les types.
        SeedData.Initialize(db);
        SeedData.Initialize(db);

        Assert.Equal(SeedData.Defauts.Length, db.SensorTypes.Count());
    }
}
