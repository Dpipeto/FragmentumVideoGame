# Prompts del Mundo 3 · Miedo

Todas las imágenes de `Nivel3_Diseno.md` (sección 8), en dos tandas:

- **Primera tanda (2026-10-13): los enemigos y el jefe**, apartados A a D. El Espectro del Pavor va
  por **Google AI Studio** (apartado B); todo lo demás, por **Gemini**.
- **Segunda tanda (2026-10-14), con el diseño ya aprobado: el escenario, las mecánicas, el fondo,
  las cinemáticas y las puertas**, apartados E a J. Todo por Gemini.

El diseño se aprobó entero el 2026-10-14 («haz lo que recomiendas»): el Centinela Ciego con su
máscara de gas y el Acechador se pueden pedir ya, como todo lo demás.

Los prompts están en inglés porque Gemini sigue mejor las medidas así. Las explicaciones van en
español.

---

## 0. Cómo pedirlas

Las reglas del Mundo 2 (`Prompts_Mundo2.md`, apartado 0) siguen valiendo:

1. **Una imagen por mensaje.**
2. **Fondo magenta plano (`#FF00FF`).** Nunca el damero gris.
3. **Primero la maestra; los demás fotogramas, editándola.** Adjunta siempre la maestra, nunca el
   fotograma anterior: el error se acumula.
4. **Guárdalas en disco**, en `Tesis/Imagenes/Mundo3/<Pieza>/` y con el nombre que indica cada
   apartado.
5. **Si una sale mal, repítela** editando otra vez la maestra.

Y lo que aprendimos con el Mundo 2, que los prompts de aquí ya llevan dentro:

- **Cada maestra tiene todo lo que no puede faltar, y cada prompt lo enumera.** Al Lamento le
  desaparecían los ojos porque la base no los tenía: lo que la maestra no tiene, sus ediciones
  tampoco.
- **Que no cambie el tamaño del píxel.** En el ataque del Lamento, Gemini devolvió lienzos de
  2390 × 1760 y cada fotograma con su propio píxel; en el barrido de la Plañidera, un píxel un
  23 % más fino. Por eso todos llevan «Do NOT zoom in». Aun así, **mira cada imagen antes de
  pedir la siguiente**: si la figura sale más grande o más fina, repítela.
- **Margen alrededor de la figura.** A la Doncella el lienzo le cortaba la cola.
- **No adjuntes imágenes con el damero pintado**: usa las bases que preparé.

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

Negro profundo `#0A0A0F`, violeta oscuro `#2D1B4E`, verde pantanoso `#1A2E1A` y gris espectral
`#3A3A4A` (Anexo A), más un **verde pálido `#7FE6B0`** solo para lo que brilla: las vetas del jefe,
los ojos de los Espectros cuando atacan y el aliento del Acechador. **Nada naranja:** el único
color cálido del mundo será la luz de las antorchas.

---

## A. Los enemigos

### A1. Espectro: el Eco del Miedo → `Mundo3/Espectro/` con los nombres del Eco

Como el Lamento: en `Referencias/Arte/EnemigosNiveles.jpeg` está la familia de los Ecos, uno por
mundo, todos con el mismo cuerpo. **El verde que se deshace en polvo negro (el tercero de la fila
de arriba) es el del Miedo.** No se diseña de cero: **se hace editando cada fotograma del Eco de
la Ira**. Así conserva su pose, su lienzo y su pivote, y entra con el mismo controlador de
animación.

**Las bases ya están** en `Tesis/Imagenes/Mundo3/Espectro/Bases/` (`I1.png` … `d3.png`): los 12
fotogramas que usa el juego, sobre magenta y con el mismo lienzo (1229 × 905). Usa esas.

En el juego es casi invisible y **solo se ve del todo cuando ataca**. Por eso, en los cuatro
fotogramas del ataque se le encienden los ojos: es la señal de que va a golpear.

#### Paso 1: la maestra → `Mundo3/Espectro/I1.png`

**Adjunta:** `Bases/I1.png` y `Referencias/Arte/EnemigosNiveles.jpeg`, en ese orden.

```text
Edit the FIRST attached image. Keep EXACTLY the same canvas size, the same art-pixel size, the
same pose, the same body proportions and the same position: the feet stay on the same row and
in the same place. Do NOT zoom in, do NOT crop and do NOT redraw the figure bigger or with finer
pixels.
Redraw the character as its FEAR version: the green figure, the THIRD one in the top row of the
SECOND attached image. It MUST have these three things, clearly visible:
1. EYES: two dark empty eye holes in the mask, almost black #0A0A0F.
2. CHEST HOLE: the ragged hole in the middle of the chest stays where it is, almost black
   #0A0A0F (or see-through magenta, as in the first image). It is the emptiness where the
   emotion used to be.
3. DISSOLVING: the back of the body (his back, the back arm and the back leg, the side he is
   NOT facing) breaks apart into a cloud of small square particles, black #0A0A0F and dark
   violet #2D1B4E, 1 to 3 art pixels each, drifting backwards away from him. The front of the
   body (the mask, the chest, the front arm and the front leg) stays solid.
A sickly pale grey-green mask and skin, ragged work clothes in dark swamp green #1A2E1A and
spectral grey #3A3A4A. Hunched and wary, as if listening. No orange, no ember cracks, no water.
Palette: black #0A0A0F, dark violet #2D1B4E, swamp green #1A2E1A, spectral grey #3A3A4A and a
pale grey-green for the skin and the mask.
Background flat solid #FF00FF magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo,
NO transparency checkerboard, NO text.
```

**Antes de seguir, mírala:** si le faltan los ojos, el hueco o el polvo, repítela. Todo lo demás
los copia de ella.

#### Paso 2: los otros 11 → `I2`, `r1` … `r4`, `a1` … `a4`, `d2`, `d3`

Cada uno en su mensaje y con dos adjuntos, en este orden: **la base de ese fotograma**
(`Bases/r1.png`, …) y **tu maestra `I1.png`**. Guárdalo con el mismo nombre que su base.

```text
Edit the FIRST attached image. Keep EXACTLY its canvas size, its art-pixel size, its pose and its
position: the feet stay on the same row and in the same place. Do NOT zoom in and do NOT redraw
the figure bigger or with finer pixels.
Redraw the character with the design of the SECOND attached image: the same mask, skin,
clothes, colours and the same black dust. Before answering, check these three things. They MUST
be in the result even if the body is turned, leaning or in motion:
1. the two dark eye holes in the mask. In the first image the eyes may glow orange: replace
   that glow with dark holes. Never remove the eyes;
2. the hole in the middle of the chest, as in the second image;
3. the back of the body breaking into small black and dark violet square particles drifting
   backwards.
No orange anywhere: if the first image has orange eyes or cracks, they become dark.
Background flat solid #FF00FF magenta. Hard-edged fully opaque pixels. NO blur, NO glow halo,
NO transparency checkerboard, NO text.
```

**Para el ataque (`a1` … `a4`)** añade al final: «He is ATTACKING: only in this image, the two
eye holes glow pale green #7FE6B0 (a small solid spot of 2 to 3 art pixels in each), and the dust
behind him is thinner: he is fully visible.»

**Para la muerte (`d2`, `d3`)** añade al final: «It is dying: the whole body bursts into a swarm
of black and dark violet square particles that scatter backwards and upwards, more dust than
body. The mask is the last solid thing left, falling. Only here, the chest hole may be hidden.»

### A2. Centinela Ciego → `Mundo3/Centinela/`

*Decisión 6 de `Nivel3_Diseno.md`, aprobada:* un centinela de trinchera, ciego, con una
máscara de gas cuyos cristales están pintados y rajados. No ve: oye. Patrulla, se para a
escuchar y lanza una estocada con la bayoneta.

**Adjunta:** `Tesis/Imagenes/Mundo1/Golem/Idle/Golem I1.jpg`, solo para el tamaño del píxel (el de
los enemigos del Nivel 1).

#### La maestra → `W1.png` (andar, primer paso)

```text
Pixel art game sprite, side view facing LEFT, flat solid #FF00FF magenta background, the same
art-pixel size and hard dark outline style as the attached golem. The golem is only a size
reference: copy nothing else from it.
A BLIND SENTRY of a forgotten trench, walking: a tall, gaunt soldier about 56 art pixels tall
from the soles to the top of the helmet, on a canvas about 100 art pixels wide and 80 tall,
a little right of center, leaving about 30 art pixels of empty space on the LEFT for his
bayonet.
- an old dented round steel helmet;
- an old GAS MASK covering the whole face, with two round glass lenses painted over and cracked,
  milky white #D8DCE0: he is BLIND, and the two lenses must be clearly visible;
- a short filter canister hanging from the mask;
- a long tattered greatcoat down to the knees, spectral grey #3A3A4A and swamp green #1A2E1A,
  with belts and empty pouches;
- a long old rifle with a bayonet, held upright against his shoulder;
- the head slightly tilted, as if listening.
Mid-step: the front leg forward, the back leg behind, both feet on the same flat bottom row.
Palette: black #0A0A0F, dark violet #2D1B4E for the shadows, swamp green #1A2E1A, spectral grey
#3A3A4A, milky white only for the lenses and the edge of the bayonet.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text,
NO border, NO floor shadow.
```

#### Los demás, editando siempre `W1.png`

Con la plantilla de edición, añadiendo antes de «Change:» la línea **«The two milky white blind
lenses of the gas mask MUST stay visible.»** (menos en `M2` y `M3`):

| Archivo | Animación | Change |
|---|---|---|
| `W2.png` | Andar | «both feet together under the body, the coat swinging» |
| `W3.png` | Andar | «mid-step with the OTHER leg forward» |
| `W4.png` | Andar | «both feet together again, the head tilted to the other side» |
| `L1.png` | Escuchar | «LISTENING: he stops, the head tilted far to one side, one hand raised next to the mask, the rifle still upright» |
| `L2.png` | Escuchar | «listening harder: crouching slightly, the head turned toward the left, the hand still at the mask» |
| `A1.png` | Estocada | «WARNING: the rifle lowered to the hip with both hands, the bayonet pointing LEFT, leaning back, ready to thrust» |
| `A2.png` | Estocada | «THRUST: lunging forward to the left, the rifle stretched as far as it goes, the bayonet far forward, a short white motion streak behind its tip. His body moves at most 12 art pixels to the left» |
| `A3.png` | Estocada | «recovering: pulling the rifle back to upright, standing straight again» |
| `H1.png` | Daño | «hit: leaning back, the coat flapping, the helmet tilted» |
| `M1.png` | Muerte | «falling to his knees, the rifle slipping from his hands» |
| `M2.png` | Muerte | «collapsing forward, the gas mask falling off: under it there is NO face, only darkness and a few drifting black particles» |
| `M3.png` | Muerte | «only the empty coat, the helmet and the gas mask lying on the floor, a little black dust rising from them» |

En `M2` y `M3`, cambia la línea de los cristales por «In this image, the lenses may be hidden.»

### A3. El Acechador, mini-jefe del Mundo 3 → `Mundo3/Acechador/`

*Decisión 5 de `Nivel3_Diseno.md`, aprobada:* como el Golem en el Mundo 1 y la Plañidera
en el Mundo 2. Una bestia de sombra sin ojos, como un lobo enorme: lo que «respira entre los
árboles» en la apertura. Caza por el ruido: se agazapa y gruñe (el aviso), embiste en línea
recta y, si se estrella contra un árbol, se queda aturdido. Tiene hiperarmadura, como el Golem:
no lleva fotograma de daño.

Son **17 fotogramas**: Merodear 4, Escuchar 1, Aviso 2, Embestida 2, Aturdido 2, Mordisco 2 y
Muerte 4.

**Adjunta:** `Tesis/Imagenes/Mundo1/Golem/Idle/Golem I1.jpg`, solo para el tamaño del píxel.

#### La maestra → `P1.png` (merodear)

```text
Pixel art game sprite, side view facing LEFT, flat solid #FF00FF magenta background, the same
art-pixel size and hard dark outline style as the attached golem. The golem is only a size
reference: copy nothing else from it.
THE STALKER, a huge blind beast of shadow, prowling: like an enormous gaunt wolf, about 90 art
pixels long from the snout to the tail and about 52 art pixels tall to the tips of the ears, on
a canvas about 140 art pixels wide and 80 tall, centered, its paws on the same flat bottom row.
- a long narrow head with NO EYES at all: smooth dark skin where the eyes should be, big
  wrinkled nostrils and two tall torn ears pricked up;
- a pale green #7FE6B0 breath coming out of the snout, drawn as a small cloud of solid pixels;
- a thin body, the ribs showing under short bristly black fur, long legs with big pale claws;
- the hind body and the tail breaking apart into small square particles, black #0A0A0F and dark
  violet #2D1B4E, drifting backwards like smoke made of pixels;
- the head low, sniffing the ground.
Palette: black #0A0A0F, dark violet #2D1B4E, spectral grey #3A3A4A for the fur highlights, pale
grey #9A98A8 for the claws and the teeth, pale green #7FE6B0 only for the breath. Menacing but
not gory: no blood.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text,
NO border, NO floor shadow.
```

**Antes de seguir, mírala:** sin ojos, con las dos orejas, el aliento verde y la grupa en polvo.

#### Los demás, editando siempre `P1.png`

Con la plantilla de edición, añadiendo antes de «Change:» la línea **«It MUST stay eyeless, with
its two tall ears, the pale green breath from the snout and the hind body breaking into black
dust.»** (menos en `X3` y `X4`):

| Archivo | Animación | Change |
|---|---|---|
| `P2.png` | Merodear | «a slow step: the front left paw lifted, the head swinging a little to the side, sniffing» |
| `P3.png` | Merodear | «the paws in the opposite position of the original, the head low» |
| `P4.png` | Merodear | «the front right paw lifted, the head swinging to the other side» |
| `E1.png` | Escuchar | «LISTENING: frozen still, the head raised high, the ears straight up, the breath held (only a wisp)» |
| `G1.png` | Aviso | «WARNING: crouched low, front legs spread, the head down and forward, lips pulled back showing pale teeth, the fur on the back bristling» |
| `G2.png` | Aviso | «the same crouch, the hind legs coiled under it, ready to spring, a big burst of pale green breath» |
| `C1.png` | Embestida | «the CHARGE: the body stretched long and low in a full gallop to the left, the ears flat back, a long trail of black dust behind. It moves at most 15 art pixels to the left» |
| `C2.png` | Embestida | «the same gallop with the legs in the opposite phase» |
| `S1.png` | Aturdido | «STUNNED after crashing into a tree: the head bent sideways, the front legs buckled, a few chips of bark and small white sparks around the head» |
| `S2.png` | Aturdido | «still stunned: shaking its head, swaying on unsteady legs, the breath coming out in small puffs» |
| `B1.png` | Mordisco | «BITE WARNING: the head pulled back, the jaws opening wide» |
| `B2.png` | Mordisco | «the BITE: the head thrust forward to the left, the jaws snapping shut» |
| `X1.png` | Muerte | «hit hard: rearing up on the hind legs, the head thrown back in a silent howl» |
| `X2.png` | Muerte | «fallen on its side, the legs limp, the breath fading» |
| `X3.png` | Muerte | «the body breaking into black dust from the tail toward the head: only the head, the neck and the front legs are left» |
| `X4.png` | Muerte | «nothing left but a small pile of black dust on the floor and a last wisp of pale green breath» |

En `X3` y `X4` añade al final «In this image, the MUST list no longer applies.»

---

## B. El Espectro del Pavor · Google AI Studio

El concepto que hay (`Referencias/Arte/Jefes/jefe3.jpeg`) funciona: una figura alta con una
armadura de escamas negras como hojas o plumas, vetas verdes que brillan, una máscara rajada que
brilla por dentro, una gema en el pecho, hojas negras que flotan alrededor y una espada curva. A
oscuras solo se verían sus vetas y su máscara, que es justo lo que pide el combate: es invisible y
se le ve al avisar.

Aprobaste quedarte con él tal cual (decisión 7): **sáltate B1** y ve directo a la maestra (B2)
adjuntando `jefe3.jpeg`. B1 queda por si algún día quieres verlo con otro aspecto.

### Cómo usar AI Studio

Como con la Doncella (`Prompts_Mundo2.md`, apartado D): `aistudio.google.com`, un chat nuevo con
un modelo que genere imágenes, el bloque de abajo en *System instructions*, las referencias con
el botón **+** y los pasos en orden, en el mismo chat. **No reutilices el chat de la Doncella:**
sus instrucciones del sistema son otras.

### Instrucciones del sistema

```text
You are the pixel-art character designer of "Fragmentum: Ecos de la Identidad", a 2D
side-view action platformer about a soldier's clone who recovers the emotions the soldier
suppressed after a war. Each world is one emotion guarded by an "Architect of the Soul".
You are designing the guardian of FEAR: "El Espectro del Pavor" (the Specter of Dread).
His story: during the war the soldier learned to disappear to stay alive, to keep still and
silent so that nobody would find him. The Specter is everything the soldier hid. He lives in a
dark forest, he is invisible most of the time and shows himself only in the instant before he
strikes, with a bright flash.

Rules for EVERY image you generate:
- Pixel art with hard-edged, fully opaque pixels; a clear dark outline; limited palette.
- Palette: black #0A0A0F, dark violet #2D1B4E, swamp green #1A2E1A, spectral grey #3A3A4A, and
  a pale green #7FE6B0 (near-white #D8FFF0 at its brightest) ONLY for what glows: the cracks of
  his mask, the veins of his armour, the edge of his sword and the gem in his chest. No orange,
  no red, no gold, no blue.
- Side view facing LEFT (the player comes from the left).
- Flat solid #FF00FF magenta background. NEVER a transparency checkerboard, NEVER a scene
  behind him (unless asked), no text, no border, no floor shadow.
- NO blur, NO glow halo, NO soft or anti-aliased edges, NO gradients, NO semi-transparent
  parts: even the floating leaves and the light are drawn with solid pixels.
- One image per answer unless told otherwise. When asked to edit an image, keep everything that
  is not mentioned exactly as it is: canvas, pixel size, palette, design and position. Never
  zoom in and never redraw him bigger or with finer pixels.
```

### B1. Concepto: tres variantes (opcional)

**Adjunta:** `Referencias/Arte/Jefes/jefe3.jpeg` y `Tesis/Jefe1 animaciones/Idle/Jefe I1.png` (la
Centinela, para el estilo y el tamaño de jefe).

```text
Design THREE variants of the boss in the first attached image, as full-body character concepts
in the pixel-art style of the second attached image (the boss of world 1). Keep what works: the
tall dark figure, the armour of black scales like feathers or leaves, the pale green glowing
veins, the cracked mask that glows from inside, the white-green gem in the chest, the black
leaves floating around him and the long curved sword.
Make him more readable from far away in a dark forest: a clear, tall, slightly hunched
silhouette, and the glowing parts (mask, veins, sword edge and gem) must draw a recognizable
shape on their own, as if the rest of him were invisible.
Unsettling, not gory: no blood, no skull, no teeth.
Three separate images, three clearly different variants, each one the full figure centered on
a flat #FF00FF background, about 160 art pixels tall.
```

Elige una. Si quieres retocarla, pídelo editándola antes de seguir.

### B2. La maestra: Reposo 1 → `Mundo3/Espectro del Pavor/I1.png`

**Adjunta:** `jefe3.jpeg` (o la variante elegida) y `Tesis/Jefe1 animaciones/Idle/Jefe I1.png`.

```text
Turn the boss in the FIRST attached image into an IN-GAME SPRITE, the first frame of his idle
animation, at the same art-pixel size as the boss in the SECOND attached image and about as tall
as it: about 160 art pixels from the soles to the top of the head.
Canvas about 240 art pixels wide and 200 tall. He stands facing LEFT, a little right of center,
leaving about 70 art pixels of empty magenta on the LEFT for his sword swings and some empty
space above his head for the leaves. The soles of his feet rest exactly on art-pixel row 192
(counted from the top): this row is his anchor in every future frame.
Calm, menacing pose: standing, slightly hunched, the curved sword held low in his front hand
with the tip near the ground, the free hand open at his side, a few black leaves floating
around his shoulders. The mask, the veins, the sword edge and the chest gem glow softly in pale
green.
Flat solid #FF00FF background. Hard-edged fully opaque pixels.
```

**Antes de seguir, mírala:** la máscara, las vetas, la gema y la espada se tienen que leer a ese
tamaño, y le tiene que sobrar lienzo por todos lados.

### B3. Los demás fotogramas, editando la maestra

Cada uno en su mensaje, **adjuntando siempre `I1.png`** y empezando así:

```text
Edit the attached image. Keep EXACTLY the same canvas, the same art-pixel size, the same palette,
the same design and the same anchor: the soles of his feet stay on the same row and in the same
place, unless the change below lifts or moves him. He keeps facing LEFT. Do NOT zoom in and do
NOT redraw him bigger or with finer pixels. These MUST stay visible in every image: the cracked
mask that glows from inside, the glowing gem in his chest and his curved sword.
Change ONLY his pose, as described:
```

| Archivo | Animación | Pose |
|---|---|---|
| `I2.png` | Reposo | «he breathes in: the shoulders rise one art pixel, the floating leaves drift up» |
| `I3.png` | Reposo | «the head turns slightly toward the viewer, the leaves drift to the right» |
| `I4.png` | Reposo | «he breathes out: the shoulders lower, the leaves drift down» |
| `A1.png` | Aparecer | «NO body yet: only a tight swirl of black leaves standing where he will be, as tall as him, with the glowing cracks of his mask already visible inside it at the height of his head» |
| `A2.png` | Aparecer | «the swirl thickening into his silhouette: the head, the mask and the shoulders are formed; the legs and the sword are still whirling leaves» |
| `A3.png` | Aparecer | «almost whole: only the feet and the tip of the sword are still leaves, and leaves are falling off him» |
| `A4.png` | Aparecer | «whole again, in the idle pose, a few leaves settling to the ground around his feet» (desvanecerse es A4 → A1, al revés) |
| `T1.png` | Tajo | «WARNING: the sword raised high behind his head with both hands; the mask, the veins and the whole edge of the sword FLASH bright near-white #D8FFF0: the warning flash» |
| `T2.png` | Tajo | «the SLASH: the sword sweeping down and forward to the left in a wide arc, a crescent trail of pale green behind the blade» |
| `T3.png` | Tajo | «follow-through: the sword low in front of him, its tip almost touching the ground on the left, the body leaning forward» |
| `T4.png` | Tajo | «RECOVERY: hunched and panting, the sword tip resting on the ground, the glow of the mask and the veins DIM: he is exposed» |
| `E1.png` | Embestida | «WARNING: crouched low, the sword held back along his body, the mask and the veins FLASHING bright near-white #D8FFF0» |
| `E2.png` | Embestida | «the CHARGE: the body almost horizontal, stretched forward to the left, the sword pointing ahead, his back half dissolving into a long trail of black leaves. He moves at most 20 art pixels to the left» |
| `E3.png` | Embestida | «stopping: skidding, one hand on the ground, leaves scattering in front of him» |
| `H1.png` | Hojas | «WARNING: the free arm raised high, palm up, the mask FLASHING near-white, black leaves rising from the ground around him» |
| `H2.png` | Hojas | «the leaves swirling in a tall column above his raised hand» |
| `H3.png` | Hojas | «the arm thrown down, the column of leaves shooting up and out of the top of the canvas» |
| `R1.png` | Grito | «the head thrown back, the black scales of his armour lifting like bristling feathers» |
| `R2.png` | Grito | «a silent scream: arms wide, a burst of leaves flying outward all around him, the crack of the mask splitting wider and glowing brighter» |
| `R3.png` | Grito | «settling again, the leaves returning to float around his shoulders» |
| `M1.png` | Muerte | «hit hard: he recoils backwards, the mask cracking further, a shard of it falling» |
| `M2.png` | Muerte | «falling to one knee, the sword dropping from his hand, the glow of the veins flickering» |
| `M3.png` | Muerte | «his body coming apart into black leaves from the feet up; only the chest, the head and the glowing mask are left» |
| `M4.png` | Muerte | «only the cracked mask, falling among the leaves» |
| `M5.png` | Muerte | «only a pile of fallen black leaves on the floor and, floating above it, the dark violet Fear gem» (como el fragmento que dejan la Centinela y la Doncella) |

En `A1`, `A2` y de `M2` a `M5`, añade al final «In this image, the MUST list applies only to
what is still there.»

**Las tres sombras de la segunda fase no llevan imágenes propias:** el juego usa los mismos
fotogramas, más oscuros, y lo que distingue a la verdadera es su luciérnaga (C3).

**Cuando estén:** guárdalas en `Tesis/Imagenes/Mundo3/Espectro del Pavor/` y avísame. Si alguna
sale con otro tamaño de píxel o con el ancla movida, te digo cuál repetir; no la arreglo a mano.

---

## C. Lo que lanza y lo que lo distingue (Gemini)

### C1. La hoja que cae (3) → `Mundo3/Hojas/Hoja1.png` … `Hoja3.png`

**Adjunta:** `Espectro del Pavor/I1.png`, para que salga con su píxel.

```text
Edit the attached image. Keep the same canvas size, the same art-pixel size (the same blocks)
and the same palette. Remove the figure completely: keep only ONE of the black leaves that float
around him, alone in the center of the canvas, about 20 art pixels long, falling with its sharp
tip pointing DOWN, with a thin pale green #7FE6B0 edge along one side. Everything else is flat
solid #FF00FF magenta. Do NOT zoom in. Hard-edged fully opaque pixels. NO blur, NO glow halo,
NO soft edges, NO transparency checkerboard, NO text, NO border.
```

Después, editando **Hoja1** con la plantilla de edición:
- **Hoja2:** «the same leaf, turned a quarter turn as it spins, still falling, tip down-left».
- **Hoja3:** «the leaf SHATTERING against the ground: five or six small black fragments flying
  apart sideways from the bottom center; no leaf left».

### C2. El aviso de las hojas (2) → `Mundo3/Hojas/Aviso1.png`, `Aviso2.png`

**Adjunta:** `Espectro del Pavor/I1.png`, solo para el tamaño del píxel.

```text
Pixel art game sprite, side view, flat solid #FF00FF magenta background, the same art-pixel size
as the attached image. A STRIP OF DARK FOREST FLOOR about 100 art pixels wide and 8 tall,
centered on the canvas: a few dead black leaves lying on it and, through the cracks between
them, a faint pale green #7FE6B0 light shining up in thin vertical lines. The bottom row is
flat. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO gradient, NO transparency
checkerboard, NO text, NO border.
```

Después, editando **Aviso1**: **Aviso2:** «the same strip, the pale green light stronger and
taller, the leaves lifting slightly off the floor».

### C3. La luciérnaga (2) → `Mundo3/Luciernaga/Luciernaga1.png`, `Luciernaga2.png`

Una sola, clara y casi sin color: el juego la tiñe de dorado en la figura verdadera y de verde en
las sombras.

**Adjunta:** `Espectro del Pavor/I1.png`, para el tamaño del píxel.

```text
Edit the attached image. Keep the same canvas size, the same art-pixel size and the same flat
#FF00FF magenta background. Remove the figure completely and draw only ONE tiny FIREFLY in the
center: a round body 4 art pixels across, near-white #F4F4E8, a 1-art-pixel ring of pale grey
#B8B8B0 around it, and two tiny wings. Nothing else. Do NOT zoom in. Hard-edged fully opaque
pixels. NO blur, NO glow halo, NO soft edges, NO transparency checkerboard, NO text.
```

Después, editando **Luciernaga1**: **Luciernaga2:** «the same firefly with the wings in the
other position and the grey ring one art pixel wider».

### C4. El Fragmento del Miedo → `Mundo3/Fragmento/Fragmento1.png`

**Adjunta:** `Tesis/Imagenes/Mundo2/Fragmento/Fragmentoo.jpg`, el de la Tristeza: así los
fragmentos casan.

```text
Edit the attached image. Keep EXACTLY the same canvas, the same art-pixel size, the same shape
and the same position of the gem. Change ONLY its colours: it becomes the FEAR fragment, a deep
dark violet #2D1B4E gem with lighter violet #6B4FA0 facets, a thin pale green #7FE6B0 glint along
one edge and one near-white #F0E8FF highlight. Background flat solid #FF00FF magenta. Hard-edged
fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text.
```

---

## D. Dónde va cada cosa

| Carpeta en `Tesis/Imagenes/Mundo3/` | Archivos | Herramienta |
|---|---|---|
| `Espectro/` | `I1`, `I2`, `r1` … `r4`, `a1` … `a4`, `d2`, `d3` | Gemini (editando las `Bases/` del Eco, con la maestra `I1`) |
| `Centinela/` | `W1` … `W4`, `L1`, `L2`, `A1` … `A3`, `H1`, `M1` … `M3` | Gemini (editando `W1`) |
| `Acechador/` | `P1` … `P4`, `E1`, `G1`, `G2`, `C1`, `C2`, `S1`, `S2`, `B1`, `B2`, `X1` … `X4` | Gemini (editando `P1`) |
| `Espectro del Pavor/` | `I1` … `I4`, `A1` … `A4`, `T1` … `T4`, `E1` … `E3`, `H1` … `H3`, `R1` … `R3`, `M1` … `M5` | **AI Studio** |
| `Hojas/` | `Hoja1` … `Hoja3`, `Aviso1`, `Aviso2` | Gemini (editando la maestra del jefe) |
| `Luciernaga/` | `Luciernaga1`, `Luciernaga2` | Gemini |
| `Fragmento/` | `Fragmento1` | Gemini (editando el de la Tristeza) |

Cuando tengas un paquete completo, avísame. Lo reviso antes de integrarlo: el mismo tamaño de
píxel, la misma base y nada de damero. Si algo no cuadra, te digo qué repetir.

---

# Segunda tanda (2026-10-14): el escenario, las mecánicas, el fondo, las cinemáticas y las puertas

Las mismas reglas del apartado 0. Aquí casi todo sale de **editar algo que ya existe** (las hojas
del Mundo 2, sus fondos, sus cinemáticas): así hereda el tamaño del píxel y la distribución, y el
juego corta las piezas en los mismos sitios.

## E. El escenario y las mecánicas

### E1. Tileset del bosque → `Mundo3/Tileset/TileMap3.png`

**Adjunta:** `Tesis/Imagenes/Mundo2/Tileset/TileMap2.jpg`. Pide la misma distribución: si cada
pieza queda en su sitio, el juego la corta igual que en el Mundo 2.

```text
Edit the attached pixel-art tileset sheet. Keep EXACTLY the same canvas, the same layout (every
piece in the same place and with the same size), the same art-pixel size and the same hard pixel
style. Keep the flat solid #FF00FF magenta background.

Change only the theme: the frozen drowned city becomes a DARK HAUNTED FOREST at night.
Palette: black #0A0A0F, dark violet #2D1B4E, swamp green #1A2E1A, spectral grey #3A3A4A, and a
few pale green #7FE6B0 pixels for moss and glowing fungus. No snow, no ice, no blue, no orange.

- The stone masonry blocks -> packed dark earth with small stones and thin roots running through
  it; the top edge of each block is a thin layer of dead leaves and moss.
- The pieces with icicles hanging below -> the same shapes in earth, with roots and thin
  rootlets hanging below.
- The square slab of ice -> a square block of a dense TANGLE OF DARK, WET ROOTS.
- The pile of ice blocks -> a pile of fallen dead branches and stones.
- The frozen puddle -> a small puddle of dark swamp water.
- The snowy wooden beams and their supports -> thick gnarled TREE BRANCHES with bark and moss on
  top, and the vertical posts -> dark tree trunks.
- The frozen torn sails -> torn, rotten military canvas hanging stiff from a branch.
- The anchor chain -> a hanging tangle of vines and roots.
- The street lamp -> an old trench oil lamp, UNLIT, rusty, hanging from a hook.
- The hooded statue -> a soldier's grave: a rifle stuck bayonet-down in a mound of earth, an old
  helmet resting on its stock, stones around it.

Every block of earth must tile seamlessly with copies of itself on all four sides.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO gradients, NO transparency
checkerboard, NO text, NO border.
```

### E2. Decoración extra → `Mundo3/Tileset/Props3.png`

**Adjunta:** `Tesis/Imagenes/Mundo2/Tileset/Props2.jpg`, por lo mismo.

```text
Edit the attached props sheet. Keep EXACTLY the same canvas, the same layout (every prop in the
same place and about the same size), the same art-pixel size and the same hard pixel style, and
the flat solid #FF00FF magenta background. Change only the theme to a DARK HAUNTED FOREST at
night with an old war trench, with the palette black #0A0A0F, dark violet #2D1B4E, swamp green
#1A2E1A, spectral grey #3A3A4A and a little pale green #7FE6B0 moss:
- The bell hanging from a beam -> an old soldier's helmet and a canteen hanging by their straps
  from a gnarled branch.
- The arched window -> the entrance of a trench dugout: a low frame of rotten planks with
  sandbags piled around it, pitch dark inside.
- The seaweed -> three tall dark ferns.
- The table and the chair -> a wooden ammunition crate and a short stack of sandbags.
- The bell lying on the floor -> an old helmet lying half buried in dead leaves.
- The broken column and its fallen piece -> a dead tree stump and a fallen rotten log.
No snow, no ice, no blue, no orange. Hard-edged fully opaque pixels. NO blur, NO glow halo, NO
gradients, NO transparency checkerboard, NO text, NO border.
```

### E3. La antorcha (5) → `Mundo3/Antorcha/Apagada.png`, `Encendida1.png` … `Encendida3.png`, `Consumida.png`

En el juego mide 0,6 × 2 u. Se enciende con E, alumbra 8 u durante 15 s y se consume. **Su
llama es lo único cálido de todo el mundo.**

**Adjunta:** `Tesis/Imagenes/Mundo1/Golem/Idle/Golem I1.jpg`, solo para el tamaño del píxel.

#### La maestra → `Apagada.png`

```text
Pixel art game sprite, side view, flat solid #FF00FF magenta background, the same art-pixel size
and hard dark outline style as the attached golem. The golem is only a size reference: copy
nothing else from it.
A WOODEN TORCH ON A POST stuck in the forest floor, UNLIT, about 12 art pixels wide and 48 tall,
centered horizontally, its base on a flat bottom row: a rough dark wooden post and, on top, a
head of tarred rags bound with rusty wire, cold and black. A thin white cloth ribbon tied around
the post. Leave about 30 art pixels of empty magenta ABOVE the torch: the flame of the next
images goes there.
Palette: black #0A0A0F, dark violet #2D1B4E, swamp green #1A2E1A, spectral grey #3A3A4A. No
flame, no warm colour.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text,
NO border, NO floor shadow.
```

#### Los demás, editando siempre `Apagada.png` (plantilla del apartado 0)

| Archivo | Change |
|---|---|
| `Encendida1.png` | «LIT: a flame on the head of the torch, about 14 art pixels tall, drawn with solid pixels: warm orange #FFB040 outside, yellow #FFE080 inside and a pale yellow core; three sparks above it. This warm flame is the only warm colour in the image» |
| `Encendida2.png` | «LIT, the same flame leaning a little to the left, the sparks in other places» |
| `Encendida3.png` | «LIT, the same flame taller and leaning a little to the right» |
| `Consumida.png` | «BURNT OUT: the rags charred down to a black stub, two dying dark red embers in it and a thin wisp of grey smoke above» |

### E4. El cepo (2) → `Mundo3/Cepo/Abierto.png`, `Cerrado.png`

Mide 1 u de ancho. A oscuras solo se ve a 1 u de Kael: el juego lo esconde; el dibujo es el de
siempre.

**Adjunta:** `Golem I1.jpg`, para el tamaño del píxel.

```text
Pixel art game sprite, side view, flat solid #FF00FF magenta background, the same art-pixel size
and hard dark outline style as the attached golem (only a size reference).
A rusty steel BEAR TRAP lying OPEN on the forest floor, seen from the side, about 26 art pixels
wide and 8 tall, centered, on a flat bottom row: two jagged semicircular jaws opened flat, a
round pressure plate between them, a short chain, half covered by a few dead leaves. Dark rusty
grey #3A3A4A and black, a few paler pixels on the teeth. No blood.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text.
```

Después, editándolo: **`Cerrado.png`:** «SNAPPED SHUT: the two jaws closed upright against each
other into a tall jagged arch about 12 art pixels tall, the dead leaves thrown up around it».

### E5. Las ramas (4) → `Mundo3/Rama/Rama.png`, `RamaPodrida.png`, `RamaRota.png`, `Astillas.png`

Las ramas son las plataformas del bosque: miden 2 u de largo y media de grueso (las de la arena,
3). El juego repite su parte central para alargarlas, como las vigas del Mundo 2. **La podrida
tiene que distinguirse con luz, pero solo con luz:** misma forma, otro color.

**Adjunta:** `Golem I1.jpg`, para el tamaño del píxel.

```text
Pixel art game sprite, side view, flat solid #FF00FF magenta background, the same art-pixel size
and hard dark outline style as the attached golem (only a size reference).
A horizontal TREE BRANCH used as a platform, about 48 art pixels long and 12 tall, centered:
thick dark bark (black #0A0A0F, dark violet #2D1B4E), a perfectly FLAT walkable top on a single
row, covered with moss (swamp green #1A2E1A, a few pale green #7FE6B0 pixels), and both ends
broken off irregularly. The middle 24 art pixels must be able to repeat side by side with no
visible seam.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text.
```

Después, editando **`Rama.png`**:
- **`RamaPodrida.png`:** «the same branch ROTTEN: dry grey bark (#3A3A4A, light grey #6A6A78),
  two long cracks along it, pale fungus shelves underneath, no moss. Exactly the same shape and
  the same flat top».
- **`RamaRota.png`:** «the rotten grey branch SNAPPING in the middle: the two halves bent down at
  the break, splinters sticking out».
- **`Astillas.png`:** «no branch: only splinters and chips of grey rotten wood falling apart».

### E6. La cinta blanca (2) → `Mundo3/Cinta/Cinta1.png`, `Cinta2.png`

Marca el camino bueno de la bifurcación. Solo se ve con luz.

```text
Pixel art game sprite, side view, flat solid #FF00FF magenta background, the same art-pixel size
as the attached golem (only a size reference).
A strip of torn WHITE CLOTH tied in a knot around a short piece of dark branch, its loose end
hanging down and to the right, about 12 art pixels wide and 16 tall, centered. Off-white #E8E4DC
with light grey folds.
Hard-edged fully opaque pixels. NO blur, NO glow halo, NO transparency checkerboard, NO text.
```

Después, editándola: **`Cinta2.png`:** «the loose end of the cloth blown to the left by a
breeze».

## F. El fondo (parallax, como el de los otros dos mundos)

**Adjunta en todos:** `Tesis/Imagenes/Mundo2/fondos/Cielo.jpg`, el del Mundo 2, para el estilo, el
tamaño del píxel y el formato (16:9). Las capas que se ven a través de otras llevan magenta en lo
que debe quedar transparente; el cielo es opaco. **El fondo de este mundo casi no se verá**: el
nivel es oscuro. Por eso importan las siluetas, no los detalles.

| Archivo | Prompt |
|---|---|
| `Fondos/Cielo.jpg` | `Pixel art parallax background layer, 16:9, same style, same art-pixel size and same format as the attached image. A NIGHT SKY over a forest: very dark violet #2D1B4E low on the horizon fading to black #0A0A0F at the top, a pale cold MOON half hidden behind thin dark clouds, sparse single-pixel stars. Fully opaque, no ground, no trees. The left and right edges must match so it can repeat horizontally. Hard-edged pixels, NO blur, NO gradient banding, NO text.` |
| `Fondos/BosqueLejano.jpg` | `Pixel art parallax background layer, 16:9, same style as the attached image. The distant silhouette of a DENSE FOREST: rows of tall, pointed and bare trees in near-black violet (#1A1426, #2D1B4E), a thin band of grey mist at their feet along the bottom. Everything above the treeline is flat solid #FF00FF magenta. The left and right edges must match. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/BosqueCercano.jpg` | `Pixel art parallax background layer, 16:9, same style as the attached image. Tall dark TREE TRUNKS with twisted bare branches, closer and bigger than a distant forest, black #0A0A0F with swamp green #1A2E1A moss, a few branches crossing between them. Between the trunks and above the branches, flat solid #FF00FF magenta. The left and right edges must match. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/ArbolHueco.jpg` | `Pixel art background prop, same style as the attached image. A HUGE ANCIENT DEAD TREE, much taller than any other, with a dark hollow at its base like a doorway, roots spread like claws, and a few pale green #7FE6B0 fungus lights on its bark. A landmark seen from far away. Flat solid #FF00FF magenta around it. Hard-edged pixels, NO blur, NO text.` *(Es el hito, como la fortaleza y la torre: se ve desde el inicio y es donde espera el jefe.)* |
| `Fondos/Niebla.jpg` | `Pixel art parallax foreground layer, 16:9, same style as the attached image. A horizontal band of low MIST along the bottom third, drawn with solid pixels in two or three tones of spectral grey #3A3A4A and dark violet #2D1B4E, with a wavy top edge. Everything else flat solid #FF00FF magenta. The left and right edges must match. Hard-edged pixels, NO blur, NO soft gradients, NO text.` |
| `Fondos/PrimerPlanoArriba.jpg` *(opcional)* | `Pixel art foreground layer, 16:9, same style as the attached image. Black leafless branches and hanging moss along the top edge. Everything else flat solid #FF00FF magenta. Edges match for repeating. Hard-edged pixels, NO blur, NO text.` |
| `Fondos/PrimerPlanoAbajo.jpg` *(opcional)* | `Pixel art foreground layer, 16:9, same style as the attached image. Ferns and thorny bushes rising from the bottom edge, black with dark green. Everything else flat solid #FF00FF magenta. Edges match for repeating. Hard-edged pixels, NO blur, NO text.` |

## G. Las cinemáticas del jefe (480 × 268, como las del Mundo 2)

**Pídelas cuando tengas la maestra del jefe** (`Espectro del Pavor/I1.png`).

**Adjunta, en este orden:**
1. `Tesis/Imagenes/Mundo2/Cinematicas/DerrotaDoncella2.jpg` (el lienzo y el tamaño del píxel de
   las cinemáticas a 480);
2. `Tesis/Imagenes/Mundo2/Cinematicas/SalidaTristeza2.jpeg` (Kael, de espaldas, con sus huecos y
   sus dos fragmentos);
3. `Tesis/Imagenes/Mundo3/Espectro del Pavor/I1.png` (el jefe).

Todas empiezan así:

```text
Pixel art cinematic still, 16:9. EXACTLY the same canvas and the same art-pixel size (the same
blocks) as the FIRST attached image, and the same style. Palette: black #0A0A0F, dark violet
#2D1B4E, swamp green #1A2E1A, spectral grey #3A3A4A, and pale green #7FE6B0 only for what
glows. The hero is the one in the SECOND attached image: same design and same dark worn coat,
with the orange shard on his left shoulder and the light blue one on his left forearm. The
guardian is the one in the THIRD attached image. Hard-edged pixels, NO blur, NO text, NO border,
NO letterbox bars.
```

Y siguen con:

| Archivo | Plano |
|---|---|
| `Cinematicas/PresentacionEspectro1.jpg` | «A clearing in a pitch-black forest at night. The hero, tiny, at the bottom left, inside a small circle of dim light around him (his own light): only that circle and a few blades of grass in it are visible. Everything else is black. Far away in the centre of the dark, at the height of a tall man, the two thin glowing pale green cracks of a mask, watching him.» |
| `Cinematicas/PresentacionEspectro2.jpg` | «Close-up of the guardian from the chest up, half dissolved into the dark: only the glowing cracks of his mask, the veins of his armour and the edge of his curved sword, raised beside his face, are lit in bright pale green; the rest of him is black leaves fading into the black background. A few black leaves float around him.» |
| `Cinematicas/DerrotaEspectro1.jpg` | «The guardian breaking apart into a storm of black leaves in the centre of the clearing, his cracked mask falling, pale green light leaking from its cracks. Around him the darkness is thinning: grey-violet tree trunks begin to appear. The hero, tiny, at the bottom left, watching.» |
| `Cinematicas/DerrotaEspectro2.jpg` | «The same forest seen with light for the first time: soft silver moonlight falls into the clearing, the trees are dark green and violet, low mist on the ground, a few firefly pixels. The hero, tiny, in the centre, holding a small dark violet gem that glows.» |

## H. Las puertas de este mundo (como las de los otros dos)

### Decisiones mías, por si quieres cambiarlas antes de generar

- **El emblema es un farol apagado que se enciende al abrirse** la puerta. Es la segunda frase
  del cierre: el bosque era «un lugar donde nadie había encendido una luz».
- **La puerta está tallada en el tronco de un árbol muerto**, sellada por raíces.
- **Por la puerta abierta se ve el mundo siguiente:** la luz ocre y polvorienta de los campos de
  batalla de la Culpa, con ceniza.
- **El checkpoint es una lámpara de trinchera** colgada de una estaca con una cinta blanca:
  apagada y encendida. Es luz cálida, como la de las antorchas.
- **La verja de la arena es un muro de raíces negras con espinas** sobre una empalizada.
- **Los huecos de Kael:** al salir del Miedo, el fragmento violeta tapa **el codo derecho** y
  quedan dos huecos, las dos pantorrillas. El hueco lo elegí yo, como en la Tristeza.

### H1. La puerta → `Mundo3/Puerta/Puerta1.jpg` … `Puerta4.jpg`

**Adjunta:** `Mundo3/Tileset/TileMap3.png` (el tileset del bosque, E1), para el tamaño del píxel y
el estilo. Si todavía no lo tienes, `Mundo2/Tileset/TileMap2.jpg`.

```text
Pixel art game sprite for a 2D side-view platformer, flat solid #FF00FF magenta background.
Same art-pixel size and same hard pixel style as the attached tileset.

A GATE GROWN INTO THE TRUNK OF A HUGE DEAD TREE, seen straight from the front, CLOSED. About 96
art pixels wide and 128 art pixels tall, centered on the canvas, standing on its own flat bottom
row. Gnarled bark forms a pointed arch. Inside it, two door leaves of black rotten planks, bound
by thick roots that have grown across the seam and seal it. Pale fungus on the bark. At the top
of the arch, carved into the wood, the emblem of an UNLIT LANTERN.
Palette: black #0A0A0F, dark violet #2D1B4E, swamp green #1A2E1A, spectral grey #3A3A4A, a few
pale green #7FE6B0 fungus pixels. No warm colours.

Nothing below the bottom row: no ground, no roots spreading on the floor. Nothing touches the
canvas edges. Hard-edged fully opaque pixels only. NO blur, NO glow halo, NO gradient, NO
transparency checkerboard, NO text, NO border, NO shadow on the floor.
```

Después, **editando siempre `Puerta1.jpg`** con la plantilla de edición de
`Prompts_Puertas.md`:

- **`Puerta2.jpg`, abriéndose:** «the roots across the seam have snapped and withered; the door
  leaves are ajar: the vertical gap in the middle shows a band of dusty light drawn as flat solid
  colours (pale ochre #E0C890 in the centre, ochre #C9A567, burnt brown #5C3D1E at the sides). A
  tiny warm flame (#FFB040) has appeared in the carved lantern.»
- **`Puerta3.jpg`, abierta:** «the door leaves are fully open and hidden behind the frame: the
  whole doorway is filled with dusty war light drawn as flat solid bands (pale ochre #E0C890 in
  the centre, then ochre #C9A567, then burnt brown #5C3D1E next to the frame), with four or five
  grey ash pixels drifting out of it. The carved lantern on the arch is LIT with a warm flame
  (#FFB040, yellow).»
- **`Puerta4.jpg`, abierta (variante):** «the same open gate as described for the open state.
  Move the bands of light one or two art pixels and put the ash pixels in different places, so
  this image and the previous one can flicker in a loop.» *(Adjunta `Puerta3.jpg` y edítala.)*

### H2. La verja de la arena → `Mundo3/Verja/Verja.jpg`

Con el prompt de la verja de `Prompts_Puertas.md` (A3), adjuntando el tileset del bosque, y con
`<TEMA>` = «A wall of black THORNY ROOTS and brambles grown over an old wooden palisade: twisted
black roots, sharp grey thorns, a few pale green #7FE6B0 fungus pixels. No warm colours.»

### H3. El checkpoint → `Mundo3/Checkpoint/Checkpoint1.jpg`, `Checkpoint2.jpg`

Con el prompt del checkpoint de `Prompts_Puertas.md` (A3), adjuntando el tileset del bosque, y con
`<TEMA>` = «an old trench OIL LAMP hanging from a hook on a wooden stake, UNLIT: dark glass, a
rusty frame, a white cloth ribbon tied to the stake.» Y la encendida, editándola: «the lamp is
LIT: a small warm flame (#FFB040 and yellow pixels) inside the glass, the ribbon lifted by a
breeze».

### H4. La cinemática de salida (480 × 268) → `Mundo3/Cinematicas/SalidaMiedo1.jpg`, `SalidaMiedo2.jpg`

**Pídela cuando tengas `Puerta3.jpg` y las cinemáticas del jefe.**

**Adjunta, en este orden:**
1. `Mundo3/Cinematicas/DerrotaEspectro2.jpg` (el bosque con luz y el tamaño del píxel);
2. `Mundo2/Cinematicas/SalidaTristeza2.jpeg` (Kael, de espaldas);
3. `Mundo3/Puerta/Puerta3.jpg` (la puerta abierta).

Las dos empiezan así:

```text
Pixel art cinematic still, 16:9. EXACTLY the same canvas and the same art-pixel size (the same
blocks) as the FIRST attached image, and the same style and palette. The hero is the one in the
SECOND attached image: same design and same dark worn coat. He now carries THREE glowing shards:
the orange one on his left shoulder, the light blue one on his left forearm and a dark violet one
that fills the hole in his right elbow. Exactly TWO empty holes remain in his body: his left calf
and his right calf. The gate is the one in the THIRD attached image, with its lit lantern on the
arch. Hard-edged pixels, NO blur, NO text, NO border, NO letterbox bars.
```

| Archivo | Frases que lleva encima | Plano |
|---|---|---|
| `SalidaMiedo1.jpg` | «La oscuridad retrocede un paso. Luego otro.» y «El bosque no era un laberinto: era un lugar donde nadie había encendido una luz.» | «Wide shot of the clearing after the fight, now in soft silver moonlight: dark green and violet trees, low mist, a few fireflies. On the right third of the image the tree gate stands OPEN, warm dusty ochre light pouring out of it across the grass. The hero, tiny, at the bottom left, seen from behind, walking toward it.» |
| `SalidaMiedo2.jpg` | «Kael integra el Fragmento del Miedo. Puede ocultarse sin dejar de avanzar.» | «Close shot from behind the hero as he steps through the open tree gate: his dark silhouette, from the knees up, in the left-centre of the image, framed by the bark arch. In front of him the doorway, full of dusty ochre light and drifting grey ash, with the far silhouettes of broken fences. The three shards glow on him: orange, light blue and violet. Behind him, at the edges, the moonlit forest.» |

## I. Kael (opcional)

**Caminar a escondidas**, en Pixelorama: en este mundo se camina mucho (Alt) y hoy usa el clip de
correr más despacio. Cuatro fotogramas en bucle, en el lienzo de Kael: encogido, los hombros
bajos, pasos cortos y la mano cerca de la espada. Exporta la tira a 1× a
`Tesis/Imagenes Kael/Sigilo/`.

## J. Dónde va cada cosa (segunda tanda)

| Carpeta en `Tesis/Imagenes/Mundo3/` | Archivos | Herramienta |
|---|---|---|
| `Tileset/` | `TileMap3`, `Props3` | Gemini (editando las hojas del Mundo 2) |
| `Antorcha/` | `Apagada`, `Encendida1` … `Encendida3`, `Consumida` | Gemini (editando `Apagada`) |
| `Cepo/` | `Abierto`, `Cerrado` | Gemini |
| `Rama/` | `Rama`, `RamaPodrida`, `RamaRota`, `Astillas` | Gemini (editando `Rama`) |
| `Cinta/` | `Cinta1`, `Cinta2` | Gemini |
| `Fondos/` | `Cielo`, `BosqueLejano`, `BosqueCercano`, `ArbolHueco`, `Niebla` y, opcionales, `PrimerPlanoArriba` y `PrimerPlanoAbajo` | Gemini |
| `Cinematicas/` | `PresentacionEspectro1`, `PresentacionEspectro2`, `DerrotaEspectro1`, `DerrotaEspectro2`, `SalidaMiedo1`, `SalidaMiedo2` | Gemini |
| `Puerta/` | `Puerta1` … `Puerta4` | Gemini (editando `Puerta1`) |
| `Verja/` | `Verja` | Gemini |
| `Checkpoint/` | `Checkpoint1`, `Checkpoint2` | Gemini |
| `Tesis/Imagenes Kael/Sigilo/` | la tira de caminar a escondidas | Pixelorama (opcional) |

**Orden recomendado para esta tanda:** el tileset (E1) primero, porque la puerta, la verja y el
checkpoint lo adjuntan; luego las mecánicas (E3 a E6), que son pequeñas; el fondo; y al final
las cinemáticas, que necesitan al jefe y la puerta.
