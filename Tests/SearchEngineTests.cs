namespace Tests;

/// <summary>Behavioural tests for the trie-backed <see cref="SearchEngine"/>.</summary>
public sealed class SearchEngineTests
{
    /// <summary>Builds an engine with the given words inserted in order.</summary>
    private static SearchEngine Indexed(params string[] words)
    {
        SearchEngine engine = new();
        foreach (string word in words)
            engine.Insert(word);

        return engine;
    }

    /// <summary>Splits a comma-separated list and sorts it ordinally, as the wrapper.</summary>
    private static string[] Sorted(string commaSeparated) =>
        [.. commaSeparated.Split(',').OrderBy(word => word, StringComparer.Ordinal)];

    /// <summary>An inserted word is found, including single-byte and multibyte words.</summary>
    [Theory]
    [InlineData("cat")]
    [InlineData("a")]
    [InlineData("café")]
    [InlineData("日本")]
    [InlineData("🦀")]
    public void Contains_IsTrueForInsertedWord(string word) =>
        Assert.True(Indexed(word).Contains(word));

    /// <summary>Words never inserted are not found, including prefixes and extensions.</summary>
    [Theory]
    [InlineData("dog")]
    [InlineData("c")]
    [InlineData("ca")]
    [InlineData("cats")]
    [InlineData("catapul")]
    [InlineData("tac")]
    public void Contains_IsFalseForNonInsertedWord(string probe) =>
        Assert.False(Indexed("cat", "catapult").Contains(probe));

    /// <summary>A prefix of an indexed word is not itself a hit.</summary>
    [Fact]
    public void Contains_IsExactMatchNotPrefixMatch()
    {
        SearchEngine engine = Indexed("cat", "cats");

        Assert.True(engine.Contains("cat"));
        Assert.True(engine.Contains("cats"));
        Assert.False(engine.Contains("ca"));
        Assert.False(engine.Contains("catsx"));
    }

    /// <summary>Re-inserting a word changes nothing, single-byte words included.</summary>
    [Fact]
    public void Insert_RepeatedWord_IsIdempotent()
    {
        SearchEngine engine = Indexed("cat", "cat", "a", "a");

        Assert.True(engine.Contains("cat"));
        Assert.True(engine.Contains("a"));
        Assert.Equal(["cat"], engine.PrefixSearch("cat"));
        Assert.Equal(["a"], engine.PrefixSearch("a"));
    }

    /// <summary>Insertion order does not change what the engine finds.</summary>
    [Theory]
    [InlineData("cat,dog,bird")]
    [InlineData("bird,dog,cat")]
    [InlineData("dog,bird,cat")]
    public void Insert_IsOrderIndependent(string order)
    {
        string[] words = order.Split(',');
        SearchEngine engine = Indexed(words);

        foreach (string word in words)
            Assert.True(engine.Contains(word));

        Assert.Equal(["cat"], engine.PrefixSearch("c"));
        Assert.False(engine.Contains("ca"));
    }

    /// <summary>Insertion order does not matter when words are prefixes of each other.</summary>
    [Theory]
    [InlineData("cats,cat")]
    [InlineData("cat,cats")]
    [InlineData("catapult,cat,cats,car")]
    [InlineData("c,cat")]
    [InlineData("cat,c")]
    public void Insert_IsOrderIndependentForPrefixRelatedWords(string order)
    {
        string[] words = order.Split(',');
        SearchEngine engine = Indexed(words);

        foreach (string word in words)
            Assert.True(engine.Contains(word), word);
    }

    /// <summary>Returns every indexed word with the prefix, including the prefix itself.</summary>
    [Theory]
    [InlineData("c", "cat,cats,car")]
    [InlineData("ca", "cat,cats,car")]
    [InlineData("cat", "cat,cats")]
    [InlineData("car", "car")]
    [InlineData("cats", "cats")]
    public void PrefixSearch_ReturnsEveryWordWithThatPrefix(string prefix, string expected)
    {
        SearchEngine engine = Indexed("cat", "cats", "car");

        Assert.Equal(Sorted(expected), engine.PrefixSearch(prefix));
    }

    /// <summary>A word that only shares a shorter leading path is not returned.</summary>
    [Fact]
    public void PrefixSearch_ExcludesWordsThatDoNotHaveThePrefix()
    {
        SearchEngine engine = Indexed("c", "cat");

        Assert.Equal(["cat"], engine.PrefixSearch("ca"));
        Assert.Equal(["c", "cat"], engine.PrefixSearch("c"));
    }

    /// <summary>Nested words appear once each, not once per level.</summary>
    [Fact]
    public void PrefixSearch_ReturnsNestedWordsExactlyOnce()
    {
        SearchEngine engine = Indexed("a", "ab", "abc");

        Assert.Equal(["a", "ab", "abc"], engine.PrefixSearch("a"));
    }

    /// <summary>Absent leading byte, dead end, or an over-long prefix yields no matches.</summary>
    [Fact]
    public void PrefixSearch_WithoutMatches_IsEmpty()
    {
        SearchEngine engine = Indexed("cat", "cats", "car");

        Assert.Empty(engine.PrefixSearch("dog"));
        Assert.Empty(engine.PrefixSearch("cart"));
        Assert.Empty(engine.PrefixSearch("caz"));
    }

    /// <summary>Multibyte characters match byte-wise; mid-character cuts still resolve.</summary>
    [Fact]
    public void PrefixSearch_HandlesMultibyteCharacters()
    {
        SearchEngine engine = Indexed("café", "cafard");

        Assert.Equal(["cafard", "café"], engine.PrefixSearch("caf"));
        Assert.Equal(["café"], engine.PrefixSearch("café"));
    }

    /// <summary>A randomized corpus agrees with a HashSet oracle for both lookups.</summary>
    [Fact]
    public void RandomCorpus_MatchesHashSetOracle()
    {
        Random random = new(20260926);
        HashSet<string> words = [];
        while (words.Count < 200)
            words.Add(RandomWord(random));

        SearchEngine engine = Indexed([.. words]);
        IEnumerable<string> probes = words.Concat(["a", "d", "aaaaaaa", "zzz", "abcd"]);

        foreach (string probe in probes)
        {
            Assert.Equal(words.Contains(probe), engine.Contains(probe));
            Assert.Equal(
                words
                    .Where(word => word.StartsWith(probe, StringComparison.Ordinal))
                    .OrderBy(word => word, StringComparer.Ordinal),
                engine.PrefixSearch(probe)
            );
        }
    }

    /// <summary>A word of 1-7 characters over 'a'-'d', so prefix overlaps are dense.</summary>
    private static string RandomWord(Random random)
    {
        char[] chars = new char[random.Next(1, 8)];
        for (int i = 0; i < chars.Length; i++)
            chars[i] = (char)random.Next('a', 'e');

        return new string(chars);
    }
}
