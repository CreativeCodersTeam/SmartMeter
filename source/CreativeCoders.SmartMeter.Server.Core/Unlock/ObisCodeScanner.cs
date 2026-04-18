using CreativeCoders.Core;

namespace CreativeCoders.SmartMeter.Server.Core.Unlock;

/// <summary>
/// Searches raw SML byte streams for occurrences of 6-byte OBIS identifiers
/// (A B C D E F). Used to verify a successful PIN unlock without extending
/// the SML parser itself.
/// </summary>
internal static class ObisCodeScanner
{
    /// <summary>Parses an OBIS string of the form "A-B:C.D.E*F" into its 6 bytes.</summary>
    public static byte[] ParseObis(string obis)
    {
        Ensure.IsNotNullOrWhitespace(obis);

        var colon = obis.IndexOf(':');
        var dash = obis.IndexOf('-');
        var star = obis.IndexOf('*');

        if (dash < 0 || colon < 0 || star < 0 || dash > colon)
        {
            throw new FormatException($"Invalid OBIS code '{obis}'. Expected format 'A-B:C.D.E*F'.");
        }

        var a = byte.Parse(obis.AsSpan(0, dash));
        var b = byte.Parse(obis.AsSpan(dash + 1, colon - dash - 1));

        var cde = obis.Substring(colon + 1, star - colon - 1).Split('.');

        if (cde.Length != 3)
        {
            throw new FormatException($"Invalid OBIS code '{obis}'. Expected three dot-separated values between ':' and '*'.");
        }

        var c = byte.Parse(cde[0]);
        var d = byte.Parse(cde[1]);
        var e = byte.Parse(cde[2]);
        var f = byte.Parse(obis.AsSpan(star + 1));

        return [a, b, c, d, e, f];
    }

    /// <summary>
    /// Returns the subset of <paramref name="expected"/> whose 6-byte OBIS pattern
    /// appears in <paramref name="data"/>.
    /// </summary>
    public static IEnumerable<string> FindMatches(byte[] data, IReadOnlyList<string> expected)
    {
        Ensure.NotNull(data);
        Ensure.NotNull(expected);

        foreach (var code in expected)
        {
            var pattern = ParseObis(code);

            if (ContainsPattern(data, pattern))
            {
                yield return code;
            }
        }
    }

    private static bool ContainsPattern(byte[] data, byte[] pattern)
    {
        if (pattern.Length == 0 || data.Length < pattern.Length)
        {
            return false;
        }

        for (var i = 0; i <= data.Length - pattern.Length; i++)
        {
            var match = true;

            for (var j = 0; j < pattern.Length; j++)
            {
                if (data[i + j] != pattern[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return true;
            }
        }

        return false;
    }
}
