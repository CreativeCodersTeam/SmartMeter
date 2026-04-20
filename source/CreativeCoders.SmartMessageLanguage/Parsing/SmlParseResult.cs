namespace CreativeCoders.SmartMessageLanguage.Parsing;

/// <summary>
/// Result of parsing an SML frame.
/// </summary>
/// <param name="Values">All OBIS values extracted from the frame.</param>
/// <param name="Warnings">Non-fatal issues encountered during parsing.</param>
public sealed record SmlParseResult(
    IReadOnlyList<ObisValue> Values,
    IReadOnlyList<string> Warnings);
