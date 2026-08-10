# Safarni

Safarni is a travel planning platform with an ASP.NET Core backend and an Angular frontend. It includes account/profile features, travel preferences, and an AI concierge chatbot for travel questions, destination guidance, Google Places recommendations, and optional RAG indexing through Qdrant.

## Tech Stack

- Backend: ASP.NET Core / .NET 10, MediatR, FluentValidation, Entity Framework Core
- Database: SQL Server
- Frontend: Angular 21, standalone components, Angular Router
- AI services: Gemini itinerary planning, Groq chat completions, local embeddings, optional Qdrant vector search
- External APIs: SerpAPI discovery/commerce, Google Places (chat), Google Routes, Google OAuth, Geoapify

## Project Structure

```text
Safarni/
  backend/
    Safarni.slnx
    src/
      Safarni.API/              # Controllers, middleware, API startup
      Safarni.Application/      # Features, commands, queries, interfaces
      Safarni.Domain/           # Domain entities
      Safarni.Infrastructure/   # EF Core, repositories, AI, email, storage
  Frontend/
    src/
      app/
        pages/                  # Explore, Planner, Concierge, Register
        services/               # Auth and chat API clients
```

## Backend Setup

From the repository root:

```powershell
dotnet restore backend\Safarni.slnx
dotnet build backend\Safarni.slnx
dotnet run --project backend\src\Safarni.API\Safarni.API.csproj --launch-profile http
```

The API listens on:

```text
http://localhost:5110
```

Useful endpoints:

- `GET /api/health`
- `POST /api/Auth/register`
- `POST /api/Auth/login`
- `GET /api/Chat/conversations`
- `POST /api/Chat/conversations`
- `POST /api/Chat/send`
- `POST /api/Chat/regenerate`
- `DELETE /api/Chat/conversations/{id}`
- Swagger is available in Development mode at `/swagger`.

## Database

The default connection string is configured in:

```text
backend/src/Safarni.API/appsettings.json
```

Example local SQL Server override:

```powershell
$env:ConnectionStrings__DefaultConnection='Server=.;Database=Saffrni_DB;Trusted_Connection=True;Encrypt=False;'
dotnet run --project backend\src\Safarni.API\Safarni.API.csproj --launch-profile http
```

Apply EF Core migrations when the database schema changes:

```powershell
dotnet ef database update --project backend\src\Safarni.Infrastructure --startup-project backend\src\Safarni.API
```

No migration is needed for prompt/chat-language changes unless entities, `ApplicationDbContext`, or migrations were modified.

## Pricing And Subscriptions

Pricing is implemented as two backend-enforced vertical slices on top of one billing foundation:

- Traveler Premium: `$5/month` or `$50/year`, protecting AI itinerary generation.
- Agency Marketplace: `$29/month` or `$299/year`, protecting marketplace publishing while keeping agency verification separate.

The pricing catalog is seeded by `PricingCatalogSeeder`. Stripe Checkout is now the active provider. The flow is:

`Pricing -> Stripe Checkout -> checkout.session.completed -> Active -> invoice.paid/invoice.payment_failed renewal updates`

Set these values in the ignored root `.env` file (never commit real secrets):

```text
Payments__Provider=Stripe
Payments__StripeSecretKey=sk_test_...
Payments__StripeWebhookSecret=whsec_...
Frontend__BaseUrl=http://localhost:4200
```

For local development, forward Stripe events to the API with the Stripe CLI:

```powershell
stripe listen --forward-to localhost:5110/api/pricing/webhooks/stripe
```

Copy the `whsec_...` value printed by the CLI into `Payments__StripeWebhookSecret`. For a deployed environment, create a Stripe Dashboard webhook endpoint at `/api/pricing/webhooks/stripe` and subscribe it to `checkout.session.completed`, `checkout.session.async_payment_failed`, `checkout.session.expired`, `invoice.paid`, and `invoice.payment_failed`.

Useful API routes are `GET /api/pricing/plans`, `GET /api/pricing/subscription`, `POST /api/pricing/checkout`, `POST /api/pricing/subscription/cancel`, and the verified Stripe callback `POST /api/pricing/webhooks/stripe`. The test callback remains available only when `Payments__Provider=Test`.

## Frontend Setup

From the frontend folder:

```powershell
cd Frontend
npm install
npm start
```

The frontend listens on:

```text
http://127.0.0.1:4200
```

The Angular app calls the backend at:

```text
http://localhost:5110/api
```

## Running Both Locally

Backend:

```powershell
$env:ConnectionStrings__DefaultConnection='Server=.;Database=Saffrni_DB;Trusted_Connection=True;Encrypt=False;'
dotnet run --project backend\src\Safarni.API\Safarni.API.csproj --launch-profile http
```

Frontend:

```powershell
cd Frontend
npm start -- --host 127.0.0.1 --port 4200
```

## Itinerary Generation

The itinerary pipeline uses two independent SerpAPI lanes:

```text
SerpApi__DiscoveryApiKey  # attractions, restaurants, and images
SerpApi__CommerceApiKey   # airports, flights, and hotels
```

`SerpApi__ApiKey` remains a backwards-compatible fallback when only one key is
available. Keep real keys in `.env`, environment variables, or .NET user
secrets; do not commit them to `appsettings.json`.

For each city, the backend runs two attraction searches and one food search,
deduplicates and ranks the returned places, and keeps unused alternatives for
regeneration. Flights, hotels, and discovery run concurrently. Gemini receives
the verified catalog and creates the complete trip in one normal request.

When an active itinerary already exists, **Regenerate all days** excludes places
used by up to five recent versions where enough alternatives exist. The new
plan is checked for meaningful difference before replacing the active version;
older versions remain stored as inactive records.

## Chatbot Notes

The AI Concierge supports:

- English and Arabic responses based on the latest user message
- Travel-only guardrails for non-travel questions
- Conversation follow-up context
- SQL Server knowledge fallback
- Google Places recommendations
- Optional Qdrant vector search when Qdrant is running on `http://localhost:6333`

If Qdrant is not running, the backend logs a warning and falls back to SQL/database knowledge. This does not stop the chatbot from working.

## Configuration And Secrets

Do not commit real API keys, tokens, passwords, or local-only connection strings.

Recommended local secret options:

- Environment variables
- `dotnet user-secrets`
- A local ignored config file such as `appsettings.Local.json`

Important configuration keys:

- `ConnectionStrings:DefaultConnection`
- `JwtSettings:*`
- `GoogleSettings:ClientId`
- `EmailSettings:*`
- `Payments__Provider`
- `Payments__StripeSecretKey`
- `Payments__StripeWebhookSecret`
- `Groq:ApiKey`
- `GooglePlaces:ApiKey`
- `RAG:QdrantUrl`

## Troubleshooting

SQL Server connection fails:

- Confirm the SQL Server instance exists.
- The checked-in example may point to `.\MSSQLSERVER01`; if your database is on the default instance, use `Server=.` as an environment override.

Backend build fails because DLL files are locked:

- Stop the running `Safarni.API.exe` / `dotnet run` process, then rebuild.

Qdrant warning appears:

- Start Qdrant on `http://localhost:6333`, or ignore the warning if SQL fallback is enough for local development.

Frontend cannot reach backend:

- Confirm backend is running on `http://localhost:5110`.
- Confirm CORS includes `http://127.0.0.1:4200` and `http://localhost:4200`.

## Verification

Backend:

```powershell
dotnet build backend\Safarni.slnx --no-restore
Invoke-WebRequest http://localhost:5110/api/health
```

Frontend:

```powershell
cd Frontend
npm run build
```
