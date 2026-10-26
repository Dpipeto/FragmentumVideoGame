# Prompts de las puertas y de sus cinemáticas

> Pedidos el 2026-10-12. Tres cosas: **la puerta de salida** de cada nivel (hoy es un rectángulo
> translúcido de 2 × 6 u), **la cinemática de Kael cruzándola** (hoy el cierre de cada mundo son
> tres textos sobre negro) y, de paso, las otras dos piezas que siguen siendo rectángulos: la
> verja que cierra la arena y la bandera del checkpoint.
>
> Las reglas son las de siempre (`Prompts_Mundo2.md`, apartado 0): una imagen por mensaje, fondo
> magenta plano `#FF00FF`, una **maestra** por pieza y las demás **editando la maestra**, y todo
> guardado en disco. Los prompts van en inglés; las explicaciones, en español.
>
> **Orden:** primero las puertas (A) y después las cinemáticas (B), porque cada cinemática adjunta
> su puerta abierta para que sea la misma que se ve en el juego.

---

## Decisiones mías, por si quieres cambiarlas antes de generar

- **Cada puerta lleva un emblema de su emoción, que cambia al abrirse.** La de la Ira, un puño
  cerrado que acaba en mano abierta (es la segunda frase de su cierre: «un puño que, por fin, se
  abre»). La de la Tristeza, un ojo cerrado con una lágrima helada que acaba abierto (la línea de
  la Doncella: «Él cerró los ojos para no llorar»).
- **Por la puerta abierta se ve la luz del mundo siguiente.** Por la de la Ira, luz fría azul (la
  Tristeza); por la de la Tristeza, una penumbra verde y violeta (el Miedo). Si prefieres una luz
  blanca neutra en las dos, cambia en los prompts los colores de la luz por `snow white and pale
  gold`.
- **Tamaño:** 3 u de ancho y 4 u de alto, algo más del doble que Kael. A la densidad de los
  tilesets (32 px por unidad) son unos 96 × 128 píxeles de arte. Si Gemini no respeta la medida, no
  importa mientras todos los fotogramas de una puerta salgan de su maestra: la ajusto con los
  píxeles por unidad, sin reescalar.
- **Dos planos por cinemática**, como las del jefe. El primero aguanta las dos primeras frases
  del cierre y el segundo, la tercera («Kael integra el Fragmento…»).
- **Los huecos de Kael.** Al salir de la Ira lleva lo de `CinematicaFinalLvl2`: el fragmento
  naranja en el hombro izquierdo y cuatro huecos (antebrazo izquierdo, codo derecho y las dos
  pantorrillas). Al salir de la Tristeza, el fragmento azul tapa **el antebrazo izquierdo** y
  quedan tres. El hueco lo elegí yo.

---

## A. Las puertas de salida

Cuatro imágenes por puerta: cerrada (la maestra), abriéndose, abierta y una variante de la abierta
para que la luz parpadee. Guárdalas en `Tesis/Imagenes/Puertas/Ira/` y
`Tesis/Imagenes/Puertas/Tristeza/` como `Puerta1.jpg` … `Puerta4.jpg`.

### A1. La puerta de la Ira · maestra → `Puertas/Ira/Puerta1.jpg`

**Adjunta:** `Tesis/TileMap Nivel 1/TileMap.png`, para el tamaño del píxel, la paleta y el
estilo de la piedra.

```text
Pixel art game sprite for a 2D side-view platformer, flat solid #FF00FF magenta background.
Same art-pixel size, same palette and same hard pixel style as the attached tileset.

A MONUMENTAL GATE of a volcanic fortress, seen straight from the front, CLOSED. About 96 art
pixels wide and 128 art pixels tall, centered on the canvas, standing on its own flat bottom row.
A thick frame of black basalt blocks with a round arch on top. Inside it, two heavy stone door
leaves bound with dark iron bands, meeting at a vertical seam in the middle. Thin lava cracks
(deep red #8B1A1A and orange #CC4400, a few yellow-hot pixels) run across the leaves and glow
along the seam: the gate is sealed by fire. On the keystone of the arch, a carved emblem of a
CLENCHED FIST.

Nothing below the bottom row: no ground, no steps, no rubble. Nothing touches the canvas edges.
Hard-edged fully opaque pixels only. NO blur, NO glow halo, NO gradient, NO transparency
checkerboard, NO text, NO border, NO shadow on the floor.
```

Después, **editando siempre `Puerta1.jpg`** con la plantilla de edición de abajo:

- **`Puerta2.jpg`, abriéndose:** «the two door leaves have slid apart by a quarter of the
  opening: the vertical gap in the middle shows a band of cold light drawn as flat solid colours
  (snow white #E8EEF4 in the centre, cyan #00D4FF, ice blue #4A7FA5 at the sides). The lava
  cracks on the leaves are dimmer. The fist on the keystone is unchanged.»
- **`Puerta3.jpg`, abierta:** «the door leaves are fully open and hidden behind the frame: the
  whole doorway is filled with cold light drawn as flat solid bands (snow white #E8EEF4 in the
  centre, then cyan #00D4FF, then ice blue #4A7FA5 next to the frame), with five or six single
  white pixels drifting out of it like snowflakes. The lava cracks on the frame have cooled to
  dark grey: no orange and no red anywhere. The emblem on the keystone is now an OPEN HAND.»
- **`Puerta4.jpg`, abierta (variante):** «the same open gate as described for the open state:
  doorway full of cold light, cooled grey cracks, open hand on the keystone. Move the bands of
  light one or two art pixels and put the snowflake pixels in different places, so this image and
  the previous one can flicker in a loop.» *(Para esta, adjunta `Puerta3.jpg` y edítala.)*

### A2. La puerta de la Tristeza · maestra → `Puertas/Tristeza/Puerta1.jpg`

**Adjunta:** `Tesis/Imagenes/Mundo2/Tileset/TileMap2.jpg`.

```text
Pixel art game sprite for a 2D side-view platformer, flat solid #FF00FF magenta background.
Same art-pixel size, same palette and same hard pixel style as the attached tileset.

THE TALL DOOR OF A DROWNED, FROZEN CATHEDRAL, seen straight from the front, CLOSED. About 96 art
pixels wide and 128 art pixels tall, centered on the canvas, standing on its own flat bottom row.
A frame of wet blue-grey stone masonry (slate #2C3E50, deep blue #1E3A5F) with a pointed arch on
top. Inside it, two door leaves of old dark wood bound with frosted bronze bands. A sheet of
cloudy ice (ice blue #4A7FA5 with snow white #E8EEF4 cracks) covers the seam between the leaves
and seals the door. Short icicles hang from the arch, snow lies on the ledges. On the keystone
of the arch, a carved emblem of a CLOSED EYE with one frozen tear below it (cyan #00D4FF).
No orange, no red, no warm colour anywhere.

Nothing below the bottom row: no ground, no steps, no snow pile. Nothing touches the canvas edges.
Hard-edged fully opaque pixels only. NO blur, NO glow halo, NO gradient, NO transparency
checkerboard, NO text, NO border, NO shadow on the floor.
```

Después, **editando siempre `Puerta1.jpg`**:

- **`Puerta2.jpg`, abriéndose:** «the sheet of ice has cracked and half melted, thin lines of
  water run down the leaves; the leaves are ajar: the vertical gap in the middle shows a band of
  dim light drawn as flat solid colours (pale green #7FE0B0 in the centre, dark violet #2D1B4E
  at the sides). The eye on the keystone is half open.»
- **`Puerta3.jpg`, abierta:** «the ice is gone and the door leaves are fully open, hidden behind
  the frame: the whole doorway is filled with the gloom of a dark forest drawn as flat solid
  bands (pale green #7FE0B0 glow in the centre, then dark green #1A2E1A, then violet-black
  #2D1B4E next to the frame), with four or five single pale green pixels floating in it like
  fireflies. A few meltwater pixels drip from the arch. The eye on the keystone is OPEN and the
  tear is gone.»
- **`Puerta4.jpg`, abierta (variante):** «the same open door as described for the open state.
  Move the bands of light one or two art pixels and put the firefly pixels in different places,
  so this image and the previous one can flicker in a loop.» *(Adjunta `Puerta3.jpg` y edítala.)*

### Plantilla de edición

```text
Edit the attached image. Keep EXACTLY the same canvas size, the same flat #FF00FF magenta
background, the same art-pixel size (the same blocks), the same palette, the same frame and the
same position: its bottom row stays on the same line and in the same place, and the frame does
not move or change size.
Change ONLY what is described below. Hard-edged fully opaque pixels only. NO blur, NO glow halo,
NO soft edges, NO transparency checkerboard, NO text, NO border. Nothing touches the canvas edges.

Change: <la línea del fotograma>
```

### A3. Opcional: la verja de la arena y la bandera del checkpoint

Son los otros dos rectángulos de color que quedan. No los pediste; si los quieres, van así.

**La verja** se cierra detrás de Kael al empezar el combate. Mide 1 u de ancho y 15 u de alto, así
que tiene que poder repetirse hacia arriba. Una imagen por mundo, en
`Puertas/Ira/Verja.jpg` y `Puertas/Tristeza/Verja.jpg`, adjuntando el tileset de su mundo:

```text
Pixel art game sprite, front view, flat solid #FF00FF magenta background. Same art-pixel size,
palette and style as the attached tileset.
A VERTICAL STRIP OF A HEAVY GATE, 32 art pixels wide and 96 art pixels tall, centered on the
canvas. Its top edge and its bottom edge must match exactly, so the strip can be repeated
upwards with no visible seam. <TEMA>
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO gradient, NO transparency
checkerboard, NO text, NO border.
```

- **Ira, `<TEMA>`:** «Thick black iron bars riveted to cross-bands, glowing dull orange #CC4400
  at the rivets, as if just lowered red-hot.»
- **Tristeza, `<TEMA>`:** «A wall of jagged ice spikes grown over old bronze bars: ice blue
  #4A7FA5 bodies, snow white #E8EEF4 edges, a few cyan #00D4FF glints. No warm colours.»

**La bandera del checkpoint** mide unas 0,9 × 2,8 u (unos 29 × 90 píxeles de arte). Dos imágenes
por mundo, apagada y encendida (`Puertas/<Mundo>/Checkpoint1.jpg` y `Checkpoint2.jpg`), la
segunda editando la primera:

```text
Pixel art game sprite, side view, flat solid #FF00FF magenta background. Same art-pixel size,
palette and style as the attached tileset.
A CHECKPOINT MARKER standing on its own flat bottom row, about 29 art pixels wide and 90 art
pixels tall, centered on the canvas: <TEMA>
Nothing below the bottom row. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO
gradient, NO transparency checkerboard, NO text, NO border.
```

- **Ira, `<TEMA>`:** «an iron brazier on a tall thin post, UNLIT: cold black coals, a torn
  banner hanging limp below it.» Y la encendida: «the brazier is LIT: a small flame drawn in
  solid orange #CC4400 and yellow pixels, the banner lifted by the heat.»
- **Tristeza, `<TEMA>`:** «a street lamp of a drowned city on a tall thin frosted post, UNLIT:
  dark cracked glass, an icicle hanging from it.» Y la encendida: «the lamp is LIT: a small
  cyan #00D4FF light inside the glass, the icicle melted to a single drop.» *(Es el farol que ya
  está en el tileset: puedes adjuntarlo como referencia.)*

---

## B. Las cinemáticas de Kael cruzando la puerta

Dos planos por mundo. **Pídelos cuando ya tengas `Puerta3.jpg`** de ese mundo.

### B1. Salida de la Ira (240 × 135, como las del Nivel 1)

Guárdalas en `Tesis/Imagenes/Cinematica Salida/` como `SalidaIra1.jpg` y `SalidaIra2.jpg`.

**Adjunta, en este orden:**
1. `Tesis/Imagenes/Cinematica Entrada Jefe/CinematicaJefe1.jpg` (el encuadre, el tamaño del píxel
   y la escala de Kael);
2. `Tesis/Imagenes/Cinematica Muerte Jefe/CinematicaFinalLvl2.jpg` (Kael con sus huecos y el
   fragmento en el hombro);
3. `Tesis/Imagenes/Puertas/Ira/Puerta3.jpg` (la puerta abierta).

Las dos empiezan así:

```text
Pixel art cinematic still, 16:9, EXACTLY 240 art pixels wide and 135 art pixels tall: every art
pixel is a solid square block of the same size. Same style, same palette and same art-pixel size
as the FIRST attached image. The hero is the one in the SECOND attached image: same design, same
dark worn coat, the same four empty holes in his body and the same glowing orange shard on his
left shoulder. The gate is the one in the THIRD attached image, with its open hand on the
keystone. Hard-edged pixels, NO blur, NO text, NO border, NO letterbox bars.
```

Y siguen con:

| Archivo | Frases que lleva encima | Plano |
|---|---|---|
| `SalidaIra1.jpg` | «El fuego se apaga sin que nadie lo sople.» y «En la ceniza queda una forma: un puño que, por fin, se abre.» | «Wide shot of the boss arena of the volcanic fortress after the fight. The lava in the cracks of the floor has gone dark, thin smoke rises, grey ash falls as single pixels. On the right third of the image the monumental gate stands OPEN, and cold blue-white light pours out of it across the floor. In the centre of the floor lies a heap of grey ash in the shape of an OPEN HAND. The hero, tiny, at the bottom left, seen from behind, walking toward the gate. No enemies, no fire.» |
| `SalidaIra2.jpg` | «Kael integra el Fragmento de la Ira. Ahora puede contener un golpe… y soltarlo cuando decida.» | «Close shot from behind the hero as he steps through the open gate: his dark silhouette, from the knees up, in the left-centre of the image, framed by the basalt arch. In front of him the doorway, full of cold blue-white light, with a few snowflake pixels blowing in. The orange shard on his left shoulder glows: it is the only warm colour left in the image. At the edges, behind him, the dark cooling fortress.» |

### B2. Salida de la Tristeza (480 × 268, como las de la Doncella)

Guárdalas en `Tesis/Imagenes/Mundo2/Cinematicas/` como `SalidaTristeza1.jpg` y
`SalidaTristeza2.jpg`.

**Adjunta, en este orden:**
1. `Tesis/Imagenes/Mundo2/Cinematicas/DerrotaDoncella2.jpg` (el encuadre y el tamaño del píxel
   de las cinemáticas de este mundo);
2. `Tesis/Imagenes/Cinematica Muerte Jefe/CinematicaFinalLvl2.jpg` (el diseño de Kael);
3. `Tesis/Imagenes/Puertas/Tristeza/Puerta3.jpg` (la puerta abierta).

Las dos empiezan así:

```text
Pixel art cinematic still, 16:9. EXACTLY the same canvas and the same art-pixel size (the same
blocks) as the FIRST attached image, and the same style and palette: deep blue #1E3A5F, ice blue
#4A7FA5, slate #2C3E50, snow white #E8EEF4, cyan #00D4FF accents. The hero is the one in the
SECOND attached image: same design and same dark worn coat. He now carries TWO glowing shards:
the orange one on his left shoulder and a light blue one that fills the hole in his left
forearm. Exactly THREE empty holes remain in his body: right elbow, left calf and right calf.
The door is the one in the THIRD attached image, with its open eye on the keystone.
Hard-edged pixels, NO blur, NO text, NO border, NO letterbox bars.
```

Y siguen con:

| Archivo | Frases que lleva encima | Plano |
|---|---|---|
| `SalidaTristeza1.jpg` | «El agua baja despacio, como quien termina de llorar.» y «La ciudad respira por primera vez en años.» | «Wide shot of the frozen plaza after the fight. The water is draining away through the cracks in the ice and leaves wet stone and small pools. In the background the rooftops and the clock tower, dripping, under a pale dawn. On the right third of the image the cathedral door stands OPEN, its ice melted, a dim green glow inside it. The hero, tiny, at the bottom left, seen from behind, walking toward the door. No enemies.» |
| `SalidaTristeza2.jpg` | «Kael integra el Fragmento de la Tristeza. Ya puede avanzar también bajo el agua.» | «Close shot from behind the hero as he steps through the open door: his dark silhouette, from the knees up, in the left-centre of the image, framed by the stone arch, meltwater dripping from it. The two shards glow on him, orange on the left shoulder and light blue on the left forearm. In front of him the doorway: dark trees and mist in deep violet and dark green, with a few pale green firefly pixels. Behind him, at the edges, soft dawn light on wet stone.» |

---

## Cuando las tengas

Avísame con la carpeta. Compruebo que cada puerta tenga el mismo tamaño de píxel y la misma base
en sus cuatro imágenes y que no traigan damero; las limpio y monto:

- **las puertas**, en la salida de cada nivel: cerrada mientras vive el jefe, se abre cuando cae
  y se queda parpadeando entre la 3 y la 4;
- **las cinemáticas**, en el cierre de cada mundo, con sus tres frases sobre los dos planos. La
  cámara de cinemáticas ya admite las dos rejillas (240 × 135 y 480 × 268).

Si algo no cuadra, te digo qué repetir, como con la erupción.
