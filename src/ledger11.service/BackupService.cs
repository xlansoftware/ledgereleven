using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ledger11.data;
using ledger11.model;
using ledger11.model.Data;

namespace ledger11.service;

public enum ExportFormat
{
    Csv,
    Excel
}

public class ImportStatus
{
    public int TotalTransactions { get; set; }
    public int CreatedTransactions { get; set; }
    public int UpdatedTransactions { get; set; }
    public int FailedTransactions { get; set; }
    public int CreatedCategories { get; set; }
    public List<string> Errors { get; set; } = new List<string>();
}

public interface IBackupService
{
    Task ExportAsync(ExportFormat format, LedgerDbContext ledgerContext, Stream outputStream);
    Task<ImportStatus> ImportAsync(Stream inputStream, LedgerDbContext ledgerContextm, bool clearExistingData = false);
}

public class BackupService : IBackupService
{
    private readonly ILogger<BackupService> _logger;

    public BackupService(ILogger<BackupService> logger)
    {
        _logger = logger;
    }

    public async Task ExportAsync(ExportFormat format, LedgerDbContext db, Stream outputStream)
    {
        _logger.LogInformation("Starting export with format: {Format}", format);

        switch (format)
        {
            case ExportFormat.Csv:
                await ExportAsCsvAsync(db, outputStream);
                break;
            case ExportFormat.Excel:
                await ExportAsExcelAsync(db, outputStream);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format), "Unsupported export format.");
        }
    }

    private async Task ExportAsExcelAsync(LedgerDbContext db, Stream outputStream)
    {
        var workbook = new XLWorkbook();

        var transactions = await db.Transactions.Include(t => t.Category).ToListAsync();
        var transactionDetails = await db.TransactionDetail
            .Include(td => td.Category)
            .Include(td => td.Transaction)
            .ToListAsync();
        var categories = await db.Categories.ToListAsync();

        var transactionSheet = workbook.Worksheets.Add("Transactions");
        transactionSheet.Cell(1, 1).InsertTable(transactions.Select(t => new
        {
            t.Id,
            Date = t.Date?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"),
            t.Value,
            t.ExchangeRate,
            t.Currency,
            Category = t.Category?.Name,
            t.Notes,
            t.User
        }));

        var detailsSheet = workbook.Worksheets.Add("TransactionDetails");
        detailsSheet.Cell(1, 1).InsertTable(transactionDetails.Select(td => new
        {
            td.Id,
            td.Value,
            td.Description,
            td.Quantity,
            Category = td.Category?.Name,
            td.TransactionId
        }));

        var categorySheet = workbook.Worksheets.Add("Categories");
        categorySheet.Cell(1, 1).InsertTable(categories.Select(c => new
        {
            c.Id,
            c.Name,
            c.DisplayOrder,
            c.Color,
            c.Icon
        }));

        // Save workbook to memory stream
        using var memoryStream = new MemoryStream();
        workbook.SaveAs(memoryStream);
        memoryStream.Position = 0;

        // Copy to output stream asynchronously
        await memoryStream.CopyToAsync(outputStream);
        await outputStream.FlushAsync();
    }


    private async Task ExportAsCsvAsync(LedgerDbContext db, Stream outputStream)
    {
        var writer = new StreamWriter(outputStream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);

        // Write CSV header
        await writer.WriteLineAsync("Id,DateUTC,Value,ExchangeRate,Currency,Category,Notes,User");

        // Retrieve transactions with associated categories
        var transactions = await db.Transactions
            .Include(t => t.Category)
            .ToListAsync();

        foreach (var t in transactions)
        {
            // Format each field, ensuring proper CSV escaping
            var id = t.Id.ToString();
            var dateUtc = t.Date?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ") ?? "";
            var value = t.Value.ToString("F2", CultureInfo.InvariantCulture);
            var category = EscapeCsvField(t.Category?.Name ?? "");
            var exchangeRate = t.ExchangeRate != null ? t.ExchangeRate.Value.ToString("F2", CultureInfo.InvariantCulture) : "";
            var currency = t.Currency ?? "";
            var notes = EscapeCsvField(t.Notes ?? "");
            var user = EscapeCsvField(t.User ?? "");

            // Combine fields into a CSV line
            var line = $"{id},{dateUtc},{value},{exchangeRate},{currency},{category},{notes},{user}";

            // Write the line asynchronously
            await writer.WriteLineAsync(line);
        }

        await writer.FlushAsync();
    }

    // Helper method to escape CSV fields containing special characters
    private string EscapeCsvField(string field)
    {
        if (field.Contains(",") || field.Contains("\"") || field.Contains("\n") || field.Contains("\r"))
        {
            field = field.Replace("\"", "\"\"");
            return $"\"{field}\"";
        }
        return field;
    }


    public async Task<ImportStatus> ImportAsync(Stream inputStream, LedgerDbContext ledgerContext, bool clearExistingData = false)
    {
        if (clearExistingData)
        {
            ledgerContext.TransactionDetail.RemoveRange(ledgerContext.TransactionDetail);
            ledgerContext.Transactions.RemoveRange(ledgerContext.Transactions);
            ledgerContext.Categories.RemoveRange(ledgerContext.Categories);
            await ledgerContext.SaveChangesAsync();
        }

        // Determine file type
        inputStream.Position = 0;
        byte[] buffer = new byte[4];
        int bytesRead = 0;
        while (bytesRead < 4)
        {
            int read = await inputStream.ReadAsync(buffer, bytesRead, 4 - bytesRead);
            if (read == 0)
            {
                break; // End of stream reached
            }
            bytesRead += read;
        }
        inputStream.Position = 0;

        bool isExcel = buffer[0] == 0x50 && buffer[1] == 0x4B; // ZIP header for XLSX

        var result = isExcel
            ? await ImportFromExcelAsync(ledgerContext, inputStream)
            : await ImportFromCsvAsync(ledgerContext, inputStream);

        await ledgerContext.SaveChangesAsync();

        return result;
    }

    private async Task<ImportStatus> ImportFromExcelAsync(LedgerDbContext db, Stream inputStream)
    {
        using var workbook = new XLWorkbook(inputStream);
        var status = new ImportStatus();

        var categoryStatus = await ImportCategoriesFromExcelAsync(db, workbook, "Categories");
        var transactionStatus = await ImportTransactionsFromExcelAsync(db, workbook, "Transactions");
        var transactionDetailStatus = await ImportTransactionDetailsFromExcelAsync(db, workbook, "TransactionDetails");

        return new ImportStatus
        {
            TotalTransactions = transactionStatus.TotalTransactions,
            CreatedTransactions = transactionStatus.CreatedTransactions,
            UpdatedTransactions = transactionStatus.UpdatedTransactions,
            FailedTransactions = transactionStatus.FailedTransactions + categoryStatus.FailedTransactions + transactionDetailStatus.FailedTransactions,
            CreatedCategories = categoryStatus.CreatedCategories,
            Errors = transactionStatus.Errors.Concat(categoryStatus.Errors).Concat(transactionDetailStatus.Errors).ToList()
        };
    }

    private Dictionary<string, int>? GetColumnMap(IXLWorksheet sheet)
    {
        var headerRow = sheet.FirstRowUsed();
        if (headerRow == null) return null;
        return headerRow.Cells()
            .Select((cell, index) => new { Name = cell.GetString(), Index = index + 1 }) // +1 for 1-based indexing in ClosedXML
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .ToDictionary(x => x.Name.Trim(), x => x.Index, StringComparer.OrdinalIgnoreCase);
    }

    private async Task<ImportStatus> ImportCategoriesFromExcelAsync(LedgerDbContext db, XLWorkbook workbook, string sheetName)
    {
        var status = new ImportStatus();

        if (!workbook.Worksheets.Contains(sheetName))
            return status;

        var sheet = workbook.Worksheet(sheetName);
        var rows = sheet.RangeUsed()?.RowsUsed().Skip(1);
        if (rows == null)
            return status;

        var columnMap = GetColumnMap(sheet);
        if (columnMap == null)
            return status;

        foreach (var row in rows)
        {
            try
            {
                int? id = columnMap.TryGetValue("Id", out var idCol) ? row.Cell(idCol).GetValue<int?>() : null;
                string name = columnMap.TryGetValue("Name", out var nameCol) ? row.Cell(nameCol).GetValue<string>() : "";
                int? displayOrder = columnMap.TryGetValue("DisplayOrder", out var orderCol) ? row.Cell(orderCol).GetValue<int?>() : null;
                string? color = columnMap.TryGetValue("Color", out var colorCol) ? row.Cell(colorCol).GetValue<string?>() : null;
                string? icon = columnMap.TryGetValue("Icon", out var iconCol) ? row.Cell(iconCol).GetValue<string?>() : null;

                if (string.IsNullOrWhiteSpace(name)) continue;

                var category = id.HasValue ? await db.Categories.FindAsync(id) : null;
                if (category != null)
                {
                    category.Name = name;
                    category.DisplayOrder = displayOrder ?? category.DisplayOrder;
                    category.Color = color;
                    category.Icon = icon;
                }
                else
                {
                    category = new Category
                    {
                        Name = name,
                        DisplayOrder = displayOrder ?? 99,
                        Color = color,
                        Icon = icon
                    };
                    if (id.HasValue)
                        category.Id = id.Value;

                    db.Categories.Add(category);
                    status.CreatedCategories++;
                }
            }
            catch (Exception ex)
            {
                var errorId = columnMap.TryGetValue("Id", out var idCol) ? $"Id: {row.Cell(idCol).GetValue<string>()}" : $"Row: {row.RowNumber()}";
                status.Errors.Add($"Category import failed at {errorId}: {ex.Message}");
                status.FailedTransactions++;
            }
        }

        await db.SaveChangesAsync();
        return status;
    }

    private async Task<ImportStatus> ImportTransactionsFromExcelAsync(LedgerDbContext db, XLWorkbook workbook, string sheetName)
    {
        var status = new ImportStatus();

        if (!workbook.Worksheets.Contains(sheetName))
            return status;

        var sheet = workbook.Worksheet(sheetName);
        var rows = sheet.RangeUsed()?.RowsUsed().Skip(1);
        if (rows == null)
            return status;

        var columnMap = GetColumnMap(sheet);
        if (columnMap == null)
            return status;

        foreach (var row in rows)
        {
            try
            {
                status.TotalTransactions++;

                int? id = columnMap.TryGetValue("Id", out var idCol) ? row.Cell(idCol).GetValue<int?>() : null;
                string? dateStr = columnMap.TryGetValue("Date", out var dateCol) ? row.Cell(dateCol).GetValue<string>() : null;
                decimal value = columnMap.TryGetValue("Value", out var valueCol) ? row.Cell(valueCol).GetValue<decimal>() : 0;
                decimal? exchangeRate = columnMap.TryGetValue("ExchangeRate", out var exchangeRateCol) ? row.Cell(exchangeRateCol).GetValue<decimal?>() : null;
                string? currency = columnMap.TryGetValue("Currency", out var currencyCol) ? row.Cell(currencyCol).GetValue<string?>() : null;
                string? categoryName = columnMap.TryGetValue("Category", out var catCol) ? row.Cell(catCol).GetValue<string>() : null;
                string? notes = columnMap.TryGetValue("Notes", out var notesCol) ? row.Cell(notesCol).GetValue<string?>() : null;
                string? user = columnMap.TryGetValue("User", out var userCol) ? row.Cell(userCol).GetValue<string?>() : null;

                if (dateStr == null)
                    continue;

                DateTime date = DateTime.Parse(dateStr);
                var category = !string.IsNullOrWhiteSpace(categoryName)
                    ? await db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName)
                    : null;

                // var all = await db.Transactions.ToListAsync();

                var transaction = id.HasValue ? await db.Transactions.FindAsync(id) : null;
                if (transaction != null)
                {
                    transaction.Date = date;
                    transaction.Value = value;
                    transaction.ExchangeRate = exchangeRate;
                    transaction.Currency = currency;
                    transaction.CategoryId = category?.Id;
                    transaction.Notes = notes;
                    transaction.User = user;
                    status.UpdatedTransactions++;
                }
                else
                {
                    transaction = new Transaction
                    {
                        Date = date,
                        Value = value,
                        ExchangeRate = exchangeRate,
                        Currency = currency,
                        CategoryId = category?.Id,
                        Notes = notes,
                        User = user
                    };

                    if (id.HasValue)
                        transaction.Id = id.Value;

                    db.Transactions.Add(transaction);
                    status.CreatedTransactions++;
                }
            }
            catch (Exception ex)
            {
                var errorId = columnMap.TryGetValue("Id", out var idCol) ? $"Id: {row.Cell(idCol).GetValue<string>()}" : $"Row: {row.RowNumber()}";
                status.Errors.Add($"Transaction import failed at {errorId}: {ex.Message}");
                status.FailedTransactions++;
            }
        }

        await db.SaveChangesAsync();
        return status;
    }

    private async Task<ImportStatus> ImportTransactionDetailsFromExcelAsync(LedgerDbContext db, XLWorkbook workbook, string sheetName)
    {
        var status = new ImportStatus();

        if (!workbook.Worksheets.Contains(sheetName))
            return status;

        var sheet = workbook.Worksheet(sheetName);
        var rows = sheet.RangeUsed()?.RowsUsed().Skip(1);
        if (rows == null)
            return status;

        var columnMap = GetColumnMap(sheet);
        if (columnMap == null)
            return status;

        foreach (var row in rows)
        {
            try
            {
                int? id = columnMap.TryGetValue("Id", out var idCol) ? row.Cell(idCol).GetValue<int?>() : null;
                decimal value = columnMap.TryGetValue("Value", out var valueCol) ? row.Cell(valueCol).GetValue<decimal>() : 0;
                string? description = columnMap.TryGetValue("Description", out var descCol) ? row.Cell(descCol).GetValue<string?>() : null;
                decimal? quantity = columnMap.TryGetValue("Quantity", out var qtyCol) ? row.Cell(qtyCol).GetValue<decimal?>() : null;
                string? categoryName = columnMap.TryGetValue("Category", out var catCol) ? row.Cell(catCol).GetValue<string?>() : null;
                int transactionId = columnMap.TryGetValue("TransactionId", out var txCol) ? row.Cell(txCol).GetValue<int>() : 0;

                var category = !string.IsNullOrWhiteSpace(categoryName)
                    ? await db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName)
                    : null;

                var existing = id.HasValue ? await db.TransactionDetail.FindAsync(id) : null;

                if (existing != null)
                {
                    existing.Value = value;
                    existing.Description = description;
                    existing.Quantity = quantity;
                    existing.CategoryId = category?.Id;
                    existing.TransactionId = transactionId;
                }
                else
                {
                    db.TransactionDetail.Add(new TransactionDetail
                    {
                        Id = id ?? 0,
                        Value = value,
                        Description = description,
                        Quantity = quantity,
                        CategoryId = category?.Id,
                        TransactionId = transactionId
                    });
                }
            }
            catch (Exception ex)
            {
                var errorId = columnMap.TryGetValue("Id", out var idCol) ? $"Id: {row.Cell(idCol).GetValue<string>()}" : $"Row: {row.RowNumber()}";
                status.Errors.Add($"Transaction detail import failed at {errorId}: {ex.Message}");
                status.FailedTransactions++;
            }
        }

        await db.SaveChangesAsync();
        return status;
    }

    private async Task<ImportStatus> ImportFromCsvAsync(LedgerDbContext db, Stream inputStream)
    {
        using var reader = new StreamReader(inputStream, Encoding.UTF8, leaveOpen: true);
        var status = new ImportStatus();

        string? headerLine = await reader.ReadLineAsync();
        if (string.IsNullOrWhiteSpace(headerLine))
            return status;

        var headerColumns = ParseCsvLine(headerLine);
        var columnMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < headerColumns.Length; i++)
        {
            if (!string.IsNullOrWhiteSpace(headerColumns[i]))
                columnMap[headerColumns[i].Trim()] = i;
        }

        while (!reader.EndOfStream)
        {
            string? line = await reader.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            status.TotalTransactions++;

            string[] fields;
            try
            {
                fields = ParseCsvLine(line);
            }
            catch (Exception ex)
            {
                status.FailedTransactions++;
                status.Errors.Add($"Failed to parse line: \"{line}\". Error: {ex.Message}");
                continue;
            }

            int? id = null;
            try
            {
                id = TryGetInt(fields, columnMap, "Id");
                DateTime? date = TryGetDate(fields, columnMap, "DateUTC");
                decimal? value = TryGetDecimal(fields, columnMap, "Value");
                decimal? exchangeRate = TryGetDecimal(fields, columnMap, "ExchangeRate");
                string? currency = TryGetString(fields, columnMap, "Currency");
                string? categoryName = TryGetString(fields, columnMap, "Category");
                string? notes = TryGetString(fields, columnMap, "Notes");
                string? user = TryGetString(fields, columnMap, "User");

                if (date == null || value == null)
                    continue;

                Transaction? transaction = null;

                if (id.HasValue)
                {
                    transaction = await db.Transactions.Include(t => t.Category).FirstOrDefaultAsync(t => t.Id == id);
                }

                if (transaction == null)
                {
                    transaction = new Transaction();
                    db.Transactions.Add(transaction);
                    status.CreatedTransactions++;
                }
                else
                {
                    status.UpdatedTransactions++;
                }

                if (id.HasValue)
                    transaction.Id = id.Value;

                transaction.Date = date;
                transaction.Value = value.Value;
                transaction.ExchangeRate = exchangeRate;
                transaction.Currency = currency;
                transaction.Notes = notes;
                transaction.User = user;

                if (!string.IsNullOrWhiteSpace(categoryName))
                {
                    var category = await db.Categories.FirstOrDefaultAsync(c => c.Name == categoryName);
                    if (category == null)
                    {
                        category = new Category { Name = categoryName, DisplayOrder = 0 };
                        db.Categories.Add(category);
                        status.CreatedCategories++;
                    }

                    transaction.Category = category;
                }
            }
            catch (Exception ex)
            {
                status.FailedTransactions++;
                status.Errors.Add($"Failed to process line: \"{line}\"{(id.HasValue ? " with Id = " + id.ToString() : "")}. Error: {ex.Message}");
            }
        }

        await db.SaveChangesAsync();
        return status;
    }

    private static string? TryGetString(string[] fields, Dictionary<string, int> columnMap, string key)
    {
        return columnMap.TryGetValue(key, out var index) && index < fields.Length ? fields[index]?.Trim() : null;
    }

    private static int? TryGetInt(string[] fields, Dictionary<string, int> columnMap, string key)
    {
        if (columnMap.TryGetValue(key, out var index) && index < fields.Length && int.TryParse(fields[index], out var result))
            return result;
        return null;
    }

    private static decimal? TryGetDecimal(string[] fields, Dictionary<string, int> columnMap, string key)
    {
        if (columnMap.TryGetValue(key, out var index)
            && index < fields.Length)
        {
            if (string.IsNullOrWhiteSpace(fields[index])) return null;
            return decimal.Parse(fields[index], NumberStyles.Any, CultureInfo.InvariantCulture);
        }
        return null;
    }

    private static DateTime? TryGetDate(string[] fields, Dictionary<string, int> columnMap, string key)
    {
        if (columnMap.TryGetValue(key, out var index)
            && index < fields.Length)
        {
            return DateTime.Parse(fields[index]).ToUniversalTime();
        }
        return null;
    }

    private string[] ParseCsvLine(string line)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (inQuotes)
            {
                if (c == '"' && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"'); // Escaped quote
                    i++;
                }
                else if (c == '"')
                {
                    inQuotes = false;
                }
                else
                {
                    sb.Append(c);
                }
            }
            else
            {
                if (c == ',')
                {
                    result.Add(sb.ToString());
                    sb.Clear();
                }
                else if (c == '"')
                {
                    inQuotes = true;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        result.Add(sb.ToString());
        return result.ToArray();
    }


    private class CsvTransaction
    {
        public int? Id { get; set; }
        public string? DateUTC { get; set; }
        public decimal Value { get; set; }
        public string? Category { get; set; }
        public string? Notes { get; set; }
        public string? User { get; set; }
    }
}
