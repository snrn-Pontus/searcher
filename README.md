# Search Hit Counter

Visual Studio ASP.NET Core web project that compares search hit counts across multiple search providers.

A user enters one or more words. The app searches each word independently for each provider, sums the hit counts per provider, and displays only the totals. For example, `Hello world` searches `Hello` and `world` separately, then presents each provider's combined count.

## Tech Stack

- ASP.NET Core / C# / .NET 10 LTS
- Vanilla HTML, CSS, and JavaScript frontend
- xUnit test project
- Swagger UI in Development

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
load-tests/search-smoke.js
```

## Configuration

Provider endpoints are configured in `appsettings.json`. API tokens are intentionally not committed.

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

## Build, Test, and Run

Prerequisite: .NET 10 SDK or Visual Studio with .NET 10 support.

```bash
dotnet restore
dotnet test
dotnet run --project searcher
```

Open:

```text
http://localhost:5164
```

Development-only API tooling:

```text
http://localhost:5164/swagger
http://localhost:5164/openapi/v1.json
http://localhost:5164/health
```

## API

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
        { "term": "Hello", "hits": 5000000, "error": null, "succeeded": true, "fromCache": false },
        { "term": "world", "hits": 8000000, "error": null, "succeeded": true, "fromCache": true }
      ]
    }
  ],
  "elapsedMilliseconds": 250
}
```

`fromCache` shows whether a provider/term result came from the in-memory cache. Provider failures are returned as sanitized partial results, so successful provider results are still useful.

## Architecture

```text
Browser
  -> POST /api/search
    -> ASP.NET Core rate limiter
    -> SearchService
      -> SearchQueryParser
      -> CachedSearchProvider
        -> ProviderConcurrencyLimiter
          -> AltavistaSearchProvider
          -> ClassicSongSearchProvider
          -> LibraryOfCongressSearchProvider
```

The providers share a generic typed base class for token handling, timeouts, HTTP status handling, JSON deserialization, sanitized errors, and hit-count validation. Altavista maps `totalHits`, Classic Song maps `totalSearchHits`, and Library of Congress maps `pagination.total`. Each provider only owns its request shape and provider-specific response mapping.

## Scalability and Reliability

The project includes production-oriented safeguards without adding external infrastructure:

- `HttpClientFactory` for outbound provider calls.
- Per-IP API rate limiting.
- Short-lived in-memory caching per `(provider, term)`.
- Shared provider concurrency limits to prevent unbounded fan-out.
- Provider timeouts and partial failure handling.
- Structured logging keeps one successful search-completion event at `Information`, detailed cache/provider success-path events at `Debug`, and degraded behavior at `Warning`. Verbosity is controlled with standard `Logging:LogLevel` configuration.
- Lightweight `/health` endpoint that does not call external providers.

For a multi-instance production deployment, local cache/rate limits should move to shared infrastructure such as Redis or an API gateway. Useful next steps would be OpenTelemetry tracing, provider-specific circuit breakers/retries, provider quota tracking, and distributed rate limiting.

The provided assignment endpoints appear to return synthetic hit counts, so those providers may produce very similar totals for the same terms. Library of Congress returns a real search-result total through `pagination.total`.

## Security Notes

- API tokens are not committed; use user secrets, environment variables, or a secret manager.
- Swagger UI is Development-only.
- The browser UI escapes rendered values.
- Provider exception details, HTTP reason phrases, and parsing details are logged server-side but not returned to the frontend.
- CORS is not enabled by default.

## Load Test Smoke Check

A small k6 smoke test is included as a quick optimization signal, not a full benchmark.

```bash
dotnet run --project searcher
k6 run load-tests/search-smoke.js
```

Target another URL:

```bash
BASE_URL=https://localhost:7215 k6 run load-tests/search-smoke.js
```
