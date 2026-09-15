using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

// ────────────────────────────────────────────────────────────────────────────────────────────────
// RefOffset — что можно узнать об ЭТАЛОННОМ форке по его файлам, без запуска игры.
//
// Режимы:
//   --layout <Тип>       раскладка структуры: смещение (у CLR, не «по порядку полей»), размер, тип
//   --trace  <Тип>       IL всех методов типа: чтения полей, литералы, вызовы — и ВЕРДИКТ по телу
//   --structs <подстрока> все структуры GameOffsets: раскладка и размер, по убыванию размера
//
// Зачем был написан. Оффсеты этого форка от старой сборки игры; эталон читает текущего клиента
// правильно. Хотелось снять соответствие «свойство IngameState.Data → поле структуры → смещение»
// прямо с файлов: публичные имена ExileCore обфускация пережили, лямбды конструктора сохранили и
// имена (b__13_2), и возвращаемые типы, так что нужное было бы видно в IL.
//
// ЧТО ОКАЗАЛОСЬ (замер 2026-09-15). Тела методов эталона ПОДМЕНЕНЫ: get_Data состоит ровно из
// «ldarg.0; ret», то есть возвращает this там, где по сигнатуре обязан вернуть IngameData; у
// get_ShortcutSettings переход уходит в середину инструкции. Настоящий код восстанавливает защита
// в рантайме, в файле его нет. Поэтому --trace отвечает не «вот поле», а «тело подменено» — и этот
// ответ он ДОКАЗЫВАЕТ проверками ниже, а не объявляет.
//
// Вывод: статический реверс эталона по IL закрыт. Раскладка структур при этом читается — целы
// метаданные, подменены только тела.
//
// Инструмент только читает. Сборки эталона грузятся, но ни один их метод не вызывается:
// GetMethodBody / ResolveField / ResolveMethod не запускают ни кода форка, ни его cctor'ов.
//
// Запуск:
//   dotnet run -c Release --project tools/RefOffset -- "путь\к\эталону" --trace IngameState
//   dotnet run -c Release --project tools/RefOffset -- "путь\к\эталону" --layout t83743
//
// Коды возврата: 0 — отработал; 2 — не разобрал аргументы или не нашёл тип.
// ────────────────────────────────────────────────────────────────────────────────────────────────

Console.OutputEncoding = System.Text.Encoding.UTF8;

if (args.Length < 1 || args[0].StartsWith("--"))
{
    Console.Error.WriteLine("RefOffset: первым аргументом нужен корень установки эталона.");
    Console.Error.WriteLine(@"  пример: RefOffset ""C:\...\ExileApi-Compiled"" --trace IngameState");
    return 2;
}

string root = args[0];
if (!Directory.Exists(root))
{
    Console.Error.WriteLine($"RefOffset: каталога нет: {root}");
    return 2;
}

string mode = args.Length > 1 ? args[1] : "--trace";
string want = args.Length > 2 ? args[2] : "IngameState";
if (mode == "--structs") want = "IngameState"; // тип для --structs не нужен: он перебирает все

var ctx = new DirLoadContext(root);
var assemblies = new List<Assembly>();
foreach (var name in new[] { "ExileCore.dll", "GameOffsets.dll" })
{
    string path = Path.Combine(root, name);
    if (!File.Exists(path)) { Console.Error.WriteLine($"RefOffset: нет {path}"); return 2; }
    assemblies.Add(ctx.LoadFromAssemblyPath(path));
}

Console.WriteLine($"# корень: {root}");
foreach (var a in assemblies) Console.WriteLine($"#   {a.GetName().Name} {a.GetName().Version}");
Console.WriteLine();

Type target = Tracer.FindType(assemblies, want);
if (target == null)
{
    Console.Error.WriteLine($"RefOffset: тип не найден: {want}");
    return 2;
}

switch (mode)
{
    case "--structs": Tracer.PrintStructs(assemblies, args.Length > 2 ? args[2] : ""); return 0;
    case "--layout": Tracer.PrintLayout(target); return 0;
    case "--trace": Tracer.PrintTrace(target); return 0;
    default:
        Console.Error.WriteLine($"RefOffset: неизвестный режим {mode} (ожидается --trace или --layout)");
        return 2;
}

static class Tracer
{
    // ── поиск типа: сначала точное простое имя, затем подстрока FullName ─────────────────────────
    public static Type FindType(List<Assembly> assemblies, string want)
    {
        var all = new List<Type>();
        foreach (var a in assemblies)
        {
            Type[] types;
            try { types = a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = Array.FindAll(ex.Types, t => t != null); }
            all.AddRange(types);
        }

        foreach (var t in all) if (t.Name == want) return t;
        foreach (var t in all) if (t.FullName != null && t.FullName.Contains(want, StringComparison.OrdinalIgnoreCase)) return t;
        return null;
    }

    // ── перечень структур ───────────────────────────────────────────────────────────────────────
    // Обфускация переименовала ЧАСТЬ типов GameOffsets (t83743 и подобные), поэтому искать нужную
    // раскладку по имени бесполезно. Размер и вид раскладки имена переживают: по ним структура
    // опознаётся независимо от того, как её назвали.
    public static void PrintStructs(List<Assembly> assemblies, string filter)
    {
        var rows = new List<(int Size, string Name, string Kind, int Fields)>();
        foreach (var a in assemblies)
        {
            Type[] types;
            try { types = a.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = Array.FindAll(ex.Types, t => t != null); }

            foreach (var t in types)
            {
                if (!t.IsValueType || t.IsEnum || t.IsGenericType) continue;
                if (filter.Length > 0 && (t.FullName == null || !t.FullName.Contains(filter, StringComparison.OrdinalIgnoreCase))) continue;

                int size;
                try { size = Marshal.SizeOf(t); }
                catch { continue; } // размер не вычислим — структура не блоб памяти, тут не о ней речь

                string kind = t.StructLayoutAttribute?.Value.ToString() ?? "?";
                int fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).Length;
                rows.Add((size, t.FullName, kind, fields));
            }
        }

        rows.Sort((x, y) => y.Size.CompareTo(x.Size));
        Console.WriteLine($"### структур: {rows.Count}   (фильтр: {(filter.Length > 0 ? filter : "нет")})");
        Console.WriteLine();
        foreach (var r in rows)
            Console.WriteLine($"  0x{r.Size:X6}  {r.Size,9:N0} б  {r.Kind,-10} полей={r.Fields,-4} {r.Name}");
    }

    // ── раскладка структуры ─────────────────────────────────────────────────────────────────────
    // Смещение берётся у CLR (Marshal.OffsetOf), а не считается по порядку полей: для Explicit
    // порядок вообще ничего не значит, а ручное выравнивание Sequential — это гипотеза.
    // Тип не blittable — OffsetOf бросит: печатаем причину, а не пропускаем поле молча. Поле, чей
    // размер не вычислим, печатается с полным именем типа: обычно это и есть причина.
    public static void PrintLayout(Type t)
    {
        var layout = t.StructLayoutAttribute;
        string size;
        try { size = $"{Marshal.SizeOf(t)} б (0x{Marshal.SizeOf(t):X})"; }
        catch (Exception ex) { size = $"не вычислим: {ex.GetType().Name}"; }

        Console.WriteLine($"### {t.FullName}   [{t.Assembly.GetName().Name}]");
        Console.WriteLine($"    layout={layout?.Value.ToString() ?? "?"}  pack={layout?.Pack}  размер={size}");
        Console.WriteLine();

        foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        {
            string off;
            try { off = $"0x{(int)Marshal.OffsetOf(t, f.Name):X}"; }
            catch (Exception ex) { off = $"?({ex.GetType().Name})"; }

            string fsize, note = "";
            try { fsize = Marshal.SizeOf(f.FieldType).ToString(); }
            catch { fsize = "?"; note = $"   ← {f.FieldType.FullName}"; }

            Console.WriteLine($"  {off,-8} {f.Name,-26} {Short(f.FieldType),-26} размер={fsize}{note}");
        }
    }

    // ── трассировка IL ──────────────────────────────────────────────────────────────────────────
    // По каждому методу печатается вердикт о ТЕЛЕ и только те инструкции, что несут смысл для
    // реверса оффсетов: чтения полей (со смещением, если поле структуры), числовые литералы и
    // вызовы. Вердикт идёт первым намеренно: разбирать «какое поле читает метод» в подменённом
    // теле — значит выдавать мусор защиты за факт о клиенте.
    public static void PrintTrace(Type t)
    {
        Console.WriteLine($"### {t.FullName}   [{t.Assembly.GetName().Name}]");
        Console.WriteLine();

        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance
                  | BindingFlags.Static | BindingFlags.DeclaredOnly;

        var methods = new List<MethodBase>();
        methods.AddRange(t.GetMethods(flags));
        methods.AddRange(t.GetConstructors(flags));

        // Сигнатуры конструкторов печатаются отдельно и ДО разбора тел: у конструктора тело может
        // отсутствовать вовсе, а знать, чем тип создаётся, нужно раньше, чем чем он занят внутри.
        foreach (var c in t.GetConstructors(flags))
            Console.WriteLine($"  .ctor({string.Join(", ", Array.ConvertAll(c.GetParameters(), x => $"{Short(x.ParameterType)} {x.Name}"))})");
        Console.WriteLine();

        int withBody = 0, broken = 0;

        foreach (var m in methods)
        {
            byte[] il;
            try
            {
                var body = m.GetMethodBody();
                if (body == null) continue;
                il = body.GetILAsByteArray();
                if (il == null || il.Length == 0) continue;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  {m.Name}: тело не прочитано ({ex.GetType().Name})");
                continue;
            }

            withBody++;
            var (lines, verdict) = Walk(m, il);
            if (verdict != null) broken++;

            string ret = m is MethodInfo mi ? Short(mi.ReturnType) : "void";
            string ps = string.Join(", ", Array.ConvertAll(m.GetParameters(), x => $"{Short(x.ParameterType)} {x.Name}"));
            Console.WriteLine($"  {m.Name}({ps}) : {ret}   [{il.Length} б IL]{(verdict != null ? "   ← ТЕЛО ПОДМЕНЕНО" : "")}");
            if (verdict != null) Console.WriteLine($"      {verdict}");
            foreach (var line in lines) Console.WriteLine($"      {line}");
            Console.WriteLine();
        }

        Console.WriteLine($"ИТОГО: тел прочитано {withBody}, заведомо подменённых {broken}.");
        if (broken > 0)
            Console.WriteLine("Настоящий код таких методов защита восстанавливает в рантайме; в файле его нет,\n" +
                              "и статический реверс по IL для этой сборки закрыт.");
    }

    // Возвращает интересные строки и, если тело заведомо невалидно, — причину.
    private static (List<string> Lines, string Verdict) Walk(MethodBase m, byte[] il)
    {
        var outp = new List<string>();
        var module = m.Module;
        Type[] typeArgs = SafeArgs(() => m.DeclaringType?.GetGenericArguments());
        Type[] methArgs = SafeArgs(() => m.GetGenericArguments());

        // Границы инструкций и цели переходов копятся ради проверки тела: переход, попавший не на
        // границу, — это не «сложный код», это невозможный код.
        var starts = new HashSet<int>();
        var branches = new List<(int From, int To)>();
        string verdict = null;

        int pos = 0;
        while (pos < il.Length)
        {
            int start = pos;
            starts.Add(start);

            short code = il[pos++];
            if (code == 0xFE)
            {
                if (pos >= il.Length) { verdict = $"IL_{start:X4}: префикс 0xFE в конце тела"; break; }
                code = (short)(0xFE00 | il[pos++]);
            }

            if (!OpTable.Value.TryGetValue(code, out var op))
            {
                verdict = $"IL_{start:X4}: опкода 0x{code:X} не существует";
                break;
            }

            int operand = OperandSize(op.OperandType, il, pos);
            if (operand < 0 || pos + operand > il.Length)
            {
                verdict = $"IL_{start:X4}: {op.Name} — операнд не помещается в тело";
                break;
            }

            switch (op.OperandType)
            {
                case OperandType.InlineField:
                    outp.Add($"IL_{start:X4}: {op.Name} {DescribeField(module, BitConverter.ToInt32(il, pos), typeArgs, methArgs)}");
                    break;

                case OperandType.InlineMethod:
                    outp.Add($"IL_{start:X4}: {op.Name} {DescribeMethod(module, BitConverter.ToInt32(il, pos), typeArgs, methArgs)}");
                    break;

                case OperandType.InlineI:
                {
                    int v = BitConverter.ToInt32(il, pos);
                    if (v > 8 || v < 0) outp.Add($"IL_{start:X4}: {op.Name} {v} (0x{v:X})");
                    break;
                }
                case OperandType.InlineI8:
                {
                    long v = BitConverter.ToInt64(il, pos);
                    outp.Add($"IL_{start:X4}: {op.Name} {v} (0x{v:X})");
                    break;
                }
                case OperandType.ShortInlineI:
                {
                    int v = (sbyte)il[pos];
                    if (v > 8) outp.Add($"IL_{start:X4}: {op.Name} {v} (0x{v:X})");
                    break;
                }
                case OperandType.ShortInlineBrTarget:
                    branches.Add((start, pos + 1 + (sbyte)il[pos]));
                    break;
                case OperandType.InlineBrTarget:
                    branches.Add((start, pos + 4 + BitConverter.ToInt32(il, pos)));
                    break;
            }

            pos += operand;
        }

        starts.Add(il.Length); // переход «за последнюю инструкцию» — законный выход из тела

        if (verdict == null)
            foreach (var (from, to) in branches)
                if (!starts.Contains(to))
                {
                    verdict = $"IL_{from:X4}: переход на IL_{to:X4} — не на границу инструкции";
                    break;
                }

        // Возврат this из метода, чей тип возврата к типу-владельцу не приводится, невозможен.
        // Именно в эту форму защита сворачивает большинство тел (02-2A), поэтому проверка узкая
        // и конкретная, а не «код выглядит странно».
        if (verdict == null && il.Length == 2 && il[0] == 0x02 && il[1] == 0x2A
            && m is MethodInfo mi && !mi.IsStatic && m.DeclaringType != null
            && !mi.ReturnType.IsAssignableFrom(m.DeclaringType))
        {
            verdict = $"тело = «ldarg.0; ret», то есть возврат this, а сигнатура требует {Short(mi.ReturnType)}";
        }

        return (outp, verdict);
    }

    private static string DescribeField(Module module, int token, Type[] typeArgs, Type[] methArgs)
    {
        try
        {
            var f = module.ResolveField(token, typeArgs, methArgs);
            string off = "";
            if (f.DeclaringType is { IsValueType: true })
            {
                try { off = $"  @ 0x{(int)Marshal.OffsetOf(f.DeclaringType, f.Name):X}"; }
                catch { off = "  @ ?"; }
            }
            return $"{Short(f.DeclaringType)}.{f.Name} : {Short(f.FieldType)}{off}";
        }
        catch (Exception ex) { return $"токен 0x{token:X8} не разобран ({ex.GetType().Name})"; }
    }

    private static string DescribeMethod(Module module, int token, Type[] typeArgs, Type[] methArgs)
    {
        try
        {
            var mb = module.ResolveMethod(token, typeArgs, methArgs);
            string ga = "";
            if (mb.IsGenericMethod)
                ga = "<" + string.Join(", ", Array.ConvertAll(mb.GetGenericArguments(), Short)) + ">";
            return $"{Short(mb.DeclaringType)}.{mb.Name}{ga}";
        }
        catch (Exception ex) { return $"токен 0x{token:X8} не разобран ({ex.GetType().Name})"; }
    }

    private static Type[] SafeArgs(Func<Type[]> get)
    {
        try { return get() ?? Type.EmptyTypes; }
        catch { return Type.EmptyTypes; }
    }

    private static int OperandSize(OperandType t, byte[] il, int pos) => t switch
    {
        OperandType.InlineNone => 0,
        OperandType.ShortInlineBrTarget or OperandType.ShortInlineI or OperandType.ShortInlineVar => 1,
        OperandType.InlineVar => 2,
        OperandType.InlineBrTarget or OperandType.InlineField or OperandType.InlineI
            or OperandType.InlineMethod or OperandType.InlineSig or OperandType.InlineString
            or OperandType.InlineTok or OperandType.InlineType or OperandType.ShortInlineR => 4,
        OperandType.InlineI8 or OperandType.InlineR => 8,
        OperandType.InlineSwitch => pos + 4 <= il.Length ? 4 + 4 * BitConverter.ToInt32(il, pos) : -1,
        _ => -1
    };

    public static string Short(Type t)
    {
        if (t == null) return "?";
        string n = t.FullName ?? t.Name;
        foreach (var prefix in new[] { "ExileCore.PoEMemory.MemoryObjects.", "ExileCore.PoEMemory.", "ExileCore.", "GameOffsets.", "System." })
            if (n.StartsWith(prefix, StringComparison.Ordinal)) { n = n.Substring(prefix.Length); break; }
        int tick = n.IndexOf('`');
        if (tick > 0 && t.IsGenericType)
            n = n.Substring(0, tick) + "<" + string.Join(", ", Array.ConvertAll(t.GetGenericArguments(), Short)) + ">";
        return n;
    }
}

static class OpTable
{
    private static readonly Lazy<Dictionary<short, OpCode>> Table = new(Build);
    public static Dictionary<short, OpCode> Value => Table.Value;

    private static Dictionary<short, OpCode> Build()
    {
        var d = new Dictionary<short, OpCode>();
        foreach (var f in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            if (f.GetValue(null) is OpCode oc) d[oc.Value] = oc;
        return d;
    }
}

// Сборки эталона лежат в одном каталоге и тянут друг друга (SharpDX, ImGui.NET, ...). Штатный
// резолвер их не найдёт — ищем по имени файла рядом. Не нашли — возвращаем null, и тип, которому
// зависимость нужна, просто не загрузится: это видно в выводе, а не превращается в падение.
sealed class DirLoadContext : AssemblyLoadContext
{
    private readonly string _dir;
    public DirLoadContext(string dir) : base(isCollectible: false) => _dir = dir;

    protected override Assembly Load(AssemblyName name)
    {
        string path = Path.Combine(_dir, name.Name + ".dll");
        return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
    }
}
