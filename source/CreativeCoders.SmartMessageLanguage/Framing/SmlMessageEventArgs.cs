using CreativeCoders.Core;

namespace CreativeCoders.SmartMessageLanguage.Framing;

/// <summary>
/// Event arguments raised by <see cref="SmlMessageDetector"/> when a complete
/// SML transport frame has been detected in the byte stream.
/// </summary>
public sealed class SmlMessageEventArgs : EventArgs
{
    /// <summary>Creates a new instance wrapping the given frame.</summary>
    /// <param name="frame">The detected frame.</param>
    public SmlMessageEventArgs(SmlFrame frame)
    {
        Frame = Ensure.NotNull(frame);
    }

    /// <summary>The detected SML frame.</summary>
    public SmlFrame Frame { get; }
}
