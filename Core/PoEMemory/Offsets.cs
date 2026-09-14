using System.Collections.Generic;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Interfaces;

namespace ExileCore.PoEMemory;

/// <summary>
/// Holds the per-client memory offsets and signature patterns used to locate key
/// game structures, and resolves them at runtime via pattern scanning.
/// </summary>
public class Offsets
{
    // Client executable names. GGG dropped the "_x64" suffix at some point, so the historical
    // names alone no longer match a current install: the live client observed on 2026-09-14 was
    // "PathOfExile_KG", which matches none of the "_x64" spellings. Both spellings are kept so
    // that an older client still attaches.
    //
    // The current spellings are not guessed: they are the literals carried by the reference
    // ExileApi-Compiled distribution, which does attach to this install
    // (PathOfExile / PathOfExileSteam / PathOfExileEGS / Pathofexile_KG).
    //
    // NOTE: the Epic client ("PathOfExileEGS") exists but is deliberately NOT mapped here — its
    // IgsOffset/IgsDelta are unknown, and attaching with the wrong delta reads the wrong memory
    // silently. Add it only together with a value that has been verified against that client.

    /// <summary>Offsets for the standalone (non-Steam) client.</summary>
    public static Offsets Regular = new Offsets
    {
        IgsOffset = 0, IgsDelta = 0,
        ExeNames = new[] {"PathOfExile", "PathOfExile_x64"},
    };

    /// <summary>Offsets for the Korean Garena client.</summary>
    public static Offsets Korean = new Offsets
    {
        IgsOffset = 0, IgsDelta = 0,
        ExeNames = new[] {"PathOfExile_KG", "Pathofexile_KG", "Pathofexile_x64_KG"},
    };

    /// <summary>Offsets for the Steam client.</summary>
    public static Offsets Steam = new Offsets
    {
        IgsOffset = 0x28, IgsDelta = 0,
        ExeNames = new[] {"PathOfExileSteam", "PathOfExile_x64Steam"},
    };

    /// <summary>Every client variant the loader knows how to attach to.</summary>
    public static readonly Offsets[] All = {Regular, Korean, Steam};
    /*
    00007FF7006C7891  | 90                                 | nop                                        |
    00007FF7006C7892  | 48 8B 1D EF 93 06 01               | mov rbx,qword ptr ds:[7FF701730C88]        |
    00007FF7006C7899  | 48 89 05 E8 93 06 01               | mov qword ptr ds:[7FF701730C88],rax        |
    00007FF7006C78A0  | 48 85 DB                           | test rbx,rbx                               |
    00007FF7006C78A3  | 74 15                              | je pathofexile_x64.7FF7006C78BA            |
    00007FF7006C78A5  | 48 8B CB                           | mov rcx,rbx                                |
    00007FF7006C78A8  | E8 53 D7 00 00                     | call pathofexile_x64.7FF7006D5000          |
    */
    //    90 48 8B 1D ?? ?? ?? ?? 48 89 05 ?? ?? ?? ?? 48 85 DB 74 15 48 8B CB E8

    private static readonly Pattern basePtrPattern = new Pattern(
        new byte[]
        {
            0x90, 0x48, 0x03, 0xD8, 0x48, 0x8B, 0x03, 0x48, 0x85, 0xC0, 0x75, 0x00, 0x48, 0x8B, 0x1D, 0x00, 0x00, 0x00, 0x00, 0x48,
            0x8B, 0x05, 0x00, 0x00, 0x00, 0x00, 0x48, 0x85, 0xC0, 0X74, 0x00, 0x66, 0x90
        }, "xxxxxxxxxxx?xxx????xxx????xxxx?xx", "BasePtr");

    /* FileRoot Pointer
    00007FF6C47EED01  | 48 8D 0D A8 23 7F 00               | lea rcx,qword ptr ds:[7FF6C4FE10B0]        | <--FileRootPtr
    00007FF6C47EED08  | E8 E3 5C 56 FF                     | call pathofexile_x64.7FF6C3D549F0          |
    00007FF6C47EED0D  | 48 8B 3D A4 23 7F 00               | mov rdi,qword ptr ds:[7FF6C4FE10B8]        |
    00007FF6C47EED14  | 48 8B 1F                           | mov rbx,qword ptr ds:[rdi]                 |
    00007FF6C47EED17  | 48 3B DF                           | cmp rbx,rdi                                |
    00007FF6C47EED1A  | 0F 84 26 01 00 00                  | je pathofexile_x64.7FF6C47EEE46            |
    */
    // 3.3.x
    //    48 8D 0D ?? ?? ?? ?? E8 ?? ?? ?? ?? 48 8B 3D ?? ?? ?? ?? 48 8B 1F

    // ── Сигнатуры для ТЕКУЩЕГО клиента ──────────────────────────────────────────────────────────
    //
    // Прежние сигнатуры были сняты с клиента 2017 года (комментарии рядом с ними упоминали
    // "3.0.3b" и "alpha 2.5") и на клиенте 2026-09-14 не находятся НИ ОДНА: проверено и сканом
    // файла клиента, и прогоном tools/SanityRead против живого процесса. Они остались в истории
    // git — см. коммит, который заменил их на эти.
    //
    // Откуда взяты эти. Из эталонного дистрибутива ExileApi-Compiled, который на этом клиенте
    // работает: его сигнатуры лежат в куче строк #US его сборки, а имена якорей он печатает в свои
    // логи вместе со смещением найденного совпадения. Сопоставление «сигнатура -> имя» сделано не
    // на глаз, а по совпадению чисел: сканируем файл клиента, переводим файловое смещение в RVA по
    // таблице секций PE и сравниваем с тем, что эталон записал в лог за сегодня. Совпало точно:
    //
    //   File Root     RVA 0x21CFE80   (лог эталона: 35454592)
    //   Area change   RVA 0xD59C96    (лог эталона: 13999254)
    //   Game State    RVA 0xF2611     (лог эталона: 992785)
    //
    // Проверка, что сопоставление не случайно: седьмая сигнатура эталона (DiagnosticInfoType) в
    // файле клиента не находится вовсе — и эталон в своём логе пишет для неё ровно Offset:[0].
    //
    // Две правки формата против того, как сигнатура записана у эталона, обе вынужденные:
    //   * убран ВЕДУЩИЙ "??" — эталон сообщает смещение на байт дальше начала совпадения, и без
    //     этого RVA расходился бы с логом ровно на 1;
    //   * убраны ХВОСТОВЫЕ "??" — Memory.FindPatterns сверяет первый и последний байт МИМО маски
    //     (Core/Memory.cs, CompareData), поэтому паттерн, кончающийся на wildcard, не найдётся
    //     никогда. Информации хвостовые wildcard не несут, на позицию якоря не влияют.
    // Каждая из трёх после этих правок даёт в файле клиента РОВНО ОДНО совпадение.
    //
    // startOffset снят (был подсказкой поиска от старого клиента и указывал мимо): скан идёт с нуля.

    /// <summary>Signature for the file-root pointer. Anchor (RIP displacement) at +2.</summary>
    private static readonly Pattern fileRootPattern =
        new Pattern(new byte[]
            {
                0x89, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x8B, 0x00, 0x00, 0x8B, 0x00, 0x00, 0x00, 0x00, 0x8B,
                0x00, 0x00, 0x00, 0x0F, 0x28, 0x00, 0x00, 0x00, 0x00, 0x83, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xC3
            }, "x??????x??x????x???xx????x??????x", "File Root");

    /* Area Change
    00007FF63317CE40 | 48 83 EC 58                    | sub rsp,58                                      |
    00007FF63317CE44 | 4C 8B C1                       | mov r8,rcx                                      |
    00007FF63317CE47 | 41 B9 01 00 00 00              | mov r9d,1                                       |
    00007FF63317CE4D | 48 8B 49 10                    | mov rcx,qword ptr ds:[rcx+10]                   |
    00007FF63317CE51 | 48 89 4C 24 30                 | mov qword ptr ss:[rsp+30],rcx                   |
    00007FF63317CE56 | 48 85 C9                       | test rcx,rcx                                    |
    00007FF63317CE59 | 74 11                          | je pathofexile_x64 - alpha 2.5.7FF63317CE6C     |
    00007FF63317CE5B | 41 8B C1                       | mov eax,r9d                                     |
    00007FF63317CE5E | F0 0F C1 41 54                 | lock xadd dword ptr ds:[rcx+54],eax             |
    00007FF63317CE63 | 8B 05 7B 09 F0 00              | mov eax,dword ptr ds:[<AreaChangeCount>]        |
    00007FF63317CE69 | 89 41 50                       | mov dword ptr ds:[rcx+50],eax                   |
    00007FF63317CE6C | 49 8B 08                       | mov rcx,qword ptr ds:[r8]                       |
    00007FF63317CE6F | 49 8B 40 18                    | mov rax,qword ptr ds:[r8+18]                    |
    */
    // 3.0.3b
    //     48 83 EC 58 4C 8B C1 41 B9 01 00 00 00 48 8B 49 10

    /// <summary>Signature for the area-change counter. Anchor (RIP displacement) at +22.</summary>
    private static readonly Pattern areaChangePattern =
        new Pattern(
            new byte[]
            {
                0x8B, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x8B, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00,
                0x00, 0xE8, 0x00, 0x00, 0x00, 0x00, 0xFF, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x8B
            }, "x??????x?x?????x????x??????x", "Area change");

    /*
    PathOfExile_x64.exe+853E28 - 48 89 05 E9ABC400     - mov [PathOfExile_x64.exe+149EA18],rax { [00000000] }
    PathOfExile_x64.exe+853E2F - 48 8B 44 24 40        - mov rax,[rsp+40]
    PathOfExile_x64.exe+853E34 - 48 89 06              - mov [rsi],rax
    PathOfExile_x64.exe+853E37 - 48 8B C6              - mov rax,rsi
    PathOfExile_x64.exe+853E3A - 48 83 C4 20           - add rsp,20 { 32 }
    PathOfExile_x64.exe+853E3E - 5E                    - pop rsi
    PathOfExile_x64.exe+853E3F - C3                    - ret 
    */

    private static readonly Pattern isLoadingScreenPattern =
        new Pattern(
            new byte[] {0x48, 0x89, 0x05, 0x00, 0x00, 0x00, 0x00, 0x48, 0x8B, 0x00, 0x00, 0x00, 0x48, 0x89, 0x00, 0x48, 0x8B, 0xC6},
            "xxx????xx???xx?xxx", "Loading");

    /// <summary>Signature for the game-state controller. Anchor (RIP displacement) at +11.</summary>
    private static readonly Pattern GameStatePattern = new Pattern(
        new byte[]
        {
            0x83, 0x00, 0x00, 0x00, 0x8B, 0x00, 0x33, 0x00, 0x00, 0x39, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x0F, 0x85, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xE8
        }, "x???x?x??x?????xx?????????x", "Game State");

    /*
    PathOfExile_x64.exe+118FD9 - 4C 8B 35 48255B01     - mov r14,[PathOfExile_x64.exe+16CB528] { [C6151734A0] }<<here
    PathOfExile_x64.exe+118FE0 - 4D 85 F6              - test r14,r14
    PathOfExile_x64.exe+118FE3 - 0F94 C0               - sete al
    PathOfExile_x64.exe+118FE6 - 84 C0                 - test al,al
    */
    /// <summary>Gets the resolved offset of the game's area-change counter.</summary>
    public long AreaChangeCount { get; private set; }

    /// <summary>Gets the resolved base offset.</summary>
    public long Base { get; private set; }

    /// <summary>Gets the executable name this offset set targets.</summary>
    /// <summary>
    /// Process names this variant may appear under, newest spelling first. Kept as a LIST because
    /// the executable has been renamed across client versions and both spellings are in the wild.
    /// </summary>
    public string[] ExeNames { get; private set; } = System.Array.Empty<string>();

    /// <summary>First known process name. Kept for callers that expect a single name.</summary>
    public string ExeName => ExeNames.Length > 0 ? ExeNames[0] : "";

    /// <summary>Gets the resolved offset of the game's file-root pointer.</summary>
    public long FileRoot { get; private set; }

    /// <summary>Gets the client-specific delta applied to <see cref="IgsOffset"/>.</summary>
    public int IgsDelta { get; private set; }

    /// <summary>Gets the in-game-state base offset.</summary>
    public int IgsOffset { get; private set; }

    /// <summary>Gets the effective in-game-state offset (<see cref="IgsOffset"/> plus <see cref="IgsDelta"/>).</summary>
    public int IgsOffsetDelta => IgsOffset + IgsDelta;

    /// <summary>Gets the resolved offset of the loading-screen flag.</summary>
    public long isLoadingScreenOffset { get; private set; }

    /// <summary>Gets the resolved offset of the game-state pointer.</summary>
    public long GameStateOffset { get; private set; }

    /// <summary>
    /// Scans the game process for the configured signature patterns and resolves the
    /// dependent runtime offsets.
    /// </summary>
    /// <param name="m">The memory reader used to scan and read the game process.</param>
    /// <returns>A map of resolved offsets keyed by <see cref="OffsetsName"/>.</returns>
    public Dictionary<OffsetsName, long> DoPatternScans(IMemory m)
    {
        var array = m.FindPatterns( /*basePtrPattern,*/
            fileRootPattern, areaChangePattern, /* isLoadingScreenPattern,*/ GameStatePattern);

        var result = new Dictionary<OffsetsName, long>();

        //  System.Console.WriteLine("Base Pattern: " + (m.AddressOfProcess + array[0]).ToString("x8"));

        var index = 0;
        var baseAddress = m.Process.MainModule.BaseAddress.ToInt64();

        //  Base = m.Read<int>(baseAddress + array[index] + 0xF) + array[index] + 0x13; index++;
        //  System.Console.WriteLine("Base Address: " + (Base + m.AddressOfProcess).ToString("x8"));

        //  long InGameState = m.Read<long>(Base + BaseAddress, 0x8, 0xF8, 0x38);
        //  System.Console.WriteLine("InGameState: " + InGameState.ToString("x8"));

        // Арифметика якоря: смещение RIP-относительного disp32 внутри сигнатуры, затем конец
        // инструкции (disp + 4). Позиции взяты из того же места, что и сами сигнатуры — из позиции
        // токена '^' в строке эталона, с поправкой на убранный ведущий "??".
        FileRoot = m.Read<int>(baseAddress + array[index] + 2) + array[index] + 6;
        index++;

        //   System.Console.WriteLine("FileRoot Pointer: " + (FileRoot + m.AddressOfProcess).ToString("x8"));

        AreaChangeCount = m.Read<int>(baseAddress + array[index] + 22) + array[index] + 26;
        index++;

        // System.Console.WriteLine("AreaChangeCount: " + m.ReadInt(AreaChangeCount + m.AddressOfProcess).ToString());

        //    isLoadingScreenOffset = m.Read<int>(baseAddress + array[index] + 0x03) + array[index] + 0x07;
        //index++;
        // System.Console.WriteLine("Is Loading Screen Offset:" + (isLoadingScreenOffset + m.AddressOfProcess).ToString("x8"));

        GameStateOffset = m.Read<int>(baseAddress + array[index] + 11) + array[index] + 15;

        //  System.Console.WriteLine("Game State Offset:" + (GameStateOffset + m.AddressOfProcess).ToString("x8"));

        //  result.Add(OffsetsName.Base,Base);
        result.Add(OffsetsName.FileRoot, FileRoot);
        result.Add(OffsetsName.AreaChangeCount, AreaChangeCount);

        //   result.Add(OffsetsName.IsLoadingScreenOffset,isLoadingScreenOffset);
        result.Add(OffsetsName.GameStateOffset, GameStateOffset);
        return result;
    }
}
