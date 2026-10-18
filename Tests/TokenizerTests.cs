using System.Text;
using Homeili;

namespace Tests;

public sealed class TokenizerTests
{
    [Theory]
    [InlineData("CAFÉ NAÏVE", "cafe,naive")]
    [InlineData("ÉLÈVE—Straße", "eleve,straße")]
    [InlineData("COÖPERATE déjà-vu", "cooperate,deja,vu")]
    [InlineData("中文 café", "cafe")]
    [InlineData("e\u0301té", "ete")]
    public void TokenizeFromUtf8_NormalizesLatinTokens(string input, string expected)
    {
        List<string> tokens = Decode(Tokenizer.TokenizeFromUtf8(Encoding.UTF8.GetBytes(input)));

        Assert.Equal(expected.Split(','), tokens);
    }

    [Theory]
    [InlineData("CAFÉ NAÏVE", "cafe,naive")]
    [InlineData("ÉLÈVE—Straße", "eleve,straße")]
    [InlineData("中文 café", "cafe")]
    [InlineData("e\u0301té", "ete")]
    public void TokenizeFromUtf16Chars_NormalizesLatinTokens(string input, string expected)
    {
        List<string> tokens = Decode(Tokenizer.TokenizeFromUtf16Chars(input));

        Assert.Equal(expected.Split(','), tokens);
    }

    [Fact]
    public void Utf8Tokenizer_SplitsTokensAtMalformedSequences()
    {
        byte[] input = [0x66, 0xC3, 0x28, 0x6F, 0x6F];

        Assert.Equal(["f", "oo"], Decode(Tokenizer.TokenizeFromUtf8(input)));
    }

    [Fact]
    public void Utf16Tokenizer_SplitsTokensAtUnpairedSurrogates()
    {
        string input = "a\uD800b";

        Assert.Equal(["a", "b"], Decode(Tokenizer.TokenizeFromUtf16Chars(input)));
    }

    [Fact]
    public void AsciiTokenizers_ReturnLowercaseNonEmptyTokensIncludingFinalToken()
    {
        Assert.Equal(["hello", "world"], Decode(Tokenizer.TokenizeFromAsciiChars("Hello, WORLD!")));
        Assert.Equal(["hello", "world"], Decode(Tokenizer.TokenizeFromAsciiBytes("Hello, WORLD!"u8)));
    }

    private static List<string> Decode(IEnumerable<byte[]> tokens) =>
        tokens.Select(Encoding.UTF8.GetString).ToList();
}
