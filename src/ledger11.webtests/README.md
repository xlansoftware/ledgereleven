# Ledger Eleven Web Tests

This project contains the end-to-end tests for the Ledger Eleven application, written using Playwright. These tests simulate real user interactions to ensure the application functions correctly from end to end.

## Key Features

*   **Playwright**: A powerful framework for reliable end-to-end web testing.
*   **End-to-End Testing**: Validates the application's functionality across the entire stack, from UI to database.
*   **Cross-Browser Testing**: Configured to run tests on Chromium, with options to easily extend to Firefox and WebKit.

## Getting Started

*   **Playwright Browsers**: Playwright will guide you to install the necessary browsers (Chromium by default) the first time you run tests.

### Installation

Navigate to the `ledger11.webtests` directory and install the Node.js dependencies:

```bash
cd src/ledger11.webtests
npm install
npx playwright install # Install Playwright browser binaries
```

## Running the Tests

You have two primary options for running the tests: against a local instance of the application or within a dedicated Docker container environment.

### Option 1: Run Against a Local Application Instance

This method requires you to build and run the client (frontend) and web (backend) applications manually.

1.  **Build and Publish the Client Application (Frontend)**
    The Playwright tests will interact with the compiled client application.
    ```bash
    cd ../ledger11.client # Navigate to the client project directory
    npm run build       # Build the client application
    npm run publish     # Publish the build output to the backend's wwwroot
    ```

2.  **Start the Backend Application**
    The backend provides the API services that the frontend and thus the tests interact with.
    ```bash
    cd ../ledger11.web # Navigate to the web project directory
    dotnet run         # Start the backend. It runs on http://localhost:5139/ bu default.
    ```
    *Leave this terminal running with the backend active.*

3.  **Run the Web Tests**
    Open a *new terminal*, navigate back to the `src/ledger11.webtests` directory, and execute the tests:
    ```bash
    cd src/ledger11.webtests
    npx playwright test
    ```
    The tests will connect to the locally running backend and frontend.

### Option 2: Run Tests in a Docker Container

Running tests in Docker provides an isolated and consistent environment, ensuring that your local machine's setup doesn't interfere with test execution. This method automatically spins up the backend and test runner in containers.

1.  **Navigate to the Test Configuration**
    ```bash
    cd .devops/test/web
    ```

2.  **Execute the Test Script**
    The `test.sh` script handles building the necessary Docker images and running the tests.
    ```bash
    ./test.sh
    ```
    This command will:
    *   Build the `ledgerapp` container (your .NET backend) using `src/.devops/build/Dockerfile.ledger-eleven-app`.
    *   Build the `test` container (which includes Playwright and its dependencies) using `src/.devops/test/web/Dockerfile`.
    *   Start the `ledgerapp` (backend) service.
    *   Run the `test` service, which executes the Playwright tests against the `ledgerapp` service within the Docker network (`APP_URL: http://ledgerapp:8080/`).
    *   Remove the containers after the tests complete (`--rm`).

## Useful Playwright Commands

Here are some common Playwright commands you might find useful:

*   **Run all tests**:
    ```bash
    npx playwright test
    ```

*   **Run specific tests by name (grep)**:
    ```bash
    npx playwright test -g "my test name" --timeout=5000
    ```

*   **Start the interactive UI mode (recommended for debugging)**:
    ```bash
    npx playwright test --ui
    ```

*   **Run tests only on a specific browser project**:
    ```bash
    npx playwright test --project=chromium
    ```

*   **Run tests in a specific file**:
    ```bash
    npx playwright test example.spec.ts
    ```

*   **Run tests in debug mode**:
    ```bash
    npx playwright test --debug
    ```

*   **Auto-generate tests with Codegen**:
    ```bash
    npx playwright codegen
    ```
    This tool watches your browser interactions and generates Playwright test code.

Feel free to explore the `playwright.config.ts` file for more configuration options.