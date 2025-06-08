# Ledger Eleven

Personal expense tracker

# How to build

## Prerequisites

```bash
nvm install --lts
nvm use --lts
npm i
cd src/ledger11.client
npm i --legacy-peer-deps
```

```bash
dotnet tool install --global dotnet-ef
```

## Run the tests

```bash
npm run test
```

## Start

```bash
npm run dev
```

## leger11.auth

```bash
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "your-connection-string"
dotnet user-secrets set "Smtp:Password" "your-dev-password"
```

## Work with SQLite in a container

```bash
sudo apt update
sudo apt install sqlite3

sqlite3
.open <db file name>
.tabes
```