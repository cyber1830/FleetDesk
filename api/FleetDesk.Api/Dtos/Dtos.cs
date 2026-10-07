using System.ComponentModel.DataAnnotations;
using FleetDesk.Api.Models;

namespace FleetDesk.Api.Dtos;

public record RegisterRequest(
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, MinLength(8), StringLength(100)] string Password);

public record LoginRequest([Required] string Email, [Required] string Password);

public record AuthResponse(string Token, string Email, string Role, DateTime ExpiresAtUtc);

public record VesselRequest(
    [Required, StringLength(120)] string Name,
    [Required, StringLength(20)] string ImoNumber,
    [Required, StringLength(60)] string VesselType);

public record VesselDto(int Id, string Name, string ImoNumber, string VesselType);

public record CrewRequest(
    [Required, StringLength(120)] string Name,
    [Required, StringLength(60)] string Rank);

public record CrewSummaryDto(int Id, string Name, string Rank);

public record JobWriteRequest(
    [Required, StringLength(120)] string Title,
    string? Description,
    [Range(1, int.MaxValue)] int VesselId,
    JobPriority Priority,
    DateTimeOffset ScheduledStart,
    DateTimeOffset ScheduledEnd);

public record StatusRequest(JobStatus Status);

public record AssignRequest([Range(1, int.MaxValue)] int CrewMemberId);

public record JobDto(
    int Id,
    string Title,
    string? Description,
    int VesselId,
    string VesselName,
    JobStatus Status,
    JobPriority Priority,
    DateTimeOffset ScheduledStart,
    DateTimeOffset ScheduledEnd,
    IReadOnlyList<CrewSummaryDto> Crew);
