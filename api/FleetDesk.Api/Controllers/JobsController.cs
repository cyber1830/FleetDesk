using FleetDesk.Api.Data;
using FleetDesk.Api.Dtos;
using FleetDesk.Api.Models;
using FleetDesk.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Controllers;

[ApiController]
[Route("api/jobs")]
[Authorize]
public class JobsController(AppDbContext db, SchedulingService scheduling) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<JobDto>>> List(
        [FromQuery] JobStatus? status,
        [FromQuery] int? vesselId,
        [FromQuery] string? search,
        CancellationToken ct)
    {
        var query = db.Jobs.AsNoTracking()
            .Include(j => j.Vessel)
            .Include(j => j.Assignments).ThenInclude(a => a.CrewMember)
            .AsQueryable();

        if (status is not null) query = query.Where(j => j.Status == status);
        if (vesselId is not null) query = query.Where(j => j.VesselId == vesselId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(term));
        }

        var jobs = await query.OrderBy(j => j.ScheduledStart).ToListAsync(ct);
        return jobs.Select(ToDto).ToList();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<JobDto>> Get(int id, CancellationToken ct)
    {
        var job = await LoadAsync(id, ct);
        return job is null ? NotFound() : ToDto(job);
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<JobDto>> Create(JobWriteRequest req, CancellationToken ct)
    {
        var error = Validate(req);
        if (error is not null) return BadRequest(new { error });
        if (!await db.Vessels.AnyAsync(v => v.Id == req.VesselId, ct))
            return BadRequest(new { error = "Vessel does not exist." });

        var job = new Job
        {
            Title = req.Title.Trim(),
            Description = req.Description?.Trim(),
            VesselId = req.VesselId,
            Priority = req.Priority,
            ScheduledStart = req.ScheduledStart.UtcDateTime,
            ScheduledEnd = req.ScheduledEnd.UtcDateTime
        };
        db.Jobs.Add(job);
        await db.SaveChangesAsync(ct);

        var created = await LoadAsync(job.Id, ct);
        return CreatedAtAction(nameof(Get), new { id = job.Id }, ToDto(created!));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<JobDto>> Update(int id, JobWriteRequest req, CancellationToken ct)
    {
        var error = Validate(req);
        if (error is not null) return BadRequest(new { error });

        var job = await db.Jobs.Include(j => j.Assignments).SingleOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();
        if (!await db.Vessels.AnyAsync(v => v.Id == req.VesselId, ct))
            return BadRequest(new { error = "Vessel does not exist." });

        var newStart = req.ScheduledStart.UtcDateTime;
        var newEnd = req.ScheduledEnd.UtcDateTime;

        // Re-check crew conflicts if the time window changed.
        foreach (var a in job.Assignments)
        {
            var conflicts = await scheduling.FindConflictsAsync(a.CrewMemberId, newStart, newEnd, id, ct);
            if (conflicts.Count > 0)
                return Conflict(new { error = $"Crew member {a.CrewMemberId} is already booked: {conflicts[0].Title}." });
        }

        job.Title = req.Title.Trim();
        job.Description = req.Description?.Trim();
        job.VesselId = req.VesselId;
        job.Priority = req.Priority;
        job.ScheduledStart = newStart;
        job.ScheduledEnd = newEnd;
        await db.SaveChangesAsync(ct);

        return ToDto((await LoadAsync(id, ct))!);
    }

    [HttpPatch("{id:int}/status")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<JobDto>> SetStatus(int id, StatusRequest req, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([id], ct);
        if (job is null) return NotFound();

        job.Status = req.Status;
        await db.SaveChangesAsync(ct);
        return ToDto((await LoadAsync(id, ct))!);
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var job = await db.Jobs.FindAsync([id], ct);
        if (job is null) return NotFound();

        db.Jobs.Remove(job);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpPost("{id:int}/assignments")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<JobDto>> Assign(int id, AssignRequest req, CancellationToken ct)
    {
        var job = await db.Jobs.Include(j => j.Assignments).SingleOrDefaultAsync(j => j.Id == id, ct);
        if (job is null) return NotFound();
        if (!await db.CrewMembers.AnyAsync(c => c.Id == req.CrewMemberId, ct))
            return BadRequest(new { error = "Crew member does not exist." });
        if (job.Assignments.Any(a => a.CrewMemberId == req.CrewMemberId))
            return Conflict(new { error = "Crew member is already assigned to this job." });

        var conflicts = await scheduling.FindConflictsAsync(
            req.CrewMemberId, job.ScheduledStart, job.ScheduledEnd, id, ct);
        if (conflicts.Count > 0)
            return Conflict(new { error = $"Crew member is already booked on \"{conflicts[0].Title}\" during this time." });

        db.JobAssignments.Add(new JobAssignment { JobId = id, CrewMemberId = req.CrewMemberId });
        await db.SaveChangesAsync(ct);
        return ToDto((await LoadAsync(id, ct))!);
    }

    [HttpDelete("{id:int}/assignments/{crewMemberId:int}")]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<JobDto>> Unassign(int id, int crewMemberId, CancellationToken ct)
    {
        var assignment = await db.JobAssignments.FindAsync([id, crewMemberId], ct);
        if (assignment is null) return NotFound();

        db.JobAssignments.Remove(assignment);
        await db.SaveChangesAsync(ct);
        return ToDto((await LoadAsync(id, ct))!);
    }

    private static string? Validate(JobWriteRequest req) =>
        req.ScheduledEnd <= req.ScheduledStart ? "ScheduledEnd must be after ScheduledStart." : null;

    private Task<Job?> LoadAsync(int id, CancellationToken ct) =>
        db.Jobs.AsNoTracking()
            .Include(j => j.Vessel)
            .Include(j => j.Assignments).ThenInclude(a => a.CrewMember)
            .SingleOrDefaultAsync(j => j.Id == id, ct);

    private static JobDto ToDto(Job j) => new(
        j.Id,
        j.Title,
        j.Description,
        j.VesselId,
        j.Vessel?.Name ?? string.Empty,
        j.Status,
        j.Priority,
        new DateTimeOffset(DateTime.SpecifyKind(j.ScheduledStart, DateTimeKind.Utc)),
        new DateTimeOffset(DateTime.SpecifyKind(j.ScheduledEnd, DateTimeKind.Utc)),
        j.Assignments
            .Where(a => a.CrewMember is not null)
            .Select(a => new CrewSummaryDto(a.CrewMember!.Id, a.CrewMember.Name, a.CrewMember.Rank))
            .ToList());
}
