namespace UrlShortener.Core.Exceptions;

public class DuplicateAliasException(string alias)
    : Exception($"The alias '{alias}' is already in use.")
{
    public string Alias { get; } = alias;
}
