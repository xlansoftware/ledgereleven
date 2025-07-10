# Ledger Eleven Data

This project handles data access and persistence for the Ledger Eleven application.

## Key Features

* **Database Context**: Contains the Entity Framework Core `DbContext` for interacting with the database.
* **Migrations**: Manages database schema changes using Entity Framework Core migrations.

**Prerequisites:**

```bash
dotnet tool install --global dotnet-ef
```

To create a new migration, run the corresponding command from the repository root. Replace `NameOfTheChange` with a descriptive name for your migration (e.g., `AddPurchaseDate`).

- **For `LedgerDbContext`:**
  ```bash
  dotnet ef migrations add NameOfTheChange --context LedgerDbContext --output-dir Migrations/Ledger
  ```

- **For `AppDbContext`:**
  ```bash
  dotnet ef migrations add NameOfTheChange --context AppDbContext --output-dir Migrations/App
  ```

