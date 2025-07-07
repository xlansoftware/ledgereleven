# Ledger Eleven

LedgerEleven is a simple, mobile-first personal expense tracker designed for quick and easy entry on the go. It helps you visualize spending trends, import/export data for analysis, and leverages AI for a smarter experience.

## Live Application

The application is publicly available at: **[https://ledgereleven.com/](https://ledgereleven.com/)**

## Running the Application

### Locally

(_Requires: .NET 9.0 SDK and node.js installed._)

To run the application locally, you need to start both the backend and frontend services.

1.  **Start the backend:**
    ```bash
    cd ./src/ledger11.web
    dotnet run
    ```

2.  **Start the frontend (in a new terminal):**
    ```bash
    cd ./src/ledger11.client
    npm run dev
    ```

The application will be available at `http://localhost:5173`.

### In a Container

(_Requires: Docker or Docker for Desktop installed._)

To build and run the application in a Docker container, execute the following command:

```bash
cd ./.devops/build
docker-compose up --build
```

The application will be available at `http://localhost:8080`.

## Run the Tests

### Locally

**Backend Tests:**
```bash
cd src
dotnet test
```

**Frontend Tests:**

First, start the backend (see "Running the Application" section). Then, in a new terminal:
```bash
cd ./src/ledger11.webtests
npx playwright test
```

### In a Container

**Backend Tests:**
```bash
cd ./.devops/test/backend
docker-compose run --build --rm app-test
```

**Frontend Tests:**
```bash
cd ./.devops/test/web
docker-compose run --build --rm test
```

## Documentation

For more detailed instructions, troubleshooting, and a full list of useful commands, please refer to the following documents:

*   **[Getting Started](./docs/getting-started/index.md)**: Detailed setup and development instructions.
*   **[About](./docs/welcome.md)**: More about the app, architecture, technology and design choices.


