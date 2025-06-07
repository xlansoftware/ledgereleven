using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ledger11.data;
using ledger11.model;
using ledger11.model.Data;
using ledger11.service;
using Xunit;

namespace ledger11.tests;

public class TestBackupService
{
    [Fact]
    public async Task Test_Export_Excel()
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        await using var context = new LedgerDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var category = new Category { Name = "Test Category", DisplayOrder = 1, Color = "red", Icon = "icon" };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var utcDate = new DateTime(2024, 11, 20, 9, 15, 0, DateTimeKind.Utc);

        var transaction = new Transaction
        {
            Date = utcDate,
            Value = 100,
            CategoryId = category.Id,
            Notes = "Test Note",
            User = "Test User"
        };
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        var detail = new TransactionDetail
        {
            TransactionId = transaction.Id,
            Value = 50,
            Quantity = 2,
            Description = "Test Detail",
            CategoryId = category.Id
        };
        context.TransactionDetail.Add(detail);
        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<BackupService>();
        var backupService = new BackupService(logger);

        await using var stream = new MemoryStream();
        await backupService.ExportAsync(ExportFormat.Excel, context, stream);

        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);

        stream.Position = 0;
        using var workbook = new XLWorkbook(stream);

        var txSheet = workbook.Worksheet("Transactions");
        var txRow = txSheet.Row(2);

        var expectedUtc = utcDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
        Assert.Equal(expectedUtc, txRow.Cell(2).GetValue<string>());
        Assert.Equal("100", txRow.Cell(3).GetValue<string>());
        Assert.Equal("Test Category", txRow.Cell(4).GetValue<string>());
        Assert.Equal("Test Note", txRow.Cell(5).GetValue<string>());
        Assert.Equal("Test User", txRow.Cell(6).GetValue<string>());

        var detailSheet = workbook.Worksheet("TransactionDetails");
        var detailRow = detailSheet.Row(2);
        Assert.Equal("50", detailRow.Cell(2).GetValue<string>());
        Assert.Equal("Test Detail", detailRow.Cell(3).GetValue<string>());
        Assert.Equal("2", detailRow.Cell(4).GetValue<string>());
        Assert.Equal("Test Category", detailRow.Cell(5).GetValue<string>());

        var catSheet = workbook.Worksheet("Categories");
        var catRow = catSheet.Row(2);
        Assert.Equal("Test Category", catRow.Cell(2).GetValue<string>());
        Assert.Equal("1", catRow.Cell(3).GetValue<string>());
        Assert.Equal("red", catRow.Cell(4).GetValue<string>());
        Assert.Equal("icon", catRow.Cell(5).GetValue<string>());
    }

    [Fact]
    public async Task Test_Export_Csv()
    {
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        await using var context = new LedgerDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        var category = new Category { Name = "Test Category", DisplayOrder = 1 };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var utcDate = new DateTime(2024, 12, 1, 14, 30, 0, DateTimeKind.Utc);

        var transaction = new Transaction
        {
            Date = utcDate,
            Value = 123.45m,
            CategoryId = category.Id,
            Notes = "CSV Note",
            User = "csv-user"
        };
        context.Transactions.Add(transaction);
        await context.SaveChangesAsync();

        var logger = new LoggerFactory().CreateLogger<BackupService>();
        var backupService = new BackupService(logger);

        await using var stream = new MemoryStream();
        await backupService.ExportAsync(ExportFormat.Csv, context, stream);

        Assert.NotNull(stream);
        Assert.True(stream.Length > 0);

        stream.Position = 0;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync();

        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.True(lines.Length >= 2, "Expected at least header + 1 data row");

        var header = lines[0].Trim();
        var data = lines[1].Trim();

        Assert.Equal("Id,DateUTC,Value,Category,Notes,User", header);

        var expectedDate = utcDate.ToString("yyyy-MM-ddTHH:mm:ssZ");
        Assert.Contains(expectedDate, data);
        Assert.Contains("123.45", data);
        Assert.Contains("Test Category", data);
        Assert.Contains("CSV Note", data);
        Assert.Contains("csv-user", data);
    }

    [Fact]
    public async Task Test_Import_Csv()
    {
        // Arrange - In-memory SQLite
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .Options;

        await using var context = new LedgerDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.EnsureCreatedAsync();

        // Pre-existing categories
        var catOld = new Category { Name = "Old Category", DisplayOrder = 1 };
        var catNew = new Category { Name = "New Category", DisplayOrder = 2 };
        var catUpdated = new Category { Name = "Updated Category", DisplayOrder = 3 };

        context.Categories.AddRange(catOld, catNew, catUpdated);
        await context.SaveChangesAsync();

        // Existing transaction (Id = 1)
        var existingTransaction = new Transaction
        {
            Id = 1,
            Date = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            Value = 50.00m,
            CategoryId = catOld.Id,
            Notes = "Old Note",
            User = "old-user"
        };
        context.Transactions.Add(existingTransaction);
        await context.SaveChangesAsync();

        // Dates
        var updateDate = new DateTime(2024, 12, 1, 14, 30, 0, DateTimeKind.Utc);
        var insertDate = new DateTime(2024, 6, 1, 9, 0, 0, DateTimeKind.Utc);
        var insertNoIdDate = new DateTime(2024, 7, 15, 13, 45, 0, DateTimeKind.Utc);
        var insertWithNewCategoryDate = new DateTime(2024, 8, 20, 11, 0, 0, DateTimeKind.Utc);

        var csvContent = new StringBuilder();
        csvContent.AppendLine("Id,DateUTC,Value,Category,Notes,User");

        // Valid cases
        csvContent.AppendLine($"1,{updateDate:yyyy-MM-ddTHH:mm:ssZ},123.45,Updated Category,Updated Note,updated-user");
        csvContent.AppendLine($"999,{insertDate:yyyy-MM-ddTHH:mm:ssZ},75.00,New Category,Inserted Row,new-user");
        csvContent.AppendLine($",{insertNoIdDate:yyyy-MM-ddTHH:mm:ssZ},88.88,New Category,No Id Row,anon-user");
        csvContent.AppendLine($",{insertWithNewCategoryDate:yyyy-MM-ddTHH:mm:ssZ},44.44,Completely New Category,Brand New Row,fresh-user");

        // Invalid data (missing fields)
        csvContent.AppendLine("invalid,data,line");
        csvContent.AppendLine(",,not-a-decimal,,,");

        // Act
        var logger = new LoggerFactory().CreateLogger<BackupService>();
        var backupService = new BackupService(logger);

        var csvBytes = Encoding.UTF8.GetBytes(csvContent.ToString());
        using var stream = new MemoryStream(csvBytes);

        var result = await backupService.ImportAsync(stream, context);

        // Assert: updated transaction
        var updated = await context.Transactions.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == 1);
        Assert.NotNull(updated);
        Assert.Equal(updateDate, updated.Date);
        Assert.Equal(123.45m, updated.Value);
        Assert.Equal("Updated Note", updated.Notes);
        Assert.Equal("updated-user", updated.User);
        Assert.Equal("Updated Category", updated.Category?.Name);

        // Assert: inserted with known Id
        var inserted = await context.Transactions.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == 999);
        Assert.NotNull(inserted);
        Assert.Equal(insertDate, inserted.Date);
        Assert.Equal(75.00m, inserted.Value);
        Assert.Equal("Inserted Row", inserted.Notes);
        Assert.Equal("new-user", inserted.User);
        Assert.Equal("New Category", inserted.Category?.Name);

        // Assert: inserted without Id
        var anon = await context.Transactions.Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Notes == "No Id Row");
        Assert.NotNull(anon);
        Assert.Equal(insertNoIdDate, anon.Date);
        Assert.Equal(88.88m, anon.Value);
        Assert.Equal("anon-user", anon.User);
        Assert.Equal("New Category", anon.Category?.Name);

        // Assert: new category was created and linked
        var fresh = await context.Transactions.Include(t => t.Category)
            .FirstOrDefaultAsync(t => t.Notes == "Brand New Row");
        Assert.NotNull(fresh);
        Assert.Equal(insertWithNewCategoryDate, fresh.Date);
        Assert.Equal(44.44m, fresh.Value);
        Assert.Equal("fresh-user", fresh.User);
        Assert.Equal("Completely New Category", fresh.Category?.Name);

        // Assert: new category exists
        var newCat = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Completely New Category");
        Assert.NotNull(newCat);

        // Assert: import status
        Assert.NotNull(result);
        Assert.Equal(6, result.TotalTransactions); // 4 valid + 2 invalid
        Assert.Equal(3, result.CreatedTransactions); // all except id=1
        Assert.Equal(1, result.UpdatedTransactions); // id=1
        Assert.Equal(2, result.FailedTransactions);  // the 2 invalid rows
        Assert.Equal(1, result.CreatedCategories);   // "Completely New Category"
        Assert.Equal(2, result.Errors.Count);
    }

    [Fact]
    public async Task Test_Import_Excel_WithInvalidData()
    {
        // Setup in-memory SQLite DbContext
        var options = new DbContextOptionsBuilder<LedgerDbContext>()
            .UseSqlite("DataSource=:memory:")
            .EnableSensitiveDataLogging()
            .UseLoggerFactory(new LoggerFactory())
            .Options;

        await using var context = new LedgerDbContext(options);
        await context.Database.OpenConnectionAsync();
        await context.Database.MigrateAsync();

        // Seed existing data
        var catOld = new Category { Name = "Old Category", DisplayOrder = 1 };
        var catNew = new Category { Name = "New Category", DisplayOrder = 2 };
        var catUpdated = new Category { Name = "Updated Category", DisplayOrder = 3 };
        context.Categories.AddRange(catOld, catNew, catUpdated);
        await context.SaveChangesAsync();

        var existingTransaction = new Transaction
        {
            Id = 1,
            Date = new DateTime(2023, 1, 1, 10, 0, 0, DateTimeKind.Utc),
            Value = 50.00m,
            CategoryId = catOld.Id,
            Notes = "Old Note",
            User = "old-user"
        };
        context.Transactions.Add(existingTransaction);
        await context.SaveChangesAsync();

        var dateUtc = new DateTime(2024, 05, 27, 15, 0, 0, DateTimeKind.Utc);

        // Build Excel in memory
        using var workbook = new XLWorkbook();

        AddSheet(workbook, "Categories",
        [
            [ "Id", "Name", "DisplayOrder", "Color", "Icon" ],
        [ 1, "Test Category", 1, "red", "icon" ],
        [ "", "Inserted Category", 4, "blue", "star" ],
        [ "BAD_ID", "", "XYZ", XLCellValue.FromObject(null), XLCellValue.FromObject(null) ] // Invalid row (empty name)
        ]);

        AddSheet(workbook, "Transactions",
        [
            [ "Id", "Date", "Value", "Category", "Notes", "User" ],
        [ 1, dateUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"), 123.45m, "Test Category", "Updated Note", "excel-user" ],
        [ 999, dateUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"), 75.00m, "Inserted Category", "Inserted with ID", "user-2" ],
        [ "", dateUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"), 88.88m, "Inserted Category", "Inserted with no ID", "user-3" ],
        [ "ABC", "not-a-date", "???", "Unknown Category", XLCellValue.FromObject(null), XLCellValue.FromObject(null) ] // Invalid row
        ]);

        AddSheet(workbook, "TransactionDetails",
        [
            [ "Id", "Value", "Description", "Quantity", "Category", "TransactionId" ],
        [ 1, 50m, "Detail Description", 2, "Test Category", 1 ],
        [ "", "bad-value", "Invalid Detail", "no-qty", "", "no-id" ] // Invalid row
        ]);

        await using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var logger = new LoggerFactory().CreateLogger<BackupService>();
        var backupService = new BackupService(logger);

        var importStatus = await backupService.ImportAsync(stream, context, clearExistingData: false);

        // 🔍 Validate Categories
        var insertedCategory = await context.Categories.FirstOrDefaultAsync(c => c.Name == "Inserted Category");
        Assert.NotNull(insertedCategory);
        Assert.Equal("blue", insertedCategory.Color);

        // 🔍 Validate Transactions
        var updatedTransaction = await context.Transactions.FindAsync(1);
        Assert.NotNull(updatedTransaction);
        Assert.Equal(123.45m, updatedTransaction.Value);
        Assert.Equal("Updated Note", updatedTransaction.Notes);
        Assert.Equal("excel-user", updatedTransaction.User);

        var insertedWithId = await context.Transactions.FindAsync(999);
        Assert.NotNull(insertedWithId);
        Assert.Equal(75.00m, insertedWithId.Value);

        var insertedNoId = await context.Transactions.FirstOrDefaultAsync(t => t.Notes == "Inserted with no ID");
        Assert.NotNull(insertedNoId);
        Assert.Equal("user-3", insertedNoId.User);

        var transactions = await context.Transactions.ToListAsync();
        foreach (var tx in transactions)
        {
            Console.WriteLine($"Transaction Id: {tx.Id}, Date: {tx.Date}, Value: {tx.Value}, Notes: {tx.Notes}, User: {tx.User}");
        }

        // Status assertions
        Assert.Equal(4, importStatus.TotalTransactions);
        Assert.Equal(2, importStatus.CreatedTransactions);
        Assert.Equal(1, importStatus.UpdatedTransactions);
        Assert.Equal(3, importStatus.FailedTransactions); // 1 invalid category + 1 transaction + 1 detail

        Assert.Contains(importStatus.Errors, e => e.Contains("Category import failed"));
        Assert.Contains(importStatus.Errors, e => e.Contains("Transaction import failed"));
        Assert.Contains(importStatus.Errors, e => e.Contains("Transaction detail import failed"));

        // Verify ID or row number appears in error messages
        foreach (var error in importStatus.Errors)
        {
            Assert.Matches(@"(Id: .+|Row: \d+)", error);
        }
    }

    private void AddSheet(XLWorkbook workbook, string sheetName, XLCellValue[][] data)
    {
        var sheet = workbook.Worksheets.Add(sheetName);

        for (int i = 0; i < data.Length; i++)
        {
            for (int j = 0; j < data[i].Length; j++)
            {
                sheet.Cell(i + 1, j + 1).Value = data[i][j];
            }
        }
    }

}
