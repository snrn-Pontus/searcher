using Searcher.Models;

namespace Searcher.Services;

public interface ISearchService
{
    Task<SearchResponse> SearchAsync(string query, CancellationToken cancellationToken);
}
