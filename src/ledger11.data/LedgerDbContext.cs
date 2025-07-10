using Microsoft.EntityFrameworkCore;
using ledger11.model.Data;

namespace ledger11.data;

/// <summary>
/// Represents the database context for a single "ledger" (also referred to as a "space" or "book").
/// This context manages the transactions and related data specific to one ledger.
/// Each ledger has its own dedicated database.
/// <para>
/// In the application, the terms <c>Space</c>, <c>Ledger</c>, and <c>Book</c> are sometimes used interchangeably,  
/// but they have distinct meanings and roles:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><c>Space</c></term>
///     <description>
///         A record in the <c>AppDbContext</c> that represents a user's financial workspace.  
///         It holds metadata such as <c>Name</c>, <c>Currency</c>, and a unique <c>Id</c>, which is used to generate  
///         and identify a separate database for the space.
///     </description>
///   </item>
///   <item>
///     <term><c>Ledger</c></term>
///     <description>
///         The actual database created for a given space. It contains all the transactional data,  
///         including transactions, categories, and other financial structures.
///     </description>
///   </item>
///   <item>
///     <term><c>Book</c></term>
///     <description>
///         A user-facing metaphor for a ledger or space. Just like one can have multiple financial books  
///         for different purposes, each book in the app is tied to a unique ledger database and represented  
///         by a corresponding space in the <c>AppDbContext</c>.
///     </description>
///   </item>
/// </list>
///
/// <para>
/// Summary: <c>Book</c> (user concept) → <c>Space</c> (AppDbContext record) → <c>Ledger</c> (underlying database).
/// </para>
/// </summary>
public class LedgerDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LedgerDbContext"/> class using an in-memory SQLite database.
    /// This constructor is primarily for testing or scenarios where a transient database is needed.
    /// </summary>
    public LedgerDbContext()
        : base(new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LedgerDbContext"/> class with the specified options.
    /// </summary>
    /// <param name="options">The options for this context.</param>
    public LedgerDbContext(DbContextOptions<LedgerDbContext> options)
        : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for categories.
    /// </summary>
    public DbSet<Category> Categories { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for transactions.
    /// </summary>
    public DbSet<Transaction> Transactions { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for transaction details.
    /// </summary>
    public DbSet<TransactionDetail> TransactionDetail { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for widgets.
    /// </summary>
    public DbSet<Widget> Widgets { get; set; }

    /// <summary>
    /// Gets or sets the <see cref="DbSet{TEntity}"/> for ledger-specific settings.
    /// </summary>
    public DbSet<Setting> Settings { get; set; }

    /// <summary>
    /// Configures the model for the ledger database context.
    /// </summary>
    /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure the Date property of Transaction to use UtcNullableDateTimeConverter for proper UTC handling.
        modelBuilder.Entity<Transaction>()
            .Property(t => t.Date)
            .HasConversion(new UtcNullableDateTimeConverter());

        // Add an index to the Date property of Transaction for efficient querying.
        modelBuilder.Entity<Transaction>()
            .HasIndex(t => t.Date);

        // Configure the relationship between Transaction and Category.
        // A Transaction has one Category, and a Category can be associated with many Transactions.
        // Deletion of a Category will not cascade to Transactions (NoAction).
        modelBuilder.Entity<Transaction>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        // Add an index to the TransactionId property of TransactionDetail for efficient querying.
        modelBuilder.Entity<TransactionDetail>()
            .HasIndex(t => t.TransactionId);

        // Configure the relationship between TransactionDetail and Transaction.
        // A TransactionDetail belongs to one Transaction, and a Transaction can have many TransactionDetails.
        // Deletion of a Transaction will cascade to its TransactionDetails.
        modelBuilder.Entity<TransactionDetail>()
            .HasOne(t => t.Transaction)
            .WithMany(t => t.TransactionDetails)
            .HasForeignKey(t => t.TransactionId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure the relationship between TransactionDetail and Category.
        // A TransactionDetail has one Category, and a Category can be associated with many TransactionDetails.
        // Deletion of a Category will not cascade to TransactionDetails (NoAction).
        modelBuilder.Entity<TransactionDetail>()
            .HasOne(t => t.Category)
            .WithMany()
            .HasForeignKey(t => t.CategoryId)
            .OnDelete(DeleteBehavior.NoAction);

        // Configure the Setting entity to have a unique index on the Key property.
        modelBuilder.Entity<Setting>()
            .HasIndex(s => s.Key)
            .IsUnique();

    }
}

