using FleetDesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Data;

public static class Seeder
{
    public static async Task SeedAsync(AppDbContext db, IConfiguration config)
    {
        if (!await db.Users.AnyAsync())
        {
            var email = config["Seed:AdminEmail"] ?? "admin@fleetdesk.local";
            var password = config["Seed:AdminPassword"] ?? "ChangeMe123!";
            db.Users.Add(new User
            {
                Email = email.ToLowerInvariant(),
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                Role = UserRole.Admin
            });
        }

        if (!await db.Vessels.AnyAsync())
        {
            var tug = new Vessel { Name = "MV Harbour Star", ImoNumber = "IMO9000001", VesselType = "Tugboat" };
            var osv = new Vessel { Name = "MV North Wind", ImoNumber = "IMO9000002", VesselType = "Offshore Service Vessel" };
            db.Vessels.AddRange(tug, osv);

            db.CrewMembers.AddRange(
                new CrewMember { Name = "A. Singh", Rank = "Captain" },
                new CrewMember { Name = "R. Fernandes", Rank = "First Mate" },
                new CrewMember { Name = "L. Kowalski", Rank = "Engineer" });

            var start = DateTime.UtcNow.Date.AddHours(8);
            db.Jobs.AddRange(
                new Job { Title = "Hull inspection", Vessel = tug, Priority = JobPriority.Normal, ScheduledStart = start.AddDays(1), ScheduledEnd = start.AddDays(1).AddHours(6) },
                new Job { Title = "Engine service", Vessel = osv, Priority = JobPriority.High, ScheduledStart = start.AddDays(2), ScheduledEnd = start.AddDays(2).AddHours(8) });
        }

        await db.SaveChangesAsync();
    }
}
