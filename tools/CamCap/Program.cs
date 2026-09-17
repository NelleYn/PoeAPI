// ─────────────────────────────────────────────────────────────────────────────────────────────────
// CamCap — атомарный снимок КАМЕРЫ живого клиента. ТОЛЬКО ЧТЕНИЕ.
//
// Зачем отдельный инструмент. Камера — не набор независимых полей, а одна согласованная арифметика:
// матрица вида-проекции, позиция игрока и размер экрана обязаны сойтись В ОДНОМ кадре. Разными
// запусками так не померить — между двумя командами человек ходит, камера едет, и матрица из
// первого запуска не соответствует позиции из второго. Здесь окно камеры снимается ОДНИМ
// ReadProcessMemory, и снимок обрамлён проверкой «зона, база Data и указатель на камеру не
// менялись»: если сменились — печатается НЕДЕЙСТВИТЕЛЕН и код возврата 3, а не число.
//
// Ничего не подставляется и не подгоняется. Смещения полей здесь НЕ ЗАДАНЫ константами: они
// ИЩУТСЯ по самопроверяющимся критериям, которые неверное смещение не может выполнить случайно.
//
//   КРИТЕРИЙ ЭКРАНА       пара int32 в окне равна размеру клиентской области окна игры, взятому
//                         у ОС через GetClientRect — факт, известный ВНЕ памяти процесса.
//   КРИТЕРИЙ ЦЕНТРА       блок из 16 float, прочитанный как матрица вида-проекции, обязан
//                         спроецировать позицию игрока в ГОРИЗОНТАЛЬНЫЙ ЦЕНТР экрана: в PoE камера
//                         следует за персонажем, и это верно в любой момент. Ищется перебором всех
//                         выровненных смещений окна; мусорный блок этого не выполнит.
//   КРИТЕРИЙ ПОЛОЖЕНИЯ    центр камеры вычисляется ОБРАЩЕНИЕМ найденной матрицы (столбцы X, Y и W
//                         дают три уравнения на три неизвестных) и затем ИЩЕТСЯ в окне как тройка
//                         подряд лежащих float. Совпадение вычисленного с лежащим в памяти и есть
//                         смещение Position. Числа в вычисление не подставляются.
//   КРИТЕРИЙ ПЛОСКОСТЕЙ   ближняя и дальняя плоскости выводятся из той же матрицы (z_ndc = k + d/w,
//                         откуда w при z_ndc = 0 и при z_ndc = 1) и тоже ИЩУТСЯ в окне.
//
// Вышележащие смещения взяты ЗАКРЫТЫМИ (docs/offsets.md): IngameState.Data = 0x218,
// IngameState.Camera = 0x270 (указатель), IngameData.CurrentAreaHash = 0x114,
// IngameData.LocalPlayer = 0x970, сущность+0x10 — вектор компонентов, Render.Pos = 0x120,
// RVA vtable компонента Render = 0x3468920 (GameOffsets/ComponentVtables.cs).
// Проверяемые здесь гипотезы — только про раскладку САМОЙ КАМЕРЫ.
//
// Использование:  CamCap.exe --igs <адрес IngameState в HEX> [режим]
//   (без режима)  найти все поля по критериям и напечатать сводку.
//   --dump        полный дамп окна камеры по 8 байт с интерпретациями.
//   --watch N     N секунд опроса: тот же критерий центра на КАЖДОМ кадре. Это единственный способ
//                 развести копии матрицы, которые в покое совпадают побайтно, — для него надо
//                 ХОДИТЬ, пока идёт замер.
//   --len N       длина окна камеры (по умолчанию 0x800; короче 0x500 прячет вторые копии
//                 Width/Height и Position, см. комментарий у разбора аргументов).
//
// Коды возврата: 0 — снимок действителен; 1 — действителен, но критерий центра не сошёлся ни на
// одном смещении; 2 — читать нечего; 3 — состояние сменилось посередине.
// ─────────────────────────────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

internal static class Program
{
    private const int ExitOk = 0;
    private const int ExitNoCriterion = 1;
    private const int ExitNothingToRead = 2;
    private const int ExitStale = 3;

    // ── Закрытые смещения вышележащего слоя (docs/offsets.md) ────────────────────────────────────
    private const long IgsData = 0x218;
    private const long IgsCamera = 0x270;      // указатель, не встроенная структура
    private const long DataAreaHash = 0x114;
    private const long DataLocalPlayer = 0x970;
    private const long EntComps = 0x10;        // StdVector указателей на компоненты
    private const long EntPositioned = 0x98;   // прямой указатель на Positioned
    private const long RenderPos = 0x120;      // три float, замерено 2026-09-16
    private const long PositionedWorldPos = 0x2B8;
    private const long RenderVtableRva = 0x3468920;   // GameOffsets/ComponentVtables.cs

    private static IntPtr _handle;
    private static long _moduleBase;

    private static int Main(string[] args)
    {
        try { Console.OutputEncoding = Encoding.UTF8; } catch { /* консоли может не быть */ }

        var igsText = Arg(args, "--igs");
        var dump = args.Contains("--dump");
        var watch = int.TryParse(Arg(args, "--watch"), out var w) ? w : 0;
        // 0x800, а не 0x400. Первый замер шёл по 0x400 и печатал «единственная такая пара в окне»
        // про Width/Height — а в 0x800 их ДВЕ (+0x318 и +0x498), и у Position тоже две (+0x2E8
        // и +0x420). Короткое окно не опровергает двойника, оно его НЕ ВИДИТ, и разница между
        // этими двумя утверждениями — ровно то, ради чего инструмент написан.
        var len = TryHex(Arg(args, "--len") ?? "0x800", out var l) ? (int)l : 0x800;
        if (len < 0x80 || len > 0x10000) len = 0x800;

        if (igsText == null || !TryHex(igsText, out var igs))
        {
            Console.WriteLine("нужен адрес IngameState: CamCap.exe --igs <HEX> [--dump] [--watch N] [--len N]");
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

        Console.WriteLine("CamCap — атомарный снимок камеры. ТОЛЬКО ЧТЕНИЕ.");
        Console.WriteLine($"процесс {process.ProcessName} (pid {process.Id}), база модуля 0x{_moduleBase:X}, " +
                          $"IngameState 0x{igs:X}");

        // ── Размер окна игры у ОС: факт, известный ВНЕ памяти процесса ───────────────────────────
        var osW = 0;
        var osH = 0;
        if (process.MainWindowHandle != IntPtr.Zero && GetClientRect(process.MainWindowHandle, out var rc))
        {
            osW = rc.Right - rc.Left;
            osH = rc.Bottom - rc.Top;
        }

        Console.WriteLine(osW > 0
            ? $"клиентская область окна игры по GetClientRect: {osW} x {osH} — это контроль ВНЕ памяти"
            : "окно игры не опрошено (нет MainWindowHandle) — критерий экрана будет без внешнего контроля");
        Console.WriteLine();

        // ── Рамка «до» ───────────────────────────────────────────────────────────────────────────
        var data = Q(igs + IgsData);
        if (!IsPointer(data))
        {
            Console.WriteLine($"IngameState+0x218 = 0x{data:X} — не похоже на указатель. Персонаж не в зоне?");
            return ExitNothingToRead;
        }

        var camera = Q(igs + IgsCamera);
        if (!IsPointer(camera))
        {
            Console.WriteLine($"IngameState+0x270 = 0x{camera:X} — не похоже на указатель. Камеры нет?");
            return ExitNothingToRead;
        }

        var hashBefore = U32(data + DataAreaHash);
        Console.WriteLine($"состояние ДО:  Data 0x{data:X}  хэш зоны 0x{hashBefore:X}  камера 0x{camera:X}");
        Console.WriteLine();

        var code = watch > 0
            ? Watch(igs, camera, len, watch, osW, osH)
            : Snapshot(data, camera, len, osW, osH, dump);

        // ── Рамка «после» ────────────────────────────────────────────────────────────────────────
        var dataAfter = Q(igs + IgsData);
        var cameraAfter = Q(igs + IgsCamera);
        var hashAfter = IsPointer(dataAfter) ? U32(dataAfter + DataAreaHash) : 0;

        Console.WriteLine();
        if (data == dataAfter && hashBefore == hashAfter && camera == cameraAfter)
        {
            Console.WriteLine($"СНИМОК ДЕЙСТВИТЕЛЕН: зона, база Data и указатель на камеру не менялись " +
                              $"(0x{hashBefore:X}, 0x{data:X}, 0x{camera:X})");
            return code;
        }

        Console.WriteLine($"СНИМОК НЕДЕЙСТВИТЕЛЕН: было 0x{hashBefore:X}/0x{data:X}/0x{camera:X}, " +
                          $"стало 0x{hashAfter:X}/0x{dataAfter:X}/0x{cameraAfter:X} — " +
                          $"повторить, не используя числа выше.");
        return ExitStale;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // Один снимок: найти поля по критериям.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private static int Snapshot(long data, long camera, int len, int osW, int osH, bool dump)
    {
        var win = Raw(camera, len);
        var player = FindPlayerPos(data, out var playerAddr, out var source, out var crossCheck);

        Console.WriteLine("ПОЗИЦИЯ ИГРОКА (это вход критерия центра)");
        Console.WriteLine(new string('-', 100));
        if (playerAddr == 0)
        {
            Console.WriteLine("  LocalPlayer не прочитан — критерий центра невозможен.");
            return ExitNothingToRead;
        }

        Console.WriteLine($"  LocalPlayer 0x{playerAddr:X}");
        Console.WriteLine($"  {source}");
        Console.WriteLine($"    = ({player.X:F3}, {player.Y:F3}, {player.Z:F3})");
        Console.WriteLine($"  контроль: Positioned.WorldPosition = {crossCheck}");
        Console.WriteLine();

        // ── Критерий экрана ──────────────────────────────────────────────────────────────────────
        Console.WriteLine("КРИТЕРИЙ ЭКРАНА — где в окне камеры лежит пара int32, равная размеру окна игры");
        Console.WriteLine(new string('-', 100));
        var screenHits = new List<int>();
        if (osW > 0)
        {
            for (var off = 0; off + 8 <= len; off += 4)
                if (I32(win, off) == osW && I32(win, off + 4) == osH)
                    screenHits.Add(off);

            if (screenHits.Count == 0)
                Console.WriteLine($"  НЕ НАЙДЕНО: пары ({osW}, {osH}) в окне длиной 0x{len:X} нет.");
            foreach (var off in screenHits)
                Console.WriteLine($"  +0x{off:X3} / +0x{off + 4:X3}   {osW} x {osH}   " +
                                  $"[совпало с GetClientRect — это Width и Height]");

            if (screenHits.Count > 1)
                Console.WriteLine($"  НЕОДНОЗНАЧНО: совпало {screenHits.Count} пар, и по ЗНАЧЕНИЮ они " +
                                  $"неразличимы. Развести их можно только сменой разрешения или размера " +
                                  $"окна игры: после неё повторить замер и взять ту пару, которая изменилась.");
        }
        else
        {
            Console.WriteLine("  пропущен: размер окна у ОС не получен.");
        }

        Console.WriteLine($"  для сравнения, что лежит по НЫНЕШНИМ объявленным смещениям " +
                          $"(Width = 0x4, Height = 0x8): {I32(win, 0x4)} x {I32(win, 0x8)}");
        Console.WriteLine();

        // ── Критерий центра: поиск матрицы ───────────────────────────────────────────────────────
        var halfW = (screenHits.Count > 0 ? I32(win, screenHits[0]) : osW) * 0.5;
        var halfH = (screenHits.Count > 0 ? I32(win, screenHits[0] + 4) : osH) * 0.5;

        Console.WriteLine("КРИТЕРИЙ ЦЕНТРА — какие 16 float окна проецируют позицию игрока в центр экрана по X");
        Console.WriteLine(new string('-', 100));
        Console.WriteLine($"  экран {halfW * 2:F0} x {halfH * 2:F0}, центр по X = {halfW:F1} px");
        Console.WriteLine();
        Console.WriteLine("  Одного «X попал в центр» НЕ ХВАТАЕТ, и это видно на данных: у мусорного блока W");
        Console.WriteLine("  выходит в миллионы, отчего X/W -> 0 и точка «попадает в центр» СЛУЧАЙНО. Поэтому");
        Console.WriteLine("  критерий — конъюнкция из четырёх условий, и все четыре печатаются отдельно:");
        Console.WriteLine("    центр   |X - центр| <= 2 px;");
        Console.WriteLine("    экран   Y лежит в пределах экрана с запасом четверти;");
        Console.WriteLine("    форма   столбцы Z и W пропорциональны в первых трёх строках — это СТРУКТУРА");
        Console.WriteLine("            матрицы вида-проекции, и мусор её не имеет;");
        Console.WriteLine("    глубина W (расстояние до камеры) лежит между выведенными ближней и дальней.");
        Console.WriteLine();
        Console.WriteLine("  смещение   экран X    экран Y   |X-центр|       W (глубина)  центр экран форма глубина");

        var hits = new List<MatrixHit>();
        for (var off = 0; off + 64 <= len; off += 4)
        {
            var m = ReadMatrix(win, off);
            if (m == null) continue;
            if (!Project(m, player, halfW, halfH, out var sx, out var sy, out var cw)) continue;
            if (cw <= 1.0) continue;                       // точка обязана быть ПЕРЕД камерой
            if (Math.Abs(sx - halfW) > 2.0) continue;      // дальше разбираются только попавшие в центр
            hits.Add(new MatrixHit { Off = off, X = sx, Y = sy, W = cw, M = m });
        }

        var passed = new List<MatrixHit>();
        foreach (var h in hits.OrderBy(x => x.Off))
        {
            var okCentre = Math.Abs(h.X - halfW) <= 2.0;
            var okScreen = h.Y >= -0.25 * halfH * 2 && h.Y <= 1.25 * halfH * 2;
            var okShape = Planes(h.M, out var n, out var f, out _, out _);
            var okDepth = okShape && h.W > n && h.W < f;
            if (okCentre && okScreen && okShape && okDepth) passed.Add(h);

            Console.WriteLine($"  +0x{h.Off:X3}     {h.X,9:F2}  {h.Y,9:F2}  {Math.Abs(h.X - halfW),9:F4}  " +
                              $"{h.W,15:F1}  {Mark(okCentre),5} {Mark(okScreen),5} {Mark(okShape),5} {Mark(okDepth),7}" +
                              (okShape ? $"   ближняя {n:F2}, дальняя {f:F2}" : ""));
        }

        if (passed.Count == 0)
        {
            Console.WriteLine();
            Console.WriteLine("  НЕ СОШЁЛСЯ НИ ОДИН: ни один блок не выполнил все четыре условия.");
            Console.WriteLine("  Отрицательный исход засчитывается: матрицы в этом окне нет, либо позиция игрока");
            Console.WriteLine("  читается не оттуда, либо арифметика проекции у форка иная.");
            return ExitNoCriterion;
        }

        Console.WriteLine();
        Console.WriteLine($"  все четыре условия выполнили: " +
                          $"{string.Join(", ", passed.Select(h => $"+0x{h.Off:X3}"))}");

        Console.WriteLine();
        Console.WriteLine($"  сошлось смещений: {passed.Count}. Если их больше одного — это КОПИИ матрицы;");
        Console.WriteLine("  в покое они совпадают побайтно, и развести их можно только ходьбой (--watch N).");

        for (var i = 1; i < passed.Count; i++)
        {
            var same = Enumerable.Range(0, 64).All(k => win[passed[0].Off + k] == win[passed[i].Off + k]);
            Console.WriteLine($"  +0x{passed[0].Off:X3} против +0x{passed[i].Off:X3}: " +
                              (same ? "совпадают ПОБАЙТНО прямо сейчас" : "РАЗЛИЧАЮТСЯ прямо сейчас"));
        }
        Console.WriteLine();

        // ── Критерий положения камеры ────────────────────────────────────────────────────────────
        var first = passed[0];
        Console.WriteLine("КРИТЕРИЙ ПОЛОЖЕНИЯ — центр камеры, вычисленный ОБРАЩЕНИЕМ матрицы, ищется в окне");
        Console.WriteLine(new string('-', 100));

        if (CameraCentre(first.M, out var cx, out var cy, out var cz))
        {
            Console.WriteLine($"  вычислено из матрицы +0x{first.Off:X3}: ({cx:F3}, {cy:F3}, {cz:F3})");
            Console.WriteLine("  (столбцы X, Y и W дают три уравнения на три неизвестных; числа ниоткуда не взяты)");

            var found = 0;
            for (var off = 0; off + 12 <= len; off += 4)
            {
                if (!Near(F(win, off), cx) || !Near(F(win, off + 4), cy) || !Near(F(win, off + 8), cz)) continue;
                found++;
                Console.WriteLine($"  +0x{off:X3}   в памяти лежит ({F(win, off):F3}, {F(win, off + 4):F3}, " +
                                  $"{F(win, off + 8):F3})   [СОВПАЛО — это Position]");
            }

            if (found == 0)
                Console.WriteLine("  НЕ НАЙДЕНО: такой тройки float в окне нет. Position в окне не лежит " +
                                  "или лежит в другом виде.");
            if (found > 1)
                Console.WriteLine($"  НЕОДНОЗНАЧНО: совпало {found} троек, и по ЗНАЧЕНИЮ они неразличимы. " +
                                  "Развести их может только наблюдение в движении (--watch N), если копии " +
                                  "обновляются с разной задержкой.");

            Console.WriteLine($"  для сравнения, что лежит по НЫНЕШНЕМУ объявленному Position = 0xD4: " +
                              $"({F(win, 0xD4):F3}, {F(win, 0xD8):F3}, {F(win, 0xDC):F3})");
            var hyp = first.Off + 0x34;
            if (hyp + 12 <= len)
                Console.WriteLine($"  для сравнения, гипотеза «матрица + 0x34» = +0x{hyp:X3}: " +
                                  $"({F(win, hyp):F3}, {F(win, hyp + 4):F3}, {F(win, hyp + 8):F3})");
        }
        else
        {
            Console.WriteLine("  система вырождена — центр камеры из этой матрицы не восстанавливается.");
        }

        Console.WriteLine();

        // ── Критерий плоскостей ──────────────────────────────────────────────────────────────────
        Console.WriteLine("КРИТЕРИЙ ПЛОСКОСТЕЙ — ближняя и дальняя, выведенные из той же матрицы, ищутся в окне");
        Console.WriteLine(new string('-', 100));

        if (Planes(first.M, out var zNear, out var zFar, out var k, out var d))
        {
            Console.WriteLine($"  z_ndc = {k:F6} + ({d:F3}) / w   =>   ближняя {zNear:F3},  дальняя {zFar:F3}");

            var hitsNear = new List<int>();
            var hitsFar = new List<int>();
            for (var off = 0; off + 4 <= len; off += 4)
            {
                if (Near(F(win, off), zNear)) hitsNear.Add(off);
                if (Near(F(win, off), zFar)) hitsFar.Add(off);
            }

            Console.WriteLine(hitsNear.Count > 0
                ? $"  ближняя найдена по: {string.Join(", ", hitsNear.Select(o => $"+0x{o:X3} ({F(win, o):F3})"))}"
                : "  ближняя в окне НЕ НАЙДЕНА");
            Console.WriteLine(hitsFar.Count > 0
                ? $"  дальняя найдена по: {string.Join(", ", hitsFar.Select(o => $"+0x{o:X3} ({F(win, o):F3})"))}"
                : "  дальняя в окне НЕ НАЙДЕНА");
            Console.WriteLine($"  для сравнения, что лежит по НЫНЕШНЕМУ объявленному ZFar = 0x1C8: {F(win, 0x1C8):F3}");
        }
        else
        {
            Console.WriteLine("  столбцы Z и W матрицы не пропорциональны — вывод плоскостей неприменим.");
        }

        if (dump)
        {
            Console.WriteLine();
            Dump(camera, win, len);
        }

        return ExitOk;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // Наблюдение: тот же критерий центра на каждом кадре. Развести копии матрицы можно ТОЛЬКО так —
    // в покое они совпадают побайтно. Пока идёт замер, надо ХОДИТЬ.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private static int Watch(long igs, long camera, int len, int seconds, int osW, int osH)
    {
        Console.WriteLine($"НАБЛЮДЕНИЕ {seconds} с — ХОДИТЕ, пока идёт замер. В покое копии матрицы неразличимы.");
        Console.WriteLine(new string('-', 100));

        var data = Q(igs + IgsData);
        var win = Raw(camera, len);

        // Смещения матриц ищутся ОДИН раз, на первом кадре, тем же критерием центра.
        var player0 = FindPlayerPos(data, out var pa0, out _, out _);
        if (pa0 == 0)
        {
            Console.WriteLine("  LocalPlayer не прочитан.");
            return ExitNothingToRead;
        }

        var halfW = osW * 0.5;
        var halfH = osH * 0.5;
        var candidates = new List<int>();
        for (var off = 0; off + 64 <= len; off += 4)
        {
            var m = ReadMatrix(win, off);
            if (m == null) continue;
            if (!Project(m, player0, halfW, halfH, out var sx, out var sy, out var cw)) continue;
            if (cw <= 1.0 || Math.Abs(sx - halfW) > 2.0) continue;
            if (sy < -0.5 * halfH || sy > 2.5 * halfH) continue;
            if (!Planes(m, out var n, out var f, out _, out _)) continue;   // та же конъюнкция, что в снимке
            if (cw <= n || cw >= f) continue;
            candidates.Add(off);
        }

        if (candidates.Count == 0)
        {
            Console.WriteLine("  на первом кадре критерий центра не сошёлся.");
            return ExitNoCriterion;
        }

        Console.WriteLine($"  кандидатов-матриц: {string.Join(", ", candidates.Select(o => $"+0x{o:X3}"))}");
        Console.WriteLine();

        var worst = candidates.ToDictionary(o => o, _ => 0.0);
        var worstAt = candidates.ToDictionary(o => o, _ => "");
        var differed = 0;
        var moved = 0;
        var samples = 0;
        var maxStep = 0.0;
        var prev = player0;

        var sw = Stopwatch.StartNew();
        while (sw.Elapsed.TotalSeconds < seconds)
        {
            Thread.Sleep(16);
            var w2 = Raw(camera, len);
            var p = FindPlayerPos(data, out var pa, out _, out _);
            if (pa == 0) continue;
            samples++;

            var step = Math.Sqrt((p.X - prev.X) * (p.X - prev.X) + (p.Y - prev.Y) * (p.Y - prev.Y));
            if (step > 1.0) moved++;
            if (step > maxStep) maxStep = step;
            prev = p;

            if (candidates.Count > 1 &&
                !Enumerable.Range(0, 64).All(k => w2[candidates[0] + k] == w2[candidates[1] + k]))
                differed++;

            foreach (var off in candidates)
            {
                var m = ReadMatrix(w2, off);
                if (m == null) continue;
                if (!Project(m, p, halfW, halfH, out var sx, out var sy, out var cw) || cw <= 1.0) continue;
                var err = Math.Abs(sx - halfW);
                if (err > worst[off])
                {
                    worst[off] = err;
                    worstAt[off] = $"игрок ({p.X:F1}, {p.Y:F1}, {p.Z:F1}) -> экран ({sx:F1}, {sy:F1})";
                }
            }
        }

        Console.WriteLine($"  кадров опрошено {samples}; из них со смещением игрока > 1 ед. мира: {moved}; " +
                          $"наибольший шаг {maxStep:F1}");
        if (candidates.Count > 1)
            Console.WriteLine($"  кадров, где копии матрицы РАЗЛИЧАЛИСЬ: {differed} из {samples}");
        Console.WriteLine();
        Console.WriteLine("  смещение   наибольшая |X - центр| за наблюдение   на чём достигнута");
        foreach (var off in candidates.OrderBy(o => worst[o]))
            Console.WriteLine($"  +0x{off:X3}      {worst[off],12:F3} px                  {worstAt[off]}");

        Console.WriteLine();
        if (moved == 0)
            Console.WriteLine("  ПЕРСОНАЖ НЕ ДВИГАЛСЯ: этот прогон копии матрицы НЕ РАЗВОДИТ. Повторить, ходя.");
        else if (differed == 0 && candidates.Count > 1)
            Console.WriteLine("  копии ни разу не разошлись, хотя персонаж двигался — значит это одно и то же " +
                              "значение, и выбор между ними безразличен.");
        else
            Console.WriteLine("  выбирать ту, у которой наибольшая ошибка меньше: она соответствует ТЕКУЩЕМУ кадру.");

        return ExitOk;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // Чтение позиции игрока. Основной источник — Render.Pos (это и есть Entity.Pos у форка),
    // компонент опознаётся по RVA собственной vtable. Рядом печатается Positioned.WorldPosition —
    // независимый контроль: X и Y обязаны совпасть.
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private static Vec3 FindPlayerPos(long data, out long playerAddr, out string source, out string crossCheck)
    {
        source = "не прочитано";
        crossCheck = "не прочитан";
        playerAddr = 0;

        var player = Q(data + DataLocalPlayer);
        if (!IsPointer(player)) return default;
        playerAddr = player;

        var positioned = Q(player + EntPositioned);
        if (IsPointer(positioned))
        {
            var b = Raw(positioned + PositionedWorldPos, 12);
            crossCheck = $"({BitConverter.ToSingle(b, 0):F3}, {BitConverter.ToSingle(b, 4):F3}, " +
                         $"{BitConverter.ToSingle(b, 8):F3})";
        }

        var first = Q(player + EntComps);
        var last = Q(player + EntComps + 8);
        if (!IsPointer(first) || last < first || last - first > 0x2000) return default;

        for (long i = 0; i < (last - first) / 8; i++)
        {
            var comp = Q(first + i * 8);
            if (!IsPointer(comp)) continue;
            if (Q(comp) - _moduleBase != RenderVtableRva) continue;

            var b = Raw(comp + RenderPos, 12);
            source = $"Render.Pos (компонент 0x{comp:X}, опознан по RVA vtable 0x{RenderVtableRva:X})";
            return new Vec3
            {
                X = BitConverter.ToSingle(b, 0),
                Y = BitConverter.ToSingle(b, 4),
                Z = BitConverter.ToSingle(b, 8)
            };
        }

        return default;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    // Арифметика. Матрица читается ровно так, как её читает форк: SharpDX Matrix — 16 float подряд,
    // строка за строкой, а Vector4.Transform умножает СТРОКУ-ВЕКТОР на матрицу:
    //     out[j] = sum_i v[i] * M[i][j]
    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private static float[] ReadMatrix(byte[] win, int off)
    {
        var m = new float[16];
        for (var i = 0; i < 16; i++)
        {
            var v = BitConverter.ToSingle(win, off + i * 4);
            if (float.IsNaN(v) || float.IsInfinity(v) || Math.Abs(v) > 1e9f) return null;
            m[i] = v;
        }

        return m;
    }

    private static bool Project(float[] m, Vec3 p, double halfW, double halfH,
        out double screenX, out double screenY, out double clipW)
    {
        screenX = screenY = clipW = 0;
        double[] v = { p.X, p.Y, p.Z, 1.0 };
        var o = new double[4];
        for (var j = 0; j < 4; j++)
        for (var i = 0; i < 4; i++)
            o[j] += v[i] * m[i * 4 + j];

        clipW = o[3];
        if (Math.Abs(clipW) < 1e-6 || double.IsNaN(clipW) || double.IsInfinity(clipW)) return false;

        screenX = (o[0] / clipW + 1.0) * halfW;
        screenY = (1.0 - o[1] / clipW) * halfH;
        return !double.IsNaN(screenX) && !double.IsNaN(screenY);
    }

    // Центр камеры — точка, дающая ноль по столбцам X, Y и W (классический центр проекции).
    // Три уравнения, три неизвестных; решается исключением Гаусса с выбором ведущего элемента.
    private static bool CameraCentre(float[] m, out double cx, out double cy, out double cz)
    {
        cx = cy = cz = 0;
        int[] cols = { 0, 1, 3 };
        var a = new double[3, 4];
        for (var r = 0; r < 3; r++)
        {
            var j = cols[r];
            a[r, 0] = m[0 * 4 + j];
            a[r, 1] = m[1 * 4 + j];
            a[r, 2] = m[2 * 4 + j];
            a[r, 3] = -m[3 * 4 + j];
        }

        for (var c = 0; c < 3; c++)
        {
            var pivot = c;
            for (var r = c + 1; r < 3; r++)
                if (Math.Abs(a[r, c]) > Math.Abs(a[pivot, c])) pivot = r;
            if (Math.Abs(a[pivot, c]) < 1e-9) return false;

            if (pivot != c)
                for (var k = 0; k < 4; k++)
                    (a[c, k], a[pivot, k]) = (a[pivot, k], a[c, k]);

            for (var r = 0; r < 3; r++)
            {
                if (r == c) continue;
                var f = a[r, c] / a[c, c];
                for (var k = c; k < 4; k++) a[r, k] -= f * a[c, k];
            }
        }

        cx = a[0, 3] / a[0, 0];
        cy = a[1, 3] / a[1, 1];
        cz = a[2, 3] / a[2, 2];
        return !double.IsNaN(cx) && !double.IsNaN(cy) && !double.IsNaN(cz);
    }

    // Плоскости. У матрицы вида-проекции столбец Z пропорционален столбцу W во всех строках, кроме
    // последней: z_clip = k * w_clip + d. Значит z_ndc = k + d/w, откуда w при z_ndc = 0 и при 1.
    private static bool Planes(float[] m, out double zNear, out double zFar, out double k, out double d)
    {
        zNear = zFar = k = d = 0;
        var ks = new List<double>();
        for (var i = 0; i < 3; i++)
        {
            var w = m[i * 4 + 3];
            if (Math.Abs(w) < 1e-9) continue;
            ks.Add(m[i * 4 + 2] / w);
        }

        if (ks.Count < 2) return false;
        var mean = ks.Average();
        if (ks.Any(x => Math.Abs(x - mean) > 1e-4 * Math.Max(1.0, Math.Abs(mean)))) return false;
        k = mean;

        d = m[3 * 4 + 2] - k * m[3 * 4 + 3];
        if (Math.Abs(k) < 1e-9 || Math.Abs(1.0 - k) < 1e-9) return false;

        zNear = -d / k;
        zFar = d / (1.0 - k);
        return zNear > 0 && zFar > zNear;
    }

    // ─────────────────────────────────────────────────────────────────────────────────────────────
    private static void Dump(long camera, byte[] win, int len)
    {
        Console.WriteLine("ДАМП ОКНА КАМЕРЫ ПО 8 БАЙТ");
        Console.WriteLine(new string('-', 100));
        Console.WriteLine("  смещение   значение             int32 пара              float пара");
        for (var off = 0; off + 8 <= len; off += 8)
        {
            var q = BitConverter.ToUInt64(win, off);
            Console.WriteLine($"  +0x{off:X3}      0x{q:X16}   {I32(win, off),11} {I32(win, off + 4),11}   " +
                              $"{F(win, off),13:G6} {F(win, off + 4),13:G6}");
        }

        Console.WriteLine($"  окно 0x{camera:X} .. 0x{camera + len:X}");
    }

    private struct Vec3
    {
        public float X, Y, Z;
    }

    private sealed class MatrixHit
    {
        public int Off;
        public double X, Y, W;
        public float[] M;
    }

    // Совпадение float'ов: относительное, потому что сравниваются ВЫЧИСЛЕННОЕ (double, с накоплённой
    // ошибкой обращения матрицы) и ЛЕЖАЩЕЕ В ПАМЯТИ (float, семь значащих цифр).
    private static bool Near(double a, double b)
    {
        if (double.IsNaN(a) || double.IsNaN(b) || double.IsInfinity(a) || double.IsInfinity(b)) return false;
        var scale = Math.Max(1.0, Math.Max(Math.Abs(a), Math.Abs(b)));
        return Math.Abs(a - b) <= 1e-3 * scale;
    }

    private static string Mark(bool ok) => ok ? "да" : "НЕТ";

    private static int I32(byte[] b, int off) => BitConverter.ToInt32(b, off);
    private static float F(byte[] b, int off) => BitConverter.ToSingle(b, off);

    private static long Q(long address) => BitConverter.ToInt64(Raw(address, 8), 0);
    private static uint U32(long address) => BitConverter.ToUInt32(Raw(address, 4), 0);

    private static byte[] Raw(long address, int size)
    {
        var buffer = new byte[size];
        if (address <= 0) return buffer;
        ReadProcessMemory(_handle, (IntPtr)address, buffer, size, out _);
        return buffer;
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

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool GetClientRect(IntPtr handle, out Rect rect);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr OpenProcess(int access, bool inherit, int pid);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadProcessMemory(IntPtr handle, IntPtr address, byte[] buffer,
        int size, out IntPtr read);
}
