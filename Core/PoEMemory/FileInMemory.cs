using System;
using System.Collections.Generic;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory;

/// <summary>
/// Base class for a game data file (a ".dat" table) located in the process's memory.
/// Provides access to the addresses of the individual records the file contains.
/// </summary>
public abstract class FileInMemory
{
    private readonly Func<long> fAddress;

    /// <summary>Initializes a new instance bound to the given memory reader and a late-bound file address.</summary>
    /// <param name="m">The memory reader used to access the game process.</param>
    /// <param name="address">A factory returning the current base address of the file.</param>
    protected FileInMemory(IMemory m, Func<long> address)
    {
        M = m;
        Address = address();
        fAddress = address;
    }

    /// <summary>Gets the memory reader used to access the game process.</summary>
    public IMemory M { get; }

    /// <summary>Gets the base address of the file captured at construction time.</summary>
    public long Address { get; }

    /// <summary>Gets the number of records stored in the file.</summary>
    private int NumberOfRecords => M.Read<int>(fAddress() + 0x38, 0x20);

    /// <summary>
    /// Enumerates the address of every record in the file. Yields a single zero
    /// when the file is unavailable or contains no records.
    /// </summary>
    /// <returns>A lazily evaluated sequence of record addresses.</returns>
    protected IEnumerable<long> RecordAddresses()
    {
        if (fAddress() == 0)
        {
            yield return 0;
            yield break;
        }

        var cnt = NumberOfRecords;

        if (cnt == 0)
        {
            yield return 0;
            yield break;
        }

        var firstRec = M.Read<long>(fAddress() + 0x38, 0x0);
        var lastRec = M.Read<long>(fAddress() + 0x38, 0x8);

        var recLen = (lastRec - firstRec) / cnt;

        for (var i = 0; i < cnt; i++)
        {
            yield return firstRec + i * recLen;
        }
    }
}
