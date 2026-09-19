# PENDIENTES — Fragmentum: Ecos de la Identidad

> **Versión 0.2 — Fase 2, 2026-09-19.** Este archivo vive ahora en el proyecto **v2** (`Tesis2.0`).
> Lista viva de lo que falta: arte, audio, texto, escenas y decisiones. Se actualiza en cada fase.

## Revisión 0.2: qué cambió respecto de la v0.1

- El trabajo se trasladó a **`Tesis2.0`** (Unity 6000.6.0f1). `Tesis` queda congelado como v1 de referencia.
- **Las decisiones D1 a D11 están resueltas** (ver más abajo): quedan como registro, no como preguntas.
- **Escala fijada:** el tileset nuevo son 32 px por tile a 32 PPU, así que **1 tile = 1 unidad**. Todo el arte que se importe desde la v1 hay que reimportarlo a esa escala. Las hojas de Kael de la v1 estaban a 500 PPU y el jugador dependía de un `localScale` de 0,215; eso desaparece.
- **Tags y capas ya creados** en el proyecto por el menú *Fragmentum → Configurar proyecto*: tags `Enemy`, `Boss`, `Weapon`, `suelo`, `Lava`, `HotPlatform`, `IcePlatform`, `FalsePlatform`, `Checkpoint` y `Memory`; capas `suelo` (6), `Wall` (7), `Player` (8), `Enemy` (9), `PlayerWeapon` (10) y `EnemyWeapon` (11), con la matriz de colisión del Anexo A aplicada.

### Arte nuevo generado con Gemini (carpeta `Imagestesis2/` de la v1)

Son **hojas de referencia en JPEG**, no sprites listos: llevan el cuadriculado de transparencia
incrustado en la propia imagen, así que **hay que recortar cada pose y quitar el fondo** antes de
importarlas. Eso no lo puedo hacer yo: no genero ni edito imágenes.

| Archivo | Qué contiene | Qué hay que hacer | Prioridad |
|---|---|---|---|
| `tileset_32.png` | Tileset volcánico de basalto con lava | **Ya importado** en `Tesis2.0` con su paleta | — |
| `posiblesanimaciiones kael.jpeg` | 8 poses de Kael: quieto, correr, saltar, caer, atacar, dash, herido y derrota | Recortar pose a pose, fondo transparente, PNG a 32 px por unidad, y montar los clips | P1 (fase 3) |
| `AnimacionesGeneralesEnemigos.jpeg`, `EnemigosNiveles.jpeg` | Enemigos genéricos y por nivel | Igual | P1 (fase 4) |
| `Jefes/Jefe1.jpeg`, `Fase2Jefe1.jpeg`, `PosiblesAnimacionesJefe1.jpeg` | Centinela Ardiente y su fase 2 | Igual | P2 (fase 7) |
| `Jefes/jefe2.jpeg`, `jefe3.jpeg`, `Jefe4.jpeg`, `JefeFinal.jpeg` | Los otros cuatro jefes | Igual | P2 |
| `Jefes/cinematicJefe1.jpeg`, `CinematicaJefefinal.jpeg` | Cinemáticas (imágenes fijas) | Recortar a 16:9 y guardar como PNG; van en `CutsceneData` | P2 (fase 8) |
| `Jefes/fragmentos de alma.jpeg` | Iconos de los fragmentos de identidad | Recortar uno por fragmento | P2 |
| `Mapas/` (9 imágenes) | Capas de parallax: cielo, plano lejano, medio, cercano y primer plano, en dos variantes | Recortar, decidir qué variante es canónica y marcar las capas que deben repetirse en horizontal | P1 (fase 3, fondo del Nivel 1) |

**Sigue faltando por completo:** los diálogos escritos, la música de los niveles 2 al final, **todos**
los efectos de sonido, los iconos de la UI y el concepto del jugador con sus habilidades en acción.

---

**Leyenda de prioridad**
- **P1** — bloquea el Nivel 1 (Ira) como implementación de referencia.
- **P2** — bloquea otro nivel o un sistema narrativo.
- **P3** — pulido: el sistema funciona con placeholder.

**Convención de formato recomendada** (a confirmar en Fase 1)
- Tiles: 16 px a 100 PPU (1 tile = 0,16 u), que es lo que usa hoy `Level1` (`Grid.cellSize = 0.16`).
- Sprites de personaje: exportar ya a la escala del juego. Hoy el `Player` depende de `localScale = 0.215`, y eso rompe el wall jump. Las hojas de Kael se importan a 500 PPU con frames enormes.
- Imágenes de cinemática: PNG 16:9, 1920×1080, o 480×270 escalado ×4 con filtro *Point* si se quiere pixel art.
- Música: OGG, 44,1 kHz, estéreo, con loop limpio. SFX: WAV mono, 44,1 kHz, menos de 2 s.

---

## A. Decisiones (todas resueltas en la Fase 2; se conservan como registro)

| # | Decisión | Evidencia encontrada | Resolución |
|---|----------|----------------------|------------|
| D1 | **Versión de Unity.** El documento dice 2022 LTS; el proyecto es **Unity 6000.2.14f1** (Unity 6.2). | `ProjectSettings/ProjectVersion.txt`. El código ya usa `linearVelocity`, que solo existe en Unity 6. | Adoptar Unity 6.2 como plataforma oficial y corregir la tesis. |
| D2 | **Control de versiones.** La carpeta **no es un repositorio git**. | No hay `.git`. | `git init` con `.gitignore` de Unity **antes** de la Fase 2. Sin eso no hay vuelta atrás si un refactor rompe escenas. |
| D3 | **Input System.** El paquete `com.unity.inputsystem 1.16.0` **ya está instalado**. `activeInputHandler = Both` y los `EventSystem` ya usan `InputSystemUIInputModule`. El gameplay usa el `Input` clásico. | `Packages/manifest.json`, `ProjectSettings.asset`, escenas. | Mantener el `Input` clásico en gameplay, envuelto en un `PlayerInput` propio, y plantear la migración como opción separada en Fase 1. |
| D4 | **Título canónico.** | `Assets/Images/tituloxd.png` dice «FRAGMENTUM — *ECHOES OF IDENTITY*». No existe ningún texto en código ni escenas con otro título. | **Fragmentum: Ecos de la Identidad**. El logo actual es su versión en inglés. |
| D5 | **Jefe de Ira: Centinela Ardiente / Pyrokratos.** | No aparece ninguno de los dos nombres en el proyecto. | **Centinela Ardiente**, como en el GDD. |
| D6 | **Jefe de Culpa: Juez Silente / Juez Silencioso.** | Ninguno aparece en el proyecto. | **Juez Silente**, como en el GDD. |
| D7 | **Nivel 5: Esperanza / Aceptación.** | Ninguno aparece en el proyecto. | Emoción **Esperanza**, con «aceptación» como descriptor del `LevelData`. |
| D8 | **Nombre de la escena del menú.** Build Settings y código usan `Menu_Inicial`; el Anexo A dice `MainMenu`. | `EditorBuildSettings.asset`, `GameManager.cs`, `Pause.cs`. | Conservar `Menu_Inicial` para no romper referencias; el nombre vive solo en `LevelDatabase`. |
| D9 | **Correspondencia de conceptos de jefe.** | `Jefe2 (2).png` es una figura encapuchada azul/cian con esquirlas. `Jefe3 (2).png` es una figura oscura con espada y brillo verde azulado. | Suposición: Jefe2 = Doncella del Lamento (Tristeza), Jefe3 = Espectro del Pavor (Miedo). **Confirmar.** |
| D10 | **Tecla E.** Hoy es el dash. El Anexo A la usa para encender antorchas en el Nivel 3. | `PlayerMovement.cs:121` | Reasignar el dash (p. ej. Shift) y dejar E como «interactuar». |
| D11 | **Idioma de la UI.** El menú está en inglés («Play», «Level Selector», «Configuration», «Exit»); pausa y Game Over mezclan español e inglés. | Textos TMP de las escenas. | Todo en español, con textos en datos. |

---

## B. Assets faltantes por sistema

### B1. Narrativa y cinemáticas — sistema `CutscenePlayer` + `CutsceneData` *(propuesto)*

| Asset | Sistema que lo espera | Campo *(propuesto)* | Formato | Prioridad | Placeholder |
|---|---|---|---|---|---|
| Imágenes de la **cinemática inicial** | `CutscenePlayer` | `CutsceneData.diapositivas[i].imagen` | PNG 16:9 | P2 | Pantalla negra con el texto del guion |
| **Guion** de la cinemática inicial (texto y duración por diapositiva) | `CutsceneData` | `diapositivas[i].texto`, `.duracion` | Texto | P2 | Texto «[CINEMÁTICA INICIAL PENDIENTE]» |
| Cinemáticas de **cierre de cada mundo** (Ira, Tristeza, Miedo, Culpa) | `LevelManager` → `CutscenePlayer` | `LevelData.cinematicaCierre` | PNG 16:9 + texto | P2 (Ira: P1) | Negro + texto |
| Cinemática de cierre del Jardín (~30 s, Kael ve al Soldado) | idem | `LevelData.cinematicaCierre` (Nivel 5) | PNG + texto | P2 | Negro + texto |
| Cinemática de **entrada al final** (~45 s) | idem | `LevelData.cinematicaEntrada` (Final) | PNG + texto | P2 | Negro + texto |
| **Secuencia final + créditos** («¿Eres un error… o una nueva identidad?») | idem | `LevelData.cinematicaCierre` (Final) | PNG + texto | P2 | Negro + texto |
| **Memorias del Nivel 4** (4 × ~5 s) | `MemoryObject` → `CutscenePlayer` | `MemoryObject.cinematica` | PNG + texto | P2 | Negro + texto |
| **Memorias del Nivel 5** (4) | idem | idem | PNG + texto | P2 | Negro + texto |

> Hoy no existe ningún asset de Timeline ni `PlayableDirector`, aunque el paquete `com.unity.timeline` está instalado. El único arte narrativo es `Images/artbreeder-image-…jpeg`, que se usa como fondo del menú.

### B2. Diálogos — `DialogueData` *(propuesto)*

| Asset | Campo *(propuesto)* | Formato | Prioridad |
|---|---|---|---|
| Líneas de Kael y de los Arquitectos por mundo: **apertura, eco de memoria, pre-jefe, cierre** (×6 mundos) | `DialogueData.lineas[]` referenciado desde `LevelData.dialogos` | Texto (hablante + línea) | Ira P1, resto P2 |
| Líneas del registro emocional (preguntas y escala) | `EmotionalLogConfig` *(propuesto)* | Texto | P2 |
| Retratos opcionales (Kael + 5 Arquitectos + Soldado) | `DialogueSpeaker.retrato` | PNG 256×256 | P3 (sin retrato se muestra solo el nombre) |

### B3. Jugador — Kael

Estado actual: en juego se usa el **Samurai** de un pack (idle, run, attack, hurt, dead) con `Animaciones/Player.controller`. El arte propio de Kael **existe** pero no está conectado al jugador: `Characters/Mc/camin-ando.png` (8 frames de caminata), `Elsaltodel.png` (16 de salto) y `finalsignodepregunta.png` (7; clip `agachao`). Hoy está puesto como sprite decorativo en `Level1/Enemys/Player`.

| Animación que falta (Kael) | Estado de la FSM que la usará | Prioridad | Placeholder |
|---|---|---|---|
| Idle | `IDLE` | P1 | Frame 0 de `walk` |
| Run (¿o reutilizar walk?) | `RUN` | P1 | `walk` |
| Fall / doble salto | `FALL`, `DOUBLE_JUMP` | P1 | Frames finales de `Jump` |
| Ataque básico | `ATTACK` | P1 | `AttackSamurai` |
| Daño | `DAMAGED` | P1 | `HurtSamurai` |
| Muerte | `DEAD` | P1 | `Samuraidead` |
| Dash | `DASH` | P2 | `walk` + tinte |
| Wall slide / wall jump | `WALL_SLIDE`, `WALL_JUMP` | P2 | `Jump` |
| Ataque cargado (Ira) | modificador `ATTACK` | P1 | `AttackSamurai` + tinte rojo |
| Nado / movimiento bajo el agua (Tristeza) | modificador `UNDERWATER` | P2 | `walk` lento |
| Invisibilidad parcial (Miedo) | modificador | P2 | Alfa 0,4 en el `SpriteRenderer` (no necesita arte) |
| Defensa reforzada (Culpa) | modificador | P2 | Contorno o tinte |
| Agacharse (Culpa) | modificador `CROUCH` | P2 | **Ya existe** `agachao.anim` |

> Ni el Samurai ni Kael tienen hoy salto, caída, wall slide ni dash en el Animator del jugador.

### B4. Enemigos por mundo — `EnemyData` *(propuesto)*

| Mundo | Enemigo del Anexo A | Arte disponible hoy | Prioridad |
|---|---|---|---|
| Ira | Soldado de lava ×7 | **Candidato:** `Characters/FireWorm` (attack, death, get hit, idle, move, walk). Sin controller propio. | P1 |
| Ira | Torreta de fuego fija | **Candidato:** `Pixel Adventure 1/Traps/Fire` (On/Off/Hit) + proyectil ✗ | P1 |
| Ira | Mini-jefe **Golem de Ceniza** (vida 10) | ✔ `Characters/Golems` + `Animaciones/Golem1`/`Golem2` con idle/walk/attack/hurt/die | P1 |
| Tristeza | Lamentos, Esculturas Vivientes, Medusas de Hielo | ✗ | P2 |
| Miedo | Espectros, Centinelas Ciegos | ✗ (candidatos: `Fliying Demon`, `Enemy/Bringer-of-Death`) | P2 |
| Culpa | Penitentes, Artilleros (+ proyectil) | ✗ (candidatos: `Enemy/NECRHOMANCER`, `EvilWizard`) | P2 |
| Esperanza | Guardianes del Jardín (pacíficos) | ✗ | P2 |
| Genérico | Enemigo actual `Prefabs/Enemy.prefab` (Bringer of Death) | ✔ idle/walk/attack/hurt/death/cast | — |

### B5. Jefes — `BossController` *(propuesto: base abstracta)*

| Jefe | Hay hoy | Falta | Prioridad |
|---|---|---|---|
| **Centinela Ardiente** (Ira) | `Characters/Bosses/Boss1/jefe1.1.png` y clips `Idle`, `Walk`, `Attack` (~50 frames cada uno), `Animaciones/Jefe1.controller` | Daño, muerte/explosión, martillazo, VFX de onda de suelo, VFX de plataformas en erupción, animación de transición de fase | P1 |
| **Doncella del Lamento** (Tristeza) | Solo concepto estático `Jefe2 (2).png` (300×300), inactivo en `Level1` | Todas las animaciones, proyectil de ola, VFX de inundación y congelamiento | P2 |
| **Espectro del Pavor** (Miedo) | Solo concepto estático `Jefe3 (2).png` (256×300), inactivo en `Level1` | Todas las animaciones, destello de aviso, partícula que distingue al real de sus clones | P2 |
| **Juez Silente** (Culpa) | ✗ | Todo | P2 |
| **El Verdadero Soldado** (Final) | ✗ (el concepto **no está** en el proyecto) | Todo, incluidas variantes espejo de los ataques de Kael | P2 |

### B6. Escenas — `LevelDatabase` / Build Settings

| Escena | Existe | En Build Settings | Prioridad |
|---|---|---|---|
| `Menu_Inicial` | ✔ | ✔ (0) | — |
| `Niveles` (selector) | ✔ | ✔ (1) | — |
| `Level1` (Ira) | ✔, pero mide **~37 u** (el tilemap `ground` tiene 229 celdas de 0,16 u). El Anexo pide ~80 u. Sin checkpoint ni arena cerrada. | ✔ (2) | P1 (rediseño del layout) |
| `Level2` (Tristeza) | ✗ | ✗ | P2 |
| `Level3` (Miedo) | ✗ | ✗ | P2 |
| `Level4` (Culpa) | ✗ | ✗ | P2 |
| `Level5` (Esperanza) | ✗ | ✗ | P2 |
| `LevelFinal` | ✗ | ✗ | P2 |

### B7. Tilesets y fondos por nivel — `LevelData.paleta` / escenas

| Nivel | Disponible | Falta |
|---|---|---|
| 1 Ira | ✔ Tiles de lava (`Tiles/LavaTile set`, el que pinta `Level1`; `Tiles/freelavatileset-Sheet`), `Starter Tiles Platformer/FireSet` | ✗ **Parallax volcánico de 3 capas** (humo, montañas, cielo rojo). Hoy `Level1` usa el parallax *demon woods* (bosque), que encaja con Miedo. ✗ Sprite de plataforma de basalto inestable (candidato: `Pixel Adventure 1/Traps/Falling Platforms`). |
| 2 Tristeza | ✔ `Starter Tiles Platformer/IceSet`, `tileset/Tilemaps/rock_ice*.aseprite`, `Pixel Adventure 1/Traps/Sand Mud Ice` | ✗ Ciudad sumergida, corriente y superficie helada (parallax). ✗ Burbuja de oxígeno (UI). |
| 3 Miedo | ✔ Parallax `TileMap/Forest` (*demon woods*, 4 capas), tileset de pantano `TileMap/ParallaxVideo`, antorcha animada (`Top Down Lava…/Torch ( No Light )`, 7 frames), URP 2D con `Light2D` ya activo | ✗ Raíces y ramas. ✗ Partícula de «ruido» del sigilo. |
| 4 Culpa | ~ `Starter Tiles Platformer/DarkCastle` | ✗ Tierra quemada, escombros, tanques volcados. ✗ **Barril explosivo** (`Explodable`). ✗ Objetos de memoria. ✗ Marcas proyectadas en el suelo. |
| 5 Esperanza | ✔ Fondos `TileMap/Naturaleza` (nature_1…8), `TileMap/Sky-Place`, `Tilemap2/Free Pixel Art Hill`, `BasicGreenTiles` | ✗ Musgo, flores y partículas flotantes (un Particle System puede servir sin arte). |
| Final | ✗ | ✗ Todo, incluida la dualidad oscuro/luminoso. |

### B8. Audio — `LevelData.musica`, `AudioManager` *(propuesto)*

| Asset | Existe | Prioridad |
|---|---|---|
| Música del menú | ✔ `Music/MainMenu.mp3` (asignada en `Menu_Inicial`) | — |
| Música del Nivel 1 | ✔ `Music/level1.mp3` (asignada en `Level1`). **Verificar** si cumple el Anexo A (industrial, ~160 BPM). | — |
| Música de los niveles 2, 3, 4 y 5 | ✗ | P2 |
| Música del final en 3 fases | ✗ | P2 |
| Temas de jefe (Centinela, Doncella, Espectro, Juez) | ✗ | Centinela P1, resto P2 |
| Stinger de antorcha (Miedo) | ✗ | P2 |
| Música del selector de niveles | ✗ (la escena no tiene `AudioSource`) | P3 |
| **Todos los SFX**: salto, doble salto, dash, ataque, ataque cargado, daño, muerte, moneda, poción, caja, checkpoint, clics de UI, golpes a enemigos, muerte de enemigo | ✗ **No existe ni un solo efecto de sonido** | P1 (silencio como placeholder) |

### B9. UI

| Asset | Sistema | Existe | Prioridad |
|---|---|---|---|
| Viñeta en gradiente radial (pausa y Game Over) | `UIManager` | ✗ | P3 |
| Ícono de pausa | `UIManager` | ✗ | P3 |
| Materiales de glow para TMP | `UIManager` | ✗ | P3 |
| Íconos por mundo para el selector (×6) + candado | `SelectorNivel` → `LevelData.icono` | ✗ | P2 |
| Íconos de los 5 fragmentos de identidad | Pantalla «yo reconstruido» | ✗ | P2 |
| Arte de la pantalla «yo reconstruido» | idem | ✗ | P2 |
| UI del registro emocional (escala + campo de texto) | `EmotionalLog` | ✗ (se puede montar con uGUI sin arte) | P2 |
| Pantalla de carga / fade | `SceneLoader` | ✗ (negro sólido basta) | P3 |
| Panel de opciones | `MainMenu` | ✗ **No existe**: el botón «Configuration» no hace nada | P3 |
| Corazones y monedas | HUD | ✔ `Elementos/Corazones.png`, `coins.png`, `Animaciones/hearths.anim`, `Coin.anim` | — |
| Logo | Menú | ✔ `Images/tituloxd.png` | — |

### B10. VFX y objetos de mecánica

| Asset | Componente *(propuesto)* | Existe / candidato | Prioridad |
|---|---|---|---|
| Plataforma que se desmorona | `PlatformCrumble` | Candidato: `Pixel Adventure 1/Traps/Falling Platforms` | P1 |
| Plataforma de combustión (estado caliente) | `HotPlatform` | Candidato: `FireSet` + tinte | P1 |
| Lava (animada) | `Lava` | ✔ `Top Down Lava…/Burning Lava`, `Tiles/Lava.prefab`, `lava2.prefab` | P1 |
| Checkpoint | `CheckpointManager` | ✔ `Pixel Adventure 1/Items/Checkpoints` (bandera) y `tileset/Puerta/respawn point_*` | P1 |
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

- **0.2 (2026-09-19)** — Traslado a `Tesis2.0`, decisiones resueltas, inventario del arte de Gemini.
- **0.1 (2026-09-19)** — Primera versión, generada en la auditoría de la Fase 0.
