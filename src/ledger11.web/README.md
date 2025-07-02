# Ledger Eleven Web

This project is the main web application for Ledger Eleven. It is an ASP.NET 9.0 MVC application that serves as the backend for the React frontend. 

## Key Features

* **Frontend Hosting**: Serves the compiled React application from the `wwwroot/app` directory.
* **API Endpoints**: Provides API endpoints for the frontend to interact with the application's data and services.
* **Authentication and Authorization**: Manages user authentication and authorization using ASP.NET Core Identity and OpenID Connect.
* **Dependency Injection**: Wires up the application's services and dependencies.
* **OpenTelemetry**: Integrated for observability and monitoring.
