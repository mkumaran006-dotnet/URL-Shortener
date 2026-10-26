# URL Shortener — Agentic SDLC System

A production-quality URL shortener service built as a demonstration of **end-to-end agentic software engineering**. The system includes:

1. A **URL shortener REST API** with analytics and reliability features
2. An **Agentic Orchestration Engine** that automates the full SDLC lifecycle

---

## Quick Start

```bash
cd src/UrlShortener.Api
dotnet run
```

Open the Swagger UI at `https://localhost:7xxx/swagger` or `http://localhost:5xxx/swagger`.

**Health check:** `GET /health`

---

## API Reference

| Method | Path | Description |
|--------|------|-------------|
| POST   | /api/urls | Create a short URL |
| GET    | /api/urls | List all short URLs (paged) |
| GET    | /api/urls/{code} | Get short URL details |
| PATCH  | /api/urls/{code} | Update title / expiry / active state |
| DELETE | /api/urls/{code} | Delete short URL |
| GET    | /{code} | Redirect to original URL (301) |
| GET    | /api/urls/{code}/analytics | Click summary |
| GET    | /api/urls/{code}/analytics/daily | Daily click breakdown |
| GET    | /api/urls/{code}/analytics/referrers | Top referrers |
| GET    | /api/urls/{code}/analytics/countries | Click breakdown by country |
| GET    | /health | Health check |
| POST   | /api/orchestration/run/{scenario} | Run orchestration scenario |
| GET    | /api/orchestration/{workflowId} | Get workflow status |
| GET    | /api/orchestration | List all workflows |
| DELETE | /api/orchestration/{workflowId} | Safe-stop a workflow |

### Create Short URL

```json
POST /api/urls
{
  "originalUrl": "https://example.com/very/long/url",
  "customAlias": "my-link",
  "title": "Example",
  "expiresAt": "2027-01-01T00:00:00Z"
}
```

### Run Orchestration

```bash
POST /api/orchestration/run/greenfield    # Greenfield scenario
POST /api/orchestration/run/brownfield   # Brownfield scenario
POST /api/orchestration/run/ambiguous    # Ambiguous requirements scenario
```

---

## Project Structure

```
src/
  UrlShortener.Core/          Domain entities, interfaces, business logic
  UrlShortener.Infrastructure/ EF Core (SQLite), repositories, in-memory cache
  UrlShortener.Api/           ASP.NET Core Web API, controllers, middleware
  UrlShortener.Orchestration/ Agentic SDLC orchestration engine

tests/
  UrlShortener.Core.Tests/
  UrlShortener.Api.Tests/
  UrlShortener.Orchestration.Tests/

docs/
  architecture.md
  setup.md
  scenarios/  greenfield.md  brownfield.md  ambiguous.md
```

---

## Running Tests

```bash
dotnet test
```

42 tests covering service logic, controller behavior, dependency graph algorithms, orchestration engine state transitions, and all three scenario types.

---

## Setup

See [docs/setup.md](docs/setup.md) for full setup instructions.

---

## Architecture

See [docs/architecture.md](docs/architecture.md) for the full architecture overview and ADRs.
