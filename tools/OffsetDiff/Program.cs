using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

// offsetdiff — сравнение ЧИСЛОВЫХ смещений полей между двумя сборками GameOffsets.
//
// Зачем. Паритет API между форками бессмысленно обсуждать, не зная, целятся ли они в ОДИН И ТОТ ЖЕ
// патч игры. Если смещения расходятся, то перенос типов/членов — только половина работы, а числа
// придётся брать заново; если совпадают — порт механический.
//
// Смещение берётся Marshal.OffsetOf — то есть МАРШАЛИРУЕМОЙ раскладкой, а не управляемой.
// Это принципиально: у эталонных структур последним полем идёт `object EndMarker` (сентинел
// размера), и CLR в УПРАВЛЯЕМОЙ раскладке поднимает поле-ссылку в начало, сдвигая все остальные
// на 8 байт. Первая версия этого инструмента мерила именно управляемую раскладку через ldflda и
// поэтому давала неверные числа для ВСЕХ структур с EndMarker. Для чтения чужого процесса значение
// имеет только маршалируемая раскладка.
//
// Обе сборки называются "GameOffsets", поэтому грузятся в РАЗНЫЕ контексты — иначе вторая тихо
// подменится первой, и дифф покажет идеальное совпадение на пустом месте.
internal static class Program
{
    private sealed class Ctx : AssemblyLoadContext
    {
        public Ctx(string name) : base(name, isCollectible: false) { }
        protected override Assembly? Load(AssemblyName n) => null; // всё остальное — из default
    }

    /// <summary>
    /// Маршалируемое смещение поля. Бросает для полей, которые не маршалируются (например
    /// <c>object EndMarker</c>) — такие поля в сравнение не попадают, и это правильно: в памяти
    /// игры их нет.
    /// </summary>
    private static int NativeOffset(Type t, FieldInfo f) => Marshal.OffsetOf(t, f.Name).ToInt32();

    private static Dictionary<string, Dictionary<string, int>> Map(string path, string ctxName,
                                                                   out int failedTypes)
    {
        var asm = new Ctx(ctxName).LoadFromAssemblyPath(path);
        Type[] types;
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types.Where(t => t != null).ToArray()!; }

        var map = new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
        failedTypes = 0;
        foreach (var t in types)
        {
            if (!t.IsValueType || t.IsEnum || t.Name.Contains('<')) continue;
            var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (fields.Length == 0) continue;
            var per = new Dictionary<string, int>(StringComparer.Ordinal);
            try
            {
                foreach (var f in fields)
                {
                    try { per[f.Name] = NativeOffset(t, f); } catch { /* поле не маршалируется — в памяти игры его нет */ }
                }
            }
            catch { failedTypes++; continue; }
            if (per.Count > 0) map[t.Name] = per;
        }
        return map;
    }

    private static int Main(string[] args)
    {
        if (args.Length < 2)
        {
            Console.Error.WriteLine("offsetdiff <GameOffsets_A.dll> <GameOffsets_B.dll> [--all]");
            return 2;
        }
        bool all = args.Contains("--all");

        // Режим проверки САМОГО инструмента: печатает все поля названной структуры из обеих сборок.
        // Нужен потому, что вывод «оффсеты расходятся» бессмысленно принимать на веру, не убедившись,
        // что измеритель не врёт сам.
        int dumpAt = Array.IndexOf(args, "--dump");
        if (dumpAt >= 0 && dumpAt + 1 < args.Length)
        {
            string needle = args[dumpAt + 1];
            foreach (var (path, tag) in new[] { (args[0], "A"), (args[1], "B") })
            {
                var m = Map(path, tag + "d", out _);
                Console.WriteLine($"--- {tag}: {path}");
                foreach (var kv in m.Where(k => k.Key.Contains(needle, StringComparison.OrdinalIgnoreCase)))
                {
                    Console.WriteLine($"  ### {kv.Key}");
                    foreach (var f in kv.Value.OrderBy(x => x.Value))
                        Console.WriteLine($"      0x{f.Value:X4}  {f.Key}");
                }
            }
            return 0;
        }

        var a = Map(args[0], "A", out int failA);
        var b = Map(args[1], "B", out int failB);
        Console.WriteLine($"A: {a.Count} структур ({failA} не разложились)   {args[0]}");
        Console.WriteLine($"B: {b.Count} структур ({failB} не разложились)   {args[1]}");

        var common = a.Keys.Intersect(b.Keys, StringComparer.Ordinal).OrderBy(x => x, StringComparer.Ordinal).ToList();
        Console.WriteLine($"общих структур: {common.Count};  только в A: {a.Count - common.Count};  только в B: {b.Count - common.Count}");
        Console.WriteLine();

        int same = 0, diff = 0, onlyA = 0, onlyB = 0, typesAllSame = 0, typesAnyDiff = 0;
        var report = new List<string>();
        foreach (string tn in common)
        {
            var fa = a[tn];
            var fb = b[tn];
            var shared = fa.Keys.Intersect(fb.Keys, StringComparer.Ordinal).ToList();
            onlyA += fa.Count - shared.Count;
            onlyB += fb.Count - shared.Count;
            if (shared.Count == 0) continue;

            var mismatches = shared.Where(f => fa[f] != fb[f]).OrderBy(f => fa[f]).ToList();
            same += shared.Count - mismatches.Count;
            diff += mismatches.Count;
            if (mismatches.Count == 0) { typesAllSame++; if (!all) continue; }
            else typesAnyDiff++;

            report.Add($"### {tn}   общих полей {shared.Count}, расходится {mismatches.Count}");
            foreach (string f in mismatches)
                report.Add($"    {f,-34} A=0x{fa[f]:X}  B=0x{fb[f]:X}");
        }

        Console.WriteLine($"общих полей: {same + diff};  СОВПАЛО: {same};  РАЗОШЛОСЬ: {diff}");
        Console.WriteLine($"структур с полным совпадением: {typesAllSame};  с расхождениями: {typesAnyDiff}");
        Console.WriteLine($"полей только в A: {onlyA};  только в B: {onlyB}");
        Console.WriteLine();
        foreach (string line in report) Console.WriteLine(line);
        return diff == 0 ? 0 : 1;
    }
}
