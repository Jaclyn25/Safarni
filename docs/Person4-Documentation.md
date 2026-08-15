# Safarni — Person 4 Documentation

This content is ready to place into slides 9, 11, 12, and 14 of `Documentation.pptx`.

## 6. Project Structure

Safarni follows a layered architecture that separates the HTTP API, business use cases, domain rules, and infrastructure concerns.

```text
Safarni/
├── backend/
│   ├── Safarni.slnx
│   ├── src/
│   │   ├── Safarni.API/
│   │   │   ├── Controllers/       # REST endpoints
│   │   │   ├── Middleware/        # Global exception handling
│   │   │   ├── Extensions/        # Authentication and startup extensions
│   │   │   ├── Common/            # API response and error contracts
│   │   │   └── Program.cs         # Dependency injection and pipeline
│   │   ├── Safarni.Application/
│   │   │   ├── Features/          # Commands, queries, validators, handlers
│   │   │   ├── Abstractions/      # Interfaces for persistence and services
│   │   │   ├── Common/            # DTOs, errors, results, behaviors
│   │   │   └── Behaviors/         # MediatR validation behavior
│   │   ├── Safarni.Domain/
│   │   │   ├── Entities/          # Core business entities
│   │   │   └── Enums/             # Roles and domain statuses
│   │   └── Safarni.Infrastructure/
│   │       ├── Persistence/       # EF Core DbContext and repositories
│   │       ├── Migrations/        # Versioned SQL Server schema changes
│   │       ├── Security/           # Password hashing and token services
│   │       ├── AI/                 # Groq, Gemini, embeddings, and Qdrant
│   │       ├── Integrations/       # Places, weather, routes, and SerpAPI
│   │       └── Storage/            # Uploaded photos and documents
│   └── tests/
│       └── Safarni.Application.UnitTests/
├── Frontend/
│   ├── src/app/pages/              # User, agency, admin, planner, and chat screens
│   ├── src/app/services/           # HTTP clients for backend features
│   ├── src/app/guards/             # Route access rules
│   ├── src/app/interceptors/       # JWT attachment and token refresh
│   └── src/assets/                 # Images, icons, and static media
└── .github/workflows/
    └── deploy-monster.yml          # Build, test, publish, and deploy pipeline
```

## 8. Security

| Area | Implementation |
|---|---|
| Authentication | ASP.NET Core JWT Bearer authentication validates issuer, audience, signature, and token lifetime. |
| Authorization | Role-based policies protect travelers, verified agencies, administrators, support, content managers, and analysts. |
| Password protection | Passwords are stored as hashes through `IPasswordHasher`; raw passwords are not persisted. |
| Session security | Short-lived access tokens are paired with refresh tokens. The Angular interceptor performs a single-flight refresh after an expired access token. |
| Input validation | FluentValidation runs through the MediatR validation behavior before commands reach handlers. |
| Data protection | Production connection strings, JWT signing keys, admin credentials, email passwords, AI keys, and payment secrets must be supplied as environment variables or hosting secrets. |
| Public surface | Swagger is enabled only in Development. Protected APIs use `[Authorize]` and named authorization policies. |
| Operational logging | Audit logs and AI request logs capture security-relevant activity without returning raw secret values to the client. |

## 9. Error Handling

`ExceptionHandlingMiddleware` is registered near the beginning of the ASP.NET Core pipeline. It converts failures into a consistent JSON response instead of exposing stack traces to users.

| Failure | HTTP status | Client-facing behavior |
|---|---:|---|
| FluentValidation failure | 400 | Returns validation error codes and messages. |
| Database concurrency conflict | 409 | Tells the client that the record changed and should be reloaded. |
| AI provider rate limit | 429 | Returns a retry-safe message and avoids exposing provider internals. |
| Unexpected server exception | 500 | Logs the full exception server-side and returns a generic error. |
| Missing/expired JWT | 401 | Angular attempts one refresh; if it fails, local session data is cleared and the user returns to login. |
| Insufficient role/policy | 403 | The API rejects the operation without revealing protected data. |

All API failures use the shared `ApiResponse<T>` / `ApiError` contract so frontend services can display predictable messages.

## 11. Deployment

### Target architecture

```text
GitHub push (jacqueline)
        ↓
GitHub Actions: restore → npm ci → Angular build → .NET test → publish
        ↓
Angular dist copied into ASP.NET wwwroot
        ↓
MonsterASP WebDeploy
        ↓
safarni-travel.runasp.net + MonsterASP MSSQL database
```

### Required MonsterASP resources

- One Free website: `safarni-travel.runasp.net` (EU datacenter).
- One MSSQL database using the default `SQL_Latin1_General_CP1_CI_AS` collation.
- ASP.NET scripting set to `.NET 10.x` / x86.
- WebDeploy enabled for the website.
- The website WebDeploy values saved as GitHub repository secrets.

### GitHub repository secrets

```text
WEBSITE_NAME          = siteXXXXX
SERVER_COMPUTER_NAME  = https://siteXXXXX.siteasp.net:8172
SERVER_USERNAME       = siteXXXXX
SERVER_PASSWORD       = <WebDeploy password>
```

The application secrets are configured in MonsterASP application settings/environment variables, not committed to GitHub:

```text
ConnectionStrings__DefaultConnection
JwtSettings__SecretKey
JwtSettings__Issuer
JwtSettings__Audience
Admin__Email
Admin__Password
Frontend__BaseUrl=https://safarni-travel.runasp.net
Cors__AllowedOrigins__0=https://safarni-travel.runasp.net
GooglePlaces__ApiKey
GoogleRoutes__ApiKey
GroqChat__ApiKey
GroqItinerary__ApiKey
OPENROUTER_API_KEY
SerpApi__DiscoveryApiKey
SerpApi__CommerceApiKey
GeoapifySettings__ApiKey
WeatherApi__ApiKey
Payments__Provider=Stripe
Payments__StripeSecretKey
Payments__StripeWebhookSecret
RAG__QdrantUrl
```

### Publish and database migration

The workflow publishes a Windows x86 ASP.NET Core application and copies the Angular production files into `publish/wwwroot`. EF Core migrations are applied by the API startup initializer against the configured SQL Server connection. The deployed database must therefore be reachable before the first application start.

After deployment, verify:

```text
https://safarni-travel.runasp.net/
https://safarni-travel.runasp.net/api/health
```

Swagger remains disabled in Production. Use the MonsterASP error log and application logs if the health endpoint fails.

### Deployment limitation

MonsterASP's direct “Github Deploy” clones repository files into `/wwwroot`; it does not build this multi-project repository. The included `deploy-monster.yml` workflow is therefore the correct path for Safarni because it builds Angular and ASP.NET first, then publishes the compiled output through WebDeploy.
