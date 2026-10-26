import os
from pathlib import Path

PROY = Path(r'C:\Users\dpipe\OneDrive\Desktop\Importante\Tesis2.0')


def efectivas(ruta):
    n = 0
    en_bloque = False
    for l in open(ruta, encoding='utf-8-sig', errors='replace'):
        s = l.strip()
        if en_bloque:
            if '*/' in s:
                en_bloque = False
            continue
        if not s or s.startswith('//'):
            continue
        if s.startswith('/*'):
            if '*/' not in s:
                en_bloque = True
            continue
        n += 1
    return n


tot = {}
for raiz in ('Assets/Scripts', 'Assets/Tests'):
    for f in (PROY / raiz).rglob('*.cs'):
        partes = f.relative_to(PROY / raiz).parts
        clave = partes[0] if raiz == 'Assets/Scripts' else 'Tests/' + partes[0]
        tot.setdefault(clave, [0, 0])
        tot[clave][0] += 1
        tot[clave][1] += efectivas(f)
for k, (a, l) in sorted(tot.items()):
    print(f'{k:20s} {a:4d} archivos {l:7d} líneas')
juego = sum(l for k, (a, l) in tot.items() if not k.startswith('Tests') and k != 'Editor')
print('juego (sin editor ni pruebas):', juego, 'archivos', sum(a for k, (a, l) in tot.items() if not k.startswith('Tests') and k != 'Editor'))
k = juego / 1000
pm = 2.94 * k ** 1.0997
t = 3.67 * pm ** (0.28 + 0.2 * (1.0997 - 0.91))
print(f'COCOMO II nominal: {pm:.1f} meses-persona, {t:.1f} meses, {pm / t:.1f} personas')
