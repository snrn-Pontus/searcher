namespace Searcher.Services;

public sealed class SearchValidationException(string message) : Exception(message);