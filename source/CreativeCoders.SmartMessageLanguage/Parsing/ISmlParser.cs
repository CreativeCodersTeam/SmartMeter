using CreativeCoders.SmartMessageLanguage.Framing;
using JetBrains.Annotations;

namespace CreativeCoders.SmartMessageLanguage.Parsing;

[PublicAPI]
public interface ISmlParser
{
    /// <summary>Parses all OBIS values contained in the given frame.</summary>
    /// <param name="frame">Frame produced by <see cref="SmlMessageDetector"/>.</param>
    SmlParseResult Parse(SmlFrame frame);

    /// <summary>Parses all OBIS values contained in the given de-escaped payload.</summary>
    /// <param name="payload">Payload bytes of an SML frame (without start/end escape).</param>
    SmlParseResult Parse(ReadOnlySpan<byte> payload);
}
