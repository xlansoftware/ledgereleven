using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ledger11.data;

namespace ledger11.tests;

public class TestMigration
{
    private static async Task Scan(string folder, Func<string, Task> action) {
        var files = Directory.GetFiles(folder, "*.db");
        foreach (var file in files) {
            await action(file);
        }

        var folders = Directory.GetDirectories(folder);
        foreach (var subFolder in folders) {
            await Scan(subFolder, action);
        }
    } 

    [Fact]
    public async Task Migration_With_OldDbVersion_ShouldSucceed()
    {
        var testDataPath = Path.Combine(AppContext.BaseDirectory, "TestData");
        var appdataDb = Directory.GetFiles(testDataPath, "appdata.db", SearchOption.AllDirectories);
        foreach (var dbFile in appdataDb) {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite($"Data Source={dbFile};Pooling=false")
                .Options;

            var context = new AppDbContext(options);
            Console.WriteLine($"Upgrading {dbFile.Substring(testDataPath.Length)} ...");
            await context.Database.MigrateAsync();
        }

        var spaceDb = Directory.GetFiles(testDataPath, "space*.db", SearchOption.AllDirectories);
        foreach (var dbFile in spaceDb) {
            var options = new DbContextOptionsBuilder<LedgerDbContext>()
                .UseSqlite($"Data Source={dbFile};Pooling=false")
                .Options;

            var context = new LedgerDbContext(options);
            Console.WriteLine($"Upgrading space {dbFile.Substring(testDataPath.Length)} ...");
            await context.Database.MigrateAsync();
        }
    }
}
