using System.CommandLine;
using ledger11.cli;

var rootCommand = new RootCommand("ledger11 CLI tool");

rootCommand.AddListUsersComand();
rootCommand.AddCreateUserComand();
rootCommand.AddGenerateComand();
rootCommand.AddInfoComand();

try
{
    // Run the CLI
    var result = await rootCommand.InvokeAsync(args);
    return result;
}
catch (Exception ex)
{
    Console.WriteLine(ex.Message);
    return 1;
}
