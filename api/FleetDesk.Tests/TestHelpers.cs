using FleetDesk.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Tests;

internal static class TestHelpers
{
    public static AppDbContext NewDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }
}
