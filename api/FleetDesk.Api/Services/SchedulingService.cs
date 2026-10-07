using FleetDesk.Api.Data;
using FleetDesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Services;

/// <summary>Detects crew double-booking across overlapping jobs.</summary>
public class SchedulingService(AppDbContext db)
{
    public static bool Overlaps(DateTime startA, DateTime endA, DateTime startB, DateTime endB)
        => startA < endB && startB < endA;

    public async Task<List<Job>> FindConflictsAsync(
        int crewMemberId,
        DateTime start,
        DateTime end,
        int? excludeJobId = null,
        CancellationToken ct = default)
    {
        return await db.JobAssignments
            .Where(a => a.CrewMemberId == crewMemberId)
            .Where(a => excludeJobId == null || a.JobId != excludeJobId)
            .Select(a => a.Job!)
            .Where(j => j.Status != JobStatus.Cancelled && j.Status != JobStatus.Completed)
            .Where(j => j.ScheduledStart < end && j.ScheduledEnd > start)
            .ToListAsync(ct);
    }
}
