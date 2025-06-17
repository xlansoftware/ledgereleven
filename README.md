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

Run the backend tests locally:

```bash
cd src
dotnet test
```

Run the backend tests in a container:

```bash
cd .devops/test/backend
docker-compose run --build --rm app-test
```

Run the frontend test in a container.

```bash
cd .devops/test/web/certs
./generate-test-certificates.sh
cd ..
docker-compose run --build --rm test
```

Run the frontend


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

## Convert wav to mp3

```bash
sudo apt install ffmpeg
ffmpeg -i input.wav output.mp3
```