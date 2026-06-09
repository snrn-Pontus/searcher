using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Searcher.Logging;
using Searcher.Models;
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

builder.Services
    .AddSearchOptions(builder.Configuration)
    .AddSearchServices()
    .AddSearchProvider<AltavistaSearchProvider>()
    .AddSearchProvider<ClassicSongSearchProvider>()
    .AddSearchProvider<LibraryOfCongressSearchProvider>();

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
        ILogger<Program> logger,
        CancellationToken cancellationToken) =>
    {
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            AppLog.RejectedEmptySearch(logger);
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Search query is required.",
                Detail = "Enter one or more words to search for.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        try
        {
            if (logger.IsEnabled(LogLevel.Debug))
            {
                AppLog.SearchRequestReceived(logger, request.Query.Length);
            }

            var response = await searchService.SearchAsync(request.Query, cancellationToken);
            if (logger.IsEnabled(LogLevel.Information))
            {
                AppLog.SearchRequestCompleted(logger, response.Terms.Count, response.Providers.Count, response.ElapsedMilliseconds);
            }

            return TypedResults.Ok(response);
        }
        catch (SearchValidationException ex)
        {
            AppLog.InvalidSearchRejected(logger, ex);
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid search query.",
                Detail = ex.Message,
                Status = StatusCodes.Status400BadRequest
            });
        }
    })
    .RequireRateLimiting("SearchApi")
    .WithName("Search");

app.Run();
