using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Searcher.Models;
using Searcher.Options;
using Searcher.Providers;
using Searcher.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.AddMemoryCache();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("SearchApi", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            httpContext.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 30,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 5,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst
            }));
});

builder.Services.AddOptions<ObservabilityOptions>()
    .Bind(builder.Configuration.GetSection(ObservabilityOptions.SectionName));

builder.Services.AddOptions<SearchEngineOptions>()
    .Bind(builder.Configuration.GetSection(SearchEngineOptions.SectionName))
    .Validate(SearchEngineOptions.IsValid, "Search provider options are invalid.");

builder.Services.AddSingleton<ISearchQueryParser, SearchQueryParser>();
builder.Services.AddSingleton<IProviderConcurrencyGate, ProviderConcurrencyGate>();
builder.Services.AddScoped<ISearchService, SearchService>();
builder.Services.AddHttpClient<AltavistaSearchProvider>();
builder.Services.AddHttpClient<ClassicSongSearchProvider>();
builder.Services.AddScoped<ISearchProvider>(sp =>
    new CachedSearchProvider(
        new ProviderConcurrencyLimiter(
            sp.GetRequiredService<AltavistaSearchProvider>(),
            sp.GetRequiredService<IProviderConcurrencyGate>(),
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SearchEngineOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservabilityOptions>>(),
            sp.GetRequiredService<ILogger<ProviderConcurrencyLimiter>>()),
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SearchEngineOptions>>(),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservabilityOptions>>(),
        sp.GetRequiredService<ILogger<CachedSearchProvider>>()));
builder.Services.AddScoped<ISearchProvider>(sp =>
    new CachedSearchProvider(
        new ProviderConcurrencyLimiter(
            sp.GetRequiredService<ClassicSongSearchProvider>(),
            sp.GetRequiredService<IProviderConcurrencyGate>(),
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SearchEngineOptions>>(),
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservabilityOptions>>(),
            sp.GetRequiredService<ILogger<ProviderConcurrencyLimiter>>()),
        sp.GetRequiredService<Microsoft.Extensions.Caching.Memory.IMemoryCache>(),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<SearchEngineOptions>>(),
        sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<ObservabilityOptions>>(),
        sp.GetRequiredService<ILogger<CachedSearchProvider>>()));

var app = builder.Build();

app.UseDefaultFiles(new DefaultFilesOptions
{
    DefaultFileNames = ["index.html"]
});
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Search Hit Counter API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseRateLimiter();

app.MapHealthChecks("/health", new HealthCheckOptions
{
    AllowCachingResponses = false
});

app.MapPost("/api/search", async Task<Results<Ok<SearchResponse>, BadRequest<ProblemDetails>>> (
        SearchRequest request,
        ISearchService searchService,
        Microsoft.Extensions.Options.IOptions<ObservabilityOptions> observabilityOptions,
        ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            logger.LogWarning("Rejected empty search request.");
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Search query is required.",
                Detail = "Enter one or more words to search for.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            if (observabilityOptions.Value.DetailedSearchLogging)
            {
                logger.LogInformation("Received search request with {QueryLength} characters.", request.Query.Length);
            }
            var response = await searchService.SearchAsync(request.Query, cancellationToken);
            if (observabilityOptions.Value.DetailedSearchLogging)
            {
                logger.LogInformation(
                    "Completed search request for {TermCount} terms across {ProviderCount} providers in {ElapsedMilliseconds} ms.",
                    response.Terms.Count,
                    response.Providers.Count,
                    response.ElapsedMilliseconds);
            }
            return TypedResults.Ok(response);
        }
        catch (SearchValidationException ex)
        {
            logger.LogWarning(ex, "Rejected invalid search request.");
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid search query.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    })
    .RequireRateLimiting("SearchApi")
    .WithName("Search")
    .WithOpenApi();

app.Run();

public partial class Program;
