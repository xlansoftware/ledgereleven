using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;

namespace ledger11.data;

public class LedgerDbContext : DbContext
{
    public LedgerDbContext()
        : base(new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options)
    {
    }

    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options)
    {
    }

    public DbSet<Category> Categories { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<TransactionDetail> TransactionDetail { get; set; }
    public DbSet<Widget> Widgets { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Transaction>()
            .Property(t => t.Date)
            .HasConversion(new UtcNullableDateTimeConverter());

        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.Date);

        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<TransactionDetail>()
            .HasIndex(t => t.TransactionId);

        modelBuilder.Entity<TransactionDetail>()
            .HasOne(t => t.Transaction)
            .WithMany(t => t.TransactionDetails)
            .HasForeignKey(t => t.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TransactionDetail>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

    }
}

