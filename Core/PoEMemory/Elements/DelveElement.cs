using System;
using System.Collections.Generic;
using System.Linq;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Helpers;
using GameOffsets.Native;
using SharpDX;

namespace ExileCore.PoEMemory.Elements;

/// <summary>
/// UI element representing the Delve mine grid, exposing its big cells with caching keyed on the client rectangle.
/// </summary>
public class DelveElement : Element
{
    private readonly CachedValue<IList<DelveBigCell>> _cachedValue;
    private RectangleF rect = RectangleF.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelveElement"/> class.
    /// </summary>
    public DelveElement()
    {
        _cachedValue = new ConditionalCache<IList<DelveBigCell>>(() => Children.Select(x => x.AsObject<DelveBigCell>()).ToList(), () =>
        {
            if (GetClientRect() != rect)
            {
                rect = GetClientRect();
                return true;
            }

            return false;
        });
    }

    /// <summary>
    /// Gets the big cells of the Delve grid.
    /// </summary>
    public IList<DelveBigCell> Cells => _cachedValue.Value;
}

/// <summary>
/// UI element representing a large cell in the Delve grid, containing smaller <see cref="DelveCell"/> entries.
/// </summary>
public class DelveBigCell : Element
{
    private readonly CachedValue<IList<DelveCell>> _cachedValue;
    private RectangleF rect = RectangleF.Empty;
    private string text;
    private long? type;

    /// <summary>
    /// Initializes a new instance of the <see cref="DelveBigCell"/> class.
    /// </summary>
    public DelveBigCell()
    {
        _cachedValue = new ConditionalCache<IList<DelveCell>>(() => Children.Select(x => x.AsObject<DelveCell>()).ToList(), () =>
        {
            if (GetClientRect() != rect)
            {
                rect = GetClientRect();
                return true;
            }

            return false;
        });
    }

    /// <summary>
    /// Gets the smaller cells contained in this big cell.
    /// </summary>
    public IList<DelveCell> Cells => _cachedValue.Value;

    /// <summary>
    /// Gets the cached pointer to the cell type structure.
    /// </summary>
    public long TypePtr => type ?? (type = M.Read<long>(Address + 0x150)).Value;

    /// <inheritdoc />
    public override string Text => text = text ?? M.ReadStringU(M.Read<long>(TypePtr + 0x0));
}

/// <summary>
/// UI element representing a single Delve cell, exposing its mods, mines, and type strings.
/// </summary>
public class DelveCell : Element
{
    private DelveCellInfoStrings info;
    private NativeStringU mods => M.Read<NativeStringU>(Address + 0x498);

    /// <summary>
    /// Gets the modifier text for this cell.
    /// </summary>
    public string Mods => mods.ToString(M);

    private NativeStringU mines => M.Read<NativeStringU>(M.Read<long>(Address + 0x150) + 0x38);

    /// <summary>
    /// Gets the mines text for this cell.
    /// </summary>
    public string MinesText => mines.ToString(M);

    /// <summary>
    /// Gets the descriptive string set for this cell.
    /// </summary>
    public DelveCellInfoStrings Info => info = info ?? ReadObjectAt<DelveCellInfoStrings>(0x640);

    /// <summary>
    /// Gets the internal type identifier for this cell.
    /// </summary>
    public string Type => M.ReadStringU(M.Read<long>(Address + 0x650, 0x0));

    /// <summary>
    /// Gets the human-readable type name for this cell.
    /// </summary>
    public string TypeHuman => M.ReadStringU(M.Read<long>(Address + 0x650, 0x8));

    /// <inheritdoc />
    public override string Text => $"{Info.TestString} [{Info.TestString5}]";
}

/// <summary>
/// Memory object holding the set of descriptive strings associated with a <see cref="DelveCell"/>.
/// </summary>
public class DelveCellInfoStrings : RemoteMemoryObject
{
    private bool _interesting;
    private string _testString;
    private string _testString2;
    private string _testString3;
    private string _testString4;
    private string _testString5;
    private string _testStringGood;

    /// <summary>
    /// Gets the primary descriptive string.
    /// </summary>
    public string TestString => _testString = _testString ?? M.ReadStringU(M.Read<long>(Address));

    /// <summary>
    /// Gets the primary descriptive string with newlines inserted before upper-case letters.
    /// </summary>
    public string TestStringGood => _testStringGood = _testStringGood ?? _testString.InsertBeforeUpperCase(Environment.NewLine);

    /// <summary>
    /// Gets the second descriptive string.
    /// </summary>
    public string TestString2 => _testString2 = _testString2 ?? M.ReadStringU(M.Read<long>(Address + 0x8));

    /// <summary>
    /// Gets the third descriptive string.
    /// </summary>
    public string TestString3 => _testString3 = _testString3 ?? M.ReadStringU(M.Read<long>(Address + 0x40));

    /// <summary>
    /// Gets the fourth descriptive string.
    /// </summary>
    public string TestString4 => _testString4 = _testString4 ?? M.ReadStringU(M.Read<long>(Address + 0x58));

    /// <summary>
    /// Gets the fifth descriptive string.
    /// </summary>
    public string TestString5
    {
        get
        {
            var s = _testString5;
            if (s != null) return s;

            _testString5 = M.ReadStringU(M.Read<long>(Address + 0x60));

            return _testString5;
        }
    }

    /// <summary>
    /// Gets a value indicating whether this cell is considered interesting based on its descriptive strings.
    /// </summary>
    public bool Interesting
    {
        get
        {
            if (_testString5 == null)
            {
                var testString5 = TestString5;

                if (testString5.Length > 1 && !testString5.EndsWith("Azurite") && !TestString.StartsWith("Azurite3") &&
                    !testString5.EndsWith("Weapons") && !testString5.EndsWith("Armour") && !testString5.EndsWith("Jewellery") &&
                    !testString5.EndsWith("Items"))
                    _interesting = true;
                else if (TestString.StartsWith("Obstruction")) _interesting = true;
            }

            return _interesting;
        }
    }
}
