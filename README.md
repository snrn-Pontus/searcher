# Search Hit Counter

A Visual Studio ASP.NET Core web project for comparing search hit counts across multiple search providers.

The app accepts one or more words, searches each word independently against the configured providers, sums the hit counts per provider, and displays only the total number of hits. For example, `Hello world` searches `Hello` and `world` separately for each provider, then presents the provider-specific sum.

## Technology

- ASP.NET Core
- C#
- .NET 10 LTS
- Visual Studio solution format
- Vanilla HTML, CSS, and JavaScript frontend
- xUnit test project

## Project Structure

```text
searcher.sln
searcher/
  Program.cs
  Models/
  Options/
  Providers/
  Services/
  wwwroot/index.html
searcher.Tests/
```

The backend is split into small, testable parts:

- `Providers` contains the search engine clients and a shared hit-count parser.
- `Services` contains query parsing and orchestration logic.
- `Options` contains strongly typed configuration for provider endpoints and tokens.
- `Models` contains API request and response DTOs.
- `wwwroot/index.html` contains the browser UI.

This keeps `Program.cs` focused on composition, routing, and middleware setup.

## Search Providers

The app uses the two assignment endpoints:

- Altavista Search Engine, called with `GET` and a query string.
- Classic Song Search Engine, called with `POST` and a JSON body.

Each provider is represented by an `ISearchProvider` implementation. Adding another provider should only require a new provider class plus registration in dependency injection.

## Configuration

`appsettings.json` contains endpoint configuration and placeholder API tokens.

`appsettings.Development.json` keeps token placeholders. Configure the assignment tokens with user secrets or environment variables before running provider searches.

Configure tokens with user secrets:

```bash
dotnet user-secrets set "SearchEngines:Altavista:ApiToken" "<altavista-token>" --project searcher
dotnet user-secrets set "SearchEngines:ClassicSong:ApiToken" "<classic-song-token>" --project searcher
```

Environment variables are also supported:

```bash
SearchEngines__Altavista__ApiToken=<altavista-token>
SearchEngines__ClassicSong__ApiToken=<classic-song-token>
```

## Build and Run

Prerequisite: install the .NET 10 SDK or use Visual Studio with .NET 10 support.

From the repository root:

```bash
dotnet restore
dotnet build
dotnet run --project searcher
```

Open:

```text
http://localhost:5164
```

Swagger UI is available in Development at:

```text
http://localhost:5164/swagger
```

The generated OpenAPI document is also available at:

```text
http://localhost:5164/openapi/v1.json
```

Then enter one or more words, for example:

```text
Hello world
```

## Run Tests

```bash
dotnet test
```

The tests cover:

- Query parsing and validation.
- Summing hit counts per provider.
- Partial provider failures.
- Provider HTTP method, headers, query string, and JSON body behavior.
- Supported hit-count response shapes.

## API

### Search

```http
POST /api/search
Content-Type: application/json
```

Request:

```json
{
  "query": "Hello world"
}
```

Response shape:

```json
{
  "query": "Hello world",
  "terms": ["Hello", "world"],
  "providers": [
    {
      "provider": "Altavista",
      "totalHits": 13000000,
      "succeeded": true,
      "error": null,
      "terms": [
        { "term": "Hello", "hits": 5000000, "error": null, "succeeded": true },
        { "term": "world", "hits": 8000000, "error": null, "succeeded": true }
      ]
    }
  ],
  "elapsedMilliseconds": 250
}
```

If one provider or term fails, the successful results are still returned and the failed provider includes sanitized error information. This makes the UI useful even when one remote service has a temporary problem.

## Design Notes

The implementation intentionally uses a little more structure than the smallest possible solution:

- `HttpClientFactory` is used for provider clients.
- Provider settings are bound through typed options.
- Query parsing is isolated and unit tested.
- Provider calls are executed concurrently.
- Successful provider/term responses are cached for a short configurable TTL.
- The search API has IP-based rate limiting to protect the app from request spikes.
- Outbound calls are guarded by a provider-level concurrency limit so one spike cannot create unbounded fan-out.
- Repeated words are counted repeatedly, matching the user's exact input.
- Remote provider failures are surfaced as sanitized partial results instead of failing the entire search.
- The frontend is dependency-free so the project remains easy to run from Visual Studio.

## Load Handling

The current implementation includes a few production-oriented safeguards:

- `HttpClientFactory` prevents socket exhaustion from outbound provider calls.
- In-memory caching stores successful `(provider, term)` results for 5 minutes by default.
- The API is rate limited to 30 search requests per minute per remote IP, with a small queue.
- Provider calls use a shared concurrency gate, capped at 8 simultaneous outbound calls per provider by default.
- Provider timeouts prevent requests from waiting indefinitely on a slow remote service.
- Detailed structured logs can capture request flow, cache hits/misses, provider latency, provider failures, and concurrency-slot waits.
- `GET /health` exposes a lightweight application health endpoint for uptime checks.
- Swagger UI is enabled in Development for interactive API exploration.

For a multi-instance production deployment, the in-memory cache and rate limiter should be replaced or backed by shared infrastructure such as Redis or an API gateway, because each app instance currently keeps its own local limits and cache.

## Health and Logging

The app exposes a lightweight health endpoint:

```http
GET /health
```

It reports application availability without actively calling the external search providers. This avoids turning health checks into extra provider traffic.

Detailed search logging is enabled by default and can be disabled with `Observability:DetailedSearchLogging=false`. Structured logging is used around the main request flow and provider calls. Useful production signals include request completion time, provider latency, provider errors, cache hits and misses, and time spent waiting for a provider concurrency slot.

## Security Notes

- Swagger UI is only enabled in Development.
- The browser UI escapes values before rendering provider output.
- Provider exception details, HTTP reason phrases, and response parsing details are logged server-side but are not returned to the frontend.
- Provider tokens are not committed. Configure them with user secrets, environment variables, or a secret manager.
