using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Cache;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Interfaces;
using GameOffsets;

// Оба пространства имён объявляют тип Offsets (ExileCore.PoEMemory.Offsets — набор сигнатур клиента,
// GameOffsets.Offsets — таблица смещений структур). Нужен первый; без псевдонима это CS0104.
using Offsets = ExileCore.PoEMemory.Offsets;

namespace ExileApi.Tools.SanityRead;

// ─────────────────────────────────────────────────────────────────────────────────────────────────
// SanityRead — «числа этого форка верны для той игры, что стоит на машине, или устарели?»
//
// Почему инструмент вообще существует. docs/api/parity-measured.md фиксирует, что этот форк и
// эталонный ExileApi-Compiled нацелены на РАЗНЫЕ сборки игры (86 расхождений из 121 общего поля
// структур, GameStat 10613 против 23339, RenderComponentOffsetsPos 120 против 128). При этом сам
// форк, судя по машине, ни разу не запускался — в каталоге сборки нет ни логов, ни конфигов. То
// есть его числа против УСТАНОВЛЕННОЙ игры не проверялись ни разу, и весь паритет пока ставка.
//
// Что делает. Прикладывается к живому процессу игры ЭТИМ форком (ProjectReference на Core и
// GameOffsets — читаем именно теми оффсетами, что лежат в репозитории) и печатает таблицу
// «что прочитали / похоже ли на правду». Порядок шагов — от самого надёжного к самому хрупкому,
// чтобы по МЕСТУ ОБРЫВА было видно, где именно ломается цепочка: процесс → паттерн-скан →
// корень игрового состояния → зона → игрок → сущности.
//
// Границы (заданы жёстко и соблюдены по построению):
//   * ТОЛЬКО ЧТЕНИЕ. Memory открывает хэндл с ProcessAccessFlags.VirtualMemoryRead (Core/Memory.cs)
//     и ничего, кроме чтения, не умеет. Ни записи, ни ввода, ни внедрения здесь нет.
//   * Ни оверлея, ни DirectX, ни ImGui, ни WinForms-окон, ни плагинов, ни корутин. Отработал — вышел.
//   * НЕ ПАДАТЬ. Игра не запущена, процесс исчез посреди чтения, указатель мусорный — это
//     ОЖИДАЕМЫЕ исходы. Каждый печатается строкой, не стектрейсом.
//   * Core/, GameOffsets/, Loader/ и .sln не тронуты. Чего не хватило в публичной поверхности —
//     взято обходным путём и описано на месте (см. FindGameProcess и WalkEntityList).
//
// Коды возврата:
//   0 — все прочитанные числа выглядят правдоподобно;
//   1 — есть явно бессмысленные значения (ЭТО и есть ответ, ради которого всё писалось);
//   2 — прочитать нечего: игра не запущена или процесс недоступен.
//       Сбой паттерн-скана сюда НЕ относится: это измерение с результатом «мусор», то есть код 1.
//
// Числа печатаются КАК ЕСТЬ, рядом с ожидаемым диапазоном. Ничего не «чинится» и не подгоняется:
// инструмент — измеритель, а не лечение.
// ─────────────────────────────────────────────────────────────────────────────────────────────────
internal static class Program
{
    private enum Verdict
    {
        Ok,
        Bad,
        Skip
    }

    private const int ExitPlausible = 0;
    private const int ExitGarbage = 1;
    private const int ExitNothingToRead = 2;

    // Плоскость адресов пользовательского режима x64: ниже 0x10000 — заведомо не указатель
    // (нулевая страница), выше 0x7FFF_FFFF_FFFF — не пользовательский адрес.
    private const long MinUserPointer = 0x10000L;
    private const long MaxUserPointer = 0x7FFFFFFFFFFFL;

    // Подстрока, по которой клиент PoE узнаётся, даже когда ни одна из трёх зашитых в форк строк
    // Offsets.*.ExeName не подошла (см. FindGameProcess).
    private const string ExeNameFragment = "pathofexile";

    private static int _passed;
    private static int _failed;
    private static int _skipped;

    // Совпало ли имя найденного процесса с одной из строк Offsets.*.ExeName, или он взят
    // запасным поиском. Это измерение, а не деталь реализации: несовпадение означает, что сам
    // форк такой клиент не найдёт вообще.
    private static bool _exeNameIsKnown;

    // Процесс игры вообще нашёлся. Без этого флага итог рапортовал бы про несовпадение имени
    // и когда игра просто не запущена — то есть выдавал бы находку там, где ничего не измеряли.
    private static bool _processFound;

    // Клиент стоял НЕ в игре (меню, выбор персонажа, загрузочный экран). Тогда часть строк ниже
    // честно помечена мусором просто потому, что читать было нечего, — и об этом надо сказать
    // в итоге, иначе вердикт «числа устарели» будет выдан за то, чего не измеряли.
    private static bool _notInGame;

    private static int Main()
    {
        // Вывод читают и глазами, и через перенаправление в файл; без этого .NET пишет кодовой
        // страницей консоли (на этой машине cp866) и таблица становится нечитаемой при первом «> file».
        try
        {
            Console.OutputEncoding = Encoding.UTF8;
        }
        catch
        {
            // Консоли может не быть вовсе (перенаправленный stdout под службой) — не повод падать.
        }

        Console.WriteLine("SanityRead — проверка оффсетов ЭТОГО форка против установленной игры.");
        Console.WriteLine("Только чтение. Числа печатаются как есть, рядом — ожидаемый диапазон.");

        StartWatchdog();

        Memory memory = null;

        try
        {
            var code = Run(ref memory);
            Summary();
            return code;
        }
        catch (Exception e)
        {
            // Последний рубеж: что угодно непойманное обязано стать строкой, а не стектрейсом.
            Console.WriteLine();
            Console.WriteLine($"СБОЙ: непредвиденное исключение {e.GetType().Name}: {Oneline(e.Message)}");
            Summary();
            return ExitGarbage;
        }
        finally
        {
            try
            {
                memory?.Dispose();
            }
            catch
            {
                // Memory.Dispose логирует через Core-логгер, которого в консольном режиме нет.
            }
        }
    }

    // ── Сторож ───────────────────────────────────────────────────────────────────────────────────
    //
    // Стоп-кран по GameStateOffset выше закрывает известный путь в разнос, но он не единственный:
    // любая ссылка ниже по цепочке может оказаться мусором, а Core на мусоре не ограничивает ни
    // глубину обхода структур, ни размер выделяемого буфера (ReadMem выделяет ровно столько байт,
    // сколько насчиталось из памяти игры). Измеренный исход — два процесса инструмента с рабочим
    // набором ~4 ГиБ, не завершающиеся сами; на машине, где в это же время работает игра, это
    // хуже любого стектрейса. Поэтому ограничение стоит СНАРУЖИ разбора: по времени и по памяти.
    // Поток фоновый и только наблюдает, в чужой процесс не пишет.
    private const int WatchdogSeconds = 120;
    private const long WatchdogMemoryBytes = 1536L * 1024 * 1024;

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
                    reason = $"разбор идёт дольше {WatchdogSeconds} с и не сходится";
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
                        // Счётчики процесса могут быть временно недоступны — не повод падать.
                    }
                }

                if (reason == null)
                    continue;

                Console.WriteLine();
                Console.WriteLine($"СБОЙ: сторож остановил инструмент — {reason}.");
                Console.WriteLine("Так выглядит разбор случайной памяти по мусорным якорям: структуры не сходятся,");
                Console.WriteLine("а размеры, посчитанные из них, уходят в гигабайты. Это тоже ответ: числа форка");
                Console.WriteLine("не подходят установленной игре.");
                Summary();

                try
                {
                    Console.Out.Flush();
                }
                catch
                {
                    // Поток вывода мог быть уже закрыт.
                }

                Environment.Exit(ExitGarbage);
            }
        });

        thread.IsBackground = true;
        thread.Start();
    }

    private static int Run(ref Memory memory)
    {
        // ── Шаг 1. Процесс ───────────────────────────────────────────────────────────────────────
        Head("1. ПРОЦЕСС");

        var found = FindGameProcess(out var process, out var offsets, out var findError);

        if (!found)
        {
            Console.WriteLine($"  игра не найдена: {findError}");
            Console.WriteLine("  читать нечего — запустите Path of Exile и повторите.");
            return ExitNothingToRead;
        }

        long moduleBase;
        long moduleSize;

        try
        {
            moduleBase = process.MainModule.BaseAddress.ToInt64();
            moduleSize = process.MainModule.ModuleMemorySize;
        }
        catch (Exception e)
        {
            // Типичные причины: процесс исчез между поиском и чтением, или не хватает прав.
            Console.WriteLine($"  главный модуль процесса недоступен: {e.GetType().Name}: {Oneline(e.Message)}");
            Console.WriteLine("  читать нечего — запустите консоль от того же пользователя, что и игру.");
            return ExitNothingToRead;
        }

        // Имя исполняемого файла — такое же число форка, как любое смещение, и первое, что можно
        // сверить с установленной игрой. Если оно не совпало, форк этот клиент НЕ НАЙДЁТ, и это
        // уже ответ на главный вопрос — независимо от того, что прочитается ниже.
        Row("имя процесса", process.ProcessName, "совпадает с Offsets.*.ExeName",
            _exeNameIsKnown ? Verdict.Ok : Verdict.Bad);

        Row("набор оффсетов", _exeNameIsKnown ? offsets.ExeName : $"{offsets.ExeName} (подобран)",
            "Regular / Steam / Korean", _exeNameIsKnown ? Verdict.Ok : Verdict.Skip);

        if (!_exeNameIsKnown)
        {
            Console.WriteLine();
            Console.WriteLine($"  ВНИМАНИЕ: процесс \"{process.ProcessName}\" не совпал ни с одной строкой Offsets.*.ExeName");
            Console.WriteLine($"  ({Offsets.Regular.ExeName} / {Offsets.Steam.ExeName} / {Offsets.Korean.ExeName}).");
            Console.WriteLine("  Сам форк такой клиент не нашёл бы вообще. Приложились запасным поиском по подстроке");
            Console.WriteLine($"  \"{ExeNameFragment}\"; всё, что напечатано ниже, измерено на этом процессе честно.");
            Console.WriteLine();
        }
        Row("pid", process.Id.ToString(CultureInfo.InvariantCulture), "> 0", process.Id > 0 ? Verdict.Ok : Verdict.Bad);
        Row("база модуля", Hex(moduleBase), "указатель 0x10000..0x7FFFFFFFFFFF", PointerVerdict(moduleBase));

        // Клиент PoE — это десятки мегабайт кода. И ноль, и «полтора гигабайта» означают, что мы
        // читаем не тот модуль, и паттерн-скан ниже будет искать в пустоте.
        Row("размер модуля", $"{moduleSize:N0} б ({moduleSize / 1048576.0:F1} МиБ)", "8..512 МиБ",
            moduleSize >= 8L * 1048576 && moduleSize <= 512L * 1048576 ? Verdict.Ok : Verdict.Bad);

        // ── Шаг 2. Паттерн-скан ──────────────────────────────────────────────────────────────────
        Head("2. ЯКОРЯ ПАТТЕРН-СКАНА (BaseOffsets)");
        Console.WriteLine("  Сначала СЫРОЙ результат скана по каждой сигнатуре из Offsets.cs: 0 — паттерн НЕ НАЙДЕН,");
        Console.WriteLine("  и это первый и самый прямой признак, что сигнатуры не от той сборки игры. Затем —");
        Console.WriteLine("  производные от них якоря BaseOffsets: они считаются ИЗ найденного смещения, поэтому");
        Console.WriteLine("  ненайденный паттерн даёт там не ноль, а просто мусорное число.");
        Console.WriteLine();

        try
        {
            // Конструктор Memory сам открывает read-хэндл и запускает DoPatternScans — то есть
            // весь шаг 2 происходит здесь, и это самая долгая операция инструмента (скан ~30-60 МиБ).
            memory = new Memory((process, offsets));
        }
        catch (Exception e)
        {
            // Сюда попадает падение самого DoPatternScans этого форка: при ненайденных сигнатурах
            // он индексирует массив результатов без проверки. Если до этого уже нашлось хоть одно
            // бессмысленное число, «прочитать нечего» — неправда: прочитать как раз удалось, и
            // прочитанное оказалось мусором. Код 2 тут скрыл бы ровно тот ответ, ради которого
            // инструмент и написан, поэтому различаем эти два исхода.
            Console.WriteLine($"  приложиться не удалось: {e.GetType().Name}: {Oneline(e.Message)}");

            if (_failed > 0)
            {
                Console.WriteLine("  Это сбой самого форка на паттерн-скане, а не отсутствие игры: выше уже есть");
                Console.WriteLine("  бессмысленные значения. Цепочка обрывается на втором шаге.");
                return ExitGarbage;
            }

            Console.WriteLine("  читать нечего.");
            return ExitNothingToRead;
        }

        var baseOffsets = memory.BaseOffsets;

        if (baseOffsets == null)
        {
            // Пустая таблица якорей — это тоже результат измерения, а не «нечего читать»:
            // процесс открыт, образ прочитан, просто ни один якорь не сложился.
            Console.WriteLine("  BaseOffsets == null — паттерн-скан не дал ни одного якоря.");
            Row("BaseOffsets", "null", "таблица якорей заполнена", Verdict.Bad);
            return ExitGarbage;
        }

        // Сначала СЫРОЙ результат скана, потом производные от него якоря: ноль виден только здесь.
        var rawHits = ReportRawPatterns(memory, moduleSize);
        Console.WriteLine();

        // Какая сигнатура даёт какой якорь — читается прямо из Offsets.DoPatternScans.
        var anchorSource = new Dictionary<OffsetsName, string>
        {
            { OffsetsName.FileRoot, "File Root" },
            { OffsetsName.AreaChangeCount, "Area change" },
            { OffsetsName.GameStateOffset, "Game State" }
        };

        foreach (OffsetsName name in Enum.GetValues(typeof(OffsetsName)))
        {
            if (!baseOffsets.TryGetValue(name, out var value))
            {
                // Base и IsLoadingScreenOffset закомментированы в самом DoPatternScans этого форка.
                // Это не находка инструмента, а факт исходника — потому «н/д», а не «мусор».
                Row(name.ToString(), "не сканируется", "закомментирован в Offsets.DoPatternScans", Verdict.Skip);
                continue;
            }

            // Проверять якорь одним лишь диапазоном мало, и это не теория: на этой машине при
            // ВСЕХ ненайденных сигнатурах FileRoot получился 0xB813, AreaChangeCount 0x4000A,
            // GameStateOffset 0x21 — все три честно лежат внутри образа и раньше печатались как
            // «ok». Якорь считается от найденного смещения, поэтому при смещении 0 он мусор по
            // происхождению, каким бы правдоподобным ни выглядел.
            if (anchorSource.TryGetValue(name, out var source) && rawHits != null &&
                rawHits.TryGetValue(source, out var sourceHit) && sourceHit <= 0)
            {
                Row(name.ToString(), $"{Hex(value)} (abs {Hex(moduleBase + value)})",
                    $"считается от сигнатуры «{source}», а она НЕ НАЙДЕНА", Verdict.Bad);

                continue;
            }

            // Якоря — это RVA внутри образа, не абсолютные адреса: сравниваем с размером модуля.
            var ok = value > 0 && value < moduleSize;

            Row(name.ToString(), $"{Hex(value)} (abs {Hex(moduleBase + value)})", $"0 < RVA < {Hex(moduleSize)}",
                ok ? Verdict.Ok : Verdict.Bad);
        }

        // Стоп-кран. GameStateOffset — вход во ВСЁ остальное, и если сигнатура «Game State» не
        // нашлась, он посчитан от нуля. Идти дальше нельзя не из осторожности, а по факту:
        // ReadHashMap в Core/PoEMemory/MemoryObjects/GameStateContoller.cs обходит дерево по
        // прочитанным указателям без ограничения глубины, а GameStateActive считает длину вектора
        // как (last - start) и просит ReadMem выделить ровно столько. На мусорных числах это дало
        // два процесса инструмента по ~4 ГиБ рабочего набора, которые не завершались сами.
        if (rawHits != null && rawHits.TryGetValue("Game State", out var gameStateHit) && gameStateHit <= 0)
        {
            Console.WriteLine();
            Console.WriteLine("  Сигнатура «Game State» НЕ НАЙДЕНА — GameStateOffset посчитан от нуля и указывает в никуда.");
            Console.WriteLine("  Разбор состояний по нему пошёл бы по случайной памяти, а он в Core не ограничен ни");
            Console.WriteLine("  глубиной обхода, ни размером выделения. Останавливаемся здесь намеренно.");
            Console.WriteLine();
            Console.WriteLine("  ОТВЕТ: числа этого форка НЕ подходят установленной игре. Смещения структур");
            Console.WriteLine("  проверять не на чем — цепочка обрывается на самом первом якоре.");

            return ExitGarbage;
        }

        // ── Шаг 3. Корень игрового состояния ─────────────────────────────────────────────────────
        Head("3. КОРЕНЬ ИГРОВОГО СОСТОЯНИЯ");

        TheGame game;

        try
        {
            var cache = new Cache();

            // TheGame — первое, что ОБЯЗАНО сломаться, если GameStateOffset указывает не туда:
            // конструктор читает хэш-мапу состояний и достаёт из неё "InGameState" по строковому
            // ключу. Промах = KeyNotFoundException, и это ровно тот ответ, который нам нужен.
            game = new TheGame(memory, cache);
        }
        catch (Exception e)
        {
            Row("TheGame", $"исключение {e.GetType().Name}", "конструктор проходит целиком", Verdict.Bad);
            Console.WriteLine($"       └ {Oneline(e.Message)}");
            // Место падения важнее текста: имя метода в стеке сразу называет слой, чья раскладка
            // разошлась с клиентом. Без него приходится гадать по сообщению.
            foreach (var frame in (e.StackTrace ?? "").Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).Take(6))
                Console.WriteLine($"       │ {Oneline(frame)}");
            Console.WriteLine();
            Console.WriteLine("  Цепочка оборвана на корне: дальше читать не из чего.");
            Console.WriteLine("  Это и есть ответ — GameStateOffset/раскладка хэш-мапы состояний не от этой сборки.");
            return ExitGarbage;
        }

        Row("TheGame.Address", Hex(game.Address), "указатель 0x10000..0x7FFFFFFFFFFF", PointerVerdict(game.Address));

        var states = Safe(() => game.AllGameStates, out var statesErr);

        if (states == null)
        {
            Row("состояний в хэш-мапе", $"ошибка: {statesErr}", "5..40 штук", Verdict.Bad);
        }
        else
        {
            // Ванильный клиент держит около десятка именованных состояний. Единицы — мусорная
            // мапа; сотни — мы разбираем не хэш-мапу, а случайную память.
            Row("состояний в хэш-мапе", states.Count.ToString(CultureInfo.InvariantCulture), "5..40 штук",
                states.Count >= 5 && states.Count <= 40 ? Verdict.Ok : Verdict.Bad);

            Row("есть InGameState", states.ContainsKey("InGameState") ? "да" : "нет", "обязано быть \"да\"",
                states.ContainsKey("InGameState") ? Verdict.Ok : Verdict.Bad);

            Console.WriteLine("       имена: " + Oneline(string.Join(", ", states.Keys.OrderBy(x => x, StringComparer.Ordinal))));
        }

        var inGame = SafeStruct(() => game.InGame, out var inGameErr);

        Row("InGame", inGame?.ToString() ?? $"ошибка: {inGameErr}", "true/false (читается без исключения)",
            inGame.HasValue ? Verdict.Ok : Verdict.Bad);

        var isLoading = SafeStruct(() => game.IsLoading, out var loadingErr);

        Row("IsLoading", isLoading?.ToString() ?? $"ошибка: {loadingErr}", "true/false (читается без исключения)",
            isLoading.HasValue ? Verdict.Ok : Verdict.Bad);

        var inGameState = SafeStruct(() => game.IsInGameState, out var igsErr);

        Row("IsInGameState", inGameState?.ToString() ?? $"ошибка: {igsErr}", "true/false (читается без исключения)",
            inGameState.HasValue ? Verdict.Ok : Verdict.Bad);

        // Три строки выше проверяют только «прочиталось без исключения», а этого мало: Memory.Read<T>
        // на мусорном адресе возвращает default, а не бросает, — значит, сплошные нули прошли бы
        // все три как «ok». Нужен признак, который на нулях ЛОЖЕН. Вот он: клиент всегда держит
        // хотя бы одно активное состояние (вектор по Address+0x20) и никогда — все семь сразу.
        // Ноль активных = мы разбираем нули; семь = мы разбираем случайную память.
        var flags = new (string name, bool? value)[]
        {
            ("PreGame", SafeStruct(() => game.IsPreGame, out _)),
            ("Login", SafeStruct(() => game.IsLoginState, out _)),
            ("SelectCharacter", SafeStruct(() => game.IsSelectCharacterState, out _)),
            ("Waiting", SafeStruct(() => game.IsWaitingState, out _)),
            ("InGame", inGameState),
            ("Loading", SafeStruct(() => game.IsLoadingState, out _)),
            ("Escape", SafeStruct(() => game.IsEscapeState, out _))
        };

        var activeNames = flags.Where(f => f.value == true).Select(f => f.name).ToList();
        var readableFlags = flags.Count(f => f.value.HasValue);

        Row("активных состояний", $"{activeNames.Count} из {readableFlags} читаемых", "1..4 (0 = читаем нули)",
            activeNames.Count >= 1 && activeNames.Count <= 4 ? Verdict.Ok : Verdict.Bad);

        Console.WriteLine("       активны: " + (activeNames.Count == 0 ? "ни одного" : string.Join(", ", activeNames)));

        _notInGame = inGameState != true;

        var areaChanges = SafeStruct(() => game.AreaChangeCount, out var accErr);

        // Ноль проходил как «правдоподобный», хотя это ровно то, что вернёт чтение нулей. Внутри
        // игры счётчик смен зоны не бывает нулевым: чтобы там оказаться, зону уже сменили.
        var mustBePositive = inGameState == true || inGame == true;

        Row("AreaChangeCount", areaChanges?.ToString(CultureInfo.InvariantCulture) ?? $"ошибка: {accErr}",
            mustBePositive ? "1..1000000 (клиент сообщает, что мы в игре)" : "0..1000000",
            areaChanges.HasValue && areaChanges.Value >= (mustBePositive ? 1 : 0) && areaChanges.Value <= 1000000
                ? Verdict.Ok
                : Verdict.Bad);

        var ingameState = Safe(() => game.IngameState, out _);
        var stateAddress = ingameState?.Address ?? 0;
        Row("IngameState.Address", Hex(stateAddress), "указатель 0x10000..0x7FFFFFFFFFFF", PointerVerdict(stateAddress));

        var data = ingameState == null ? null : Safe(() => ingameState.Data, out _);
        var dataAddress = data?.Address ?? 0;
        Row("IngameState.Data", Hex(dataAddress), "указатель 0x10000..0x7FFFFFFFFFFF", PointerVerdict(dataAddress));

        if (data == null || !IsPointer(dataAddress))
        {
            Console.WriteLine();
            Console.WriteLine("  Цепочка оборвана на IngameData: зону, игрока и сущности читать не из чего.");
            Console.WriteLine("  Если игра стояла на загрузочном экране или в выборе персонажа — это норма,");
            Console.WriteLine("  иначе смещение IngameStateOffsets.Data не от этой сборки.");
            return _failed > 0 ? ExitGarbage : ExitPlausible;
        }

        // ── Шаг 4. Текущая зона ──────────────────────────────────────────────────────────────────
        Head("4. ТЕКУЩАЯ ЗОНА");

        var areaTemplate = Safe(() => data.CurrentArea, out var areaErr);

        if (areaTemplate == null || !IsPointer(areaTemplate.Address))
        {
            Row("AreaTemplate", areaErr ?? Hex(areaTemplate?.Address ?? 0), "указатель 0x10000..0x7FFFFFFFFFFF", Verdict.Bad);
        }
        else
        {
            Row("AreaTemplate.Address", Hex(areaTemplate.Address), "указатель 0x10000..0x7FFFFFFFFFFF", Verdict.Ok);

            var name = Safe(() => areaTemplate.Name, out var nameErr) ?? "";
            Row("имя зоны", Quote(name, nameErr), "непустой печатный текст <= 64 символов", TextVerdict(name, 1, 64));

            var rawName = Safe(() => areaTemplate.RawName, out var rawErr) ?? "";
            Row("сырое имя зоны", Quote(rawName, rawErr), "непустой печатный текст <= 64 символов", TextVerdict(rawName, 1, 64));

            var act = SafeStruct(() => areaTemplate.Act, out var actErr);

            Row("акт", act?.ToString(CultureInfo.InvariantCulture) ?? $"ошибка: {actErr}", "0..12",
                act.HasValue && act.Value >= 0 && act.Value <= 12 ? Verdict.Ok : Verdict.Bad);
        }

        var areaLevel = SafeStruct(() => data.CurrentAreaLevel, out var lvlErr);

        Row("уровень зоны", areaLevel?.ToString(CultureInfo.InvariantCulture) ?? $"ошибка: {lvlErr}", "1..100",
            areaLevel.HasValue && areaLevel.Value >= 1 && areaLevel.Value <= 100 ? Verdict.Ok : Verdict.Bad);

        var areaHash = SafeStruct(() => data.CurrentAreaHash, out var hashErr);

        Row("хеш инстанса", areaHash?.ToString(CultureInfo.InvariantCulture) ?? $"ошибка: {hashErr}",
            "не 0 и не uint.MaxValue",
            areaHash.HasValue && areaHash.Value != 0 && areaHash.Value != uint.MaxValue ? Verdict.Ok : Verdict.Bad);

        // Размеры terrain-сетки нужны и сами по себе, и как рамка для проверки позиции игрока ниже.
        var terrain = SafeStruct(() => data.Terrain, out var terrainErr);
        var gridWidth = 0;
        var gridHeight = 0;

        if (!terrain.HasValue)
        {
            Row("terrain-сетка", $"ошибка: {terrainErr}", "NumCols/NumRows/BytesPerRow читаются", Verdict.Bad);
        }
        else
        {
            var t = terrain.Value;

            // NumCols/NumRows считают ТАЙЛЫ, а не клетки: сторона тайла — 23 клетки. Ширина сетки в
            // клетках — BytesPerRow*2 (байт пакует ДВЕ 4-битные клетки), высота — размер мели-слоя,
            // делённый на шаг строки; так её выводит и плагин (Terrain/TerrainGrid.cs), и эталон.
            // Брать NumRows за высоту НЕЛЬЗЯ, и это ровно ловушка «ok не значит верно»: на замеренной
            // зоне вышло бы 81 вместо 1863, и 81 выглядит совершенно правдоподобной высотой.
            var meleeBytes = t.LayerMelee.Last - t.LayerMelee.First;
            var strideOk = t.BytesPerRow >= 1 && t.BytesPerRow <= 4000;

            gridWidth = strideOk ? t.BytesPerRow * 2 : 0;
            gridHeight = strideOk && meleeBytes > 0 && meleeBytes / t.BytesPerRow <= int.MaxValue
                ? (int) (meleeBytes / t.BytesPerRow)
                : 0;

            Row("terrain NumCols (тайлы)", t.NumCols.ToString(CultureInfo.InvariantCulture), "1..1000",
                t.NumCols >= 1 && t.NumCols <= 1000 ? Verdict.Ok : Verdict.Bad);

            Row("terrain NumRows (тайлы)", t.NumRows.ToString(CultureInfo.InvariantCulture), "1..1000",
                t.NumRows >= 1 && t.NumRows <= 1000 ? Verdict.Ok : Verdict.Bad);

            Row("terrain BytesPerRow", t.BytesPerRow.ToString(CultureInfo.InvariantCulture), "1..4000",
                strideOk ? Verdict.Ok : Verdict.Bad);

            Row("мели-слой, байт", meleeBytes.ToString(CultureInfo.InvariantCulture),
                "кратен BytesPerRow",
                strideOk && meleeBytes > 0 && meleeBytes % t.BytesPerRow == 0 ? Verdict.Ok : Verdict.Bad);

            Row("сетка (ширина x высота)", $"{gridWidth} x {gridHeight}", "обе стороны 1..8000",
                gridWidth >= 1 && gridWidth <= 8000 && gridHeight >= 1 && gridHeight <= 8000 ? Verdict.Ok : Verdict.Bad);

            // Независимая сверка: высота, посчитанная по слою, обязана совпасть с высотой, посчитанной
            // по тайлам. Два разных поля структуры сходятся только если ОБА прочитаны верно.
            Row("высота = NumRows * 23", $"{gridHeight} против {t.NumRows * 23}", "числа совпадают",
                gridHeight > 0 && gridHeight == t.NumRows * 23 ? Verdict.Ok : Verdict.Bad);
        }

        // ── Шаг 5. Игрок ─────────────────────────────────────────────────────────────────────────
        Head("5. ИГРОК");

        var player = Safe(() => data.LocalPlayer, out var playerErr);

        if (player == null || !IsPointer(player.Address))
        {
            Row("LocalPlayer", playerErr ?? Hex(player?.Address ?? 0), "указатель 0x10000..0x7FFFFFFFFFFF", Verdict.Bad);
            Console.WriteLine("       (в загрузочном экране или в выборе персонажа это норма)");
        }
        else
        {
            Row("LocalPlayer.Address", Hex(player.Address), "указатель 0x10000..0x7FFFFFFFFFFF", Verdict.Ok);

            // Entity.Pos/GridPos/RenderName ВОЗВРАЩАЮТ КЭШ, пока IsValid == false: этот флаг обычно
            // ставит EntityListWrapper, которого здесь нет (он тянет потоки и корутины). Ставим сами —
            // свойство публичное и с сеттером, обходных путей не требуется.
            TrySet(() => player.IsValid = true);

            var path = Safe(() => player.Path, out var pathErr) ?? "";
            Row("путь игрока", Quote(path, pathErr), "начинается с \"Metadata/\"", MetadataVerdict(path));

            var life = Safe(() => player.GetComponent<Life>(), out var lifeErr);

            if (life == null || !IsPointer(life.Address))
            {
                Row("компонент Life", lifeErr ?? "не найден", "указатель 0x10000..0x7FFFFFFFFFFF", Verdict.Bad);
            }
            else
            {
                Row("Life.Address", Hex(life.Address), "указатель 0x10000..0x7FFFFFFFFFFF", Verdict.Ok);

                var curHp = SafeStruct(() => life.CurHP, out var curHpErr);
                var maxHp = SafeStruct(() => life.MaxHP, out var maxHpErr);
                var curMana = SafeStruct(() => life.CurMana, out _);
                var maxMana = SafeStruct(() => life.MaxMana, out _);
                var curEs = SafeStruct(() => life.CurES, out _);
                var maxEs = SafeStruct(() => life.MaxES, out _);

                // Персонаж 100 уровня редко переваливает за 10-12 тысяч HP; 100000 — заведомо
                // не показатель жизни, а соседнее поле, прочитанное как жизнь.
                Row("HP", curHp.HasValue && maxHp.HasValue ? $"{curHp} / {maxHp}" : $"ошибка: {curHpErr ?? maxHpErr}",
                    "MaxHP 1..100000 и 0 <= CurHP <= MaxHP", PoolVerdict(curHp, maxHp, false));

                Row("мана", curMana.HasValue && maxMana.HasValue ? $"{curMana} / {maxMana}" : "ошибка чтения",
                    "MaxMana 1..100000 и 0 <= CurMana <= MaxMana", PoolVerdict(curMana, maxMana, false));

                // Энергощит у билда может законно отсутствовать — поэтому MaxES == 0 допустим.
                Row("энергощит", curEs.HasValue && maxEs.HasValue ? $"{curEs} / {maxEs}" : "ошибка чтения",
                    "MaxES 0..100000 и 0 <= CurES <= MaxES", PoolVerdict(curEs, maxEs, true));
            }

            var gridPos = SafeStruct(() => player.GridPosNum, out var gridErr);

            if (!gridPos.HasValue)
            {
                Row("позиция в сетке", $"ошибка: {gridErr}", "внутри размеров зоны", Verdict.Bad);
            }
            else
            {
                var g = gridPos.Value;
                var boundsKnown = gridWidth > 0 && gridHeight > 0;

                // Если размеры сетки прочитать не удалось, проверять позицию «по зоне» нечем —
                // тогда порог вырожденный, и это честнее показать отдельной формулировкой,
                // чем молча выдать зелёную строку по несуществующей рамке.
                var inBounds = boundsKnown
                    ? g.X >= 0 && g.X < gridWidth && g.Y >= 0 && g.Y < gridHeight
                    : g.X >= 0 && g.X < 8000 && g.Y >= 0 && g.Y < 8000;

                Row("позиция в сетке", $"({g.X:F1}, {g.Y:F1})",
                    boundsKnown ? $"0..{gridWidth} x 0..{gridHeight} (размеры зоны)" : "0..8000 (размеры зоны неизвестны)",
                    inBounds ? Verdict.Ok : Verdict.Bad);
            }

            var worldPos = SafeStruct(() => player.PosNum, out var posErr);

            if (!worldPos.HasValue)
            {
                Row("позиция в мире", $"ошибка: {posErr}", "конечные числа, |X|,|Y| < 1e7", Verdict.Bad);
            }
            else
            {
                var p = worldPos.Value;

                var finite = !float.IsNaN(p.X) && !float.IsNaN(p.Y) && !float.IsNaN(p.Z) &&
                             !float.IsInfinity(p.X) && !float.IsInfinity(p.Y) && !float.IsInfinity(p.Z);

                Row("позиция в мире", $"({p.X:F1}, {p.Y:F1}, {p.Z:F1})", "конечные числа, |X|,|Y| < 1e7",
                    finite && Math.Abs(p.X) < 1e7f && Math.Abs(p.Y) < 1e7f ? Verdict.Ok : Verdict.Bad);
            }
        }

        // ── Шаг 6. Сущности ──────────────────────────────────────────────────────────────────────
        Head("6. СПИСОК СУЩНОСТЕЙ");

        var declaredCount = SafeStruct(() => data.EntitiesCount, out var countErr);

        Row("EntitiesCount (заявлено игрой)",
            declaredCount?.ToString(CultureInfo.InvariantCulture) ?? $"ошибка: {countErr}", "1..20000",
            declaredCount.HasValue && declaredCount.Value >= 1 && declaredCount.Value <= 20000 ? Verdict.Ok : Verdict.Bad);

        var listAddress = SafeStruct(() => data.DataStruct.EntityList, out _) ?? 0;
        Row("EntityList.Address", Hex(listAddress), "указатель 0x10000..0x7FFFFFFFFFFF", PointerVerdict(listAddress));

        if (!IsPointer(listAddress))
        {
            Console.WriteLine("  адрес списка сущностей не похож на указатель — обход не начат.");
            return _failed > 0 ? ExitGarbage : ExitPlausible;
        }

        var addresses = WalkEntityList(memory, listAddress, out var walkError);

        if (addresses == null)
        {
            Row("обход списка", $"ошибка: {walkError}", "обход завершается без исключения", Verdict.Bad);
            return ExitGarbage;
        }

        Row("сущностей найдено обходом", addresses.Count.ToString(CultureInfo.InvariantCulture), "1..20000",
            addresses.Count >= 1 && addresses.Count <= 20000 ? Verdict.Ok : Verdict.Bad);

        PrintNearestEntities(data, player, addresses);

        // ── Шаг 7. Камера ────────────────────────────────────────────────────────────────────────
        //
        // Единственный раздел, который проверяет СОДЕРЖИМОЕ поля, а не диапазон числа. Остальные
        // проверки этого инструмента нули проходят: «позиция в сетке (0,0)» была зелёной ровно до
        // того дня, когда Positioned замерили. Здесь нулю пройти нечем — проекция обязана попасть
        // в конкретную точку экрана, и мимо неё промахивается любая ложь о камере.
        Head("7. КАМЕРА");

        CheckCamera(ingameState, player, process);

        return _failed > 0 ? ExitGarbage : ExitPlausible;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // Камера. Проверяется ТЕМ ЖЕ кодом, которым ей пользуется плагин: Camera.WorldToScreen, а не
    // собственной арифметикой инструмента. Иначе проверялись бы числа, а не путь до них.
    //
    // Главный критерий — проекция позиции игрока. В PoE камера следует за персонажем, поэтому его
    // экранный X обязан совпадать с горизонтальным ЦЕНТРОМ экрана в ЛЮБОЙ момент: стоит он, идёт
    // или дерётся. Замер 2026-09-17 дал ровно центр (ошибка 0.0000 px), и тот же центр вышел
    // 2026-09-16 в другой зоне при другой позиции — значит это инвариант, а не удача одной точки.
    //
    // Чем это ловит регресс. Прежние смещения давали Width = 0, отчего HalfWidth = 0 и
    // WorldToScreen возвращал X = (cord.X + 1) * 0 — тождественный ноль для любой точки мира.
    // Ноль проходит любую проверку диапазона и не прошёл бы эту: |0 - 1280| = 1280 px.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private static void CheckCamera(IngameState ingameState, Entity player, Process process)
    {
        var camera = ingameState == null ? null : Safe(() => ingameState.Camera, out _);

        if (camera == null || !IsPointer(camera.Address))
        {
            Row("Camera.Address", Hex(camera?.Address ?? 0), "указатель 0x10000..0x7FFFFFFFFFFF", Verdict.Bad);
            return;
        }

        Row("Camera.Address", Hex(camera.Address), "указатель 0x10000..0x7FFFFFFFFFFF", Verdict.Ok);

        var width = SafeStruct(() => camera.Width, out var widthErr) ?? 0;
        var height = SafeStruct(() => camera.Height, out _) ?? 0;

        // Размер клиентской области у ОС — факт, известный ВНЕ памяти процесса, и потому годный
        // в контроль. Если окна нет (свёрнуто, ещё не создано), проверка вырождается в диапазон,
        // и это сказано в самой строке, а не спрятано за зелёным «ok».
        var osWidth = 0;
        var osHeight = 0;

        try
        {
            if (process.MainWindowHandle != IntPtr.Zero && GetClientRect(process.MainWindowHandle, out var rc))
            {
                osWidth = rc.Right - rc.Left;
                osHeight = rc.Bottom - rc.Top;
            }
        }
        catch
        {
            // Диагностика не имеет права ронять вызывающего: окно могло исчезнуть между вызовами.
        }

        if (osWidth > 0 && osHeight > 0)
            Row("размер экрана", widthErr ?? $"{width} x {height}",
                $"{osWidth} x {osHeight} — столько же, сколько у окна игры по GetClientRect",
                width == osWidth && height == osHeight ? Verdict.Ok : Verdict.Bad);
        else
            Row("размер экрана", widthErr ?? $"{width} x {height}",
                "обе стороны 320..16000 (окно игры не опрошено, сверять не с чем)",
                width >= 320 && width <= 16000 && height >= 320 && height <= 16000 ? Verdict.Ok : Verdict.Bad);

        var zNear = SafeStruct(() => camera.ZNear, out _) ?? 0f;
        var zFar = SafeStruct(() => camera.ZFar, out _) ?? 0f;

        Row("плоскости отсечения", $"ближняя {zNear:F2}, дальняя {zFar:F2}",
            "0 < ближняя < дальняя < 100000",
            zNear > 0 && zFar > zNear && zFar < 100000 ? Verdict.Ok : Verdict.Bad);

        var camPos = SafeStruct(() => camera.Position, out var camPosErr);

        if (player == null || !IsPointer(player.Address))
        {
            Row("проекция позиции игрока", "игрок не прочитан", "экранный X равен центру экрана", Verdict.Skip);
            return;
        }

        var worldPos = SafeStruct(() => player.Pos, out var posErr);

        if (!worldPos.HasValue)
        {
            Row("проекция позиции игрока", $"ошибка: {posErr}", "экранный X равен центру экрана", Verdict.Skip);
            return;
        }

        // Положение камеры проверяется ПОСЛЕ позиции игрока, потому что сверяется с ней: камера
        // висит над персонажем, а не в другом конце зоны. Это слабый критерий (он ловит мусор,
        // но не сдвиг на несколько единиц) — и назван слабым прямо в строке ожидания.
        if (camPos.HasValue)
        {
            var d = camPos.Value - worldPos.Value;
            var dist = (float) Math.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z);
            var finite = !float.IsNaN(dist) && !float.IsInfinity(dist);
            Row("положение камеры", $"({camPos.Value.X:F1}, {camPos.Value.Y:F1}, {camPos.Value.Z:F1}), " +
                                    $"до игрока {dist:F0}",
                "конечные числа и до игрока < 5000 (слабый критерий: ловит мусор, не сдвиг)",
                finite && dist < 5000f ? Verdict.Ok : Verdict.Bad);
        }
        else
        {
            Row("положение камеры", $"ошибка: {camPosErr}", "конечные числа", Verdict.Bad);
        }

        var screen = SafeStruct(() => camera.WorldToScreen(worldPos.Value), out var projErr);

        if (!screen.HasValue)
        {
            Row("проекция позиции игрока", $"ошибка: {projErr}", "экранный X равен центру экрана", Verdict.Bad);
            return;
        }

        var sx = screen.Value.X;
        var sy = screen.Value.Y;
        var centreX = width * 0.5f;

        // Допуск 15% ширины, и это число ЗАМЕРЕНО, а не выбрано из осторожности.
        //
        // Камера PoE следует за персонажем СО СГЛАЖИВАНИЕМ: на бегу он действительно не в центре
        // экрана. Наблюдение CamCap --watch на живом клиенте (2026-09-17, два окна разного размера,
        // около 2000 обрамлённых отсчётов, шаг до 97 единиц мира за кадр) дало: в покое ошибка
        // 0.000 px, медиана в движении 0.000 px, 95-й процентиль 0.001 px, а НАИБОЛЬШИЙ выброс —
        // 4.8% и 5.1% ширины экрана в двух прогонах.
        //
        // Прежний допуск 1% был бы красным на ИСПРАВНОЙ камере просто оттого, что игрок шёл, а гейт
        // запускают когда угодно. Ложная тревога здесь хуже пропуска: гейт, который краснеет сам по
        // себе, перестают читать. 15% оставляет тройной запас над замеренным выбросом и всё равно
        // ловит реальную поломку с запасом больше чем втрое: при Width = 0 промах равен ПОЛОВИНЕ
        // ширины (50%), при чужой матрице — сотни пикселей.
        var tolerance = Math.Max(8f, width * 0.15f);
        var onScreen = sy >= 0 && sy <= height && !float.IsNaN(sx) && !float.IsNaN(sy);

        Row("проекция позиции игрока", $"({sx:F1}, {sy:F1})",
            $"X в пределах {tolerance:F0} px от центра {centreX:F0}, Y на экране",
            onScreen && Math.Abs(sx - centreX) <= tolerance ? Verdict.Ok : Verdict.Bad);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr handle, out Rect rect);

    // ── Сырой результат паттерн-скана ────────────────────────────────────────────────────────────
    //
    // РЕФЛЕКСИЯ, и другого пути нет. Вопрос «нашёлся ли паттерн» решается только сырым выводом
    // Memory.FindPatterns, а сигнатуры лежат в Offsets как private static readonly Pattern и наружу
    // не отдаются: DoPatternScans сразу превращает найденные смещения в производные якоря и сырой
    // массив выбрасывает. Поэтому поля достаём отражением. Правкой Core это лечилось бы одной
    // строкой (сделать поля internal + [InternalsVisibleTo]), но Core трогать нельзя.
    //
    // Вызов FindPatterns — публичный и только читающий; цена — второй проход по образу (~секунда).
    // Оговорка честности: паттерн, чьё настоящее совпадение стоит ровно по смещению 0, неотличим от
    // ненайденного. На практике нулевой RVA — это заголовок PE, кода там нет.
    private static Dictionary<string, long> ReportRawPatterns(Memory memory, long moduleSize)
    {
        List<IPattern> patterns;

        try
        {
            patterns = typeof(Offsets)
                .GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public)
                .Where(f => typeof(IPattern).IsAssignableFrom(f.FieldType))
                .Select(f => f.GetValue(null) as IPattern)
                .Where(p => p != null)
                .ToList();
        }
        catch (Exception e)
        {
            Row("сырой скан сигнатур", $"рефлексия не удалась: {e.GetType().Name}", "поля-паттерны видны в Offsets", Verdict.Skip);
            return null;
        }

        if (patterns.Count == 0)
        {
            Row("сырой скан сигнатур", "полей-паттернов не найдено", "Offsets хранит сигнатуры полями", Verdict.Skip);
            return null;
        }

        long[] hits;

        try
        {
            hits = memory.FindPatterns(patterns.ToArray());
        }
        catch (Exception e)
        {
            Row("сырой скан сигнатур", $"ошибка: {e.GetType().Name}: {Oneline(e.Message)}", "скан проходит без исключения", Verdict.Bad);
            return null;
        }

        var result = new Dictionary<string, long>(StringComparer.Ordinal);

        for (var i = 0; i < patterns.Count && i < hits.Length; i++)
        {
            var hit = hits[i];
            result[patterns[i].Name] = hit;

            Row($"сигнатура «{patterns[i].Name}»", hit == 0 ? "0 — НЕ НАЙДЕНА" : Hex(hit),
                $"0 < смещение < {Hex(moduleSize)}",
                hit > 0 && hit < moduleSize ? Verdict.Ok : Verdict.Bad);
        }

        return result;
    }

    // ── Поиск процесса ───────────────────────────────────────────────────────────────────────────
    //
    // Core.FindPoe() публичен, но использовать его здесь нельзя: при НЕСКОЛЬКИХ клиентах он зовёт
    // приватный ChooseSingleProcess, а тот показывает WinForms MessageBox и блокирует поток — в
    // консольном инструменте это тупик. Поэтому отбор процессов повторён здесь: публичных
    // Offsets.Regular/Steam/Korean для этого достаточно, Core не тронут.
    // При нескольких клиентах молча берём первый и честно печатаем, что их было больше одного.
    private static bool FindGameProcess(out Process process, out Offsets offsets, out string error)
    {
        process = null;
        offsets = null;
        error = null;
        _exeNameIsKnown = false;

        var candidates = new List<(Process proc, Offsets off, bool exact)>();

        foreach (var variant in new[] { Offsets.Regular, Offsets.Steam, Offsets.Korean })
        {
            try
            {
                foreach (var p in Process.GetProcessesByName(variant.ExeName))
                {
                    candidates.Add((p, variant, true));
                }
            }
            catch (Exception e)
            {
                error = $"перечисление процессов \"{variant.ExeName}\" не удалось: {e.GetType().Name}: {Oneline(e.Message)}";
                return false;
            }
        }

        // Запасной поиск — не удобство, а условие того, что инструмент вообще отвечает.
        // На ЭТОЙ машине установлен корейский клиент Daum, и его процесс называется
        // PathOfExile_KG, тогда как Offsets.Korean.ExeName в форке — "Pathofexile_x64_KG".
        // При точном сравнении имён инструмент печатал «игра не найдена» и выходил с кодом 2
        // при ЗАПУЩЕННОЙ игре, то есть молчал ровно там, где обязан отвечать. Несовпадение имени
        // само по себе — уже устаревшее число форка, и ниже оно печатается отдельной строкой
        // как МУСОР; но приложиться к процессу и измерить остальное это не мешает.
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
                    // Процесс мог закончиться прямо во время перечисления — не повод падать.
                    continue;
                }

                if (name.IndexOf(ExeNameFragment, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                // Лаунчер — не клиент: у него нет ни образа игры, ни игрового состояния.
                if (name.IndexOf("launcher", StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                // ExeName нигде, кроме поиска процесса, не используется (проверено по Core), а из
                // остального в Offsets различается только IgsOffset: 0x28 у Steam, 0 у Regular и
                // Korean. Поэтому для неизвестного имени вариант выбирается по признаку Steam.
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

        // Точное совпадение имени всегда предпочтительнее найденного запасным поиском.
        var ordered = candidates.OrderByDescending(c => c.exact).ToList();

        if (ordered.Count > 1)
            Console.WriteLine($"  найдено клиентов: {ordered.Count}; берём первый (диалог выбора в консоли неуместен).");

        process = ordered[0].proc;
        offsets = ordered[0].off;
        _exeNameIsKnown = ordered[0].exact;
        _processFound = true;
        return true;
    }

    // ── Обход списка сущностей ───────────────────────────────────────────────────────────────────
    //
    // EntityList.CollectEntities — корутина (IEnumerator) и требует EntityCollectSettingsContainer с
    // пулом потоков, кэшами и счётчиком версий: тянуть это в консольный однопроходный инструмент
    // значит тянуть половину рантайма. Публичного «просто дай адреса» в Core нет, поэтому обход
    // дерева повторён здесь ОДИН-В-ОДИН с Core/PoEMemory/MemoryObjects/EntityList.cs — включая его
    // особенность, что node.Entity берётся от ПРЕДЫДУЩЕГО узла. Повторяем как есть намеренно:
    // цифра должна совпадать с той, что получил бы сам форк, а не быть «лучше» его собственной.
    // Границы 0x100000000..0x7F0000000000 и лимит в 10000 итераций — тоже оттуда.
    private static List<long> WalkEntityList(Memory memory, long listAddress, out string error)
    {
        error = null;

        try
        {
            var result = new List<long>(1024);
            var seen = new HashSet<long>();
            var queue = new Queue<long>(256);

            var root = memory.Read<long>(listAddress + 0x8);

            if (!IsPointer(root))
            {
                error = $"корень дерева {Hex(root)} не похож на указатель";
                return null;
            }

            queue.Enqueue(root);
            var node = memory.Read<EntityListOffsets>(root);
            queue.Enqueue(node.FirstAddr);
            queue.Enqueue(node.SecondAddr);

            var loops = 0;

            while (queue.Count > 0 && loops < 10000)
            {
                loops++;
                var next = queue.Dequeue();

                if (!seen.Add(next))
                    continue;

                if (next == root || next == 0)
                    continue;

                var entityAddress = node.Entity;

                if (entityAddress > 0x100000000L && entityAddress < 0x7F0000000000L)
                    result.Add(entityAddress);

                node = memory.Read<EntityListOffsets>(next);
                queue.Enqueue(node.FirstAddr);
                queue.Enqueue(node.SecondAddr);
            }

            if (loops >= 10000)
                Console.WriteLine("  обход упёрся в лимит 10000 итераций (как и сам форк) — список неполон.");

            return result;
        }
        catch (Exception e)
        {
            error = $"{e.GetType().Name}: {Oneline(e.Message)}";
            return null;
        }
    }

    private static void PrintNearestEntities(IngameData data, Entity player, List<long> addresses)
    {
        var playerGrid = player == null ? null : SafeStruct(() => player.GridPosNum, out _);
        var sampled = new List<(long address, string path, float dist)>();

        foreach (var address in addresses)
        {
            // Достаточная выборка: нам нужны ближайшие, а не полный разбор всей зоны — а каждый
            // разобранный путь метаданных это несколько чтений чужой памяти.
            if (sampled.Count >= 400)
                break;

            string path;
            System.Numerics.Vector2? grid;

            try
            {
                // GetObject — публичный метод RemoteMemoryObject, и data годится как «фабрика»:
                // сеттер Address у RemoteMemoryObject защищённый, извне сущность иначе не собрать.
                var entity = data.GetObject<Entity>(address);
                TrySet(() => entity.IsValid = true);
                path = Safe(() => entity.Path, out _) ?? "";
                grid = SafeStruct(() => entity.GridPosNum, out _);
            }
            catch
            {
                continue;
            }

            var dist = playerGrid.HasValue && grid.HasValue
                ? System.Numerics.Vector2.Distance(playerGrid.Value, grid.Value)
                : float.MaxValue;

            sampled.Add((address, path, dist));
        }

        if (sampled.Count == 0)
        {
            Row("ближайшие сущности", "ни одной не материализовалось", "хотя бы одна читается", Verdict.Bad);
            return;
        }

        var nearest = sampled.OrderBy(x => x.dist).Take(5).ToList();
        var goodPaths = 0;

        Console.WriteLine();
        Console.WriteLine("  ближайшие сущности (адрес / дистанция в ячейках / путь метаданных):");

        foreach (var item in nearest)
        {
            var ok = IsMetadataPath(item.path);
            if (ok) goodPaths++;

            var distText = item.dist >= float.MaxValue
                ? "н/д"
                : item.dist.ToString("F1", CultureInfo.InvariantCulture);

            Console.WriteLine($"    {Hex(item.address)}  {distText,8}  {Mark(ok ? Verdict.Ok : Verdict.Bad)} {Oneline(item.path)}");
        }

        Console.WriteLine();

        Row("пути метаданных у ближайших", $"{goodPaths} из {nearest.Count} валидны",
            "каждый начинается с \"Metadata/\"", goodPaths == nearest.Count ? Verdict.Ok : Verdict.Bad);
    }

    // ── Правдоподобие ────────────────────────────────────────────────────────────────────────────

    private static bool IsPointer(long value)
    {
        return value >= MinUserPointer && value <= MaxUserPointer;
    }

    private static Verdict PointerVerdict(long value)
    {
        return IsPointer(value) ? Verdict.Ok : Verdict.Bad;
    }

    // Строка, прочитанная по верному смещению, — это короткий печатный ASCII-текст. Мусор выдаёт
    // себя либо пустотой, либо непечатными байтами, либо длиной «на всю прочитанную простыню».
    private static Verdict TextVerdict(string value, int minLength, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return Verdict.Bad;
        if (value.Length < minLength || value.Length > maxLength) return Verdict.Bad;

        foreach (var c in value)
        {
            if (c < 0x20 || c > 0x7E) return Verdict.Bad;
        }

        return Verdict.Ok;
    }

    private static bool IsMetadataPath(string path)
    {
        return !string.IsNullOrEmpty(path) &&
               path.StartsWith("Metadata/", StringComparison.Ordinal) &&
               TextVerdict(path, 10, 200) == Verdict.Ok;
    }

    private static Verdict MetadataVerdict(string path)
    {
        return IsMetadataPath(path) ? Verdict.Ok : Verdict.Bad;
    }

    // Общее правило для HP/маны/ES: текущее не больше максимума, максимум в человеческих пределах.
    // allowZeroMax — для энергощита, которого у билда может законно не быть.
    private static Verdict PoolVerdict(int? current, int? max, bool allowZeroMax)
    {
        if (!current.HasValue || !max.HasValue) return Verdict.Bad;
        if (max.Value < (allowZeroMax ? 0 : 1) || max.Value > 100000) return Verdict.Bad;
        if (current.Value < 0 || current.Value > max.Value) return Verdict.Bad;

        return Verdict.Ok;
    }

    // ── Печать ───────────────────────────────────────────────────────────────────────────────────

    private static void Head(string title)
    {
        Console.WriteLine();
        Console.WriteLine(title);
        Console.WriteLine(new string('-', 100));
    }

    private static void Row(string name, string value, string rule, Verdict verdict)
    {
        switch (verdict)
        {
            case Verdict.Ok:
                _passed++;
                break;
            case Verdict.Bad:
                _failed++;
                break;
            default:
                _skipped++;
                break;
        }

        Console.WriteLine($"  {Pad(name, 28)} {Pad(Oneline(value), 34)} {Mark(verdict)}  ожидаем: {rule}");
    }

    private static string Mark(Verdict verdict)
    {
        switch (verdict)
        {
            case Verdict.Ok: return "[ ok    ]";
            case Verdict.Bad: return "[ МУСОР ]";
            default: return "[  н/д  ]";
        }
    }

    private static void Summary()
    {
        Console.WriteLine();
        Console.WriteLine(new string('=', 100));
        Console.WriteLine($"ИТОГО: правдоподобно {_passed}, мусор {_failed}, не проверялось {_skipped}.");

        if (_processFound && !_exeNameIsKnown)
        {
            Console.WriteLine("ОТДЕЛЬНО: имя процесса игры не совпало ни с одной строкой Offsets.*.ExeName —");
            Console.WriteLine("это расхождение форка с установленной игрой, и оно не зависит от всего остального.");
        }

        if (_notInGame)
        {
            Console.WriteLine("ОГОВОРКА: клиент был не в игре (меню / выбор персонажа / загрузка). Строки про зону,");
            Console.WriteLine("игрока и сущности в этом состоянии помечаются мусором законно — перезапустите");
            Console.WriteLine("инструмент, стоя персонажем в зоне, иначе вердикт про смещения делать не на чем.");
        }
    }

    private static string Pad(string s, int width)
    {
        if (s == null) s = "";
        if (s.Length >= width) return s.Substring(0, Math.Max(0, width - 1)) + "~";

        return s.PadRight(width);
    }

    private static string Hex(long value)
    {
        return value < 0 ? $"-0x{-value:X}" : $"0x{value:X}";
    }

    // Значение из памяти может содержать переводы строк и прочий мусор — таблица от этого
    // разъезжается ровно там, где её собирались читать глазами.
    private static string Oneline(string s)
    {
        if (string.IsNullOrEmpty(s)) return s ?? "";

        var sb = new StringBuilder(s.Length);

        foreach (var c in s)
        {
            sb.Append(c == '\r' || c == '\n' || c == '\t' ? ' ' : c);
        }

        return sb.ToString();
    }

    private static string Quote(string value, string error)
    {
        return error != null ? $"ошибка: {error}" : $"\"{value}\"";
    }

    // ── Безопасные чтения ────────────────────────────────────────────────────────────────────────
    //
    // Memory.ReadMem пробрасывает исключение наружу после логирования, а любая ссылка в цепочке может
    // оказаться мусором — поэтому КАЖДОЕ чтение идёт через эти обёртки. Смысл инструмента в том,
    // чтобы промах печатался строкой, а не стектрейсом.

    private static T Safe<T>(Func<T> read, out string error) where T : class
    {
        try
        {
            error = null;
            return read();
        }
        catch (Exception e)
        {
            error = $"{e.GetType().Name}: {Oneline(e.Message)}";
            return null;
        }
    }

    private static T? SafeStruct<T>(Func<T> read, out string error) where T : struct
    {
        try
        {
            error = null;
            return read();
        }
        catch (Exception e)
        {
            error = $"{e.GetType().Name}: {Oneline(e.Message)}";
            return null;
        }
    }

    private static void TrySet(Action action)
    {
        try
        {
            action();
        }
        catch
        {
            // Не критично: без IsValid позиции вернут кэш (нули), и это будет видно в таблице как мусор.
        }
    }
}
