<<<<<<< HEAD
# 📦 Inventory Management System

<div align="center">

![.NET](https://img.shields.io/badge/.NET-9.0-512BD4?style=for-the-badge&logo=dotnet&logoColor=white)
![SQL Server](https://img.shields.io/badge/SQL%20Server-CC2927?style=for-the-badge&logo=microsoftsqlserver&logoColor=white)
![MediatR](https://img.shields.io/badge/MediatR-CQRS-007ACC?style=for-the-badge)
![JWT](https://img.shields.io/badge/JWT-Auth-000000?style=for-the-badge&logo=jsonwebtokens&logoColor=white)
![Swagger](https://img.shields.io/badge/Swagger-UI-85EA2D?style=for-the-badge&logo=swagger&logoColor=black)
![Gemini AI](https://img.shields.io/badge/Gemini-AI%20Assistant-4285F4?style=for-the-badge&logo=google&logoColor=white)

**Multi-tenant inventory and stock management REST API — powered by Clean Architecture, CQRS, and an AI Assistant.**

</div>

---

## 📋 Table of Contents

- [About the Project](#-about-the-project)
- [Features](#-features)
- [Architecture](#-architecture)
- [Tech Stack](#-tech-stack)
- [Domain Model](#-domain-model)
- [Getting Started](#-getting-started)
  - [Prerequisites](#prerequisites)
  - [Installation](#installation)
  - [Configuration](#configuration)
- [API Endpoints](#-api-endpoints)
- [Authentication](#-authentication)
- [AI Assistant](#-ai-assistant)
- [Email Notifications](#-email-notifications)
- [Project Structure](#-project-structure)
- [License](#-license)

---

## 🚀 About the Project

**Inventory Management System** is a robust, production-ready REST API built with **.NET 9** and **Clean Architecture** principles. It enables companies to manage their products, warehouses (inventories), stock movements, suppliers, and delivery schedules — all in a **multi-tenant** environment where each company's data is fully isolated.

The system features a built-in **AI Assistant** powered by **Google Gemini**, allowing users to interact with the system using natural language (e.g., *"Add 50 units of product X to warehouse Y"*).

---

## ✨ Features

| Feature | Description |
|---|---|
| 🏢 **Multi-Tenancy** | Full data isolation per company via `CompanyId` on all entities |
| 📦 **Inventory Management** | Track multiple warehouses with current stock levels |
| 🔄 **Stock Movements** | Record incoming/outgoing movements with supplier and cost info |
| 🤖 **AI Assistant** | Natural language interface via Google Gemini API |
| 🚚 **Delivery Rules** | Configurable weekly/monthly recurring delivery schedules |
| 📧 **Email Notifications** | Automated delivery reminders via background service |
| 🔐 **JWT Authentication** | Secure stateless authentication with role-based access |
| ✅ **FluentValidation** | Comprehensive input validation with pipeline behavior |
| 🗂️ **CQRS Pattern** | Clean separation of commands and queries via MediatR |
| 📊 **Swagger UI** | Interactive API documentation with Bearer token support |

---

## 🏛️ Architecture

The project follows **Clean Architecture** with strict layer separation:

```
┌─────────────────────────────────────────┐
│           Presentation Layer            │
│    (Inventory_Management.WebApi)        │
│  Controllers │ Middlewares │ Services   │
│         BackgroundServices              │
└────────────────────┬────────────────────┘
                     │ depends on
┌────────────────────▼────────────────────┐
│            Core Layer                   │
│  ┌──────────────────────────────────┐   │
│  │  Inventory_Management.Application│   │
│  │  CQRS: Commands │ Queries        │   │
│  │  Handlers │ Validators │ Results │   │
│  └──────────────────────────────────┘   │
│  ┌──────────────────────────────────┐   │
│  │  Inventory_Management.Domain     │   │
│  │  Entities │ Base Classes         │   │
│  │  Interfaces (ICurrentUserService)│   │
│  └──────────────────────────────────┘   │
└────────────────────┬────────────────────┘
                     │ depends on
┌────────────────────▼────────────────────┐
│         Infrastructure Layer            │
│   (Inventory_Management.Persistance)    │
│       DbContext │ EF Core Migrations    │
└─────────────────────────────────────────┘
```

### Design Patterns Used

- **CQRS** — Commands and Queries are separated with MediatR
- **Pipeline Behavior** — `ValidationBehavior<TRequest, TResponse>` runs FluentValidation before every handler
- **Repository Pattern** — via Entity Framework Core DbContext
- **Multi-Tenancy** — `IHasCompany` interface + `ICurrentUserService` for automatic tenant filtering
- **Background Service** — `DeliveryNotificationService` runs on a 30-minute interval

---

## 🛠️ Tech Stack

| Technology | Version | Purpose |
|---|---|---|
| **.NET** | 9.0 | Core framework |
| **ASP.NET Core Web API** | 9.0 | REST API |
| **Entity Framework Core** | 9.0.9 | ORM & Migrations |
| **SQL Server** | — | Database |
| **MediatR** | 13.0.0 | CQRS mediator |
| **FluentValidation** | 11.3.1 | Input validation |
| **JWT Bearer** | 9.0.11 | Authentication |
| **Swashbuckle (Swagger)** | 9.0.6 | API documentation |
| **Google Gemini API** | — | AI Assistant |
| **SMTP (Gmail)** | — | Email notifications |

---

## 🗃️ Domain Model

```
Companies
    ├── Users (1:N)
    │     └── UsersRoles (N:M) ── Roles
    ├── Products (1:N)
    │     ├── Categories
    │     └── Unit_Types
    ├── Inventories (1:N)
    │     └── Stock_Movements (1:N)
    │           ├── Products
    │           ├── Move_Types (IN / OUT)
    │           ├── Suppliers
    │           └── Users
    ├── Suppliers (1:N)
    └── Delivery_Rules (1:N)
          └── Suppliers
```

### Key Entities

| Entity | Description |
|---|---|
| `Companies` | Root tenant entity — every record belongs to a company |
| `Products` | Products with category, unit type, barcode, and image URL |
| `Inventories` | Physical warehouse locations with current stock |
| `Stock_Movements` | Tracks every IN/OUT movement with quantity and payment |
| `Suppliers` | Supplier records linked to a company |
| `Delivery_Rules` | Recurring delivery schedules (weekly/monthly) with lead time |
| `Users` | Users with email, phone, and password hash |
| `Roles` / `UsersRoles` | Role-based access control |

---

## 🚀 Getting Started

### Prerequisites

- [.NET 9 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (LocalDB, Express, or full)
- A [Google Gemini API Key](https://aistudio.google.com/app/apikey) (for the AI Assistant)
- A Gmail account with an App Password (for email notifications)

### Installation

1. **Clone the repository**

```bash
git clone https://github.com/YOUR_USERNAME/Inventory_Management_System.git
cd Inventory_Management_System
```

2. **Restore NuGet packages**

```bash
dotnet restore
```

3. **Apply database migrations**

```bash
cd Presentation/Inventory_Management.WebApi
dotnet ef database update
```

4. **Run the application**

```bash
dotnet run --project Presentation/Inventory_Management.WebApi
```

The API will be available at `https://localhost:7057` and the Swagger UI at `https://localhost:7057/swagger`.

### Configuration

Copy `appsettings.Development.json` and update the following values:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=Inventory_Management_DB;Integrated Security=True;TrustServerCertificate=true"
  },
  "Jwt": {
    "Secret": "YOUR_SUPER_SECRET_KEY_MIN_32_CHARS",
    "Issuer": "https://localhost:7057/",
    "Audience": "https://localhost:7057/",
    "ExpiryInMinutes": 60
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY"
  },
  "EmailSettings": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "SenderEmail": "your-email@gmail.com",
    "SenderPassword": "YOUR_APP_PASSWORD"
  }
}
```

> ⚠️ **Security Warning:** Never commit real secrets to version control. Use [.NET User Secrets](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets) or environment variables in production.

---

## 📡 API Endpoints

All endpoints (except login/register) require a valid **Bearer JWT token**.

| Controller | Base Route | Operations |
|---|---|---|
| `UsersController` | `/api/users` | CRUD + Login/Register |
| `CompaniesController` | `/api/companies` | CRUD |
| `ProductsController` | `/api/products` | CRUD |
| `InventoriesController` | `/api/inventories` | CRUD + Stock Queries |
| `StockMovementsController` | `/api/stockmovements` | CRUD |
| `CategoriesController` | `/api/categories` | CRUD |
| `SuppliersController` | `/api/suppliers` | CRUD |
| `DeliveryRulesController` | `/api/deliveryrules` | CRUD |
| `MoveTypesController` | `/api/movetypes` | CRUD |
| `UnitTypesController` | `/api/unittypes` | CRUD |
| `RolesController` | `/api/roles` | CRUD |
| `UserRolesController` | `/api/userroles` | CRUD |
| `AiAssistantController` | `/api/aiassistant` | Chat + Session Management |

---

## 🔐 Authentication

The API uses **JWT Bearer tokens** for authentication.

1. **Register** a user via `POST /api/users`
2. **Login** via `POST /api/users/login` to receive a JWT token
3. Include the token in the `Authorization` header:

```
Authorization: Bearer <your-token>
```

**Password Requirements:**
- Minimum 6 characters
- At least one uppercase letter
- At least one lowercase letter
- At least one digit
- At least one special character

**Phone Number Format:** Must start with `5` and be 10 digits (e.g., `5551234567`)

---

## 🤖 AI Assistant

The system includes a conversational AI assistant powered by **Google Gemini**. It understands your inventory context and can perform real operations through natural language.

### What the AI can do:

- ✅ Query current stock levels
- ✅ List products, suppliers, and delivery rules
- ✅ Create new stock movements (IN/OUT)
- ✅ Add new products, suppliers, or delivery rules
- ✅ Answer questions about inventory health
- ✅ Multi-turn conversation with session context

### Example prompts:

```
"How many units of Laptop do we have in the main warehouse?"
"Add 100 units of product XYZ from supplier ABC to Warehouse 1"
"Show me all upcoming deliveries this week"
"Create a new weekly delivery rule for Supplier X every Monday"
```

**Endpoint:** `POST /api/aiassistant/chat`

```json
{
  "query": "What products are running low on stock?",
  "sessionId": "optional-session-id-for-context"
}
```

---

## 📧 Email Notifications

The `DeliveryNotificationService` is a **hosted background service** that:

- Runs every **30 minutes** automatically
- Checks all active `Delivery_Rules`
- Calculates the next delivery date based on recurrence (weekly/monthly)
- Sends **HTML email notifications** to all company users when a delivery is approaching (based on `LeadTimeDays`)

### Delivery Rule Frequencies

| Type | Description |
|---|---|
| `Weekly` | Triggers on specified days of the week with configurable interval (e.g., every 2 weeks) |
| `Monthly` | Triggers on specified days of the month with configurable interval |

---

## 📁 Project Structure

```
Inventory_Management_System/
│
├── Core/
│   ├── Inventory_Management.Domain/
│   │   ├── Entities/           # Domain entities (Products, Inventories, etc.)
│   │   └── Common/             # BaseEntity, IHasCompany, ICurrentUserService
│   │
│   └── Inventory_Management.Application/
│       ├── Features/
│       │   ├── Commands/       # Create, Update, Delete commands per entity
│       │   ├── Queries/        # Read queries per entity
│       │   ├── Handlers/       # MediatR command & query handlers
│       │   ├── Behaviors/      # ValidationBehavior pipeline
│       │   ├── Results/        # Response DTOs
│       │   └── Exceptions/     # Domain exceptions
│       └── Interfaces/         # IEmailService, etc.
│
├── Infrastructure/
│   └── Inventory_Management.Persistance/
│       ├── Context/            # EF Core DbContext
│       └── Migrations/         # EF Core migration files
│
├── Presentation/
│   └── Inventory_Management.WebApi/
│       ├── Controllers/        # API controllers (incl. AiAssistantController)
│       ├── BackgroundServices/ # DeliveryNotificationService
│       ├── Middlewares/        # ExceptionHandlingMiddleware
│       ├── Services/           # CurrentUserService, EmailService
│       ├── Program.cs          # App entry point & DI configuration
│       └── wwwroot/            # Static frontend files
│
└── Inventory_Management_System.sln
```

---

## 📜 License

This project is licensed under the **MIT License**. See the [LICENSE](LICENSE) file for details.

---

<div align="center">

Made with ❤️ using .NET 9 & Clean Architecture

</div>
=======
# Inventory Management System

Bu depo, şirketlerin envanter yönetimini kolaylaştırmak için geliştirilmiş bir uygulama olan "Inventory Management System" (Envanter Yönetim Sistemi) projesini içerir.

## Genel Bakış

- Dil / Teknolojiler: C# (arka uç), JavaScript, HTML, CSS (ön yüz)
- Amaç: Ürünleri, stok seviyelerini, tedarikçileri ve envanter hareketlerini takip etmek.
- Mevcut durum: Kod tabanı C# ağırlıklı olup istemci tarafında JavaScript/HTML/CSS kullanımı bulunmaktadır.

> Not: Bu README genel kurulum ve kullanım talimatları içerir. Projeye özgü dosya isimleri veya komutlar (ör. çözüm dosyası adı, veritabanı sağlayıcısı) farklı olabilir — lütfen gerektiğinde README içeriğini proje dosyalarına göre güncelleyin.

## Özellikler

- Ürün ekleme / düzenleme / silme
- Stok giriş/çıkış hareketleri
- Tedarikçi ve kategori yönetimi
- Basit raporlama ve arama

## Gereksinimler

- .NET SDK (genellikle .NET 6 veya üstü) — projenin kökünde hangi sürümün kullanıldığını kontrol edin.
- (Varsa) Node.js ve npm/yarn — eğer frontend ayrı bir paket olarak yönetiliyorsa.
- Veritabanı: (örn. SQL Server, SQLite, PostgreSQL) — proje yapılandırmasına göre bağlantı dizesini ayarlayın.

## Hızlı Başlangıç (Yerel)

1. Depoyu klonlayın:

   git clone https://github.com/ETS26/Inventory_Management_System.git
   cd Inventory_Management_System

2. Çözüm dosyasını açın veya projeyi restore edin:

   # .NET araçlarıyla
   dotnet restore
   dotnet build

3. Veritabanı yapılandırması

- appsettings.json veya uygun konfigürasyon dosyasında ConnectionString (bağlantı dizesi) ayarlarını güncelleyin.
- Eğer Entity Framework Core kullanılıyorsa, database migration uygulamak için örnek:

   dotnet ef database update

(Not: `dotnet ef` komutunu kullanmak için EF Core araçlarının kurulu olması gerekir: `dotnet tool install --global dotnet-ef`)

4. Uygulamayı çalıştırın:

   dotnet run --project <ProjeKlasoru>

Varsa, frontend ayrı dizindeyse:

   cd frontend
   npm install
   npm start

5. Tarayıcıda uygulamayı açın: http://localhost:5000 veya proje yapılandırmasında belirtilen URL

## Testler

- Proje test projeleri içeriyorsa çalıştırmak için:

  dotnet test

## Katkıda Bulunma

Katkılar memnuniyetle karşılanır. Lütfen bir issue açın veya doğrudan pull request gönderin. Kod stili, commit mesajları ve branch stratejisi için proje içindeki CONTRIBUTING.md dosyasını inceleyin (varsa).

## Yayınlama

- Ürünü üretim ortamına taşıma, CI/CD yapılandırmaları ve ortam değişkenleri için proje sahibi veya README içindeki yayımlama rehberini takip edin.

## Lisans

Bu depo için lisans bilgisi eklenmemişse, lisans dosyası eklemeyi düşünün (örn. MIT, Apache-2.0). Eğer lisans zaten ekliyse, burayı güncelleyin.

## İletişim

Sorular veya geri bildirimler için proje sahibi ile iletişime geçin.

---

README dosyasını proje dosyalarına göre özelleştirmeniz önerilir: çözüm/proje adları, kullanılan .NET sürümü, veritabanı sağlayıcısı ve çalıştırma portu gibi bilgiler güncellenmelidir.
>>>>>>> 4fb523be4ae084404883227ed249d6f5fb53a77f
