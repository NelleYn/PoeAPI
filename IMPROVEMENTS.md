# Предложения по улучшению — ExileApi (ExileCore)

_Сгенерировано автоматически: 214 идей (цель — 200), из них отобрано 35 лучших._

## О проекте

ExileApi (сборка `ExileCore`) — приватный форк PoeHud: Windows-only HUD/overlay-фреймворк для Path of Exile на **C# / .NET 10** (`net10.0-windows`, x64). Приложение отдельным процессом читает память игры в режиме read-only, интерпретирует её как типизированные объекты (`Core/PoEMemory/**`), кэширует чтения (`Core/Shared/Cache/**`) и рисует оверлей поверх окна игры через DirectX 11 + ImGui (`Core/RenderQ/**`). Плагины (`Plugins/Compiled/**`, `Plugins/Source/**`) загружаются и компилируются в рантайме через Roslyn (`Core/Shared/RoslynCompiler.cs`, `PluginManager.cs`). Три проекта: `GameOffsets` (структуры под сырую память), `Core` (движок, namespace `ExileCore`), `Loader` (точка входа, Windows Forms + DX11-окно). Многопоточность реализована собственным пулом (`Core/MultiThreadManager.cs`) и корутинами (`Core/Shared/Runner.cs`, `Coroutine.cs`).

Замечание: собрать/запустить проект в этой среде нельзя (нет Windows, .NET-тулчейна и живой игры), поэтому все находки основаны на чтении исходников. Это документ-предложение: исходный код не менялся.

## Топ 35: приоритетные улучшения

### 1. `Thread.Abort()` падает на .NET 10
- **Категория:** Bugs / correctness
- **Impact:** High **Effort:** S
- **Обоснование:** `ThreadUnit.ForceAbort()` (`Core/MultiThreadManager.cs:455`) вызывает `thread.Abort()` на `System.Threading.Thread`. Начиная с .NET 5 `Thread.Abort()` бросает `PlatformNotSupportedException`, а проект таргетит `net10.0-windows`. Механизм «починки» зависших воркеров при срабатывании выкинет исключение вместо восстановления потока. Нужен кооперативный останов (флаг + `CancellationToken`).

### 2. Падение при первом запуске: `CoreSettings` остаётся null
- **Категория:** Bugs / correctness
- **Impact:** High **Effort:** S
- **Обоснование:** В `SettingsContainer.LoadCoreSettings()` (`Core/SettingsContainer.cs:67-78`) при отсутствии файла создаётся локальный `coreSettings`, пишется на диск, но поле `CoreSettings` не присваивается; следующая строка `CurrentProfileName = CoreSettings.Profiles.Value` даёт NRE. Ошибка гасится в `Console.WriteLine`, после чего `Core` получает `CoreSettings == null` и падает каскадом. Чистая установка не стартует.

### 3. Утечка write-lock в `SettingsContainer` (потенциальный дедлок)
- **Категория:** Bugs / correctness
- **Impact:** High **Effort:** S
- **Обоснование:** `SaveCoreSettings` (`:91-96`) и `SaveSettings` (`:109-116`) вызывают `rwLock.EnterWriteLock()` без `try/finally`. Любое исключение при сериализации/`File.WriteAllText` оставляет lock захваченным — все последующие сохранения виснут навсегда. Обернуть в `try/finally`.

### 4. Рассинхрон имён файлов настроек: `Name` vs `InternalName`
- **Категория:** Bugs / correctness
- **Impact:** High **Effort:** S
- **Обоснование:** `SaveSettings` пишет в `{plugin.InternalName}_settings.json` (`Core/SettingsContainer.cs:113`), а `LoadSettings` читает из `{plugin.Name}_settings.json` (`:125`). В `BaseSettingsPlugin` `Name` — сеттабельное свойство, часто отличное от `InternalName` (= namespace). Если плагин задал кастомное `Name`, настройки сохраняются, но не загружаются. Использовать один ключ.

### 5. Пропуск корутин при удалении в `Runner`
- **Категория:** Bugs / correctness
- **Impact:** High **Effort:** S
- **Обоснование:** `Runner.Update()` и `ParallelUpdate()` (`Core/Shared/Runner.cs:140-181`, `:195-238`) итерируются по индексу `for(i…)` и в той же итерации делают `Coroutines.Remove(coroutine)`. После удаления элемента индексы смещаются, и следующая корутина пропускается в этом кадре. Итерировать копию или идти с конца.

### 6. `Coroutine.OwnerName` всегда «Free»
- **Категория:** Bugs / correctness
- **Impact:** Med **Effort:** S
- **Обоснование:** В приватном конструкторе (`Core/Shared/Coroutine.cs:14-19`) `OwnerName` вычисляется из свойства `Owner` (ещё null на этом этапе), а не из параметра `owner`. В итоге весь диагностический вывод по корутинам показывает «Free» вместо namespace владельца — теряется привязка проблемных корутин к плагину.

### 7. `Runner.Coroutines` — неблокирующий `List`, читаемый/мутируемый из нескольких потоков
- **Категория:** Concurrency / thread-safety
- **Impact:** High **Effort:** M
- **Обоснование:** `ParallelUpdate` (`Runner.cs:195-247`) раздаёт `coroutine.MoveNext()` в воркер-потоки через `MultiThreadManager`, одновременно делая `Coroutines.Add/Remove` и обходя список. `List<Coroutine>` не потокобезопасен: возможны пропуски, `IndexOutOfRange` и порча состояния. Нужен снапшот на кадр или конкурентная структура.

### 8. Однопоточная ветка `MultiThreadManager.Process` теряет починенный поток
- **Категория:** Bugs / correctness
- **Impact:** Med **Effort:** S
- **Обоснование:** В ветке `else` (`Core/MultiThreadManager.cs:251-256`) при `ThreadsCount == 1` создаётся новый `ThreadUnit` в локальную переменную `threadUnit`, но он не записывается обратно в `threads[0]` и не попадает в `FreeThreads`. После первой «починки» пул из одного потока деградирует.

### 9. `ExileApi.sln` ссылается на gitignore-нутые проекты плагинов
- **Категория:** Config / build
- **Impact:** High **Effort:** S
- **Обоснование:** Решение перечисляет ~17 проектов `Plugins/Source/*/*.csproj`, а `.gitignore` целиком игнорирует `Plugins/`. На свежем клоне этих файлов нет, поэтому `dotnet build -c Release ExileApi.sln` (главная команда из README) падает. Нужно отвязать плагины от `.sln` (solution filter) или не игнорировать примеры.

### 10. Нет CI
- **Категория:** Tests & CI
- **Impact:** High **Effort:** M
- **Обоснование:** Каталог `.github/` отсутствует. Даже без игры Windows-раннер GitHub Actions может собирать `Loader/Loader.csproj` (тянет `Core` и `GameOffsets`) и ловить регрессии компиляции — критично для проекта, где плагины компилируются против `ExileCore.dll`.

### 11. Нет модульных тестов на чистую логику
- **Категория:** Tests & CI
- **Impact:** High **Effort:** M
- **Обоснование:** В репозитории ноль тестов. Не зависящие от игры части — `RoslynCompiler`, кэши (`Cache/*`), `TypeConverter`, `MathHepler`, парсер паттернов `Memory.FindPatterns/CompareData`, `SettingsParser` — тестируются на любой ОС. Дают быструю защиту от регрессий вроде находок №2–8.

### 12. Удалить финализатор `CachedValue`
- **Категория:** Performance
- **Impact:** High **Effort:** S
- **Обоснование:** `~CachedValue()` (`Core/Shared/Cache/CachedValue.cs:102-105`) существует лишь чтобы декрементировать счётчик `LifeCount`. Каждый кэш-объект (а их тысячи на сущность/кадр) попадает в очередь финализации → лишний проход GC и давление на память. Заменить на `IDisposable` или убрать счётчик.

### 13. `Input.IsKeyDown` бросает `KeyNotFoundException`
- **Категория:** Bugs / correctness
- **Impact:** Med **Effort:** S
- **Обоснование:** `IsKeyDown(Keys)` (`Core/Input.cs:67-75`) под `#if DebugKeys` логирует незарегистрированную клавишу, но всё равно выполняет `return Keys[nVirtKey]`, что бросает исключение на отсутствующем ключе. Плагин, забывший `RegisterKey`, роняет тик. Использовать `TryGetValue` с `false` по умолчанию.

### 14. Не проверяется результат `ReadProcessMemory` (дрейф оффсетов не виден)
- **Категория:** Bugs / correctness
- **Impact:** High **Effort:** M
- **Обоснование:** `Memory.Read<T>` (`Core/Memory.cs:274-285`) и `ReadMem` (`:107-122`) игнорируют возвращаемое значение/число прочитанных байт `ProcessMemory.ReadProcessMemory(Array)`. Частичное или неуспешное чтение молча даёт `default`/мусорную структуру — после патча игры оффсеты «плывут» без единого сигнала. Проверять успех и логировать через Serilog.

### 15. `ReadMem` перебрасывает исключение вопреки контракту «best-effort»
- **Категория:** Error handling & logging
- **Impact:** Med **Effort:** S
- **Обоснование:** Docstring `Memory` обещает «best-effort, defaults/empty on invalid». Но `ReadMem(IntPtr,size)` (`:117-121`) в `catch` делает `throw;`, и одно плохое чтение в цикле (например, в `ReadStructsArray`) валит весь тик через `Core.Tick` catch. Вернуть пустой буфер согласно контракту.

### 16. `RangeNode<long>` обрезается до `int` в меню
- **Категория:** Bugs / correctness
- **Impact:** Med **Effort:** S
- **Обоснование:** В `SettingsParser` (`Core/SettingsParser.cs:229-236`) ветка `RangeNode<long>` делает `(int) n.Value`, `(int) n.Min`, `(int) n.Max` и рисует `SliderInt`. Значения вне диапазона `int` ломаются/переполняются. Использовать корректный виджет для long.

### 17. Случайные ID пунктов меню могут коллизиться
- **Категория:** Bugs / correctness
- **Impact:** Med **Effort:** S
- **Обоснование:** При `menuAttribute.index == -1` ID берётся как `MathHepler.Randomizer.Next(int.MaxValue)` (`SettingsParser.cs:54`). Совпадение ID двух холдеров ломает поиск родителя (`Find(x => x.ID == parentIndex)`) и вложенность меню. Использовать детерминированный монотонный счётчик.

### 18. Уйти с устаревшего `Serilog.Sinks.RollingFile` и обновить Serilog
- **Категория:** Dependencies & maintenance
- **Impact:** Med **Effort:** S
- **Обоснование:** `Core.csproj`/`Loader.csproj` тянут `Serilog 2.8.0` (очень старый) и одновременно `Serilog.Sinks.RollingFile 3.3.0` (архивирован, deprecated) вместе с `Serilog.Sinks.File`. RollingFile убрать, файловый роллинг делать через `Serilog.Sinks.File`, поднять Serilog до 3.x/4.x.

### 19. Централизованное управление версиями пакетов
- **Категория:** Config / build
- **Impact:** Med **Effort:** S
- **Обоснование:** SharpDX 4.2.0 и Serilog-пакеты продублированы в `Core.csproj` и `Loader.csproj` (`Directory.Build.props` их не задаёт). Ввести `Directory.Packages.props` с `ManagePackageVersionsCentrally` — исключит расхождение версий между проектами.

### 20. Единый канал логирования
- **Категория:** Error handling & logging
- **Impact:** Med **Effort:** M
- **Обоснование:** Ошибки размазаны по трём каналам: `Core.Logger`/`Logger.Log` (Serilog), `DebugWindow.LogError` (оверлей) и `Console.WriteLine` (`Runner.cs:160-170`, `SettingsContainer.cs:82,100`). Часть ошибок вообще не попадает в файл-лог. Свести к Serilog + прокидывать в `DebugWindow` для UI. Заодно убрать диагностику `"WTF {type}"` (`MultiThreadManager.cs:158`).

### 21. Не глотать ошибки загрузки ссылок в `RoslynCompiler`
- **Категория:** Error handling & logging
- **Impact:** Med **Effort:** S
- **Обоснование:** `BuildReferences` (`Core/Shared/RoslynCompiler.cs:167-168`) делает `catch { }` при `CreateFromFile`. Если ключевая сборка не подгрузилась, плагин упадёт на непонятных ошибках компиляции «type not found». Логировать пропущенные ссылки.

### 22. Вызывать `Core` напрямую вместо reflection в `Loader`
- **Категория:** Architecture / refactor
- **Impact:** Med **Effort:** M
- **Обоснование:** `Loader.cs` делает `Assembly.Load("ExileCore")` и дёргает `Render`/`Dispose`/`FixImGui` через `GetMethod(...).Invoke` в цикле рендера (`:128-142`), хотя есть `ProjectReference` на `Core`. Reflection в hot-path хрупок (ломается при переименовании) и медленнее. Заменить на прямые вызовы.

### 23. Дубли текстур `textures/` и `Core/textures/`
- **Категория:** Developer experience / tooling
- **Impact:** Low **Effort:** S
- **Обоснование:** По 33 одинаковых PNG в корневом `textures/` и в `Core/textures/`; в вывод копируется только `Core/textures/**` (`Core.csproj`). Корневая копия — мёртвый вес и риск рассинхрона. Оставить один источник.

### 24. Добавить LICENSE и прояснить лицензию форка
- **Категория:** Documentation
- **Impact:** High **Effort:** S
- **Обоснование:** Файла LICENSE нет, а это «private fork of PoeHud» (README). Без явной лицензии правовой статус переиспользования и вклада плагинов неясен. Добавить LICENSE, совместимый с апстримом.

### 25. Предупреждение об анти-чите/ToS в README
- **Категория:** Security
- **Impact:** Med **Effort:** S
- **Обоснование:** Инструмент читает память чужого процесса и шлёт синтетический ввод (`mouse_event`/`keybd_event` в `Input.cs`) — ровно то, что детектят анти-читы, и что нарушает ToS GGG. В README/доках нет предупреждения о рисках блокировки. Добавить явную секцию.

### 26. Переписать статус/сборку в README
- **Категория:** Documentation
- **Impact:** Med **Effort:** S
- **Обоснование:** README открывается двумя H1 подряд и фразой «Current version can lags and crash because i didn't test that a lot time». Плюс рекомендованная команда `dotnet build ExileApi.sln` не работает (см. №9). Дать корректный quickstart (собирать `Loader/Loader.csproj`).

### 27. Перейти на `SendInput` вместо legacy `keybd_event`/`mouse_event`
- **Категория:** Bugs / correctness
- **Impact:** Med **Effort:** M
- **Обоснование:** `Input.cs` использует `WinApi.keybd_event`/`mouse_event` (устаревшие с 20 лет, документированы как «superseded by SendInput»). Они не атомарны, хуже работают с абсолютными координатами/несколькими мониторами и легче фильтруются. `SendInput` надёжнее и группирует события.

### 28. Оптимизировать `FindPatterns`
- **Категория:** Performance
- **Impact:** Med **Effort:** M
- **Обоснование:** `Memory.FindPatterns` (`Core/Memory.cs:373-442`) целиком копирует ~33 МБ образа модуля в managed `byte[]` и делает наивный побайтовый скан на каждый паттерн. Использовать `Span<byte>`/`IndexOf` или Boyer–Moore и переиспользовать буфер — заметно ускорит холодный старт HUD.

### 29. Создавать каталог `Logs` перед fallback-записью
- **Категория:** Bugs / correctness
- **Impact:** Low **Effort:** S
- **Обоснование:** `Loader.LogLoaderError` при отсутствии логгера пишет в `Logs\Loader.txt` через `File.WriteAllText` (`Loader/Loader.cs:179`), не создавая каталог. Если `Logs/` нет, аварийное логирование само бросает `DirectoryNotFoundException`, скрывая исходную ошибку.

### 30. Модель доверия и allowlist для плагинов
- **Категория:** Security
- **Impact:** Med **Effort:** L
- **Обоснование:** `PluginManager`/`RoslynCompiler` грузят произвольные DLL и компилируют исходники с полным доверием, `AllowUnsafe`, ссылками на все TPA и любые `libs/*.dll`. Плагин получает права на чтение памяти игры и синтетический ввод. Минимум — задокументировать риск, в идеале — allowlist ссылок и проверка происхождения.

### 31. Исправить страж переполнения в `ReadStructsArray`
- **Категория:** Bugs / correctness
- **Impact:** Low **Effort:** S
- **Обоснование:** В `Memory.ReadStructsArray` (`Core/Memory.cs:156-160`) при `i > 100000` код только логирует, но не делает `break` — цикл продолжается до `endAddress`, добавляя мусорные объекты. Прерывать чтение.

### 32. Инкрементально включить Nullable reference types
- **Категория:** Architecture / refactor
- **Impact:** Med **Effort:** L
- **Обоснование:** Во всех csproj `Nullable=disable`, при том что кодовая база полна возвращающих null указателей и `GetObject<T>`/`GetComponent<T>`. Включение (хотя бы `annotations`) в новых файлах ловит целый класс NRE вроде находки №2 на этапе компиляции.

### 33. Разнести/сгенерировать `GameStat.cs`
- **Категория:** Performance
- **Impact:** Med **Effort:** M
- **Обоснование:** `Core/Shared/Enums/GameStat.cs` — 53 071 строка одного enum (сравнимо со всей остальной кодовой базой ~34k). Замедляет компиляцию и IDE, раздувает метаданные сборки. Генерировать из данных/ресурса или разбить, оставив только используемые значения.

### 34. Добавить `global.json` для фиксации SDK
- **Категория:** Config / build
- **Impact:** Low **Effort:** S
- **Обоснование:** Нет `global.json`, фиксирующего версию .NET 10 SDK. Для проекта с рантайм-компиляцией через Roslyn различие версий SDK у разработчиков/CI ведёт к «у меня собирается, у тебя нет». Закрепить SDK.

### 35. Переименовать публичный `MathHepler` → `MathHelper`
- **Категория:** Architecture / refactor
- **Impact:** Low **Effort:** M
- **Обоснование:** Опечатка в имени публичного класса `Core/Shared/Helpers/MathHepler.cs` тиражируется по всей базе (`SettingsParser`, `Coroutine` и др.) и попадает в API, которым пользуются плагины. Переименовать с сохранением обёртки-`[Obsolete]` для совместимости плагинов.

---

## Полный список кандидатов (214)

### Bugs / correctness (28)
1. `ThreadUnit.ForceAbort()` вызывает `thread.Abort()` — `PlatformNotSupportedException` на .NET 10 (`MultiThreadManager.cs:455`).
2. Первый запуск: `CoreSettings` не присваивается, `CurrentProfileName = CoreSettings.Profiles.Value` даёт NRE (`SettingsContainer.cs:67-78`).
3. `SaveCoreSettings`/`SaveSettings` без `try/finally` вокруг `EnterWriteLock` → утечка write-lock (`SettingsContainer.cs:91,109`).
4. Рассинхрон ключа файла: сохранение по `InternalName`, чтение по `Name` (`SettingsContainer.cs:113` vs `:125`).
5. `Runner.Update`/`ParallelUpdate` удаляют элемент списка внутри `for` по индексу → пропуск корутины (`Runner.cs:140-181`).
6. `Coroutine` ctor: `OwnerName` считается из `Owner` (null), а не из параметра `owner` → всегда «Free» (`Coroutine.cs:18`).
7. Однопоточная ветка `Process` теряет починенный `ThreadUnit` (не пишется в `threads[0]`) (`MultiThreadManager.cs:251-256`).
8. `Input.IsKeyDown` индексирует `Keys[nVirtKey]` → `KeyNotFoundException` для незарегистрированных клавиш (`Input.cs:74`).
9. `Read<T>`/`ReadMem` игнорируют результат `ReadProcessMemory` → тихие частичные чтения (`Memory.cs:283,113`).
10. `ReadMem(IntPtr,size)` перебрасывает исключение вопреки контракту «best-effort» (`Memory.cs:117-121`).
11. `RangeNode<long>` приводится к `int` в `SettingsParser` — переполнение больших значений (`SettingsParser.cs:229-236`).
12. Случайные ID меню (`Randomizer.Next(int.MaxValue)`) могут коллизиться и ломать вложенность (`SettingsParser.cs:54`).
13. `LoadCoreSettings` создаёт дефолт через `File.AppendAllText` (дописывает, а не перезаписывает) (`SettingsContainer.cs:70`).
14. `ReadStructsArray` при `i>100000` логирует, но не `break` — продолжает добавлять мусор (`Memory.cs:156-160`).
15. `Loader.LogLoaderError` пишет в `Logs\Loader.txt` без создания каталога → падает fallback-логгер (`Loader.cs:179`).
16. `ReadString` guard `addr <= 65536 && addr >= -1` пропускает адреса `< -1` (`Memory.cs:56`).
17. `CachedValue.Value` до первого успешного `Update` возвращает `_func()` при каждом чтении — кэш не работает (`CachedValue.cs:69-73`).
18. `ReadDoublePtrVectorClasses` считает `length = last - start` без валидации отрицательного/огромного размера перед `ReadMem` (`Memory.cs:175`).
19. `ReadDoublePointerIntList` читает первый узел до проверки таймаута/валидности `head` (`Memory.cs:298-300`).
20. `Coroutine.MoveNext` рекурсивно спускается по вложенным `IEnumerator` без ограничения глубины → возможен stack overflow (`Coroutine.cs:183-198`).
21. `MultiThreadManager.Process` вход защищён `_lock`, но при раннем `return` из-за занятого lock вызывающий думает, что работа сделана (`MultiThreadManager.cs:154`).
22. `KeyPressRelease` использует один статический `sw` на все клавиши — тайминги мешаются при нескольких клавишах (`Input.cs:222-273`).
23. `SetCursorPositionSmooth`: шаг `Max(dist/100,4)`, но цикл `if(step>6)` — при `step` в (4;6] движение мгновенное без сглаживания (`Input.cs:123-136`).
24. `WaitRandom` использует общий `Stopwatch sw` из `YieldBase` и `rnd.Next(min,max)` — при `min==max` бросит `ArgumentOutOfRange` (`Coroutine.cs:201-226`).
25. `SettingsContainer.LoadSettings` бросает `DirectoryNotFoundException`, если профиль не создан, хотя это штатный случай при первом заходе (`SettingsContainer.cs:122-123`).
26. `Core` ctor подписывает `_soundController.SetVolume` даже когда `_soundController` null (пропущен на Win7) → NRE при изменении громкости (`Core.cs:114-118`).
27. `FindPoeProcess` при нескольких клиентах и `Cancel` возвращает `clients[-1]` только после проверки `ixChosen`, но `ChooseSingleProcess` сравнивает лишь первые 2 из возможного большего списка (`Core.cs:514-541`).
28. `RangeNode<int>` в `Core` ctor пересоздаётся с `min=0,max=ProcessorCount`, обрезая ранее сохранённое пользователем значение потоков (`Core.cs:101`).

### Concurrency / thread-safety (14)
1. `Runner.Coroutines` (`List`) читается/мутируется из main- и parallel-потоков одновременно (`Runner.cs:195-247`).
2. `Input.Keys`/`KeysPressed` словари читаются на render-потоке и пишутся в `Update`/`RegisterKey` под разными/без локов (`Input.cs:35-118`).
3. `Core.DebugInformations` (`ObservableCollection`) мутируется из воркеров, тогда как UI её перечисляет — не потокобезопасно (`Core.cs:210`).
4. `ThreadUnit.Free`/`AddJob` гонятся с `DoWork` при завершении джобы — неатомарная подмена слота `Job`/`SecondJob` (`MultiThreadManager.cs:350,409`).
5. `CachedValue.Value` пишет/читает `_value` и `_updated` без синхронизации при доступе с разных потоков (`CachedValue.cs:53-79`).
6. `PluginManager.TryLoadPlugin` добавляет в `Plugins` под `locker`, но `Core.Tick` читает `Plugins` без лока при параллельной загрузке (`PluginManager.cs:299-304`).
7. `CoroutinePerformance` (`Dictionary`) обновляется из `ParallelUpdate` и воркеров без синхронизации (`Runner.cs:221-246`).
8. `MultiThreadManager.ChangeNumberThreads` вызывается из корутины смены числа потоков, но `threads[]` читается конкурентно в `Process` (`MultiThreadManager.cs:78-113`).
9. `WaitRender.FrameCount`/`WaitRender.Frame()` — статический счётчик без `volatile`/`Interlocked`, гонка между render- и coroutine-потоками (`Coroutine.cs:237-243`).
10. `Job` использует `volatile bool`, но `ElapsedMs`/`WorkingOnThread` меняются без барьеров и читаются в диагностике (`MultiThreadManager.cs:37-40`).
11. `SettingsContainer.rwLock` защищает записи, но `LoadSettings`/`LoadCoreSettings` читают файлы без read-lock (`SettingsContainer.cs:120-130`).
12. `EntityListWrapperOnEntityAdded` шлёт `EntityAdded` из воркеров нескольким плагинам параллельно — общий стейт плагинов может биться (`PluginManager.cs:445-467`).
13. Статический `Core.SyncLocker` заявлен для «shared static state», но реально почти нигде не используется — синхронизация непоследовательна (`Core.cs:34`).
14. `ParallelCoroutineManualThread` ловит исключение и `throw;` — падение фонового потока роняет процесс без диагностики в UI (`Core.cs:581-585`).

### Performance (20)
1. Финализатор `~CachedValue` кладёт каждый кэш в очередь финализации → нагрузка на GC (`CachedValue.cs:102-105`).
2. `FindPatterns` копирует ~33 МБ в managed-массив и сканирует наивно на каждый паттерн (`Memory.cs:373-442`).
3. `Core.Tick` busy-ждёт джобы `SpinWait.SpinUntil(..., JOB_TIMEOUT_MS)` каждый кадр (`Core.cs:455`).
4. `MultiThreadManager.Process` крутит `spinWait.SpinOnce()` в опросе занятых потоков — сжигает CPU под нагрузкой (`MultiThreadManager.cs:188-227`).
5. `ReadPointersArray`/`ReadDoublePtrVectorClasses` аллоцируют новый `List<T>` и полный буфер на каждый вызов в hot-path (`Memory.cs:167-225`).
6. `static Input()` заполняет `KeysPressed` для всех значений enum `Keys` (сотни записей) (`Input.cs:43-49`).
7. `SettingsParser.Parse` рефлексирует свойства при каждом построении меню — кэшировать делегаты по типу (`SettingsParser.cs:37-251`).
8. `GameStat.cs` (53k строк enum) раздувает компиляцию и метаданные сборки (`Core/Shared/Enums/GameStat.cs`).
9. `ReadNativeArray` дважды вычисляет длину и повторно читает указатели через `ReadPointersArray` (`Memory.cs:451-466`).
10. `Core.Render` дергает `Time.TotalMilliseconds` многократно за проход вместо кэша локальной переменной (`Core.cs:589-641`).
11. `PluginManager` компилит исходные плагины при каждом старте, если нет `Errors.txt` — нет кэша артефактов по хэшу исходников (`PluginManager.cs:223-255`).
12. `RTrimNull`/`ReadStringU` создают промежуточные строки и `Encoding.Unicode.GetString` на каждый ридинг строки (`Memory.cs:80-98,486-490`).
13. Диагностические `DebugInformation` считаются даже когда UI-оверлей скрыт (`ForeGroundTime` gate только частично) (`Core.cs:405-506`).
14. `SpinWait.SpinUntil(...,500)` в `ParallelUpdate` блокирует main-поток корутин до 500 мс при зависшем джобе (`Runner.cs:241`).
15. `ChooseSingleProcess`/`FindPoeProcess` вызывают `Process.GetProcessesByName` дважды (Regular+Korean) на каждый ретрай `MainControl` (`Core.cs:529-541`).
16. Повторные `GetCustomAttribute` в `SettingsParser` — заменить на один проход атрибутов (`SettingsParser.cs:41-48`).
17. Отсутствие `ArrayPool`/переиспользования буферов для `ReadMem` — постоянные аллокации `new byte[size]` (`Memory.cs:112`).
18. `Coroutine` `WaitTime.GetEnumerator` крутит `yield return null` в цикле опроса `Stopwatch` вместо событийного ожидания (`Coroutine.cs:289-299`).
19. `LatancyCache`/`TimeCache` держат отдельные `Stopwatch` — общий тайм-сорс снизил бы накладные (`Cache/TimeCache.cs`, `LatancyCache.cs`).
20. `MoreLinq.Batch`/`JM.LinqFaster` в hot-path создают итераторы каждый кадр (`PluginManager.cs:451`).

### Security (14)
1. Плагины грузятся/компилируются с полным доверием и `AllowUnsafe`, без песочницы (`PluginManager.cs`, `RoslynCompiler.cs`).
2. `BuildReferences` даёт плагинам ссылки на все TPA и произвольные `libs/*.dll` — нет allowlist (`RoslynCompiler.cs:158-180`).
3. Чтение памяти чужого процесса + синтетический ввод — детектируемо анти-читом; риск не задокументирован (`Memory.cs`, `Input.cs`).
4. `AskToKillOtherRunningProcesses` матчит по имени процесса и `Kill()` — может убить неродственные процессы с тем же именем (`Program.cs:24-50`).
5. Newtonsoft-десериализация настроек: при включении `TypeNameHandling` в будущем — вектор RCE; зафиксировать запрет (`SettingsContainer.cs:30-35`).
6. Hot-reload грузит DLL по событию файловой системы из каталога плагинов без проверки целостности/подписи (`PluginManager.cs:313-378`).
7. `deps/ImGui.NET.dll` и `cimgui.dll` — бинарники в репозитории без checksum/подписи/провенанса (`deps/`).
8. `ReceivePluginEvent` рассылает произвольные `object args` всем плагинам — нет изоляции данных между плагинами (`PluginManager.cs:483-490`).
9. `Errors.txt` пишется в каталог плагина как контроль повторной компиляции — плагин может им манипулировать, чтобы обойти пересборку (`PluginManager.cs:207,388`).
10. Логи (`Logs/`, `config/*.json`) могут содержать пути/идентификаторы; нет ротации-очистки чувствительных данных.
11. `SetForegroundWindow`/принудительный фокус (`LostFocus`) — потенциальная помеха вводу пользователя, нет opt-out (`Core.cs:331-335`).
12. Отсутствует проверка, что подключаемся именно к клиенту PoE (совпадение по имени exe), а не к подделке процесса (`Core.cs:531-534`).
13. Нет ограничения размера/таймаута компиляции плагина — вредоносный исходник может подвесить старт (`RoslynCompiler.Compile`).
14. `File.Copy(SETTINGS_FILE_NAME, dumpSettings.json, true)` перезаписывает бэкап без атомарности — гонка при параллельных сохранениях (`SettingsContainer.cs:94`).

### Tests & CI (16)
1. Добавить GitHub Actions: сборка `Loader/Loader.csproj` на `windows-latest` (`.github/workflows/build.yml`).
2. Юнит-тесты `RoslynCompiler.Compile` (успех/ошибки/битые пути) — не требуют игры.
3. Тесты кэшей `FrameCache`/`TimeCache`/`ConditionalCache` (политики обновления, `ForceUpdate`).
4. Тесты `Memory.CompareData`/`FindPatterns` на синтетическом буфере с известными паттернами.
5. Тесты `SettingsParser` на разные типы `Node` (в т.ч. регресс `RangeNode<long>`).
6. Тесты `TypeConverter` (`Core/Shared/SomeMagic/TypeConverter.cs`) для всех веток типов.
7. Регресс-тест на цикл удаления в `Runner.Update` (порядок/полнота обхода).
8. Тест сериализации/десериализации настроек с кастомными конвертерами (`ColorNode`/`ToggleNode`/`FileNode`).
9. Тесты `MathHepler` (рандомайзер, генерация слов, гео-хелперы).
10. Smoke-тест `PluginManager.SearchPlugins` на временном каталоге с фейковыми плагинами.
11. Ввести `dotnet format --verify-no-changes` в CI (есть `.editorconfig`).
12. Прогонять `dotnet build -warnaserror` для новых файлов (после чистки предупреждений).
13. Добавить бейдж статуса сборки в README.
14. Публиковать артефакт сборки (`PoeHelper/`) из CI для ручного smoke-теста на Windows.
15. Тесты на потокобезопасность `MultiThreadManager` (стресс-раздача джобов) под `dotnet test`.
16. Snapshot-тест на неизменность публичного API `ExileCore.dll` (плагины зависят от него) — например, через PublicApiAnalyzer.

### Developer experience / tooling (16)
1. Починить `.sln`: отвязать gitignore-нутые плагины (solution filter `.slnf`) (`ExileApi.sln`).
2. Не игнорировать примеры плагинов целиком (`.gitignore: Plugins/`) — хотя бы `Plugins/Source/Examples/`.
3. Убрать дубликат `textures/` (оставить `Core/textures/`).
4. Заменить legacy-шаблон `.gitignore` (шапка про PowerShell/wget) на `dotnet new gitignore`.
5. Добавить `CONTRIBUTING.md` (стиль, ветки, как писать плагин) — есть `proposals/PluginTemplate`.
6. Добавить `.editorconfig`-правила для nullability/analyzers по мере включения.
7. Ввести `Directory.Build.props` общий TFM/Platform/OutputPath (сейчас продублировано в 3 csproj).
8. Скрипт `build.ps1`/`justfile` для сборки и раскладки в `PoeHelper/`.
9. Добавить `global.json` с фиксированным SDK.
10. Вынести `deps` в подключаемый NuGet `ImGui.NET` вместо ручных DLL (или задокументировать точную версию).
11. Шаблон issue/PR (`.github/ISSUE_TEMPLATE`).
12. `EditorConfig`-совместимый анализатор именований, чтобы поймать `MathHepler` и `ReadDoublePtr...` опечатки.
13. README-раздел «Как отлаживать оффсеты» со ссылкой на `DevTree`/`docs/offsets.md`.
14. Скрипт валидации оффсетов против reference-дампа (перекликается с `docs/offsets.md`).
15. Devcontainer/`Directory.Build.rsp` для единообразия LangVersion между Core и рантайм-компиляцией плагинов.
16. Пометить `proposals/Compat` как отдельный NuGet/док, чтобы авторы плагинов знали о шиме совместимости.

### Architecture / refactor (18)
1. `Loader` дергает `Core` через reflection несмотря на `ProjectReference` — заменить прямыми вызовами (`Loader.cs`).
2. Разбить `Core.cs` (650 строк): окно/атач/FPS/тик плагинов/драйв корутин — разные ответственности.
3. Переименовать `MathHepler` → `MathHelper` (публичный API) (`Core/Shared/Helpers/MathHepler.cs`).
4. `Input` — статический god-класс (стейт клавиш + синтетический ввод + сглаживание); выделить интерфейс для тестируемости.
5. Консолидировать перекрывающиеся кэши: `CachedValue`-иерархия + `FramesCache`/`StaticCache`/`StaticStringCache` (`Core/Shared/Cache/`).
6. `IMemory` содержит методы, бросающие `NotImplementedException` — вынести/удалить мёртвую поверхность (`Memory.cs:227-271`).
7. Ввести `CancellationToken`-модель останова воркеров вместо `Thread.Abort` (`MultiThreadManager.cs`).
8. Заменить самописный пул потоков на `Channel<Job>` + `Task`/`ThreadPool` где возможно.
9. Ввести абстракцию `IProcessMemory` для мока памяти в тестах (сейчас `Memory` жёстко завязан на `ProcessMemoryUtilities`).
10. Отделить рендер-очередь (`RenderQ`) от `Graphics`-фасада чёткой границей.
11. `Coroutine`/`Runner`/`YieldBase` — большой самописный планировщик; рассмотреть `async`/`IAsyncEnumerable`.
12. Вынести создание оффсетов из reflection-команды `loader_offsets` в типизированный сервис (`Loader.CreateOffsets`).
13. Убрать двусмысленность `Name`/`InternalName`/`DirectoryName` в модели плагина (единая идентификация).
14. Ввести DI-контейнер вместо ручной передачи `GameController`/`Graphics`/`PluginManager` в каждый враппер.
15. Слить дублирующую логику `ReadNativeString` (`Memory.cs`) и `NativeStringReader` в один тип.
16. Выделить константы задержек ввода (`ACTION_DELAY` и т.п.) в конфиг, а не `const` в `Input`.
17. `SettingsHolder.Draw` смешивает layout-логику и ImGui-вызовы — вынести компоновку.
18. Единый тип результата чтения памяти (`ReadResult<T>` с флагом успеха) вместо «молчаливого default».

### API / interfaces (14)
1. Удалить/реализовать `IMemory.ReadSecondPointerArray_Count` и `Read<T>(Pointer,...)` (`throw NotImplementedException`) (`Memory.cs:227-271`).
2. `IPlugin.OnPluginSelectedInMenu()` помечен `//TODO: Implement me` — определить контракт (`IPlugin.cs:29`).
3. Устранить рассинхрон `Save`(InternalName)/`Load`(Name) в публичном контракте настроек (`SettingsContainer.cs`).
4. Схлопнуть дублирующие перегрузки `ReadBytes(long,long)`/`ReadBytes(long,int)`/`ReadMem` (`Memory.cs:124-134`).
5. Сделать `CachedValue.TotalCount/LifeCount` не публичными изменяемыми полями (инкапсулировать) (`CachedValue.cs:13-16`).
6. `Core.SyncLocker` как публичное статическое поле — заменить приватной синхронизацией (`Core.cs:34`).
7. Возвращать `IReadOnlyList<T>` из read-методов памяти вместо `List<T>` (защита от мутаций вызывающим).
8. Пометить `[Obsolete]` legacy-методы ввода после ввода `SendInput`-варианта.
9. Явные nullable-аннотации в сигнатурах `GetObject<T>`/`GetComponent<T>` (возвращают null).
10. Документировать единицы измерения/системы координат в публичных API (см. `docs/coordinates.md`) прямо в XML-doc.
11. `Runner.Run` бросает `NullReferenceException` вместо `ArgumentNullException` при null-аргументе (`Runner.cs:64,89`).
12. Ввести версионирование публичного API `ExileCore` (SemVer) для стабильности плагинов.
13. `ISettings`/`ISettingsHolder` — задокументировать обязательный `Enable` и правила `[Menu]`-атрибутов.
14. Стандартизировать событийную модель плагинов (`ReceiveEvent(string,object)`) типобезопасными payload-ами.

### Error handling & logging (14)
1. Свести `Console.WriteLine` (Runner, SettingsContainer) к Serilog (`Runner.cs:160-170`, `SettingsContainer.cs:82,100`).
2. Не глотать `catch { }` в `RoslynCompiler.BuildReferences` — логировать (`RoslynCompiler.cs:167-168`).
3. Убрать диагностический `"WTF {type}"` из `MultiThreadManager.Process` (`:158`).
4. `ReadMem` не должен `throw;` — вернуть пустой буфер по контракту (`Memory.cs:117-121`).
5. Обернуть `EnterWriteLock` в `try/finally` во всех сохранениях настроек (`SettingsContainer.cs`).
6. Проверять и логировать неуспех `ReadProcessMemory` (дрейф оффсетов) (`Memory.cs:283`).
7. `Core.Tick`/`Render` ловят всё в общий `catch` — детализировать и не терять stack trace (`Core.cs:508-511`).
8. Логировать таймауты чтения (`ReadList`, `ReadPointersArray`) с адресом/типом единообразно.
9. `ParallelCoroutineManualThread` при падении фонового потока — не `throw;`, а перезапуск с логом (`Core.cs:581-585`).
10. Единый уровень серьёзности сообщений в `DebugWindow.LogMsg(...,level,...)` — сейчас магические числа (2/5/6/7/10).
11. Логировать причину пропуска плагина (`Errors.txt` найден) в файл-лог, а не только оверлей (`PluginManager.cs:391`).
12. Показывать пользователю понятную ошибку при провале pattern-scan вместо тихого `default`-старта.
13. Ввести структурные логи Serilog (`{PluginName}`, `{ElapsedMs}`) вместо интерполяции строк.
14. Не терять первоначальное исключение в `Loader.LogLoaderError` при падении fallback-записи (`Loader.cs:170-183`).

### Documentation (16)
1. Добавить `LICENSE` и прояснить статус форка PoeHud.
2. Переписать шапку README (два H1, «i didn't test that a lot time»).
3. Исправить команду сборки в README: собирать `Loader/Loader.csproj`, а `.sln` починить (см. Build).
4. Добавить секцию про анти-чит/ToS-риски.
5. `CONTRIBUTING.md` с гайдом по написанию плагина (ссылка на `proposals/PluginTemplate`).
6. Задокументировать процедуру обновления оффсетов после патча (`docs/offsets.md` расширить `TerrainData`-кейсом).
7. Описать модель потоков (main render / parallel coroutine / worker pool) в `docs/architecture.md`.
8. Документировать формат и расположение `config/*.json` и профилей.
9. CHANGELOG.md + политика версионирования `ExileCore.dll`.
10. Пометить `TODO/FIXME` в `GameOffsets` (`IngameUElementsOffsets.cs:37-38`, `ServerDataOffsets.cs:35`) как известные ограничения в доках.
11. Гайд по отладке производительности (`CollectDebugInformation`, `DebugInformations`) — есть богатая телеметрия, но нет описания.
12. Документировать зависимость от `deps/ImGui.NET.dll`+`cimgui.dll` и их точные версии.
13. README-таблица «поддерживаемая версия PoE / соответствующие оффсеты».
14. Дополнить XML-doc в файлах без него (`Runner.cs`, `Coroutine.cs`, часть `SettingsContainer`).
15. Проверить корректность опечаток в доках («stashoverflow» в `SettingsParser.cs:270`).
16. Пример «hello-world» плагина шаг-за-шагом на базе `proposals/PluginTemplate` + скриншот.

### Dependencies & maintenance (14)
1. Обновить `Serilog` c 2.8.0 до 3.x/4.x (`Core.csproj`, `Loader.csproj`).
2. Убрать deprecated `Serilog.Sinks.RollingFile 3.3.0`, роллинг делать через `Serilog.Sinks.File`.
3. Аудит/обновление `Antlr4.Runtime 4.6.6` (используется через `PoeFilterParser`).
4. Проверить актуальность `PoeFilterParser 1.0.44`, `ProcessMemoryUtilities.Net 1.2.0`.
5. SharpDX 4.2.0 заброшен — задокументировать план миграции (Vortice.Windows/Silk.NET) как долг.
6. Оценить необходимость `JM.LinqFaster 1.0.0` (микро-оптимизации LINQ) — возможно, удалить.
7. Проверить, используется ли `System.Runtime.Caching 8.0.0`, иначе удалить (`Core.csproj`).
8. `morelinq 3.2.0` — обновить и заменить `Batch` на встроенный `Chunk` (.NET 6+).
9. Ввести `Directory.Packages.props` (Central Package Management).
10. Включить `dotnet list package --vulnerable`/Dependabot в CI.
11. Зафиксировать версии транзитивных нативных зависимостей (cimgui) в доке/checksum.
12. Пересмотреть `NoWarn` (`SYSLIB0011` = BinaryFormatter) — устранить причину, а не подавлять (`Core.csproj`).
13. `Newtonsoft.Json 13.0.3` — рассмотреть переход на `System.Text.Json` для настроек.
14. Проверить лицензии всех NuGet-пакетов на совместимость с лицензией проекта.

### Config / build (16)
1. Починить `ExileApi.sln` (ссылки на отсутствующие/gitignored плагины ломают сборку).
2. Ввести `Directory.Packages.props` для версий пакетов.
3. Перенести общие свойства (TFM, Platform, `OutputPath`, `Nullable`) в `Directory.Build.props`.
4. Добавить `global.json` с фиксацией .NET 10 SDK.
5. Убрать бессмысленный `AnyCPU` из `Platforms=AnyCPU;x64` (проект x64-only) во всех csproj.
6. Устранить причину `SYSLIB0011` вместо `NoWarn` (BinaryFormatter) (`Core.csproj`).
7. Инкрементально включить `Nullable` (сначала annotations) во всех проектах.
8. Задать `<Version>`/`<InformationalVersion>` (сейчас `GenerateAssemblyInfo=false` без версии).
9. Сделать `OutputPath` конфигурируемым (свойство/переменная), а не жёсткий `..\..\PoeHelper\`.
10. Единый LangVersion для рантайм-компиляции плагинов (`RoslynCompiler` использует `LanguageVersion.Latest`) и Core.
11. Добавить `.slnf` (solution filter) для сборки только core-проектов.
12. Настроить `TreatWarningsAsErrors` для core-проектов после чистки предупреждений.
13. Проверить, что `Content Include="textures\**"` не тянет корневые дубли (см. DX).
14. Включить `ContinuousIntegrationBuild=true` в CI для детерминированных PDB.
15. Пометить нативные `cimgui.dll`/`deps` с `runtime`-идентификатором (`win-x64`).
16. Добавить `.gitattributes` (нормализация EOL, `*.dll binary`) для бинарников в `deps/textures`.

### Offsets / game-data robustness (6)
1. Автоматизировать проверку `TerrainData` внутри `IngameDataOffsets` (README отмечает ручную верификацию) (`GameOffsets/TerrainData.cs`).
2. Пометить/починить `WorldMap` и `MapTabWindowStartPtr` с `FieldOffset(0x0)` и `//TOFO: Fixme. Cause reading errors` (`IngameUElementsOffsets.cs:37-38`).
3. Проверить устаревший комментарий `//TODO: 3.8.1 fix me` у `FreePassiveSkillPointsLeft` (`ServerDataOffsets.cs:35`).
4. Добавить sanity-проверки диапазонов (`size`, `count`) во все `Read*Array`-методы централизованно.
5. Ввести версионированный набор оффсетов (Regular/Korean уже есть) с явной привязкой к билду PoE (`Core/PoEMemory/Offsets.cs`).
6. Логировать при старте резюме pattern-scan (что найдено/не найдено), чтобы быстро видеть поломку после патча (`Memory.FindPatterns`).

### UX / features (8)
1. Диалог выбора процесса `ChooseSingleProcess` показывает только 2 клиента — поддержать N (`Core.cs:514-527`).
2. Дать opt-out для принудительного возврата фокуса окну игры (`LostFocus`) (`Core.cs:331-335`).
3. Кнопка «reload plugin» в меню вместо только file-watch hot-reload (`PluginManager.HotReloadDll`).
4. Показывать в UI причину, по которой плагин пропущен (`Errors.txt`), с содержимым ошибок.
5. Индикатор здоровья pattern-scan/оффсетов в оверлее (зелёный/красный).
6. Настройка каталога вывода логов/конфигов из UI.
7. Профили настроек: UI переключения профилей уже есть — добавить импорт/экспорт профиля.
8. Тултипы с единицами координат/грид-позиций в отладочных оверлеях (`DevTree`).
