# CHANGELOG — Fragmentum: Ecos de la Identidad (v2)

> Registro de la refactorización, en formato **problema → causa → solución**, pensado para el
> capítulo de desarrollo de la tesis. Una entrada por fase.
>
> - **v1**: proyecto original `Tesis` (Unity 6000.2.14f1), congelado tras la auditoría de la Fase 0.
>   Desde la Fase 5 está actualizado a 6000.6.3f1 para poder abrirlo en el PC nuevo; su código no cambia.
> - **v2**: este proyecto `Tesis2.0` (Unity 6000.6.3f1 desde la Fase 3), donde se construye la versión refactorizada.

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
