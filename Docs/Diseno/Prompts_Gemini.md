# Prompts para Gemini

Prompts del arte que falta, listos para pegar. Adjunta siempre la referencia que indica cada uno.

Los del **Mundo 2** (Tristeza) están aparte, en `Prompts_Mundo2.md`.

**Una regla para todos:** pide **fondo magenta plano (#FF00FF)**. La limpieza recorta ese color.
La bola de fuego llegó sobre el damero gris que imita la transparencia, pintado dentro del JPG, y
hubo que escribir una limpieza aparte (`Imagenes/limpios/script/limpiar_bola.py`).

---

## La onda de la Centinela (onda de suelo y ondas del salto)

**Qué es en el juego:** cuando la Centinela golpea el suelo con la espada, sale una onda a ras de
suelo que recorre la arena a 9 u/s. Su zona de daño mide 0,8 × 0,9 u. Hoy se ve como un
rectángulo naranja. El espadazo y la caída del salto ya no lo pintan, porque su animación enseña
el golpe; la onda sí, para que no haga daño invisible.

**Adjunta:** el golpe al suelo de la Centinela, `Tesis/Jefe1 animaciones/Golpe al suelo/G2.jpg`.
Así copia la paleta y el tamaño del píxel: la Centinela se dibujó con unos 200 píxeles de arte a
lo ancho del lienzo.

```text
Pixel art game sprite, side view, 2048×2048 canvas, flat solid #FF00FF magenta background.
Match the attached Sentinel sprite exactly: same palette, same pixel size (about 200 art pixels
across the canvas), same hard black outline.

Draw a GROUND SHOCKWAVE OF FIRE travelling from left to right along the floor: a low crest of
molten rock and flame, about 60 art pixels wide and 45 art pixels tall, sitting on an invisible
floor line. Its bottom row is flat and on the same row in every frame. A crust of dark basalt
along the base, deep red and orange flame above it, and only a few yellow-white hot pixels at the
very tip of the crest. The leading edge (right side) is the tallest and sharpest; the back fades
into a short trail of solid ember pixels.

Generate 3 SEPARATE images, not a grid, not a sprite sheet:
1. the crest rising;
2. the crest at its highest, curling forward;
3. the crest falling, throwing two or three loose embers forward.
Same position and same base line in all three.

Hard-edged opaque pixels only. NO blur, NO glow halo, NO semi-transparent smoke, NO gradient.
NO transparency checkerboard: the background is plain #FF00FF and nothing else.
No text, no border, no shadow on the floor.
```

**Llegaron (2026-10-07) y ya están en el juego.** Las tres venían en orden inverso (la que rompe,
la que se curva y la que sube) y las ordené así: `Onda1` sube, `Onda2` se curva y `Onda3` rompe.
La limpieza está en `Imagenes/limpios/script/limpiar_onda.py`, y el prefab
`Assets/Prefabs/Efectos/OndaDeFuego` viste la onda de suelo, la doble y las del salto.

Dos cosas que no salieron como se pidió:
- **El tamaño.** Gemini la dibujó con unos 225 píxeles de ancho, no 60. A la escala de la Centinela
  (49 px/u) mediría 4,7 × 2,4 u, más alta que el salto de Kael. Entra a 98 px/u, con píxeles de la
  mitad de tamaño que los de la Centinela. Así mide 2,4 × 1,2 u, y la roca de su cresta, lo que su
  zona de daño (0,9 u).
- **La posición.** Cada imagen tenía el suelo y el frente en un sitio distinto, y una de ellas era
  de 1024 px. Se alinearon por la línea de suelo y por el frente de la roca, sin reescalar nada.

---

## Las erupciones de la Centinela

**Qué es en el juego:** en la fase 2 la Centinela clava la espada y brotan tres columnas de lava,
una bajo el jugador y dos alrededor. Cada columna mide 1,6 u de ancho y 5 u de alto. Primero
avisa unos 0,3 s sin hacer daño y después quema 0,9 s. Brota del suelo o de la plataforma donde
esté Kael, así que su base no puede llevar un trozo de suelo pegado. **Desde la v0.34 está completa:**
la grieta avisa (E1), brota (E2), arde (E3 y E4 en bucle) y, al acabar, se derrumba (E5).

### Primera tanda (2026-10-07): no entró

Llegaron cinco imágenes, solo por el chat, y no cuadraban entre sí:
- **Dos rejillas distintas.** El aviso (1) y el colapso (5) tienen píxeles de unos 40 px; la 2, la
  3 y la 4, de unos 20 px. Ninguna siguió los 8 px pedidos.
- **Suelo en la 2.** Trae un montículo de roca debajo, y además es una explosión ancha, no una
  columna. La erupción brota también en las plataformas: no puede llevar suelo.
- **Una columna corta.** La de la 3 y la 4 mide unas 38 × 70 de esos píxeles. Para llenar su zona
  de daño (1,6 × 5 u) habría que verla con píxeles 3,5 veces los de la Centinela.

Lo bueno: la columna de la 3 tiene el estilo que queremos. Si la guardaste, adjúntala como
referencia de estilo en la segunda tanda.

### Segunda tanda: prompt corregido

Gemini dibuja solo con bloques de unos 16-20 px, así que el prompt se adapta a eso: **128
píxeles de arte a lo ancho del lienzo** (bloques de 16 px). La erupción entra entonces a 24 px/u,
el mismo tamaño de píxel que el Golem, la torreta y la bola de fuego. A esa escala, 38 píxeles son
sus 1,6 u de ancho y 120 píxeles, sus 5 u de alto.

**Pídelas de una en una, en el mismo chat.** Empieza con el prompt de abajo para la imagen 3; para
las demás escribe «Same canvas, same 16-pixel blocks, same column position as the previous image.
Now draw image N: …» con el texto de esa imagen. Así no cambia la rejilla entre una y otra.

**Adjunta:** la onda nueva, `Tesis/Imagenes/Onda/Onda2.jpg`, para la paleta; y la columna de la
primera tanda, si la guardaste.

```text
Pixel art game sprite, side view, 2048×2048 canvas, flat solid #FF00FF magenta background.
The canvas is EXACTLY 128 art pixels across: every art pixel is a 16×16 block, in EVERY image.
Same palette as the attached lava wave: dark basalt, deep red, orange, and only a few
yellow-white hot pixels.

Draw a VERTICAL ERUPTION OF LAVA bursting up from the floor: a narrow pillar 38 art pixels wide
and up to 120 art pixels tall, centered horizontally on the canvas. Its bottom row is flat and
sits on row 124 of 128, centered at column 64. NOTHING below that row and NOTHING around its base:
no ground, no mound, no rocks lying on the floor. It must work on any floor or thin platform.

Image 3, FULL: the pillar at full height (120 art pixels), molten core brighter at the bottom,
ragged flame edges, the top breaking into a few droplets. Width 38 art pixels from bottom to top.

The other images, one at a time, same canvas, same blocks, same base row and column:
1. WARNING: only a glowing crack on the bottom row, 38 art pixels wide, and three or four ember
   pixels rising. No column, at most 8 art pixels tall.
2. BURST: the pillar at half height (60 art pixels), 38 wide, a few droplets flying up.
4. FULL, second variant: same size and silhouette as image 3, flames and droplets shifted, so 3
   and 4 can flicker in a loop.
5. COLLAPSE: the pillar falling apart, about 40 art pixels tall, droplets raining down.

Hard-edged opaque pixels only. NO blur, NO glow halo, NO soft magenta edge, NO semi-transparent
smoke, NO gradient. NO transparency checkerboard: the background is plain #FF00FF and nothing
else. No text, no border, no shadow on the floor.
```

**Cuando lleguen:** guárdalas en `Tesis/Imagenes/Erupcion` como `Erupcion1.jpg` …
`Erupcion5.jpg`, en ese orden, y avísame. Hace falta tenerlas en disco: las del chat no se pueden
procesar. El aviso usará la 1; luego vienen la 2, la 3 y la 4 en bucle mientras quema, y la 5 al
apagarse.

### Segunda tanda (2026-10-07): entraron dos de cinco

Llegaron en disco como `E1.jpg` … `E5.jpg`. Gemini no dibujó los bloques de 16 px pedidos, sino
de 8 (E1 y E2) y de 10,24 (E3, E4 y E5). Medidas a su rejilla nativa (`limpiar_erupcion.py`):

| Imagen | Qué es | Rejilla | Veredicto |
|---|---|---|---|
| E1 | aviso | 8 px | **No entra:** el aviso es un montículo de roca de 211 píxeles de ancho (8,8 u a 24 px/u), no una grieta en la base de la columna. |
| E2 | brote | 8 px | **No entra:** a su rejilla mide 54 px de ancho frente a los 44 de E3, y casi lo mismo de alto (165 frente a 150). El brote sería más ancho que la columna llena y no la mitad de alto. |
| E3 | llena | 10,24 px | **Entra.** 44 × 136 píxeles (150 con las gotas), sin suelo. |
| E4 | llena, variante | 10,24 px | **Entra.** La misma silueta y la misma base que E3: alternan en bucle. |
| E5 | colapso | 10,24 px | **No entra:** lleva roca amontonada en la base (88 píxeles de ancho) y las gotas salen cortadas por el borde de arriba. |

E3 y E4 están en el juego a **24 px/u** (el píxel del Golem, la torreta y la bola): la columna mide
1,83 × 5,7 u y cubre su zona de daño (1,6 × 5) con un poco de margen. No se recortó ni se reescaló
ninguna de las otras para arreglarla.

### Tercera tanda: el aviso, el brote y el colapso, editando E3

Pedirlas de cero vuelve a cambiar la rejilla. Esta vez, **adjunta `Tesis/Imagenes/Erupcion/E3.jpg`
y pide a Gemini que la edite**: así conserva el lienzo, el tamaño de los bloques y la posición de
la base. Una imagen por mensaje, en el mismo chat; cada vez, vuelve a adjuntar E3.

```text
Edit the attached image. Keep EXACTLY the same 2048×2048 canvas, the same flat #FF00FF magenta
background, the same art-pixel size (the same blocks), the same palette, and the same position
of the lava column: its bottom row stays on the same line, centered at the same place, the same
width at the base.

Redraw ONLY the column, as described below. Hard-edged opaque pixels only. NO blur, NO glow
halo, NO soft magenta edge, NO semi-transparent smoke. NO ground, NO mound, NO rocks lying
around the base: it must work on any floor or thin platform. Everything stays inside the canvas:
nothing touches its edges. No text, no border, no shadow.
```

Y añade, según la imagen:

- **1, aviso (`E1.jpg`):** «WARNING: no column yet. Only a glowing crack lying on the bottom row,
   exactly as wide as the column's base, and three or four ember pixels rising above it. At most
   8 blocks tall. Nothing else.»
- **2, brote (`E2.jpg`):** «BURST: the same column cut to HALF its height, same width and same base,
   ragged flames at the top and a few droplets flying up.»
- **5, colapso (`E5.jpg`):** «COLLAPSE: the column falling apart, about a third of its height, same
   base, droplets falling down. No rocks on the floor.»

**Cuando lleguen:** guárdalas en `Tesis/Imagenes/Erupcion` como `E1.jpg`, `E2.jpg` y `E5.jpg`,
encima de las de ahora, y avísame. Compruebo que tengan la rejilla de E3 y la base en la misma
fila y del mismo ancho; si cuadran, el aviso sustituye al rectángulo, el brote abre la columna y
el colapso la cierra.

### Tercera tanda (2026-10-08): entraron las tres

Editar E3 funcionó: las cinco imágenes tienen la misma rejilla (10,24 px), la misma fase y la base
en la misma fila, y ninguna lleva suelo. `limpiar_erupcion.py` las muestrea sobre esa rejilla común,
sin alinear ni recentrar nada, en un lienzo de 74 × 150 con el pivote en el centro de la base de la
columna. El aviso (66 px de ancho) y el derrumbe (73 px) se extienden más que la columna (44 px):
son llama baja sobre el suelo, unas 3 u.

---

## CinematicaFinalLvl2: retoque ✔ hecho (2026-10-08)

**Resultado (2026-10-08), ya en el juego.** Hicieron falta dos retoques. El primero llegó a
2752 × 1536 y sin el fragmento que flotaba sobre la espada, pero con cinco huecos: el de la espalda
pasó al muslo. El segundo, a 1024 × 572, tiene los cuatro huecos y el fragmento solo en el hombro.
A 240 × 135 se lee bien; por ser más pequeño y JPEG, el fondo oscuro sale algo más ruidoso. Debajo
queda, como referencia, el prompt que se dio para los huecos.

Si quieres corregirlo, pídele a Gemini que **edite** la versión nueva.

**Adjunta:** `Imagenes/Cinematica Muerte Jefe/CinematicaFinalLvl2.jpg` (la de ahora).

```text
Edit the attached image. Keep everything exactly as it is — same framing, same pixel art, same
palette, same character — with one change only: Kael must have exactly FOUR empty holes in his
body. Fill the hole on the back of his left thigh with the same dark worn cloth as around it, so
that only these four remain: left forearm, right elbow, left calf and right calf.
The glowing shard on his left shoulder stays as it is.
```

**Cuando la tengas:** guárdala encima de `CinematicaFinalLvl2.jpg` y avísame. Se reprocesa con
`python cine.py CinematicaFinalLvl2` y entra sola.
