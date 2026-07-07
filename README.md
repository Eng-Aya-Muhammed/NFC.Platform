# NFC.Platform Backend

A clean-architecture, enterprise-ready Backend API for NFC card management and sales, built with **.NET 8** following standard software engineering practices.

---

## 📂 Project Architecture

The solution uses a layered architecture to ensure separation of concerns:

- **NFC.Platform.API**: Entry point, Controllers, Middlewares, and API versioning.
- **NFC.Platform.Application**: Business services (e.g., `CardService`, `AuthService`), DTOs, FluentValidation, and AutoMapper profiles.
- **NFC.Platform.Infrastructure**: EF Core `DbContext`, generic repositories, Unit of Work, SaveChanges interceptors, database seeders, and migrations.
- **NFC.Platform.Domain**: Core domain entities (`Card`, `User`, `Role`, `UserRole`, `RefreshToken`).
- **NFC.Platform.BuildingBlocks**: Shared cross-cutting concerns (localization resource files, unified `ServiceResult` wrapper, PBKDF2 `PasswordHasher`).

---

## 🛡️ Core Features

- **Authentication & Authorization**: Full JWT bearer auth, database-backed Refresh Token rotation, token revocation (logout), and secure forgot/reset password flows.
- **Auto-Auditing Interceptor**: EF Core SaveChanges interceptor that dynamically populates `CreatedAt`, `CreatedBy`, `UpdatedAt`, and `UpdatedBy` using the authenticated user identity.
- **Soft Deletes**: Automatic global query filters excluding records marked with `IsDeleted == true`.
- **Multi-lingual Localization**: Dynamic runtime response translation (Arabic/English) based on the HTTP `Accept-Language` header.
- **Global Error Handling**: Custom middleware capturing exceptions and mapping them to standardized unified HTTP status responses, keeping stack traces hidden in production.
- **Strict Code Style**: Configured with Roslyn analyzers, `.editorconfig`, and `Directory.Build.props` to enforce quality.

---

## 🚀 Getting Started

### Prerequisites
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (optional)

### Running with Docker (Recommended)
This launches both SQL Server and the API container automatically:
```bash
# Create your local gitignored .env file in the root
echo "ADMIN_PASSWORD=YourSecurePassword123!" > .env

# Run docker-compose
docker-compose up --build -d
```

### Running Locally
To run the API on your host machine:
```bash
# 1. Initialize user secrets locally
dotnet user-secrets init -p NFC.Platform.API/NFC.Platform.API.csproj
dotnet user-secrets set "AdminSettings:Password" "YourSecurePassword123!" -p NFC.Platform.API/NFC.Platform.API.csproj

# 2. Build the solution
dotnet build

# 3. Apply EF Migrations
dotnet ef database update -p NFC.Platform.Infrastructure -s NFC.Platform.API

# 4. Run the API project
dotnet run --project NFC.Platform.API/NFC.Platform.API.csproj
```

---

## 🔍 Testing the API (Swagger)
Once the application is running, open the interactive Swagger UI at:
👉 **[http://localhost:5161/swagger](http://localhost:5161/swagger)**

---

## 🧪 Unit Tests
The project contains comprehensive unit tests for localization and services:
```bash
dotnet test
```
