namespace FleetDesk.Api.Models;

public enum UserRole { Viewer = 0, Dispatcher = 1, Admin = 2 }

public enum JobStatus { Planned = 0, InProgress = 1, Completed = 2, Cancelled = 3 }

public enum JobPriority { Low = 0, Normal = 1, High = 2, Urgent = 3 }

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Viewer;
}

public class Vessel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ImoNumber { get; set; } = string.Empty;
    public string VesselType { get; set; } = string.Empty;
    public List<Job> Jobs { get; set; } = new();
}

public class CrewMember
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Rank { get; set; } = string.Empty;
    public List<JobAssignment> Assignments { get; set; } = new();
}

public class Job
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int VesselId { get; set; }
    public Vessel? Vessel { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Planned;
    public JobPriority Priority { get; set; } = JobPriority.Normal;
    public DateTime ScheduledStart { get; set; }
    public DateTime ScheduledEnd { get; set; }
    public List<JobAssignment> Assignments { get; set; } = new();
}

public class JobAssignment
{
    public int JobId { get; set; }
    public Job? Job { get; set; }
    public int CrewMemberId { get; set; }
    public CrewMember? CrewMember { get; set; }
}
