# CHANGELOG — Fragmentum: Ecos de la Identidad (v2)

> Registro de la refactorización, en formato **problema → causa → solución**, pensado para el
> capítulo de desarrollo de la tesis. Una entrada por fase.
>
> - **v1**: proyecto original `Tesis` (Unity 6000.2.14f1), congelado tras la auditoría de la Fase 0.
>   Desde la Fase 5 está actualizado a 6000.6.3f1 para poder abrirlo en el PC nuevo; su código no cambia.
> - **v2**: este proyecto `Tesis2.0` (Unity 6000.6.3f1 desde la Fase 3), donde se construye la versión refactorizada.

---

## La portada del menú (2026-10-14)

Misma rama `nivel3-recorrido`. El autor pidió poner la portada que había generado (PENDIENTES N41).

- **Problema.** El menú principal y el selector llevaban de fondo, provisionalmente, el plano 7
  de la cinemática inicial. La portada nueva llegó como un JPG de 2752 × 1536, sin una rejilla
  limpia de pixel art.
- **Causa.** El generador dibuja un número redondo de píxeles de arte a lo ancho del lienzo y
  luego lo reescala. Además, esta imagen mezcla bloques de varios tamaños: el suelo va en bloques
  de unos 7 px y Kael, en bloques más finos.
- **Solución.**
  - **`limpiar_portada.py`** (en `Tesis/Imagenes/limpios/script`) la baja a la rejilla de 480 a
    lo ancho, con el motor de las cinemáticas de salida (periodo y fase reales). Es la de las
    cinemáticas de la Doncella y la única que conserva el detalle de Kael. Salen 268 filas, y se
    repiten la de arriba (cielo liso) y la de abajo para que quede en **480 × 270**: 16:9 exacto,
    que en 1920 × 1080 se ve a ×4 sin interpolar.
  - **`traer_arte_puertas.py`** la copia al proyecto como `Assets/Sprites/Interfaz/FondoMenu.png`,
    la ruta que el generador del menú ya buscaba (`GeneradorProgresion.RutaFondoMenu`).
  - **`TablaArte`** tiene una regla para ella: pixel art (filtro Point, sin compresión) y su
    lienzo de 480 × 270.
  - Las escenas del menú y del selector se volvieron a montar. Comparadas con las de antes, solo
    cambian la imagen y su proporción.
  - **El verificador del Nivel 2** revisaba toda la carpeta `Interfaz`, y contaba la portada como
    arte del Mundo 2 (118 sprites en vez de 117). Ahora mira solo `Interfaz/Oxigeno`, la burbuja
    del aire.

### Pruebas

- **Nueva:** `DatosProgresionTests.ElMenuYElSelector_LlevanLaPortada_EnPixelArt` comprueba que la
  portada mide 480 × 270, que se importa en pixel art y que el menú y el selector la llevan con
  su proporción.
- **La pasada completa queda para la siguiente vez que Unity esté cerrado.** La de esta entrada
  dio 534 de 535 en EditMode (la que fallaba era el recuento del verificador del Nivel 2, ya
  corregido) y se cortó en PlayMode, al volver a abrir el Editor. El código compila sin errores ni
  avisos.

---

## Mundo 3, fase B: la oscuridad, las antorchas, los cepos y las ramas que ceden (2026-10-14)

Misma rama `nivel3-recorrido`, sobre la fase A. El autor pidió seguir con la
fase B. Es la fase de las mecánicas del Miedo, sin arte: rectángulos de color y luces 2D.

### 1. La oscuridad y la luz de Kael

- **Problema.** El `L3_Miedo` decía desde la Fase 5 que el nivel es oscuro y que Kael lleva una luz
  de 3 u, pero nada lo aplicaba: el Nivel 3 se jugaba con luz.
- **Causa.** Faltaban las luces: el Anexo A pide luces 2D de URP, y el proyecto ya usa el
  renderizador 2D, pero ninguna escena las cambiaba.
- **Solución.**
  - **`LuzDelNivel`** lleva la luz global de la escena y la cambia según el tramo en que esté
    Kael, poco a poco: la linde con luna, el bosque cerrado a oscuras, el claro y la antesala con
    luna otra vez y la arena con la oscuridad total. Los valores están en `DisenoNivel3.Luces`.
  - **`LuzJugador`**: una luz 2D hija de Kael, apagada en el prefab.
  - **El `LevelManager` aplica las reglas del nivel**: si `reglas.oscuridad`, activa la
    `LuzDelNivel` de la escena y enciende la luz de Kael con `reglas.radioLuzJugador`. En el
    Editor la escena se sigue viendo con luz, y fuera del Miedo nada cambia.
  - La luz de Kael no se registra en `Iluminacion`: con ella ve dónde pisa, pero los Espectros no
    la respetan.

### 2. Las antorchas y la tecla E

- **Problema.** La antorcha es la decisión central del mundo: alumbra 8 u durante 15 s, enseña las
  trampas, despierta a lo que duerme y se enciende una vez por vida. Interactuar (E) existía en el
  input, pero nada la usaba.
- **Causa.** Solo estaba `ZonaIluminada`, el círculo fijo de la Fase 4.
- **Solución.**
  - **`Antorcha`**, con tres estados (apagada, encendida, consumida). Mientras arde se registra en
    `Iluminacion`, desde la llama, y parpadea los 3 últimos segundos. Al encenderse lo anuncia
    (`Antorcha.AlEncenderse`), para que en la fase C despierten los Espectros de su radio. Vuelve a
    estar lista cuando Kael reaparece tras morir.
  - **`Interacciones` e `InteraccionJugador`**: lo que se puede usar se registra, y Kael usa lo
    que tiene más cerca al pulsar E. La tecla se lee en un solo sitio.
  - **El aviso del HUD** (`AvisoInteraccion`): «E · Encender» sobre la antorcha que Kael tiene al
    alcance. Es el tutorial de la primera antorcha.

### 3. El cepo y el ruido

- **Problema.** Las trampas del Anexo A son «invisibles hasta 1 u». La propuesta las hizo cepos que
  sujetan y suenan, y pidió que el jugador vea cuándo hace ruido.
- **Causa.** No existían ni el cepo ni una forma de hacer ruido que no fuera correr.
- **Solución.**
  - **`Cepo`**: se ve a 1 u de Kael o bajo una antorcha, y se funde al aparecer. Al pisarlo:
    - Quita 1 sin retroceso: lo sujeta, no lo empuja.
    - Lo sujeta 1 s: no anda, no salta y no sale con el dash. Lo hace con un modificador del motor
      y sujetando su posición, sin tocar `PlayerController`.
    - Suena a 8 u.
    - Se queda cerrado y a la vista hasta que Kael reaparece.
  - **`PlayerModoMovimiento.HacerRuido`**: el chasquido sale por el mismo canal que la carrera, así
    que los Centinelas Ciegos de la Fase 4 ya lo oyen, sin cambiar `EnemyController`.
  - **`AnillosDeRuido`**: cada ruido dibuja a los pies de Kael un anillo pálido que crece hasta
    donde se oye (6 u corriendo, 8 u el cepo) y se desvanece. Son líneas sin imagen que no reaccionan
    a la luz: se ven a oscuras.

### 4. Las ramas podridas y las cintas

- **Problema.** En la fase A las ramas podridas eran ramas de otro color, que se veían a oscuras y
  no cedían.
- **Causa.** Eran bloques del recorrido.
- **Solución.**
  - **El prefab `RamaPodrida`**: la `PlataformaInestable` del Nivel 1 con los tiempos del Miedo
    (cruje 0,3 s y vuelve a los 4 s) y el componente `RamaPodrida`. A oscuras se ve como las sanas;
    bajo una antorcha, gris; y tiembla mientras cruje.
  - **Las cuatro cintas blancas** de la ruta buena cuelgan de las ramas sanas de las copas y solo
    se ven con luz. La antorcha de la bifurcación alcanza la primera.
  - `DisenoNivel3.Version` pasa a 3: la escena se vuelve a montar con todo esto.
  - Los prefabs de la antorcha, el cepo y la rama podrida se crean con los de las otras mecánicas
    (`GeneradorMecanicas`), con un rectángulo provisional que avisa de que falta su arte.

### Pruebas

- **Nuevas:**
  - `MecanicasMiedoTests` (9): los tres prefabs con las medidas del diseño; la luz, la
    interacción y los anillos de Kael; el parpadeo; cuándo se ve un cepo; qué luz toca en cada
    tramo; y qué tiene Kael al alcance.
  - `MecanicasMiedoPlayModeTests` (7):
    - La antorcha: solo al alcance, su tiempo, su parpadeo, una vez por vida.
    - El cepo: se ve de cerca o con luz; sujeta, quita y suena; lo oye un Centinela que no oye
      caminar; se rearma al reaparecer.
    - La rama podrida: cruje, se parte, vuelve y se delata con luz.
    - Los anillos y la luz de Kael.
- **Ampliadas:**
  - `DisenoNivel3Tests` (+3): la luz de cada tramo, las cintas y las ramas podridas.
  - `EscenaNivel3Tests` (+3): la oscuridad, las antorchas y los cepos, las ramas podridas y las
    cintas, en la escena.
  - `Nivel3PlayModeTests` (+3): la oscuridad cambia por tramos y Kael lleva su luz; la primera
    antorcha, con su aviso, enseña el primer cepo; el Nivel 1 sigue con luz.
- **534 EditMode y 220 PlayMode en verde.**

---

## Mundo 3, fase A: el recorrido del Nivel 3 (2026-10-14)

Rama `nivel3-recorrido`, que sale de `nivel2-recorrido` (antes, los commits de la v0.45 y de la
propuesta). El autor dio por buenos los dos niveles alargados con su puntaje (N42) y pidió empezar
el Mundo 3 como el 2: primero el mundo, sus enemigos (el básico, de la familia de los Ecos) y su
jefe, con sus prompts. Aprobó la propuesta con sus nueve decisiones («haz lo que recomiendas»).
Esta fase monta el recorrido en bloques de color, todavía con luz.

### 1. El diseño del Mundo 3

- **Problema.** El Nivel 3 solo existía como dato (`L3_Miedo`, de 0 a 70 u, sin escena). Del
  Miedo ya estaban programados desde la Fase 4 el sigilo (`PlayerModoMovimiento`), la detección
  en la oscuridad y por ruido y la Invisibilidad Parcial, pero no había un nivel donde usarlos ni
  un diseño que dijera cómo enseñan la emoción.
- **Causa.** El Anexo A da las piezas (antorchas que despiertan a los Espectros, Centinelas Ciegos
  que oyen, un laberinto de dos rutas, trampas ocultas, un jefe invisible), no su recorrido.
- **Solución.** Un diseño en tres tiempos que sale de la línea del guion, «ocultarse sin
  perderse» (`Docs/Diseno/Nivel3_Diseno.md`): en la linde la luz enseña las trampas pero despierta
  lo que duerme; en el bosque cerrado, a oscuras, esconderse funciona pero no basta (el camino
  fácil acaba en un callejón); y en el claro y la arena hay que enfrentar al miedo (hacer ruido a
  propósito ante el Acechador, acercarse al Espectro del Pavor cuando avisa). El Espectro es el Eco
  de este mundo (el verde que se deshace en polvo de `EnemigosNiveles.jpeg`). Las decisiones
  aprobadas: antorchas de un uso por vida, Espectros dormidos que despierta la luz o el roce, cepos
  que sujetan y suenan, la ruta falsa en un callejón, un mini-jefe nuevo (el Acechador), el
  Centinela Ciego con máscara de gas, el jefe de `jefe3.jpeg`, el castigo de golpear a una sombra
  y un nivel de unos 145 u con tres checkpoints y 15 min de tiempo con bono. Los prompts de todas
  sus imágenes están en `Docs/Diseno/Prompts_Mundo3.md`, en dos tandas.

### 2. El recorrido como datos y su montaje

- **Problema.** Había que llevar el diseño a una escena que crezca por fases, como la del Nivel 2.
- **Causa.** Es el mismo caso que el Nivel 2.
- **Solución.**
  - **`DisenoNivel3`**: suelos, ramas sanas y podridas, paredes, las ramas de la arena, antorchas,
    cepos, enemigos, checkpoints, la arena y los saltos del recorrido. Su `Version` hace que
    `PreparadorNivel` (con `PreparadorNivelMiedo.cs`) lo vuelva a montar entero cuando cambia.
  - **La escena `Nivel3`** se crea desde la plantilla URP 2D al regenerar, se asigna a su
    `LevelData` con sus cuatro secciones y sus tres checkpoints y entra en Build Settings: el
    tercer mundo ya se juega desde el selector. El que queda «en construcción» es el cuarto.
  - **Las raíces del callejón no agarran** (`SinAgarre`, como el hielo liso del Mundo 2): sin eso,
    el salto de pared sacaba de la trampa.
  - **Los Espectros y los Centinelas Ciegos** de la Fase 4 están en su sitio, con su ruta. El
    Acechador llega en la fase C; las antorchas, los cepos y la oscuridad, en la B.
  - **El tiempo con bono del Nivel 3 es de 15 min** (`TablaPuntaje`): aquí se camina mucho.

### 3. Cruzarlo todo caminando

- **Problema.** En el Miedo correr hace ruido. Si algún salto obligara a correr, el sigilo sería
  una trampa y no una decisión.
- **Causa.** Caminando (Alt), Kael va a 2,1 u/s: un salto le avanza 1,5 u y uno doble, 2,5. El
  esquema de la propuesta tenía huecos de 1,5 u y un barranco con un escalón que quedaba bajo una
  rama: Kael daba con la cabeza al saltar.
- **Solución.**
  - **Todas las ramas van a 1 u unas de otras**, y `DisenoNivel3Tests` comprueba cada salto con la
    velocidad de caminar (con los márgenes de siempre, el 85 % del alcance y el 90 % de la altura).
  - **El barranco mide 2,5 u de hondo y no tiene escalón:** bajo sus ramas quedan 2 u y se sale
    con el doble salto junto a cualquiera de sus paredes, donde no hay rama encima. Las pruebas
    comprueban que bajo cada rama por la que se pasa caben Kael y 0,2 u más.
  - **El callejón es de verdad un callejón:** desde su fondo, nada de encima queda al alcance del
    doble salto, y sus raíces no agarran. Se vuelve por un escalón.
  - **La primera rama de las copas está a 1 u del borde**, no encima: encima le cortaba el paso a
    Kael y escondía la bajada a la hondonada.

### Pruebas

- **Nuevas:** `DisenoNivel3Tests` (13: las medidas de caminar, que coinciden con `FuenteInputJugador`
  y con el umbral del sigilo; cada salto caminando; que del inicio se llega a la arena sin pasar
  por la trampa; el callejón; el hueco bajo las ramas; las ramas podridas sobre la trampa; la
  arena; las secciones; los recuentos del Anexo A; los checkpoints; las antorchas y los cepos),
  `EscenaNivel3Tests` (9: la escena sigue al diseño) y `Nivel3PlayModeTests` (3: correr hace ruido
  y caminar no, la trinchera guarda la partida, las raíces no agarran y el puesto del vigía se
  sostiene).
- **Cambiadas:** `ProgresionPlayModeTests` y `DatosProgresionTests`, porque el tercer mundo ya
  tiene escena.
- **519 EditMode y 210 PlayMode en verde.**

---

## Puntaje, menú con letra e imagen, niveles más largos y las puertas de cada mundo (2026-10-13)

El autor dio por buenos el Nivel 2 tal como quedó (N39) y las decisiones de diseño de las
puertas, que ya generó (N40), y pidió tres cosas: cambiar las monedas por **puntaje** (los
enemigos valen distinto, los mini-jefes más, y tardar menos da más), poner en el menú **los
nombres de quienes hicieron el juego**, una **letra más llamativa** (consejo del director) y una
**imagen de fondo**, y **alargar los dos niveles** antes de empezar el tercero: bien jugados, se
pasaban en diez minutos. Eligió Cinzel y Alegreya, una portada nueva (mientras llega, el plano 7
de la cinemática inicial), quitar del todo el código de las monedas y los nombres completos.

### Puntaje en lugar de monedas

- **Problema:** las monedas de la v1 no medían nada del juego. No había monedas en ningún nivel:
  el HUD enseñaba «Monedas: 0» para siempre.
- **Causa:** venían de la v1 (`Moneda`, `EconomyManager`, `ContadorMonedas` y la migración de los
  PlayerPrefs), donde tampoco se usaban.
- **Solución:** fuera las monedas (en `GameManager.cs` solo cambian las seis líneas que las
  nombraban; el autor lo aprobó) y un marcador por nivel:
  - `Levels/MarcadorNivel`, junto al `LevelManager`, que lo crea si la escena no lo trae. Suma lo
    que vale cada enemigo al morir (escucha su `Health`: `EnemyController` no sabe que existe el
    puntaje) y lo que vale el jefe cuando el `LevelManager` lo da por caído. Cada enemigo cuenta
    una vez por intento. El reloj corre solo jugando: ni en pausa, ni en cinemáticas, ni en el
    Game Over, y morir no lo para.
  - Al activar un checkpoint guarda una foto del intento (`DatosIntento`: puntos, reloj y a quién
    se derrotó). Si se sale y se continúa desde él, el intento sigue donde iba: salir no pone el
    reloj a cero ni deja cobrar dos veces al mismo enemigo. Reiniciar el nivel es otro intento.
  - Al cruzar la salida, `Cerrar` suma el **bono por tiempo** (`Core/Puntuacion/ReglasPuntaje`: 5
    puntos por cada segundo que sobre de `LevelData.tiempoConBono`, 10 min en el Nivel 1 y 13 en
    el 2; pasado, no hay bono, pero tampoco se resta) y anota el mejor de cada nivel en la partida
    (`DatosGuardado.puntajes`; a igualdad de puntos, el más rápido).
  - `Editor/TablaPuntaje` pone los valores en los assets en cada regeneración: un enemigo común
    vale 100, la Escultura 250, el Golem 500 y la Plañidera 600 (los mini-jefes); la Centinela
    1500, la Doncella 2000 y cada jefe siguiente más. Correr y pelear compiten: cruzar el Nivel 1
    en la mitad de su tiempo da un bono del orden de su jefe.
  - Lo enseñan el HUD (puntaje, reloj y, un momento, lo que se acaba de ganar: «+100»), el fin de
    nivel (el desglose y si es récord), el selector (el mejor puntaje y tiempo de cada mundo) y el
    menú (la suma de todos).
  - El guardado ya no migra nada de la v1: las partidas con monedas se cargan igual, sin ellas.

### El menú y la letra

- **Problema:** el director pidió una letra más llamativa; el menú era un color plano con seis
  rectángulos de color y no decía quién hizo el juego.
- **Causa:** toda la interfaz usaba la fuente integrada de Unity (`LegacyRuntime.ttf`).
- **Solución:** dos letras de Google Fonts con licencia OFL, que va con ellas en
  `Resources/Fuentes`: **Cinzel** (capitales talladas) para títulos, menús, botones, nombres y el
  HUD (`FabricaUI.FuenteTitulos`, `FabricaUI.Titulo`), y **Alegreya** (serifa de lectura con
  cursiva propia) para los diálogos y el texto corrido (`FabricaUI.Fuente`). La línea de los
  diálogos sube de 26 a 30 px: Alegreya tiene el ojo más pequeño que la integrada. El menú lleva
  de fondo una imagen del juego (`UI/FondoMenu`: la imagen y un velo en la escena, y la viñeta y
  un degradado oscuro a la izquierda, que se generan al arrancar), el título y las opciones a la
  izquierda, y al pie «Creado por David Felipe Garzón Barbosa · Juan Diego Romero Jiménez»
  (`MainMenu.Autores`). La imagen es la portada (`Assets/Sprites/Interfaz/FondoMenu.png`) cuando
  exista; mientras, el plano 7 de la cinemática inicial (Kael ante los cinco fragmentos). El
  selector lleva la misma imagen, más velada.

### Los niveles, más largos

- **Problema:** bien jugados, los dos niveles se pasaban en diez minutos: el puntaje por tiempo
  no tenía recorrido.
- **Solución:** una sección nueva en cada uno, entre lo que había y la arena, que es el examen de
  lo aprendido:
  - **Nivel 1, «la forja» (x 55 a 100, +45 u; el nivel mide 126 u):** una inestable sobre lava
    para subir a la almena, dos Ecos bajo el fuego de una segunda torreta, un lago de 12 u con una
    inestable entre dos de combustión (sobre la inestable no se puede esperar: se mira la segunda
    desde la primera), una cornisa con otro Eco y la antesala. Dos checkpoints más: tras el Golem
    (x 57,5) y ante la arena (x 94,5), así que morir ante la Centinela ya no obliga a repetir el
    Golem. `DisenoNivel1.Version` (2) hace que `PreparadorNivel` vuelva a montar la escena entera,
    como ya hacía con el Nivel 2.
  - **Nivel 2, «el canal» (x 74 a 104, +30 u; mide 139 u):** la azotea al salir de la torre, con
    un Lamento; un canal de 12 u con tres placas que suben 2 u a la contra (hay que medir el
    momento; las primeras se cruzaban sin esperar) y dos Medusas debajo para quien se caiga; y un
    patio de hielo con las dos Esculturas del Anexo entre dos estatuas de piedra y otro Lamento: lo
    que enseñó la primera estatua (son decorado) aquí ya no vale. `DisenoNivel2.Version` 13.
  - Los tramos y los checkpoints de los dos `LevelData` siguen siempre al diseño
    (`GeneradorProgresion.SeguirElDiseno`, que ahora rehace la lista entera si cambia).
- **Problema encontrado al montarlos:** al volver a montar el Nivel 1, el pintor no ponía ni la
  estatua ni los braseros. **Causa:** reserva el sitio de todo lo que tiene dibujo o zona de golpe,
  y lo recién colocado (los enemigos y las mecánicas se instancian en el origen y después se
  mueven) aún tenía su caja de física en el origen: en edición, Unity no la mueve hasta
  sincronizar. Esas cajas ocupaban el inicio del nivel, donde va la estatua, y sin estatua no se
  pone lo demás. **Solución:** `PintorEscenario` sincroniza la física antes de mirar
  (`Physics2D.SyncTransforms`) y no cuenta ni lo apagado ni el fondo con parallax.

### Las puertas de cada mundo

- **Problema:** la salida de cada nivel era un rectángulo translúcido que aparecía de golpe, la
  puerta de la arena un bloque de color, el checkpoint un mástil con un paño, y el cierre de cada
  mundo, tres frases sobre negro.
- **Solución:** el arte de la tanda N40, limpiado con `limpiar_puertas.py` (motor de
  `limpiar_mundo2.py`) y traído con `Herramientas/traer_arte_puertas.py`, se monta con
  `Editor/ConstructorPuertas`:
  - **La puerta de salida** (`Levels/PuertaDeSalida`, unas 4 u de alto): se ve cerrada desde que
    se entra en la arena y, al recoger el fragmento, se entreabre y se abre, alternando sus dos
    dibujos de abierta. `SalidaNivel` le dice cuándo.
  - **La verja** que cierra la arena tras Kael: la de la Ira se repite hasta el techo; la de la
    Tristeza va abajo, con un muro de hielo encima. La colisión sigue siendo la del bloque.
  - **El checkpoint:** el brasero (Ira) y el farol (Tristeza), apagados y encendidos; cada estado
    con el pivote en su pie, para que no salte al encenderse.
  - **La cinemática de salida** de cada mundo: el plano general con las dos primeras frases del
    cierre y Kael ante la puerta con la tercera, en la rejilla de 480 × 268 (14 s, se puede
    saltar). Cada frase va sobre una zona oscura de su plano: en el general de la Tristeza, todo
    hielo blanco, va sobre la losa que lleva a la puerta.
- La carpeta `limpios` quedó dentro de `Tesis/Imagenes/Mundo1` al reorganizar las imágenes: los
  scripts la buscan ahí y en `Tesis/Imagenes`.

### Pruebas

Nuevas: `PuntajeTests` (bono, reloj, récord, tabla y letras), `PuertasTests` (arte importado,
salidas, checkpoints, verjas y cinemáticas de salida), `PuntajePlayModeTests` (sumar en el
Nivel 1, reloj y pausa, fin de nivel, récord, continuar desde un checkpoint, reiniciar y el menú)
y `PuertasPlayModeTests` (el checkpoint se enciende, la puerta se abre al recoger el fragmento).
Puestas al día las del diseño y las escenas de los dos niveles, el guardado y las cinemáticas de
cierre. 497 EditMode y 207 PlayMode en verde.

---

## Mundo 2: lo que salió de jugarlo (2026-10-12)

Misma rama, `nivel2-recorrido`. El autor jugó el Nivel 2 de principio a fin (PENDIENTES N31). Dio
por buenos el hielo, el agua, las placas, el golpe de la Escultura, la torre, la Doncella con sus
dos fases, los Lamentos (N37) y la cinemática (N38); pidió seis cambios y dejó a mi criterio el
resto del tileset (N36). `EnemyController`, `PlayerController`, `GameManager` y `BossController`
no se tocan.

### 1. El aire sobraba

- **Problema.** Con 15 s de aire, la calle hundida y la torre se cruzaban sin mirar la barra.
- **Causa.** Cada tramo sin aire se hace en unos 8 s: los 15 de la decisión del 2026-10-08
  dejaban 7 de margen.
- **Solución.** 10 s (`OxigenoJugador`), y se sigue llenando en 1 s al respirar. Quedan 2 s para
  esperar a una Medusa. Lo que falte se paga, como antes, con un corazón cada 2 s, que ahora
  devuelve el checkpoint de la campana (punto 6).

### 2. El tapón solo lo entendía quien lo había programado

- **Problema.** Nada decía que el tapón de hielo se rompe con el Ataque Cargado.
- **Causa.** La pista era la grieta naranja, el color de la Ira. Sugiere, pero no dice, y un
  golpe normal lo agrieta sin romperlo: parece que basta con insistir.
- **Solución.** Una línea nueva de Kael: «Este hielo no cede a un golpe cualquiera. Hace falta el
  Ataque Cargado: contener el golpe… y soltarlo.» Retoma el cierre del Mundo 1 («contener un
  golpe… y soltarlo cuando decida») y nombra la habilidad, para que se sepa qué usar.
  - Suena al llegar ante el tapón (las 4 u de plaza de delante) y, si no se ha oído, al agrietarlo
    con un golpe normal, que es cuando el jugador acaba de ver que no basta. No se repite
    seguida y calla con el tapón roto. Es `ZonaDeDialogo`, una pieza nueva.
  - Es un momento nuevo de los diálogos (`MOMENTOS_DIALOGO.PISTA`), y su texto vive en
    `GuionMundos`, como los demás. El generador de narrativa ahora añade a un asset que ya
    existía los momentos que le falten, sin tocar lo escrito a mano.

### 3. La Plañidera no tenía sitio

- **Problema.** El mini-jefe peleaba en 6 u de hielo, entre el puente y el tapón.
- **Causa.** Ocupó el hueco de la segunda Escultura, pensado para un enemigo que no anda.
- **Solución.** La plaza este mide 16 u. Ella flota en el centro (x 38, de 36 a 40), con 6 u
  libres hasta el puente y otras 6 hasta el tapón; el tercer Lamento sigue junto al tapón. Todo
  lo que hay del tapón en adelante se corrió 10 u, y de la campana en adelante, 13 (punto 7): el
  nivel mide 115 u y no 102. `DisenoNivel2.Version` sube a 12 y la escena se vuelve a montar.
  - **La cámara.** Lo que enseña sale de las secciones del `LevelData`, que eran números fijos
    del generador: se habría quedado a 13 u del fondo de la arena. Ahora las secciones y los
    checkpoints del Nivel 2 siguen a su diseño (`GeneradorProgresion.SeguirElDiseno`), y la torre
    del reloj del fondo, también.

### 4. Las esquirlas no se veían venir

- **Solución.** Vez y media más grandes: se importan a 104 px por unidad, no a 156, sin tocar
  los PNG. La piedra mide 0,4 × 0,2 u y su zona de golpe crece con ella (radio 0,18 u).

### 5. La muralla se escalaba

- **Problema.** Con el salto de pared se subía la muralla que cierra la plaza, se andaba por el
  techo helado y por el tejado de la torre, y se caía en la antesala: medio nivel saltado, con
  todo lo de debajo del agua.
- **Causa.** El diseño contaba con que «no se salta» (9 u), y la prueba medía eso. Pero Kael
  trepa una pared sola encadenando saltos de pared: la altura no lo para.
- **Solución.** La muralla es de hielo liso y no agarra. `SinAgarre` es una marca que se pone en
  la pared; `PlayerSensors`, al buscar una pared a la que agarrarse, se salta las que la llevan.
  Sigue siendo sólida. Se dibuja con el bloque de hielo, como el techo helado, para que la regla
  se vea: lo que es de hielo no agarra. Las demás paredes no cambian.
  - Es el único cambio en el código de Kael: unas líneas en sus sentidos (`PlayerSensors`), no
    en `PlayerController`.

### 6. Los checkpoints curan

- **Solución.** Cada vez que Kael toca un checkpoint recupera toda la vida, también si ya estaba
  activo (`Checkpoint.cura`, con su evento `OnCuro`). Viene activado por defecto, así que vale
  para los checkpoints ya montados del Nivel 1 y del Nivel 2 sin tocar sus escenas, y para los
  que vengan.

### 7. El resto del tileset

- **Problema.** De las dos hojas solo estaban colocados el ladrillo, el hielo, la viga, la silla
  y la campana (N36).
- **Solución.** Nueve piezas más, cortadas de las hojas sin tocarlas y colocadas desde
  `DisenoNivel2.Decorados`:
  - **la estatua** en el inicio, antes que la Escultura Viviente, y otra en la plaza este: la
    primera enseña que las estatuas son decorado y la Escultura lo desmiente;
  - **los faroles** en los tres sitios donde se puede parar: el inicio, el puente y la antesala;
  - **rocas y una columna rota** en la plaza de la Plañidera, que queda como una plaza en ruinas;
  - **la mesa**, junto a la silla vacía del eco de memoria. Para que quepan la silla, la mesa y
    la bandera, la campana es 3 u más ancha y su repisa mide 4 u, con un hueco de 2 u a cada lado
    (antes, el del este medía 1);
  - **algas, la campana caída, otra columna y rocas** en la calle hundida;
  - **dos cadenas** junto a la campana del campanario, y **los estandartes** sobre el frente del
    puente y bajo el balcón de la arena.
- **Sin usar:** los remates de nieve con carámbanos, el charco helado, la repisa pequeña, los
  postes, las escuadras, los dos puentes de madera, la ventana y los demás trozos de muro.

### 8. Los prompts que pidió

- `Docs/Diseno/Prompts_Puertas.md`: la puerta de salida de cada mundo (cerrada, abriéndose y
  abierta), las dos cinemáticas de Kael cruzándola y, opcionales, la verja de la arena y la
  bandera del checkpoint.
- Al final de `Docs/Diseno/Prompts_Gemini.md`: lo que le falta a la Centinela, que son la caída
  del salto (tres imágenes, editando la onda) y `R2`.

### Pruebas

470 EditMode y 196 PlayMode en verde. Nuevas o cambiadas:

- `DisenoNivel2Tests`: la plaza este da 14 u o más y deja 4 a cada lado de la Plañidera; la
  muralla es la única pared que no agarra y no se pasa ni subido al tapón; la pista está delante
  del tapón y fuera de la ronda de la Plañidera; las secciones llevan la cámara de pared a pared;
  en la campana caben la silla, la mesa y la bandera; y cada pieza de atrezo se apoya en un
  suelo o cuelga de un bloque en el que cabe.
- `EscenaNivel2Tests`: las secciones del asset, la muralla con su marca, la zona de la pista
  enlazada al tapón y los checkpoints que curan.
- `ArteMundo2Tests`: las quince piezas cortadas a su tamaño y con su pivote, el hielo de la
  muralla de arriba abajo y cada pieza de atrezo en su sitio.
- `DatosNarrativaTests`: la pista del guion está en los diálogos de la Tristeza y nombra la
  habilidad del Mundo 1.
- `JugadorPlayModeTests`: junto a una pared sin agarre, Kael ni se desliza ni salta desde ella;
  sin la marca, la misma pared agarra.
- `FlujoNivelPlayModeTests` y `Nivel2PlayModeTests`: los checkpoints curan en los dos niveles,
  también el que ya está activo; respirar no cura y la bandera sí; la pista suena al llegar y al
  agrietar, sin repetirse; y la muralla no se detecta como pared.

Una cosa que enseñaron las pruebas: las secciones del nivel son un `struct`, y al cambiarlas a
través de la lista se cambiaba una copia. Lo vio `EscenaNivel2Tests` (la cámara seguía acabando
en x 96).

---

## Mundo 2, fase F: las cinemáticas de la Doncella (2026-10-10)

Misma rama, `nivel2-recorrido`. Con el arte ya dentro (fase E), quedaban la narrativa y el
sonido del mundo. Los textos del guion estaban cargados desde que se aprobó
(`Docs/Diseno/Guion.md`): la apertura, el eco de memoria de la silla vacía, la línea de la
Doncella y el cierre. Faltaban sus cinemáticas. `EnemyController`, `PlayerController`,
`GameManager` y `BossController` no se tocan.

### 1. Una rejilla por cinemática

- **Problema.** Los cuatro planos de la Doncella miden 480 × 268 (decisión N34) y la cámara de
  cinemáticas solo sabía dibujar en 240 × 135: se habría visto una esquina del plano.
- **Causa.** La rejilla eran dos constantes de `CamaraCinematica`, y de ellas colgaban el tamaño
  de la textura, el encuadre, el flash y el sitio de cada texto.
- **Solución.** Cada cinemática trae su rejilla (`CutsceneData.rejilla`, 240 × 135 por defecto) y
  la cámara la adopta al empezar (`FijarRejilla`): cambia la resolución de la Pixel Perfect
  Camera, lo que ve y el flash. Los textos se colocan en la rejilla de su cinemática, tanto los
  de la interfaz como los de fuente bitmap. En 1920 × 1080 los planos de 480 se amplían por 4,
  con 4 px de franja arriba y abajo; en 1280 × 720, por 2. Las cinemáticas del Nivel 1 no
  cambian: siguen a ×8. Las brasas siguen repartidas a lo ancho de 240: solo las usa la
  Centinela.

### 2. La presentación y la derrota

- **Problema.** Al entrar en su arena salía la presentación genérica (su nombre sobre el juego
  congelado), y al vencerla no se veía nada.
- **Causa.** Sus planos estaban limpios desde la fase C, pero sin montar. Y para ella no hay un
  guion plano a plano, como el de la Centinela.
- **Solución.** `ConstructorCinematicasNivel2` las monta, y «Regenerar todo» lo llama.
  - **Presentación** (`Cinematica_Jefe_Tristeza`), 9,8 s, una vez por partida: negro 0,3 s; la
    nave helada, con ella subiendo por la grieta (sube en 0,7 s, se queda 3,8 y se va en 0,3);
    negro 0,2 s; y su rostro (0,6 + 3,6 + 0,3). Lleva la estructura de la de la Centinela y casi
    su duración (las presentaciones de jefe no pasan de 10 s), sin brasas, temblores ni flash.
    Dice su línea del guion repartida entre los dos planos, sin cambiarle una letra: «Él cerró
    los ojos para no llorar, y la ciudad se hundió.» sobre la nave y «¿Vienes a cerrarlos tú
    también?» de cerca. Al final entra su rótulo, con el nombre del mundo debajo. La letra
    mide 10 px del plano: 40 en pantalla, como la de la Centinela.
  - **Derrota** (`Cinematica_Derrota_Tristeza`), cuando termina de derretirse: ella deshecha
    sobre el hielo y la ciudad fuera del agua, 4,5 s cada plano, sin texto, como la de la Ira.
    Después se vuelve al juego, con el fragmento en el charco.
  - Si falta algún plano no se toca nada: la presentación sigue siendo la genérica.
- **Para verlas sin jugar:** Fragmentum → Cinemáticas → Capturar las cinemáticas de la Doncella
  deja seis imágenes en `Docs/Cinematicas`.

### 3. Sin sonido

- En el proyecto no hay ningún audio, y no se ha inventado ninguno: ni música ni cues. Cuando lo
  haya, la música del nivel y la del jefe van en `L2_Tristeza` y la de cada cinemática, en su
  asset (PENDIENTES B8).

### Pruebas

460 EditMode y 190 PlayMode en verde. Nuevas: `CinematicaDoncellaTests` (los cuatro planos, la
presentación beat a beat, su línea contra el guion, la derrota, la rejilla de 480 en 1080 y en
720, la cámara que cambia de rejilla y vuelve, y el texto en su píxel), una en `ArteMundo2Tests`
(el verificador nota una cinemática fuera de su rejilla o sin un plano), una en
`CinematicaRejillaPlayModeTests` (con el reproductor de verdad, la cámara amplía por 4 los planos
de la Doncella y vuelve a ampliar por 8 los del Nivel 1) y una en `Nivel2PlayModeTests`, que
recorre el final del nivel: la presentación al entrar en la arena, la derrota cuando termina de
derretirse y el fragmento, que Kael recoge al pasar por debajo y le da el Dash Acuático. `VerificadorNivel2` revisa también los
cuatro planos y las dos cinemáticas.

Dos cosas que enseñaron las pruebas. La primera versión de la presentación duraba 13,6 s, y
`DatosNarrativaTests` exige que la de cada jefe no pase de 10: se acortó a 9,8 s dejando a su
línea el tiempo de leerla (no más de 17 letras por segundo). Y en modo batch no se dibuja ningún
fotograma, así que la Pixel Perfect Camera no recalcula su escala sola: la prueba le pide uno
sobre una textura de 1920 × 1080.

---

## Mundo 2, fase E: el arte de la Tristeza (2026-10-10)

Misma rama, `nivel2-recorrido`. Entran en el juego los 124 PNG limpios del Mundo 2
(`Tesis/Imagenes/limpios/Mundo2`): enemigos, jefe, efectos, mecánicas, la burbuja del aire, el
fondo y el tileset. Ningún PNG se reescala, se recorta ni se recentra: todo son reglas de
importación (píxeles por unidad y pivote) y rectángulos sobre las hojas. `EnemyController`,
`PlayerController`, `GameManager` y `BossController` no se tocan.

### 1. Traer el arte y darle sus reglas

- **Problema.** El arte limpio vivía fuera del proyecto, y cada pieza llegó a una escala distinta.
- **Solución.** `Herramientas/traer_arte_mundo2.py` copia los PNG byte a byte a sus carpetas
  (`--comprobar` dice si alguno cambió). `ArteMundo2.Reglas` reúne las reglas de cada constructor y
  se suma a `TablaArte`, así que el importador de siempre las aplica. «Regenerar todo» llama a
  `ArteMundo2.Enemigos`, `Jefe` y `Mecanicas`, y hay un menú propio: Fragmentum → Niveles → Generar
  el arte del Mundo 2 (Tristeza). Sin los PNG, nada falla: cada pieza se queda como estaba.

### 2. Los cuatro enemigos

- **El Lamento** (`ConstructorLamento`). Es el Eco de la Ira empapado, así que usa su cuerpo, su
  arma y su controlador. Sus doce fotogramas llegaron con lienzos y escalas distintos: cada uno
  lleva su regla, con el ancla de los pies donde la tiene ese mismo fotograma del Eco. Los cuatro
  del ataque no van a la escala que decía el LEEME (82, 79, 79 y 73 px por unidad): encajando cada
  silueta sobre la del Eco salen 81, 77, 76 y 70. Gotea agua, y al morir deja su charco.
- **La Escultura** (`ConstructorEscultura`). Duerme en `E1`, despierta en tres fotogramas y golpea.
  `A3`, `A4` y `A5` vienen dibujados mirando al otro lado que los demás: el clip del ataque lleva
  una curva que voltea el dibujo justo en esos tres. Un golpe la hace encogerse (`H1`) sin
  interrumpirla, y rota se queda en `M4`.
- **La Medusa** (`ConstructorMedusa`). Flota en bucle, se aparta de la espada (`A1`, mirando hacia
  donde la empujan) y se encoge mientras no quema (`A2`).
- **La Plañidera** (`ConstructorPlanidera`). Su `Estado` mueve el dibujo: alza, apunta, lanza, se
  queda desarmada y se rehace. El fotograma del barrido (`A2`) tiene otro lienzo: se ancla por el
  corazón, que mide lo mismo que en los demás, y no «un 23 % más fino» como decía el LEEME. Las
  esquirlas van a tres veces su densidad de píxel, que es como salen del tamaño de su mano.
- Las tres miran a la izquierda en el dibujo: el prefab las voltea y la geometría sigue en
  «mirando a la derecha».

### 3. Los Lamentos, al tamaño y al ritmo de los Ecos del Nivel 1

- **Problema.** El Lamento salía pequeño (1 u de alto junto a un Kael de 1,8) y golpeaba despacio:
  0,45 s de aviso y 2,2 s de descanso entre golpes.
- **Causa.** Es el Eco de este mundo, y le faltaban los dos ajustes que el autor hizo a los Ecos de
  la Ira tras probar el Nivel 1: los escaló a 1,8 × 1,5 (PENDIENTES N18) y aceleró su golpe.
- **Solución.** `DisenoNivel2.EscalaLamento` es `DisenoNivel1.EscalaEco`, y el Nivel 2 coloca a sus
  tres Lamentos a esa escala. `ConstructorLamento.VelocidadAtaque` es la del Eco (1,6×): el aviso
  dura 0,28 s, el zarpazo 0,09 s y el ataque entero 0,5 s; descansa 0,45 s, como el Eco. Sus datos
  llevan esos tiempos. Sigue siendo lento en lo demás: anda a 0,8 u/s, persigue a 1,4 y espera
  2,5 s en cada punto.

### 4. La Doncella

- **Sus clips** (`ConstructorDoncella`): flota (`F1`, `F2`, `F3`, `F2`; `F4` no se usa, rompe el
  vaivén), un gesto por ataque (ola, inundar y congelar, cada uno con su momento en el tercer
  fotograma), el rugido, el llanto (baja, llora arrodillada y se alza) y la muerte, que acaba en
  el charco con su fragmento.
- **El aviso es su gesto.** Cada patrón le pide el gesto (`DoncellaLamento.Gesto`) para que su
  momento llegue justo cuando acaba el aviso. Con arte ya no se tiñe de rojo para avisar: solo
  late en cian mientras llora.
- **La cola cortada (N35).** El lienzo corta su cola de agua en los 28 fotogramas. Un shader propio
  (`Fragmentum/SpriteFundidoAbajo`) funde a transparente las 22 filas de abajo, y `ColaFundida` lo
  apaga en los fotogramas que sí están enteros (arrodillada y el charco).
- **El golpe que no le hace nada** la vuelve una silueta de hielo un instante: con arte, el
  destello blanco de antes no se veía (el color de un sprite solo oscurece).
- **Problema: moría en el aire.** En la fase 2 llora sobre la capa de hielo, a 3,7 u. Al morir, la
  capa se deshace, y el charco y el fragmento se quedaban colgados donde lloraba.
- **Solución.** Con arte se derrite cayendo (`CaerAlHielo`, 0,9 s): el charco y el fragmento
  quedan en el hielo seco, donde Kael llega.

### 5. Las mecánicas y los efectos

- **El tapón de hielo:** intacto, agrietado y, al romperse, las esquirlas y los restos en el borde
  del agujero. **La placa flotante**, a 66 px por unidad (N33). **Una columna de burbujas** bajo
  cada bolsa de aire.
- **El agua.** La zona ya no es un velo liso: lleva su superficie dibujada, en bucle y repetida a
  lo ancho, sobre un cuerpo liso del color con el que acaba. La última fila del lienzo de la
  superficie viene vacía: sin descontarla quedaba una raya de fondo sin teñir entre los dos.
- **La ola** de la Doncella (`OlaDeAgua.prefab`): avisa, avanza y rompe.
- **Las franjas de la arena** (`ArteDeFranja` y `DibujoDeFranja`): las grietas del aviso, la
  superficie del agua, la escarcha, las púas que brotan, se quedan y se rompen, y las losas de la
  capa, que se forman, aguantan y avisan de que se derriten. La franja sigue mandando: el dibujo
  solo mira en qué estado está y cuánto lleva en él.

### 6. La burbuja del aire

- En el HUD, la barra del aire es ahora la burbuja (`ArteInterfaz`, en `Resources`): cinco
  fotogramas, de llena a punto de romperse. Al asfixiarse parpadea en rojo.

### 7. El fondo

- **Problema.** El Nivel 2 baja 11 u bajo el agua, y el fondo del Mundo 1 sigue a la cámara: el
  cielo se vería también en la calle hundida.
- **Solución.** `ConstructorFondoMundo2` monta dos grupos. El de la vista (cielo, ciudad lejana,
  torre y carámbanos) sigue a la cámara, como en el Mundo 1. El de debajo del agua (los rayos de
  luz, la ciudad hundida y las algas) está anclado al mundo: `ParallaxMovement` tiene ahora la
  opción de no seguir a la cámara en vertical. Las dos ciudades no casan por los bordes: se repiten
  en espejo.
- **Problema: una raya vertical** cruzaba el fondo cada pocos metros.
- **Causa.** Mi script de limpieza (`limpiar_mundo2.py`) dejaba transparentes la primera columna y
  la primera fila de las capas con alfa: el cierre morfológico erosiona el borde del lienzo.
- **Solución.** Las capas del fondo se cierran sobre el lienzo prolongado. Las cuatro capas con
  alfa se limpiaron otra vez: mismo tamaño, y solo cambian los bordes.

### 8. El tileset y los props

- **Problema.** Los bloques del nivel eran rectángulos de color.
- **Causa.** El tileset del Mundo 2 no es una rejilla de celdas, como el volcánico: son dos hojas
  de piezas sueltas, y el pintor del Nivel 1 no vale.
- **Solución.** `ConstructorEscenarioMundo2` corta las hojas en piezas (rectángulos, sin tocar los
  PNG) y viste cada bloque: **ladrillo** en la masa (32 × 32 px elegidos para repetirse sin
  costura; a 32 px por unidad casa de un bloque al siguiente), **vigas de madera nevadas** en las
  plataformas finas (las patas no se repiten) y **hielo** sobre los suelos que resbalan y en el
  techo helado de la calle. La colisión sigue siendo la del bloque. Y dos props que pide el
  diseño: **la silla vacía** junto al primer checkpoint y **la campana** del campanario. El resto
  de las hojas queda sin colocar (PENDIENTES N36).

### 9. Un verificador para el Nivel 2

- `VerificadorNivel2` (Fragmentum → Niveles → Verificar el arte del Nivel 2, y al final de
  «Regenerar todo») revisa con las comprobaciones del Nivel 1 los sprites y sus reglas, los
  clips y sus eventos, los Animators y los prefabs, y además lo que no es un prefab con Animator:
  el volteo del ataque de la Escultura, el arte de las franjas, la burbuja, la cola y el fragmento
  de la Doncella, la ola de sus patrones, el fondo y el tileset.

### Pruebas

450 EditMode y 188 PlayMode en verde. Nuevas: `ArteMundo2Tests` (el verificador y lo que
encuentra cuando algo se rompe a propósito, el Lamento ajustado como el Eco, las cuentas de las
franjas, de la burbuja y de los bucles, las piezas del tileset y la escena vestida) y tres en
`DoncellaPlayModeTests` (el destello de hielo, el dibujo que sigue al llanto y la caída al morir
sobre la capa). Cambian las que daban por hecho que no había arte: las de `EscenaNivel2Tests`
(el agua ya no es un velo y los prefabs son los del arte), la de la ola y la de la derrota sin
arte (ahora con el Espectro), la de la muerte de la Plañidera (tarda lo que su dibujo) y la que
comparaba el descanso del Lamento con el de la Ira. `DisenoNivel2.Version` sube a 11.

---

## Mundo 2, fase D: la Doncella del Lamento (2026-10-10)

Misma rama, `nivel2-recorrido`. El autor respondió a lo que quedaba abierto de las imágenes
(N30, N33, N34 y N35) y pidió empezar la fase D antes de probar el nivel. Todavía sin arte.
`EnemyController`, `PlayerController`, `GameManager` y `BossController` no se tocan.

### 1. La Doncella pelea

- **Problema.** La Doncella era un esqueleto: tenía vida y dos fases, pero ningún patrón.
  Esperaba quieta y se la vencía a golpes.
- **Causa.** Faltaba la fase D del diseño (`Docs/Diseno/Nivel2_Diseno.md`, sección 6).
- **Solución.**
  - **Flota.** `DoncellaLamento` vuelve cinemático su cuerpo: ni cae ni choca. Espera en el
    hielo, sube al empezar el combate y va de lado a lado con los pies a y 3. Sus patrones la
    mandan a un punto (`FlotarA`) y, al acabar, vuelve a flotar a su aire.
  - **Solo se la hiere mientras llora** (`inmuneFueraDeVulnerable`, que ya existía para el Juez
    Silente). Fuera de la ventana, los golpes no le hacen nada y destella en blanco; llorando late
    en cian y cada golpe vale el doble. Tocarla no hace daño.
  - **Cuatro patrones nuevos**, como datos: `PatronOla` (una ola a ras del hielo cruza la arena
    desde la pared que ella tiene a la espalda), `PatronInundacion` (avisa y sube el agua en la
    franja de Kael), `PatronCongelar` (el agua se pone blanca, brotan púas y queda una capa de
    hielo) y `PatronLlanto` (baja adonde le toca llorar y abre la ventana).
  - **Fase 1:** ola, inundación, ola y llanto en el hielo seco, a 2 u de Kael.
  - **Fase 2** (50 % de vida): grita, y la ronda es ola doble, inundación, congelar y llanto
    sobre la capa de hielo. Si no llegó a congelar, llora metida en su agua.
  - **Al caer o al reiniciarse el combate**, el agua se va y el hielo se deshace.

### 2. El agua que sigue ahí cuando ella llora

- **Problema.** Las zonas que crean los patrones de un jefe se borran cuando el jefe corta su
  ataque, y abrir la ventana vulnerable lo corta. El agua desaparecería justo al llorar.
- **Causa.** Esa limpieza está pensada para golpes y erupciones, que duran lo que el ataque.
- **Solución.** Cada franja del suelo es una `FranjaInundable` (en `Levels/Mecanicas`) que vive
  por su cuenta: seca, aviso, inundada, bajando, poniéndose blanca y congelada. El patrón solo
  la pone en marcha. Su agua es la del recorrido (`ZonaModificadorMovimiento`, que ahora puede
  cambiar de tipo por código) y sus púas, una `ZonaDeDanio`. La arena tiene tres: A (x 70 a 78),
  B (78 a 87) y C (87 a 95).

### 3. La capa de hielo, a una altura que se alcanza

- **Problema.** El diseño decía que el agua sube hasta y 4. A esa altura, la capa de hielo que
  deja al congelarse quedaba a 0,15 u del límite del doble salto de Kael (4,15 u).
- **Causa.** El diseño contaba con que el agua deja saltar más alto. Es verdad en la torre, que
  tiene agua de sobra, pero una inundación de 4 u de fondo solo da unas 0,08 u más que en seco:
  al cruzar la superficie, la gravedad vuelve a ser la de fuera.
- **Solución.** El agua sube hasta y 3,4 y la capa se pisa a y 3,5: se sube con el doble salto
  y con margen (con un salto solo, no), y saliendo del agua de la franja de al lado. Lo
  comprueba `DisenoNivel2Tests`, con los mismos márgenes que el resto del recorrido.

### 4. Nadie se queda encajado en el hielo

- **Problema.** La capa de hielo aparece de golpe. Si Kael está saltando dentro del agua justo a
  esa altura, quedaría metido en un bloque sólido.
- **Solución.** Quien esté dentro de la capa al formarse la atraviesa hasta que sale de ella; a
  partir de ahí es sólida también para él.

### 5. Un jefe inmune tiene que poder llorar

- **Problema.** Como solo se la hiere llorando, un cambio en sus datos (quitar el llanto de una
  fase, dejar una fase vacía) la dejaría invencible sin que nada fallara a la vista.
- **Solución.** `VerificadorDoncella` lo comprueba al final de «Regenerar todo», que falla si
  no cuadra: es inmune fuera de la ventana, cada fase tiene patrones y un llanto, los ataques
  avisan al menos 0,3 s y no congela sin haber inundado. También revisa su prefab.

### 6. Sus datos se completan solos

- **Problema.** Los generadores no pisan los datos que ya existen, para respetar lo ajustado a
  mano. La Doncella ya tenía datos (el esqueleto), así que no habría recibido sus patrones.
- **Solución.** `GeneradorJefes` completa también al jefe que todavía es un esqueleto (fases sin
  ningún patrón). Una vez completa, lo ajustado a mano se respeta.

### 7. El agua sube igual a cualquier número de imágenes por segundo

- **Problema.** En las capturas del combate, la franja «inundada» no tenía agua: no empezaba a
  subir hasta que alguna imagen del juego tardaba más de la cuenta.
- **Causa.** `FranjaInundable` ponía el nivel a cero mientras no pasara de 0,01 u («todavía no
  moja»). El agua sube a 6,8 u/s: a más de 680 imágenes por segundo, cada paso es menor que ese
  umbral y se perdía entero, una imagen tras otra. Unity sin ventana va mucho más rápido que eso
  (unas 2.700 imágenes por segundo en este PC), y a un PC rápido sin sincronía vertical le pasaría lo mismo. Las pruebas no lo
  veían porque cualquier imagen lenta (crear a Kael, lanzar una ola) arrancaba la subida.
- **Solución.** El nivel se guarda aunque todavía no moje; solo se oculta el agua. Prueba nueva
  con el reloj fijo a una milésima de segundo por imagen (`Time.captureDeltaTime`).

### 8. Una prueba de la Plañidera que dependía de lo rápido que fuera el PC

- **Problema.** `LaPlanidera_DeCerca_AvisaYBarreConElBrazo` falló en la batería final: medía
  0,44 s de aviso donde hay 0,55.
- **Causa.** La prueba apuntaba el inicio del ataque la primera vez que la miraba, después de
  colocar a Kael a su lado (seis pasos de física, 0,12 s). Si ella atacaba en cuanto Kael
  aparecía, ese tiempo se restaba del aviso: la medida quedaba entre 0,43 y 0,47 s contra un
  mínimo de 0,45, y pasaba o no según cayera el reloj.
- **Solución.** El inicio lo apunta el cambio de estado (`OnStateChanged`). El juego no cambia.

### 9. Las últimas imágenes y las decisiones

- Limpios `Doncella/L2` (ya son sus 28 fotogramas, en el mismo lienzo) y la
  `PresentacionDoncella2` nueva, que salió a 480 × 268 como las otras tres.
- Decidido por el autor: la placa se importará a PPU 66 (N33), las cinemáticas van a 480 (N34)
  y la cola cortada de la Doncella se funde en el juego (N35). Se aplican al integrar el arte.

### Pruebas

434 EditMode y 185 PlayMode en verde. Nuevas: `DoncellaLamentoTests` (sus datos, dónde
llora y el verificador), `DoncellaPlayModeTests` (flota, la ventana, cada patrón, la fase 2, el
reinicio y la derrota, con el prefab y el jugador reales) e `InundacionPlayModeTests` (el agua,
las púas y la capa de hielo). `DisenoNivel2Tests` y `EscenaNivel2Tests` comprueban las franjas,
la altura de la capa y que la escena las lleva, y `Nivel2PlayModeTests` pelea una ronda entera
en la arena real. `DisenoNivel2.Version` sube a 6.

---

## Mundo 2, fase C cerrada: la Plañidera y las imágenes limpias (2026-10-10)

Misma rama, `nivel2-recorrido`. El autor terminó de generar las imágenes y pidió limpiarlas,
apuntar lo que falta y cerrar la fase C. De la fase C quedaba el mini-jefe. `EnemyController`,
`PlayerController` y `GameManager` no se tocan.

### 1. La Plañidera, el mini-jefe del Mundo 2

- **Problema.** El Mundo 2 no tenía mini-jefe. El Mundo 1 tiene al Golem de Ceniza.
- **Causa.** La figura que dio AI Studio para la Doncella pasó a ser el mini-jefe el 2026-10-09,
  con una propuesta de combate. Faltaba decidir dónde iba (PENDIENTES N32) y montarla.
- **Solución.**
  - **De cerca es un `EnemyController` más**, con sus datos (`Planidera.asset`): 10 de vida e
    hiperarmadura, como el Golem. Flota hacia Kael a 1,6 u/s y, a tiro, echa el brazo atrás
    0,55 s y barre (quita 2). Tocarla no hace daño.
  - **Las esquirlas las lleva `Planidera`**, un componente a su lado. Cuando lleva 2,5 s
    persiguiendo a Kael, alza las esquirlas (0,5 s), las apunta (0,8 s: el aviso) y lanza tres,
    una cada 0,35 s, hacia donde esté Kael. Son el `Proyectil` de la torreta: quitan 1 y se
    deshacen contra el suelo y las paredes.
  - **Después llora 2 s desarmada**, sin atacar ni moverse, y las esquirlas vuelven (0,6 s). Es
    la ventana para castigarla, y lo que pedirá la Doncella: esperar a que baje la guardia.
  - **Mientras maneja las esquirlas, su `EnemyController` está apagado**, como el de la
    Escultura dormida: así no anda ni barre sin tocar el controlador. Solo empieza la ráfaga
    desde la persecución, nunca en mitad de un barrido.
  - **Si muere con el controlador apagado**, nadie oiría la muerte. `Health` avisa del golpe
    antes que de la muerte, así que `Planidera` enciende el controlador al recibir el golpe
    mortal: llega a tiempo de oírla y muere por el camino de siempre.
  - **Sin arte**, el aviso y el llanto se leen en el color del bloque: cian al apuntar y gris
    al llorar.

### 2. Dónde va

- **Problema.** Un mini-jefe con proyectiles necesita sitio para esquivarlos y algo que guardar.
- **Causa.** El recorrido ya estaba medido y probado: no convenía moverlo.
- **Solución.**
  - **En la plaza helada, en el sitio de la segunda Escultura** (x 32, flota de x 31 a x 33).
    Es la propuesta de N32. Son 6 u de hielo entre el puente y el tapón rompible, que es la
    única salida de la superficie: lo guarda ella.
  - **Ve con línea de visión.** El puente mide 2 u y tapa a Kael mientras está al otro lado:
    no le lanza esquirlas a quien aún pelea con la Escultura.
  - El nivel queda con **una Escultura**, no con las dos del Anexo A. `DisenoNivel2` va por la
    versión 5.
  - El tercer Lamento sigue en la plaza este, a su lado. Si la prueba dice que es demasiado, se
    mueve (N31).

### 3. Fuera del proyecto: las 121 imágenes del Mundo 2

- **Problema.** Llegaron todas las imágenes, cada grupo con su lienzo y su tamaño de píxel:
  bloques de 20,48, 15, 11,38, 10,24, 9,47, 8, 6,83 y 5,73 px.
- **Causa.** Gemini y AI Studio ya no dibujan siempre 100 o 200 píxeles de arte a lo ancho. La
  medida automática de ayer fallaba con los bloques finos: la Plañidera salía a 4,7 px.
- **Solución.** `limpiar_mundo2.py` lleva el periodo escrito en cada grupo, medido al revisar la
  tanda (por autocorrelación de los bordes, comprobado a ojo), y aprende cuatro cosas:
  - salta solo los fotogramas que traen el damero de transparencia pintado;
  - conserva los morados de la capa de la Doncella, que la limpieza normal dejaba verdosos;
  - baja los fondos y las cinemáticas enteros, sin recortar, y mide si casan al repetirse;
  - ancla en vertical sin dar la vuelta (a `d3` le reaparecía por arriba lo que salía por abajo).
  Los grupos de ayer salen idénticos, byte a byte.
- **Lo que se encontró** (todo en el LEEME de `Tesis/Imagenes/limpios/Mundo2/` y en N30):
  - **faltan** las tres esquirlas de la Plañidera, el ataque del Lamento (tres fotogramas
    traen el damero pintado) y el tapón de hielo, que sigue siendo la losa del prompt viejo;
  - **hay que repetir** la torre del fondo, `Puas/P4` y tres fotogramas de la Doncella;
  - **dos decisiones nuevas:** las cinemáticas salieron a 480 píxeles de ancho, no a 240 (N34),
    y la cola de la Doncella se sale del lienzo por abajo en los 28 fotogramas (N35).

### Verificación

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** crea los datos y el prefab de la Plañidera y vuelve a montar el Nivel 2
  (versión 5 del diseño).
- **Pruebas EditMode: 420 correctas de 420.** Nuevas:
  - en `EnemigosTristezaTests`: los datos de la Plañidera y su prefab (las esquirlas, el
    aviso, la ventana y el barrido, que empieza en su cuerpo);
  - en `DisenoNivel2Tests`: guarda el tapón, entre el puente y el agujero;
  - en `EscenaNivel2Tests`: está en la escena con su ruta, y la segunda Escultura ya no.
- **Pruebas PlayMode: 168 correctas de 168.** Nuevas, en la escena real del Nivel 2:
  - alza, apunta y lanza sus tres esquirlas, y se queda desarmada: no ataca ni se mueve, y la
    espada la castiga;
  - de cerca, avisa y barre con el brazo;
  - tras acechar a Kael, alza las esquirlas ella sola;
  - si muere con las esquirlas en alto, muere de verdad.

---

## Mundo 2, fase C: los enemigos de la Tristeza (2026-10-09)

Misma rama, `nivel2-recorrido`. Mientras Gemini estaba caído, el autor pidió limpiar las imágenes
y avanzar en lo que se pudiera. Todo va sin arte. `EnemyController`, `PlayerController` y
`GameManager` no se tocan.

### 1. El pesar del Lamento

- **Problema.** El Lamento era la cápsula de la Fase 4: lento, pero su golpe no tenía nada de
  la Tristeza.
- **Causa.** El «reducen velocidad al contacto» del Anexo A estaba previsto para esta fase.
- **Solución.**
  - **`Pesar`**, en el Lamento, escucha el `OnImpacto` de su arma y de su cuerpo: solo pesa el
    golpe que entra (con i-frames o escudo, nada).
  - **`Pesadumbre`**, en Kael, le pone un modificador de movimiento de media velocidad durante
    2 s. Se multiplica con el agua y el hielo, otro golpe renueva el tiempo y no lo suma, y
    reaparecer lo quita. Vive en Kael para que se le pase aunque el Lamento muera.

### 2. La Escultura Viviente

- **Problema.** Faltaba la Escultura: una estatua del decorado que despierta, no anda, avisa su
  golpe y se queda clavada después.
- **Causa.** Era de esta fase. Lo difícil era hacerla dormir sin tocar `EnemyController`.
- **Solución.**
  - **Datos** (`Escultura.asset`): velocidades 0, hiperarmadura como el Golem, sin daño por
    contacto, 1 s de aviso, 2 de daño y 2,25 s clavada después del golpe.
  - **`EsculturaDormida`** tiene el `EnemyController` apagado hasta que Kael pasa a 3,5 u.
    Tarda 1 s en despertar y entonces lo enciende.
  - **Si la golpean dormida, despierta en el acto.** El controlador deja la vida al máximo al
    arrancar, así que lo que le quitaron se le vuelve a quitar justo después: el golpe a la
    estatua cuenta.
  - **El prefab** (`Enemigo_Escultura`) mide 2,4 u y golpea delante y abajo con las dos manos.

### 3. La Medusa de Hielo

- **Problema.** Faltaban las cuatro Medusas: flotan sobre una línea, queman y no mueren.
- **Causa.** Era de esta fase. No es un `EnemyController`: no anda, no persigue y no tiene vida.
- **Solución.** `MedusaHielo` es un cuerpo cinemático en la capa Enemy: la espada la nota y Kael
  la atraviesa. Lleva un contacto que quema. Un golpe la aparta 1,5 u y la encoge 1 s, y su
  contacto encoge con ella: ahí está el hueco. Las de la calle suben y bajan cada 4 s; las de la
  torre cruzan cada 5 s. Van a la contra dos a dos.
- **Un fallo del diseño que salió al colocarlas.** A la altura de la fase A, las dos Medusas de
  la torre (y −4,75 e y −0,4) rozaban la cabeza de Kael, que mide 1,8 u, mientras esperaba en
  los rellanos: esperar el hueco no era seguro. Ahora pasan a y −4 y a y 0,1, con 0,2 u de margen
  y sin atravesar las losas. `DisenoNivel2` va por la versión 4.

### 4. Fuera del proyecto: las imágenes y los prompts

- **Las imágenes del Mundo 2 que había, limpias**, en `Tesis/Imagenes/limpios/Mundo2/` con su
  LEEME y su script (`limpiar_mundo2.py`). Usa el mismo motor que el Golem, con dos cambios: abre
  lienzos que no son de 2048 sin deformarlos, y mide el bloque de arte (un número redondo de
  píxeles a lo ancho del lienzo). Dos avisos: el hielo rompible salió con el prompt viejo (una
  losa) y hay que repetirlo, y el arte de la placa mide 3,46 u frente a las 2,5 del juego
  (PENDIENTES N33).
- **Los prompts de los enemigos** (`Prompts_Mundo2.md`):
  - El del Lamento pedía «sin ojos», y la referencia los tiene. Ahora va en dos pasos: una
    maestra, y cada fotograma con dos adjuntos (la pose y el diseño) y una lista de lo que no
    puede faltar.
  - La Escultura atacaba editando la estatua dormida, que no tiene ojos. Ahora tiene una segunda
    maestra despierta.
  - **La Plañidera**, la figura de AI Studio, será el mini-jefe del Mundo 2. Tiene propuesta de
    combate y sus 19 prompts.

### Verificación

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** crea los datos y los prefabs nuevos y vuelve a montar el Nivel 2 (versión 4
  del diseño). Volver a ejecutarlo no cambia nada.
- **Pruebas EditMode: 416 correctas de 416.**
  - **Nuevas:**
    - `EnemigosTristezaTests`: el pesar, los datos y el prefab de la Escultura, el seno y el
      golpe de la Medusa, y los prefabs del Lamento, de la Medusa y de Kael;
    - en `DisenoNivel2Tests`: el ritmo de las Medusas, y que en los rellanos no rozan a Kael;
    - en `EscenaNivel2Tests`: las Esculturas dormidas y las Medusas, con el recorrido del diseño.
- **Pruebas PlayMode: 164 correctas de 164.** Nuevas, en la escena real del Nivel 2:
  - el zarpazo del Lamento frena a Kael 2 s;
  - la Escultura duerme hasta que Kael se acerca y despierta sin moverse;
  - la Escultura descarga su golpe si Kael se queda delante;
  - a golpes, la Escultura despierta en el acto y el golpe cuenta;
  - la Medusa no muere (tampoco con el golpe cargado), la espada la aparta y la encoge;
  - la Medusa quema al tocarla.

---

## Mundo 2, fase B: las mecánicas de la Tristeza (2026-10-08)

Misma rama, `nivel2-recorrido`. El autor pidió seguir con la fase B mientras genera las imágenes.
Todo va sin arte: rectángulos del tamaño de cada pieza y la interfaz generada por código.

### 1. El hielo rompible: la única salida de la superficie

- **Problema.** El agujero de la plaza estaba abierto: nada obligaba a usar lo aprendido en la Ira.
- **Causa.** Faltaba la pieza (era de esta fase).
- **Solución.** `HieloRompible`: un golpe normal lo agrieta y uno cargado (`Golpe.Cargado`) lo
  rompe y deja el hueco abierto. Dos detalles:
  - **Un tapón que asoma, no una losa.** La espada de Kael golpea de frente, entre 0,25 y 1,55 u
    sobre sus pies: una losa a ras del suelo no se podía golpear. El tapón llena el hueco y asoma
    1,25 u, lo justo para subirse encima.
  - **La espada solo golpea la capa Enemy.** El tapón se pisa en la capa del suelo y lleva una
    zona de golpe en la capa Enemy, que la matriz de colisión no deja chocar con Kael ni con los
    enemigos.

### 2. Las placas que flotan

- **Problema.** Las placas eran bloques quietos.
- **Causa.** Kael no sabía ir encima de algo que se mueve.
- **Solución.** `PlataformaFlotante`, un cuerpo cinemático que sube y baja en un seno con
  `MovePosition`: al subir empuja a Kael, y al bajar (como mucho a 1 u/s) Kael la sigue por su
  peso. Su franja de hielo es un hijo y se mueve con ella. Las del agua suben de y 0 a y 1, a la
  contra: en lo más bajo siguen fuera del agua (encima no se está mojado) y se cruzan sin esperar el
  momento. Las de la arena sí piden esperarlo.

### 3. El aire, y ahogarse sin que nadie empuje

- **Problema.** Bajo el agua no había oxígeno.
- **Causa.** Era de esta fase. El autor decidió que al acabarse no se muriera de golpe, sino que se
  perdiera un corazón cada 2 s.
- **Solución.**
  - **`OxigenoJugador`** en el prefab de Kael, con la cuenta en `ReservaOxigeno` (sin Unity, para
    probarla): 15 s de aire, lleno en 1 s fuera del agua, y sin aire un corazón cada 2 s.
  - **`Health.PerderVida`:** quitar vida sin que sea un golpe. Con `RecibirGolpe`, el ahogo habría
    hecho pasar a Kael por DAMAGED, con su retroceso, cada 2 s: lo habría sacado de su trayectoria
    bajo el agua. Tampoco respeta los i-frames ni el escudo, que son contra golpes. No toca
    `PlayerController`.
  - **El HUD (`IndicadorAgua`):** una barra de aire bajo la de vida, que aparece en el agua y
    parpadea en rojo sin aire; y un velo azul en los bordes de la pantalla bajo el agua. Es la
    «visibilidad reducida» del Anexo A, hecha con la viñeta de la interfaz en lugar de con un
    volumen de post-procesado: mismo efecto, sin un perfil más que mantener.
  - **Un fallo que encontró la prueba PlayMode:** el oxígeno solo avisaba al HUD si el aire
    cambiaba más de 0,001, y el último paso hasta 0 puede cambiar menos. Entonces Kael perdía
    corazones con la barra sin ponerse roja. Ahora el paso a asfixia avisa siempre.

### 4. Los ecos de intentos y el eco de memoria

- **Problema.** El Anexo A pide que, tras cada muerte, una sombra repita el último recorrido. Y las
  líneas de «eco de memoria» del guion no sonaban en ningún nivel.
- **Causa.** Las dos cosas estaban previstas para la Fase 10.
- **Solución.**
  - **`EcoDeIntentos`** graba los últimos 8 s de Kael (posición, fotograma y hacia dónde mira) en un
    búfer circular (`GrabacionIntento`). Al morir guarda el intento; al reaparecer lo repite con una
    sombra translúcida, sin colisiones, que después se desvanece. Usa los fotogramas de Kael: no
    necesita arte.
  - **El eco de memoria:** `Checkpoint.ecoDeMemoria` marca el checkpoint donde suena, y el
    `LevelManager` dice la línea la primera vez que se toca en esa carga del nivel (restaurar una
    partida no cuenta). El preparador lo marca en el primer checkpoint de cada nivel: en el Nivel 2,
    junto a la silla vacía; y en el Nivel 1 ahora también suena («Una voz grita órdenes…»).

### Verificación

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** crea los dos prefabs nuevos y vuelve a montar el Nivel 2 (versión 3 del
  diseño). Volver a ejecutarlo no cambia nada.
- **Pruebas EditMode: 405 correctas de 405.**
  - **Nuevas:**
    - `MecanicasTristezaTests`: el aire y la asfixia, perder vida sin golpe, la grabación circular,
      el seno de las placas, el tapón y los tres prefabs;
    - en `DisenoNivel2Tests`: cada salto en el mejor momento de las placas, las placas del agua en
      el peor y el tapón que tapa y asoma;
    - en las de las escenas: las placas, el tapón, los ecos y el eco de memoria del Nivel 1 y del 2.
- **Pruebas PlayMode: 158 correctas de 158.** Nuevas, en la escena real del Nivel 2:
  - la espada agrieta el tapón y la cargada lo rompe, y Kael cae al agua;
  - la placa sube y baja llevando a Kael;
  - sin aire se pierde un corazón cada 2 s sin retroceso, y en la campana se respira;
  - tras morir, la sombra repite el intento y se va;
  - en la campana suena el eco de memoria.

---

## Mundo 2, fase A: el recorrido del Nivel 2 (2026-10-08)

Rama `nivel2-recorrido`, que sale de `nivel1-montado`. Empieza la Fase 10 con el mundo de la
Tristeza. El autor aprobó el diseño (`Docs/Diseno/Nivel2_Diseno.md`) con sus ocho decisiones y pidió
arrancar. Esta fase monta el recorrido en bloques de color, sin arte y sin las mecánicas nuevas, que
llegan en las fases B a D.

### 1. El diseño del Mundo 2

- **Problema.** El Nivel 2 solo existía como dato (`L2_Tristeza`): sin escena, sin recorrido y con
  un jefe esqueleto. El Anexo A daba la emoción, las mecánicas y los recuentos, pero no cómo enseñar
  la Tristeza con ellos; la Escultura Viviente ni siquiera tenía comportamiento.
- **Causa.** Hasta ahora se trabajó el Nivel 1 como referencia (Fase 9), como pedía el encargo.
- **Solución.** Un diseño en tres tiempos que sale de la emoción: arriba el hielo suprime (todo
  resbala y la superficie no lleva a ninguna parte), la única salida es hundirse (el Ataque Cargado
  rompe el hielo), y bajo el agua todo pesa pero Kael salta más alto: la torre solo se sube así, y la
  Doncella solo se hiere cuando llora. Las decisiones del autor: hielo rompible con el Ataque
  Cargado, Medusas que no mueren, Escultura que despierta pero no anda, oxígeno de 15 s que después
  quita un corazón cada 2 s, dos checkpoints y la Doncella vulnerable solo al llorar. Los prompts de
  sus 26 imágenes están en `Docs/Diseno/Prompts_Mundo2.md` (la Doncella, por Google AI Studio).

### 2. El recorrido como datos y su montaje

- **Problema.** Había que llevar el diseño a una escena que se pueda rehacer, como el Nivel 1, pero
  sabiendo que el nivel va a crecer en varias fases.
- **Causa.** El montaje del Nivel 1 solo se ejecuta la primera vez: rehacerlo obliga a borrar la
  escena a mano.
- **Solución.**
  - **`DisenoNivel2`**: suelos, muros, placas, agua, bolsas de aire, hielo, enemigos, checkpoints,
    arena y los saltos del recorrido con el medio en que se dan (en seco o bajo el agua).
  - **El montaje** (`PreparadorNivel`, ahora `partial`, con `PreparadorNivelTristeza.cs`) se
    vuelve a ejecutar entero cuando sube `DisenoNivel2.Version`: así cada fase entra sola en la
    escena. Con la misma versión, preparar otra vez no cambia nada.
  - **La escena `Nivel2`** se crea desde la plantilla URP 2D al regenerar, se asigna a su
    `LevelData` con sus checkpoints (x 50 y x 65) y entra en Build Settings. Ya se juega desde el
    selector y con «Continuar» tras superar la Ira.
  - **El agua y el hielo son los de verdad:** triggers con `ZonaModificadorMovimiento` (agua:
    velocidad × 0,6, gravedad × 0,3 y salto × 0,6; hielo: aceleración × 0,15). El agua es un velo
    translúcido por delante de Kael; el hielo, un remate blanco sobre los suelos que resbalan.
  - **La cámara** baja hasta y −13 (la calle está a −11) con el encuadre del Nivel 1, y la zona
    mortal queda en y −16.
  - **La arena** es la del Nivel 1 con las medidas del 2: 25 × 15 u, la puerta, la entrada y la
    Doncella, que atraviesa las placas y el balcón.

### 3. Saltar bajo el agua: lo que el esquema no tenía en cuenta

- **Problema.** En el esquema de la propuesta, la repisa de la casa y el último escalón de la torre
  se alcanzaban sobre el papel, pero por muy poco.
- **Causa.** Al cruzar la superficie la gravedad vuelve a ser la de fuera, y lo que queda del salto
  sube 0,3 veces lo que subiría dentro. Una subida de 4 u que sale del agua dejaba a Kael 0,3 u por
  encima del borde. Además, una superficie a ras de lo que se pisa toca a Kael de pie en el borde: la
  física cuenta como solapados dos cuerpos que solo se rozan, y lo habría dejado frenado.
- **Solución.**
  - **La casa es una campana** colgada del techo de hielo, que guarda el aire debajo; no tiene suelo
    y se entra por debajo.
  - **Las subidas que salen del agua son más bajas:** 3,5 u a la repisa y 3,75 al último rellano.
    Los dos primeros escalones de la torre miden 4,25 u (4,5 no cabía en el margen del 90 %) y
    siguen sin poder subirse en seco (4,1).
  - **Cada superficie queda 0,1 u por debajo** de lo que se pisa al salir.
  - **Las pruebas lo calculan:** cada salto en su medio, con los mismos márgenes que el Nivel 1, y
    al salir del agua al menos 0,25 u por encima del borde.

### Lo que falta, por fases

- **B · Mecánicas:** el hielo rompible (hoy el agujero está abierto), las placas que suben y bajan
  (hoy quietas), el oxígeno, los ecos de intentos, el eco de memoria y el volumen submarino.
- **C · Enemigos:** el pesar del Lamento, las Esculturas y las Medusas.
- **D · Jefe:** los patrones de la Doncella. Hoy espera quieta.

### Verificación

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** crea la escena, la monta, la asigna y la añade a Build Settings. Volver a
  ejecutarlo no cambia nada.
- **Pruebas EditMode: 391 correctas de 391.**
  - **Nuevas:** `DisenoNivel2Tests` (cada salto en su medio, que la torre solo se sube bajo el
    agua, el impulso al salir, el agua sin solapes y casi a ras, las bolsas de aire, los pasos
    estrechos, los recuentos del Anexo A, los bordes enteros y el hielo solo sobre el hielo) y
    `EscenaNivel2Tests` (la escena sigue al diseño, y prepararla otra vez no cambia nada).
  - **Al día:** `DatosProgresionTests`, con el Nivel 2 jugable.
- **Pruebas PlayMode: 153 correctas de 153.**
  - **Nuevas:** `Nivel2PlayModeTests`: en la escena real, el hielo resbala, el agua frena y la
    repisa de la campana es un checkpoint seco.
  - **Al día:** las del flujo y la progresión. Tras superar la Ira se entra en la Tristeza, y el
    «en construcción» del selector lo enseña ahora el tercer mundo.

---

## Respuestas a la prueba del Nivel 1 montado (2026-10-08)

Rama `nivel1-montado`. El autor jugó el nivel (N26) y encontró un solo fallo: el diálogo del jefe
solo salía al entrar con «Continuar partida». Además aprobó subir la altura de Kael agachado (N28),
descartó retocar la muerte (N29) y dejó en sus carpetas la tercera tanda de las erupciones (N27) y
el retoque de `CinematicaFinalLvl2` (N25).

### 1. La línea de la Centinela, también en partida nueva

- **Problema.** En partida nueva, al entrar en la arena se veía la presentación del jefe, pero la
  Centinela no decía su línea del guion («Él me dejó aquí para que nadie tocara su rabia…»). Con
  «Continuar partida» sí la decía.
- **Causa.** La presentación del Nivel 1 se ve solo la primera vez: queda anotada como vista en la
  partida guardada. Cuando se veía, `ArenaJefe` no ponía las líneas «antes del jefe» en el HUD,
  porque daba por hecho que iban dentro de la presentación. Eso vale para las de los jefes 2 a 4,
  que las generan con «{jefe}: {línea}». La de la Ira, en cambio, está montada desde el guion de su
  cinemática y su único texto es de Kael («No recuerdo esta sala. El calor, sí.»). Al continuar, la
  presentación ya estaba vista y la línea sí salía.
- **Solución.** `CutsceneData.llevaDialogoDelJefe` dice si una presentación ya lleva las líneas del
  jefe. `ArenaJefe` solo las calla en ese caso. `GeneradorNarrativa` la marca en las presentaciones
  que genera; la de la Ira queda sin marcar, así que su línea suena al empezar el combate.

### 2. La erupción, de la grieta al derrumbe

- **Problema.** La erupción solo tenía arte mientras quemaba (E3 y E4). El aviso seguía siendo el
  rectángulo naranja que late.
- **Causa.** De la segunda tanda no entraron el aviso, el brote ni el colapso (entrada anterior).
- **Solución.**
  - **La tercera tanda cuadra:** Gemini editó E3, como se le pidió, y las cinco imágenes comparten
    rejilla (10,24 px), fase y fila de base. `limpiar_erupcion.py` ya no alinea nada a mano: las
    muestrea sobre una rejilla común en un lienzo de 74 × 150, con el pivote en el centro de la
    base de la columna. El aviso y el derrumbe se extienden por el suelo (unas 3 u). E3 y E4 salen
    idénticas a las de antes, solo en un lienzo más ancho.
  - **`DibujoDeZona`**, un componente nuevo del prefab, sigue las fases de su zona de daño: la
    zona le avisa al quemar (`Quema`) y al acabar (`Apagada`). Al acabar, el dibujo se suelta de
    la zona, se derrumba 0,25 s ya sin daño y se destruye solo. Sustituye a `trasElAviso` de la
    entrada anterior, que mostraba el rectángulo mientras avisaba.
  - **El Animator de la erupción:** `Aviso` en bucle; con `Quema`, `Brota` 0,1 s y pasa sola a
    `Arde` en bucle; con `Apagada`, desde cualquiera de ellos, `Apaga`.
  - **Sin rectángulo:** con dibujo, las erupciones ya no pintan su zona, ni al avisar.

### 3. Kael agachado, a la altura de su dibujo

- **Problema.** Agachado, Kael se dibuja con 1,08 u de alto, pero su collider medía 0,9 u: el pelo
  asomaba por encima de lo que golpean los ataques.
- **Causa.** `alturaAgachado` (0,5) era anterior al dibujo.
- **Solución.** `alturaAgachado` = 0,6: 1,8 × 0,6 = 1,08 u. Sigue cabiendo bajo el techo bajo de la
  escena de prueba (1,2 u). Una prueba compara el collider del prefab con el dibujo.

### 4. `CinematicaFinalLvl2` retocada

Dos retoques del autor. El primero llegó a 2752 × 1536 sin el fragmento que flotaba sobre la
espada, pero con cinco huecos en vez de cuatro. El segundo, que es el que entra, llegó a 1024 × 572
con los cuatro huecos y el fragmento solo en el hombro. `cine.py` saca el periodo del ancho
(4,27 px) y lo baja a 240 × 135 como los demás planos. Al venir más pequeño y en JPEG, el fondo
oscuro sale algo más ruidoso; se lee bien. N25 queda resuelto.

### Verificación

- **Pruebas EditMode: 370 correctas de 370.**
  - **Nuevas:** que la presentación de la Ira no lleve la línea del jefe y la de los demás sí; que
    el collider agachado mida lo que el dibujo; y en `ErupcionArteTests`, los cinco fotogramas, los
    cuatro clips y el Animator que sigue las fases de la zona.
- **Pruebas PlayMode: 150 correctas de 150.**
  - **Reescritas:**
    - la de la presentación de la Centinela, que exigía que el HUD callara después: ahora exige
      la línea de la Centinela, y que lo de Kael no se repita;
    - la de la erupción, que se ve dibujada de la grieta al derrumbe; el derrumbe sobrevive un
      momento a su zona y desaparece solo.
  - **Dos pruebas de tiempos fallaron en una primera pasada y pasaron en la segunda, sin cambiar
    nada:** el coyote time y el ciclo de la torreta. Las dos miden fracciones de segundo y el PC
    estaba ocupado.

---

## La erupción quema con su columna y Kael se agacha y muere (2026-10-07)

Rama `nivel1-montado`, tras el commit del Nivel 1. El autor dejó en disco la segunda tanda de las
erupciones (`Tesis/Imagenes/Erupcion`, E1 a E5) y dos hojas nuevas de Kael en Pixelorama:
agacharse y morir (`Tesis/Imagenes Kael`).

### 1. La erupción, con su columna

- **Problema.** Las erupciones de la fase 2 de la Centinela eran rectángulos naranjas: latían
  durante el aviso y se volvían opacos al quemar.
- **Causa.** Faltaba su arte. La primera tanda no entró (entrada anterior) y la segunda solo en
  parte: Gemini dibujó con bloques de 8 px (E1, E2) y de 10,24 px (E3 a E5), no de 16. Medidas a
  su rejilla nativa:
  - **E3 y E4 cuadran:** la columna llena en dos variantes, la misma silueta y la misma base,
    sin suelo;
  - **E1 (aviso) y E5 (colapso) no:** traen un montículo de roca en la base, y la erupción brota
    también sobre las plataformas. E5 además sale cortada por arriba;
  - **E2 (brote) tampoco:** con su otra rejilla mide 54 px de ancho frente a 44, y casi lo mismo
    de alto que la llena.

  Ninguna se recortó ni se reescaló para arreglarla. Se pide una tercera tanda editando E3
  (`Docs/Diseno/Prompts_Gemini.md`).
- **Solución.**
  - **`limpiar_erupcion.py`** (en `Tesis/Imagenes/limpios/script`) baja E3 y E4 a su rejilla y
    las alinea por la base y por su centro: un lienzo común de 44 × 150 con el pivote en el
    centro de la base.
  - **A 24 px/u**, el tamaño de píxel del Golem, la torreta y la bola: 1,83 × 5,7 u (6,25 con las
    gotas), un poco más que su zona de daño (1,6 × 5), así que no quema donde no se ve. Regla
    nueva en `TablaArte`.
  - **`ConstructorEfectos`** construye ya los dos efectos por el mismo camino: el clip
    `Erupcion_Arde` (las dos variantes cada 0,1 s, en bucle), su controlador y el prefab
    `ErupcionDeLava`. Sin el arte de uno, el otro se construye igual.
  - **`ZonaDeDanio.Vestir`** admite dos casos nuevos: con dirección 0 el dibujo va centrado y sin
    espejar, y con `trasElAviso` espera a que la zona queme. Mientras avisa se ve el rectángulo
    que late; al activarse, la columna lo sustituye.
  - **`PatronPlataformasErupcion.efecto`**, que `GeneradorJefes` rellena en las dos erupciones de
    la Centinela solo si está vacío, como con las ondas.

### 2. Kael se agacha y muere

- **Problema.** Agachado, Kael se veía de pie (CROUCH reproducía el idle) aunque su collider se
  encogía a la mitad. Al morir, también seguía de pie (DEAD reproducía el idle).
- **Causa.** Faltaban sus clips (PENDIENTES N3, B3).
- **Solución.**
  - **Las hojas, a 1×:** `Herramientas/reducir_hoja.py` detecta la exportación a 10× de las dos y
    las reduce sin pérdida: agacharse, 7 fotogramas de 64 × 128 (el lienzo de correr); morir, 3
    de 66 × 121. Kael mide lo mismo que en las demás hojas (120 px de pie).
  - **Los pivotes, en la planta de los pies y en el torso:** x 39 en agacharse (el torso de pie
    cae en la 40, como en el idle) y x 33 en morir, que se echa hacia atrás.
  - **Agacharse:** `Agacharse` baja en 0,15 s (fotogramas 1 a 3) y encadena con `AgachadoBucle`
    (3 y 4, al ritmo del idle). Al soltar, `PlayerAnimacion.ClipDePaso` pone `Levantarse` (5 y 6),
    que encadena solo con el idle; si Kael corre, salta o cae, pasa directamente a su clip.
  - **Morir:** los tres fotogramas a 5 fps, como en `Morir.pxo` (0,6 s). Se queda de rodillas
    hasta que `Revivir` lo devuelve al idle.
  - **Dos detalles para el autor**, sin cambiar nada:
    - el dibujo agachado mide 1,08 u y el collider agachado 0,9 u (`alturaAgachado` = 0,5).
      Cabe igual bajo el techo bajo de la prueba (1,2 u). Se recomienda 0,6 (N28);
    - la bota de atrás de `Morir` toca el borde izquierdo del lienzo en los tres fotogramas y
      queda cortada (N29, opcional).

### Verificación

- **Pruebas EditMode: 367 correctas de 367.**
  - **Nuevas:**
    - `ErupcionArteTests`: el lienzo y los 24 px/u, la base apoyada en la fila de abajo, que el
      dibujo cubra su zona de daño, el clip, el prefab y los dos patrones;
    - en `ArteKaelTests`, agacharse (su tabla, el clip de paso y que agachado cabe bajo 1,2 u) y
      morir (5 fps, sin bucle, de rodillas).
  - **Generalizada:** la de las transiciones propias, que ahora son tres cadenas (caer, agacharse
    y levantarse).
- **Pruebas PlayMode: 150 correctas de 150.**
  - **Nuevas:**
    - la erupción avisa con el rectángulo y quema con su columna, centrada y sin espejar;
    - Kael se agacha, se queda abajo y se levanta antes del idle;
    - Kael cae de rodillas, se queda y vuelve de pie al reaparecer.

---

## Nivel 1 montado, guion cargado y la onda de la Centinela (2026-10-07)

Rama `nivel1-montado`, que sale de `ajustes-pendientes`. El autor aprobó el diseño del Nivel 1 y
el guion («móntalo y carga el guion») con las tres decisiones del Anexo A que se le propusieron, y
mandó las tres imágenes de la onda. Pidió también el prompt de las erupciones; su primera tanda
llegó, pero no entró (punto 4).

### 1. El Nivel 1, con su recorrido de diseño

- **Problema.** El Nivel 1 se jugaba sobre el recorrido provisional de la Fase 6: un suelo plano
  con dos fosos, un Eco y el Golem. No enseñaba nada ni decía nada de la Ira.
- **Causa.** El diseño del nivel faltaba (PENDIENTES N10). Ya estaba escrito y aprobado
  (`Docs/Diseno/Nivel1_Diseno.md`): la Ira empuja a correr y el nivel enseña a contenerla; cada
  peligro sale primero sin riesgo, luego sobre lava y luego junto a enemigos.
- **Solución.**
  - **El diseño como datos:** `DisenoNivel1` (suelos, muros, plataformas de la arena, lava,
    mecánicas, enemigos con su ruta, checkpoint, arena y salida). Lo montan `PreparadorNivel` y
    lo comprueban las pruebas. Un paso nuevo del preparador sustituye el provisional, y lo que se
    colocó para él, por el recorrido; solo la primera vez, así que sigue siendo idempotente.
  - **Las piezas reales no cabían en el esquema.** La plataforma de combustión mide 3,42 u y la
    inestable, 3,83; el esquema las dibujaba de 1,8 y 1,6. El lago pasa de 6 a 9 u, la subida
    lleva una inestable y no dos, y el checkpoint queda en x 43 (en la propuesta, 41).
  - **Todos los bordes, en unidades enteras.** El tileset pinta celdas enteras: un bloque que
    acababa a media unidad se dibujaba medio tile fuera de su colisión, como un saliente que no
    se puede pisar.
  - **La lava, detrás del suelo y apoyada en su cuenca.** Sus franjas miden 3,15 u y los huecos,
    lo que pide el recorrido; por detrás de las orillas, lo que sobra no se ve. Las plataformas de
    combustión quedan en pie dentro de ella.
  - **Los Ecos, a la escala que les dio el autor** (1,8 × 1,5) y de pie en su suelo.
  - **La torreta, espejada:** dispara hacia el jugador que llega. Las dos de combustión van
    desfasadas medio ciclo: cuando una se calienta, la otra se apaga.
  - **El pintor** reconoce el recorrido, decora sobre cualquier suelo grueso y deja la arena sin
    decoración. La primera regeneración dejó el nivel sin decoración, por dos motivos:
    - **el paso fijo:** buscaba sitio cada 9 u y, entre tantos enemigos, caía siempre en uno;
    - **una malla vieja:** contaba como ocupado el plano de malla que quedó del proyecto original
      junto al inicio.

    Ahora busca unidad a unidad y solo esquiva sprites.

### 2. La Centinela, en una arena con plataformas

- **Problema.** La arena nueva tiene plataformas a tres alturas (Anexo A), la más baja a 3 u del
  suelo. La Centinela mide 3,4 u: chocando con ellas se habría quedado atascada debajo. Y su salto
  apuntaba a la superficie bajo el jugador, así que habría saltado encima de las plataformas.
  Además las erupciones buscaban el suelo desde 2 u por encima del jugador: con el jugador debajo
  de una plataforma, brotaban encima de ella.
- **Causa.** El jefe se probó en arenas planas: la provisional tenía una sola plataforma.
- **Solución.**
  - **`ArenaJefe.plataformas`:** el jefe ignora su colisión (`Physics2D.IgnoreCollision`).
  - **`BossController.SueloPisableEn`:** el salto cae en el suelo que el jefe pisa, y por eso
    subirse a una plataforma es esquivarlo. Es lo que el diseño quería que enseñaran.
  - **`SueloEn` busca desde el centro del jugador:** las erupciones brotan bajo sus pies, también
    en una plataforma, y obligan a bajar.
  - **Las ondas se rompen contra la pared** en lugar de atravesarla (`PatronAtaque.RecorridoEnArena`).

### 3. La onda de la Centinela, con su arte

- **Problema.** La onda de suelo y las del salto se veían como rectángulos naranjas.
- **Causa.** Faltaba su dibujo. Llegó con dos problemas:
  - **Escala:** cuatro veces más grande de lo pedido (unos 225 píxeles de arte de ancho, no 60).
  - **Encuadre:** cada imagen con el suelo y el frente en un sitio distinto. Una era de 1024 px,
    con otra rejilla.
- **Solución.**
  - **Una limpieza nueva,** `Tesis/Imagenes/limpios/script/limpiar_onda.py`, sin scipy. Cada
    imagen se baja a su rejilla nativa (270 o 256 píxeles a lo ancho, medidos). Luego se alinean
    por la línea de suelo y por el frente de la roca en un lienzo común de 232 × 119, sin
    reescalar nada.
  - **A 98 px/u,** el doble que la Centinela. A 49 la onda mediría 4,7 × 2,4 u, más alta que el
    salto de Kael. A 98 mide 2,4 × 1,2 u, y la roca de su cresta coincide con su zona de daño
    (0,9 u), que no cambia: la pelea ya estaba aprobada.
  - **`ConstructorEfectos`** hace su clip en bucle (sube, se curva, rompe), su controlador y el
    prefab `OndaDeFuego`. **`ZonaDeDanio.Vestir`** lo pone en la zona con el pivote en su borde
    delantero, espejado si va a la izquierda.
  - **Los patrones de la onda** (`efecto`) y del salto (`efectoOndas`) lo llevan. El generador de
    jefes rellena el campo si está vacío.

### 4. Las erupciones: la primera tanda no entró

- **Problema.** Llegaron cinco imágenes de la erupción, solo por el chat, y no cuadraban:
  - **dos tamaños de píxel:** el aviso y el colapso, el doble de grandes que las demás;
  - **suelo en la imagen de la explosión,** y la erupción brota también en las plataformas;
  - **una columna corta:** para llenar su zona de daño (1,6 × 5 u), sus píxeles tendrían que ser
    3,5 veces los de la Centinela.
- **Causa.** El prompt pedía 256 píxeles de arte a lo ancho, y Gemini dibuja con bloques de unos
  16-20 px.
- **Solución.** No se integran. Hay un prompt corregido en `Docs/Diseno/Prompts_Gemini.md`:
  - **128 píxeles a lo ancho,** para entrar a 24 px/u como el Golem y la torreta;
  - **sin suelo;**
  - **una imagen cada vez** en el mismo chat (PENDIENTES N27).

### 5. El guion, cargado

- **Problema.** Las cinemáticas de cierre y los diálogos decían «pendiente de guion».
- **Causa.** El guion faltaba (N11). Ya estaba escrito y aprobado (`Docs/Diseno/Guion.md`).
- **Solución.**
  - **`GuionMundos` tiene los textos.**
  - **El generador de narrativa los pone** en los diálogos, los cierres, la entrada al final, el
    final y las presentaciones de los jefes 2, 3 y 4. Solo sustituye lo provisional: lo escrito a
    mano en un asset, o una cinemática con imágenes, se respeta.
  - **La pregunta final se queda 6 s,** sola y al final.

### Verificación

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»:**
  - monta el Nivel 1 y lo pinta;
  - importa la onda y crea su clip y su prefab;
  - carga el guion.

  Volver a ejecutarlo no cambia nada.
- **Pruebas EditMode: 360 correctas de 360.**
  - **Nuevas:**
    - `DisenoNivel1Tests`, con las medidas reales de Kael: que cada pisada se alcanza desde la
      anterior, el hueco más largo, las plataformas de la arena, los bordes enteros, la lava sobre
      su cuenca y los recuentos del Anexo A;
    - `OndaArteTests`: el lienzo, el pivote sobre el frente de la roca, el clip, el prefab y los
      patrones;
    - cuatro del guion: ningún placeholder, las líneas de cada mundo, las cinemáticas y las
      presentaciones.
  - **Reescritas:** las de la escena del Nivel 1, para el recorrido, los enemigos, las mecánicas y
    la arena.
- **Pruebas PlayMode: 147 correctas de 147.**
  - **Nuevas:** la onda vestida, espejada y rota contra la pared, y el suelo del salto frente al de
    las erupciones con una plataforma.
  - **Al día:** las del flujo del nivel, ahora con el suelo a varias alturas, y que la Centinela
    atraviesa las plataformas de la arena.

---

## Respuestas a la prueba del Nivel 1 (2026-10-07)

Rama `ajustes-pendientes`, que sale de `menu-mecanicas`. El autor jugó el Nivel 1 y respondió
a PENDIENTES:

- la pelea, el golpe del Eco y su tamaño están bien;
- el encuadre del ojo se queda;
- no hace falta fuente bitmap;
- los textos de la guía están bien.

Además pidió cinco cosas.

### 1. La bola de fuego, con su arte

- **Problema.** La torreta disparaba un círculo naranja provisional.
- **Causa.** Faltaba `Fireball3`. Llegó, y con ella las tres imágenes de la bola. No venían sobre
  el fondo magenta de las demás, sino sobre el **damero gris y negro que imita la
  transparencia, pintado dentro del JPG**. La limpieza de siempre no sirve.
- **Solución.** Una limpieza nueva, `Tesis/Imagenes/limpios/script/limpiar_bola.py`:
  - **Cada casilla del damero es un píxel del dibujo** (2048 / 50 = 40,96 px), así que se
    trabaja en la rejilla nativa de 50 × 50, donde el fondo es predecible. En una paridad de
    casilla toca negro y en la otra gris.
  - **Es dibujo lo que se aparta del damero en su sitio:** el color saturado, el núcleo blanco y el
    negro donde tocaba gris (el contorno).
  - **El contorno que cae en casilla negra se recupera por vecindad.** Solo crece alrededor de la
    bola, no de las chispas sueltas, que no llevan contorno. Al principio la franja de abajo, que
    tiene el damero desplazado una casilla, se colaba como contorno.
  - **El núcleo blanco se cuantiza aparte** para que no se mezcle con el naranja.
  - **Las tres comparten lienzo** (40 × 29) para que la bola no salte al cambiar de fotograma.
  - **El pivote va en el centro de la bola, no del lienzo,** para que la estela quede detrás.
    Va a 24 px/u, la misma densidad que la torreta.

  En el juego:
  - **Vuelo:** la bola vuela en bucle con sus dos fotogramas (clip `Bola_Vuelo`, controlador
    `Bola`, un solo estado).
  - **Al apagarse:** al golpear, al tocar escenario o al acabar su alcance, el `Proyectil` deja el
    tercer fotograma, las chispas, 0,2 s y sin daño.
  - **La bola se destruye en el acto,** como antes, para que no pueda golpear dos veces.

### 2. El segundo plano de la derrota

- `CinematicaFinalLvl2` llegó regenerada con el prompt nuevo: Kael grande, de espaldas, con el
  fragmento en el hombro y los huecos visibles. A 240 × 135 los huecos se leen; en el intento
  anterior eran manchas de 2 px.
- `cine.py` se adapta a las rutas de este PC y procesa solo los planos que se le piden. El plano
  entra solo en la cinemática de la derrota, como estaba previsto.
- **Lo que no cuadra** (N25, opcional): flota otro fragmento sobre la espada y hay cinco huecos
  vacíos, no cuatro. El prompt de edición está en `Docs/Diseno/Prompts_Gemini.md`. Además, llegó
  la copia de 2000 px y no el original de 2752; se procesa igual.

### 3. Sin rectángulos naranjas donde la animación ya avisa

- **Problema.** Los ataques de la Centinela pintaban su zona de daño como un rectángulo naranja.
  El autor: «por eso se hicieron las animaciones».
- **Solución.**
  - `PatronAtaque.dibujarZona`: el espadazo (normal y de furia) y la marca de la caída del salto
    ya no se pintan. Siguen haciendo el mismo daño.
  - La **onda** y las **erupciones** no tienen arte todavía y siguen pintándose: si no, harían
    daño invisible. Para las ondas del salto hay un campo aparte, `dibujarOndas`.
  - El prompt de la onda está en `Docs/Diseno/Prompts_Gemini.md`.

### 4. Caminar con Alt

- **Problema.** El sigilo del mundo del Miedo cuenta como silencioso ir por debajo del 40 % de la
  velocidad máxima. Con teclado, A y D van siempre a fondo.
- **Solución.**
  - Una acción nueva, `Walk`, en Alt izquierdo.
  - `FuenteInputJugador` deja la dirección al 35 % mientras se mantiene (`factorCaminar`). La
    velocidad de Kael ya seguía a la dirección, así que no se toca `PlayerController`.
  - La guía de mecánicas tiene su fila «Caminar». El generador añade a la guía los movimientos
    nuevos sin tocar los textos que ya había.

### 5. Propuestas de diseño: el Nivel 1 y el guion

El autor pidió que diseñara el recorrido del Nivel 1 (N10) y el guion (N11) a partir de la tesis.
Son propuestas: no están en la escena ni en los assets hasta que las apruebe.

- **`Docs/Diseno/Nivel1_Diseno.md` y su esquema a escala (`Nivel1_Diseno.png`):**
  - la Ira empuja a correr, y cada peligro castiga la prisa;
  - cada mecánica aparece tres veces: sin riesgo, sobre lava y con enemigos;
  - los recuentos del Anexo A;
  - las medidas de Kael que limitan huecos y subidas;
  - tres cosas del Anexo A que no cuadran.
- **`Docs/Diseno/Guion.md`:** los seis mundos. Cada cierre nombra la habilidad como un gesto
  emocional: contener el golpe y soltarlo, avanzar bajo el agua, ocultarse sin perderse, resistir
  sin negar.

### Verificación

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»:**
  - importa la bola y crea su clip, su controlador y su prefab;
  - mete el segundo plano en la derrota;
  - añade «Caminar» a la guía.
- **Pruebas EditMode: 341 correctas.** Hay 5 nuevas:
  - la bola: su clip en bucle, su controlador de un estado, su lienzo y su pivote;
  - los patrones que no pintan su zona y los que sí;
  - una zona sin dibujo sigue haciendo daño;
  - Alt deja la dirección por debajo del umbral del sigilo;
  - la tecla de `Walk`.

  Se ponen al día las del verificador del Nivel 1 (34 sprites, 14 clips y 5 Animators), la del
  prefab de la bola y la de la cinemática de la derrota.
- **Pruebas PlayMode: 145 correctas.** La de la bola comprueba también que deja sus chispas, que
  no hacen daño y que se apagan solas.

---

## El build de Windows fallaba por las luces globales (2026-10-07)

- **Problema.** *Build* y *Build and Run* fallaban en medio segundo con cuatro errores «More than
  one global light on layer Default for light blend style index 0» y «Error building Player: 4
  errors».
- **Causa.** Es un fallo de URP 6000.6. Con la opción *Strip 2D Unused Variants* activada, URP
  abre cada escena del build para ver qué luces 2D usa. La abre **junto** a la escena que está
  abierta en el Editor. Las tres escenas (`Menu_Inicial`, `Niveles` y `Nivel1`) traen su propia
  Global Light 2D, como deben. Al juntarse dos, URP registra un error por cada escena, y esto
  ocurre dos veces. El botón de build del Editor compila en modo estricto, así que cualquier
  error lo aborta. Desde un script sin modo estricto, el build terminaba con los mismos 4
  errores.
- **Solución.** Desactivar *Strip 2D Unused Variants*: `m_Strip2DUnusedVariants: 0` en
  `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset` (*Project Settings → Graphics →
  URP → Shader Stripping*). URP deja de analizar las escenas y el build incluye las 64 variantes
  del shader de luces 2D: cuesta 1 MB (de 254 a 255 MB). No se tocan las escenas: cada una
  necesita su luz global, porque se cargan de una en una.
- **Verificación.** Build de Windows en modo estricto con `Menu_Inicial` abierta, como lo hace
  el Editor: antes, los 4 errores; después, 0 errores, 255 MB y 54 s. El ejecutable arranca sin
  excepciones.

---

## Guía de mecánicas en los menús (2026-10-07)

Rama `menu-mecanicas`, que sale de `cinematicas-pixel`. El autor pidió una sección en el menú que
explique lo que sabe hacer Kael y que se vaya completando según se desbloquean las habilidades.

### 1. Nada explicaba los controles ni las habilidades

- **Problema.** Lo único que el juego decía de los controles era la ayuda al pie de la pausa
  («↑↓ navegar · Enter seleccionar · Esc continuar»). Para saber que Shift es el dash, que se
  puede saltar desde una pared o qué hace la habilidad recién integrada, había que leer el código
  o el Anexo A.
- **Causa.** Las habilidades tenían nombre y descripción (`HabilidadData`), pero solo las nombraba
  «Yo reconstruido», sin decir cómo se usan. Los movimientos básicos no estaban descritos en
  ningún dato.
- **Solución.** Una pantalla nueva, **«Mecánicas»** (`PantallaMecanicas`), hecha con el mismo
  marco que la pausa y «Yo reconstruido». Se abre desde el **menú principal** (botón nuevo entre
  «Yo reconstruido» y «Salir») y desde la **pausa** (opción entre «Reiniciar» y «Menú
  principal»). Tiene dos columnas:
  - **Movimientos:** moverse, saltar, doble salto, pared, dash y atacar, cada uno con sus teclas
    y una frase. **Agacharse** solo aparece cuando se desbloquea un mundo que lo permite (la
    Culpa), porque en los demás la tecla no hace nada.
  - **Habilidades:** las cuatro de los fragmentos, en el orden de los mundos. La integrada se
    explica con el color de su emoción, sus teclas y el fragmento del que viene. La que falta se
    ve bloqueada («? ? ?»), con el mundo donde se consigue y sin decir cuál es, igual que «Yo
    reconstruido» no dice la habilidad de un fragmento que no tienes.

  Arriba, la cuenta: «1 de 4 habilidades integradas». La pantalla se recalcula cada vez que se
  abre, así que crece con la partida. Una habilidad cuenta como integrada si está en el progreso
  guardado o si el jugador vivo ya la tiene: tras derrotar al jefe, la guía ya la explica antes
  de salir del nivel.

### 2. Las teclas salen del asset de input, no del texto

- **Problema.** Si la guía escribiera «Shift» a mano, dejaría de ser verdad el día que cambie la
  tecla en `InputSystem_Actions`.
- **Solución.** Los textos nombran acciones entre llaves y `EtiquetasControles` escribe las
  teclas que tengan asignadas: `{Jump}` → «ESPACIO», `{Attack}` → «CLIC / ENTER», «Mantén
  {Attack}» → «Mantén CLIC / ENTER». En un compuesto se eligen las partes y sus teclas se
  emparejan: `{Move/left,right}` → «A D / ← →». Solo se muestran teclado y ratón, como en la
  ayuda de la pausa. Una prueba comprueba que cada acción nombrada existe y tiene tecla.

### 3. Los textos son datos

- **`GuiaMecanicas`** (`Assets/Data/Habilidades/GuiaMecanicas.asset`): los movimientos, con su
  nombre, sus teclas, su frase y cuándo aparecen (`SIEMPRE` o `MUNDO_CON_AGACHARSE`). Si falta el
  asset, la pantalla usa los mismos textos desde el código.
- **`HabilidadData.controles`**, un campo nuevo en cada habilidad: «Mantén {Attack}», «{Sprint} en
  el agua», «{Ability}» y «Pasiva».
- El generador crea la guía y completa ese campo en las habilidades que ya existían, sin pisar lo
  editado a mano. El prefab de sistemas lleva la guía (`UIManager.guiaMecanicas`), y la escena
  `Menu_Inicial` se regeneró con el botón (es una escena generada, sin cambios a mano).

### 4. Navegación

- Desde la pausa, la guía **tapa** el menú de pausa en vez de sumarse a él. «Volver» o Esc
  regresan a la pausa; otro Esc reanuda.
- Desde el menú principal, «Volver» o Esc la cierran.
- El juego sigue en PAUSED mientras está abierta: no hay estado nuevo en la FSM global.
- No se tocan `PlayerController`, `EnemyController` ni `GameManager`.

### Verificación

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** crea la guía, completa las teclas de las cuatro habilidades y monta el
  prefab de sistemas con la guía. La escena del menú se regeneró una vez con
  `GeneradorProgresion.RegenerarEscenasNavegacion`.
- **Capturas a 1920 × 1080,** revisadas a ojo, sin progreso y en la pausa del Nivel 1 con tres
  habilidades: las dos columnas caben dentro del marco y cada tecla cabe en una línea.
- **Pruebas EditMode: 336 correctas.** Hay 17 nuevas en `GuiaMecanicasTests`:
  - las teclas de cada acción, leídas del asset (9 casos, también una acción que no existe);
  - cada texto de la guía y de las habilidades nombra acciones que existen y tienen tecla;
  - sin progreso: los seis movimientos y las cuatro habilidades bloqueadas, con el mundo donde
    se consiguen;
  - con la Ira integrada, el Ataque Cargado con «Mantén CLIC / ENTER», su fragmento y su color;
  - una habilidad del jugador vivo también cuenta;
  - agacharse aparece al desbloquear el mundo de la Culpa, y dentro de él;
  - el generador deja la guía, las teclas de cada habilidad y la guía en el prefab de sistemas;
  - sin asset, los movimientos del diseño;
  - el menú principal tiene el botón.
- **Pruebas PlayMode: 145 correctas.** Hay 3 nuevas en `MenuMecanicasPlayModeTests`:
  - desde el menú: 0 de 4 habilidades, y 2 de 4 al volver a abrirla con dos integradas;
  - desde la pausa: tapa el menú, sigue en pausa con el color del mundo, «Volver» y Esc regresan a
    la pausa, y otro Esc reanuda;
  - al derrotar al jefe, la guía ya explica el Ataque Cargado.

### Qué probar a mano

- En `Menu_Inicial`, pulsa «Mecánicas»: con una partida nueva verás los seis movimientos y las
  cuatro habilidades bloqueadas.
- En el Nivel 1, pausa con Esc y entra en «Mecánicas». Derrota a la Centinela, vuelve a pausar y
  mira cómo aparece el Ataque Cargado.
- Los textos se cambian en `Assets/Data/Habilidades/GuiaMecanicas.asset` (movimientos) y en cada
  habilidad de esa carpeta (campos `descripcion` y `controles`).

---

## Cinemáticas en pixel art: la cámara y el reproductor (2026-10-06)

Rama `cinematicas-pixel`, que sale de `kael-dash`. Llegó un encargo para montar el sistema de
cinemáticas y, con él, la entrada del jefe del Nivel 1 («El que ya estaba aquí», 9,6 s). Trae su
guion, `Tesis/Imagenes/limpios/GUION-cinematica-jefe1.md`, y cinco fases:

- A: cámara y render;
- B: el sistema;
- C: la cinemática;
- D: integración;
- E: verificación.

El encargo estaba escrito para otro proyecto. Antes de empezar, el autor aceptó estas
adaptaciones:

### Decisiones que ajustan el encargo

1. **Un solo sistema de cinemáticas.** Ya había uno dirigido por datos: `CutsceneData` y
   `PantallaCinematica`, con 14 cinemáticas, y la del jefe ya estaba montada en él. Se amplía con
   el modelo de beats del encargo en vez de escribir un segundo reproductor. Los planos en pixel
   art se dibujan con una cámara propia; los pintados (la cinemática inicial, de 1376 × 768) siguen
   como estaban.
2. **La Pixel Perfect Camera de URP.** El proyecto está en Unity 6 con URP 2D, que trae la suya; el
   paquete `2d.pixel-perfect` del encargo no se usa. El recorte «16:9» es *Windowbox*.
3. **El input lo sigue bloqueando la máquina de estados global**, como en todas las cinemáticas.
   Para que Kael no se mueva durante el rugido, la arena mantendrá el bloqueo hasta que acabe
   (fase D).
4. **La marca de «ya vista» ya está en `GameManager`** y se guarda en la partida. La cinemática
   será «solo la primera vez».
5. **El texto del beat 2 entra a 3,5 s y sale a 5,5 s** desde el inicio del beat, con 0,4 s de
   fundido. El encargo decía 2,5 y 4,5, pero los temblores y el rótulo cuentan desde el inicio del
   beat con el fundido incluido; con ese criterio, 3,5 y 5,5 son los tiempos del guion.
6. **Varios cues por beat**, cada uno con su instante y su bucle. Con uno solo no cabe el sonido
   del guion.
7. **Falta la fuente bitmap.** Al final no hizo falta esperarla: el autor eligió el texto como en
   la cinemática inicial (fase C). Con una fuente bitmap, pasa solo a la rejilla.
8. **No se puede saltar el primer medio segundo** (el negro inicial). Así una tecla pulsada al
   entrar en la sala no se lleva el rótulo, que es lo que importa para el estudio.
9. **Rutas y menús de la v2:** *Fragmentum → …* y `Assets/Data/Narrativa`; los planos siguen en
   `Assets/Sprites/Cinematicas/Nivel1`.

### Fase A: cámara y render

- **`CamaraCinematica`.** Es una cámara propia, aparte de la del juego, con la Pixel Perfect
  Camera de URP:
  - **resolución de referencia 240 × 135** y *Assets PPU* 1;
  - ***Upscale Render Texture*:** dibuja en una textura de exactamente 240 × 135 y la amplía por
    el mayor entero que cabe;
  - **recorte *Windowbox*:** centra la imagen y pone negro lo que sobra;
  - **filtro final Point.**

  Vive lejos de los niveles y en su propia capa, `Cinematica` (nueva). La cámara del juego no la
  ve y ella solo ve lo suyo. Se dibuja encima y limpia la pantalla a negro, así que no hace falta
  apagar la cámara del juego ni pelearse con su seguimiento.
- **Los planos del Nivel 1 van a 1 píxel por unidad** (`TablaArte`), como la cámara: un píxel del
  plano es una unidad del mundo. Lo que se mueva en la cinemática (el encuadre, el temblor, las
  brasas) se moverá en unidades enteras y no saldrá de la rejilla.
- **El prefab** `Assets/Prefabs/Cinematicas/CamaraCinematica.prefab` se genera con el mismo código
  que la monta en el juego (`ConstructorCamaraCinematica`, también en «Regenerar todo»).
- **La captura:** *Fragmentum → Cinemáticas → Capturar la cámara de cinemáticas a 1920 × 1080*.
  - Dibuja el plano con la cámara en una escena de vista previa (no toca la escena abierta) y
    guarda la imagen en `Docs/Cinematicas`.
  - Después la compara píxel a píxel con el PNG: cada píxel del plano tiene que ser un cuadrado de
    k × k píxeles idénticos al original, y lo que sobra, negro.

#### Comprobado (fase A)

- **A 1920 × 1080** (`Docs/Cinematicas/CamaraCinematica_1920x1080.png`), cada píxel del plano es un
  cuadrado de 8 × 8 píxeles idénticos al original. La diferencia máxima es 0 y la captura tiene los
  mismos 1528 colores que el PNG: ni uno nuevo, que es lo que dejaría una interpolación.
  `CamaraCinematica_1920x1080_detalle.png` amplía la zona de Kael con una rejilla cada 8 píxeles:
  cada celda es un solo color.
- **A 1280 × 720**, cuadrados de 5 × 5, centrados, con un borde negro de 40 y 22 píxeles.
- **A 960 × 540**, de 4 × 4.
- Las tres capturas se comprobaron también aparte, con Python, contra el PNG.

### Fase B: el sistema

El sistema de cinemáticas que ya había (`CutsceneData`, `LineaTiempoCinematica`,
`PantallaCinematica`) se amplía; no hay un segundo reproductor. Las 14 cinemáticas de antes siguen
funcionando igual.

- **El montaje por beats** (`CutsceneData.montaje = BEATS`). Cada diapositiva es un beat:
  - sube desde negro, se mantiene y baja a negro con sus propios tiempos;
  - un fundido de 0 es un corte seco;
  - un beat sin imagen es negro a propósito: no avisa de que le falte arte;
  - la cinemática dura la suma de los tres tiempos de cada beat;
  - los textos, los temblores y los cues cuentan desde el inicio del beat, con la subida incluida.

  Con fundidos cruzados (el montaje de antes) nada cambia. Los campos de la tabla del encargo
  están así en el proyecto:

| Encargo | En el proyecto | Notas |
|---|---|---|
| `sprite` | `imagen` | Vacía: negro |
| `fundidoEntrada`, `duracion`, `fundidoSalida` | Igual | 0 = corte seco |
| `desplazamiento` | Igual (`Vector2Int`) | En píxeles del plano: x a la derecha, y arriba |
| `texto`, `textoPosicion`, `textoAlineacion`, `textoEntra`, `textoSale` | `carteles` | Cada cartel lleva su texto, `inicio`, `duracion`, `punto` (píxel del plano, desde arriba a la izquierda, como en el guion) y `alineacion`; un beat puede tener varios |
| `cueAudio` | `cues` | Lista: nombre, clip, instante, bucle y `hasta` (corte seco) |
| `temblores` | Igual | Segundos desde el inicio del beat |
| `brasas` | Igual | La capa llega en la fase C |
| `flashFinal` | Igual | Fotogramas, no segundos |

- **La fuente y el temblor** son de la cinemática: `fuente`, `tamanoFuente`, `interlineado`,
  `colorTexto`, `temblorPixeles` (1) y `temblorFotogramas` (4).
- **En pixel art** (`escalaEntera`), el reproductor lo dibuja todo con la cámara de cinemáticas:
  - **los planos,** con su opacidad y su encuadre en píxeles enteros. Para los fundidos cruzados
    hay dos planos;
  - **el texto** (`TextoEnRejilla`, nuevo). Cada glifo se coloca en píxeles enteros del plano, con
    los píxeles de la fuente del tamaño de los del plano, así que se amplía con la imagen por el
    mismo entero. Monta su propia malla, sin `TextMesh` ni interfaz. Sin fuente bitmap usa la de
    por defecto y avisa (N21);
  - **el temblor:** la cámara se desplaza un píxel entero por fotograma, recorriendo las cuatro
    direcciones, y vuelve a su sitio;
  - **el flash:** un rectángulo blanco sobre la rejilla. Los fotogramas se cuentan en el `Update`
    del reproductor, no con `WaitForSeconds`: dos son dos, también a 30 fps.

  La interfaz se queda transparente y solo deja el aviso para saltar. Las cinemáticas pintadas
  siguen en la interfaz, como antes.
- **Sonido:** cada cue suena en su instante y se corta en seco en `hasta`, aunque sea en otro beat.
  Si falta su clip, avisa por consola con su nombre, su diapositiva y su segundo, y la cinemática
  sigue sin él. No hay audio de relleno.
- **Saltar:** pasado el bloqueo, cualquier tecla corta la cinemática y va directo al flash final,
  entero. Sin flash, termina como antes.
- **Input:** lo sigue bloqueando la FSM global mientras dura (estado CUTSCENE). Al terminar, lo
  devuelve.
- **La cámara la monta el `UIManager`,** con los sistemas persistentes. Usa el prefab, que
  `Sistemas.prefab` referencia; si no está, la monta por código. La escala entera que hacía la
  interfaz se quita: ahora la hace la cámara.
- **Las dos cinemáticas del Nivel 1 que ya eran pixel art** (la presentación y la derrota) se ven
  ya con la cámara. Sus textos van con la fuente por defecto hasta que llegue la bitmap.

### Fase C: la cinemática del jefe

- **El texto, como en la cinemática inicial** (decisión del autor; ajusta la 7). No hacía falta
  esperar a la fuente bitmap:
  - **sin fuente bitmap** (`CutsceneData.fuente` vacía), el texto va en la interfaz, con la fuente
    del juego y la sombra de la cinemática inicial;
  - **pero a la escala de la imagen:** la letra mide `tamanoFuente` píxeles del plano (5, que son
    40 px a 1080p, como en la inicial; el título, el doble) y se ancla en su píxel del plano, en
    múltiplos de la escala;
  - **con una fuente bitmap** en ese campo, el mismo texto pasa solo a la rejilla
    (`TextoEnRejilla`).
- **El texto sigue al encuadre de su plano.** El punto de un cartel es un píxel de la imagen y se
  mueve con su desplazamiento. Así el rótulo cae bajo el ojo con (17, 19) y también con (0, 0).
- **La cinemática** es `Cinematica_Jefe_Ira`, la «Cine01_EntradaJefe» del encargo. Conserva el
  nombre porque el Nivel 1 y el generador de narrativa ya la usan. `ConstructorCinematicasNivel1`
  la escribe desde el guion al regenerar, como la cinemática inicial. Conserva los clips de sonido y
  la fuente que se pongan a mano.

| Beat | Imagen | Subida · se mantiene · bajada | Lo demás |
|---|---|---|---|
| 1 | Negro | 0,5 s | `drone_sala` en bucle, hasta los 9,6 s |
| 2 | `CinematicaJefe1` | 1,0 · 4,9 · 0 (corte seco) | Ver abajo |
| 3 | Negro | 0,5 s | Silencio |
| 4 | `CinematicaJefe2` | 1,1 · 1,6 · 0 | Ver abajo |

- **Beat 2, la sala:**
  - brasas;
  - temblores de un píxel a los 1,9 y 3,1 s del beat (2,4 y 3,6 s de la cinemática);
  - el texto de Kael, de los 4,0 a los 6,4 s, con fundidos de 0,4 s;
  - cues: `amb_forja` (corte seco a los 6,4 s), `golpe_escala` (1,6 s), `respiracion_fuelle`
    (en bucle de los 2,0 a los 6,0 s) y `voz_kael_01` (4,0 s).
- **Beat 4, el ojo:**
  - encuadrado (17, 19);
  - el rótulo entra en seco a los 8,0 s y se queda hasta el final;
  - flash de 2 fotogramas al terminar;
  - cues: `brasa_aviva` (de los 6,9 a los 8,0 s) y `rotulo_metal` (8,0 s).

- **Los textos,** medidos sobre los planos:
  - **Kael:** «No recuerdo esta sala. / El calor, sí.», las dos líneas juntas, en la columna 22 y
    la fila 113. Kael ocupa las columnas 22 a 25 y pisa en la fila 109: el texto queda alineado
    bajo su silueta;
  - **el rótulo:** «CENTINELA ARDIENTE» (título) y «Arquitecto de la Ira», centrados bajo el ojo.
    En la imagen el ojo ocupa las columnas 73 a 155 y llega a la fila 67; el rótulo va en la fila
    77 y el subtítulo en la 90. Entra en seco (cartel `sinFundido`, nuevo), con el golpe
    metálico.
- **La capa de brasas** (`CapaBrasas`, en la cámara de cinemáticas):
  - son 16 puntos de un píxel del plano, no un dibujo;
  - caen a entre 3 y 8 px/s, de píxel entero en píxel entero, con una deriva lateral de hasta
    1,5 px/s;
  - cambian de brillo a saltos cada 0,15 s, con tres colores de brasa;
  - se apagan en el suelo de la sala (fila 110) y vuelven a caer desde arriba;
  - su opacidad es la del plano: suben desde negro y cortan con él;
  - dependen solo del tiempo y de una semilla, así que la cinemática se ve igual cada vez. Sus
    valores se ajustan en el prefab.
- **Una vez por partida** (`soloPrimeraVez`, se guarda en `GameManager`).
- **El bloqueo para saltar es de medio segundo**, el negro inicial.
- **Sin el aviso «pulsa cualquier tecla»** encima de los planos. Invitaría a saltarse la primera
  vista, y el rótulo es lo que importa para el estudio. Se vuelve a poner en el asset
  (`avisoSaltar`) si se quiere.
- **`rugido_centinela`** no está en la cinemática: suena con el rugido del jefe, ya en el juego
  (fase D). Si se pusiera en la cinemática, el flash la cortaría a los dos fotogramas.
- **La captura:** *Fragmentum → Cinemáticas → Capturar la presentación del jefe del Nivel 1* la
  fotografía como se ve en el juego, con el texto de la interfaz encima, a 1920 × 1080:
  - a los 1,0, 4,5, 8,5 y 9,55 s;
  - y el ojo sin el encuadre, para compararlo.

  Las capturas se guardan en `Docs/Cinematicas/Presentacion_*.png`.

#### Lo que no cuadra con el guion: el hueco del encuadre se ve

El guion dice que, al desplazar el plano del ojo (17, 19), «el relleno que queda es negro, y el
plano ya es negro», así que no se ve. Pero el fondo del plano no es negro: es un azul casi negro,
RGB (16, 17, 22), y no tiene ni un píxel negro puro. Por eso se ven una franja de 17 píxeles a la
izquierda y otra de 19 abajo, de negro (0, 0, 0). En `Presentacion_8.5s.png` se nota; en
`Presentacion_8.5s_sin_encuadre.png` el plano llena la pantalla.

Se deja como dice el guion, con el desplazamiento en el asset para compararlo con (0, 0). No se
rellena nada: sería pintar sobre el arte (PENDIENTES N22).

### Verificación (fase A)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **Pruebas EditMode: 303 correctas.** Hay 7 nuevas en `CamaraCinematicaTests`:
  - la cámara lleva la Pixel Perfect Camera configurada como arriba y solo ve su capa;
  - los tres planos van a 1 píxel por unidad y miden 240 × 135 unidades;
  - el rectángulo en pantalla es k veces la referencia, centrado (también en 16:10);
  - se captura cada plano a 1920 × 1080, 1280 × 720 y 960 × 540: cada píxel es un cuadrado de
    k × k idéntico al original;
  - la comprobación detecta un plano ampliado con filtro bilineal.

  La de los píxeles por unidad del Nivel 1 espera ahora 1 en los planos.
- **Pruebas PlayMode: 135 correctas.** No cambian.

### Verificación (fase B)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **Pruebas EditMode: 310 correctas.** Hay 7 nuevas en `CinematicaBeatsTests`, con los tiempos del
  guion:
  - la cinemática dura 9,6 s, el corte seco cae a los 6,4 s y el flash no cuenta en la duración;
  - un beat sube desde negro, se mantiene y, con fundido 0, corta en seco;
  - un beat sin imagen es negro a propósito, sin el aviso de arte pendiente (con fundidos
    cruzados sigue avisando);
  - el texto entra a los 4,0 s, los temblores caen a los 2,4 y 3,6 s, y los cues empiezan y se
    cortan donde dice el guion;
  - el flash son 2 fotogramas al final del último beat, y saltar va a él;
  - el texto en la rejilla cae en píxeles enteros del plano y se ancla donde dice el cartel;
  - los bordes del plano coinciden con los de la rejilla.
- **Pruebas PlayMode: 140 correctas.** Hay 5 nuevas en `CinematicaRejillaPlayModeTests`, con los
  sistemas reales:
  - una cinemática en pixel art la dibuja la cámara, a la escala entera de la pantalla, y la
    interfaz no tapa nada;
  - por beats: el plano sube desde negro, corta en seco a negro, tiembla un píxel entero, el
    segundo plano va encuadrado (17, 19) y el flash dura exactamente 2 fotogramas. Mientras dura,
    Kael no recibe input; al acabar, sí;
  - al saltarla, va directo al flash final, de 2 fotogramas;
  - un cue sin clip avisa con su nombre y la cinemática sigue;
  - el texto se escribe en su píxel del plano.

  Se corrige una prueba de la torreta de la fase D que a veces fallaba. Medía el tiempo sin quemar
  desde que aparecía Kael, pero el ciclo de la torreta empieza al crearla; ahora espera a su
  disparo.

### Verificación (fase C)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** escribe la presentación desde el guion y rehace la cámara con su capa de
  brasas.
- **Las capturas** (`Docs/Cinematicas/Presentacion_*.png`) se revisaron a ojo:
  - las brasas son cuadrados de 8 × 8, en la rejilla;
  - el texto de Kael queda bajo su silueta, legible sobre el suelo;
  - el rótulo, centrado bajo el ojo, con y sin encuadre.
- **Pruebas EditMode: 319 correctas.** Hay 9 nuevas en `CinematicaJefe1Tests`, con los tiempos copiados
  del guion:
  - cuatro beats que suman 9,6 s (± 1 ms), con sus fundidos y sus cortes secos;
  - brasas solo en la sala, y los dos temblores de un píxel a los 2,4 y 3,6 s;
  - el texto de Kael de 4,0 a 6,4 s, bajo su silueta;
  - el ojo encuadrado (17, 19), el rótulo en seco de los 8,0 s al final, sin tocar el ojo, y el
    flash de 2 fotogramas;
  - los siete cues, donde dice el guion;
  - una vez por partida, con el bloqueo de medio segundo y sin aviso, y es la del Nivel 1;
  - regenerar conserva los clips puestos a mano;
  - las brasas ocupan píxeles enteros y caen de uno en uno;
  - el texto se mueve con el encuadre de su plano.

  Se adaptan tres pruebas de la presentación anterior. Esperaban el diálogo provisional de la
  Centinela y que se viera cada vez que se carga el nivel.
- **Pruebas PlayMode: 142 correctas.** Hay 2 nuevas en `CinematicaRejillaPlayModeTests`:
  - sin fuente bitmap, el texto va en la interfaz, anclado en su píxel del plano y con la letra a
    la escala de la imagen;
  - la presentación entera: brasas en la sala y no en el ojo, el texto de Kael, el rótulo, el ojo
    encuadrado y el flash de 2 fotogramas. Cada cue que aún no tiene clip avisa con su nombre.

  La del texto en la rejilla lleva ahora una fuente. La de la presentación en
  `NarrativaPlayModeTests` busca el texto de Kael.

---

## Dash de Kael (2026-10-06)

Rama `kael-dash`, que sale de `golem-mecanicas`. Llegó el dash de Kael, dibujado en Pixelorama
(`Tesis/Imagenes Kael/Dash`): 4 fotogramas en un lienzo de 120×120.

### 1. El dash se veía como un fotograma de correr

- **Problema.** Kael hacía el dash con el clip de correr, que se quedaba casi fijo durante los
  0,18 s del dash (PENDIENTES B3).
- **Causa.** No había clip de dash.
- **Solución.** Se sigue el procedimiento de «Arte de Kael» (*Pasos manuales en el Editor*):
  - **La hoja.** `Dash.png` venía exportada a 5× (2400×600). `Herramientas/reducir_hoja.py` la
    reduce sin pérdida a 1×, de 480×120, en `Assets/Sprites/Kael/Kael_Dash.png`. Cada bloque de
    5×5 píxeles era idéntico, así que no se pierde nada. No se recorta ni se recoloca nada.
  - **`ConstructorArteKael`** recorta la hoja en sus 4 fotogramas y crea el clip `Kael_Dash`.
    El pivote está en la planta de los pies y en la x 42, donde cae el torso de los dos primeros
    fotogramas, como en el idle. Así, pasar de correr al dash no desplaza a Kael. El tercer
    fotograma se lanza hacia delante con la estela detrás, tal como está dibujado.
  - **`PlayerAnimacion`** reproduce `Dash` en el estado DASH, desde su primer fotograma.

### 2. Que el dash dure lo que dura el dash

- **El clip dura 1 s y se estira a la duración del dash,** igual que el ataque. El parámetro
  nuevo `VelocidadDash` vale 1 / `dashDuration`. Con los 0,18 s de ahora, cada fotograma se ve
  0,045 s; si se cambia `dashDuration` en `ParametrosJugador`, la animación lo sigue sola.
- **Los cuatro fotogramas duran lo mismo,** como en el proyecto de Pixelorama (a 6 fps, todos
  con duración 1):
  - el arranque agachado;
  - el impulso;
  - la estela;
  - la frenada.

  No me inventé otro reparto. Si quieres que la estela se vea más, dime cuánto y lo cambio en la
  tabla (`ConstructorArteKael.Clips`).
- Si el dash durara más que el clip, se queda en la frenada.

### Verificación

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** importa la hoja, crea el clip y el estado `Dash` (con su parámetro de
  velocidad) y rehace el prefab del jugador con la tabla nueva.
- **Pruebas EditMode: 296 correctas.**
  - Las de `ArteKaelTests` ya recorren la hoja nueva: se recorta a 64 PPU con filtro Point, Kael
    mide lo que su collider (1,61 u agachado en el primer fotograma) y los pies caen en el
    pivote.
  - Hay 2 nuevas:
    - el clip dura 1 s, sin bucle, con los cuatro fotogramas a 0,25 s, y el estado DASH lo usa;
    - la hoja está a 1× (fotogramas de 120×120) y el pivote, en los pies y en la x 42.
  - La del controlador comprueba también `VelocidadDash`.
- **Pruebas PlayMode: 135 correctas.** La del dash se reescribe
  (`Dash_ReproduceSuClip_EstiradoALaDuracionDelDash`):
  - al hacer el dash suena su clip, con la velocidad de 1 / `dashDuration`;
  - mientras dura solo se ven sus fotogramas, y al acabar va por la segunda mitad;
  - después vuelve a correr.

### Qué probar a mano (`Assets/Scenes/Pruebas/PruebaJugador.unity` o el Nivel 1)

- Corre y pulsa Shift: el dash se ve agacharse, lanzarse con la estela y frenar. Kael no salta
  de sitio al empezar ni al acabar.
- Hacia la izquierda, el sprite se voltea igual que al correr.
- En el aire, el dash usa el mismo clip.

---

## Golem de Ceniza, mecánicas y cinemáticas del Nivel 1 (2026-10-06)

Rama `golem-mecanicas`, que sale de `centinela-arte`. El autor entregó 30 PNG limpios en
`Tesis/Imagenes/limpios`, con su LEEME:

- 15 fotogramas del Golem de Ceniza;
- 12 de mecánicas: la lava (y su espejo), la torreta (3), la plataforma inestable (4) y la
  plataforma de combustión (3);
- 3 planos de cinemática del Nivel 1.

Con el arte llegó un encargo en cinco fases, parando al final de cada una:

- A: importación;
- B: clips;
- C: Animators;
- D: controladores y prefabs;
- E: verificación.

### Decisiones que ajustan el encargo

- **Problema.** Como el de la Centinela, el encargo estaba escrito en parte sobre la v1:
  `playerScript.takeDamage`, el tag `"Weapon"`, un `GolemController` nuevo, `Assets/Editor` y
  menús en `Tools/`. Además, al medir el arte y leer el proyecto aparecieron seis cosas que no
  cuadraban.
- **Solución.** Se le preguntaron al autor, que aceptó las seis recomendaciones:
  1. **La torreta disparando está dibujada 15 px más a la izquierda** que en reposo y en carga:
     el bloque ocupa x 1–45 en vez de 16–61 (se midió comparando los ladrillos). Su pivote X
     pasa de 0,470 a 0,287 y el bloque no salta al disparar. El PNG no se toca.
  2. **La lava sigue matando**, como dice el Anexo A. El encargo pedía daño continuo «igual que
     `DeadZone`», pero `DeadZone` mata, en la v1 y en la v2.
  3. **La plataforma de combustión pasa a un ciclo fijo** (apagada → calienta → encendida) que no
     depende del jugador, como pide el encargo: la inestable se resuelve moviéndose rápido y esta,
     esperando. El Anexo A (quema si te quedas encima 2 s) queda desactualizado; lo corrige el
     autor en el documento.
  4. **El Golem aguanta los golpes sin interrumpirse:** destella, pero sigue con su ataque. No
     tiene animación de daño, y con 10 de vida, si cada golpe lo aturdiera, apenas llegaría a
     atacar. Necesita un cambio mínimo en `EnemyController` (fase D).
  5. **El destello del golpe es la silueta blanca**, como en el Eco y la Centinela.
     `SpriteRenderer.color` solo multiplica: no puede aclarar el gris del Golem.
  6. **`CinematicaFinalLvl1` sale cuando D4 termina de caer.** Después se vuelve al juego para
     que Kael recoja el fragmento.

  Y, como con la Centinela:
  - las convenciones de la v2: el editor en `Assets/Scripts/Editor`, los clips en
    `Assets/Animaciones`, los menús en `Fragmentum/…` y la verificación como menú y pruebas;
  - el daño de la v2: `Hitbox`, `Golpe` e `IGolpeable`;
  - el Golem sigue siendo `EnemyController` con su `EnemyData`, vestido como el Eco con
    `EnemigoAnimacion`. Lo de hacia dónde mira ya lo resuelve `EnemyData.spriteMiraDerecha`;
  - `Lava`, `TorretaFuego` y `PlataformaInestable` se amplían en vez de escribirse de nuevo.
- **Una errata del encargo.** 240×135 no escala a 1280×720 sin interpolar: sería ×5,33. Ahí la
  escala entera es ×5 (1200×675), con un borde negro fino.

### Fase A: importación

- **Los sprites.** Los 30 PNG se copian sin tocar; son idénticos byte a byte a los del arte:
  - `Assets/Sprites/Enemigos/GolemCeniza`, los 15 del Golem;
  - `Assets/Sprites/Mecanicas`, los 12 de las mecánicas;
  - `Assets/Sprites/Cinematicas/Nivel1`, los 3 planos.
- **Un solo importador, con una tabla.**
  - **Problema.** Había tres postprocessors: `ImportadorJefes`, `ImportadorEnemigos` e
    `ImportadorFondos`. El de enemigos repetía, copiados, los ajustes de pixel art de los jefes.
    Añadir el Golem y las mecánicas habría sumado otro sitio donde se decide lo mismo.
  - **Solución.** `ImportadorArte` (antes `ImportadorJefes`) es el único. Los ajustes salen de
    `TablaArte` (antes `ArteJefes`): una fila por carpeta o grupo de archivos, con sus píxeles por
    unidad y su pivote, y gana la más concreta.
  - La Centinela y el Eco pasan a la tabla con las constantes de sus constructores; ninguno de
    sus ajustes cambia, y una prueba lo comprueba. Los fondos, que se ajustan por capa, entran
    por el mismo importador y siguen ajustándose en su constructor. Kael y la cinemática inicial
    no tienen regla: los ajusta su constructor.
  - «Regenerar todo» reaplica la tabla al empezar (`TablaArte.AplicarATodo`), por si algo se
    importó antes de tener regla.
- **La escala.** Un píxel de arte mide lo mismo en todo el juego, y la Centinela fijó la
  referencia (49 px por unidad). Por eso los píxeles por unidad cambian por carpeta:

  | Archivos | Px por unidad | Pivote | Tamaño |
  |---|---|---|---|
  | `GolemCeniza/Golem_*` | 24 | (0,443; 0,043), los pies | 2,7 u de alto |
  | `Mecanicas/Torreta_*` | 24 | (0,470; 0,048), la base del bloque | 1,9 × 1,7 u |
  | `Mecanicas/Torreta_3_disparo` | 24 | (0,287; 0,048), corrido 15 px | igual |
  | `Mecanicas/Lava*` | 48 | (0; 1), la costra | 3,15 × 1,33 u |
  | `Mecanicas/Plataforma_*` | 48 | (0,5; 0,852), la superficie | 3,83 u de ancho |
  | `Mecanicas/Combustion_*` | 48 | (0,5; 0,708), la superficie | 3,42 u de ancho |
  | `Cinematicas/Nivel1/*` | — | el centro | 240 × 135 px |

  Todos: sprite único, Point, sin compresión, Full Rect, sin mipmaps y Clamp.
- **Los pivotes caen donde se pisa**, no en el centro: una plataforma se coloca poniendo su
  Transform justo donde el jugador apoya el pie. Las pruebas lo comprueban contra los píxeles.

### Notas sobre el arte (no se ha tocado)

- **No hay A2 del Golem:** el ataque va de A1 a A3, y funciona así. El LEEME trae el prompt para
  rehacerlo.
- **Falta la bola de fuego** (Fireball3 está por regenerar): el proyectil de la torreta llevará
  un sprite provisional. **Falta `CinematicaFinalLvl2`**, el segundo plano del final; el LEEME
  trae su prompt.
- **El pivote del Golem y el de la torreta** caen en el borde de arriba de su fila más baja, como
  el de la Centinela: la última fila de píxeles se hunde 1 px de arte en el suelo, que es lo que
  hace que pise.

### Fase B: clips

- **El menú** *Fragmentum → Niveles → Generar los clips del Golem y de las mecánicas*
  (`ArteNivel1`) construye los clips desde dos tablas: `ConstructorGolem.Clips` y
  `ConstructorMecanicas.Mecanicas`. «Regenerar todo» también lo hace, antes de los prefabs que
  los usarán.
- **Un solo constructor de clips**, `ClipsDeSprites`, con el criterio de la Centinela:
  - una clave por fotograma y una de cierre, para que el clip dure exactamente lo de la tabla;
  - el fotograma que dura 1/60 s se queda en pantalla al acabar el clip;
  - si el clip ya existe, se rehace sobre el mismo asset y conserva su identificador.
- **El Golem** (`Assets/Animaciones/Enemigos/GolemCeniza`), con los tiempos del encargo:

  | Clip | Fotogramas y duración (s) | Bucle |
  |---|---|---|
  | `Golem_Idle` | I1 0,40 · I2 0,40 · I3 0,40 · I2 0,40 | sí |
  | `Golem_Andar` | W1 0,13 · W2 0,13 · W3 0,13 · W4 0,13 | sí |
  | `Golem_Ataque` | **A1 0,50** · **A3 0,08** · A4 0,14 · A5 0,26 | no |
  | `Golem_Muerte` | D1 0,35 · D2 0,45 · D3 0,50 · D4 se queda | no |

  - **El ataque no se uniforma:** A1, medio segundo con los puños en alto, es la ventana para
    reaccionar, y A3, el impacto, dura 0,08 s. Sin A2: el salto de A1 a A3 es el golpe.
- **Las mecánicas** (`Assets/Animaciones/Mecanicas`): un clip por estado.
  - **Problema.** El encargo no da tiempos por fotograma para las mecánicas: sus tiempos son los
    del script, públicos para ajustar la dificultad (cuánto avisa la torreta, cuánto aguanta la
    plataforma, el ciclo de la combustión). Si estuvieran dentro del clip, cambiarlos en el
    inspector descuadraría el dibujo.
  - **Solución.** Cada estado es un fotograma que se queda, y el script decide cuánto dura:

    | Mecánica | Estados (clip) |
    |---|---|
    | Torreta | `Torreta_Reposo` · `Torreta_Carga` · `Torreta_Disparo` |
    | Plataforma inestable | `Plataforma_Intacta` · `Plataforma_Agrietada` · `Plataforma_Rompe` |
    | Combustión | `Combustion_Apagada` · `Combustion_Calienta` · `Combustion_Encendida` |

  - **La rotura es el único clip de dos fotogramas:** se ve partida en tres trozos 0,25 s
    (`Plataforma_3_rota`) y después el polvo, que se queda hasta que el script la esconde.
  - **La lava no tiene clip:** es un solo fotograma, que se repite alternando con su espejo.
- **Para la fase C.** Una vuelta del andar del Golem dura 0,52 s; para que los pies no patinen,
  el ritmo del clip seguirá la velocidad real, como en el Eco.

### Fase C: Animators

- **El menú** pasa a ser *Fragmentum → Niveles → Generar los clips y los Animators del Golem y
  de las mecánicas*. Los controladores los arma `ControladoresAnimator`, que reutiliza estados y
  transiciones por nombre: regenerar no cambia los archivos.
- **El Golem** (`GolemCeniza.controller`): cuatro estados, movidos por los parámetros que ya
  escribe la FSM del enemigo (`MovEn`, `Attack`, `Death`) y por el ritmo de andar que escribe
  `EnemigoAnimacion` (`VelocidadCorrer`):

  | Desde | Hacia | Cuándo |
  |---|---|---|
  | Idle | Ataque | `Attack` («Ya a tiro»: la FSM puede llegar a ATTACK sin que se vea `MovEn`) |
  | Idle | Andar | `MovEn` y ritmo > 0,15 |
  | Andar | Ataque | `Attack` (va primero) |
  | Andar | Idle | pierde al jugador, o ritmo < 0,05 (se para) |
  | Ataque | Idle | al acabar el clip, o si la FSM corta el ataque |
  | Cualquiera | Muerte | `Death`; sin salida: se queda en D4 |

  - **Problema.** El andar no puede seguir la zancada del dibujo, como en el Eco: el pie de atrás
    no se mueve en los cuatro fotogramas (columnas 21–33), así que no hay zancada que medir.
  - **Solución.** La referencia es su velocidad persiguiendo, 2 u/s
    (`ConstructorGolem.VelocidadNaturalAndar`): persiguiendo anda a los tiempos de la tabla
    (0,13 s), patrullando a 1 u/s a la mitad de ritmo, y parado respira en Idle. Entre pararse
    (ritmo < 0,05) y echar a andar (> 0,15) hay margen, para que no parpadee entre los dos.
    `EnemigoAnimacion` recibe el valor en la fase D.
  - **Sin estado de daño:** el Golem no tiene animación de daño ni se aturde (decisiones 4 y 5).
    `Damage` se declara porque la FSM lo apaga al morir.
- **Las mecánicas** (`Torreta.controller`, `Plataforma.controller` y `Combustion.controller`):
  un estado por clip y un solo parámetro, el entero `Estado`, que pone su script.
  - Cada valor es el de un enum nuevo del juego, en `Levels/Mecanicas/EstadosMecanicas.cs`:
    `ESTADOS_TORRETA` (reposo, carga, disparo), `ESTADOS_PLATAFORMA` (intacta, agrietada, rota)
    y `ESTADOS_COMBUSTION` (apagada, calienta, encendida). El editor comprueba que haya un clip
    por valor y en su orden.
  - Desde cualquier estado se salta al del valor pedido, al instante y nunca a sí mismo; ningún
    estado cambia solo. Ningún parámetro viene del jugador.
  - **Por qué no una cadena** reposo → carga → disparo: el script puede saltar (por ejemplo, volver
    a reposo cuando el nivel se reinicia), y una cadena dejaría el Animator atascado si se salta
    un paso. El ciclo y sus tiempos viven en el script.
  - `AnimatorParams` gana `EstadoMecanica` y `FijarEntero`.

### Fase D: scripts y prefabs

- **El menú** pasa a ser *Fragmentum → Niveles → Generar el arte del Golem, las mecánicas y las
  cinemáticas del Nivel 1* (`ArteNivel1`). En «Regenerar todo» va después del Eco, porque el
  Golem usa su material de destello, y antes que los prefabs de las mecánicas.

#### El Golem de Ceniza (`Prefabs/Enemigos/GolemCeniza.prefab`, `ConstructorGolem`)

- **Problema.** El encargo pedía un `GolemController` nuevo, con daño por `takeDamage`, el tag
  `"Weapon"` y el signo de giro al revés que el Eco.
- **Solución.** Es el mismo `EnemyController` que los demás, vestido como el Eco con
  `EnemigoAnimacion`. Ya hace lo que pide el encargo: patrulla, persigue, se para a distancia de
  ataque, golpea con el ciclo entero, espera y repite. Lo que el encargo pedía exponer está en su
  asset (`GolemCeniza.asset`): vida máxima 10, velocidad 1 patrullando y 2 persiguiendo, rango de
  detección 6, distancia de ataque 1,8 y 2 s entre ataques. Hacia dónde mira lo resuelve
  `spriteMiraDerecha`: no hay signos copiados del Eco.
- **Los golpes salen del dibujo.** Eventos en los clips: `AbrirGolpe` en A3, `CerrarGolpe` en A4,
  `FinAtaque` al acabar A5 y `FinMuerte` al llegar a D4.
- **La onda de A3**, ancha y baja: de 0,42 a 2,2 u por delante y 0,6 u de alto. Hace daño solo
  los 0,08 s de A3 y se esquiva saltando.
- **El contacto con el cuerpo** hace daño siempre.
- **Hiperarmadura (decisión 4).** Un campo nuevo, `EnemyData.hiperarmadura`, y una condición en
  `EnemyController.AlRecibirGolpe`: con ella, el golpe quita vida y destella, pero no pasa a
  DAMAGED. No hay retroceso ni se corta el ataque.
- **El destello (decisión 5).** Es la silueta del Eco (`Destello.mat`). `EnemigoAnimacion` gana
  `colorDestello` (el color de la silueta, blanco por defecto), junto a `duracionDestello`.
- **La muerte.** D1 a D3, y D4 (los restos) se queda 0,7 s antes de desvanecerse
  (`restosEnPantalla`, nuevo en `EnemigoAnimacion`; al Eco no le cambia nada).
- **El andar** va a los tiempos de la tabla a 2 u/s (fase C).
- **Sus datos.** El constructor deja en el asset lo que cambia con el arte: tinte blanco,
  hiperarmadura y la ventana del golpe del clip (0,50 a 0,58 de 0,98 s). Esa ventana solo se usa
  sin animación. Lo demás no se toca.
- **En el Nivel 1** (`PreparadorNivel`, paso 10), el Golem provisional (la cápsula de la Fase 4)
  se sustituye por este, en su sitio y con su ruta de patrulla.

#### Las mecánicas (`GeneradorMecanicas`)

- **Con su arte, y medidas en él.** Cada collider sale de los píxeles nativos del encargo
  (`GeneradorMecanicas.Geometria`), con el pivote de `TablaArte`. El Transform va donde se apoya
  el pie. Los dibujos van detrás de los tiles del suelo, para que lo que se hunde en el terreno
  no se vea por delante; la lava va por delante, porque su costra tiene que verse.
- **Las zonas de daño, apagadas en reposo.** La llama de la torreta solo está activa en el disparo
  y la de la combustión, encendida.
- **Lava (decisión 2).** Sigue matando. `tramos` franjas centradas, alternando `Lava` y
  `Lava_espejo`; las monta el propio componente, también en el editor, sin guardarlas en la
  escena. El trigger empieza en la costra.
- **Torreta.** Ciclo fijo: reposo 1,6 s, carga 0,7 s y disparo 0,35 s, los tres públicos; la carga
  es el aviso.
  - Al disparar, la llama hace daño y sale una bola.
  - Dispara hacia donde mira: con la escala X negativa, a la izquierda.
  - `desfase` evita que dos torretas disparen a la vez.
  - La bola es un prefab (`BolaDeFuego`) con su Rigidbody2D, su collider y su script. Su sprite
    es provisional: el círculo del proyecto, en naranja, porque falta Fireball3.
- **Plataforma inestable.** Retardo antes de romperse 0,6 s, caída 0,6 s y reaparición 3 s, los
  tres públicos.
  - Se agrieta al pisarla y se rompe aunque el jugador se baje.
  - Al romperse, el collider se apaga en el acto.
  - Reaparece intacta, pero no si el jugador está donde reaparecería.
- **Plataforma de combustión (decisión 3).** `PlataformaCaliente` pasa a `PlataformaCombustion`
  (script y prefab con el mismo GUID, así que el Nivel 1 los conserva).
  - Ciclo fijo: apagada 2 s, calienta 0,8 s (el aviso) y encendida 1 s.
  - La llama solo hace daño encendida, y el collider no se apaga nunca.
  - Tiene su propio script, distinto del de la plataforma inestable: allí el suelo falla y aquí
    no.

#### Las cinemáticas (sección 5 y decisión 6)

- **Escala entera** (`CutsceneData.escalaEntera`, en `PantallaCinematica`). El plano mide en
  pantalla su tamaño por el mayor entero que cabe, centrado y con los bordes en píxeles enteros.
  El tamaño se pasa a unidades del Canvas dividiendo por su escala, así que el Canvas del resto
  de la interfaz no se toca.
- **La presentación** (`Cinematica_Jefe_Ira`) lleva `CinematicaJefe1` y `CinematicaJefe2`, opaca y
  a escala entera. Sus textos son del guion (N17) y no se tocan.
- **La Centinela ruge al empezar el combate**, también en la fase 1: se levanta tras el ojo que
  se abre.
- **La derrota** (`Cinematica_Derrota_Ira`, `CinematicaFinalLvl1`). Cuando la Centinela termina de
  caer (`BossController.OnCaidaTerminada`, nuevo), la arena la reproduce
  (`LevelData.cinematicaJefeDerrotado`, nuevo) y se vuelve al juego, con el fragmento en la
  ceniza para recogerlo.
- **`CinematicaFinalLvl2` entra sola:** al dejar su PNG en la carpeta y regenerar, se añade como
  segundo plano. Hasta entonces, la regeneración avisa de que falta.

#### Comprobado a ojo

Un render del Nivel 1 en modo batch, con un script temporal que se borró, mostró:

- la torreta sobre el suelo, sin que el bloque salte al disparar;
- la plataforma inestable a ras del foso, con lo que sobra (mide 3,83 u en un foso de 3 u)
  escondido tras los tiles;
- las de combustión como rejillas en el suelo;
- el Golem apoyado, a 2,7 u;
- la lava en su pozo.

Las piezas no se movieron. El recorrido sigue siendo el provisional; el definitivo es N10.

### Fase E: verificación

- **El menú** *Fragmentum → Niveles → Verificar el arte del Nivel 1* (`VerificadorNivel1`) revisa
  los seis puntos del encargo:
  1. **Los 30 sprites** existen (15 del Golem, 12 de las mecánicas y 3 planos). Cada uno tiene el
     lienzo, los píxeles por unidad y el pivote de la regla de su carpeta en `TablaArte`, y es
     pixel art (Point, sin compresión, Full Rect, sin mipmaps, Clamp). Un PNG nuevo en esas
     carpetas también se revisa contra su regla.
  2. **Los 13 clips** existen, con sus fotogramas en su sitio, su bucle y la duración de su tabla
     (margen de 1 ms). Los del Golem llevan exactamente los eventos de su tabla, y ningún clip
     anima un collider.
  3. **Los 4 Animators** (Golem, torreta, plataforma inestable y combustión) tienen un estado por
     clip, ninguno suelto, y ninguna transición rota. Una transición está rota si no tiene
     destino, si le faltan a la vez condición y tiempo de salida, o si su parámetro no existe o
     es de otro tipo (un «mayor que» sobre un bool nunca se cumple). En las mecánicas, además,
     cada valor de `Estado` lleva al estado de su clip.
  4. **En los 6 prefabs**, lo que hace daño (una hitbox o la capa EnemyWeapon) está apagado en
     reposo: la onda del Golem y las llamas de la torreta y de la combustión. Quedan fuera, por
     decisión, el cuerpo del Golem (hace daño siempre) y la bola (solo existe disparada). La lava
     mata con su propio trigger, no con una hitbox. También comprueba que cada prefab tiene sus
     zonas de daño y que su dibujo usa su controlador.
  5. **Cada evento de animación** llama a una función pública que existe en un script del objeto
     de su Animator: `EnemigoAnimacion` en el Golem. En las mecánicas no hay ninguno.
  6. **Lo que sostiene:**
     - **La plataforma inestable.** El estado que muestra `Plataforma_3_rota` es un valor de
       `Estado` en el que su script apaga el collider. La regla es nueva y pública
       (`PlataformaInestable.SostieneEn`): el script la aplica y el verificador la compara con el
       arte de cada estado.
     - **La combustión.** Sostiene en todos sus fotogramas. Su suelo es el collider sólido de la
       raíz, encendido. Ningún clip lo toca. La llama, que se enciende y se apaga, está en otro
       objeto: si estuviera en la raíz, apagarla se llevaría el suelo.
- **El arte que falta no es un fallo.** `CinematicaFinalLvl2` sale como aviso («Arte pendiente»).
- **Falla ruidosamente,** con un error por problema y el nombre de lo que falta o está mal. Por
  ejemplo: «Animator Torreta: Estado = 2 (FIRE) no lleva a Disparo; el dibujo no seguiría al
  script».
- **También al final de «Regenerar todo»,** después de los prefabs de las mecánicas. Si algo no
  cuadra, la regeneración falla y `verificar.ps1 -Unity` también. El menú que genera el arte del
  Nivel 1 también verifica al terminar.

#### Cambios en lo que ya había

- **Comprobaciones compartidas.** Las del verificador de la Centinela que valen para cualquier
  arte (sprite, clip, Animator, eventos y daño en reposo) pasan a `ComprobacionesArte`, y los dos
  verificadores las usan. Los mensajes de la Centinela no cambian, salvo «collider de ataque»,
  que pasa a ser «collider de daño».
- **Los eventos se comprueban contra los scripts que los reciben de verdad.** Antes era una clase
  fija. También se aceptan los parámetros que un evento puede pasar (float, int, string, un
  objeto o el propio evento).
- **El lienzo de cada grupo está en `TablaArte`** (`Regla.Lienzo`). Lo usan el verificador y la
  geometría de las mecánicas, que antes lo tenía repetido.

### Verificación (fase A)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **Pruebas EditMode: 268 correctas.** Hay 8 nuevas en `GolemMecanicasArteTests`:
  - los 30 PNG, con el lienzo entero;
  - cada uno con los píxeles por unidad y el pivote de su carpeta (los valores esperados se
    copian del encargo, no de la tabla) y los ajustes de pixel art;
  - los píxeles por unidad cambian por carpeta y los tamaños son los del encargo;
  - los pivotes caen en el suelo del Golem (fila 88 en los 15) y de la torreta, en la superficie
    de las plataformas (filas 21 y 26) y en la costra de la lava;
  - la torreta no salta al disparar;
  - la tabla da la regla más concreta y una sola por ruta;
  - la tabla ya está aplicada a todo, también a la Centinela y al Eco;
  - un PNG nuevo en `Mecanicas` recibe la regla de su grupo.
- **Pruebas PlayMode: 115 correctas.** No cambian.
- **Regenerar** no cambia nada más que los sprites nuevos y sus carpetas.

### Verificación (fase B)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **Pruebas EditMode: 273 correctas.** Hay 5 nuevas en `GolemMecanicasArteTests`:
  - los 4 clips del Golem duran lo de la tabla del encargo (margen de 1 ms), con su bucle y cada
    fotograma en su sitio;
  - el ataque no está uniformado: A1 dura al menos seis veces A3, las cuatro duraciones son
    distintas y no hay A2;
  - la muerte se detiene en D4;
  - los 9 clips de las mecánicas, con el último fotograma que se queda, y la lava sin clip;
  - regenerar los clips no cambia nada.
- **Pruebas PlayMode: 115 correctas.** No cambian.

### Verificación (fase C)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **Pruebas EditMode: 279 correctas.** Hay 6 nuevas en `GolemMecanicasArteTests`:
  - el Animator del Golem tiene un estado por clip, empieza en Idle y usa los parámetros de la
    FSM; andar y atacar siguen sus parámetros de velocidad;
  - ataca desde Idle o andando (atacar va primero) y vuelve a Idle al acabar el clip entero;
  - parado respira, y entre pararse y echar a andar hay margen;
  - la muerte interrumpe cualquier estado, no vuelve a sí misma y no tiene salida;
  - cada mecánica salta al estado que pide su script: un solo parámetro entero, un estado por
    valor de su enum y en su orden, y ninguno cambia solo;
  - ningún Animator tiene estados sueltos, transiciones rotas ni mezclas.

  La de regenerar ahora incluye los controladores.
- **Pruebas PlayMode: 121 correctas.** Hay 6 nuevas en
  `GolemMecanicasAnimatorPlayModeTests`, con los Animators reproduciendo de verdad:
  - el Golem respira (I1 · I2 · I3 · I2, 0,40 s), anda a 0,13 s por fotograma a su velocidad
    natural y al doble a media velocidad, y parado vuelve a respirar;
  - el ataque: A1 0,50, A3 0,08, A4 0,14, A5 0,26 y vuelta a Idle;
  - ataca andando, y si la FSM corta el ataque vuelve a Idle;
  - la muerte interrumpe el ataque y se queda en D4 pase lo que pase;
  - cada mecánica muestra el fotograma del estado pedido y no cambia sola;
  - la plataforma se ve partida 0,25 s y después solo el polvo.

### Verificación (fase D)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** monta el Golem y las mecánicas, pone los planos y sustituye el Golem del
  Nivel 1. Avisa de que falta `CinematicaFinalLvl2`.
- **Pruebas EditMode: 284 correctas.** Hay 5 nuevas en `GolemMecanicasArteTests`:
  - el prefab del Golem: sus datos, su arte, su destello, la onda (ancha, baja y apagada) y el
    contacto (siempre);
  - sus eventos, en el inicio de A3, A4 y D4 y al acabar A5, a funciones que existen;
  - los prefabs de las mecánicas, con los colliders medidos en el dibujo (la llama de la torreta
    llega a la x 81 del disparo) y la bola montada;
  - en reposo, ninguna zona de ataque encendida;
  - las cinemáticas del jefe, con sus planos y a escala entera.

  Se adaptan las del Nivel 1: el Golem con arte y la plataforma de combustión. También la de las
  presentaciones de jefe: opaca si tiene planos.
- **Pruebas PlayMode: 135 correctas.**
  - 6 nuevas en `GolemPlayModeTests`, con el prefab, el jugador y la física reales:
    - la onda solo hace daño en A3, tras 0,5 s de A1, y quita los 2 de su daño;
    - los golpes no le cortan el ataque, pero destella;
    - persigue andando y, parado a distancia de ataque, respira;
    - mira hacia Kael;
    - el contacto hace daño siempre;
    - al morir se queda en D4 0,7 s y después se desvanece.
  - `MecanicasPlayModeTests`, reescritas con 14 pruebas:
    - la plataforma inestable se rompe aunque te bajes, desaparece y vuelve;
    - la combustión cicla sola y solo quema encendida;
    - la lava monta sus franjas alternadas, sin huecos y con el trigger desde la costra;
    - la torreta cicla sola, la llama quema delante del bloque, la bola hiere y, volteada,
      dispara a la izquierda.
  - Una nueva en `NarrativaPlayModeTests`: el plano mide k × 240 por k × 135 píxeles, con los
    bordes en píxeles enteros.
  - Una nueva en `CentinelaPlayModeTests`: al empezar el combate ruge y después ataca.
  - El flujo del Nivel 1 pasa por la cinemática de la derrota antes de recoger el fragmento.

### Verificación (fase E)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** no cambia ningún archivo. Termina con:
  - «[Centinela] Verificación correcta: 29 sprites, 8 clips…»;
  - el aviso de `CinematicaFinalLvl2`;
  - «[Nivel 1] Verificación correcta: 30 sprites, 13 clips, 4 Animators, 6 prefabs y sus eventos;
    la plataforma inestable no sostiene rota y la de combustión siempre».
- **Pruebas EditMode: 294 correctas.** Hay 10 nuevas en `VerificadorNivel1Tests`. Lo roto son
  copias u objetos en memoria:
  - el proyecto pasa la verificación;
  - revisa 30 sprites, 13 clips, 4 Animators y 6 prefabs;
  - un sprite con otro lienzo, 100 píxeles por unidad, filtro bilineal y comprimido se nombra
    contra la regla del Golem y contra la de la torreta disparando (pivote 0,287);
  - el ataque del Golem uniformado, con el evento fuera de A3 y un collider animado, se nombra;
  - una torreta cuyo `Estado = 2` no lleva a Disparo, o con un estado suelto, se nombra;
  - un «mayor que» sobre el bool `Attack` se nombra;
  - la onda del Golem encendida se nombra, y el contacto no; una torreta sin llama o sin su
    controlador, también;
  - un evento sin función que lo reciba se nombra, en el Golem y en una mecánica;
  - la plataforma inestable: la regla del collider, la plataforma rota mientras aún sostendría y
    el prefab sin collider se nombran;
  - la combustión: con la llama en la raíz o el suelo apagado, se nombra.

  Se adapta una de `VerificadorCentinelaTests` («collider de daño»).
- **Pruebas PlayMode: 135 correctas.** No cambian.

---

## Arte de la Centinela Ardiente (2026-10-05)

Rama `centinela-arte`, que sale de `ajustes-nivel1`. El autor terminó el arte del jefe: 29 sprites
limpios y alineados en `Tesis/Jefe1 animaciones/limpios`, con su LEEME. Con el arte llegó un
encargo para integrarlo en cinco fases, parando al final de cada una:

- A: importación;
- B: clips;
- C: Animator;
- D: controlador y prefab;
- E: verificación.

### Decisiones que ajustan el encargo

- **Problema.** El encargo (y una segunda versión que llegó después, con las mismas peticiones)
  estaba escrito en parte sobre la v1:
  - daño con `PlayerMovement.takeDamage` y el tag `"Weapon"`;
  - un `CentinelaController` y un prefab nuevos;
  - scripts en `Assets/Editor`, clips en `Assets/Animations` y menús en `Tools/`.

  En la v2 el daño va por `IGolpeable` y capas de física, y la Centinela ya existe como jefe
  completo: `BossController`, arena, presentación y recompensa. Seguirlo al pie de la letra
  habría dejado dos jefes y dos formas de hacer daño.
- **Solución.** Antes de empezar se le preguntó al autor, que decidió:
  - **La Centinela que ya existe.** El arte va en `CentinelaArdiente`, no en un controlador
    nuevo.
  - **El combate actual.** Se mantienen las tres fases (50 % y 25 %), los combos, las ondas que
    recorren la arena, las erupciones y el daño doble con la espada clavada. Los clips se
    ajustan a ese combate.
  - **Las convenciones de la v2:** editor en `Assets/Scripts/Editor`, clips en
    `Assets/Animaciones` y menús en `Fragmentum/…`. La verificación será un menú y, además,
    pruebas automáticas.
  - **`D4 (2).png` es el D4 definitivo.** Se copia como `D4.png`.
  - **De la segunda versión se añade lo que encaja con ese combate:**
    - D4, la ceniza con el fragmento, se queda en pantalla y Kael recoge el fragmento;
    - la altura del salto la pone el cuerpo, no el sprite;
    - el forcejeo de la espada clavada, sin fotograma extra;
    - los golpes salen de eventos de animación.

### Fase A: importación

- **Los sprites.** Los 29 PNG se copian sin tocar a `Assets/Sprites/Jefes/Centinela`; son idénticos
  byte a byte a los del arte.
- **El importador.** `ImportadorJefes` es un postprocessor que ajusta todo lo que cae en
  `Assets/Sprites/Jefes`. Así los ajustes sobreviven a un reimport:
  - a cualquier jefe, los de pixel art (`ArteJefes.Ajustar`): sprite único, Point, sin
    compresión ni mipmaps, Clamp y Full Rect;
  - a la Centinela, además (`ConstructorCentinela.AjustarImportador`): 49 px por unidad, 58 el
    idle frontal (I1–I3), y el pivote común (0,534; 0,060).

  Sigue el patrón del Eco: `ImportadorEnemigos` llama a `ConstructorEcoIra`. Cuando otro jefe
  tenga arte, se añade su carpeta.
- **Escala.** Andando, la Centinela mide 3,4 u: casi el doble que Kael (1,77 u) y lo mismo que su
  caja de jefe. El idle frontal está dibujado más grande y a 58 px por unidad mide lo mismo.

### Notas sobre el arte (no se ha tocado)

- **A4** es el paso en el aire: la planta queda 3 px por encima del suelo.
- **G3** baja 8 px por debajo de la línea de suelo: es la onda del golpe.
- **Densidad.** Un píxel del dibujo mide 1/49 u (1/58 en el idle) y uno de Kael 1/64 u, así que los
  de la Centinela son un 30 % más gruesos. Es la escala del LEEME y no se cambia.

### Fase B: clips

- **La tabla.** `ConstructorCentinela.Clips` tiene los 8 clips con los tiempos del LEEME, sin
  normalizar. El menú *Fragmentum → Jefes → Generar los clips de la Centinela* y «Regenerar todo»
  los crean en `Assets/Animaciones/Jefes/Centinela` (`Centinela_Idle.anim`, etc.). Si cambia un
  tiempo, se cambia en la tabla y se regeneran.

  | Clip | Fotogramas y duración (s) | Bucle |
  |---|---|---|
  | Idle | I1 0,40 · I2 0,40 · I3 0,40 · I2 0,40 | sí |
  | Andar | A1 0,15 · A2 0,15 · A4 0,15 · A2 0,15 | sí |
  | Espadazo | **E1 0,55** · E2 0,08 · E3 0,22 · E4 0,30 | no |
  | EspadaClavada | C1 0,09 · **C2 1,20** · C4 0,18 | no |
  | GolpeSuelo | **G1 0,50** · G2 0,07 · G3 0,40 | no |
  | Rugido | R1 0,35 · R2 0,35 · R3 0,20 | no |
  | Salto | **J1 0,50** · J2 0,10 · J3 0,30 · J4 0,15 · J5 0,45 | no |
  | Muerte | D1 0,40 · D2 0,50 · D3 1,20 · D4 (se queda) | no |

- **Duración exacta.** Unity alarga un fotograma (1/60 s) los clips de sprites tras su última
  clave. Como en el Eco, la clave de cierre va un fotograma antes y cada clip dura lo que dice la
  tabla.
- **D4 se queda.** La muerte no tiene clave de cierre: D4 aparece a los 2,1 s y el clip, sin bucle,
  se detiene en él.
- **El forcejeo de C2.** No hay fotograma extra. El sprite sube 0,02 u (un píxel del dibujo) y vuelve
  a 0 cada 0,3 s, en escalones (tangentes *Constant*), solo mientras dura C2.
- **El latido de color va en la fase D, no en el clip.** `BossController` reescribe el color del
  sprite en cada `LateUpdate` (aviso, ventana, golpe), así que una curva de color en el clip no se
  vería. Irá en contrafase con el forcejeo, desde el componente que reciba los eventos.
- **El salto no sube el sprite.** J3 y J4 están anclados al suelo como los demás: la altura la pone
  el cuerpo del jefe con su tiro parabólico (`BossController.Saltar`). Ningún clip, salvo el
  forcejeo, mueve el sprite.
- **Los eventos de animación** (golpes, ventanas y fin de cada clip) se añaden en la fase D, junto
  con el componente que los recibe.

### Fase C: Animator

- **El controlador.** `CentinelaArdiente.controller` está en la misma carpeta que los clips y
  tiene un estado por clip. Lo arma `ConstructorCentinela` desde la misma tabla, con el menú
  *Fragmentum → Jefes → Generar los clips y el Animator de la Centinela* o con «Regenerar todo».
  Empieza en Idle.
- **Los parámetros** están en `AnimatorParams`, para que el código del jefe use los mismos nombres:

  | Parámetro | Tipo | Qué hace |
  |---|---|---|
  | `Walk` | bool | Idle ↔ Andar mientras se mueve |
  | `Espadazo`, `GolpeSuelo`, `Salto`, `Rugido` | disparador | empiezan su clip al momento, desde cualquier estado |
  | `Clavar` | bool | el espadazo acaba con la espada clavada (C1-C4) en vez de E3-E4 |
  | `Death` | bool | muere desde cualquier estado |

- **El rugido y la muerte interrumpen cualquier estado.** La muerte va la primera y nada la
  interrumpe: el estado Muerte no tiene salida y se queda en D4.
- **Los ataques salen de cualquier estado, no solo de Andar.**
  - **Problema.** El encargo pedía pasar de Caminar al ataque al entrar en rango.
  - **Por qué no encaja.** En el combate actual los ataques se encadenan (dos o tres espadazos
    seguidos, la onda doble) y cada golpe tiene que coincidir con su zona de daño. Si el ataque
    esperara a que acabe el clip anterior, el dibujo iría por detrás del daño.
  - **Solución.** Salen de cualquier estado. Solo el espadazo y el golpe al suelo pueden volver a
    empezar sobre sí mismos; el salto, el rugido y la muerte no se reinician.
- **La espada clavada sale del espadazo.** Con `Clavar`, el espadazo pasa a C1 justo al acabar E2
  (a los 0,63 s de sus 1,15 s). Sin él sigue con E3 y E4. El jefe lo decide al empezar el último
  espadazo del combo.
- **Al acabar, a Idle o a Andar.** Cada ataque vuelve a Idle al acabar su clip, sin corte: E4 es
  la pose de guardia. Si el jefe ya camina, va directo a Andar, porque el idle es frontal y un
  solo fotograma suyo entre dos poses de perfil se notaría.
- **Sin mezclas.** Todas las transiciones duran 0, porque son fotogramas de pixel art. Solo
  esperan a su clip las de fin de ataque y la de clavar la espada; lo que interrumpe no espera.
- **Write defaults en todos los estados:** al salir de la espada clavada, el forcejeo vuelve a 0.
  Para la fase D, el sprite tiene que estar a altura 0 dentro del jefe, porque el forcejeo fija
  la altura, no la suma.

### Fase D: el arte en el combate

Aprobado por el autor tras la fase C («de acuerdo con todo»). El combate sigue siendo el actual
(opción B); lo que cambia es que el dibujo marca el ritmo.

- **`AnimacionJefe`** (nuevo, en el objeto del sprite, como `EnemigoAnimacion` en el Eco) une el
  Animator con `BossController`:
  - recibe los eventos de los clips y los reenvía al jefe;
  - pone `Walk` según la velocidad del cuerpo;
  - al recibir un golpe, el sprite se vuelve una silueta blanca 0,1 s con el material del Eco.
    Un tinte blanco no se vería sobre el arte.
- **Los golpes salen con el dibujo (eventos de animación):**

  | Evento | Fotograma | Qué hace |
  |---|---|---|
  | `Golpe` | E2, G3, J2 | sale la zona del tajo, las ondas (o la lava) y el salto |
  | `FinGolpe` | E3, C1 | se apaga la zona del tajo: dura lo que se ve E2 |
  | `VentanaCorta` | E3 | 0,22 s de daño doble, sin detener al jefe |
  | `Ventana` | C2 | la ventana vulnerable, 1,2 s: lo que se ve la espada clavada |
  | `EnElAire` | final de J4 | si aún vuela, J4 se sostiene hasta tocar el suelo |
  | `Aterrizaje` | J5 | salen las ondas del salto |
  | `FinMuerte` | D4 | aparece el fragmento |

  Los patrones esperan estos momentos con `BossController.Golpear`, `TerminarGolpe` y
  `EsperarMomento`. Sin arte (los otros cuatro jefes, todavía cápsulas) siguen yendo por tiempo,
  como antes.
- **Los ajustes de tiempo aprobados:**
  1. **Espadazos encadenados:** su aviso (0,35 o 0,3 s) es más corto que E1 (0,55 s), así que el
     clip empieza ya avanzado y E1 dura justo el aviso. Los demás fotogramas no cambian.
  2. **Ventana de furia:** dura lo que C2 (1,2 s) en todas las fases, porque la abre C2. Antes, en
     la furia, eran 0,9 s.
  3. **Salto:** J2 y J3 suman 0,4 s, justo lo alto de un vuelo de 0,8 s. J4 se sostiene desde ahí
     hasta tocar el suelo y J5 llega con el aterrizaje. El vuelo no cambia.
  4. **Erupciones:** usan GolpeSuelo: la lava brota con G3. El rugido queda solo para los cambios
     de fase.
  5. **Muerte:** la derrota dura 2,1 s, lo que tarda en llegar a D4, y D4 se queda: no se
     desvanece ni desaparece.
  6. **E3:** ventana corta de daño doble (0,22 s).
- **Onda doble.** Entre dos golpes al suelo seguidos se ve G3 0,2 s, la pose que mata: el segundo
  golpe acorta su G1 para no alargar el intervalo.
- **El fragmento de la ceniza** (`FragmentoJefe`, nuevo):
  - **Problema.** D4 trae el fragmento blanco dibujado encima de la ceniza, y hasta ahora la arena
    daba la recompensa al derrotar al jefe.
  - **Solución.** Al llegar a D4 aparece un trigger que va del suelo al fragmento: basta con que
    Kael entre en la ceniza, sin saltar.
  - **Al recogerlo:** `BossController.OnFragmentoRecogido` (evento) y `RecogerFragmento()`
    (método público) avisan; la arena otorga entonces el fragmento y la habilidad y abre las
    puertas.
  - **El dibujo:** una máscara del tamaño del fragmento lo borra de D4 y la ceniza se queda, sin
    sprite nuevo. El fragmento flota 20 px por encima de la ceniza, así que la máscara no la toca
    (`ConstructorCentinela.MedirFragmento` lo mide en el PNG).
- **El latido de la espada clavada** va en contrafase con el forcejeo: con el sprite abajo, brasa;
  arriba, casi apagado (`CentinelaArdiente.ColorVentana`).
- **El rugido** del cambio de fase reproduce R1-R3, que dura lo mismo que la espera de la fase
  (0,9 s).
- **El prefab** (`Assets/Prefabs/Jefes/CentinelaArdiente.prefab`) lo sigue generando
  `GeneradorJefes`, que ahora lo viste con `ConstructorCentinela.Vestir`:
  - el sprite, a altura 0 y escala 1 en el pivote, con I1 y sin la cápsula provisional;
  - el Animator con su controlador, animando siempre: los eventos llegan aunque el jefe esté fuera
    de cámara;
  - `AnimacionJefe`, con la anticipación de cada ataque leída de la tabla;
  - el fragmento, escondido hasta D4.
- **Datos:** el arte manda sobre dos: `tinte` blanco y `duracionDerrota` 2,1 s. «Regenerar todo»
  los fija en `CentinelaArdiente.asset`.
- **Se mantiene** el destello rojo del aviso durante la anticipación, como hasta ahora.
- **No se ha tocado** `EnemyController.cs` ni `GameManager.cs`.

### Fase E: verificación

- **El menú** *Fragmentum → Jefes → Verificar Centinela* (`VerificadorCentinela`) revisa los cinco
  puntos del encargo:
  1. los 29 sprites existen, con su lienzo de 262×252, sus píxeles por unidad (49; 58 el idle), el
     pivote de los pies, Point, sin compresión, Full Rect, sin mipmaps y Clamp;
  2. los 8 clips existen, con sus fotogramas en su sitio y la duración de la tabla (margen de
     1 ms);
  3. el Animator tiene los 8 estados con su clip y ninguno suelto, y ninguna transición rota: sin
     destino, con un parámetro que no existe o sin condición ni tiempo de salida;
  4. en el prefab, todo lo que hace daño (el contacto, la capa EnemyWeapon) está apagado en
     reposo, el fragmento está escondido, el sprite está en los pies y lleva su Animator;
  5. cada evento de animación llama a una función que existe en `AnimacionJefe`.
- **Falla ruidosamente:** un error en la consola por cada problema, con el nombre de lo que falta
  o está mal (por ejemplo, «Sprite D4: 100 píxeles por unidad (debe ser 49)»).
- **También al final de «Regenerar todo».** Si algo no cuadra, la regeneración falla y
  `verificar.ps1 -Unity` también. El importador ya vuelve a poner los ajustes de los sprites en
  cada reimportación; el verificador avisa si eso dejara de pasar, o si un clip, el Animator o el
  prefab se tocan a mano.
- **Cada comprobación recibe lo que revisa.** Así las pruebas le pasan copias u objetos en memoria
  rotos a propósito, sin tocar el arte del proyecto.

### Qué probar a mano (fase D)

Desde `Menu_Inicial`, «Nueva partida», hasta la arena del Nivel 1:

- El tajo hace daño mientras se ve E2. En el combo, el segundo espadazo se ve venir menos.
- Con la espada clavada (C2) late y forcejea, y cada golpe vale el doble. En E3 también, un
  instante.
- La onda sale del golpe al suelo y, en la onda doble, se ve G3 entre los golpes.
- El salto: J4 en el aire hasta caer y J5 al tocar el suelo, con las ondas.
- Al cambiar de fase ruge.
- Al morir se queda en la ceniza. La puerta no se abre hasta que Kael entra en ella y recoge el
  fragmento, que desaparece del dibujo.

### Verificación (fase A)

- **Compilación** con `dotnet build` y en Unity 6000.6.3f1 en modo batch: 0 errores y 0 avisos.
- **Pruebas EditMode: 239 de 239 correctas.** Hay 5 nuevas, en `CentinelaArteTests`:
  - los 29 se importan como pixel art, con el lienzo entero y el pivote común;
  - el idle frontal va a 58 px por unidad y el resto a 49;
  - los pies apoyan en la fila 237 y solo la onda de G3 la cruza;
  - mide 3,4 u, casi el doble que Kael, y el idle mide lo mismo que andando;
  - el sprite de un jefe sin ajustes propios recibe igualmente los de pixel art.
- **Pruebas PlayMode: 99 de 99 correctas.** No cambian: el jefe todavía no usa el arte.

### Verificación (fase B)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **Pruebas EditMode: 245 de 245 correctas.** Hay 6 nuevas en `CentinelaArteTests`:
  - la tabla usa los fotogramas del arte y solo C1b se queda fuera;
  - los clips tienen los tiempos del LEEME, copiados en la prueba y no leídos de la tabla, con
    margen de 1 ms;
  - la muerte acaba en D4 y se queda ahí;
  - en los tres ataques la anticipación es el fotograma más largo y el golpe no pasa de 0,15 s.
    (El LEEME dice que E2, G2 y J4 son los más cortos, pero en su tabla J2, el despegue, dura
    0,10 s y J4 0,15 s. Mandan los tiempos de la tabla.);
  - el forcejeo sube un píxel cada 0,3 s, en escalones y solo en C2;
  - ningún otro clip mueve el sprite, tampoco el salto.
- **Pruebas PlayMode: 99 de 99 correctas.** No cambian: el jefe todavía no usa el arte.

### Verificación (fase C)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **Pruebas EditMode: 250 de 250 correctas.** Hay 5 nuevas en `CentinelaArteTests` sobre el
  controlador:
  - un estado por clip, sin acelerar ni frenar, y empieza en Idle;
  - la muerte y el rugido interrumpen cualquier estado, y la muerte no tiene salida;
  - el espadazo clava la espada justo al acabar E2;
  - cada ataque vuelve a Idle al acabar, o a Andar si ya camina;
  - ningún estado suelto, ninguna transición rota (sin destino o con un parámetro que no existe)
    y ninguna mezcla.
- **Pruebas PlayMode: 104 de 104 correctas.** Hay 5 nuevas en `CentinelaAnimatorPlayModeTests`, con
  el Animator reproduciendo de verdad:
  - cada ataque pasa por sus fotogramas con los tiempos del LEEME (margen de 0,05 s) y vuelve al
    idle;
  - con `Clavar`, de E2 a C1; C2 dura 1,2 s y solo ahí el sprite sube un píxel y baja;
  - un segundo espadazo durante E3 vuelve a empezar en E1;
  - si ya camina, de G3 pasa a andar sin ver el idle;
  - el rugido corta el espadazo, la muerte corta el rugido y, después, nada la saca de D4.

### Verificación (fase D)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **Regenerar dos veces** deja el prefab de la Centinela y la escena del Nivel 1 idénticos.
- **Pruebas EditMode: 254 de 254 correctas.** Hay 4 nuevas en `CentinelaArteTests`:
  - los eventos caen al empezar su fotograma, con los tiempos copiados del diseño, y las ventanas
    duran lo que se ven;
  - cada evento llama a una función que existe en `AnimacionJefe` (punto 5 de la verificación del
    encargo);
  - el prefab lleva el arte, con el sprite en los pies, sin cápsula y con el contacto apagado en
    reposo;
  - la máscara del fragmento tapa todos sus píxeles y ninguno de la ceniza.
- **Pruebas PlayMode: 115 de 115 correctas.** Hay 11 nuevas en `CentinelaPlayModeTests`, con el
  prefab, el jugador y la física reales:
  - el tajo solo hace daño mientras se ve E2, y la ventana es C2 y dura 1,2 s;
  - en la furia, E1 se acorta en los encadenados (0,45 y 0,3 s), E2 no cambia y la ventana sigue
    durando 1,2 s;
  - E3 da daño doble y E1, normal;
  - las ondas salen con G3 y, en la onda doble, se ve G3 entre los golpes;
  - la lava brota con G3;
  - el salto despega con J2, J4 dura 0,4 s y J5 llega con el suelo, con las ondas;
  - al cambiar de fase ruge;
  - un golpe recibido pone la silueta blanca 0,1 s;
  - el latido va en contrafase con el forcejeo;
  - al caer se queda en D4 y la recompensa espera al fragmento: Kael lo recoge entrando en la
    ceniza y la máscara lo borra;
  - al reiniciar vuelve al idle.

  Cambian 3:
  - el Animator solo de la fase C recibe ahora los eventos con un receptor vacío;
  - la derrota que se desvanece se prueba con un jefe sin arte (la Doncella);
  - el flujo del Nivel 1 recoge el fragmento antes de comprobar la recompensa.

### Verificación (fase E)

- **Compilación** con `dotnet build` y en Unity en modo batch: 0 errores y 0 avisos.
- **«Regenerar todo»** termina con «[Centinela] Verificación correcta: 29 sprites, 8 clips, el
  Animator, el prefab y los eventos».
- **Pruebas EditMode: 260 de 260 correctas.** Hay 6 nuevas en `VerificadorCentinelaTests`:
  - el proyecto pasa la verificación;
  - un sprite con otro lienzo, 100 píxeles por unidad, el pivote en el centro, filtro bilineal y
    comprimido se nombra con cada problema (y el idle pide 58);
  - un clip igualado a 0,25 s y con E3 donde va E2 se nombra;
  - una copia del Animator sin Salto, con un estado suelto, una transición a un parámetro que no
    existe y otra sin condición se nombra;
  - el prefab con el contacto encendido, el fragmento visible y el sprite fuera de los pies se
    nombra;
  - un evento a una función que no existe, o que no es un evento válido, se nombra.
- **Pruebas PlayMode: 115 de 115 correctas.** No cambian.

---

## Ajustes tras la primera prueba del Nivel 1 (2026-10-02)

Rama `ajustes-nivel1`, que sale de `enemigo-eco-ira`. El autor jugó el Nivel 1 y contestó a
`PENDIENTES.md`:

- el control, la hitbox de la espada y el fondo están bien;
- la tecla E se pulsa, no se mantiene;
- el registro emocional no debe pedir código de participante;
- el golpe del Eco es demasiado lento;
- el Golem de Ceniza no debe morir en la lava;
- la Centinela se siente fácil: tiene que transmitir la ira;
- al entrar en la arena tiene que haber una cinemática corta con el diálogo del jefe.

Además dio el visto bueno para fusionar las ramas que había (N6). Después señaló que los
conceptos de la Centinela llevan espada, no martillo (sección 6).

### 1. El golpe del Eco de la Ira era lento

- **Problema.** En la prueba, el ataque del Eco se sentía demasiado lento.
- **Causa.** El clip seguía los tiempos del encargo: 0,73 s en total, con 0,35 s de anticipación.
  Después del golpe, el Eco descansaba 0,7 s más.
- **Solución.** El estado Attack tiene ahora su propio multiplicador de velocidad.
  - **Velocidad.** El parámetro `VelocidadAtaque` del controlador lo escribe
    `EnemigoAnimacion.velocidadAtaque`, que en el prefab vale 1,6. Así a1 dura 0,22 s, a2 + a3
    duran 0,08 s y el ataque entero 0,46 s.
  - **Proporciones.** Siguen igual: la anticipación casi triplica el golpe.
  - **Ventana de daño.** Los eventos se mueven con el clip, así que el daño sigue llegando solo en
    a2 y a3.
  - **Descanso.** Baja de 0,7 s a 0,45 s.
  - **Tiempos del encargo.** No se tocan: la tabla de `ConstructorEcoIra` sigue igual. Para
    volver a ellos basta con poner `velocidadAtaque` a 1.

### 2. El Golem de Ceniza moría en la lava

- **Problema.** La lava de la sección 2 está a ras de suelo, y el Golem persigue al jugador por el
  suelo, así que la pisaba y moría. El autor decidió que no debe morir en ella (N9).
- **Solución.**
  - **El dato.** Nuevo `EnemyData.inmuneALava`, que solo tiene el Golem.
  - **La regla.** `EnemyController` implementa la interfaz nueva `IInmuneALava`
    (`Core/Combate`), y `Lava` no quema a quien la cumple. Así la lava no depende de los enemigos.
  - **El vacío.** Caer al vacío sigue matándolo.

### 3. La Centinela Ardiente se sentía fácil

- **Problema.** El combate no transmitía la ira del mundo.
- **Causa.**
  - Un solo ataque cada vez, con pausas de 0,9 s.
  - El espadazo, que es el que abre la ventana, era siempre el mismo.
  - La fase 2 solo añadía erupciones.
  - Con 24 de vida y una ventana de 1,2 s a doble daño, caía en unas cuatro ventanas.
- **Solución.** Casi todo son datos. Solo hay un patrón nuevo, el salto, y tres campos nuevos.
  - **Patrones ampliados:**
    - `PatronEspadazo.golpes`: el espadazo es un combo y solo el último deja la espada
      clavada;
    - `PatronOndaSuelo.repeticiones`: hay que saltar una onda detrás de otra.
  - **Patrón nuevo, `PatronSaltoAplastante`:**
    - salta hacia el jugador y marca en el suelo dónde caerá desde que despega;
    - al aterrizar quema ese tramo (2 de daño) y suelta una onda a cada lado;
    - el salto es un tiro parabólico (`BossController.Saltar`) que cae justo cuando la marca
      empieza a quemar.
  - **Tres fases.** Cada una persigue más rápido (nuevo `FaseJefe.multiplicadorVelocidad`) y
    descansa menos:

    | Fase | Desde | Velocidad | Pausa | Ataques, en orden |
    |---|---|---|---|---|
    | 1 | 100 % | ×1 (2,4 u/s) | 0,7 s | espadazo ×2, onda, salto, espadazo ×2, onda |
    | 2 · Furia | 50 % | ×1,3 | 0,5 s | onda doble, espadazo ×3, erupción (4), salto, espadazo ×3, erupción |
    | 3 · Ira desatada | 25 % | ×1,6 | 0,3 s | onda doble, ráfaga de erupciones (6), salto, espadazo ×3, salto, onda doble, espadazo ×3 |

  - **El rugido.** Al empezar las fases 2 y 3 ruge 0,9 s, quieta y sin atacar: es la señal de que
    el combate cambia. Lo hace el nuevo paso `EsperaAlEmpezarFase` del Template Method. En la
    fase 3, además, se pone al rojo.
  - **Vida y velocidad.** La vida sube de 24 a 30 y la velocidad de 2,2 a 2,4 u/s.
  - **Ventana vulnerable.** En la fase 1 sigue durando 1,2 s, como pide el Anexo A. En las fases 2
    y 3, el espadazo de furia la deja en 0,9 s.
  - **Límites, para que siga siendo justo:**
    - ningún aviso baja de 0,3 s;
    - cada fase tiene un espadazo que abre la ventana, así que siempre hay por dónde ganarle.
  - **Para la tesis.** La tercera fase no está en el Anexo A: hay que anotarla en el capítulo de
    diseño.

### 4. La presentación del jefe

- **Problema.** El autor quería que, al entrar en la arena, una cinemática corta diera el diálogo
  del jefe. La FSM global ya admitía PLAYING → CUTSCENE → PLAYING para «memoria, jefe o cierre»,
  pero nada la pedía al llegar al jefe.
- **Solución.**
  - **Dónde va.** Nuevo campo `LevelData.cinematicaJefe`. El generador de narrativa crea
    `Cinematica_Jefe_Ira`, `_Tristeza`, `_Miedo` y `_Culpa`, con el nombre del jefe como título y
    una línea pendiente de guion. El final no tiene: ya se presenta en su cinemática de entrada.
  - **El flujo.** `ArenaJefe.Entrar` cierra las puertas y pide la cinemática.
    - **La cámara.** Se desliza hasta encuadrar a la Centinela (`CameraMovement.Presentar`). Usa
      tiempo sin escala, porque la cinemática para el juego.
    - **El combate.** Empieza al volver a PLAYING, tanto si la cinemática termina como si se salta.
    - **Cuándo se ve.** Una vez por cada carga del nivel: al reintentar tras morir, el combate
      empieza directamente.
  - **Cómo se ve.** Nuevo `CutsceneData.sobreElJuego`: en vez de fondo negro, un velo al 55 % y
    dos franjas de cine sobre la escena congelada. Así se ve la arena con el jefe mientras habla,
    aunque todavía no haya imágenes. Si se dibujan, las imágenes la tapan como en cualquier otra
    cinemática.
  - **El diálogo.** El diálogo antes del jefe va en la cinemática. Las líneas «antes del jefe» del
    HUD solo se usan en un nivel sin presentación, para no decir lo mismo dos veces.

### 5. Decisiones del autor aplicadas

- **Tecla E (N4):** la acción `Interact` ya no lleva la interacción *Hold* de fábrica, así que basta
  con pulsar.
- **Registro emocional (N12):** `pedirParticipante` está apagado. La pregunta se queda como estaba.
- **Sin cambios:** la hitbox de la espada (N13), el control (N1) y el fondo (N14) se quedan como
  están.
- **Ramas (N6):** `main` avanzó hasta `enemigo-eco-ira` (70f32b1).
  - No hizo falta un commit de fusión, porque cada rama sale de la anterior.
  - Las ramas de cada fase se conservan.
  - No se ha subido nada al remoto.
- **Cambio del autor en el Nivel 1:** el primer Eco, en x = 12,5, tiene escala 1,8 × 1,5. Se
  conserva tal cual; la pregunta va en PENDIENTES (N18).

### 6. La Centinela lleva espada, no martillo

- **Problema.** El autor señaló que los conceptos de la Centinela (`Referencias/Arte/Jefes`) llevan
  un mandoble, mientras el juego hablaba de martillazos.
- **Causa.** El Anexo A describe la fase 1 como «martillazo directo». Se tomó al pie de la letra:
  el patrón se llamó `PatronMartillazo` y la ficha de arte pedía un martillo.
- **Solución.** Solo cambian los nombres: la mecánica funciona igual con cualquier arma.
  - **El golpe.** Es un tajo de arriba abajo y la espada queda clavada en el suelo: esa es la
    ventana vulnerable.
  - **Los nombres.** `PatronMartillazo` pasa a `PatronEspadazo`, y los assets `Martillazo` y
    `MartillazoFuria` a `Espadazo` y `EspadazoFuria`. Se renombraron con su `.meta`: el GUID no
    cambia y `CentinelaArdiente.asset` los sigue encontrando.
  - **El alcance.** La zona del golpe (2,6 × 2,6 u delante) ya cubre un mandoble del largo de sus
    piernas, como el de los conceptos. No se toca.
  - **El arte.** La ficha de B5 en `PENDIENTES.md` describe la espada y dice qué pose de los
    conceptos sirve para cada clip. Los conceptos no se importan: el autor los va a trabajar
    siguiendo la ficha.
  - **Entradas anteriores.** Las de este CHANGELOG conservan el nombre antiguo.
- **Para la tesis.** Anotar que el «martillazo» del Anexo A se implementó como espadazo.
- **Verificación.** Tras el cambio se repitió todo: compilación sin errores ni avisos, 234 de 234
  pruebas EditMode y 99 de 99 PlayMode. La regeneración no creó assets nuevos.

### Verificación

- **Compilación** con `dotnet build`: 0 errores y 0 avisos.
- **Compilación y regeneración** en Unity 6000.6.3f1 en modo batch: 0 errores y 0 avisos.
- **Pruebas EditMode: 234 de 234 correctas.** Hay 6 nuevas:
  - la Centinela tiene tres fases;
  - cada fase es más rápida y descansa menos;
  - ningún aviso baja de 0,3 s;
  - todas las fases abren la ventana y tienen salto;
  - solo el Golem es inmune a la lava, y el Eco descansa menos;
  - cada jefe, salvo el final, tiene su presentación sobre el juego, con su nombre y su diálogo;
  - el registro no pide código;
  - `Interact` no lleva *Hold*;
  - el controlador del Eco tiene `VelocidadAtaque` en el estado Attack.
- **Pruebas PlayMode: 99 de 99 correctas.** Hay 5 nuevas y 3 ampliadas:
  - el espadazo son dos golpes y la ventana solo se abre tras el segundo;
  - el salto marca la caída bajo el jugador, no hace daño en el aire, cae donde marcó, suelta
    las ondas y quema al jugador que no se apartó;
  - la fase 3 llega al 25 % (1 → 2 → 3), ruge sin atacar, persigue más rápido y se pone al rojo;
  - el Golem pisa la lava sin perder vida;
  - el golpe del Eco dura la anticipación y la ventana del diseño divididas entre 1,6, con el
    daño solo en a2 y a3;
  - al entrar en la arena del Nivel 1:
    - primero la presentación, con las puertas cerradas, el jefe quieto, la arena visible
      detrás y la cámara en el jefe;
    - al terminar, el combate y la cámara de vuelta;
    - el diálogo va en la presentación y no se repite en el HUD;
    - sin presentación, la línea va en el HUD.
- **Dos fallos en la primera pasada (98 de 100), los dos de las propias pruebas:**
  - `MorirEnLaArena_ReiniciaElCombate` no sabía de la presentación: esperaba el Game Over con el
    juego todavía en la cinemática;
  - la prueba nueva de «la presentación no se repite» volvía a meter al jugador en la arena sin
    pasos de física entre medias, y Box2D no registraba la salida del trigger. La prueba antigua
    ya documentaba ese caso.

  Las dos quedan unidas en `MorirEnLaArena_ReiniciaElCombate_SinRepetirLaPresentacion`, que salta
  la presentación y espera esos pasos. La segunda pasada completa dio 99 de 99.
- **Regenerar todo otra vez** solo vuelve a tocar `TorretaFuego.prefab` y `ProjectSettings.asset`,
  que se reescriben solos en cada regeneración y se descartan.

### Qué probar a mano (desde `Menu_Inicial`, «Nueva partida»)

- **Eco (x ≈ 12):** el golpe sale mucho antes. Sigue viéndose venir (a1 con las grietas
  encendidas), pero ya no da tiempo a pasearse delante de él.
- **Golem (sección 2):** si te persigue hasta la lava, la cruza sin morir.
- **Arena:**
  - al cruzar la entrada se cierra la puerta, la cámara va hacia la Centinela y aparece su nombre
    sobre la arena oscurecida;
  - pasado 1 s, cualquier tecla la salta;
  - al terminar, empieza el combate;
  - si mueres y vuelves, ya no sale.
- **Centinela:**
  - el espadazo son dos golpes y solo después se queda clavada la espada;
  - el salto marca en el suelo dónde caerá: apártate y salta la onda;
  - al 50 % ruge y se acelera;
  - al 25 % ruge otra vez, se pone al rojo y apenas descansa.
  - Si se pasa de difícil, dime en qué ataque mueres.
- **Tecla E:** una pulsación basta (se notará en la Fase 10, con antorchas y memorias).
- **Fin del nivel:** el registro emocional ya no pide código de participante.

---

## Eco de la Ira: el primer enemigo con arte (2026-10-02)

Rama `enemigo-eco-ira`, que sale de `fondo-mundo1`. Llegaron los 12 fotogramas del enemigo del
Mundo 1 (`Tesis/Enemigo/limpios`), con un encargo: importarlos sin tocar su lienzo, crear
`EcoIra.controller` con tiempos deliberadamente desiguales, conectar el Animator al
`EnemyController` sin reescribirlo, y añadir una ventana de daño por eventos de animación, un
multiplicador de ritmo, la muerte con partículas y un squash en el ataque. Antes de montar nada
se revisó el plan, y salieron varios problemas.

### 1. Los eventos de animación no habrían llegado al EnemyController

- **Problema.** Los eventos de un clip solo llaman a métodos de los scripts del objeto que tiene el
  Animator. Los receptores del prototipo (`CancelAttack`, `DesactiveDamage`) están en el
  `EnemyController`, en la raíz, y el Animator va en el hijo `Pivote/Sprite`.
- **Solución.** **`EnemigoAnimacion`** (`Scripts/Enemies`), en el mismo objeto que el Animator,
  recibe los eventos y se los pasa al `EnemyController`. También lleva lo que es solo de la
  presentación: `velocidadAnimacion`, el destello del golpe y el final de la muerte. La FSM sigue
  siendo la que decide; el componente solo le avisa de lo que pasa en el clip.

### 2. La ventana del golpe iba por tiempo y habría cortado el ataque

- **Problema.** `EnemyAttackState` abría y cerraba el arma con un temporizador en `Update`, con
  los tiempos de `EnemyData`: el Soldado de lava ataca 0,4 s y el clip del Eco dura 0,73 s. La FSM
  habría dado por terminado el ataque a mitad del clip, y la ventana no habría coincidido con a2
  y a3.
- **Solución** (diff revisado por el autor antes de aplicarlo):
  - con animación, el arma la abren y la cierran los eventos `AbrirGolpe` (primer instante de a2)
    y `CerrarGolpe` (primer instante de a4), y el ataque termina con `FinAtaque`, al acabar el
    clip. Fuera de ATTACK los eventos no hacen nada;
  - el límite de ATTACK pasa a ser la duración del clip a su ritmo, con 0,25 s de margen por si
    el evento final no llegara;
  - se conserva la garantía de la Fase 4: el arma pasa al menos un paso de física abierta. Si un
    tirón de fotogramas junta a2, a3 y a4, el cierre espera a ese paso;
  - sin animación (los enemigos que aún son cápsulas), todo sigue igual que antes.

### 3. Un «tinte blanco» en el SpriteRenderer no se ve

- **Problema.** El encargo pedía el daño como tinte blanco del SpriteRenderer durante 0,1 s.
- **Causa.** El color del SpriteRenderer multiplica la textura. El Eco ya va con tinte blanco (el
  arte final no lleva tinte), así que ponerlo en blanco no cambia nada.
- **Solución.** Un shader propio, **`Fragmentum/SiluetaSprite`** (`Assets/Shaders`), pinta el
  sprite de un color liso conservando solo su alfa, sin luces. Al recibir un golpe,
  `EnemigoAnimacion` le pone al sprite el material `Destello` durante 0,1 s y luego le devuelve
  el suyo. Sigue sin haber arte de daño.

### 4. Al Animator le faltaban transiciones que la FSM necesita

- **Problema.** Con solo las transiciones del encargo, el Eco:
  - no volvería a Idle al perder al jugador;
  - terminaría la animación del golpe aunque un golpe de Kael interrumpiera el ataque, es decir,
    golpearía sin hacer daño;
  - con «Cualquiera → Death» reiniciaría el clip de muerte en cada fotograma, porque Death sigue
    activo, y la muerte no acabaría nunca.

  Las pruebas encontraron dos casos más:
  - al entrar en ATTACK la FSM apaga `MovEn`, y un «Run → Idle si no se mueve» se adelantaba a
    «Run → Attack»;
  - la FSM encadena transiciones en un mismo fotograma. Con el jugador ya a tiro pasa de IDLE a
    ATTACK (por CHASE) sin que el Animator llegue a ver `MovEn` encendido, y el Eco atacaba
    quieto en Idle.
- **Solución.** `EcoIra.controller` usa los parámetros que la FSM ya escribía (`MovEn`, `Attack`,
  `Damage`, `Death`) y añade a las del encargo:
  - **Idle → Attack** si el ataque empieza en el mismo fotograma en que lo detecta;
  - **Run → Idle** cuando pierde al jugador, salvo aturdido (`Damage`, así los ojos no se apagan
    al recibir un golpe) o atacando (`Attack`). En Idle y en Run, atacar se comprueba primero;
  - **Attack → Run sin esperar** cuando un golpe interrumpe el ataque;
  - *Can Transition To Self* desactivado en «Cualquiera → Death».

  Todas van sin Exit Time salvo Attack → Run al terminar el golpe, y sin mezcla (duración 0).

### 5. Las piernas habrían patinado

- **Problema.** El ciclo r1–r4 a 0,12 s es un solo paso de unas 0,62 u: avanza 1,3 u/s. El
  Soldado de lava patrulla a 2,5 u/s y persigue a 5. A esa velocidad los pies se habrían
  deslizado, y durante el cooldown, quieto a tiro del jugador, el Eco habría corrido en el sitio.
- **Solución** (decisión del autor: el ritmo según la velocidad, como Kael):
  - el estado Run se acelera con el parámetro `VelocidadCorrer`, que es la velocidad real del
    cuerpo entre la del ciclo dibujado. Quieto se congela, sin correr en el sitio, y aturdido no
    corre;
  - `velocidadAnimacion` acelera todo el Animator, pero se descuenta en Run: las piernas siguen al
    cuerpo con cualquier ritmo;
  - `EcoIra.asset` patrulla a 1,3 u/s (justo los 0,12 s del encargo) y persigue a 2,6 u/s (el
    ciclo al doble). A 5 u/s cada fotograma duraría 0,03 s y se vería frenético.

### 6. El daño por contacto contradecía el criterio del golpe

- **Problema.** Fuera de a2 y a3 el jugador debe poder pasarle por encima sin daño, pero todos los
  enemigos de la Fase 4 dañan al tocarlos (`danioContacto = 1`).
- **Solución.** El Eco tiene sus propios datos, **`EcoIra.asset`**, con `danioContacto = 0`. El
  resto sale del Soldado de lava (vida 3, daño 1, visión de 7 u, cooldown 0,7 s). Para que se vean
  sus pausas en Idle, espera 1,6 s en cada punto de patrulla, dos ciclos de I1 a I2.

### 7. La muerte no esperaba al clip

- **Problema.** `Morir()` apagaba los colliders y destruía al enemigo pasado `deathDelay`, con un
  tiempo fijo y sin saber nada del clip. Unas partículas hijas del enemigo habrían desaparecido
  con él a medio vuelo.
- **Solución.** Con animación, `Morir()` apaga ya el arma y el contacto (un enemigo moribundo no
  hace daño) y deja el resto a `EnemigoAnimacion`. Al acabar el clip (`FinMuerte`):
  - apaga el collider del cuerpo;
  - suelta las partículas desde la **cavidad del pecho**, a (0,04; 0,28) u de los pies, tal como
    está dibujada en d3, el fotograma que se ve al acabar. Las partículas se separan del enemigo
    para que destruirlo no las corte, y se borran solas;
  - funde el sprite en 0,4 s y destruye al enemigo.

  Si el evento no llegara, se remata igual 1 s después de lo previsto.

### El montaje

`ConstructorEcoIra` (menú *Fragmentum → Enemigos → Importar el arte del Eco de la Ira*, y dentro de
*Regenerar todo*) importa los fotogramas, crea los clips y el controlador y arma
`Assets/Prefabs/Enemigos/EcoIra.prefab`.

- **Arte:** `Assets/Sprites/Enemigos/EcoIra/` (los 12 PNG con su nombre original; D1 no se usa).
  `ImportadorEnemigos` fija sus ajustes en cada importación:
  - Sprite Single, Point, sin compresión, sin mipmaps, alfa como transparencia y Full Rect;
  - el lienzo entero, de 1229×905;
  - el pivote común (0,345; 0,014), que es el ancla de los pies en el píxel (424, 892).
- **Escala: 708 px por unidad.** Kael mide 113 px de arte a 64 px por unidad, es decir 1,77 u.
  El Eco corriendo mide 750 px, el 60 % de Kael (1,06 u). El arte es pixel art ampliado: cada
  píxel del dibujo son unos 10 px de la textura, y a 708 PPU mide 0,014 u, casi lo mismo que un
  píxel de Kael (0,016 u). Los dos se ven de la misma resolución.
- **Clips** (`Assets/Animaciones/Enemigos/EcoIra/`):

  | Estado | Fotogramas | Por fotograma | Bucle | Eventos |
  |---|---|---|---|---|
  | Idle | I1, I2 | 0,40 s | sí | — |
  | Run | r1–r4 | 0,12 s (a ritmo 1) | sí | — |
  | Attack | a1 · a2 · a3 · a4 | 0,35 · 0,08 · 0,05 · 0,25 s | no | `AbrirGolpe` 0,35 · `CerrarGolpe` 0,48 · `FinAtaque` 0,73 |
  | Death | d2, d3 | 0,15 s | no | `FinMuerte` 0,30 |

- **Squash:** el clip de ataque anima la escala X del sprite en escalones: 1 en a1, 1,15 en a2, 1
  en a3, 0,9 en a4 y 1 al acabar.
  - Va en el clip y no en eventos: así cambia con el fotograma exacto, escala con
    `velocidadAnimacion` y vuelve a 1 si el ataque se interrumpe.
  - Va en `Sprite` y no en `Pivote`, porque el `EnemyController` reescribe la escala del
    `Pivote` para voltear.
  - El arma no se estira.
- **Prefab `EcoIra`:**
  - la raíz, en la capa Enemy: Rigidbody2D, cápsula del cuerpo (0,42 × 1 u), Health y
    `EnemyController` con `EcoIra.asset`;
  - `Pivote`, lo que se voltea al girar:
    - `Sprite`, con el SpriteRenderer, el Animator (siempre animado, también fuera de cámara,
      para que lleguen los eventos) y `EnemigoAnimacion`;
    - `Arma`, en la capa EnemyWeapon: un trigger de 0,25 a 1,25 u, porque el puño de a2,
      estirado, llega a 1,29 u;
    - `Pecho`, con las partículas: 28 cuadrados de 2 a 3 píxeles del dibujo, brasa y ceniza con
      los colores del arte, en abanico hacia arriba;
  - `Contacto`, apagado porque el Eco no daña por contacto.
- **Escenas:**
  - en el Nivel 1, el Eco sustituye al Soldado de lava provisional, en su misma posición y con
    su ruta (*Fragmentum → Enemigos → Sustituir los Soldados de lava de la escena abierta*);
  - `PreparadorNivel` ya pone el Eco en los recorridos provisionales de la Ira;
  - `PruebaEnemigos` tiene una zona nueva en x = 128.

### Decisiones que ajustan el encargo

1. **Carpetas de la convención del proyecto** (decisión del autor): `Assets/Sprites/Enemigos/EcoIra`
   y `Assets/Animaciones/Enemigos/EcoIra`, como Kael y los fondos, en lugar de `Assets/Art`.
2. **Patrulla con Run y pausas en Idle** (decisión del autor). Al patrullar el Eco lleva los ojos
   encendidos; en las pausas, apagados.
3. **El ritmo de Run sigue a la velocidad real** (decisión del autor), y por eso el Eco persigue a
   2,6 u/s y no a 5. Se cambia en `EcoIra.asset` y las piernas se ajustan solas.
4. **Tres transiciones más y Death sin volver a sí misma** (problema 4).
5. **El arma y el contacto se apagan al morir, no al acabar el clip.** El cuerpo sí espera al
   clip, como pedía el encargo.
6. **En a3 el squash vuelve a 1**: el encargo solo fija a2 y a4.
7. **`EnemyDeadState` no cambia**: todo cabe en `EnemyController.Morir()`.

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **228 de 228 correctas** (10 nuevas en `EcoIraTests`):
  - los doce fotogramas son pixel art con el lienzo entero, a 708 PPU y con el mismo pivote, y la
    planta de los pies de cada PNG cae en la fila del pivote;
  - corriendo, el Eco mide el 60 % de Kael;
  - los clips tienen los tiempos del diseño, sin fotogramas de relleno, y la anticipación dura
    más de 2,5 veces el golpe;
  - los eventos van al principio de a2 y de a4 y al final del ataque y de la muerte;
  - el squash de a2 y a4;
  - el controlador: estados, transiciones y Exit Time;
  - el prefab completo, y el arma cubre el brazo de a2 y a3.
- Pruebas PlayMode: **94 de 94 correctas** (8 nuevas en `EcoIraPlayModeTests`, con el jugador real):
  - el arma solo está abierta mientras se ven a2 y a3, el daño llega en esos fotogramas, a1 dura
    0,35 s y el golpe 0,13 s;
  - a `velocidadAnimacion` 0,5 todo dura el doble y la ventana sigue en a2 y a3;
  - un golpe en a1 cancela el ataque sin que el arma llegue a abrirse;
  - pasando por Idle, Run, Attack y Death, la base del sprite no se mueve ni 0,005 u;
  - corriendo, el ritmo de las piernas es el de la velocidad real; parado en el borde, el clip
    se congela;
  - patrulla con Run y se para en Idle en cada punto;
  - el destello dura 0,1 s;
  - la muerte espera al clip (0,3 s), apaga el cuerpo, suelta las partículas desde el pecho, se
    funde y desaparece a los 0,7 s.
- Regenerar el Eco dos veces deja sus archivos idénticos.
- Render en modo batch a 1080p y 720p:
  - los doce fotogramas apoyan los pies en la misma línea;
  - corriendo, el Eco mide 118 px de pantalla frente a los 196 de Kael, el 60 %;
  - el pixel art se ve nítido, sin suavizado;
  - la silueta blanca, el volteo y las partículas desde el pecho salen como se espera.
- Desplazando la cámara 1/3 y 2/3 de píxel, cambia de columna el 17–30 % de los píxeles del Eco,
  frente al 4–8 % de Kael. No se emborrona, pero al moverse su textura «repta» un poco más que la
  de Kael (ver «Notas sobre el arte»).

### Pasos manuales en el Editor

Ninguno: el prefab, el Nivel 1 y `PruebaEnemigos` ya tienen el Eco.

- **Otro Eco con los mismos clips:** duplica `EcoIra.prefab`, cambia sus datos en su
  `EnemyController` (un `EnemyData` propio) y su `velocidadAnimacion` en `EnemigoAnimacion`. Si
  cambia el dibujo, usa un *Animator Override Controller* sobre `EcoIra.controller`: los eventos
  van en los clips y `EnemigoAnimacion` lee sus duraciones de ellos.
- **Para cambiar un ajuste de importación,** cámbialo en `ConstructorEcoIra.AjustarImportador`:
  `ImportadorEnemigos` lo restablece en cada importación.

### Qué revisar en el Editor

Abre `Assets/Scenes/Pruebas/PruebaEnemigos.unity`, dale a Play y camina hasta x = 128 (F2 muestra
el estado del enemigo).

- **No salta:** al pasar de Idle a Run, de Run a Attack y al morir, los pies no se separan del
  suelo. En la escena, con `EcoIra` seleccionado, `Pivote/Sprite` está siempre en (0, 0).
- **Anticipación y golpe:** a1 se ve venir (0,35 s, con las grietas de lava encendidas) y a2–a3
  son un latigazo. Con el Gizmo del collider visible, `Pivote/Arma` solo se enciende en a2 y a3.
- **Daño:** si te quedas pegado al Eco sin que ataque, no te hace daño. Solo duele si te toca el
  golpe.
- **Destello:** al golpearlo, el Eco se vuelve una silueta blanca un instante.
- **Muerte:** cae (d2 → d3), salen brasas y ceniza del pecho, se funde y desaparece.
- **Piernas:** al perseguirte no patina, y quieto a tu lado durante el cooldown no corre en el
  sitio. Al patrullar se para en cada punto con los ojos apagados.
- **Nitidez:** en la vista Game a 1920×1080, los bordes se ven duros, sin suavizado.
- **Ritmo:** en `Pivote/Sprite → EnemigoAnimacion`, `velocidadAnimacion` cambia en Play el ritmo
  de idle, ataque y muerte. A 2, la anticipación dura 0,175 s: menos que el tiempo de reacción
  habitual.
- **Consola:** sin errores ni avisos del Eco.

### Notas sobre el arte (no se ha tocado)

- **La línea de suelo parpadea.** r1–r4 y a1 llevan una línea negra bajo los pies; idle, a2–a4 y
  la muerte no. Esa «sombra» aparece y desaparece al cambiar de estado.
- **La textura «repta» al moverse.** El arte viene de JPG ampliados: cada píxel del dibujo son ~10
  px de la textura, con ruido de un píxel dentro. Con Point y sin mipmaps, al moverse se nota
  algo más que en Kael. Si molesta, la solución es reducir el arte a 1× (un píxel de textura por
  píxel del dibujo), conservando la alineación. El encargo pedía no redimensionar, así que queda
  como opción.
- **Memoria:** sin compresión, los 12 fotogramas ocupan ~53 MB de textura. Los demás Ecos los
  comparten.
- **D1** está en `limpios/` a otra escala y no se usa.

---

## Fondo con parallax del Mundo 1 (2026-10-01)

Rama `fondo-mundo1`, que sale de `cinematica-y-mapa`. Llegaron las seis capas del fondo de la
Fortaleza Volcánica (`Tesis/Imagestesis2/Mapas/limpias`), con un encargo de montaje: seis
profundidades, `ParallaxMovement` del prototipo para las capas que se repiten y un
`ParallaxProps` nuevo para los hitos sueltos. Antes de montar nada se revisó el plan, y salieron
varios problemas.

### 1. El parallax del prototipo no se movía con URP

- **Problema.** `ParallaxMovement` desplaza el offset de la textura de cada capa, pero en la v2
  las capas no se habrían movido.
- **Causa.** La v2 usa URP con el 2D Renderer. Sus shaders de sprites (`Sprite-Lit-Default`,
  `Sprite-Unlit-Default`, `Mesh2D`) dibujan la textura con su UV tal cual: ignoran el tiling y el
  offset. Además, el script escribía `_MainTex`, que en URP se llama `_BaseMap`. Y ni siquiera
  estaba en la v2: solo existía en el prototipo.
- **Solución.** `ParallaxMovement` se trae a la v2 (`Levels/Escenario`) y se corrige:
  - las capas repetibles usan un material **URP/Unlit transparente**, que sí aplica el offset;
  - el desplazamiento se escribe con un `MaterialPropertyBlock` en la textura principal del
    shader, sea cual sea su nombre (`_BaseMap` o `_MainTex`). Avisa si el shader es de sprites;
  - ya no fija el objeto en (cámara − 1, y, 9.92): ese −1 dejaba un hueco en el borde derecho y el
    9.92 movía todas las capas en Z.

### 2. Las velocidades no seguían la profundidad

- **Problema.** Con las capas a la altura de la pantalla y `parallaxSpeed = 0,05`, las
  velocidades reales habrían sido 0,35 (fondo lejano), 0,42 (muro) y 1,39 (primer plano), en
  lugar de 0,2, 0,6 y 0,8. La catarata (0,6, con `ParallaxProps`) habría resbalado sobre el muro y
  el primer plano habría corrido más que el suelo.
- **Causa.** El script sumaba el mismo desplazamiento de UV a todas las capas, pero las texturas
  miden distinto: 1376 px el cielo y 2752 el fondo lejano. Un mismo desplazamiento en UV es el
  doble de distancia en pantalla en la textura el doble de ancha.
- **Solución.** **`CalculoParallax`** reúne las cuentas que usan las dos técnicas:
  - la velocidad sale de la Z como en el prototipo: `1 - (z - zCámara) / distancia de la capa más lejana`;
  - el desplazamiento de textura se divide por el ancho de la textura en el mundo, así que la
    velocidad de la tabla es literal: 0,2 = 0,2 u en pantalla por cada unidad de cámara;
  - `ParallaxProps` lleva su velocidad escrita, y una prueba comprueba que es la que daría su Z.
  - `parallaxSpeed` desaparece: era el factor que lo descuadraba.

### 3. El fondo mide una pantalla y la cámara sube y baja

- **Problema.** Cada imagen mide justo una pantalla de alto (768 px), pero la cámara sigue al
  jugador también en vertical. Al saltar habría asomado el borde.
- **Solución.**
  - Las capas siguen a la cámara en vertical: las repetibles enteras, y los props con
    `seguirCamaraEnY` (su Y local es su altura en pantalla).
  - Las capas repetibles se ajustan cada fotograma al tamaño de la vista. Los props se escalan
    igual (`altoReferencia`). Así el fondo encaja con cualquier cámara: el Nivel 1 usa tamaño
    ortográfico 4,85 y la plantilla de escenas, 5.
  - Los dos scripts se ejecutan después de `CameraMovement` (`DefaultExecutionOrder(100)`). Si
    fuera antes, irían un fotograma por detrás y asomaría el borde.

### 4. El muro tapaba medio montaje y los hitos no cabían

- **Problema.**
  - El muro es opaco en un 92 % (del todo por debajo del 29 % superior). Como capa continua en
    todo el nivel, habría tapado el cielo, las montañas y la fortaleza.
  - La fortaleza mide 17,4 u, más que la pantalla. A velocidad 0,4 tarda 87 u de cámara en
    cruzarla, y la cámara del Nivel 1 solo recorre 75: tres fortalezas se habrían visto a la vez.
  - `ParallaxProps` anclaba los props a la posición de la cámara en su `Start`. Al continuar desde
    un checkpoint guardado, la cámara arranca en otro sitio y los hitos habrían salido en otro
    lugar. Además, `LevelManager.Start` coloca la cámara antes o después según el orden de
    ejecución.
- **Solución** (decisiones del autor: muro en tramos y una fortaleza):
  - **el muro va en tramos**: un sprite en modo *Tiled* del ancho justo para tapar la pantalla
    mientras la cámara recorre el tramo. En el Nivel 1 tapa la arena del jefe, el interior de la
    fortaleza;
  - **cada hito se ancla en su X**: se ve centrado cuando la cámara pasa por donde está colocado,
    empiece donde empiece la partida. Así se coloca en el editor donde se quiere ver;
  - **`RepartoHitos`** guarda dónde va cada hito como fracción del nivel y los coloca según la
    longitud de cada nivel. Avisa si dos hitos de la misma capa se ven a la vez. En el Nivel 1:
    - la fortaleza se ve centrada en x = 24;
    - la catarata se ve centrada en x = 68 y cae sobre el muro;
    - el muro tapa la pantalla de x = 60 al final.

### 5. La importación habría estropeado el pixel art

- **Problema.** Los ajustes del encargo (Point, sin compresión, sin mipmaps) no bastaban.
- **Causa.** En el tipo *Default*, «Non-Power of 2» viene en «To Nearest»: Unity habría reescalado
  las texturas de 2752×768 a 2048×1024. Además, el tamaño máximo por defecto (2048) las habría
  reducido.
- **Solución.**
  - **`ImportadorFondos`** (un `AssetPostprocessor`) fija en todo `Assets/Sprites/Fondos` el
    filtro Point, sin compresión, sin mipmaps, sin reescalar y con tamaño máximo 4096.
  - Las capas repetibles se repiten en horizontal (Repeat) y no en vertical (Clamp): con Repeat y
    filtro Point podría asomar una línea de la fila contraria arriba o abajo.
  - Los props son sprites Single y Full Rect (el modo Tiled lo exige), a 79,2 px por unidad, la
    misma escala que las repetibles.

### El montaje

`ConstructorFondoMundo1` (menú *Fragmentum → Escenario → Importar el fondo del Mundo 1*, y dentro
de *Regenerar todo*) importa las imágenes, crea los materiales y arma
`Assets/Prefabs/Fondos/FondoMundo1.prefab`:

| Capa | Z | Imagen | Técnica | Velocidad | Orden |
|---|---|---|---|---|---|
| Cielo | 40 | `Skie.png` | Quad + `ParallaxMovement` | 0,0 | −60 |
| Fondo lejano | 30 | `Fondo lejano tileable.png` | Quad + `ParallaxMovement` | 0,2 | −50 |
| Fortaleza | 20 | `Plano medio.png` | Sprite + `ParallaxProps` (1) | 0,4 | −40 |
| Muro | 10 | `Muro tileable.png` | Sprite Tiled + `ParallaxProps` (tramos) | 0,6 | −30 |
| Catarata | 10 | `Prop catarata.png` | Sprite + `ParallaxProps` (1) | 0,6 | −29 |
| Primer plano | 0 | `Primer plano tileable.png` | Quad + `ParallaxMovement` | 0,8 | −10 |

- **El orden de dibujo** va de lejos a cerca y siempre por debajo del juego (el escenario pintado
  usa −2 en adelante). El muro y la catarata comparten Z, pero no orden: la catarata va delante.
- **Todo el fondo es unlit:** las capas repetibles con URP/Unlit y los props con
  `Sprite-Unlit-Default`. Ni la luz del jugador ni la de la lava lo iluminan.
- **`PreparadorNivel`** pone el fondo en las escenas de nivel del mundo de la Ira y reparte sus
  hitos (paso 9). El Nivel 1 ya lo tiene.

### Decisiones que ajustan el encargo

1. **El muro no es una capa repetible sino tramos**, con `ParallaxProps` como la catarata. Así
   los dos van a la misma velocidad por construcción.
2. **Una fortaleza y una catarata en el Nivel 1**, no tres y dos. La fortaleza no cabe más
   veces: el nivel es corto para su tamaño. La catarata necesita una pared de la que caer: la
   primera, en campo abierto (x = 12), no encajaba con nada y se quitó tras verla en el Editor.
   Queda la que cae sobre el muro. En niveles más largos se añaden posiciones en `RepartoHitos`.
3. **El arte va en `Assets/Sprites/Fondos/Mundo1Ira`**, junto al resto de sprites, y los scripts en
   `Assets/Scripts/Levels/Escenario`, junto a `CameraMovement`, con el namespace del proyecto.
4. **Las imágenes conservan su nombre original**, para poder volver a copiarlas desde
   `Imagestesis2/Mapas/limpias` si cambian.

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **218 de 218 correctas** (14 nuevas en `FondoParallaxTests`):
  - las cuentas: la tabla de velocidades sale de la Z; dos texturas de distinto ancho se mueven
    igual en pantalla; con velocidad 1 la textura queda fija en el mundo; un tramo del ancho
    calculado tapa la pantalla; un hito se ve centrado al pasar la cámara por su ancla;
  - la importación: pixel art sin reescalar, repetibles con Repeat/Clamp, sprites a 79,2 px por
    unidad, y las texturas que se repiten encajan borde con borde;
  - el prefab: seis capas con su técnica y su material, velocidades coherentes con la Z y orden
    de dibujo de lejos a cerca, por debajo del juego;
  - el reparto: el Nivel 1 tiene una fortaleza y una catarata que cae dentro del tramo de muro,
    y el muro tapa la arena; en un nivel de 300 u se copian hitos, se ensancha el tramo y se avisa si dos
    fortalezas se ven a la vez.
- Pruebas PlayMode: **86 de 86 correctas** (5 nuevas en `ParallaxPlayModeTests`, con una cámara
  que se mueve):
  - cada una de las seis capas se desplaza en pantalla a su velocidad;
  - siguen a la cámara en vertical y tapan la vista;
  - el muro y su catarata van juntos;
  - los hitos salen en su sitio aunque la cámara empiece en el checkpoint;
  - con otro tamaño de cámara, todo se ajusta a la vista.
- Capturas del Nivel 1 renderizadas en modo batch con la cámara en ocho posiciones: pixel art
  nítido, las capas con alfa dejan ver las de atrás y no hay costuras en el muro.

### Pasos manuales en el Editor

Ninguno: el Nivel 1 ya tiene el fondo.

- **Para ponerlo en otro nivel de la Ira:** *Fragmentum → Niveles → Preparar la escena de nivel
  abierta* lo coloca y reparte los hitos. También se puede arrastrar
  `Assets/Prefabs/Fondos/FondoMundo1.prefab` a la escena y usar *Fragmentum → Niveles → Repartir
  los hitos del fondo en la escena abierta*.
- **Para mover los hitos:** en el objeto `FondoMundo1` de la escena, componente `RepartoHitos`,
  cambia las fracciones (0 = inicio del nivel, 1 = final) y vuelve a repartir. La consola avisa si
  dos hitos de la misma capa se ven a la vez.
- **Para cambiar un ajuste de importación** de las imágenes, cámbialo en
  `ConstructorFondoMundo1.AjustarImportador`: `ImportadorFondos` lo restablece en cada
  importación.

### Cómo ajustar la velocidad

El fondo solo se mueve al darle a Play. La velocidad es lo que una capa se corre en pantalla por
cada unidad que avanza la cámara: 0 = parece no moverse, 1 = va pegada al suelo.

- **Una capa repetible va demasiado rápida:** aléjala en Z (sube su Z en el prefab). Con el cielo
  en z = 40, la velocidad es `1 - (z + 10) / 50`: z = 35 → 0,1; z = 30 → 0,2; z = 25 → 0,3;
  z = 5 → 0,7; z = 0 → 0,8. **Demasiado lenta:** acércala (baja su Z). No muevas el cielo: es la
  referencia de todas.
- **Una capa de props va demasiado rápida o lenta:** cambia `velocidad` en su `ParallaxProps` y
  pon su Z a juego, para que la tabla siga siendo cierta. El muro y las cataratas deben llevar la
  misma, o la catarata resbala sobre el muro.
- **Todo el fondo se siente demasiado rápido:** baja todas las velocidades a la vez. Por
  ejemplo: primer plano a z = 10 (0,6), fondo lejano a z = 32,5 (0,15), fortaleza a 0,3 y muro y
  cataratas a 0,45 en sus `ParallaxProps`, con su Z a juego (25 y 17,5).
- **Tras cambiar la velocidad de un hito, vuelve a repartir:** cuanto más lento va, más recorrido
  tarda en cruzar la pantalla y más fácil es que dos se vean a la vez.

### Qué probar a mano (desde `Menu_Inicial`, «Nueva partida»)

- Al correr hacia la derecha, las seis capas se mueven a velocidades distintas: el cielo casi
  quieto, las montañas despacio, la fortaleza algo más rápido, las rocas del primer plano casi
  con el suelo.
- La fortaleza se ve centrada hacia el final de la sección 1. La catarata aparece sobre el
  muro, en la arena.
- El muro entra poco a poco durante la sección 2 y tapa la pantalla en la arena del jefe.
- Al saltar, no asoma ningún borde arriba ni abajo.
- Al morir y reaparecer en el checkpoint, la fortaleza sigue en el mismo sitio del paisaje.
- El pixel art se ve nítido. Al desplazarse puede notarse un leve parpadeo de los píxeles: a
  1080p cada píxel del fondo ocupa 1,4 píxeles de pantalla.
- Los extremos del tramo de muro son un corte recto (PENDIENTES N14).

---

## Cinemática inicial y escenario pintado (2026-09-30)

Rama `cinematica-y-mapa`, que sale de `arte-kael`. Llegaron las 12 imágenes de la cinemática
inicial con su guion, y el tileset volcánico ya recortado en `Assets/TileMap` se usa por fin
para pintar el escenario.

### 1. La cinemática inicial: 12 planos, 80 s

- **Problema.** El encargo pedía:
  - fundidos cruzados de 0,6 s entre planos, nunca un corte duro;
  - textos con sus propios tiempos y posición, con fundidos de 0,4 s;
  - saltar con cualquier tecla pasados 2 s, con un aviso discreto;
  - que se pudiera reutilizar para los cierres de cada mundo.

  El sistema de la Fase 8 cortaba en seco entre diapositivas, tenía un solo texto fijo abajo
  y se saltaba con Esc desde el primer instante.
- **Causa.** La Fase 8 se hizo sin arte ni guion: bastaba con que la cinemática se viera,
  se cronometrara y se pudiera saltar.
- **Solución.** Se amplió el mismo sistema en lugar de crear uno nuevo, como proponía el
  encargo con `CinematicSequence` y `CinematicController`. Esos dos nombres corresponden a
  `CutsceneData` y `PantallaCinematica`, que ya existían, se reproducían desde la FSM global y
  guardaban en la partida que la cinemática ya se había visto.
  - **`CutsceneData`:**
    - cada diapositiva puede tener **varios carteles**, con su inicio, duración, tercio (arriba,
      centro o abajo) y estilo de título;
    - el campo `texto` de siempre funciona como cartel por defecto: abajo, entra a los 0,8 s y se
      va 0,8 s antes del final;
    - los tiempos (fundidos, margen y bloqueo del salto), el aviso y una música opcional para
      toda la cinemática se editan en el asset.
    - **Una cinemática nueva se crea con *Create → Fragmentum → Cinemática*, sin escribir código.**
  - **`LineaTiempoCinematica`** (nueva) dice qué se ve en cada instante. Es lógica pura, así
    que las pruebas recorren los 80 s sin esperarlos. Reglas:
    - los fundidos van **dentro** de cada plano, así que la cinemática dura exactamente la suma
      de sus planos;
    - la primera imagen sale de negro y la última vuelve a negro;
    - en el fundido cruzado, la imagen que entra aparece **encima** de la que sale, que sigue
      opaca debajo, de modo que el cruce no se oscurece a la mitad.
  - **`PantallaCinematica`:**
    - todo entra y sale con `CanvasGroup.alpha`;
    - las imágenes conservan su proporción (1376×768, 1,79:1) sobre fondo negro;
    - **los textos se colocan por tercios de la imagen, no de la pantalla**: en 16:10, el título
      de arriba sigue cayendo sobre la mitad vacía de la imagen y no sobre la franja negra.
  - **Saltar:**
    - pasados 2 s, cualquier tecla, botón del mando o clic la salta (`InputSystem.onAnyButtonPress`);
    - antes de los 2 s no se puede saltar, ni siquiera con Esc: así no se salta con la tecla que
      inició la partida;
    - el aviso «Pulsa cualquier tecla para saltar» aparece a los 2 s con opacidad baja y se va a
      los 5 s.
  - **`ConstructorCinematicaInicial`** (menú *Fragmentum → Narrativa → Importar la cinemática
    inicial*) prepara las imágenes como pixel art y escribe el guion en `Cinematica_Inicial`:
    - las imágenes, en `Assets/Sprites/Cinematicas/Inicial/Plano_01.png` a `Plano_12.png`, se
      importan con filtro *Point*, sin compresión y sin mipmaps;
    - el guion incluye los dos textos temporizados del plano 4 y el título del plano 11 arriba.
- **Problema encontrado:** con «cualquier tecla», pulsar Esc para saltar la cinemática habría
  pausado el juego nada más volver.
  - **Causa:** la misma pulsación llegaba primero a la cinemática y después al `UIManager`, que
    ya veía el juego en marcha.
  - **Solución:** la cinemática aplica el salto en el frame siguiente a la pulsación. Lo
    comprueba una prueba PlayMode.

### 2. El escenario pintado con el tileset

- **Problema.** El Nivel 1 y las escenas de prueba se veían como rectángulos de color.
- **Causa.** El tileset volcánico estaba recortado desde la Fase 2 (`Assets/TileMap`, 283 tiles de
  32 px), pero nadie había pintado con él. `Nivel1` solo tenía un Tilemap con 2 tiles, que se borró
  a mano antes de este cambio.
- **Solución.** **`PintorEscenario`** viste los bloques de colisión con el tileset:
  - **Solo cambia el dibujo.** Las colisiones siguen siendo las de los bloques: el recorrido,
    las mecánicas y todas las pruebas quedan igual. Se apaga el rectángulo de color de cada bloque
    y se pinta encima un Tilemap sin colisión propia.
  - **La masa sólida** (suelos, techos y paredes) usa la pieza de ladrillo de 3×3, la única que
    encaja sin costuras en las dos direcciones. Las demás se probaron y dejaban juntas visibles.
    - Los suelos a ras de 0 siguen 7 filas hacia abajo como cimiento: los fosos se ven como
      huecos entre acantilados.
  - **Las plataformas finas** (0,5 u) usan la viga de hierro, con sus dos extremos.
  - **Los bloques fuera de la rejilla** (el techo bajo, a 1,2 u) van en un Tilemap desplazado,
    para que el dibujo coincida con su colisión.
  - **En el mundo de la Ira, decoración detrás del jugador:** la estatua del caballero, braseros,
    escombros y cadenas.
    - Nunca tapa un objeto del juego (mecánicas, checkpoints, enemigos, salida, arena).
    - No se coloca debajo de una plataforma ni contra una pared.
  - **Dónde se pinta:**
    - `Nivel1`, `PruebaJugador` y `PruebaEnemigos` ya están pintadas;
    - la herramienta *Preparar la escena de nivel* pinta sola el recorrido provisional de los
      niveles de la Ira, y las escenas de prueba se pintan al regenerarse;
    - menú para repintar la escena abierta: *Fragmentum → Niveles → Pintar el escenario*.

### Decisiones que ajustan el plan de la Fase 1

1. **No se crean `CinematicSequence` ni `CinematicController`, ni una escena «Intro».** La
   cinemática inicial ya se reproducía al entrar al Nivel 1, en el flujo de la FSM global. Una
   escena aparte habría duplicado el sistema y la marca de «ya vista».
2. **La marca de «ya vista» sigue en la partida, no en PlayerPrefs.** PlayerPrefs es del PC: si
   los participantes del piloto comparten ordenador, solo el primero vería la cinemática.
3. **Se sigue usando el `Text` integrado de Unity, no TextMeshPro.** Los recursos de TMP no están
   importados y toda la UI usa `FabricaUI` (PENDIENTES B9). Si se adopta TMP, se cambia para toda
   la UI a la vez.
4. **Ya no se pasa de diapositiva con un clic:** cualquier tecla salta la cinemática entera, como
   pedía el encargo.

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **204 de 204 correctas** (14 nuevas):
  - `CinematicaInicialTests` (10):
    - 12 planos con sus imágenes y 80 s exactos;
    - imágenes importadas como pixel art;
    - **ningún corte duro en los 80 s**, comprobado cada 1/120 s;
    - sale de negro, cruza los planos y vuelve a negro;
    - los textos entran a los 0,8 s y salen 0,8 s antes;
    - los dos textos del plano 4 y el título arriba en el 11;
    - no se puede saltar antes de 2 s;
    - el aviso aparece a los 2 s y se va a los 5 s.
  - `PintorEscenarioTests` (4):
    - cada bloque queda cubierto por tiles alineados con su colisión, también el desplazado;
    - la colisión no cambia;
    - pintar dos veces deja lo mismo;
    - las tres escenas reales están pintadas y el Nivel 1 tiene decoración.
- Pruebas PlayMode: **81 de 81 correctas**. Las de narrativa se adaptaron al bloqueo de 2 s:
  - la cinemática inicial no se salta antes de 2 s, ni con Esc ni con otra tecla;
  - el aviso aparece;
  - el salto se aplica en el frame siguiente;
  - un texto entra con fundido.

### Pasos manuales en el Editor

Ninguno.

- **Para cambiar un texto o una duración de la cinemática inicial:** edita
  `Assets/Data/Narrativa/Cinematica_Inicial` en el Inspector. Volver a ejecutar *Importar la
  cinemática inicial* restablece el guion de este cambio.
- **Para hacer una cinemática de cierre:** en su asset `Cinematica_Cierre_*`, arrastra cada
  imagen a `imagen`, escribe el `texto` y, si hace falta, añade `carteles` con sus tiempos.
  Importa las imágenes con filtro *Point* y sin compresión, como las de `Assets/Sprites/Cinematicas/Inicial`.
- **Para pintar el nivel a mano más adelante (N10):** borra «Escenario (tiles)» y «Provisional»
  y sigue los pasos de la Fase 9.

### Qué probar a mano (desde `Menu_Inicial`, «Nueva partida»)

- La cinemática dura 1 min 20 s si no se salta. Las imágenes se funden entre sí, sin cortes,
  y los textos aparecen y desaparecen suavemente. El título del plano 11 sale arriba y más
  grande.
- Pulsar una tecla durante los 2 primeros segundos no hace nada. Después, el aviso aparece
  abajo a la derecha, y cualquier tecla, botón o clic lleva al Nivel 1 sin pausar el juego.
- En una partida nueva vuelve a verse; en una partida ya empezada, no.
- En el Nivel 1: el suelo de ladrillo volcánico, los fosos como huecos entre acantilados, las
  vigas de las plataformas, la estatua del caballero, los braseros y los escombros. Las
  mecánicas, la lava y la puerta de la arena siguen con su dibujo provisional (PENDIENTES B10).

---

## Arte de Kael (2026-09-30)

Rama `arte-kael`, que sale de `fase-9-nivel1`. No es una fase del plan: llegaron las primeras
animaciones de Kael, dibujadas en Pixelorama (quieto, correr, salto y ataque), y el jugador deja
de ser una cápsula.

### 1. Kael era una cápsula

- **Problema.** Desde la Fase 3, el jugador era un rectángulo claro (PENDIENTES B3).
- **Causa.** No había arte de Kael listo para importar. Las hojas que llegaron, además, venían
  escaladas: la de correr y la del salto a 5×, la de quieto y la del ataque a 10×. La del
  ataque medía 13 300 px de ancho: Unity la habría reducido a su máximo de 8192 px con filtro y
  el pixel art se habría emborronado. La del salto venía exportada en ping-pong (9 fotogramas de
  ida y 7 de vuelta).
- **Solución.**
  - **`Herramientas/reducir_hoja.py`** detecta la escala de cada hoja y la reduce sin pérdida a
    1× (un píxel de cada bloque idéntico de N×N). Con `--conservar 9` quita la vuelta del
    ping-pong. Las hojas quedan en `Assets/Sprites/Kael`.
  - **`ConstructorArteKael`** (menú *Fragmentum → Jugador → Importar arte de Kael*, incluido en
    *Regenerar todo*) hace el resto:
    - recorta cada hoja en fotogramas con el pivote en la planta de los pies y en el centro del
      torso, para que cambiar de clip no desplace a Kael;
    - crea 6 clips en `Assets/Animaciones/Kael`: Idle, Correr, Salto, Caida, CaidaBucle y Ataque;
    - arma `Kael.controller`.
    - Es idempotente: reutiliza los identificadores de sprites y estados, así que volver a
      ejecutarlo sin cambios no modifica ningún archivo (comprobado).
  - **El prefab** usa el primer fotograma de Idle sin desplazar ni escalar el sprite. Si el arte
    falta, vuelve a la cápsula y avisa («construir para ausencia»).

### 2. Dos máquinas de estados que se contradecían

- **Problema.** En la v1, el Animator tenía sus propias transiciones, gobernadas por booleanos
  sueltos que el código escribía cada frame. Si el código y el Animator no coincidían, Kael
  hacía una cosa y mostraba otra.
- **Causa.** El estado del personaje vivía en dos sitios: en las banderas de `PlayerMovement` y
  en el grafo del Animator.
- **Solución.** La FSM del código es la única que decide.
  - El nuevo componente **`PlayerAnimacion`** escucha `OnStateChanged` (patrón Observer) y, al
    entrar en cada estado, reproduce el clip que dice su tabla.
  - El Animator no tiene transiciones propias: solo guarda los clips. La única excepción es
    Caida → CaidaBucle al terminar el clip, que es parte de la animación y no una decisión.
  - La tabla se edita en el Inspector. Los estados sin arte propio usan el clip más parecido:

| Estado de la FSM | Clip | Nota |
|---|---|---|
| IDLE | Idle | 2 fotogramas, respiración de 1 s |
| RUN | Correr | Las piernas siguen la velocidad real (ver 3) |
| JUMP, DOUBLE_JUMP, WALL_JUMP | Salto | Vuelve al primer fotograma aunque ya sonara |
| FALL | Caida → CaidaBucle | El pelo sube al empezar a caer y luego ondea |
| ATTACK | Ataque | Se estira a la duración del golpe (ver 3) |
| WALL_SLIDE | Caida | Provisional: falta su clip |
| DASH | Correr | Provisional: falta su clip. Se ve un fotograma de carrera fijo |
| CROUCH | Idle | Provisional: falta su clip |
| DEAD | Idle | Provisional: falta su clip |
| DAMAGED | (sigue el clip anterior) | El parpadeo ya marca el golpe |

### 3. Que la animación encaje con el movimiento

- **Escala.** Kael mide unos 113 px de arte. A 64 px por unidad son 1,77 u, la altura de su
  collider (1,8 u). Los tiles siguen a 32 PPU: Kael tiene el doble de detalle que el escenario,
  como en el arte original.
- **Correr.** El parámetro `Movement` (velocidad horizontal en u/s, que ya escribía la FSM)
  multiplica la velocidad del clip. Una zancada completa recorre 3 u: a 6 u/s la vuelta dura
  0,5 s, y bajo el agua o sobre hielo las piernas van al ritmo real del cuerpo, sin patinar.
- **Ataque.** El clip dura 1 s y `PlayerAnimacion` lo acelera con el parámetro nuevo
  `VelocidadAtaque` = 1 / duración del golpe. Así ocupa exactamente el ataque normal (0,35 s) y
  el cargado (0,5 s) se ve más lento sin un clip propio. La preparación es rápida para que el
  tajo (fotogramas 4 y 5) se vea mientras la hitbox está abierta.
- **Problema encontrado:** el clip de ataque duraba 1,017 s en lugar de 1 s.
  - **Causa:** Unity alarga los clips de sprites un fotograma (1/60 s) después de la última clave.
  - **Solución:** la clave de cierre se coloca ese fotograma antes. Lo comprueba una prueba
    para todos los clips.

### Decisiones que ajustan el plan de la Fase 1

1. **Kael va a 64 PPU, no a 32** como decía la convención de PENDIENTES. A 32 PPU mediría 3,5 u,
   el doble de su collider, y habría que reducir el arte a la mitad.
2. **Se quitan `EnSuelo` y `VelocidadY` de `AnimatorParams`.** Estaban reservados para que el
   Animator eligiera entre salto y caída; ahora lo decide la FSM. Se conservan `Movement`,
   `Attack`, `Damage` y `Death` en el controlador, aunque solo `Movement` mueve el clip, para
   que el código no avise de que faltan.

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **190 de 190 correctas** (9 nuevas en `ArteKaelTests`):
  - cada hoja está recortada en sus fotogramas, a 64 PPU y con filtro *Point*;
  - Kael mide lo mismo que su collider en las 4 hojas (falla si una se exporta a 5×);
  - el pivote de cada hoja está en la fila de los pies;
  - cada estado de la FSM tiene un clip que existe en el controlador;
  - los clips tienen sus fotogramas, su bucle y su duración exacta;
  - el controlador tiene los parámetros que escribe el código y solo la transición de la caída;
  - el tajo del ataque se ve con la hitbox abierta;
  - el prefab usa el arte sin `localScale`.
- Pruebas PlayMode: **80 de 80 correctas** (4 nuevas en `AnimacionJugadorPlayModeTests`):
  - correr, saltar, caer y aterrizar cambian de clip, y los pies del sprite pisan donde pisa el
    cuerpo;
  - el doble salto vuelve al primer fotograma del salto;
  - el ataque se estira a la duración del golpe, normal y cargado;
  - el dash usa el clip de correr sin reiniciarlo.

### Pasos manuales en el Editor

Ninguno: el prefab ya tiene el arte, y las escenas lo usan porque instancian el prefab.

- **Para añadir una animación** (por ejemplo, el dash):
  1. En Pixelorama, exporta la tira horizontal **a escala 1× y sin ping-pong** a
     `Assets/Sprites/Kael/Kael_Dash.png`. Si ya la exportaste escalada:
     `python Herramientas/reducir_hoja.py entrada.png FOTOGRAMAS Assets/Sprites/Kael/Kael_Dash.png`.
  2. Añade la hoja y su clip a las tablas de `ConstructorArteKael`. El pivote se mide en píxeles
     desde la esquina inferior izquierda del fotograma: la x en el torso y la y en los pies.
  3. Ejecuta *Fragmentum → Jugador → Importar arte de Kael y regenerar el prefab*.
  4. En la tabla de `PlayerAnimacion` (o en `TablaPorDefecto`), apunta el estado DASH al clip
     nuevo. Las pruebas dicen si algo no cuadra.
- **Si cambias el lienzo de una hoja en Pixelorama,** ajusta su pivote en `ConstructorArteKael`.
  La prueba del pivote avisa si los pies ya no están en la fila indicada.

### Qué probar a mano (`Assets/Scenes/Pruebas/PruebaJugador.unity`)

- Quieto, Kael respira. Al correr, el pelo ondea y las piernas no patinan; en el agua van más
  despacio.
- El salto sube con las piernas recogidas, el pelo se levanta al empezar a caer y vuelve a Idle
  al aterrizar. El doble salto repite el impulso.
- El ataque (clic) es rápido y el tajo coincide con el golpe al muñeco. El cargado (mantener y
  soltar) se ve más lento y rojizo.
- Al girar, el sprite se voltea sin desplazarse.
- **El tajo se dibuja más alto (hasta ~2,8 u) que la hitbox de la espada (0,25 a 1,55 u).**
  Dime si debo subir la hitbox al arco del dibujo: así alcanzaría a enemigos más altos.

---

## Fase 9 — Nivel 1 de referencia (2026-09-29)

Rama `fase-9-nivel1`, que sale de `fase-8-narrativa`. Cubre la tarea H: las mecánicas del
mundo de la Ira del Anexo A, como prefabs listos para arrastrar al pintar el nivel, y ya
colocadas en el recorrido provisional del Nivel 1.

Lo demás que pide la tarea venía de fases anteriores:
- de la Fase 6, el checkpoint en x = 40, los enemigos y las guías de las secciones (Gizmos del
  `LevelManager`);
- de la Fase 7, la arena cerrada.

Pintar el layout de ~80 u sigue siendo trabajo tuyo en el Editor (PENDIENTES N10).

### 1. El mundo de la Ira no tenía mecánicas propias

- **Problema.** En la v1, la lava del Nivel 1 era solo un tile decorativo que no mataba.
  Tampoco había plataformas que se desmoronaran ni torretas. El nivel se jugaba igual que
  cualquier otro.
- **Causa.** Las mecánicas del Anexo A (`PlatformCrumble`, `HotPlatform`, `Lava` y la torreta)
  nunca se programaron.
- **Solución.** Cuatro componentes en `Levels/Mecanicas`, cada uno con su prefab provisional:
  rectángulos de color hasta que haya arte (PENDIENTES B10).
  - **`PlataformaInestable`.**
    - Con el jugador encima tiembla 0,6 s, cae y vuelve a los 3 s.
    - No vuelve mientras el jugador esté justo donde reaparecería.
    - Detecta al jugador con una consulta fina sobre su superficie, no con contactos, así que
      funciona igual si llega saltando o caminando.
  - **`PlataformaCaliente`.**
    - Se puede pisar, pero se va poniendo al rojo y a los 2 s quema.
    - Sigue quemando mientras el jugador no se baje; al bajarse, se enfría.
    - Es la mecánica de la emoción del mundo: la Ira no deja quedarse quieto.
  - **`Lava`.** Muerte instantánea para el jugador y para los enemigos. Usa el mismo
    `Golpe.Letal` que la zona de caída: atraviesa los i-frames y el escudo.
  - **`TorretaFuego`.**
    - Es fija e indestructible.
    - Cuando el jugador está a menos de 10 u, avisa con un destello y dispara una bola de fuego
      horizontal cada 2,2 s.
    - Se esquiva saltando.
- Lo verifican las pruebas de `MecanicasPlayModeTests`, con los prefabs reales, el jugador real
  y física real.

### 2. Un proyectil reutilizable

- **`Proyectil`** (en `Core/Combate`) vuela en línea recta y hace daño con un `Hitbox`, así que
  respeta las capas, los i-frames y los escudos.
  - Desaparece al golpear, al tocar el suelo o una pared, o al acabar su alcance.
  - También lo usarán las olas de la Doncella y los Artilleros (Fase 10).
- **Problema encontrado:** la bola atravesaba las paredes.
  - **Causa:** la matriz de colisión del Anexo A no deja que las armas enemigas choquen con el
    suelo ni con las paredes. Por eso un trigger en la capa `EnemyWeapon` nunca detecta una
    pared.
  - **Solución:** en cada paso de física, el proyectil comprueba con `OverlapCircle` si está
    tocando suelo o pared, en lugar de esperar un contacto. La matriz no cambia.

### 3. El Nivel 1 ya tiene sus mecánicas

La herramienta *Preparar la escena de nivel* tiene un paso nuevo:
- Solo actúa sobre el recorrido provisional, y solo si todavía no hay mecánicas.
- En un nivel pintado a mano, las mecánicas se colocan arrastrando los prefabs.

Siguiendo el Anexo A, coloca esto:

| Tramo | Qué coloca | Dónde |
|---|---|---|
| Final de la sección 1 | Torreta de fuego | x = 24 |
| Primer foso (27–30 u) | Plataforma inestable, para cruzarlo | x = 28,5 |
| Sección 2, antes del checkpoint | Dos plataformas de combustión | x = 33,5 y 37 |
| Final de la sección 2 | Río de lava de 3 u, que hay que saltar | x = 50–53 |
| Segundo foso (57–60 u), antes de la arena | Lava en el fondo | x = 58,5 |

- La arena del jefe queda sin lava en el suelo hasta que se equilibre el combate (N9).
- Lo verifican `ElNivel1_TieneLasMecanicasDeLaIra` y `LosPrefabsDeMecanicas_Existen`.
- `PrepararOtraVez_NoCambiaNada` sigue comprobando que la herramienta es idempotente.

### Decisiones que ajustan el plan de la Fase 1

1. **Nombres en español,** como el resto del código: `PlataformaInestable` y
   `PlataformaCaliente` son los `PlatformCrumble` y `HotPlatform` del plan.
2. **La torreta no es un enemigo** con `EnemyData` y FSM, sino una mecánica del escenario: no
   se mueve, no se puede destruir y no persigue. Así no carga con estados que no usa.
3. **Las mecánicas son prefabs, no tiles.** Tienen el pivote en su cara superior (la torreta,
   en su base), para que al arrastrarlas queden a la altura del suelo.

### Además

- **Nuevo menú *Fragmentum → Niveles → Crear prefabs de mecánicas (Ira)*,** incluido en
  *Regenerar todo*.
- **`PreparadorNivel.PosicionesFosos` ahora es pública.** La usan los enemigos (para que su
  patrulla no cruce un foso) y las mecánicas.
- **Ajuste en una prueba de flujo:** para matar al jugador, ahora lo lleva a x = 55 en lugar
  de x = 52, porque en 52 está el río de lava.

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **181 de 181 correctas** (2 nuevas: las mecánicas del Nivel 1 y sus prefabs).
- Pruebas PlayMode: **76 de 76 correctas** (7 nuevas en `MecanicasPlayModeTests`):
  - la plataforma inestable aguanta, cae y vuelve;
  - la de combustión no quema antes de 2 s, quema después y se enfría;
  - la lava mata al jugador aunque tenga i-frames, y también a un enemigo;
  - la torreta no dispara fuera de su alcance, avisa antes del primer disparo y su bola hiere
    y desaparece;
  - el proyectil se frena en una pared y desaparece al acabar su alcance.

### Pasos manuales en el Editor

Ninguno obligatorio: el Nivel 1 ya tiene las mecánicas sobre el recorrido provisional.

- **Al pintar el layout (N10):**
  - Borra «Provisional»: sus mecánicas se van con él.
  - Arrastra a su sitio los prefabs de `Assets/Prefabs/Mecanicas`. Van a la altura del
    suelo, porque el pivote está en su cara superior.
- **Para ajustar una mecánica:** los tiempos, el daño y el alcance están en el componente de
  cada prefab, por ejemplo el aguante de la plataforma inestable o el intervalo de la torreta.
  Si seleccionas la torreta, su alcance se ve como un círculo.

### Qué probar a mano (desde `Menu_Inicial`, «Nueva partida»)

- **Sección 1:** al acercarte, la torreta avisa con un destello y dispara. Salta la bola.
- **Primer foso:** si te quedas encima de la plataforma, tiembla y cae. Al rato vuelve.
- **Antes del checkpoint:** si te quedas quieto sobre una plataforma de combustión, se pone al
  rojo y quema a los 2 s.
- **Río de lava:** hay que saltarlo. Caer en él o en el segundo foso mata al instante, aunque
  acabes de recibir un golpe.
- **Golem de Ceniza:** si te persigue hasta el río, puede morir en la lava. Dime si te parece
  una estrategia válida o si el Golem debe detenerse ante la lava.

---

## Fase 8 — Narrativa (2026-09-29)

Rama `fase-8-narrativa`, que sale de `fase-7-jefes`. Cubre la tarea G: la cinemática inicial
como sistema, los diálogos internos de Kael, el registro emocional y la pantalla «yo
reconstruido». Criterios de aceptación: la cinemática inicial funciona sin imágenes y ningún
asset ausente bloquea el juego.

### 1. La historia no tenía dónde vivir

- **Problema.** La v1 no tenía cinemáticas ni diálogos. La cinemática inicial del diseño no
  existía, y el único arte narrativo era una imagen de fondo del menú.
- **Causa.** No había ningún modelo de datos para la narrativa ni nada que la reprodujera.
- **Solución.** Las cinemáticas son datos (**`CutsceneData`**: diapositivas con imagen, texto,
  duración y audio opcional), y las reproduce **`PantallaCinematica`**:
  - **Funciona sin arte.** Una diapositiva sin imagen se ve en negro con su texto, y sin texto
    dice qué falta. Así la cinemática inicial ya se puede ver, cronometrar y saltar.
  - **Controles:** clic o Enter pasa de diapositiva, y Esc o Start la salta si es saltable.
  - **Integración con la FSM global:**
    - La cinemática de entrada va entre LOADING y PLAYING. La inicial es la entrada del
      Nivel 1 y solo se ve la primera vez; queda guardado en la partida.
    - La de cierre va entre la victoria y el fin de nivel.
    - Las memorias (Fase 10) usarán `IniciarCinematica`.
  - **Sin reproductor** (sin `UIManager`), la FSM global avanza sola: nunca se queda atascada.
  - Lo verifican `LaCinematicaInicial_SeVeAntesDelNivel1_SinImagenes`,
    `LaCinematicaInicial_NoSeRepite`, `UnaCinematica_AvanzaSola_YVuelveAlJuego` y
    `SinReproductor_LaCinematicaNoBloqueaElJuego`.

### 2. Los diálogos internos de Kael

- **`DialogueData`**, uno por mundo, con líneas para cuatro momentos: apertura, eco de memoria,
  antes del jefe y cierre. Viven en datos, nunca en la escena.
- **`DialogueManager`** los pone en cola y el HUD los muestra abajo, con el nombre de quien
  habla en el color del mundo:
  - No detienen el juego: son pensamientos, no conversaciones.
  - Esperan durante las cinemáticas y la pausa.
  - La **apertura** suena al empezar el nivel y la línea **antes del jefe**, al entrar en la
    arena. La de **cierre** acompaña al fragmento en el fin de nivel, porque tras la victoria
    ya no se juega. El **eco de memoria** lo usarán las memorias (Fase 10).
- **Los textos son del guion** (PENDIENTES B2). Mientras falten, cada línea dice que está
  pendiente y ya nombra la emoción de su mundo, como pide la tarea G.
- Lo verifican `LasLineasDeApertura_AparecenAlEmpezarAJugar` y `AntesDelJefe_HablaElArquitecto`.

### 3. El registro emocional: datos cualitativos desde el propio juego

- **Problema.** El instrumento de la tesis solo tenía cuestionarios externos (TMMS-24 y SCCS).
  No recogía nada de lo que el jugador vive en cada mundo.
- **Solución.** Al terminar cada nivel, la pantalla de fin de nivel empieza por el registro
  emocional:
  - Pregunta «¿Con qué intensidad sentiste ira en este mundo?», con una escala de 1 a 5.
  - Un comentario opcional y, la primera vez, un código de participante opcional para el
    pilotaje.
  - **«Omitir»** pasa sin guardar nada: nunca bloquea el juego.

  **`RegistroEmocional`** guarda cada respuesta en local, en un archivo aparte de la partida
  (`registro_emocional.json`), y la exporta a la vez a `registro_emocional.csv`:
  - separado por «;» y con BOM, para que Excel lea bien las tildes;
  - con escritura atómica;
  - si el archivo está dañado, se conserva como `.danado` y se empieza uno nuevo.

  La pregunta y las etiquetas de la escala son datos (`ConfiguracionRegistro`), para ajustarlas
  al instrumento sin tocar código. Lo verifican `RegistroEmocionalTests`,
  `AlSuperarElNivel_CierreYRegistroEmocional` y `ElRegistroEmocional_SePuedeOmitir`.

### 4. La pantalla «yo reconstruido»

- Muestra de forma acumulativa los fragmentos de identidad: Kael en el centro y los cinco
  fragmentos alrededor.
  - Los integrados brillan con el color de su mundo y dicen qué habilidad dieron.
  - Los que faltan son un contorno apagado.
- Se abre desde el menú principal (botón nuevo «Yo reconstruido»).
- El fin de nivel muestra la misma cuenta en una fila de rombos.
- Lo verifica `YoReconstruido_MuestraLosFragmentosIntegrados`.

### Decisiones que ajustan el plan de la Fase 1

1. **El reproductor de cinemáticas es una pantalla más del `UIManager`** (`PantallaCinematica`),
   no un sistema aparte. La FSM global guarda qué cinemática toca y si es de cierre, y
   `TerminarCinematica()` ya no necesita parámetros.
2. **El nivel se completa antes de la cinemática de cierre:** el fragmento ya está otorgado y la
   cinemática solo retrasa la pantalla de fin de nivel.
3. **La línea de cierre se muestra en el fin de nivel,** porque tras la victoria ya no se juega.
4. **La configuración del registro es un ScriptableObject** (`ConfiguracionRegistro`, el
   `EmotionalLogConfig` propuesto), y el código de participante se pide dentro del propio
   registro la primera vez, en lugar de en una pantalla de opciones.
5. **Placeholders evidentes:** las cinemáticas y los diálogos generados dicen «pendiente de
   guion». Solo se usa texto del Anexo A donde el Anexo lo da: el cierre del Jardín, la entrada
   al final y la pregunta de los créditos.

### Además

- **Nuevo menú *Fragmentum → Narrativa*:** generar y restablecer los placeholders. *Regenerar
  todo* los incluye y los enlaza a los `LevelData` sin pisar lo que se haya asignado a mano.
- **`FabricaUI.Campo`:** campo de texto con marcador, para el registro.
- **`PantallaSuperpuesta.PrimeraSeleccion`:** cada pantalla elige qué queda seleccionado al
  abrirse. En el registro es «Omitir», para que pulsar Enter sin mirar no guarde una respuesta.
- La escena `Menu_Inicial` se regeneró con el botón nuevo (conserva su GUID).

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **179 de 179 correctas** (15 nuevas: datos de cinemáticas y diálogos,
  registro emocional en JSON y CSV, y la narrativa generada y enlazada).
- Pruebas PlayMode: **69 de 69 correctas**:
  - 9 nuevas de narrativa, con los sistemas reales y el Nivel 1;
  - las de progresión y flujo se adaptaron a la cinemática de cierre y al registro del fin de
    nivel.

### Pasos manuales en el Editor

Ninguno obligatorio. Los placeholders están en `Assets/Data/Narrativa`, ya enlazados a cada
`LevelData`.

- **Para escribir el guion:** edita el texto de las diapositivas de `Cinematica_*` y de las
  líneas de `Dialogos_*`. Las imágenes van en el campo `imagen` de cada diapositiva (16:9).
- **Para ajustar el registro emocional:** en `Assets/Data/Narrativa/RegistroEmocional.asset`
  están la pregunta (`{emocion}` se sustituye por la del mundo), las etiquetas de la escala y
  los campos opcionales. Desmarca `activo` para no preguntar.
- **Dónde quedan las respuestas:**
  `%USERPROFILE%\AppData\LocalLow\DefaultCompany\Tesis2.0\registro_emocional.csv`, y el `.json`
  al lado. «Nueva partida» no las borra.

### Qué probar a mano (desde `Menu_Inicial`, «Nueva partida»)

- Antes del Nivel 1 se ve la cinemática inicial: pantalla negra con el texto pendiente. Enter
  pasa de diapositiva y Esc la salta. Si vuelves a entrar, ya no sale.
- Al empezar a jugar aparece abajo la línea de apertura de Kael, que nombra la ira.
- Al entrar en la arena habla la Centinela Ardiente.
- Tras cruzar la salida: la cinemática de cierre y luego el registro emocional. Elige un número,
  escribe algo y «Guardar». Después sale el fragmento con la línea de cierre y la fila de
  rombos. Comprueba el `.csv`.
- En el menú, «Yo reconstruido» muestra el Fragmento de la Ira encendido. Esc lo cierra.

---

## Fase 7 — Jefes (2026-09-29)

Rama `fase-7-jefes`, que sale de `fase-6-flujo-nivel`. Cubre la tarea C: jefes con FSM por
fases, la Centinela Ardiente completa como referencia, los otros cuatro como esqueletos
documentados y el evento que otorga el fragmento y la habilidad al derrotar al jefe.

### 1. El jefe era un enemigo con otro sprite

- **Problema.** El `BossController` de la v1 era una copia del enemigo:
  - 3 puntos de vida y ninguna fase;
  - un único ataque, que además solo lanzaba si el jugador estaba a su izquierda;
  - el retroceso de cada golpe lo empujaba como a cualquier enemigo.
- **Causa.** No había ningún modelo de jefe: ni fases, ni patrones, ni datos.
- **Solución.** **`BossController`** abstracto con una FSM por fases sobre el mismo núcleo que el
  jugador y los enemigos:
  - **Estados:** PHASE_1, PHASE_2 y PHASE_3, que avanzan según la vida que le queda, y
    VULNERABLE. Los cuatro forman el superestado COMBATE, del que se sale a DEFEATED.
  - **Patrón Template Method:** la base lleva el flujo, y cada jefe redefine solo sus pasos
    (`CondicionDerrota`, `AlActivarse`, `AlCambiarDeFase`, `AlSerDerrotado` y `AlReiniciarse`).
  - **Patrón Strategy:** lo que hace cada fase es un dato, la lista de `PatronAtaque` de su
    `FaseJefe` en el `JefeData`. Cambiar una fase es cambiar un asset, no el código.
  - El diagrama `Docs/FSM/Jefe.md` se genera desde la tabla.
  - Lo verifican `TablaJefeTests`, `DatosJefesTests` y
    `AlBajarDeLaMitadDeVida_PasaALaFase2_UnaSolaVez`.

### 2. Un golpe podía dejar al jefe congelado

- **Problema.** En la v1, si el evento de animación `DesactiveDamage` no llegaba, el jefe se
  quedaba para siempre inmóvil e inmune.
- **Causa.** `takingDamage` solo lo apagaba ese evento, y mientras estuviera encendido el jefe ni
  se movía ni recibía daño.
- **Solución.** Los jefes **no tienen estado DAMAGED**. Un golpe solo los hace parpadear y no
  interrumpe el ataque en curso. La única forma de «abrirlos» es la **ventana vulnerable**:
  - La abre un patrón (el martillo clavado de la Centinela) o el propio jefe (la memoria correcta
    del Juez Silente).
  - Dura un tiempo fijo y multiplica el daño recibido (×2).
  - Con `inmuneFueraDeVulnerable`, el jefe solo recibe daño durante la ventana.
  - Lo verifican `UnGolpe_NoInterrumpeElAtaqueDelJefe`,
    `LaVentanaVulnerable_DuplicaElDanio_YVuelveALaFaseQueToca` y `SaludJefeTests`.

### 3. La Centinela Ardiente, implementación de referencia

Sigue el Anexo A. Tiene 24 de vida y dos fases:

- **Fase 1:**
  - **Martillazo:** se acerca, avisa con un destello y golpea delante. El martillo queda
    clavado 1,2 s: es la ventana vulnerable.
  - **Onda de suelo:** una onda baja recorre el piso hacia los dos lados y se esquiva saltando.
    El Anexo A la describe como un «raycast horizontal». Aquí es una zona que viaja, para que el
    jugador la vea venir y pueda saltarla.
- **Fase 2 (50 % de vida):** además, **plataformas en erupción**. Son columnas de lava bajo el
  jugador y a su alrededor, que se anuncian antes de quemar y obligan a moverse. Al entrar en
  la fase, la Centinela ruge con un aviso largo.
- **Muerte:** destello y desvanecido, provisionales hasta que haya arte. Todas sus zonas de daño
  desaparecen: «el suelo se estabiliza».

Todos los ataques se telegrafían y ninguno sale de la arena. Lo verifican
`Martillazo_GolpeaAlJugadorDelante_YDejaAlJefeVulnerable`,
`OndaDeSuelo_RecorreElSuelo_YAlcanzaAlJugador` y
`PlataformasEnErupcion_BrotanBajoElJugador_ConAvisoPrevio`.

### 4. Derrotar al jefe no daba nada

- **Problema.** En la v1, la muerte del jefe solo lo detenía: no había recompensa ni forma de
  terminar el nivel.
- **Causa.** Nada escuchaba la derrota.
- **Solución.** El jefe emite **`OnJefeDerrotado`** y la **`ArenaJefe`** lo convierte en la
  recompensa del nivel:
  - El `LevelManager` otorga **en el acto** el fragmento y la habilidad, y los guarda. El Ataque
    Cargado se puede usar antes de salir, y el HUD avisa «Fragmento de la Ira integrado».
  - Tras 2 s, la arena abre sus puertas y aparece la **salida**. Mientras el jefe vive, la salida
    está oculta: no se puede terminar el nivel esquivando el combate.
  - Lo verifican `Derrota_EmiteElEvento_ApagaElDanio_YDesvanece` y
    `DerrotarALaCentinela_OtorgaFragmentoYHabilidad_YAbreLaSalida`.

### 5. La arena del jefe

- **`ArenaJefe`:** cuando el jugador cruza su entrada, cierra la puerta, activa al jefe con los
  límites de la arena, cambia a la música del jefe (`LevelData.musicaJefe`, si la hay) y muestra
  su barra de vida en el HUD.
- **Si el jugador muere en la arena,** al reaparecer el combate vuelve a empezar: el jefe
  recupera la vida y su sitio, y la puerta se abre. Lo verifica `MorirEnLaArena_ReiniciaElCombate`.
- ***Preparar la escena de nivel*** crea la arena en la última sección de los niveles con jefe.
  El Nivel 1 ya la tiene: entrada en x = 61, puerta detrás y la Centinela en x = 71,5.

### 6. Los otros cuatro jefes, como esqueletos documentados

Compilan, tienen datos (vida y fases del Anexo A), prefab y el flujo completo, pero sus fases
aún no tienen patrones: esperan sin atacar. Cada uno documenta lo que le falta y ya incorpora
su regla propia:

- **Juez Silente:** inmune salvo en la ventana, que abre `ActivarMemoriaCorrecta()`. Es la
  mecánica de la tesis: enfrentar la culpa es lo que permite avanzar. Lo verifica
  `ElJuezSilente_EsInmuneHastaLaMemoriaCorrecta`.
- **El Verdadero Soldado:** no cae al quitarle la vida (se queda con 1 y pasa a la fase 3), sino
  con la integración (`CompletarIntegracion()`). El combate termina sin violencia. Lo verifica
  `ElVerdaderoSoldado_NoCaeSinLaIntegracion`.
- **Espectro del Pavor:** casi invisible durante el combate.
- **Doncella del Lamento:** solo el flujo base.

### Decisiones que ajustan el plan de la Fase 1

1. **El jefe no tiene estado previo al combate.** La máquina arranca en PHASE_1 cuando la arena
   lo activa, y `MaquinaEstados.Detener()` (nueva) la para para reiniciar el combate. El plan
   dibujaba `[*] --> COMBATE : Activar()`, que es lo mismo.
2. **La recompensa llega al derrotar al jefe y el nivel se completa al salir.** El plan hacía
   depender la victoria del jefe. Separarlas cumple la tarea C (el evento otorga el fragmento) y
   conserva la salida de la Fase 6 como único final del nivel.
3. **La onda de suelo es una zona que viaja,** no un rayo instantáneo, para poder esquivarla.
4. **`ZonaDeDanio`** (en `Core/Combate`) es la pieza común de los ataques de jefe: puede avisar,
   moverse y desaparecer sola. Servirá también para trampas y proyectiles (Fases 9 y 10).
5. `JefeData` añade vida, fases, ventana vulnerable, velocidad, daño por contacto y el aspecto
   provisional. Se completan también en los assets que ya existían, sin pisar lo editado.

### Además

- **Nuevo menú *Fragmentum → Jefes*:** generar datos, patrones y prefabs, y restablecerlos al
  Anexo A. *Regenerar todo* lo incluye.
- **HUD:** barra de vida del jefe con su nombre canónico durante el combate.
- **`EventosJuego`** añade `JefeEnCombate` y `JefeFueraDeCombate`, para que la UI no conozca a
  los jefes.

### Verificación

- Compilación con `dotnet build`: 0 errores, 0 avisos.
- Compilación real en Unity 6000.6.3f1 en modo batch: 0 errores, 0 avisos.
- Pruebas EditMode: **164 de 164 correctas** (30 nuevas: tabla del jefe, fase según la vida, datos
  del Anexo A, prefabs, vida de los jefes, `MaquinaEstados.Detener` y la arena del Nivel 1).
- Pruebas PlayMode: **60 de 60 correctas**:
  - 11 nuevas de jefes, con física real y los prefabs reales;
  - 3 nuevas de flujo, sobre la arena del Nivel 1;
  - la prueba de la salida de la Fase 6 pasa a exigir antes la derrota del jefe.

### Pasos manuales en el Editor

Ninguno obligatorio. Los datos (`Assets/Data/Jefes` y `Assets/Data/Jefes/Patrones`), los prefabs
(`Assets/Prefabs/Jefes`) y la arena del Nivel 1 ya están generados.

- **Para ajustar a la Centinela:** su vida y sus fases están en
  `Assets/Data/Jefes/CentinelaArdiente.asset`, y cada ataque en `Assets/Data/Jefes/Patrones`
  (aviso, daño, alcance, velocidad de la onda, número de erupciones…).
- **Cuando haya arte del jefe:** va en `Pivote/Sprite` del prefab. Pon `tinte` en blanco en su
  `JefeData`, y desmarca `spriteMiraDerecha` si el dibujo mira a la izquierda.

### Qué probar a mano (Nivel 1, desde el checkpoint de x = 40)

- Avanza hasta la arena (x ≈ 61): la puerta se cierra detrás y arriba aparece «CENTINELA
  ARDIENTE» con su barra.
- Colócate delante: destella en rojo y descarga el martillo. Mientras el martillo está clavado
  (el jefe brilla en amarillo), cada golpe vale el doble.
- Onda de suelo: salta para esquivarla.
- A mitad de vida ruge; desde entonces brotan columnas de lava bajo tus pies, primero
  translúcidas. Muévete.
- Muere en la arena: reapareces en la bandera y el jefe tiene otra vez toda la vida.
- Derrótalo: aparece «Fragmento de la Ira integrado» y ya puedes usar el Ataque Cargado. Unos
  segundos después se abre la puerta y aparece el portal de salida.

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
