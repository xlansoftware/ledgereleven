using System.CommandLine;
using ledger11.cli;

var rootCommand = new RootCommand("ledger11 CLI tool");

rootCommand.AddCreateApiKeyCommand();
rootCommand.AddRemoveApiKeyCommand();
rootCommand.AddListUsersCommand();
rootCommand.AddCreateUserCommand();
rootCommand.AddGenerateCommand();
rootCommand.AddInfoCommand();
rootCommand.AddTestUpgradeCommand();
rootCommand.AddSyncUsersCommand();

var result = await rootCommand.InvokeAsync(args);
return Tools.ExitCode != 0 ? Tools.ExitCode : result;
