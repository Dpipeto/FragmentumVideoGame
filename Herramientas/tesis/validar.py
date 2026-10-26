"""Comprobaciones estructurales de un .docx generado (no hay Word ni LibreOffice para abrirlo)."""
import collections
import re
import sys
import zipfile

import docx
from docx.oxml.ns import qn
from lxml import etree

W = '{http://schemas.openxmlformats.org/wordprocessingml/2006/main}'
WP = '{http://schemas.openxmlformats.org/drawingml/2006/wordprocessingDrawing}'


def validar(ruta, volcado=None):
    problemas = []
    z = zipfile.ZipFile(ruta)
    for nombre in z.namelist():
        if nombre.endswith('.xml') or nombre.endswith('.rels'):
            try:
                etree.fromstring(z.read(nombre))
            except etree.XMLSyntaxError as e:
                problemas.append(f'{nombre}: XML mal formado: {e}')
    doc = etree.fromstring(z.read('word/document.xml'))
    # marcadores
    ini = collections.Counter(b.get(W + 'id') for b in doc.iter(W + 'bookmarkStart'))
    fin = collections.Counter(b.get(W + 'id') for b in doc.iter(W + 'bookmarkEnd'))
    nombres = collections.Counter(b.get(W + 'name') for b in doc.iter(W + 'bookmarkStart'))
    for i, n in ini.items():
        if n > 1:
            problemas.append(f'id de marcador repetido: {i}')
        if fin[i] != n:
            problemas.append(f'marcador {i} sin cierre')
    for nombre, n in nombres.items():
        if n > 1:
            problemas.append(f'nombre de marcador repetido: {nombre}')
    # comentarios
    com = etree.fromstring(z.read('word/comments.xml'))
    ids_com = {c.get(W + 'id') for c in com.iter(W + 'comment')}
    for etiqueta in ('commentRangeStart', 'commentRangeEnd', 'commentReference'):
        for e in doc.iter(W + etiqueta):
            if e.get(W + 'id') not in ids_com:
                problemas.append(f'{etiqueta} {e.get(W + "id")} sin comentario')
    usados = {e.get(W + 'id') for e in doc.iter(W + 'commentReference')}
    for i in ids_com - usados:
        problemas.append(f'comentario {i} sin anclar')
    # dibujos
    docpr = collections.Counter(e.get('id') for e in doc.iter(WP + 'docPr'))
    for i, n in docpr.items():
        if n > 1:
            problemas.append(f'docPr id repetido: {i}')
    # celdas
    for tc in doc.iter(W + 'tc'):
        hijos = [h for h in tc if h.tag != W + 'tcPr']
        if not hijos or hijos[-1].tag != W + 'p':
            problemas.append('celda sin párrafo final')
    # campos equilibrados por párrafo
    for p in doc.iter(W + 'p'):
        tipos = [f.get(W + 'fldCharType') for f in p.iter(W + 'fldChar')]
        if tipos.count('begin') != tipos.count('end'):
            # los índices abarcan varios párrafos: solo se informa si no es un índice
            texto = ''.join(it.text or '' for it in p.iter(W + 'instrText'))
            if 'TOC' not in texto and not any(t == 'end' for t in tipos):
                pass
    d = docx.Document(ruta)
    print(f'{ruta}: {len(d.paragraphs)} párrafos, {len(d.tables)} tablas, {len(d.inline_shapes)} imágenes, '
          f'{len(ids_com)} comentarios')
    if volcado:
        with open(volcado, 'w', encoding='utf-8') as f:
            for i, p in enumerate(d.paragraphs):
                t = p.text.strip()
                if t:
                    f.write(f'{i:5d} [{p.style.name}] {t}\n')
    return problemas


if __name__ == '__main__':
    sys.stdout.reconfigure(encoding='utf-8')
    pr = validar(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else None)
    print('problemas:', len(pr))
    for x in pr[:40]:
        print(' ', x)
