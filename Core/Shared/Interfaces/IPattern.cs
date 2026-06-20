namespace ExileCore.Shared.Interfaces;

/// <summary>
/// Describes a byte-signature pattern used to scan process memory.
/// </summary>
public interface IPattern
{
    /// <summary>Gets the human-readable name of the pattern.</summary>
    string Name { get; }

    /// <summary>Gets the bytes to match.</summary>
    byte[] Bytes { get; }

    /// <summary>Gets the mask string indicating which bytes are significant.</summary>
    string Mask { get; }

    /// <summary>Gets the offset applied to a successful match.</summary>
    int StartOffset { get; }
}
