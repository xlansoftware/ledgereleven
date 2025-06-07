using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;

namespace ledger11.data;

public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    public AppDbContext()
    : base(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite("Data Source=:memory:")
        .Options)
    {
    }
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ExternalLogin> Logins { get; set; }
    public DbSet<Space> Spaces { get; set; }
    public DbSet<SpaceMember> SpaceMembers { get; set; }
    public DbSet<WaitlistEntry> WaitlistEntries { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // SpaceMember composite primary key
        modelBuilder.Entity<SpaceMember>()
            .HasKey(sm => new { sm.SpaceId, sm.UserId });

        // Space -> SpaceMember relationship
        modelBuilder.Entity<SpaceMember>()
            .HasOne(sm => sm.Space)
            .WithMany(s => s.Members)
            .HasForeignKey(sm => sm.SpaceId)
            .OnDelete(DeleteBehavior.Cascade);

        // User -> SpaceMember relationship
        modelBuilder.Entity<SpaceMember>()
            .HasOne(sm => sm.User)
            .WithMany(u => u.SpaceMemberships)
            .HasForeignKey(sm => sm.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.CurrentSpace)
            .WithMany()
            .HasForeignKey(u => u.CurrentSpaceId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}