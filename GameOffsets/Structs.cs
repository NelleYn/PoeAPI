namespace GameOffsets
{
    /// <summary>
    /// Field offsets of the Render component, kept as an enum because
    /// <see cref="RenderComponentOffsets"/> is the only consumer and the two have historically drifted
    /// apart when the numbers were duplicated. THIS ENUM IS THE ONE PLACE THE NUMBERS LIVE; the struct
    /// only references them. Provenance is documented per member here and on the struct's fields.
    /// </summary>
    public enum Offsets
    {
        /// <summary>
        /// Render position, three floats. MEASURED 2026-09-16 against a live client (PathOfExile_KG,
        /// pid 13660, zone "The Reliquary" 1_5_7). Its X and Y are exactly equal to the Positioned
        /// component's WorldPosition X and Y, which is how it was confirmed: two independent
        /// components agreeing is a much stronger statement than one plausible-looking triple of
        /// floats. Was 0x78 (pre-2026 fork).
        /// </summary>
        RenderComponentOffsetsPos = 0x120,

        /// <summary>
        /// Model bounds, three floats, immediately after <see cref="RenderComponentOffsetsPos"/>.
        /// MEASURED 2026-09-16. Was 0x84 (pre-2026 fork).
        /// </summary>
        RenderComponentOffsetsBounds = 0x12C,

        /// <summary>
        /// Render name, an EMBEDDED native UTF-16 string (not a pointer to one). MEASURED 2026-09-16:
        /// length sits at 0x158 and capacity at 0x160, i.e. at +0x10 and +0x18 from here, which is
        /// exactly the shape of <see cref="Native.NativeStringU"/>. Was 0x98 (pre-2026 fork).
        /// </summary>
        RenderComponentOffsetsName = 0x148,

        /// <summary>
        /// NOT MEASURED on this client. Inherited from the pre-2026 fork. Everything else in this
        /// component moved by ~0xA8 bytes between that fork and this client, so assume this is wrong.
        /// </summary>
        RenderComponentOffsetsRotation = 0xB8,

        /// <summary>
        /// NOT MEASURED on this client. Inherited from the pre-2026 fork. See the note on
        /// <see cref="RenderComponentOffsetsRotation"/>.
        /// </summary>
        RenderComponentOffsetsHeight = 0xD4
    }
}
