# Generadores de TesisIA.docx y del GDD

Scripts con los que se actualizó `TesisIA.docx` tras el tercer concepto (16-10-2026) y se generó
`GDD_Fragmentum.docx` (21-10-2026). Trabajan sobre la versión de TesisIA del 13 de octubre
(`TesisIA (versión del 13 de octubre).docx`, en Descargas) y usan sus estilos.

- `docx_util.py`: editar el .docx conservando la numeración automática (SEQ), las referencias
  cruzadas (REF), los comentarios y los resaltados.
- `actualizar_tesisia.py` (con `t3_parte1.py`, `t3_parte2.py` y `t3_anexos.py`): la actualización.
- `gdd.py`: el GDD. `figuras_t3.py`: las figuras compuestas (`fig/`), a partir de las capturas
  (`capturas/`), del arte de `Assets/Sprites` y de `Docs/Diseno` y `Docs/Cinematicas`.
- `subsistemas.py` y `contar_lineas.py`: las cifras de las pruebas y del código.
- `validar.py`: comprobaciones estructurales del .docx (no hay Word ni LibreOffice para abrirlo).

Las rutas a los resultados de las pruebas (`salida_faseD*`) y a las imágenes de TesisIA (`x/word/media`)
eran de la carpeta temporal de la sesión: ajústalas si los vuelves a ejecutar.

## El mundo de la Culpa y la música (24-10-2026)

`actualizar_n4_musica.py` pone al día la TesisIA que tiene el autor (con sus cambios), no la del 13 de
octubre: el mundo de la Culpa terminado, la música, las opciones de volumen, los efectos de Kael y el
Espectro del Pavor más rápido. Solo cambia lo que nombra; lo demás queda como estaba.

```
python figuras_n4.py
python actualizar_n4_musica.py <TesisIA.docx> <salida.docx> <resultados-EditMode.xml> <resultados-PlayMode.xml>
python validar.py <salida.docx>
```

- `figuras_n4.py`: los personajes de la Culpa (con el Juez), el recorrido del Nivel 4 y sus cinemáticas.
- `subsistemas.py` acepta ahora los dos XML de resultados de Unity y tiene el grupo de la música, los
  efectos y las opciones.
- `docx_util.py` numera los marcadores `_Toc` de las leyendas nuevas después de los que ya tiene el
  documento (antes empezaba siempre en el mismo número y, en una segunda actualización, se repetían).
