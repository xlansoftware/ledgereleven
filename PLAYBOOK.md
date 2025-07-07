# Playbook

This file contains a collection of useful commands for the LedgerEleven project.

# How to build

## Prerequisites

```bash
nvm install --lts
nvm use --lts
npm i
cd ./src/ledger11.client
npm i --legacy-peer-deps
cd ./src/ledger11.playwright
npm i --legacy-peer-deps
```

## Install the EF tools

```bash
dotnet tool install --global dotnet-ef
```

## Run the tests

```bash
npm run test
```

### Run the backend tests locally

```bash
cd src
dotnet test
```

### Run the backend tests in a container

```bash
cd ./.devops/test/backend
docker-compose run --build --rm app-test
```

### Run the frontend tests in a container

```bash
cd ./.devops/test/web
docker-compose run --build --rm test
```

### Run the frontend tests locally:

Start the backend:
```bash
# start the backend
cd ./src/ledger11.web
dotnet run
```
Leave it running and open another terminal for the playwright tests
```bash
# start the playwright tests
cd ./src/ledger11.webtests
npx playwright test
```

## Start

```bash
npm run dev
```

## Work with SQLite in a container

```bash
# Install the sqlite3 tool
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
