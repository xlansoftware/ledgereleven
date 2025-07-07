# Architecture

This document describes the architecture of the LedgerEleven project.

## Overview

The project is a web application with a C# backend and a React frontend. The backend is a monolithic application that serves a REST API. The frontend is a single-page application (SPA) that consumes the API.

## Backend

The backend is built with ASP.NET Core and follows the Model-View-Controller (MVC) pattern. However, instead of using Razor Views, it serves a REST API that is consumed by the React frontend. The backend is responsible for:

*   User authentication and authorization
*   Data persistence
*   Business logic

## Frontend

The frontend is a single-page application built with React and Vite. It is responsible for:

*   Rendering the user interface
*   Client-side routing
*   Communicating with the backend API

## Communication

The frontend and backend communicate via a REST API. The API is defined in the backend and is consumed by the frontend using standard HTTP requests.
