# Prompts del Mundo 4 · Culpa

Las imágenes de la **primera tanda** de `Nivel4_Diseno.md` (sección 8): **los enemigos y el jefe**,
apartados A a D. El Juez Silente va por **Google AI Studio** (apartado B); todo lo demás, por
**Gemini**. La **segunda tanda** (el escenario, las coberturas, los barriles, la mina, el río, el
campanario, las memorias, el fondo, las cinemáticas, las puertas y el escudo de Kael) está al final,
apartados E a L, desde el 2026-10-22: con ella se cierran la fase E y la F.

**El Penitente y el Juez se pueden pedir ya**: salen de la familia de los Ecos y de `Jefe4.jpeg`,
y no dependen de las decisiones. **El Artillero y el Verdugo, mejor después de aprobar** las
decisiones 5 y 7, por si los cambias.

Los prompts están en inglés porque Gemini sigue mejor las medidas así. Las explicaciones van en
español.

---

## 0. Cómo pedirlas

Las mismas reglas de los otros mundos (`Prompts_Mundo3.md`, apartado 0):

1. **Una imagen por mensaje.**
2. **Fondo magenta plano (`#FF00FF`).** Nunca el damero gris.
3. **Primero la maestra; los demás fotogramas, editándola.** Adjunta siempre la maestra, nunca el
   fotograma anterior: el error se acumula. **El Penitente es la excepción:** sus fotogramas salen
   de las bases del Eco, y se piden con una sola imagen, la base (apartado A1, paso 2). Con la base
   y la maestra a la vez, Gemini devuelve la maestra.
4. **Guárdalas en disco**, en `Tesis/Imagenes/Mundo4/<Pieza>/` y con el nombre que indica cada
   apartado (PNG o JPG, da igual).
5. **Si una sale mal, repítela** editando otra vez la maestra.

Y lo que aprendimos, que los prompts ya llevan dentro:

- **Cada maestra tiene todo lo que no puede faltar, y cada prompt lo enumera.** Lo que la maestra
  no tiene, sus ediciones tampoco.
- **Que no cambie el tamaño del píxel.** Todos llevan «Do NOT zoom in». Aun así, **mira cada
  imagen antes de pedir la siguiente**: si la figura sale más grande o más fina, repítela.
- **Margen alrededor de la figura**, sobre todo donde va el arma.
- **Que no aparezca el jefe donde ya no está.** En la derrota del Pavor, el jefe seguía de pie
  después de deshacerse: cada prompt dice qué queda y qué no.
- **Lo que se recoge tiene que verse.** La gema del Miedo, oscura sobre las hojas, apenas se veía:
  la de la Culpa se pide con facetas claras.

### Plantilla de edición (para los fotogramas que no son la maestra)

```text
Edit the attached image. Keep EXACTLY the same canvas size, the same flat #FF00FF magenta
background, the same art-pixel size (the same blocks), the same palette, the same design and
the same position: its lowest row stays on the same line and in the same place.
Do NOT zoom in, do NOT crop and do NOT redraw it bigger or with finer pixels.
Change ONLY what is described below. Hard-edged fully opaque pixels only. NO blur, NO glow halo,
NO soft edges, NO transparency checkerboard, NO text, NO border. Nothing touches the canvas edges.

Change: <la línea del fotograma>
```

### Paleta del mundo (ya va dentro de cada prompt)

Gris parduzco `#3C2E1E`, marrón quemado `#5C3D1E`, verde oliva muerto `#4A5C2E` y rojo
desaturado `#8B3333` (Anexo A), más grises de hierro para las armas y las placas, y un **ámbar
`#E0B060`** (casi blanco `#FFE6A8` en lo más claro) **solo para lo que se recuerda**: la luz del
pecho del Juez. **El rojo apagado es lo que hace daño**: los ojos de los Penitentes al atacar.

---

## A. Los enemigos

### A1. Penitente: el Eco de la Culpa → `Mundo4/Penitente/` con los nombres del Eco

Como el Lamento y el Espectro: en `Referencias/Arte/EnemigosNiveles.jpeg` está la familia de los
Ecos, uno por mundo, todos con el mismo cuerpo. **El pardo encorvado de abajo a la izquierda, con
placas en el pecho y cadenas en las muñecas, es el de la Culpa.** No se diseña de cero: **se hace
editando cada fotograma del Eco de la Ira**. Así conserva su pose, su lienzo y su pivote, y entra
con el mismo controlador de animación.

**Las bases ya están** en `Tesis/Imagenes/Mundo4/Penitente/Bases/` (`I1.png` … `d3.png`): los 12
fotogramas que usa el juego, sobre magenta y con el mismo lienzo (1229 × 905). Son las mismas que
sirvieron para el Espectro. Usa esas.

#### Paso 1: la maestra → `Mundo4/Penitente/I1.png`

**Adjunta:** `Bases/I1.png` y `Referencias/Arte/EnemigosNiveles.jpeg`, en ese orden.

```text
Edit the FIRST attached image. Keep EXACTLY the same canvas size, the same art-pixel size, the
same pose, the same body proportions and the same position: the feet stay on the same row and
in the same place. Do NOT zoom in, do NOT crop and do NOT redraw the figure bigger or with finer
pixels.
Redraw the character as its GUILT version: the brownish hunched figure, the FIRST one in the
BOTTOM row of the SECOND attached image. It MUST have these four things, clearly visible:
1. EYES: two dark empty eye holes in the mask, almost black #1A140E.
2. CHEST HOLE: the ragged hole in the middle of the chest stays where it is, almost black (or
   see-through magenta, as in the first image). It is the emptiness where the emotion used to be.
3. DOG TAGS: eight to twelve small metal dog tags, dull grey #8A8A80 with a dark outline, each
   about 2 by 3 art pixels, hanging from thin cords all over the front of his shirt.
4. SHACKLES AND CHAINS: an iron shackle on each wrist and, from each one, a rusty chain in burnt
   brown #5C3D1E with lighter links, hanging down to the floor and lying on it for a few art
   pixels.
A dull greyish-brown mask and skin, ragged military work clothes in dead olive green #4A5C2E and
burnt brown #5C3D1E, worn and torn. Hunched forward as if carrying a weight, the head low. No
orange, no ember cracks, no water, no dust cloud.
Palette: greyish brown #3C2E1E, burnt brown #5C3D1E, dead olive green #4A5C2E, faded red #8B3333
only for a few small dried stains, dull grey #8A8A80 for the tags and dark iron grey for the
shackles.
Background flat solid #FF00FF magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo,
NO transparency checkerboard, NO text.
```

**Antes de seguir, mírala:** si le faltan los ojos, el hueco, las placas o las cadenas, repítela.
Todo lo demás lo copian de ella.

#### Paso 2: los otros 11 → `I2`, `r1` … `r4`, `a1` … `a4`, `d2`, `d3`

> **Cambiado el 2026-10-19.** La primera versión adjuntaba dos imágenes, la base del fotograma y la
> maestra, y Gemini devolvía la maestra otra vez: el `I1` de siempre, en lugar de la pose de la base.
> Ahora **cada fotograma se pide con una sola imagen, su base**, y el diseño va descrito con
> palabras, con los colores medidos en tu maestra. Así Gemini no tiene otra figura que copiar.

**Cómo pedirlos:**

1. **En la misma conversación en la que salió la maestra**: Gemini recuerda el diseño. Un
   mensaje por fotograma.
2. **Adjunta SOLO la base de ese fotograma** (`Bases/r1.png`, …). **Nunca la maestra.**
3. Pega el prompt de abajo y **cambia `<POSE>` por la línea de ese fotograma** (la tabla de
   debajo). Guárdalo con el mismo nombre que su base.
4. **Si aun así devuelve la maestra o la pose de otro fotograma**, abre una conversación nueva y
   pide ese fotograma igual, con su base sola: el prompt describe el diseño entero y funciona sin
   recordar nada.

```text
Edit the attached image. This is a NEW POSE of the character: keep EXACTLY this pose, this
canvas size, this art-pixel size and this position (the feet stay on the same row and in the
same place). Do NOT copy the pose of any earlier image. Do NOT zoom in, do NOT crop and do NOT
redraw the figure bigger or with finer pixels.
The pose in this image is: <POSE>.
Change ONLY the look of the character, into the same GUILT version as the master you drew
before (the brownish hunched figure). It MUST have these four things, clearly visible, even if the
body is turned, leaning or in motion:
1. EYES: two dark empty eye holes in the mask, almost black #1A140E. If the eyes glow orange in
   this image, replace the glow with dark holes. Never remove the eyes.
2. CHEST HOLE: the ragged hole in the middle of the chest stays where it is, almost black.
3. DOG TAGS: eight to twelve small pale grey-beige dog tags #A89E8C with a dark outline, each
   about 2 by 3 art pixels, hanging from thin cords all over the front of the shirt.
4. SHACKLES AND CHAINS: a dark iron shackle #5E5653 on each wrist and, from each one, a rusty
   brown chain #675145 with darker links #3B2F24 hanging down to the floor; when the arms move,
   the chains swing with them.
A dull greyish-taupe mask and skin (#806F62, darker #6F6356), ragged military work clothes in dark
olive (#4A472F, shadows #2F2F1F), worn and torn, with a few small dried stains in faded red
#8B3333. No orange anywhere: if this image has orange eyes or cracks, they become dark.
Background flat solid #FF00FF magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo,
NO transparency checkerboard, NO text.
```

**La pose de cada fotograma** (todas miran a la derecha, como el Eco). Copia la línea en inglés en
lugar de `<POSE>`:

| Archivo | Lo que es | `<POSE>` |
|---|---|---|
| `I2` | Reposo, al soltar el aire | standing still and breathing out: the same hunched stance as before, but the head and the shoulders a little lower and the knees slightly bent, the arms hanging |
| `r1` | Andar 1 | walking forward, hunched: the body leaning forward, the back leg stretched behind him, the front knee bent with the foot flat, the arms bent at the elbows |
| `r2` | Andar 2 | a low step: crouched forward, the back leg stretched far behind him, the front foot flat under the body |
| `r3` | Andar 3 | a step with the back foot lifted behind him: both knees bent, leaning forward, one hand forward and the other one back |
| `r4` | Andar 4 | a long stride: the back leg stretched behind, the front leg forward, the front arm reaching forward and the back arm swinging behind |
| `a1` | Ataque 1, el aviso | the wind-up: crouched and hunched forward, the knees bent, both arms low, about to lunge |
| `a2` | Ataque 2, la embestida | the lunge: the whole body thrown forward almost horizontally, the front arm fully stretched forward with the hand open, the back leg stretched behind |
| `a3` | Ataque 3, el zarpazo | the grab: crouched, the front arm bent forward at chest height with the hand clawing |
| `a4` | Ataque 4, se recupera | the recovery: rising again, still leaning forward, the arms hanging |
| `d2` | Muerte 1 | dying, still on his feet: the head tilted to one side and the body starting to break apart |
| `d3` | Muerte 2 | dead on the floor: a low heap of the body, with the mask lying on the ground in front of it |

**Para el ataque (`a1` … `a4`)** añade al final del prompt: «He is ATTACKING: only in this image,
the two eye holes glow a dull faded red #8B3333 (a small solid spot of 2 to 3 art pixels in each),
and the chain of the reaching arm whips forward with the hand.»

**Para la muerte (`d2`, `d3`)** añade al final del prompt: «It is dying: the body crumbles into dry
earth and grey ash falling to the ground; the dog tags and the chains drop to the floor in a heap.
The mask is the last solid thing left. Only here, the chest hole may be hidden.»

**Antes de pedir el siguiente, mira el que llegó:** la pose tiene que ser la de su base, no la de la
maestra. Si no, repítelo (en una conversación nueva si hace falta).

**Sobre el tamaño:** tu maestra llegó en un lienzo de 2390 × 1760, casi el doble que las bases
(1229 × 905), como el ataque del Lamento. No hace falta repetirla: al limpiarlos, mido el píxel de
cada fotograma y los encajo sobre el Eco, como al Espectro. Que los demás lleguen del tamaño que
quieran; lo que importa es que cada uno tenga la pose de su base.

### A2. Artillero → `Mundo4/Artillero/`

*Decisión 7 de `Nivel4_Diseno.md`, por aprobar:* un soldado **arrodillado** con casco, un fusil
largo y, bajo el casco, la cara lisa de los Ecos. No se mueve: apunta, dispara tres veces y
recarga. Los sacos terreros que tiene delante van aparte, en la segunda tanda, porque también son
cobertura.

Son **13 fotogramas**: Reposo 2, Apuntar 1, Disparo 2, Recarga 2, Culatazo 2, Daño 1 y Muerte 3.

**Adjunta:** `Tesis/Imagenes/Mundo1/Golem/Idle/Golem I1.jpg`, solo para el tamaño del píxel (el de
los enemigos del Nivel 1).

#### La maestra → `K1.png` (reposo)

```text
Pixel art game sprite, side view facing LEFT, flat solid #FF00FF magenta background, the same
art-pixel size and hard dark outline style as the attached golem. The golem is only a size
reference: copy nothing else from it.
A GUNNER of a burnt battlefield, KNEELING on one knee: about 38 art pixels tall from the floor to
the top of the helmet, on a canvas about 110 art pixels wide and 64 tall, a little right of
center, leaving about 40 art pixels of empty space on the LEFT for his rifle and the muzzle
flash.
- an old dented steel helmet;
- under the helmet, NO human face: the smooth greyish-brown mask of the Ecos, with two dark
  empty eye holes. The mask and its two eye holes must be clearly visible;
- a long trench coat in burnt brown #5C3D1E and dead olive green #4A5C2E, with ammunition pouches;
- a few small dull grey #8A8A80 dog tags hanging on cords from the back of his neck;
- a long old rifle held across his raised knee, pointing LEFT and slightly down;
- watchful: the head up, looking LEFT.
The knee and the foot rest on the same flat bottom row.
Palette: greyish brown #3C2E1E, burnt brown #5C3D1E, dead olive green #4A5C2E, dark iron grey for
the helmet and the rifle, dull grey #8A8A80 for the tags, faded red #8B3333 only for a small
stained armband.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text,
NO border, NO floor shadow.
```

#### Los demás, editando siempre `K1.png`

Con la plantilla de edición, añadiendo antes de «Change:» la línea **«He stays kneeling. The
smooth mask with its two dark eye holes MUST stay visible under the helmet.»** (menos en `M2` y
`M3`):

| Archivo | Animación | Change |
|---|---|---|
| `K2.png` | Reposo | «he breathes out: the shoulders one art pixel lower, the head turned slightly» |
| `A1.png` | Apuntar | «AIMING, the warning: the rifle raised to the shoulder, pointing straight LEFT and level, the mask against the stock; a tiny near-white glint of 2 art pixels on the end of the barrel» |
| `F1.png` | Disparo | «FIRING: the same aim, a short bright muzzle flash at the end of the barrel, pale yellow #FFE6A8 with a faded red #8B3333 edge, at most 8 art pixels long, the rifle kicked up by 1 art pixel» |
| `F2.png` | Disparo | «right after the shot: the same aim, a small puff of grey smoke at the end of the barrel» |
| `R1.png` | Recarga | «RELOADING: the rifle lowered across the knee, one hand working the bolt, the head down» |
| `R2.png` | Recarga | «pushing a clip of bullets into the top of the rifle with the thumb» |
| `G1.png` | Culatazo | «WARNING: the rifle turned around, the wooden stock raised toward the LEFT at chest height, ready to strike» |
| `G2.png` | Culatazo | «the STRIKE: the stock thrust forward to the LEFT, the body leaning into it; he moves at most 8 art pixels to the left» |
| `H1.png` | Daño | «hit: knocked back on his knee, the helmet tilted, the rifle slipping in his hands» |
| `M1.png` | Muerte | «falling sideways, the rifle dropping from his hands» |
| `M2.png` | Muerte | «lying on the ground; the helmet rolling off: under it, the smooth mask, cracked» |
| `M3.png` | Muerte | «only the empty coat, the helmet and the rifle lying on the floor, the dog tags spilled, a little grey ash rising» |

En `M2` y `M3`, cambia la línea de la máscara por «In this image, the mask may be cracked or
hidden.»

### A3. El Verdugo, mini-jefe del Mundo 4 → `Mundo4/Verdugo/`

*Decisión 5 de `Nivel4_Diseno.md`, por aprobar:* como el Golem, la Plañidera y el Acechador. Un
Penitente enorme con capucha de verdugo, un hacha de dos manos y una cadena con garfio: la mano
que cumplió la sentencia. Tiene hiperarmadura: no lleva fotograma de daño. Una explosión lo deja
de rodillas.

Son **18 fotogramas**: Andar 4, Hachazo 3, Barrido 2, Cadena 3, De rodillas 2 y Muerte 4.

**Adjunta:** `Tesis/Imagenes/Mundo1/Golem/Idle/Golem I1.jpg`, solo para el tamaño del píxel.

#### La maestra → `W1.png` (andar, primer paso)

```text
Pixel art game sprite, side view facing LEFT, flat solid #FF00FF magenta background, the same
art-pixel size and hard dark outline style as the attached golem. The golem is only a size
reference: copy nothing else from it.
THE EXECUTIONER, a huge hunched penitent, walking slowly: about 72 art pixels tall from the soles
to the tip of his hood, on a canvas about 170 art pixels wide and 120 tall, a little right of
center, leaving about 50 art pixels of empty space on the LEFT for his axe and about 30 art
pixels above his head for raising it.
- a tall pointed executioner's hood of dark burnt brown #5C3D1E cloth, with two ragged eye holes,
  dark inside: NO face can be seen;
- a broad bare greyish-brown torso, crossed by two heavy chains from which dozens of small dull
  grey #8A8A80 dog tags hang;
- an iron shackle on each wrist;
- a huge two-handed executioner's axe with a wide, chipped, dark iron blade, held in both hands
  across his body, the blade low in front of him on the LEFT;
- a long chain with an iron hook, coiled and hanging from his belt;
- a heavy leather apron and ragged trousers in dead olive green #4A5C2E, the feet wrapped in rags;
- slow and heavy: mid-step, the front leg forward, the back leg behind, both feet on the same
  flat bottom row.
Palette: greyish brown #3C2E1E, burnt brown #5C3D1E, dead olive green #4A5C2E, faded red #8B3333
only for old stains on the apron, dark iron grey for the axe and the chains, dull grey for the
tags. Menacing but not gory: no blood, no wounds.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text,
NO border, NO floor shadow.
```

**Antes de seguir, mírala:** la capucha sin cara, las cadenas con placas y el hacha.

#### Los demás, editando siempre `W1.png`

Con la plantilla de edición, añadiendo antes de «Change:» la línea **«The pointed hood with its
two dark eye holes, the chains with dog tags across his chest and the axe MUST stay visible.»**
(menos en `M3` y `M4`):

| Archivo | Animación | Change |
|---|---|---|
| `W2.png` | Andar | «both feet together under the body, the axe swaying, the chains swinging» |
| `W3.png` | Andar | «mid-step with the OTHER leg forward» |
| `W4.png` | Andar | «both feet together again, the head a little lower» |
| `H1.png` | Hachazo | «WARNING: the axe raised high above his head with both hands, leaning back» |
| `H2.png` | Hachazo | «the CHOP: the axe brought down in front of him to the LEFT, the blade hitting the floor, a few chips of earth flying; he leans forward and moves at most 10 art pixels to the left» |
| `H3.png` | Hachazo | «pulling the axe out of the ground with both hands, straightening up» |
| `B1.png` | Barrido | «WARNING: the axe pulled back to his far side, the body twisted, ready to swing all the way around» |
| `B2.png` | Barrido | «the SWEEP: spinning, the axe swung horizontally around him at waist height, a curved motion streak behind the blade» |
| `C1.png` | Cadena | «WARNING: one hand swinging the hook overhead in a circle, the chain whirling, a circular motion streak around it» |
| `C2.png` | Cadena | «THROWING: the arm thrust forward to the LEFT at chest height, the hand open, only the first few links leaving his hand (the rest of the chain is drawn by the game)» |
| `C3.png` | Cadena | «PULLING: both hands gripping the chain at his chest, leaning back, pulling hard» |
| `K1.png` | De rodillas | «STUNNED by an explosion: down on both knees, the axe dropped on the floor beside him, smoke rising from his shoulders and his hood, a few black soot marks on his skin» |
| `K2.png` | De rodillas | «still on his knees, swaying, one hand on the floor, the head hanging» |
| `M1.png` | Muerte | «hit hard: rearing back, the axe falling from his hands» |
| `M2.png` | Muerte | «collapsing to his knees, the chains loosening around him» |
| `M3.png` | Muerte | «falling forward, the body crumbling into dry earth and grey ash; the hood slipping off: under it NO face, only darkness» |
| `M4.png` | Muerte | «only the empty hood, the axe and a heap of chains and dog tags on the floor» |

En `M3` y `M4` añade al final «In this image, the MUST list applies only to what is still there.»

---

## B. El Juez Silente · Google AI Studio

El concepto que hay (`Referencias/Arte/Jefes/Jefe4.jpeg`) funciona: una figura alta con armadura
oscura y un yelmo liso sin rostro, una capa gris cubierta de placas de identificación, una luz
ámbar en el pecho, una balanza colgando de una mano y una espada larga y recta. Las placas lo
unen con los Penitentes; la luz del pecho es lo único cálido del mundo, como las memorias.

Te recomiendo quedarte con él tal cual (decisión 8): **sáltate B1** y ve directo a la maestra
(B2) adjuntando `Jefe4.jpeg`. B1 queda por si quieres verlo con otro aspecto.

### Cómo usar AI Studio

Como con la Doncella y el Pavor: `aistudio.google.com`, un chat nuevo con un modelo que genere
imágenes, el bloque de abajo en *System instructions*, las referencias con el botón **+** y los
pasos en orden, en el mismo chat. **No reutilices el chat del Pavor:** sus instrucciones del
sistema son otras.

### Instrucciones del sistema

```text
You are the pixel-art character designer of "Fragmentum: Ecos de la Identidad", a 2D
side-view action platformer about a soldier's clone who recovers the emotions the soldier
suppressed after a war. Each world is one emotion guarded by an "Architect of the Soul".
You are designing the guardian of GUILT: "El Juez Silente" (the Silent Judge).
His story: in the war the soldier received an order and said yes, and a village is gone. He has
never looked at what happened. The Judge does not speak and does not condemn: he only shows the
memories the soldier refused to look at. He wears the dog tags of the dead on his cloak, carries
the scales of judgement and a long straight sword, and the only warm light in his burnt world
is the amber glow in his chest. He can only be harmed when the hero looks at a memory: then he
kneels and the scales touch the ground.

Rules for EVERY image you generate:
- Pixel art with hard-edged, fully opaque pixels; a clear dark outline; limited palette.
- Palette: greyish brown #3C2E1E, burnt brown #5C3D1E, dead olive green #4A5C2E, faded red
  #8B3333, dark iron greys for the armour and the sword, dull grey #8A8A80 for the dog tags, and
  amber #E0B060 (near-white #FFE6A8 at its brightest) ONLY for the light in his chest. No blue,
  no green glow, no purple.
- Side view facing LEFT (the player comes from the left).
- Flat solid #FF00FF magenta background. NEVER a transparency checkerboard, NEVER a scene
  behind him (unless asked), no text, no border, no floor shadow.
- NO blur, NO glow halo, NO soft or anti-aliased edges, NO gradients, NO semi-transparent
  parts: even the light and the flying tags are drawn with solid pixels.
- One image per answer unless told otherwise. When asked to edit an image, keep everything that
  is not mentioned exactly as it is: canvas, pixel size, palette, design and position. Never
  zoom in and never redraw him bigger or with finer pixels.
```

### B1. Concepto: tres variantes (opcional)

**Adjunta:** `Referencias/Arte/Jefes/Jefe4.jpeg` y `Tesis/Jefe1 animaciones/Idle/Jefe I1.png` (la
Centinela, para el estilo y el tamaño de jefe).

```text
Design THREE variants of the boss in the first attached image, as full-body character concepts
in the pixel-art style of the second attached image (the boss of world 1). Keep what works: the
tall armoured figure, the smooth faceless helmet, the grey cloak covered with dog tags, the
amber light in the chest, the scales hanging from one hand and the long straight sword.
Make him readable from far away on a burnt brown battlefield: a tall, solemn, upright
silhouette, and the scales and the chest light must be recognizable at a glance.
Solemn and heavy, not monstrous: no blood, no skull, no teeth.
Three separate images, three clearly different variants, each one the full figure centered on
a flat #FF00FF background, about 160 art pixels tall.
```

Elige una. Si quieres retocarla, pídelo editándola antes de seguir.

### B2. La maestra: Reposo 1 → `Mundo4/Juez/I1.png`

**Adjunta:** `Jefe4.jpeg` (o la variante elegida) y `Tesis/Jefe1 animaciones/Idle/Jefe I1.png`.

```text
Turn the boss in the FIRST attached image into an IN-GAME SPRITE, the first frame of his idle
animation, at the same art-pixel size as the boss in the SECOND attached image and about as tall
as it: about 160 art pixels from the soles to the top of the helmet.
Canvas about 240 art pixels wide and 200 tall. He stands facing LEFT, a little right of center,
leaving about 70 art pixels of empty magenta on the LEFT for his sword and some empty space
above his head. The soles of his feet rest exactly on art-pixel row 192 (counted from the top):
this row is his anchor in every future frame.
Solemn, still pose: standing upright, the long straight sword held low in his front hand with
the tip near the ground, the scales hanging from the other hand, the cloak of dog tags over his
shoulders, the amber light in his chest glowing softly. The helmet smooth and faceless.
Flat solid #FF00FF background. Hard-edged fully opaque pixels.
```

**Antes de seguir, mírala:** el yelmo, la luz del pecho, las placas de la capa, la balanza y la
espada se tienen que leer a ese tamaño, y le tiene que sobrar lienzo por todos lados.

### B3. Los demás fotogramas, editando la maestra

Cada uno en su mensaje, **adjuntando siempre `I1.png`** y empezando así:

```text
Edit the attached image. Keep EXACTLY the same canvas, the same art-pixel size, the same palette,
the same design and the same anchor: the soles of his feet stay on the same row and in the same
place, unless the change below moves him. He keeps facing LEFT. Do NOT zoom in and do NOT
redraw him bigger or with finer pixels. These MUST stay visible in every image: the smooth
faceless helmet, the amber light in his chest, the cloak of dog tags, the scales and the sword.
Wherever something was before and is no longer, leave flat magenta: no smear, no ghost of the
old position, no pink trail.
Change ONLY his pose, as described:
```

| Archivo | Animación | Pose |
|---|---|---|
| `I2.png` | Reposo | «he breathes in: the shoulders rise one art pixel, the scales sway slightly» |
| `I3.png` | Reposo | «the helmet turns slightly toward the viewer, the scales still» |
| `I4.png` | Reposo | «he breathes out: the shoulders lower, the scales sway to the other side» |
| `W1.png` | Andar | «a slow, solemn step: the front leg forward, the sword low, the cloak swinging back» |
| `W2.png` | Andar | «both feet together under the body» |
| `W3.png` | Andar | «mid-step with the OTHER leg forward» |
| `W4.png` | Andar | «both feet together again, the scales swaying» |
| `O1.png` | La orden | «WARNING: the sword raised and pointed straight up; the amber light in his chest FLASHES bright near-white #FFE6A8» |
| `O2.png` | La orden | «the ORDER: the arm and the sword stretched straight forward to the LEFT, pointing, like giving a command; the chest light still bright» |
| `O3.png` | La orden | «lowering the sword slowly back to the idle pose» |
| `C1.png` | El pueblo | «WARNING: the scales lifted high in his hand, the pommel of the sword raised toward them; the chest light FLASHES bright near-white» |
| `C2.png` | El pueblo | «striking the scales with the pommel of the sword: the two plates swing wildly, with four to six short straight lines around them, like a bell ringing» |
| `C3.png` | El pueblo | «the scales still swinging as he lowers them» |
| `P1.png` | El compañero | «WARNING: the free hand grabbing the edge of his cloak; the chest light FLASHES bright near-white» |
| `P2.png` | El compañero | «throwing the cloak wide open with one arm: a burst of small dull grey dog tags flying off it to the LEFT, each one drawn with solid pixels» |
| `P3.png` | El compañero | «closing the cloak again, a few tags still falling» |
| `S1.png` | La sentencia | «WARNING: the sword raised high above his head with both hands, the scales hanging from his belt; the chest light FLASHES bright near-white» |
| `S2.png` | La sentencia | «the STRIKE: the sword brought down in front of him to the LEFT» |
| `S3.png` | La sentencia | «the sword driven deep into the floor in front of him, both hands on the pommel, a crack and a few chips of earth around the blade» |
| `S4.png` | La sentencia | «pulling the sword out of the floor, straightening up» |
| `K1.png` | Arrodillado | «EXPOSED: down on one knee, the head bowed, the sword lying flat on the floor beside him, the scales resting on the ground and tipped to one side, the chest light DIM» |
| `K2.png` | Arrodillado | «the same kneeling pose, the head slightly lifted, the chest light flickering a little brighter» |
| `F1.png` | La condena | «the sword held point-down in both hands above the floor, the dog tags of the cloak lifting into the air around him» |
| `F2.png` | La condena | «the sword driven into the floor: a ring of dog tags bursting outward all around him, the chest light blazing bright near-white #FFE6A8» |
| `F3.png` | La condena | «standing behind the sword planted in the floor, the tags falling back around him» |
| `M1.png` | Muerte | «hit hard: recoiling backwards, a crack opening in the helmet, amber light leaking through it» |
| `M2.png` | Muerte | «falling to his knees, the sword and the scales slipping from his hands» |
| `M3.png` | Muerte | «the armour emptying: the helmet falling off with NO face under it, only darkness and a few amber sparks; the cloak collapsing» |
| `M4.png` | Muerte | «only the empty cloak of dog tags, the helmet, the sword and the scales lying on the floor. Nobody is standing» |
| `M5.png` | Muerte | «only a low heap of the cloak and the dog tags on the floor and, floating above it, the Guilt gem: faded red #8B3333 with clearly lighter red #D88A7A facets and a near-white highlight, so that it stands out. Nobody is standing» (como el fragmento que dejan los otros jefes) |

En `K1`, `K2` y de `M2` a `M5`, añade al final «In this image, the MUST list applies only to
what is still there.»

**Cuando estén:** guárdalas en `Tesis/Imagenes/Mundo4/Juez/` y avísame. Si alguna sale con
otro tamaño de píxel o con el ancla movida, te digo cuál repetir; no la arreglo a mano.

### Revisión del 21 de octubre de 2026: 30 de 31

> **Muy bien de escala y de sitio:** las 31 tienen la misma rejilla de píxel (200 píxeles de arte a lo
> ancho: la maestra llegó en 1024 × 1024 y las demás, editadas sobre ella, en 2048 × 2048, el doble
> exacto) y el Juez está en el mismo sitio en todas.
>
> **Limpias (30):** todas menos `M5`, en `Tesis/Imagenes/limpios/Mundo4/Juez/` (`limpiar_mundo4.py`).
> `C1` y `P1` se repitieron (sus primeros avisos salieron como el reposo con el pecho encendido) y ya
> distinguen su ataque. A `S3` se le borró la estela borrosa de la espada, y a todas, el rosa que se
> coló. Tres valen aunque no son exactamente lo pedido: en `O2` la espada apunta adelante a la altura
> de la cintura; en `S2` la espada va a medio camino (queda bien entre `S1` y `S3`), y en `K1` la
> balanza cuelga del cinturón en lugar de apoyarse en el suelo.
>
> **Hay que repetir `M5`.** Gemini pintó un resplandor magenta sobre lo alto de la capa y, al
> quitarlo, la capucha queda rosada en lugar de parda: ese color no se puede recuperar. Además se pidió
> un montón bajo y salió otra vez la capa de pie, como en `M4`. Pídela **adjuntando `I1.png`**, con la
> plantilla de B3, la línea del final de la tabla («In this image, the MUST list applies only to what
> is still there.») y esta pose:
>
> «Nobody is left: the empty cloak of dog tags has COLLAPSED into a LOW heap on the floor, no taller
> than his knees were, with the helmet, the sword and the scales lying beside it. Floating above the
> heap, the Guilt gem: faded red #8B3333 with clearly lighter red #D88A7A facets and a near-white
> highlight, about 10 art pixels tall. NO glow and NO halo anywhere: the gem and the cloak are drawn
> with solid pixels, and the cloak keeps its brown, olive and grey colours; nothing is pink or
> magenta.»
>
> **Opcional: arrodillado más bajo.** En `K1`, `K2`, `M2` y `M3` el Juez se arrodilla, pero el yelmo se
> queda casi a la misma altura que de pie (las piernas dobladas se alargan). Se lee bien y no hay que
> cambiarlo; si quieres que al arrodillarse se le vea bajar, repítelos con su pose de la tabla y esta
> línea al final: «He is LOWER than in the attached image: the knee on the floor rests on the same row
> as his soles, and the top of the helmet is about 35 art pixels lower than in the attached image. He
> keeps exactly his size: he must NOT look bigger.»
>
> *Resuelto el 22 de octubre: ver la revisión del apartado C.*

---

## C. Lo que lanza y lo que deja (Gemini)

Todas **adjuntando `Juez/I1.png`**, para que salgan con su píxel, salvo la placa (C1), que parte de
su propia base. Si en alguna otra Gemini deja al Juez (como pasó con la placa), adjunta
`Placa/Bases/Placa_base.png` en su lugar y cambia «Remove the figure completely and draw» por
«Replace the dog tag with»: así tampoco hay nada que borrar.

### C1. La placa que vuela (2) → `Mundo4/Placa/Placa1.png`, `Placa2.png`

> **Cambiado el 21 de octubre de 2026.** Editando `I1.png`, Gemini no quitó al Juez: dejó una placa
> cinco veces más grande pegada a su vientre, con un resplandor magenta encima que se come el cordón
> y el borde derecho (`placa1.jpg`, no sirve). Ahora se parte de **`Mundo4/Placa/Bases/Placa_base.png`**:
> la maestra con todo borrado menos una placa de la capa, con su píxel, sus colores y su cordón, y
> llevada al centro 25 píxeles de arte justos. Así no hay nada que quitar. Sin estela: la placa sale
> disparada hacia la cabeza de Kael en cualquier dirección y el Juez se voltea, así que una estela
> pintada hacia la derecha quedaría mal en cuanto vuele de otra forma.

**Adjunta:** `Placa/Bases/Placa_base.png` (en lugar de la maestra).

```text
Edit the attached image: it is ONE small dog tag on a flat #FF00FF background. Keep EXACTLY the
same art-pixel size (the same blocks), the same colours and the same flat #FF00FF background.
Do NOT zoom in and do NOT add anything else to the image.
Make it slightly bigger, about 6 art pixels wide and 9 tall, by using MORE blocks of the SAME
size, never bigger blocks. Keep its pale grey colour, its dark outline and its small dark hole.
The cord above it is snapped: only two short dark ends are left. Tilt the whole tag about
30 degrees, as if it were spinning through the air. Leave it in the center of the canvas.
Hard-edged fully opaque pixels. NO motion lines, NO blur, NO glow, NO halo, NO soft edges,
NO pink, NO transparency checkerboard, NO text, NO border.
```

Después, editando **Placa1** (la que salió) con el mismo comienzo, **Placa2:** «the same tag
halfway through its spin: we see it almost from the edge, so it is only about 3 art pixels wide
and just as tall, with the same outline and colours. Nothing else changes.»

**Antes de seguir, mírala:** tiene que ser pequeña (como una placa de la capa, un poco mayor) y no
llevar nada rosa ni difuminado alrededor. Si vuelve a salir con resplandor, repítela: ese color no
se limpia.

### C2. La explosión (4) y su aviso (2) → `Mundo4/Explosion/Explosion1.png` … `Explosion4.png`, `Marca1.png`, `Marca2.png`

La misma para los barriles y para la artillería de «La orden».

```text
Edit the attached image. Keep the same canvas size, the same art-pixel size and the same flat
#FF00FF magenta background. Remove the figure completely and draw an EXPLOSION on the ground,
its first instant: a small bright burst about 30 art pixels wide at the bottom center of the
canvas, a pale yellow #FFE6A8 core, burnt orange and faded red #8B3333 flames around it, and a
few dark earth chunks flying up. Its bottom row is flat. Do NOT zoom in. Hard-edged fully opaque
pixels. NO blur, NO glow halo, NO soft edges, NO gradient, NO transparency checkerboard, NO text.
```

Después, editando **Explosion1** con la plantilla:
- **Explosion2:** «the full blast: a round fireball about 60 art pixels wide rising from the same spot on the ground, dark smoke at its edges, earth chunks flying outward».
- **Explosion3:** «the fireball turning into a thick dark grey-brown smoke cloud, a few embers falling».
- **Explosion4:** «only a thin wisp of smoke over a small scorched black crater on the ground».
- **Marca1:** «no explosion: only a strip of scorched ground about 60 art pixels wide and 6 tall at the bottom center, with a faded red #8B3333 ring of cracks on it: the warning mark».
- **Marca2:** «the same warning mark, the cracks glowing a brighter red, a few tiny sparks above it».

### C3. La ola de fuego (2) → `Mundo4/Fuego/Ola1.png`, `Ola2.png`

```text
Edit the attached image. Keep the same canvas size, the same art-pixel size and the same flat
#FF00FF magenta background. Remove the figure completely and draw a low WALL OF FIRE rolling
along the ground to the LEFT, about 30 art pixels tall and 40 wide, sitting on a flat bottom
row: faded red #8B3333 and burnt orange flames with pale yellow #FFE6A8 tips leaning left, and
black smoke trailing at the back on the right. Do NOT zoom in. Hard-edged fully opaque pixels.
NO blur, NO glow halo, NO soft edges, NO gradient, NO transparency checkerboard, NO text.
```

Después, editando **Ola1**: **Ola2:** «the same wall of fire with the flames in another position, as
the next instant of the same movement».

### C4. La cadena (2) → `Mundo4/Cadena/Garfio.png`, `Cadena.png`

La del Verdugo y la de la trampa del Juez. El juego repite el tramo tantas veces como haga falta.

```text
Edit the attached image. Keep the same canvas size, the same art-pixel size and the same flat
#FF00FF magenta background. Remove the figure completely and draw ONE heavy iron HOOK at the end
of three chain links, about 16 art pixels long, dark rusty iron in burnt brown #5C3D1E with
lighter edges, the hook pointing LEFT, in the center of the canvas. Do NOT zoom in. Hard-edged
fully opaque pixels. NO blur, NO glow halo, NO soft edges, NO transparency checkerboard, NO text.
```

Después, editando **Garfio**: **Cadena:** «no hook: only a straight horizontal piece of the same chain,
exactly 32 art pixels long and 6 tall, the links alternating flat and side-on, both ends cut
cleanly so that it can repeat end to end».

### C5. La bala del Artillero (1) → `Mundo4/Bala/Bala1.png`

```text
Edit the attached image. Keep the same canvas size, the same art-pixel size and the same flat
#FF00FF magenta background. Remove the figure completely and draw ONE short bullet tracer flying
to the LEFT in the center of the canvas: a bright pale yellow #FFE6A8 core 6 art pixels long and
2 tall, with a faded red #8B3333 streak 10 art pixels long behind it. Nothing else. Do NOT zoom
in. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO soft edges, NO transparency
checkerboard, NO text.
```

### C6. El Fragmento de la Culpa → `Mundo4/Fragmento/Fragmento1.png`

**Adjunta:** `Tesis/Imagenes/Mundo2/Fragmento/Fragmentoo.jpg`, el de la Tristeza: así los
fragmentos casan. (En lugar de la maestra del Juez.)

```text
Edit the attached image. Keep EXACTLY the same canvas, the same art-pixel size, the same shape
and the same position of the gem. Change ONLY its colours: it becomes the GUILT fragment, a
faded red #8B3333 gem with clearly lighter red #D88A7A facets, a thin amber #E0B060 glint along
one edge and one near-white #FFF0E8 highlight, so that it stands out on dark ground. Background
flat solid #FF00FF magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO
transparency checkerboard, NO text.
```

### Revisión del 22 de octubre de 2026: el apartado C y los repetidos del Juez

> **Limpias todas**, en `Tesis/Imagenes/limpios/Mundo4/<Pieza>/` (`limpiar_mundo4.py`). No hay que
> repetir nada.
>
> - **La placa**, desde su base: `Placa1`, bien; `Placa2` no salió de canto sino girada hacia el otro
>   lado, y sirve igual como segundo fotograma del giro.
> - **La explosión, la marca, la ola, el garfio, la cadena y la bala** salieron con un píxel el doble
>   de grande que el del Juez (100 a lo ancho en 2048), todas iguales entre sí. En el juego cada pieza
>   lleva sus píxeles por unidad, así que valen.
> - **Lo que se borró**, como los fantasmas de antes: en `Explosion4`, el humo de `Explosion3` a medio
>   deshacer (borroso a la izquierda, rosa a la derecha); en `Marca1`, una voluta de humo; en el
>   garfio, la ola de fuego que había debajo (Gemini editó `Ola1` en lugar de la maestra: el garfio
>   y sus tres eslabones estaban enteros por encima); y bajo la `Cadena`, un garfio suelto.
> - **`M5` del Juez:** el montón con el yelmo, la espada y la balanza está bien, pero encima volvió a
>   quedar el Juez de pie, con la gema delante y un resplandor rosa. Entre los dos hay magenta
>   limpio: se queda solo el montón, y **la gema que flota encima la pone el juego con `Fragmento1`**
>   (C6), que salió bien. Así no hace falta pedirla una tercera vez.
> - **`K1` y `K2`**, arrodillado más bajo: el yelmo baja 33 píxeles de arte y el Juez conserva su
>   tamaño. **`M2` y `M3`** salieron a la altura de antes; valen igual.

---

## D. Dónde va cada cosa

| Carpeta en `Tesis/Imagenes/Mundo4/` | Archivos | Herramienta |
|---|---|---|
| `Penitente/` | `I1`, `I2`, `r1` … `r4`, `a1` … `a4`, `d2`, `d3` | Gemini (editando las `Bases/` del Eco, con la maestra `I1`) |
| `Artillero/` | `K1`, `K2`, `A1`, `F1`, `F2`, `R1`, `R2`, `G1`, `G2`, `H1`, `M1` … `M3` | Gemini (editando `K1`) |
| `Verdugo/` | `W1` … `W4`, `H1` … `H3`, `B1`, `B2`, `C1` … `C3`, `K1`, `K2`, `M1` … `M4` | Gemini (editando `W1`) |
| `Juez/` | `I1` … `I4`, `W1` … `W4`, `O1` … `O3`, `C1` … `C3`, `P1` … `P3`, `S1` … `S4`, `K1`, `K2`, `F1` … `F3`, `M1` … `M5` | **AI Studio** |
| `Placa/` | `Placa1`, `Placa2` | Gemini (editando `Placa/Bases/Placa_base.png`) |
| `Explosion/` | `Explosion1` … `Explosion4`, `Marca1`, `Marca2` | Gemini (editando la maestra del jefe) |
| `Fuego/` | `Ola1`, `Ola2` | Gemini (editando la maestra del jefe) |
| `Cadena/` | `Garfio`, `Cadena` | Gemini (editando la maestra del jefe) |
| `Bala/` | `Bala1` | Gemini (editando la maestra del jefe) |
| `Fragmento/` | `Fragmento1` | Gemini (editando el de la Tristeza) |

Cuando tengas un paquete completo, avísame. Lo reviso antes de integrarlo: el mismo tamaño de
píxel, la misma base y nada de damero. Si algo no cuadra, te digo qué repetir.

---

# Segunda tanda (2026-10-22): el escenario, las memorias, el fondo, las cinemáticas y las puertas

> **Recibida y montada entera** (E1 a H el 2026-10-22; I, J, K y las dos cinemáticas repetidas, el
> 2026-10-23): el tileset, los props, las coberturas, el barril, la mina, el agua, la alambrada, el
> campanario, las memorias con su losa, el fondo, las cinemáticas del Juez, las imágenes de las
> memorias, las puertas, la verja, el checkpoint, la salida, el escudo de Kael, el aviso y el abrigo
> vacío del Artillero y la onda de la sentencia. **No falta nada del Mundo 4.**
>
> **Corregido el 2026-10-22:** las piezas nuevas ya no adjuntan el tileset «para el píxel». Gemini lo
> editaba y el objeto salía entre sus piezas (la radio y la campana de E9). Ahora se piden sin adjuntar
> nada y en un chat nuevo; los fondos, editando cada uno la misma capa del Mundo 3. Ver abajo «Lo que
> aprendimos».

Lo que falta para vestir el Nivel 4 entero: **el escenario y sus mecánicas** (apartados E y F, lo
último de la fase E) y **la narrativa** (G a I, la fase F: las cinemáticas del Juez, las cuatro
memorias con su imagen y las puertas). Más el escudo de Kael (J), que se estrena en el Jardín, y
lo opcional (K): dos fotogramas del Artillero que no salieron y la onda de «La sentencia».

Las mismas reglas del apartado 0. Casi todo sale de **editar algo del Mundo 3** (sus hojas, su
agua, sus cinemáticas): así hereda el tamaño del píxel y la distribución, y el juego corta las
piezas en los mismos sitios.

**Las medidas.** Van en píxeles de arte a la densidad de los tilesets: **32 por unidad**. Kael mide
1,8 u (unos 58 píxeles a esa densidad). Si Gemini no respeta la medida no pasa nada mientras todos
los fotogramas de una pieza salgan de su maestra: la ajusto con los píxeles por unidad, sin
reescalar.

**Lo que aprendimos con el apartado C y con esta tanda,** que estos prompts ya llevan dentro:

- **Gemini edita lo que le adjuntas, aunque le digas que es «solo una referencia».** En el apartado C
  dejó al Juez en la placa y en `M5`, y la ola de fuego debajo del garfio. En esta tanda, el tileset
  adjunto «para el píxel» salió detrás de las coberturas (hubo que recortarlas), de la radio y de la
  campana, con sus piezas pegadas al objeto. Por eso **solo se adjunta lo que tiene que salir en la
  imagen**:
  - la maestra de la misma pieza, para sus otros fotogramas (plantilla de edición del apartado 0);
  - la misma pieza de otro mundo, para cambiarla entera («replace»): las hojas, el agua, los fondos;
  - en las cinemáticas, los personajes y los objetos que salen en el plano.
- **Las piezas nuevas se piden sin adjuntar nada**, con la plantilla de abajo. El tamaño del píxel va
  en palabras: el lienzo en píxeles de arte, y lo que mide la pieza en ellos.
- **Un chat nuevo para cada maestra, y otro para cada edición.** En el mismo chat, Gemini sigue
  editando la última imagen que vio, aunque no la adjuntes otra vez.
- **Ningún resplandor.** Lo que brilla (las memorias, la mecha, el escudo) se dibuja con píxeles
  sólidos: un borde ámbar y unas chispas. La luz la pone el juego.

### Plantilla de pieza nueva (sin adjuntar nada, en un chat nuevo)

```text
Low-resolution pixel art game sprite, side view. The canvas is <LIENZO> art pixels, and every art
pixel is drawn as one crisp, hard-edged square block, all of the same size (nearest-neighbour
upscale). Flat solid #FF00FF magenta background over the whole canvas.
The image contains ONLY ONE THING: <OBJETO>
Nothing else at all: no tiles, no ground strip, no platforms, no wooden beams, no other props, no
scenery, no frame. Centered, with a wide empty magenta margin on every side: nothing touches the
canvas edges.
Palette: <PALETA>
Hard-edged fully opaque pixels only. NO blur, NO glow halo, NO anti-aliasing, NO gradients, NO
transparency checkerboard, NO text, NO letters, NO border, NO floor shadow.
```

Cada apartado trae su prompt ya completo, con el lienzo, la pieza y la paleta puestos. Si Gemini no
respeta exacto el lienzo no pasa nada: lo que importa es que los píxeles sean bloques iguales y que
la pieza mida, en ellos, lo que dice el prompt.

**Orden recomendado:** primero las piezas sueltas (E9, I1 a I3 y J), en el orden que quieras: ya no
dependen de nada. Luego el fondo (F). Y al final las cinemáticas (G, H e I4), que adjuntan al Juez,
las memorias y la puerta.

---

## E. El escenario y las mecánicas (fase E)

### E1. Tileset del campo → `Mundo4/TileMap/TileMap4.png`

**Adjunta:** `Tesis/Imagenes/Mundo3/TileMap/Tileset.jpg` (el del bosque). Misma distribución: si
cada pieza queda en su sitio, el juego la corta igual que en el Mundo 3.

```text
Edit the attached pixel-art tileset sheet. Keep EXACTLY the same canvas, the same layout (every
piece in the same place and with the same size), the same art-pixel size and the same hard pixel
style. Keep the flat solid #FF00FF magenta background.

Change only the theme: the dark haunted forest becomes a BURNT BATTLEFIELD and a RUINED VILLAGE,
in a grey daylight under a sky of smoke.
Palette: greyish brown #3C2E1E, burnt brown #5C3D1E, dead olive green #4A5C2E, ash grey #7A7268,
old brick red-brown #6E3B2A, and faded red #8B3333 only for a few small details. No green moss,
no violet, no glowing fungus, no snow.

- The blocks of dark earth -> SCORCHED EARTH: dark grey-brown soil with grey ash, small stones,
  broken bits of brick and a few spent brass shell casings; the top edge of each block is a thin
  layer of grey ash and dry dead grass instead of moss.
- The floating pieces with roots hanging below -> the same shapes as broken CONCRETE SLABS of a
  blown-up bridge, with bent rusty iron rods hanging below instead of roots.
- The square block of tangled roots -> a square block of cracked grey STONE MASONRY (the old
  bridge and the church tower): big stones, mortar lines, two cracks, soot stains.
- The pile of dead branches and stones -> a heap of broken BRICKS and a split charred beam.
- The small swamp puddle -> a small SHELL CRATER with muddy brown water in it.
- The mossy branch platforms and their supports -> the charred wooden ROOF BEAMS of a burnt
  house, with a row of broken red-brown clay roof tiles along the top; the supports become
  charred rafters.
- The vertical tree trunks -> charred wooden posts.
- The torn rotten canvas -> a torn, burnt curtain hanging from a beam.
- The hanging vines -> sagging broken telegraph wires.
- The trench lamp -> a broken street lamp of the village, its glass shattered, unlit.
- The soldier's grave -> a small wooden cross made of two planks in a mound of earth, with one
  dull grey dog tag hanging from it.

Every block of earth and of stone masonry must tile seamlessly with copies of itself on all four
sides. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO gradients, NO transparency
checkerboard, NO text, NO border.
```

### E2. Decoración extra → `Mundo4/TileMap/Props4.png`

**Adjunta:** `Tesis/Imagenes/Mundo3/TileMap/Propts.jpg`, por lo mismo.

```text
Edit the attached props sheet. Keep EXACTLY the same canvas, the same layout (every prop in the
same place and about the same size), the same art-pixel size and the same hard pixel style, and
the flat solid #FF00FF magenta background. Change only the theme to a BURNT BATTLEFIELD and a
RUINED VILLAGE, with the palette greyish brown #3C2E1E, burnt brown #5C3D1E, dead olive green
#4A5C2E, ash grey #7A7268 and old brick red-brown #6E3B2A:
- The branch with the helmet and the canteen -> the broken crossbar of a telegraph pole with an
  old gas mask and a canteen hanging from it by their straps.
- The entrance of the trench dugout -> the front wall of a RUINED VILLAGE HOUSE: broken bricks
  and plaster, an empty window with a charred frame, an empty doorway, no roof left.
- The three tall ferns -> a barbed-wire obstacle: three wooden stakes crossed in an X with rusty
  barbed wire wound around them.
- The wooden ammunition crate -> the same crate, burnt brown, with a faded red stencilled band.
- The stack of sandbags -> the same stack, in dull olive and beige sandbags.
- The helmet lying in dead leaves -> an old helmet half sunk in grey mud.
- The dead tree stump -> a charred dead tree with its branches broken off.
- The fallen rotten log -> a fallen charred roof beam with a few bricks beside it.
No green moss, no violet, no glow. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO
gradients, NO transparency checkerboard, NO text, NO border.
```

### E3. Las coberturas (5) → `Mundo4/Coberturas/`

Paran las balas. **De pie, Kael asoma la cabeza por encima; agachado, queda debajo.** Su lomo es
plano porque también se pisan. Hoy son bloques de color: estos son sus dibujos.

| Archivo | En el juego | Píxeles de arte | Dónde |
|---|---|---|---|
| `Escombro.png` | 1 × 1,3 u | 32 × 42 | los tres del tutorial y los dos de la plaza |
| `SacosLargos.png` | 1,5 × 1,3 u | 48 × 42 | el refugio, antes de la tierra de nadie |
| `SacosBajos.png` | 0,8 × 0,9 u | 26 × 29 | delante de cada Artillero (tu decisión: más bajos que las demás) |
| `Chimenea.png` | 1 × 1,3 u | 32 × 42 | las dos del segundo tejado |
| `Tanque.png` | 3 × 2 u | 96 × 64 | el tanque volcado del campo: se sube de un salto |

*Recibidas y montadas (v0.60): llegaron con el tileset detrás y se recortaron.* Si repites alguna,
**no adjuntes nada**: cada una en un chat nuevo, con este prompt, cambiando `<OBJETO>` por su línea:

```text
Low-resolution pixel art game sprite, side view. The canvas is 128 × 128 art pixels, and every art
pixel is drawn as one crisp, hard-edged square block, all of the same size. Flat solid #FF00FF
magenta background over the whole canvas. The image contains ONLY this object, nothing else (no
tiles, no ground strip, no other props):
<OBJETO>
Its TOP must be FLAT on a single row, because the player stands on it, and its bottom rests on a
flat bottom row. Centered on the canvas with a wide magenta margin. Palette: greyish brown
#3C2E1E, burnt brown #5C3D1E, dead olive green #4A5C2E, ash grey #7A7268, old brick red-brown
#6E3B2A. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO gradients, NO transparency
checkerboard, NO text, NO border, NO floor shadow.
```

| Archivo | `<OBJETO>` |
|---|---|
| `Escombro.png` | «A low heap of RUBBLE, exactly 32 art pixels wide and 42 tall: a broken chunk of brick wall standing on a pile of bricks and stones, a bent iron rod sticking out of one side.» |
| `SacosLargos.png` | «A low wall of stacked SANDBAGS, exactly 48 art pixels wide and 42 tall: four rows of dull olive and beige sandbags, slightly bulging, a few torn with sand spilling.» |
| `SacosBajos.png` | «A small, low wall of SANDBAGS, exactly 26 art pixels wide and 29 tall: three rows of dull olive and beige sandbags. It is the cover of a kneeling gunner.» |
| `Chimenea.png` | «The broken stub of a brick CHIMNEY standing on a roof, exactly 32 art pixels wide and 42 tall: old red-brown bricks blackened by soot, its top edge broken off flat.» |
| `Tanque.png` | «An OVERTURNED old TANK lying on its side, exactly 96 art pixels wide and 64 tall: its rusty belly and one caterpillar track facing the viewer, the turret crushed against the ground on the right, the long gun barrel bent, a few holes and soot stains. Its upper side is flat.» |

### E4. El barril (4) → `Mundo4/Barril/Barril.png`, `Mecha1.png`, `Mecha2.png`, `Quemado.png`

Mide 0,7 × 1 u. Solo lo enciende el Ataque Cargado, otra explosión o una bala: entonces la mecha
parpadea en rojo 0,6 s y estalla (la explosión ya está: C2). **El rojo apagado es lo que hace
daño**: la franja del barril es la que avisa.

*Recibido y montado (v0.60).* Si repites la maestra, **no adjuntes nada** y pídela en un chat nuevo.

#### La maestra → `Barril.png`

```text
Low-resolution pixel art game sprite, side view. The canvas is 64 × 64 art pixels, and every art
pixel is drawn as one crisp, hard-edged square block, all of the same size. Flat solid #FF00FF
magenta background over the whole canvas. The image contains ONLY this object, nothing else (no
tiles, no ground strip, no other props):
An old rusty METAL BARREL standing upright, exactly 22 art pixels wide and 32 tall, centered on
the canvas, its bottom on a flat bottom row: dented dark iron grey and burnt brown #5C3D1E rust,
two raised rings around it, a wide faded red #8B3333 hazard band in the middle, and a short black
fuse sticking out of the cap on top. Leave about 12 art pixels of empty magenta ABOVE it.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text,
NO border, NO floor shadow.
```

#### Los demás, editando siempre `Barril.png` (plantilla del apartado 0)

| Archivo | Change |
|---|---|
| `Mecha1.png` | «the FUSE IS LIT: a small spark on the tip of the fuse, drawn with 3 or 4 solid pixels of pale yellow #FFE6A8 and orange; the red hazard band is BRIGHTER, a clear red #C04040» |
| `Mecha2.png` | «the FUSE IS LIT: the spark a little lower on the fuse and smaller, two tiny sparks jumping off it; the hazard band back to its dark faded red #8B3333» |
| `Quemado.png` | «BURST: the barrel is torn open at the top into jagged metal petals bent outward, the whole barrel scorched black, the band no longer visible, a thin wisp of grey smoke rising from it» |

### E5. La mina (2) → `Mundo4/Mina/Mina.png`, `Aviso.png`

Flota en el segundo ojo del puente, sujeta al fondo por su cadena, y sube y baja. Mide 0,8 u. Su
cadena es la del Verdugo (`Cadena`, C4), girada: no hace falta pedirla.

*Recibida y montada (v0.60).* Si la repites, **no adjuntes nada** y pídela en un chat nuevo.

```text
Low-resolution pixel art game sprite, side view. The canvas is 64 × 64 art pixels, and every art
pixel is drawn as one crisp, hard-edged square block, all of the same size. Flat solid #FF00FF
magenta background over the whole canvas. The image contains ONLY this object, nothing else (no
tiles, no ground strip, no other props):
An old rusty SEA MINE, exactly 26 art pixels wide and 26 tall including its horns, centered on
the canvas: a round iron ball in dark iron grey and burnt brown #5C3D1E rust, with six short
horns sticking out of it, a riveted band around its middle and a small iron ring at the bottom
where a chain would hang. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency
checkerboard, NO text, NO border.
```

Después, editándola: **`Aviso.png`:** «the same mine about to explode: the tips of its horns glow
red #C04040 with solid pixels, and the riveted band has a thin red line of light».

### E6. El agua del río (3) → `Mundo4/Agua/Agua1.jpg` … `Agua3.jpg`

La tira de la superficie, como la de la ciénaga: el juego la repite a lo ancho y pinta debajo el
cuerpo del agua. **Adjunta:** `Tesis/Imagenes/Mundo2/Agua/agua1.jpg`.

```text
Edit the attached image. Keep EXACTLY the same canvas size, the same flat #FF00FF magenta
background, the same art-pixel size (the same blocks), the same strip shape, the same position
and the same straight flat bottom row. Do NOT zoom in, do NOT crop, do NOT redraw it bigger.
Change ONLY the colours and the surface details: it is now the slow, MUDDY water of a river
crossing a burnt battlefield, not icy water. The top 3-4 rows are the surface: a thin broken line
of murky ochre-brown (#6B5A3A) with a few flakes of grey ASH (#9A9288) and one or two small bits
of charred wood floating on it. Below it, dark brown #3C2E1E water with a few darker #241A10
bands and two or three tiny bubbles. No blue, no green, no white foam.
The left and right edges must still match so the strip can repeat side by side.
Hard-edged fully opaque pixels only. NO blur, NO glow, NO gradient, NO transparency
checkerboard, NO text, NO border.
```

Después, con la plantilla de edición del apartado 0, **adjuntando siempre `Agua1`**:
**`Agua2`:** «the ash flakes and the ripples shifted 4 art pixels to the right».
**`Agua3`:** «the ash flakes and the ripples shifted 8 art pixels to the right».

### E7. La alambrada sumergida (4) → `Mundo4/Alambrada/Alambrada1.jpg`, `Alambrada2.jpg`, `Atrapa1.jpg`, `Atrapa2.jpg`

La maraña de la ciénaga, hecha alambre de espino: mide **2 u de ancho**, llena el ojo del puente
del fondo al techo (2 u de alto en el primero y 3 en el segundo) y sujeta a Kael si entra sin el
dash. **Por eso sus cuatro lados tienen que poder tocarse** con otra copia: el juego la repite
hacia arriba en el ojo alto.

**Adjunta:** `Tesis/Imagenes/Mundo3/Marana/marana1.jpg` (la maraña de raíces). Aquí sí se edita:
la maraña es una sola pieza y se cambia entera, sin nada que borrar.

#### La maestra → `Alambrada1.jpg`

```text
Edit the attached image. Keep EXACTLY the same canvas size, the same flat #FF00FF magenta
background, the same art-pixel size (the same blocks) and the same square in the same place.
Do NOT zoom in and do NOT make it bigger.
REPLACE the tangle of roots with a tangle of rusty BARBED WIRE under muddy water that fills the
same square: several strands of dark iron wire with sharp barbs every few pixels, looping and
crossing from the top edge of the square down to its bottom edge, wound around two broken
wooden stakes that lean inside the square; a torn scrap of olive cloth caught on one barb. There
are gaps between the strands where the magenta background shows through, so it reads as
something that grabs, not as a wall. Palette: dark iron grey #3A3A3A, rust #5C3D1E, a few paler
grey pixels on the barbs, greyish brown #3C2E1E for the stakes. No roots, no green, no violet.
All four sides of the square must be able to touch another copy of it with no visible seam.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text,
NO border. Nothing touches the canvas edges.
```

#### Los demás, editando siempre `Alambrada1.jpg` (plantilla del apartado 0)

- **`Alambrada2.jpg`:** «the loose strands of wire and the scrap of cloth swayed slightly to the
  right, as if moved by a slow current; the stakes do not move».
- **`Atrapa1.jpg`:** «the loose strands of wire TIGHTEN toward the centre of the square, like a
  snare closing; the gaps between them become smaller; the stakes do not move».
- **`Atrapa2.jpg`:** «the strands closed even tighter around the centre, almost no gaps left in
  the middle, the barbs pointing inward».

### E8. El campanario → `Mundo4/Campanario/Campanario.png`

La torre de la iglesia del pueblo, en primer plano, donde están los dos Artilleros que se turnan
sobre la tierra de nadie. **Sin campana**: la campana es la memoria 2. Las medidas son las del
juego, y aquí sí importan, porque Kael pasa por debajo y los Artilleros se arrodillan en el balcón:

- **la torre:** 3 u de ancho (96 píxeles de arte) y 12 u de alto (384), rota arriba;
- **el arco de abajo:** ocupa todo el ancho de la torre y mide 2,5 u (80 píxeles): Kael pasa por él;
- **el balcón:** sale 2 u (64 píxeles) a la IZQUIERDA de la torre, con el suelo a 4,5 u (144
  píxeles sobre la fila de abajo) y un parapeto bajo.

*Recibido y montado (v0.60), con el balcón, el arco y lo alto de la torre como salieron (sección 10
de `Nivel4_Diseno.md`).* Si lo repites, **no adjuntes nada** y pídelo en un chat nuevo.

```text
Low-resolution pixel art game sprite, side view. The canvas is 192 art pixels wide and 416 tall, and every art
pixel is drawn as one crisp, hard-edged square block, all of the same size. Flat solid #FF00FF
magenta background over the whole canvas. The image contains ONLY the tower, nothing else (no
tiles, no ground strip, no other props):
The BELL TOWER of a ruined village church, seen from the side, 160 art pixels wide in all:
- the TOWER is exactly 96 art pixels wide and stands on the RIGHT side of the canvas; its bottom
  rests on a flat bottom row and it rises 384 art pixels;
- at its base, an open ARCHWAY as wide as the whole tower and 80 art pixels tall: two narrow stone
  pillars at the sides and a round arch, empty in the middle (one can walk through it);
- on its LEFT side, a stone BALCONY sticking out 64 art pixels from the tower, its floor exactly
  144 art pixels above the bottom row, with a low stone parapet 16 art pixels tall along its
  edge and a stone bracket under it. The balcony is EMPTY;
- higher up, the empty belfry: a tall arched window with NO bell, only a broken wooden beam
  where the bell used to hang;
- the top of the tower is broken off irregularly, a few stones missing, soot stains and a long
  crack down its side.
Old grey stone masonry with mortar lines, ash grey #7A7268 and greyish brown #3C2E1E, soot black,
a few old brick red-brown #6E3B2A patches where the plaster fell off.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO gradients, NO transparency checkerboard,
NO text, NO border, NO sky, NO ground.
```

### E9. Las memorias y su losa (10) → `Mundo4/Memorias/`

> **Recibidas y montadas el 2026-10-22.** Salieron con más píxeles de arte de los pedidos (100 a lo
> ancho en lugar de 64): no pasa nada, cada una va a la escala de lo que se pidió. La encendida cae
> encima de la apagada píxel a píxel.

Cada memoria es un objeto del campo de batalla que se activa con **E**. **Apagada**, es un objeto
más; **brillando**, tiene un borde ámbar y unas chispas, para que se vea desde lejos que hay algo
que mirar. Al activarla se apaga. En la plaza, las cuatro esperan **sobre una losa** del suelo (la
marca del Juez): la losa se enciende cuando su ataque está en marcha.

| Archivo | El objeto | Píxeles de arte |
|---|---|---|
| `Radio1.png`, `Radio2.png` | una radio de campaña en el barro | 32 × 26 |
| `Campana1.png`, `Campana2.png` | una campana de bronce caída de lado entre cenizas | 34 × 30 |
| `Casco1.png`, `Casco2.png` | un casco sobre un fusil clavado, con una placa colgando | 20 × 48 |
| `Carta1.png`, `Carta2.png` | una carta sobre una caja de munición | 32 × 22 |
| `Losa1.png`, `Losa2.png` | la losa de la plaza, apagada y encendida | 46 × 8 |

**No adjuntes nada.** Las tres que llegaron el 2026-10-22 (`Radio.jpg`, `Campana.jpg` y `Campana
Radio.jpg`) salieron dentro del tileset adjunto, con sus piezas alrededor y pegadas al objeto:
repítelas así. La radio y la campana estaban bien dibujadas, y el prompt ya las describe como
salieron. Primero la apagada, **cada una en un chat nuevo**, cambiando `<OBJETO>` por su línea:

```text
Low-resolution pixel art game sprite, side view. The canvas is 64 × 64 art pixels, and every art
pixel is drawn as one crisp, hard-edged square block, all of the same size (nearest-neighbour
upscale). Flat solid #FF00FF magenta background over the whole canvas.
The image contains ONLY ONE THING: <OBJETO>
Nothing else at all: no tiles, no ground strip, no platforms, no wooden beams, no lanterns, no
crosses, no banners, no other props, no scenery. Its bottom rests on a flat bottom row. Centered,
with a wide empty magenta margin on every side: nothing touches the canvas edges.
Palette: greyish brown #3C2E1E, burnt brown #5C3D1E, dead olive green #4A5C2E, ash grey
#7A7268, dull grey #8A8A80 for metal tags. Hard-edged fully opaque pixels only. NO blur, NO glow
halo, NO anti-aliasing, NO transparency checkerboard, NO text, NO letters, NO border, NO floor
shadow.
```

| Archivo | `<OBJETO>` |
|---|---|
| `Radio1.png` | «An old military FIELD RADIO half sunk in mud, about 32 art pixels wide and 26 tall: a dented olive box with two round dials and a few switches, a bent antenna, and its handset lying beside it on its coiled cord.» |
| `Campana1.png` | «A church BELL lying on its side in a heap of grey ash, about 34 art pixels wide and 30 tall: dull bronze (#7A5A30, darker #4A3418), a crack running up from its rim, its clapper hanging out.» |
| `Casco1.png` | «A soldier's grave marker, about 20 art pixels wide and 48 tall: a RIFLE stuck bayonet-down in the ground, an old dented HELMET resting on its stock, and one dull grey DOG TAG hanging from it on a thin chain.» |
| `Carta1.png` | «A folded LETTER lying on top of a small wooden AMMUNITION CRATE, about 32 art pixels wide and 22 tall: the crate burnt brown with rope handles; the letter off-white #D8D0C0 paper, slightly crumpled, with a few short grey lines that cannot be read.» |
| `Losa1.png` | «A flat STONE SLAB set into the ground, seen from the side: exactly 46 art pixels wide and 8 tall, ash grey #7A7268 with darker edges, a thin carved groove along its top surface. It is level with the ground.» |

Después, la que brilla, con el mismo nombre y un **2**: **en un chat nuevo, adjuntando solo su
apagada**, con la plantilla de edición del apartado 0 y este cambio:

```text
Change: the object now GLOWS AS A MEMORY: a 1-art-pixel rim of amber #E0B060 along its upper
edges, its brightest parts near-white #FFE6A8, and four or five tiny amber sparkles of 1 art pixel
floating just above it. Drawn with solid pixels only: NO glow halo, NO blur, NO soft light around
it. The object itself does not change shape or colours.
```

Para **`Losa2.png`**: «the carved groove along the top glows amber #E0B060, with a near-white
#FFE6A8 line in its middle, drawn with solid pixels; nothing else changes».

---

## F. El fondo (parallax, como el de los otros mundos)

> **Recibido y montado el 2026-10-22.** El primer plano de abajo trajo el cielo de nubes con lluvia de
> la capa del Mundo 3, como le pasó a esa: se borró al limpiarlo, y se quedan la alambrada, las estacas y
> la hierba.

**Cada capa, editando la misma capa del Mundo 3** (`Tesis/Imagenes/Mundo3/Fondos/`), que se cambia
entera: así hereda el tamaño del píxel, el formato (16:9) y dónde va el magenta. (Antes se adjuntaba
el cielo para todas, y Gemini habría dejado el cielo detrás de cada capa.) **Cada una en un chat
nuevo, adjuntando solo su capa del Mundo 3.** Las capas que se ven a través de otras llevan magenta
en lo que debe quedar transparente; el cielo es opaco. **Aquí es de día**: lo que importa es que se
lea el humo y que nada compita con la luz ámbar de las memorias.

Todas empiezan así:

```text
Edit the attached pixel art parallax layer. Keep EXACTLY the same canvas size, the same art-pixel
size (the same blocks) and the same 16:9 format. REPLACE EVERYTHING drawn in it: nothing of the old
forest may remain (no trees, no moon, no mist, no ferns, no fungus lights). Draw instead:
```

| Archivo | Adjunta (del Mundo 3) | Y sigue con |
|---|---|---|
| `Fondos/Cielo.jpg` | `Cielo.jpg` | `A SKY OF SMOKE over a burnt battlefield in grey daylight: low heavy clouds of grey-brown smoke (#5C5248, #7A7268), a pale dirty ochre glow (#B8A078) low on the horizon where the sun is hidden, a few darker columns of smoke rising far away. Fully opaque, no ground. The left and right edges must match so it can repeat horizontally. Hard-edged pixels, NO blur, NO gradient banding, NO text.` |
| `Fondos/PuebloLejano.jpg` | `BosqueLejano.jpg` | `The distant silhouette of a RUINED VILLAGE on low hills: broken roofs, walls with empty windows, a few bare burnt trees and thin smoke rising, all in flat dark greyish brown (#3C2E1E, #4A3F33). Everything above the skyline is flat solid #FF00FF magenta. The left and right edges must match. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/CampoCercano.jpg` | `BosqueCercano.jpg` | `A closer, bigger band of the battlefield: shattered tree trunks, a broken wall, a toppled cart wheel, sagging telegraph poles and a line of barbed-wire stakes, dark greyish brown and burnt brown (#3C2E1E, #5C3D1E) with a little dead olive green #4A5C2E. Above them, flat solid #FF00FF magenta. The left and right edges must match. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/Campanario.jpg` | `ArbolHueco.jpg` | `The BROKEN BELL TOWER of the village church seen from far away, much taller than anything else, its top broken off and its belfry empty (no bell), smoke drifting past it. A landmark seen from far away. Flat solid #FF00FF magenta around it. Hard-edged pixels, NO blur, NO text.` *(Es el hito, como la fortaleza, la torre y el árbol hueco: se ve desde el inicio. Es el mismo campanario de E8, visto de lejos.)* |
| `Fondos/Humo.jpg` | `Niebla.jpg` | `A horizontal band of low drifting SMOKE and dust along the bottom third, drawn with solid pixels in two or three tones of grey-brown (#5C5248, #7A7268), with a wavy top edge. Everything else flat solid #FF00FF magenta. The left and right edges must match. Hard-edged pixels, NO blur, NO soft gradients, NO text.` |
| `Fondos/PrimerPlanoArriba.jpg` *(opcional)* | `PrimerPlanoArriba.jpg` | `Sagging broken telegraph wires and a few hanging scraps of burnt cloth along the top edge, black and dark brown. Everything else flat solid #FF00FF magenta. Edges match for repeating. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/PrimerPlanoAbajo.jpg` *(opcional)* | `PrimerPlanoAbajo.jpg` | `Tangled barbed wire on wooden stakes, broken planks and tufts of burnt grass rising from the bottom edge, black and dark brown. Everything else flat solid #FF00FF magenta. Edges match for repeating. Hard-edged pixels, NO blur, NO text.` |

---

## G. Las cinemáticas del Juez (480 × 268, fase F)

> **Recibidas y montadas** (2026-10-22). `PresentacionJuez2` y `DerrotaJuez1` salieron con restos de la
> imagen del Mundo 3 que se adjuntaba para el lienzo; se repitieron el 2026-10-23 adjuntando
> `PresentacionJuez1.jpg` en su lugar, y ya son del pueblo.

Como las del Pavor: la presentación al cruzar la verja de la plaza y la derrota.

**Adjunta, en este orden:**
1. `Tesis/Imagenes/Mundo3/Cinematicas/DerrotaEspectro2.jpg` (el lienzo y el tamaño del píxel de
   las cinemáticas a 480);
2. `Tesis/Imagenes/Mundo3/Cinematicas/SalidaMiedo2.jpg` (Kael, de espaldas, con sus tres
   fragmentos);
3. `Tesis/Imagenes/Mundo4/Juez/I1.png` (el jefe).

Todas empiezan así:

```text
Pixel art cinematic still, 16:9. EXACTLY the same canvas and the same art-pixel size (the same
blocks) as the FIRST attached image, and the same style. Repaint the WHOLE canvas: nothing of the
first image's scene may remain, it only gives the canvas and the pixel size. From the SECOND and
THIRD images take ONLY the characters, never their backgrounds. Palette: greyish brown #3C2E1E, burnt
brown #5C3D1E, dead olive green #4A5C2E, ash grey #7A7268, faded red #8B3333, and amber #E0B060
(near-white #FFE6A8 at its brightest) ONLY for what is remembered: the light in the guardian's
chest and the memories. The hero is the one in the SECOND attached image: same design and same
dark worn coat, with his three glowing shards (orange, light blue and violet). The guardian is
the one in the THIRD attached image: the smooth faceless helmet, the cloak of dog tags, the
scales and the long straight sword. Hard-edged pixels, NO blur, NO text, NO border, NO
letterbox bars.
```

Y siguen con:

| Archivo | Plano |
|---|---|
| `Cinematicas/PresentacionJuez1.jpg` | «The square of a ruined village in grey daylight under a sky of smoke: broken house walls around it, the stump of the bell tower behind them. On the ground of the square, four flat stone slabs in a row. At the far end, in the centre, the guardian stands perfectly still, small in the distance, the amber light in his chest the only warm thing in the image. The hero, tiny, at the bottom left, seen from behind, entering the square.» |
| `Cinematicas/PresentacionJuez2.jpg` | «Close-up of the guardian from the chest up, facing the viewer: the smooth faceless helmet, the amber light glowing in his chest, the scales raised in front of him at the height of his face, perfectly level. Dozens of dull grey dog tags hang from his cloak. Grey ash falls around him as single pixels.» |
| `Cinematicas/DerrotaJuez1.jpg` | «The guardian kneeling in the centre of the square, the helmet cracked, amber light leaking from the crack; his sword lies flat on the ground and his scales rest on it, tipped. Around him the dog tags fall from his cloak like rain. The four stone slabs on the ground glow amber. The hero, tiny, at the bottom left, watching.» |
| `Cinematicas/DerrotaJuez2.jpg` | «The same square after the fight, empty: only a low heap of the cloak and the dog tags where the guardian was. The smoke is thinning and a soft warm light falls on the square for the first time. The hero, tiny, in the centre, holding up a small faded red gem with lighter red facets that glows. Nobody else is in the image.» |

---

## H. Las cuatro memorias (480 × 268, fase F)

> **Recibidas y montadas el 2026-10-22**: la cinemática de cada memoria y la pared del Juez.

La imagen de cada memoria: la de su cinemática (unos 5 s, con la línea de Kael encima) y la que el
Juez proyecta en la pared de la plaza, grande y desvaída. **Son recuerdos del soldado, no de Kael:**
no sale Kael, y del soldado solo se ven las manos o la espalda, nunca la cara. **Colores
apagados, casi sepia; solo lo que se recuerda va en ámbar.**

**Adjunta:** `Tesis/Imagenes/Mundo3/Cinematicas/DerrotaEspectro2.jpg` (el lienzo y el píxel) y,
cuando la tengas, la imagen apagada de esa memoria (`Memorias/Radio1.png`, …), para que sea el
mismo objeto.

```text
Pixel art cinematic still, 16:9. EXACTLY the same canvas and the same art-pixel size (the same
blocks) as the FIRST attached image, and the same style. Repaint the WHOLE canvas: nothing of the
first image's scene may remain, it only gives the canvas and the pixel size. It is a MEMORY: muted, almost sepia
colours (greyish brown #3C2E1E, burnt brown #5C3D1E, dead olive green #4A5C2E, ash grey
#7A7268, dusty ochre #B8A078), and amber #E0B060 (near-white #FFE6A8 at its brightest) ONLY for
the object that is remembered, which is the one in the SECOND attached image (take only the object,
not its magenta background). A soldier may
appear, but NEVER his face: only his hands, his back or his boots. Hard-edged pixels, NO blur,
NO text, NO letters, NO border, NO letterbox bars.
```

| Archivo | La memoria | Plano |
|---|---|---|
| `Memorias/LaOrden.jpg` | «Una radio entre el barro. "Adelante"…» | «Close shot of a muddy trench at dusk. A field radio stands on a crate; a soldier's gloved hand holds its handset to the edge of the image, where his helmet is seen from behind. The radio's dials glow amber. In the distance, beyond the trench, the roofs of a village under a grey sky.» |
| `Memorias/ElPueblo.jpg` | «Una campana caída entre cenizas…» | «The burnt village square seen from the ground: a church bell lies on its side in grey ash in the foreground, its rim lit amber; behind it, the empty houses with black windows and the bell tower without its bell. Smoke. Nobody is there.» |
| `Memorias/ElCompanero.jpg` | «Una placa con un nombre que no es el suyo…» | «A rifle stuck bayonet-down in a mound of earth with a helmet on its stock, in a grey field. In the foreground, a soldier's hand holds a dog tag on a chain, taken from the rifle; the dog tag catches amber light. Only the hand and the sleeve are seen.» |
| `Memorias/LaCarta.jpg` | «Una carta que solo dice "Lo siento"…» | «Inside a dark ruined room at night, a candle stub on an ammunition crate. On the crate, a letter and several crumpled sheets of paper; the letter is lit amber by the candle, its lines unreadable. A soldier's hand rests beside it, holding a pencil. Only the hand is seen.» |

---

## I. Las puertas de este mundo (como las de los otros tres)

### Decisiones mías, por si quieres cambiarlas antes de generar

- **El emblema es una balanza, inclinada mientras la puerta está cerrada, que se nivela al
  abrirse.** Es la segunda frase del cierre: «Nadie las borra. Por primera vez, alguien las
  sostiene».
- **La puerta es la de la iglesia quemada del pueblo**, cerrada con cadenas de las que cuelgan
  placas de identificación, como la capa del Juez.
- **Por la puerta abierta se ve el mundo siguiente:** la luz dorada y blanca del Jardín de la
  Esperanza, con algún pétalo.
- **El checkpoint es una bandera blanca** atada a un fusil roto clavado en los escombros: caída
  cuando está apagado; levantada por el viento, con una vela encendida al pie, cuando está
  encendido. Es la tregua: aquí se descansa.
- **La verja de la plaza es un muro de cadenas con placas colgando** sobre una reja de hierro
  doblada.
- **Los huecos de Kael:** al salir de la Culpa, el fragmento rojo tapa **la pantorrilla izquierda**
  y queda un hueco, la pantorrilla derecha. Lo elegí yo, como en los otros mundos.

### I1. La puerta → `Mundo4/Puerta/Puerta1.jpg` … `Puerta4.jpg`

**No adjuntes nada**, y pídela en un chat nuevo:

```text
Low-resolution pixel art game sprite for a 2D side-view platformer. The canvas is 160 × 160 art
pixels, and every art pixel is drawn as one crisp, hard-edged square block, all of the same size.
Flat solid #FF00FF magenta background over the whole canvas. The image contains ONLY the door
described below: no tiles, no wall around it, no ground, no other props.

The DOOR OF A BURNT VILLAGE CHURCH, seen straight from the front, CLOSED. About 96 art pixels
wide and 128 art pixels tall, centered on the canvas, standing on its own flat bottom row. A
pointed stone arch of grey masonry blackened by soot. Inside it, two charred wooden door leaves
with iron studs, held shut by heavy rusty CHAINS crossed over the seam, with a dozen small dull
grey #8A8A80 dog tags hanging from the chains. On the keystone of the arch, a carved emblem of a
pair of SCALES, clearly TIPPED to one side.
Palette: greyish brown #3C2E1E, burnt brown #5C3D1E, ash grey #7A7268, dead olive green #4A5C2E,
dull grey #8A8A80. No warm light.

Nothing below the bottom row: no ground, no rubble on the floor. Nothing touches the canvas edges.
Hard-edged fully opaque pixels only. NO blur, NO glow halo, NO gradient, NO transparency
checkerboard, NO text, NO border, NO shadow on the floor.
```

Después, **cada una en un chat nuevo, adjuntando solo `Puerta1.jpg`**, con la plantilla de edición de
`Prompts_Puertas.md`:

- **`Puerta2.jpg`, abriéndose:** «the chains have fallen to the foot of the door, the dog tags
  lying among them; the door leaves are ajar: the vertical gap in the middle shows a band of soft
  light drawn as flat solid colours (warm white #FFF4D8 in the centre, pale gold #F0D890, soft
  green #A8C890 at the sides). The carved scales on the keystone are now almost level.»
- **`Puerta3.jpg`, abierta:** «the door leaves are fully open and hidden behind the arch: the
  whole doorway is filled with the soft light of a garden drawn as flat solid bands (warm white
  #FFF4D8 in the centre, then pale gold #F0D890, then soft green #A8C890 next to the arch), with
  four or five pale pink petal pixels drifting out of it. The carved scales on the keystone are
  perfectly LEVEL.»
- **`Puerta4.jpg`, abierta (variante):** «the same open door as described for the open state.
  Move the bands of light one or two art pixels and put the petal pixels in different places, so
  this image and the previous one can flicker in a loop.» *(Adjunta `Puerta3.jpg` y edítala.)*

### I2. La verja de la plaza → `Mundo4/Verja/Verja.jpg`

Se cierra detrás de Kael al cruzar a la plaza: 1 u de ancho y repetida hacia arriba. **No adjuntes
nada**, y pídela en un chat nuevo:

```text
Low-resolution pixel art game sprite, front view. The canvas is 128 × 128 art pixels, and every
art pixel is drawn as one crisp, hard-edged square block, all of the same size. Flat solid #FF00FF
magenta background over the whole canvas. The image contains ONLY a VERTICAL STRIP OF A HEAVY
GATE, 32 art pixels wide and 96 art pixels tall, centered on the canvas, and nothing else (no
tiles, no wall, no ground, no other props). Its top edge and its bottom edge must match exactly,
so the strip can be repeated upwards with no visible seam.
A curtain of heavy rusty CHAINS hanging straight down over a bent iron fence, with dozens of small
dull grey #8A8A80 dog tags hanging from the chains. Dark iron grey and burnt brown #5C3D1E rust.
No warm colours.
Hard-edged fully opaque pixels only. NO blur, NO glow halo, NO gradient, NO transparency
checkerboard, NO text, NO border.
```

### I3. El checkpoint → `Mundo4/Checkpoint/Checkpoint1.jpg`, `Checkpoint2.jpg`

Mide unas 0,9 × 2,8 u (unos 29 × 90 píxeles de arte). **No adjuntes nada**, y pide el apagado en un
chat nuevo:

```text
Low-resolution pixel art game sprite, side view. The canvas is 128 × 128 art pixels, and every art
pixel is drawn as one crisp, hard-edged square block, all of the same size. Flat solid #FF00FF
magenta background over the whole canvas. The image contains ONLY a CHECKPOINT MARKER standing on
its own flat bottom row, about 29 art pixels wide and 90 art pixels tall, centered on the canvas,
and nothing else (no tiles, no ground strip, no other props): a broken RIFLE stuck upright in a
small heap of rubble, with a torn WHITE FLAG of truce (off-white #E8E2D4 with grey folds) tied to
it, hanging limp; an unlit candle stub on a stone at its foot.
Nothing below the bottom row. Hard-edged fully opaque pixels only. NO blur, NO glow halo, NO
gradient, NO transparency checkerboard, NO text, NO border.
```

Y el encendido, **en un chat nuevo, adjuntando solo `Checkpoint1.jpg`**, con la plantilla de edición
del apartado 0: «the white flag is LIFTED by the wind and flies to the right; the candle at its
foot is LIT with a tiny amber #E0B060 flame of solid pixels».

### I4. La cinemática de salida (480 × 268) → `Mundo4/Cinematicas/SalidaCulpa1.jpg`, `SalidaCulpa2.jpg`

**Pídela cuando tengas `Puerta3.jpg` y las cinemáticas del jefe.**

**Adjunta, en este orden:**
1. `Mundo4/Cinematicas/DerrotaJuez2.jpg` (la plaza con luz y el tamaño del píxel);
2. `Mundo3/Cinematicas/SalidaMiedo2.jpg` (Kael, de espaldas);
3. `Mundo4/Puerta/Puerta3.jpg` (la puerta abierta).

Las dos empiezan así:

```text
Pixel art cinematic still, 16:9. EXACTLY the same canvas and the same art-pixel size (the same
blocks) as the FIRST attached image, and the same style and palette. From the SECOND and THIRD
images take ONLY the hero and the door, never their backgrounds. The hero is the one in the
SECOND attached image: same design and same dark worn coat. He now carries FOUR glowing shards:
the orange one on his left shoulder, the light blue one on his left forearm, the dark violet one
on his right elbow and a faded red one with lighter red facets that fills the hole in his LEFT
CALF. Exactly ONE empty hole remains in his body: his right calf. The door is the one in the
THIRD attached image, with its level scales on the keystone. Hard-edged pixels, NO blur, NO
text, NO border, NO letterbox bars.
```

| Archivo | Frases que lleva encima | Plano |
|---|---|---|
| `SalidaCulpa1.jpg` | «Las memorias se encienden una a una sobre la plaza.» y «Nadie las borra. Por primera vez, alguien las sostiene.» | «Wide shot of the village square after the fight, in soft warm light, the smoke thinning. On the ground, the four stone slabs glow amber, and above each one floats, faint, the image of its memory: a radio, a bell, a helmet on a rifle and a letter. On the right third of the image the church door stands OPEN, soft golden-white light pouring out of it. The hero, tiny, at the bottom left, seen from behind, walking toward it.» |
| `SalidaCulpa2.jpg` | «Kael integra el Fragmento de la Culpa. Un escudo que no niega el golpe: lo resiste.» | «Close shot from behind the hero as he steps through the open church door: his dark silhouette, from the knees up, in the left-centre of the image, framed by the stone arch. In front of him the doorway, full of soft golden-white light, with green leaves and a few pale pink petals. The four shards glow on him: orange, light blue, violet and faded red. Behind him, at the edges, the grey ruins.» |

---

## J. El escudo de Kael (3) → `Tesis/Imagenes Kael/Escudo/Escudo1.png` … `Escudo3.png`

La **Defensa Reforzada**, que Kael gana aquí: un escudo que para un golpe cada 15 s. Se estrena en
el Jardín, pero su dibujo sale de este mundo («un escudo que no niega el golpe: lo resiste»):
**placas de hierro** solapadas, como las de los Penitentes y el Juez, con el borde rojo apagado del
fragmento. Va delante de Kael, así que se pide **sin Kael**.

**No adjuntes nada**: adjuntando a Kael «solo para el tamaño», saldría Kael. El tamaño va en
píxeles de arte de Kael, que mide unos 115 de alto. Pídelo en un chat nuevo:

```text
Low-resolution pixel art game sprite, side view. The canvas is 160 × 160 art pixels, and every art
pixel is drawn as one crisp, hard-edged square block, all of the same size. Flat solid #FF00FF
magenta background over the whole canvas. The image contains ONLY a protective barrier: NO
character, NO hero, nothing else.
A protective BARRIER facing RIGHT: a tall curved arc of small overlapping IRON PLATES, like dog
tags or scales of armour, about 112 art pixels tall and 28 art pixels wide, its concave side
facing LEFT (in the game a hero stands there; do NOT draw him). Dark iron grey plates with lighter edges, a 1-art-pixel faded red #8B3333 rim along the
outer edge, and a few amber #E0B060 pixels where the plates catch the light. Centered on the
canvas. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO
text, NO border.
```

Después, **cada uno en un chat nuevo, adjuntando solo `Escudo1.png`** (plantilla de edición del
apartado 0):
- **`Escudo2.png`, para el golpe:** «the barrier STOPS A BLOW: the plates in the middle are pushed
  back one art pixel, a short burst of 6 to 8 near-white #FFE6A8 sparks flies off the outer side
  at chest height, and two cracks run across the middle plates».
- **`Escudo3.png`, se rompe:** «the barrier BREAKS: the plates come loose and scatter outward to
  the RIGHT as separate pieces with gaps between them, a few falling, the red rim broken into
  bits».

---

## K. Opcional: lo que le falta al Artillero y al Juez

### K1. El Artillero

Dos fotogramas no salieron con su pose y se apañan con otros (`limpiar_mundo4.py`, docstring): el
**aviso del culatazo** (hoy enseña el reposo un instante) y el **abrigo vacío** del final de la
muerte (hoy se queda tendido y se desvanece). Si los quieres, pídelos **adjuntando `K1.png`**, con
la plantilla del apartado 0 y la línea de la máscara de A2:

- **`G1.png`:** «WARNING before a rifle-butt strike: he has TURNED THE RIFLE AROUND in his hands;
  the wooden STOCK is now raised high toward the LEFT, at the height of his helmet, and the barrel
  points back over his right shoulder. He is still kneeling.»
- **`M3.png`:** «Nobody is left: only his EMPTY COAT lying flat on the floor, the helmet beside it
  and the rifle across it, a few dull grey dog tags spilled around and a little grey ash rising.
  In this image, the mask may be hidden.»

### K2. La onda de «La sentencia» (2) → `Mundo4/Onda/Onda1.png`, `Onda2.png`

Es el único ataque del Juez que todavía no tiene dibujo: al clavar la espada, una onda sale por el
suelo hacia los dos lados y hay que saltarla. Mide **0,8 u de alto**: hoy es un rectángulo rojo. Se
pide **editando la ola de fuego** (`Fuego/Ola1`), que ya tiene el tamaño y el píxel de una onda que
avanza por el suelo: es una sola pieza, así que se cambia entera.

**Adjunta:** `Tesis/Imagenes/Mundo4/Fuego/Ola1.jpg`.

```text
Edit the attached image. Keep EXACTLY the same canvas size, the same flat #FF00FF magenta
background, the same art-pixel size (the same blocks) and the same flat bottom row, in the same
place. Do NOT zoom in and do NOT make it bigger.
REPLACE the wall of fire with a SHOCKWAVE of the ground rolling to the LEFT: a low wave of broken
earth, stones and grey dust, about TWO THIRDS as tall as the fire was, its front on the left
curling over like a breaking wave, chunks of earth jumping out of it, and a trail of settling dust
behind it on the right. Palette: greyish brown #3C2E1E, burnt brown #5C3D1E, ash grey #7A7268, and
a thin faded red #8B3333 crack of light along the ground under its front. No fire, no orange.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO soft edges, NO gradient, NO transparency
checkerboard, NO text.
```

Después, editando **`Onda1`**: **`Onda2`:** «the same shockwave a moment later: the front curled a
little further, the stones and the dust in other places».

---

## L. Dónde va cada cosa (segunda tanda)

| Carpeta en `Tesis/Imagenes/Mundo4/` | Archivos | Herramienta |
|---|---|---|
| `TileMap/` | `TileMap4`, `Props4` | Gemini (editando las hojas del Mundo 3) |
| `Coberturas/` | `Escombro`, `SacosLargos`, `SacosBajos`, `Chimenea`, `Tanque` | Gemini |
| `Barril/` | `Barril`, `Mecha1`, `Mecha2`, `Quemado` | Gemini (editando `Barril`) |
| `Mina/` | `Mina`, `Aviso` | Gemini (editando `Mina`) |
| `Agua/` | `Agua1` … `Agua3` | Gemini (editando el agua del Mundo 2) |
| `Alambrada/` | `Alambrada1`, `Alambrada2`, `Atrapa1`, `Atrapa2` | Gemini (editando la maraña del Mundo 3) |
| `Campanario/` | `Campanario` | Gemini |
| `Memorias/` | `Radio1`, `Radio2`, `Campana1`, `Campana2`, `Casco1`, `Casco2`, `Carta1`, `Carta2`, `Losa1`, `Losa2`, y las cuatro imágenes `LaOrden`, `ElPueblo`, `ElCompanero`, `LaCarta` | Gemini (las apagadas, sin adjuntar nada; las que brillan, editando su apagada) |
| `Fondos/` | `Cielo`, `PuebloLejano`, `CampoCercano`, `Campanario`, `Humo` y, opcionales, `PrimerPlanoArriba` y `PrimerPlanoAbajo` | Gemini (editando cada una la misma capa del Mundo 3) |
| `Cinematicas/` | `PresentacionJuez1`, `PresentacionJuez2`, `DerrotaJuez1`, `DerrotaJuez2`, `SalidaCulpa1`, `SalidaCulpa2` | Gemini |
| `Puerta/` | `Puerta1` … `Puerta4` | Gemini (`Puerta1` sin adjuntar nada; las demás, editándola) |
| `Verja/` | `Verja` | Gemini (sin adjuntar nada) |
| `Checkpoint/` | `Checkpoint1`, `Checkpoint2` | Gemini (`Checkpoint1` sin adjuntar nada; `Checkpoint2`, editándolo) |
| `Artillero/` *(opcional)* | `G1`, `M3` | Gemini (editando `K1`) |
| `Onda/` *(opcional)* | `Onda1`, `Onda2` | Gemini (editando la ola de fuego) |
| `Tesis/Imagenes Kael/Escudo/` | `Escudo1` … `Escudo3` | Gemini (`Escudo1` sin adjuntar nada; los otros dos, editándolo) |

**Cuando tengas un paquete completo, avísame.** Lo limpio y lo monto como el primero: cada pieza
con su píxel y su base, nada de damero, y si algo no cuadra, te digo qué repetir.
