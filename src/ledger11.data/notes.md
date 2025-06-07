# Install tools

```bash
sudo apt update
sudo apt install sqlite3
dotnet tool install --global dotnet-ef
```

# Re-create migrations

Remove existing migrations and execute
```bash
dotnet ef migrations add InitialCreate --context LedgerDbContext --output-dir Migrations/Ledger
dotnet ef migrations add InitialCreate --context AppDbContext --output-dir Migrations/App
```

```bash
dotnet ef migrations list --context LedgerDbContext 
dotnet ef migrations remove --context LedgerDbContext 

```

# Add change