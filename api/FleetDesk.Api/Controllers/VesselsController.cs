using FleetDesk.Api.Data;
using FleetDesk.Api.Dtos;
using FleetDesk.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Controllers;

[ApiController]
[Route("api/vessels")]
[Authorize]
public class VesselsController(AppDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<VesselDto>>> List(CancellationToken ct) =>
        await db.Vessels.AsNoTracking().OrderBy(v => v.Name)
            .Select(v => new VesselDto(v.Id, v.Name, v.ImoNumber, v.VesselType))
            .ToListAsync(ct);

    [HttpPost]
    [Authorize(Roles = "Admin,Dispatcher")]
    public async Task<ActionResult<VesselDto>> Create(VesselRequest req, CancellationToken ct)
    {
        var imo = req.ImoNumber.Trim().ToUpperInvariant();
        if (await db.Vessels.AnyAsync(v => v.ImoNumber == imo, ct))
            return Conflict(new { error = "A vessel with this IMO number already exists." });

        var vessel = new Vessel { Name = req.Name.Trim(), ImoNumber = imo, VesselType = req.VesselType.Trim() };
        db.Vessels.Add(vessel);
        await db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(List), new VesselDto(vessel.Id, vessel.Name, vessel.ImoNumber, vessel.VesselType));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var vessel = await db.Vessels.FindAsync([id], ct);
        if (vessel is null) return NotFound();
        if (await db.Jobs.AnyAsync(j => j.VesselId == id, ct))
            return Conflict(new { error = "Vessel has jobs and cannot be deleted." });

        db.Vessels.Remove(vessel);
        await db.SaveChangesAsync(ct);
        return NoContent();
    }
}
