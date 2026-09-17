// ─────────────────────────────────────────────────────────────────────────────────────────────────
// EntCap — атомарный снимок СЛОЯ СУЩНОСТЕЙ живого клиента. ТОЛЬКО ЧТЕНИЕ.
//
// Зачем отдельный инструмент. Проверка гипотез о раскладке сущности требует десятка согласованных
// чтений (Data -> EntityList -> обход дерева -> details -> lookup -> слоты -> компоненты). Делать их
// отдельными запусками нельзя: между двумя командами человек меняет зону, Data переезжает, и старый
// адрес начинает указывать в чужую память. Однажды это уже дало дамп, целиком состоящий из float'ов.
// Здесь всё чтение идёт в ОДНОМ процессе, и снимок обрамлён проверкой «зона/Data не менялись»:
// если сменились — печатается НЕДЕЙСТВИТЕЛЕН и код возврата 3, а не число.
//
// Ничего не «чинится» и не подгоняется: инструмент печатает сырые числа и явно отделяет
// ПРОВЕРЯЕМЫЙ КРИТЕРИЙ (например «длина по +0x18 равна длине строки по +0x08») от самого числа.
//
// Вышележащие смещения взяты ЗАКРЫТЫМИ (docs/offsets.md): IngameState.Data = 0x218,
// IngameData.CurrentAreaLevel = 0xD4, .CurrentAreaHash = 0x114, .EntityList = 0xA28,
// .EntitiesCount = 0xA30. Проверяемые здесь гипотезы — только про раскладку САМОЙ СУЩНОСТИ.
//
// Использование:  EntCap.exe --igs <адрес IngameState в HEX> [--max N] [--slots]
//   --igs    адрес IngameState; берётся у FindOffset.exe --ingame-state. Он стабилен на протяжении
//            запуска клиента (объект состояния), в отличие от Data, который переезжает со сменой зоны.
//   --max    сколько сущностей разобрать подробно (по умолчанию 12).
//   --slots  дополнительно печатать полный дамп слот-таблицы и вектора компонентов.
//   --census <RVA vtable в HEX> [--census-bytes N]
//            ПЕРЕПИСЬ поля по популяции: собрать этот компонент со ВСЕХ сущностей зоны и
//            напечатать, какие байты строго булевы и непостоянны, а какие постоянны по всей
//            зоне. Ищет смещение признака вместо того, чтобы проверять подставленное.
//
// Коды возврата: 0 — снимок действителен; 2 — читать нечего; 3 — состояние сменилось посередине.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

internal static class Program
{
    private const int ExitOk = 0;
    private const int ExitNothingToRead = 2;
    private const int ExitStale = 3;

    // ── Закрытые смещения вышележащего слоя (docs/offsets.md) ────────────────────────────────────
    private const long IgsData = 0x218;
    private const long DataAreaLevel = 0xD4;
    private const long DataAreaHash = 0x114;
    private const long DataEntityList = 0xA28;
    private const long DataEntitiesCount = 0xA30;

    // ── Гипотезы о раскладке сущности (survey 2026-09-16, замерено, НЕ переподтверждено) ─────────
    private const long EntDetails = 0x08;   // EntityDetails*
    private const long EntComps = 0x10;     // StdVector указателей на компоненты
    private const long EntInventoryId = 0x70;
    private const long EntIngameData = 0x78;
    private const long EntId = 0x88;
    private const long EntFlags = 0x8C;
    private const long EntPositioned = 0x98; // прямой указатель на Positioned, минуя таблицу

    private const long DetPathPtr = 0x08;
    private const long DetPathLen = 0x18;
    private const long DetLookup = 0x28;

    private const long LookupVtable = 0x10;
    private const long LookupSlots = 0x40;   // StdVector слотов {uint32 nameId, uint32 index}

    private static IntPtr _handle;
    private static bool _raw;
    private static bool _comp;
    private static bool _byvtbl;
    private static bool _posdump;
    private static bool _items;
    private static int _watch;
    private static long _census = -1;
    private static int _censusBytes = 0x100;
    private static long _moduleBase;

    private static int Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* консоли может не быть */ }

        var igsText = Arg(args, "--igs");
        var maxText = Arg(args, "--max");
        var dumpSlots = args.Contains("--slots");
        _raw = args.Contains("--raw");
        _comp = args.Contains("--comp");
        _byvtbl = args.Contains("--byvtbl");
        _posdump = args.Contains("--posdump");
        _items = args.Contains("--items");
        _watch = int.TryParse(Arg(args, "--watch"), out var w) ? w : 0;
        if (TryHex(Arg(args, "--census") ?? "", out var cen)) _census = cen;
        if (int.TryParse(Arg(args, "--census-bytes"), out var cb) && cb >= 0x20 && cb <= 0x800)
            _censusBytes = cb;
        var max = int.TryParse(maxText, out var m) ? m : 12;

        if (igsText == null || !TryHex(igsText, out var igs))
        {
            Console.WriteLine("нужен адрес IngameState: EntCap.exe --igs <HEX> [--max N] [--slots]");
            Console.WriteLine("взять его у: FindOffset.exe --ingame-state");
            return ExitNothingToRead;
        }

        var process = Process.GetProcesses()
            .FirstOrDefault(p => p.ProcessName.IndexOf("pathofexile", StringComparison.OrdinalIgnoreCase) >= 0);

        if (process == null)
        {
            Console.WriteLine("игра не найдена.");
            return ExitNothingToRead;
        }

        // PROCESS_VM_READ | PROCESS_QUERY_INFORMATION — ничего сверх чтения.
        _moduleBase = process.MainModule.BaseAddress.ToInt64();
        _handle = OpenProcess(0x0010 | 0x0400, false, process.Id);

        if (_handle == IntPtr.Zero)
        {
            Console.WriteLine($"не удалось открыть процесс {process.Id} на чтение (код {Marshal.GetLastWin32Error()}).");
            return ExitNothingToRead;
        }

        Console.WriteLine("EntCap — атомарный снимок слоя сущностей. ТОЛЬКО ЧТЕНИЕ.");
        Console.WriteLine($"процесс {process.ProcessName} (pid {process.Id}), IngameState 0x{igs:X}");
        Console.WriteLine();

        // ── Рамка «до» ───────────────────────────────────────────────────────────────────────────
        var data = Q(igs + IgsData);
        if (!IsPointer(data))
        {
            Console.WriteLine($"IngameState+0x218 = 0x{data:X} — не похоже на указатель. Персонаж не в зоне?");
            return ExitNothingToRead;
        }

        var levelBefore = (int)U32(data + DataAreaLevel);
        var hashBefore = U32(data + DataAreaHash);

        Console.WriteLine($"состояние ДО:  Data 0x{data:X}  уровень {levelBefore}  хэш 0x{hashBefore:X}");

        if (_watch > 0) return Watch(igs, _watch);

        var listAddress = Q(data + DataEntityList);
        var declaredCount = U32(data + DataEntitiesCount);

        Console.WriteLine($"EntityList 0x{listAddress:X}   EntitiesCount (заявлено игрой) {declaredCount}");

        if (!IsPointer(listAddress))
        {
            Console.WriteLine("EntityList не похож на указатель — дальше читать нечего.");
            return ExitNothingToRead;
        }

        var entities = WalkEntityList(listAddress);
        Console.WriteLine($"обходом дерева найдено сущностей: {entities.Count}");
        Console.WriteLine();

        if (entities.Count == 0)
        {
            Console.WriteLine("обход не дал ни одной сущности.");
            return ExitNothingToRead;
        }

        if (_census >= 0)
        {
            Census(entities, _census, _censusBytes);

            // Рамка «после» — та же, что у остальных режимов: перепись идёт по всей зоне и стоит
            // секунды, за которые человек успевает сменить зону.
            var dAfter = Q(igs + IgsData);
            var hAfter = IsPointer(dAfter) ? U32(dAfter + DataAreaHash) : 0;
            Console.WriteLine();
            if (data == dAfter && hashBefore == hAfter)
            {
                Console.WriteLine($"СНИМОК ДЕЙСТВИТЕЛЕН: зона и база Data не менялись (0x{hashBefore:X}, 0x{data:X})");
                return ExitOk;
            }

            Console.WriteLine($"СНИМОК НЕДЕЙСТВИТЕЛЕН: было 0x{hashBefore:X}/0x{data:X}, " +
                              $"стало 0x{hAfter:X}/0x{dAfter:X} — повторить, не используя числа выше.");
            return ExitStale;
        }

        // ── Разбор сущностей ─────────────────────────────────────────────────────────────────────
        Console.WriteLine("РАЗБОР СУЩНОСТЕЙ (гипотезы survey 2026-09-16, проверяются содержимым)");
        Console.WriteLine(new string('=', 100));

        var vtables = new Dictionary<long, int>();
        var pathOk = 0;
        var pathTotal = 0;
        var harvest = new Dictionary<uint, List<string>>();

        foreach (var ent in entities.Take(max))
        {
            pathTotal++;
            if (DescribeEntity(ent, dumpSlots, vtables, harvest)) pathOk++;
            Console.WriteLine();
        }

        // ── Сводки ───────────────────────────────────────────────────────────────────────────────
        Console.WriteLine(new string('=', 100));
        Console.WriteLine("СВОДКА");
        Console.WriteLine(new string('-', 100));
        Console.WriteLine($"  путь прочитан и начинается с Metadata/: {pathOk} из {pathTotal}");
        Console.WriteLine($"  vtable сущностей (значение -> сколько раз): " +
                          string.Join(", ", vtables.OrderByDescending(x => x.Value)
                              .Select(x => $"0x{x.Key:X}={x.Value}")));

        // Полный проход по ВСЕМ сущностям: статистика критерия длины пути (одна сущность могла
        // совпасть случайно, сотня — нет) и корреляция «nameId -> какие сущности им обладают».
        // Корреляция сама по себе имени НЕ ДАЁТ: она лишь сужает круг. Имя даёт только оракул.
        int allOk = 0, allRead = 0;
        var corr = new Dictionary<uint, SortedSet<string>>();
        foreach (var ent in entities)
        {
            var det = Q(ent + EntDetails);
            if (!IsPointer(det)) continue;
            var ptr = Q(det + DetPathPtr);
            var len = Q(det + DetPathLen);
            if (!IsPointer(ptr) || len <= 0 || len > 512) continue;
            allRead++;
            var s = ReadUtf16(ptr, (int)len);
            if (s != null && s.Length == len && s.StartsWith("Metadata/", StringComparison.Ordinal)) allOk++;

            var lk = Q(det + DetLookup);
            if (!IsPointer(lk)) continue;
            var sf = Q(lk + LookupSlots);
            var sl = Q(lk + LookupSlots + 8);
            if (!IsPointer(sf) || sl < sf || sl - sf > 0x2000) continue;

            // Ключ корреляции — ВИД сущности, а не путь целиком: "Metadata/Chests" вместо
            // "Metadata/Chests/DarkPot2v2". Иначе таблица распухает вариантами одного и того же.
            var kind = s == null ? "?" : string.Join("/", s.Split('/').Take(2));

            for (long i = 0; i < (sl - sf) / 8 && i < 256; i++)
            {
                var slot = Q(sf + i * 8);
                var nid = (uint)(slot & 0xFFFFFFFF);
                if (nid == 0) continue;
                if (!corr.TryGetValue(nid, out var set)) corr[nid] = set = new SortedSet<string>();
                set.Add(kind);
            }
        }

        Console.WriteLine($"  КРИТЕРИЙ ПУТИ по всем {entities.Count} сущностям: " +
                          $"{allOk} из {allRead} дали строку, чья длина РАВНА числу по +0x18 и которая " +
                          $"начинается с Metadata/");

        if (corr.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine($"  КОРРЕЛЯЦИЯ nameId -> виды сущностей, у которых он есть ({corr.Count} разных nameId");
            Console.WriteLine("  по всем сущностям зоны). Имя компонента отсюда НЕ следует — только круг кандидатов:");
            foreach (var kv in corr.OrderBy(x => x.Key))
                Console.WriteLine($"    0x{kv.Key:X3}  {string.Join(", ", kv.Value)}");
        }

        if (harvest.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("  nameId, встреченные в слот-таблицах (nameId -> сколько сущностей):");
            foreach (var kv in harvest.OrderBy(x => x.Key))
                Console.WriteLine($"    0x{kv.Key:X3}  у {kv.Value.Count} сущн.  напр. {kv.Value.First()}");
        }

        // ── Рамка «после» ────────────────────────────────────────────────────────────────────────
        var dataAfter = Q(igs + IgsData);
        var hashAfter = IsPointer(dataAfter) ? U32(dataAfter + DataAreaHash) : 0;

        Console.WriteLine();
        if (data == dataAfter && hashBefore == hashAfter)
        {
            Console.WriteLine($"СНИМОК ДЕЙСТВИТЕЛЕН: зона и база Data не менялись (0x{hashBefore:X}, 0x{data:X})");
            return ExitOk;
        }

        Console.WriteLine($"СНИМОК НЕДЕЙСТВИТЕЛЕН: было 0x{hashBefore:X}/0x{data:X}, " +
                          $"стало 0x{hashAfter:X}/0x{dataAfter:X} — повторить, не используя числа выше.");
        return ExitStale;
    }

    // Печатает одну сущность. Возвращает true, если путь прочитался и выглядит как путь метаданных.
    private static bool DescribeEntity(long ent, bool dumpSlots,
        Dictionary<long, int> vtables, Dictionary<uint, List<string>> harvest)
    {
        var vtable = Q(ent);
        vtables.TryGetValue(vtable, out var seen);
        vtables[vtable] = seen + 1;

        var details = Q(ent + EntDetails);
        var id = U32(ent + EntId);
        var invId = U32(ent + EntInventoryId);
        var flagsByte = B(ent + EntFlags);
        var flagsDword = U32(ent + EntFlags);
        var ingameData = Q(ent + EntIngameData);
        var positionedDirect = Q(ent + EntPositioned);

        // Вектор компонентов: First/Last/End по 8 байт. Количество компонентов = (Last-First)/8 —
        // именно оно теряется, если читать ComponentList одиночным long.
        var compFirst = Q(ent + EntComps);
        var compLast = Q(ent + EntComps + 8);
        var compEnd = Q(ent + EntComps + 0x10);
        var compCount = (IsPointer(compFirst) && compLast >= compFirst && compLast - compFirst < 0x4000)
            ? (compLast - compFirst) / 8
            : -1;

        string path = null;
        long pathLen = 0;
        var pathPtr = 0L;

        if (IsPointer(details))
        {
            pathPtr = Q(details + DetPathPtr);
            pathLen = Q(details + DetPathLen);
            if (IsPointer(pathPtr) && pathLen > 0 && pathLen <= 512)
                path = ReadUtf16(pathPtr, (int)pathLen);
        }

        var pathGood = path != null && path.Length == pathLen &&
                       path.StartsWith("Metadata/", StringComparison.Ordinal);

        Console.WriteLine($"сущность 0x{ent:X}");

        if (_raw) RawDump(ent, 0xB0);

        Console.WriteLine($"  vtable      0x{vtable:X}");
        Console.WriteLine($"  details     0x{details:X}   (ent+0x08)");
        Console.WriteLine($"  путь        {(path == null ? "НЕ ПРОЧИТАН" : "\"" + path + "\"")}");
        Console.WriteLine($"              строка [details+0x08] = 0x{pathPtr:X}, длина [details+0x18] = {pathLen}" +
                          $"{(path == null ? "" : $", фактическая длина строки {path.Length}")}" +
                          $"   -> КРИТЕРИЙ {(pathGood ? "СОШЁЛСЯ" : "НЕ СОШЁЛСЯ")}");
        Console.WriteLine($"  Id          {id} (0x{id:X})   (ent+0x88)");
        Console.WriteLine($"  InventoryId {invId} (0x{invId:X})   (ent+0x70)");
        Console.WriteLine($"  Flags       байт 0x{flagsByte:X2}   dword 0x{flagsDword:X8}   (ent+0x8C)");
        Console.WriteLine($"  IngameData  0x{ingameData:X}   (ent+0x78)");
        Console.WriteLine($"  компоненты  вектор ent+0x10: First 0x{compFirst:X} Last 0x{compLast:X} " +
                          $"End 0x{compEnd:X}  -> штук {compCount}");

        if (!IsPointer(details))
        {
            Console.WriteLine("  details не похож на указатель — слот-таблицу читать не из чего.");
            return pathGood;
        }

        var lookup = Q(details + DetLookup);
        Console.WriteLine($"  lookup      0x{lookup:X}   (details+0x28)");

        if (!IsPointer(lookup))
        {
            Console.WriteLine("  lookup не похож на указатель — слот-таблицы нет.");
            return pathGood;
        }

        Console.WriteLine($"  lookup.vtbl 0x{Q(lookup + LookupVtable):X}   (lookup+0x10, запасной ключ типа)");

        var slotFirst = Q(lookup + LookupSlots);
        var slotLast = Q(lookup + LookupSlots + 8);
        var slotEnd = Q(lookup + LookupSlots + 0x10);

        Console.WriteLine($"  слоты       вектор lookup+0x40: First 0x{slotFirst:X} Last 0x{slotLast:X} " +
                          $"End 0x{slotEnd:X}");

        if (!IsPointer(slotFirst) || slotLast < slotFirst || slotLast - slotFirst > 0x2000)
        {
            Console.WriteLine("  вектор слотов не похож на вектор — разбор слотов пропущен.");
            return pathGood;
        }

        var slotCount = (slotLast - slotFirst) / 8;
        Console.WriteLine($"              слотов {slotCount} (ожидаем степень двойки; непустых должно " +
                          $"быть {compCount})");

        var filled = 0;

        // Предел стоит намеренно: по устаревшему указателю обход когда-то съел ~4 ГиБ.
        for (long i = 0; i < slotCount && i < 256; i++)
        {
            var slot = Q(slotFirst + i * 8);
            if (slot == 0) continue;

            var nameId = (uint)(slot & 0xFFFFFFFF);
            var index = (uint)((ulong)slot >> 32);

            if (nameId == 0) continue;
            filled++;

            var compAddr = 0L;
            var inRange = compCount > 0 && index < compCount;
            if (inRange) compAddr = Q(compFirst + index * 8);

            // Компонент обязан ссылаться на своего владельца по +0x08 — это независимая проверка
            // того, что индекс разобран верно, а не совпал случайно.
            var owner = IsPointer(compAddr) ? Q(compAddr + 0x8) : 0;
            var ownerOk = owner == ent;

            if (!harvest.TryGetValue(nameId, out var list))
                harvest[nameId] = list = new List<string>();
            if (list.Count < 3) list.Add($"0x{ent:X}");

            if (dumpSlots || filled <= 16)
                Console.WriteLine($"    слот[{i,2}] nameId 0x{nameId:X3}  index {index,2}  " +
                                  $"компонент 0x{compAddr:X}  vtbl 0x{Q(compAddr):X}" +
                                  $"{(inRange ? "" : "  ИНДЕКС ВНЕ ВЕКТОРА")}" +
                                  $"{(IsPointer(compAddr) ? (ownerOk ? "  владелец СОШЁЛСЯ" : $"  владелец 0x{owner:X} НЕ сошёлся") : "")}");
        }

        Console.WriteLine($"              непустых слотов {filled}, компонентов в векторе {compCount}" +
                          $"  -> {(filled == compCount ? "СОШЛОСЬ" : "НЕ СОШЛОСЬ")}");

        if (_items) ItemProbe(ent, compFirst, compCount);
        if (_posdump) PosDump(ent, compFirst, compCount);
        if (_byvtbl) ResolveByVtable(ent, compFirst, compCount);
        if (_comp) ProbeComponents(ent, compFirst, compCount, slotFirst, slotLast);

        // Контроль: прямой указатель на Positioned по ent+0x98 обязан совпасть с адресом одного из
        // компонентов вектора — если да, это независимо подтверждает и вектор, и само поле 0x98.
        if (IsPointer(positionedDirect) && compCount > 0)
        {
            var found = -1;
            for (long i = 0; i < compCount && i < 64; i++)
                if (Q(compFirst + i * 8) == positionedDirect) { found = (int)i; break; }

            Console.WriteLine($"  ent+0x98    0x{positionedDirect:X} -> " +
                              (found >= 0
                                  ? $"это компонент №{found} вектора (контроль СОШЁЛСЯ)"
                                  : "в векторе компонентов не найден (контроль НЕ сошёлся)"));
        }

        return pathGood;
    }

    // Обход дерева сущностей — ОДИН-В-ОДИН с Core/PoEMemory/MemoryObjects/EntityList.cs и с
    // tools/SanityRead, включая особенность, что node.Entity берётся от ПРЕДЫДУЩЕГО узла.
    // Повторено как есть намеренно: цифра должна совпадать с той, что получает сам форк.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // ПЕРЕПИСЬ ПОЛЯ ПО ПОПУЛЯЦИИ. Не проверяет подставленное смещение, а ищет его.
    //
    // Приём описан в промте (пункт 3 «Порядок работ») и здесь реализован: поле, объявленное в
    // структуре, но КОНСТАНТНОЕ по всей зоне или никогда не похожее на свой тип, почти наверняка
    // читается не оттуда. Обратное тоже верно и полезнее: если ищут булев признак, то настоящее
    // поле обязано быть строго булевым НА ВСЕЙ ПОПУЛЯЦИИ и обязано РАЗЛИЧАТЬ виды сущностей.
    //
    // Почему это сильнее оракула. Оракул отвечает про один адрес, и его ответ — выборка из одной
    // сущности; ровно так «InventoryId +0x70» сошёлся на трёх и развалился на сорока. Перепись
    // берёт все сущности зоны разом и отделяет поле, которое ВСЕГДА булево и при этом не
    // постоянно, от сотни байт, которые случайно оказались нулём или единицей у первых трёх.
    //
    // Вид сущности — первые два сегмента пути метаданных (Metadata/Monsters, Metadata/Chests),
    // как и в корреляции nameId ниже: иначе таблица распухает вариантами одного и того же.
    private static void Census(List<long> entities, long vtableRva, int bytes)
    {
        Console.WriteLine($"ПЕРЕПИСЬ КОМПОНЕНТА ПО ПОПУЛЯЦИИ: vtable RVA 0x{vtableRva:X}, " +
                          $"первые 0x{bytes:X} байт");
        Console.WriteLine(new string('=', 100));

        var comps = new List<(long Comp, string Kind, long Ent)>();

        foreach (var ent in entities)
        {
            var first = Q(ent + EntComps);
            var last = Q(ent + EntComps + 8);
            if (!IsPointer(first) || last < first || last - first > 0x2000) continue;

            long found = 0;
            for (long i = 0; i < (last - first) / 8 && i < 256; i++)
            {
                var c = Q(first + i * 8);
                if (!IsPointer(c)) continue;
                if (Q(c) - _moduleBase != vtableRva) continue;
                found = c;
                break;
            }

            if (found == 0) continue;

            var det = Q(ent + EntDetails);
            var kind = "?";
            if (IsPointer(det))
            {
                var ptr = Q(det + DetPathPtr);
                var len = Q(det + DetPathLen);
                if (IsPointer(ptr) && len > 0 && len <= 512)
                {
                    var path = ReadUtf16(ptr, (int)len);
                    if (path != null) kind = string.Join("/", path.Split('/').Take(2));
                }
            }

            comps.Add((found, kind, ent));
        }

        Console.WriteLine($"  сущностей в зоне {entities.Count}; несут этот компонент {comps.Count}");

        if (comps.Count == 0)
        {
            Console.WriteLine("  компонент не найден ни у одной сущности — перепись невозможна.");
            Console.WriteLine("  (проверьте RVA: он берётся из GameOffsets/ComponentVtables.cs и");
            Console.WriteLine("   действителен только для той сборки клиента, на которой снят)");
            return;
        }

        var kinds = comps.GroupBy(x => x.Kind).OrderByDescending(g => g.Count()).ToList();
        Console.WriteLine($"  виды сущностей: " +
                          string.Join(", ", kinds.Select(g => $"{g.Key}={g.Count()}")));
        Console.WriteLine();

        // Читаем окно каждого компонента ОДНИМ чтением: перепись идёт по всей зоне, и экономия
        // на системных вызовах здесь же превращается в сокращение времени, за которое человек
        // может сменить зону под нами.
        var windows = comps.Select(c => Raw(c.Comp, bytes)).ToList();

        Console.WriteLine("БАЙТОВЫЕ ПОЛЯ: какие смещения СТРОГО БУЛЕВЫ по всей популяции и при этом не постоянны");
        Console.WriteLine(new string('-', 100));
        Console.WriteLine("  Постоянное поле бесполезно как признак, даже если оно булево: оно ничего не");
        Console.WriteLine("  различает. Полезно то, что булево И меняется, И меняется ПО ВИДУ сущности.");
        Console.WriteLine();
        Console.WriteLine("  смещ.  значения        доля 1   разбивка по видам (вид: доля единиц из числа)");

        var printed = 0;

        for (var off = 0; off < bytes; off++)
        {
            var values = new HashSet<int>();
            foreach (var win in windows) values.Add(win[off]);
            if (values.Count < 2) continue;                    // постоянное — не различает ничего
            if (values.Any(v => v > 1)) continue;              // не булево

            var ones = windows.Count(win => win[off] == 1);
            var byKind = new List<string>();
            for (var k = 0; k < kinds.Count && k < 8; k++)
            {
                var g = kinds[k];
                var idx = comps.Select((c, i) => (c, i)).Where(t => t.c.Kind == g.Key).Select(t => t.i).ToList();
                var o = idx.Count(i => windows[i][off] == 1);
                byKind.Add($"{g.Key}: {o}/{idx.Count}");
            }

            Console.WriteLine($"  +0x{off:X2}   0 и 1       {ones * 100.0 / comps.Count,5:F1}%   " +
                              string.Join("  ", byKind));
            printed++;
        }

        if (printed == 0)
            Console.WriteLine("  НИ ОДНОГО: строго булевых и при этом непостоянных байт в окне нет.");

        // Если булевых кандидатов несколько, сама по себе перепись их НЕ РАЗВОДИТ: она говорит
        // «искомое поле здесь», но не какое из них какое. Разводят их СУЩНОСТИ, НА КОТОРЫХ ОНИ
        // РАСХОДЯТСЯ, — поэтому они печатаются поимённо, с полным путём метаданных. Дальше вопрос
        // решается смыслом («может ли ЭТО быть целью?»), а не ещё одним числом.
        var boolOffsets = new List<int>();
        for (var off = 0; off < bytes; off++)
        {
            var values = new HashSet<int>();
            foreach (var win in windows) values.Add(win[off]);
            if (values.Count >= 2 && values.All(v => v <= 1)) boolOffsets.Add(off);
        }

        if (boolOffsets.Count >= 2)
        {
            Console.WriteLine();
            Console.WriteLine("ГДЕ КАНДИДАТЫ РАСХОДЯТСЯ — поимённо. Это и есть материал для решения");
            Console.WriteLine(new string('-', 100));
            Console.WriteLine("  " + string.Join("  ", boolOffsets.Select(o => $"+0x{o:X2}")) + "   путь метаданных");

            var shown = 0;
            for (var i = 0; i < comps.Count && shown < 40; i++)
            {
                var vals = boolOffsets.Select(o => windows[i][o]).ToList();
                if (vals.Distinct().Count() < 2) continue;      // согласны между собой — не информативны

                var det = Q(comps[i].Ent + EntDetails);
                var path = "?";
                if (IsPointer(det))
                {
                    var ptr = Q(det + DetPathPtr);
                    var len = Q(det + DetPathLen);
                    if (IsPointer(ptr) && len > 0 && len <= 512) path = ReadUtf16(ptr, (int)len) ?? "?";
                }

                Console.WriteLine("  " + string.Join("     ", vals.Select(v => v.ToString())) + "      " + path);
                shown++;
            }

            if (shown == 0)
                Console.WriteLine("  НИ ОДНОЙ: кандидаты согласны на всей популяции — значит это одно и то же поле,");
            else
                Console.WriteLine($"  расходятся на {shown} сущностях (показано не больше 40).");
        }

        Console.WriteLine();
        Console.WriteLine($"  строго булевых и непостоянных байт: {printed} из {bytes}");
        Console.WriteLine();

        // ── Малые целые: перечисления ───────────────────────────────────────────────────────────
        // Булев признак — частный случай. Перечисление (редкость, реакция, состояние) выглядит
        // иначе: небольшой набор значений, непостоянный, и РАСПРЕДЕЛЕНИЕ у него неравномерное —
        // обычных монстров много, редких мало. Равномерность как раз подозрительна: у мусорного
        // байта, случайно попавшего в узкий диапазон, нет причин быть скошенным.
        Console.WriteLine("МАЛЫЕ ЦЕЛЫЕ: смещения, где значений немного (<= 8, все <= 15) и они не постоянны");
        Console.WriteLine(new string('-', 100));
        Console.WriteLine("  Так выглядит перечисление. Смотрите на ГИСТОГРАММУ: у настоящего признака она");
        Console.WriteLine("  скошена (редкого мало), у случайно узкого мусора — нет причин быть скошенной.");
        Console.WriteLine();
        Console.WriteLine("  смещ.  гистограмма значений (значение x сколько)");

        var enums = 0;

        for (var off = 0; off < bytes; off++)
        {
            var hist = new SortedDictionary<int, int>();
            foreach (var win in windows)
            {
                hist.TryGetValue(win[off], out var n);
                hist[win[off]] = n + 1;
            }

            if (hist.Count < 2 || hist.Count > 8) continue;
            if (hist.Keys.Any(v => v > 15)) continue;
            if (hist.Keys.All(v => v <= 1)) continue;          // это булев, он уже напечатан выше

            Console.WriteLine($"  +0x{off:X2}   " +
                              string.Join("  ", hist.Select(kv => $"{kv.Key} x{kv.Value}")));
            enums++;
        }

        if (enums == 0)
            Console.WriteLine("  НИ ОДНОГО.");
        Console.WriteLine();
        Console.WriteLine($"  кандидатов-перечислений: {enums} из {bytes}");
        Console.WriteLine();

        // ── Векторы ─────────────────────────────────────────────────────────────────────────────
        // StdVector — три указателя подряд (First, Last, End), и это ОЧЕНЬ узкий шаблон: требуется
        // First <= Last <= End, все три либо нули, либо канонические указатели, и (Last - First)
        // кратно 8. Случайные 24 байта этого почти никогда не выполняют, а по популяции — тем более.
        // Полезен вектор не сам по себе, а как ЯКОРЬ: рядом с ним лежит то, что он описывает
        // (например, редкость рядом с вектором модификаторов), и число элементов у него
        // коррелирует с признаком, который ищут.
        Console.WriteLine("ВЕКТОРЫ (First/Last/End): где в компоненте лежат векторы и сколько в них элементов");
        Console.WriteLine(new string('-', 100));
        Console.WriteLine("  смещ.  сошлось у   гистограмма числа элементов");

        var vecs = 0;

        for (var off = 0; off + 24 <= bytes; off += 8)
        {
            var ok = 0;
            var counts = new SortedDictionary<long, int>();

            foreach (var win in windows)
            {
                var f = BitConverter.ToInt64(win, off);
                var l = BitConverter.ToInt64(win, off + 8);
                var e = BitConverter.ToInt64(win, off + 16);

                if (f == 0 && l == 0 && e == 0)
                {
                    ok++;
                    counts.TryGetValue(0, out var z);
                    counts[0] = z + 1;
                    continue;
                }

                if (!IsPointer(f) || !IsPointer(l) || !IsPointer(e)) break;
                if (l < f || e < l) break;
                if ((l - f) % 8 != 0) break;
                var n = (l - f) / 8;
                if (n > 4096) break;

                ok++;
                counts.TryGetValue(n, out var c);
                counts[n] = c + 1;
            }

            if (ok != windows.Count) continue;                  // шаблон обязан выполниться У ВСЕХ
            if (counts.Count < 2) continue;                     // вектор одной и той же длины неинтересен

            Console.WriteLine($"  +0x{off:X2}   {ok}/{windows.Count}      " +
                              string.Join("  ", counts.Take(10).Select(kv => $"{kv.Key} x{kv.Value}")) +
                              (counts.Count > 10 ? " …" : ""));
            vecs++;
        }

        if (vecs == 0)
            Console.WriteLine("  НИ ОДНОГО вектора переменной длины, выполняющего шаблон у ВСЕХ.");
        Console.WriteLine();
        Console.WriteLine($"  векторов переменной длины: {vecs}");
        Console.WriteLine("  Чем их меньше, тем сильнее вывод: если такой байт один, он и есть искомый признак.");
        Console.WriteLine();

        // Постоянные байты печатаются отдельно и сжато: это карта «здесь поля нет», и именно она
        // опровергает объявленные смещения, по которым лежит вечный ноль.
        var constOffsets = new List<int>();
        for (var off = 0; off < bytes; off++)
        {
            var v = windows[0][off];
            if (windows.All(win => win[off] == v)) constOffsets.Add(off);
        }

        Console.WriteLine($"ПОСТОЯННЫЕ ПО ВСЕЙ ПОПУЛЯЦИИ БАЙТЫ: {constOffsets.Count} из {bytes}");
        Console.WriteLine(new string('-', 100));
        Console.WriteLine("  По такому смещению поле-признак лежать не может: оно одинаково у монстра, сундука");
        Console.WriteLine("  и декорации. Смещение из этого списка, объявленное в структуре, — почти наверняка");
        Console.WriteLine("  ошибка (ровно так выглядел бы прежний Targetable, если он читает не оттуда).");
        Console.WriteLine("  " + string.Join(", ", constOffsets.Take(96).Select(o => $"0x{o:X2}={windows[0][o]}")) +
                          (constOffsets.Count > 96 ? " …" : ""));
    }

    private static List<long> WalkEntityList(long listAddress)
    {
        var result = new List<long>(1024);
        var seen = new HashSet<long>();
        var queue = new Queue<long>(256);

        var root = Q(listAddress + 0x8);
        if (!IsPointer(root)) return result;

        queue.Enqueue(root);
        var first = Q(root + 0x0);
        var second = Q(root + 0x10);
        var entity = Q(root + 0x28);
        queue.Enqueue(first);
        queue.Enqueue(second);

        var loops = 0;

        while (queue.Count > 0 && loops < 10000)
        {
            loops++;
            var next = queue.Dequeue();

            if (!seen.Add(next)) continue;
            if (next == root || next == 0) continue;

            if (entity > 0x100000000L && entity < 0x7F0000000000L)
                result.Add(entity);

            first = Q(next + 0x0);
            second = Q(next + 0x10);
            entity = Q(next + 0x28);
            queue.Enqueue(first);
            queue.Enqueue(second);
        }

        if (loops >= 10000)
            Console.WriteLine("  обход упёрся в предел 10000 итераций — список неполон.");

        return result;
    }

    // ── Непрерывное наблюдение ───────────────────────────────────────────────────────────────────
    // Часть сущностей живёт доли секунды (снаряды, эффекты умений), и одиночный снимок их не ловит:
    // к моменту следующей команды их уже нет. Здесь список перечитывается в цикле, и копится только
    // НОВОЕ — vtable компонентов, которых нет в таблице, вместе с путём сущности и nameId слота.
    //
    // Атомарность тут другая, чем в снимке: зона МОЖЕТ смениться посреди наблюдения, и это не портит
    // результат, потому что накапливаются RVA (они не зависят от зоны), а не адреса. Смена зоны
    // просто отмечается в выводе.
    private static int Watch(long igs, int seconds)
    {
        var known = ComponentVtables.Select(x => x.Rva).ToHashSet();
        var found = new Dictionary<long, (SortedSet<string> Paths, SortedSet<uint> NameIds, long Example, long Entity)>();
        var paths = new SortedSet<string>();
        var zones = new SortedSet<uint>();

        var until = DateTime.UtcNow.AddSeconds(seconds);
        var passes = 0;
        var entitiesSeen = 0;

        Console.WriteLine($"наблюдение {seconds} с — копим vtable компонентов, которых нет в таблице из {known.Count}");
        Console.WriteLine();

        while (DateTime.UtcNow < until)
        {
            passes++;

            var data = Q(igs + IgsData);
            if (!IsPointer(data)) { System.Threading.Thread.Sleep(50); continue; }

            zones.Add(U32(data + DataAreaHash));

            var list = Q(data + DataEntityList);
            if (!IsPointer(list)) { System.Threading.Thread.Sleep(50); continue; }

            foreach (var ent in WalkEntityList(list))
            {
                entitiesSeen++;

                var details = Q(ent + EntDetails);
                if (!IsPointer(details)) continue;

                var sp = Q(details + DetPathPtr);
                var sl = Q(details + DetPathLen);
                if (!IsPointer(sp) || sl <= 0 || sl > 512) continue;

                var path = ReadUtf16(sp, (int)sl);
                if (path == null || path.Length != sl) continue;

                paths.Add(string.Join("/", path.Split('/').Take(2)));

                var first = Q(ent + EntComps);
                var last = Q(ent + EntComps + 8);
                if (!IsPointer(first) || last < first || last - first > 0x4000) continue;

                var count = (last - first) / 8;

                // nameId слота нужен только как ПОДСКАЗКА для стыка с оракулом: он не ключ типа
                // (доказано), но позволяет узнать компонент, который оракул уже называл раньше.
                var nameIdByIndex = new Dictionary<uint, uint>();
                var lookup = Q(details + DetLookup);

                if (IsPointer(lookup))
                {
                    var sf = Q(lookup + LookupSlots);
                    var slLast = Q(lookup + LookupSlots + 8);

                    if (IsPointer(sf) && slLast >= sf && slLast - sf <= 0x2000)
                        for (long i = 0; i < (slLast - sf) / 8 && i < 256; i++)
                        {
                            var slot = Q(sf + i * 8);
                            var nid = (uint)(slot & 0xFFFFFFFF);
                            if (nid != 0) nameIdByIndex[(uint)((ulong)slot >> 32)] = nid;
                        }
                }

                for (long i = 0; i < count && i < 64; i++)
                {
                    var c = Q(first + i * 8);
                    if (!IsPointer(c)) continue;

                    var rva = Q(c) - _moduleBase;
                    if (rva <= 0 || known.Contains(rva)) continue;

                    if (!found.TryGetValue(rva, out var rec))
                        found[rva] = rec = (new SortedSet<string>(), new SortedSet<uint>(), c, ent);

                    rec.Paths.Add(path);
                    if (nameIdByIndex.TryGetValue((uint)i, out var nid)) rec.NameIds.Add(nid);
                }
            }

            System.Threading.Thread.Sleep(30);
        }

        Console.WriteLine($"проходов {passes}, просмотров сущностей {entitiesSeen}, зон за наблюдение: {zones.Count}");
        Console.WriteLine($"виды сущностей: {string.Join(", ", paths)}");
        Console.WriteLine();
        Console.WriteLine($"VTABLE, КОТОРЫХ НЕТ В ТАБЛИЦЕ: {found.Count}");
        Console.WriteLine(new string('-', 100));

        foreach (var kv in found.OrderBy(x => x.Key))
        {
            var ids = kv.Value.NameIds.Count == 0 ? "—" : string.Join(" ", kv.Value.NameIds.Select(n => $"0x{n:X3}"));

            Console.WriteLine($"  RVA 0x{kv.Key:X7}  nameId {ids,-14} пример: компонент 0x{kv.Value.Example:X} " +
                              $"сущность 0x{kv.Value.Entity:X}");

            foreach (var pth in kv.Value.Paths.Take(4))
                Console.WriteLine($"      {pth}");
        }

        return ExitOk;
    }

    // ── Предметы: сущность внутри компонента ─────────────────────────────────────────────────────
    // Компоненты предмета (Base, Mods, Quality, Stack, RenderItem) лежат НЕ на сущности зоны,
    // а на отдельной ПРЕДМЕТНОЙ сущности, на которую ссылается компонент WorldItem. Смещение этой
    // ссылки не замерено, и здесь оно ищется СОДЕРЖИМЫМ, а не гаданием: предметная сущность — это
    // сущность, а у всех сущностей одна и та же vtable (RVA 0x3456508, проверено на 40 из 40).
    // Значит достаточно перебрать указатели внутри WorldItem и спросить у каждого его vtable.
    private const long WorldItemVtableRva = 0x3465DA8;
    private const long EntityVtableRva = 0x3456508;

    private static void ItemProbe(long ent, long compFirst, long compCount)
    {
        if (compCount <= 0) return;

        long worldItem = 0;

        for (long i = 0; i < compCount && i < 64; i++)
        {
            var c = Q(compFirst + i * 8);
            if (IsPointer(c) && Q(c) - _moduleBase == WorldItemVtableRva) { worldItem = c; break; }
        }

        if (worldItem == 0) return;

        Console.WriteLine($"  [WorldItem @0x{worldItem:X}]  ищем ссылку на предметную сущность");

        // Искать по vtable сущности НЕЛЬЗЯ: замер показал, что так находятся только владелец
        // (он же [comp+0x08]) и соседняя НАЗЕМНАЯ сущность-предмет, а у самой предметной сущности
        // vtable другая. Поэтому ищем по СОДЕРЖИМОМУ: за нужным указателем должен читаться путь
        // метаданных тем же способом, что и у обычной сущности (details=[p+0x08], строка +0x08,
        // длина +0x18), и этот путь обязан начинаться с Metadata/Items/.
        for (var off = 0; off < 0x200; off += 8)
        {
            var p = Q(worldItem + off);
            if (!IsPointer(p)) continue;

            var det = Q(p + EntDetails);
            if (!IsPointer(det)) continue;

            var sp = Q(det + DetPathPtr);
            var sl = Q(det + DetPathLen);
            if (!IsPointer(sp) || sl <= 0 || sl > 512) continue;

            var text = ReadUtf16(sp, (int)sl);
            if (text == null || text.Length != sl ||
                !text.StartsWith("Metadata/", StringComparison.Ordinal)) continue;

            var vt = Q(p) - _moduleBase;

            Console.WriteLine($"    +0x{off:X2}  0x{p:X}  vtable RVA 0x{vt:X}  путь \"{text}\"" +
                              (text.StartsWith("Metadata/Items/", StringComparison.Ordinal)
                                  ? "   <-- ЭТО ПРЕДМЕТ"
                                  : ""));

            if (text.StartsWith("Metadata/Items/", StringComparison.Ordinal))
                DescribeItemEntity(p);
        }
    }

    // Разбирает предметную сущность так же, как обычную, и печатает её компоненты по vtable.
    // Неопознанные печатаются с RVA — именно они и есть добор (Base, Mods, Quality, Stack, ...).
    private static void DescribeItemEntity(long item)
    {
        var details = Q(item + EntDetails);
        string path = null;

        if (IsPointer(details))
        {
            var ptr = Q(details + DetPathPtr);
            var len = Q(details + DetPathLen);
            if (IsPointer(ptr) && len > 0 && len <= 512) path = ReadUtf16(ptr, (int)len);
        }

        var first = Q(item + EntComps);
        var last = Q(item + EntComps + 8);
        var count = (IsPointer(first) && last >= first && last - first < 0x4000) ? (last - first) / 8 : -1;

        Console.WriteLine($"      путь предмета  {(path == null ? "НЕ ПРОЧИТАН" : "\"" + path + "\"")}" +
                          $"   компонентов {count}");

        if (count <= 0) return;

        var byRva = ComponentVtables.ToDictionary(x => x.Rva, x => x.Name);

        for (long i = 0; i < count && i < 64; i++)
        {
            var c = Q(first + i * 8);
            if (!IsPointer(c)) { Console.WriteLine($"      [{i,2}] указатель не похож на указатель"); continue; }

            var rva = Q(c) - _moduleBase;
            var known = byRva.TryGetValue(rva, out var name);
            var owner = Q(c + 0x8);

            Console.WriteLine($"      [{i,2}] 0x{c:X}  RVA 0x{rva:X}  {(known ? name : "??? НЕ В ТАБЛИЦЕ")}" +
                              $"   владелец {(owner == item ? "СОШЁЛСЯ" : $"0x{owner:X} НЕ сошёлся")}");
        }
    }

    // Сырой дамп компонента Positioned побайтно — ради поиска полей, у которых нет своего
    // критерия, но есть ИЗВЕСТНОЕ РАЗЛИЧИЕ между видами сущностей (Reaction: у игрока 1, у
    // враждебного монстра иное). Такое поле ищется не значением, а СТОЛБЦОМ: байт, который у
    // игрока равен одному, а у всех монстров — другому, и так на каждой сущности.
    private static void PosDump(long ent, long compFirst, long compCount)
    {
        if (compCount <= 0) return;

        long pos = 0;
        for (long i = 0; i < compCount && i < 64; i++)
        {
            var c = Q(compFirst + i * 8);
            if (IsPointer(c) && Q(c) - _moduleBase == 0x35A0F18) { pos = c; break; }
        }

        if (pos == 0) return;

        var bytes = Raw(pos, 0x300);
        Console.WriteLine("  POSRAW " + string.Join("", bytes.Select(b => b.ToString("X2"))));
    }

    // ── Таблица «тип компонента -> RVA его vtable» ───────────────────────────────────────────────
    // ЗАМЕРЕНО 2026-09-16 на живом клиенте: имена взяты у оракула (RefLive --harvest), vtable — своим
    // чтением [компонент+0x00], соединение по адресу компонента.
    //
    // ВНИМАНИЕ: это СНИМОК таблицы из GameOffsets/ComponentVtables.cs, а не ссылка на неё.
    // Инструмент намеренно не ссылается на GameOffsets: он обязан уметь читать гипотезу, которой
    // в коде ещё нет, и не должен ломаться от того, что код меняется под ним. Плата — таблицу
    // приходится синхронизировать руками; расхождение видно как «не в таблице N» на сущностях,
    // у которых в коде всё опознаётся.
    //
    // Почему ключ именно vtable, а не nameId из слот-таблицы. Замер показал, что nameId НЕ является
    // идентификатором типа: у сундуков BaseEvents лежит под 0x214, а у дудадов и монстров — под
    // 0x114, и то же с InteractionAction (0x22E против 0x12E), при ОДНОЙ И ТОЙ ЖЕ vtable. Обратно:
    // под одним nameId 0x1D8 живут ДВА разных типа — Chest (RVA 0x3465EC0) у Metadata/Chests и
    // WorldItem (RVA 0x3465DA8) у Metadata/MiscellaneousObjects/WorldItem, и у них РАЗНЫЕ vtable.
    // То есть nameId не уникален ни в одну сторону, а vtable — уникальна в обе.
    private static readonly (string Name, long Rva)[] ComponentVtables =
    {
        // Сущности зоны — 30 пар.
        ("Actor", 0x346E558), ("Animated", 0x35A5030), ("AreaTransition", 0x346DE00),
        ("BaseEvents", 0x35A55E8), ("Brackets", 0x359EC30), ("Buffs", 0x359F5F8),
        ("Chest", 0x3465EC0), ("DiesAfterTime", 0x359F478), ("Functions", 0x359F288),
        ("HideoutDoodad", 0x34668A8), ("InteractionAction", 0x359F7D0), ("Inventories", 0x359E078),
        ("Life", 0x35A7340), ("MinimapIcon", 0x35A7018), ("Monster", 0x35A6778),
        ("NPC", 0x3467218), ("ObjectMagicProperties", 0x35A6AE0), ("Pathfinding", 0x35A6BA0),
        ("PetAi", 0x35A6398), ("Player", 0x3466450), ("PlayerClass", 0x35A0078),
        ("Portal", 0x346C740), ("Positioned", 0x35A0F18), ("Render", 0x3468920),
        ("StateMachine", 0x35DA8B8), ("Stats", 0x35DD4E8), ("Targetable", 0x346C238),
        ("Transitionable", 0x35DD290), ("TriggerableBlockage", 0x35DCFE0), ("WorldItem", 0x3465DA8),

        // Компоненты предмета — 9 пар. Живут на предметной сущности за WorldItem+0x28, у неё
        // СВОЯ vtable (RVA 0x35E0358), поэтому обходом списка сущностей они недостижимы.
        ("Armour", 0x363AD40), ("Base", 0x35E0168), ("LocalStats", 0x363AB88),
        ("Mods", 0x35DF838), ("Quality", 0x363ACD8), ("RenderItem", 0x345EE58),
        ("Sockets", 0x35DF9A0), ("Stack", 0x3634B10), ("Weapon", 0x363AB48),

        // Установлено СЛАБЕЕ остальных: прямого соединения по адресу нет — оракул трижды за бой
        // не застал живого снаряда. Имя держится на nameId 0x1CA из другого прогона и на том, что
        // этот vtable встречается только на Metadata/Projectiles/*.
        ("Projectile", 0x35A0D10),

        // НЕ вносится намеренно: 0x35DFB80 оракул зовёт AttributeRequirements у брони и Usable у
        // валюты при одной vtable, значит одно из имён неверно и установить нечем.
        // Без имени: 0x35A5C68 (nameId 0x11E) на Metadata/Effects/Effect.
    };

    // Разрешает компоненты сущности ПО VTABLE, не заглядывая в слот-таблицу вообще. Это и есть
    // проверяемая модель: если она верна, то (а) каждый компонент вектора опознаётся не более чем
    // одним именем, и (б) найденный Positioned обязан совпасть с прямым указателем по ent+0x98.
    private static void ResolveByVtable(long ent, long compFirst, long compCount)
    {
        if (compCount <= 0) return;

        var byRva = ComponentVtables.ToDictionary(x => x.Rva, x => x.Name);
        var named = new List<string>();
        var unknown = 0;
        long positioned = 0;

        for (long i = 0; i < compCount && i < 64; i++)
        {
            var c = Q(compFirst + i * 8);
            if (!IsPointer(c)) { unknown++; continue; }

            var rva = Q(c) - _moduleBase;

            if (byRva.TryGetValue(rva, out var name))
            {
                named.Add($"{name}[{i}]");
                if (name == "Positioned") positioned = c;
            }
            else unknown++;
        }

        var direct = Q(ent + EntPositioned);

        Console.WriteLine($"  ПО VTABLE: {string.Join(" ", named)}");
        Console.WriteLine($"             опознано {named.Count} из {compCount}, не в таблице {unknown}" +
                          $"   Positioned {(positioned == direct && direct != 0 ? "СОВПАЛ с ent+0x98" : "НЕ совпал с ent+0x98")}");
    }

    // ── Проба компонентов горячего пути ──────────────────────────────────────────────────────────
    // Каждая проба печатает не только число, но и СВОЙ КРИТЕРИЙ — то, чем она проверяется изнутри,
    // без оракула:
    //   Positioned — мировая координата обязана быть примерно grid * 10.87 (размер клетки);
    //   Life       — голова каждого блока VitalStruct содержит указатель НА САМ КОМПОНЕНТ;
    //   Render     — имя лежит встроенной строкой, и её фактическая длина обязана совпасть с полем длины.
    // Числа смещений — гипотезы survey 2026-09-16: замерено на живом клиенте, НЕ переподтверждено.
    private static void ProbeComponents(long ent, long compFirst, long compCount,
        long slotFirst, long slotLast)
    {
        if (compCount <= 0 || !IsPointer(slotFirst)) return;

        var byName = new Dictionary<uint, long>();

        for (long i = 0; i < (slotLast - slotFirst) / 8 && i < 256; i++)
        {
            var slot = Q(slotFirst + i * 8);
            var nid = (uint)(slot & 0xFFFFFFFF);
            var idx = (uint)((ulong)slot >> 32);
            if (nid == 0 || idx >= compCount) continue;
            byName[nid] = Q(compFirst + idx * 8);
        }

        // Positioned = 0x11C (подтверждён независимо: он же лежит по ent+0x98 у всех сущностей).
        if (byName.TryGetValue(0x11C, out var pos) && IsPointer(pos))
        {
            var gx = U32(pos + 0x294);
            var gy = U32(pos + 0x298);
            var gxf = F(pos + 0x294);
            var gyf = F(pos + 0x298);
            var wx = F(pos + 0x2B8);
            var wy = F(pos + 0x2B8 + 4);
            var wz = F(pos + 0x2B8 + 8);

            // Клетка сетки — 10.87 единиц мира. Если и grid, и world прочитаны верно, отношение
            // обязано сойтись; если нет — сошлось бы только случайно, и сразу по двум осям.
            var rx = gxf != 0 ? wx / gxf : 0;
            var ry = gyf != 0 ? wy / gyf : 0;
            var ok = Math.Abs(rx - 10.87) < 0.6 && Math.Abs(ry - 10.87) < 0.6;

            Console.WriteLine($"  [Positioned 0x11C @0x{pos:X}]");
            Console.WriteLine($"    grid   +0x294/+0x298  как int {gx}/{gy}   как float {gxf:0.###}/{gyf:0.###}");
            Console.WriteLine($"    world  +0x2B8         {wx:0.##} {wy:0.##} {wz:0.##}");
            Console.WriteLine($"    КРИТЕРИЙ world/grid = {rx:0.###}/{ry:0.###} (ждём ~10.87) -> " +
                              (ok ? "СОШЁЛСЯ" : "НЕ СОШЁЛСЯ"));
            Console.WriteLine($"    Reaction +0x?? (не замерено); сырьё +0x28C..+0x2A0: " +
                              string.Join(" ", Enumerable.Range(0, 6)
                                  .Select(k => U32(pos + 0x28C + k * 4).ToString())));
        }

        // Render = 0x101.
        if (byName.TryGetValue(0x101, out var ren) && IsPointer(ren))
        {
            var px = F(ren + 0x120);
            var py = F(ren + 0x120 + 4);
            var pz = F(ren + 0x120 + 8);
            var bx = F(ren + 0x12C);
            var by = F(ren + 0x12C + 4);
            var bz = F(ren + 0x12C + 8);

            // Имя — ВСТРОЕННЫЙ NativeUtf16Text: короткая строка лежит прямо в объекте, длинная —
            // по указателю в тех же байтах. Различаем по полю ёмкости, как это делает сам клиент.
            var len = (int)U32(ren + 0x158);
            var cap = (int)U32(ren + 0x160);
            string name = null;

            if (len >= 0 && len < 256)
                name = cap >= 8 ? ReadUtf16(Q(ren + 0x148), len) : ReadUtf16Inline(ren + 0x148, len);

            Console.WriteLine($"  [Render 0x101 @0x{ren:X}]");
            Console.WriteLine($"    Pos    +0x120  {px:0.##} {py:0.##} {pz:0.##}");
            Console.WriteLine($"    Bounds +0x12C  {bx:0.##} {by:0.##} {bz:0.##}");
            Console.WriteLine($"    Name   +0x148  \"{name}\"  длина поля +0x158 = {len}, ёмкость +0x160 = {cap}");
            Console.WriteLine($"    КРИТЕРИЙ длина строки == поле длины -> " +
                              ((name != null && name.Length == len) ? "СОШЁЛСЯ" : "НЕ СОШЁЛСЯ"));
        }

        // Life = 0x15F. Голова каждого блока обязана указывать на САМ компонент — это и есть примета.
        if (byName.TryGetValue(0x15F, out var life) && IsPointer(life))
        {
            Console.WriteLine($"  [Life 0x15F @0x{life:X}]");

            foreach (var (head, max, cur, what) in new[]
                     {
                         (0x180L, 0x1A4L, 0x1A8L, "здоровье"),
                         (0x1D0L, 0x1F4L, 0x1F8L, "мана"),
                         (0x218L, 0x23CL, 0x240L, "щит"),
                     })
            {
                var back = Q(life + head);
                var mx = (int)U32(life + max);
                var cu = (int)U32(life + cur);

                Console.WriteLine($"    {what,-9} голова +0x{head:X3} -> 0x{back:X}  " +
                                  $"{(back == life ? "УКАЗЫВАЕТ НА САМ КОМПОНЕНТ (критерий СОШЁЛСЯ)" : "НЕ на компонент (критерий НЕ сошёлся)")}");
                Console.WriteLine($"              max +0x{max:X3} = {mx}   cur +0x{cur:X3} = {cu}" +
                                  $"   {(mx > 0 && cu >= 0 && cu <= mx ? "0 <= cur <= max — правдоподобно" : "НЕПРАВДОПОДОБНО")}");
            }
        }
    }

    private static float F(long address) => BitConverter.ToSingle(Raw(address, 4), 0);

    private static string ReadUtf16Inline(long address, int chars)
    {
        if (chars <= 0 || chars > 8) return "";
        var bytes = Raw(address, chars * 2);
        return Encoding.Unicode.GetString(bytes);
    }

    // Сырой дамп головы сущности. Нужен там, где поле «то сходится, то нет»: по одной сущности
    // такое не отличить от совпадения, а по столбцу байтов сразу видно, поле это или чужой массив.
    private static void RawDump(long address, int length)
    {
        for (var off = 0; off < length; off += 8)
        {
            var q = Q(address + off);
            var bytes = Raw(address + off, 8);
            Console.WriteLine($"    RAW +0x{off:X2}  0x{q:X16}  " +
                              string.Join(" ", bytes.Select(b => b.ToString("X2"))) +
                              $"   u32 {BitConverter.ToUInt32(bytes, 0),-11} {BitConverter.ToUInt32(bytes, 4)}");
        }
    }

    // ── Чтение ───────────────────────────────────────────────────────────────────────────────────
    // Все чтения МОЛЧА возвращают ноль при неудаче: это соответствует поведению Memory.Read<T> в
    // форке (на мусорном адресе возвращает default, а не бросает), и потому вызывающий обязан
    // проверять правдоподобие сам, а не полагаться на исключение.

    private static long Q(long address) => BitConverter.ToInt64(Raw(address, 8), 0);
    private static uint U32(long address) => BitConverter.ToUInt32(Raw(address, 4), 0);
    private static byte B(long address) => Raw(address, 1)[0];

    private static byte[] Raw(long address, int size)
    {
        var buffer = new byte[size];
        if (address <= 0) return buffer;
        ReadProcessMemory(_handle, (IntPtr)address, buffer, size, out _);
        return buffer;
    }

    private static string ReadUtf16(long address, int chars)
    {
        if (chars <= 0 || chars > 512) return null;
        var bytes = new byte[chars * 2];
        if (!ReadProcessMemory(_handle, (IntPtr)address, bytes, bytes.Length, out var read) || read == 0)
            return null;

        var s = Encoding.Unicode.GetString(bytes, 0, (int)read);
        var nul = s.IndexOf('\0');
        return nul >= 0 ? s.Substring(0, nul) : s;
    }

    // «Похоже на канонический указатель»: пользовательская половина адресов и выравнивание на 8.
    // Проверка «не ноль» бесполезна — по незамеренному полю обычно лежит мусор, а не ноль.
    private static bool IsPointer(long value) =>
        value > 0x10000 && value < 0x7FFFFFFFFFFF && (value & 7) == 0;

    private static string Arg(string[] args, string key)
    {
        for (var i = 0; i < args.Length - 1; i++)
            if (string.Equals(args[i], key, StringComparison.OrdinalIgnoreCase))
                return args[i + 1];
        return null;
    }

    private static bool TryHex(string text, out long value)
    {
        if (text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) text = text.Substring(2);
        return long.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int access, bool inherit, int pid);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr handle, IntPtr address, byte[] buffer,
        int size, out IntPtr read);
}
