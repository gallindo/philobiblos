namespace Philobiblos.Application.Books;

public static class IsbnValidator
{
    public static string Normalize(string isbn) =>
        new(isbn.Where(character => character is not ('-' or ' ')).ToArray());

    public static bool IsValid(string? isbn)
    {
        if (string.IsNullOrWhiteSpace(isbn))
        {
            return false;
        }

        var normalized = Normalize(isbn);
        return normalized.Length switch
        {
            10 => HasValidIsbn10Checksum(normalized),
            13 => HasValidIsbn13Checksum(normalized),
            _ => false,
        };
    }

    private static bool HasValidIsbn10Checksum(string isbn)
    {
        var sum = 0;
        for (var index = 0; index < isbn.Length; index++)
        {
            var character = isbn[index];
            int value;
            if (character is >= '0' and <= '9')
            {
                value = character - '0';
            }
            else if (index == 9 && character is 'X' or 'x')
            {
                value = 10;
            }
            else
            {
                return false;
            }

            sum += value * (10 - index);
        }

        return sum % 11 == 0;
    }

    private static bool HasValidIsbn13Checksum(string isbn)
    {
        if (!isbn.StartsWith("978", StringComparison.Ordinal) && !isbn.StartsWith("979", StringComparison.Ordinal))
        {
            return false;
        }

        var sum = 0;
        for (var index = 0; index < isbn.Length; index++)
        {
            var character = isbn[index];
            if (character is < '0' or > '9')
            {
                return false;
            }

            sum += (character - '0') * (index % 2 == 0 ? 1 : 3);
        }

        return sum % 10 == 0;
    }
}
