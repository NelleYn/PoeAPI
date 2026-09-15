#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────────────────────────
# measure-ingamedata.sh — согласованный замер раскладки IngameData на ЖИВОЙ игре.
#
# Зачем скрипт, а не три команды руками. В игру играют: между двумя соседними командами меняется
# зона, и адрес, полученный первой командой, начинает указывать в память другой зоны. Это не
# теория — дамп окна по адресу, снятому минутой раньше, однажды вышел целиком нулевым.
# Поэтому замер обрамлён оракулом с обеих сторон, и если зона (или база Data) между ними
# сменилась, результат объявляется НЕДЕЙСТВИТЕЛЬНЫМ, а не печатается как число.
#
# Что делает: спрашивает истинные адреса у эталона (RefLive), ищет каждый внутри объекта
# (FindOffset), снимает полный дамп окна и повторяет вопрос эталону.
#
# Использование:
#   bash tools/measure-ingamedata.sh <префикс_для_файлов> [окно=0x2000]
# Оставляет рядом с префиксом: .before .after (вывод оракула) и .dump (дамп окна).
#
# Коды возврата: 0 — замер действителен; 2 — читать нечего (игра не запущена, оракул молчит);
#                3 — состояние сменилось посередине, повторить.
# ─────────────────────────────────────────────────────────────────────────────────────────────────
set -u

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
RL="$ROOT/tools/RefLive/bin/Release/net10.0-windows/RefLive.exe"
FO="$ROOT/tools/FindOffset/bin/Release/net10.0-windows/FindOffset.exe"

OUT="${1:-}"
WIN="${2:-0x2000}"

if [ -z "$OUT" ]; then
    echo "нужен префикс для файлов: bash tools/measure-ingamedata.sh <префикс> [окно]"
    exit 2
fi

for exe in "$RL" "$FO"; do
    [ -x "$exe" ] || { echo "нет инструмента: $exe — собери его (dotnet build -c Release)"; exit 2; }
done

"$RL" > "$OUT.before" 2>&1
grep -q "IngameState.Data" "$OUT.before" || {
    echo "оракул не отдал Data — игра не запущена или персонаж не в зоне:"
    tail -4 "$OUT.before"
    exit 2
}

DATA=$(grep -m1 "IngameState.Data"       "$OUT.before" | grep -o "0x[0-9A-F]*")
IGS=$(grep  -m1 "^  IngameState  "       "$OUT.before" | grep -o "0x[0-9A-F]*")
HASH=$(grep -m1 "хэш зоны"               "$OUT.before" | grep -o "0x[0-9A-F]*")
LEVEL=$(grep -m1 "уровень зоны"          "$OUT.before" | awk '{print $NF}')

echo "состояние: зона $HASH, уровень $LEVEL, IngameState $IGS, Data $DATA, окно $WIN"
echo

# Печатает строку «искали — где нашли». Несколько совпадений — это ответ «смещение неоднозначно»,
# и он печатается как есть: выбирать из них первое было бы угадыванием.
find_at() { # имя  значение  начало_окна  размер  [окно]
    local name="$1" val="$2" base="$3" size="${4:-8}" win="${5:-$WIN}" out n hits
    if [ -z "${val:-}" ] || [ "$val" = "0x0" ]; then
        printf '  %-28s %-16s ПУСТО — искать нечего\n' "$name" "${val:-—}"
        return
    fi
    out=$("$FO" --find --in "$base" --len "$win" --value "$val" --size "$size" 2>&1)
    n=$(echo "$out" | grep -m1 "совпадений:" | grep -oE "[0-9]+$")
    hits=$(echo "$out" | grep -oE "^  \+0x[0-9A-F]+" | tr -d ' ' | tr '\n' ' ')
    if [ -z "${n:-}" ]; then
        printf '  %-28s %-16s НЕ НАЙДЕНО в окне %s\n' "$name" "$val" "$win"
    else
        printf '  %-28s %-16s совпадений=%-3s %s\n' "$name" "$val" "$n" "$hits"
    fi
}

echo "── внутри IngameState ──────────────────────────────────────────────────────────────────"
find_at "Data" "$DATA" "$IGS" 8
for m in ServerData Camera IngameUi UIRoot; do
    v=$(grep -m1 "^  IngameState\.$m " "$OUT.before" | grep -o "0x[0-9A-F]*")
    find_at "$m" "$v" "$IGS" 8
done

echo
echo "── подобъекты внутри IngameData ────────────────────────────────────────────────────────"
while read -r name val; do
    find_at "$name" "$val" "$DATA" 8
done < <(grep -oE "^  Data\.[A-Za-z]+ +0x[0-9A-F]+" "$OUT.before" | sed 's/^  //' | awk '{print $1, $2}')

echo
echo "── значения внутри IngameData ──────────────────────────────────────────────────────────"
# Счётчики сущностей ЛЕТУЧИЕ: они меняются несколько раз в секунду, и «НЕ НАЙДЕНО» здесь — это
# нормальный ответ, а не опровержение поля. Такие поля подтверждаются дампом окна ниже.
for m in CurrentAreaLevel:4 CurrentAreaHash:4 EntitiesCount:8 SleepingEntityCount:8; do
    name="${m%%:*}"; size="${m##*:}"
    v=$(grep -m1 "Data\.$name " "$OUT.before" | grep -oE "0x[0-9A-F]+")
    find_at "$name($size)" "$v" "$DATA" "$size"
done

echo
echo "── поля TerrainData (по первому указателю каждого массива) ─────────────────────────────"
for f in TgtArray TileIndexes TileDescriptions LayerMelee LayerRanged; do
    v=$(grep -m1 "\.$f " "$OUT.before" | grep -oE "[0-9A-F]{9,} - " | head -1 | sed 's/ - //')
    [ -n "${v:-}" ] && find_at "Terrain.$f.First" "0x$v" "$DATA" 8
done

echo
echo "── массив MapStats (искать можно только В КАРТЕ) ───────────────────────────────────────"
PAIRS=$(grep -m1 "Data.MapStats " "$OUT.before" | grep -oE "пар: [0-9]+" | grep -oE "[0-9]+")
echo "  пар у эталона: ${PAIRS:-?} (это ${PAIRS:-0} * 8 байт в массиве)"
echo "  поле опознаётся тройкой First/Last/End нужного размера в дампе, а затем СОДЕРЖИМЫМ:"
grep -m3 "сырая пара" "$OUT.before" | sed 's/^/  /'

echo
"$FO" --window --in "$DATA" --len "$WIN" > "$OUT.dump" 2>&1
echo "дамп окна: $OUT.dump ($(grep -cE '^  \+0x' "$OUT.dump") строк)"

"$RL" > "$OUT.after" 2>&1
DATA2=$(grep -m1 "IngameState.Data" "$OUT.after" | grep -o "0x[0-9A-F]*")
HASH2=$(grep -m1 "хэш зоны"         "$OUT.after" | grep -o "0x[0-9A-F]*")

echo
if [ "$DATA" = "$DATA2" ] && [ "$HASH" = "$HASH2" ]; then
    echo "ЗАМЕР ДЕЙСТВИТЕЛЕН: зона и база Data не менялись ($HASH, $DATA)"
    exit 0
fi

echo "ЗАМЕР НЕДЕЙСТВИТЕЛЕН: было $HASH/$DATA, стало $HASH2/$DATA2 — повторить"
exit 3
