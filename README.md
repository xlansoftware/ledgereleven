# Ledger Eleven

LedgerEleven is a simple, mobile-first personal expense tracker designed for quick and easy entry on the go. It helps you visualize spending trends, import/export data for analysis, and leverages AI for a smarter experience.

## Live Application

The application is publicly available at: **[https://ledgereleven.com/](https://ledgereleven.com/)**

## Running the Application

### In a Container (Recommended)

To build and run the application in a Docker container, execute the following command:

```bash
cd ./.devops/build
docker-compose up --build
```

The application will be available at `http://localhost:8080`.

### Locally

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

## Documentation

For more detailed instructions, troubleshooting, and a full list of useful commands, please refer to the following documents:

*   **[Getting Started](./docs/getting-started/index.md)**: Detailed setup and development instructions.
*   **[Playbook](./PLAYBOOK.md)**: A collection of useful commands for development and testing.


