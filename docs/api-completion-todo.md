# API completion roadmap

Status of recovering the protected logic (see also `ported-from-dll.md`).

## Deobfuscation status

- The shipped `ExileCore.dll` uses **ConfuserEx-style anti-tamper in JIT mode** (or a custom variant):
  method bodies are decrypted only transiently at JIT time and never written back to the module image.
- A static **module memory dump** (dnSpyEx *Save Module*) therefore still contains the encrypted bodies
  (verified: 800/7539 methods still scrambled in the dump).
- Correct path: an **injecting, force-JIT dumper** (ExtremeDumper / MegaDumper). Pending.
- Until a decrypted dump exists, memory-reading logic (offsets) cannot be recovered and is left as `// TODO`.

## Work buckets

### A. Derivable now without the dump (89)
Pure-logic / infra / settings / records / disposables — reconstructable from signatures + intent.

- `Core/ActionOverlay.cs`
- `Core/BackgroundTask.cs`
- `Core/ControllerInput.cs`
- `Core/CoreDebugSettings.cs`
- `Core/CoreFontSettings.cs`
- `Core/CoreLoggingSettings.cs`
- `Core/CorePerformanceSettings.cs`
- `Core/CorePluginSettings.cs`
- `Core/DataExporter.cs`
- `Core/DebugMessage.cs`
- `Core/DefaultInputManager.cs`
- `Core/DefaultMemoryBackend.cs`
- `Core/DelegateCompiler.cs`
- `Core/FuncAttachment.cs`
- `Core/Graphics.SetTextScaleDisposable.cs`
- `Core/IInputManager.cs`
- `Core/ImGuiHelpers.cs`
- `Core/Limits.cs`
- `Core/MenuWindow.MainDebugTableRecord.cs`
- `Core/MouseMoveStroke.cs`
- `Core/MouseMoveStrokePoint.cs`
- `Core/NamedPipeHandler.cs`
- `Core/PagedMemoryBackend.cs`
- `Core/PoEMemory/Components/ArmourStatRange.cs`
- `Core/PoEMemory/Components/HarvestInfrastructureMod.cs`
- `Core/PoEMemory/Components/Sockets.Socket.cs`
- `Core/PoEMemory/Components/StateMachineState.cs`
- `Core/PoEMemory/Elements/InventoryElements/BlightInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/DeliriumInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/FlaskInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/GemInventoryItem.cs`
- `Core/PoEMemory/Elements/InventoryElements/MetamorphInventoryItem.cs`
- `Core/PoEMemory/Elements/ItemsOnGroundLabelElement.VisibleGroundItemDescription.cs`
- `Core/PoEMemory/Elements/NpcLine.cs`
- `Core/PoEMemory/Elements/VendorStashTabContainer.cs`
- `Core/PoEMemory/Elements/VendorStashTabContainerInventory.cs`
- `Core/PoEMemory/Elements/Village/VillageWorker.cs`
- `Core/PoEMemory/Elements/Village/VillageWorkerForSale.cs`
- `Core/PoEMemory/FilesInMemory/CachedStatDescription.cs`
- `Core/PoEMemory/FilesInMemory/CachedStatDescriptionSection.cs`
- `Core/PoEMemory/FilesInMemory/GemEffects.cs`
- `Core/PoEMemory/FilesInMemory/ItemVisualIdentities.cs`
- `Core/PoEMemory/FilesInMemory/MiscAnimatedList.cs`
- `Core/PoEMemory/FilesInMemory/ModTranslationReplacerInput.cs`
- `Core/PoEMemory/FilesInMemory/QuestFlagsDat.cs`
- `Core/PoEMemory/FilesInMemory/StatDescriptionWrapper.cs`
- `Core/PoEMemory/FilesInMemory/StatTranslationUtils.cs`
- `Core/PoEMemory/FilesInMemory/UniqueItemDescriptions.cs`
- `Core/PoEMemory/FilesInMemory/Village/VillageJobSkillLevels.cs`
- `Core/PoEMemory/FilesInMemory/Village/VillageJobs.cs`
- `Core/PoEMemory/MemoryObjects/AncestorFightOptionReward.cs`
- `Core/PoEMemory/MemoryObjects/Camera.CameraSnapshot.cs`
- `Core/PoEMemory/MemoryObjects/EnvironmentSettingValue.cs`
- `Core/PoEMemory/MemoryObjects/EscapeState.cs`
- `Core/PoEMemory/MemoryObjects/LoginState.cs`
- `Core/PoEMemory/MemoryObjects/SellWindowHideout.cs`
- `Core/PoEMemory/MemoryObjects/TypedEnvironmentData.cs`
- `Core/PoEMemory/StringPattern.cs`
- `Core/ProcessPicker.cs`
- `Core/RenderQ/ImGuiRender.PopFont.cs`
- `Core/Shared/Attributes/ConditionalDisplayAttribute.cs`
- `Core/Shared/Attributes/SubmenuAttribute.cs`
- `Core/Shared/BuildError.cs`
- `Core/Shared/BuildTarget.cs`
- `Core/Shared/BuildWarning.cs`
- `Core/Shared/DebugInformation.MeasureHolder.cs`
- `Core/Shared/Helpers/InputHelper.cs`
- `Core/Shared/Helpers/WindowsUtils.cs`
- `Core/Shared/MsBuildLogger.cs`
- `Core/Shared/NextFrameTask.cs`
- `Core/Shared/Nodes/ContentNode.cs`
- `Core/Shared/Nodes/ContentNodeConverter.cs`
- `Core/Shared/Nodes/HotkeyNodeV2.cs`
- `Core/Shared/Nodes/JsonSerializationHelper.cs`
- `Core/Shared/PluginAssemblyLoadContext.cs`
- `Core/Shared/PluginCompiler.cs`
- `Core/Shared/PluginManager.LoadedAssembly.cs`
- `Core/Shared/PluginManager.NotificationId.cs`
- `Core/Shared/PluginManager.PluginMountConfig.cs`
- `Core/Shared/PluginNotification.cs`
- `Core/Shared/SyncAwaiter.cs`
- `Core/Shared/SyncTaskMethodBuilder.cs`
- `Core/Shared/TaskUtils.cs`
- `Core/Shared/WaitFunctionTimed.cs`
- `Core/SnapshotBuilder.cs`
- `Core/SnapshotMemoryBackend.cs`
- `Core/SnapshotSettings.cs`
- `Core/SortedListExtensions.cs`
- `Core/StatCollector.cs`

### B. Needs the decrypted dump (207)
Memory-backed types (RemoteMemoryObject / Component / Element / StructuredRemoteMemoryObject).
Their getters read specific process-memory offsets that the protection hides — do **not** guess these.

- `Core/PoEMemory/Components/ActiveAnimationData.cs`
- `Core/PoEMemory/Components/AnimatedRender.cs`
- `Core/PoEMemory/Components/AnimationController.cs`
- `Core/PoEMemory/Components/AnimationStage.cs`
- `Core/PoEMemory/Components/AnimationStageList.cs`
- `Core/PoEMemory/Components/Buffs.cs`
- `Core/PoEMemory/Components/CapturedMonster.cs`
- `Core/PoEMemory/Components/EffectPack.cs`
- `Core/PoEMemory/Components/ExpeditionSaga.cs`
- `Core/PoEMemory/Components/HarvestInfrastructure.cs`
- `Core/PoEMemory/Components/HarvestSeedSpawnDescriptor.cs`
- `Core/PoEMemory/Components/HeistBlueprint.cs`
- `Core/PoEMemory/Components/HeistContract.cs`
- `Core/PoEMemory/Components/HeistEquipment.cs`
- `Core/PoEMemory/Components/ItemInfoData.cs`
- `Core/PoEMemory/Components/LocalStats.cs`
- `Core/PoEMemory/Components/MapKey.cs`
- `Core/PoEMemory/Components/Movement.cs`
- `Core/PoEMemory/Components/ParticleEffects.cs`
- `Core/PoEMemory/Components/Shield.cs`
- `Core/PoEMemory/Components/SoundEvents.cs`
- `Core/PoEMemory/Components/SupportedAnimationList.cs`
- `Core/PoEMemory/Components/Usable.cs`
- `Core/PoEMemory/Elements/AltarEntity.cs`
- `Core/PoEMemory/Elements/ArchnemesisAltarElement.cs`
- `Core/PoEMemory/Elements/ArchnemesisPanelElement.cs`
- `Core/PoEMemory/Elements/AsyncItemRightClickPriceMenu.cs`
- `Core/PoEMemory/Elements/AtlasElements/AtlasMasterMissionPanelElement.cs`
- `Core/PoEMemory/Elements/AtlasElements/VoidStoneSlot.cs`
- `Core/PoEMemory/Elements/AtlasPanel.cs`
- `Core/PoEMemory/Elements/AzmeriData.cs`
- `Core/PoEMemory/Elements/AzmeriElement.cs`
- `Core/PoEMemory/Elements/BanditDialog.cs`
- `Core/PoEMemory/Elements/BestiaryTab.cs`
- `Core/PoEMemory/Elements/CapturedBeast.cs`
- `Core/PoEMemory/Elements/CapturedBeastsTab.cs`
- `Core/PoEMemory/Elements/ChallengePanel.cs`
- `Core/PoEMemory/Elements/ChallengePanelTabContainer.cs`
- `Core/PoEMemory/Elements/ChallengePanelTabContainerTabInfo.cs`
- `Core/PoEMemory/Elements/ChatPanel.cs`
- `Core/PoEMemory/Elements/DivineFontPanel.cs`
- `Core/PoEMemory/Elements/DropdownElement.cs`
- `Core/PoEMemory/Elements/DropdownElementOption.cs`
- `Core/PoEMemory/Elements/ExpeditionDetonator.cs`
- `Core/PoEMemory/Elements/ExpeditionDetonatorInfo.cs`
- `Core/PoEMemory/Elements/ExpeditionElements/ArtifactSliderElement.cs`
- `Core/PoEMemory/Elements/ExpeditionElements/ExpeditionVendorCurrencyInfoElement.cs`
- `Core/PoEMemory/Elements/ExpeditionElements/ExpeditionVendorElement.cs`
- `Core/PoEMemory/Elements/ExpeditionElements/TujenHaggleWindowElement.cs`
- `Core/PoEMemory/Elements/HarvestCraftElement.cs`
- `Core/PoEMemory/Elements/HarvestWindow.cs`
- `Core/PoEMemory/Elements/InvitesPanel.cs`
- `Core/PoEMemory/Elements/InvitesPanelItem.cs`
- `Core/PoEMemory/Elements/ItemRightClickPriceMenu.cs`
- `Core/PoEMemory/Elements/KalandraTabletWindow.cs`
- `Core/PoEMemory/Elements/LeagueMechanicButtonsElement.cs`
- `Core/PoEMemory/Elements/MapReceptacleWindow.cs`
- `Core/PoEMemory/Elements/Necropolis/NecropolisCollectableCorpse.cs`
- `Core/PoEMemory/Elements/Necropolis/NecropolisMonsterPanel.cs`
- `Core/PoEMemory/Elements/NpcDialog.cs`
- … and 147 more

## Progress update

**Offsets are also protected.** `GameOffsets.dll` decompiles cleanly (its code isn't
anti-tampered), but every `FieldLayout` offset is obfuscated in metadata (values in the
`0x7F00_0000` band, impossible for the declared struct sizes) and is only patched to the
real value at runtime by ExileCore's protector. So the field offsets — like the method
bodies — exist only inside the live process. de4dot, AntiTamperKiller, dnSpyEx *Save
Module*, and ExtremeDumper were all tried and could not recover them.

**Category-A work done (no offsets needed):**
- `StructuredRemoteMemoryObject<T>` base implemented (per-frame struct cache).
- `DisposableAction`, `Memory.EmptyDisposable`, `Memory.BooleanHolder` implemented.
- 131 mangled auto-properties restored across 29 settings/POCO files (`{ get; set; }`,
  node-typed props initialized with `= new()`).
- 8 records cleaned: synthesized value-semantics members stripped so the compiler
  regenerates them; `DebugMessage` ctor/Deconstruct fixed.

**Still open:**
- Complex category-A (compilers, memory backends, async/task infra, ImGui helpers) — left
  as `// TODO` (their algorithms aren't safely derivable from signatures).
- 207 memory-backed types — blocked on real offsets. The lightest way to get them is an
  **in-process `Marshal.OffsetOf` dump** (a tiny ExileApi plugin run once), which would
  unlock the `Structure.Field` getters.
