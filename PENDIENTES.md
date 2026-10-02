# PENDIENTES — Fragmentum: Ecos de la Identidad

> **Versión 0.12 — Fondo con parallax del Mundo 1, 2026-10-01.** Este archivo vive en el proyecto **v2** (`Tesis2.0`).
> Lista viva de lo que falta: arte, audio, texto, escenas y decisiones. Se actualiza en cada fase.

## ▶ Lo que necesito de ti

Nada de esto bloquea el trabajo: sigo con las fases siguientes y lo que falte usa placeholders.
Ordenado por urgencia.

| # | Qué | Por qué | Cómo |
|---|---|---|---|
| N1 | **Prueba la escena `Assets/Scenes/Pruebas/PruebaJugador.unity`** y dime cómo se siente el control. | Los valores de salto, carrera y dash están calculados, no probados por una persona. | Play, recorre el circuito (el panel F1 dice qué probar en cada zona). Si algo se siente mal, dime qué («salta poco», «patina») o cambia `PlayerController → Parametros` en el prefab. |
| N3 | **Dibuja los clips de Kael que faltan:** dash, wall slide, agacharse, daño y muerte (y, para la Fase 10, nado). | Quieto, correr, salto y ataque ya están en el juego. Los demás estados usan el clip más parecido; por ejemplo, el dash muestra un fotograma de carrera fijo. | En Pixelorama, con el mismo lienzo y tamaño de Kael, exporta cada tira **a escala 1× y sin ping-pong** a `Assets/Sprites/Kael`. Si prefieres pasarme los PNG, los monto yo. Detalle en B3. |
| N4 | **Decide si la tecla E de interactuar debe ser «pulsar» o «mantener».** | La acción `Interact` del asset trae la interacción *Hold* de fábrica: hay que mantener E unos 0,4 s. | Recomiendo «pulsar» para antorchas y memorias. Lo cambio yo cuando lo decidas (afecta a la Fase 10). |
| N5 | **Decide la tecla de «caminar»** para el sigilo del mundo del Miedo. | Con teclado la dirección es todo o nada: el jugador siempre corre y siempre haría ruido. El mando sí permite caminar. | Propuesta: mantener **Alt izquierdo** para caminar al 35 %. Se implementa en la Fase 10. |
| N6 | **Da el visto bueno para fusionar a `main`** las ramas `fase-2-cimientos`, `fase-3-jugador`, `fase-4-enemigos`, `fase-5-progresion`, `fase-6-flujo-nivel`, `fase-7-jefes`, `fase-8-narrativa`, `fase-9-nivel1`, `arte-kael`, `cinematica-y-mapa` y `fondo-mundo1` (cada una sale de la anterior). | El plan dice que cada fase se fusiona cuando la pruebas tú en Unity. | Cuando termines N1, N7 y N9, dime «fusiona» y lo hago. |
| N7 | **Prueba la escena `Assets/Scenes/Pruebas/PruebaEnemigos.unity`.** | Las 6 variantes de enemigo tienen valores de diseño, no probados por una persona. | Recorre la pista (F2 muestra el estado de cada enemigo). Si una variante no «se siente» como su emoción, dime cuál y por qué, o ajusta su asset en `Assets/Data/Enemigos`. |
| N8 | **Recorta los enemigos** de `Referencias/Arte/EnemigosNiveles.jpeg` y `AnimacionesGeneralesEnemigos.jpeg`. | Hoy son cápsulas tintadas con el color de su mundo. | Igual que Kael (B3): tiras de fotogramas con fondo transparente, exportadas a escala 1× y a la misma densidad de píxel que Kael. Detalle en el apartado B4. |
| N9 | **Juega el Nivel 1 de principio a fin desde `Assets/Scenes/Menu_Inicial.unity`**, jefe incluido. | Menú, selector, guardado, checkpoints, pausa, Game Over, la Centinela Ardiente, la narrativa, el fin de nivel y las mecánicas de la Ira ya funcionan juntos (Fases 5 a 9). | Sigue las listas «Qué probar a mano» de las Fases 5 a 9 en `CHANGELOG.md`. Dime qué pantalla no se entiende y qué se siente mal. También si la Centinela es demasiado fácil o difícil (sus valores están en `Assets/Data/Jefes`) y si el Golem de Ceniza debe poder morir en la lava. |
| N10 | **Pinta el layout definitivo del Nivel 1** (~80 u, Anexo A) cuando quieras. | El recorrido provisional ya se ve con el tileset volcánico (ladrillo, vigas y decoración), pero su forma sigue siendo la provisional: suelo plano con dos fosos y una plataforma por sección. | Sigue los «Pasos manuales en el Editor» de las Fases 6 y 9: borra «Escenario (tiles)» y «Provisional» (se lleva sus mecánicas), pinta y prepara el tilemap como suelo, ajusta la altura de la bandera y de la salida, y arrastra las mecánicas desde `Assets/Prefabs/Mecanicas`. No bloquea. |
| N11 | **Escribe el guion** de los cierres de cada mundo y las líneas de Kael y de los Arquitectos. | La cinemática inicial ya tiene guion e imágenes. Los cierres y los diálogos siguen con textos marcados como «pendiente de guion» (Fase 8). | Edita los assets `Cinematica_Cierre_*` y `Dialogos_*` de `Assets/Data/Narrativa` (detalle en B1 y B2). Cada diapositiva admite varios textos con sus tiempos, como la inicial. Prioridad: el mundo de la Ira. Si prefieres pasarme el texto y las imágenes, lo vuelco yo. |
| N12 | **Revisa la pregunta del registro emocional.** | Hoy pregunta «¿Con qué intensidad sentiste {emoción} en este mundo?», de 1 (Nada) a 5 (Muchísimo). Debe encajar con el instrumento de la tesis (TMMS-24 y SCCS). | Cámbiala en `Assets/Data/Narrativa/RegistroEmocional.asset`, o dime cuál quieres. También puedes pedir que no se pida código de participante. |
| N14 | **Mira el fondo del Nivel 1 en movimiento** y dime si la velocidad se siente bien. Si quieres, dibuja los **remates del muro**. | Las seis capas ya se mueven con su parallax, pero las velocidades vienen de la tabla del encargo, no de verlas en pantalla. Los extremos de cada tramo de muro son un corte recto a través de los ladrillos. | Play en el Nivel 1 y recórrelo. Para ajustar, sigue «Cómo ajustar la velocidad» en la entrada del fondo en `CHANGELOG.md`. Para los remates: dos PNG de 768 px de alto (inicio y final del muro, con alfa en el lado que da al paisaje), en `Assets/Sprites/Fondos/Mundo1Ira`, y los coloco en cada extremo del tramo. Detalle en B7. |
| N13 | **¿Subo la hitbox de la espada al arco del dibujo?** | El tajo del ataque nuevo se dibuja hasta ~2,8 u de alto, pero la hitbox cubre de 0,25 a 1,55 u. Se golpea lo que está a la altura del pecho, no lo que el arco parece tocar. | Dime «súbela» y la ajusto al arco: alcanzaría a enemigos más altos y a los que estén justo encima. Si prefieres el alcance actual, no hay que hacer nada. |

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
| `Cinematica_Cierre_Ira` | Al superar el mundo de la Ira | Guion e imágenes | P1 |
| `Cinematica_Cierre_Tristeza`, `_Miedo`, `_Culpa` | Al superar cada mundo | Guion e imágenes | P2 |
| `Cinematica_Cierre_Jardin` (~30 s: Kael ve al Soldado) | Al superar el Jardín | Guion e imágenes (la idea, del Anexo A, ya está) | P2 |
| `Cinematica_Entrada_Final` (~45 s) | Antes del final | Guion e imágenes (la idea, del Anexo A, ya está) | P2 |
| `Cinematica_Final` y créditos («¿Eres un error… o una nueva identidad?») | Al terminar el juego | Imágenes; texto del Anexo A ya incluido | P2 |
| Memorias de los Niveles 4 y 5 (4 + 4, ~5 s) | Al activar cada memoria (`MemoryObject`, Fase 10) | Todo: se crearán con la mecánica | P2 |

> La v2 no usa Timeline: las cinemáticas son diapositivas. Si alguna necesita animación, una
> diapositiva puede apuntar más adelante a un clip de Timeline sin cambiar el resto del sistema.

### B2. Diálogos — `DialogueData` + `DialogueManager` (Fase 8)

**Estado en la v2.** Cada mundo tiene su `Dialogos_<Emoción>` en `Assets/Data/Narrativa`, con una
línea pendiente por momento: apertura, eco de memoria, antes del jefe (si hay jefe) y cierre.
Todas nombran ya la emoción del mundo. Se muestran abajo en el HUD (el cierre, en el fin de
nivel).

| Asset | Dónde | Formato | Prioridad |
|---|---|---|---|
| Líneas de Kael y de los Arquitectos, **×6 mundos**. Pueden ser varias por momento: se muestran en orden. | `Dialogos_*` → `lineas` (hablante, texto, momento, duración) | Texto | Ira P1, resto P2 |
| Pregunta y escala del registro emocional, ajustadas al instrumento de la tesis (N12) | `Assets/Data/Narrativa/RegistroEmocional.asset` | Texto | P1 |
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
6 `EnemyData`. Visualmente, cada enemigo es una cápsula tintada con un color de la paleta de su
mundo.

- **Dónde va el arte:** en `Pivote/Sprite` del prefab base (`Assets/Prefabs/Enemigos/Enemigo.prefab`),
  o en cada variante si cambia el sprite. Hay que crear un controlador de Animator por enemigo con
  los parámetros bool `MovEn`, `Attack`, `Damage` y `Death`.
- **Orientación del arte:** si mira a la izquierda, desmarca `spriteMiraDerecha` en su `EnemyData`.
- **Color:** cuando tenga arte final, pon `tinte` en blanco.
- **Arte fuente:** `Referencias/Arte/EnemigosNiveles.jpeg` y `AnimacionesGeneralesEnemigos.jpeg`
  (hay que recortarlas). Los packs de la v1 (FireWorm, Golems, Bringer of Death…) sirven como
  alternativa, reimportados a 32 PPU.

| Mundo | Enemigo | Datos y prefab | Arte | Falta además | Prioridad |
|---|---|---|---|---|---|
| — | Genérico | ✔ `Generico` / `Enemigo.prefab` | Cápsula gris | — | — |
| Ira | Soldado de lava | ✔ `SoldadoLava` / `Enemigo_SoldadoLava` | Cápsula naranja | — | P1 |
| Ira | Golem de Ceniza (mini-jefe, vida 10) | ✔ `GolemCeniza` / `Enemigo_GolemCeniza` (más grande) | Cápsula ceniza | — | P1 |
| Ira | Torreta de fuego fija | ✔ Es una mecánica, no un enemigo: `TorretaFuego` / `Mecanicas/TorretaFuego.prefab` (Fase 9) | Bloque oscuro con boca naranja; la bola de fuego es un cuadrado naranja | Sprite de la torreta y de la bola (B10) | P1 |
| Tristeza | Lamento | ✔ `Lamento` / `Enemigo_Lamento` | Cápsula azul | Ralentizar al jugador al tocarlo (Anexo: «reducen velocidad al contacto»), Fase 10 | P2 |
| Tristeza | Esculturas Vivientes, Medusas de Hielo | ✗ | ✗ | Movimiento sinusoidal de las medusas (Fase 10) | P2 |
| Miedo | Espectro | ✔ `Espectro` / `Enemigo_Espectro` (detección en oscuridad) | Cápsula oscura translúcida | Que sea invisible salvo al atacar (Fase 10) | P2 |
| Miedo | Centinela Ciego | ✔ `CentinelaCiego` / `Enemigo_CentinelaCiego` (detección por ruido) | Cápsula violeta | Tecla de caminar con teclado (N5) | P2 |
| Culpa | Penitentes, Artilleros (+ proyectil) | ✗ | ✗ | `Proyectil` y cobertura (Fase 10) | P2 |
| Esperanza | Guardianes del Jardín (pacíficos) | ✗ | ✗ | `GardenGuardian` (Fase 10) | P2 |

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
- **Cuando haya VFX:** cada patrón (`Assets/Data/Jefes/Patrones`) tiene su color provisional. Los
  efectos se engancharán a `ZonaDeDanio`, que es la pieza que crean todos los ataques.

| Jefe | Estado | Falta | Prioridad |
|---|---|---|---|
| **Centinela Ardiente** (Ira) | ✔ Completo: martillazo, onda de suelo y erupciones; arena en el Nivel 1 | Sprites y animaciones: en la v1 existen `Characters/Bosses/Boss1/jefe1.1.png` y los clips `Idle`, `Walk` y `Attack`, que hay que reimportar a 32 PPU. Faltan martillazo, daño, rugido de fase 2 y muerte/explosión, y los VFX de la onda y de las erupciones. | P1 |
| **Doncella del Lamento** (Tristeza) | Esqueleto: datos, prefab y dos fases sin patrones | Patrones de olas, inundación y congelamiento (Fase 10); todas las animaciones. Concepto: `Jefe2 (2).png` en la v1. | P2 |
| **Espectro del Pavor** (Miedo) | Esqueleto: casi invisible en combate | Patrón de ataque con destello y clones (Fase 10); animaciones; la partícula que distingue al real. Concepto: `Jefe3 (2).png` en la v1. | P2 |
| **Juez Silente** (Culpa) | Esqueleto: inmune salvo en la ventana, que abre `ActivarMemoriaCorrecta()` | Proyectar las memorias y conectarlas (`MemoryObject`, Fase 10); todo el arte. | P2 |
| **El Verdadero Soldado** (Final) | Esqueleto: tres fases; no cae sin `CompletarIntegracion()` | Combate espejo, secciones de cada mundo y el plataformeo final (Fase 10); todo el arte (el concepto **no está** en el proyecto). | P2 |

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
| 1 Ira | ✔ **Tileset volcánico de la v2** (`Assets/TileMap/tileset_32.png`): ya viste el Nivel 1 y las escenas de prueba con `PintorEscenario` (ladrillo, vigas, estatua, braseros, cadenas y escombros). Su versión grande con fondo negro está en `Tesis/TileMap Nivel 1`. En la v1: tiles de lava (`Tiles/LavaTile set`, `Tiles/freelavatileset-Sheet`) y `Starter Tiles Platformer/FireSet` | ✔ **Parallax volcánico de 6 capas** en `Assets/Sprites/Fondos/Mundo1Ira` (cielo, montañas, fortaleza, muro, catarata y primer plano), montado en `FondoMundo1.prefab`. ~ **Remates del muro** (N14): los extremos de cada tramo son un corte recto. ✗ Sprite de plataforma de basalto inestable (candidato: `Pixel Adventure 1/Traps/Falling Platforms`). |
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
