# Getting Started

This document provides a quick overview of how to get started with the LedgerEleven project. For more detailed instructions, please refer to the `CONTRIBUTING.md` file.

## Setting up your development environment

The easiest way to get started is to use the provided devcontainer. This will set up a consistent development environment with all the required tools and dependencies.

1.  Install [Visual Studio Code](https://code.visualstudio.com/).
2.  Install the [Dev Containers extension](https://marketplace.visualstudio.com/items?itemName=ms-vscode-remote.remote-containers).
3.  Open the project folder in VS Code.
4.  When prompted, click "Reopen in Container".

## Running the application

1.  **Start the backend:**

    ```bash
    cd src/ledger11.web
    dotnet run
    ```

2.  **Run the react app:**

    In a separate terminal, navigate to the `ledger11.client` directory and run:

    ```bash
    cd src/ledger11.client
    npm install --legacy-peer-deps
    npm run dev
    ```

Open a browser and navigate to ```http://localhost:5173```.

## Running Tests

**Backend Tests:**

```bash
dotnet test
```

**Frontend Tests:**

```bash
cd src/ledger11.webtests
npm test
```
