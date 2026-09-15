using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using ExileCore;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Interfaces;

// Both namespaces declare a type named Offsets (ExileCore.PoEMemory.Offsets — the client
// signatures; GameOffsets.Offsets — the structure offset table). We need the first one; without
// the alias this is CS0104. Same alias, same reason, as in tools/SanityRead.
using Offsets = ExileCore.PoEMemory.Offsets;

namespace ExileApi.Tools.FindOffset;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// FindOffset — "at which offset inside MY object does the TRUE address live?"
//
// Why this tool exists. tools/SanityRead proved that the pattern signatures and the game-state
// array of this fork do work against the installed client, and that the chain then breaks on the
// first STRUCTURAL offset: IngameStateOffsets.Data (0x370) reads as zero. SanityRead cannot go
// further by construction — it prints a fixed table, takes no arguments and never shows raw memory.
//
// The recovery method is mechanical:
//   1. the reference ExileApi-Compiled distribution reads the SAME process correctly and exposes
//      the true address of the wanted sub-object through a public property;
//   2. our own object's base address is known (--ingame-state resolves it with THIS repository's
//      offsets, so the base is the fork's own, not a borrowed one);
//   3. the offset is the difference — and --find prints every place in the window where the true
//      value actually lies.
// When the true address is not known yet, --window and --chain let the window be read by eye:
// a field is usually recognised by WHAT it looks like (module pointer, heap object with a vtable,
// small count, float, string), not by its digits.
//
// Hard limits, all of them explicit and all of them printed when they fire:
//   * READ ONLY. PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, nothing else. No write, no input,
//     no injection.
//   * EVERY read and every walk is bounded: MaxWindowBytes for the window, MaxChainTargets for the
//     pointers followed, MaxChainQwords per target, MaxProbes for the "what is behind this pointer"
//     probes, plus a watchdog on wall time and working set. The bound in this project is not
//     theory: an unbounded walk over a stale pointer previously grew two tool processes to ~4 GiB
//     each (see the comment on GameStateContoller.MaxHashMapNodes).
//   * NEVER CRASH. Game not running, garbage address, unreadable page — all expected outcomes,
//     each one printed as a line.
//   * Core/, GameOffsets/, Loader/ and the .sln are untouched.
//   * Numbers are printed as they are. Nothing is "fixed" or nudged.
//
// Exit codes:
//   0 — something was found / printed;
//   1 — nothing found (a --find with no match; a mode that could read but produced no answer);
//   2 — nothing to read: the game is not running, the process is unreachable, or the arguments
//       do not describe a readable window.
// ─────────────────────────────────────────────────────────────────────────────────────────────────
internal static class Program
{
    private const int ExitFound = 0;
    private const int ExitNotFound = 1;
    private const int ExitNothingToRead = 2;

    // x64 user-mode address plane: below 0x10000 is the null page, above 0x7FFF'FFFFFFFF is not a
    // user address. Same bounds as SanityRead uses.
    private const long MinUserPointer = 0x10000L;
    private const long MaxUserPointer = 0x7FFFFFFFFFFFL;

    // ── Limits ───────────────────────────────────────────────────────────────────────────────────
    // Every one of these is announced when it fires; none of them truncates silently.

    /// <summary>Largest window that may be read at once. 64 KiB = 8192 qwords of dump.</summary>
    private const int MaxWindowBytes = 64 * 1024;

    /// <summary>Page size used to split the window read, so one dead page does not kill the rest.</summary>
    private const int PageBytes = 0x1000;

    /// <summary>Pointers followed by --chain.</summary>
    private const int MaxChainTargets = 256;

    /// <summary>Qwords printed behind each followed pointer.</summary>
    private const int MaxChainQwords = 6;

    /// <summary>
    /// Bytes examined inside each candidate by "--at any". A back-pointer to the owner sits in the
    /// head of the object; scanning further turns a targeted check into a memory sweep, and the
    /// price of an unbounded sweep in this project is on record (see MaxHashMapNodes).
    /// </summary>
    private const int MaxBackrefScanBytes = 0x400;

    /// <summary>"What is behind this pointer" probes issued while describing a window.</summary>
    private const int MaxProbes = 4096;

    /// <summary>Bytes read by one string probe.</summary>
    private const int StringProbeBytes = 96;

    /// <summary>Loaded modules enumerated for the "pointer into a module" verdict.</summary>
    private const int MaxModules = 1024;

    /// <summary>
    /// Matches printed by --find. The COUNT is always printed in full; only the table is cut, and
    /// the cut is announced. A 64 KiB window searched for a common value (zero, a small count)
    /// produces tens of thousands of byte-granular hits, and a table that long buries the answer
    /// instead of being it.
    /// </summary>
    private const int MaxPrintedMatches = 256;

    /// <summary>Cached VirtualQueryEx regions. A bound, because the cache grows per distinct region.</summary>
    private const int MaxRegionCache = 4096;

    private const int WatchdogSeconds = 120;
    private const long WatchdogMemoryBytes = 1024L * 1024 * 1024;

    // The substring that identifies the PoE client when none of the three ExeName strings baked
    // into this fork matched. Verbatim from tools/SanityRead, and for the same reason: a name
    // mismatch must be reported, not turned into "game not found" while the game is running.
    private const string ExeNameFragment = "pathofexile";

    private static int _probesUsed;
    private static bool _probeLimitAnnounced;
    private static bool _regionCacheLimitAnnounced;

    private static IntPtr _handle = IntPtr.Zero;
    private static ModuleRange[] _modules = Array.Empty<ModuleRange>();
    private static readonly List<RegionInfo> _regionCache = new List<RegionInfo>();

    private enum Mode
    {
        None,
        Find,
        Window,
        Chain,
        Backref,
        IngameStateOnly
    }

    private sealed class ModuleRange
    {
        public string Name;
        public long Base;
        public long Size;
        public bool IsGame;
        public long End => Base + Size;
    }

    private sealed class RegionInfo
    {
        public long Base;
        public long End;
        public bool Committed;
        public bool Readable;
        public string ProtectText;
    }

    private static int Main(string[] args)
    {
        // The output is read both by eye and through "> file"; without this .NET writes in the
        // console code page (cp866 on this machine) and the redirected dump becomes unreadable.
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch
        {
            // There may be no console at all (redirected stdout under a service) — not a reason to fail.
        }

        try
        {
            return Run(args);
        }
        catch (Exception e)
        {
            // Last line of defence: anything uncaught must become a line, not a stack trace.
            Console.WriteLine();
            Console.WriteLine($"СБОЙ: непредвиденное исключение {e.GetType().Name}: {Oneline(e.Message)}");
            return ExitNothingToRead;
        }
        finally
        {
            if (_handle != IntPtr.Zero)
            {
                try
                {
                    CloseHandle(_handle);
                }
                catch
                {
                    // Closing a handle at exit is best effort.
                }
            }
        }
    }

    private static int Run(string[] args)
    {
        var mode = Mode.None;
        string inText = null;
        string lenText = null;
        string valueText = null;
        string atText = null;
        var valueSize = 8;
        var useIngameState = false;

        for (var i = 0; i < args.Length; i++)
        {
            var a = args[i];

            switch (a)
            {
                case "--help":
                case "-h":
                case "/?":
                    Usage();
                    return ExitNothingToRead;

                case "--find":
                    mode = Mode.Find;
                    break;

                case "--window":
                    mode = Mode.Window;
                    break;

                case "--chain":
                    mode = Mode.Chain;
                    break;

                case "--backref":
                    mode = Mode.Backref;
                    break;

                case "--at":
                    if (++i >= args.Length)
                    {
                        Console.WriteLine("ОШИБКА: у --at нет значения.");
                        return ExitNothingToRead;
                    }

                    atText = args[i];
                    break;

                case "--ingame-state":
                    // Doubles as a mode of its own ("just tell me the address") and as the source
                    // of --in for the other three modes.
                    useIngameState = true;
                    if (mode == Mode.None) mode = Mode.IngameStateOnly;
                    break;

                case "--in":
                    if (++i >= args.Length)
                    {
                        Console.WriteLine("ОШИБКА: у --in нет значения.");
                        return ExitNothingToRead;
                    }

                    inText = args[i];
                    break;

                case "--len":
                    if (++i >= args.Length)
                    {
                        Console.WriteLine("ОШИБКА: у --len нет значения.");
                        return ExitNothingToRead;
                    }

                    lenText = args[i];
                    break;

                case "--value":
                    if (++i >= args.Length)
                    {
                        Console.WriteLine("ОШИБКА: у --value нет значения.");
                        return ExitNothingToRead;
                    }

                    valueText = args[i];
                    break;

                case "--size":
                    if (++i >= args.Length)
                    {
                        Console.WriteLine("ОШИБКА: у --size нет значения.");
                        return ExitNothingToRead;
                    }

                    if (args[i] != "4" && args[i] != "8")
                    {
                        Console.WriteLine($"ОШИБКА: --size {args[i]} — допустимо только 4 или 8.");
                        return ExitNothingToRead;
                    }

                    valueSize = int.Parse(args[i], CultureInfo.InvariantCulture);
                    break;

                default:
                    Console.WriteLine($"ОШИБКА: неизвестный аргумент \"{a}\".");
                    Console.WriteLine();
                    Usage();
                    return ExitNothingToRead;
            }
        }

        if (mode == Mode.None)
        {
            Usage();
            return ExitNothingToRead;
        }

        // Printed before the arguments are interpreted, so that a limit message (a clamped --len,
        // say) lands UNDER the banner and not above it in a redirected log.
        Console.WriteLine("FindOffset — поиск смещения структуры в живой памяти клиента. ТОЛЬКО ЧТЕНИЕ.");
        Console.WriteLine("Числа печатаются как есть. Каждый сработавший предел печатается строкой.");

        // "--in ingame-state" is accepted as a synonym of the --ingame-state flag: both spellings
        // turn up in notes, and failing on one of them would cost a run.
        if (inText != null &&
            (inText.Equals("ingame-state", StringComparison.OrdinalIgnoreCase) ||
             inText.Equals("@ingame-state", StringComparison.OrdinalIgnoreCase)))
        {
            useIngameState = true;
            inText = null;
        }

        if (mode != Mode.IngameStateOnly && inText == null && !useIngameState)
        {
            Console.WriteLine("ОШИБКА: не задано начало окна — нужен --in <адрес> или --ingame-state.");
            Console.WriteLine();
            Usage();
            return ExitNothingToRead;
        }

        long start = 0;

        if (inText != null && !TryParseHex(inText, out start, out var inError))
        {
            Console.WriteLine($"ОШИБКА: --in {inText} — {inError}");
            return ExitNothingToRead;
        }

        long value = 0;

        // The window base as a --value. Addresses change on every game restart, so a mode whose
        // whole point is repeating the measurement after a restart must not require the address to
        // be retyped: "self" keeps the command line valid across runs.
        var valueIsSelf = valueText != null && valueText.Equals("self", StringComparison.OrdinalIgnoreCase);

        if (mode == Mode.Find || mode == Mode.Backref)
        {
            if (valueText == null)
            {
                Console.WriteLine($"ОШИБКА: режим {(mode == Mode.Find ? "--find" : "--backref")} требует --value <значение> (для --backref годится и \"self\").");
                return ExitNothingToRead;
            }

            if (!valueIsSelf && !TryParseHex(valueText, out value, out var valError))
            {
                Console.WriteLine($"ОШИБКА: --value {valueText} — {valError}");
                return ExitNothingToRead;
            }

            if (!valueIsSelf && valueSize == 4 && (ulong) value > uint.MaxValue)
            {
                Console.WriteLine($"ОШИБКА: --value {HexU(value)} не помещается в 4 байта, а задан --size 4.");
                return ExitNothingToRead;
            }
        }

        long at = 0;
        var atAny = false;

        if (mode == Mode.Backref)
        {
            if (atText == null)
            {
                Console.WriteLine("ОШИБКА: режим --backref требует --at <смещение обратного указателя>.");
                return ExitNothingToRead;
            }

            // "any" is the honest default when no foreign layout says where the back-pointer sits:
            // the head of each candidate is scanned and every hit is reported WITH its offset, so
            // the answer is measured rather than assumed.
            atAny = atText.Equals("any", StringComparison.OrdinalIgnoreCase);

            if (!atAny && !TryParseHex(atText, out at, out var atError))
            {
                Console.WriteLine($"ОШИБКА: --at {atText} — {atError}");
                return ExitNothingToRead;
            }

            if (!atAny && (at < 0 || at > MaxWindowBytes))
            {
                Console.WriteLine($"ОШИБКА: --at {HexU(at)} вне разумного диапазона 0..{Hex(MaxWindowBytes)}.");
                return ExitNothingToRead;
            }
        }

        var length = 0;

        if (mode != Mode.IngameStateOnly)
        {
            if (lenText == null)
            {
                Console.WriteLine("ОШИБКА: не задан размер окна — нужен --len <байт>.");
                return ExitNothingToRead;
            }

            if (!TryParseLength(lenText, out length, out var lenError))
            {
                Console.WriteLine($"ОШИБКА: --len {lenText} — {lenError}");
                return ExitNothingToRead;
            }
        }

        StartWatchdog();

        // ── The process ──────────────────────────────────────────────────────────────────────────
        Head("ПРОЦЕСС");

        if (!FindGameProcess(out var process, out var offsets, out var exact, out var findError))
        {
            Console.WriteLine($"  игра не найдена: {findError}");
            Console.WriteLine("  читать нечего — запустите Path of Exile и повторите.");
            return ExitNothingToRead;
        }

        long moduleBase;
        long moduleSize;
        string moduleName;

        try
        {
            moduleBase = process.MainModule.BaseAddress.ToInt64();
            moduleSize = process.MainModule.ModuleMemorySize;
            moduleName = process.MainModule.ModuleName;
        }
        catch (Exception e)
        {
            Console.WriteLine($"  главный модуль процесса недоступен: {e.GetType().Name}: {Oneline(e.Message)}");
            Console.WriteLine("  читать нечего — запустите консоль от того же пользователя, что и игру.");
            return ExitNothingToRead;
        }

        Console.WriteLine($"  процесс      {process.ProcessName} (pid {process.Id})" +
                          (exact ? "" : $"  [имя НЕ совпало с Offsets.*.ExeName, подобран по \"{ExeNameFragment}\"]"));
        Console.WriteLine($"  модуль игры  {moduleName}  база {Hex(moduleBase)}  размер {Hex(moduleSize)} ({moduleSize / 1048576.0:F1} МиБ)");

        _handle = OpenReadHandle(process.Id, out var openError);

        if (_handle == IntPtr.Zero)
        {
            Console.WriteLine($"  открыть процесс на чтение не удалось: {openError}");
            Console.WriteLine("  читать нечего — запустите консоль от того же пользователя, что и игру.");
            return ExitNothingToRead;
        }

        CollectModules(process, moduleBase);

        // ── The window start ─────────────────────────────────────────────────────────────────────
        if (useIngameState)
        {
            Head("АДРЕС IngameState (паттерн-скан -> контроллер состояний -> InGameState)");

            if (!ResolveIngameState(process, offsets, moduleBase, moduleSize, out var resolved))
            {
                Console.WriteLine();

                // An explicit --in is a window start the tool ALREADY has. Discarding it because
                // the CONVENIENCE lookup failed would refuse to read a perfectly readable window —
                // and --in is precisely the fallback one reaches for when the pattern scan dies.
                if (inText != null && mode != Mode.IngameStateOnly)
                {
                    Console.WriteLine($"  Цепочка до IngameState не сложилась — но задан явный --in {HexU(start)}, читаем от него.");
                }
                else
                {
                    Console.WriteLine("  Цепочка до IngameState не сложилась — окно брать не от чего.");

                    // The distinction is not cosmetic. Asked ONLY for the address, the tool did
                    // read the process and simply did not find it: that is code 1. Asked for a
                    // window, it now has no window to read at all: that is code 2.
                    return mode == Mode.IngameStateOnly ? ExitNotFound : ExitNothingToRead;
                }
            }
            else if (inText != null)
            {
                // Both an explicit --in and --ingame-state were given. Silently preferring one of
                // them would make the printed offsets meaningless, so say which one wins.
                Console.WriteLine($"  ВНИМАНИЕ: задан и --in {HexU(start)}, и --ingame-state — берём явный --in.");
            }
            else
            {
                start = resolved;
            }
        }

        if (mode == Mode.IngameStateOnly)
            return IsPointer(start) ? ExitFound : ExitNotFound;

        if (!IsPointer(start))
        {
            Head("ОКНО");
            Console.WriteLine($"  начало окна {HexU(start)} не похоже на пользовательский адрес " +
                              $"({Hex(MinUserPointer)}..{Hex(MaxUserPointer)}).");
            Console.WriteLine("  читать нечего.");
            return ExitNothingToRead;
        }

        // ── Read the window ──────────────────────────────────────────────────────────────────────
        Head("ОКНО");
        Console.WriteLine($"  начало {Hex(start)}   длина {length} б ({Hex(length)})   конец {Hex(start + length)}");

        if ((start & 7L) != 0)
            Console.WriteLine("  ВНИМАНИЕ: начало окна не выровнено по 8 — смещения ниже считаются от него как есть.");

        var window = ReadWindow(start, length, out var readable, out var deadPages);

        if (window == null)
        {
            Console.WriteLine("  окно не прочиталось целиком ни одной страницей.");
            return ExitNothingToRead;
        }

        Console.WriteLine($"  прочитано {readable} б из {length} б" +
                          (deadPages.Count == 0 ? " (все страницы читаются)." : $"; НЕ ЧИТАЕТСЯ страниц: {deadPages.Count}."));

        foreach (var dead in deadPages.Take(16))
            Console.WriteLine($"    не читается {Hex(dead.Item1)}..{Hex(dead.Item2)}  ({dead.Item3})");

        if (deadPages.Count > 16)
            Console.WriteLine($"    ПРЕДЕЛ ПЕЧАТИ: показаны первые 16 нечитаемых участков из {deadPages.Count}.");

        if (readable == 0)
        {
            Console.WriteLine("  читать нечего — ни одна страница окна не отдалась.");
            return ExitNothingToRead;
        }

        var pageOk = BuildPageMap(start, length, deadPages);

        switch (mode)
        {
            case Mode.Find:
                return DoFind(start, window, pageOk, length, value, valueSize);

            case Mode.Window:
                return DoWindow(start, window, pageOk, length);

            case Mode.Chain:
                return DoChain(start, window, pageOk, length);

            case Mode.Backref:
                return DoBackref(start, window, pageOk, length, valueIsSelf ? start : value, at, atAny, valueIsSelf);

            default:
                Console.WriteLine("ОШИБКА: режим не выбран.");
                return ExitNothingToRead;
        }
    }

    // ── Mode 1: --find ───────────────────────────────────────────────────────────────────────────
    //
    // The main mode. The true address comes from the reference fork, the object is ours, and the
    // difference is the offset being looked for. EVERY match is printed, not the first one: several
    // matches ARE the answer ("the offset is ambiguous") and hiding them would turn a measurement
    // into a guess. The scan is byte-granular on purpose — an unaligned match is rare but real
    // (packed structures), and rounding it away would be exactly the kind of silent "fixing" this
    // tool must not do; alignment is reported per match instead.
    private static int DoFind(long start, byte[] window, bool[] pageOk, int length, long value, int size)
    {
        Head($"ПОИСК ЗНАЧЕНИЯ {HexU(value)} ({size} б)");

        var needle = new byte[size];

        if (size == 8)
            BitConverter.GetBytes(value).CopyTo(needle, 0);
        else
            BitConverter.GetBytes((uint) value).CopyTo(needle, 0);

        var matches = new List<long>();

        for (var off = 0; off + size <= length; off++)
        {
            if (!PageRangeOk(pageOk, off, size))
                continue;

            var hit = true;

            for (var k = 0; k < size; k++)
            {
                if (window[off + k] != needle[k])
                {
                    hit = false;
                    break;
                }
            }

            if (hit)
                matches.Add(off);
        }

        if (matches.Count == 0)
        {
            Console.WriteLine($"  НЕ НАЙДЕНО: значение {HexU(value)} не встречается в окне {HexU(start)}..{HexU(start + length)}.");
            Console.WriteLine("  Это тоже ответ: либо база объекта не та, либо поле лежит за пределами окна,");
            Console.WriteLine("  либо истинный адрес взят у другого объекта. Увеличьте --len или проверьте --in.");
            return ExitNotFound;
        }

        Console.WriteLine($"  совпадений: {matches.Count}");
        Console.WriteLine();
        Console.WriteLine("  смещение      десятично   выравнивание   абсолютный адрес поля");
        Console.WriteLine("  " + new string('-', 76));

        foreach (var off in matches.Take(MaxPrintedMatches))
        {
            var align = (start + off) % 8 == 0 ? "по 8" : ((start + off) % 4 == 0 ? "по 4" : "НЕ выровнено");

            Console.WriteLine($"  +{Hex(off),-12} {off,9}   {align,-13}  {HexU(start + off)}");
        }

        if (matches.Count > MaxPrintedMatches)
        {
            Console.WriteLine();
            Console.WriteLine($"  ПРЕДЕЛ ПЕЧАТИ: показаны первые {MaxPrintedMatches} совпадений из {matches.Count}.");
            Console.WriteLine("  Столько совпадений значит, что значение слишком обычное для окна такого размера");
            Console.WriteLine("  (ноль, малый счётчик): сузьте --len или ищите значение, которое встречается редко.");
        }

        if (matches.Count > 1)
        {
            Console.WriteLine();
            Console.WriteLine("  ВНИМАНИЕ: совпадений больше одного — смещение НЕОДНОЗНАЧНО. Это и есть результат");
            Console.WriteLine("  измерения, а не помеха: то же значение лежит в нескольких полях (кэш, дубль,");
            Console.WriteLine("  соседний объект). Отличить их можно, повторив запуск на другом запуске игры —");
            Console.WriteLine("  верное смещение совпадёт снова, случайное разойдётся.");
        }

        return ExitFound;
    }

    // ── Mode 2: --window ─────────────────────────────────────────────────────────────────────────
    //
    // A qword dump where the NOTE matters more than the digits: a field is recognised by what it
    // looks like — a pointer into the game module (printed with its RVA), a heap pointer, a small
    // count, a pair of floats, a readable string behind the pointer.
    private static int DoWindow(long start, byte[] window, bool[] pageOk, int length)
    {
        Head("ДАМП ОКНА ПО 8 БАЙТ");
        Console.WriteLine("  смещение   значение             на что похоже");
        Console.WriteLine("  " + new string('-', 96));

        var rows = 0;

        for (var off = 0; off + 8 <= length; off += 8)
        {
            rows++;

            if (!PageRangeOk(pageOk, off, 8))
            {
                Console.WriteLine($"  +{Hex(off),-8}  {"— не читается —",-20} страница окна не отдалась");
                continue;
            }

            var qword = BitConverter.ToInt64(window, off);
            Console.WriteLine($"  +{Hex(off),-8}  0x{qword:X16}   {Describe(qword)}");
        }

        Console.WriteLine();
        Console.WriteLine($"  строк напечатано: {rows}");

        if (_probeLimitAnnounced)
            Console.WriteLine($"  ПРЕДЕЛ: исчерпаны {MaxProbes} проб «что лежит по указателю» — часть пометок короче обычного.");

        return rows > 0 ? ExitFound : ExitNotFound;
    }

    // ── Mode 3: --chain ──────────────────────────────────────────────────────────────────────────
    //
    // For every pointer in the window, say what is BEHIND it: a vtable inside the game module (the
    // same check Core.ReadGameStates uses to reject a stale layout) and the first few qwords. That
    // is how a pointer to a real object is told apart from a number that merely looks like one.
    private static int DoChain(long start, byte[] window, bool[] pageOk, int length)
    {
        Head("ЧТО ЛЕЖИТ ПО КАЖДОМУ УКАЗАТЕЛЮ ОКНА");

        var followed = 0;
        var withVtable = 0;
        var limitHit = false;

        for (var off = 0; off + 8 <= length; off += 8)
        {
            if (!PageRangeOk(pageOk, off, 8))
                continue;

            var qword = BitConverter.ToInt64(window, off);

            if (!IsPointer(qword))
                continue;

            if (followed >= MaxChainTargets)
            {
                limitHit = true;
                break;
            }

            followed++;

            var region = QueryRegion(qword);
            var targetModule = FindModule(qword);

            Console.WriteLine();
            Console.WriteLine($"  +{Hex(off)}  ->  0x{qword:X16}   {ShortPlace(qword, targetModule, region)}");

            if (region == null || !region.Readable)
            {
                Console.WriteLine("      по этому адресу читать нечего (память не отображена или закрыта на чтение)");
                continue;
            }

            var head = ReadBytesSafe(qword, MaxChainQwords * 8, out var headError);

            if (head == null)
            {
                Console.WriteLine($"      чтение не удалось: {headError}");
                continue;
            }

            var vtable = BitConverter.ToInt64(head, 0);
            var vtableModule = FindModule(vtable);

            if (vtableModule != null && vtableModule.IsGame)
            {
                withVtable++;
                Console.WriteLine($"      vtable 0x{vtable:X16} -> {vtableModule.Name}+{Hex(vtable - vtableModule.Base)}   [ЕСТЬ vtable в модуле игры — похоже на объект]");
            }
            else if (vtableModule != null)
            {
                Console.WriteLine($"      первый qword 0x{vtable:X16} -> {vtableModule.Name}+{Hex(vtable - vtableModule.Base)}   [указатель в ЧУЖОЙ модуль, не в модуль игры]");
            }
            else
            {
                Console.WriteLine($"      первый qword 0x{vtable:X16}   [vtable в модуле игры НЕ обнаружена]");
            }

            for (var k = 0; k < MaxChainQwords && (k + 1) * 8 <= head.Length; k++)
            {
                var v = BitConverter.ToInt64(head, k * 8);
                Console.WriteLine($"        [+{Hex(k * 8),-6}] 0x{v:X16}   {Describe(v)}");
            }
        }

        Console.WriteLine();

        if (limitHit)
            Console.WriteLine($"  ПРЕДЕЛ: пройдено {MaxChainTargets} указателей — остальные в окне НЕ разобраны (сузьте --len).");

        Console.WriteLine($"  указателей разобрано: {followed}; из них с vtable в модуле игры: {withVtable}");

        if (_probeLimitAnnounced)
            Console.WriteLine($"  ПРЕДЕЛ: исчерпаны {MaxProbes} проб «что лежит по указателю» — часть пометок короче обычного.");

        return followed > 0 ? ExitFound : ExitNotFound;
    }

    // ── Mode 4: --backref ────────────────────────────────────────────────────────────────────────
    //
    // "Which pointer in this window leads to an object that points BACK at us."
    //
    // Why this is a separate mode and not a variant of --find. --find needs the true address to be
    // known already, and the only way to learn it was to run a second, correct fork against the same
    // process at the same moment. A back-pointer removes that dependency: the candidate proves
    // itself, because the value being matched is the base of OUR window and a random qword cannot
    // equal it by accident. A foreign layout is then used only to say WHERE inside the candidate the
    // back-pointer should sit (--at); the verdict still comes from this client's memory.
    //
    // Bounded exactly like --chain: at most MaxChainTargets pointers followed, one 8-byte read
    // behind each, every limit announced. Read only.
    private static int DoBackref(long start, byte[] window, bool[] pageOk, int length, long value, long at, bool atAny, bool valueIsSelf)
    {
        Head(atAny
            ? $"ОБРАТНЫЙ УКАЗАТЕЛЬ: у какого указателя окна в первых {Hex(MaxBackrefScanBytes)} б лежит {HexU(value)}"
            : $"ОБРАТНЫЙ УКАЗАТЕЛЬ: у какого указателя окна по +{Hex(at)} лежит {HexU(value)}");

        if (valueIsSelf)
            Console.WriteLine($"  --value self: ищем указатель НА НАЧАЛО ОКНА {HexU(value)}.");

        Console.WriteLine();

        var followed = 0;
        var probed = 0;
        var limitHit = false;
        var matches = new List<long[]>();

        for (var off = 0; off + 8 <= length; off += 8)
        {
            if (!PageRangeOk(pageOk, off, 8))
                continue;

            var qword = BitConverter.ToInt64(window, off);

            if (!IsPointer(qword))
                continue;

            if (followed >= MaxChainTargets)
            {
                limitHit = true;
                break;
            }

            followed++;

            var probe = atAny ? qword : qword + at;
            var probeLen = atAny ? MaxBackrefScanBytes : 8;

            if (!IsPointer(probe))
                continue;

            var region = QueryRegion(probe);

            if (region == null || !region.Readable)
                continue;

            var bytes = ReadBytesSafe(probe, probeLen, out _);

            if (bytes == null)
                continue;

            probed++;

            var head = ReadBytesSafe(qword, 8, out _);
            var vtable = head == null ? 0L : BitConverter.ToInt64(head, 0);

            // Byte granular on purpose, same reason as --find: an unaligned hit is rare but real,
            // and rounding it away would be the silent "fixing" this tool must never do.
            for (var k = 0; k + 8 <= bytes.Length; k++)
            {
                if (BitConverter.ToInt64(bytes, k) != value)
                    continue;

                matches.Add(new[] {off, qword, vtable, atAny ? k : at});

                if (!atAny)
                    break;
            }
        }

        if (matches.Count == 0)
        {
            Console.WriteLine($"  НЕ НАЙДЕНО: ни один указатель окна не ведёт к объекту, у которого {(atAny ? $"в первых {Hex(MaxBackrefScanBytes)} б" : $"по +{Hex(at)}")} лежит {HexU(value)}.");
            Console.WriteLine($"  Прочитано {probed} кандидатов из {followed} указателей окна.");
            Console.WriteLine("  Это тоже ответ: либо обратного указателя по этому смещению нет (чужая раскладка");
            Console.WriteLine("  не подошла), либо поле лежит за пределами окна — увеличьте --len или смените --at.");
            return ExitNotFound;
        }

        Console.WriteLine($"  совпадений: {matches.Count}   (проверено {probed} кандидатов из {followed} указателей окна)");
        Console.WriteLine();
        Console.WriteLine("  смещение      адрес объекта        обратный ук. внутри   первый qword объекта");
        Console.WriteLine("  " + new string('-', 92));

        foreach (var m in matches)
        {
            var vtableModule = FindModule(m[2]);
            var vtableText = vtableModule != null && vtableModule.IsGame
                ? $"0x{m[2]:X16} -> {vtableModule.Name}+{Hex(m[2] - vtableModule.Base)}"
                : $"0x{m[2]:X16} [vtable в модуле игры НЕ обнаружена]";

            Console.WriteLine($"  +{Hex(m[0]),-12} 0x{m[1]:X16}   +{Hex(m[3]),-18} {vtableText}");
        }

        Console.WriteLine();

        if (limitHit)
            Console.WriteLine($"  ПРЕДЕЛ: пройдено {MaxChainTargets} указателей — остальные в окне НЕ проверены (сузьте --len).");

        // Several matches are an answer too, and a worse one: it means the back-pointer does not
        // single the field out. Saying so is the difference between a measurement and a guess.
        if (matches.Count > 1)
            Console.WriteLine("  ВНИМАНИЕ: совпадений больше одного — смещение этим признаком НЕ определяется однозначно.");

        if (_probeLimitAnnounced)
            Console.WriteLine($"  ПРЕДЕЛ: исчерпаны {MaxProbes} проб «что лежит по указателю».");

        return ExitFound;
    }

    // ── Convenience: resolving IngameState ───────────────────────────────────────────────────────
    //
    // The address of IngameState changes on every launch of the game, so retyping it between runs
    // is not a nuisance but a source of wrong answers. This walks the SAME chain SanityRead does —
    // pattern scan -> game-state controller -> the InGameState slot — using Memory and the offsets
    // OF THIS REPOSITORY (that is why the project has a ProjectReference rather than DLL refs).
    //
    // It stops one step short of constructing TheGame on purpose. TheGame's constructor also builds
    // FilesContainer(m), which eagerly walks the client's whole file table: that walk has no bound
    // of the kind this tool is required to have, and it is not needed to learn one address. The
    // slot read below is byte-for-byte what Core's TheGame.ReadGameStates does (array at
    // controller+0x48, stride 0x10, the stored address being state+0x10), including its vtable
    // check — so the address printed here is the same one TheGame.IngameState.Address would hold.
    private const int GameStateArrayOffset = 0x48;
    private const int GameStateArrayStride = 0x10;

    private static bool ResolveIngameState(Process process, Offsets offsets, long moduleBase, long moduleSize,
        out long address)
    {
        address = 0;

        Memory memory = null;

        try
        {
            try
            {
                // The constructor opens its own read handle and runs DoPatternScans — the pattern
                // scan of ~30-60 MiB is the slowest thing this tool does, and it happens here.
                memory = new Memory((process, offsets));
            }
            catch (Exception e)
            {
                Console.WriteLine($"  приложиться не удалось: {e.GetType().Name}: {Oneline(e.Message)}");
                return false;
            }

            var baseOffsets = memory.BaseOffsets;

            if (baseOffsets == null || !baseOffsets.TryGetValue(OffsetsName.GameStateOffset, out var gameStateRva))
            {
                Console.WriteLine("  BaseOffsets не содержит GameStateOffset — паттерн-скан не дал якоря.");
                return false;
            }

            // A not-found signature still yields a plausible-looking RVA, because the anchor is
            // COMPUTED from the found offset: with offset 0 it is garbage by origin. SanityRead
            // measured exactly this (FileRoot 0xB813, GameStateOffset 0x21 with all signatures
            // missing). So the raw scan result is checked, not just the range.
            var gameStateFound = RawPatternFound(memory, "Game State");

            if (gameStateFound == false)
            {
                Console.WriteLine("  сигнатура «Game State» НЕ НАЙДЕНА — GameStateOffset посчитан от нуля.");
                Console.WriteLine("  Идти по нему значило бы разбирать случайную память; останавливаемся намеренно.");
                return false;
            }

            if (gameStateFound == null)
                Console.WriteLine("  сырой результат скана получить не удалось — идём по якорю как есть.");

            Console.WriteLine($"  GameStateOffset (RVA)  {Hex(gameStateRva)}   абс {Hex(moduleBase + gameStateRva)}   " +
                              (gameStateRva > 0 && gameStateRva < moduleSize ? "[внутри образа]" : "[ВНЕ образа — мусор]"));

            long controller;

            try
            {
                controller = memory.Read<long>(moduleBase + gameStateRva);
            }
            catch (Exception e)
            {
                Console.WriteLine($"  чтение контроллера состояний не удалось: {e.GetType().Name}: {Oneline(e.Message)}");
                return false;
            }

            Console.WriteLine($"  контроллер состояний   {Hex(controller)}   " +
                              (IsPointer(controller) ? "[похоже на указатель]" : "[НЕ указатель — цепочка обрывается]"));

            if (!IsPointer(controller))
                return false;

            var slot = controller + GameStateArrayOffset + (int) GameStateTypes.InGameState * GameStateArrayStride;

            long entry;

            try
            {
                entry = memory.Read<long>(slot);
            }
            catch (Exception e)
            {
                Console.WriteLine($"  чтение слота InGameState не удалось: {e.GetType().Name}: {Oneline(e.Message)}");
                return false;
            }

            Console.WriteLine($"  слот InGameState       {Hex(slot)} (контроллер+{Hex(GameStateArrayOffset + (int) GameStateTypes.InGameState * GameStateArrayStride)})");
            Console.WriteLine($"  IngameState.Address    {Hex(entry)}   " +
                              (IsPointer(entry) ? "[похоже на указатель]" : "[НЕ указатель]"));

            if (!IsPointer(entry))
                return false;

            // Same guard as Core.ReadGameStates: a real state object starts with a vtable pointing
            // into the game module. Without it a stale layout hands out plausible-looking garbage.
            long vtable;

            try
            {
                vtable = memory.Read<long>(entry - 0x10);
            }
            catch (Exception e)
            {
                Console.WriteLine($"  проверка vtable не удалась: {e.GetType().Name}: {Oneline(e.Message)}");
                vtable = 0;
            }

            var vtableOk = vtable >= moduleBase && vtable < moduleBase + moduleSize;

            Console.WriteLine($"  vtable объекта         {Hex(vtable)}   " +
                              (vtableOk
                                  ? $"[внутри модуля игры, +{Hex(vtable - moduleBase)}]"
                                  : "[ВНЕ модуля игры — раскладка массива состояний не от этого клиента]"));

            if (!vtableOk)
            {
                Console.WriteLine("  Адрес всё равно печатается как есть — «чинить» его инструмент не будет,");
                Console.WriteLine("  но окно от него, скорее всего, к IngameState отношения не имеет.");
            }

            address = entry;
            return true;
        }
        finally
        {
            try
            {
                memory?.Dispose();
            }
            catch
            {
                // Memory.Dispose logs through the Core logger, which does not exist in console mode.
            }
        }
    }

    // REFLECTION, and there is no other way. Whether a signature was found is answered only by the
    // raw output of Memory.FindPatterns, and the signatures live in Offsets as private static
    // readonly Pattern fields: DoPatternScans turns them into derived anchors and throws the raw
    // array away. Fixing this in Core would be a one-line change (internal + InternalsVisibleTo),
    // but Core must not be touched. Verbatim in spirit from tools/SanityRead.ReportRawPatterns.
    // Returns: true — found, false — not found, null — could not tell.
    private static bool? RawPatternFound(Memory memory, string patternName)
    {
        try
        {
            var patterns = typeof(Offsets)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                .Where(f => typeof(IPattern).IsAssignableFrom(f.FieldType))
                .Select(f => f.GetValue(null) as IPattern)
                .Where(p => p != null)
                .ToArray();

            if (patterns.Length == 0)
                return null;

            var hits = memory.FindPatterns(patterns);

            for (var i = 0; i < patterns.Length && i < hits.Length; i++)
            {
                if (string.Equals(patterns[i].Name, patternName, StringComparison.Ordinal))
                    return hits[i] > 0;
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    // ── Describing a qword ───────────────────────────────────────────────────────────────────────
    //
    // The note is the product of this tool. Everything here is a GUESS and worded as one: a number
    // that looks like a float may be two shorts, and a "small number" may be a flag byte.
    private static string Describe(long value)
    {
        if (value == 0)
            return "ноль";

        var notes = new List<string>();

        var module = FindModule(value);

        if (module != null)
        {
            notes.Add(module.IsGame
                ? $"указатель в МОДУЛЬ ИГРЫ: {module.Name}+{Hex(value - module.Base)}"
                : $"указатель в модуль {module.Name}+{Hex(value - module.Base)}");
        }
        else if (IsPointer(value))
        {
            var region = QueryRegion(value);

            if (region == null)
                notes.Add("похоже на указатель, но область памяти не опрашивается");
            else if (!region.Committed)
                notes.Add("похоже на указатель, но память по нему НЕ ОТОБРАЖЕНА");
            else if (!region.Readable)
                notes.Add($"указатель в отображённую память, но она закрыта на чтение ({region.ProtectText})");
            else
                notes.Add($"указатель в кучу ({region.ProtectText})");

            if (region != null && region.Committed && region.Readable)
            {
                var behind = ProbeBehindPointer(value);

                if (behind != null)
                    notes.Add(behind);
            }
        }

        if (notes.Count == 0)
        {
            // Not a pointer at all: the remaining shapes are numbers and packed text.
            if (value > -0x100000L && value < 0x100000L)
                notes.Add($"малое число {value.ToString(CultureInfo.InvariantCulture)}");

            var floats = DescribeFloats(value);

            if (floats != null)
                notes.Add(floats);

            var inline = DescribeInlineAscii(value);

            if (inline != null)
                notes.Add(inline);

            if (notes.Count == 0)
                notes.Add("большое число, ни на что из известного не похоже");
        }

        return string.Join("; ", notes);
    }

    private static string DescribeFloats(long value)
    {
        var lo = BitConverter.Int32BitsToSingle((int) (value & 0xFFFFFFFFL));
        var hi = BitConverter.Int32BitsToSingle((int) ((value >> 32) & 0xFFFFFFFFL));

        var loOk = PlausibleFloat(lo);
        var hiOk = PlausibleFloat(hi);

        if (!loOk && !hiOk)
            return null;

        return "похоже на float: " +
               (loOk ? $"мл={lo.ToString("G6", CultureInfo.InvariantCulture)}" : "мл=—") + ", " +
               (hiOk ? $"ст={hi.ToString("G6", CultureInfo.InvariantCulture)}" : "ст=—");
    }

    // Game coordinates, sizes and timers live roughly in this band. Denormals, NaN and 1e30 are the
    // usual look of "these four bytes are not a float at all".
    private static bool PlausibleFloat(float f)
    {
        if (float.IsNaN(f) || float.IsInfinity(f)) return false;
        if (f == 0f) return false;

        var a = Math.Abs(f);
        return a >= 1e-3f && a <= 1e7f;
    }

    private static string DescribeInlineAscii(long value)
    {
        var bytes = BitConverter.GetBytes(value);
        var sb = new StringBuilder(8);
        var printable = 0;

        foreach (var b in bytes)
        {
            if (b == 0)
                break;

            if (b < 0x20 || b > 0x7E)
                return null;

            printable++;
            sb.Append((char) b);
        }

        return printable >= 4 ? $"текст в самих байтах \"{sb}\"" : null;
    }

    // "What is behind this pointer": a vtable inside the game module, or a readable string. Bounded
    // by MaxProbes, and the exhaustion is announced by the caller rather than silently degrading.
    private static string ProbeBehindPointer(long address)
    {
        if (_probesUsed >= MaxProbes)
        {
            _probeLimitAnnounced = true;
            return null;
        }

        _probesUsed++;

        var bytes = ReadBytesSafe(address, StringProbeBytes, out _);

        if (bytes == null)
            return null;

        var first = BitConverter.ToInt64(bytes, 0);
        var vtableModule = FindModule(first);

        if (vtableModule != null && vtableModule.IsGame)
            return $"по нему vtable -> {vtableModule.Name}+{Hex(first - vtableModule.Base)} (похоже на объект)";

        var ascii = TryAscii(bytes);

        if (ascii != null)
            return $"по нему строка ASCII \"{ascii}\"";

        var utf16 = TryUtf16(bytes);

        if (utf16 != null)
            return $"по нему строка UTF-16 \"{utf16}\"";

        return null;
    }

    private static string TryAscii(byte[] bytes)
    {
        var sb = new StringBuilder();

        foreach (var b in bytes)
        {
            if (b == 0)
                break;

            if (b < 0x20 || b > 0x7E)
                return null;

            sb.Append((char) b);
        }

        return sb.Length >= 4 ? Oneline(sb.ToString()) : null;
    }

    private static string TryUtf16(byte[] bytes)
    {
        var sb = new StringBuilder();

        for (var i = 0; i + 1 < bytes.Length; i += 2)
        {
            var c = (char) (bytes[i] | (bytes[i + 1] << 8));

            if (c == '\0')
                break;

            if (c < 0x20 || c > 0x7E)
                return null;

            sb.Append(c);
        }

        return sb.Length >= 4 ? Oneline(sb.ToString()) : null;
    }

    private static string ShortPlace(long address, ModuleRange module, RegionInfo region)
    {
        if (module != null)
            return module.IsGame
                ? $"МОДУЛЬ ИГРЫ {module.Name}+{Hex(address - module.Base)}"
                : $"модуль {module.Name}+{Hex(address - module.Base)}";

        if (region == null)
            return "область памяти не опрашивается";

        if (!region.Committed)
            return "память НЕ ОТОБРАЖЕНА";

        return region.Readable ? $"куча ({region.ProtectText})" : $"закрыта на чтение ({region.ProtectText})";
    }

    // ── Reading ──────────────────────────────────────────────────────────────────────────────────
    //
    // The window is read page by page on purpose: a single dead page in the middle would otherwise
    // fail the whole read, and "half the object is unreadable" is itself a measurement worth
    // printing. Core's Memory.ReadMem cannot be used for this — it ignores the result of
    // ReadProcessMemory and hands back a zero-filled buffer, so a failed read would be
    // indistinguishable from a region of zeros, which is precisely the distinction this tool sells.
    private static byte[] ReadWindow(long start, int length, out int readableBytes,
        out List<Tuple<long, long, string>> deadPages)
    {
        var buffer = new byte[length];
        deadPages = new List<Tuple<long, long, string>>();
        readableBytes = 0;

        var addr = start;
        var end = start + length;

        while (addr < end)
        {
            var pageEnd = Math.Min((addr & ~(long) (PageBytes - 1)) + PageBytes, end);
            var size = (int) (pageEnd - addr);

            var chunk = ReadBytesSafe(addr, size, out var error);

            if (chunk == null)
            {
                deadPages.Add(Tuple.Create(addr, pageEnd, error ?? "чтение не удалось"));
            }
            else
            {
                Buffer.BlockCopy(chunk, 0, buffer, (int) (addr - start), size);
                readableBytes += size;
            }

            addr = pageEnd;
        }

        return buffer;
    }

    private static bool[] BuildPageMap(long start, int length, List<Tuple<long, long, string>> deadPages)
    {
        var map = new bool[length];

        for (var i = 0; i < length; i++)
            map[i] = true;

        foreach (var dead in deadPages)
        {
            var from = (int) (dead.Item1 - start);
            var to = (int) (dead.Item2 - start);

            for (var i = Math.Max(0, from); i < Math.Min(length, to); i++)
                map[i] = false;
        }

        return map;
    }

    private static bool PageRangeOk(bool[] map, int offset, int size)
    {
        for (var i = offset; i < offset + size; i++)
        {
            if (i < 0 || i >= map.Length || !map[i])
                return false;
        }

        return true;
    }

    private static byte[] ReadBytesSafe(long address, int size, out string error)
    {
        error = null;

        if (size <= 0 || size > MaxWindowBytes)
        {
            error = $"запрошено {size} б — вне предела 1..{MaxWindowBytes}";
            return null;
        }

        if (!IsPointer(address))
        {
            error = "адрес вне пользовательской плоскости";
            return null;
        }

        try
        {
            var buffer = new byte[size];

            if (!ReadProcessMemory(_handle, new IntPtr(address), buffer, new IntPtr(size), out var read))
            {
                error = $"ReadProcessMemory: win32 {Marshal.GetLastWin32Error()}";
                return null;
            }

            if (read.ToInt64() != size)
            {
                error = $"прочитано {read.ToInt64()} б из {size}";
                return null;
            }

            return buffer;
        }
        catch (Exception e)
        {
            error = $"{e.GetType().Name}: {Oneline(e.Message)}";
            return null;
        }
    }

    // ── Where an address lives ───────────────────────────────────────────────────────────────────

    private static void CollectModules(Process process, long gameBase)
    {
        var list = new List<ModuleRange>();

        try
        {
            var count = 0;

            foreach (ProcessModule m in process.Modules)
            {
                if (count++ >= MaxModules)
                {
                    Console.WriteLine($"  ПРЕДЕЛ: перечислено {MaxModules} модулей, остальные не учтены в пометках.");
                    break;
                }

                try
                {
                    list.Add(new ModuleRange
                    {
                        Name = m.ModuleName,
                        Base = m.BaseAddress.ToInt64(),
                        Size = m.ModuleMemorySize,
                        IsGame = m.BaseAddress.ToInt64() == gameBase
                    });
                }
                catch
                {
                    // A module can be unloaded mid-enumeration — not a reason to fail.
                }
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"  модули процесса не перечислились ({e.GetType().Name}) — пометка «указатель в модуль» будет только по модулю игры.");
        }

        if (list.Count == 0)
        {
            // Even without the module list the game module itself is known, and it is the one that
            // matters for the vtable verdict.
            list.Add(new ModuleRange { Name = "модуль игры", Base = gameBase, Size = 0, IsGame = true });
        }

        _modules = list.OrderBy(x => x.Base).ToArray();

        Console.WriteLine($"  модулей в процессе: {_modules.Length}");
    }

    private static ModuleRange FindModule(long address)
    {
        if (!IsPointer(address))
            return null;

        foreach (var m in _modules)
        {
            if (m.Size <= 0)
                continue;

            if (address >= m.Base && address < m.End)
                return m;
        }

        return null;
    }

    // VirtualQueryEx is why this tool opens its own handle: Core's Memory opens the process with
    // VirtualMemoryRead only, and querying a region needs QUERY_INFORMATION as well. Results are
    // cached per region, bounded by MaxRegionCache, because a 64 KiB window can ask thousands of
    // times about the same few regions.
    private static RegionInfo QueryRegion(long address)
    {
        foreach (var cached in _regionCache)
        {
            if (address >= cached.Base && address < cached.End)
                return cached;
        }

        MEMORY_BASIC_INFORMATION info;

        try
        {
            var size = Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION));

            if (VirtualQueryEx(_handle, new IntPtr(address), out info, new IntPtr(size)) == 0)
                return null;
        }
        catch
        {
            return null;
        }

        const int MEM_COMMIT = 0x1000;
        const int PAGE_NOACCESS = 0x01;
        const int PAGE_GUARD = 0x100;

        var regionBase = info.BaseAddress.ToInt64();
        var regionSize = info.RegionSize.ToInt64();

        var region = new RegionInfo
        {
            Base = regionBase,
            End = regionBase + (regionSize > 0 ? regionSize : 1),
            Committed = info.State == MEM_COMMIT,
            Readable = info.State == MEM_COMMIT &&
                       (info.Protect & PAGE_NOACCESS) == 0 &&
                       (info.Protect & PAGE_GUARD) == 0,
            ProtectText = ProtectText(info.Protect)
        };

        if (_regionCache.Count < MaxRegionCache)
        {
            _regionCache.Add(region);
        }
        else if (!_regionCacheLimitAnnounced)
        {
            _regionCacheLimitAnnounced = true;
            Console.WriteLine($"  ПРЕДЕЛ: кэш областей памяти заполнен ({MaxRegionCache}) — дальше каждая область");
            Console.WriteLine("  опрашивается заново. Пометки от этого не меняются, только работа идёт медленнее.");
        }

        return region;
    }

    private static string ProtectText(int protect)
    {
        switch (protect & 0xFF)
        {
            case 0x01: return "нет доступа";
            case 0x02: return "r";
            case 0x04: return "rw";
            case 0x08: return "copy-on-write";
            case 0x10: return "x";
            case 0x20: return "rx";
            case 0x40: return "rwx";
            case 0x80: return "x + copy-on-write";
            default: return $"protect 0x{protect:X}";
        }
    }

    // ── Finding the process ──────────────────────────────────────────────────────────────────────
    //
    // Core.FindPoe() is public but unusable here: with SEVERAL clients it calls the private
    // ChooseSingleProcess, which shows a WinForms MessageBox and blocks the thread — a dead end in
    // a console tool. So the selection is repeated here, exactly as in tools/SanityRead: the public
    // Offsets.Regular/Steam/Korean are enough and Core stays untouched.
    private static bool FindGameProcess(out Process process, out Offsets offsets, out bool exactName, out string error)
    {
        process = null;
        offsets = null;
        exactName = false;
        error = null;

        var candidates = new List<(Process proc, Offsets off, bool exact)>();

        foreach (var variant in new[] { Offsets.Regular, Offsets.Steam, Offsets.Korean })
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(variant.ExeName))
                    candidates.Add((p, variant, true));
            }
            catch (Exception e)
            {
                error = $"перечисление процессов \"{variant.ExeName}\" не удалось: {e.GetType().Name}: {Oneline(e.Message)}";
                return false;
            }
        }

        // The fallback is not a convenience: tools/SanityRead was written after an exact-name-only
        // search reported "game not found" while the game was running, because the installed
        // client's process name did not match any Offsets.*.ExeName string. A name mismatch is
        // itself a stale number of the fork, and it is reported on the process line above — but it
        // must not stop the tool from attaching and measuring the rest.
        if (candidates.Count == 0)
        {
            Process[] all;

            try
            {
                all = Process.GetProcesses();
            }
            catch (Exception e)
            {
                error = $"перечисление процессов не удалось: {e.GetType().Name}: {Oneline(e.Message)}";
                return false;
            }

            foreach (var p in all)
            {
                string name;

                try
                {
                    name = p.ProcessName;
                }
                catch
                {
                    // The process may have ended mid-enumeration.
                    continue;
                }

                if (name.IndexOf(ExeNameFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                if (name.IndexOf("launcher", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                var off = name.IndexOf("steam", StringComparison.OrdinalIgnoreCase) >= 0
                    ? Offsets.Steam
                    : Offsets.Regular;

                candidates.Add((p, off, false));
            }
        }

        if (candidates.Count == 0)
        {
            error = "ни одного процесса с именами " +
                    string.Join(" / ", new[] { Offsets.Regular.ExeName, Offsets.Steam.ExeName, Offsets.Korean.ExeName }) +
                    $", и ни одного, чьё имя содержит \"{ExeNameFragment}\"";

            return false;
        }

        var ordered = candidates.OrderByDescending(c => c.exact).ToList();

        if (ordered.Count > 1)
            Console.WriteLine($"  найдено клиентов: {ordered.Count}; берём первый (диалог выбора в консоли неуместен).");

        process = ordered[0].proc;
        offsets = ordered[0].off;
        exactName = ordered[0].exact;
        return true;
    }

    private static IntPtr OpenReadHandle(int pid, out string error)
    {
        const int PROCESS_VM_READ = 0x0010;
        const int PROCESS_QUERY_INFORMATION = 0x0400;

        error = null;

        try
        {
            var handle = OpenProcess(PROCESS_VM_READ | PROCESS_QUERY_INFORMATION, false, pid);

            if (handle == IntPtr.Zero)
                error = $"OpenProcess: win32 {Marshal.GetLastWin32Error()}";

            return handle;
        }
        catch (Exception e)
        {
            error = $"{e.GetType().Name}: {Oneline(e.Message)}";
            return IntPtr.Zero;
        }
    }

    // ── Watchdog ─────────────────────────────────────────────────────────────────────────────────
    //
    // Every walk in this tool is bounded by a counter, but a bound outside the parsing is still
    // required: the incident this project remembers (two tool processes at ~4 GiB working set, not
    // finishing on their own) came from reads whose SIZE was computed out of the game's memory.
    // Nothing here computes a size that way, and the watchdog is what keeps that true by force.
    // The thread is background and only observes; it writes nothing anywhere.
    private static void StartWatchdog()
    {
        var started = DateTime.UtcNow;

        var thread = new System.Threading.Thread(() =>
        {
            Process self;

            try
            {
                self = Process.GetCurrentProcess();
            }
            catch
            {
                return;
            }

            while (true)
            {
                System.Threading.Thread.Sleep(250);

                string reason = null;

                if ((DateTime.UtcNow - started).TotalSeconds > WatchdogSeconds)
                {
                    reason = $"работа идёт дольше {WatchdogSeconds} с";
                }
                else
                {
                    try
                    {
                        self.Refresh();

                        if (self.WorkingSet64 > WatchdogMemoryBytes)
                            reason = $"занято больше {WatchdogMemoryBytes / 1048576} МиБ памяти";
                    }
                    catch
                    {
                        // Process counters can be temporarily unavailable.
                    }
                }

                if (reason == null)
                    continue;

                Console.WriteLine();
                Console.WriteLine($"СБОЙ: сторож остановил инструмент — {reason}.");
                Console.WriteLine("Это внешний предел поверх всех внутренних: если он сработал, значит что-то");
                Console.WriteLine("разбирает случайную память. Числа, напечатанные выше, всё равно верны.");

                try
                {
                    Console.Out.Flush();
                }
                catch
                {
                    // The output stream may already be closed.
                }

                Environment.Exit(ExitNothingToRead);
            }
        });

        thread.IsBackground = true;
        thread.Start();
    }

    // ── Arguments ────────────────────────────────────────────────────────────────────────────────

    // Addresses and values are HEX, with or without 0x — that is how they are copied out of the
    // reference fork's output and out of a debugger. The parsed value is echoed in both forms by
    // the caller, so the convention is never a guess for the reader.
    private static bool TryParseHex(string text, out long value, out string error)
    {
        value = 0;
        error = null;

        if (string.IsNullOrWhiteSpace(text))
        {
            error = "пустое значение";
            return false;
        }

        var t = text.Trim();

        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            t = t.Substring(2);

        t = t.Replace("`", "").Replace("_", "");

        if (t.Length == 0 || t.Length > 16)
        {
            error = "ожидается 1..16 шестнадцатеричных цифр";
            return false;
        }

        if (!ulong.TryParse(t, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var parsed))
        {
            error = "это не шестнадцатеричное число";
            return false;
        }

        value = unchecked((long) parsed);
        return true;
    }

    // --len is a LENGTH, not an address: decimal unless it carries an explicit 0x. Both forms are
    // printed back, so there is nothing to guess about which reading was used.
    private static bool TryParseLength(string text, out int value, out string error)
    {
        value = 0;
        error = null;

        var t = (text ?? "").Trim().Replace("_", "");

        long parsed;

        if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            if (!long.TryParse(t.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out parsed))
            {
                error = "это не шестнадцатеричное число";
                return false;
            }
        }
        else if (!long.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed))
        {
            error = "это не число (десятичное, либо с префиксом 0x)";
            return false;
        }

        if (parsed <= 0)
        {
            error = "длина должна быть больше нуля";
            return false;
        }

        if (parsed > MaxWindowBytes)
        {
            // The limit is announced, not applied silently: a truncated window that looked like a
            // full one would turn "not found" into a lie.
            Console.WriteLine($"ПРЕДЕЛ: запрошено {parsed} б ({Hex(parsed)}), предел окна {MaxWindowBytes} б " +
                              $"({Hex(MaxWindowBytes)}) — окно УРЕЗАНО до предела.");
            Console.WriteLine("Чтобы осмотреть больше, делайте несколько запусков с разным --in.");
            parsed = MaxWindowBytes;
        }

        value = (int) parsed;
        return true;
    }

    private static void Usage()
    {
        Console.WriteLine("FindOffset — чем смещение структуры отличается от базы объекта. ТОЛЬКО ЧТЕНИЕ.");
        Console.WriteLine();
        Console.WriteLine("РЕЖИМЫ");
        Console.WriteLine("  --find   --in <адрес> --len <байт> --value <значение> [--size 4|8]");
        Console.WriteLine("           напечатать КАЖДОЕ смещение окна, по которому лежит это значение.");
        Console.WriteLine("           Основной режим: истинный адрес берут у эталонного ExileApi-Compiled,");
        Console.WriteLine("           объект — свой; разность и есть искомое смещение.");
        Console.WriteLine();
        Console.WriteLine("  --window --in <адрес> --len <байт>");
        Console.WriteLine("           дамп окна по 8 байт с пометкой «на что это похоже»: указатель в модуль");
        Console.WriteLine("           игры (с RVA), указатель в кучу, малое число, float, строка по адресу.");
        Console.WriteLine();
        Console.WriteLine("  --chain  --in <адрес> --len <байт>");
        Console.WriteLine("           для каждого указателя окна — что лежит ПО НЕМУ: vtable внутри модуля");
        Console.WriteLine("           игры и первые qword'ы. Так объект отличают от случайного числа.");
        Console.WriteLine();
        Console.WriteLine("  --ingame-state");
        Console.WriteLine("           сам найти адрес IngameState (паттерн-скан -> контроллер состояний ->");
        Console.WriteLine("           слот InGameState, оффсетами ЭТОГО репозитория). Отдельно — печатает");
        Console.WriteLine("           адрес; вместе с любым режимом — подставляется вместо --in, чтобы не");
        Console.WriteLine("           переписывать адрес руками между запусками игры.");
        Console.WriteLine();
        Console.WriteLine("ФОРМАТ ЧИСЕЛ");
        Console.WriteLine("  --in, --value — HEX, с 0x и без (0x2A6B1F40C0 == 2A6B1F40C0).");
        Console.WriteLine("  --len         — десятичное, либо с явным 0x. Обе формы печатаются в ответе.");
        Console.WriteLine();
        Console.WriteLine("ПРЕДЕЛЫ (каждый печатается строкой, когда срабатывает)");
        Console.WriteLine($"  окно не больше {MaxWindowBytes} б ({Hex(MaxWindowBytes)}) — сверх предела окно урезается ГРОМКО;");
        Console.WriteLine($"  --chain проходит не больше {MaxChainTargets} указателей, по {MaxChainQwords} qword на каждый;");
        Console.WriteLine($"  не больше {MaxProbes} проб «что лежит по указателю»;");
        Console.WriteLine($"  сторож снаружи: {WatchdogSeconds} с и {WatchdogMemoryBytes / 1048576} МиБ рабочего набора.");
        Console.WriteLine();
        Console.WriteLine("КОДЫ ВОЗВРАТА");
        Console.WriteLine("  0 — что-то найдено и напечатано;");
        Console.WriteLine("  1 — не найдено (окно прочитано, совпадений нет);");
        Console.WriteLine("  2 — читать нечего: игра не запущена, процесс недоступен, аргументы не задают окна.");
        Console.WriteLine();
        Console.WriteLine("КАК ИМ ПОЛЬЗУЮТСЯ (восстановление IngameStateOffsets.Data)");
        Console.WriteLine("  1. запустить эталонный ExileApi-Compiled на ТОМ ЖЕ процессе и списать у него");
        Console.WriteLine("     истинный адрес IngameData (ingameState.Data.Address);");
        Console.WriteLine("  2. FindOffset.exe --find --ingame-state --len 0x2000 --value <этот адрес>");
        Console.WriteLine("     напечатанное смещение и есть новое [FieldOffset] для поля Data.");
        Console.WriteLine("  Окно берите с запасом: известная раскладка IngameStateOffsets тянется за 0xF00,");
        Console.WriteLine("  и короткий --len даёт «НЕ НАЙДЕНО» там, где поле просто не попало в окно.");
        Console.WriteLine();
        Console.WriteLine("ПРИМЕРЫ");
        Console.WriteLine("  FindOffset.exe --ingame-state");
        Console.WriteLine("  FindOffset.exe --find --ingame-state --len 0x2000 --value 0x2A6B1F40C0");
        Console.WriteLine("  FindOffset.exe --window --ingame-state --len 1024");
        Console.WriteLine("  FindOffset.exe --chain --in 0x2A6B1F0000 --len 512");
    }

    // ── Printing ─────────────────────────────────────────────────────────────────────────────────

    private static void Head(string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine(new string('-', 100));
    }

    private static bool IsPointer(long value)
    {
        return value >= MinUserPointer && value <= MaxUserPointer;
    }

    private static string Hex(long value)
    {
        return value < 0 ? $"-0x{-value:X}" : $"0x{value:X}";
    }

    // A value typed into --value, and a candidate window start, are BIT PATTERNS, not signed
    // numbers. Hex() echoes 0xCAFEBABEDEADBEEF back as "-0x3501454121524111" — a number the reader
    // never typed, which is exactly the silent "fixing" this tool must not do. Offsets, lengths and
    // RVAs keep Hex(): for those a minus sign is real information.
    private static string HexU(long value)
    {
        return "0x" + ((ulong) value).ToString("X");
    }

    // A value read out of memory can contain newlines and other debris — the table falls apart
    // exactly where it was meant to be read by eye.
    private static string Oneline(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";

        var sb = new StringBuilder(s.Length);

        foreach (var c in s)
            sb.Append(c == '\r' || c == '\n' || c == '\t' ? ' ' : c);

        return sb.ToString();
    }

    // ── Win32 ────────────────────────────────────────────────────────────────────────────────────
    //
    // Read-only by construction: OpenProcess asks for VM_READ | QUERY_INFORMATION and nothing else,
    // so even a bug in this file cannot write into the game. ReadProcessMemory is declared here
    // rather than reused from Core because Core's wrapper discards the success flag (see ReadWindow).

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle,
        int dwProcessId);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, IntPtr nSize,
        out IntPtr lpNumberOfBytesRead);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress,
        out MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool CloseHandle(IntPtr hObject);

    [StructLayout(LayoutKind.Sequential)]
    private struct MEMORY_BASIC_INFORMATION
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public int AllocationProtect;
        public int Alignment1;
        public IntPtr RegionSize;
        public int State;
        public int Protect;
        public int Type;
        public int Alignment2;
    }
}
