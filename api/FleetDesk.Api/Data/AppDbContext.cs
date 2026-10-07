using FleetDesk.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace FleetDesk.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Vessel> Vessels => Set<Vessel>();
    public DbSet<CrewMember> CrewMembers => Set<CrewMember>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobAssignment> JobAssignments => Set<JobAssignment>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(256);
            e.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);
        });

        b.Entity<Vessel>(e =>
        {
            e.HasIndex(v => v.ImoNumber).IsUnique();
            e.Property(v => v.Name).HasMaxLength(120);
            e.Property(v => v.ImoNumber).HasMaxLength(20);
            e.Property(v => v.VesselType).HasMaxLength(60);
        });

        b.Entity<CrewMember>(e =>
        {
            e.Property(c => c.Name).HasMaxLength(120);
            e.Property(c => c.Rank).HasMaxLength(60);
        });

        b.Entity<Job>(e =>
        {
            e.Property(j => j.Title).HasMaxLength(120);
            e.Property(j => j.Status).HasConversion<string>().HasMaxLength(20);
            e.Property(j => j.Priority).HasConversion<string>().HasMaxLength(20);
            e.HasIndex(j => new { j.Status, j.ScheduledStart });
            e.HasOne(j => j.Vessel).WithMany(v => v.Jobs).HasForeignKey(j => j.VesselId).OnDelete(DeleteBehavior.Restrict);
        });

        b.Entity<JobAssignment>(e =>
        {
            e.HasKey(a => new { a.JobId, a.CrewMemberId });
            e.HasOne(a => a.Job).WithMany(j => j.Assignments).HasForeignKey(a => a.JobId).OnDelete(DeleteBehavior.Cascade);
            e.HasOne(a => a.CrewMember).WithMany(c => c.Assignments).HasForeignKey(a => a.CrewMemberId).OnDelete(DeleteBehavior.Cascade);
        });
    }
}
