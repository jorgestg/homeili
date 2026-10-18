using System.Text;
using Homeili;

namespace Tests;

public sealed class SearchEngine
{
    private readonly Fst _fst = new();
    private readonly Dictionary<string, string> _words = new(StringComparer.Ordinal);

    public void Insert(string word)
    {
        List<byte[]> tokens = Tokenizer.TokenizeFromUtf8(Encoding.UTF8.GetBytes(word));
        if (tokens.Count != 1)
            throw new ArgumentException("Expected a single token", nameof(word));

        byte[] token = tokens[0];
        string normalized = Encoding.UTF8.GetString(token);
        if (_words.ContainsKey(normalized))
            return;

        _fst.Insert(token, _words.Count + 100);
        _words.Add(normalized, word);
    }

    public bool Contains(string word)
    {
        List<byte[]> tokens = Tokenizer.TokenizeFromUtf8(Encoding.UTF8.GetBytes(word));
        return tokens.Count == 1 && _fst.Contains(tokens[0]);
    }

    public IReadOnlyList<string> PrefixSearch(string prefix, int limit = 10)
    {
        List<byte[]> tokens = Tokenizer.TokenizeFromUtf8(Encoding.UTF8.GetBytes(prefix));
        if (tokens.Count != 1)
            return [];

        string normalizedPrefix = Encoding.UTF8.GetString(tokens[0]);
        return _words
            .Where(word => word.Key.StartsWith(normalizedPrefix, StringComparison.Ordinal))
            .OrderBy(word => word.Key, StringComparer.Ordinal)
            .Take(limit)
            .Select(word => word.Value)
            .ToArray();
    }
}
