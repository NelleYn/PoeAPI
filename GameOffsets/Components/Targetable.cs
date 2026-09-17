using System.Runtime.InteropServices;

namespace GameOffsets.Components;

/// <summary>
/// Inherited layout of the Targetable component. NOT MEASURED ON THIS CLIENT, and NOT the struct the
/// engine reads — that is <see cref="GameOffsets.TargetableComponentOffsets"/>, which was measured
/// on 2026-09-17 and puts the targetable flag at 0x50, not 0x30.
/// </summary>
/// <remarks>
/// This type is kept for its FORM, not its numbers: the order of the fields — a pointer, then
/// IsTargetable, IsHighlightable, IsTargetted, then a run of unknown flags — is what identified 0x51
/// as the highlight flag rather than a second targeting flag. The offsets themselves are stale; a
/// census over the whole zone shows 0x30..0x34 are not boolean bytes at all on this build, and the
/// pointer this layout puts at 0x28 does not appear at 0x48 either, so the block did not simply
/// move. Nothing in the fork reads this struct — only <c>ComponentHeader</c> from this namespace is
/// used — but two different numbers for one field must not sit in the tree unlabelled, which is what
/// this note is for.
/// </remarks>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Targetable
{
    [FieldOffset(0x0000)] public ComponentHeader Header;
    [FieldOffset(0x0028)] public long UnknownPtr0;
    [FieldOffset(0x0030)] public byte IsTargetable;
    [FieldOffset(0x0031)] public byte IsHighlightable;
    [FieldOffset(0x0032)] public byte IsTargetted;
    [FieldOffset(0x0033)] public byte UnknownBool0;
    [FieldOffset(0x0034)] public byte UnknownBool1;
    [FieldOffset(0x0035)] public byte UnknownBool2;
    [FieldOffset(0x0036)] public byte UnknownBool3;
    [FieldOffset(0x0037)] public byte UnknownBool4;
    [FieldOffset(0x0038)] public int UnknownInt0;
    [FieldOffset(0x003C)] public int UnknownInt1;
    [FieldOffset(0x0040)] public int UnknownInt2;
    [FieldOffset(0x0044)] public int UnknownInt3;
}
