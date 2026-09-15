using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

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
    // Коды возврата: 0 — адреса напечатаны; 1 — эталон приложился, но подобъекта нет (вне зоны);
    //                2 — читать нечего: игра не запущена или эталон не сконструировался.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    internal static class Program
    {
        private const int ExitOk = 0;
        private const int ExitEmpty = 1;
        private const int ExitNothingToRead = 2;

        private const string ExeNameFragment = "pathofexile";

        private static string _refDir;

        private static int Main(string[] args)
        {
            try { Console.OutputEncoding = System.Text.Encoding.UTF8; }
            catch { /* консоли может не быть вовсе — не повод падать */ }

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
                return ExitEmpty;
            }

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

            Console.WriteLine();
            Console.WriteLine("ДАЛЬШЕ: смещение внутри IngameState считает FindOffset —");
            Console.WriteLine($"  FindOffset.exe --find --ingame-state --len 0x2000 --value 0x{data.Address:X}");

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
                if (shown++ >= 8) break;

                var key = Convert.ToInt64(Convert.ToInt32(e.Key));
                var val = Convert.ToInt64(Convert.ToInt32(e.Value));

                Console.WriteLine($"      {e.Key,-34} = {e.Value,-8} сырая пара 0x{(val << 32 | (key & 0xFFFFFFFFL)):X16}");
            }
        }

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
