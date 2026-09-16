# `IngameData` — сырые замеры

> Снято 2026‑09‑15 и 2026‑09‑16 против живого клиента `PathOfExile_KG`:
> три зоны на одном запуске игры и четвёртая **после перезапуска клиента**.
> Документ существует ради одной вещи: **утверждение «воспроизвелось в стольких‑то зонах» должно
> быть проверяемым**. Без этой таблицы оно живёт только в прозе комментариев, и проверить его
> нельзя даже автору.
>
> Повторить всё одной командой: `bash tools/measure-ingamedata.sh <префикс> 0x2000`
> (при запущенной игре, персонаж в загруженной зоне).

## Как читать таблицы

В зонах A–C `IngameState` не менял адрес — `0x44C17002C10`; игра не перезапускалась.
Зона D снята уже после перезапуска, и там всё адресное пространство другое.
`Data` переезжает при каждой смене зоны, и именно поэтому смена зоны работает как независимая
проверка: адрес другой, значения полей другие, **смещение обязано остаться тем же**.

Столбец «истинный адрес» — это то, что сообщил эталонный дистрибутив (`tools/RefLive`),
столбец «смещение» — то, что нашёл `tools/FindOffset` внутри объекта.

## Зона A — уровень 67, хэш `0x557A38DC`

    Data = 0x44C6D91E000

| поле | смещение | чем установлено |
|---|---|---|
| `IngameState.Data` | `+0x218` | единственное совпадение в окне `0x2000` |
| `CurrentAreaLevel` | `+0xD4` | значение 67, единственное совпадение |
| `CurrentAreaHash` | `+0x114` | значение `0x557A38DC`, единственное совпадение |

## Зона B — уровень 60, хэш `0xDD5680DD`

    Data = 0x44D05EAA000

| поле | истинный адрес / значение | смещение |
|---|---|---|
| `IngameState.Data` | `0x44D05EAA000` | `+0x218` |
| `CurrentArea` | `0x44C34C007F2` | `+0xB0` |
| `CurrentAreaLevel` | 60 | `+0xD4` |
| `CurrentAreaHash` | `0xDD5680DD` | `+0x114` |
| `ServerData` | `0x44D36530000` | `+0x968` |
| `LocalPlayer` | `0x44D31D93F00` | `+0x970` |
| `EntityList` | `0x44D0426E3D0` | `+0xA28` |
| `EntitiesCount` | 151 | `+0xA30` |
| `SleepingEntityList` | `0x44D04264B00` | `+0xA38` |
| `Terrain.TgtArray.First` | `0x44D0DF0A010` | `+0xC18` |
| `Terrain.TileIndexes.First` | `0x44D49B40E10` | `+0xC40` |
| `Terrain.TileDescriptions.First` | `0x44D497F54E0` | `+0xC58` |
| `Terrain.LayerMelee.First` | `0x44D50160010` | `+0xCC0` |
| `Terrain.LayerRanged.First` | `0x44D49B60010` | `+0xCD8` |

Скаляры terrain в этой зоне: `NumCols` 31, `NumRows` 31, `NumTileIndexCols` 32,
`NumTileIndexRows` 32, `BytesPerRow` 357, `TileHeightMultiplier` 1.
Сходимость: размер мели‑слоя 254 541 = 357 × 713, и 713 = 31 × 23.

Единственное неоднозначное место замера: уровень зоны 60 (`0x3C`) нашёлся дважды — по `+0xD4`
и по `+0x5BB`. Второе не выровнено ни на что и является случайным байтом; в зоне C с уровнем 83
осталось одно совпадение.

## Зона C — уровень 83, хэш `0xFC4844AC` («Estuary», акт 11)

    Data = 0x44D362AC000

| поле | истинный адрес / значение | смещение | окно |
|---|---|---|---|
| `IngameState.Data` | `0x44D362AC000` | `+0x218` | 1 совпадение и в `0x10000` |
| `IngameState.Camera` | `0x44C0C561A00` | `+0x270` | 1 совпадение в `0x10000` |
| `IngameState.ServerData` | `0x44D36510000` | **не найдено** | `0x2000` — его там нет |
| `CurrentArea` | `0x44C34BBFAFC` | `+0xB0` | 1 |
| `CurrentAreaLevel` | 83 | `+0xD4` | 1 в `0x2000`, 7 в `0x10000` |
| `CurrentAreaHash` | `0xFC4844AC` | `+0x114` | 1 |
| `MapStats` | 11 пар, 88 байт | `+0x128` | опознан содержимым |
| `ServerData` | `0x44D36510000` | `+0x968` | 1 |
| `LocalPlayer` | `0x44D30312C80` | `+0x970` | **2 в `0x2000`**: `+0x970` и `+0x10E8` |
| `EntityList` | `0x44D31A4E310` | `+0xA28` | 1 |
| `EntitiesCount` | 77 | `+0xA30` | 1 |
| `SleepingEntityList` | `0x44D31A4D710` | `+0xA38` | 1 |
| `SleepingEntityCount` | 409 | `+0xA40` | 1 |
| `EnvironmentData` | `0x44C644E1800` | `+0x1110` | 1 |
| `Terrain.TgtArray.First` | `0x44C64AE0010` | `+0xC18` | 1 |
| `Terrain.TileIndexes.First` | `0x44C2C158010` | `+0xC40` | 1 |
| `Terrain.TileDescriptions.First` | `0x44C64F39910` | `+0xC58` | 1 |
| `Terrain.LayerMelee.First` | `0x44D62310010` | `+0xCC0` | 1 |
| `Terrain.LayerRanged.First` | `0x44D62510010` | `+0xCD8` | 1 |

Скаляры terrain: `NumCols` 87, `NumRows` 81, `NumTileIndexCols` 88, `NumTileIndexRows` 82,
`BytesPerRow` 1001, `TileHeightMultiplier` 1.
Сходимость: размер мели‑слоя 1 864 863 = 1001 × 1863, и 1863 = 81 × 23.
Ширина: 1001 × 2 = 2002 против 87 × 23 = 2001 — последняя колонка паддинг, строка хранится
целыми байтами.

### `MapStats`, опознание содержимым

Эталон назвал 11 пар; по адресу из `+0x128` лежат ровно они, в том же порядке, и массив
кончается ровно после них:

    0x0000006D000009C5   MapHiddenMonsterLifePctFinal      = 109
    0x0000000F000009C6   MapHiddenMonsterDamagePctFinal    = 15
    0x000000DE000009E2   MapAreaPortalVariation            = 222
    0x00001B10000022B8   IsCapturableMonster               = 6928
    0x00000001000022C1   MapNumBetrayalFortTerrainFailures = 1
    0x00000010000027B0   MinionsAreDefensive               = 16
    0x0000001600003F9F   MapSpawnAtlasAllies               = 22
    0x000000010000505B   MtxCrabUniqueItemPickup           = 1
    (ещё три пары: 0x5832, 0x58D6, 0x58F6)


## Зона D — уровень 33, хэш `0xE537D5DF` (Лабиринт, «Estate Path» / `1_Labyrinth_OH_straight`)

**Снято после полного перезапуска клиента.** Это и есть проверка на ASLR:

    TheGame      0x44C0C092E80  ->  0x5F4F6093300
    IngameState  0x44C17002C10  ->  0x5F4FC562810
    Data                            0x5F5E7867800

Адресное пространство другое целиком — **все смещения вышли те же**:

| поле | истинный адрес / значение | смещение | замечание |
|---|---|---|---|
| `IngameState.Data` | `0x5F5E7867800` | `+0x218` | 1 совпадение |
| `IngameState.Camera` | `0x5F4F6562800` | `+0x270` | 1 совпадение |
| `IngameState.ServerData` | `0x5F4F6630000` | **не найдено** | в `IngameState` его нет и после перезапуска |
| `CurrentArea` | `0x5F51EB44B18` | `+0xB0` | 1 |
| `CurrentAreaLevel` | 33 | `+0xD4` | **4 совпадения**: `+0xD4 +0x11C +0xD50 +0xD70` — байт `0x21` слишком частый |
| `CurrentAreaHash` | `0xE537D5DF` | `+0x114` | 1 |
| `MapStats` | 1 пара, 8 байт | `+0x128` | опознан содержимым второй раз |
| `LabDataPtr` | `0x5F61DC0E180` | `+0x48` | см. ниже |
| `ServerData` | `0x5F4F6630000` | `+0x968` | 1 |
| `LocalPlayer` | `0x5F61DF9C900` | `+0x970` | опять 2: `+0x970` и `+0x10E8` |
| `EntityList` | `0x5F61DC03BF0` | `+0xA28` | 1 |
| `EntitiesCount` | 54 | `+0xA30` | 1 |
| `SleepingEntityList` | `0x5F61DC03EF0` | `+0xA38` | 1 |
| `SleepingEntityCount` | 811 | `+0xA40` | 1 |
| `EnvironmentData` | `0x5F52785D000` | `+0x1110` | 1 |
| `Terrain.TgtArray.First` | `0x5F5276E0010` | `+0xC18` | 1 |
| `Terrain.TileIndexes.First` | `0x5F52795C010` | `+0xC40` | 1 |
| `Terrain.TileDescriptions.First` | `0x5F527655A70` | `+0xC58` | 1 |
| `Terrain.LayerMelee.First` | `0x5F527B50010` | `+0xCC0` | 1 |
| `Terrain.LayerRanged.First` | `0x5F527D50010` | `+0xCD8` | 1 |

Скаляры terrain: `NumCols` 85, `NumRows` 73, `BytesPerRow` 978.
Сходимость: 1 642 062 = 978 × 1679, и 1679 = 73 × 23.

`MapStats` в Лабиринте — ровно одна пара, и она лабиринтовая:
`0x0000000000001931` = `LabyrinthDarkshrineDivineFontGrantsOneAdditionalEnchantmentUseToPlayerX`.
Тройка по `+0x128`: `First 0x5F4F62351D8`, `Last 0x5F4F62351E0` — размах 8 байт, ровно одна пара,
как и сказал эталон.

### `LabDataPtr` — замерен ТАМ, ГДЕ ОРАКУЛ МОЛЧИТ

Эталон и в Лабиринте отдаёт `Data.LabyrinthData = 0x0`. То есть истинного адреса спросить не у
кого: **эталон верен для тех полей, которыми пользуются его потребители, и это поле не из них.**
Поэтому замер шёл иначе.

1. **Разностью.** Голова объекта вне Лабиринта — длинный ряд нулей (`0x30`..`0x88`). В Лабиринте
   ровно один qword из этого ряда, `+0x48`, стал указателем в кучу: `0x5F61DC0E180`.
2. **Содержимым.** На этот адрес натравлен ПАРСЕР ЭТАЛОНА
   (`RefLive.exe --as LabyrinthData 0x5F61DC0E180`), и он вычитал связный расклад Лабиринта:

       [0] Secret1: Trinket, Secret2: Gauntlet, LinkedWith: ...F860, Section: Entry_1_Simple EntranceStraight
       [1] Secret1: ToggleStatuesNormal1,        LinkedWith: ...F800, ...F8C0, Section: Entry_1_Simple Boss1
       [2] Secret1: Trinket, Secret2: Payload,   LinkedWith: ...F860, ...F920, ...F980, Section: Middle_1_SimpleN
       [5] Secret1: Gauntlet, Secret2: Hidden,   Section: End_1_Modal ElegantBlockBranch
       [7] Secret1: SilverKey, Secret2: SilverDoorReward, Section: End_1_Modal IndoorBranchBottleneck2
       Rooms: элементов 10

   Ссылки `LinkedWith` сходятся между комнатами в обе стороны, имена секций и секретов осмысленны.
3. **Контролем.** Тот же парсер по адресу `ServerData` и по адресу `EntityList` даёт `Rooms: 0`.
   Значит десять комнат — не то, что парсер выдумывает из любой памяти.

Старое число было `0x11C` — не выровнено на 8 (все замеренные выровнены) и содержало
`0x92CFB39000000021`. Проверка «не ноль» его пропускала, и `LabyrinthData` строился из мусора.

## Что эти замеры НЕ показывают

1. **Не покрыты состояния**: город/убежище, загрузочный экран, выбор персонажа, смерть.
   Одно из них закрывает конкретный вопрос: загрузочный экран или выбор персонажа разводит
   `+0x970` и `+0x10E8` — в состоянии, где игрока нет, они должны разойтись.
2. **Метод обычно доказывает «форк читает то же, что эталон»**, а не «это поля C++-структуры
   клиента»: `RefLive` исполняет код эталона против того же процесса. Исключение —
   `LabDataPtr`: там эталон молчит, и число получено разностью и содержимым, то есть без него.
3. **Эталон не всеведущ.** Его `LabyrinthData` в Лабиринте — `null`. Пустой ответ оракула больше
   не считается свидетельством об отсутствии объекта: то же самое он говорит про `IngameUi` и
   `UIRoot` при живом HUD.
4. **`parity.sh` тут ни при чём.** Он считает ИМЕНА эталонной поверхности и не проверяет ни одного
   числа; ссылаться на него как на подтверждение раскладки нельзя.
