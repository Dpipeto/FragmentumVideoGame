# Prompts para Gemini

Prompts del arte que falta, listos para pegar. Adjunta siempre la referencia que indica cada uno.

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
avisa unos 0,3 s sin hacer daño y después quema 0,9 s. Hoy se ve como un rectángulo naranja que
late. Brota del suelo o de la plataforma donde esté Kael, así que su base no puede llevar un
trozo de suelo pegado.

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

---

## CinematicaFinalLvl2: retoque opcional

La versión que entró funciona: a 240 × 135 se leen los huecos de Kael y el hombro que brilla. Tiene
dos detalles que contradicen la historia:

- **Sigue flotando un fragmento sobre la espada,** aunque Kael ya lo lleva en el hombro.
- **Hay cinco huecos vacíos.** Con cinco fragmentos en el juego y el primero ya integrado, deberían
  quedar cuatro.

Si quieres corregirlo, pídele a Gemini que **edite** la imagen. No hace falta generarla de nuevo.

**Adjunta:** `Imagenes/Cinematica Muerte Jefe/CinematicaFinalLvl2.jpg`.

```text
Edit the attached image. Keep everything exactly as it is — same framing, same pixel art, same
palette, same character — with two changes only:
1. Remove the small glowing shard floating above the sword in the rubble. The sword stays; the
   air above it is as dark as the rest of the hall.
2. Kael must have exactly FOUR empty holes in his body (right forearm, lower back, back of the
   right thigh, left calf). Fill the fifth one with the same dark worn cloth as around it.
The glowing shard on his left shoulder stays as it is.
```

**Cuando la tengas:** guárdala encima de `CinematicaFinalLvl2.jpg`, mejor a su tamaño original
(2752 × 1536) y no a la copia de 2000 px que entró ahora, y avísame. Se reprocesa con
`python cine.py CinematicaFinalLvl2` y entra sola.
