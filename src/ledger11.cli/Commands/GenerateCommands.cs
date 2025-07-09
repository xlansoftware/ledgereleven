using System.CommandLine;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ledger11.data;
using ledger11.model.Data;
using ledger11.service;

namespace ledger11.cli;

public static class GenerateCommands
{

    public static RootCommand AddGenerateComand(this RootCommand rootCommand)
    {
        var command = new Command("generate-data", "Generate random data");

        var logLevelOption = Tools.LogLevelOption;
        var dataOption = Tools.DataOption;

        var emailOption = new Option<string>("--email", description: "Email of the user") { IsRequired = true };
        var countOption = new Option<int>("--count", getDefaultValue: () => 10000, description: "Count of records") { IsRequired = false, };

        command.AddGlobalOption(logLevelOption);
        command.AddGlobalOption(dataOption);
        command.AddOption(emailOption);
        command.AddOption(countOption);

        command.SetHandler(async (data, logLevel, email, count) =>
        {
            await Tools.Catch(async () =>
            {
                var dataPath = Tools.DataPath(null, data);

                var host = Tools.CreateHost(logLevel, dataPath);
                using var scope = host.Services.CreateScope();
                var services = scope.ServiceProvider;

                var logger = services.GetRequiredService<ILogger<Program>>();

                await Tools.EnsureDatabaseMigratedAsync(services);
                var dbContext = services.GetRequiredService<AppDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

                var user = await userManager.FindByEmailAsync(email);
                if (user == null)
                    throw new Exception($"User {email} is not present.");

                if (user.CurrentSpaceId == null)
                    throw new Exception($"User {email} has not current space.");

                var space = await dbContext.Spaces.FirstOrDefaultAsync((s => s.Id == user.CurrentSpaceId));
                if (space == null)
                    throw new Exception($"User {email} has not current space.");

                logger.LogInformation($"Creating {count} records in {space.Name} for user {user.UserName} ...");

                logger.LogTrace($"Using data path {dataPath}");
                var dbPath = Path.Combine(dataPath, $"space-{UserSpaceService.SanitizeFileName(space.Id.ToString())}.db");
                var optionsBuilder = new DbContextOptionsBuilder<LedgerDbContext>()
                    .UseSqlite($"Data Source={dbPath};Pooling=false");

                var context = new LedgerDbContext(optionsBuilder.Options);
                await UserSpaceService.InitializeDbAsync(context);

                await Generate(context, count);

                Console.WriteLine("Done.");
            });

        }, dataOption, logLevelOption, emailOption, countOption);

        rootCommand.AddCommand(command);

        return rootCommand;
    }

    private static async Task Generate(LedgerDbContext db, int count)
    {
        using var tran = db.Database.BeginTransaction();

        var categories = db.Categories.ToArray();

        var now = DateTime.UtcNow;
        var startDate = now.AddYears(-3);

        var rand = new Random();

        for (int i = 0; i < count; i++)
        {
            var cat = categories[rand.Next(categories.Length)];
            var date = startDate.AddDays(rand.Next((int)(now - startDate).TotalDays));
            var totalValue = Math.Round(rand.NextDouble() * 150 + 5, 2);

            var transaction = new Transaction
            {
                Date = date,
                Value = (decimal)totalValue,
                Category = cat,
                User = rand.NextDouble() > 0.5 ? "alice@example.com" : "bob@example.com",
                TransactionDetails = new List<TransactionDetail>()
            };

            // Add 1–4 details, optionally
            var detailCount = rand.Next(1, 5);
            double remaining = totalValue;

            for (int j = 0; j < detailCount; j++)
            {
                double value;
                if (j == detailCount - 1)
                    value = remaining;
                else
                {
                    value = Math.Round(rand.NextDouble() * (remaining / 2), 2);
                    remaining -= value;
                }

                var detail = new TransactionDetail
                {
                    Value = (decimal)value,
                    Category = categories[rand.Next(categories.Length)],
                    Description = $"Auto {cat.Name} detail",
                    Quantity = rand.NextDouble() < 0.5 ? rand.Next(1, 5) : null
                };

                transaction.TransactionDetails.Add(detail);
            }

            db.Transactions.Add(transaction);
        }

        await db.SaveChangesAsync();
        await tran.CommitAsync();
    }
}