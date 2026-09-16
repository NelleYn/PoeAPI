#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
dumpdiff.py — разностный поиск поля по двум дампам окна.

Зачем. Оракул называет истинный адрес не всегда: в Лабиринте эталон отдаёт
LabyrinthData = null, хотя объект есть. Тогда поле ищется не поиском значения, а
РАЗНОСТЬЮ состояний: снять дамп окна в состоянии «поля нет» и в состоянии «поле
есть», и найти qword, который из нуля стал указателем. Так был найден LabDataPtr
(+0x48), и так же ищутся портал, лига, любой объект, появляющийся по событию.

Использование:

    FindOffset.exe --window --in <адрес> --len 0x2000 > before.dump   # состояния нет
    ... поменять состояние в игре ...
    FindOffset.exe --window --in <адрес> --len 0x2000 > after.dump    # состояние есть
    python tools/dumpdiff.py before.dump after.dump

Оба дампа ОБЯЗАНЫ быть сняты от одной базы и желательно в одной зоне: при смене
зоны объект переезжает и меняется целиком, и тогда «изменилось» перестаёт что-либо
значить. Скрипт об этом предупредит, если баз в заголовках разные.

Коды возврата: 0 — есть переходы «ноль -> указатель»; 1 — таких переходов нет;
               2 — дампы не разобраны.
"""

import re
import sys

MIN_PTR = 0x10000
MAX_PTR = 0x7FFFFFFFFFFF

LINE = re.compile(r"^\s*\+0x([0-9A-Fa-f]+)\s+0x([0-9A-Fa-f]{16})")
BASE = re.compile(r"начало\s+0x([0-9A-Fa-f]+)")


def load(path):
    """Читает дамп FindOffset --window: {смещение: значение} и база окна."""
    values, base = {}, None

    with open(path, encoding="utf-8", errors="replace") as handle:
        for line in handle:
            if base is None:
                found = BASE.search(line)
                if found:
                    base = int(found.group(1), 16)

            found = LINE.match(line)
            if found:
                values[int(found.group(1), 16)] = int(found.group(2), 16)

    return values, base


# Наблюдённые на этом клиенте кучи лежат выше 2^40, образ игры — около 0x7FF7'00000000.
# Всё, что ниже, при 8-байтовом выравнивании обычно оказывается парой float или счётчиком,
# и валит выдачу шумом, поэтому оно помечается, а не отбрасывается: отбрасывать догадкой
# о диапазоне — это ровно то угадывание, от которого тут все правила.
HEAP_MIN = 0x10000000000
MODULE_MIN = 0x7F0000000000


def is_pointer(value):
    """Похоже ли на указатель пользовательской половины: в диапазоне и выровнен на 8."""
    return MIN_PTR <= value <= MAX_PTR and value % 8 == 0


def kind(value):
    """Грубая примета: куча, образ модуля или мелочь, похожая на пару float."""
    if value >= MODULE_MIN:
        return "модуль"
    if value >= HEAP_MIN:
        return "КУЧА"
    return "мелкое — скорее float/счётчик"


def main(argv):
    if len(argv) < 3:
        print(__doc__)
        return 2

    before, base_before = load(argv[1])
    after, base_after = load(argv[2])

    if not before or not after:
        print("дампы не разобраны: нужен вывод FindOffset --window")
        return 2

    print(f"дамп ДО:     {argv[1]}  ({len(before)} qword, база 0x{base_before or 0:X})")
    print(f"дамп ПОСЛЕ:  {argv[2]}  ({len(after)} qword, база 0x{base_after or 0:X})")

    if base_before != base_after:
        print()
        print("ВНИМАНИЕ: базы окон РАЗНЫЕ. Скорее всего дампы сняты в разных зонах,")
        print("и тогда объект переехал целиком — «изменилось» перестаёт что-либо значить.")
        print("Разностный поиск имеет смысл в ОДНОЙ зоне: снимок до события и после.")

    common = sorted(set(before) & set(after))
    appeared, vanished, changed = [], [], 0

    for offset in common:
        was, now = before[offset], after[offset]

        if was == now:
            continue

        changed += 1

        if was == 0 and is_pointer(now):
            appeared.append((offset, now))
        elif is_pointer(was) and now == 0:
            vanished.append((offset, was))

    print()
    print(f"изменилось qword: {changed} из {len(common)}")

    print()
    print("ПОЯВИЛОСЬ (было 0, стало похоже на указатель) — это и есть кандидаты:")

    if appeared:
        # Сначала то, что похоже на объект в куче: именно там живут подобъекты, и именно
        # ради них всё это и делается. Указатели в образ игры и мелочь — следом, как фон.
        def rank(item):
            value = item[1]
            if HEAP_MIN <= value < MODULE_MIN:
                return 0, item[0]
            if value >= MODULE_MIN:
                return 1, item[0]
            return 2, item[0]

        for offset, value in sorted(appeared, key=rank):
            print(f"  +0x{offset:<6X} 0x{value:<14X} {kind(value)}")

        heap = sum(1 for _, value in appeared if HEAP_MIN <= value < MODULE_MIN)
        print()
        print(f"  из них похожих на объект в куче: {heap}")
    else:
        print("  нет")

    print()
    print("ИСЧЕЗЛО (было указателем, стало 0):")

    if vanished:
        for offset, value in vanished:
            print(f"  +0x{offset:<6X} было 0x{value:X}")
    else:
        print("  нет")

    print()
    print("ДАЛЬШЕ: кандидата подтверждать СОДЕРЖИМЫМ, а не позицией —")
    print("  RefLive.exe --as <ТипЭталона> <адрес кандидата>")
    print("и обязательно с контролем: тот же разбор по заведомо чужому адресу должен дать пусто.")

    return 0 if appeared else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
