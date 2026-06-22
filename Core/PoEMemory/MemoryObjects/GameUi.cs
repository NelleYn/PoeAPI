using ExileCore.PoEMemory.Elements;

namespace ExileCore.PoEMemory.MemoryObjects;

public class GameUi : Element
{
    public Element UnusedPassivePointsButton => GetChildAtIndex(3);
    public int UnusedPassivePointsAmount => GetUnusedPassivePointsAmount();

    // Reconstructed (no upstream/offset source) — returns safe default; verify the child path in-game.
    public SentinelPanel SentinelPanel => null;

    // Reconstructed (no upstream/offset source) — returns safe default; verify the child path in-game.
    public AzmeriElement AzmeriElement => null;

    // Child indices recovered from the decompiler's (Element)N / int[] artifacts.
    public Element LifeOrb => GetChildAtIndex(1);
    public Element ManaOrb => GetChildAtIndex(2);
    public Element FlaskPanel => GetChildFromIndices(5, 1);

    private int GetUnusedPassivePointsAmount()
    {
        var numberInButton = GetChildAtIndex(3)?.GetChildAtIndex(1);
        if (numberInButton == null || !numberInButton.IsVisible)
        {
            return 0;
        }

        var success = int.TryParse(GetChildAtIndex(3)?.GetChildAtIndex(1)?.Text, out var result);
        return success ? result : 0;
    }
}
