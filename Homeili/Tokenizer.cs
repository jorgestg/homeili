using System.Buffers;
using System.Globalization;
using System.Text;

namespace Homeili;

public static class Tokenizer
{
    public const int TokenMaxLength = byte.MaxValue;

    public static List<byte[]> TokenizeFromUtf16Chars(ReadOnlySpan<char> chars)
    {
        List<byte[]> tokens = new(capacity: 4);
        List<byte> buffer = new(capacity: TokenMaxLength);
        bool hasLatinLetter = false;

        while (!chars.IsEmpty)
        {
            OperationStatus status = Rune.DecodeFromUtf16(
                chars,
                out Rune rune,
                out int charsConsumed
            );

            if (status != OperationStatus.Done)
            {
                FlushToken(tokens, buffer);
                hasLatinLetter = false;
                chars = chars[(charsConsumed > 0 ? charsConsumed : 1)..];
                continue;
            }

            if (IsLatinLetter(rune))
            {
                AppendNormalizedLatinRune(buffer, rune);
                hasLatinLetter = true;
            }
            else if (!IsCombiningMark(rune) || !hasLatinLetter)
            {
                FlushToken(tokens, buffer);
                hasLatinLetter = false;
            }

            chars = chars[charsConsumed..];
        }

        FlushToken(tokens, buffer);
        return tokens;
    }

    public static List<byte[]> TokenizeFromUtf8(ReadOnlySpan<byte> utf8Bytes)
    {
        List<byte[]> tokens = new(capacity: 4);
        List<byte> buffer = new(capacity: TokenMaxLength);
        bool hasLatinLetter = false;

        while (!utf8Bytes.IsEmpty)
        {
            OperationStatus status = Rune.DecodeFromUtf8(
                utf8Bytes,
                out Rune rune,
                out int bytesConsumed
            );
            if (status != OperationStatus.Done)
            {
                FlushToken(tokens, buffer);
                hasLatinLetter = false;
                utf8Bytes = utf8Bytes[(bytesConsumed > 0 ? bytesConsumed : 1)..];
                continue;
            }

            if (IsLatinLetter(rune))
            {
                AppendNormalizedLatinRune(buffer, rune);
                hasLatinLetter = true;
            }
            else if (!IsCombiningMark(rune) || !hasLatinLetter)
            {
                FlushToken(tokens, buffer);
                hasLatinLetter = false;
            }

            utf8Bytes = utf8Bytes[bytesConsumed..];
        }

        FlushToken(tokens, buffer);
        return tokens;
    }

    public static List<byte[]> TokenizeFromAsciiChars(ReadOnlySpan<char> chars)
    {
        List<byte[]> tokens = new(capacity: 4);
        List<byte> buffer = new(capacity: TokenMaxLength);
        foreach (char character in chars)
        {
            if (IsAsciiLetter(character))
            {
                buffer.Add((byte)char.ToLowerInvariant(character));
            }
            else
            {
                FlushToken(tokens, buffer);
            }
        }

        FlushToken(tokens, buffer);
        return tokens;
    }

    public static List<byte[]> TokenizeFromAsciiBytes(ReadOnlySpan<byte> asciiBytes)
    {
        List<byte[]> tokens = new(capacity: 4);
        List<byte> buffer = new(capacity: TokenMaxLength);
        foreach (byte value in asciiBytes)
        {
            if (IsAsciiLetter(value))
            {
                buffer.Add(
                    value is (>= (byte)'A' and <= (byte)'Z') ? (byte)(value + ('a' - 'A')) : value
                );
            }
            else
            {
                FlushToken(tokens, buffer);
            }
        }

        FlushToken(tokens, buffer);
        return tokens;
    }

    private static bool IsAsciiLetter(char value) =>
        value is (>= 'A' and <= 'Z') or (>= 'a' and <= 'z');

    private static bool IsAsciiLetter(byte value) =>
        value is (>= (byte)'A' and <= (byte)'Z') or (>= (byte)'a' and <= (byte)'z');

    private static bool IsLatinLetter(Rune rune)
    {
        UnicodeCategory category = Rune.GetUnicodeCategory(rune);
        if (
            category
            is not (
                UnicodeCategory.UppercaseLetter
                or UnicodeCategory.LowercaseLetter
                or UnicodeCategory.TitlecaseLetter
                or UnicodeCategory.ModifierLetter
                or UnicodeCategory.OtherLetter
            )
        )
            return false;

        int value = rune.Value;
        return value
            is (>= 0x0041 and <= 0x007A)
                or (>= 0x00AA and <= 0x02FF)
                or (>= 0x1D00 and <= 0x1DBF)
                or (>= 0x1E00 and <= 0x1EFF)
                or (>= 0x2C60 and <= 0x2C7F)
                or (>= 0xA720 and <= 0xA7FF)
                or (>= 0xAB30 and <= 0xAB6F)
                or (>= 0x10780 and <= 0x107BF)
                or (>= 0x1DF00 and <= 0x1DFFF);
    }

    private static bool IsCombiningMark(Rune rune) =>
        Rune.GetUnicodeCategory(rune)
            is UnicodeCategory.NonSpacingMark
                or UnicodeCategory.SpacingCombiningMark
                or UnicodeCategory.EnclosingMark;

    private static void AppendNormalizedLatinRune(List<byte> buffer, Rune rune)
    {
        Rune loweredRune = Rune.ToLowerInvariant(rune);

        Span<char> loweredBuffer = stackalloc char[2];
        int written = loweredRune.EncodeToUtf16(loweredBuffer);
        loweredBuffer = loweredBuffer[..written];

        Span<char> normalizedBuffer = stackalloc char[4];
        loweredBuffer.TryNormalize(normalizedBuffer, out written, NormalizationForm.FormD);
        normalizedBuffer = normalizedBuffer[..written];

        foreach (Rune normalizedRune in normalizedBuffer.EnumerateRunes())
        {
            if (!IsCombiningMark(normalizedRune))
                AppendRune(buffer, normalizedRune);
        }
    }

    private static void AppendRune(List<byte> buffer, Rune rune)
    {
        Span<byte> encoded = stackalloc byte[4];
        int bytesWritten = rune.EncodeToUtf8(encoded);
        for (int i = 0; i < bytesWritten; i++)
            buffer.Add(encoded[i]);
    }

    private static void FlushToken(List<byte[]> tokens, List<byte> buffer)
    {
        if (buffer.Count == 0)
            return;

        tokens.Add(buffer.ToArray());
        buffer.Clear();
    }
}
