using System.Text;
using Homeili;

namespace Tests;

public sealed class SearchEngine
{
    private readonly Trie _trie = new();

    public void Insert(string word) => _trie.Insert(Encoding.UTF8.GetBytes(word));

    public bool Contains(string word) => _trie.Contains(Encoding.UTF8.GetBytes(word));

    public IReadOnlyList<string> PrefixSearch(string word) =>
        [
            .. _trie
                .PrefixSearch(Encoding.UTF8.GetBytes(word))
                .Select(node => node.Word!)
                .OrderBy(word => word, StringComparer.Ordinal),
        ];
}
