using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;

namespace ledger11.data;

/// <summary>
/// Represents the application's main database context, managing ASP.NET Identity users
/// and their associated "spaces" (also referred to as ledgers or books).
/// Each "space" corresponds to a dedicated database for storing transaction data.
/// </summary>
public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class using an in-memory SQLite database.
    /// This constructor is primarily for testing or scenarios where a transient database is needed.
    /// </summary>
    public AppDbContext()
    : base(new DbContextOptionsBuilder<AppDbContext>()
        .UseSqlite("Data Source=:memory:")
        .Options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ApiKey> ApiKeys { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for user "spaces" (ledgers/books).
    /// Each space represents a collection of transactions stored in its own dedicated database.
    /// </summary>
    public DbSet<Space> Spaces { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for members of a "space".
    /// This table defines the many-to-many relationship between users and spaces.
    /// </summary>
    public DbSet<SpaceMember> SpaceMembers { get; set; }

    /// <summary>
    /// Configures the schema needed for the identity framework and custom entities.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the composite primary key for the SpaceMember entity.
        modelBuilder.Entity<SpaceMember>()
            .HasKey(sm => new { sm.SpaceId, sm.UserId });

        // Configure the many-to-many relationship between Space and SpaceMember.
        // A Space can have many SpaceMembers, and each SpaceMember belongs to one Space.
        modelBuilder.Entity<SpaceMember>()
            .HasOne(sm => sm.Space)
            .WithMany(s => s.Members)
            .HasForeignKey(sm => sm.SpaceId)
            .OnDelete(DeleteBehavior.Cascade); // If a Space is deleted, its SpaceMembers are also deleted.

        // Configure the many-to-many relationship between ApplicationUser and SpaceMember.
        // An ApplicationUser can have many SpaceMemberships, and each SpaceMember belongs to one ApplicationUser.
        modelBuilder.Entity<SpaceMember>()
            .HasOne(sm => sm.User)
            .WithMany(u => u.SpaceMemberships)
            .HasForeignKey(sm => sm.UserId)
            .OnDelete(DeleteBehavior.Cascade); // If a User is deleted, their SpaceMemberships are also deleted.

        // Configure the relationship between ApplicationUser and their CurrentSpace.
        // An ApplicationUser has one CurrentSpace, and a Space can be the CurrentSpace for many users.
        // When a Space is deleted, the CurrentSpaceId for associated users is set to null.
        modelBuilder.Entity<ApplicationUser>()
            .HasOne(u => u.CurrentSpace)
            .WithMany() // No navigation property back from Space to ApplicationUser for CurrentSpace.
            .HasForeignKey(u => u.CurrentSpaceId)
            .OnDelete(DeleteBehavior.SetNull); // If the referenced Space is deleted, set CurrentSpaceId to null.
    }
}