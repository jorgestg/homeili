using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Homeili;

public sealed class Trie
{
    private readonly Dictionary<byte, TrieNode> _children = [];

    public void Insert(ReadOnlySpan<byte> word)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(word.Length, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(word.Length, 255);

        if (TryGetChild(word[0], out TrieNode? child))
        {
            if (word.Length > 1)
                InsertCore(child, word, rest: word.Slice(1));
            else
                child.Word = Encoding.UTF8.GetString(word);

            return;
        }

        child = _children[word[0]] = new TrieNode(
            word: word.Length == 1 ? Encoding.UTF8.GetString(word) : null
        );

        if (child.IsTerminal)
            return;

        InsertCore(child, word, rest: word.Slice(1));
    }

    private bool TryGetChild(byte b, [NotNullWhen(true)] out TrieNode? child) =>
        _children.TryGetValue(b, out child);

    private static void InsertCore(TrieNode node, ReadOnlySpan<byte> word, ReadOnlySpan<byte> rest)
    {
        TrieNode lastMatch = node;
        if (
            ContainsCore(node, rest, ref lastMatch, out ReadOnlySpan<byte> containsRest)
            || containsRest.IsEmpty
        )
        {
            lastMatch.Word = Encoding.UTF8.GetString(word);
            return;
        }

        Debug.Assert(containsRest.Length > 0);

        lastMatch.Children ??= [];
        TrieNode newLastMatch = new(
            word: containsRest.Length == 1 ? Encoding.UTF8.GetString(word) : null
        );

        lastMatch.Children[containsRest[0]] = newLastMatch;

        if (newLastMatch.IsTerminal)
            return;

        InsertCore(newLastMatch, word, rest: containsRest.Slice(1));
    }

    public List<TrieNode> PrefixSearch(ReadOnlySpan<byte> word)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(word.Length, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(word.Length, 255);

        List<TrieNode> matches = [];
        if (!TryGetChild(word[0], out TrieNode? child))
            return matches;

        if (word.Length == 1)
        {
            CollectTerminalNodes(child, matches);
            return matches;
        }

        PrefixSearchCore(child, rest: word.Slice(1), matches);
        return matches;
    }

    private static void PrefixSearchCore(
        TrieNode node,
        ReadOnlySpan<byte> rest,
        List<TrieNode> matches
    )
    {
        if (!TryGetChild(node, rest[0], out TrieNode? child))
            return;

        if (rest.Length > 1)
        {
            PrefixSearchCore(child, rest.Slice(1), matches);
            return;
        }

        CollectTerminalNodes(child, matches);
    }

    private static void CollectTerminalNodes(TrieNode node, List<TrieNode> matches)
    {
        if (node.IsTerminal)
            matches.Add(node);

        if (node.Children == null)
            return;

        foreach ((_, TrieNode n) in node.Children)
        {
            CollectTerminalNodes(n, matches);
        }
    }

    public bool Contains(ReadOnlySpan<byte> word)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(word.Length, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(word.Length, 255);

        if (!_children.TryGetValue(word[0], out TrieNode? child))
            return false;

        return word.Length == 1
            ? child.IsTerminal
            : ContainsCore(child, word.Slice(1), ref child, out _);
    }

    private static bool ContainsCore(
        TrieNode node,
        ReadOnlySpan<byte> word,
        [NotNullWhen(true)] ref TrieNode lastMatch,
        out ReadOnlySpan<byte> rest
    )
    {
        byte b = word[0];
        if (!TryGetChild(node, b, out TrieNode? match))
        {
            rest = word;
            return false;
        }

        lastMatch = match;
        if (word.Length == 1)
        {
            rest = [];
            return lastMatch.IsTerminal;
        }

        return ContainsCore(lastMatch, word.Slice(1), ref lastMatch, out rest);
    }

    private static bool TryGetChild(TrieNode node, byte b, [NotNullWhen(true)] out TrieNode? child)
    {
        if (node.Children?.TryGetValue(b, out child) == true)
            return true;

        child = null;
        return false;
    }

    public void PrettyPrint(TextWriter writer)
    {
        foreach ((byte b, TrieNode node) in _children)
            PrettyPrintCore(b, node, writer, "  ", isLast: false);
    }

    private static void PrettyPrintCore(
        byte b,
        TrieNode node,
        TextWriter writer,
        string indent,
        bool isLast
    )
    {
        writer.WriteLine(
            $"{indent}{(isLast ? "└── " : "├── ")}" + $"{(char)b}{(node.IsTerminal ? " *" : "")}"
        );

        if (node.Children == null)
        {
            return;
        }

        int i = 0;
        foreach ((byte childB, TrieNode childNode) in node.Children.OrderBy(x => x.Key))
        {
            PrettyPrintCore(
                childB,
                childNode,
                writer,
                indent + (isLast ? "    " : "│   "),
                isLast: i++ == node.Children.Count - 1
            );
        }
    }
}
