# Ledger Eleven CLI

This project provides a command-line interface (CLI) for interacting with the Ledger Eleven application.

## Key Features

* **Command-Line Interface**: Offers a set of commands for managing the application from the terminal.
* **System.CommandLine**: Uses the `System.CommandLine` library for parsing command-line arguments and building the CLI.
* **Dependency Injection**: Leverages dependency injection to access the application's services.

### Create a user

```bash
dotnet run create-user --data ../ledger11.web --email demo@example.com --password Super-Secret-42
```
### Seed the test user with data

```bash
dotnet run generate-data --data ../ledger11.web --email demo@example.com
```