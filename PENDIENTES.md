# PENDIENTES — Fragmentum: Ecos de la Identidad

> **Versión 0.40 — Mundo 2: la segunda tanda de imágenes, limpia, 2026-10-10.** Este archivo vive en el proyecto **v2** (`Tesis2.0`).
> Lista viva de lo que falta: arte, audio, texto, escenas y decisiones. Se actualiza en cada fase.

## ▶ Lo que necesito de ti

Nada de esto bloquea el trabajo: lo que falte usa placeholders. Ordenado por urgencia. Lo marcado
**en curso** ya lo estás haciendo: no hace falta que me contestes.

| # | Qué | Por qué | Cómo |
|---|---|---|---|
| N31 | **Juega el Nivel 2 montado** (rama `nivel2-recorrido`): desde el selector o con «Continuar» tras superar el Nivel 1. | Son las fases A, B y C del Mundo 2: el recorrido en bloques de color, con el agua, el hielo, sus mecánicas y sus enemigos, ya con el mini-jefe. Lo que más importa: si el hielo resbala lo justo; si el agua se siente pesada pero no lenta de más; si 15 s de aire dan para la calle hundida sin agobiar ni sobrar, con las Medusas; si el tapón se entiende (un golpe lo agrieta, el cargado lo rompe); si las placas se cruzan bien; si el golpe de la Escultura se ve venir (1 s de aviso) y se castiga después; si la torre se sube eligiendo el hueco entre las Medusas sin frustrar; y **la Plañidera** (el bloque alto azul de la plaza, x 32): si sus esquirlas se ven venir (se pone cian 0,8 s antes), si los 2 s que llora desarmada dan para castigarla, y si la plaza este, con ella y el Lamento a la vez, queda demasiado cargada. | Ten en cuenta lo que aún no está: el arte y la Doncella, que espera quieta (fase D). Al morir, una sombra azul repite tu intento: es el eco de intentos. Si un Lamento te alcanza, vas a media velocidad 2 s. El puente tapa las esquirlas. Dime lo que notes; los números están en `Docs/Diseno/Nivel2_Diseno.md`, secciones 3 y 5. Si la plaza este se hace pesada, muevo el Lamento: es una línea de `DisenoNivel2`. |
| N30 | *En curso.* **Mundo 2: lo que queda de las imágenes.** La segunda tanda llegó y está limpia en `Tesis/Imagenes/limpios/Mundo2/` (su LEEME dice cómo quedó cada una): las esquirlas de la Plañidera, el ataque del Lamento, el tapón de hielo, la torre, `Puas/P4` y `F2`, `F3` y `M5` de la Doncella. | Solo falta un fotograma para tener todos los paquetes completos. | **Falta `Doncella/L2`.** El fotograma nuevo del llanto se guardó como `L3.jpg` y sustituyó al anterior. Es bueno (arrodillada, cara descubierta, sin suelo pintado), pero `L2.jpg` sigue siendo el viejo, con la cara tapada y la franja de suelo, y los dos alternan en bucle. Pide `L2` adjuntando el `L3.jpg` nuevo, con el prompt de `L2` de `Docs/Diseno/Prompts_Mundo2.md`, apartado I2, y guárdalo como `L2.jpg`. **El ataque del Lamento vale, con un pero:** Gemini devolvió cada fotograma con un tamaño de píxel distinto, así que cada uno irá a su PPU y su píxel se verá entre un 4 y un 16 % más fino que el del resto. Yo lo dejaría así; si al verlo en el juego molesta, se repite. **Opcional** (prompts en el apartado I3): `Lamento/I2`, `Planidera/A2`, `Inundacion/Aviso2`, `Capa/Capa3`, `Doncella/F4` y Kael nadando (Pixelorama). |
| N33 | **¿Qué tamaño tiene la placa flotante?** | Su arte (`Placa1`) mide 3,46 u de ancho a la escala del Nivel 1, y en el juego la placa mide 2,5 u. | Dos salidas: importarla más pequeña (PPU 66: la placa se queda en 2,5 u, con el píxel de arte algo menor que el del resto) o hacer las placas de 3,5 u (hay que volver a comprobar los saltos del diseño). Yo me quedaría con la primera: no toca el recorrido, que ya está probado. |
| N34 | **¿A qué resolución van las cinemáticas del Mundo 2?** | Las del Nivel 1 miden 240 × 135 píxeles de arte. Las cuatro de la Doncella salieron más finas, aunque el prompt pedía 240: tres a 480 × 268 y `PresentacionDoncella2` a 394 × 220. | Tres salidas: (a) usarlas a 480, con el píxel la mitad de grande que en las del Nivel 1 (hay que ajustar la cámara de cinemáticas y repetir solo `PresentacionDoncella2`); (b) repetir las cuatro insistiendo en 240; (c) bajarlas yo a 240 promediando cada 2 × 2, que emborrona algo el dibujo. Yo iría por la (a): son las más bonitas así, y cada mundo puede tener su resolución. Se decide en la fase de narrativa. |
| N35 | **La cola de la Doncella sale cortada: ¿se repite la maestra o se tapa?** | En `F1`, la cola de agua se sale del lienzo por abajo, y los 28 fotogramas lo heredan: acaba en un corte recto. Como flota en lo alto de la arena, el corte se vería. | Dos salidas: repetir la maestra `F1` con más margen abajo (y con ella los otros 27), o conservarlas y que el juego funda en transparente las últimas filas de la cola. Yo probaría primero lo segundo al integrarla, que no cuesta imágenes, y repetiría solo si no convence. |
| N19 | **Corrige el Anexo A de `Tesis.docx`** en cuatro puntos, para que diga lo mismo que el juego. | La plataforma de combustión va por ciclo fijo (el Anexo dice que quema si te quedas encima más de 2 s). Y en el Nivel 1, aprobaste: la arena mide 25 × 15 u (el Anexo dice 60 × 15, pero también que va de x 55 a x 80), su suelo es firme (la lava brota en las erupciones) y el checkpoint está en x 43, no en x 40. | Yo no toco `Tesis.docx`. Para la combustión basta una frase: «se enciende cada pocos segundos; las brasas avisan; se resuelve esperando, no corriendo». Lo del Nivel 1 está explicado en `Docs/Diseno/Nivel1_Diseno.md`, sección 6. **Del Mundo 2** hay seis diferencias más, listadas al final de `Docs/Diseno/Nivel2_Diseno.md` (sección 9). La última es del 2026-10-10: la Plañidera no está en el Anexo, y el nivel queda con una Escultura Viviente en vez de dos. |
| N3 | *En curso.* **Clips de Kael que faltan:** daño primero; después wall slide (y, para la Fase 10, nado). El dash ✔, la muerte ✔ y agacharse ✔ ya están. | El daño se ve en cada partida: hoy sigue el clip anterior y parpadea. | En Pixelorama, con el mismo lienzo y tamaño de Kael, exporta cada tira **a 1× y sin ping-pong** a `Assets/Sprites/Kael`, o pásame los PNG. Detalle en B3. |
| N8 | *En curso.* **Arte de los demás enemigos y del jefe.** | Solo el Eco de la Ira tiene arte. | Ver «Arte que más necesito», justo debajo. |

### Arte que más necesito, por urgencia

1. **Centinela Ardiente:** ✔ *recibida.* Los 29 sprites ya están importados y la integración
   sigue por fases (B5). Opcional, según su LEEME: rehacer `R2`, que salió sobreexpuesto. Basta
   con sustituir el PNG en `Assets/Sprites/Jefes/Centinela`: los ajustes se aplican solos.
2. **Kael: daño** (N3). Se ve en cada partida. La muerte ✔ y agacharse ✔ ya están (v0.33).
3. **Golem de Ceniza:** ✔ *recibido* (15 fotogramas), en integración por fases en la rama
   `golem-mecanicas`. Opcional: rehacer **A2** (el LEEME trae el prompt); el ataque funciona sin él.
4. **Mecánicas:** ✔ *completas.* La lava, la torreta, la plataforma inestable, la de combustión
   y, desde la v0.31, la bola de fuego (dos fotogramas de vuelo y uno de chispas).
5. **Cinemáticas del jefe:** ✔ *completas.* `CinematicaJefe1`, `CinematicaJefe2`,
   `CinematicaFinalLvl1` y `CinematicaFinalLvl2`, retocada en la v0.34.
6. **Efectos de la Centinela:** ✔ *completos.* **La onda** (v0.32) y **la erupción** entera
   (v0.34): la grieta que avisa, el brote, la columna que arde y el derrumbe. Solo la caída del
   salto sigue sin VFX propio (la enseñan J4 y J5 de la Centinela).

### Resuelto en la v0.40

- **La segunda tanda de imágenes, limpia:** las tres esquirlas de la Plañidera, los cuatro
  fotogramas del ataque del Lamento (ya sin damero), el tapón de hielo con sus esquirlas, la
  torre del fondo (entera y nítida), `Puas/P4` sin halo y `F2`, `F3` y `M5` de la Doncella.
  Queda un fotograma, `Doncella/L2` (N30).
- **La capa de la Doncella salía agujereada en la limpieza de la mañana.** Su morado oscuro tiene
  el mismo tono que el fondo magenta y se tomaba por fondo. Se vio al ampliar, está corregido y
  sus 27 fotogramas están vueltos a limpiar. Los demás grupos no tenían el problema.

### Resuelto en la v0.39

- **N32: la Plañidera está en el nivel.** Va donde propuse, en la plaza helada y en el sitio de la
  segunda Escultura (x 32), y pelea como estaba escrito:
  - flota despacio hacia Kael y, de cerca, echa el brazo atrás y barre (quita 2);
  - cuando lleva 2,5 s persiguiéndolo, alza las esquirlas, las apunta 0,8 s (el aviso) y lanza
    tres, una tras otra, hacia donde esté;
  - después llora 2 s desarmada, sin atacar ni moverse: ahí se la castiga;
  - tiene 10 de vida e hiperarmadura, como el Golem, y tocarla no hace daño.
  El puente la tapa: mientras Kael está al otro lado, no lo ve. Con esto la fase C está cerrada.
  Pruébalo en N31.
- **Todas las imágenes del Mundo 2, limpias:** las 121, en `Tesis/Imagenes/limpios/Mundo2/`. Lo
  que falta y lo que hay que repetir está en N30; las dos decisiones nuevas, en N34 y N35.

### Resuelto en la v0.38

- **Fase C: los enemigos de la Tristeza**, todavía sin arte:
  - **el pesar del Lamento:** si su zarpazo o su cuerpo alcanzan a Kael, este va a media
    velocidad 2 s;
  - **las Esculturas Vivientes:** duermen como estatuas hasta que Kael pasa cerca, despiertan en
    1 s, no andan, avisan su golpe 1 s y se quedan clavadas después; los golpes no las
    interrumpen;
  - **las Medusas de Hielo:** cuatro, con su ritmo (4 s en la calle y 5 s en la torre, a la
    contra dos a dos); queman al tocarlas y no mueren: la espada las aparta y las encoge 1 s.
  - Las dos Medusas de la torre se movieron medio metro, porque rozaban la cabeza de Kael en los
    rellanos.
  Pruébalo en N31.
- **Las imágenes del Mundo 2 que había, limpias** (`Tesis/Imagenes/limpios/Mundo2/`, con su
  LEEME), y los prompts del Lamento, la Escultura y la Medusa arreglados (pedían «sin ojos» o
  editaban la estatua dormida). La Plañidera tiene los suyos.

### Resuelto en la v0.37

- **Fase B: las mecánicas de la Tristeza**, todavía sin arte:
  - **el tapón de hielo** que cierra el agujero de la plaza: un golpe lo agrieta y el cargado lo
    rompe;
  - **las placas que flotan**, subiendo y bajando con Kael encima, y resbalan;
  - **el oxígeno:** 15 s bajo el agua y, sin aire, un corazón cada 2 s sin retroceso, con una
    barra de aire en el HUD que parpadea en rojo;
  - **el velo submarino**, que oscurece los bordes de la pantalla bajo el agua;
  - **los ecos de intentos:** al reaparecer, una sombra repite el intento anterior;
  - **el eco de memoria**, que suena en el primer checkpoint de cada nivel, también en el Nivel 1.
  Pruébalo en N31.

### Resuelto en la v0.36

- **El Mundo 2 tiene diseño** (`Docs/Diseno/Nivel2_Diseno.md`), con tus ocho decisiones: el hielo
  se rompe con el Ataque Cargado, las Medusas no mueren, la Escultura despierta pero no anda, el
  oxígeno dura 15 s y después quita un corazón cada 2 s, dos checkpoints, la Doncella solo es
  vulnerable cuando llora, `jefe2.jpeg` es ella (D9) y la lista de imágenes con sus prompts
  (`Prompts_Mundo2.md`).
- **Fase A: el Nivel 2 está montado** en `Assets/Scenes/Nivel2.unity`, ya en Build Settings y en
  el selector: el recorrido de `DisenoNivel2` con el agua y el hielo de verdad, los tres Lamentos,
  los dos checkpoints, la salida y la arena de la Doncella. Pruébalo en N31.

### Resuelto en la v0.34

- **N26:** jugaste el Nivel 1 montado. El único fallo: en partida nueva, la Centinela no decía su
  línea antes del combate (con «Continuar», sí). Era la presentación: se ve solo la primera vez y,
  cuando se veía, el HUD callaba la línea creyendo que ya iba en ella, pero la de la Ira lleva la
  frase de Kael. Ahora la línea suena tras la presentación también en partida nueva.
- **N27:** las tres erupciones que faltaban entraron. La erupción se ve dibujada de principio a fin
  y ya no queda ningún rectángulo naranja en el combate de la Centinela.
- **N28:** el collider agachado mide 1,08 u (`alturaAgachado` = 0,6), lo mismo que el dibujo.
- **N29:** la muerte se queda como está.
- **N25:** `CinematicaFinalLvl2` ya está como querías: sin el fragmento flotante y con cuatro
  huecos. Llegó a 1024 × 572; a 240 × 135 se ve bien, con el fondo un poco más ruidoso.

### Resuelto en la v0.33

- **Kael se agacha y muere con su arte** (N3, a falta del daño). Agacharse: baja, se queda
  agachado respirando y, al soltar, se incorpora antes del idle. Morir: el golpe, se dobla y cae
  de rodillas; ahí se queda hasta reaparecer. Quedan dos detalles: N28 y N29.
- **La erupción quema con su columna dibujada** (E3 y E4 de la segunda tanda). El resto, en N27.

### Resuelto en la v0.32

- **N10:** el Nivel 1 está montado con el recorrido diseñado y tus tres decisiones del Anexo A
  (arena de 25 × 15, suelo firme, checkpoint tras la subida). Las piezas reales son más anchas que
  en el esquema: el lago creció, la subida lleva una inestable y el checkpoint queda en x 43.
  Pruébalo en N26.
- **N11:** el guion está cargado: líneas del HUD, cierres de los cuatro mundos con jefe, cierre del
  Jardín, entrada al final, final y presentaciones de los jefes 2, 3 y 4. Se corrige desde el
  inspector de cada asset: lo escrito a mano no se pisa.
- **La onda de la Centinela** tiene su arte: la de suelo, la doble y las del salto. Se rompe
  contra la pared de la arena en lugar de atravesarla.

### Resuelto en la v0.31

- **N15 y N16:** el golpe del Eco y la pelea con la Centinela están bien así.
- **N18:** el Eco se queda al tamaño que le diste en la escena (1,8 × 1,5).
- **N20:** jugaste el Nivel 1. Lo que pediste:
  - los rectángulos naranjas del espadazo y de la caída del salto ya no se pintan;
  - el prompt de la onda está en `Docs/Diseno/Prompts_Gemini.md`.
- **N21 y N22:** sin fuente bitmap; el encuadre del ojo (17, 19) se queda.
- **N23:** los textos de la guía de mecánicas están bien.
- **N5 y N24:** **Alt izquierdo camina.** Mientras se mantiene, la dirección se queda al 35 %, por
  debajo del 40 % del sigilo. La guía de mecánicas ya tiene su fila «Caminar».

### Resuelto en la v0.29

- **N17:** el texto de la presentación del jefe del Nivel 1 es el del guion «El que ya estaba
  aquí»: la frase de Kael y el rótulo con el nombre del jefe.

### Resuelto en la v0.21

- **N7:** el Golem de Ceniza está dibujado. Llegó con las mecánicas y tres planos de cinemática.
- **Las seis preguntas del nuevo encargo,** con lo que recomendé:
  1. la torreta disparando lleva su propio pivote, para que el bloque no salte;
  2. la lava sigue matando;
  3. la combustión va por ciclo fijo (falta el Anexo A, N19);
  4. el Golem aguanta los golpes sin interrumpir su ataque;
  5. el destello es la silueta blanca;
  6. `CinematicaFinalLvl1` sale cuando la Centinela termina de caer.

### Resuelto en la v0.15

- **La espada de la Centinela:** no es un problema. El Anexo A dice «martillazo», pero el ataque
  funciona igual con espada: tajo de arriba abajo y la hoja clavada en el suelo es la ventana para
  pegarle. En el juego ya se llama «espadazo» y la ficha de B5 describe la espada.
- **Los conceptos del jefe no se importan:** hay que trabajarlos siguiendo la ficha de B5.

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

## Revisión 0.34: qué cambió respecto de la v0.33

- **La línea de la Centinela en partida nueva** (N26): `CutsceneData.llevaDialogoDelJefe` dice si
  una presentación ya lleva las líneas «antes del jefe». La de la Ira no, así que el HUD las dice
  al empezar el combate; las de los jefes 2 a 4 sí, y no se repiten.
- **La erupción completa** (N27): aviso, brote, arde y derrumbe, que sigue un momento tras acabar
  la zona y ya sin daño.
- **Kael agachado, a la altura de su dibujo** (N28). **N29** descartado.
- **`CinematicaFinalLvl2` retocada** (N25): sin el fragmento flotante y con cuatro huecos.

## Revisión 0.33: qué cambió respecto de la v0.32

- **El trabajo de la v0.32, confirmado** en la rama `nivel1-montado` (360 EditMode y 147 PlayMode
  correctas).
- **Erupciones:** entran E3 y E4 como bucle mientras quema, a 24 px/u; el aviso sigue siendo el
  rectángulo. N27 pide la tercera tanda.
- **Kael:** agacharse (7 fotogramas) y morir (3, a 5 fps como en `Morir.pxo`). Nuevos N28 (altura
  agachado) y N29 (pie de `Morir`, opcional). N3 y B3, al día.

## Revisión 0.32: qué cambió respecto de la v0.31

- **El Nivel 1 tiene su recorrido de diseño** (`DisenoNivel1`), que sustituye al provisional:
  tres secciones, lava en cuencas, dos plataformas inestables, dos de combustión desfasadas, la
  torreta mirando al jugador, 7 Ecos a tu escala y el Golem, y la arena de 25 × 15 con plataformas
  a tres alturas.
- **La Centinela atraviesa las plataformas de la arena** y su salto cae al suelo: subirse a una es
  esquivarlo. Las erupciones brotan bajo los pies de Kael, también en una plataforma.
- **El guion, cargado** en `Assets/Data/Narrativa`.
- **La onda, con arte.** Las erupciones no: la primera tanda no cuadraba (N27).
- **N10 y N11 resueltos.** Nuevos N26 (juega el nivel montado) y N27 (erupciones). N19 incluye ahora
  las tres decisiones del Nivel 1.

## Revisión 0.31: qué cambió respecto de la v0.30

- **Respuestas a tu prueba del Nivel 1,** en la rama `ajustes-pendientes`. Resueltos N5, N15, N16,
  N18, N20, N21, N22, N23 y N24.
- **La bola de fuego** tiene su arte. La limpieza es nueva: venía sobre un damero pintado, no
  sobre magenta.
- **`CinematicaFinalLvl2`** entra como segundo plano de la derrota. Nuevo N25 (retoque
  opcional).
- **El espadazo y la caída del salto** ya no pintan su rectángulo.
- **Alt izquierdo camina.**
- **N10 y N11** pasan a ser «revisa la propuesta»: el diseño del Nivel 1 y el guion están en
  `Docs/Diseno`.

## Revisión 0.30: qué cambió respecto de la v0.29

- **Guía de mecánicas** (rama `menu-mecanicas`): una pantalla «Mecánicas» en el menú principal y en
  la pausa. Explica los movimientos de Kael con las teclas del asset de input y las cuatro
  habilidades, que aparecen según se integran.
- **Nuevos N23** (revisar los textos) **y N24** (cómo ir en sigilo con teclado, para la Fase 10).
- **B9:** filas nuevas para los íconos de las habilidades y las teclas del mando en la guía.

## Revisión 0.29: qué cambió respecto de la v0.28

- **Cinemáticas en pixel art, fase C:** la presentación del jefe del Nivel 1 sigue el guion «El
  que ya estaba aquí» (9,6 s, cuatro beats). Lleva brasas en la rejilla, el texto de Kael, el
  rótulo y el flash final, y se ve una vez por partida.
- **El texto va como en la cinemática inicial**, a la escala de la imagen, por decisión tuya; N21
  pasa a ser opcional.
- **N17 resuelto:** el texto de la presentación es el del guion.
- **Nuevo N22:** el encuadre del ojo deja ver un hueco negro.
- **Los ocho cues de sonido del guion** están puestos con su nombre y sin clip. Cada uno avisa por
  consola al sonar. El rugido irá con el del jefe en la fase D.

## Revisión 0.28: qué cambió respecto de la v0.27

- **Cinemáticas en pixel art, fase B:**
  - el sistema de cinemáticas monta beats (fundidos desde y hacia negro, encuadre en píxeles,
    temblores, flash en fotogramas y cues de sonido por nombre);
  - el pixel art lo dibuja la cámara de cinemáticas, también el texto, en la rejilla.
- **Paro antes de la fase C,** la cinemática del jefe: necesita la fuente bitmap (N21).

## Revisión 0.27: qué cambió respecto de la v0.26

- **Cinemáticas en pixel art, fase A** (rama `cinematicas-pixel`): la cámara de cinemáticas con la
  Pixel Perfect Camera de URP, a 240 × 135, y los planos del Nivel 1 a 1 píxel por unidad. Las
  capturas a 1920 × 1080, 1280 × 720 y 960 × 540 están en `Docs/Cinematicas`.
- **Nuevo N21:** la fuente bitmap para el texto de las cinemáticas.

## Revisión 0.26: qué cambió respecto de la v0.25

- **El dash de Kael** tiene su clip (rama `kael-dash`, que sale de `golem-mecanicas`): 4
  fotogramas estirados a la duración del dash. N3 y B3, al día. Pruébalo con el resto (N20).

## Revisión 0.25: qué cambió respecto de la v0.24

- **Fase E (verificación):** el menú *Fragmentum → Niveles → Verificar el arte del Nivel 1*
  revisa los 30 sprites, los 13 clips, los 4 Animators, los 6 prefabs y sus eventos. Comprueba
  también que la plataforma inestable no sostiene cuando se ve rota y que la de combustión
  sostiene siempre. Se ejecuta también al final de «Regenerar todo», que falla si algo no cuadra.
- **La integración del Golem, las mecánicas y las cinemáticas del Nivel 1 está completa** en la
  rama `golem-mecanicas`, a falta de tu prueba (N20) y de tu «fusiona».

## Revisión 0.24: qué cambió respecto de la v0.23

- **Fase D (en el juego):**
  - el Golem de Ceniza con su arte sustituye a la cápsula en el Nivel 1, con hiperarmadura y la
    onda de A3;
  - la lava, la torreta, la plataforma inestable y la de combustión, con su arte y sus ciclos
    nuevos;
  - la bola de la torreta, con sprite provisional;
  - los planos del jefe, a escala entera: la presentación y la nueva cinemática de la derrota;
  - la Centinela ruge al empezar el combate.
- **Nuevo N20:** juega ese tramo. Queda la fase E (verificación).
- **B1, B4 y B10, al día.**

## Revisión 0.23: qué cambió respecto de la v0.22

- **Fase C (Animators):** el del Golem, movido por la FSM del enemigo (anda al ritmo de su
  velocidad y parado respira), y uno por mecánica, movido por un entero que pone su script.
  Siguen las fases D (scripts y prefabs) y E (verificación).

## Revisión 0.22: qué cambió respecto de la v0.21

- **Fase B (clips):** los 4 clips del Golem, con los tiempos del encargo, y los 9 de las
  mecánicas, uno por estado. Se generan desde *Fragmentum → Niveles → Generar los clips del
  Golem y de las mecánicas* y en «Regenerar todo». Siguen las fases C a E.

## Revisión 0.21: qué cambió respecto de la v0.20

- **Golem de Ceniza, mecánicas y cinemáticas del Nivel 1, fase A (importación):** los 30 PNG
  están en `Assets/Sprites` (`Enemigos/GolemCeniza`, `Mecanicas` y `Cinematicas/Nivel1`), cada
  uno con los píxeles por unidad y el pivote de su carpeta.
- **Un solo importador** (`ImportadorArte`) con una tabla (`TablaArte`). Sustituye a los tres de
  antes; la Centinela y el Eco no cambian.
- **Rama nueva** `golem-mecanicas`, que sale de `centinela-arte`. Siguen las fases B a E.
- **N7 resuelto, N19 nuevo** (el Anexo A). La lista de arte, al día.

## Revisión 0.20: qué cambió respecto de la v0.19

- **Arte de la Centinela, fase E (verificación):** el menú *Fragmentum → Jefes → Verificar
  Centinela* revisa los sprites, los clips, el Animator, el prefab y los eventos, y da un error
  con el nombre de lo que falte. También se ejecuta al final de «Regenerar todo».
- **La integración del arte está completa** (fases A a E) en la rama `centinela-arte`, que sale
  de `ajustes-nivel1`. Las dos esperan a que lo pruebes: dime «fusiona» y las llevo a `main`.

## Revisión 0.19: qué cambió respecto de la v0.18

- **Arte de la Centinela, fase D:** el jefe ya combate con su arte en el Nivel 1.
  - Los golpes salen cuando el dibujo golpea.
  - La espada clavada (C2) es la ventana y late.
  - El salto sostiene J4 hasta caer.
  - Al morir se queda en la ceniza.
- **Los seis ajustes que aprobaste**, aplicados tal cual (CHANGELOG):
  1. los encadenados acortan E1;
  2. la ventana dura C2 en todas las fases;
  3. J4 se sostiene en el aire;
  4. las erupciones usan GolpeSuelo;
  5. la derrota dura 2,1 s y D4 se queda;
  6. E3 es una ventana corta.
- **Cambia cuándo llega la recompensa:** el Fragmento de la Ira y el Ataque Cargado llegan cuando
  Kael recoge el fragmento de la ceniza, no al derrotar al jefe. La puerta se abre después.
- **Para probar a mano:** la lista está en el CHANGELOG («Qué probar a mano (fase D)»).

## Revisión 0.18: qué cambió respecto de la v0.17

- **Arte de la Centinela, fase C (Animator):** `CentinelaArdiente.controller`, con un estado por
  clip. El rugido y la muerte interrumpen cualquier estado y la muerte se queda en D4. El
  espadazo puede acabar con la espada clavada.
- **Un cambio respecto de tu encargo:** los ataques salen de cualquier estado, no solo de Caminar,
  porque el combate actual los encadena y el golpe tiene que coincidir con el daño (CHANGELOG).

## Revisión 0.17: qué cambió respecto de la v0.16

- **Arte de la Centinela, fase B (clips):** los 8 clips, con los tiempos del LEEME, en
  `Assets/Animaciones/Jefes/Centinela`. La espada clavada forcejea sin fotograma extra y la muerte
  se queda en D4. Se generan desde una tabla (*Fragmentum → Jefes → Generar los clips de la
  Centinela*).
- **De tu segunda versión del encargo** se añade lo que encaja con el combate actual: D4 se queda
  y Kael recoge el fragmento, el salto lo sube el cuerpo, el forcejeo y los golpes por eventos.

## Revisión 0.16: qué cambió respecto de la v0.15

- **Arte de la Centinela, fase A (importación),** en la rama nueva `centinela-arte`, que sale de
  `ajustes-nivel1`. Los 29 sprites están en `Assets/Sprites/Jefes/Centinela` con sus ajustes:
  49 px por unidad, 58 el idle frontal, y el pivote en los pies. Los fija un importador, así que
  sobreviven a un reimport.
- **Tus respuestas al encargo:** el arte va en la Centinela que ya existe, el combate se queda
  como está, `D4 (2).png` es el D4 y se siguen las rutas de la v2.
- **B5 al día.** La ficha de la Centinela se conserva como referencia de tiempos.

## Revisión 0.15: qué cambió respecto de la v0.14

- **La Centinela lleva espada.** El ataque «martillazo» pasa a llamarse «espadazo» en el juego,
  los datos y las pruebas. La mecánica no cambia. Va en la misma rama `ajustes-nivel1`.
- **Ficha de la Centinela (B5) reescrita:** espada en vez de martillo, qué pose de
  `PosiblesAnimacionesJefe1` sirve para cada clip y qué falta por dibujar. Los conceptos no se
  importan.

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
| `Jefes/Jefe1.jpeg`, `Fase2Jefe1.jpeg`, `PosiblesAnimacionesJefe1.jpeg` | Centinela Ardiente y su fase 2 | Son conceptos: no se importan. Guían los sprites, que se dibujan siguiendo la ficha de B5 | P1 |
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
| D9 | **Correspondencia de conceptos de jefe.** | `Jefe2 (2).png` es una figura encapuchada azul/cian con esquirlas. `Jefe3 (2).png` es una figura oscura con espada y brillo verde azulado. | Jefe2 = Doncella del Lamento ✔ (confirmado el 2026-10-08; puede rediseñarse, ver N30). Jefe3 = Espectro del Pavor (Miedo): **sin confirmar.** |
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
| `Cinematica_Jefe_Ira` (~9,6 s, con sus planos y a escala entera) | Al entrar en la arena de la Centinela, antes del combate (una vez por carga del nivel) | ✔ Planos `CinematicaJefe1` y `CinematicaJefe2` y su guion «El que ya estaba aquí» | — |
| `Cinematica_Derrota_Ira` (a escala entera) | Cuando la Centinela termina de caer, antes de recoger el fragmento | ✔ `CinematicaFinalLvl1` y `CinematicaFinalLvl2`, retocada (v0.34) | — |
| `Cinematica_Jefe_Tristeza`, `_Miedo`, `_Culpa` | Al entrar en la arena de cada jefe | ✔ Guion (nombre, mundo y la línea del jefe). Imágenes opcionales | P2 |
| `Cinematica_Cierre_Ira` | Al superar el mundo de la Ira | ✔ Guion. Faltan las imágenes | P1 |
| `Cinematica_Cierre_Tristeza`, `_Miedo`, `_Culpa` | Al superar cada mundo | ✔ Guion. Faltan las imágenes | P2 |
| `Cinematica_Cierre_Jardin` (~30 s: Kael ve al Soldado) | Al superar el Jardín | ✔ Guion (cinco textos de 6 s). Faltan las imágenes | P2 |
| `Cinematica_Entrada_Final` (~45 s) | Antes del final | ✔ Guion (siete textos de 6 s). Faltan las imágenes | P2 |
| `Cinematica_Final` y créditos («¿Eres un error… o una nueva identidad?») | Al terminar el juego | ✔ Guion (la pregunta, sola y al final). Faltan las imágenes | P2 |
| Memorias de los Niveles 4 y 5 (4 + 4, ~5 s) | Al activar cada memoria (`MemoryObject`, Fase 10) | Todo: se crearán con la mecánica | P2 |

> **Presentación del jefe.** Lleva `sobreElJuego` marcado: detrás se ve la escena congelada, con
> un velo (`opacidadVelo`) y franjas de cine. Para que la cámara encuadre al jefe y el combate
> empiece al terminar, basta con que la cinemática esté en el campo `cinematicaJefe` del
> `LevelData`. Su diálogo sustituye a las líneas «antes del jefe» del HUD.
>
> La v2 no usa Timeline: las cinemáticas son diapositivas. Si alguna necesita animación, una
> diapositiva puede apuntar más adelante a un clip de Timeline sin cambiar el resto del sistema.

### B2. Diálogos — `DialogueData` + `DialogueManager` (Fase 8)

**Estado en la v2 (v0.32).** Cada mundo tiene su `Dialogos_<Emoción>` en `Assets/Data/Narrativa`
con las líneas del guion (`Docs/Diseno/Guion.md`): apertura, eco de memoria, antes del jefe (si hay
jefe) y cierre. La apertura y el cierre nombran la emoción del mundo. Se muestran abajo en el HUD
(el cierre, en el fin de nivel). Los ecos de memoria suenan desde la v0.37 en el primer
checkpoint de cada nivel (`Checkpoint.ecoDeMemoria`), la primera vez que se toca.

| Asset | Dónde | Formato | Prioridad |
|---|---|---|---|
| Líneas de Kael y de los Arquitectos, **×6 mundos**. Pueden ser varias por momento: se muestran en orden. Las de «antes del jefe» solo se usan en un nivel sin presentación del jefe (B1). | `Dialogos_*` → `lineas` (hablante, texto, momento, duración) | ✔ Guion cargado. Se corrige desde el inspector | — |
| Pregunta y escala del registro emocional | `Assets/Data/Narrativa/RegistroEmocional.asset` | ✔ Revisadas (N12). Sin código de participante (`pedirParticipante` apagado) | — |
| Retratos opcionales (Kael, los 5 Arquitectos y el Soldado) | Pendiente de campo (hoy solo se muestra el nombre) | PNG 256×256 | P3 |

### B3. Jugador — Kael

**Estado en la v2 (arte de Kael, 2026-09-30).** El jugador está completo en lógica (prefab
`Assets/Prefabs/Player.prefab`, FSM de 12 estados) y **ya tiene arte**: quieto, correr, salto con
caída, ataque, dash, agacharse y morir, dibujados en Pixelorama. El detalle está en el CHANGELOG,
entrada «Arte de Kael» (y las del dash y de agacharse y morir).

- **Dónde está el arte:**
  - las hojas, en `Assets/Sprites/Kael`, a escala 1× y 64 PPU;
  - los clips y `Kael.controller`, en `Assets/Animaciones/Kael`, generados por
    *Fragmentum → Jugador → Importar arte de Kael y regenerar el prefab*.
- **Quién elige el clip:** `PlayerAnimacion`, con una tabla estado de la FSM → clip que se edita
  en el Inspector. El Animator no tiene transiciones propias.
- **Parámetros que escribe el código:**
  - `Movement` (float, u/s) acelera el clip de correr;
  - `VelocidadAtaque` (float) estira el de ataque a la duración del golpe;
  - `VelocidadDash` (float) estira el del dash a la duración del dash;
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
| Muerte | `DEAD` | P1 | ✔ 3 fotogramas a 5 fps, como en `Morir.pxo`: se queda de rodillas hasta reaparecer |
| Dash | `DASH` | P2 | ✔ 4 fotogramas, estirados a la duración del dash (0,18 s) |
| Wall slide / wall jump | `WALL_SLIDE`, `WALL_JUMP` | P2 | Usan Caida y Salto. Falta el de wall slide (pegado a la pared) |
| Agacharse | `CROUCH` | P2 | ✔ 7 fotogramas: baja (1 a 3), agachado en bucle (3 y 4) y, al soltar, se levanta (5 y 6) antes del idle. Avanzar agachado usa el mismo bucle. El collider agachado mide lo que el dibujo: 1,08 u |
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
| Ira | Golem de Ceniza (mini-jefe, vida 10) | ✔ `GolemCeniza.prefab`, con su arte. Inmune a la lava; los golpes no lo aturden (hiperarmadura) | ✔ Arte final (15 fotogramas) | Opcional: rehacer A2 (el LEEME trae el prompt) | P1 |
| Ira | Torreta de fuego fija | ✔ Es una mecánica, no un enemigo: `TorretaFuego` / `Mecanicas/TorretaFuego.prefab` (Fase 9) | Bloque oscuro con boca naranja; la bola de fuego es un cuadrado naranja | Sprite de la torreta y de la bola (B10) | P1 |
| Tristeza | Lamento (el Eco de la Tristeza) | ✔ `Lamento` / `Enemigo_Lamento`; tres en el Nivel 2. ✔ Su pesar (v0.38): media velocidad 2 s | Cápsula azul. Su arte sale de editar los fotogramas del Eco (`Prompts_Mundo2.md`, B1): limpios los 12; los cuatro del ataque, cada uno en su rejilla y con su PPU | Integrar el arte | P1 |
| Tristeza | Escultura Viviente, Medusas de Hielo | ✔ En el Nivel 2: `Enemigo_Escultura` con `EsculturaDormida` (1, desde la v0.39) y `MedusaHielo` (4) | Rectángulo gris que se aclara al despertar; círculo celeste. Su arte está limpio y completo: 14 de la Escultura y 6 de la Medusa | Integrar el arte | P1 |
| Tristeza | La Plañidera (mini-jefe) | ✔ En el Nivel 2 (v0.39): `Enemigo_Planidera`, con `Planidera` para las esquirlas | Rectángulo azul que se pone cian al apuntar y gris al llorar. Su arte está limpio y completo: 16 fotogramas (`A2`, en otra rejilla) y las tres esquirlas | Integrar el arte | P1 |
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
| **Centinela Ardiente** (Ira) | ✔ Completo y más difícil desde la v0.14: tres fases, espadazo en combo, onda doble, salto aplastante y erupciones; presentación al entrar en la arena del Nivel 1 | Arte integrado en la rama `centinela-arte`, pendiente de tu prueba y de fusionar: los 29 sprites, los 8 clips, su Animator, el combate por eventos del dibujo, la ceniza de D4 con el fragmento que recoge Kael y el menú *Verificar Centinela*. La onda ✔ (v0.32) y la erupción entera ✔ (v0.34) ya tienen arte. Falta: el VFX de la caída del salto y, si quieres, rehacer R2. | P1 |
| **Doncella del Lamento** (Tristeza) | Esqueleto: datos, prefab y dos fases sin patrones | Patrones de olas, inundación, llanto y congelamiento (fase D del Mundo 2). **Su arte está recibido y limpio, sin importar:** 27 de los 28 fotogramas (con el diseño de `jefe2.jpeg`; falta `L2`, N30), la ola, el aviso de la inundación, las púas, la capa congelada, el fragmento y las cuatro cinemáticas. Antes de integrarlo: N34 (resolución de las cinemáticas) y N35 (la cola cortada). | P1 |
| **Espectro del Pavor** (Miedo) | Esqueleto: casi invisible en combate | Patrón de ataque con destello y clones (Fase 10); animaciones; la partícula que distingue al real. Concepto: `Jefe3 (2).png` en la v1. | P2 |
| **Juez Silente** (Culpa) | Esqueleto: inmune salvo en la ventana, que abre `ActivarMemoriaCorrecta()` | Proyectar las memorias y conectarlas (`MemoryObject`, Fase 10); todo el arte. | P2 |
| **El Verdadero Soldado** (Final) | Esqueleto: tres fases; no cae sin `CompletarIntegracion()` | Combate espejo, secciones de cada mundo y el plataformeo final (Fase 10); todo el arte (el concepto **no está** en el proyecto). | P2 |

#### Ficha: Centinela Ardiente (para dibujarla)

> **Ya está dibujada:** el arte está en `Assets/Sprites/Jefes/Centinela`. La ficha se conserva
> como referencia de los tiempos del combate.

**Qué es.** La Arquitecta de la Ira y el jefe del Nivel 1: una armadura de roca volcánica con
grietas de lava y un mandoble al rojo, como en tus conceptos. Se enfurece según pierde vida: más
rápida, con menos pausas y, en la última fase, al rojo vivo (el rojo lo pongo yo por código).

**La espada.** El Anexo A dice «martillazo», pero con espada funciona igual: el golpe es un tajo de
arriba abajo y la hoja se queda clavada en el suelo, que es cuando se le pega. En el juego el ataque
ya se llama «espadazo». Quema 2,6 × 2,6 u delante de ella: cabe un mandoble del largo de sus
piernas, como el de los conceptos, así que no hay que tocar nada.

**Tamaño.** 2,2 u de ancho y 3,4 u de alto, casi el doble que Kael. Recomendado a 1×, al detalle
de Kael: unos 220 px de alto. Si la dibujas ampliada, que el lienzo no pase de 2048 px de lado; yo
ajusto la importación.

En `cinematicJefe1` es mucho más grande que Kael. En una cinemática vale, pero en el combate esa
talla no dejaría sitio para esquivar. Si la quieres más grande, dímelo antes de dibujarla: cambian
la hitbox y el alcance de los golpes.

**Tus conceptos** (`Referencias/Arte/Jefes`). No los importo: sirven de guía. Las seis poses de
`PosiblesAnimacionesJefe1` (de izquierda a derecha, fila de arriba y luego la de abajo) encajan así:

| Pose del concepto | Clip |
|---|---|
| 1. De pie, la espada clavada y las manos en el pomo | Golpe al suelo: clava la espada y salen las ondas. También vale para la presentación |
| 2. La espada baja, en diagonal (como en `Jefe1`) | Idle y base de Andar |
| 3. La espada sobre la cabeza y el pecho encendido | Anticipación del espadazo: es el aviso que el jugador aprende a leer |
| 4. El barrido con la estela | Golpe del espadazo |
| 5. Las grietas estallan y alza el puño | Rugido |
| 6. Apagada, gris y de rodillas | Muerte: la ira se enfría y «el suelo se estabiliza» |

- **`Fase2Jefe1`**, con la armadura rota y el núcleo blanco a la vista, puede ser su aspecto desde
  el 50 %. Es opcional: si la dibujas, cambio el sprite al entrar en la fase 2.
- **Lo que falta en los conceptos:** el salto entero, la espada clavada (atrapada y sacándola) y
  los fotogramas intermedios de cada clip.
- **Al recortar:** los conceptos tienen pintado el cuadriculado de la transparencia. Los sprites
  necesitan transparencia real.

**Animaciones.** Cada ataque tiene que reconocerse por la pose antes de que llegue: así el jugador
aprende a esquivarlo.

| Clip | Qué hace | Tiempo en el juego | Fotogramas sugeridos |
|---|---|---|---|
| Idle | Quieta, la espada baja en la mano | Bucle | 2–4 |
| Andar | Avanza hacia el jugador: 2,4 u/s, que llega a 3,8 en la última fase | Bucle; ajusto el ritmo a la velocidad | 4–6 |
| Espadazo | Levanta la espada sobre la cabeza y la descarga delante | Anticipación: 0,55 s el primero y 0,3–0,35 s los encadenados. Golpe: 0,15 s | 3–4 |
| Espada clavada | La hoja queda hundida en el suelo y ella atrapada: es cuando se le pega | 1,2 s (0,9 s en furia) | 1–2, más 1–2 para sacarla |
| Golpe al suelo | Clava la espada y salen las ondas | Anticipación 0,5–0,6 s, golpe breve | 2–3 |
| Salto | Se agacha, salta y cae con todo su peso, la espada por delante | Agacharse 0,5 s, aire 0,8 s, caída breve | 4–5: agachada, subiendo, cayendo y aterrizaje |
| Rugido | Al empezar las fases 2 y 3 | 0,9 s | 2–3 |
| Muerte | Se apaga y cae de rodillas | 2 s | 3–5 |
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
| `Nivel1` (Ira) | ✔ Montada con su recorrido de diseño (v0.32, `DisenoNivel1`): tres secciones pintadas con el tileset, 7 Ecos y el Golem, las mecánicas, checkpoint en x 43 y la arena de 25 × 15 con la Centinela. | ✔ (2) | ✔ Probada por ti (v0.34): solo fallaba la línea del jefe en partida nueva, ya corregida. | P1 |
| `Nivel2` (Tristeza) | ✔ Montada con su recorrido de diseño (v0.36, `DisenoNivel2`), en bloques de color: agua, hielo, 3 Lamentos, 2 checkpoints y la arena de 25 × 15 con la Doncella sin patrones. ✔ Sus mecánicas (v0.37): tapón rompible, placas flotantes, oxígeno, velo, ecos de intentos y eco de memoria. ✔ Sus enemigos (v0.38 y v0.39): Lamentos que pesan, una Escultura dormida, Medusas y la Plañidera, el mini-jefe. | ✔ (3) | La fase D (la Doncella), el arte y tu prueba (N31). | P1 |
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
| 2 Tristeza | ✔ `Starter Tiles Platformer/IceSet`, `tileset/Tilemaps/rock_ice*.aseprite`, `Pixel Adventure 1/Traps/Sand Mud Ice` | ~ **Recibido y limpio, sin importar** (`Tesis/Imagenes/limpios/Mundo2/`): el tileset y la decoración (falta cortarlos), siete capas de parallax (cielo, ciudad lejana, torre, fondo submarino, ciudad hundida y dos primeros planos; las dos ciudades no casan por los bordes), la superficie del agua, las burbujas, la burbuja de oxígeno del HUD y el tapón de hielo rompible. |
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
| Íconos de las 4 habilidades | `PantallaMecanicas` → `HabilidadData.icono` | ✗ La guía funciona sin ellos: cada habilidad lleva el color de su emoción. | P3 |
| Teclas del mando en la guía de mecánicas | `EtiquetasControles` | ✗ Hoy solo teclado y ratón, como la ayuda de la pausa. Para el mando harían falta sus íconos (A, B, X, Y). | P3 |
| Panel de opciones | `MainMenu` | ✗ **No existe**: el botón «Configuration» no hace nada | P3 |
| Corazones y monedas | HUD | ✔ `Elementos/Corazones.png`, `coins.png`, `Animaciones/hearths.anim`, `Coin.anim` | — |
| Logo | Menú | ✔ `Images/tituloxd.png` en la v1; falta importarlo en la v2 (hoy el título es texto) | P3 |
| Recursos esenciales de TextMeshPro | Toda la UI | ✗ No importados: la UI usa el `Text` integrado de Unity a través de `FabricaUI`. Se importan con *Window → TextMeshPro → Import TMP Essential Resources*; después se cambia `FabricaUI` y nada más. | P3 |

### B10. VFX y objetos de mecánica

| Asset | Componente *(propuesto)* | Existe / candidato | Prioridad |
|---|---|---|---|
| Plataforma que se desmorona | `PlataformaInestable` | ✔ Arte final: intacta, agrietada (el aviso), rota y polvo. Se rompe aunque te bajes; los tiempos son públicos. | — |
| Plataforma de combustión | `PlataformaCombustion` (antes `PlataformaCaliente`) | ✔ Arte final: apagada, calienta y encendida, en un ciclo fijo. Falta corregir el Anexo A (N19). | — |
| Lava | `Lava` | ✔ Arte final: la franja y su espejo, alternados (`tramos`). Es un solo fotograma: si se quiere que se mueva, harían falta más (opcional). | P3 |
| Torreta de fuego y su bola | `TorretaFuego` + `Proyectil` | ✔ Arte final de la torreta (reposo, carga y disparo) y de la bola: vuela en bucle con dos fotogramas y al apagarse deja sus chispas 0,2 s (sin daño). | — |
| Checkpoint | `Checkpoint` (Fase 6) | Provisional: un mástil y un paño que se tiñe con el acento del mundo al activarse. Candidatos de la v1: `Pixel Adventure 1/Items/Checkpoints` (bandera animada) y `tileset/Puerta/respawn point_*`. El sprite va en `Nivel/Checkpoints/L1_CP1/Pano` (campo `indicador` del `Checkpoint`). | P1 |
| Salida del nivel | `SalidaNivel` (Fase 6) | Provisional: un portal translúcido del color de acento. Falta decidir su aspecto; en los niveles con jefe (Fase 7) puede bastar la puerta de la arena. | P2 |
| VFX del ataque cargado | modificador de `ATTACK` | ✗ | P1 |
| Estela del dash | `DASH` | ✗ | P3 |
| Material semitransparente de la sombra de eco | `EcoDeIntentos` | ✔ No hace falta: la sombra es un `SpriteRenderer` translúcido con los fotogramas de Kael (v0.37). | — |
| Fade a blanco (oxígeno agotado) | `OxigenoJugador` | Sustituido (v0.37): sin aire se pierde un corazón cada 2 s y la barra de aire parpadea en rojo. | — |
| Volumen de post-procesado submarino | `IndicadorAgua` | ✔ Sustituido por el velo del HUD (v0.37): la viñeta de la interfaz en azul, sin perfil de URP. | — |
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

- **0.40 (2026-10-10)** — Segunda tanda de imágenes del Mundo 2, limpia: esquirlas, ataque del Lamento, tapón de hielo, torre, `P4` y tres de la Doncella. Corregida la limpieza de la Doncella, que agujereaba la capa. Queda `Doncella/L2` (N30).
- **0.39 (2026-10-10)** — Mundo 2, fase C cerrada: la Plañidera, el mini-jefe, en la plaza helada. Las 121 imágenes del Mundo 2, limpias. N32 resuelto; N30 reescrito con lo que falta; nuevos N34 (resolución de las cinemáticas) y N35 (la cola de la Doncella).
- **0.38 (2026-10-09)** — Mundo 2, fase C: el pesar del Lamento, las Esculturas dormidas y las Medusas. Nuevos N32 (dónde va la Plañidera) y N33 (tamaño de la placa).
- **0.37 (2026-10-08)** — Mundo 2, fase B: el tapón de hielo, las placas flotantes, el oxígeno, el velo, los ecos de intentos y el eco de memoria.
- **0.36 (2026-10-08)** — El Mundo 2 tiene diseño y su fase A: el Nivel 2 montado en bloques de color. Nuevos N30 (las imágenes) y N31 (juega el nivel).
- **0.34 (2026-10-08)** — Respuestas a la prueba del Nivel 1 montado: la línea de la Centinela en partida nueva, la erupción completa, Kael agachado a 1,08 u y `CinematicaFinalLvl2` retocada. Resueltos N25, N26, N27, N28 y N29.
- **0.33 (2026-10-07)** — La v0.32, confirmada en `nivel1-montado`. La erupción quema con su columna dibujada (E3 y E4); N27 pide el aviso, el brote y el colapso. Kael se agacha y muere con su arte; nuevos N28 (altura agachado) y N29 (pie de `Morir`). N3 y B3 al día.
- **0.32 (2026-10-07)** — Nivel 1 montado con su recorrido de diseño, guion cargado y la onda de la Centinela con arte. N10 y N11 resueltos; nuevos N26 (juega el nivel) y N27 (erupciones); N19 ampliado.
- **0.31 (2026-10-07)** — Respuestas a la prueba del Nivel 1: bola de fuego, `CinematicaFinalLvl2`, zonas sin rectángulo, caminar con Alt y propuestas de diseño del Nivel 1 y del guion (N10 y N11). Nuevo N25.
- **0.30 (2026-10-07)** — Guía de mecánicas en el menú principal y en la pausa, en la rama `menu-mecanicas`. Nuevos N23 (textos de la guía) y N24 (sigilo con teclado).
- **0.29 (2026-10-06)** — Cinemáticas en pixel art, fase C: la presentación del jefe según su guion. N17 resuelto, N21 opcional, nuevo N22 (encuadre del ojo).
- **0.28 (2026-10-06)** — Cinemáticas en pixel art, fase B: el sistema por beats y el texto en la rejilla. La fase C espera la fuente (N21).
- **0.27 (2026-10-06)** — Cinemáticas en pixel art, fase A: la cámara con la Pixel Perfect Camera, en la rama `cinematicas-pixel`. Nuevo N21 (fuente bitmap).
- **0.26 (2026-10-06)** — Dash de Kael: su clip, estirado a la duración del dash, en la rama `kael-dash`.
- **0.25 (2026-10-06)** — Golem de Ceniza y mecánicas, fase E: el menú de verificación. La integración está completa, a falta de tu prueba.
- **0.24 (2026-10-06)** — Golem de Ceniza y mecánicas, fase D: scripts, prefabs y cinemáticas en el juego.
- **0.23 (2026-10-06)** — Golem de Ceniza y mecánicas, fase C: los Animators.
- **0.22 (2026-10-06)** — Golem de Ceniza y mecánicas, fase B: los clips, desde dos tablas.
- **0.21 (2026-10-06)** — Golem de Ceniza, mecánicas y cinemáticas del Nivel 1, fase A: los 30 sprites importados con una sola tabla, en la rama `golem-mecanicas`.
- **0.20 (2026-10-05)** — Arte de la Centinela, fase E: el menú de verificación. La integración está completa, a falta de tu prueba.
- **0.19 (2026-10-05)** — Arte de la Centinela, fase D: el jefe combate con su arte y deja el fragmento en la ceniza.
- **0.18 (2026-10-05)** — Arte de la Centinela, fase C: el Animator.
- **0.17 (2026-10-05)** — Arte de la Centinela, fase B: los 8 clips. Se añade lo nuevo de la segunda versión del encargo.
- **0.16 (2026-10-05)** — Arte de la Centinela, fase A: los 29 sprites, importados en la rama `centinela-arte`. B5 y la lista de arte al día.
- **0.15 (2026-10-02)** — La Centinela lleva espada: el «martillazo» pasa a ser «espadazo» y la ficha de B5 se reescribe con las poses de los conceptos, que no se importan.
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
