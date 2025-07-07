# Contributing to LedgerEleven

Thank you for considering contributing to LedgerEleven!

We welcome any type of contribution, not just code. You can help with:
*   **Reporting a bug**
*   **Discussing the current state of the code**
*   **Submitting a fix**
*   **Proposing new features**
*   **Becoming a maintainer**

## We Use GitHub Flow

We use GitHub flow, so all code changes happen through pull requests:

1.  Fork the repo and create your branch from `main`.
2.  If you've added code that should be tested, add tests.
3.  If you've changed APIs, update the documentation.
4.  Ensure the test suite passes.
5.  Issue that pull request!

## Any contributions you make will be under the MIT Software License

In short, when you submit code changes, your submissions are understood to be under the same [MIT License](http://choosealicense.com/licenses/mit/) that covers the project. Feel free to contact the maintainers if that's a concern.

## Report bugs using GitHub's [issues](https://github.com/xlansoftware/ledgereleven/issues)

We use GitHub issues to track public bugs. Report a bug by [opening a new issue](https://github.com/xlansoftware/ledgereleven/issues/new)

## Write bug reports with detail, background, and sample code

**Great Bug Reports** tend to have:

*   A quick summary and/or background
*   Steps to reproduce
    *   Be specific!
    *   Give sample code if you can.
*   What you expected would happen
*   What actually happens
*   Notes (possibly including why you think this might be happening, or stuff you tried that didn't work)

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

### Windows

If you are not using WSL, you can follow these steps:

1.  Install the [.NET 9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0).
2.  Install [Node.js 22](https://nodejs.org/en/download/current/).

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

Alternatively, you can use the ```lefger11.cli``` cli tool to create a demo user and seed it with random data:

```bash
cd src/ledger11.cli

# Create demo user
dotnet run create-user --data ../ledger11.web --email demo@example.com --password Super-Secret-42

# Generate sample data
dotnet run generate-data --data ../ledger11.web --email demo@example.com
```

## Submitting a Pull Request

When you're ready to submit a pull request, please follow these steps:

1.  **Open a new pull request** with the patch.
2.  **Ensure the PR description clearly describes the problem and solution.** Include the relevant issue number if applicable.
3.  **Before submitting, please make sure all of your commits are atomic** (one feature per commit).

Thank you!