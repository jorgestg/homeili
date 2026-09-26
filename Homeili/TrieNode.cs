using System.Diagnostics.CodeAnalysis;

namespace Homeili;

public sealed class TrieNode(string? word)
{
    public Dictionary<byte, TrieNode>? Children { get; set; }
    public string? Word { get; set; } = word;

    [MemberNotNullWhen(true, nameof(Word))]
    [MemberNotNullWhen(true, nameof(Postings))]
    public bool IsTerminal => Word != null;

    public List<TrieNodePosting>? Postings => IsTerminal ? (field ??= []) : null;
}

public sealed class TrieNodePosting(int documentId, int position)
{
    public int DocumentId { get; } = documentId;
    public int Position { get; } = position;
}
