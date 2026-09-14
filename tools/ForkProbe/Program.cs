using System.Reflection;
using System.Text.Json;

// ────────────────────────────────────────────────────────────────────────────────────────────────
// ForkProbe — «как этот член памяти зовётся на ЭТОЙ сборке ExileApi».
//
// Плагин ищет почти всё, что читает из памяти игры, РЕФЛЕКСИЕЙ по списку имён-кандидатов: форки
// ExileApi переименовывают члены, и типизированное обращение к отсутствующему полю падает не мягкой
// ошибкой, а исключением наружу (см. комментарий к TerrainLayerProbe.MeleeLayerCandidates). Цена
// этого стиля — молчание: подсистема, чей список имён промахнулся, выглядит ровно как подсистема,
// которой просто нечего делать.
//
// Этот зонд снимает вопрос статически. Читает только МЕТАДАННЫЕ сборок форка (MetadataLoadContext —
// кода не исполняет, зависимости грузить не пытается), поэтому игра не нужна, а ответ получается
// сразу по всем подсистемам.
//
// Запуск (путь — корень установки форка, где лежат ExileCore.dll / GameOffsets.dll):
//   dotnet run -c Release --project tools/ForkProbe -- "C:\path\to\ExileApi-Compiled" <режим> <арг>
//
// Режимы:
//   --dump <ИмяТипа>          все свойства/поля/методы типа (точное простое имя ИЛИ подстрока FullName)
//   --grepmember <подстрока>  все члены ВСЕХ типов, чьё имя содержит подстроку («а как оно тут зовётся»)
//   --types <подстрока>       все типы, чьё FullName содержит подстроку
//   --jagged <подстрокаТипа>  члены-зубчатые массивы (T[][]) — так ищется слой высот
//   --layout <ПодстрокаТипа>  раскладка в памяти: LayoutKind и числовой FieldOffset каждого поля
//   (--dump рядом с типом свойства печатает доступность аксессоров: «(get: public, set: internal)»)
//   --spec <файл.json>        ПАКЕТНАЯ проверка: TSV-таблица «подсистема → искали → нашли → вердикт»
//
// Формат файла для --spec — массив объектов:
//   [ { "subsystem": "слой высот", "type": "IngameData", "kind": "member",
//       "candidates": ["RawTerrainHeightData", "TerrainHeightData", "HeightData"],
//       "note": "Terrain/ExileApiTerrainReader.cs:339" } ]
//   kind: "member" (свойство/поле) | "method". Готовый файл — tools/ForkProbe/spec.json.
//   "knownMiss": true — промах уже разобран и безвреден: печатается как «ИЗВЕСТНЫЙ ПРОМАХ» и не
//   влияет на код возврата (иначе зонд навсегда красный и в гейт не годится).
//   "requireAll": true — candidates это НАБОР, а не альтернативы: строка зелёная, только если найдены
//   ВСЕ имена (так проверяется карта поверхности surface.json).
//   Файлов можно передать несколько: --spec tools/ForkProbe/spec.json tools/ForkProbe/surface.json
//
// ЧЕГО ЗОНД НЕ УМЕЕТ (и не должен): проверить поиск по СТРОКОВЫМ ДАННЫМ игры (имена состояний
// StateMachine, пути `Metadata/...`, тексты кнопок) и места, где тип известен только в рантайме
// (`dynamic` поверх значения, типизированного как Element/object). Такие зонды судит только игра.
// ────────────────────────────────────────────────────────────────────────────────────────────────

// Вывод зонда читают и глазами, и скриптами, и он весь по-русски. Без этого .NET пишет кодовой
// страницей консоли (на этой машине cp866), и перенаправленный в файл TSV становится нечитаемым —
// то есть машинно-проверяемая карта переставала быть машинно-читаемой ровно при первом `> file`.
Console.OutputEncoding = System.Text.Encoding.UTF8;

string exileDir = args.Length > 0 && !args[0].StartsWith("--")
    ? args[0]
    : Environment.GetEnvironmentVariable("EXILE_ROOT") ?? "";

if (string.IsNullOrWhiteSpace(exileDir) || !Directory.Exists(exileDir))
{
    Console.Error.WriteLine("ForkProbe: первым аргументом нужен корень установки ExileApi (или EXILE_ROOT).");
    Console.Error.WriteLine("  пример: dotnet run -c Release --project tools/ForkProbe -- \"C:\\Users\\me\\Desktop\\ExileApi-Compiled\" --dump IngameData");
    return 2;
}

var asmPaths = new List<string>(Directory.GetFiles(exileDir, "*.dll"));
// Ref-сборки рантайма — иначе MetadataLoadContext не разрешит System.*/CoreLib.
asmPaths.AddRange(Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll"));

// Дубли простых имён (одна и та же сборка из двух папок) ломают резолвер — оставляем первую.
var byName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
foreach (var p in asmPaths)
{
    string n = Path.GetFileNameWithoutExtension(p);
    if (!byName.ContainsKey(n)) byName[n] = p;
}

using var mlc = new MetadataLoadContext(new PathAssemblyResolver(byName.Values), "System.Private.CoreLib");

var assemblies = new List<Assembly>();
foreach (string name in new[] { "ExileCore", "GameOffsets", "ItemFilterLibrary" })
{
    if (!byName.TryGetValue(name, out string path)) continue;
    try { assemblies.Add(mlc.LoadFromAssemblyPath(path)); }
    catch (Exception ex) { Console.Error.WriteLine($"!! не загрузилась {name}: {ex.GetType().Name}: {ex.Message}"); }
}

var allTypes = new List<Type>();
foreach (var a in assemblies)
{
    // Частично разрешимая сборка отдаёт то, что смогла, — этого для имён членов достаточно.
    try { allTypes.AddRange(a.GetTypes()); }
    catch (ReflectionTypeLoadException ex) { allTypes.AddRange(ex.Types.Where(t => t != null)!); }
}
Console.Error.WriteLine($"# сборок: {assemblies.Count}, типов: {allTypes.Count}, корень: {exileDir}");

// Те же флаги, что у TerrainLayerProbe.MemberFlags — иначе зонд отвечал бы не на тот вопрос.
const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;

string mode = args.FirstOrDefault(a => a.StartsWith("--")) ?? "--dump";
string[] rest = args.Where(a => !a.StartsWith("--")).Skip(1).ToArray();

// Пометка «член найден, но НЕ public». Зонд ищет с BindingFlags.NonPublic, потому что так же ищет
// рефлексия в проде; но типизированный код такой член не откомпилирует (CS0122). Разница между
// «читается рефлексией» и «доступно типизированно» — это разница между зондом и будущим API.
const string NonPublicMark = " [не public]";

switch (mode)
{
    case "--types":
        foreach (var t in allTypes
                     .Where(t => rest.Length == 0 || rest.Any(r => SafeName(t).Contains(r, StringComparison.OrdinalIgnoreCase)))
                     .OrderBy(t => t.FullName))
            Console.WriteLine($"{t.Assembly.GetName().Name}\t{SafeName(t)}");
        break;

    case "--dump":
        foreach (string needle in rest)
        {
            var matches = allTypes.Where(t => t.Name.Equals(needle, StringComparison.OrdinalIgnoreCase)
                                           || SafeName(t).Contains(needle, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0) { Console.WriteLine($"### {needle}: ТИП НЕ НАЙДЕН"); continue; }
            foreach (var t in matches.OrderBy(SafeName))
            try
            {
                Console.WriteLine($"### {SafeName(t)}   [{t.Assembly.GetName().Name}]  " +
                                  $"base={Try(() => t.BaseType?.Name ?? "-")}  " +
                                  // Реализуемые интерфейсы: без них --dump молчал о том, что
                                  // IMemoryBackend наследует IDisposable, и попытка реализовать его
                                  // по одному лишь списку методов давала CS0535.
                                  $"impl={Try(() => string.Join(",", t.GetInterfaces().Select(i => i.Name)))}" +
                                  // [Flags] не выводится из значений надёжно (набор 0,1,2 бывает и
                                  // у обычного enum), а без него перенесённый enum ведёт себя иначе
                                  // при ToString и HasFlag. Атрибут лежит в метаданных — читаем его.
                                  Try(() => t.GetCustomAttributesData()
                                             .Any(x => x.AttributeType.Name == "FlagsAttribute") ? "  [Flags]" : ""));
                foreach (var p in t.GetProperties(Flags).OrderBy(p => p.Name))
                    Line($"  prop {p.Name,-40} : ", () => Sig(p.PropertyType) + Accessors(p));
                foreach (var f in t.GetFields(Flags).OrderBy(f => f.Name))
                    Line($"  fld  {f.Name,-40} : ", () => Sig(f.FieldType));
                foreach (var f in t.GetFields(StaticFlags).OrderBy(f => f.Name))
                    // Для enum и const печатаем ЧИСЛО: без него член enum нельзя перенести в другую
                    // сборку — имя без значения бесполезно, а угадывать значения запрещено.
                    Line($"  sfld {f.Name,-40} : ", () => Sig(f.FieldType) + ConstValue(f));
                foreach (var p in t.GetProperties(StaticFlags).OrderBy(p => p.Name))
                    Line($"  sprop {p.Name,-39} : ", () => Sig(p.PropertyType) + Accessors(p));
                foreach (var m in t.GetMethods(Flags).Where(m => !m.IsSpecialName).OrderBy(m => m.Name))
                    Line($"  meth {m.Name,-40} ", () => $"({Params(m)}) : {Sig(m.ReturnType)}");
                // Статические методы отдельной пометкой: у ExileCore так выглядит, например,
                // вся поверхность DebugWindow.Log*, и без них таблица врала бы «метода нет».
                foreach (var m in t.GetMethods(StaticFlags).Where(m => !m.IsSpecialName).OrderBy(m => m.Name))
                    Line($"  smeth {m.Name,-39} ", () => $"({Params(m)}) : {Sig(m.ReturnType)}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                // Тип не разобрался целиком — печатаем это строкой и идём дальше. Оборванный на
                // середине вывод хуже: отсутствие оставшихся типов читается как факт о сборке.
                Console.WriteLine($"  !! разбор типа прерван: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine();
            }
        }
        break;

    case "--jagged":
        foreach (string needle in rest)
            foreach (var t in allTypes.Where(t => t.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
                                      .OrderBy(SafeName))
            {
                var hits = new List<string>();
                foreach (var p in t.GetProperties(Flags))
                    try { if (p.CanRead && LooksJagged(p.PropertyType)) hits.Add($"prop {p.Name} : {Sig(p.PropertyType)}"); } catch { /* тип члена не разрешается — см. Line() */ }
                foreach (var f in t.GetFields(Flags))
                    try { if (LooksJagged(f.FieldType)) hits.Add($"fld  {f.Name} : {Sig(f.FieldType)}"); } catch { /* то же */ }
                if (hits.Count == 0) continue;
                Console.WriteLine($"### {SafeName(t)}");
                foreach (string h in hits) Console.WriteLine("  " + h);
            }
        break;

    // Раскладка структуры в памяти: LayoutKind/Pack/Size типа и FieldOffset каждого поля.
    // Это единственный способ узнать ЧИСЛОВЫЕ оффсеты на обфусцированной сборке: тела
    // методов ILSpy не восстанавливает, а атрибуты раскладки лежат в метаданных открыто.
    case "--layout":
        foreach (string needle in rest)
        {
            var matches = allTypes.Where(t => t.Name.Equals(needle, StringComparison.OrdinalIgnoreCase)
                                           || SafeName(t).Contains(needle, StringComparison.OrdinalIgnoreCase)).ToList();
            if (matches.Count == 0) { Console.WriteLine($"### {needle}: ТИП НЕ НАЙДЕН"); continue; }
            foreach (var t in matches.OrderBy(SafeName))
            {
                if (t.Name.Contains('<')) continue; // компиляторный мусор
                string kind = Try(() => t.IsExplicitLayout ? "Explicit" : t.IsLayoutSequential ? "Sequential" : "Auto");
                Console.WriteLine($"### {SafeName(t)}   [{t.Assembly.GetName().Name}]  layout={kind}" +
                                  (t.IsValueType ? "  (struct)" : "  (class)"));
                foreach (var f in t.GetFields(Flags).Concat(t.GetFields(StaticFlags)))
                {
                    string off = "";
                    foreach (var cad in f.GetCustomAttributesData())
                    {
                        if (cad.AttributeType.Name != "FieldOffsetAttribute") continue;
                        var v = cad.ConstructorArguments.Count > 0 ? cad.ConstructorArguments[0].Value : null;
                        if (v is int i) off = $"0x{i:X}";
                    }
                    Line($"  {(off.Length > 0 ? off.PadLeft(8) : "       ?")}  {f.Name,-40} : ",
                         () => Sig(f.FieldType) + (f.IsStatic ? "  (static)" : ""));
                }
                Console.WriteLine();
            }
        }
        break;

    case "--grepmember":
        foreach (string needle in rest)
        {
            Console.WriteLine($"### члены, содержащие \"{needle}\":");
            foreach (var t in allTypes.OrderBy(SafeName))
            try
            {
                foreach (var p in t.GetProperties(Flags))
                    if (p.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
                        Line($"  {SafeName(t)}.{p.Name} : ", () => Sig(p.PropertyType) + "  (prop)");
                foreach (var f in t.GetFields(Flags))
                    if (f.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
                        Line($"  {SafeName(t)}.{f.Name} : ", () => Sig(f.FieldType) + "  (fld)");
                foreach (var m in t.GetMethods(Flags).Where(m => !m.IsSpecialName))
                    if (m.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
                        Line($"  {SafeName(t)}.{m.Name}(...) : ", () => Sig(m.ReturnType) + "  (meth)");
                foreach (var m in t.GetMethods(StaticFlags).Where(m => !m.IsSpecialName))
                    if (m.Name.Contains(needle, StringComparison.OrdinalIgnoreCase))
                        Line($"  {SafeName(t)}.{m.Name}(...) : ", () => Sig(m.ReturnType) + "  (static meth)");
            }
            catch (Exception ex)
            {
                // Сам перебор членов типа тоже может бросить (базовый тип из недостающей сборки).
                // Молча пропустить нельзя: пропуск типа при поиске «а как оно тут зовётся» — это
                // ложный отрицательный ответ, дороже которого в этом проекте ничего нет.
                Console.WriteLine($"  !! {SafeName(t)}: перебор прерван — {ex.GetType().Name}");
            }
        }
        break;

    case "--spec":
        {
            // Файлов может быть несколько: карта поверхности живёт отдельно от зондов самого плагина
            // (spec.json — «имена, которые угадывает код», surface.json — «что вообще можно прочитать»),
            // но перепроверяются ОДНОЙ командой, иначе половина карты тихо устареет.
            var specPaths = rest.Length > 0 ? rest : new[] { Path.Combine(AppContext.BaseDirectory, "spec.json") };
            var spec = new List<SpecEntry>();
            foreach (string specPath in specPaths)
            {
                if (!File.Exists(specPath)) { Console.Error.WriteLine($"ForkProbe: нет файла {specPath}"); return 2; }
                spec.AddRange(JsonSerializer.Deserialize<List<SpecEntry>>(File.ReadAllText(specPath),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!);
            }

            Console.WriteLine("подсистема\tтип\tискали\tнашли\tвердикт\tгде в коде");
            int missing = 0, nonPublic = 0;
            foreach (var e in spec)
            {
                var t = allTypes.FirstOrDefault(x => x.Name.Equals(e.Type, StringComparison.Ordinal))
                     ?? allTypes.FirstOrDefault(x => x.Name.Equals(e.Type, StringComparison.OrdinalIgnoreCase))
                     ?? allTypes.FirstOrDefault(x => x.FullName!.Equals(e.Type, StringComparison.OrdinalIgnoreCase))
                     ?? allTypes.FirstOrDefault(x => x.FullName!.EndsWith("." + e.Type, StringComparison.OrdinalIgnoreCase));
                if (t == null)
                {
                    if (!e.KnownMiss) missing++;
                    Console.WriteLine($"{e.Subsystem}\t{e.Type}\t{string.Join("/", e.Candidates)}\t—\tТИП НЕ НАЙДЕН\t{e.Note}");
                    continue;
                }

                var found = new List<string>();
                var absent = new List<string>();
                var hidden = new List<string>(); // найдено, но НЕ public — из плагина не вызвать
                foreach (string c in e.Candidates)
                {
                    if (e.Kind == "method")
                    {
                        var mm = t.GetMethods(Flags).Concat(t.GetMethods(StaticFlags))
                                  .FirstOrDefault(m => m.Name.Equals(c, StringComparison.Ordinal));
                        if (mm == null) { absent.Add(c); continue; }
                        found.Add(c + (mm.IsPublic ? "" : NonPublicMark));
                        if (!mm.IsPublic) hidden.Add(c);
                        continue;
                    }
                    var p = t.GetProperty(c, Flags);
                    if (p != null && p.CanRead)
                    {
                        bool pub = p.GetMethod?.IsPublic == true;
                        found.Add($"{c}:{Try(() => Sig(p.PropertyType))}{(pub ? "" : NonPublicMark)}");
                        if (!pub) hidden.Add(c);
                        continue;
                    }
                    var f = t.GetField(c, Flags) ?? t.GetField(c, StaticFlags); // StaticFlags — для значений enum
                    if (f != null)
                    {
                        found.Add($"{c}:{Try(() => Sig(f.FieldType))}{(f.IsPublic ? "" : NonPublicMark)}");
                        if (!f.IsPublic) hidden.Add(c);
                        continue;
                    }
                    absent.Add(c);
                }

                // Два разных вопроса об одной строке:
                //   requireAll=false (по умолчанию) — «сработает ли перебор кандидатов в коде»: достаточно ОДНОГО;
                //   requireAll=true — «читается ли ВЕСЬ этот набор членов»: карта поверхности API обязана
                //   краснеть от пропажи любого поля, иначе схема молча потеряет поле при смене форка.
                bool ok = e.RequireAll ? absent.Count == 0 && found.Count > 0 : found.Count > 0;
                if (!ok && !e.KnownMiss) missing++;
                // Найденный, но НЕ public член — отдельный вердикт, а не «OK». Зонд ищет с флагом
                // NonPublic (так же, как рефлексия в проде), но ТИПИЗИРОВАННЫЙ код такой член вызвать
                // не может: компилятор даёт CS0122. Без этой отметки карта обещает схеме API поля,
                // которых у неё не будет.
                if (ok && hidden.Count > 0) nonPublic++;
                string verdict = ok
                    ? (hidden.Count > 0 ? "OK, НО НЕ PUBLIC: " + string.Join("/", hidden) : "OK")
                    : e.KnownMiss ? "ИЗВЕСТНЫЙ ПРОМАХ"
                    : e.RequireAll && found.Count > 0 ? "НЕТ ЧАСТИ: " + string.Join("/", absent)
                    : "НЕ НАЙДЕНО";
                Console.WriteLine($"{e.Subsystem}\t{SafeName(t)}\t{string.Join("/", e.Candidates)}\t" +
                                  $"{string.Join(" | ", found)}\t{verdict}\t{e.Note}");
            }
            Console.Error.WriteLine($"# строк: {spec.Count}, требуют внимания: {missing}, " +
                                    $"из них только-рефлексией (не public): {nonPublic}");
            return missing == 0 ? 0 : 1; // ненулевой код — чтобы зонд можно было поставить в CI форка
        }

    default:
        Console.Error.WriteLine("режимы: --types --dump --jagged --layout --grepmember --spec");
        return 2;
}
return 0;

// Защищённая печать строки члена. Причина: MetadataLoadContext разбирает сигнатуру ЛЕНИВО, и член,
// чей тип лежит в сборке, которой рядом нет (у этого форка — Microsoft.Build.Utilities.Core через
// ItemFilterLibrary), бросает FileNotFoundException в момент ЧТЕНИЯ типа, а не загрузки сборки.
// Без этой защиты широкий --dump умирал посреди вывода, и отсутствие оставшихся типов выглядело как
// факт о сборке — ровно тот класс молчаливого отказа, против которого зонд и сделан.
static void Line(string prefix, Func<string> tail)
{
    string t;
    try { t = tail(); }
    catch (Exception ex) { t = $"?  (тип не разрешён: {ex.GetType().Name})"; }
    Console.WriteLine(prefix + t);
}

static string Try(Func<string> f)
{
    try { return f(); }
    catch (Exception ex) { return $"?({ex.GetType().Name})"; }
}

// Имя типа тоже читается лениво и тоже может бросить — например у обобщённого типа с чужим аргументом.
static string SafeName(Type t)
{
    try { return t.FullName ?? t.Name; }
    catch { return t.Name; }
}

// Доступность аксессоров свойства. Отвечает на вопрос, который не решается ни именем, ни телом
// метода: «сможет ли плагин ПРОЧИТАТЬ и ПРИСВОИТЬ этот член типизированно». Видимость аксессоров
// лежит в метаданных и обфускацией не затрагивается, поэтому ответ статический и точный.
// Пусто = обычное публичное чтение-запись; всё остальное печатается явно.
static string Accessors(PropertyInfo p)
{
    string get = p.GetMethod == null ? "нет" : Vis(p.GetMethod);
    string set = p.SetMethod == null ? "нет" : Vis(p.SetMethod);
    if (get == "public" && set == "public") return "";
    return $"   (get: {get}, set: {set})";

    static string Vis(MethodInfo m) =>
        m.IsPublic ? "public"
        : m.IsFamily ? "protected"
        : m.IsAssembly ? "internal"
        : m.IsFamilyOrAssembly ? "protected internal"
        : m.IsFamilyAndAssembly ? "private protected"
        : "private";
}

// Та же проверка формы, что у TerrainLayerProbe.LooksJagged: object/Array статически не решаются.
static bool LooksJagged(Type t)
{
    if (t == null) return false;
    if (t.FullName is "System.Object" or "System.Array") return true;
    if (!t.IsArray || t.GetArrayRank() != 1) return false;
    var e = t.GetElementType();
    return e != null && e.IsArray && e.GetArrayRank() == 1;
}

// Значение константы/члена enum, если оно есть. GetRawConstantValue работает и в
// MetadataLoadContext (читает метаданные, а не исполняет код), поэтому доступно без игры.
static string ConstValue(FieldInfo f)
{
    if (!f.IsLiteral) return "";
    try
    {
        object? v = f.GetRawConstantValue();
        return v == null ? "" : $" = {Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)}";
    }
    catch { return ""; }
}

// Список параметров метода в короткой подписи.
static string Params(MethodInfo m) =>
    string.Join(", ", m.GetParameters().Select(x => Sig(x.ParameterType) + " " + x.Name));

// Короткая подпись типа: без общих префиксов, иначе таблица не читается.
static string Sig(Type t) => (t.FullName ?? t.Name)
    .Replace("System.", "").Replace("ExileCore.", "").Replace("GameOffsets.", "");

internal sealed class SpecEntry
{
    public string Subsystem { get; set; } = "";
    public string Type { get; set; } = "";
    public string Kind { get; set; } = "member";
    public List<string> Candidates { get; set; } = new();
    public string Note { get; set; } = "";

    /// <summary>
    /// Промах, который УЖЕ разобран и признан безвредным (у подсистемы есть рабочий путь помимо
    /// этого зонда). Такая строка печатается как «ИЗВЕСТНЫЙ ПРОМАХ» и НЕ влияет на код возврата —
    /// иначе зонд навсегда остался бы красным и перестал годиться в гейт. Разбор каждого такого
    /// случая обязан лежать в docs/OPTIMIZATION_LOG.md; ставить флаг без разбора запрещено.
    /// </summary>
    public bool KnownMiss { get; set; }

    /// <summary>
    /// Смысл списка <see cref="Candidates"/>: по умолчанию это АЛЬТЕРНАТИВЫ (код перебирает
    /// имена и берёт первое попавшееся), и строка зелёная при одном попадании. С <c>requireAll: true</c>
    /// это НАБОР: строка зелёная, только если найдены ВСЕ имена. Второй режим — для карты
    /// поверхности (surface.json), где пропажа одного поля означает дыру в схеме API, а не смену кандидата.
    /// </summary>
    public bool RequireAll { get; set; }
}
