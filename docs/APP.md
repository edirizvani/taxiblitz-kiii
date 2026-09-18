# TaxiBlitz Ohrid

**A full-stack taxi tour booking platform for Ohrid, North Macedonia.**

Built with ASP.NET Core 10, Clean Architecture, and Entity Framework Core — handling everything from tour discovery and online booking to driver management, a referral/commission system, a blog, and production-grade SEO.

---

## Table of Contents

- [Overview](#overview)
- [Architecture](#architecture)
- [Features](#features)
- [Tech Stack](#tech-stack)
- [Database Schema](#database-schema)
- [Getting Started](#getting-started)
- [Deployment](#deployment)
- [Security](#security)
- [SEO](#seo)
- [Testing](#testing)

---

## Overview

TaxiBlitz Ohrid is a production web application that lets travelers discover and book taxi tours around Ohrid and the broader Macedonian/Albanian region. Administrators manage tours, drivers, and the booking pipeline. Receptionists can generate referral codes and earn commissions on bookings they refer. Drivers have public profile pages. Registered users can favorite tours, leave reviews, and follow a travel blog.

**Live domain:** [taxiblitzohrid.com](https://taxiblitzohrid.com)

---

## Architecture

The solution follows **Clean Architecture** with a strict unidirectional dependency rule.

```
TaxiBlitzOhrid/
├── src/
│   ├── TaxiBlitz.Domain          # Entities only — no framework dependencies
│   ├── TaxiBlitz.Application     # Service interfaces, view models, repository contracts
│   ├── TaxiBlitz.Persistence     # EF Core DbContext, migrations, repository implementations
│   ├── TaxiBlitz.Infrastructure  # Email, weather API, geocoding, PDF receipts, SEO builder
│   ├── TaxiBlitz.Shared          # Role constants, string/date extensions
│   └── TaxiBlitz.Web             # ASP.NET Core MVC — controllers, views, filters, middleware
├── tests/
│   └── TaxiBlitz.Tests           # xUnit — unit + integration tests
├── docker-compose.yml            # SQL Server 2022
└── TaxiBlitz.sln
```

**Layer rules:**
- `Domain` has zero dependencies — pure C# entities and value objects
- `Application` depends only on `Domain` — interfaces live here, never implementations
- `Persistence` and `Infrastructure` implement `Application` interfaces
- `Web` composes everything via dependency injection in `Program.cs`

---

## Features

### Authentication & Authorization
- **ASP.NET Identity** with email confirmation required before first login
- **Google OAuth 2.0** (environment-configured, optional)
- **4 roles:** Administrator, User, Driver, Receptionist
- Password reset via email (token + Base64Url-encoded link)
- Login lockout after 15 failed attempts (5-minute lockout window)
- Session timeout at 30 minutes of inactivity
- Rate limiting on all auth endpoints (per-IP, sliding window)
- Automatic admin account seeding on first startup
- Profile completion gate — authenticated routes redirect to profile setup if incomplete

### Tour Management
- Full CRUD for tours (Admin only)
- Multi-stop route description with cultural highlights
- YouTube video embed per tour
- Cover photo + route photo gallery
- SEO-friendly slug URLs (auto-generated from title, with Unicode normalization for Macedonian/Albanian characters)
- 301 redirect from legacy `/Tours/Details/{id}` → slug URL
- International tour flag (auto-detected when route includes Albania)
- Search, filter, and pagination (9 per page)
- Review count and average rating aggregation per tour

### Booking System
- Customers submit: name, email, phone, number of people, preferred date/time
- Status workflow: **Pending → Approved → Canceled**
- Optional driver assignment per booking
- Referral code application at checkout (validates eligibility, applies discount)
- Email notifications: admin alert on new booking + customer confirmation
- Calendar view of approved bookings
- Admin/Receptionist approval interface

### Driver Management
- Driver profiles with: bio, languages spoken, vehicle type, coverage areas, experience years, trips completed, specialties, rating (0–5)
- Multiple profile pictures per driver
- Driver assignment to bookings
- Public driver index page with structured data (Person schema)

### Referral & Commission System
- Receptionists generate unique 6-character referral codes
- Configurable discount percentage per code (default 5%)
- Validation rules: no self-use, per-user usage limit (one use per user per code), active/inactive toggle
- `ReferralUsage` table tracks: code, user, booking, discount amount, timestamp, commission paid status
- Receptionist dashboard: earned commissions, paid/unpaid breakdown
- Admin view: all receptionists, mark commissions as paid

### Tour Stories (Blog)
- `TourPost` with title, excerpt, full content, featured flag, publication status, view counter
- Multiple images per post with display order and captions
- Threaded comments with moderation (approval required before display)
- Slug-based URLs
- Related tour linking

### User Features
- Favourite tours (toggle, per-user list)
- Leave reviews (1–5 stars, text, optional image)
- Manage profile (name, city, country, language, date of birth, bio, profile picture)
- Change password, manage external logins

### Infrastructure Services
- **Email:** SMTP via Gmail (booking alerts, auth emails — registration, password reset)
- **PDF Receipts:** QuestPDF — generated on booking confirmation
- **Weather:** OpenWeather API with database-backed caching
- **Geocoding:** Address → coordinates with `GeoCoordinateCache` to avoid repeated API calls
- **SEO Structured Data:** `StructuredDataBuilder` generates JSON-LD for TaxiService, WebSite, tours, reviews

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core 10 (MVC) |
| Language | C# 13, nullable reference types enabled |
| ORM | Entity Framework Core 10 |
| Database | SQL Server 2022 |
| Identity | ASP.NET Core Identity |
| OAuth | Microsoft.AspNetCore.Authentication.Google 10.0.0 |
| PDF | QuestPDF 2025.3.3 |
| HTML Sanitizer | HtmlSanitizer 8.1.870 |
| JSON | Newtonsoft.Json 13.0.3 |
| Frontend | Razor Views, Bootstrap 5, jQuery |
| PWA | Service worker + manifest.json |
| CI/CD | GitHub Actions → Azure App Service |

---

## Database Schema

### Identity (managed by EF Core Identity)
| Table | Purpose |
|---|---|
| `AspNetUsers` | `ApplicationUser` — extends IdentityUser with FirstName, LastName, City, Country, ProfilePictureUrl, Bio, DateOfBirth, Gender, PreferredLanguage, CreatedAt |
| `AspNetRoles` | Administrator, User, Driver, Receptionist |
| `AspNetUserRoles` | Role assignments |

### Core Entities

**Tour** — title, description, price `decimal(18,2)`, duration, slug (unique filtered index), YouTube link, cover photo, route stops, cultural highlights, starting/ending points

**BookingTour** — FK to Tour, optional FK to Driver, customer name/email/phone, number of people, booking date, status (Pending/Approved/Canceled), discount amount, FK to ReferralCode

**Driver** — name, bio, photo, rating (0–5), languages, vehicle type, coverage areas, experience years, trips completed, specialties, list of pictures

**Review** — reviewer name, text, date, optional image, rating (1–5)

**FavouriteTour** — (userId, tourId) unique pair — cascade delete from both User and Tour

**TourPost** — title, excerpt, content, cover image, slug, tags, featured, published, view count, FK to ApplicationUser (Restrict on delete)

**TourPostImage** — URL, caption, display order, FK to TourPost (Cascade)

**TourComment** — content, FK to TourPost (Cascade), FK to ApplicationUser (Restrict), nullable self-FK parent (Restrict), approved flag

**ReferralCode** — code (unique), FK to owner ApplicationUser (Restrict), discount percent `decimal(5,2)`, usage count, max uses, is active

**ReferralUsage** — FK to ReferralCode (Cascade), FK to user (Restrict), nullable FK to booking (SetNull), discount amount, used at, commission paid at; unique index on (CodeId, UserId)

**GeoCoordinateCache / WeatherCache** — API response caching tables

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server 2022 (local or via Docker below)
- Git

### 1. Clone the repository

```bash
git clone https://github.com/edirizvani/TaxiBlitzOhrid.git
cd TaxiBlitzOhrid
```

### 2. Start SQL Server (Docker)

```bash
docker-compose up -d
```

Or use `(localdb)\mssqllocaldb` — no extra setup needed on Windows with Visual Studio installed.

### 3. Configure secrets

Copy the template and fill in your values:

```bash
cp src/TaxiBlitz.Web/appsettings.Development.json.example src/TaxiBlitz.Web/appsettings.Development.json
```

Required keys:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=TaxiBlitzOhrid;Trusted_Connection=True;"
  },
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "your-gmail@gmail.com",
  "SmtpPass": "your-gmail-app-password",
  "SmtpFrom": "your-gmail@gmail.com",
  "AdminNotificationEmail": "your-admin@gmail.com",
  "GoogleClientId": "",
  "GoogleClientSecret": "",
  "OpenWeatherApiKey": ""
}
```

> Gmail requires an **App Password** (not your account password). Generate one at myaccount.google.com → Security → App passwords.

### 4. Apply migrations

```bash
dotnet ef database update --project src/TaxiBlitz.Persistence --startup-project src/TaxiBlitz.Web
```

The first startup automatically seeds the admin account and all four roles.

### 5. Run

```bash
dotnet run --project src/TaxiBlitz.Web
```

Navigate to `https://localhost:5001`.

### 6. Run tests

```bash
dotnet test tests/TaxiBlitz.Tests
```

---

## Deployment

The application is deployed to **Microsoft Azure** with a fully automated CI/CD pipeline.

| Component | Detail |
|---|---|
| Hosting | Azure App Service (Linux, .NET 10, Italy North) |
| Database | Azure SQL Database (`taxiblitzohrid-sql.database.windows.net`) |
| CI/CD | GitHub Actions — `.github/workflows/deploy.yml` |
| Domain | `taxiblitzohrid.com` (registered on Namecheap) |
| Data Protection Keys | `/home/data/DataProtection-Keys` (persistent App Service storage) |

**Deploy trigger:** Push to `main` → GitHub Actions builds, publishes, and deploys to Azure App Service using the publish profile secret.

**Production config** is supplied entirely via Azure App Service Application Settings (environment variables) — no secrets committed to the repository.

---

## Security

- **Role-Based Access Control:** All admin/receptionist routes decorated with `[Authorize(Roles = "...")]`
- **Rate Limiting (per-IP, sliding window):**
  - Login: 10 attempts / 10 minutes
  - Register: 5 attempts / 60 minutes
  - Forgot Password: 5 attempts / 60 minutes
  - Custom 429 response page
- **Account Lockout:** 15 failed login attempts → 5-minute lockout
- **Security Headers:** `X-Content-Type-Options: nosniff`, `X-Frame-Options: SAMEORIGIN`
- **Cookies:** HttpOnly, SameSite=Lax in dev / `__Host-` prefix + Secure in production
- **XSS Protection:** HtmlSanitizer on all user-generated HTML content
- **HTTPS:** Enforced in production with HSTS
- **Email Confirmation:** Required before first login
- **Data Protection Keys:** Persisted to disk for cookie decryption across App Service restarts

---

## SEO

- **Slug URLs:** All tours use `/tours/{slug}` with auto-generated, Unicode-normalized slugs
- **301 Redirects:** Legacy `/Tours/Details/{id}` redirects to slug URL
- **JSON-LD Structured Data:** TaxiService, WebSite (with SearchAction), Organization, per-tour FAQ schema, Person schema for drivers
- **Dynamic Sitemap:** `/sitemap.xml` includes all published tours and blog posts
- **IndexNow:** Notifies search engines on tour publish/update
- **Meta Tags:** Per-page `<title>`, `<meta description>`, Open Graph, Twitter Card, canonical URLs
- **robots.txt:** Blocks auth/manage pages, allows tour and blog content
- **noindex/nofollow:** Applied to Login, Register, and account management pages

---

## Testing

**Test project:** `tests/TaxiBlitz.Tests` (xUnit)

**Tools:** xUnit · Moq · FluentAssertions · EF Core InMemory / SQLite · WebApplicationFactory

### Unit Tests
- Domain entity validation (Tour, TourPost, Booking, Driver, Referral)
- Service logic (TourService, ReferralService, DriverService, ReviewService, FavouriteService, TourStoryService)
- Extension methods (ToSlug with Unicode characters, date helpers)

### Integration Tests
- **Auth flows:** Registration, login, password reset, email confirmation, rate limiting, lockout
- **Repositories:** Tour, Booking, Driver, Review, Favourite, Referral, TourPost
- **Infrastructure:** SMTP service, weather service, geocoding service, StructuredDataBuilder
- **Controllers:** All 14 controllers tested end-to-end via WebApplicationFactory
- **Business flows:**
  - Full booking flow (create → approve → confirm email)
  - Referral code lifecycle (generate → validate → apply → track commission)
  - Commission payout flow
  - Profile completion gate enforcement
  - Favourite toggle (add/remove, unique constraint)
  - Review submission and rate limiting
  - Cascade delete behavior (tour deletion, driver unlinking)

```bash
# Run all tests
dotnet test

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"
```
