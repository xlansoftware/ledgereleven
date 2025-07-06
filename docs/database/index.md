# Database

This document describes the database setup for the LedgerEleven project.

## Entity Framework

The application uses [Entity Framework (EF) Core](https://learn.microsoft.com/en-us/ef/core/) as its object-relational mapper (O/RM). EF Core provides an abstraction layer over the database, which means the application code doesn't interact directly with the database using SQL. Instead, it works with C# objects, and EF Core handles the translation to and from the database.

The most important benefit of this approach is that it "hides" the specific database system being used, making the application largely "database agnostic." This allows developers to focus on the business logic without worrying about the intricacies of a particular database. It also makes it possible to switch the underlying database with minimal changes to the application code.

Entity Framework Core migrations are used to manage the database schema. When you make changes to the model, you will need to create a new migration and apply it to the database.

### Creating a Migration

To create a new migration, run the following command from the `src/ledger11.data` directory:

```bash
dotnet ef migrations add <MigrationName>
```

### Applying a Migration

To apply a migration to the database, run the following command from the `src/ledger11.web` directory:

```bash
dotnet ef database update
```

## SQLite vs. PostgreSQL

The project is configured to use **SQLite** by default. SQLite is a lightweight, file-based database that is simple to set up and use, making it ideal for development, testing, and applications with low concurrency and a relatively small amount of data. For a personal finance application like LedgerEleven, SQLite is often a perfectly suitable choice.

However, because the application uses Entity Framework Core, switching to a more robust, server-based database like **PostgreSQL** is a viable alternative. If the application's requirements were to grow—for example, to support multiple users with high concurrency or a much larger dataset—migrating to PostgreSQL would be a straightforward process. You would primarily need to install a PostgreSQL server, update the database connection string in the configuration, and run the EF Core migrations against the new database.

## Using the SQLite CLI

If you want to inspect the SQLite database directly, you can use the `sqlite3` command-line tool.

1.  Install `sqlite3`.
2.  Open the database file:

    ```bash
    sqlite3 data/ledger11.db
    ```

From the SQLite prompt, you can run SQL queries to inspect the database.
