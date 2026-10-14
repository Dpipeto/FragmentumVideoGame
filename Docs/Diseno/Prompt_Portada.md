# Prompt de la portada del menú principal

> Pedido el 2026-10-13: el menú tenía un fondo plano. Elegiste una **imagen nueva**; mientras no
> llegue, el menú usa el plano 7 de la cinemática inicial (Kael ante el páramo y los cinco
> fragmentos volando hacia sus mundos).
>
> Las reglas son las de siempre (`Prompts_Mundo2.md`, apartado 0): una imagen por mensaje, todo
> guardado en disco y el prompt en inglés. Esta va **opaca**: sin magenta, es un fondo entero.

---

## Decisiones mías, por si quieres cambiarlas antes de generar

- **El tercio izquierdo, oscuro y tranquilo.** Ahí van el título, las seis opciones y los
  créditos, sobre un degradado negro que pone el juego. Si la imagen tiene algo importante a la
  izquierda, lo tapa el menú.
- **Kael de espaldas, a la derecha del centro**, pequeño frente a lo que le espera: como en la
  cinemática inicial, el que juega se pone en su sitio.
- **Los cinco mundos en el horizonte**, cada uno con su color: la fortaleza volcánica (rojo), la
  ciudad sumergida (azul), el bosque (verde), el campo de batalla (ocre) y el jardín (dorado). Es
  la promesa del juego entera en una imagen, y el jugador reconocerá cada mundo al llegar a él.
- **Los cinco fragmentos flotan alrededor de Kael**, todavía apagados: el menú es antes de
  integrar ninguno.
- **480 × 270 píxeles de arte**, la rejilla de las cinemáticas de la Doncella: en 1920 × 1080 se
  ve a ×4, sin estirar.

## El prompt

Guárdala en `Tesis/Imagenes/Portada/Portada.jpg`.

**Adjunta, en este orden:**
1. `Tesis2.0/Assets/Sprites/Cinematicas/Inicial/Plano_07.png` (el estilo, la paleta, el tamaño del
   píxel y el páramo);
2. `Tesis/Imagenes/Mundo2/Cinematicas/SalidaTristeza2.jpeg` (Kael de espaldas con su abrigo y sus
   huecos).

```text
Pixel art key art for a video game title screen, 16:9, EXACTLY 480 art pixels wide and 270 art
pixels tall: every art pixel is a solid square block of the same size. Same style, same muted
palette and same art-pixel size as the FIRST attached image. The hero is the one in the SECOND
attached image, seen from behind: same light hair, same dark worn coat, the same empty holes in
his body. Hard-edged pixels, NO blur, NO text, NO logo, NO border, NO letterbox bars.

A vast grey wasteland of cracked stone and rubble at night, seen from a low rocky ledge in the
foreground. The hero stands small on the ledge, slightly RIGHT of the centre, seen from behind,
looking at the horizon. Five small dim crystal shards float in a loose circle around him, not
glowing yet. On the far horizon, spread from the centre to the right edge, five distant worlds
side by side, each a small silhouette with its own light: a volcanic fortress with red lava
glow, a sunken city with cold blue light, a dark forest with green mist, a ruined battlefield
with ochre dust, and a garden with soft golden light. A dark navy sky with sparse single-pixel
stars above them.

The LEFT THIRD of the image is calm and dark: only empty night sky and dark rocks, no figures,
no bright lights, no details that draw the eye, so that a title and a menu can be written over it.
```

## Cuando la tengas

Pásamela (o dime que está en `Tesis/Imagenes/Portada/`). Yo la limpio a su rejilla y la pongo en
`Assets/Sprites/Interfaz/FondoMenu.png`: el menú y el selector la usan solos en cuanto existe, en
lugar del plano 7 (`GeneradorProgresion.RutaFondoMenu`).
