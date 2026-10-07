using FleetDesk.Api.Models;
using FleetDesk.Api.Services;

namespace FleetDesk.Tests;

public class SchedulingServiceTests
{
    private static readonly DateTime T0 = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, 4, 2, 6, true)]   // partial overlap
    [InlineData(0, 4, 4, 8, false)]  // back-to-back is fine
    [InlineData(0, 4, 5, 8, false)]  // disjoint
    [InlineData(0, 8, 2, 4, true)]   // containment
    public void Overlaps_detects_time_window_clashes(int s1, int e1, int s2, int e2, bool expected)
    {
        var result = SchedulingService.Overlaps(T0.AddHours(s1), T0.AddHours(e1), T0.AddHours(s2), T0.AddHours(e2));
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task FindConflicts_returns_overlapping_job_for_same_crew_member()
    {
        await using var db = TestHelpers.NewDb();
        var vessel = new Vessel { Name = "MV Test", ImoNumber = "IMO1", VesselType = "Tug" };
        var crew = new CrewMember { Name = "A. Singh", Rank = "Captain" };
        var existing = new Job { Title = "Existing", Vessel = vessel, ScheduledStart = T0, ScheduledEnd = T0.AddHours(4) };
        db.JobAssignments.Add(new JobAssignment { Job = existing, CrewMember = crew });
        await db.SaveChangesAsync();

        var service = new SchedulingService(db);
        var conflicts = await service.FindConflictsAsync(crew.Id, T0.AddHours(2), T0.AddHours(6));

        Assert.Single(conflicts);
        Assert.Equal("Existing", conflicts[0].Title);
    }

    [Fact]
    public async Task FindConflicts_ignores_cancelled_and_completed_jobs()
    {
        await using var db = TestHelpers.NewDb();
        var vessel = new Vessel { Name = "MV Test", ImoNumber = "IMO1", VesselType = "Tug" };
        var crew = new CrewMember { Name = "A. Singh", Rank = "Captain" };
        db.JobAssignments.AddRange(
            new JobAssignment { Job = new Job { Title = "Cancelled", Vessel = vessel, Status = JobStatus.Cancelled, ScheduledStart = T0, ScheduledEnd = T0.AddHours(4) }, CrewMember = crew },
            new JobAssignment { Job = new Job { Title = "Done", Vessel = vessel, Status = JobStatus.Completed, ScheduledStart = T0, ScheduledEnd = T0.AddHours(4) }, CrewMember = crew });
        await db.SaveChangesAsync();

        var conflicts = await new SchedulingService(db).FindConflictsAsync(crew.Id, T0, T0.AddHours(4));

        Assert.Empty(conflicts);
    }

    [Fact]
    public async Task FindConflicts_excludes_the_job_being_edited()
    {
        await using var db = TestHelpers.NewDb();
        var vessel = new Vessel { Name = "MV Test", ImoNumber = "IMO1", VesselType = "Tug" };
        var crew = new CrewMember { Name = "A. Singh", Rank = "Captain" };
        var job = new Job { Title = "Self", Vessel = vessel, ScheduledStart = T0, ScheduledEnd = T0.AddHours(4) };
        db.JobAssignments.Add(new JobAssignment { Job = job, CrewMember = crew });
        await db.SaveChangesAsync();

        var conflicts = await new SchedulingService(db).FindConflictsAsync(crew.Id, T0, T0.AddHours(4), excludeJobId: job.Id);

        Assert.Empty(conflicts);
    }
}
