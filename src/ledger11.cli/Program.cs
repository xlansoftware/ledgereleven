using System.CommandLine;
using ledger11.cli;

var rootCommand = new RootCommand("ledger11 CLI tool");

rootCommand.AddListUsersComand();
rootCommand.AddCreateUserComand();
rootCommand.AddGenerateComand();
rootCommand.AddInfoComand();
rootCommand.AddTestUpgradeComand();
rootCommand.AddSyncUsersCommand();

var result = await rootCommand.InvokeAsync(args);
return Tools.ExitCode != 0 ? Tools.ExitCode : result;
