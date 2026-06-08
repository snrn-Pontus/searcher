using Searcher.Models;

namespace Searcher.Providers;

public interface ISearchProvider
{
    string Name { get; }
    Task<SearchTermResult> SearchAsync(string term, CancellationToken cancellationToken);
}
