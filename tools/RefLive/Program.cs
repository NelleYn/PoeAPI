using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text;

namespace ExileApi.Tools.RefLive
{
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // RefLive — спрашивает АДРЕСА подобъектов у эталонного форка, исполняя его код.
    //
    // Печатает то и только то, что нужно для восстановления одного смещения: адрес объекта-владельца
    // и адреса его подобъектов. Числа — как есть. Ничего не «чинится» и не подгоняется.
    //
    // Соседний инструмент tools/FindOffset превращает эти адреса в смещение:
    //   FindOffset.exe --find --ingame-state --len 0x2000 --value <адрес отсюда>
    //
    // КЛЮЧИ
    //   (без ключей)             адреса подобъектов IngameData и признаки зоны
    //   --as <Тип> <адрес>       натравить ПАРСЕР ЭТАЛОНА на произвольный адрес и напечатать свойства
    //   --as <Тип> --at <адрес>  то же самое, если адрес удобнее отдельным ключом
    //   --only <подстрока>       в режиме --as печатать только свойства, чьё имя содержит подстроку
    //                            (регистр не важен). Без фильтра длинный объект не прочитать: печать
    //                            идёт по алфавиту и уезжает за экран.
    //   --entities [N]           перечислить сущности СВОИМ обходом EntityList; N — сколько напечатать
    //   --harvest [N]            то же + стык «имя компонента у эталона ↔ наш nameId» по адресу
    //   --nodes <N>              жёсткий предел числа узлов при обходе списка (по умолчанию 2000)
    //   --help                   эта справка
    //
    // Почему --entities и --harvest идут СВОИМ обходом, а не спрашивают эталон: перечисление
    // сущностей у эталона — корутина CollectEntities(...) (возвращает IEnumerator и рассчитана на
    // хост с потоками и настройками). Свойства, которое отдало бы готовый список, у него нет,
    // раскручивать чужую корутину без хоста дороже и менее предсказуемо, чем пройти связный список
    // самим. Поэтому у эталона берётся только АДРЕС объекта EntityList, а узлы читаются нами.
    //
    // ПРОВЕНАНС ЧИСЕЛ В ЭТИХ ДВУХ РЕЖИМАХ. Смещения внутри сущности и слот-таблицы взяты из
    // docs/api/survey-2026-09-16.md, но с тех пор их статус РАЗОШЁЛСЯ, и печатать над ними одну
    // общую оговорку больше нельзя:
    //   * раскладка сущности (+0x08 details, +0x10 вектор компонентов, +0x88 Id), строка пути и
    //     слот-таблица (details+0x28, lookup+0x40) — ПЕРЕПОДТВЕРЖДЕНЫ обходом зоны и УЖЕ ВНЕСЕНЫ
    //     в структуры форка (EntityOffsets, ComponentLookupOffsets);
    //   * поле +0x70 — ОПРОВЕРГНУТО в прежнем толковании «InventoryId», см. EntFieldAt70 ниже.
    // Сам инструмент по-прежнему ничего не подтверждает: он печатает рядом чужой (эталонный) и наш
    // ответ, чтобы их можно было сверить глазом. Разница в том, что теперь эта сверка стережёт уже
    // применённые числа, а не проверяет кандидатов.
    //
    // Коды возврата: 0 — адреса напечатаны; 1 — эталон приложился, но подобъекта нет (вне зоны);
    //                2 — читать нечего: игра не запущена, эталон не сконструировался или у его
    //                    Memory нет метода чтения, на который рассчитан обход.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    internal static class Program
    {
        private const int ExitOk = 0;
        private const int ExitEmpty = 1;
        private const int ExitNothingToRead = 2;

        private const string ExeNameFragment = "pathofexile";

        // Сколько элементов коллекции печатать. Было 8 (и 16 в списках) — этого не хватало даже на
        // один инвентарь, а обрезанная печать неотличима от «поля нет».
        private const int ItemPrintLimit = 32;

        // Сколько ШАГОВ разрешено сделать чужому перечислителю. Это НЕ предел печати: печать режет
        // вывод, а этот предел режет работу, и путать их нельзя — именно этой путаницей и был
        // испорчен DumpEnumerable. Там стоял `continue` после ItemPrintLimit: печать прекращалась,
        // а MoveNext продолжал вызываться, пока чужой перечислитель не остановится САМ. У ленивого
        // перечислителя, читающего чужую память по устаревшему указателю, такого «сам» может не
        // случиться никогда — ровно так обход однажды разогнал процесс до ~4 ГиБ, то есть шапка
        // файла обещала защиту, которой в этом месте не было.
        //
        // Число взято с запасом относительно всего, что здесь осмысленно перечислять: самые длинные
        // коллекции эталона — сущности зоны и позиции инвентарей, это сотни элементов, а не тысячи.
        // Предел существует не ради точности итога, а ради того, чтобы обход заведомо КОНЧИЛСЯ.
        private const int EnumerationStepLimit = 4096;

        // Пределы обхода. Ловушка, за которую уже заплачено: обход по устаревшему указателю
        // разогнал процесс до ~4 ГиБ. Любой предел здесь обязан быть ЖЁСТКИМ и ВИДИМЫМ в выводе —
        // молча оборванный обход выглядит как «в зоне мало сущностей», то есть как ложный замер.
        private const int DefaultNodeLimit = 2000;
        private const int DefaultEntityPrintLimit = 64;
        private const int DefaultHarvestPrintLimit = 32;

        // Слотов в таблице компонентов обследование видело 8 и 16 (степень двойки). Предел взят с
        // большим запасом и существует не ради точности, а ради того, чтобы мусорный вектор не увёл
        // чтение в мегабайты.
        private const int SlotCountLimit = 1024;

        // Длиннее осмысленного пути метаданных не бывает; ограничение защищает от мусорной длины.
        private const int PathCharLimit = 512;

        private static string _refDir;

        // База модуля игры. Нужна ради RVA: абсолютная vtable меняется с каждым запуском
        // (ASLR), RVA — нет, и только RVA годится в таблицу, которую хранят в коде.
        private static long _moduleBase;

        // Аргументы нужны глубоко внутри Run(), а он вызывается без параметров: обработчик
        // разрешения сборок должен быть поставлен ДО первого обращения к типам эталона.
        private static string[] _args = Array.Empty<string>();

        private static int Main(string[] args)
        {
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; }
            catch { /* консоли может не быть вовсе — не повод падать */ }

            _args = args ?? Array.Empty<string>();

            // Справка отвечает ДО поиска эталона: спросить «какие тут ключи» можно и на машине,
            // где эталона нет вовсе.
            if (Has("--help") || Has("-h") || Has("/?"))
            {
                PrintHelp();
                return ExitOk;
            }

            _refDir = args.Length > 0 && !args[0].StartsWith("--")
                ? args[0]
                : typeof(Program).Assembly
                    .GetCustomAttributes<AssemblyMetadataAttribute>()
                    .FirstOrDefault(a => a.Key == "RefDir")?.Value;

            if (string.IsNullOrWhiteSpace(_refDir) || !Directory.Exists(_refDir))
            {
                Console.WriteLine($"ОШИБКА: каталог эталона не найден: \"{_refDir}\".");
                Console.WriteLine("  передайте его первым аргументом.");
                return ExitNothingToRead;
            }

            // Зависимости ExileCore эталона (SharpDX, ImGui.NET, Newtonsoft, ...) лежат рядом с ним,
            // а не рядом с этим exe. Обработчик ставится ДО первого обращения к типам эталона —
            // поэтому вся работа вынесена в отдельный невстраиваемый метод.
            AssemblyLoadContext.Default.Resolving += ResolveFromRefDir;

            Console.WriteLine("RefLive — адреса подобъектов по данным ЭТАЛОННОГО форка. ТОЛЬКО ЧТЕНИЕ.");
            Console.WriteLine($"эталон: {_refDir}");
            Console.WriteLine();

            try
            {
                return Run();
            }
            catch (Exception e)
            {
                Console.WriteLine($"СБОЙ: {e.GetType().Name}: {Oneline(e.Message)}");
                foreach (var frame in (e.StackTrace ?? "").Split('\n').Take(8))
                    Console.WriteLine($"  │ {Oneline(frame)}");
                return ExitNothingToRead;
            }
        }

        private static Assembly ResolveFromRefDir(AssemblyLoadContext ctx, AssemblyName name)
        {
            var path = Path.Combine(_refDir, name.Name + ".dll");
            return File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static int Run()
        {
            var process = FindGameProcess(out var exact);

            try { _moduleBase = process?.MainModule?.BaseAddress.ToInt64() ?? 0; }
            catch { _moduleBase = 0; }

            if (process == null)
            {
                Console.WriteLine("игра не найдена — запустите Path of Exile и повторите.");
                return ExitNothingToRead;
            }

            Console.WriteLine($"процесс  {process.ProcessName} (pid {process.Id})" +
                              (exact ? "" : $"  [найден по подстроке \"{ExeNameFragment}\"]"));

            // Конструкторы эталона отличаются от наших, и это главное, ради чего инструмент вообще
            // возможен: Memory сама делает паттерн-скан по СВОИМ сигнатурам, вариант оффсетов ей не
            // передают. Mutex и GameController — служебные аргументы хоста; мьютекс свой, а хоста
            // здесь нет, и передаётся null: если конструктору он действительно нужен, это увидим
            // исключением, а не молчаливо неверными числами.
            var settings = new ExileCore.CoreSettings();
            var mutex = new System.Threading.Mutex(false, "RefLive-" + Environment.ProcessId);

            // Конструктор Memory у эталона не public, поэтому вызывается рефлексией. Это не обход
            // защиты, а единственный способ создать объект чужой сборки, не переписывая её: сама
            // сборка ничего не скрывает, просто рассчитана на своего хоста (Loader.exe).
            var memory = (ExileCore.Shared.Interfaces.IMemory) Construct(
                typeof(ExileCore.Memory), new object[] {process, mutex, settings});

            var game = (ExileCore.PoEMemory.MemoryObjects.TheGame) Construct(
                typeof(ExileCore.PoEMemory.MemoryObjects.TheGame),
                new object[] {memory, new ExileCore.Shared.Cache.Cache(), settings, null});

            // Режим --as <Тип> <адрес>: натравить ПАРСЕР ЭТАЛОНА на произвольный адрес и напечатать,
            // что он оттуда вычитал. Нужен там, где оракул не может назвать адрес сам (его свойство
            // отдаёт null), но опознать объект по содержимому всё ещё можно — чужой разбор,
            // написанный без оглядки на нашу гипотезу, либо даёт осмысленные поля, либо нет.
            var asType = Arg(_args, "--as");

            if (asType != null)
                return Describe(game, asType, Arg(_args, "--at") ?? Arg(_args, "--as", 2));

            Console.WriteLine();
            Console.WriteLine("АДРЕСА ПО ДАННЫМ ЭТАЛОНА");
            Console.WriteLine(new string('-', 100));

            var igs = game.IngameState;
            Report("TheGame", game.Address);
            Report("IngameState", igs?.Address ?? 0);

            if (igs == null)
            {
                Console.WriteLine();
                Console.WriteLine("эталон не отдал IngameState — читать дальше нечего.");
                return ExitEmpty;
            }

            var data = igs.Data;
            var server = igs.ServerData;
            var ui = igs.IngameUi;

            Report("IngameState.Data", data?.Address ?? 0);
            Report("IngameState.ServerData", server?.Address ?? 0);
            Report("IngameState.IngameUi", ui?.Address ?? 0);
            Report("IngameState.UIRoot", igs.UIRoot?.Address ?? 0);
            Report("IngameState.Camera", igs.Camera?.Address ?? 0);

            if (data == null || data.Address == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Data пуст. Это состояние, а не отказ: на логин-экране и в выборе");
                Console.WriteLine("персонажа его нет и у эталона. Зайдите в зону и повторите.");

                if (Has("--entities") || Has("--harvest"))
                    Console.WriteLine("  (запрошенный режим обхода сущностей без Data невыполним — обходить нечего.)");

                return ExitEmpty;
            }

            // Режимы обхода сущностей отвечают вместо общего отчёта: их вывод длинный, и мешать его
            // с картой смещений незачем. Адреса TheGame/IngameState/Data выше уже напечатаны — этого
            // хватает, чтобы понять, к чему именно приложились.
            if (Has("--entities") || Has("--harvest"))
                return RunEntityModes(game, memory, data, Has("--harvest"));

            // Признаки зоны печатаются ТОЛЬКО ради ответа на вопрос «мы точно в загруженной зоне»:
            // числами отсюда ничего не чинится, они принадлежат чужой раскладке.
            Console.WriteLine();
            Console.WriteLine("ПРИЗНАКИ ЗОНЫ (данные эталона, для контроля состояния)");
            Console.WriteLine(new string('-', 100));
            Safe("уровень зоны", () => data.CurrentAreaLevel.ToString());
            Safe("хэш зоны", () => "0x" + data.CurrentAreaHash.ToString("X"));
            Safe("список сущностей", () => data.EntityList == null ? "(null)" : "есть");

            // ── Адреса подобъектов IngameData ────────────────────────────────────────────────
            // Это вход в следующий слой. Каждое напечатанное число FindOffset ищет внутри окна,
            // начинающегося с data.Address, и отвечает смещением. Числа принадлежат ЭТОМУ запуску
            // игры: после перезапуска они будут другими, а смещение обязано остаться тем же.
            Console.WriteLine();
            Console.WriteLine($"АДРЕСА ВНУТРИ IngameData  (окно: --in 0x{data.Address:X})");
            Console.WriteLine(new string('-', 100));

            foreach (var member in SubObjectMembers)
                ReportMemberAddress(data, member);

            // ── Значения внутри IngameData ───────────────────────────────────────────────────────
            // Поле опознаётся не только указателем: уровень зоны и хэш — числа из другого источника,
            // и единственное совпадение такого числа в окне подтверждает поле содержимым, а не
            // раскладкой. Это самое сильное подтверждение, доступное без отладчика.
            Console.WriteLine();
            Console.WriteLine("ЗНАЧЕНИЯ ВНУТРИ IngameData  (искать как --find --size 4|8)");
            Console.WriteLine(new string('-', 100));

            foreach (var member in ValueMembers)
                ReportMemberValue(data, member);

            DumpStructMember(data, "Terrain");

            // MapStats измерим ТОЛЬКО в карте: вне карты эталон отдаёт пустой набор, и искать в окне
            // нечего. Печатается количество и пары: по ним опознаётся нативный массив, на который
            // указывает поле, — само поле находится как тройка First/Last/End нужного размера.
            DumpDictionaryMember(data, "MapStats");
            DumpDictionaryMember(data, "MapStatsVisible");

            // Порталы города: список объектов, у каждого свой адрес. Адреса и нужны — по ним
            // ищется вектор, в котором они лежат подряд.
            DumpListMember(data, "TownPortals");

            Console.WriteLine();
            Console.WriteLine("ДАЛЬШЕ: смещение внутри IngameState считает FindOffset —");
            Console.WriteLine($"  FindOffset.exe --find --ingame-state --len 0x2000 --value 0x{data.Address:X}");

            return ExitOk;
        }

        // Возвращает значение ключа из командной строки: Arg(args, "--as") — следующее слово,
        // Arg(args, "--as", 2) — второе после ключа (так читается "--as Тип адрес").
        private static string Arg(string[] args, string key, int offset = 1)
        {
            for (var i = 0; i < args.Length; i++)
                if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase) &&
                    i + offset < args.Length)
                    return args[i + offset];

            return null;
        }

        private static bool Has(string key) =>
            _args.Any(a => string.Equals(a, key, StringComparison.OrdinalIgnoreCase));

        // Числовой параметр ключа — необязательный: "--entities" и "--entities 200" одинаково
        // допустимы, а "--entities --harvest" означает, что числа не дали. Слово, начинающееся с
        // "--", числом не считается никогда, иначе следующий ключ молча съедался бы как значение.
        private static int ArgInt(string key, int fallback)
        {
            var text = Arg(_args, key);

            if (string.IsNullOrWhiteSpace(text) || text.StartsWith("--")) return fallback;

            var hex = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase);
            var body = hex ? text.Substring(2) : text;
            var style = hex ? NumberStyles.HexNumber : NumberStyles.Integer;

            int value;

            if (!int.TryParse(body, style, CultureInfo.InvariantCulture, out value) || value <= 0)
                return fallback;

            return value;
        }

        private static void PrintHelp()
        {
            Console.WriteLine("RefLive — адреса и содержимое подобъектов по данным ЭТАЛОННОГО форка. ТОЛЬКО ЧТЕНИЕ.");
            Console.WriteLine();
            Console.WriteLine("  RefLive.exe [каталог-эталона] [ключи]");
            Console.WriteLine();
            Console.WriteLine("  (без ключей)             адреса подобъектов IngameData и признаки зоны");
            Console.WriteLine("  --as <Тип> <адрес>       разобрать адрес ПАРСЕРОМ ЭТАЛОНА и напечатать свойства");
            Console.WriteLine("  --as <Тип> --at <адрес>  то же самое");
            Console.WriteLine("  --only <подстрока>       в режиме --as — только свойства с этой подстрокой в имени");
            Console.WriteLine($"  --entities [N]           перечислить сущности своим обходом EntityList (N, по умолчанию {DefaultEntityPrintLimit})");
            Console.WriteLine($"  --harvest [N]            то же + стык «имя компонента эталона ↔ наш nameId» (N, по умолчанию {DefaultHarvestPrintLimit})");
            Console.WriteLine($"  --nodes <N>              предел числа узлов при обходе списка (по умолчанию {DefaultNodeLimit})");
            Console.WriteLine("  --help                   эта справка");
            Console.WriteLine();
            Console.WriteLine("Смещения сущности и слот-таблицы, которыми пользуются --entities/--harvest,");
            Console.WriteLine("ПЕРЕПОДТВЕРЖДЕНЫ и внесены в структуры форка. Исключение — поле +0x70: его прежнее");
            Console.WriteLine("толкование «InventoryId» ОПРОВЕРГНУТО, печатается сырым и без имени.");
        }

        // Строит объект эталона нужного типа по заданному адресу и печатает его свойства.
        // Ничего не «подтверждает» само по себе: подтверждением служит ОСМЫСЛЕННОСТЬ прочитанного.
        private static int Describe(object game, string typeName, string addressText)
        {
            if (string.IsNullOrWhiteSpace(addressText))
            {
                Console.WriteLine("--as требует адрес: --as <Тип> <адрес>  (или --as <Тип> --at <адрес>)");
                return ExitNothingToRead;
            }

            var text = addressText.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? addressText.Substring(2)
                : addressText;

            if (!long.TryParse(text, System.Globalization.NumberStyles.HexNumber,
                    CultureInfo.InvariantCulture, out var address) || address <= 0)
            {
                Console.WriteLine($"--as: адрес \"{addressText}\" не разобран как HEX.");
                return ExitNothingToRead;
            }

            var type = FindRefType(game.GetType().Assembly, typeName);

            if (type == null)
            {
                Console.WriteLine($"--as: у эталона нет типа \"{typeName}\".");
                return ExitNothingToRead;
            }

            var generic = FindGetObject(game);

            if (generic == null)
            {
                Console.WriteLine("--as: у эталона не нашлось GetObject<T>(long).");
                return ExitNothingToRead;
            }

            object built;

            try { built = generic.MakeGenericMethod(type).Invoke(game, new object[] {address}); }
            catch (Exception e)
            {
                Console.WriteLine($"--as: построить {type.Name} по 0x{address:X} не удалось — {DescribeException(e)}");
                return ExitEmpty;
            }

            Console.WriteLine($"РАЗБОР ЭТАЛОНА: {type.FullName} по адресу 0x{address:X}");
            Console.WriteLine(new string('-', 100));

            if (built == null)
            {
                Console.WriteLine("  эталон вернул null.");
                return ExitEmpty;
            }

            // Фильтр по имени свойства. Нужен не для удобства: печать идёт по алфавиту, и одно
            // свойство на сотню строк выталкивает за экран всё, что стоит после него. Именно так
            // «пропадала» вся вторая половина ServerData.
            var only = Arg(_args, "--only");

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(x => x.GetIndexParameters().Length == 0)
                .OrderBy(x => x.Name)
                .ToArray();

            var matched = 0;

            foreach (var prop in props)
            {
                // Свойства-обратные ссылки (TheGame, M) увели бы печать в весь граф объектов.
                if (prop.Name == "TheGame" || prop.Name == "M") continue;

                if (only != null &&
                    prop.Name.IndexOf(only, StringComparison.OrdinalIgnoreCase) < 0) continue;

                matched++;

                object value;

                try { value = prop.GetValue(built); }
                catch (Exception e) { value = e; }

                if (value is System.Collections.IEnumerable list and not string)
                {
                    // Имя печатается ДО элементов: их теперь до 32, и без заголовка непонятно, чьи
                    // они. Итог — после, чтобы было видно, сколько элементов срезал предел.
                    Console.WriteLine($"  {prop.Name,-22} коллекция:");

                    DumpEnumerable(list, ItemPrintLimit, out var total, out var trouble);

                    Console.WriteLine("  " + new string(' ', 22) + $" всего: {total}" +
                                      (total > ItemPrintLimit ? $" (показаны первые {ItemPrintLimit})" : ""));

                    if (trouble != null)
                        Console.WriteLine("  " + new string(' ', 22) + " " + trouble);

                    continue;
                }

                Console.WriteLine($"  {prop.Name,-22} {Describe(value)}");
            }

            if (only != null)
                Console.WriteLine($"  --- фильтр --only \"{only}\": подошло свойств {matched} из {props.Length}");

            return ExitOk;
        }

        // Члены эталонного IngameData, чей АДРЕС нужен для восстановления смещения. Список — это
        // имена, а не числа: имена между форками совпадают, числа нет (docs/api/parity-measured.md).
        private static readonly string[] SubObjectMembers =
        {
            "LocalPlayer", "EntityList", "SleepingEntityList", "ServerData",
            "CurrentArea", "EnvironmentData", "LabyrinthData"
        };

        // Члены, чьё ЗНАЧЕНИЕ пришло из эталона и потому годится как независимая примета поля.
        private static readonly string[] ValueMembers =
        {
            "CurrentAreaLevel", "CurrentAreaHash", "EntitiesCount", "SleepingEntityCount",
            "AreaDimensions"
        };

        // Читает член эталона ПО ИМЕНИ. Свойство может бросить (объекта нет, страница не читается);
        // это ответ «пусто», а не отказ инструмента — печатается строкой, работа продолжается.
        private static object Member(object owner, string name, out string error)
        {
            error = null;

            const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic |
                                       BindingFlags.Instance | BindingFlags.FlattenHierarchy;

            var type = owner.GetType();

            try
            {
                var prop = type.GetProperty(name, Flags);
                if (prop != null) return prop.GetValue(owner);

                var field = type.GetField(name, Flags);
                if (field != null) return field.GetValue(owner);
            }
            catch (Exception e)
            {
                error = DescribeException(e);
                return null;
            }

            error = "нет такого члена у эталона";
            return null;
        }

        private static void ReportMemberAddress(object owner, string name)
        {
            var value = Member(owner, name, out var error);

            if (error != null)
            {
                Console.WriteLine($"  Data.{name,-22} {error}");
                return;
            }

            if (value == null)
            {
                Console.WriteLine($"  Data.{name,-22} 0x0   [пусто]");
                return;
            }

            var address = Member(value, "Address", out var inner);

            if (address is long a)
                Report("Data." + name, a);
            else
                Console.WriteLine($"  Data.{name,-22} {inner ?? "нет члена Address у " + value.GetType().Name}");
        }

        private static void ReportMemberValue(object owner, string name)
        {
            var value = Member(owner, name, out var error);
            Console.WriteLine($"  Data.{name,-22} {error ?? Describe(value)}");
        }

        // Структура печатается по полям: подтверждать её придётся по одному полю, а не целиком.
        private static void DumpStructMember(object owner, string name)
        {
            var value = Member(owner, name, out var error);

            if (error != null || value == null)
            {
                Console.WriteLine($"  Data.{name,-22} {error ?? "(null)"}");
                return;
            }

            Console.WriteLine($"  Data.{name,-22} {value.GetType().Name}:");

            foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                object inner;

                try { inner = field.GetValue(value); }
                catch (Exception e) { inner = e; }

                Console.WriteLine($"      .{field.Name,-18} {Describe(inner)}");
            }
        }

        // Список эталона печатается по элементам, с АДРЕСОМ каждого: именно адрес элемента ищется
        // потом в окне владельца — вектор хранит их подряд, и первый из них равен полю First.
        private static void DumpListMember(object owner, string name)
        {
            var value = Member(owner, name, out var error);

            if (error != null || value == null)
            {
                Console.WriteLine($"  Data.{name,-22} {error ?? "(null)"}");
                return;
            }

            if (value is not System.Collections.IEnumerable list)
            {
                Console.WriteLine($"  Data.{name,-22} не список: {value.GetType().Name}");
                return;
            }

            DumpEnumerable(list, ItemPrintLimit, out var total, out var trouble);

            Console.WriteLine($"  Data.{name,-22} элементов: {total}" +
                              (total > ItemPrintLimit ? $" (показаны первые {ItemPrintLimit})" : ""));

            if (trouble != null)
                Console.WriteLine("  " + new string(' ', 27) + " " + trouble);
        }

        // Печать элементов коллекции эталона, с АДРЕСОМ каждого: адрес — это то, что потом ищет
        // FindOffset, а текст элемента нужен лишь чтобы его опознать глазом.
        //
        // Защищён КАЖДЫЙ шаг по отдельности, и это не перестраховка, а плата за уже случившееся:
        // падало не свойство (оно было в try), а MoveNext и ToString ВНУТРИ перечисления. Одно
        // исключение уносило весь остаток печати, а так как свойства идут по алфавиту, пропажа
        // выглядела как «у объекта после буквы Mi ничего нет». Поэтому перечислитель, каждый шаг,
        // каждый Current и каждый ToString обёрнуты порознь, и обрыв печатается строкой, а не
        // роняет вызывающего.
        private static void DumpEnumerable(System.Collections.IEnumerable list, int limit,
                                           out int total, out string trouble)
        {
            total = 0;
            trouble = null;

            System.Collections.IEnumerator it = null;

            try { it = list.GetEnumerator(); }
            catch (Exception e)
            {
                trouble = "перечислитель не создан: " + DescribeException(e);
                return;
            }

            if (it == null)
            {
                trouble = "GetEnumerator() вернул null";
                return;
            }

            try
            {
                while (true)
                {
                    // ЖЁСТКИЙ ПРЕДЕЛ ИТЕРАЦИЙ, а не печати. Проверяется ДО MoveNext: шаг, который
                    // уводит в чужую память, должен быть не сделан, а не сделан-и-посчитан.
                    if (total >= EnumerationStepLimit)
                    {
                        trouble = $"!!! ПРЕДЕЛ ПЕРЕЧИСЛЕНИЯ {EnumerationStepLimit} ДОСТИГНУТ — обход ОБОРВАН. " +
                                  $"«всего: {total}» здесь НЕ ИТОГ, а нижняя оценка: коллекция либо длиннее " +
                                  "предела, либо зациклена/протухла (ленивый перечислитель читает чужую " +
                                  "память и может не остановиться сам).";
                        return;
                    }

                    bool moved;

                    try { moved = it.MoveNext(); }
                    catch (Exception e)
                    {
                        trouble = $"перечисление оборвалось после {total} элем.: {DescribeException(e)}";
                        return;
                    }

                    if (!moved) return;

                    total++;

                    // Печать кончилась, а перечисление продолжается — но теперь ограниченно: счётчик
                    // упрётся в EnumerationStepLimit выше. Досчитать элементы до конца полезно (итог
                    // и есть то, ради чего смотрят), но не любой ценой.
                    if (total > limit) continue;

                    object item;

                    try { item = it.Current; }
                    catch (Exception e) { item = e; }

                    string text;

                    try { text = Describe(item); }
                    catch (Exception e) { text = DescribeException(e); }

                    var address = item == null ? null : Member(item, "Address", out _);

                    Console.WriteLine(address is long a && a != 0
                        ? $"      [{total - 1}] 0x{a:X}  {text}"
                        : $"      [{total - 1}] {text}");
                }
            }
            finally
            {
                // Чужой Dispose тоже читает чужую память и тоже может бросить.
                try { (it as IDisposable)?.Dispose(); }
                catch { /* нечего добавить: элементы уже напечатаны */ }
            }
        }

        // Словарь эталона печатается парами «ключ=значение» в СЫРОМ виде: в памяти пара лежит как
        // два int подряд, и именно этим числом (value<<32 | key) пара опознаётся в дампе.
        private static void DumpDictionaryMember(object owner, string name)
        {
            var value = Member(owner, name, out var error);

            if (error != null || value == null)
            {
                Console.WriteLine($"  Data.{name,-22} {error ?? "(null)"}");
                return;
            }

            var dict = value as System.Collections.IDictionary;

            if (dict == null)
            {
                Console.WriteLine($"  Data.{name,-22} не словарь: {value.GetType().Name}");
                return;
            }

            Console.WriteLine($"  Data.{name,-22} пар: {dict.Count}  (байт в массиве: {dict.Count * 8})");

            var shown = 0;

            foreach (System.Collections.DictionaryEntry e in dict)
            {
                if (shown++ >= ItemPrintLimit) break;

                var key = Convert.ToInt64(Convert.ToInt32(e.Key));
                var val = Convert.ToInt64(Convert.ToInt32(e.Value));

                Console.WriteLine($"      {e.Key,-34} = {e.Value,-8} сырая пара 0x{(val << 32 | (key & 0xFFFFFFFFL)):X16}");
            }
        }

        // ═══ РЕЖИМЫ --entities и --harvest ═══════════════════════════════════════════════════════
        //
        // Раскладка сущности. Смещения пришли из docs/api/survey-2026-09-16.md, но статус у них
        // теперь РАЗНЫЙ, и это существенно для чтения вывода.
        //
        // EntDetails, EntComponents, EntId и смещения строки пути — ПЕРЕПОДТВЕРЖДЕНЫ обходом зоны
        // и ВНЕСЕНЫ в структуры форка (EntityOffsets.IdOffset = 0x88 и соседи). Инструмент печатает
        // прочитанное по ним рядом с ответом эталона не потому, что они под вопросом, а потому, что
        // эта сверка — единственный способ заметить, что они разъехались после патча игры.
        //
        // EntFieldAt70 — ОПРОВЕРГНУТО, и имя у него было раньше неверное (EntInventoryId).
        // Замерено: у Metadata/Chests/DarkPot2v2 по +0x70 лежит указатель ВНУТРЬ модуля игры
        // (0x00007FF792EFFF00), а uint32, который здесь читается, — просто младшая половина этого
        // указателя. Идентификатором инвентаря такое быть не может. Поле оставлено в выводе, потому
        // что его содержимое — и есть улика; но чем оно является на самом деле, НЕ УСТАНОВЛЕНО, и
        // осмысленного заголовка у него поэтому нет. Форк держит то же место как
        // EntityOffsets.InventoryIdRaw при InventoryIdMeasured = false.
        private const int EntDetails     = 0x08;  // EntityDetails*
        private const int EntComponents  = 0x10;  // StdVector указателей на компоненты: First/Last/End
        private const int EntFieldAt70   = 0x70;  // uint32; что это такое — НЕ УСТАНОВЛЕНО, см. выше
        private const int EntId          = 0x88;  // uint
        private const int DetailsPath    = 0x08;  // строка пути
        private const int DetailsPathLen = 0x18;  // длина пути В СИМВОЛАХ
        private const int DetailsLookup  = 0x28;  // корень слот-таблицы компонентов
        private const int LookupSlots    = 0x40;  // StdVector слотов по 8 байт внутри lookup

        // Двенадцать пар «имя компонента → nameId», снятые тем же обследованием. Здесь они нужны
        // ровно для одного: отделить в сводке новое от уже известного и показать расхождение, если
        // то же имя прочитается с другим номером. На чтение памяти не влияют никак.
        private static readonly (string Name, uint NameId)[] KnownNameIds =
        {
            ("Render", 0x101), ("BaseEvents", 0x114), ("Positioned", 0x11C), ("Animated", 0x151),
            ("Life", 0x15F), ("Player", 0x186), ("Pathfinding", 0x18F), ("Stats", 0x1A7),
            ("Actor", 0x1C6), ("Buffs", 0x24B), ("PlayerClass", 0x274), ("PetAi", 0x2AD)
        };

        // «Похож на канонический» — минимально осмысленная проверка указателя перед разыменованием.
        // Проверка «не ноль» тут бесполезна и уже стоила времени: Memory.Read<T> по мусорному адресу
        // возвращает default и НЕ бросает, а по незамеренному полю обычно лежит именно мусор, а не
        // ноль. Проверяется только то, что проверяется без замера: адрес в пользовательской половине
        // адресного пространства Win64 и выровнен на 8. Это не подтверждение поля — это отказ ходить
        // по заведомо не-указателю.
        private static bool LooksCanonical(long p) =>
            p >= 0x10000 && p < 0x00007FFFFFFFFFFFL && (p & 7) == 0;

        // Чтение чужого процесса идёт ТОЛЬКО через объект Memory эталона: процесс уже открыт им,
        // второй OpenProcess — лишняя сущность и лишний источник расхождений. Методы ищутся
        // рефлексией, а не вызываются по имени напрямую: сигнатуры чужой сборки могут не совпасть с
        // нашими, и тогда инструмент обязан СКАЗАТЬ, чего не нашёл, а не подменить догадкой.
        private sealed class RefMemory
        {
            private readonly object _memory;
            private readonly MethodInfo _readLong;
            private readonly MethodInfo _readUInt;
            private readonly MethodInfo _readBytes;

            // null, если всё нужное нашлось. Иначе — что именно отсутствует, дословно.
            public readonly string Complaint;

            // Что нашлось — печатается всегда, чтобы из вывода было видно, ЧЕМ читали.
            public readonly string Found;

            public RefMemory(object memory)
            {
                _memory = memory;

                _readLong = FindGenericRead(memory, typeof(long));
                _readUInt = FindGenericRead(memory, typeof(uint));
                _readBytes = FindByteRead(memory, out var bytesName);

                Complaint = _readLong != null && _readUInt != null
                    ? null
                    : "у Memory эталона не нашлось Read<T>(long) — читать нечем";

                Found = $"Read<long>: {(_readLong != null ? "есть" : "НЕТ")}, " +
                        $"Read<uint>: {(_readUInt != null ? "есть" : "НЕТ")}, " +
                        $"байты: {bytesName ?? "нет метода — читаем восьмёрками через Read<long>"}";
            }

            public bool Ready => Complaint == null;

            public long ReadLong(long addr)
            {
                if (_readLong == null) return 0;

                // Исключение здесь — не отказ инструмента, а ответ «не прочиталось»: диагностика не
                // имеет права ронять вызывающего.
                try { return _readLong.Invoke(_memory, new object[] {addr}) is long l ? l : 0; }
                catch { return 0; }
            }

            public uint ReadUInt(long addr)
            {
                if (_readUInt == null) return 0;

                try { return _readUInt.Invoke(_memory, new object[] {addr}) is uint u ? u : 0u; }
                catch { return 0u; }
            }

            // Байты. Если у эталона нет ни ReadMem, ни ReadBytes с (long,int), строка читается
            // восьмёрками через Read<long>: медленнее, зато не требует ничего сверх уже найденного.
            // Чужой ReadStringU здесь СОЗНАТЕЛЬНО не используется — неизвестно, считает он длину в
            // символах или в байтах, а гадать об этом значит печатать недостоверные строки.
            public byte[] ReadBytes(long addr, int count)
            {
                if (count <= 0) return Array.Empty<byte>();

                if (_readBytes != null)
                {
                    try
                    {
                        if (_readBytes.Invoke(_memory, new object[] {addr, count}) is byte[] direct &&
                            direct.Length >= count)
                            return direct;
                    }
                    catch { /* падаем на запасной путь ниже, а не наружу */ }
                }

                // Запасной путь читает восьмёрками и на последней может заглянуть за конец строки;
                // если там страница не отображена, Read<long> вернёт ноль и хвост строки пропадёт.
                // Это видно как обрезанный путь, а не как правдоподобный мусор, — и это приемлемо:
                // путь тут же сверяется с ответом эталона.
                var result = new byte[count];

                for (var i = 0; i < count; i += 8)
                {
                    var chunk = BitConverter.GetBytes(ReadLong(addr + i));
                    Array.Copy(chunk, 0, result, i, Math.Min(8, count - i));
                }

                return result;
            }

            private static MethodInfo FindGenericRead(object memory, Type arg)
            {
                foreach (var t in Candidates(memory))
                {
                    // Ровно ОДИН параметр long: у эталона рядом живёт Read<T>(long, params int[]),
                    // и подхватить его вместо нужного — значит читать по случайной цепочке.
                    var m = t.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                         BindingFlags.FlattenHierarchy)
                        .FirstOrDefault(x => x.Name == "Read" && x.IsGenericMethodDefinition &&
                                             x.GetGenericArguments().Length == 1 &&
                                             x.GetParameters().Length == 1 &&
                                             x.GetParameters()[0].ParameterType == typeof(long));

                    if (m == null) continue;

                    try { return m.MakeGenericMethod(arg); }
                    catch { /* ограничение типа не подошло — пробуем следующего кандидата */ }
                }

                return null;
            }

            private static MethodInfo FindByteRead(object memory, out string name)
            {
                name = null;

                foreach (var t in Candidates(memory))
                foreach (var candidate in new[] {"ReadMem", "ReadBytes"})
                {
                    var m = t.GetMethods(BindingFlags.Public | BindingFlags.Instance |
                                         BindingFlags.FlattenHierarchy)
                        .FirstOrDefault(x => x.Name == candidate && !x.IsGenericMethodDefinition &&
                                             x.ReturnType == typeof(byte[]) &&
                                             x.GetParameters().Length == 2 &&
                                             x.GetParameters()[0].ParameterType == typeof(long) &&
                                             x.GetParameters()[1].ParameterType == typeof(int));

                    if (m == null) continue;

                    name = candidate + "(long,int)";
                    return m;
                }

                return null;
            }

            private static IEnumerable<Type> Candidates(object memory)
            {
                var type = memory.GetType();
                yield return type;

                foreach (var i in type.GetInterfaces())
                    yield return i;
            }
        }

        // Обход связного списка узлов. EntityListOffsets {FirstAddr 0x0, SecondAddr 0x10,
        // Entity 0x28} — числа НАШЕГО форка, и обследование 2026‑09‑16 признало их ВЕРНЫМИ: у
        // потомка головы по +0x28 лежит адрес с той же vtable, что у трёх заведомо настоящих
        // сущностей. Это единственные числа в этих режимах, про которые сказано «верны, не трогать».
        //
        // Предел итераций и HashSet уже виденных узлов обязательны и не подлежат смягчению: обход по
        // устаревшему указателю однажды разогнал процесс до ~4 ГиБ, а молча оборванный обход
        // неотличим от пустой зоны — то есть выглядит как замер, которого не было. Поэтому факт
        // срабатывания предела возвращается наружу отдельным признаком и печатается.
        private static List<long> WalkEntityNodes(RefMemory mem, long root, int nodeLimit,
                                                  out int nodes, out bool limitHit)
        {
            const int NodeFirst = 0x00;
            const int NodeSecond = 0x10;
            const int NodeEntity = 0x28;

            var seenNodes = new HashSet<long>();
            var seenEntities = new HashSet<long>();
            var queue = new Queue<long>();
            var result = new List<long>();

            nodes = 0;
            limitHit = false;

            if (!LooksCanonical(root)) return result;

            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                if (nodes >= nodeLimit)
                {
                    limitHit = true;
                    break;
                }

                var node = queue.Dequeue();

                if (!LooksCanonical(node) || !seenNodes.Add(node)) continue;

                nodes++;

                // У самой головы списка поле +0x28 сущностью не является; отсеивать её по адресу
                // нечем, поэтому она попадёт в список и честно отпечатается как непрочитанный путь.
                var entity = mem.ReadLong(node + NodeEntity);

                if (LooksCanonical(entity) && seenEntities.Add(entity))
                    result.Add(entity);

                queue.Enqueue(mem.ReadLong(node + NodeFirst));
                queue.Enqueue(mem.ReadLong(node + NodeSecond));
            }

            return result;
        }

        // Путь сущности. Критерий, по которому обследование признало эти два поля (и он же —
        // критерий проверки сейчас): число по details+0x18 РАВНО длине строки по details+0x08.
        // Если длина не похожа на длину пути, строка не читается вовсе: пустота с причиной честнее
        // правдоподобного мусора.
        private static string ReadEntityPath(RefMemory mem, long ent, out string trouble)
        {
            trouble = null;

            var details = mem.ReadLong(ent + EntDetails);

            if (!LooksCanonical(details))
            {
                trouble = $"details 0x{details:X} не похож на указатель";
                return null;
            }

            var length = mem.ReadLong(details + DetailsPathLen);

            if (length <= 0 || length > PathCharLimit)
            {
                trouble = $"длина {length} (0x{length:X}) не похожа на длину пути";
                return null;
            }

            // ГИПОТЕЗА, НЕ ЗАМЕР: короткую строку MSVC держит прямо в поле, длинную — по указателю
            // из того же места. Граница взята обычная для std::wstring (8 символов). На путях
            // метаданных (33..46 символов у трёх замеренных сущностей) эта ветка не срабатывает
            // вовсе и на числа не влияет — она здесь ради внятного вывода, а не ради чисел.
            var inline = length < 8;
            var at = inline ? details + DetailsPath : mem.ReadLong(details + DetailsPath);

            if (!inline && !LooksCanonical(at))
            {
                trouble = $"указатель строки 0x{at:X} не похож на указатель (длина {length})";
                return null;
            }

            var bytes = mem.ReadBytes(at, (int) length * 2);

            if (bytes == null || bytes.Length < length * 2)
            {
                trouble = "строка не прочитана";
                return null;
            }

            var text = Encoding.Unicode.GetString(bytes, 0, (int) length * 2);
            var nul = text.IndexOf('\0');

            if (nul >= 0) text = text.Substring(0, nul);

            return text;
        }

        // Слот-таблица компонентов: details=[ent+0x08], lookup=[details+0x28], slots=lookup+0x40
        // (StdVector First/Last/End по 8 байт), слот = {uint32 nameId, uint32 index}, nameId==0 —
        // пустой слот, адрес компонента = [ [ent+0x10] + index*8 ]. Числа ПЕРЕПОДТВЕРЖДЕНЫ и внесены
        // в форк как ComponentLookupOffsets.
        //
        // Возвращает null с причиной в trouble, если по дороге встретилось что-то, не похожее на
        // указатель или на вектор: это ровно то место, где наша модель таблицы расходится с
        // действительностью, и увидеть его надо, а не пролистать.
        private static List<(uint NameId, uint Index, long Component)> ReadSlots(
            RefMemory mem, long ent, out string trouble, out int slotCount, out int componentCount)
        {
            trouble = null;
            slotCount = 0;
            componentCount = 0;

            var details = mem.ReadLong(ent + EntDetails);

            if (!LooksCanonical(details))
            {
                trouble = $"details 0x{details:X} не похож на указатель";
                return null;
            }

            var lookup = mem.ReadLong(details + DetailsLookup);

            if (!LooksCanonical(lookup))
            {
                trouble = $"lookup 0x{lookup:X} не похож на указатель";
                return null;
            }

            var slotsFirst = mem.ReadLong(lookup + LookupSlots);
            var slotsLast = mem.ReadLong(lookup + LookupSlots + 0x8);

            if (!LooksCanonical(slotsFirst) || slotsLast < slotsFirst)
            {
                trouble = $"вектор слотов 0x{slotsFirst:X}..0x{slotsLast:X} не похож на вектор";
                return null;
            }

            var slotBytes = slotsLast - slotsFirst;

            if (slotBytes == 0 || slotBytes % 8 != 0 || slotBytes / 8 > SlotCountLimit)
            {
                trouble = $"слотов вышло {slotBytes / 8} (байт {slotBytes}) — не похоже на таблицу, " +
                          $"предел {SlotCountLimit}";
                return null;
            }

            slotCount = (int) (slotBytes / 8);

            var compFirst = mem.ReadLong(ent + EntComponents);
            var compLast = mem.ReadLong(ent + EntComponents + 0x8);

            if (!LooksCanonical(compFirst) || compLast < compFirst)
            {
                trouble = $"вектор компонентов 0x{compFirst:X}..0x{compLast:X} не похож на вектор";
                return null;
            }

            var compBytes = compLast - compFirst;

            if (compBytes % 8 != 0 || compBytes / 8 > SlotCountLimit)
            {
                trouble = $"компонентов вышло {compBytes / 8} — не похоже на массив";
                return null;
            }

            componentCount = (int) (compBytes / 8);

            var result = new List<(uint NameId, uint Index, long Component)>();

            for (var i = 0; i < slotCount; i++)
            {
                var at = slotsFirst + i * 8L;
                var nameId = mem.ReadUInt(at);
                var index = mem.ReadUInt(at + 4);

                if (nameId == 0) continue;  // пустой слот — так и устроена открытая таблица

                // Индекс за границей массива компонентов адрес не даёт. Строка всё равно печатается:
                // она и есть признак того, что раскладка слота прочитана неверно.
                result.Add(index >= componentCount
                    ? (nameId, index, 0L)
                    : (nameId, index, mem.ReadLong(compFirst + index * 8L)));
            }

            return result;
        }

        // Общая часть режимов --entities и --harvest: взять у эталона АДРЕС объекта EntityList и
        // пройти список САМИМ. Ни один шаг не считается удавшимся молча — на каждом отказе печатается
        // причина, потому что «пусто» и «не прочитали» здесь означают совершенно разное.
        private static int RunEntityModes(object game, object memory, object data, bool harvest)
        {
            var listObject = Member(data, "EntityList", out var listError);

            if (listError != null || listObject == null)
            {
                Console.WriteLine();
                Console.WriteLine($"EntityList у эталона: {listError ?? "(null)"} — обходить нечего.");
                return ExitEmpty;
            }

            Console.WriteLine();
            Console.WriteLine("ОБХОД СПИСКА СУЩНОСТЕЙ — НАШ. У эталона взят только адрес головы.");
            Console.WriteLine(new string('-', 100));
            Console.WriteLine($"  тип у эталона   {listObject.GetType().FullName}");

            // Состав чужого типа печатается ради следующей сессии: если у эталона появится готовое
            // свойство-перечислитель, свой обход станет не нужен, а увидеть это можно только здесь.
            Console.WriteLine($"  его члены       {MemberNames(listObject.GetType())}");

            var listAddressBox = Member(listObject, "Address", out var addressError);

            if (listAddressBox is not long listAddress || listAddress == 0)
            {
                Console.WriteLine($"  Address         {addressError ?? Describe(listAddressBox)} — обходить нечего.");
                return ExitEmpty;
            }

            Console.WriteLine($"  Address         0x{listAddress:X}");

            var mem = new RefMemory(memory);

            Console.WriteLine($"  чтение памяти   {mem.Found}");

            if (!mem.Ready)
            {
                Console.WriteLine();
                Console.WriteLine("ЧИТАТЬ НЕЧЕМ: " + mem.Complaint);
                Console.WriteLine("Это не «пусто», а расхождение с эталоном: обход рассчитан на метод,");
                Console.WriteLine("которого у его Memory нет. Подменять его догадкой нельзя — правьте RefMemory.");
                return ExitNothingToRead;
            }

            var nodeLimit = ArgInt("--nodes", DefaultNodeLimit);

            var printLimit = ArgInt(harvest ? "--harvest" : "--entities",
                harvest ? DefaultHarvestPrintLimit : DefaultEntityPrintLimit);

            // Корень обхода. Наш Core/PoEMemory/MemoryObjects/EntityList.cs берёт его как
            // [EntityList.Address + 0x8]; это НАСЛЕДСТВО АПСТРИМА, обследование 2026‑09‑16 его не
            // проверяло. Поэтому пробуется и запасной вариант (сам адрес как узел), и печатается,
            // который сработал: число, которое нельзя назвать замеренным, должно быть видно как
            // сделанный выбор, а не подразумеваться.
            var rootByEight = mem.ReadLong(listAddress + 0x8);
            var entities = WalkEntityNodes(mem, rootByEight, nodeLimit, out var nodes, out var limitHit);

            var rootUsed = $"[EntityList+0x8] = 0x{rootByEight:X}  (наследство апстрима, не переподтверждено)";

            if (entities.Count == 0)
            {
                var fallback = WalkEntityNodes(mem, listAddress, nodeLimit, out var fallbackNodes,
                    out var fallbackLimit);

                if (fallback.Count > 0)
                {
                    entities = fallback;
                    nodes = fallbackNodes;
                    limitHit = fallbackLimit;

                    rootUsed = $"сам EntityList = 0x{listAddress:X}  " +
                               "(ЗАПАСНОЙ вариант: по [+0x8] обход не дал ни одной сущности)";
                }
            }

            Console.WriteLine($"  корень обхода   {rootUsed}");
            Console.WriteLine($"  узлов пройдено  {nodes} (предел {nodeLimit})");

            if (limitHit)
            {
                Console.WriteLine("  !!! ПРЕДЕЛ УЗЛОВ ДОСТИГНУТ — обход оборван, список НЕПОЛОН.");
                Console.WriteLine("      Либо список зациклен/протух, либо предел мал: поднимите --nodes <N>.");
            }

            Console.WriteLine($"  сущностей       {entities.Count}");

            // Сколько сущностей в зоне, эталон знает сам (Data.EntitiesCount) — это цифра, к нашему
            // обходу отношения не имеющая. Близкие числа значат, что обход прошёл по тому списку;
            // сильно разные — что не по тому. Точного равенства ждать не следует: голова списка и
            // спящие сущности считаются по-разному, и насколько — видно только при живой игре.
            var countBox = Member(data, "EntitiesCount", out var countError);

            Console.WriteLine($"  у эталона       Data.EntitiesCount = {countError ?? Describe(countBox)}");

            if (entities.Count == 0)
            {
                Console.WriteLine();
                Console.WriteLine("Ни одного узла с похожим на сущность указателем. Возможные причины:");
                Console.WriteLine("  — зона ещё грузится: Data уже есть, список ещё пуст;");
                Console.WriteLine("  — голова списка не там, где её ищет наш Core (см. «корень обхода» выше).");
                return ExitEmpty;
            }

            // Разбор эталона нужен как ВТОРОЙ, независимый ответ: свой путь мы читаем по числам из
            // survey, чужой — чужим кодом по чужим числам. Совпадение строк и есть то подтверждение,
            // ради которого режим написан. Может не построиться — тогда просто не будет второй
            // колонки, и об этом сказано прямо, а не умолчано.
            var assembly = game.GetType().Assembly;

            var entityType = FindRefType(assembly, "ExileCore.PoEMemory.MemoryObjects.Entity") ??
                             FindRefType(assembly, "Entity");

            var getObject = FindGetObject(game);
            MethodInfo buildEntity = null;

            if (entityType == null)
            {
                Console.WriteLine("  эталонный Entity: типа с таким именем у эталона нет.");
            }
            else if (getObject == null)
            {
                Console.WriteLine("  эталонный Entity: у эталона не нашлось GetObject<T>(long).");
            }
            else
            {
                try { buildEntity = getObject.MakeGenericMethod(entityType); }
                catch (Exception e)
                {
                    Console.WriteLine($"  эталонный Entity: не построить — {DescribeException(e)}");
                }
            }

            if (buildEntity == null)
                Console.WriteLine("  => сверять наше чтение будет НЕ С ЧЕМ: вторая колонка отсутствует.");

            Console.WriteLine();

            return harvest
                ? PrintHarvest(mem, game, buildEntity, entities, printLimit)
                : PrintEntities(mem, game, buildEntity, entities, printLimit);
        }

        // --entities: по строке на сущность. Смысл вывода — не список сам по себе, а колонка
        // «эталон» рядом с нашим путём: расхождение строк означает, что смещения пути из survey на
        // этом клиенте не те, совпадение — что те.
        private static int PrintEntities(RefMemory mem, object game, MethodInfo buildEntity,
                                         List<long> entities, int printLimit)
        {
            Console.WriteLine("СУЩНОСТИ. Id +0x88; путь: details=[ent+0x08], строка по details+0x08,");
            Console.WriteLine("длина по details+0x18 — ПЕРЕПОДТВЕРЖДЕНО обходом зоны и внесено в структуры форка;");
            Console.WriteLine("колонка «эталон» ниже и есть непрерывная проверка этого.");
            Console.WriteLine("Третьим печатается сырой uint32 по +0x70: прежнее имя «InventoryId» ОПРОВЕРГНУТО");
            Console.WriteLine("(у Metadata/Chests/DarkPot2v2 там указатель в модуль игры, и этот uint32 —");
            Console.WriteLine("его младшая половина); чем поле является на самом деле, НЕ УСТАНОВЛЕНО.");
            Console.WriteLine(new string('-', 100));

            var shown = 0;
            var agreed = 0;
            var disagreed = 0;
            var silent = 0;

            foreach (var ent in entities)
            {
                if (shown >= printLimit) break;

                shown++;

                var id = mem.ReadUInt(ent + EntId);
                var fieldAt70 = mem.ReadUInt(ent + EntFieldAt70);
                var path = ReadEntityPath(mem, ent, out var trouble);

                // X8, а не прежний X4: половина указателя в четыре знака не влезала, и обрезанный
                // вывод как раз и прятал улику, по которой имя «InventoryId» было опровергнуто.
                Console.WriteLine($"  0x{ent:X}  Id 0x{id:X8}  +0x70 0x{fieldAt70:X8} (опровергнуто, не InventoryId)  " +
                                  (path ?? "путь НЕ ПРОЧИТАН: " + trouble));

                var oracle = OraclePathOf(BuildOracleEntity(game, buildEntity, ent));

                if (oracle == null)
                {
                    silent++;
                    continue;
                }

                if (path != null && string.Equals(path, oracle, StringComparison.Ordinal))
                {
                    agreed++;
                    continue;
                }

                disagreed++;
                Console.WriteLine($"        эталон: {oracle}   <-- РАСХОЖДЕНИЕ с нашим чтением");
            }

            Console.WriteLine(new string('-', 100));
            Console.WriteLine($"  напечатано {shown} из {entities.Count}" +
                              (entities.Count > shown ? "  (остальные скрыты пределом --entities <N>)" : ""));

            Console.WriteLine(agreed + disagreed > 0
                ? $"  сверка с эталоном: совпало {agreed}, разошлось {disagreed}, эталон промолчал {silent}"
                : $"  сверка с эталоном НЕ СОСТОЯЛАСЬ: он не отдал ни одного пути (промолчал {silent} раз)");

            return ExitOk;
        }

        // --harvest: стык двух источников по АДРЕСУ КОМПОНЕНТА. Слева — имя, которое знает эталон;
        // справа — nameId, который читаем мы. Совпал адрес — значит имя и номер про одно и то же;
        // это и есть добор таблицы, которой в памяти клиента не существует (строк имён компонентов
        // там нет вообще, см. survey-2026-09-16).
        private static int PrintHarvest(RefMemory mem, object game, MethodInfo buildEntity,
                                        List<long> entities, int printLimit)
        {
            Console.WriteLine("ДОБОР ТАБЛИЦЫ «имя компонента → nameId».");
            Console.WriteLine("  слева  — CacheComp ЭТАЛОНА: его имена и его адреса компонентов;");
            Console.WriteLine("  справа — наш обход: details=[ent+0x08], lookup=[details+0x28],");
            Console.WriteLine("           slots=lookup+0x40 (StdVector), слот = {uint32 nameId, uint32 index},");
            Console.WriteLine("           адрес компонента = [ [ent+0x10] + index*8 ];");
            Console.WriteLine("           эти числа ПЕРЕПОДТВЕРЖДЕНЫ и внесены в форк (ComponentLookupOffsets).");
            Console.WriteLine("  Связь строк — ПО АДРЕСУ: совпал адрес, значит имя слева и nameId справа про одно.");
            Console.WriteLine(new string('-', 100));

            var seen = new Dictionary<string, uint>(StringComparer.Ordinal);
            var vtables = new Dictionary<string, SortedSet<long>>(StringComparer.Ordinal);
            var conflicts = new List<string>();
            var shown = 0;

            foreach (var ent in entities)
            {
                if (shown >= printLimit) break;

                shown++;

                var oracle = BuildOracleEntity(game, buildEntity, ent);
                var path = ReadEntityPath(mem, ent, out var pathTrouble) ?? OraclePathOf(oracle);
                var pathText = path ?? "путь НЕ ПРОЧИТАН: " + pathTrouble;

                Console.WriteLine();
                Console.WriteLine($"  0x{ent:X}  {pathText}");

                var byAddress = OracleComponents(oracle, out var cacheTrouble);

                // Причина печатается ВСЕГДА, когда она есть, а не только при null: OracleComponents
                // умеет вернуть ЧАСТИЧНЫЙ словарь — оборвавшись на шаге или упёршись в предел, — и
                // молчание об этом выдало бы неполный список имён за полный. Неполный список имён
                // здесь означает ложные строки «есть у эталона, нашего слота НЕТ» и наоборот.
                if (cacheTrouble != null)
                    Console.WriteLine($"      CacheComp эталона: {cacheTrouble}");

                var slots = ReadSlots(mem, ent, out var slotTrouble, out var slotCount, out var compCount);

                if (slots == null)
                {
                    Console.WriteLine($"      наши слоты НЕ ПРОЧИТАНЫ: {slotTrouble}");
                    continue;
                }

                Console.WriteLine($"      слотов {slotCount}, занято {slots.Count}, " +
                                  $"компонентов у сущности {compCount}" +
                                  (byAddress != null ? $", имён у эталона {byAddress.Count}" : ""));

                var matched = new HashSet<long>();

                foreach (var slot in slots.OrderBy(s => s.NameId))
                {
                    string name = null;

                    if (slot.Component != 0 && byAddress != null &&
                        byAddress.TryGetValue(slot.Component, out var found))
                    {
                        name = found;
                        matched.Add(slot.Component);
                    }

                    // Имя «?» означает не «компонента нет», а «эталон не назвал этот адрес»: либо он
                    // не разобрал сущность, либо у него нет класса под этот тип. Это тоже находка.
                    var label = name ?? "?";

                    // vtable САМОГО компонента — вот настоящий ключ типа. nameId им НЕ является:
                    // один тип получает разные nameId у разных видов сущностей (BaseEvents — 0x114
                    // и 0x214), а один nameId носят разные типы (0x1D8 — и Chest, и WorldItem).
                    // Замерено 2026-09-16, см. docs/api/entities-measured.md.
                    var vtable = slot.Component == 0 ? 0 : mem.ReadLong(slot.Component);
                    var rva = vtable != 0 && _moduleBase != 0 ? vtable - _moduleBase : 0;

                    Console.WriteLine($"      {label,-24} nameId 0x{slot.NameId:X3} index {slot.Index,-3} " +
                                      (slot.Component == 0
                                          ? "(index за границей массива компонентов)"
                                          : $"0x{slot.Component:X}  vtable RVA " +
                                            (rva > 0 ? $"0x{rva:X}" : "—")));

                    if (name == null) continue;

                    if (rva > 0)
                    {
                        if (!vtables.TryGetValue(name, out var rvaSet))
                            vtables[name] = rvaSet = new SortedSet<long>();

                        rvaSet.Add(rva);
                    }

                    if (seen.TryGetValue(name, out var already) && already != slot.NameId)
                        conflicts.Add($"{name}: 0x{already:X3} и 0x{slot.NameId:X3}");
                    else
                        seen[name] = slot.NameId;
                }

                if (byAddress == null) continue;

                // Имена эталона, которым наш обход не нашёл слота, — ровно то место, где наша модель
                // таблицы расходится с действительностью. Молчать о них нельзя: это не «нет данных»,
                // а «у нас не сошлось».
                foreach (var pair in byAddress)
                {
                    if (matched.Contains(pair.Key)) continue;

                    Console.WriteLine($"      {pair.Value,-24} nameId   —   index  —   0x{pair.Key:X}" +
                                      "  <-- есть у эталона, нашего слота с этим адресом НЕТ");
                }
            }

            Console.WriteLine();
            Console.WriteLine(new string('-', 100));
            Console.WriteLine($"  разобрано сущностей: {shown} из {entities.Count}");

            // Таблица «имя -> RVA vtable» — то, ради чего режим теперь и нужен: именно RVA
            // ставится в GameOffsets/ComponentVtables.cs.
            Console.WriteLine();
            Console.WriteLine($"  ТАБЛИЦА «имя -> RVA vtable», снятая этим запуском: {vtables.Count}");
            Console.WriteLine("  (RVA от базы модуля; устойчив к ASLR, меняется только с патчем игры)");
            Console.WriteLine();

            foreach (var pair in vtables.OrderBy(x => x.Key, StringComparer.Ordinal))
                Console.WriteLine($"            (\"{pair.Key}\", 0x{string.Join(", 0x", pair.Value.Select(v => v.ToString("X")))}" +
                                  (pair.Value.Count > 1 ? "),   <-- НЕСКОЛЬКО RVA: имя покрывает разные типы" : "),"));

            Console.WriteLine();

            var known = KnownNameIds.ToDictionary(x => x.Name, x => x.NameId, StringComparer.Ordinal);
            var fresh = seen.Where(p => !known.ContainsKey(p.Key)).OrderBy(p => p.Value).ToArray();
            var confirmed = seen.Count(p => known.TryGetValue(p.Key, out var v) && v == p.Value);

            Console.WriteLine($"  из них уже было в survey и прочиталось так же: {confirmed} из {known.Count}");

            foreach (var pair in seen.Where(p => known.TryGetValue(p.Key, out var v) && v != p.Value))
                Console.WriteLine($"  !!! РАСХОДИТСЯ С survey: {pair.Key} здесь 0x{pair.Value:X3}, " +
                                  $"в survey 0x{known[pair.Key]:X3}");

            foreach (var line in conflicts.Distinct())
                Console.WriteLine($"  !!! ОДНО ИМЯ — РАЗНЫЕ nameId в одном запуске: {line}");

            Console.WriteLine();
            Console.WriteLine($"  НОВЫЕ ПАРЫ имя↔nameId, встреченные в этом запуске: {fresh.Length}");
            Console.WriteLine("  (это то, что прочитал ИМЕННО ЭТОТ запуск, а не подтверждённые числа:");
            Console.WriteLine("   вносить в код после второго запуска в другой зоне, когда список перестанет меняться)");
            Console.WriteLine();

            if (fresh.Length == 0)
                Console.WriteLine("            — ни одной новой пары: все встреченные имена уже есть в survey.");

            foreach (var pair in fresh)
                Console.WriteLine($"            (\"{pair.Key}\", 0x{pair.Value:X3}),");

            return ExitOk;
        }

        // Сущность глазами ЭТАЛОНА. null означает «эталон не ответил», а не «сущности нет».
        private static object BuildOracleEntity(object game, MethodInfo buildEntity, long address)
        {
            if (buildEntity == null) return null;

            try { return buildEntity.Invoke(game, new object[] {address}); }
            catch { return null; }
        }

        private static string OraclePathOf(object oracleEntity)
        {
            if (oracleEntity == null) return null;

            var path = Member(oracleEntity, "Path", out var error);

            return error == null ? path as string : null;
        }

        // CacheComp эталона — Dictionary<string,long> «имя типа компонента → адрес компонента».
        // Разворачивается АДРЕСОМ В КЛЮЧ: соединять с нашими слотами надо по адресу, потому что имя —
        // это как раз то, чего мы не знаем, и ключом быть не может.
        private static Dictionary<long, string> OracleComponents(object oracleEntity, out string trouble)
        {
            trouble = null;

            if (oracleEntity == null)
            {
                trouble = "эталонная сущность не построена";
                return null;
            }

            var value = Member(oracleEntity, "CacheComp", out var error);

            if (error != null)
            {
                trouble = error;
                return null;
            }

            if (value == null)
            {
                trouble = "CacheComp = null (эталон не разобрал компоненты этой сущности)";
                return null;
            }

            if (value is not System.Collections.IDictionary dict)
            {
                trouble = $"CacheComp не словарь, а {value.GetType().Name}";
                return null;
            }

            var result = new Dictionary<long, string>();
            var steps = 0;

            try
            {
                foreach (System.Collections.DictionaryEntry e in dict)
                {
                    // Предел шагов. Опасность здесь МЕНЬШЕ, чем в DumpEnumerable: словарь эталон уже
                    // построил в нашем процессе, его Count конечен, и ленивого чтения чужой памяти
                    // на каждом шаге тут нет. Но перечислитель всё равно чужой, а правило «любой
                    // обход — с жёстким пределом» не знает исключений. Компонентов у сущности
                    // обследование видело 4..14, так что предела не коснуться; он нужен на случай,
                    // когда CacheComp окажется не тем, чем мы его считаем.
                    if (steps++ >= EnumerationStepLimit)
                    {
                        trouble = $"!!! ПРЕДЕЛ ПЕРЕЧИСЛЕНИЯ {EnumerationStepLimit} ДОСТИГНУТ при чтении " +
                                  $"CacheComp — список имён эталона НЕПОЛОН (набрано {result.Count}). " +
                                  "Строки «есть у эталона, нашего слота нет» ниже поэтому неполны тоже.";
                        break;
                    }

                    var name = e.Key?.ToString();

                    if (string.IsNullOrEmpty(name)) continue;

                    long address;

                    try { address = Convert.ToInt64(e.Value); }
                    catch { continue; }

                    if (address == 0) continue;

                    // Два имени на один адрес быть не должно. Если случилось — это видно в строке,
                    // а не потеряно молча.
                    result[address] = result.TryGetValue(address, out var had) ? had + "/" + name : name;
                }
            }
            catch (Exception e)
            {
                trouble = DescribeException(e);
                return result.Count > 0 ? result : null;
            }

            return result;
        }

        // Состав чужого типа одной строкой. Нужен только для отчёта «что у эталона есть»: по нему
        // следующая сессия увидит, не появилось ли готового свойства взамен нашего обхода.
        private static string MemberNames(Type type)
        {
            try
            {
                var names = type
                    .GetMembers(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                                BindingFlags.DeclaredOnly)
                    .Where(m => m.MemberType is MemberTypes.Property or MemberTypes.Field or MemberTypes.Method)
                    .Select(m => m.Name)
                    .Where(n => !n.StartsWith("get_") && !n.StartsWith("set_") && !n.StartsWith("<"))
                    .Distinct()
                    .OrderBy(n => n, StringComparer.Ordinal)
                    .Take(40)
                    .ToArray();

                return names.Length == 0 ? "(нет собственных членов)" : string.Join(", ", names);
            }
            catch (Exception e)
            {
                return DescribeException(e);
            }
        }

        // Типы эталона. GetTypes() у него бросает: часть его типов ссылается на сборки, которых
        // рядом нет (MSBuild). Загруженного подмножества достаточно — но брать его надо, не падая.
        private static Type FindRefType(Assembly assembly, string typeName)
        {
            Type[] types;

            try { types = assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { types = e.Types.Where(t => t != null).ToArray(); }
            catch (Exception) { return null; }

            return types.FirstOrDefault(
                       t => string.Equals(t.FullName, typeName, StringComparison.OrdinalIgnoreCase)) ??
                   types.FirstOrDefault(
                       t => string.Equals(t.Name, typeName, StringComparison.OrdinalIgnoreCase)) ??
                   types.FirstOrDefault(
                       t => t.FullName != null &&
                            t.FullName.IndexOf(typeName, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static MethodInfo FindGetObject(object game) =>
            game.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance |
                            BindingFlags.FlattenHierarchy)
                .FirstOrDefault(m => m.Name == "GetObject" && m.IsGenericMethodDefinition &&
                                     m.GetParameters().Length == 1 &&
                                     m.GetParameters()[0].ParameterType == typeof(long));

        // Числа печатаются и десятично, и шестнадцатерично: --find принимает HEX, а глазом поле
        // опознаётся десятичным (уровень зоны — 67, а не 0x43).
        private static string Describe(object value)
        {
            switch (value)
            {
                case null:      return "(null)";
                case Exception e: return DescribeException(e);
                case long l:    return l == 0 ? "0" : $"{l} (0x{l:X})";
                case ulong ul:  return ul == 0 ? "0" : $"{ul} (0x{ul:X})";
                case int i:     return i == 0 ? "0" : $"{i} (0x{i:X})";
                case uint u:    return u == 0 ? "0" : $"{u} (0x{u:X})";
                case short s:   return $"{s} (0x{s:X})";
                case ushort us: return $"{us} (0x{us:X})";
                case byte b:    return $"{b} (0x{b:X})";
                case float f:   return f.ToString("R", CultureInfo.InvariantCulture);
                case double d:  return d.ToString("R", CultureInfo.InvariantCulture);
                default:        return value.ToString();
            }
        }

        // Рефлексия заворачивает исключение свойства в TargetInvocationException; интересна причина.
        private static string DescribeException(Exception e)
        {
            while (e is TargetInvocationException && e.InnerException != null)
                e = e.InnerException;

            return $"НЕ ПРОЧИТАНО: {e.GetType().Name}: {Oneline(e.Message)}";
        }

        // Создаёт объект чужой сборки конструктором с нужным числом аргументов, независимо от его
        // доступности. Не нашли — говорим, какие конструкторы есть: так видно, что именно разошлось.
        private static object Construct(Type type, object[] args)
        {
            var all = type.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var c in all)
                if (c.GetParameters().Length == args.Length)
                    return c.Invoke(args);

            var known = string.Join("; ", all.Select(
                c => $"({string.Join(", ", c.GetParameters().Select(x => x.ParameterType.Name))})"));

            throw new MissingMethodException(
                $"{type.FullName}: нет конструктора на {args.Length} аргумент(ов). Есть: {known}");
        }

        private static void Report(string name, long address)
        {
            Console.WriteLine(address == 0
                ? $"  {name,-26} 0x0   [пусто]"
                : $"  {name,-26} 0x{address:X}");
        }

        private static void Safe(string name, Func<string> read)
        {
            string value;

            try { value = read() ?? "(null)"; }
            catch (Exception e) { value = $"НЕ ПРОЧИТАНО: {e.GetType().Name}"; }

            Console.WriteLine($"  {name,-26} {value}");
        }

        // Имя процесса клиента менялось, поэтому точное совпадение — не условие, а лишь пометка:
        // приложиться к запущенной игре важнее, чем совпасть с чужой строкой.
        private static Process FindGameProcess(out bool exact)
        {
            exact = false;

            // У эталона нет таблицы вариантов ExeName (Offsets там — статический держатель
            // сигнатур), поэтому процесс ищется по подстроке сразу. Точного имени тут не с чем
            // сверять, и признак exact остаётся ложным — это честнее, чем объявить совпадение.
            try
            {
                return Process.GetProcesses().FirstOrDefault(
                    p => p.ProcessName.IndexOf(ExeNameFragment, StringComparison.OrdinalIgnoreCase) >= 0);
            }
            catch
            {
                return null;
            }
        }

        private static string Oneline(string s) =>
            string.IsNullOrEmpty(s) ? "" : s.Replace("\r", " ").Replace("\n", " ").Trim();
    }
}
