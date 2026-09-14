namespace ExileCore.Shared.Enums
{
    public enum InventoryType
    {
        InvalidInventory, //Incase inventory isn't opened.
        PlayerInventory,
        NormalStash,
        QuadStash,
        CurrencyStash,
        EssenceStash,
        DivinationStash,
        MapStash,
        FragmentStash,
        DelveStash,

        BlightStash = 10,
        DeliriumStash = 11,
        MetamorphStash = 12,
        UniqueStash = 13,
        FlaskStash = 14,
        GemStash = 15,
        VendorInventory = 16,
        UltimatumStash = 17,
    }
}
