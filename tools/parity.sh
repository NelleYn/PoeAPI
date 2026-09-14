#!/usr/bin/env bash
# Счётчик паритета с ExileApi-Compiled.
#
# Отвечает на один вопрос числом: сколько единиц данных эталонного дистрибутива этот форк уже
# отдаёт, а сколько ещё нет. Карта поверхности (tools/ForkProbe/surface.json) — 753 единицы,
# 3690 членов, 388 типов; снята зондом по РЕАЛЬНОЙ установке ExileApi-Compiled, а не по
# реконструкции DLL, поэтому её вердикты проверяемы, а не выведены.
#
#   tools/parity.sh                       # против собранного ..\..\PoeHelper
#   tools/parity.sh <папка-сборки>        # против другой сборки
#   tools/parity.sh <папка> --full        # с полной TSV-таблицей построчно
#
# Ненулевой код возврата = есть незакрытые строки. Это НЕ ошибка сборки: это счётчик работы.
#
# НЕ запускать одновременно со сборкой решения: скрипт читает ExileCore.dll из папки сборки, и
# если её в этот момент перезаписывают, счёт расходится на строку-другую. Замечено на прогоне,
# где сборка и счётчик стояли в одной команде: 84 против стабильных 83 в трёх последующих.
set -uo pipefail
here="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
root="$(cd "$here/.." && pwd)"
target="${1:-$root/../PoeHelper}"
full="${2:-}"

if [ ! -f "$target/ExileCore.dll" ]; then
  echo "Нет $target/ExileCore.dll — сначала: dotnet build -c Release ExileApi.sln"
  exit 2
fi

out="$(mktemp -t parity.XXXXXX.tsv)"
dotnet run -c Release --project "$here/ForkProbe" -- "$target" --spec "$here/ForkProbe/surface.json" \
    > "$out" 2>"$out.err"
rc=$?

if [ "$full" = "--full" ]; then
  cat "$out"
else
  # Сводка по видам расхождения — по ней видно, что это за работа: дописать ТИП целиком,
  # добавить недостающие члены существующему типу, или член не нашёлся вовсе.
  awk -F'\t' 'NR>1 && $5 !~ /^OK/ {
        if ($5 ~ /^ТИП/) a++; else if ($5 ~ /^НЕТ ЧАСТИ/) b++; else c++
      }
      END { printf "нет типа целиком: %d\nтип есть, нет части членов: %d\nчлены не найдены: %d\n", a, b, c }' "$out"
fi
tail -1 "$out.err"
echo "полная таблица: $out"
exit $rc
