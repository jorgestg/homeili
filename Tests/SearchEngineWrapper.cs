using Homeili;

namespace Tests;

public sealed class SearchEngine
{
    private readonly Fst _fst = new();

    public void Insert(string word) => _fst.Insert(word);

    public bool Contains(string word) => _fst.Contains(word);

    public IReadOnlyList<string> PrefixSearch(string prefix, int limit = 10) =>
        _fst.PrefixSearch(prefix, limit);
}
