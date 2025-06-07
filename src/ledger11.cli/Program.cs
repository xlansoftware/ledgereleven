using System.CommandLine;
using ledger11.cli;

var rootCommand = new RootCommand("ledger11 CLI tool");

rootCommand.AddListUsersComand();
rootCommand.AddCreateUserComand();
rootCommand.AddGenerateComand();

// Run the CLI
return await rootCommand.InvokeAsync(args);
