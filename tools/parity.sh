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
# Зонд вызывается БИНАРНИКОМ, а не через `dotnet run`, и это не косметика. `dotnet run` пишет
# вывод сборки в тот же stdout, что и программа: первый прогон после правки зонда подмешивал в
# TSV строку предупреждения компилятора, awk считал её за строку спеки, и счётчик показывал 84
# вместо 83. Счётчик, чьё число зависит от того, собирался ли проект в этот раз, — бесполезен.
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
exe="$here/ForkProbe/bin/Release/net10.0/ForkProbe.exe"

# Сборка отдельно и в свой лог: её вывод не имеет права попасть в таблицу.
if ! dotnet build -c Release "$here/ForkProbe/ForkProbe.csproj" --nologo -v q >"$out.build" 2>&1; then
  echo "зонд не собрался:"
  cat "$out.build"
  exit 2
fi
if [ ! -f "$exe" ]; then
  echo "нет $exe после сборки"
  exit 2
fi

"$exe" "$target" --spec "$here/ForkProbe/surface.json" > "$out" 2>"$out.err"
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
