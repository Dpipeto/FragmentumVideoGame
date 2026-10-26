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

**Cuando lleguen:** déjalas en `Tesis/Imagenes/Onda` como `Onda1.jpg`, `Onda2.jpg` y `Onda3.jpg`.
Las limpio, las meto en el proyecto y sustituyo el rectángulo por la onda animada.

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
