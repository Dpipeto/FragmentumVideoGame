# Prompts del Mundo 3 · Miedo

**Primera tanda: los enemigos y el jefe** de `Nivel3_Diseno.md` (sección 8). El Espectro del
Pavor va por **Google AI Studio** (apartado B); todo lo demás, por **Gemini**. Los prompts del
escenario, las mecánicas, el fondo, las cinemáticas y las puertas irán en una segunda tanda,
cuando el diseño esté aprobado (PENDIENTES N44).

Los prompts están en inglés porque Gemini sigue mejor las medidas así. Las explicaciones van en
español.

> **Si el diseño cambia** con tus respuestas a N44 (por ejemplo, si el Centinela Ciego no lleva
> máscara de gas o si no quieres al Acechador), espera a que corrija su prompt antes de
> generarlo. El Espectro y el jefe no dependen de ninguna decisión: se pueden pedir ya.

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

*Propuesta mía (decisión 6 de `Nivel3_Diseno.md`):* un centinela de trinchera, ciego, con una
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

*Propuesta mía (decisión 5 de `Nivel3_Diseno.md`):* como el Golem en el Mundo 1 y la Plañidera
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

Si te gusta tal cual (decisión 7), **sáltate B1** y ve directo a la maestra (B2) adjuntando
`jefe3.jpeg`. Si quieres verlo con otro aspecto, B1 pide tres variantes que conservan lo que
funciona.

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
