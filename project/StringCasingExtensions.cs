using System.Globalization;
using System.Text;

namespace Conventions;

public static class StringCasingExtensions
{
    private static readonly ThreadLocal<StringBuilder> _Builder = new(() => new StringBuilder());

    public static string ToUpperSnakeCase(this string input)
    {
        var span = input.AsSpan();
        return ToUpperSnakeCase(span);
    }

    public static string ToUpperSnakeCase(this ReadOnlySpan<char> input)
    {
        var globalBuilder = _Builder.Value!;
        ToUpperSnakeCase(globalBuilder, input);
        var result = globalBuilder.ToString();
        globalBuilder.Clear();
        return result;
    }

    private enum SnakeCaseLetterState
    {
        Initial,
        Uppercase,
        Lowercase,
        Separator,
    }

    public static void ToUpperSnakeCase(StringBuilder builder, ReadOnlySpan<char> input)
    {
        var state = SnakeCaseLetterState.Initial;
        const char separator = '_';
        for (var i = 0; i < input.Length; i++)
        {
            var ch = input[i];
            var category = char.GetUnicodeCategory(ch);
            switch (category)
            {
                case UnicodeCategory.UppercaseLetter:
                {
                    if (state == SnakeCaseLetterState.Lowercase)
                        builder.Append(separator);

                    state = SnakeCaseLetterState.Uppercase;
                    builder.Append(ch);
                    break;
                }
                case UnicodeCategory.SpaceSeparator:
                case UnicodeCategory.ConnectorPunctuation:
                case UnicodeCategory.DashPunctuation:
                {
                    builder.Append(separator);
                    state = SnakeCaseLetterState.Separator;
                    break;
                }
                case UnicodeCategory.LowercaseLetter:
                {
                    builder.Append(char.ToUpperInvariant(ch));
                    state = SnakeCaseLetterState.Lowercase;
                    break;
                }
                case UnicodeCategory.DecimalDigitNumber:
                {
                    builder.Append(ch);
                    state = SnakeCaseLetterState.Lowercase;
                    break;
                }
            }
        }
    }
}
