# Database

This document describes the database setup for the LedgerEleven project.

## Entity Framework

The application uses [Entity Framework (EF) Core](https://learn.microsoft.com/en-us/ef/core/) as its object-relational mapper (O/RM). EF Core provides an abstraction layer over the database, which means the application code doesn't interact directly with the database using SQL. Instead, it works with C# objects, and EF Core handles the translation to and from the database.

The most important benefit of this approach is that it "hides" the specific database system being used, making the application largely "database agnostic." This allows developers to focus on the business logic without worrying about the intricacies of a particular database. It also makes it possible to switch the underlying database with minimal changes to the application code.

Entity Framework Core migrations are used to manage the database schema. When you make changes to the model, you will need to create a new migration and apply it to the database.

### Creating a Migration

To create migrations, you first need to install the .NET EF Core command-line tool:
```bash
dotnet tool install --global dotnet-ef
```

The `ledger11.data` project contains the application's `DbContext`s. There are two:
1.  `LedgerDbContext`: For core application data like ledgers and transactions.
2.  `AppDbContext`: For ASP.NET Identity data, such as users and roles.

To create a new migration, run the corresponding command from the repository root. Replace `InitialCreate` with a descriptive name for your migration (e.g., `AddPurchaseDate`).

- **For `LedgerDbContext`:**
  ```bash
  dotnet ef migrations add NameOfTheChange --project src/ledger11.data --context LedgerDbContext --output-dir Migrations/Ledger
  ```

- **For `AppDbContext`:**
  ```bash
  dotnet ef migrations add NameOfTheChange --project src/ledger11.data --context AppDbContext --output-dir Migrations/App
  ```

### Applying a Migration

Migrations are applied automatically when the application starts up.

Because of this automatic execution, it is critical that every migration is accompanied by a migration test in the `ledger11.tests` project. These tests validate the schema changes by running against a sample database file that represents the state *before* the migration.

For complex data transformations that go beyond simple schema changes, you can implement custom logic in the `ledger11.cli` project.

## SQLite vs. PostgreSQL

The project is configured to use **SQLite** by default. SQLite is a lightweight, file-based database that is simple to set up and use, making it ideal for development, testing, and applications with low concurrency and a relatively small amount of data. For a personal finance application like LedgerEleven, SQLite is often a perfectly suitable choice.

However, because the application uses Entity Framework Core, switching to a more robust, server-based database like **PostgreSQL** is a viable alternative. If the application's requirements were to grow—for example, to support multiple users with high concurrency or a much larger dataset—migrating to PostgreSQL would be a straightforward process. You would primarily need to install a PostgreSQL server, update the database connection string in the configuration, and run the EF Core migrations against the new database.

## Using the SQLite CLI

If you want to inspect the SQLite database directly, you can use the `sqlite3` command-line tool.

1.  Install `sqlite3`.

    ```bash
    sudo apt update
    sudo apt install sqlite3
    ```

2.  Open the database file:

    ```bash
    sqlite3 data/ledger11.db
    ```

From the SQLite prompt, you can run SQL queries to inspect the database.
