namespace ExileCore.PoEMemory.MemoryObjects;

/// <summary>
/// Wraps the in-memory prophecy record, exposing its identifiers, texts and seal cost.
/// </summary>
public class ProphecyDat : RemoteMemoryObject
{
    private string flavourText;
    private string id;
    private string name;
    private string predictionText;

    /// <summary>Gets or sets the row index of this prophecy.</summary>
    public int Index { get; set; }

    /// <summary>Gets the prophecy's identifier string.</summary>
    public string Id => id != null ? id : id = M.ReadStringU(M.Read<long>(Address), 255);

    /// <summary>Gets the prophecy's prediction text.</summary>
    public string PredictionText =>
        predictionText != null ? predictionText : predictionText = M.ReadStringU(M.Read<long>(Address + 0x8), 255);

    /// <summary>Gets the numeric prophecy identifier.</summary>
    public int ProphecyId => M.Read<int>(Address + 0x10);

    /// <summary>Gets the prophecy's display name.</summary>
    public string Name => name != null ? name : name = M.ReadStringU(M.Read<long>(Address + 0x14));

    /// <summary>Gets the prophecy's flavour text.</summary>
    public string FlavourText => flavourText != null ? flavourText : flavourText = M.ReadStringU(M.Read<long>(Address + 0x1c), 255);

    /// <summary>Gets the pointer to the prophecy chain this prophecy belongs to.</summary>
    public long ProphecyChainPtr => M.Read<long>(Address + 0x44); //TODO ProphecyChainDat

    /// <summary>Gets the position of this prophecy within its chain.</summary>
    public int ProphecyChainPosition => M.Read<int>(Address + 0x4c); //TODO ProphecyChainDat

    /// <summary>Gets a value indicating whether the prophecy is enabled.</summary>
    public bool IsEnabled => M.Read<byte>(Address + 0x50) > 0;

    /// <summary>Gets the seal cost of the prophecy.</summary>
    public int SealCost => M.Read<int>(Address + 0x51);

    /// <inheritdoc />
    public override string ToString()
    {
        return $"{Name}, {PredictionText}";
    }
}
