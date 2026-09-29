# CHANGELOG — Fragmentum: Ecos de la Identidad (v2)

> Registro de la refactorización, en formato **problema → causa → solución**, pensado para el
> capítulo de desarrollo de la tesis. Una entrada por fase.
>
> - **v1**: proyecto original `Tesis` (Unity 6000.2.14f1), congelado tras la auditoría de la Fase 0.
>   Desde la Fase 5 está actualizado a 6000.6.3f1 para poder abrirlo en el PC nuevo; su código no cambia.
> - **v2**: este proyecto `Tesis2.0` (Unity 6000.6.3f1 desde la Fase 3), donde se construye la versión refactorizada.

---

## Fase 6 — Flujo de nivel (2026-09-29)

Rama `fase-6-flujo-nivel`, que sale de `fase-5-progresion`. Cierra la tarea D con el
`LevelManager` (D5) y los checkpoints (D8). Criterio de aceptación: el Nivel 1 se juega de
principio a fin con checkpoints.

### 1. Morir obligaba a repetir el nivel entero

- **Problema.** En la v1, la única salida del Game Over era la tecla R, que recargaba la escena
  desde el principio. En un juego de plataformas con daño por contacto, cada error costaba el
  nivel completo.
- **Causa.** No existía ningún punto de control ni nada que supiera dónde reaparecer.
- **Solución.**
  - **`Checkpoint`**: al tocarlo, pasa a ser el activo, se guarda en la partida y el HUD avisa
    («Punto de control»). Tiene un id estable, que es lo que se guarda.
  - **`CheckpointManager`**: encuentra solo los checkpoints de su escena, lleva el activo y
    decide el punto de reaparición. Si el checkpoint guardado ya no existe, avisa y empieza
    desde el inicio.
  - **Reintentar** devuelve al jugador vivo al último checkpoint sin recargar la escena.
  - **Continuar** otro día también empieza en el checkpoint guardado.
  - **Reiniciar nivel** olvida el checkpoint y empieza desde el principio.
  - Lo verifican `CheckpointManagerTests`, `TocarElCheckpoint_LoActivaYGuardaLaPartida`,
    `Morir_MuestraElGameOver_YReintentarVuelveAlCheckpoint`,
    `Continuar_EmpiezaEnElCheckpointGuardado` y
    `ReiniciarNivel_OlvidaElCheckpoint_YEmpiezaDesdeElInicio`.

### 2. Cada escena de nivel se montaba a mano

- **Problema.** En la v1, cada nivel llevaba su propio jugador, su cámara con el jugador
  asignado a mano y su música. Un nivel nuevo exigía repetir todo eso, y cualquier olvido lo
  rompía.
- **Causa.** No había ningún componente que aplicara a la escena los datos del nivel.
- **Solución.** **`LevelManager`**, uno por escena de nivel (patrón Type Object: la escena lee su
  `LevelData`):
  - Usa el jugador que haya en la escena o instancia el prefab. Si hay un checkpoint guardado de
    ese nivel, lo coloca ahí.
  - La cámara sigue al jugador, con límites calculados desde las secciones del Anexo A, y su
    fondo toma el color más oscuro de la paleta.
  - Toca la música del `LevelData`. Si no hay, avisa por consola y el nivel suena en silencio.
  - Fuera de los niveles del juego (escenas de prueba) no hay Game Over, así que hace reaparecer
    al jugador tras una espera. Sustituye a la `ReaparicionSimple` provisional de la Fase 3.
  - Lo verifica `EntrarEnElNivel_ApareceUnSoloJugadorEnElInicio_ConHudYCamara`.

### 3. La pausa y el Game Over rompían el tiempo del juego

- **Problema.** En la v1:
  - Volver al menú desde la pausa lo dejaba congelado: `Pause` cargaba el menú sin restaurar
    `Time.timeScale`.
  - El Game Over no detenía el juego: los enemigos seguían moviéndose detrás del panel.
  - Se manejaba con las teclas R y M, leídas en el `Update` del `GameManager`.
  - Esc seguía pausando y reanudando durante el Game Over.
- **Causa.** Cada pantalla escribía el tiempo y cargaba escenas por su cuenta, y la UI del Game
  Over vivía dentro del `GameManager`.
- **Solución.** **`UIManager`**, persistente como el resto de los sistemas, muestra la pantalla
  que corresponde a cada estado de la FSM global: HUD al jugar, `MenuPausa` en PAUSED,
  `PantallaGameOver` en GAME_OVER y `PantallaFinNivel` en LEVEL_COMPLETE.
  - Las pantallas no tocan el tiempo ni cargan escenas: cada opción es una petición a la FSM
    global, que ya era la única dueña de las dos cosas (Fase 5).
  - La pausa es la acción `Game/Pause` del asset de input (Esc o Start). Solo actúa jugando o en
    pausa.
  - Se navegan con ratón, teclado o mando: al abrirse, queda seleccionada la primera opción.
  - Siguen la maqueta `Referencias/Menus/fragmentum_pause_worlds.jsx`: velo oscuro con el tono del
    mundo, viñeta, esquinas y separador del color de acento, el mundo en números romanos
    («I · FORTALEZA VOLCÁNICA»), su jefe y su frase. La viñeta y el icono de pausa se generan por
    código, sin necesidad de assets.
  - El Game Over dice dónde se reaparecerá: en el último checkpoint o al principio.
  - Lo verifican `Pausa_MuestraElMenu_YContinuarReanuda` y la espera de 2 s sin que el Game Over ni
    el fin de nivel avancen solos.

### 4. No había forma de terminar un nivel

- **Problema.** En la v1 ningún nivel tenía final: no existía victoria, ni fin de nivel, ni paso
  al siguiente mundo.
- **Causa.** No había condición de victoria ni pantalla que la mostrara.
- **Solución.**
  - **`SalidaNivel`**: al cruzarla, el `LevelManager` declara la victoria. En la Fase 7 también la
    declarará la derrota del jefe.
  - La FSM global otorga el fragmento y la habilidad (Fase 5).
  - **`PantallaFinNivel`** muestra el fragmento integrado y la habilidad nueva. «Continuar» lleva
    al siguiente mundo, o al selector si ese mundo aún está en construcción.
  - Lo verifica `CruzarLaSalida_CompletaElNivel_YContinuarLlevaAlSelector`.

### 5. El Nivel 1 no se podía jugar

- **Problema.** La escena `Nivel1` tenía 2 tiles pintados, ningún suelo que colisionara y ningún
  jugador.
- **Causa.** El pintado del layout es trabajo de la Fase 9 (y tuyo en el Editor).
- **Solución.** **`PreparadorNivel`** (*Fragmentum → Niveles → Preparar la escena de nivel
  abierta*) añade a una escena de nivel lo que le falte y nunca toca lo que ya hay:
  - el `LevelManager` y el punto de inicio;
  - los checkpoints (en el Nivel 1, el de x = 40 del Anexo A);
  - la salida al final de la última sección;
  - la zona mortal;
  - el seguimiento de la cámara.

  Si la escena no tiene suelo que colisione, añade además un **recorrido provisional**: suelo con
  un foso tras cada cambio de sección, una plataforma por sección, paredes en los extremos y un
  enemigo del mundo por sección. En el Nivel 1 son un Soldado de lava y, tras el checkpoint, el
  Golem de Ceniza. Todo eso va dentro del objeto «Provisional (bórralo al pintar el nivel)».

  La herramienta es idempotente: ejecutarla otra vez no cambia nada, y la escena solo se guarda
  si cambió. *Regenerar todo* prepara todas las escenas de nivel de la `LevelDatabase`. Lo
  verifica `EscenasNivelTests`.

### Decisiones que ajustan el plan de la Fase 1

1. **Las pantallas y el HUD se construyen por código** dentro del `UIManager` (con `FabricaUI`),
   en lugar de un prefab de UI por escena. No hay referencias de Inspector que puedan quedar
   rotas, y el estilo se cambia en un solo sitio. El HUD es persistente y solo se muestra en los
   niveles del juego; las escenas de prueba conservan el suyo.
2. **PLAYING → GAME_OVER llega 1 s después de la muerte**, para que se vea caer al jugador. La
   espera vive en el estado PLAYING y el diagrama la recoge.
3. **Reintentar no recarga la escena:** el jugador vuelve al checkpoint y los enemigos siguen como
   estaban. Reiniciar sí recarga, y además olvida el checkpoint. Entrar en un nivel desde
   «Continuar» o desde el selector empieza en el checkpoint guardado de ese nivel.
4. **Nuevo mapa de input `Game`** con la acción `Pause` (Esc o Start), separado de `Player`. Así
   la pausa funciona aunque el input del jugador esté bloqueado o su mapa desactivado.
5. **`LevelData` añade** `puntosDeControl` (guía del Anexo A para preparar la escena), `acento` y
   `cita` (la maqueta de los menús). Se completan también en los assets que ya existían, sin
   pisar lo editado a mano.
6. **Se elimina `ReaparicionSimple`.** Las escenas de prueba usan el `LevelManager`, sin Game
   Over.

### Además

- **El clic que pulsa «Continuar» ya no ataca.** El clic izquierdo y Enter son también el ataque,
  así que el input de juego ignora el fotograma en que se sale de un menú
  (`EventosJuego.EntradaIgnorada`).
- **EventSystem:** el `UIManager` trae uno propio y solo lo activa si la escena no tiene otro. Los
  niveles no necesitan el suyo y el menú no tiene dos.
- **El jugador se busca en la escena que entra** (`PlayerController.BuscarEnEscena`), no con un
  `FindGameObjectWithTag` global. Durante una carga asíncrona conviven la escena que entra y la
  que sale.
- **Red de seguridad del reintento:** si tras reintentar el jugador sigue muerto (una escena sin
  `LevelManager`), el `GameManager` lo revive donde apareció y avisa por consola.
- **`AtajosDepuracion`** pierde Esc, que ahora es la pausa real; conserva F9 (completar nivel) y
  F10 (menú).
- **Regenerar todo en batch ya no reescribe las escenas de prueba que existen.** Al regenerarlas,
  Unity cambiaba todos sus identificadores internos y git las mostraba cambiadas por completo
  (unas 5000 líneas) sin que cambiara nada. `verificar.ps1 -Unity -Forzar` y el menú del Editor
  sí las regeneran.

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **134 de 134 correctas** (21 nuevas: checkpoints, datos de la interfaz, la
  escena del Nivel 1 y la herramienta que la prepara).
- Pruebas PlayMode: **47 de 47 correctas** (8 nuevas de flujo de nivel sobre la escena real del
  Nivel 1; las de progresión se adaptaron a que el Nivel 1 ya trae jugador y pantallas).
- Las pruebas encontraron dos fallos de la propia herramienta antes del commit:
  - **Enemigos genéricos en lugar de los de la Ira.** Al preparar varias escenas seguidas, abrir
    cada una en modo `Single` descarga los assets sin uso. La referencia a la emoción del nivel,
    cargada antes, pasaba a valer `null`. Ahora el `LevelData` se vuelve a cargar después de abrir
    su escena.
  - **Dos enemigos con la misma ruta.** La raíz de todas las variantes de enemigo se llama
    «Enemigo». Ahora cada uno toma el nombre de su prefab y tiene su propia ruta.

### Pasos manuales en el Editor

Ninguno obligatorio. La escena `Nivel1` ya está preparada y el `UIManager` está en
`Resources/Sistemas.prefab`.

- **Cuando pintes el Nivel 1** (Fase 9):
  1. Selecciona el Tilemap y ejecuta *Fragmentum → Jugador → Preparar tilemap seleccionado como
     suelo*.
  2. Borra el objeto «Provisional (bórralo al pintar el nivel)».
  3. Si el suelo no queda a la altura y = 0, sube o baja `Nivel/Inicio`, la bandera de
     `Nivel/Checkpoints` y `Nivel/Salida`.
- **Para añadir un checkpoint:** duplica `Nivel/Checkpoints/L1_CP1`, muévelo y cámbiale el `Id`
  (por ejemplo, `L1_CP2`). Dos checkpoints con el mismo id generan un aviso en la consola.
- **Para poner música a un nivel:** arrastra el clip al campo `musica` de su asset en
  `Assets/Data/Niveles`.

### Qué probar a mano (desde `Menu_Inicial`, «Nueva partida»)

- El jugador aparece a la izquierda. Arriba a la izquierda está la vida con «I · FORTALEZA
  VOLCÁNICA», y arriba a la derecha, las monedas.
- **Esc** abre la pausa con el acento naranja de la Ira y «la rabia consume al que la niega».
  ↑↓ y Enter funcionan, y Esc reanuda. Al reanudar con un clic, el jugador no lanza un ataque.
- Cae en el foso de x ≈ 28: tras un segundo aparece «HAS CAÍDO». «Reintentar» te devuelve al
  inicio.
- Toca la bandera de x = 40: se tiñe y aparece «Punto de control». Si mueres ahora, reapareces
  en la bandera, junto al Golem de Ceniza.
- Pausa, «Menú principal» y luego «Continuar»: empiezas en la bandera.
- Pausa y «Reiniciar»: vuelves al principio y la bandera está apagada.
- Cruza el portal del final (x = 78): «MUNDO SUPERADO», el Fragmento de la Ira y el Ataque
  Cargado. «Continuar» lleva al selector, porque el mundo 2 está en construcción.

---

## Fase 5 — Progresión, guardado y FSM global (2026-09-28)

Rama `fase-5-progresion`, que sale de `fase-4-enemigos`. Cubre la tarea D (salvo el
`LevelManager` y el `CheckpointManager`, que son de la Fase 6) y la tarea E. D es, junto con A,
una de las dos tareas de prioridad máxima.

### 1. El GameManager no persistía nada

- **Problema.** El `GameManager` de la v1 se destruía en cada cambio de escena, así que no
  conservaba nada entre niveles. Además mezclaba la UI de Game Over, las monedas y la navegación.
- **Causa.** No tenía `DontDestroyOnLoad`: cada escena llevaba su propia copia. Era una clase con
  tres responsabilidades sin relación.
- **Solución.**
  - `GameManager` es un Singleton persistente, el único del proyecto, y solo lleva la progresión.
  - `Bootstrap` crea los sistemas persistentes antes de cargar la primera escena, sea cual sea.
    Salen de `Resources/Sistemas.prefab`: `GameManager`, `GameStateMachine`, `SceneLoader` y
    `EconomyManager`. Ninguna escena necesita su propio `GameManager`, y darle a Play en cualquier
    escena funciona.
  - Las monedas pasan a `EconomyManager`, la navegación a `GameStateMachine` y la pantalla de
    Game Over a la UI (Fase 6).

### 2. Las monedas no se guardaban nunca

- **Problema.** Las monedas volvían a 0 cada vez que se abría el juego.
- **Causa.** `CargarMonedas()` leía los `PlayerPrefs`, pero ningún método los escribía. El
  progreso estaba repartido en claves sueltas.
- **Solución.** `SaveSystem` (patrón Facade) guarda la partida en
  `persistentDataPath/fragmentum_guardado.json`. Incluye el nivel máximo alcanzado, los niveles
  completados, los fragmentos, las habilidades, las monedas, el checkpoint, las cinemáticas vistas
  y las memorias.
  - **Escritura atómica:** escribe primero en un temporal y después lo reemplaza, dejando el
    archivo anterior como `.bak`. Si el archivo está dañado, recupera la copia; si tampoco sirve,
    empieza una partida nueva y avisa. Nunca lanza una excepción al jugador.
  - **Cuándo se guarda:** al completar un nivel, al activar un checkpoint, al volver al menú y al
    cerrar el juego.
  - **Migración:** si no hay JSON pero sí la clave `Monedas` de la v1, se importan esas monedas.
    La clave antigua no se borra.
  - Los fragmentos y las habilidades se guardan por nombre (`IRA`, `ATAQUE_CARGADO`), así que
    reordenar los enum no corrompe las partidas.
  - Lo verifican `SaveSystemTests` y `Monedas_SeGuardanAlVolverAlMenu`.

### 3. El selector cargaba escenas que no existían

- **Problema.** El selector generaba 10 botones que cargaban «Level1»…«Level10», y hacer clic
  daba error porque esas escenas no existían. Además:
  - Se podía entrar al nivel final desde el principio.
  - Los botones decían «Level 3».
  - No limpiaba los botones anteriores al regenerarse.
- **Causa.** Los nombres de escena se formaban concatenando texto y no existía ningún modelo de
  datos del nivel.
- **Solución.**
  - **`LevelData`** (patrón Type Object): un asset por nivel con escena, mundo, emoción, paleta,
    jefe, fragmento, habilidad, reglas y secciones.
  - **`LevelDatabase`**: la lista ordenada de niveles y las escenas de menú y selector. Es la única
    fuente de verdad.
  - **`ReferenciaEscena`**: la escena se elige arrastrando el asset y se guarda su ruta.
    `EnBuild` dice si de verdad se puede cargar.
  - **El selector** crea un botón por nivel de la base de datos, con el texto «3. Miedo — Bosque
    Tenebroso» y el color de la emoción. Los niveles no desbloqueados aparecen bloqueados; los
    que no tienen escena, «en construcción»; y los superados, con su fragmento integrado.
  - **`SceneLoader`** comprueba que la escena esté en Build Settings antes de cargarla. Si no lo
    está, avisa y no carga nada.
  - **Build Settings** se generan desde la base de datos, y su inspector avisa si alguna escena
    no está incluida.
  - Lo verifican `Flujo_MenuSelectorNivelCompletado_DesbloqueaYGuarda`,
    `NivelSinEscenaOBloqueado_NoSeCargaNiRompeNada` y `LevelDatabaseTests`.

### 4. El menú no tenía «Continuar» y «Salir» no hacía nada en el Editor

- **Problema.** El menú principal tenía los nombres de escena escritos en el código («Niveles»,
  «Level1»). No existía la opción de continuar la partida, y «Salir» no funcionaba en el Editor.
- **Causa.** El menú cargaba escenas por su cuenta, y `Application.Quit()` no tiene efecto en el
  Editor.
- **Solución.** `MainMenu` pide cada acción a la FSM global:
  - **Continuar** lleva al nivel desbloqueado más avanzado que tenga escena, y solo está activo si
    hay progreso.
  - **Nueva partida** empieza desde cero; la partida anterior queda en la copia `.bak`.
  - **Salir** guarda y también funciona en el Editor.
  - Ya no queda ningún nombre de escena en el código.

### 5. `Time.timeScale` se escribía desde cinco sitios

- **Problema.** La pausa, el Game Over y los cambios de escena se pisaban: cualquiera de ellos
  podía dejar el juego congelado o en marcha cuando no tocaba.
- **Causa.** La v1 no tenía un estado global del juego y escribía `Time.timeScale` en cinco
  scripts distintos.
- **Solución.** **`GameStateMachine`**, una FSM de 9 estados sobre el mismo núcleo que el jugador
  y los enemigos: BOOT, MAIN_MENU, LEVEL_SELECT, LOADING, PLAYING, PAUSED, CUTSCENE, GAME_OVER y
  LEVEL_COMPLETE. Los estados de dentro de un nivel forman el grupo EN_NIVEL.
  - Es la única que escribe `Time.timeScale` y la única que bloquea el input de juego
    (`EventosJuego.EntradaBloqueada`, que respeta `FuenteInputJugador`). Los efectos de cada
    estado están en una sola tabla.
  - Es la única que pide cargar escenas. El menú, el selector y los niveles llaman a su API.
  - Cuenta el tiempo sin escala, así que sus esperas funcionan con el juego en pausa.
  - La muerte del jugador en un nivel lleva al Game Over mediante el evento global
    `EventosJuego.JugadorMurio`, sin que la FSM conozca al jugador.
  - El diagrama `Docs/FSM/Juego.md` se genera desde su tabla.
  - Lo verifican `TablaJuegoTests` y `Pausa_CongelaElTiempoYBloqueaElInput`.

### 6. El diseño de los niveles solo existía en el documento

- **Problema.** Emociones, mundos, jefes, fragmentos y habilidades del Anexo A no estaban en el
  proyecto: cada escena tendría que haberlos repetido a mano.
- **Causa.** No había dónde guardar esos datos.
- **Solución.**
  - *Fragmentum → Progresión* genera desde el Anexo A las 6 emociones, las 4 habilidades, los
    5 fragmentos, los 5 jefes y los 6 niveles. Cada nivel lleva su paleta, sus reglas (sigilo,
    oscuridad, agacharse) y sus secciones.
  - Los nombres canónicos siguen el GDD: Centinela Ardiente, Juez Silente y Esperanza.
  - Al entrar en un nivel, el `GameManager` aplica al jugador las habilidades guardadas y las
    reglas del nivel.
  - Lo verifican `DatosProgresionTests` y `EnUnNivel_SeAplicanLasHabilidadesGuardadasYLasReglas`.

### Decisiones que ajustan el plan de la Fase 1

1. **«Seleccionar nivel» y «Volver» pasan por LOADING.** El menú y el selector son escenas
   distintas, así que no existen las transiciones directas MAIN_MENU → LEVEL_SELECT y
   LEVEL_SELECT → MAIN_MENU del plan (§3.6).
2. **Nueva transición BOOT → LEVEL_SELECT**, para darle a Play con el selector abierto. BOOT →
   PLAYING cubre también las escenas de prueba.
3. **GAME_OVER y LEVEL_COMPLETE avanzan solos** tras 1,5 s mientras no existan sus pantallas
   (Fase 6): el Game Over reintenta y el fin de nivel continúa. El juego nunca se queda atascado.
4. **La UI usa el `Text` integrado de Unity, no TextMeshPro**, porque los recursos esenciales de
   TMP no están importados (PENDIENTES B9). `FabricaUI` concentra el estilo, así que el cambio
   posterior se hará en un solo sitio.
5. **Las escenas de prueba no son niveles.** El `GameManager` no les impone progreso ni reglas, y
   la muerte del jugador en ellas no lleva al Game Over.

### Además

- **`AtajosDepuracion`** (solo en el Editor y en builds de desarrollo): Esc pausa, F9 completa el
  nivel y F10 vuelve al menú. Sirven para probar el flujo hasta que existan la salida de nivel y
  los jefes.
- **`Moneda`** suma en `EconomyManager` y **`ContadorMonedas`** escucha su evento.
- **Nuevo menú *Fragmentum → Progresión*:** generar y restablecer los datos, crear las escenas de
  menú y selector, configurar Build Settings y validar la base de datos. *Regenerar todo* incluye
  la progresión.
- **Pruebas sin tocar la partida real:** en el ejecutor de pruebas, `Bootstrap` no crea los
  sistemas. Las pruebas montan los suyos y guardan en una carpeta temporal.
- **La v1 (`Tesis`) se actualizó a 6000.6.3f1** en un commit aparte. Son los cambios que hizo el
  propio editor al abrirla en el PC nuevo, sin ningún script.

### Correcciones tras la prueba en el Editor (2026-09-29)

- **Un doble clic en el selector pedía una segunda carga**, y la consola decía «No se puede
  cambiar de escena desde LOADING».
  - *Causa:* el fundido activaba `blocksRaycasts`, pero su lienzo no tenía `GraphicRaycaster`, así
    que no interceptaba ningún clic.
  - *Solución:* el lienzo del fundido tiene su `GraphicRaycaster`, y al empezar la carga se quita
    la selección de teclado. Lo verifica `DuranteLaCarga_ElFundidoSeTragaLosClics`.
- **Abrir `Menu_Inicial` en el Editor la marcaba como modificada.**
  - *Causa:* en modo batch no se calculan los layouts de uGUI. Además, al cargar la escena Unity
    sube el tamaño máximo de cada texto hasta su tamaño de fuente.
  - *Solución:* el generador calcula los layouts antes de guardar y `FabricaUI` fija el tamaño
    máximo. Las escenas de menú y selector se regeneraron conservando su GUID.
- **Aviso UAC0009:** Unity 6000.6 marca como obsoleta la directiva `DEVELOPMENT_BUILD`.
  `AtajosDepuracion` comprueba ahora `Debug.isDebugBuild`.

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **113 de 113 correctas** (34 nuevas: guardado, base de datos, tabla del juego
  y datos del Anexo A).
- Pruebas PlayMode: **39 de 39 correctas** (8 nuevas de progresión, con las escenas reales de menú,
  selector y Nivel 1).

### Pasos manuales en el Editor

Ninguno obligatorio. Ya están generados:
- los datos (`Assets/Data/Emociones`, `Habilidades`, `Fragmentos`, `Jefes` y `Niveles`);
- las escenas `Menu_Inicial` y `Niveles`;
- `Assets/Resources/Sistemas.prefab`;
- Build Settings: `Menu_Inicial`, `Niveles` y `Nivel1`.

Notas:
- **Para jugar desde el principio,** abre `Menu_Inicial` y dale a Play. También se puede dar a
  Play en cualquier otra escena: los sistemas se crean solos.
- **`Nivel1` todavía no tiene jugador.** Lo colocará el `LevelManager` de la Fase 6.
- **Para borrar la partida,** elimina
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Tesis2.0\fragmentum_guardado.json` o usa
  «Nueva partida».

### Qué probar a mano (desde `Menu_Inicial`)

- En una partida nueva, «Continuar» está desactivado y «Nueva partida» lleva al Nivel 1.
- El selector muestra los 6 mundos con su emoción. Solo el primero está disponible.
- En el Nivel 1, Esc pausa (el tiempo se detiene) y F9 completa el nivel. De vuelta en el
  selector, el Nivel 1 dice «Fragmento integrado» y el 2, «En construcción».
- Cierra y vuelve a abrir el juego: el progreso sigue ahí y «Continuar» está activo.
- Un doble clic rápido sobre un mundo solo lo carga una vez.

---

## Fase 4 — Enemigos (2026-09-28)

Rama `fase-4-enemigos`, que sale de `fase-3-jugador`. Cubre la tarea A del encargo, una de las
dos de prioridad máxima.

### 1. La máquina de estados del enemigo era decorativa

- **Problema.** La v1 tenía un `enum STATES` con solo IDLE y ATTACK. `currentState` se asignaba
  en `Start()` y no volvía a cambiar, el `switch` estaba comentado y la IA vivía en condicionales
  anidados dentro de `EnemyMovement()`.
- **Causa.** El enum no gobernaba nada: la conducta dependía de banderas (`attack`, `takingDamage`,
  `death`) que se escribían en varios sitios.
- **Solución.** `EnemyController` es una FSM real de 6 estados (IDLE, PATROL, CHASE, ATTACK,
  DAMAGED y DEAD) sobre el mismo núcleo que el jugador:
  - Todos los cambios pasan por el único `TransitionTo`, validado contra la tabla.
  - El diagrama `Docs/FSM/Enemigo.md` se genera desde esa tabla.
  - Cada estado fija el Animator en su `OnEnter`, en lugar de los cuatro `SetBool` por frame de la
    v1. Los parámetros conservan su nombre (`MovEn`, `Attack`, `Damage` y `Death`).
  - Se mantienen `CancelAttack()` y `DesactiveDamage()` como receptores de los eventos de
    animación de la v1. Solo pueden **acortar** un estado; su final está garantizado por tiempo.

### 2. El enemigo solo atacaba si el jugador estaba a su izquierda

- **Problema.** Si el jugador se acercaba por la derecha, el enemigo lo perseguía pero nunca lo
  golpeaba. Además, el volteo del sprite estaba invertido.
- **Causa.** El bloque de ataque estaba anidado dentro de `if (direction.x < 0)`, y la escala se
  fijaba con el signo contrario al de la dirección.
- **Solución.**
  - CHASE → ATTACK se decide por distancia horizontal y diferencia de altura, sin mirar el lado.
  - ATTACK se gira hacia el jugador al entrar.
  - El volteo se hace sobre un hijo `Pivote` y respeta `EnemyData.spriteMiraDerecha`.
  - Lo verifican `AtacaAlJugador_PorLaIzquierda` y `AtacaAlJugador_PorLaDerecha`.

### 3. Fuera del radio el enemigo se quedaba inmóvil

- **Problema.** Todos los enemigos se sentían iguales y predecibles; así lo recogieron las
  pruebas de usabilidad.
- **Causa.** No había patrulla ni parámetros por tipo de enemigo.
- **Solución.**
  - PATROL entre waypoints (`Transform[]`), con tolerancia y espera configurables. Si un borde o
    una pared le cortan el paso, da la vuelta.
  - Sin waypoints, el enemigo se queda en IDLE sin lanzar ninguna excepción.
  - Lo verifican `SinJugadorCerca_PatrullaEntreSusWaypoints` y `SinWaypoints_SeQuedaEnIdleSinErrores`.

### 4. El enemigo parpadeaba en el borde del radio y atacaba sin pausa

- **Problema.** En el borde del radio de detección el enemigo alternaba entre perseguir y parar.
  Tampoco había cooldown de ataque.
- **Causa.** Se usaba el mismo radio para empezar y para dejar de perseguir, y `attack` solo se
  apagaba por un evento de animación.
- **Solución.**
  - **Histéresis:** el enemigo empieza a perseguir dentro de `detectionRadius` y solo suelta al
    jugador más allá de `detectionRadius × 1,5`.
  - **Cooldown** por tiempo: empieza al salir de ATTACK.
  - Lo verifican `Histeresis_NoSueltaAlJugadorHastaUnRadioYMedio` (una sola entrada en CHASE) y
    `Ataques_RespetanElCooldown`.

### 5. El daño y la muerte no eran estados reales

- **Problema.** En la v1:
  - `death` congelaba al enemigo, pero seguía colisionando y haciendo daño.
  - Recibir un golpe no interrumpía el ataque.
- **Causa.** Eran banderas sin comportamiento de entrada ni de salida.
- **Solución.**
  - DAMAGED interrumpe ATTACK y cierra el arma. Aplica un retroceso atenuado por
    `resistenciaRetroceso`, y los i-frames vienen de los datos.
  - DEAD apaga todos los colliders y hitbox, deja el cuerpo cinemático, dispara `Death` y
    destruye el objeto tras `deathDelay`.
  - Lo verifican `Golpe_InterrumpeElAtaqueYCierraElArma` y `Muerte_ApagaColisionesYDestruyeElObjeto`.

### 6. Parámetros duplicados por escena y referencia al jugador manual

- **Problema.** `public Transform player` había que asignarlo a mano en cada escena, o saltaba un
  `NullReferenceException`. `life` era un float y los parámetros vivían sueltos en cada instancia.
- **Causa.** No existía un modelo de datos del enemigo.
- **Solución.**
  - **`EnemyData`** (patrón Type Object) reúne vida entera, daño, velocidades, radios, cooldown,
    duraciones, retroceso, detección y tinte provisional.
  - El jugador se busca por tag en tiempo de ejecución. Si no lo encuentra, avisa una vez,
    patrulla y reintenta cada segundo.

### 7. Variantes por mundo emocional sin duplicar scripts

- **Problema.** El encargo pide enemigos distintos por emoción (Ira agresiva, Tristeza lenta,
  Miedo que solo ataca en la oscuridad) sin copiar el controlador.
- **Causa.** En la v1 la única forma de variar un enemigo era editar su script.
- **Solución.** Detección como **patrón Strategy** (`DeteccionData`, ScriptableObject sin estado).
  Todas las estrategias salvo la del ruido fallan si el jugador está invisible, así que la
  Invisibilidad Parcial tiene efecto real:
  - `PorDistancia`: genérica.
  - `ConVision`: no ve a través del suelo ni de las paredes.
  - `EnOscuridad`: ignora al jugador si está iluminado. Usa el nuevo registro `Iluminacion` y el
    componente `ZonaIluminada`, base de la antorcha del Nivel 3.
  - `PorRuido`: oye el `OnRuido` de la región de sigilo del jugador.

  Hay 6 variantes con valores del Anexo A: Genérico, Soldado de lava, Golem de Ceniza (vida 10),
  Lamento, Espectro y Centinela Ciego. `VariantesEmocionales_SeComportanDeFormaDistinta` mide que
  el Soldado de lava persigue más del doble de rápido que el Lamento, y `DatosEnemigosTests`
  vigila que los datos mantengan esa correspondencia emoción ↔ mecánica.

### Decisiones que ajustan el plan de la Fase 1

1. **El cuerpo del jugador ya no choca con el de los enemigos, ni los enemigos entre sí.** Se
   añaden dos parejas a la matriz (Player × Enemy y Enemy × Enemy). El daño por contacto llega por
   una hitbox `Contacto` (capa EnemyWeapon), que sustituye al `OnCollisionEnter2D` de la v1. Si los
   cuerpos chocaran, el jugador podría quedarse de pie sobre un enemigo, que no es suelo, y los
   enemigos se atascarían al patrullar.
2. El pseudoestado de elección del diagrama del plan («¿hay waypoints?») se resuelve arrancando
   siempre en IDLE, sin espera inicial: si hay ruta, IDLE → PATROL ocurre en el primer frame.
3. Se añade la **evitación de bordes y paredes** (`EnemyData.evitarBordes`), que no estaba en el
   plan. Sin ella, un enemigo que persigue cae a la lava o se queda empujando una pared.
4. El enum se llama `ESTADOS_ENEMIGO`, igual que `ESTADOS_JUGADOR`, en lugar del `STATES` de la v1.

### Además

- `Health.Inicializar` acepta también la duración de los i-frames, que ahora vienen de los datos.
- `EnemyController.Activos` es un registro de los enemigos vivos. Lo usa `DepuracionEnemigos`,
  que muestra con F2 el estado y la vida de cada enemigo sobre su cabeza.
- Herramientas de Editor:
  - Menú *Fragmentum → Enemigos*: generar y restablecer los datos, crear los prefabs (base y 5
    variantes) y crear la escena de prueba `PruebaEnemigos`.
  - *Fragmentum → Regenerar todo* reconstruye, también en modo batch, todo lo que generan las
    herramientas del proyecto.
  - Las piezas comunes están en `UtilidadesEditor`.
- Las pruebas PlayMode comparten la base `PruebaPlayModeBase`.
- **`Herramientas/verificar.ps1`** deja la verificación dentro del repositorio.
  - Sin parámetros, compila el juego, el Editor y las pruebas con el SDK de .NET que trae Unity,
    sin abrir el editor.
  - Con `-Unity`, además regenera todo y ejecuta las pruebas EditMode y PlayMode en modo batch.
  - En el PC anterior este proyecto de verificación vivía fuera del repositorio y se perdió.

### Correcciones de fases anteriores

- **Los diagramas del jugador estaban vacíos en el commit de la Fase 3.** Un paso manual que
  normalizaba los saltos de línea falló por una ruta relativa y dejó los dos archivos en blanco.
  Se regeneraron, y el exportador ya escribe siempre saltos LF, así que ese paso deja de hacer falta.
- **Regenerar una escena de prueba le cambiaba el GUID**, porque se borraba y se volvía a copiar.
  Ahora se sobrescribe el contenido conservando el `.meta`, así que las referencias a la escena
  (Build Settings, `LevelDatabase`) no se romperán. Se comprobó regenerando dos veces seguidas.

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **79 de 79 correctas** (18 nuevas: tabla del enemigo, estrategias de detección
  y datos del Anexo A).
- Pruebas PlayMode: **31 de 31 correctas** (15 nuevas de enemigos).
- Regeneración completa en modo batch sin avisos; matriz de colisión con 10 parejas.

### Pasos manuales en el Editor

Ninguno obligatorio. Los datos (`Assets/Data/Enemigos`), los prefabs (`Assets/Prefabs/Enemigos`)
y la escena `Assets/Scenes/Pruebas/PruebaEnemigos.unity` ya están generados.

- **Para colocar un enemigo en un nivel:**
  1. Arrastra el prefab de la variante.
  2. Crea dos objetos vacíos como puntos de la ruta y asígnalos en `Waypoints`.
  3. Opcional: cambia su `EnemyData`.
- **Parámetros del Animator** (no cambia ninguno): `MovEn` en PATROL y CHASE; `Attack`, `Damage`
  y `Death` (bool).

### Qué probar a mano (escena `PruebaEnemigos`, F2 muestra los estados)

- Acércate a cada enemigo por la izquierda y por la derecha: debe atacar por ambos lados.
- Sal de su radio y vuelve a entrar despacio: no debe parpadear entre perseguir y patrullar.
- Compara el Soldado de lava (x = 30) con el Lamento (x = 62): se nota la diferencia de ritmo.
- Golpea a un enemigo mientras levanta el arma: el ataque se corta.
- Mata a uno: desaparece al segundo y ya no hace daño al tocarlo.
- Espectro (x = 78): dentro del círculo de luz (x = 71) no se acerca; fuera, sí.
- Centinela Ciego (x = 94): si corres cerca te oye; la invisibilidad (Q) no lo engaña.
- Enemigo de la plataforma (x = 110): te persigue hasta el borde, pero no se tira.

---

## Fase 3 — Jugador (2026-09-28)

Rama `fase-3-jugador`, que sale de `fase-2-cimientos`. Es la primera fase jugable de la v2: el
jugador completo como máquina de estados, su prefab y una escena de prueba con una zona para cada
criterio de aceptación.

### 1. El salto era flotante o infinito

- **Problema.** En la v1, mantener el espacio en el suelo volvía a fijar la velocidad de salto en
  cada frame: el jugador flotaba mientras durase la pulsación. Además, el `return` de ese bloque se
  saltaba el ataque, el wall slide y la actualización del Animator durante ese frame.
- **Causa.** Se usaba `Input.GetKey` (tecla mantenida) donde hacía falta una pulsación, y no había
  ningún estado que recordara que el salto ya se había hecho.
- **Solución.**
  - JUMP aplica el impulso **una sola vez**, en su `OnEnter`, y consume la pulsación del jump buffer.
  - La tabla de transiciones no permite volver a saltar desde DOUBLE_JUMP.
  - Mantener el botón solo alarga la subida hasta su altura máxima; soltarlo antes la recorta
    (salto de altura variable).
  - Lo verifican `MantenerElSalto_DaUnSoloSaltoDeAlturaNormal` y `SoltarElSaltoPronto_SaltaMenos`.

### 2. El movimiento teletransportaba al jugador

- **Problema.** La v1 movía al jugador con `transform.position`, así que el movimiento peleaba con
  el dash, con el rebote del daño y con las colisiones. Además, el blend tree recibía `runSpeed²`.
- **Causa.** La física se aplicaba en `Update`, sin separar la lectura del input de la simulación,
  y `runSpeed` se multiplicaba dos veces.
- **Solución.**
  - `PlayerMotor` es la única pieza que toca el `Rigidbody2D`. El control continuo se aplica en
    `FixedUpdate` con `linearVelocity`, con aceleración y frenado.
  - Los impulsos instantáneos (salto, dash, retroceso) fijan la velocidad una sola vez, al entrar
    en su estado.
  - El parámetro `Movement` del Animator recibe la velocidad real en u/s.
  - Lo verifica `Correr_MueveElRigidbodyPorVelocidad`.

### 3. Dos plataformas solapadas rompían la detección de suelo

- **Problema.** Al salir de una de dos plataformas solapadas, `isGrounded` pasaba a `false` aunque
  el jugador siguiera apoyado en la otra.
- **Causa.** Era un bool que se encendía y apagaba con `OnTriggerEnter/Exit`, filtrando por el tag
  `suelo`.
- **Solución.** `CheckGround` pregunta a la física en cada frame (`Physics2D.OverlapBox` filtrado
  por **capa**) qué hay bajo los pies. No guarda estado que pueda desincronizarse y cuenta los
  contactos. Lo verifica `PlataformasSolapadas_NoRompenLaDeteccionDeSuelo`: el jugador pisa las dos
  plataformas a la vez y nunca cae.

### 4. El wall jump y el dash no hacían lo que decían

- **Problema.**
  - El input horizontal anulaba el impulso del wall jump en el mismo frame.
  - La dirección del wall jump salía de `localScale.x`, así que dependía de hacia dónde mirase el sprite.
  - El `dashCooldown` público se sobrescribía con 1 al usarlo.
  - El dash solo funcionaba si ya te estabas moviendo.
- **Causa.** `isWallJumping` se activaba, pero nada lo consultaba. El dash era un `AddForce` metido
  dentro de `Movement()`.
- **Solución.**
  - WALL_JUMP es un estado que no aplica control horizontal mientras dura `wallJumpDuration`.
  - La dirección del wall jump sale del lado de la pared que detectan los sensores.
  - DASH es un estado con duración y cooldown propios. Anula la gravedad mientras dura, sale hacia
    donde mira el jugador y, desde una pared, hacia fuera.
  - En el aire hay un dash por salto.
  - Lo verifican `WallJump_SaleHaciaFueraDeLaPared` y `Dash_RecorreSuDistanciaYRespetaElCooldown`.

### 5. El salto se sentía injusto en los bordes

- **Problema.** Saltar en el último instante al llegar a un borde, o justo antes de aterrizar, no
  funcionaba.
- **Causa.** No había *coyote time* ni *jump buffer*.
- **Solución.**
  - Coyote time de 0,1 s al salir de un borde caminando.
  - Jump buffer de 0,1 s: una pulsación hecha justo antes de aterrizar también salta.
  - Coyote de pared: FALL → WALL_JUMP poco después de soltar la pared. Conserva el
    `wallJumpingTime` de la v1.
  - Lo verifican `CoyoteTime_PermiteSaltarJustoTrasElBorde` y
    `SinCoyoteTime_SaltarEnElAireUsaElDobleSalto`.

### 6. La vida vivía dentro del movimiento

- **Problema.** En la v1:
  - `PlayerMovement` restaba vida y llamaba directamente a `GameManager.Instance.GameOver()`.
  - `LifeBar` hacía `GameObject.Find("Player")` (y encontraba el sprite decorativo que también se
    llamaba Player) y leía la vida en cada frame.
- **Causa.** Un solo script llevaba el movimiento, el combate y la vida, y los demás lo buscaban
  por nombre.
- **Solución.**
  - `PlayerHealth` (hereda de `Health`) avisa por eventos.
  - `PlayerController` convierte `OnGolpeRecibido` en DAMAGED y `OnMuerte` en DEAD. Esos cambios
    pasan por `TransitionTo` y por la tabla, como cualquier otro.
  - `LifeBar` escucha `OnVidaCambiada` y se pinta solo cuando la vida cambia.
  - El encargo pedía los eventos `OnDamaged`, `OnHealed` y `OnDeath`; aquí se llaman
    `OnGolpeRecibido`, `OnCurado` y `OnMuerte`, en español como el resto de la API pública (§5).

### 7. Las habilidades y las mecánicas de nivel no tenían dónde engancharse

- **Problema.** El encargo pide cuatro habilidades desbloqueables y mecánicas de nivel (agua,
  agacharse, sigilo) sin añadir scripts sueltos ni banderas nuevas.
- **Causa.** En la v1 no existía ningún punto común donde conectarlas.
- **Solución.** Las habilidades son **modificadores** que consultan los estados existentes
  (`PlayerAbilities`):
  - *Ataque Cargado* (Ira): pulsar ataca al instante, mantener carga durante 0,6 s y soltar lanza
    un ataque cargado de daño doble, marcado como `Cargado` para los barriles explosivos.
  - *Dash Acuático* (Tristeza): DASH solo se permite bajo el agua con esta habilidad.
  - *Invisibilidad Parcial* (Miedo): tecla Q, 3 s de duración y 8 s de espera; el jugador se ve
    al 40 % de opacidad.
  - *Defensa Reforzada* (Culpa): un escudo en `PlayerHealth` absorbe un golpe cada 15 s. Los
    golpes letales (lava, vacío) lo atraviesan.

  Las mecánicas de nivel también se integran en la FSM en lugar de ser banderas:
  - `ModificadorMovimiento` y `ZonaModificadorMovimiento` para el agua (gravedad ×0,3, velocidad
    ×0,6) y el hielo.
  - CROUCH es un estado.
  - El sigilo es una **región ortogonal**: una segunda FSM (`PlayerModoMovimiento`, con los modos
    STEALTH, NORMAL y RUN) que publica `OnRuido`.

### 8. El diagrama no mostraba la jerarquía

- **Problema.** El exportador dibujaba los grupos como estados hermanos. LOCOMOCION no aparecía
  dentro de VIVO, aunque en el código es un superestado suyo.
- **Causa.** Cada estado se asignaba al primer grupo declarado que lo contenía.
- **Solución.**
  - Un grupo contenido en otro se dibuja **anidado**, y cada transición se escribe en el bloque
    más interno que contiene sus dos extremos. El resultado está en `Docs/FSM/Jugador.md`.
  - La búsqueda de FSM usa `TypeCache`, porque Unity 6000.6.3 avisa (UAC0005) contra recorrer
    `AppDomain.GetAssemblies()`.

### 9. Los criterios de aceptación solo se podían comprobar jugando

- **Problema.** Comprobar el salto, el suelo o el wall jump exigía abrir el editor y probar a mano
  cada vez.
- **Causa.** No existía ninguna forma de mover al jugador sin teclado.
- **Solución.** **Pruebas PlayMode** con el prefab real y física real, controladas por
  `InputSimulado`. Es la misma interfaz `IFuenteInput` que el teclado (patrón Adapter), así que el
  jugador no distingue una prueba de una partida. Son 16 pruebas que cubren salto, doble salto,
  suelos solapados, movimiento por física, dash, coyote time, pared, daño, muerte, agacharse,
  ataque a ambos lados, ataque cargado, agua e invisibilidad.

### Decisiones que ajustan el plan de la Fase 1

1. **DASH y ATTACK salen de los 8 subestados de LOCOMOCION**, no solo de 5. El superestado
   implementa la salida una sola vez y cada subestado solo refina la condición: agachado, solo si
   cabe de pie; desde la pared, el dash sale hacia fuera. Así el diagrama coincide exactamente con
   el código.
2. **FALL → WALL_JUMP** (coyote de pared), que no estaba en el diagrama del plan.
3. DASH, ATTACK y DAMAGED terminan en IDLE o FALL. Si hay input, IDLE → RUN se encadena en el mismo
   frame, porque la máquina permite hasta 8 transiciones seguidas.
4. La espera de 8 s de la invisibilidad se cuenta desde que termina, no desde que empieza.

### Además

- `CameraMovement` sigue al jugador sin salirse de los límites del nivel, calculados sobre el área
  visible. `DeadZone` envía un `Golpe.Letal`.
- `ReaparicionSimple` devuelve al jugador al inicio al morir. Es provisional: la sustituye el
  CheckpointManager en la Fase 6.
- Piezas de «construir para ausencia»:
  - `SpriteProvisional` dibuja un rectángulo de color y avisa si falta el sprite.
  - `InputNeutro` (patrón Null Object) deja al jugador quieto si no hay fuente de input.
- `DepuracionJugador` es un panel en pantalla (F1) con los estados de las dos FSM, los sensores,
  la vida y las habilidades.
- Input:
  - Nueva acción `Ability` (Q o botón RB) para la invisibilidad.
  - Ctrl izquierdo también agacha, como pide el Anexo A.
  - `FuenteInputJugador` se ejecuta antes que el resto de scripts, para que el jump buffer no
    pierda un frame.
- `Object.FindFirstObjectByType` está obsoleto en Unity 6000.6; se usa `FindAnyObjectByType`.
- El proyecto se actualizó a **Unity 6000.6.3f1**, en un commit aparte.
- Menú *Fragmentum → Jugador*: crear el prefab, crear la escena de prueba y preparar un tilemap
  como suelo. Admite modo batch (`ConstruirEnModoBatch`).

### Verificación

- Compilación con `dotnet build` contra las DLL de Unity 6000.6.3f1: 0 errores, 0 avisos.
- Compilación real en Unity en modo batch: 0 errores, 0 avisos (analizadores incluidos).
- Pruebas EditMode: **61 de 61 correctas** (27 de la Fase 2 y 34 nuevas: tabla del jugador, escudo,
  habilidades y exportador).
- Pruebas PlayMode: **16 de 16 correctas**.
- Prefab, escena de prueba y diagramas generados en modo batch, sin avisos.

### Pasos manuales en el Editor

Ninguno obligatorio. El prefab `Assets/Prefabs/Player.prefab` y la escena
`Assets/Scenes/Pruebas/PruebaJugador.unity` ya están generados.

- **Para jugar:** abre `PruebaJugador` y dale a Play. El panel de la izquierda (F1) explica qué
  probar en cada zona.
- **Para usar el jugador en `Nivel1`:**
  1. Selecciona el objeto `Tilemap`.
  2. Ejecuta *Fragmentum → Jugador → Preparar tilemap seleccionado como suelo*.
  3. Arrastra `Player.prefab` a la escena.
- **Parámetros del Animator:** no cambia ninguno. El jugador usa `Movement` (float, u/s), `Attack`,
  `Damage` y `Death` (bool). Todavía no hay controlador de Kael: ver `PENDIENTES.md`, apartado B3.

### Qué probar a mano

- Correr, frenar y girar: la respuesta debe ser inmediata y sin deslizar.
- Mantener el salto sube hasta el máximo; tocarlo da un salto bajo; el doble salto funciona una
  sola vez por salto.
- Salir caminando de un borde y saltar tarde todavía salta.
- Pasar por los suelos solapados (x = 6 a 10): en el panel, «Suelo» debe mostrar 2 contactos sin
  parpadear.
- Agacharse bajo el techo bajo: no debe levantarse debajo de él.
- En el pozo, deslizar por las paredes y encadenar wall jumps hasta la cornisa.
- Pinchos: daño, retroceso y parpadeo. El escudo absorbe el primer golpe.
- Muñeco: clic para el ataque normal; mantener y soltar para el cargado (la consola lo indica).
- Agua: movimiento lento y flotante; el dash solo funciona si tienes Dash Acuático (en la escena
  de prueba están desbloqueadas todas las habilidades).
- Q: el jugador se vuelve semitransparente durante 3 s.
- Caer al vacío: muerte y reaparición.

---

## Fase 2 — Cimientos (2026-09-19)

Rama `fase-2-cimientos`. Sin cambios de jugabilidad todavía: esta fase levanta la base sobre la
que se construyen el jugador, los enemigos, los jefes y el juego global.

### 1. Las máquinas de estado eran decorativas

- **Problema.** En la v1 existían un `enum STATES` y comentarios de FSM, pero el estado se
  asignaba una sola vez en `Start()` y nunca cambiaba. Toda la IA vivía en condicionales
  anidados dentro de `Update()`, y el diagrama de la tesis no correspondía a ningún código.
- **Causa.** No había ninguna estructura que representara un estado ni que controlara las
  transiciones: solo banderas booleanas dispersas.
- **Solución.** Núcleo genérico de FSM (patrón State), común a las cuatro máquinas del juego:
  - `IEstado<TEstado>` con `OnEnter`, `OnExit`, `Actualizar`, `ActualizarFisica` y
    `EvaluarTransiciones`. Un estado **nunca** cambia el estado: lo pide.
  - `MaquinaEstados<TEstado>` con un **único** `TransitionTo`, que ejecuta `OnExit` y `OnEnter`
    en ese orden, emite `OnStateChanged` y corta los ciclos a las 8 transiciones por frame.
  - `TablaTransiciones<TEstado>`: la función de transición declarada en un solo sitio, con
    superestados. `TransitionTo` **rechaza y avisa** ante cualquier transición no declarada.

### 2. Los diagramas de la tesis podían desviarse del código

- **Problema.** Los diagramas de estados se dibujaban a mano, así que nada garantizaba que
  siguieran correspondiendo al código después de cada cambio.
- **Causa.** El diseño vivía en el documento y la lógica en el código, sin fuente común.
- **Solución.** `ExportadorDiagramasFSM` (menú *Fragmentum → Exportar diagramas FSM*) genera el
  Mermaid **desde la tabla de transiciones**, con los superestados como estados compuestos, y
  escribe un archivo por máquina en `Docs/FSM/`. El diagrama deja de ser una promesa.

### 3. El daño se repartía por tag y golpeaba a quien no debía

- **Problema.** En la v1 cualquier collider con el tag `Weapon` dañaba a lo que tocara: el arma
  de un enemigo hería a otros enemigos, dañaba al jefe y rompía las cajas.
- **Causa.** El filtro era un tag compartido por el arma del jugador y la de los enemigos, y cada
  receptor decidía por su cuenta qué hacer.
- **Solución.** Combate con contrato explícito: la estructura `Golpe` (daño, origen, atacante, si
  es cargado y si ignora invulnerabilidad), la interfaz `IGolpeable` y el componente `Hitbox`,
  que filtra por **capa de física**. Las capas `PlayerWeapon` y `EnemyWeapon` ya no colisionan con
  su propio bando.

### 4. La vida y la invulnerabilidad estaban rotas

- **Problema.** En la v1, tres fallos distintos venían del mismo sitio:
  - La zona de caída no mataba si el jugador estaba parpadeando tras un golpe: caía sin fin.
  - El jefe quedaba invulnerable para siempre tras el primer impacto, porque su bandera de daño
    solo se apagaba con un evento de animación que su Animator no tenía.
  - La vida era un `float` en los enemigos aunque el daño fuera entero.
- **Causa.** Cada entidad implementaba su propia vida con banderas y dependía de eventos de
  animación para volver a un estado válido.
- **Solución.** Componente `Health` compartido:
  - Vida **entera** con eventos (`OnVidaCambiada`, `OnGolpeRecibido`, `OnCurado`, `OnMuerte`,
    `OnRevivir`), de los que se cuelgan la UI, el audio y la FSM.
  - Invulnerabilidad **por tiempo**, no por bandera: no puede quedarse encendida.
  - `Golpe.IgnoraInvulnerabilidad` para lava, vacío y ahogamiento, que matan siempre.
  - El reloj es inyectable, así que las pruebas comprueban los i-frames sin esperar.

### 5. El input estaba incrustado en la lógica de movimiento

- **Problema.** La v1 leía `Input.GetKey("space")` dentro del `Update()` del jugador. Además, ese
  código ni siquiera arrancaría en este proyecto, donde el Input System es el único sistema activo.
- **Causa.** No había separación entre «qué quiere hacer el jugador» y «cómo se lee el teclado».
- **Solución.** `IFuenteInput` (patrón Adapter) con `FuenteInputJugador` sobre el Input System,
  usando las acciones que ya trae el proyecto: Move, Jump, Attack, **Sprint como dash**, Crouch e
  Interact. Incluye el *jump buffer* y un interruptor `Bloqueado` para la pausa y las cinemáticas.
  Si falta el asset o una acción, avisa una vez y devuelve input neutro.

### 6. Tags, capas y literales repetidos

- **Problema.** Los tags y las capas aparecían como literales por todo el código (`"Weapon"`,
  `"suelo"`, `"Player"`), y el proyecto v2 no tenía ninguno creado.
- **Causa.** No había un sitio único donde declararlos.
- **Solución.** `GameTags` y `Capas` como constantes y máscaras, `AnimatorParams` con los hashes
  del Animator, y el menú *Fragmentum → Configurar proyecto*, que crea los tags y las capas y
  aplica la matriz de colisión del Anexo A. Es idempotente.

### 7. Nada verificaba que el código funcionara

- **Problema.** No había forma de comprobar un cambio salvo abrir el editor y jugar.
- **Causa.** Todo el código vivía en `Assembly-CSharp`, que no puede referenciarse desde un
  ensamblado de pruebas.
- **Solución.** Tres ensamblados (`Fragmentum.Runtime`, `Fragmentum.Editor` y
  `Fragmentum.Tests.EditMode`) y **27 pruebas EditMode** sobre el núcleo: orden de `OnExit` y
  `OnEnter`, rechazo de transiciones no declaradas, encadenado de transiciones en un frame,
  expansión de superestados, generación del Mermaid, y vida, i-frames, curación y muerte.

### Además

- `AnimatorParams` comprueba que el parámetro exista antes de escribirlo y avisa **una sola vez**.
  En la v1, el jefe generaba tres avisos por frame porque su Animator no tenía `Attack`, `Damage`
  ni `Death`.
- `Object.GetInstanceID()` está obsoleto en Unity 6000.6, así que no se usa.

### Verificación

- Compilación de los tres ensamblados con `dotnet build` contra las DLL de Unity 6000.6.0f1: **0 errores**.
- Compilación real en Unity en modo batch: **0 errores**.
- Pruebas EditMode en modo batch: **27 de 27 correctas**.
- Configurador ejecutado: 10 tags, 6 capas y 8 parejas de colisión desactivadas.

### Pasos manuales en el Editor

Ninguno. La configuración del proyecto ya se aplicó en modo batch. Al abrir Unity puede aparecer
un cambio propio del editor en `ProjectSettings/ProjectSettings.asset`: la constante
`SENTIS_ANALYTICS_ENABLED`, que el paquete de AI de Unity añade y quita solo.
