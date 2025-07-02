# Ledger Eleven Web Tests

This project contains the end-to-end tests for the Ledger Eleven application, written using Playwright.

## Key Features

* **Playwright**: A framework for web testing and automation.
* **End-to-End Testing**: Simulates real user scenarios to test the application's functionality from start to finish.
* **Cross-Browser Testing**: Configured to run tests on Chromium, with the option to add Firefox and WebKit.

---

```
npx playwright test
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
