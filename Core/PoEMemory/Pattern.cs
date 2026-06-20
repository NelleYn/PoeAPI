using System;
using System.Globalization;
using System.Linq;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory;

/// <summary>
/// Describes a byte signature (with a mask of significant/wildcard bytes) used to
/// locate code or data within the game process by pattern scanning.
/// </summary>
public class Pattern : IPattern
{
    /// <summary>Initializes a new pattern from a raw byte array.</summary>
    /// <param name="pattern">The signature bytes to search for.</param>
    /// <param name="mask">The mask string where 'x' marks a significant byte and any other character marks a wildcard.</param>
    /// <param name="name">A human-readable name identifying the pattern.</param>
    /// <param name="startOffset">An optional offset from which the scan begins.</param>
    public Pattern(byte[] pattern, string mask, string name, int startOffset = 0)
    {
        Bytes = pattern;
        Mask = mask;
        Name = name;
        StartOffset = startOffset;
    }

    /// <summary>Initializes a new pattern from a "\x"-delimited hexadecimal string.</summary>
    /// <param name="pattern">The signature as a string of "\x"-prefixed hexadecimal byte values.</param>
    /// <param name="mask">The mask string where 'x' marks a significant byte and any other character marks a wildcard.</param>
    /// <param name="name">A human-readable name identifying the pattern.</param>
    /// <param name="startOffset">An optional offset from which the scan begins.</param>
    public Pattern(string pattern, string mask, string name, int startOffset = 0)
    {
        var arr = pattern.Split(new[] {"\\x"}, StringSplitOptions.RemoveEmptyEntries);
        Bytes = arr.Select(y => byte.Parse(y, NumberStyles.HexNumber)).ToArray();
        Mask = mask;
        Name = name;
        StartOffset = startOffset;
    }

    /// <summary>Gets the human-readable name identifying this pattern.</summary>
    public string Name { get; }

    /// <summary>Gets the signature bytes to search for.</summary>
    public byte[] Bytes { get; }

    /// <summary>Gets the mask describing which bytes are significant ('x') and which are wildcards.</summary>
    public string Mask { get; }

    /// <summary>Gets the offset from which the scan begins.</summary>
    public int StartOffset { get; }
}
