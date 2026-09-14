# -*- coding: utf-8 -*-
"""Сверка ЗНАЧЕНИЙ enum этого форка с эталонным дистрибутивом ExileApi-Compiled.

Зачем отдельно от tools/parity.sh. Счётчик паритета проверяет НАЛИЧИЕ члена. Для enum этого мало:
член с тем же именем, но другим числом — это не паритет, а тихая поломка, и счётчик её не видит.

Хуже того, дописывание членов с эталонной нумерацией в enum, чьи существующие члены пронумерованы
иначе, делает enum ВНУТРЕННЕ ПРОТИВОРЕЧИВЫМ: два имени получают одно значение, ToString возвращает
лотерею, а сравнение по имени перестаёт совпадать со сравнением по числу. Ровно это и произошло при
первой попытке механического переноса (см. docs/api/parity-measured.md), поэтому проверка и заведена.

Запуск:
    python tools/enumdiff.py                 # все enum, объявленные в этом форке
    python tools/enumdiff.py ModDomain ...   # только названные
    python tools/enumdiff.py --strict        # ненулевой код возврата при любом расхождении

Код возврата: 0 — расхождений значений и дублей нет; 1 — есть (только при --strict).
"""
import os
import re
import subprocess
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROBE = os.path.join(ROOT, 'tools', 'ForkProbe', 'bin', 'Release', 'net10.0', 'ForkProbe.exe')
REF = os.environ.get('EXILE_REF', r'C:\Users\ReviPc\Desktop\ExileApi-Compiled')
MINE = os.environ.get('FORK_OUT', os.path.join(os.path.dirname(ROOT), 'PoeHelper'))

SFLD = re.compile(r'^\s+sfld\s+(\S+)\s+:\s+\S+\s+=\s+(-?\d+)\s*$')
HEAD = re.compile(r'^### (\S+)\s')
DECL = re.compile(r'\benum\s+([A-Za-z_][A-Za-z0-9_]*)')


def declared_enums():
    """Имена enum, объявленных в исходниках этого форка."""
    found = set()
    for sub in ('Core', 'GameOffsets'):
        for dirpath, _dirs, files in os.walk(os.path.join(ROOT, sub)):
            if os.sep + 'obj' in dirpath or os.sep + 'bin' in dirpath:
                continue
            for fn in files:
                if not fn.endswith('.cs'):
                    continue
                with open(os.path.join(dirpath, fn), encoding='utf-8-sig', errors='replace') as fh:
                    for line in fh:
                        line = line.strip()
                        if line.startswith('//') or 'enum' not in line:
                            continue
                        m = DECL.search(line)
                        if m:
                            found.add(m.group(1))
    return sorted(found)


def dump(root, names):
    """имя enum -> {член: значение} по дампу зонда."""
    if not names:
        return {}
    out = subprocess.run([PROBE, root, '--dump'] + names, capture_output=True)
    res, cur = {}, None
    for line in out.stdout.decode('utf-8', 'replace').split('\n'):
        head = HEAD.match(line)
        if head:
            simple = head.group(1).split('.')[-1].split('+')[-1]
            cur = simple if simple in names else None
            if cur and cur not in res:
                res[cur] = {}
            continue
        if cur is None:
            continue
        m = SFLD.match(line.rstrip('\r'))
        if m:
            res[cur][m.group(1)] = int(m.group(2))
    return res


def main():
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    strict = '--strict' in sys.argv

    if not os.path.isfile(PROBE):
        print('Нет ' + PROBE + ' — сначала: dotnet build -c Release tools/ForkProbe/ForkProbe.csproj')
        return 2
    if not os.path.isfile(os.path.join(MINE, 'ExileCore.dll')):
        print('Нет ' + MINE + r'\ExileCore.dll — сначала: dotnet build -c Release ExileApi.sln')
        return 2

    names = args or declared_enums()
    ref, mine = dump(REF, names), dump(MINE, names)

    both = [n for n in names if n in ref and n in mine]
    only_mine = [n for n in names if n in mine and n not in ref]
    bad = []

    print('%-26s %6s %6s %8s %7s %9s %6s' %
          ('enum', 'эталон', 'у нас', 'нет у нас', 'лишних', 'разошлось', 'дубли'))
    for n in both:
        a, b = ref[n], mine[n]
        missing = sum(1 for k in a if k not in b)
        extra = sum(1 for k in b if k not in a)
        mismatch = [k for k in a if k in b and a[k] != b[k]]
        byval = {}
        for k, v in b.items():
            byval.setdefault(v, []).append(k)
        dups = {v: ks for v, ks in byval.items() if len(ks) > 1}
        if mismatch or dups:
            bad.append(n)
        print('%-26s %6d %6d %8d %7d %9d %6d' %
              (n, len(a), len(b), missing, extra, len(mismatch), len(dups)))
        for v, ks in list(dups.items())[:4]:
            print('        дубль значения %-8d -> %s' % (v, ', '.join(ks)))
        for k in mismatch[:4]:
            print('        %-28s у нас %-8d эталон %d' % (k, b[k], a[k]))

    print()
    print('сверено: %d;  с расхождениями значений или дублями: %d;  только у нас: %d'
          % (len(both), len(bad), len(only_mine)))
    if bad:
        print('РАСХОДЯТСЯ: ' + ', '.join(bad))
        print('Это НЕ повод «починить» числа: существующее значение может быть верным для того')
        print('патча игры, на который нацелен этот форк. Решение о том, какой патч целевой, —')
        print('отдельное, и проверяется только живой игрой.')
    return 1 if (bad and strict) else 0


if __name__ == '__main__':
    sys.exit(main())
