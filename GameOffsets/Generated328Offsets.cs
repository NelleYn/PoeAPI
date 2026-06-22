using System.Runtime.InteropServices;
using GameOffsets.Native;
using SharpDX;

namespace GameOffsets;

// Auto-generated from an in-process Marshal.OffsetOf dump of client 328.8. These structs were
// not present in the repo; they are additive (no consumers yet) and provide verified 328.8
// offsets for game components, server data and UI windows. Fields typed as a placeholder long
// are containers/strings whose exact repo type is not modeled; their FieldOffset is exact, so
// reading any sibling field remains correct.

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActiveAnimationOffsets
{
    [FieldOffset(0x10)] public int AnimationId;
    [FieldOffset(0x48)] public long SlowAnimationStartStagePtr;
    [FieldOffset(0x50)] public long SlowAnimationEndStagePtr;
    [FieldOffset(0x58)] public float SlowAnimationSpeed;
    [FieldOffset(0x5C)] public float NormalAnimationSpeed;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActorAnimationListOffsets
{
    [FieldOffset(0x10)] public NativePtrArray AnimationList;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActorAnimationStageOffsets
{
    [FieldOffset(0x4)] public float StageStart;
    [FieldOffset(0x8)] public int ActorAnimationListIndex;
    [FieldOffset(0x18)] public NativeStringU StageName;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActorDeployedObjectOffsets
{
    [FieldOffset(0x0)] public uint EntityId;
    [FieldOffset(0x4)] public ushort SkillId;
    [FieldOffset(0x8)] public ushort ObjectType;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActorSkillCooldownOffsets
{
    [FieldOffset(0x8)] public int SkillSubId;
    [FieldOffset(0x10)] public NativePtrArray Cooldowns;
    [FieldOffset(0x30)] public int MaxUses;
    [FieldOffset(0x3C)] public ushort SkillId;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ActorSkillOffsets
{
    [FieldOffset(0x8)] public byte SkillUseStage;
    [FieldOffset(0xC)] public byte CastType;
    [FieldOffset(0x10)] public SubActorSkillOffsets SubData;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AncestorShopWindowOffsets
{
    [FieldOffset(0x228)] public long UnitPtr;
    [FieldOffset(0x238)] public long ItemPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AncestorSidePanelOffsets
{
    [FieldOffset(0x240)] public long ItemPtr;
    [FieldOffset(0x258)] public long UnitPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AnimationControllerOffsets
{
    [FieldOffset(0x18)] public NativePtrArray ActiveAnimationsArrayPtr;
    [FieldOffset(0x180)] public long ActorAnimationArrayPtr;
    [FieldOffset(0x190)] public int AnimationInActorId;
    [FieldOffset(0x1A4)] public float AnimationProgress;
    [FieldOffset(0x1A8)] public int CurrentAnimationStage;
    [FieldOffset(0x1AC)] public float NextAnimationPoint;
    [FieldOffset(0x1B0)] public float AnimationSpeedMultiplier1;
    [FieldOffset(0x1B8)] public float MaxAnimationProgressOffset;
    [FieldOffset(0x1BC)] public float MaxAnimationProgress;
    [FieldOffset(0x1F8)] public float AnimationSpeedMultiplier2;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AreaLoadingStateOffsets
{
    [FieldOffset(0x348)] public long IsLoading;
    [FieldOffset(0x704)] public uint TotalLoadingScreenTimeMs;
    [FieldOffset(0x748)] public long AreaName;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct AreaTransitionComponentOffsets
{
    [FieldOffset(0xA8)] public ushort AreaId;
    [FieldOffset(0xB2)] public byte TransitionType;
    [FieldOffset(0x148)] public long WorldAreaInfoPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct BaseComponentOffsets
{
    [FieldOffset(0x10)] public long ItemInfo;
    [FieldOffset(0x60)] public NativeStringU PublicPrice;
    [FieldOffset(0xC5)] public byte CurrencyItemLevel;
    [FieldOffset(0xC6)] public byte Influence;
    [FieldOffset(0xC7)] public byte Corrupted;
    [FieldOffset(0xC8)] public int UnspentAbsorbedCorruption;
    [FieldOffset(0xCC)] public int ScourgedTier;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct CameraOffsetsInner
{
    [FieldOffset(0x100)] public Matrix MatrixBytes;
    [FieldOffset(0x174)] public Vector3 Position;
    [FieldOffset(0x214)] public float ZFar;
    [FieldOffset(0x270)] public int Width;
    [FieldOffset(0x274)] public int Height;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct CurrencyExchangeCurrencyPickerElementOffsets
{
    [FieldOffset(0x448)] public byte PickedCurrencyType;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct CurrencyExchangePanelOffsets
{
    [FieldOffset(0x3C8)] public long WantedItemCountInputPtr;
    [FieldOffset(0x3D0)] public long WantedItemTypePtr;
    [FieldOffset(0x3E8)] public long OfferedItemCountInputPtr;
    [FieldOffset(0x3F0)] public long OfferedItemTypePtr;
    [FieldOffset(0x428)] public uint Stock1TypeHash;
    [FieldOffset(0x42C)] public uint Stock2TypeHash;
    [FieldOffset(0x430)] public NativePtrArray Stock1;
    [FieldOffset(0x448)] public NativePtrArray Stock2;
    [FieldOffset(0x470)] public short MarketRateGet;
    [FieldOffset(0x472)] public short MarketRateGive;
    [FieldOffset(0x480)] public long RatioElementPtr;
    [FieldOffset(0x4F8)] public long CurrencyPickerPtr;
    [FieldOffset(0x548)] public long OrderListContainerPtr;
    [FieldOffset(0x560)] public NativePtrArray OrderList;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct DefaultEnvironmentSettingsOffsets
{
    [FieldOffset(0x8)] public NativeStringU Category;
    [FieldOffset(0x28)] public NativeStringU Name;
    [FieldOffset(0x48)] public int IndexInGroup;
    [FieldOffset(0x4C)] public Vector3 Value;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EnvironmentDataOffsets
{
    [FieldOffset(0x490)] public NativePtrArray DefaultSettingsList;
    [FieldOffset(0x528)] public NativePtrArray ActiveEnvironmentList;
    [FieldOffset(0x1550)] public NativePtrArray FootstepAudioList;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EnvironmentOffsets
{
    [FieldOffset(0x0)] public ushort Key;
    [FieldOffset(0x2)] public ushort Value0;
    [FieldOffset(0x4)] public float Value1;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct EscapeStateOffsets
{
    [FieldOffset(0x220)] public byte WasEverActive;
    [FieldOffset(0x380)] public long UIRootPtr;
    [FieldOffset(0x3B8)] public long HoveredElementPtr;
    [FieldOffset(0x70C)] public uint TotalActiveTimeMs;
    [FieldOffset(0x748)] public byte IsUnpaused;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ExpeditionAreaDataOffsets
{
    [FieldOffset(0x20)] public NativePtrArray ModsData;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ExpeditionDetonatorInfoOffsets
{
    [FieldOffset(0x1B0)] public long PlacementMarkerPtr;
    [FieldOffset(0x1E8)] public NativePtrArray PlacedExplosives;
    [FieldOffset(0x2A8)] public Vector2i DetonatorGridPosition;
    [FieldOffset(0x2B8)] public Vector2i PlacementIndicatorGridPosition;
    [FieldOffset(0x2D0)] public byte TotalExplosiveCount;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ExpeditionSagaOffsets
{
    [FieldOffset(0x18)] public NativePtrArray AreasData;
    [FieldOffset(0x30)] public byte AreaLevel;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct GameConfigKeyValueOffsets
{
    [FieldOffset(0x0)] public NativeStringU Key;
    [FieldOffset(0x20)] public NativeStringU Value;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct GameConfigOffsets
{
    [FieldOffset(0x188)] public long ConfigMap; // StdMap container - placeholder long
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct GameConfigSectionOffsets
{
    [FieldOffset(0x0)] public NativeStringU SectionKey;
    [FieldOffset(0x20)] public long SectionMap; // UnorderedMap container - placeholder long
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct GameStateOffsets
{
    [FieldOffset(0x8)] public NativePtrArray CurrentStatePtr;
    [FieldOffset(0x48)] public long State0;
    [FieldOffset(0x58)] public long State1;
    [FieldOffset(0x68)] public long State2;
    [FieldOffset(0x78)] public long State3;
    [FieldOffset(0x88)] public long State4;
    [FieldOffset(0x98)] public long State5;
    [FieldOffset(0xA8)] public long State6;
    [FieldOffset(0xB8)] public long State7;
    [FieldOffset(0xC8)] public long State8;
    [FieldOffset(0xD8)] public long State9;
    [FieldOffset(0xE8)] public long State10;
    [FieldOffset(0xF8)] public long State11;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct HarvestWorldObjectComponentOffsets
{
    [FieldOffset(0x20)] public NativePtrArray Seeds;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct HeistBlueprintComponentOffsets
{
    [FieldOffset(0x8)] public long Owner;
    [FieldOffset(0x1C)] public byte AreaLevel;
    [FieldOffset(0x1E)] public byte IsConfirmed;
    [FieldOffset(0x20)] public NativePtrArray Wings;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct HeistContractComponentOffsets
{
    [FieldOffset(0x8)] public long Owner;
    [FieldOffset(0x20)] public long ObjectiveKey;
    [FieldOffset(0x30)] public NativePtrArray Requirements;
    [FieldOffset(0x38)] public NativePtrArray Crew;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct HeistContractObjectiveOffsets
{
    [FieldOffset(0x0)] public long TargetKey;
    [FieldOffset(0x14)] public long ClientKey;
    [FieldOffset(0x1C)] public long Unknown1Key;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct HeistContractRequirementOffsets
{
    [FieldOffset(0x0)] public long JobKey;
    [FieldOffset(0x10)] public byte JobLevel;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct HeistEquipmentComponentDataOffsets
{
    [FieldOffset(0x18)] public long HeistEquipmentKey;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct HeistEquipmentComponentOffsets
{
    [FieldOffset(0x8)] public long OwnerKey;
    [FieldOffset(0x10)] public long DataKey;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct HeistEquipmentOffsets
{
    [FieldOffset(0x8)] public long BaseItemKey;
    [FieldOffset(0x18)] public long RequiredJobKey;
    [FieldOffset(0x20)] public int RequiredJobMinimumLevel;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ItemInfoOffsets
{
    [FieldOffset(0x10)] public byte ItemCellsSizeX;
    [FieldOffset(0x11)] public byte ItemCellsSizeY;
    [FieldOffset(0x48)] public NativeStringU Name;
    [FieldOffset(0x68)] public long BaseItemType;
    [FieldOffset(0x78)] public NativePtrArray Tags;
    [FieldOffset(0x90)] public NativeStringU FlavourText;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ItemsOnGroundLabelElementOffsets
{
    [FieldOffset(0x3E0)] public NativePtrArray VisibleItemLabels;
    [FieldOffset(0x4A8)] public long ConfigPtr;
    [FieldOffset(0x4D8)] public long LabelOnHoverPtr;
    [FieldOffset(0x4E0)] public long ItemOnHoverPtr;
    [FieldOffset(0x4F0)] public long LabelsOnGroundListPtr;
    [FieldOffset(0x4F8)] public long LabelCount;
    [FieldOffset(0x538)] public long LabelCount2;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct LoginStateOffsets
{
    [FieldOffset(0x2D0)] public long UIRootPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PlayerComponentOffsets
{
    [FieldOffset(0x168)] public NativeStringU PlayerName;
    [FieldOffset(0x18C)] public uint Xp;
    [FieldOffset(0x19C)] public long Attributes; // Buffer3<int> (12 bytes) - placeholder long
    [FieldOffset(0x1AC)] public byte Level;
    [FieldOffset(0x1AD)] public byte PantheonMinor;
    [FieldOffset(0x1AE)] public byte PantheonMajor;
    [FieldOffset(0x1D0)] public NativePtrArray Flags;
    [FieldOffset(0x1F0)] public long HideoutPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct PurchaseWindowOffsets
{
    [FieldOffset(0x2D0)] public long StashTabContainerPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct QuestStateOffsets
{
    [FieldOffset(0x0)] public long QuestAddress;
    [FieldOffset(0x10)] public byte QuestStateId;
    [FieldOffset(0x18)] public long @Base;
    [FieldOffset(0x34)] public long QuestStateTextAddress;
    [FieldOffset(0x3C)] public long QuestProgressTextAddress;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct SanctumFloorWindowDataOffsets
{
    [FieldOffset(0x158)] public int Gold;
    [FieldOffset(0x160)] public int CurrentResolve;
    [FieldOffset(0x164)] public int MaxResolve;
    [FieldOffset(0x168)] public int Inspiration;
    [FieldOffset(0x281)] public bool Flag1;
    [FieldOffset(0x282)] public bool Flag2;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct SanctumFloorWindowOffsets
{
    [FieldOffset(0x370)] public long InSanctumDataPtr;
    [FieldOffset(0x380)] public long OutOfSanctumDataPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct SanctumRewardWindowOffsets
{
    [FieldOffset(0x258)] public long RewardArrayContainer;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ServerDataArtifacts
{
    [FieldOffset(0x0)] public ushort LesserBrokenCircleArtifacts;
    [FieldOffset(0x2)] public ushort GreaterBrokenCircleArtifacts;
    [FieldOffset(0x4)] public ushort GrandBrokenCircleArtifacts;
    [FieldOffset(0x6)] public ushort ExceptionalBrokenCircleArtifacts;
    [FieldOffset(0x8)] public ushort LesserBlackScytheArtifacts;
    [FieldOffset(0xA)] public ushort GreaterBlackScytheArtifacts;
    [FieldOffset(0xC)] public ushort GrandBlackScytheArtifacts;
    [FieldOffset(0xE)] public ushort ExceptionalBlackScytheArtifacts;
    [FieldOffset(0x10)] public ushort LesserOrderArtifacts;
    [FieldOffset(0x12)] public ushort GreaterOrderArtifacts;
    [FieldOffset(0x14)] public ushort GrandOrderArtifacts;
    [FieldOffset(0x16)] public ushort ExceptionalOrderArtifacts;
    [FieldOffset(0x18)] public ushort LesserSunArtifacts;
    [FieldOffset(0x1A)] public ushort GreaterSunArtifacts;
    [FieldOffset(0x1C)] public ushort GrandSunArtifacts;
    [FieldOffset(0x1E)] public ushort ExceptionalSunArtifacts;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ServerDataBetrayalMember
{
    [FieldOffset(0x0)] public byte Id;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ServerDataMinimapIconOffsets
{
    [FieldOffset(0x14)] public Vector2i GridPosition;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct ServerPlayerDataOffsets
{
    [FieldOffset(0x190)] public NativePtrArray PassiveSkillIds;
    [FieldOffset(0x1D8)] public NativePtrArray PassiveJewelSocketIds;
    [FieldOffset(0x270)] public byte PlayerClass;
    [FieldOffset(0x274)] public int CharacterLevel;
    [FieldOffset(0x278)] public int PassiveRefundPointsLeft;
    [FieldOffset(0x27C)] public int QuestPassiveSkillPoints;
    [FieldOffset(0x280)] public int FreePassiveSkillPointsLeft;
    [FieldOffset(0x284)] public int TotalAscendencyPoints;
    [FieldOffset(0x288)] public int SpentAscendencyPoints;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct SocketColorList
{
    [FieldOffset(0x0)] public int Socket1Color;
    [FieldOffset(0x4)] public int Socket2Color;
    [FieldOffset(0x8)] public int Socket3Color;
    [FieldOffset(0xC)] public int Socket4Color;
    [FieldOffset(0x10)] public int Socket5Color;
    [FieldOffset(0x14)] public int Socket6Color;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct SocketedGemList
{
    [FieldOffset(0x0)] public long Socket1GemPtr;
    [FieldOffset(0x8)] public long Socket2GemPtr;
    [FieldOffset(0x10)] public long Socket3GemPtr;
    [FieldOffset(0x18)] public long Socket4GemPtr;
    [FieldOffset(0x20)] public long Socket5GemPtr;
    [FieldOffset(0x28)] public long Socket6GemPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct SocketsComponentOffsets
{
    [FieldOffset(0x10)] public SocketColorList Sockets;
    [FieldOffset(0x28)] public NativePtrArray LinkSizes;
    [FieldOffset(0x48)] public SocketedGemList SocketedGems;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct StashElementOffsets
{
    [FieldOffset(0x368)] public long StashTitlePanelPtr;
    [FieldOffset(0x370)] public long ExitButtonPtr;
    [FieldOffset(0x398)] public long StashTabContainerPtr1;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct StashTabContainerInventoryOffsets
{
    [FieldOffset(0x0)] public NativeStringU Name;
    [FieldOffset(0x80)] public long InventoryPtr;
    [FieldOffset(0x88)] public long StashButtonPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct StashTabContainerOffsets
{
    [FieldOffset(0x2A0)] public long TabSwitchBarPtr;
    [FieldOffset(0x2B8)] public long ViewAllStashesButtonPtr;
    [FieldOffset(0x2C8)] public long PinStashTabListButtonPtr;
    [FieldOffset(0x2F8)] public NativePtrArray Stashes;
    [FieldOffset(0x310)] public int VisibleStashIndex;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct StateInternalStructure
{
    [FieldOffset(0x0)] public byte StateEnumToName;
    [FieldOffset(0x8)] public long StatePtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct StateMachineComponentOffsets
{
    [FieldOffset(0x158)] public long StatesPtr;
    [FieldOffset(0x160)] public NativePtrArray StatesValues;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct SubActorSkillOffsets
{
    [FieldOffset(0x40)] public ushort Id;
    [FieldOffset(0x42)] public ushort Id2;
    [FieldOffset(0x48)] public long EffectsPerLevelPtr;
    [FieldOffset(0xB0)] public byte CanBeUsedWithWeapon;
    [FieldOffset(0xB1)] public byte CannotBeUsed;
    [FieldOffset(0xB4)] public int TotalUses;
    [FieldOffset(0xC8)] public int Cooldown;
    [FieldOffset(0xE8)] public int SoulsPerUse;
    [FieldOffset(0xEC)] public int TotalVaalUses;
    [FieldOffset(0xF8)] public long StatsPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct SubTileStructure
{
    [FieldOffset(0x0)] public NativePtrArray SubTileHeight;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TgtDetailStruct
{
    [FieldOffset(0x0)] public NativeStringU name;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TgtTileStruct
{
    [FieldOffset(0x8)] public NativeStringU TgtPath;
    [FieldOffset(0x28)] public long TgtDetailPtr;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TileStructure
{
    [FieldOffset(0x0)] public long SubTileDetailsPtr;
    [FieldOffset(0x8)] public long TgtFilePtr;
    [FieldOffset(0x10)] public NativePtrArray EntitiesList;
    [FieldOffset(0x30)] public short TileHeight;
    [FieldOffset(0x36)] public byte RotationSelector;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TooltipItemFrameElementOffsets
{
    [FieldOffset(0x338)] public long CopyTextPtr;
    [FieldOffset(0x358)] public bool IsAdvancedTooltipText;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct TreePassiveElementOffsets
{
    [FieldOffset(0x200)] public long PassiveSkillPtr;
    [FieldOffset(0x566)] public byte CanDeallocate;
    [FieldOffset(0x735)] public byte IsAllocatedForPlan;
    [FieldOffset(0x737)] public byte CanAllocate;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Type1EnvironmentSettingsOffsets
{
    [FieldOffset(0x0)] public float Value;
    [FieldOffset(0x4)] public byte @Override;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Type2EnvironmentSettingsOffsets
{
    [FieldOffset(0x0)] public float Value;
    [FieldOffset(0x4)] public byte @Override;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Type3EnvironmentSettingsOffsets
{
    [FieldOffset(0x0)] public Vector3 Value;
    [FieldOffset(0xC)] public byte @Override;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Type4EnvironmentSettingsOffsets
{
    [FieldOffset(0x0)] public byte Value;
    [FieldOffset(0x1)] public byte @Override;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Type5EnvironmentSettingsOffsets
{
    [FieldOffset(0x0)] public byte Value;
    [FieldOffset(0x1)] public byte @Override;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Type6EnvironmentSettingsOffsets
{
    [FieldOffset(0x0)] public NativeStringU Name;
    [FieldOffset(0x20)] public NativeStringU Category;
    [FieldOffset(0x40)] public float Value1;
    [FieldOffset(0x44)] public float Value2;
    [FieldOffset(0x48)] public byte @Override;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct Type7PlusEnvironmentSettingsOffsets
{
    [FieldOffset(0x0)] public Vector4 Value;
    [FieldOffset(0x40)] public Vector4 Value2;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct UltimatumPanelOffsets
{
    [FieldOffset(0x310)] public NativePtrArray OfferedModifiers;
    [FieldOffset(0x328)] public int SelectedModifierIndex;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct VillageInfoOffsets
{
    [FieldOffset(0x160)] public NativePtrArray Workers;
    [FieldOffset(0x178)] public NativePtrArray WorkersForSale;
    [FieldOffset(0x190)] public int InitialResources;
    [FieldOffset(0x2A8)] public NativePtrArray Stats;
    [FieldOffset(0x2C0)] public int ShipInfo;
    [FieldOffset(0x3E0)] public int PortRequest;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct VisibleItemLabelGroupOffsets
{
    [FieldOffset(0x0)] public NativePtrArray Labels;
    [FieldOffset(0x20)] public Vector2 GroupPosition;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct VisibleItemLabelOffsets
{
    [FieldOffset(0x0)] public long ElementPtr;
    [FieldOffset(0x8)] public uint EntityId;
    [FieldOffset(0xC)] public Vector2 PositionOffset;
}

[StructLayout(LayoutKind.Explicit, Pack = 1)]
public struct WorldDataOffsets
{
    [FieldOffset(0xA8)] public CameraOffsetsInner Camera;
}

