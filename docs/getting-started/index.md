# Getting Started

This document provides a quick overview of how to get started with the LedgerEleven project. For more detailed instructions, please refer to the `CONTRIBUTING.md` file.

## Setting up your development environment

This project can be developed on Windows, macOS, or Linux. The following instructions provide guidance for setting up your development environment on each platform.

### Devcontainer (Recommended)

The easiest way to get started is to use the provided devcontainer. This will set up a consistent development environment with all the required tools and dependencies.

1.  Install [Visual Studio Code](https://code.visualstudio.com/).
2.  Install the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers).
3.  Open the project folder in VS Code.
4.  When prompted, click "Reopen in Container".

This will build the development environment defined in `.devcontainer/devcontainer.json`, which includes the correct versions of the .NET SDK and Node.js.

### WSL (Windows Subsystem for Linux)

If you prefer to work directly on Windows, we recommend using WSL.

1.  [Install WSL](https://learn.microsoft.com/en-us/windows/wsl/install).
2.  Install the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).
3.  Install [nvm](https://github.com/nvm-sh/nvm) (Node Version Manager).
4.  Use `nvm` to install and use the correct version of Node.js:
    ```bash
    nvm install 22
    nvm use 22
    ```

### macOS

1.  Install the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).
2.  Install [nvm](https://github.com/nvm-sh/nvm) (Node Version Manager).
3.  Use `nvm` to install and use the correct version of Node.js:
    ```bash
    nvm install 22
    nvm use 22
    ```

## Running Tests

This project has two sets of tests: backend unit tests and frontend Playwright tests. You can run them in containers or locally.

### In Containers (Recommended)

This is the most straightforward way to run the tests, as it ensures a consistent environment.

**Backend Tests:**

```bash
cd .devops/test/backend
docker compose run --build --rm app-test
```

**Frontend Tests:**

```bash
cd .devops/test/web
docker compose run --build --rm test
```

### Locally

**Backend Tests:**

To run the C# unit tests, execute the following command from the root of the repository:

```bash
dotnet test ./src/ledger11.sln
```

Alternatively, you can run ```dotnet test``` from the ```src``` folder, without specifying the project or solution name:

```bash
cd src
dotnet test
```

To run a single test, use the ```--filter``` argument:

```bash
dotnet test --filter Test_MyNewFeature
```

If you are using ```Visual Studio Code```, you may appreciate the ```C# Dev Kit``` extension, which provides a convenient graphical interface for running the C# tests.

**Frontend Tests:**

Running the Playwright tests locally requires two steps:

1.  **Start the web application:**

    ```bash
    cd src/ledger11.web
    dotnet run
    ```

2.  **Run the Playwright tests:**

    In a separate terminal, navigate to the `ledger11.webtests` directory and run the tests:

    ```bash
    cd src/ledger11.webtests
    # The install is required only once
    npm install
    npm test
    ```

## Developing the app

Developing the app requires two steps:

1.  **Start the backend:**

    ```bash
    cd src/ledger11.web
    dotnet run
    ```

2.  **Run the react app:**

    In a separate terminal, navigate to the `ledger11.client` directory and run:

    ```bash
    cd src/ledger11.client
    # The install is required only once
    npm install --legacy-peer-deps
    npm run dev
    ```

Open a browser and navigate to ```http://localhost:5173```. When you first open the app, you will be prompted to register a new user. During development, email verification is disabled, so you can use any email address.
