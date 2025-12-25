# Ledger Eleven Web Tests

This project contains the end-to-end tests for the Ledger Eleven application, written using Playwright.

## Key Features

* **Playwright**: A framework for web testing and automation.
* **End-to-End Testing**: Simulates real user scenarios to test the application's functionality from start to finish.
* **Cross-Browser Testing**: Configured to run tests on Chromium, with the option to add Firefox and WebKit.

## To run the test

Install playwright prerequisites - you will be reminded by playwright if something is missing.

### Run agains local app instance

1. Build and publish the client app.
    - ```cd /src/ledger11.client```
    - ```npm run build```
    - ```npm run publish```
2. Start the backend
    - ```cd /src/ledger11.web```
    - ```dotnet run```
    - leave it running and switch to another terminal
3. Run ```npx playwright test```

### Run in the tests in a container

You can also run the tests in a dedicated docker container. See ```/.devops/test/web/test.sh```.

---

```
npx playwright test
npx playwright test -g "my test name" --timeout=5000
```
    Runs the end-to-end tests.

```
npx playwright test --ui
```
    Starts the interactive UI mode.

```
npx playwright test --project=chromium
```
    Runs the tests only on Desktop Chrome.

```
npx playwright test example
```
    Runs the tests in a specific file.

```
npx playwright test --debug
```
    Runs the tests in debug mode.

```
npx playwright codegen
```
    Auto generate tests with Codegen.
