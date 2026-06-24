using System;
using System.Collections.Concurrent;
using ExileCore.PoEMemory.MemoryObjects;

namespace ExileCore;
public class EntityCacheContainer
{
    public readonly ConcurrentDictionary<uint, Entity> Entities;
    public DateTime LastUsedDate;
    public int InstanceId;
}