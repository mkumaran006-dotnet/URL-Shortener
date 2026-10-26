# Setup Instructions

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- Git

No additional infrastructure required — SQLite is bundled; no Redis, Docker, or external database needed for local development.

## Clone and Restore

```bash
git clone <repo>
cd Project
dotnet restore
```

## Run the API

```bash
cd src/UrlShortener.Api
dotnet run
```

The application:
1. Creates the SQLite database (`urlshortener-dev.db`) on first start via `EnsureCreated()`
2. Starts the API on `https://localhost:7xxx` (HTTPS) and `http://localhost:5xxx` (HTTP)
3. Opens Swagger UI at `/swagger`

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| `ConnectionStrings:DefaultConnection` | `Data Source=urlshortener.db` | SQLite file path |
| `RateLimit:RequestsPerWindow` | 100 (dev: 1000) | Max requests per window |
| `RateLimit:WindowSeconds` | 60 | Sliding window size |

Override in `appsettings.Development.json` or via environment variables:

```bash
ConnectionStrings__DefaultConnection="Data Source=/tmp/mydb.db" dotnet run
```

## Run Tests

```bash
dotnet test
```

## Run Orchestration Scenarios

With the API running:

```bash
# Greenfield: build URL shortener from scratch
curl -X POST https://localhost:7xxx/api/orchestration/run/greenfield

# Brownfield: add analytics to existing service
curl -X POST https://localhost:7xxx/api/orchestration/run/brownfield

# Ambiguous: "make it faster and more reliable"
curl -X POST https://localhost:7xxx/api/orchestration/run/ambiguous
```

## Production Considerations

| Concern | Dev Approach | Production Recommendation |
|---------|-------------|--------------------------|
| Database | SQLite | PostgreSQL (change connection string only — EF Core abstracts it) |
| Cache | In-Memory | Redis (`AddStackExchangeRedisCache`) |
| Rate Limiting | In-Process | Redis-backed sliding window or API Gateway |
| Approval Service | Auto-approve | Integrate with ServiceNow / Jira / Slack approval workflow |
| Logging | Console + File | OpenTelemetry → Splunk / Datadog |
| Auth | None (MVP) | JWT / API keys via ASP.NET Core auth middleware |
