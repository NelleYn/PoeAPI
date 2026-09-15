using System;
using System.Diagnostics;
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

            Console.WriteLine();
            Console.WriteLine("ДАЛЬШЕ: смещение внутри IngameState считает FindOffset —");
            Console.WriteLine($"  FindOffset.exe --find --ingame-state --len 0x2000 --value 0x{data.Address:X}");

            return ExitOk;
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
