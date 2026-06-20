using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using GameOffsets;
using MoreLinq;

namespace ExileCore.PoEMemory;

/// <summary>
/// Immutable description of a single file discovered in the game's in-memory file table.
/// </summary>
public struct FileInformation
{
    /// <summary>Initializes a new instance describing a file located in memory.</summary>
    /// <param name="ptr">The pointer to the file's information record.</param>
    /// <param name="changeCount">The area-change count at which the file was last touched.</param>
    /// <param name="test1">Implementation-specific metadata value.</param>
    /// <param name="test2">Implementation-specific metadata value.</param>
    public FileInformation(long ptr, int changeCount, int test1, int test2)
    {
        Ptr = ptr;
        ChangeCount = changeCount;
        Test1 = test1;
        Test2 = test2;
    }

    /// <summary>Gets the pointer to the file's information record.</summary>
    public long Ptr { get; }

    /// <summary>Gets the area-change count at which the file was last touched.</summary>
    public int ChangeCount { get; }

    /// <summary>Gets an implementation-specific metadata value.</summary>
    public int Test1 { get; }

    /// <summary>Gets an implementation-specific metadata value.</summary>
    public int Test2 { get; }
}

/// <summary>
/// Reads the game's in-memory file table and projects it into a dictionary keyed by file path.
/// </summary>
public class FilesFromMemory
{
    private readonly IMemory mem;

    /// <summary>Initializes a new instance bound to the given memory reader.</summary>
    /// <param name="memory">The memory reader used to access the game process.</param>
    public FilesFromMemory(IMemory memory)
    {
        mem = memory;
    }

    /// <summary>
    /// Reads every file in the game's file table in parallel.
    /// </summary>
    /// <returns>A dictionary mapping each file path to its <see cref="FileInformation"/>.</returns>
    public Dictionary<string, FileInformation> GetAllFiles()
    {
        var files = new ConcurrentDictionary<string, FileInformation>();
        var fileRoot = mem.AddressOfProcess + mem.BaseOffsets[OffsetsName.FileRoot];
        var start = mem.Read<long>(fileRoot + 0x8);

        var filesPointer = mem.ReadListPointer(new IntPtr(start));

        Parallel.ForEach(filesPointer, p =>
        {
            var filesOffsets = mem.Read<FilesOffsets>(p);
            var advancedInformation = mem.Read<GameOffsets.FileInformation>(filesOffsets.MoreInformation);
            if (advancedInformation.String.buf == 0) return;
            var str = advancedInformation.String.ToString(mem);

            if (str.Length <= 0) return;

            files.TryAdd(
                str,
                new FileInformation(filesOffsets.MoreInformation, advancedInformation.AreaCount, advancedInformation.Test1,
                    advancedInformation.Test2));
        });

        return files.ToDictionary();
    }

    /// <summary>
    /// Reads every file in the game's file table sequentially, caching the decoded path strings.
    /// </summary>
    /// <returns>A dictionary mapping each file path to its <see cref="FileInformation"/>.</returns>
    public Dictionary<string, FileInformation> GetAllFilesSync()
    {
        var files = new Dictionary<string, FileInformation>();
        var fileRoot = mem.AddressOfProcess + mem.BaseOffsets[OffsetsName.FileRoot];
        var start = mem.Read<long>(fileRoot + 0x8);
        var filesPointer = mem.ReadListPointer(new IntPtr(start));

        foreach (var p in filesPointer)
        {
            var filesOffsets = mem.Read<FilesOffsets>(p);
            var advancedInformation = mem.Read<GameOffsets.FileInformation>(filesOffsets.MoreInformation);
            if (advancedInformation.String.buf == 0) continue;

            var str = RemoteMemoryObject.Cache.StringCache.Read($"{nameof(FilesFromMemory)}{advancedInformation.String.buf}",
                () => advancedInformation.String.ToString(mem));

            if (str.Length <= 0) continue;

            files[str] = new FileInformation(filesOffsets.MoreInformation, advancedInformation.AreaCount, advancedInformation.Test1,
                advancedInformation.Test2);
        }

        return files;
    }
}
