# PENDIENTES — Fragmentum: Ecos de la Identidad

> **Versión 0.14 — Ajustes tras la primera prueba del Nivel 1, 2026-10-02.** Este archivo vive en el proyecto **v2** (`Tesis2.0`).
> Lista viva de lo que falta: arte, audio, texto, escenas y decisiones. Se actualiza en cada fase.

## ▶ Lo que necesito de ti

Nada de esto bloquea el trabajo: lo que falte usa placeholders. Ordenado por urgencia. Lo marcado
**en curso** ya lo estás haciendo: no hace falta que me contestes.

| # | Qué | Por qué | Cómo |
|---|---|---|---|
| N16 | **Pelea otra vez con la Centinela Ardiente** en el Nivel 1 y dime si ya se siente la ira. | La hice más difícil: combos de martillazos, salto aplastante, ondas dobles y una tercera fase de «ira desatada» al 25 % (ver la Revisión 0.14). Ahora hay que ver si me pasé. | Juega desde `Menu_Inicial`. Si es demasiado, dime en qué ataque mueres. Las fases están en `Assets/Data/Jefes/CentinelaArdiente.asset` y los ataques (avisos, golpes, daño) en `Assets/Data/Jefes/Patrones`. |
| N17 | **Mira la presentación del jefe** al entrar en la arena y **escribe su diálogo**. | Ya funciona: se cierra la puerta, la cámara va hacia la Centinela y aparecen su nombre y una línea sobre la arena oscurecida. Dura 7,5 s y se salta con cualquier tecla pasado 1 s. El texto es provisional. | Escribe el texto en `Assets/Data/Narrativa/Cinematica_Jefe_Ira.asset`: cada diapositiva tiene texto, carteles con tiempos y, si se dibuja, imagen. Puedes añadir diapositivas. Si prefieres pasarme el texto, lo vuelco yo. Detalle en B1. |
| N15 | **Prueba el golpe del Eco de la Ira**, ahora más rápido. | El ataque va a 1,6×: a1 dura 0,22 s, el golpe 0,08 s y el ataque entero 0,46 s. Descansa 0,45 s entre golpes, antes 0,7. | Pelea con él en el Nivel 1 (x ≈ 12). Si sigue lento, sube `velocidadAtaque` en `EcoIra.prefab → Pivote/Sprite → EnemigoAnimacion` o baja `attackCooldown` en `Assets/Data/Enemigos/EcoIra.asset`. |
| N18 | **¿Querías el Eco más grande?** En el Nivel 1, el Eco de x = 12,5 tiene escala 1,8 × 1,5. | Lo cambiaste en la escena y lo dejé tal cual. Con una escala distinta en X y en Y, los píxeles del dibujo quedan un 20 % más anchos que altos. Además, ese Eco mide el 90 % de Kael y los demás el 60 %. | Si lo quieres más grande en todo el juego, dime cuánto (por ejemplo, «al 80 % de Kael») y lo cambio en la importación, con la misma escala en X y en Y. Si fue una prueba, vuelve a poner la escala a 1 en la escena. |
| N7 | **Dibuja el Golem de Ceniza** (ficha en B4). | Me pediste cómo es para diseñarlo. Hoy es una cápsula marrón. | La ficha de B4 tiene su tamaño, su carácter, cada animación con sus tiempos y el alcance del golpe. Formato, igual que el Eco. |
| N3 | *En curso.* **Clips de Kael que faltan:** daño y muerte primero; después dash, wall slide y agacharse (y, para la Fase 10, nado). | Daño y muerte se ven en cada partida; hoy daño sigue el clip anterior y la muerte usa Idle. | En Pixelorama, con el mismo lienzo y tamaño de Kael, exporta cada tira **a 1× y sin ping-pong** a `Assets/Sprites/Kael`, o pásame los PNG. Detalle en B3. |
| N8 | *En curso.* **Arte de los demás enemigos y del jefe.** | Solo el Eco de la Ira tiene arte. | Ver «Arte que más necesito», justo debajo. |
| N11 | *En curso.* **Guion:** el diálogo de la presentación de cada jefe (N17), los cierres de cada mundo y las líneas de Kael y de los Arquitectos. | Siguen con textos marcados como «pendiente de guion». | Edita los assets `Cinematica_*` y `Dialogos_*` de `Assets/Data/Narrativa` (detalle en B1 y B2), o pásame el texto. Prioridad: el mundo de la Ira. |
| N10 | *Para más adelante.* **Pinta el layout definitivo del Nivel 1** (~80 u, Anexo A). | El recorrido sigue siendo el provisional: suelo plano con dos fosos y una plataforma por sección. | Sigue los «Pasos manuales en el Editor» de las Fases 6 y 9 en `CHANGELOG.md`. No bloquea. |
| N5 | **Decide la tecla de «caminar»** para el sigilo del mundo del Miedo. | Con teclado la dirección es todo o nada: el jugador siempre corre y siempre haría ruido. | Propuesta: mantener **Alt izquierdo** para caminar al 35 %. Se implementa en la Fase 10. |

### Arte que más necesito, por urgencia

1. **Centinela Ardiente**, el jefe del Nivel 1. Es lo más visible que sigue siendo una cápsula, y
   ahora tiene más ataques que leer: el jugador tiene que distinguir el martillazo, el golpe al
   suelo, el salto y el rugido por la pose. La ficha está en B5.
2. **Kael: daño y muerte** (N3). Se ven en cada partida.
3. **Golem de Ceniza**, el mini-jefe (N7). La ficha está en B4.
4. **Lava animada, torreta de fuego con su bola, plataforma inestable y plataforma de
   combustión.** Se ven por todo el nivel (B10). Pueden ser tiles o tiras cortas en bucle.
5. **Imágenes para la presentación de la Centinela y para el cierre de la Ira.** Son opcionales:
   la presentación ya se ve bien sobre la arena sin imágenes (B1).

### Resuelto en la v0.14

- **N1** (control): está bien, no se toca.
- **N4:** E se **pulsa**, no se mantiene. Ya está cambiado en el Input.
- **N6:** `main` está al día hasta `enemigo-eco-ira`. Las ramas se conservan y no se ha subido
  nada. La rama nueva `ajustes-nivel1` (234 EditMode y 99 PlayMode correctas) queda para
  fusionar cuando la pruebes: dime «fusiona» y lo hago.
- **N9:** el Golem de Ceniza ya no muere en la lava (la cruza andando) y la Centinela es más
  difícil (sigue en N16).
- **N12:** el registro ya no pide código de participante. La pregunta se queda como está.
- **N13:** la hitbox de la espada se queda como está.
- **N14:** el fondo se queda como está. Los remates del muro siguen siendo opcionales (B7).

## Revisión 0.14: qué cambió respecto de la v0.13

- **Rama `ajustes-nivel1`**, con lo que salió de tu prueba:
  - el golpe del Eco es más rápido;
  - el Golem es inmune a la lava;
  - la Centinela tiene combos, salto, ondas dobles y una tercera fase;
  - al entrar en la arena hay una presentación del jefe con su diálogo;
  - E se pulsa y el registro no pide código.

  El detalle está en `CHANGELOG.md`, entrada «Ajustes tras la primera prueba del Nivel 1».
- **Ramas fusionadas en `main`** hasta `enemigo-eco-ira`.
- **Nuevos N16, N17 y N18.** N7 pasa a ser la ficha del Golem. N1, N4, N6, N9, N12, N13 y N14
  están resueltos.
- **Nueva lista «Arte que más necesito».** B1, B2, B4 y B5 están al día, con las fichas del Golem
  y de la Centinela.

## Revisión 0.13: qué cambió respecto de la v0.12

- **El Eco de la Ira tiene arte y animaciones** (rama `enemigo-eco-ira`): idle, correr, ataque y
  muerte con los tiempos del encargo.
  - El golpe solo hace daño en a2 y a3, por eventos de animación.
  - Parpadea en blanco al recibir un golpe.
  - Al morir suelta brasas y ceniza desde el pecho y se funde.
  - Sustituye al Soldado de lava provisional del Nivel 1 y tiene una zona en `PruebaEnemigos`.
- **Nuevo N15:** pelea con el Eco y dime si su velocidad se siente bien.
- **N8 reescrito** para el resto de enemigos. N6 incluye la rama nueva. B4 y B5 están al día.

## Revisión 0.12: qué cambió respecto de la v0.11

- **El Nivel 1 tiene fondo con parallax** (rama `fondo-mundo1`): cielo, montañas, la fortaleza,
  el muro, una catarata de lava sobre él y el primer plano de rocas, cada capa a su velocidad.
  - El muro va en tramos: en el Nivel 1 tapa la arena del jefe.
  - Hay una fortaleza (no tres): el nivel es corto para su tamaño.
  - El prefab `Assets/Prefabs/Fondos/FondoMundo1.prefab` sirve para cualquier nivel de la Ira, y
    reparte sus hitos según la longitud del nivel.
- **Nuevo N14:** mira el fondo en movimiento y, si quieres, dibuja los remates del muro.
- B7 está al día.

## Revisión 0.11: qué cambió respecto de la v0.10

- **La cinemática inicial está completa** (rama `cinematica-y-mapa`): 12 planos con fundidos
  cruzados, los textos del guion con sus tiempos y salto con cualquier tecla pasados 2 s. Las de
  cierre usan el mismo sistema: basta con rellenar su asset.
- **El escenario se ve con el tileset volcánico** en el Nivel 1 y en las escenas de prueba. Solo
  cambia el dibujo: las colisiones son las de siempre.
- **N10** pasa a ser el layout definitivo, y **N11** ya no incluye la cinemática inicial.
- B1 y B7 están al día.

## Revisión 0.10: qué cambió respecto de la v0.9

- **Kael ya tiene arte** (rama `arte-kael`): quieto, correr, salto con caída y ataque, a partir de
  las hojas de Pixelorama. Al probar N1 ya se ve animado.
- **N3** pasa a ser «dibuja los clips que faltan» (dash, wall slide, agacharse, daño y muerte).
- **Nuevo N13:** ¿la hitbox de la espada debe cubrir el arco del dibujo?
- **Convención de escala:** Kael va a 64 px por unidad, no a 32 (ver «Convención de formato»).
- B3 está al día.

## Revisión 0.9: qué cambió respecto de la v0.8

- **El mundo de la Ira tiene sus mecánicas** (Fase 9): plataforma inestable, plataformas de combustión, lava y torreta de fuego.
  - Son prefabs provisionales en `Assets/Prefabs/Mecanicas`.
  - Ya están colocadas en el recorrido provisional del Nivel 1.
- **N9** incluye probar las mecánicas y una pregunta: ¿el Golem de Ceniza debe poder morir en la lava?
- **N10** explica cómo recolocar las mecánicas al pintar el nivel.
- B4, B6 y B10 están al día.

## Revisión 0.8: qué cambió respecto de la v0.7

- **Hay narrativa** (Fase 8). La cinemática inicial se ve antes del Nivel 1, solo la primera vez. Cada mundo tiene su cinemática de cierre y sus diálogos internos, todo con texto pendiente de guion.
- **Hay registro emocional** al terminar cada nivel: escala de 1 a 5, comentario y código de participante opcionales. Se guarda en `registro_emocional.json` y `.csv` junto a la partida, y se puede omitir.
- **Nueva pantalla «yo reconstruido»**, con botón en el menú principal.
- **Nuevos N11** (escribir el guion) **y N12** (revisar la pregunta del registro). B1, B2 y B9 están al día con lo que ya existe.

## Revisión 0.7: qué cambió respecto de la v0.6

- **Hay jefes** (Fase 7). La Centinela Ardiente está completa y espera en la arena del Nivel 1 (x ≈ 61). Los otros cuatro son esqueletos con datos y prefab (B5).
- **La salida del Nivel 1 aparece al derrotar a la Centinela.** El fragmento y el Ataque Cargado se obtienen en ese mismo momento.
- **Nuevo apartado B5** para la v2, con dónde va el arte de cada jefe. B8 anota que la música del jefe ya tiene dónde asignarse.
- **N9** incluye ahora el combate contra el jefe.

## Revisión 0.6: qué cambió respecto de la v0.5

- **El Nivel 1 se juega de principio a fin** (Fase 6). Tiene jugador, checkpoint en x = 40, salida al final y un recorrido provisional con dos enemigos de la Ira, que se borra al pintar el nivel.
- **Hay pausa (Esc o Start), Game Over y fin de nivel**, con la estética de la maqueta `Referencias/Menus`. La viñeta y el icono de pausa se generan por código: ya no hacen falta como assets (B9).
- **Nuevo N10:** pintar el layout del Nivel 1. N9 pasa a ser «juega el Nivel 1 de principio a fin».
- **Checkpoint y salida** tienen un aspecto provisional: una bandera que se tiñe y un portal translúcido (B10).
- **La música** de cada nivel se asigna en su `LevelData`. Hoy ninguno tiene, así que suenan en silencio (B8).

## Revisión 0.5: qué cambió respecto de la v0.4

- **Hay progresión** (Fase 5): menú principal con «Continuar», selector con los 6 mundos, guardado en JSON y FSM global del juego. Los datos del Anexo A están en `Assets/Data` (emociones, habilidades, fragmentos, jefes y niveles).
- **N2 resuelta:** la v1 (`Tesis`) quedó actualizada a 6000.6.3f1 en su propio commit (`ea50657`), como recomendaba.
- **Gracias por la primera prueba en el Editor.** De ella salieron dos correcciones: un doble clic en el selector ya no pide dos cargas, y abrir `Menu_Inicial` ya no la marca como modificada.
- **Nuevo N9:** probar el flujo completo desde el menú.
- **Apartado B6 reescrito para la v2**: seguía describiendo las escenas de la v1.
- **La UI usa el `Text` integrado de Unity** porque los recursos esenciales de TextMeshPro no están importados. Queda anotado en B9.

## Revisión 0.4: qué cambió respecto de la v0.3

- **Los enemigos ya existen** (Fase 4): prefab base `Assets/Prefabs/Enemigos/Enemigo.prefab`, 5 variantes, datos en `Assets/Data/Enemigos` y escena `Assets/Scenes/Pruebas/PruebaEnemigos.unity`. Los huecos de arte de los enemigos quedan concretados en el apartado B4.
- **Matriz de colisión:** el cuerpo del jugador ya no choca con el de los enemigos, ni los enemigos entre sí. El daño llega por la hitbox de contacto.
- **Nuevo menú** *Fragmentum → Regenerar todo*: reconstruye los prefabs, las escenas de prueba y los diagramas.

## Revisión 0.3: qué cambió respecto de la v0.2

- **Plataforma:** el proyecto quedó en **Unity 6000.6.3f1**, la versión instalada en el PC nuevo. Se actualizó en un commit aparte.
- **El jugador ya existe** (Fase 3): prefab `Assets/Prefabs/Player.prefab`, escena de prueba `Assets/Scenes/Pruebas/PruebaJugador.unity` y FSM completa. Los huecos de arte del jugador quedan concretados en el apartado B3.
- **Git** está instalado en este PC. Los dos repositorios tienen la identidad de los commits anteriores y finales de línea LF.
- **Input:** nueva acción `Ability` (Q o botón RB) y Ctrl izquierdo como segunda tecla de agacharse.
- **Escena `Nivel1`:** tiene 2 tiles pintados y el Tilemap todavía no colisiona. Cuando la pintes, usa *Fragmentum → Jugador → Preparar tilemap seleccionado como suelo*.

## Revisión 0.2: qué cambió respecto de la v0.1

- El trabajo se trasladó a **`Tesis2.0`** (Unity 6000.6.0f1). `Tesis` queda congelado como v1 de referencia.
- **Las decisiones D1 a D11 están resueltas** (ver más abajo): quedan como registro, no como preguntas.
- **Escala fijada:** el tileset nuevo son 32 px por tile a 32 PPU, así que **1 tile = 1 unidad**. Todo el arte que se importe desde la v1 hay que reimportarlo a esa escala. Las hojas de Kael de la v1 estaban a 500 PPU y el jugador dependía de un `localScale` de 0,215; eso desaparece.
- **Tags y capas ya creados** en el proyecto por el menú *Fragmentum → Configurar proyecto*: tags `Enemy`, `Boss`, `Weapon`, `suelo`, `Lava`, `HotPlatform`, `IcePlatform`, `FalsePlatform`, `Checkpoint` y `Memory`; capas `suelo` (6), `Wall` (7), `Player` (8), `Enemy` (9), `PlayerWeapon` (10) y `EnemyWeapon` (11), con la matriz de colisión del Anexo A aplicada.

### Arte nuevo generado con Gemini (carpeta `Referencias/Arte/`)

Son **hojas de referencia en JPEG**, no sprites listos: llevan el cuadriculado de transparencia
incrustado en la propia imagen, así que **hay que recortar cada pose y quitar el fondo** antes de
importarlas. Eso no lo puedo hacer yo: no genero ni edito imágenes.

| Archivo | Qué contiene | Qué hay que hacer | Prioridad |
|---|---|---|---|
| `tileset_32 (1).png` | Tileset volcánico de basalto con lava | **Ya importado** en `Tesis2.0` con su paleta | — |
| `posiblesanimaciiones kael.jpeg` | 8 poses de Kael: quieto, correr, saltar, caer, atacar, dash, herido y derrota | Recortar pose a pose, fondo transparente, PNG a 32 px por unidad, y montar los clips | P1 (fase 3) |
| `AnimacionesGeneralesEnemigos.jpeg`, `EnemigosNiveles.jpeg` | Enemigos genéricos y por nivel | Igual | P1 (fase 4) |
| `Jefes/Jefe1.jpeg`, `Fase2Jefe1.jpeg`, `PosiblesAnimacionesJefe1.jpeg` | Centinela Ardiente y su fase 2 | Igual | P2 (fase 7) |
| `Jefes/jefe2.jpeg`, `jefe3.jpeg`, `Jefe4.jpeg`, `JefeFinal.jpeg` | Los otros cuatro jefes | Igual | P2 |
| `Jefes/cinematicJefe1.jpeg`, `CinematicaJefefinal.jpeg` | Cinemáticas (imágenes fijas) | Recortar a 16:9 y guardar como PNG; van en `CutsceneData` | P2 (fase 8) |
| `Jefes/fragmentos de alma.jpeg` | Iconos de los fragmentos de identidad | Recortar uno por fragmento | P2 |
| `Mapas/` (9 imágenes) | Capas de parallax: cielo, plano lejano, medio, cercano y primer plano, en dos variantes | Recortar, decidir qué variante es canónica y marcar las capas que deben repetirse en horizontal | P1 (fase 3, fondo del Nivel 1) |

### Maquetas de menús (`Referencias/Menus/`)

`fragmentum_pause_worlds.jsx` es un prototipo en React con una escena SVG animada por cada uno de
los 6 mundos y su paleta. **Es la referencia visual de los menús de pausa y de muerte**, que se
traducen a uGUI en la Fase 6 (`UIManager`, `MenuPausa` y `PantallaGameOver`), con un fondo por
mundo tomado de `LevelData.paleta`. No es código de Unity y no se importa.

Falta por decidir: si el fondo de cada menú será el SVG convertido a imagen, o un degradado
generado en Unity a partir de la paleta del nivel. Lo segundo no necesita ningún asset nuevo.

---

**Sigue faltando por completo:** los diálogos escritos, la música de los niveles 2 al final, **todos**
los efectos de sonido, los iconos de la UI y el concepto del jugador con sus habilidades en acción.

---

**Leyenda de prioridad**
- **P1** — bloquea el Nivel 1 (Ira) como implementación de referencia.
- **P2** — bloquea otro nivel o un sistema narrativo.
- **P3** — pulido: el sistema funciona con placeholder.

**Convención de formato** (fijada en la v2)
- Tiles: 32 px a 32 PPU, así que 1 tile = 1 unidad (`Grid.cellSize = 1`). Filtro *Point*, sin compresión.
- Sprites de personaje: **a 64 PPU** (desde la v0.10; antes decía 32). Kael mide unos 113 px de arte, que a 64 PPU son 1,77 u, la altura de su collider (1,8 u); tiene el doble de detalle que el escenario. Tiras horizontales exportadas a escala 1×, con el pivote en los pies. El sprite puede sobresalir del collider, pero no debe depender de `localScale` (en la v1 dependía de 0,215 y eso rompía el wall jump).
- Imágenes de cinemática: PNG 16:9, 1920×1080, o 480×270 escalado ×4 con filtro *Point* si se quiere pixel art.
- Música: OGG, 44,1 kHz, estéreo, con loop limpio. SFX: WAV mono, 44,1 kHz, menos de 2 s.

---

## A. Decisiones (resueltas; se conservan como registro)

Las decisiones abiertas están arriba, en «Lo que necesito de ti».

| # | Decisión | Evidencia encontrada | Resolución |
|---|----------|----------------------|------------|
| D1 | **Versión de Unity.** El documento dice 2022 LTS; la v1 estaba en **Unity 6000.2.14f1** (Unity 6.2). | `ProjectSettings/ProjectVersion.txt`. El código ya usaba `linearVelocity`, que solo existe en Unity 6. | La v2 usa **Unity 6000.6.3f1** (desde la Fase 3; antes 6000.6.0f1). Hay que corregir la versión en la tesis. |
| D2 | **Control de versiones.** La carpeta **no era un repositorio git**. | No había `.git`. | Hecho: los dos proyectos están en git, con una rama por fase. |
| D3 | **Input System.** El paquete ya estaba instalado; el gameplay de la v1 usaba el `Input` clásico. | `Packages/manifest.json`, `ProjectSettings.asset`, escenas. | En la v2 el Input System es el **único** sistema de entrada, detrás del adaptador `IFuenteInput` (`FuenteInputJugador`). |
| D4 | **Título canónico.** | `Assets/Images/tituloxd.png` dice «FRAGMENTUM — *ECHOES OF IDENTITY*». No existe ningún texto en código ni escenas con otro título. | **Fragmentum: Ecos de la Identidad**. El logo actual es su versión en inglés. |
| D5 | **Jefe de Ira: Centinela Ardiente / Pyrokratos.** | No aparece ninguno de los dos nombres en el proyecto. | **Centinela Ardiente**, como en el GDD. |
| D6 | **Jefe de Culpa: Juez Silente / Juez Silencioso.** | Ninguno aparece en el proyecto. | **Juez Silente**, como en el GDD. |
| D7 | **Nivel 5: Esperanza / Aceptación.** | Ninguno aparece en el proyecto. | Emoción **Esperanza**, con «aceptación» como descriptor del `LevelData`. |
| D8 | **Nombre de la escena del menú.** Build Settings y código usan `Menu_Inicial`; el Anexo A dice `MainMenu`. | `EditorBuildSettings.asset`, `GameManager.cs`, `Pause.cs`. | Se usa `Menu_Inicial`, y el nombre vive solo en `LevelDatabase` (Fase 5). En la v2 las escenas se crean de cero. |
| D9 | **Correspondencia de conceptos de jefe.** | `Jefe2 (2).png` es una figura encapuchada azul/cian con esquirlas. `Jefe3 (2).png` es una figura oscura con espada y brillo verde azulado. | Suposición: Jefe2 = Doncella del Lamento (Tristeza), Jefe3 = Espectro del Pavor (Miedo). **Confirmar.** |
| D10 | **Tecla E.** En la v1 era el dash. El Anexo A la usa para encender antorchas en el Nivel 3. | `PlayerMovement.cs:121` | Dash en **Shift izquierdo** (acción *Sprint*); E es «interactuar». Invisibilidad en **Q** (acción *Ability*). |
| D11 | **Idioma de la UI.** El menú está en inglés («Play», «Level Selector», «Configuration», «Exit»); pausa y Game Over mezclan español e inglés. | Textos TMP de las escenas. | Todo en español, con textos en datos. |

---

## B. Assets faltantes por sistema

### B1. Narrativa y cinemáticas — `CutsceneData` + `PantallaCinematica` (Fase 8)

**Estado en la v2.** El sistema funciona: las cinemáticas se reproducen, se pueden saltar y
recuerdan si ya se vieron. Todas existen en `Assets/Data/Narrativa`, enlazadas a su nivel, con
**texto pendiente** y sin imágenes: se ven en negro con el texto. Para completarlas, edita el
texto y la duración de cada diapositiva y arrastra la imagen al campo `imagen`.

| Cinemática (asset) | Dónde suena | Falta | Prioridad |
|---|---|---|---|
| `Cinematica_Inicial` (solo la primera vez) | Antes del Nivel 1 | ✔ 12 planos y guion (80 s). Falta, opcional, la música o el sonido (campo `musica` del asset, o `audio` de cada plano) | P2 |
| `Cinematica_Jefe_Ira` (~7,5 s, sobre el juego) | Al entrar en la arena de la Centinela, antes del combate (una vez por carga del nivel) | Guion del diálogo (N17). Las imágenes son opcionales: sin ellas se ve la arena oscurecida con franjas de cine | P1 |
| `Cinematica_Jefe_Tristeza`, `_Miedo`, `_Culpa` | Al entrar en la arena de cada jefe | Guion; imágenes opcionales | P2 |
| `Cinematica_Cierre_Ira` | Al superar el mundo de la Ira | Guion e imágenes | P1 |
| `Cinematica_Cierre_Tristeza`, `_Miedo`, `_Culpa` | Al superar cada mundo | Guion e imágenes | P2 |
| `Cinematica_Cierre_Jardin` (~30 s: Kael ve al Soldado) | Al superar el Jardín | Guion e imágenes (la idea, del Anexo A, ya está) | P2 |
| `Cinematica_Entrada_Final` (~45 s) | Antes del final | Guion e imágenes (la idea, del Anexo A, ya está) | P2 |
| `Cinematica_Final` y créditos («¿Eres un error… o una nueva identidad?») | Al terminar el juego | Imágenes; texto del Anexo A ya incluido | P2 |
| Memorias de los Niveles 4 y 5 (4 + 4, ~5 s) | Al activar cada memoria (`MemoryObject`, Fase 10) | Todo: se crearán con la mecánica | P2 |

> **Presentación del jefe.** Lleva `sobreElJuego` marcado: detrás se ve la escena congelada, con
> un velo (`opacidadVelo`) y franjas de cine. Para que la cámara encuadre al jefe y el combate
> empiece al terminar, basta con que la cinemática esté en el campo `cinematicaJefe` del
> `LevelData`. Su diálogo sustituye a las líneas «antes del jefe» del HUD.
>
> La v2 no usa Timeline: las cinemáticas son diapositivas. Si alguna necesita animación, una
> diapositiva puede apuntar más adelante a un clip de Timeline sin cambiar el resto del sistema.

### B2. Diálogos — `DialogueData` + `DialogueManager` (Fase 8)

**Estado en la v2.** Cada mundo tiene su `Dialogos_<Emoción>` en `Assets/Data/Narrativa`, con una
línea pendiente por momento: apertura, eco de memoria, antes del jefe (si hay jefe) y cierre.
Todas nombran ya la emoción del mundo. Se muestran abajo en el HUD (el cierre, en el fin de
nivel).

| Asset | Dónde | Formato | Prioridad |
|---|---|---|---|
| Líneas de Kael y de los Arquitectos, **×6 mundos**. Pueden ser varias por momento: se muestran en orden. Las de «antes del jefe» solo se usan en un nivel sin presentación del jefe (B1). | `Dialogos_*` → `lineas` (hablante, texto, momento, duración) | Texto | Ira P1, resto P2 |
| Pregunta y escala del registro emocional | `Assets/Data/Narrativa/RegistroEmocional.asset` | ✔ Revisadas (N12). Sin código de participante (`pedirParticipante` apagado) | — |
| Retratos opcionales (Kael, los 5 Arquitectos y el Soldado) | Pendiente de campo (hoy solo se muestra el nombre) | PNG 256×256 | P3 |

### B3. Jugador — Kael

**Estado en la v2 (arte de Kael, 2026-09-30).** El jugador está completo en lógica (prefab
`Assets/Prefabs/Player.prefab`, FSM de 12 estados) y **ya tiene arte**: quieto, correr, salto con
caída y ataque, dibujados en Pixelorama. El detalle está en el CHANGELOG, entrada «Arte de Kael».

- **Dónde está el arte:**
  - las hojas, en `Assets/Sprites/Kael`, a escala 1× y 64 PPU;
  - los clips y `Kael.controller`, en `Assets/Animaciones/Kael`, generados por
    *Fragmentum → Jugador → Importar arte de Kael y regenerar el prefab*.
- **Quién elige el clip:** `PlayerAnimacion`, con una tabla estado de la FSM → clip que se edita
  en el Inspector. El Animator no tiene transiciones propias.
- **Parámetros que escribe el código:**
  - `Movement` (float, u/s) acelera el clip de correr;
  - `VelocidadAtaque` (float) estira el de ataque a la duración del golpe;
  - `Attack`, `Damage` y `Death` (bool) se conservan del prototipo, pero ningún clip depende de
    ellos.
- **Cómo añadir un clip:** en el CHANGELOG, «Arte de Kael» → *Pasos manuales en el Editor*.
  Resumen: exporta la tira a 1× y sin ping-pong a `Assets/Sprites/Kael`, añádela a las tablas de
  `ConstructorArteKael` y apunta el estado en `PlayerAnimacion`.
- **Arte fuente:** los `.pxo` y las exportaciones a 5× y 10× están en `Tesis/Imagenes Kael` (los
  `.pxo` no se versionan). Si cambias el lienzo de una hoja, ajusta su pivote en
  `ConstructorArteKael`; la prueba del pivote avisa si no cuadra.

| Clip de Kael | Estado de la FSM | Prioridad | Estado actual |
|---|---|---|---|
| Idle | `IDLE` | P1 | ✔ 2 fotogramas |
| Correr | `RUN` | P1 | ✔ 8 fotogramas, al ritmo de la velocidad real |
| Salto / caída / doble salto | `JUMP`, `FALL`, `DOUBLE_JUMP` | P1 | ✔ 9 fotogramas: 6 de subida y 3 de caída (2 en bucle) |
| Ataque básico | `ATTACK` | P1 | ✔ 7 fotogramas, estirados a la duración del golpe. Ver N13 sobre la hitbox |
| Carga y ataque cargado (Ira) | modificador de `ATTACK` | P1 | ✔ El mismo clip, más lento, con el tinte rojizo de la carga |
| Daño | `DAMAGED` | P1 | Sigue el clip anterior y parpadea. Falta un fotograma de golpe |
| Muerte | `DEAD` | P1 | Usa Idle. Falta su clip |
| Dash | `DASH` | P2 | Usa Correr, que se queda fijo en un fotograma. Falta su clip |
| Wall slide / wall jump | `WALL_SLIDE`, `WALL_JUMP` | P2 | Usan Caida y Salto. Falta el de wall slide (pegado a la pared) |
| Agacharse | `CROUCH` | P2 | Usa Idle, así que el sprite no se encoge con el collider. Falta su clip |
| Nado (Tristeza) | modificador de movimiento `BajoElAgua` | P2 | Usa Correr, más lento en el agua |
| Invisibilidad parcial (Miedo) | habilidad | — | Opacidad al 40 % (no necesita arte) |
| Defensa reforzada (Culpa) | escudo de `PlayerHealth` | P2 | Sin efecto visual: falta un VFX de escudo |

**Audio del jugador (todo falta, P1 con silencio como placeholder):** pasos, salto, doble salto,
dash, espada, carga completa, ataque cargado, golpe recibido, escudo absorbe y muerte. Los eventos
ya existen (`OnStateChanged`, `OnGolpeRecibido`, `OnEscudoAbsorbio`, `OnCargaCompleta`,
`OnInvisibilidadCambiada`); el `AudioManager` los escuchará.

### B4. Enemigos por mundo — `EnemyData` + prefabs (Fase 4)

**Estado en la v2.** La lógica está completa: FSM de 6 estados, 4 estrategias de detección y
7 `EnemyData`. **El Eco de la Ira tiene arte y animaciones** (`EcoIra.prefab`). Los demás son
cápsulas tintadas con un color de la paleta de su mundo.

- **Cómo se monta un enemigo con arte (el Eco es el modelo):**
  - fotogramas PNG con alfa en un lienzo común, alineados en los pies, en
    `Assets/Sprites/Enemigos/<nombre>`;
  - un controlador con los parámetros `MovEn`, `Attack`, `Damage`, `Death` y `VelocidadCorrer`;
  - `EnemigoAnimacion` en el objeto del Animator, y eventos `AbrirGolpe`, `CerrarGolpe`,
    `FinAtaque` y `FinMuerte` en los clips.

  Ver `ConstructorEcoIra` y la entrada del Eco en `CHANGELOG.md`.
- **Otros Ecos con los mismos clips:** prefab propio con su `EnemyData` y su
  `velocidadAnimacion`, y si cambia el dibujo, un *Animator Override Controller*.
- **Orientación del arte:** si mira a la izquierda, desmarca `spriteMiraDerecha` en su `EnemyData`.
- **Color:** cuando tenga arte final, pon `tinte` en blanco.
- **Arte fuente:** `Referencias/Arte/EnemigosNiveles.jpeg` y `AnimacionesGeneralesEnemigos.jpeg`
  (hay que recortarlas). Los packs de la v1 (FireWorm, Golems, Bringer of Death…) sirven como
  alternativa, reimportados a 32 PPU.

| Mundo | Enemigo | Datos y prefab | Arte | Falta además | Prioridad |
|---|---|---|---|---|---|
| — | Genérico | ✔ `Generico` / `Enemigo.prefab` | Cápsula gris | — | — |
| Ira | **Eco de la Ira** | ✔ `EcoIra` / `EcoIra.prefab`; en el Nivel 1 y en `PruebaEnemigos` | ✔ 12 fotogramas, 4 clips (Idle, Run, Attack, Death). El ataque va a 1,6× | Audio; probar el golpe nuevo (N15) | P1 |
| Ira | Soldado de lava | ✔ `SoldadoLava` / `Enemigo_SoldadoLava`; el Eco ocupa su sitio en el Nivel 1 | Cápsula naranja | — | P2 |
| Ira | Golem de Ceniza (mini-jefe, vida 10) | ✔ `GolemCeniza` / `Enemigo_GolemCeniza` (más grande). Inmune a la lava | Cápsula ceniza | Arte: ficha abajo (N7) | P1 |
| Ira | Torreta de fuego fija | ✔ Es una mecánica, no un enemigo: `TorretaFuego` / `Mecanicas/TorretaFuego.prefab` (Fase 9) | Bloque oscuro con boca naranja; la bola de fuego es un cuadrado naranja | Sprite de la torreta y de la bola (B10) | P1 |
| Tristeza | Lamento | ✔ `Lamento` / `Enemigo_Lamento` | Cápsula azul | Ralentizar al jugador al tocarlo (Anexo: «reducen velocidad al contacto»), Fase 10 | P2 |
| Tristeza | Esculturas Vivientes, Medusas de Hielo | ✗ | ✗ | Movimiento sinusoidal de las medusas (Fase 10) | P2 |
| Miedo | Espectro | ✔ `Espectro` / `Enemigo_Espectro` (detección en oscuridad) | Cápsula oscura translúcida | Que sea invisible salvo al atacar (Fase 10) | P2 |
| Miedo | Centinela Ciego | ✔ `CentinelaCiego` / `Enemigo_CentinelaCiego` (detección por ruido) | Cápsula violeta | Tecla de caminar con teclado (N5) | P2 |
| Culpa | Penitentes, Artilleros (+ proyectil) | ✗ | ✗ | `Proyectil` y cobertura (Fase 10) | P2 |
| Esperanza | Guardianes del Jardín (pacíficos) | ✗ | ✗ | `GardenGuardian` (Fase 10) | P2 |

#### Ficha: Golem de Ceniza (para dibujarlo, N7)

**Qué es.** El mini-jefe del mundo de la Ira (Anexo A: sección 2 del Nivel 1, vida 10). El Eco es
la ira que estalla: anticipación corta y un latigazo. El Golem es la ira acumulada: lento, pesado,
avisa mucho y pega fuerte. Está hecho de ceniza y magma, así que la lava no le hace nada y la
cruza andando (N9). Apenas retrocede al recibir un golpe.

**Tamaño.**

- **En el juego:** 1,4 u de ancho y 2,4 u de alto. Es 1,35 veces Kael (1,77 u) y más del doble
  que el Eco (1,06 u).
- **Recomendado: a 1×, al detalle de Kael** (64 px por unidad): unos 150 px de alto por 90 de
  ancho de dibujo, un píxel del PNG por píxel del dibujo. Así pesa poco y no «repta» al moverse,
  que es lo que le pasa al Eco (`CHANGELOG.md`, «Notas sobre el arte» del Eco).
- **Si lo dibujas ampliado, como el Eco:** que el lienzo no pase de 2048 px de lado. Yo ajusto la
  importación para que mida 2,4 u.

**Animaciones.** Las mismas que el Eco, así que lo monto igual: Idle, Andar, Ataque y Muerte.

| Clip | Qué hace | Tiempo en el juego | Fotogramas sugeridos |
|---|---|---|---|
| Idle | Quieto, respira; las grietas de magma laten | Bucle | 2–4 |
| Andar | Pasos pesados. Patrulla a 1 u/s y persigue a 2 u/s | Bucle; ajusto el ritmo a la velocidad real, como en el Eco | 4–6 |
| Ataque | Levanta los puños (anticipación muy visible), descarga hacia delante y hacia el suelo, se recupera | 0,9 s: anticipación 0,5 s, golpe 0,2 s, recuperación 0,2 s. Descansa 2 s entre ataques | 4–5. El golpe en 1 o 2 fotogramas |
| Muerte | Se desmorona en ceniza; las brasas se apagan | Hasta 2 s | 2–4 |
| Daño | — | Por código: destello blanco de 0,1 s | Ninguno |

**Alcance del golpe.** El arma cubre de 0,5 a 2,1 u por delante de su centro y de 0,2 a 2,2 u de
altura. El golpe dibujado tiene que llegar hasta ahí: unos 130 px por delante del centro, al
detalle de Kael. Si lo dibujas con otro alcance, ajusto la hitbox al dibujo.

**Formato.** Igual que el Eco, salvo la escala (mejor a 1×):

- PNG con alfa;
- todos los fotogramas en un mismo lienzo y alineados en los pies (la misma fila de suelo);
- mirando a la derecha y sin recortar;
- con el nombre del clip y su número, por ejemplo `I1`, `w1`, `a1` o `d1`.

**Audio de enemigos (todo falta, P2):** pasos, aviso de ataque, golpe, dolor y muerte, por tipo. El
evento `OnStateChanged` de cada enemigo ya permite dispararlos sin tocar la IA.

### B5. Jefes — `BossController` (Fase 7)

**Estado en la v2.** La lógica de los cinco jefes existe: FSM por fases, ventana vulnerable,
derrota que otorga el fragmento y arena. Cada uno tiene datos en `Assets/Data/Jefes` y prefab en
`Assets/Prefabs/Jefes`. **Visualmente** son cápsulas grandes tintadas, y sus ataques son
rectángulos de color: avisan translúcidos y queman casi opacos.

- **Dónde va el arte:** en `Pivote/Sprite` del prefab de cada jefe, con un Animator. Después
  pon `tinte` en blanco en su `JefeData` y, si el dibujo mira a la izquierda, desmarca
  `spriteMiraDerecha`.
- **Ojo con el destello del golpe:** `BossController` pone el sprite en blanco al recibir un
  golpe. Funciona con las cápsulas tintadas, pero con arte final (tinte blanco) no se vería: el
  color multiplica. Habrá que usar la silueta del Eco (`Fragmentum/SiluetaSprite`).
- **Cuando haya VFX:** cada patrón (`Assets/Data/Jefes/Patrones`) tiene su color provisional. Los
  efectos se engancharán a `ZonaDeDanio`, que es la pieza que crean todos los ataques.

| Jefe | Estado | Falta | Prioridad |
|---|---|---|---|
| **Centinela Ardiente** (Ira) | ✔ Completo y más difícil desde la v0.14: tres fases, martillazo en combo, onda doble, salto aplastante y erupciones; presentación al entrar en la arena del Nivel 1 | Sprites y animaciones (ficha abajo) y los VFX de la onda, las erupciones y la caída del salto. En la v1 existen `Characters/Bosses/Boss1/jefe1.1.png` y los clips `Idle`, `Walk` y `Attack`, que pueden servir de partida. | P1 |
| **Doncella del Lamento** (Tristeza) | Esqueleto: datos, prefab y dos fases sin patrones | Patrones de olas, inundación y congelamiento (Fase 10); todas las animaciones. Concepto: `Jefe2 (2).png` en la v1. | P2 |
| **Espectro del Pavor** (Miedo) | Esqueleto: casi invisible en combate | Patrón de ataque con destello y clones (Fase 10); animaciones; la partícula que distingue al real. Concepto: `Jefe3 (2).png` en la v1. | P2 |
| **Juez Silente** (Culpa) | Esqueleto: inmune salvo en la ventana, que abre `ActivarMemoriaCorrecta()` | Proyectar las memorias y conectarlas (`MemoryObject`, Fase 10); todo el arte. | P2 |
| **El Verdadero Soldado** (Final) | Esqueleto: tres fases; no cae sin `CompletarIntegracion()` | Combate espejo, secciones de cada mundo y el plataformeo final (Fase 10); todo el arte (el concepto **no está** en el proyecto). | P2 |

#### Ficha: Centinela Ardiente (para dibujarla)

**Qué es.** La Arquitecta de la Ira y el jefe del Nivel 1. Lleva un martillo. Se enfurece según
pierde vida: más rápida, con menos pausas y, en la última fase, al rojo vivo (el rojo lo pongo yo
por código).

**Tamaño.** 2,2 u de ancho y 3,4 u de alto, casi el doble que Kael. Recomendado a 1×, al detalle
de Kael: unos 220 px de alto. Si la dibujas ampliada, que el lienzo no pase de 2048 px de lado; yo
ajusto la importación.

**Animaciones.** Cada ataque tiene que reconocerse por la pose antes de que llegue: así el jugador
aprende a esquivarlo.

| Clip | Qué hace | Tiempo en el juego | Fotogramas sugeridos |
|---|---|---|---|
| Idle | Quieta, el martillo en la mano | Bucle | 2–4 |
| Andar | Avanza hacia el jugador: 2,4 u/s, que llega a 3,8 en la última fase | Bucle; ajusto el ritmo a la velocidad | 4–6 |
| Martillazo | Levanta el martillo y lo descarga delante | Anticipación: 0,55 s el primero y 0,3–0,35 s los encadenados. Golpe: 0,15 s | 3–4 |
| Martillo clavado | El martillo queda hundido en el suelo y ella atrapada: es cuando se le pega | 1,2 s (0,9 s en furia) | 1–2, más 1–2 para sacarlo |
| Golpe al suelo | Golpea el suelo y salen las ondas | Anticipación 0,5–0,6 s, golpe breve | 2–3 |
| Salto | Se agacha, salta y cae con todo su peso | Agacharse 0,5 s, aire 0,8 s, caída breve | 4–5: agachada, subiendo, cayendo y aterrizaje |
| Rugido | Al empezar las fases 2 y 3 | 0,9 s | 2–3 |
| Muerte | Explosión o derrumbe | 2 s | 3–5 |
| Daño | — | Por código: destello | Ninguno |

**Formato.** El mismo que el Eco y el Golem.

### B6. Escenas — `LevelDatabase` / Build Settings

**Estado en la v2 (Fase 5).** Build Settings se generan desde la `LevelDatabase`
(*Fragmentum → Progresión → Configurar Build Settings*): menú, selector y los niveles que tienen
escena, en ese orden. Un nivel sin escena aparece «en construcción» en el selector; nunca da error.

| Escena | Existe | En Build Settings | Falta | Prioridad |
|---|---|---|---|---|
| `Menu_Inicial` | ✔ generada | ✔ (0) | Fondo y logo (hoy, texto sobre color liso) | P3 |
| `Niveles` (selector) | ✔ generada | ✔ (1) | Íconos por mundo (B9) | P3 |
| `Nivel1` (Ira) | ✔ Preparada (Fase 6): `LevelManager`, checkpoint en x = 40, salida en x = 78, zona mortal y recorrido provisional con un Soldado de lava y el Golem de Ceniza. Fase 9: plataforma inestable, dos plataformas de combustión, lava y torreta sobre el recorrido provisional. Solo tiene 2 tiles pintados. | ✔ (2) | Pintar el layout de ~80 u del Anexo A (N10) y recolocar en él las mecánicas. | P1 |
| Nivel 2 (Tristeza) | ✗ | ✗ | Escena esqueleto (Fase 10) | P2 |
| Nivel 3 (Miedo) | ✗ | ✗ | idem | P2 |
| Nivel 4 (Culpa) | ✗ | ✗ | idem | P2 |
| Nivel 5 (Esperanza) | ✗ | ✗ | idem | P2 |
| Nivel final | ✗ | ✗ | idem | P2 |

Para añadir la escena de un nivel: arrástrala al campo `escena` de su asset en
`Assets/Data/Niveles`, pulsa «Configurar Build Settings desde esta base de datos» en el inspector
de la `LevelDatabase` y ejecuta *Fragmentum → Niveles → Preparar la escena de nivel abierta*.

### B7. Tilesets y fondos por nivel — `LevelData.paleta` / escenas

| Nivel | Disponible | Falta |
|---|---|---|
| 1 Ira | ✔ **Tileset volcánico de la v2** (`Assets/TileMap/tileset_32.png`): ya viste el Nivel 1 y las escenas de prueba con `PintorEscenario` (ladrillo, vigas, estatua, braseros, cadenas y escombros). Su versión grande con fondo negro está en `Tesis/TileMap Nivel 1`. En la v1: tiles de lava (`Tiles/LavaTile set`, `Tiles/freelavatileset-Sheet`) y `Starter Tiles Platformer/FireSet` | ✔ **Parallax volcánico de 6 capas** en `Assets/Sprites/Fondos/Mundo1Ira` (cielo, montañas, fortaleza, muro, catarata y primer plano), montado en `FondoMundo1.prefab`. ~ **Remates del muro** (opcional; el fondo está aprobado, N14): los extremos de cada tramo son un corte recto. ✗ Sprite de plataforma de basalto inestable (candidato: `Pixel Adventure 1/Traps/Falling Platforms`). |
| 2 Tristeza | ✔ `Starter Tiles Platformer/IceSet`, `tileset/Tilemaps/rock_ice*.aseprite`, `Pixel Adventure 1/Traps/Sand Mud Ice` | ✗ Ciudad sumergida, corriente y superficie helada (parallax). ✗ Burbuja de oxígeno (UI). |
| 3 Miedo | ✔ Parallax `TileMap/Forest` (*demon woods*, 4 capas), tileset de pantano `TileMap/ParallaxVideo`, antorcha animada (`Top Down Lava…/Torch ( No Light )`, 7 frames), URP 2D con `Light2D` ya activo | ✗ Raíces y ramas. ✗ Partícula de «ruido» del sigilo. |
| 4 Culpa | ~ `Starter Tiles Platformer/DarkCastle` | ✗ Tierra quemada, escombros, tanques volcados. ✗ **Barril explosivo** (`Explodable`). ✗ Objetos de memoria. ✗ Marcas proyectadas en el suelo. |
| 5 Esperanza | ✔ Fondos `TileMap/Naturaleza` (nature_1…8), `TileMap/Sky-Place`, `Tilemap2/Free Pixel Art Hill`, `BasicGreenTiles` | ✗ Musgo, flores y partículas flotantes (un Particle System puede servir sin arte). |
| Final | ✗ | ✗ Todo, incluida la dualidad oscuro/luminoso. |

### B8. Audio — `LevelData.musica`, `AudioManager` *(propuesto)*

**Estado en la v2 (Fase 6).** El `LevelManager` toca en bucle el clip del campo `musica` del
`LevelData`. Si el campo está vacío, avisa por consola y el nivel suena en silencio. Hoy no hay
ningún clip importado en la v2.

| Asset | Existe | Prioridad |
|---|---|---|
| Música del menú | En la v1: `Music/MainMenu.mp3`. Falta importarla en la v2 y darle un `AudioSource` al menú. | P3 |
| Música del Nivel 1 | En la v1: `Music/level1.mp3`. Para usarla, impórtala y arrástrala al campo `musica` de `Assets/Data/Niveles/L1_Ira`. **Verificar** si cumple el Anexo A (industrial, ~160 BPM). | P1 |
| Música de los niveles 2, 3, 4 y 5 | ✗ | P2 |
| Música del final en 3 fases | ✗ | P2 |
| Temas de jefe (Centinela, Doncella, Espectro, Juez) | ✗. La arena ya cambia a `LevelData.musicaJefe` al empezar el combate y vuelve a `musica` al terminar: basta con asignar el clip. | Centinela P1, resto P2 |
| Stinger de antorcha (Miedo) | ✗ | P2 |
| Música del selector de niveles | ✗ (la escena no tiene `AudioSource`) | P3 |
| **Todos los SFX**: salto, doble salto, dash, ataque, ataque cargado, daño, muerte, moneda, poción, caja, checkpoint, clics de UI, golpes a enemigos, muerte de enemigo | ✗ **No existe ni un solo efecto de sonido** | P1 (silencio como placeholder) |

### B9. UI

| Asset | Sistema | Existe | Prioridad |
|---|---|---|---|
| Viñeta en gradiente radial (pausa y Game Over) | `UIManager` | ✔ Se genera por código (`FabricaUI.Vineta`) | — |
| Ícono de pausa | `MenuPausa` | ✔ Dos barras generadas por código | — |
| Brillo (glow) de la opción seleccionada, como en la maqueta | `UIManager` | ✗ Hoy la opción seleccionada toma el color de acento, sin halo. El halo necesita TextMeshPro (fila de abajo). | P3 |
| Escena animada de fondo por mundo (los SVG de la maqueta) | `MenuPausa` | ✗ Detrás del velo se ve el propio nivel congelado, que cumple la misma función. | P3 |
| Íconos por mundo para el selector (×6) + candado | `SelectorNivel` → `LevelData.icono` | ✗ | P2 |
| Íconos de los 5 fragmentos de identidad | `PantallaYoReconstruido` y fin de nivel | ✗ Hoy son rombos del color de cada mundo. Fuente posible: `Referencias/Arte/Jefes/fragmentos de alma.jpeg` (hay que recortarlo). | P2 |
| Arte de la pantalla «yo reconstruido» (Kael en el centro) | `PantallaYoReconstruido` | ✗ Hoy es una silueta clara | P2 |
| UI del registro emocional (escala y campo de texto) | `PantallaFinNivel` | ✔ Montada con uGUI, sin arte (Fase 8) | — |
| Pantalla de carga / fade | `SceneLoader` | ✗ (negro sólido basta) | P3 |
| Panel de opciones | `MainMenu` | ✗ **No existe**: el botón «Configuration» no hace nada | P3 |
| Corazones y monedas | HUD | ✔ `Elementos/Corazones.png`, `coins.png`, `Animaciones/hearths.anim`, `Coin.anim` | — |
| Logo | Menú | ✔ `Images/tituloxd.png` en la v1; falta importarlo en la v2 (hoy el título es texto) | P3 |
| Recursos esenciales de TextMeshPro | Toda la UI | ✗ No importados: la UI usa el `Text` integrado de Unity a través de `FabricaUI`. Se importan con *Window → TextMeshPro → Import TMP Essential Resources*; después se cambia `FabricaUI` y nada más. | P3 |

### B10. VFX y objetos de mecánica

| Asset | Componente *(propuesto)* | Existe / candidato | Prioridad |
|---|---|---|---|
| Plataforma que se desmorona | `PlataformaInestable` (Fase 9) | Provisional: rectángulo marrón que tiembla y desaparece. Candidato: `Pixel Adventure 1/Traps/Falling Platforms`. El sprite va en el hijo `Visual` de `Mecanicas/PlataformaInestable.prefab`. | P1 |
| Plataforma de combustión (estado caliente) | `PlataformaCaliente` (Fase 9) | Provisional: rectángulo que se tiñe de rojo con el calor. Candidato: `FireSet` + tinte. El tinte funciona igual con un sprite (campos `colorFrio` y `colorAlRojo`). | P1 |
| Lava (animada) | `Lava` (Fase 9) | Provisional: rectángulo cuyo color late. Candidatos de la v1: `Top Down Lava…/Burning Lava`, `Tiles/Lava.prefab`, `lava2.prefab`. Si se pinta con tiles, el prefab sigue haciendo falta como zona mortal (se le puede quitar el `Visual`). | P1 |
| Torreta de fuego y su bola | `TorretaFuego` + `Proyectil` (Fase 9) | Provisional: bloque oscuro con boca naranja y un cuadrado naranja como bola. El destello de aviso tiñe el `Visual`. | P1 |
| Checkpoint | `Checkpoint` (Fase 6) | Provisional: un mástil y un paño que se tiñe con el acento del mundo al activarse. Candidatos de la v1: `Pixel Adventure 1/Items/Checkpoints` (bandera animada) y `tileset/Puerta/respawn point_*`. El sprite va en `Nivel/Checkpoints/L1_CP1/Pano` (campo `indicador` del `Checkpoint`). | P1 |
| Salida del nivel | `SalidaNivel` (Fase 6) | Provisional: un portal translúcido del color de acento. Falta decidir su aspecto; en los niveles con jefe (Fase 7) puede bastar la puerta de la arena. | P2 |
| VFX del ataque cargado | modificador de `ATTACK` | ✗ | P1 |
| Estela del dash | `DASH` | ✗ | P3 |
| Material semitransparente de la sombra de eco | `EchoRecorder` | ✗ (sirve un `SpriteRenderer` con alfa) | P2 |
| Fade a blanco (oxígeno agotado) | `UnderwaterZone` | Sin arte | — |
| Volumen de post-procesado submarino | `UnderwaterZone` | URP instalado; falta el perfil | P2 |
| Barril explosivo + explosión | `Explodable` | ✗ | P2 |
| Superficie de hielo | `IceSurface` | ✔ `IceSet` (falta un `PhysicsMaterial2D` de fricción 0,1) | P2 |

---

## C. Limpieza del proyecto (no bloquea; hacerla en Fase 2 con git ya inicializado)

| Elemento | Motivo |
|---|---|
| `Assets/_Recovery/0.unity`, `0 (1).unity` | Copias de recuperación tras un cierre inesperado; `0.unity` duplica `Menu_Inicial` y referencia `MainMenu.cs`. |
| `Assets/TileMap/**/*.zip` (4 archivos) | Paquetes comprimidos dentro de `Assets`. |
| `Assets/TextMesh Pro/Examples & Extras` (173 archivos, ~11 MB) | Ejemplos de TMP; incluye 34 scripts que se compilan con el juego. |
| `Assets/Pixel Adventure 1/Scenes/Demo.unity` | Escena demo del pack. |
| `Characters/Bosses/Boss1/Walk/*.anim` e `idle/*.anim` (~98 clips) y `Samurai/DEAD/*.anim` (6) | Clips de un solo frame creados al arrastrar sprites; solo se usan `Walk.anim`, `Idle.anim` y `Attack.anim`. |
| `PhysicsMaterial/Character.physicMaterial` | Material de física **3D**: no sirve en 2D y no se usa. |
| `Materials/BlurMaterial.mat`, `Characters/Mc/Materials/quieto.mat` | Sin uso. |
| `Level1/Enemys` (Demon, Golem, Golem2, Enemy2, Player, Wizard) | «Galería» de sprites sin IA; el objeto llamado `Player` rompe `GameObject.Find("Player")`. Decidir si se convierten en enemigos reales. |
| `16.jpg.pxo` (56 MB) y `debug.log` en la raíz | Fuera de `Assets` (Unity no los importa), pero ensucian el repositorio. |

---

## Historial

- **0.14 (2026-10-02)** — Respuestas a la primera prueba del Nivel 1, en la rama `ajustes-nivel1`.
  - Resueltos N1, N4, N6, N9, N12, N13 y N14.
  - Nuevos N16 (pelear con la Centinela), N17 (presentación del jefe) y N18 (escala del Eco).
  - N7 pasa a ser la ficha del Golem.
  - Nueva lista «Arte que más necesito», con las fichas del Golem (B4) y de la Centinela (B5).
- **0.13 (2026-10-02)** — Eco de la Ira con arte. Nuevo N15 (pelear con el Eco), N8 reescrito, N6 con la rama `enemigo-eco-ira`, B4 y B5 al día.
- **0.12 (2026-10-01)** — Fondo con parallax del Mundo 1. Nuevo N14 (velocidad del fondo y remates del muro), N6 con la rama `fondo-mundo1`, B7 al día.
- **0.11 (2026-09-30)** — Cinemática inicial y escenario pintado. N10 y N11 reescritos; B1 y B7 al día.
- **0.10 (2026-09-30)** — Arte de Kael. N3 reescrito (clips que faltan), nuevo N13 (hitbox de la espada), B3 al día y convención de escala de personajes a 64 PPU.
- **0.9 (2026-09-29)** — Fase 9 (Nivel 1 de referencia). Mecánicas de la Ira en B4, B6 y B10; N9 y N10 ampliados.
- **0.8 (2026-09-29)** — Fase 8 (narrativa). Nuevos N11 y N12; B1 y B2 reescritos para la v2; B9 con la UI del registro y de «yo reconstruido».
- **0.7 (2026-09-29)** — Fase 7 (jefes). Apartado B5 reescrito para la v2, N9 con el jefe, nota de la música del jefe en B8.
- **0.6 (2026-09-29)** — Fase 6 (flujo de nivel). Nuevo N10 (pintar el Nivel 1), N9 ampliado, y B6, B8, B9 y B10 al día con el checkpoint, la salida, la música por `LevelData` y la UI de la maqueta.
- **0.5 (2026-09-29)** — Fase 5 (progresión). N2 resuelta, nuevo N9, apartado B6 reescrito para la v2 y nota sobre TextMeshPro en B9.
- **0.4 (2026-09-28)** — Fase 4 (enemigos). Apartado B4 reescrito para la v2; nuevos N7 (probar `PruebaEnemigos`) y N8 (arte de enemigos).
- **0.3 (2026-09-28)** — Fase 3 (jugador). Nueva sección «Lo que necesito de ti», apartado B3 reescrito para la v2 y corrección de la convención de escala y de la tabla de decisiones, que se habían quedado desfasadas.
- **0.2 (2026-09-19)** — Traslado a `Tesis2.0`, decisiones resueltas, inventario del arte de Gemini.
- **0.1 (2026-09-19)** — Primera versión, generada en la auditoría de la Fase 0.
