# Prompts del Mundo 2 · Tristeza

Prompts listos para pegar, para todas las imágenes de `Nivel2_Diseno.md` (sección 8). La Doncella
va por **Google AI Studio**, en el apartado D. Todo lo demás va por **Gemini**.

Los prompts están en inglés porque Gemini sigue mejor las medidas así. Las explicaciones van en
español.

---

## 0. Cómo pedirlas: lo que funcionó en el Nivel 1

1. **Una imagen por mensaje.** Nada de hojas ni rejillas con varios fotogramas.
2. **Fondo magenta plano (`#FF00FF`).** El damero gris que imita la transparencia viene pintado
   dentro de la imagen, y limpiarlo obligó a escribir un script aparte para la bola de fuego.
3. **Cada pieza tiene una imagen «maestra», y las demás se piden editándola.** Es la lección de
   la erupción: pedidas de cero, cada imagen salía con un tamaño de píxel distinto y no casaban.
   Editando E3, las cinco salieron con la misma rejilla y la misma base. Así que:
   - pide primero la maestra con su prompt;
   - para cada fotograma siguiente, **adjunta otra vez la maestra** (no el fotograma anterior,
     porque el error se acumula), pega la plantilla de edición de abajo y añade la línea de ese
     fotograma.
4. **Mejor aún: algunas maestras salen de arte que ya está en el juego.** La ola sale de editar
   la onda de la Centinela, las púas de la erupción, el Lamento del Eco de la Ira y las placas de
   las plataformas del Nivel 1. Así heredan el tamaño de píxel, la línea de suelo y el encuadre
   que ya funcionan, y entran casi sin ajustes.
5. **Guárdalas en disco**, en `Tesis/Imagenes/Mundo2/<Pieza>/` y con el nombre que indica cada
   apartado. Las imágenes que quedan solo en el chat no se pueden procesar.
6. **Si Gemini no respeta el número de píxeles pedido, no pasa nada**, siempre que todas las
   imágenes de una pieza salgan de la misma maestra. Yo ajusto el tamaño en el juego con los
   píxeles por unidad, sin reescalar ni recortar nada.
7. **Si una sale mal, repítela** editando otra vez la maestra. No la corrijas editando la que
   salió mal.

### Plantilla de edición (para todos los fotogramas que no son la maestra)

```text
Edit the attached image. Keep EXACTLY the same canvas size, the same flat #FF00FF magenta
background, the same art-pixel size (the same blocks), the same palette, the same design and
the same position: its lowest row stays on the same line and in the same place.
Change ONLY what is described below. Hard-edged fully opaque pixels only. NO blur, NO glow halo,
NO soft edges, NO transparency checkerboard, NO text, NO border. Nothing touches the canvas edges.

Change: <la línea del fotograma>
```

### Paleta del mundo (ya va dentro de cada prompt)

Azul profundo `#1E3A5F`, azul hielo `#4A7FA5`, gris azulado `#2C3E50`, blanco nieve `#E8EEF4` y
el acento cian `#00D4FF`. El único naranja del mundo es la grieta del hielo rompible.

---

## A. El escenario

### A1. Tileset de la ciudad sumergida → `Mundo2/Tileset/TileMap2.png`

**Adjunta:** `Tesis/TileMap Nivel 1/TileMap.png`, la hoja del Nivel 1. Pide la misma
distribución: si las piezas quedan en los mismos sitios, el pintor de escenario las encuentra
igual que en el Nivel 1.

```text
Edit the attached pixel-art tileset sheet. Keep EXACTLY the same canvas, the same layout (every
piece in the same place and with the same size), the same art-pixel size and the same hard
pixel style. Replace the black background with flat solid #FF00FF magenta.

Change only the theme: the volcanic fortress becomes a FROZEN DROWNED CITY.
Palette: deep blue #1E3A5F, ice blue #4A7FA5, slate blue-grey #2C3E50, snow white #E8EEF4,
small cyan #00D4FF highlights. No orange, no red, no lava anywhere.

- Black bricks with lava cracks -> wet blue-grey stone masonry with thin frost lines.
- The bottom-rounded stone pieces -> the same shapes with short icicles hanging below.
- The lava-cracked square tile -> a square slab of thick cloudy ice with white cracks.
- The rubble pile -> a pile of broken ice blocks and stones.
- The small lava puddle -> a small frozen puddle.
- Iron beams and supports -> old wooden pier beams with snow on top and icicles below.
- The torn banners -> torn, frozen sails hanging stiff with frost.
- The chains -> a rusty anchor chain covered in frost.
- The brazier -> a sunken street lamp, its glass cracked, a faint cyan light inside.
- The kneeling knight statue -> a stone statue of a mourning citizen: a hooded figure standing
  with head bowed and hands clasped, crusted with ice.

Every block of masonry must tile seamlessly with copies of itself on all four sides.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO gradients, NO transparency
checkerboard, NO text, NO border.
```

### A2. Decoración extra → `Mundo2/Tileset/Props2.png`

Son piezas que no existen en la hoja del Nivel 1. **Adjunta:** la `TileMap2.png` que acabas de
generar, para que tenga el mismo píxel.

```text
Pixel art props sheet for a 2D side-view platformer, same art-pixel size and style as the
attached tileset. Flat solid #FF00FF magenta background. Separate props with clear magenta
space between them, all standing on their own bottom row:
1. a small wooden dining table with ONE empty chair pulled out, frosted, in a drowned house;
2. a large bronze church bell hanging from a beam, green-blue patina, frost on top;
3. a smaller bell lying tilted on the floor;
4. a broken stone column, two pieces;
5. an arched window frame of a ruined house, dark inside;
6. three strands of dark seaweed, tall and thin.
Palette: deep blue #1E3A5F, ice blue #4A7FA5, slate blue-grey #2C3E50, snow white #E8EEF4,
bronze only on the bells. Hard-edged fully opaque pixels. NO blur, NO glow, NO gradients,
NO transparency checkerboard, NO text, NO border.
```

### A3. Superficie del agua (3 en bucle) → `Mundo2/Agua/Agua1.jpg` … `Agua3.jpg`

La lava del Nivel 1 se repite alternando la franja con su espejo. El agua se hará igual, así que
no hace falta que empalme perfecto, pero ayuda.

```text
Pixel art game tile, side view, flat solid #FF00FF magenta background. A horizontal STRIP of
dark water seen from the side: about 96 art pixels wide and 24 art pixels tall, centered on a
wide canvas. The top 3-4 rows are the surface: a thin line of snow white #E8EEF4 and ice blue
#4A7FA5 ripples with small foam pixels. Below it, deep blue #1E3A5F water with a few darker
#2C3E50 bands and two or three tiny cyan #00D4FF glints. The left and right edges must match
so the strip can repeat side by side. The bottom row is straight and flat.
Hard-edged fully opaque pixels. NO blur, NO glow, NO gradient, NO transparency checkerboard,
NO text, NO border.
```

Después, con la plantilla de edición: **Agua2:** «the surface ripples shifted 4 art pixels to the
right, foam in different places». **Agua3:** «the surface ripples shifted 8 art pixels to the
right».

### A4. Placa de hielo flotante → `Mundo2/Placa/Placa1.jpg`

**Adjunta:** `Tesis/Imagenes/Plataforma Combustion/PlataformaCombustion1.jpg`. Saldrá del ancho de
la plataforma de combustión (3,4 u); el recorrido se ajusta a la pieza, como en el Nivel 1.

```text
Edit the attached image. Keep EXACTLY the same canvas, the same art-pixel size, the same
position and the same flat top surface on the same row. Replace the burning stone platform with
a FLOATING SLAB OF ICE: a flat, walkable top of snow white #E8EEF4 with a thin cyan #00D4FF
edge, a body of cloudy ice blue #4A7FA5 with white cracks, and an irregular underside with
three or four short icicles dripping. No fire, no embers, no orange. Background flat solid
#FF00FF magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency
checkerboard, NO text.
```

### A5. Hielo rompible (3 estados y esquirlas) → `Mundo2/Hielo/Hielo1.jpg` … `Hielo3.jpg`, `Esquirlas1.jpg`, `Esquirlas2.jpg`

**Cambió al montarlo (fase B):** no es una losa del suelo, sino un **tapón** que llena el hueco y
asoma 1,25 u sobre la plaza. Si fuera una losa a ras del suelo, la espada de Kael, que golpea de
frente, nunca la alcanzaría. Mide 2 u de ancho y 2,25 de alto: 1 u metida en el hueco y 1,25 por
encima del suelo.

**Adjunta:** `Tesis/Imagenes/Golem/Idle/Golem I1.jpg`, solo para el tamaño del píxel (el de las
mecánicas del Nivel 1, 24 px por unidad).

```text
Pixel art game sprite, side view, flat solid #FF00FF magenta background, the same art-pixel size
and hard outline style as the attached golem.
A BLOCK OF THICK CLOUDY ICE plugging a hole in a frozen floor: about 48 art pixels wide and 54
art pixels tall, on a canvas about 70 art pixels across. Its bottom 24 rows are sunk in the hole
(darker, slate #2C3E50 and deep blue #1E3A5F), the top 30 rows stick out above the floor line as
a rough rounded chunk of ice: snow white #E8EEF4 top, ice blue #4A7FA5 body, white inner cracks.
Across the exposed part runs ONE jagged crack that glows bright ORANGE (#FF7A33 with a few
yellow pixels): it is the only warm color in the whole world, and it must stand out.
The bottom row is flat. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency
checkerboard, NO text, NO border.
```

Después, editando **Hielo1**:
- **Hielo2:** «the block is badly cracked after a hit: the orange crack branches into five or six
  white cracks, two small chips missing from the top».
- **Hielo3:** «the block has shattered: only a few broken ice chunks remain at the edges of the
  hole, the middle is empty, no orange left».
- **Esquirlas1:** «no block: only eight to ten ice shards flying up and outwards from where it
  was».
- **Esquirlas2:** «the same shards lower and further apart, falling».

### A6. Burbujas de las bolsas de aire (4 en bucle) → `Mundo2/Burbujas/Burbujas1.jpg` … `Burbujas4.jpg`

```text
Pixel art game sprite, side view, flat solid #FF00FF magenta background. A thin COLUMN OF AIR
BUBBLES rising, about 12 art pixels wide and 36 art pixels tall, centered on the canvas:
round bubbles of different sizes (1 to 4 art pixels), outlined in snow white #E8EEF4 with ice
blue #4A7FA5 inside and one cyan #00D4FF highlight pixel each. Small bubbles at the bottom,
bigger ones higher up. Hard-edged fully opaque pixels. NO blur, NO glow, NO transparency
checkerboard, NO text, NO border.
```

Después, con la plantilla de edición, adjuntando siempre **Burbujas1** (nunca la anterior). Cada
fotograma es una petición aparte:

- **Burbujas2:** «the column stays in the same place, but every bubble inside it has risen
  exactly 3 art pixels. The bubbles that went past the top of the column are gone, and the same
  number of new tiny bubbles (1 art pixel) appear at the bottom».
- **Burbujas3:** lo mismo con «exactly 6 art pixels».
- **Burbujas4:** lo mismo con «exactly 9 art pixels».

De la 4 a la 1 vuelven a subir 3, así que el bucle cierra.

### A7. Oxígeno en el HUD (5 estados) → `Mundo2/Oxigeno/Oxigeno1.jpg` … `Oxigeno5.jpg`

```text
Pixel art UI icon, flat solid #FF00FF magenta background. A single round AIR BUBBLE, 16×16 art
pixels, centered: a 1-pixel snow white #E8EEF4 outline, filled with ice blue #4A7FA5, a small
cyan #00D4FF shine in the top-left. The bubble is completely FULL. Hard-edged fully opaque
pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text.
```

Después, editando **Oxigeno1**:
- **2:** «only the bottom three quarters are filled, the top quarter is dark #1E3A5F».
- **3:** «only the bottom half is filled».
- **4:** «only the bottom quarter is filled».
- **5:** «empty and cracked: dark inside, two white crack lines across the outline» (el aviso de
  que Kael está perdiendo vida).

---

## B. Los enemigos

### B1. Lamento: el Eco de la Tristeza → `Mundo2/Lamento/` con los nombres del Eco

En `Referencias/Arte/EnemigosNiveles.jpeg` ya estaba la familia de los Ecos: uno por mundo, todos
con el mismo cuerpo. **El azul que chorrea agua es el de la Tristeza**, así que el Lamento no se
diseña de cero: **se hace editando cada fotograma del Eco de la Ira**. Así conserva su pose, su
lienzo y su pivote, y entra con el mismo controlador de animación.

**Por qué salía sin ojos ni hueco en el pecho** (versión del 2026-10-08):
- El prompt pedía «a smooth faceless mask with no eyes», pero el Lamento de la referencia **sí
  tiene ojos**: dos agujeros oscuros de los que cae agua. Gemini seguía unas veces el texto y
  otras la imagen.
- Los fotogramas del Eco no ayudan: al correr y al atacar los ojos brillan en naranja y el hueco
  del pecho apenas se ve. Al quitar el naranja, Gemini borraba los ojos con él.

Ahora va en dos pasos, como las demás piezas: primero una **maestra** del Lamento; después, cada
fotograma con dos adjuntos, **la pose** (el Eco) y **el diseño** (la maestra). Y el prompt
enumera lo que no puede faltar.

**Las bases.** Ya están en `Tesis/Imagenes/Mundo2/Lamento/Bases/` (`I1.png` … `d3.png`): los 12
fotogramas que usa el juego, sobre magenta y todos con el mismo lienzo. Usa esas, no las de
`Tesis/Enemigo/`, que traen el damero pintado y un lienzo distinto cada una.

#### Paso 1: la maestra → `Mundo2/Lamento/I1.png`

**Adjunta:** `Bases/I1.png` y `Referencias/Arte/EnemigosNiveles.jpeg`, en ese orden.

```text
Edit the FIRST attached image. Keep EXACTLY the same canvas, the same art-pixel size, the same
pose, the same body proportions and the same position: the feet stay on the same row and in the
same place.
Redraw the character as its SORROW version: the blue figure, second from the left in the top
row of the SECOND attached image. It MUST have these three things, clearly visible:
1. EYES: two dark empty eye holes in the mask (almost black navy #0B1420), each with a thin
   stream of cyan #00D4FF water running down the mask like tears, down to the chin.
2. CHEST HOLE: a ragged dark hole in the middle of the chest, almost black navy #0B1420, as big
   as a fist, with a few drops falling from its lower edge. It goes through the shirt: it is
   the emptiness where the emotion used to be.
3. WATER: water dripping from the hands, the hem and the chin in small falling drops, and a few
   drops on the floor around the feet.
Pale grey-blue skin, a smooth mask, soaked dark navy work clothes. Shoulders a little more
slumped and the head hanging a little lower: heavy and tired, not aggressive. No orange, no
ember cracks.
Palette: deep blue #1E3A5F, ice blue #4A7FA5, slate blue-grey #2C3E50, snow white #E8EEF4,
cyan #00D4FF only for the tears and a few drops.
Background flat solid #FF00FF magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo,
NO transparency checkerboard, NO text.
```

**Antes de seguir, mírala:** si le faltan los ojos o el hueco, repítela. Todo lo demás los copia
de ella.

#### Paso 2: los otros 11 → `I2`, `r1` … `r4`, `a1` … `a4`, `d2`, `d3`

Cada uno en su mensaje y con dos adjuntos, en este orden: **la base de ese fotograma**
(`Bases/r1.png`, …) y **tu maestra `I1.png`**. Guárdalo con el mismo nombre que su base.

```text
Edit the FIRST attached image. Keep EXACTLY its canvas, its art-pixel size, its pose and its
position: the feet stay on the same row and in the same place.
Redraw the character with the design of the SECOND attached image: the same mask, skin,
clothes, colours and water. Before answering, check these three things. They MUST be in the
result even if the body is turned, leaning or in motion:
1. the two dark eye holes with the cyan tears. In the first image the eyes may glow orange:
   replace that glow with the dark holes. Never remove the eyes;
2. the dark hole in the middle of the chest, as in the second image, always visible;
3. the drops falling from the hands, the hem and the chin.
No orange anywhere: if the first image has glowing orange eyes or cracks, they become blue.
Background flat solid #FF00FF magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo,
NO transparency checkerboard, NO text.
```

**Para la muerte (`d2`, `d3`)** añade al final: «It is dying: instead of crumbling into ash,
the body collapses into falling water, big drops and a splash, more water than body. The mask
keeps its two dark eye holes. Only here, the chest hole may be hidden by the collapse.»

### B2. Escultura Viviente → `Mundo2/Escultura/`

Aprobada el 2026-10-08 tal como se propuso, y **sin andar**: no lleva fotogramas de caminar.

**Adjunta:** `Tesis/Imagenes/Golem/Idle/Golem I1.jpg`, para el tamaño del píxel: es su pariente
de piedra del Nivel 1. La maestra es la **estatua dormida**, que debe parecerse a la estatua del
tileset (A1).

```text
Pixel art game sprite, side view facing LEFT, flat solid #FF00FF magenta background, the same
art-pixel size and hard outline style as the attached golem.
A LIVING STATUE of a drowned city, DORMANT: a life-size stone statue of a mourning citizen,
about 58 art pixels tall on a canvas about 80 art pixels across. A long hooded cloak carved in
slate blue-grey #2C3E50 stone, head bowed, both hands clasped at the chest, crusted with ice
blue #4A7FA5 and snow white #E8EEF4 frost and short icicles. It looks like a decoration. No
visible eyes yet. Its feet rest on a flat bottom row, on a small square pedestal.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text,
NO border, NO floor shadow.
```

Tiene el mismo riesgo que el Lamento: la estatua dormida no tiene ojos, así que todo lo que se
pida editándola saldrá sin ellos. Por eso lleva **dos maestras**: **E1** (dormida) para el
despertar, y **P3** (despierta, con los ojos) para todo lo demás.

| Archivo | Edita | Change |
|---|---|---|
| `P1.jpg` | E1 | «the ice on the statue cracks: a few white crack lines, small ice chips falling» |
| `P2.jpg` | E1 | «the head lifts and two small cyan #00D4FF eyes light up under the hood» |
| `P3.jpg` | E1 | «AWAKE: the head raised, two small cyan #00D4FF eyes lit under the hood, the hands unclasped and the arms lowered to the sides, ice falling off» (es la segunda maestra) |

Los demás, editando **P3**. A la plantilla añade, antes de «Change:», la línea «The two small
cyan eyes under the hood MUST stay visible.» (menos en `M3` y `M4`, donde ya se ha roto):

| Archivo | Change |
|---|---|
| `A1.jpg` | «both arms raised high above the head, hands joined in a heavy stone fist» |
| `A2.jpg` | «arms at the very top, leaning back, about to strike (the warning pose)» |
| `A3.jpg` | «the joined fists slammed down to the floor in front of it, a burst of ice chips» |
| `A4.jpg` | «stuck after the blow: fists still on the floor, bent forward, ice binding them» |
| `A5.jpg` | «pulling the fists free, rising slowly» |
| `H1.jpg` | «hit: leaning back slightly, chips of stone flying off» |
| `M1.jpg` … `M4.jpg` | «cracking all over» / «the arms break off» / «collapsing into blocks» / «only a pile of stone and ice on the pedestal» |

### B3. Medusa de Hielo → `Mundo2/Medusa/`

**Adjunta:** `Tesis/Imagenes/Golem/Idle/Golem I1.jpg`, solo para el tamaño del píxel.

```text
Pixel art game sprite, side view, flat solid #FF00FF magenta background, the same art-pixel size
and hard outline style as the attached golem.
An ICE JELLYFISH floating, about 30 art pixels tall on a canvas about 40 art pixels across: a
round translucent-looking bell (drawn with SOLID pixels: ice blue #4A7FA5 with snow white
#E8EEF4 highlights and a cyan #00D4FF core), and six short tentacles of frozen crystals hanging
below. The bell is fully OPEN (expanded). It looks cold and dangerous, not cute.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO real transparency, NO transparency
checkerboard, NO text, NO border.
```

Después, editando **F1** (la maestra). A la plantilla añade, antes de «Change:», la línea «The
cyan core inside the bell MUST stay visible.»:
- **F2:** «the bell half closed, tentacles closer together».
- **F3:** «the bell fully contracted, narrow and tall, tentacles trailing straight down».
- **F4:** «the bell opening again, tentacles spreading».
- **A1:** «pushed back by a hit: the bell squashed flat and tilted, tentacles flung sideways».
- **A2:** «shrunk to two thirds of its size, the cyan core dimmed».

### B4. La Plañidera, mini-jefe del Mundo 2 → `Mundo2/Planidera/`

La figura encapuchada que dio AI Studio el 2026-10-09 no será la Doncella: será el **mini-jefe
del Mundo 2**, como el Golem de Ceniza en el Mundo 1. Nombre provisional: **la Plañidera** (las
plañideras lloraban por encargo en los entierros; ella llora por una ciudad entera). La imagen ya
está en `Mundo2/Planidera/Concepto.png`, sobre magenta.

**Cómo pelea** (montado el 2026-10-10, en la plaza helada; los números están en
`Nivel2_Diseno.md`, sección 5):
- **Flota** hacia Kael, despacio. No anda: no lleva fotogramas de andar.
- **Esquirlas:** el arco de esquirlas de detrás de su cabeza es su arma. Las alza, las apunta
  (el aviso: las puntas se encienden en cian) y las lanza una tras otra hacia Kael.
- **Desarmada:** sin esquirlas, se encoge y llora unos 2 s. No ataca: es el momento de castigarla.
  Después, las esquirlas vuelven.
- **Abrazo:** si Kael se le pega, echa el brazo atrás (el aviso) y barre con él hacia delante.
- **Hiperarmadura**, como el Golem: los golpes la hacen destellar pero no cortan sus ataques. Por
  eso no lleva fotograma de daño.
- Enseña lo que pedirá la Doncella: esperar a que baje la guardia.

Son **16 fotogramas y 3 de la esquirla**: Flotar 4, Esquirlas 5, Abrazo 3 y Muerte 4. El Golem
tiene 15. Se pueden pedir con Gemini, como el Golem. Si prefieres AI Studio, abre un chat nuevo
**sin** las instrucciones del sistema de la Doncella.

#### Paso 1: la maestra → `F1.png`

**Adjunta:** `Mundo2/Planidera/Concepto.png` y `Tesis/Imagenes/Golem/Idle/Golem I1.jpg` (solo para
el tamaño del píxel), en ese orden.

```text
Turn the FIRST attached image into an in-game sprite: the first frame of her idle animation.
Keep her design EXACTLY: the dark blue hood, the white blindfold over her eyes with cyan tears
running from under it, the glowing cyan HEART in her chest, the light sash at the waist, the
long tattered sleeves and robe, the ghostly water tail curling down to the right, and the arc of
floating dark ice shards behind her head. She faces LEFT.
Redraw her at the same art-pixel size and with the same hard dark outline as the golem in the
SECOND attached image. The golem is only a size reference: copy nothing else from it.
Her figure, from the top of the hood to the lowest point of the tail, is about 84 art pixels
tall, on a canvas about 130 art pixels wide and 110 tall. Place her a little right of center,
leaving about 35 art pixels of empty space on the LEFT for her reaching arm.
Calm floating pose: both arms lowered and slightly open, the shards evenly spread in the arc.
Flat solid #FF00FF magenta background. Hard-edged fully opaque pixels. NO blur, NO glow halo,
NO soft edges, NO transparency checkerboard, NO text, NO border, NO floor shadow.
```

**Antes de seguir, mírala:** la venda, las lágrimas, el corazón y el arco de esquirlas se tienen
que leer a ese tamaño. Si no, repítela.

#### Paso 2: los demás, editando siempre `F1.png`

Cada uno en su mensaje, **adjuntando `F1.png`** (nunca el anterior) y con esta plantilla, que
sustituye a la general:

```text
Edit the attached image. Keep EXACTLY the same canvas size, the same flat #FF00FF magenta
background, the same art-pixel size, the same palette and the same design. She keeps facing
LEFT and floating in the same place: her glowing heart stays on the same spot unless the
change below moves her. These MUST stay visible in every image: the white blindfold over her
eyes with the cyan tears under it, the glowing cyan heart in her chest, and the dark hood.
Change ONLY what is described below. Hard-edged fully opaque pixels only. NO blur, NO glow
halo, NO soft edges, NO transparency checkerboard, NO text, NO border.

Change: <la línea del fotograma>
```

| Archivo | Animación | Change |
|---|---|---|
| `F2.png` | Flotar | «she bobs 2 art pixels up; the tail and the torn hem sway to the right; the shards turn a little along the arc» |
| `F3.png` | Flotar | «the tail and the torn hem sway to the left; the sleeves lift slightly, as if underwater» |
| `F4.png` | Flotar | «she bobs 2 art pixels down; the tail curls a little tighter; the shards turn back» |
| `E1.png` | Esquirlas | «she raises the front arm, palm up; the shards leave the arc and gather in a fan above her head» |
| `E2.png` | Esquirlas | «AIMING (the warning): the front arm stretched forward to the left, pointing; the shards lined up above her, all pointing LEFT, their tips glowing cyan #00D4FF» |
| `E3.png` | Esquirlas | «THROWING: the front arm swept down and forward; the shards are gone from above her head, only a few tiny ice sparkles where they were» |
| `E4.png` | Esquirlas | «DISARMED: no shards at all; she has sunk a little and curled forward, both hands holding the blindfold, longer cyan tears falling, a few drops falling from the tail» |
| `E5.png` | Esquirlas | «the shards reforming: small fragments of dark ice gathering again in the arc behind her head, half their size; she rises back to the calm pose» |
| `A1.png` | Abrazo | «WARNING: she leans back, the front arm pulled back behind her, frost forming on the long fingers» |
| `A2.png` | Abrazo | «the SWEEP: she lunges forward to the left, the front arm stretched as far as it goes, fingers spread like a claw, a short arc of white frost trailing behind the hand» |
| `A3.png` | Abrazo | «recovering: the arm coming back to her side, a few frost flakes falling» |
| `M1.png` | Muerte | «hit hard: she recoils backwards, the shards cracking and scattering away from her» |
| `M2.png` | Muerte | «the shards falling below her, her head dropping forward, the blindfold loosening, the heart dimmer» |
| `M3.png` | Muerte | «her body melting into falling water from the tail upwards: only the head, the shoulders and the flickering heart are left» |
| `M4.png` | Muerte | «nothing left of her but the white blindfold, falling, and a few drops around it» |

En `M3` y `M4` la lista de «MUST stay visible» ya no vale: añade al final «In this image, the
MUST list no longer applies.»

#### La esquirla que lanza (3) → `Esquirla1.png` … `Esquirla3.png`

**Adjunta:** `F1.png`. Así sale con su tamaño de píxel.

```text
Edit the attached image. Keep the same canvas size, the same art-pixel size and the same
palette. Remove the woman completely: keep only ONE of the dark ice shards from the arc behind
her head, alone, in the center of the canvas, turned so its sharp tip points LEFT, with a short
trail of three or four cyan #00D4FF pixels behind it. Flat solid #FF00FF magenta background.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text.
```

Después, editando **Esquirla1** con la plantilla general:
- **Esquirla2:** «the same shard, the trail a little longer and the highlights moved, so 1 and 2
  can flicker in a loop».
- **Esquirla3:** «the shard SHATTERING: five or six small fragments flying apart from the center,
  no trail» (como las chispas de la bola de fuego).

---

## C. Los ataques y efectos de la Doncella

Se pueden pedir ya, sin esperar el diseño nuevo: no dependen de su aspecto.

### C1. La ola (3) → `Mundo2/Ola/Ola1.jpg` … `Ola3.jpg`

**Adjunta:** `Tesis/Imagenes/Onda/Onda2.jpg`, la onda de la Centinela que se curva. Editándola,
la ola mide lo mismo en el juego: 2,4 × 1,2 u. La maestra es la **Ola2**.

```text
Edit the attached image. Keep EXACTLY the same canvas, the same art-pixel size, the same
position, the same size of the shape, the same direction and the same flat bottom row. Replace
the fire shockwave with a WAVE OF ICY WATER travelling along the floor: a curling crest of deep blue
#1E3A5F and ice blue #4A7FA5 water, snow white #E8EEF4 foam on the crest, two or three cyan
#00D4FF glints, a short trail of droplets behind it. No fire, no embers, no orange, no red.
Background flat solid #FF00FF magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo,
NO transparency checkerboard, NO text.
```

Después, editando **Ola2** con la plantilla (no las otras ondas: en bruto no estaban alineadas
entre sí):
- **Ola1:** «the wave RISING: the same base, the crest lower and not yet curled».
- **Ola3:** «the wave BREAKING: the crest collapsing forward into foam and droplets, a little
  lower».

### C2. Aviso de la inundación (2) → `Mundo2/Inundacion/Aviso1.jpg`, `Aviso2.jpg`

**Adjunta:** `Tesis/Imagenes/Erupcion/E1.jpg`, el aviso de la erupción.

```text
Edit the attached image. Keep EXACTLY the same canvas, the same art-pixel size and the same
bottom row. Replace the glowing lava crack with a CRACK IN THE ICE from which water bubbles up:
a jagged white crack lying on the bottom row, with deep blue #1E3A5F water seeping through and
five or six small bubbles and droplets rising above it, at most 8 blocks tall. No fire, no
embers, no orange. Background flat solid #FF00FF magenta. Hard-edged fully opaque pixels.
NO blur, NO glow halo, NO transparency checkerboard, NO text.
```

**Aviso2**, editando Aviso1: «more water: the crack wider, bubbles higher, a thin sheet of water
spreading on both sides». Mientras el agua está subida, se usa la superficie del agua (A3).

### C3. Púas de hielo (5) → `Mundo2/Puas/P1.jpg` … `P5.jpg`

**Adjunta:** `Tesis/Imagenes/Erupcion/E3.jpg`, la columna de lava llena. La maestra es la **P3**.

```text
Edit the attached image. Keep EXACTLY the same canvas, the same art-pixel size, the same flat
bottom row and the same width at the base. Replace the lava column with a CLUSTER OF ICE
SPIKES bursting up from the floor: five or six sharp crystal spikes of different heights, the
tallest about two thirds of the old column's height, leaning slightly outwards. Ice blue
#4A7FA5 bodies, snow white #E8EEF4 edges, cyan #00D4FF at the tips, slate #2C3E50 shadows.
NO ground, NO mound, NO rocks at the base. No fire, no orange. Background flat solid #FF00FF
magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard,
NO text.
```

Después, editando **P3**:
- **P1:** «WARNING: no spikes yet. Only a white layer of frost on the bottom row, as wide as the
  base, and a few snowflake pixels».
- **P2:** «the spikes at half their height».
- **P4:** «the same spikes, with the white highlights and cyan tips moved so P3 and P4 can
  flicker in a loop».
- **P5:** «BREAKING: the spikes shattering into falling shards, a third of the height».

### C4. Capa congelada (3) → `Mundo2/Capa/Capa1.jpg` … `Capa3.jpg`

Es la plataforma que queda en y 4 cuando se congela una franja inundada. **Adjunta:** la
`Placa1.jpg` de A4, para el mismo píxel.

```text
Pixel art game tile, side view, the same art-pixel size and style as the attached ice slab.
Flat solid #FF00FF magenta background. A thin horizontal SHEET OF NEW ICE, about 96 art pixels
wide and 10 art pixels tall: a flat walkable top row of snow white #E8EEF4, cloudy ice blue
#4A7FA5 below with white cracks, a ragged underside with tiny icicles. The left and right edges
must match so it can repeat. Hard-edged fully opaque pixels. NO blur, NO glow, NO transparency
checkerboard, NO text, NO border.
```

Después, editando **Capa2** (la de arriba es la Capa2, la que se mantiene):
- **Capa1:** «FORMING: only the top half of the sheet, with gaps, crystals growing between them».
- **Capa3:** «MELTING: thinner, with holes, water drops falling from the underside».

### C5. Fragmento de la Tristeza → `Mundo2/Fragmento/Fragmento1.jpg`

**Adjunta:** `Tesis2.0/Referencias/Arte/Jefes/fragmentos de alma.jpeg`.

```text
Edit the attached image. Keep ONLY the light blue gem labelled "Sorrow", exactly as it is drawn:
same pixels, same size, same position. Remove the other four gems, all the text and the grey
checkerboard. Background flat solid #FF00FF magenta.
```

---

## D. La Doncella del Lamento · Google AI Studio

El concepto que hay (`Referencias/Arte/Jefes/jefe2.jpeg`) es un espectro encapuchado. Tiene
cuatro cosas que funcionan:
- flota;
- tiene un halo de esquirlas oscuras;
- tiene una cola de agua en lugar de piernas;
- tiene una gema que brilla en el pecho.

> **2026-10-09:** la primera figura que dio AI Studio (encapuchada, con una venda y el corazón que
> brilla) pasa a ser el mini-jefe, la Plañidera (B4). La Doncella tiene que ser otra silueta:
> el prompt D1 ya lo pide.

Le falta lo de **doncella** y, sobre todo, lo que cuenta el guion: «Él cerró los ojos para no
llorar». El diseño nuevo conserva lo que funciona y le da eso. Además, que se lea de lejos, que
se distinga de la Centinela y que sus ojos tapados sean el centro de la figura: en el combate
solo se la puede herir cuando llora.

### Cómo usar AI Studio

1. Entra en `aistudio.google.com` y abre un chat nuevo. Elige un modelo que genere imágenes (los
   que llevan «Image» en el nombre).
2. Pega el **bloque de instrucciones del sistema** en el campo *System instructions*.
3. Adjunta las referencias de cada paso con el botón **+**.
4. Haz los pasos en orden, en el mismo chat: primero el concepto (D1), luego la maestra (D2) y
   luego los fotogramas, editando la maestra (D3).

### Instrucciones del sistema

```text
You are the pixel-art character designer of "Fragmentum: Ecos de la Identidad", a 2D
side-view action platformer about a soldier's clone who recovers the emotions the soldier
suppressed after a war. Each world is one emotion guarded by an "Architect of the Soul".
You are designing the guardian of SORROW: "La Doncella del Lamento" (the Maiden of the Lament).
Her story: the soldier closed his eyes so as not to cry, and his whole city sank under the
water. She guards that unwept grief, keeps her own eyes covered, and can only be wounded when
she finally weeps.

Rules for EVERY image you generate:
- Pixel art with hard-edged, fully opaque pixels; a clear dark outline; limited palette.
- Palette: deep blue #1E3A5F, ice blue #4A7FA5, slate blue-grey #2C3E50, snow white #E8EEF4,
  cyan #00D4FF only for her tears, her heart and a few magic highlights; a desaturated violet
  is allowed in the shadows of the cloth. No orange, no red, no gold.
- Side view facing LEFT (the player comes from the left).
- Flat solid #FF00FF magenta background. NEVER a transparency checkerboard, NEVER a scene
  behind her (unless asked), no text, no border, no floor shadow.
- NO blur, NO glow halo, NO soft or anti-aliased edges, NO gradients, NO semi-transparent
  parts: even water and mist are drawn with solid pixels.
- One image per answer unless told otherwise. When asked to edit an image, keep everything
  that is not mentioned exactly as it is: canvas, pixel size, palette, design and position.
```

### D1. Concepto: tres propuestas

**Adjunta:** `Referencias/Arte/Jefes/jefe2.jpeg` (el concepto actual) y
`Tesis/Jefe1 animaciones/Idle/Jefe I1.png` (la Centinela, para el estilo y el tamaño de jefe).

```text
Design a NEW, more striking version of the boss in the first attached image, as a full-body
character concept in the pixel-art style of the second attached image (the boss of world 1).
Keep from the first image what works: she FLOATS, a broken halo of dark ice shards circles her
head, she has a long ghostly tail of flowing water instead of legs, and a gem glows in her
chest. Make her clearly a MAIDEN and more memorable:
- a tall, elegant, feminine silhouette, readable from far away;
- a mourning veil of frozen lace that covers her EYES completely (this is the key of the
  design: her eyes must be hidden);
- frozen tear tracks of bright cyan running from under the veil;
- long hair that turns into falling water at its ends;
- a high-collared gown of deep blue that dissolves into the water tail;
- slender hands with frost on the fingertips, one hand reaching forward, palm up;
- the broken halo of ice shards looks like a shattered crown or a broken stained-glass window
  from a drowned cathedral.
Sad and beautiful, not a horror monster: no skull, no gore, no teeth.
She must NOT look like a hooded ghost: no hood, no cloth bandage over the eyes, no sash. That
design is already another enemy of this world.
Generate THREE separate images, three clearly different variants of this idea, each one the
full figure centered on a flat #FF00FF background, about 150 art pixels tall.
```

Elige una. Si quieres retocarla, pídelo editándola («Edit image 2: make the veil longer…»)
antes de seguir.

### D2. La maestra: Flotar 1 → `Mundo2/Doncella/F1.png`

**Adjunta:** la propuesta elegida y `Tesis/Jefe1 animaciones/Idle/Jefe I1.png`.

```text
Turn the chosen design (first attached image) into an IN-GAME SPRITE, the first frame of her
idle animation, at the same art-pixel size as the boss in the second attached image.
Square canvas about 200 art pixels across. She floats facing LEFT, centered horizontally, her
whole figure about 150 art pixels tall from the top of the halo to the tip of the water tail.
The lowest point of the water tail sits exactly on art-pixel row 190 (counted from the top):
this row is her anchor in every future frame. Calm pose: hands slightly open at her sides,
veil down, halo shards evenly around her head. Leave empty magenta space on both sides for her
arm movements. Flat solid #FF00FF background. Hard-edged fully opaque pixels.
```

### D3. Los demás fotogramas, editando la maestra

Cada uno en su mensaje, **adjuntando siempre `F1.png`** y empezando así:

```text
Edit the attached image. Keep EXACTLY the same canvas, the same art-pixel size, the same
palette, the same design and the same anchor: the lowest point of her water tail stays on the
same row and in the same place. She keeps facing LEFT. Change ONLY her pose, as described:
```

| Archivo | Animación | Pose |
|---|---|---|
| `F2.png` | Flotar | «the hair and the water tail sway to the right, the halo shards turn slightly» |
| `F3.png` | Flotar | «the hair and the water tail sway to the left» |
| `F4.png` | Flotar | «the hair lifts as if underwater, the tail curls once» |
| `O1.png` | Ola | «the front arm rises to shoulder height, water gathering around the hand» |
| `O2.png` | Ola | «the front arm high above her head, a ball of churning water in the hand» |
| `O3.png` | Ola | «the arm sweeps down and forward to the left, the water leaving the hand as a splash toward the floor» |
| `O4.png` | Ola | «the arm lowering back to rest, a few drops falling from the fingers» |
| `I1.png` | Inundación | «both hands rise to chest height, palms up» |
| `I2.png` | Inundación | «both hands high above her head, the water tail swelling bigger» |
| `I3.png` | Inundación | «both hands pushed down toward the floor, streams of water pouring from her fingers» |
| `I4.png` | Inundación | «hands still low, the streams thinning, the tail back to normal» |
| `L1.png` | Llanto | «she sinks and her head bows, one hand rising toward the veil» |
| `L2.png` | Llanto | «the tail coiled under her as if kneeling on the floor, both hands lifting the veil, her closed eyes visible for the first time, cyan tears falling» |
| `L3.png` | Llanto | «same kneeling pose, shoulders shaking, the tears falling lower, a small puddle of cyan drops» (L2 y L3 alternan: es la ventana para herirla) |
| `L4.png` | Llanto | «the veil falls back over her eyes and she rises again» |
| `C1.png` | Congelar | «both hands joined at her chest, frost forming between the fingers» |
| `C2.png` | Congelar | «hands spreading apart, a ring of snowflakes and ice crystals expanding around her» |
| `C3.png` | Congelar | «both hands thrust down and open, ice crystals bursting toward the floor» |
| `C4.png` | Congelar | «recovering, the crystals melting around her hands» |
| `R1.png` | Rugido | «head thrown back, the veil fluttering, the halo shards spreading wider» |
| `R2.png` | Rugido | «a silent wail: her mouth open under the veil, arms wide, the halo shards burst far outwards» |
| `R3.png` | Rugido | «the shards return around her head, arms coming down» |
| `M1.png` | Muerte | «hit hard: she recoils backwards, the halo cracking» |
| `M2.png` | Muerte | «the halo shards fall, her head drops, the veil slipping off» |
| `M3.png` | Muerte | «her body starts melting into water from the tail upwards» |
| `M4.png` | Muerte | «only the upper body remains above a spreading puddle, her face finally at peace» |
| `M5.png` | Muerte | «only a calm puddle of water on the floor, and above it the light blue Sorrow gem floating» (como el fragmento que deja la Centinela) |

**Cuando estén:** guárdalas en `Tesis/Imagenes/Mundo2/Doncella/` y avísame. Si alguna sale con
otro tamaño de píxel o con el ancla movida, te digo cuál repetir; no la arreglo a mano.

**Si te quedas con el concepto actual** (`jefe2.jpeg`), los pasos D2 y D3 sirven igual
adjuntándolo en lugar de la propuesta elegida. Lo único que cambia es el velo: el espectro no
tiene ojos que tapar, así que en `L2` serían «the hood falls back and her face of light is
revealed, crying».

---

## E. El fondo (parallax, como el del Mundo 1)

**Adjunta en todos:** `Tesis2.0/Assets/Sprites/Fondos/Mundo1Ira/Fondo lejano tileable.png`. Es el
del Mundo 1, para el estilo y la densidad. Pide formato **panorámico 16:9**.

Las capas que se ven a través de otras llevan el fondo magenta en lo que debe quedar
transparente. El cielo y el fondo submarino son opacos.

| Archivo | Prompt |
|---|---|
| `Fondos/Cielo.jpg` | `Pixel art parallax background layer, 16:9, same style and pixel density as the attached image. A cold overcast SKY over a frozen sea at dawn: layered grey-blue clouds (#2C3E50, #4A7FA5), a pale snow white #E8EEF4 light low on the horizon, fine snow falling as single pixels. Fully opaque, no ground, no buildings. The left and right edges must match so it can repeat horizontally. Hard-edged pixels, NO blur, NO gradient banding, NO text.` |
| `Fondos/CiudadLejana.jpg` | `Pixel art parallax background layer, 16:9, same style as the attached image. The distant silhouette of a FROZEN DROWNED CITY: rooftops, chimneys, a cathedral dome and thin towers sticking out of a flat sheet of ice, all in dark blue-grey (#2C3E50, #1E3A5F) with snow white #E8EEF4 snow on the edges, the ice sheet as a flat line along the bottom third. Everything above the skyline is flat solid #FF00FF magenta. The left and right edges must match so it repeats. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/Torre.jpg` | `Pixel art background prop, same style as the attached image. A huge CLOCK TOWER of a drowned city, half sunk in the ice: stone walls #2C3E50, a round frozen clock face with stopped hands, a belfry with two bells at the top, icicles on every ledge, the base disappearing into the ice. A landmark seen from far away. Flat solid #FF00FF magenta around it. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/Submarino.jpg` | `Pixel art parallax background layer, 16:9, same style as the attached image. DEEP UNDERWATER, looking up: dark deep blue #1E3A5F water, slanted shafts of pale light coming down from the frozen surface drawn as solid lighter bands (#4A7FA5), small floating particles, darker #10203a at the bottom. Fully opaque. The left and right edges must match so it can repeat. Hard-edged pixels, NO blur, NO soft gradients, NO text.` |
| `Fondos/CiudadHundida.jpg` | `Pixel art parallax background layer, 16:9, same style as the attached image. The facades of a SUNKEN CITY STREET underwater: houses with dark empty windows, a leaning street lamp, a submerged arch, seaweed swaying, all in muted blues (#1E3A5F, #2C3E50) with a few cyan #00D4FF glints. Everything above the rooftops is flat solid #FF00FF magenta. The left and right edges must match. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/PrimerPlanoArriba.jpg` *(opcional)* | `Pixel art foreground layer, 16:9, same style as the attached image. A row of large dark ICICLES hanging from the top edge and a few frozen branches at the bottom corners, almost black blue #10203a with snow white #E8EEF4 highlights. Everything else flat solid #FF00FF magenta. Edges match for repeating. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/PrimerPlanoAbajo.jpg` *(opcional)* | `Pixel art foreground layer, 16:9, same style as the attached image. Tall strands of dark SEAWEED rising from the bottom edge and a few air bubbles, almost black blue #10203a with ice blue #4A7FA5 highlights. Everything else flat solid #FF00FF magenta. Edges match for repeating. Hard-edged pixels, NO blur, NO text.` |

---

## F. Las cinemáticas (240 × 135, como las del Nivel 1)

**Adjunta en todas:** `Tesis/Imagenes/Cinematica Entrada Jefe/CinematicaJefe1.jpg`, para el
encuadre y la escala de Kael (pequeño, abajo a la izquierda). Adjunta también la maestra de la
Doncella (`F1.png`) cuando salga ella. Todas empiezan así:

```text
Pixel art cinematic still, 16:9, EXACTLY 240 art pixels wide and 135 art pixels tall: every art
pixel is a solid square block of the same size. Same style, same framing and same scale of the
small hero as the attached image. Palette: deep blue #1E3A5F, ice blue #4A7FA5, slate #2C3E50,
snow white #E8EEF4, cyan #00D4FF accents. Hard-edged pixels, NO blur, NO text, NO border.
```

Y sigue con:

| Archivo | Plano |
|---|---|
| `Cinematicas/PresentacionDoncella1.jpg` | «A vast frozen plaza at the foot of a drowned cathedral, the ice floor in the bottom fifth. Kael, tiny, at the bottom left on the ice. In the centre, high up and far away, the Maiden of the Lament floats with her eyes veiled, her water tail falling into a hole in the ice. Snow falling. Silent and cold.» |
| `Cinematicas/PresentacionDoncella2.jpg` | «Close-up of the Maiden of the Lament, framed from the chest up: her frozen veil covering her eyes, her slender hands rising toward it, a single cyan tear about to fall, the broken halo of ice shards around her head. Dark blue background.» |
| `Cinematicas/DerrotaDoncella1.jpg` | «The Maiden of the Lament melting into water in the centre of the plaza, her veil slipping off, her face at peace. Cracks spreading through the ice of the floor, water rising through them. Kael, tiny, at the bottom left, watching.» |
| `Cinematicas/DerrotaDoncella2.jpg` | «The drowned city rising for a moment: rooftops, the clock tower and the cathedral dome emerging from the water under a pale dawn light, water pouring from the windows. Kael, tiny, on the edge of the ice, holding a small light blue gem.» |

---

## G. Kael

**Lo mejor es dibujarlo en Pixelorama**, como el resto de Kael. Gemini no mantiene un personaje
tan pequeño igual de un fotograma a otro. Si quieres una referencia de poses para dibujar
encima:

**Adjunta:** `Tesis2.0/Assets/Sprites/Kael/Kael_Correr.png`.

```text
Using the character in the attached sprite sheet as reference (same design, same clothes, same
proportions), draw a pose reference of him SWIMMING underwater, side view facing right: 4
separate images of a slow swimming cycle (arms pulling, legs kicking), a few air bubbles rising
from his mouth. Pixel art, flat solid #FF00FF magenta background, hard-edged pixels, NO blur,
NO text.
```

**Resbalar** (opcional): una pose de Kael frenando sobre el hielo, inclinado hacia atrás con
los brazos abiertos. Basta con un fotograma, dibujado en Pixelorama.

---

## H. Dónde va cada cosa

| Carpeta en `Tesis/Imagenes/Mundo2/` | Archivos | Herramienta |
|---|---|---|
| `Tileset/` | `TileMap2.png`, `Props2.png` | Gemini |
| `Agua/` | `Agua1` … `Agua3` | Gemini |
| `Placa/` | `Placa1` | Gemini (editando la de combustión) |
| `Hielo/` | `Hielo1` … `Hielo3`, `Esquirlas1`, `Esquirlas2` | Gemini (editando la inestable) |
| `Burbujas/` | `Burbujas1` … `Burbujas4` | Gemini |
| `Oxigeno/` | `Oxigeno1` … `Oxigeno5` | Gemini |
| `Lamento/` | `I1`, `I2`, `r1` … `r4`, `a1` … `a4`, `d2`, `d3` | Gemini (editando las `Bases/` del Eco, con la maestra `I1`) |
| `Escultura/` | `E1`, `P1` … `P3`, `A1` … `A5`, `H1`, `M1` … `M4` | Gemini |
| `Medusa/` | `F1` … `F4`, `A1`, `A2` | Gemini |
| `Planidera/` | `F1` … `F4`, `E1` … `E5`, `A1` … `A3`, `M1` … `M4`, `Esquirla1` … `Esquirla3` | Gemini (editando `F1`) |
| `Ola/` | `Ola1` … `Ola3` | Gemini (editando la onda) |
| `Inundacion/` | `Aviso1`, `Aviso2` | Gemini (editando la erupción) |
| `Puas/` | `P1` … `P5` | Gemini (editando la erupción) |
| `Capa/` | `Capa1` … `Capa3` | Gemini |
| `Fragmento/` | `Fragmento1` | Gemini |
| `Doncella/` | `F1` … `F4`, `O1` … `O4`, `I1` … `I4`, `L1` … `L4`, `C1` … `C4`, `R1` … `R3`, `M1` … `M5` | **AI Studio** |
| `Fondos/` | `Cielo`, `CiudadLejana`, `Torre`, `Submarino`, `CiudadHundida` y, opcionales, `PrimerPlanoArriba` y `PrimerPlanoAbajo` | Gemini |
| `Cinematicas/` | `PresentacionDoncella1`, `PresentacionDoncella2`, `DerrotaDoncella1`, `DerrotaDoncella2` | Gemini |
| `Tesis/Imagenes Kael/Nadar/` | la tira de nado | Pixelorama |

Cuando tengas un paquete completo, avísame. Lo reviso antes de integrarlo: el mismo tamaño de
píxel, la misma base y nada de damero. Si algo no cuadra, te digo qué repetir.
