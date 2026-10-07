using FleetDesk.Api.Data;
using FleetDesk.Api.Dtos;
using FleetDesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Controllers;

[ApiController]
[Route("api/crew")]
[Authorize]
public class CrewController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<CrewSummaryDto>>> List(CancellationToken ct) =>
        await db.CrewMembers.AsNoTracking().OrderBy(c => c.Name)
            .Select(c => new CrewSummaryDto(c.Id, c.Name, c.Rank))
            .ToListAsync(ct);

    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<CrewSummaryDto>> Create(CrewRequest req, CancellationToken ct)
    {
        var member = new CrewMember { Name = req.Name.Trim(), Rank = req.Rank.Trim() };
        db.CrewMembers.Add(member);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new CrewSummaryDto(member.Id, member.Name, member.Rank));
    }
}
