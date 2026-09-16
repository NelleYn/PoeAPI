using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExileCore.PoEMemory.FilesInMemory;
using ExileCore.PoEMemory.FilesInMemory.Atlas;
using ExileCore.PoEMemory.FilesInMemory.Metamorph;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Static;

namespace ExileCore.PoEMemory;

/// <summary>
/// Owns the game's in-memory data tables (".dat" files), exposing each as a lazily
/// loaded, strongly typed wrapper and tracking which files were loaded per area.
/// </summary>
public class FilesContainer
{
    private readonly IMemory _memory;
    private BaseItemTypes _baseItemTypes;
    private UniversalFileWrapper<BetrayalChoiceAction> _betrayalChoiceActions;
    private UniversalFileWrapper<BetrayalChoice> _betrayalChoises;
    private UniversalFileWrapper<BetrayalDialogue> _betrayalDialogue;
    private UniversalFileWrapper<BetrayalJob> _betrayalJobs;
    private UniversalFileWrapper<BetrayalRank> _betrayalRanks;
    private UniversalFileWrapper<BetrayalReward> _betrayalRewards;
    private UniversalFileWrapper<BetrayalTarget> _betrayalTargets;
    private ModsDat _mods;
    private StatsDat _stats;
    private TagsDat _tags;
    private UniversalFileWrapper<AtlasNode> atlasNodes;

    /// <summary>The reader that enumerates the game's file table from memory.</summary>
    public FilesFromMemory FilesFromMemory;
    private LabyrinthTrials labyrinthTrials;
    private MonsterVarieties monsterVarieties;
    private PassiveSkills passiveSkills;
    private PropheciesDat prophecies;
    private Quests quests;
    private QuestStates questStates;
    
    //Will be loaded on first access:
    private WorldAreas worldAreas;

    /// <summary>Initializes the container and eagerly loads the file table from the game's memory.</summary>
    /// <param name="memory">The memory reader used to access the game process.</param>
    public FilesContainer(IMemory memory)
    {
        _memory = memory;
        ItemClasses = new ItemClasses();
        FilesFromMemory = new FilesFromMemory(_memory);

        using (new PerformanceTimer("Load files from memory"))
        {
            AllFiles = FilesFromMemory.GetAllFiles();
        }
    }

    /// <summary>Gets the parsed item-class definitions.</summary>
    public ItemClasses ItemClasses { get; }
    public BaseItemTypes BaseItemTypes =>
        _baseItemTypes ?? (_baseItemTypes = new BaseItemTypes(_memory, () => FindFile("Data/BaseItemTypes.dat")));
    public ModsDat Mods => _mods ?? (_mods = new ModsDat(_memory, () => FindFile("Data/Mods.dat"), Stats, Tags));
    public StatsDat Stats => _stats ?? (_stats = new StatsDat(_memory, () => FindFile("Data/Stats.dat")));
    public TagsDat Tags => _tags ?? (_tags = new TagsDat(_memory, () => FindFile("Data/Tags.dat")));
    public WorldAreas WorldAreas => worldAreas ?? (worldAreas = new WorldAreas(_memory, () => FindFile("Data/WorldAreas.dat")));
    public PassiveSkills PassiveSkills =>
        passiveSkills ?? (passiveSkills = new PassiveSkills(_memory, () => FindFile("Data/PassiveSkills.dat")));
    public LabyrinthTrials LabyrinthTrials =>
        labyrinthTrials ?? (labyrinthTrials = new LabyrinthTrials(_memory, () => FindFile("Data/LabyrinthTrials.dat")));
    public Quests Quests => quests ?? (quests = new Quests(_memory, () => FindFile("Data/Quest.dat")));
    public QuestStates QuestStates => questStates ?? (questStates = new QuestStates(_memory, () => FindFile("Data/QuestStates.dat")));
    public MonsterVarieties MonsterVarieties =>
        monsterVarieties ?? (monsterVarieties = new MonsterVarieties(_memory, () => FindFile("Data/MonsterVarieties.dat")));
    public PropheciesDat Prophecies => prophecies ?? (prophecies = new PropheciesDat(_memory, () => FindFile("Data/Prophecies.dat")));
    public UniversalFileWrapper<AtlasNode> AtlasNodes =>
        atlasNodes ?? (atlasNodes = new AtlasNodes(_memory, () => FindFile("Data/AtlasNode.dat")));
    public UniversalFileWrapper<BetrayalTarget> BetrayalTargets =>
        _betrayalTargets ?? (_betrayalTargets =
            new UniversalFileWrapper<BetrayalTarget>(_memory, () => FindFile("Data/BetrayalTargets.dat")));
    public UniversalFileWrapper<BetrayalJob> BetrayalJobs =>
        _betrayalJobs ?? (_betrayalJobs = new UniversalFileWrapper<BetrayalJob>(_memory, () => FindFile("Data/BetrayalJobs.dat")));
    public UniversalFileWrapper<BetrayalRank> BetrayalRanks =>
        _betrayalRanks ?? (_betrayalRanks = new UniversalFileWrapper<BetrayalRank>(_memory, () => FindFile("Data/BetrayalRanks.dat")));
    public UniversalFileWrapper<BetrayalReward> BetrayalRewards =>
        _betrayalRewards ?? (_betrayalRewards =
            new UniversalFileWrapper<BetrayalReward>(_memory, () => FindFile("Data/BetrayalTraitorRewards.dat")));
    public UniversalFileWrapper<BetrayalChoice> BetrayalChoises =>
        _betrayalChoises ?? (_betrayalChoises =
            new UniversalFileWrapper<BetrayalChoice>(_memory, () => FindFile("Data/BetrayalChoices.dat")));
    public UniversalFileWrapper<BetrayalChoiceAction> BetrayalChoiceActions =>
        _betrayalChoiceActions ?? (_betrayalChoiceActions =
            new UniversalFileWrapper<BetrayalChoiceAction>(_memory, () => FindFile("Data/BetrayalChoiceActions.dat")));
    public UniversalFileWrapper<BetrayalDialogue> BetrayalDialogue =>
        _betrayalDialogue ?? (_betrayalDialogue =
            new UniversalFileWrapper<BetrayalDialogue>(_memory, () => FindFile("Data/BetrayalDialogue.dat")));

    #region Metamorph

    private UniversalFileWrapper<MetamorphMetaSkill> _metamorphMetaSkills;
    private UniversalFileWrapper<MetamorphMetaSkillType> _metamorphMetaSkillTypes;
    private UniversalFileWrapper<MetamorphMetaMonster> _metamorphMetaMonsters;
    private UniversalFileWrapper<MetamorphRewardType> _metamorphRewardTypes;
    private UniversalFileWrapper<MetamorphRewardTypeItemsClient> _metamorphRewardTypeItemsClient;

    public UniversalFileWrapper<MetamorphMetaSkill> MetamorphMetaSkills =>
        _metamorphMetaSkills ?? (_metamorphMetaSkills =
            new UniversalFileWrapper<MetamorphMetaSkill>(_memory, () => FindFile("Data/MetamorphosisMetaSkills.dat")));

    public UniversalFileWrapper<MetamorphMetaSkillType> MetamorphMetaSkillTypes =>
        _metamorphMetaSkillTypes ?? (_metamorphMetaSkillTypes =
            new UniversalFileWrapper<MetamorphMetaSkillType>(_memory, () => FindFile("Data/MetamorphosisMetaSkillTypes.dat")));

    public UniversalFileWrapper<MetamorphMetaMonster> MetamorphMetaMonsters =>
        _metamorphMetaMonsters ?? (_metamorphMetaMonsters =
            new UniversalFileWrapper<MetamorphMetaMonster>(_memory, () => FindFile("Data/MetamorphosisMetaMonsters.dat")));

    public UniversalFileWrapper<MetamorphRewardType> MetamorphRewardTypes =>
        _metamorphRewardTypes ?? (_metamorphRewardTypes =
            new UniversalFileWrapper<MetamorphRewardType>(_memory, () => FindFile("Data/MetamorphosisRewardTypes.dat")));

    public UniversalFileWrapper<MetamorphRewardTypeItemsClient> MetamorphRewardTypeItemsClient =>
        _metamorphRewardTypeItemsClient ?? (_metamorphRewardTypeItemsClient =
            new UniversalFileWrapper<MetamorphRewardTypeItemsClient>(_memory, () => FindFile("Data/MetamorphosisRewardTypeItemsClient.dat")));

    #endregion

    #region NewAtlas

    private AtlasRegions _atlasRegions;
    public AtlasRegions AtlasRegions =>
        _atlasRegions ?? (_atlasRegions = new AtlasRegions(_memory, () => FindFile("Data/AtlasRegions.dat")));

    #endregion

    /// <summary>Gets every file discovered in the game's file table, keyed by path.</summary>
    public Dictionary<string, FileInformation> AllFiles { get; private set; }

    /// <summary>Gets the files classified as metadata files, keyed by path.</summary>
    public Dictionary<string, FileInformation> Metadata { get; } = new Dictionary<string, FileInformation>();

    /// <summary>Gets the data table (".dat") files, keyed by path.</summary>
    public Dictionary<string, FileInformation> Data { get; private set; } = new Dictionary<string, FileInformation>();

    /// <summary>Gets all files that did not match the metadata or data categories, keyed by path.</summary>
    public Dictionary<string, FileInformation> OtherFiles { get; } = new Dictionary<string, FileInformation>();

    /// <summary>Gets the files loaded during the current game area, keyed by path.</summary>
    public Dictionary<string, FileInformation> LoadedInThisArea { get; private set; } = new Dictionary<string, FileInformation>(1024);

    /// <summary>Gets or sets files grouped by their <see cref="FileInformation.Test2"/> value.</summary>
    public Dictionary<int, List<KeyValuePair<string, FileInformation>>> GroupedByTest2 { get; set; }

    /// <summary>Gets or sets files grouped by their <see cref="FileInformation.ChangeCount"/> value.</summary>
    public Dictionary<int, List<KeyValuePair<string, FileInformation>>> GroupedByChangeAction { get; set; }

    /// <summary>Reloads the full file table synchronously from the game's memory.</summary>
    public void LoadFiles()
    {
        AllFiles = FilesFromMemory.GetAllFilesSync();
    }

    /// <summary>Raised after the files loaded for the current area have been parsed.</summary>
    public event EventHandler<Dictionary<string, FileInformation>> LoadedFiles;

    /// <summary>Classifies the supplied files into metadata, data and other categories.</summary>
    /// <param name="files">The files to classify.</param>
    public void ParseFiles(Dictionary<string, FileInformation> files)
    {
        foreach (var file in files)
        {
            if (file.Key[0] == 'M' && file.Key[8] == '/')
                Metadata[file.Key] = file.Value;
            else if (file.Key[0] == 'D' && file.Key[4] == '/' && file.Key.EndsWith(".dat"))
                Data[file.Key] = file.Value;
            else
                OtherFiles[file.Key] = file.Value;
        }

        Data = Data.OrderBy(x => x.Key).ToDictionary(pair => pair.Key, pair => pair.Value);
    }

    /// <summary>
    /// Classifies all files and records those loaded for the given area-change count,
    /// then raises <see cref="LoadedFiles"/> with the per-area subset.
    /// </summary>
    /// <param name="gameAreaChangeCount">The area-change count identifying the current area.</param>
    public void ParseFiles(int gameAreaChangeCount)
    {
        if (AllFiles != null)
        {
            LoadedInThisArea = new Dictionary<string, FileInformation>(1024);

            foreach (var file in AllFiles)
            {
                if (file.Value.ChangeCount == gameAreaChangeCount) LoadedInThisArea[file.Key] = file.Value;

                if (file.Key[0] == 'M' && file.Key[8] == '/')
                    Metadata[file.Key] = file.Value;
                else if (file.Key[0] == 'D' && file.Key[4] == '/' && file.Key.EndsWith(".dat"))
                    Data[file.Key] = file.Value;
                else
                    OtherFiles[file.Key] = file.Value;
            }

            LoadedFiles?.Invoke(this, LoadedInThisArea);
        }
    }

    /// <summary>Names already reported as missing, so that a lookup that keeps failing is logged once.</summary>
    private readonly HashSet<string> _reportedMissingFiles = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Looks up the pointer to the named file.</summary>
    /// <param name="name">The file path to resolve (e.g. "Data/Mods.dat").</param>
    /// <returns>
    /// The pointer to the file's information record, or 0 when the file is not in the table.
    /// </returns>
    /// <remarks>
    /// <para>
    /// WHY 0 IS SAFE HERE. The return type is <see cref="long"/>, so there is no null to survive:
    /// 0 is already this codebase's "file unavailable" value. Every caller is inside this class and
    /// passes <c>FindFile</c> as the <c>Func&lt;long&gt;</c> of a <see cref="FileInMemory"/>, and
    /// <see cref="FileInMemory.RecordAddresses"/> tests that address for 0 before walking anything,
    /// yielding an empty table instead of dereferencing. So a missing file costs the consumer an
    /// empty table, which is what it already got for a file whose record count read as 0.
    /// </para>
    /// <para>
    /// WHAT WAS REMOVED AND WHY. This used to raise a MessageBox and call Environment.Exit(1).
    /// Diagnostics must not kill the caller — the same rule PerformanceTimer.StopAndPrint broke
    /// when it took down TheGame's constructor (see README). Killing the host also loses the very
    /// information that would explain the failure. The handler was dead code besides: it was a
    /// <c>catch (KeyNotFoundException)</c> around
    /// <see cref="Dictionary{TKey,TValue}.TryGetValue"/>, which returns false on a missing key and
    /// does not throw, so the only way into it was an exception TryGetValue never raises. The real
    /// unhandled failure was <see cref="AllFiles"/> being null, which threw past this method
    /// entirely; that case is now handled explicitly.
    /// </para>
    /// <para>
    /// The report is deduplicated per name because this is reached from lazy property getters that
    /// re-evaluate; a log line per call would be its own kind of damage.
    /// </para>
    /// </remarks>
    public long FindFile(string name)
    {
        if (name == null)
        {
            DebugWindow.LogError($"{nameof(FilesContainer)}.{nameof(FindFile)}: called with a null file name.");
            return 0;
        }

        var allFiles = AllFiles;

        if (allFiles == null)
        {
            ReportMissingFile($"{nameof(FilesContainer)}.{nameof(FindFile)}: the file table has not been loaded yet, " +
                              $"cannot resolve \"{name}\". Returning 0 (file unavailable).", name);
            return 0;
        }

        if (allFiles.TryGetValue(name, out var result))
            return result.Ptr;

        ReportMissingFile($"{nameof(FilesContainer)}.{nameof(FindFile)}: \"{name}\" is not in the game's file table " +
                          $"({allFiles.Count} files loaded). Returning 0 (file unavailable); restarting the game " +
                          $"usually repopulates the table.", name);
        return 0;
    }

    /// <summary>Logs the first failure for a given file name and stays silent on the repeats.</summary>
    /// <param name="message">The message to log.</param>
    /// <param name="name">The file name the failure is about.</param>
    private void ReportMissingFile(string message, string name)
    {
        lock (_reportedMissingFiles)
        {
            if (!_reportedMissingFiles.Add(name))
                return;
        }

        DebugWindow.LogError(message);
    }

    #region Bestiary

    private BestiaryCapturableMonsters bestiaryCapturableMonsters;
    public BestiaryCapturableMonsters BestiaryCapturableMonsters =>
        bestiaryCapturableMonsters != null
            ? bestiaryCapturableMonsters
            : bestiaryCapturableMonsters =
                new BestiaryCapturableMonsters(_memory, () => FindFile("Data/BestiaryCapturableMonsters.dat"));
    private UniversalFileWrapper<BestiaryRecipe> bestiaryRecipes;
    public UniversalFileWrapper<BestiaryRecipe> BestiaryRecipes =>
        bestiaryRecipes != null
            ? bestiaryRecipes
            : bestiaryRecipes = new UniversalFileWrapper<BestiaryRecipe>(_memory, () => FindFile("Data/BestiaryRecipes.dat"));
    private UniversalFileWrapper<BestiaryRecipeComponent> bestiaryRecipeComponents;
    public UniversalFileWrapper<BestiaryRecipeComponent> BestiaryRecipeComponents =>
        bestiaryRecipeComponents != null
            ? bestiaryRecipeComponents
            : bestiaryRecipeComponents =
                new UniversalFileWrapper<BestiaryRecipeComponent>(_memory, () => FindFile("Data/BestiaryRecipeComponent.dat"));
    private UniversalFileWrapper<BestiaryGroup> bestiaryGroups;
    public UniversalFileWrapper<BestiaryGroup> BestiaryGroups =>
        bestiaryGroups != null
            ? bestiaryGroups
            : bestiaryGroups = new UniversalFileWrapper<BestiaryGroup>(_memory, () => FindFile("Data/BestiaryGroups.dat"));
    private UniversalFileWrapper<BestiaryFamily> bestiaryFamilies;
    public UniversalFileWrapper<BestiaryFamily> BestiaryFamilies =>
        bestiaryFamilies != null
            ? bestiaryFamilies
            : bestiaryFamilies = new UniversalFileWrapper<BestiaryFamily>(_memory, () => FindFile("Data/BestiaryFamilies.dat"));
    private UniversalFileWrapper<BestiaryGenus> bestiaryGenuses;
    public UniversalFileWrapper<BestiaryGenus> BestiaryGenuses =>
        bestiaryGenuses != null
            ? bestiaryGenuses
            : bestiaryGenuses = new UniversalFileWrapper<BestiaryGenus>(_memory, () => FindFile("Data/BestiaryGenus.dat"));

    #endregion
}
