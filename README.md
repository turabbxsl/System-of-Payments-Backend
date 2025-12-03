# 🏦 Payment System Backend (Transactional API)

This repository contains the core API for a secure and reliable payment processing system. The service is designed to handle payment requests and ensure transactional integrity by preventing duplicate processing.

## Key Features

* **Idempotent Transaction Handling:** A core focus of this API is to prevent payment transaction repetition. Regardless of how many times a client sends the same payment request (e.g., due to network timeouts), the underlying transaction will be processed **exactly once**, always returning the same result.
* **Database Persistence:** All payment records and transaction states are persisted reliably using **PostgreSQL**.
* **Data Access Layer:** We use **Entity Framework Core (EF Core)** for managing database migrations and interaction with the PostgreSQL instance.
* **Duplicate Prevention Mechanism:** A custom Middleware is employed to intercept requests, check for unique keys (Idempotency Key), and either process the transaction or return the previously saved successful response.

## Technology Stack

| Component | Technology | Role |
| :--- | :--- | :--- |
| **Backend** | C# .NET Core / ASP.NET Core | Main API Framework |
| **Database** | **PostgreSQL** | Primary Data Store (Reliability & Transactions) |
| **ORM** | **Entity Framework Core (EF Core)** | Data Mapping and Migration Management |

## Setup and Running Locally

To get this project running on your local machine, follow these steps:

### 1. Prerequisites

* .NET SDK [Specify your version, e.g., .NET 8]
* A running **PostgreSQL** database instance (Docker or local install).

### 2. Configuration (Mandatory)

** SECURITY ALERT: Sensitive information is NOT committed to Git.**

Since `appsettings.json` and its environment-specific versions are ignored by Git (for security reasons), you **MUST** configure your database connection string locally.

**Create your secrets file using one of the following methods:**

* **Recommended: User Secrets:** In Visual Studio, right-click the `PaymentsApi` project and select "Manage User Secrets". Add your connection string:

    ```json
    {
      "ConnectionStrings": {
        "DefaultConnection": "Host=localhost;Port=5432;Database=your_db_name;Username=your_user;Password=your_password"
      }
    }
    ```

* **Alternative: `appsettings.Development.json`:** If you are not using User Secrets, create an `appsettings.Development.json` file in the API project's root folder and add the connection string there (ensure this file is listed in `.gitignore`).

### 3. Database Initialization

The project uses EF Core Migrations to set up the database schema.

```bash
# Ensure you are in the directory containing the project file (e.g., PaymentsApi/)
dotnet ef database update
