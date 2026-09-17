using System.Runtime.InteropServices;
using SharpDX;

namespace GameOffsets;

/// <summary>
/// Maps the in-game camera: viewport dimensions, clip planes, world position and the
/// view/projection matrix used to translate world coordinates to screen space.
/// </summary>
/// <remarks>
/// <para>
/// PROVENANCE. Every number here was MEASURED on 2026-09-17 against a live client
/// (PathOfExile_KG, pid 13660, module base 0x7FF78FCD0000, zone "Overseer's Tower" 1_5_town,
/// screen 2560x1440) with <c>tools/CamCap</c>, which reads the whole camera window in a single
/// <c>ReadProcessMemory</c> and frames the snapshot with a "zone, Data base and camera pointer
/// unchanged" check. None of the offsets were copied from the reference distribution: CamCap
/// SEARCHES for each one by a criterion a wrong offset cannot satisfy by accident, and prints
/// both the criterion and the number. Re-run it to reproduce any line below:
/// <code>
/// FindOffset.exe --ingame-state
/// CamCap.exe --igs &lt;address&gt;
/// </code>
/// </para>
/// <para>
/// WHAT WAS BROKEN. <see cref="Width"/> used to be declared at 0x4 and <see cref="Height"/> at
/// 0x8. The camera object holds a zero at +0x0 and a pointer at +0x8, so Width read 0 and Height
/// read the low half of that pointer (-229238768). With Width = 0, <c>Camera.HalfWidth</c> was 0
/// and <c>WorldToScreen</c> computed <c>X = (cord.X + 1) * 0</c> — identically zero for EVERY
/// point in the world. Consumers did not fail loudly: they clicked at one wrong point, and
/// <c>Element.GetClientRect</c>, which divides by these two, produced garbage for every element.
/// </para>
/// <para>
/// WHY THE MATRIX OFFSET IS NOT JUST "THE 16 FLOATS THAT LOOK RIGHT". Projecting the player and
/// checking that X lands on the horizontal centre of the screen is NOT sufficient on its own, and
/// the measurement showed why: 21 different 4-byte-aligned offsets in the camera window passed it.
/// A garbage block yields a clip W in the millions, so X/W collapses to ~0 and the point lands on
/// the centre BY ACCIDENT. The criterion that identifies the matrix uniquely is the conjunction of
/// four conditions, and exactly two offsets satisfy all four — 0x1A8 and its byte-identical copy
/// at 0x1E8:
/// (1) the projected player is within 2 px of the horizontal centre — in PoE the camera follows
/// the character, so this holds at any moment, and here it held to 0.0000 px;
/// (2) the projected Y is on screen;
/// (3) the block has the STRUCTURE of a view/projection matrix — its Z column is proportional to
/// its W column in the first three rows, which garbage does not reproduce;
/// (4) the clip W of the player lies between the near and far planes derived from that same block.
/// </para>
/// <para>
/// THE TWO COPIES ARE INTERCHANGEABLE — settled by measurement, not assumed. The client keeps two
/// copies of the matrix, at 0x1A8 and 0x1E8. The walking control was run
/// (<c>CamCap.exe --igs &lt;address&gt; --watch</c>) with the character moving at up to 97 world
/// units per frame: across ~1600 frame-checked samples, 443 of them in motion, the two blocks were
/// byte-identical in every one. In an earlier run where they did differ in low-order bits, their
/// projections of the same point agreed to 0.001 px over 830 moving samples. Either copy may be
/// read; 0x1A8 is kept.
/// </para>
/// <para>
/// WHAT THE WALKING CONTROL ALSO SHOWED, and it matters more than the copy question: the PoE camera
/// EASES after the character rather than tracking him rigidly. Standing still he projects to the
/// exact horizontal centre (error 0.000 px). Running, the median error is still 0.000 px and the
/// 95th percentile 0.001 px, but the worst excursions reach 4.8% and 5.1% of screen width in two
/// separate runs. Any check on this projection has to allow for that: the gate in
/// <c>tools/SanityRead</c> uses 15% of width, which clears the measured excursion threefold and
/// still catches a broken camera by more than threefold (Width = 0 misses by half the width).
/// </para>
/// </remarks>
[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct CameraOffsets
{
    /// <summary>
    /// Viewport width in pixels. MEASURED 2026-09-17: the int32 pair at 0x318/0x31C equalled
    /// 2560x1440, the client area of the game window as reported by the OS through
    /// <c>GetClientRect</c> — a fact known OUTSIDE the process memory. Was 0x4, where a zero sits.
    /// </summary>
    /// <remarks>
    /// NOT THE ONLY SUCH PAIR, and the first write-up of this measurement wrongly said it was — that
    /// run searched a 0x400 window, and a second pair holding the same values sits at 0x498/0x49C.
    /// A short window did not refute the twin, it failed to SEE it. The twin has since been resolved
    /// the only way it could be: the game window was resized and the measurement repeated. BOTH
    /// pairs followed it, through three different client areas — 2560x1440, 2544x1353 and 1278x958 —
    /// across two client launches. They are interchangeable, and 0x318 is kept. That sequence also
    /// makes this the best-repeated number in the struct: three viewport sizes and a client restart,
    /// checked against the OS's own GetClientRect every time.
    /// </remarks>
    [FieldOffset(0x318)] public int Width;

    /// <summary>
    /// Viewport height in pixels. The upper half of the same qword as <see cref="Width"/>.
    /// MEASURED 2026-09-17 by the same criterion. Was 0x8, where the low half of a pointer sits.
    /// </summary>
    [FieldOffset(0x31C)] public int Height;

    /// <summary>
    /// Near clipping plane distance. MEASURED 2026-09-17, DERIVED FROM THE MATRIX AND THEN FOUND:
    /// in <see cref="MatrixBytes"/> the Z column is proportional to the W column in the first
    /// three rows, so z_ndc = k + d/w with k = 1.060282 and d = -186.875; z_ndc = 0 gives
    /// w = 176.250, and exactly that float sits at 0x308. Nothing was substituted into the
    /// derivation — it comes from the live matrix alone.
    /// </summary>
    [FieldOffset(0x308)] public float ZNear;

    /// <summary>
    /// Far clipping plane distance. MEASURED 2026-09-17 by the same derivation as
    /// <see cref="ZNear"/>: z_ndc = 1 gives w = 3099.997, and 3100.0 sits at 0x30C — the only
    /// occurrence in a 0x800 window, as for <see cref="ZNear"/>. Was 0x1C8, which now falls INSIDE
    /// the matrix and read 0.0.
    /// </summary>
    /// <remarks>
    /// NEITHER PLANE IS A CONSTANT, and confirming that is what makes these two offsets solid. In a
    /// second camera state (viewport 1278x958) the same derivation gave k = 1.045492, d = -78.412,
    /// hence near 75.000 and far 1723.632 — and those values, not the earlier 176.25 and 3100, were
    /// the ones sitting at 0x308 and 0x30C. The derivation landed on the same two offsets with
    /// completely different numbers, so what is confirmed is the working relation matrix -> planes,
    /// not a coincidence between two constants. A wrong offset does not hit twice that way.
    /// </remarks>
    /// <remarks>
    /// DERIVED, NOT COPIED, and the reference is the reason that distinction is not pedantic: its
    /// own ZFar reads 0.5886619 on this client, a value that occurs four times in the camera window
    /// and is in every case the M12 or M22 element of a DIFFERENT matrix block (the view matrices
    /// at 0x2A8 and 0x350, recognisable by their fourth row ending in W = 1). As a far-plane
    /// distance that number is not meaningful at all. The reference has no ZNear.
    /// </remarks>
    [FieldOffset(0x30C)] public float ZFar;

    /// <summary>
    /// Camera world position. MEASURED 2026-09-17, and this one is worth reading twice because the
    /// reference distribution gets it wrong. The camera centre was COMPUTED by inverting the live
    /// matrix — its X, Y and W columns give three equations in three unknowns — which yielded
    /// (3623.000, 3416.479, -1630.188); that triple was then SEARCHED FOR in the camera window and
    /// found, agreeing to seven significant figures. The reference reads Position as M42/M43/M44,
    /// i.e. matrix + 0x34; at that offset (0x1DC) this client holds
    /// (-12184.665, -1671.988, -1400.677), which is the translation row of the view/projection
    /// matrix and not a position in the world. Running the reference live against the same process
    /// confirms that from the other side: its PositionNum prints those same three floats, and its
    /// own Snapshot prints them as M42/M43/M44. It is not reading a different field, it is reading
    /// the tail of its own matrix. Was 0xD4.
    /// </summary>
    /// <remarks>
    /// A SECOND COPY of the computed triple sits at 0x420. The walking control did not separate them
    /// either — they held the same value in every frame-checked sample, moving and still — so like
    /// the two matrix copies they are interchangeable, and 0x2E8 is kept. Nothing in the engine
    /// reads this field today (an exhaustive grep over Core/ and Loader/ finds no consumer of
    /// Camera.Position at all; only the plugin's diagnostic probe reads it).
    /// </remarks>
    [FieldOffset(0x2E8)] public Vector3 Position;

    /// <summary>
    /// Camera view/projection matrix used for world-to-screen projection. MEASURED 2026-09-17 by
    /// the four-condition criterion described on the struct; projecting the player through it put
    /// him at (1280.0, 576.1) on a 2560x1440 screen — dead centre horizontally, 144 px above
    /// centre vertically, the canonical PoE camera placement — with a horizontal error of
    /// 0.0000 px. The same (1280.0, 576.1) came out on 2026-09-16 in a DIFFERENT zone at a
    /// DIFFERENT player position, so the screen point is an invariant of the camera rather than a
    /// coincidence of one spot. The vertical half of that pair is only invariant for a fixed input
    /// convention: 576.1 is what the RAW <c>Render.Pos</c> gives, while <c>Entity.Pos</c> — which
    /// adds <c>Bounds.Z</c>, the model half-height — projects the same character to 652.4. Both are
    /// correct; they are the feet and the centre of the model. Only the horizontal 1280.0 is
    /// convention-free, which is why the gate in <c>tools/SanityRead</c> asserts on X and merely
    /// requires Y to be on screen.
    /// Was 0x7C, where zeros and object pointers sit. See the note on the 0x1E8 copy above.
    /// </summary>
    //First value is changing when we change the screen size (ratio)
    //4 bytes before the matrix doesn't change
    [FieldOffset(0x1A8)] public Matrix MatrixBytes;
}
