# Nivel 3 · Miedo · Bosque Tenebroso: el recorrido

> **Estado:** aprobado el 2026-10-14, con las nueve decisiones de la sección 9 («haz lo que
> recomiendas»). **Fases A a F montadas** en `Assets/Scenes/Nivel3.unity`:
>
> - **Fase A, el recorrido:** los bloques, las ramas, las raíces del callejón que no agarran, los
>   Espectros y los Centinelas Ciegos con su ruta, los tres checkpoints, la salida y la arena con el
>   Espectro del Pavor.
> - **Fase B, las mecánicas:** la oscuridad por tramos, la luz de Kael, las antorchas (se encienden
>   con E), los cepos, las ramas podridas que ceden, los anillos de ruido y las cintas de la ruta
>   buena.
> - **Fase C, los enemigos:** el sueño, la casi invisibilidad y la espera en el borde de la luz del
>   Espectro; el oído en cono, el aviso y la estocada del Centinela Ciego; y el Acechador, que
>   embiste y se estrella contra los árboles del claro.
> - **Fase D, el jefe:** el Espectro del Pavor, invisible e intangible salvo cuando ataca, con su
>   tajo, su embestida, sus hojas y sus tres sombras; su grito y la oscuridad que retrocede al
>   vencerlo.
> - **Fase E, el arte:** los enemigos, el jefe y sus hojas, las mecánicas, el tileset del bosque,
>   el atrezo y el fondo; y sus puertas: la del árbol, la verja de raíces y la lámpara de los
>   checkpoints.
> - **Fase F, las cinemáticas:** la presentación y la derrota del Espectro del Pavor y la salida del
>   Miedo, con sus planos.
> - **La ciénaga** (2026-10-19, sección 10): una parte de agua entre el CP2 y el claro, con marañas
>   que solo cruza el Dash Acuático que Kael ganó en el Mundo 2. Aprobada a la espera de lo que diga
>   el otro tester (PENDIENTES N50), con su arte. Todo lo que había desde x 84 se corrió 28 u.
>
> Los números de abajo son los de `Assets/Scripts/Editor/DisenoNivel3.cs`, que monta
> `PreparadorNivel` y comprueban las pruebas (`DisenoNivel3Tests`, `EscenaNivel3Tests`,
> `MecanicasMiedoTests`, `EnemigosMiedoTests`, `EspectroDelPavorTests`, `Nivel3PlayModeTests`,
> `MecanicasMiedoPlayModeTests`, `EnemigosMiedoPlayModeTests`, `EspectroDelPavorPlayModeTests`, y las
> del arte, `ArteMundo3Tests`, `ArteMundo3PlayModeTests`, `PuertasTests` y `CinematicaPavorTests`). Si cambias el recorrido, cámbialo allí y sube su `Version`: la
> escena se vuelve a montar entera.
>
> Los prompts de todas las imágenes están en `Prompts_Mundo3.md`: los enemigos y el jefe (primera
> tanda) y el escenario, las mecánicas, el fondo, las cinemáticas y las puertas (segunda tanda).
>
> Las tablas de abajo ya llevan la ciénaga y las medidas corridas 28 u; las secciones «Lo que cambió
> al montarlo» de cada fase cuentan las medidas de entonces.

![Esquema del recorrido](Nivel3_Diseno.png)

*El esquema se regenera con `python Herramientas/esquema_nivel3.py`. Es un dibujo de diseño, a
escala (1 u = 1 tile), no una captura del juego. Los círculos son la luz de cada antorcha (8 u) y la
de Kael (3 u); las rayas blancas de las copas, las cintas.*

---

## 1. La idea: el miedo paraliza; el nivel enseña a avanzar a oscuras

El Anexo A resume el mundo así: «El miedo paraliza cuando se enfrenta en la oscuridad». El guion
lo dice con un gesto, **ocultarse sin perderse**, y Kael lo cierra con «El Miedo no se fue: dejó
de decidir por mí. Puedo andar en la sombra sin perderme en ella». El eco de memoria («Una
trinchera. Un silencio demasiado largo. Él se quedó quieto para que no lo encontraran») y la
línea del jefe («Él aprendió a desaparecer para seguir vivo. Yo soy todo lo que dejó escondido»)
dan la otra mitad: el soldado sobrevivió escondiéndose. El nivel convierte eso en reglas de
juego, en tres tiempos:

1. **Huir: la linde.** El miedo pide correr, y correr sale caro. Hace ruido, y en la oscuridad
   las trampas solo se ven a 1 u: corriendo no da tiempo a pararse. La luz lo enseña todo, pero
   **despierta lo que dormía**: cada antorcha es una decisión.
2. **Esconderse: el bosque cerrado.** Oscuridad completa. Los Centinelas Ciegos oyen y caminar
   despacio (Ctrl) es silencioso: esconderse funciona. Pero no basta. Los Espectros cazan en la
   oscuridad, y el bosque es un laberinto: el camino que parece fácil acaba en un callejón. Quien
   solo se esconde se pierde; hay que elegir cuándo encender una luz.
3. **Enfrentar: el claro y la arena.** El Acechador, el mini-jefe, caza por el ruido: para
   vencerlo hay que **hacerse oír a propósito**, delante de un árbol. Y el Espectro del Pavor es
   invisible: se le vence acercándose cuando avisa, no huyendo del aviso. En su segunda fase hay
   tres figuras y la verdadera es la que lleva la luz.

Dos piezas más refuerzan el tema:

- **La trinchera.** El primer checkpoint está dentro de una trinchera, donde suena el eco de
  memoria. Es el único sitio seguro del bosque cerrado: dentro no te oyen, pero quedarse no lleva
  a ningún sitio. Salir de ella en silencio es la primera prueba.
- **La luz de Kael.** Kael lleva su propia luz, de 3 u (Anexo A), que no cuenta como «iluminado»
  para los Espectros: con ella ve dónde pisa, no está a salvo.

Esto responde a dos observaciones de los evaluadores, como el Mundo 2. La primera pedía
profundizar en el diseño conceptual: cada pieza sale de la emoción. La quinta preguntaba qué
estrategias de juego sirven al autoconocimiento: aquí la que funciona es avanzar despacio, decidir
cuándo encender y enfrentar a propósito; las que fallan son correr a ciegas (trampas y ruido) y
quedarse quieto (los Espectros llegan).

## 2. Las mecánicas del mundo

Medidas con los parámetros reales de Kael (`ParametrosJugador`). **Buena parte ya existe desde la
Fase 4**, sin nivel donde usarla; lo marco con ✔.

| Mecánica | Qué hace | En el código |
|---|---|---|
| **Oscuridad** | Cada sección tiene su luz: la linde, luz de luna; el bosque cerrado, oscuridad completa; el claro, luna otra vez; la arena, oscuridad total (Anexo A). La oscuridad no es negro puro: las siluetas del fondo se adivinan contra un cielo violeta muy oscuro, para no perder la orientación. | ✔ Fase B. `LuzDelNivel` lleva la luz global de la escena y la cambia según el tramo en que esté Kael, poco a poco (1,5 s). La activa el `LevelManager` si `LevelData.reglas.oscuridad`: en el Editor la escena sigue con luz. Los valores, en `DisenoNivel3.Luces`. |
| **La luz de Kael** | Un círculo de 3 u de radio alrededor de Kael (Anexo A: intensidad 1,5). | ✔ Fase B. `LuzJugador`: una luz 2D hija de Kael, apagada en el prefab; el `LevelManager` la enciende con `reglas.radioLuzJugador`. Pálida y un poco fría. No se registra en `Iluminacion`. |
| **Antorcha** | Se enciende con **E** (Interactuar) a 1,5 u de ella. Ilumina **8 u durante 15 s** (Anexo A) y parpadea los 3 últimos. Mientras arde se ven las trampas, las ramas podridas y los Espectros, y **los Espectros no se acercan a Kael si está dentro de su luz**. Pero al encenderse **despierta a todos los Espectros dormidos en su radio** (Anexo A): esperan en el borde de la luz y, cuando se apaga, van a por él. **Se enciende una vez por vida**: al reaparecer en un checkpoint vuelve a estar lista. | ✔ Fase B. `Antorcha` (prefab en `Assets/Prefabs/Mecanicas`): se registra en `Iluminacion` mientras arde, su círculo sale de la llama (1,5 u sobre el pie) y anuncia que se encendió (`Antorcha.AlEncenderse`). ✔ Fase C: con ese aviso despiertan los Espectros de su luz. E la lee Kael (`InteraccionJugador`), y el HUD pone «E · Encender» sobre la antorcha que tiene al alcance. |
| **Sigilo** | Correr por encima del 60 % de la velocidad (3,6 u/s) hace ruido cada 0,4 s, que se oye a 6 u. **Caminar con Ctrl (2,1 u/s) o agachado (2 u/s) es silencioso.** El dash también hace ruido. Al correr salen de los pies de Kael unos anillos pálidos: el jugador **ve** que hace ruido (son las «partículas de ruido» del Anexo A). | ✔ `PlayerModoMovimiento` (STEALTH, NORMAL y RUN, como pide el Anexo A), que `GameManager` enciende si `LevelData.reglas.sigilo`; caminar ✔ (v0.31; con Ctrl desde la v0.68). ✔ Fase B: los anillos (`AnillosDeRuido`, líneas sin imagen que se ven a oscuras), que crecen hasta donde se oye cada ruido. |
| **Cepo** (trampa oculta) | Un cepo de caza oculto entre las hojas. **Solo se ve a 1 u de Kael** (Anexo A) **o bajo la luz de una antorcha.** Si Kael lo pisa: 1 de daño, **lo sujeta 1 s** (puede atacar, pero no andar ni saltar) y **suena**: el chasquido se oye a 8 u. Después se queda cerrado, inofensivo, hasta reaparecer. Caminando, se ve 0,5 s antes de pisarlo; corriendo, 0,17 s: a oscuras, quien corre cae. | ✔ Fase B. `Cepo` (prefab): no tiene collider, nota a Kael con una caja en el suelo. Su chasquido sale por el mismo canal que la carrera (`PlayerModoMovimiento.HacerRuido`), así que ya lo oyen los Centinelas Ciegos de la Fase 4. Se rearma cuando Kael reaparece tras morir. |
| **Rama podrida** | Las «plataformas que ceden» del Anexo A (`FalsePlatform`). A oscuras es igual que las demás ramas; bajo una antorcha se ve gris y agrietada. Al pisarla cruje 0,3 s y se parte; vuelve a los 4 s. | ✔ Fase B. El prefab `RamaPodrida`: una `PlataformaInestable` con estos tiempos y `RamaPodrida`, que la pinta como las sanas a oscuras, gris bajo una antorcha, y la hace temblar mientras cruje. |
| **La bifurcación** | El laberinto de dos rutas del Anexo A: **arriba, las copas (la buena)**; **abajo, la hondonada (la trampa)**, que acaba en un callejón cubierto de raíces. La buena está marcada con **cintas blancas** atadas a las ramas, que solo se ven con luz (o a 3 u, con la de Kael). Caer de las copas es caer a la hondonada: la trampa es también el castigo de un mal salto. | ✔ La hondonada (fase A). ✔ Las cintas (fase B): cuatro rectángulos blancos que cuelgan de las ramas sanas de las copas, con el material que reacciona a la luz. |
| **Eco de memoria** | En el primer checkpoint, en la trinchera. | ✔ `Checkpoint.ecoDeMemoria` (v0.37). |
| **Checkpoints que curan** | Tres: la trinchera, tras las copas y la antesala. | ✔ `Checkpoint.cura` (v0.44). |

**La Invisibilidad Parcial se gana aquí, no se usa aquí.** Ya está programada (Q, 3 s, 8 s de
espera) ✔ y los enemigos que ven ya la respetan ✔. El Anexo A la estrena en el Jardín (mundo 5);
la propuesta del Mundo 4 la adelanta a la tierra de nadie (`Nivel4_Diseno.md`, PENDIENTES N48).

**El Dash Acuático, que se ganó en el Mundo 2, se estrena aquí**, en la ciénaga de la sección 10.

## 3. El recorrido, tramo a tramo

Medidas en unidades de Unity (1 u = 1 tile). El suelo está a y 0. Son una **propuesta**: al
montarlo, las pruebas de alcance ajustarán los números, como pasó con el Nivel 2 («Lo que cambió
al montarlo»).

| Tramo | x | Suelo | Qué hay | Qué aprende o siente el jugador |
|---|---|---|---|---|
| **Inicio** | −6 a 6 | tierra, y 0 | Kael aparece en x 2. Luz de luna. | **Apertura:** «No veo mis propios pasos. Algo respira entre los árboles… o soy yo». |
| **La primera antorcha** | 6 a 17 | tierra | Antorcha en x 7. A su alrededor, un cepo (x 11) y un Espectro dormido (x 14). Sobre la antorcha, «E». | **El tutorial de la antorcha** (Anexo A). Al encenderla aparecen el cepo y el Espectro, que despierta y espera en el borde de la luz. Lo que enseña la luz lo despierta ella misma. |
| **El barranco** | 17 a 27 | hondonada a y −2,5 | Tres ramas cruzan a ras del suelo (huecos de 1 u); **la del medio está podrida**. En el fondo, un cepo (x 22). Se sale con el doble salto, por cualquiera de los dos lados. | Caer no mata, pero duele y retrasa. La rama podrida se delata con luz: en la linde todavía hay luna para adivinarla. |
| **La segunda antorcha** | 27 a 36 | tierra | Antorcha en x 30; un Espectro dormido en x 33 y el tercer cepo (x 34). | Ya se sabe lo que pasa al encender: ahora es una decisión. Se puede pasar a oscuras, con cuidado. |
| **La trinchera** | 38 a 46 | trinchera a y −2 | **CP1** (x 41) y **eco de memoria**. Empieza la oscuridad completa. | Respirar. Dentro de la trinchera no te oyen. |
| **Salir sin ruido** | 46 a 56 | tierra | El primer **Centinela Ciego** patrulla de x 47 a x 55; un Espectro dormido en x 52. | Salir caminando. Si corres, te oye; si te pegas, te nota. O acercarse por la espalda y golpear primero. |
| **La bifurcación** | 55 a 58 | tierra | Antorcha en x 55. El suelo se acaba en x 58. | Encenderla enseña las cintas de las copas, pero despierta al Espectro de x 52 si no lo ha hecho ya. |
| **Arriba: las copas** (la buena) | 59 a 80 | ramas, y 2 a 4,5 | Se sube por dos ramas (y 2 e y 4), cada una a 1 u de la anterior: todo se cruza caminando. **Una rama podrida** sobre la hondonada (x 65 a 67). **El puesto del vigía**, una plataforma de 6 u a y 4,5 (x 68 a 74) con el segundo **Centinela Ciego**, apoyada en las raíces. Pasado el puesto, otra rama podrida que solo asusta: debajo hay suelo. Se baja por una rama a y 3; debajo, un Espectro. | Pasar junto al vigía sin que te oiga, o cazarlo. Las ramas crujen. |
| **Abajo: la hondonada** (la trampa) | 58 a 73 | hondonada a y −4 | Parece el camino: llana y ancha. El tercer **Centinela Ciego**, dos cepos y dos Espectros dormidos. En x 73 **se cierra**: un muro de raíces de 8 u que sostiene el puesto del vigía. Sus raíces **no agarran** (como el hielo liso del Mundo 2: sin eso, el salto de pared sacaba de ahí). Se vuelve por un escalón en x 58 a 60. | El camino fácil era el falso. Cuesta tiempo (y puntos), no la vida. |
| **Tras las copas** | 74 a 84 | tierra | **CP2** en x 82,5. | Respirar otra vez. |
| **La ciénaga** | 84 a 112 | agua de 3,5 u | Una antorcha en la orilla (x 85) y la pista. Dos túneles bajo las raíces de un árbol hundido, con **dos marañas** (2 y 4 u) que solo cruza el Dash Acuático, y un tocón para respirar entre ellos. Sin enemigos. Detalle en la sección 10. | Quien duda, queda atrapado: el miedo paraliza. Se cruza de golpe. |
| **La antorcha del claro** | 112 a 117 | tierra | Antorcha en x 114. | Ver al Acechador antes de entrar, a costa de despertarlo. |
| **El claro** | 117 a 134 | tierra | El único sitio sin copas: entra la luna. **El Acechador**, el mini-jefe, merodea entre tres árboles gruesos (x 121, 126 y 131). Kael pasa por delante de los árboles; el Acechador, que embiste a ciegas, se estrella contra ellos. | Se puede cruzar caminando, sin que se entere. O vencerlo: correr delante de un árbol para que embista y se estrelle. |
| **La antesala** | 134 a 140 | tierra | **CP3** en x 137. | El respiro antes del jefe. |
| **La arena** | 140 a 167 | tierra | Sala de 25 × 15 u (interior de x 141 a 166), como las otras dos. **Oscuridad total: solo la luz de Kael** (Anexo A). Dos ramas a y 3,5 (x 145 a 148 y 159 a 162), a 7 u del jefe por cada lado, que se alcanzan con el doble salto. Entrada en x 143; salida en x 163,5. | La **presentación** al cruzar la verja. |

**Recuento frente al Anexo A:**

| | Anexo A | Propuesta |
|---|---|---|
| Sección 1 | 0–20 u: luz moderada, tutorial de antorchas, 2 Espectros, 3 trampas ocultas | −6 a 36 u: lo mismo |
| Sección 2 | 20–50 u: oscuridad completa, laberinto con 2 rutas, 3 Centinelas Ciegos, 4 Espectros, plataformas que ceden | 36 a 112 u: lo mismo, más la trinchera y la ciénaga |
| Sección 3 | no existe | 112 a 140 u: el claro con el Acechador, el mini-jefe (no está en el Anexo) |
| Arena | 50–70 u: oscuridad total, solo la luz del jugador | 140 a 167 u: lo mismo, en 25 × 15 u como las otras arenas |
| Longitud | ~70 u | ~173 u desde la ciénaga (antes, ~145 u, como los otros dos ya alargados: 125 y 139 u) |
| Checkpoints | no dice | tres: la trinchera, tras las copas y la antesala |
| Trampas | «invisibles hasta 1 u» | cinco cepos: tres en la linde (Anexo A) y dos en la hondonada |

**El tiempo con bono** es de 16 min: 15 (en el Nivel 1 son 10 y en el Nivel 2, 13), porque aquí se
camina mucho, y uno más por la ciénaga.

### Lo que cambió al montarlo (fase A, 2026-10-14)

- **El barranco mide 2,5 u de hondo, no 3, y no tiene escalón.** Con 3 u y un escalón para
  salir, el escalón quedaba bajo una rama: Kael daba con la cabeza al saltar y no salía. A 2,5 u,
  bajo las ramas quedan 2 u libres (Kael mide 1,8) y se sale con el doble salto pegado a
  cualquiera de las dos paredes, donde no hay rama encima. Caer cuesta un doble salto y el cepo.
- **Las ramas de las copas van a 1 u unas de otras**, y la primera está a 1 u del borde de la
  bifurcación, no encima: una rama encima del borde le cortaba el paso a Kael (su cabeza llega a
  1,8 u) y escondía la bajada a la hondonada.
- **El callejón no tiene techo de raíces.** Lo cierran las raíces, que suben hasta el puesto del
  vigía (8 u sobre el fondo, más que el doble salto) y no agarran. Un techo no añadía nada.
- **El puesto del vigía mide 6 u** (de x 68 a 74) y se apoya en las raíces. Las ramas de la
  bajada están a y 4 y a y 3, para que bajo ellas se ande sin tocarlas.
- **Ninguna rama por la que se pase por debajo deja menos de 2 u**, y lo comprueban las pruebas.
- **Las ramas de la arena se corrieron 1 u hacia el centro** (versión 2 del diseño): la de la
  derecha se montaba sobre la puerta de salida. Ahora quedan a 7 u del jefe por cada lado.

### Lo que cambió al montarlo (fase B, 2026-10-14)

- **La luna del claro empieza en x 89**, donde se abre el claro, y no al empezar la sección 3
  (x 84). La antorcha del claro (x 86) queda a oscuras: por eso sirve para ver al Acechador antes
  de entrar. La antesala comparte la luna del claro: es el respiro antes del jefe.
- **Cuánta luz hay en cada tramo lo he decidido yo**, porque el Anexo A solo dice «moderada»,
  «completa» y «total». La linde tiene 0,55; el bosque cerrado, 0,08; el claro y la antesala,
  0,45; y la arena, 0,03. Están en `DisenoNivel3.Luces`: si al jugarlo no se ve lo que debe, se
  cambian ahí.
- **El cepo no empuja: sujeta.** Quita la vida sin retroceso ni invulnerabilidad, como el ahogo
  del Mundo 2: si lo empujara, el golpe lo sacaría del cepo. Mientras lo sujeta, Kael tampoco sale
  con el dash.
- **Una rama podrida se pisa y se salta en seguida.** Caminando se tarda casi 1 s en cruzarla y
  cruje 0,3 s: hay que apoyarse en ella y saltar ya, sin pararse. Las pruebas de los pasos ya
  comprobaban que la siguiente rama se alcanza caminando.
- **«Una vez por vida» es literal.** La antorcha y el cepo vuelven a estar listos cuando Kael
  reaparece **tras morir**; tocar otro checkpoint no los repone.
- **No hay fotos de la oscuridad.** Al renderizarla en segundo plano, Unity dibuja la luz global,
  pero no las luces de Kael ni las de las antorchas. Hay que verla en el Editor, jugando.

### Lo que cambió al montarlo (fase C, 2026-10-14)

- **El oído va con los anillos de Kael.** El Centinela Ciego oye delante hasta donde llega cada
  ruido (lo que enseñan los anillos: 6 u corriendo, 8 u un cepo) y detrás, la mitad (3 u y 4 u).
  La propuesta decía «8 u delante y 3 u detrás»: con 8 u delante, oiría correr más allá de lo que
  los anillos enseñan, y el jugador no sabría por qué lo ha oído.
- **El Acechador oye el doble de lo que llega cada ruido**, en todas direcciones: 12 u corriendo,
  como pedía la propuesta. Ese sí oye más allá de los anillos: es la bestia del claro.
- **El Centinela va hacia donde oyó, no hacia Kael.** No ve: si Kael sigue allí, o se le pone
  delante por el camino, la estocada le alcanza; si no, apuñala el aire y escucha 2 s. Su
  controlador ya solo nota el roce.
- **El Espectro acude cuando se apaga la luz.** Espera fuera de la luz de la antorcha, a unas 8,5 u
  de la llama, y desde ahí no ve a Kael (ve a 6 u). Por eso, cuando la luz se apaga, va hacia él
  hasta tenerlo a tiro y entonces lo caza su controlador.
- **Los árboles no tienen collider.** El proyecto no tiene una capa que choque solo con los
  enemigos, y Kael tiene que pasar por delante. El Acechador mira sus troncos mientras embiste.
- **El Acechador no sale del claro embistiendo:** si no choca con nada, frena pasado el ruido o en
  el borde del claro (x 89 y 106). Empieza en x 100, entre el segundo árbol y el tercero (el
  esquema lo tenía encima del segundo).
- **Aturdido, el golpe vale el doble:** el segundo punto se le quita justo después del golpe, para
  que su muerte cuente una sola vez.
- **Los datos de la Fase 4 se ponen al día solos.** El aviso del Espectro (0,2 → 0,5 s) y el tacto
  del Centinela se aplican a sus datos si siguen como los dejó el generador; si los has tocado a
  mano, no.

### Lo que cambió tras probarlo (v0.67, 2026-10-24)

- **Más rápido, para que dé miedo** (lo pidió el autor). El tajo y las sombras avisan 0,6 s en vez
  de 1 s; la embestida, también 0,6 s, y cruza a 16 u/s en vez de 11. Las hojas siguen en 1 s. Los
  avisos no bajan de 0,5 s (`VerificadorPavor.AvisoMinimo`): rápido, pero se lee.
- **La embestida busca a Kael.** Antes iba siempre a ras de suelo, y en las ramas de la arena se
  estaba a salvo. Ahora aparece a la altura de lo que pisa Kael (o de lo que tiene debajo, si salta):
  si está en una rama, embiste por ella, en el aire. Se esquiva saltándola o bajándose de la rama.
  Debajo de una rama, Kael pisa el suelo: la embestida va a ras de suelo. Si muere en el aire, cae al
  suelo de su arena, donde quedan sus hojas y su fragmento.

### Lo que cambió al montarlo (fase D, 2026-10-14)

- **Cuándo se le golpea.** Desde que destella hasta que se recupera, los golpes cuentan; mientras
  jadea (tras el tajo y tras las sombras: la ventana), valen el doble. Al otro lado de la
  embestida se queda 1 s a la vista y se le puede golpear, pero sin el doble: la ventana es el
  jadeo.
- **Su dibujo no depende de la luz.** En su arena solo hay la luz de Kael (0,03 de luz global): con
  un dibujo que reacciona a la luz, su destello no se vería desde lejos. Cuando no se deja ver, no
  se ve nada (opacidad 0). Las franjas de sus hojas y el filo de sus tajos, igual.
- **Su presentación, sin cinemática.** Antes del combate espera visible en el centro de la arena;
  al empezar se desvanece y tarda 1,2 s en atacar. La cinemática de presentación llegó en la fase F.
- **La embestida sale de la pared más lejana a Kael**, para cruzar por donde está.
- **Las hojas:** aparece en el lado de la arena contrario a Kael; las franjas miden 2 u, la
  primera cae bajo Kael y entre dos franjas siempre queda al menos 1 u libre.
- **Las sombras:** las figuras salen a 3 u de cada pared y en el centro, y atacan de izquierda a
  derecha; cuál es la verdadera se elige al azar. Golpear a la verdadera las deshace a todas; si no,
  se deshacen al acabar de jadear.
- **El grito:** la luz de Kael parpadea 0,9 s y la oscuridad late (dos latidos por segundo).
- **La oscuridad retrocede** hasta 0,85 de luz global, con la luz de la luna, en 3 s, y ya no
  vuelve a cambiar por tramos: el resto del nivel se ve entero. Si al jugarlo es demasiado o
  demasiado poco, está en el prefab (`luzAlMorir`).

### Lo que cambió al montarlo (fase E, 2026-10-15)

- **Lo que llegó mal y se decidió contigo:** R1-R3 del Espectro miraban al otro lado (se voltean);
  su D3 y Hoja2 llegaron a otra escala (no se usan); y ocho fotogramas del Pavor traían una copia
  borrosa al lado (se borra). El detalle, en `Tesis/Imagenes/limpios/Mundo3/LEEME.md`.
- **Los gestos del Pavor:** cada momento de sus ataques tiene su dibujo (`GESTOS_PAVOR`): el aviso
  del tajo con la espada en alto, el tajo, el jadeo, el aviso de la embestida agazapado, la
  embestida, el frenazo, el brazo en alto de las hojas y la columna de hojas; aparecer en remolino
  (sus sombras) y deshacerse al empezar el combate. Sus sombras hacen los mismos gestos.
- **Las hojas se ven caer:** durante el aviso brilla el suelo entre las hojas muertas; al hacer
  daño caen cinco hojas por franja y se rompen contra el suelo.
- **Al caer, el Pavor deja el fragmento** del Miedo sobre su montón de hojas, como la Centinela y
  la Doncella.
- **El fondo se oscurece con la luz del nivel** (`FondoConLaLuz`): sus capas no dependen de la luz
  y, sin esto, el bosque del fondo se vería de día en el bosque cerrado y en la arena.
- **La rama podrida** se ve sana a oscuras, podrida con luz y al crujir, y al partirse se dobla y
  se deshace en astillas (su caída dura 0,4 s; vuelve igual, a los 4 s).
- **Los tamaños y el atrezo los decidí yo** (PENDIENTES N47): el Centinela Ciego mide 2 u, el
  Acechador 1,6 × 3,2, el Pavor 3,45, la antorcha 1,7 (la cabeza a la altura de su luz) y el cepo
  0,95; los árboles del claro son el tocón del árbol muerto, al doble; y el atrezo
  (`DisenoNivel3.Decorados`) cuenta la trinchera del soldado.

### Lo que cambió al montarlo (fase F, 2026-10-16)

- **Lo que llegó mal y se decidió contigo:** el primer plano de abajo traía un cielo de nubes encima
  de los helechos (se borra); y en `DerrotaEspectro2` el jefe sigue de pie a la derecha y la gema de
  Kael es verde (se montó así y se repitió el 2026-10-17).
- **La salida, en x 135,5 y no en 136.** La puerta del árbol mide 4 u de alto, como las de los otros
  mundos, y 4,7 de ancho, porque sus raíces se abren: en 136 se metía 0,3 u en la pared de la arena.
  En 135,5 cabe entre la rama de la derecha (acaba en 134) y la pared (138).
- **La verja** mide 4 u y se repite hasta el techo, como la de la Ira; **el checkpoint**, una
  lámpara de trinchera colgada de una estaca, 3,3 u.
- **Los helechos** son la séptima capa del fondo, delante de la niebla, y se oscurecen con la luz
  como las demás. Van anclados al suelo del bosque (sus puntas, a 1,5 u del suelo): siguiendo a la
  cámara, en las copas se veían en el aire.
- **La presentación del Pavor** (9,8 s): el claro a oscuras, con Kael en su luz y la máscara
  mirándolo, y él de cerca. Su línea del guion va repartida entre los dos planos. De cerca ocupa el
  centro y su espada la derecha: su frase y su rótulo van arriba a la izquierda, sobre el negro, uno
  detrás de otro. **La derrota**, sin texto: se deshace en hojas y el bosque se ve con luz.
- **La salida del Miedo**: el claro con la puerta abierta y Kael ante ella, con las tres frases del
  cierre abajo, sobre lo oscuro. La segunda tiene 80 letras: dura 2,7 s en vez de 2,6.
- **El Espectro, al tamaño y al ritmo de los Ecos** (lo pedía este diseño, y el autor lo recordó el
  2026-10-16): escala 1,8 × 1,5 y ataque a 1,6× con 0,45 s de descanso, como los Lamentos. Su aviso
  pasa de 0,5 a 0,31 s: con su fundido se le ve entero 0,1 s antes del golpe.

### Lo que cambió al jugarlo (2026-10-17)

El autor lo jugó: «el nivel por el momento se siente bien» y las decisiones del arte «están bien».
Lo único:

- **El recuadro verde** que se veía en el aviso y en el golpe del Pavor era su zona de daño, que se
  pintaba como en la fase D (sin arte era su aviso). Con su arte lo enseñan sus gestos y el suelo
  que brilla de las hojas: ya no se pinta.
- **La gema que deja al caer brilla más:** encima del fragmento dibujado (azul oscuro, sobre el
  montón de hojas) va una copia de sus mismos píxeles que recibe luz; una luz casi blanca, lavanda,
  que late (de 2,5 a 6 cada 1,4 s, 3,2 u de radio) la enciende y alumbra el suelo de alrededor; y
  su silueta blanca destella con cada latido. Al recogerla se apaga.
- **`DerrotaEspectro2`, repetida:** el bosque con luz, sin el jefe, y Kael con la gema violeta.

### Cómo se ve hoy

Fotos en juego con el arte de las fases E y F, con la luz del nivel encendida del todo (jugando,
casi todo está a oscuras). Las de las cinemáticas están en `Docs/Cinematicas/Pavor_*.png` y
`Miedo_Salida_*.png`.

![La trinchera: el checkpoint, los sacos, el refugio y la caja](Nivel3_ConArte_Trinchera.png)

![El puesto del vigía, sobre las copas: las ramas, las cintas, la lámpara y la lona](Nivel3_ConArte_Vigia.png)

![El Centinela Ciego va hacia el ruido](Nivel3_ConArte_Centinela.png)

![El Acechador en el claro, entre los árboles](Nivel3_ConArte_Acechador.png)

![El Espectro del Pavor avisa del tajo](Nivel3_ConArte_Pavor.png)

![Las hojas del Pavor caen sobre el suelo que brilla](Nivel3_ConArte_Hojas.png)

![La verja de raíces cierra la arena](Nivel3_ConArte_Verja.png)

![La puerta del árbol, abierta tras recoger el fragmento](Nivel3_ConArte_Salida.png)

Fotos de la escena montada en la fase A: bloques de color y cápsulas, con luz y sin Kael. Así
se ve en el Editor; jugando, la oscuridad de la fase B la cubre. Los bloques están más claros
que la paleta del Anexo A, que es casi negra, para que se lean con la luz de la luna y la de Kael.

![El Nivel 3 entero](Nivel3_Montado.png)

![La linde](Nivel3_Montado_Seccion1.png)

![El bosque cerrado: la trinchera, la bifurcación, las copas y la hondonada](Nivel3_Montado_Seccion2.png)

![El claro, la antesala y la arena](Nivel3_Montado_Arena.png)

## 4. Medidas de Kael, para que todo se pueda cruzar

| | Caminando (Ctrl) | Corriendo |
|---|---|---|
| Velocidad | 2,1 u/s, en silencio | 6 u/s, con ruido |
| Salto | 2,4 u de alto; avanza ~1,5 u | 2,4 u; avanza 4,4 u |
| Doble salto | 4,1 u de alto; avanza ~2,5 u | 4,1 u; avanza más de 6 u |
| Un cepo aparece a 1 u | 0,5 s para frenar | 0,17 s: no da tiempo |

- **Ningún hueco obliga a correr.** Los de las copas miden 1 u y se cruzan caminando.
  Quien corre llega antes (y, con el doble salto, se puede saltar la rama podrida), pero se oye.
  `DisenoNivel3Tests` comprueba cada salto con la velocidad de caminar.
- **Lo más alto** son las ramas de la arena (3,5 u): solo con el doble salto, como la capa de hielo
  de la Doncella. Las pruebas usarán el mismo margen que en el Nivel 2 (el 90 % de lo que salta
  Kael).
- **La luz de Kael (3 u)** llega a las ramas de y 4 desde el suelo: se ve dónde está la siguiente
  rama, aunque no si está podrida.

## 5. Los enemigos

| Enemigo | Cuántos | Cómo se comporta | Qué enseña | En el código |
|---|---|---|---|---|
| **Espectro** (el Eco del Miedo) | 6 (2 en la linde y 4 en el bosque cerrado, como el Anexo A) | **Es el Eco de este mundo:** el mismo cuerpo que el Eco de la Ira y el Lamento, el verde que se deshace en polvo negro de `Referencias/Arte/EnemigosNiveles.jpeg`. **Duerme** hasta que una antorcha lo despierta, hasta que Kael pasa a 1,5 u o hasta que le pegan. Despierto, **solo se acerca si Kael está a oscuras**: dentro de la luz de una antorcha no se acerca. **Casi invisible:** a oscuras no se ve; dentro de la luz de Kael se adivina una silueta (una cuarta parte de opaco) con su polvo negro; **cuando ataca, sus ojos se encienden y se ve entero**, y también bajo una antorcha. Rápido (persigue a 3,8 u/s) y frágil (2 de vida, quita 1). A la escala de los Ecos (1,8 × 1,5) y con su ritmo de ataque. | El miedo crece en la incertidumbre y se acerca cuando no miras. | ✔ Sus datos (`Espectro.asset`) y su detección (`DeteccionEnOscuridad`), desde la Fase 4. ✔ Fase C: `Espectro` (en su prefab) lleva el sueño, la espera en el borde de la luz y la casi invisibilidad, y su golpe avisa 0,5 s (antes, 0,2). Usará el controlador de animación del Eco, como el Lamento. ✔ Fase E: con su arte y el controlador del Eco; desde la v0.53, a la escala de los Ecos (`DisenoNivel3.EscalaEspectro`) y a su ritmo de ataque (1,6×: avisa 0,31 s y descansa 0,45). |
| **Centinela Ciego** | 3, en el bosque cerrado (Anexo A) | Un **centinela de trinchera con una máscara de gas de cristales ciegos**, abrigo largo y fusil con bayoneta. *El Anexo A solo lo nombra; el aspecto es propuesta mía, por la trinchera del eco de memoria.* Patrulla despacio (1 u/s). **No ve: oye** el ruido de Kael en un cono, hasta 8 u delante y 3 u detrás (el «cono de detección sonora» del Anexo), y el chasquido de un cepo. Al oír se detiene, ladea la cabeza (0,6 s: el aviso), va hacia donde oyó (4 u/s) y lanza una estocada (2 de daño, 1,4 u de alcance). Si en 2 s no encuentra a nadie, vuelve a su ronda. Si Kael lo roza (1,2 u), lo nota aunque no haga ruido. 5 de vida. **La Invisibilidad no sirve contra él**, porque no ve. | El silencio protege. Y acercarse en silencio deja golpear primero. | ✔ Sus datos (`CentinelaCiego.asset`), desde la Fase 4; el ruido del cepo, desde la fase B. ✔ Fase C: `CentinelaCiego` (en su prefab) lleva el cono, el aviso, ir hacia el ruido y la estocada. Su controlador ya solo nota el roce (`DeteccionPorTacto`, 1,2 u). |
| **El Acechador** (mini-jefe) | 1, en el claro | **No está en el Anexo A**: es el mini-jefe del mundo, como el Golem y la Plañidera. Una **bestia de sombra, como un lobo enorme sin ojos**, que se deshace en polvo negro como los Ecos del Miedo: es lo que «respira entre los árboles» en la línea de apertura. 10 de vida e hiperarmadura, como el Golem. Merodea despacio, olfateando. **Oye como el Centinela, pero más lejos (12 u).** Al oír se agazapa y gruñe (0,7 s: el aviso) y **embiste en línea recta** hacia donde oyó el ruido, a 10 u/s, hasta chocar: **contra un árbol, se queda aturdido 2,5 s y los golpes valen el doble** (como la espada clavada de la Centinela); contra Kael, quita 2; si no choca con nada, frena y vuelve a escuchar. Si Kael se le pega (1,5 u), echa la cabeza atrás (0,5 s) y muerde (quita 1). | **Hacerse oír a propósito.** Es el único sitio del juego donde correr es la estrategia: el miedo se enfrenta, no solo se esquiva. | ✔ Fase C. `Acechador.asset` (10 de vida, hiperarmadura, muerde con `DeteccionPorTacto`) y el componente `Acechador`, que apaga el controlador mientras se agazapa, embiste, está aturdido o frena. Los árboles (`Arbol`) no tienen collider: Kael pasa por delante y el Acechador los nota al embestir. |

Los puntos (`TablaPuntaje`): el Espectro y el Centinela Ciego, 150 ✔; el Acechador, 700 ✔ (entre
la Plañidera, 600, y el jefe); el Espectro del Pavor, 2500 ✔.

## 6. El jefe: el Espectro del Pavor

**Concepto:** `Referencias/Arte/Jefes/jefe3.jpeg`. Una figura alta con una armadura de escamas
negras como hojas o plumas, vetas de un verde pálido que brilla, una máscara rajada que brilla
por dentro, una gema blanca en el pecho, hojas negras que flotan a su alrededor y una espada
curva. Funciona tal cual: a oscuras solo se verían sus vetas y su máscara, que es justo lo que
pide el combate. El prompt de AI Studio está en `Prompts_Mundo3.md`, apartado B (con un paso
opcional para pedir tres variantes, como con la Doncella).

**Sus datos** (`Assets/Data/Jefes/EspectroDelPavor.asset`) ✔: 22 de vida y dos fases, la
segunda al 50 %, con sus patrones desde la fase D. Tocarlo no hace daño: lo hacen sus ataques.

**Cómo pelea.** Casi todo el tiempo es **invisible e intangible**: ningún golpe le alcanza (Anexo
A: «invisible el resto del tiempo»). Se ve, y se le puede golpear, desde que avisa hasta que se
recupera de su ataque. **Cada ataque avisa con su destello**: se encienden su máscara, sus vetas y
el filo de la espada. El Anexo A pedía 1 s; desde la v0.67 el tajo, las sombras y la embestida
avisan **0,6 s** (más rápido, para que dé miedo) y las hojas siguen en 1 s. En la arena no hay más
luz que la de Kael: el destello se ve desde cualquier sitio.

| Fase | Patrón | Qué pasa | Cómo se responde |
|---|---|---|---|
| 1 · El acecho: tajo, embestida, tajo y hojas | **Tajo** | Aparece a 2,5 u de Kael, a su espalda (o delante, si Kael está contra una pared), avisa 0,6 s y lanza un tajo de 3 u de alcance (quita 2). Después **se queda 1,5 s jadeando, visible**. | Alejarse del filo durante el aviso y volver a por él: es **la ventana**. |
| | **Embestida** | Aparece junto a una pared **a la altura de Kael**: a ras de suelo o, si Kael está en una rama, a la de esa rama, en el aire. Avisa 0,6 s y cruza la arena (1,6 u de alto, a 16 u/s). Quita 1. Al otro lado se queda visible 1 s. | Saltarla. Si viene por la rama, también bajarse de ella: ya no hay sitio seguro. |
| | **Hojas** | Alza el brazo y avisa 1 s; en cuatro o cinco franjas el suelo brilla en verde, y en ellas caen hojas negras afiladas. Quitan 1. | Salir de lo que brilla. Como las erupciones de la Centinela. |
| 2 · Las tres sombras (50 %): sombras, hojas, sombras y embestida | **Grito** | Al empezar la fase la oscuridad late y la luz de Kael parpadea 0,9 s. | La señal de que el combate cambia, como el rugido de la Centinela. |
| | **Sombras** | Aparecen **tres figuras** (a la izquierda, en el centro y a la derecha) y lanzan su tajo una tras otra, cada una con su aviso. Después se quedan las tres 1,5 s jadeando. **Solo la verdadera lleva una luciérnaga dorada** que le da vueltas; las sombras, verdes (la «partícula de luz distinta» del Anexo A). Golpear a la verdadera cuenta y deshace a las sombras. **Golpear a una sombra la deshace en hojas y encoge la luz de Kael a la mitad (1,5 u) durante 3 s.** | Seguir la luz. En el miedo, la figura verdadera es la que tiene luz. |
| Muerte | — | **La oscuridad retrocede** (Anexo A): la luz sube en 3 s y el bosque se ve entero por primera vez. Después, la cinemática de la derrota y el fragmento. | **Cierre:** «El bosque no era un laberinto: era un lugar donde nadie había encendido una luz». |

**En el código** ✔ (fase D): `EspectroDelPavor` lleva lo suyo (aparecer, desvanecerse, ser
intangible: `SaludJefe.Intangible`) y los ataques son datos, como los de la Centinela y la Doncella:
`PatronTajo`, `PatronEmbestidaSombra`, `PatronHojas` y `PatronSombras`. Las sombras
(`SombraDelPavor`) son figuras sin vida que se deshacen al golpearlas. `VerificadorPavor` comprueba,
al regenerar, que cada fase tiene su ventana y que su destello se ve a oscuras: sin eso no se le
podría vencer. La luz de Kael (`LuzJugador`) sabe encogerse y parpadear, y la del nivel
(`LuzDelNivel`), latir y retroceder.

**Por qué así:** su línea dice «Él aprendió a desaparecer para seguir vivo». Por eso es invisible,
y por eso se le vence **acercándose cuando avisa**: el destello que asusta es el momento de ir a
por él. En la segunda fase el miedo se multiplica en sombras, y la verdadera es la que tiene luz.
Es el «dejó de decidir por mí» del cierre, hecho combate.

## 7. Qué hay que implementar, por fases

Como en el Mundo 2: una rama por fase, pruebas en verde, el CHANGELOG y PENDIENTES al día, y
parar si algo no cuadra. **Las fases A a D no necesitan arte:** se hacen con bloques de color y
cápsulas, sin sprites inventados.

| Fase | Qué | Piezas |
|---|---|---|
| **A · Recorrido** ✔ | La escena `Nivel3`, montada desde una tabla de medidas, todavía con luz | `DisenoNivel3` y `PreparadorNivel` para el Miedo; el `LevelData` con su escena, sus secciones y sus checkpoints; pruebas de alcance (caminando y corriendo) |
| **B · Mecánicas** ✔ | La oscuridad, la luz de Kael, las antorchas, el ruido, los cepos, las ramas podridas y las cintas | Luces 2D por sección y en Kael (`ReglasNivel.oscuridad`); `Antorcha` sobre `ZonaIluminada`; los anillos de ruido; `Cepo`; `RamaPodrida`; las cintas; el eco de memoria en la trinchera |
| **C · Enemigos** ✔ | El Espectro, el Centinela Ciego y el Acechador | El sueño y la casi invisibilidad del Espectro; el cono y la estocada del Centinela; `Acechador`, con su embestida y los árboles |
| **D · Jefe** ✔ | El Espectro del Pavor completo, sin arte | Aparecer e intangibilidad en `EspectroDelPavor`; `PatronTajo`, `PatronEmbestidaSombra`, `PatronHojas` y `PatronSombras`; la arena a oscuras; un verificador |
| **E · Arte** ✔ | Vestir lo anterior a medida que llegue | Una sub-fase por paquete, como en el Mundo 2 |
| **F · Narrativa y sonido** ✔ (sin sonido: no hay audio) | Presentación, derrota y cierre con planos; las puertas | El sistema de cinemáticas, sin cambios; la puerta, la verja y el checkpoint de este mundo |

## 8. Las imágenes que necesitamos

### Reglas para todas

Las del Mundo 2 (`Prompts_Mundo2.md`, apartado 0), con lo que aprendimos allí:

- **Un PNG por fotograma**, sobre **magenta plano (#FF00FF)**. Nunca el damero gris.
- **Cada pieza tiene una maestra**, y los demás fotogramas se piden **editándola**, adjuntando
  siempre la maestra y nunca el fotograma anterior.
- **Cada maestra tiene todo lo que no puede faltar** (los ojos, el hueco del pecho…), y cada
  prompt lo enumera. Lo que la maestra no tiene, sus ediciones tampoco lo tendrán.
- **Que no cambie el tamaño del píxel ni del lienzo.** Con el ataque del Lamento, Gemini devolvió
  lienzos de 2390 × 1760 y cada fotograma con su píxel. Los prompts ya lo piden expresamente;
  aun así, mira cada imagen antes de pedir la siguiente.
- **Margen alrededor de la figura.** A la Doncella el lienzo le cortaba la cola.
- **Yo no reescalo ni recorto** (tu regla): ajusto el tamaño en el juego con los píxeles por
  unidad.
- **La paleta del Anexo A:** negro profundo `#0A0A0F`, violeta oscuro `#2D1B4E`, verde pantanoso
  `#1A2E1A` y gris espectral `#3A3A4A`, más un verde pálido (`#7FE6B0`) para lo que brilla: las
  vetas del jefe, los ojos de los Espectros al atacar y el aliento del Acechador. **El único color
  cálido del mundo es la luz de las antorchas**: lo cálido es lo que se enciende.

### P1 · Los enemigos (prompts listos: `Prompts_Mundo3.md`, apartado A)

| # | Imagen | Fotogramas | Alto en el juego | Notas |
|---|---|---|---|---|
| 1 | **Espectro** | los 12 del Eco: Idle 2, Correr 4, Ataque 4, Muerte 2 | como el Eco | **Editando cada fotograma del Eco**, como el Lamento. Las bases ya están en `Tesis/Imagenes/Mundo3/Espectro/Bases/`. Al atacar, sus ojos brillan en verde pálido; al morir se deshace en polvo negro. |
| 2 | **Centinela Ciego** | Andar 4, Escuchar 2, Estocada 3 (aviso, estocada, recuperarse), Daño 1, Muerte 3 (13) | ~2,3 u | La máscara de gas con los cristales ciegos tiene que leerse en todos. |
| 3 | **El Acechador** (mini-jefe) | Merodear 4, Escuchar 1, Aviso 2, Embestida 2, Aturdido 2, Mordisco 2, Muerte 4 (17) | ~2,2 u de alto y ~3,6 de largo | Sin ojos, con el aliento verde pálido y la grupa deshaciéndose en polvo. |

### P1 · El jefe (prompts listos: `Prompts_Mundo3.md`, apartados B y C)

| # | Imagen | Fotogramas | Tamaño en el juego | Notas |
|---|---|---|---|---|
| 4 | **Espectro del Pavor** | Reposo 4, Aparecer 4 (desvanecerse es el mismo al revés), Tajo 4, Embestida 3, Hojas 3, Grito 3, Muerte 5 (26) | ~3,4 u, como la Centinela | Por Google AI Studio, desde `jefe3.jpeg`. |
| 5 | **Hoja que cae** | 3: cae, gira, se rompe contra el suelo | ~0,5 u | Editando la maestra del jefe, para que tenga su píxel. |
| 6 | **Aviso de las hojas** | 2 | franja de ~2 u | El suelo que brilla en verde. |
| 7 | **Luciérnaga** | 2 | ~0,25 u | Una sola: el juego la tiñe de dorado (la verdadera) o de verde (las sombras). |
| 8 | **Fragmento del Miedo** | 1 | ~0,6 u | Editando el de la Tristeza, para que los fragmentos casen. |

### Segunda tanda, cuando apruebes el diseño

| # | Imagen | Notas |
|---|---|---|
| 9 | **Tileset del bosque** | Tierra con raíces, ramas (con sus extremos), trinchera con tablones, raíces del callejón. Editando la hoja del Mundo 2 con la misma distribución, como se hizo desde la del Mundo 1. |
| 10 | **Antorcha** | Apagada, encendida (3 en bucle) y consumida. |
| 11 | **Cepo** | Abierto entre hojas, cerrado. |
| 12 | **Rama y rama podrida** | Sana, podrida (se ve con luz) y partiéndose. |
| 13 | **Cinta blanca** | 1–2, atadas a una rama. |
| 14 | **El fondo** | Cielo violeta con luna, árboles lejanos, árboles cercanos, un árbol enorme como hito (como la fortaleza y la torre), niebla y ramas en primer plano. |
| 15 | **Cinemáticas** | Presentación (2 planos) y derrota (2), a 480 × 268 como las del Mundo 2. |
| 16 | **Las puertas** | Puerta de salida, verja de la arena, checkpoint y la cinemática de salida, como las de los otros dos mundos. |
| 17 | **Kael caminando a escondidas** | Opcional, en Pixelorama: hoy, al caminar, usa el clip de correr más despacio. |

### Lo que no necesita imagen

- **La oscuridad y las luces:** son luces 2D.
- **La invisibilidad del Espectro y del jefe:** el juego baja la opacidad.
- **Los anillos de ruido:** se dibujan en el juego, como los corazones del HUD.

### El sonido (no es imagen, pero hace falta)

Casi silencio, roto por ruidos del bosque (Anexo A). Un golpe de cuerdas al encender una antorcha.
El chasquido del cepo. El jefe: percusión irregular y frecuencias bajas.

**La música ✔ (2026-10-23), con los sonidos de LMMS** (`Musica/LEEME.md`):

- **El nivel** (`Nivel3_Miedo`): 192 s en bucle, casi silencio (a -30 dB). Un zumbido grave que va y viene,
  viento a ratos y, cada 8 a 16 s, un ruido del bosque: una rama, un crujido, un aliento, una campana muy
  lejos, un carillón torcido, un golpe sordo. Al azar, pero siempre el mismo azar, para que no se repita en
  tres minutos.
- **La antorcha** (`Golpes/Antorcha`): al encenderla, un golpe de cuerdas en Do# menor que se queda en la
  reverb (3 s). Lo toca la `Antorcha`.
- **El Espectro** (`Nivel3_Pavor`): Do# menor, 105 BPM, 73 s. Un latido; toms en grupos de 3 y 2 que no
  caen donde se espera; cuerdas en trémolo; racimos de cuerdas graves; y los toms se aceleran.
- **Sus cinemáticas**: la presentación (un zumbido, un aliento, un latido que se acelera; con su rostro, un
  racimo de cuerdas), la derrota (el golpe grave; luego cuerdas en Re mayor y el tema de Kael) y el cierre
  (el tema de Kael en las cuerdas).
- Falta el chasquido del cepo.

### Orden recomendado

1. **El Espectro (1):** es el enemigo que más se ve y sale de las bases del Eco.
2. **El Espectro del Pavor y lo suyo (4 a 8):** sin él, el combate son cápsulas.
3. **El Centinela Ciego y el Acechador (2 y 3).**
4. **La segunda tanda.**

Nada de esto bloquea las fases A a D: el nivel se puede montar y jugar mientras dibujas.

## 9. Decisiones (respondidas el 2026-10-14: «haz lo que recomiendas»)

Todas aprobadas tal como las recomendé:

1. **Las antorchas se encienden una vez por vida** y vuelven a estar listas al reaparecer.
   *Recomendado:* si se pudieran volver a encender, quedarse junto a una sería estar a salvo para
   siempre, y encenderla dejaría de ser una decisión.
2. **Los Espectros duermen** hasta que una antorcha los despierta, Kael pasa a 1,5 u o les pega;
   despiertos, solo se acercan a oscuras y solo se ven del todo al atacar. *Recomendado:* si solo
   los despertara la antorcha, no encenderla sería siempre lo mejor.
3. **El cepo** sujeta 1 s, quita 1 y se oye a 8 u, y solo se ve a 1 u (Anexo A) o con luz.
   *Recomendado.*
4. **La ruta falsa del laberinto acaba en un callejón** (la hondonada) y hay que volver. Es lo que
   dice el Anexo A («una correcta, una trampa»). *Recomendado.* La otra opción es un rodeo más
   largo y peligroso que también llega.
5. **El mini-jefe, el Acechador**, en el claro (no está en el Anexo A, como la Plañidera). Se
   puede cruzar el claro sin pelear, caminando. *Recomendado:* los otros dos mundos tienen
   mini-jefe y el puntaje lo premia.
6. **El Centinela Ciego es un centinela con una máscara de gas de cristales ciegos.** El Anexo A
   solo lo nombra. *Recomendado:* une el bosque con la trinchera del eco de memoria y se distingue
   bien del Espectro y del Acechador.
7. **El Espectro del Pavor es el de `jefe3.jpeg`**, sin rediseñar. *Recomendado:* ya tiene la
   máscara y las vetas que se ven a oscuras. Si prefieres otro aspecto, el paso B1 pide tres
   variantes.
8. **Golpear a una sombra encoge la luz de Kael 3 s.** El Anexo A no dice qué pasa. *Recomendado:*
   sin castigo, se golpearía a las tres sin mirar.
9. **El nivel mide unos 145 u, con tres secciones, la arena y tres checkpoints**, como los otros
   dos alargados, y 15 min de tiempo con bono. *Recomendado.*

Lo que difiere del Anexo A va a N19, para corregirlo en la tesis, como con los otros dos mundos:
el nivel mide unos 145 u y no 70, con tres secciones y la arena (el Anexo dice dos y la arena);
tiene tres checkpoints (el Anexo no dice ninguno) y el primero en una trinchera; la arena mide
25 × 15 u como las otras; el Acechador, el mini-jefe, no está en el Anexo; los Espectros
despiertan también con el roce o un golpe, no solo con la luz; las trampas son cepos que
sujetan y hacen ruido; y golpear a una sombra del jefe encoge la luz de Kael.

## 10. La ciénaga, donde hace falta el Dash Acuático (2026-10-19)

> **Estado:** aprobada el 2026-10-19 («por el momento lo apruebo; espero verificaciones del otro
> tester»), con su arte, y montada (`DisenoNivel3.Version` 9). Las medidas de abajo son las montadas;
> lo que cambió frente a la propuesta está al final de esta sección.

Lo pediste el 2026-10-19: «apliquemos las mecánicas que Kael ha ido obteniendo, como el dash
acuático: añádele una parte de agua en la que sea necesario este dash, ya que por algo lo ha ido
aprendiendo». Hasta ahora el Dash Acuático se ganaba en el Mundo 2 y no se usaba hasta el Jardín.
Con esto se estrena aquí.

![La ciénaga, de cerca](Nivel3_Cienaga.png)

*El esquema se regenera con `python Herramientas/esquema_cienaga.py`. Es un dibujo de diseño, a
escala, con las medidas montadas.*

![Kael, sujeto por la primera maraña (foto en juego, con la luz del nivel encendida)](Nivel3_ConArte_Cienaga.png)

### La idea: el miedo paraliza a quien duda

El Anexo A: «El miedo paraliza cuando se enfrenta en la oscuridad». En la ciénaga eso es literal.
Bajo el agua negra hay **marañas de raíces que atrapan a quien avanza despacio**: nadando, Kael
casi no se mueve dentro de ellas, y el aire se acaba. **Solo se cruzan de golpe, con el dash.** Lo
que Kael aprendió en la Tristeza (moverse bajo el agua) es lo que le saca de la parálisis del
Miedo. Es el «dejó de decidir por mí» del cierre, hecho movimiento: dudar es quedarse atrapado;
decidir es pasar.

### Dónde: entre el CP2 y el claro

Tras bajar de las copas (CP2, x 82,5), el bosque se hunde en una ciénaga. El árbol más grande del
bosque cayó sobre ella, y sus raíces llegan al fondo: no se pasa por encima, solo por debajo.
**Todo lo que viene después (la antorcha del claro, el claro, la antesala y la arena) se corrió
28 u a la derecha.**

| Tramo | x | Qué hay | Qué aprende o siente el jugador |
|---|---|---|---|
| **La orilla** | 84 a 86 | Suelo a y 0. **La pista** (x 84) y **una antorcha** (x 85), la quinta. | La antorcha deja ver la poza y la primera maraña antes de meterse. Aquí no hay Espectros que despertar: es solo luz. |
| **El primer túnel** | 86 a 96 | Agua de 3,5 u (fondo a y −3,5): una poza (86 a 89) y un túnel bajo las raíces (89 a 95; techo a y −1, 2,5 u de alto). **Maraña 1**, de 91 a 93 (2 u). | **El tutorial:** un dash la cruza entera, con el cuerpo. |
| **El tocón** | 96 a 98 | Un tocón a ras del agua. Se sube desde el fondo con el doble salto. | Respirar: fuera del agua, el aire se llena en 1 s. |
| **El segundo túnel** | 98 a 110 | Una poza (98 a 100), el túnel (100 a 107) y la salida (107 a 110). **Maraña 2**, de 101 a 105 (4 u). Se sale a la orilla con el doble salto. | **El examen:** dos dashes seguidos. Entre uno y otro (0,6 s), la maraña lo sujeta. |
| **La otra orilla** | 110 a 112 | Suelo a y 0. Sigue la antorcha del claro (x 114, antes 86). | Salir del agua y seguir a oscuras. |

### Las mecánicas

| Mecánica | Qué hace | En el código |
|---|---|---|
| **El agua de la ciénaga** | La del Mundo 2: velocidad ×0,6, salto ×0,6 y gravedad ×0,3 (desde el fondo se sube 2,9 u, y 5 con el doble salto); 10 s de aire y después un corazón cada 2 s. Más oscura: el velo del HUD, verde pantanoso en vez de azul. | ✔ `ZonaModificadorMovimiento` (agua) y `OxigenoJugador`, que está en el prefab de Kael desde el Mundo 2 y ya funciona en cualquier nivel; la barra de aire, en `IndicadorAgua`. ✔ El velo verde, por nivel (`ReglasNivel.colorVeloAgua`). |
| **El Dash Acuático** | Bajo el agua el dash funciona si se tiene: se gana al terminar el Mundo 2. Recorre 2,9 u en 0,18 s sin que el agua lo frene, y vuelve a estar listo 0,6 s después. Como en el aire, uno por salto: se recarga al tocar el fondo o una pared. | ✔ Desde la Fase 3 (`PlayerController.QuiereDash`). |
| **La maraña de raíces** | Raíces sumergidas que llenan el túnel del fondo al techo. Dentro, Kael va al 5 % de su velocidad (0,18 u/s) y no salta: lo sujetan, y se cierran sobre él. **El dash no lo frena nada**, así que la cruza. No quita vida: quita aire. | ✔ Casi entera: es una `ZonaModificadorMovimiento` personalizada, el mismo componente del agua y del hielo. ✔ Su dibujo, que se mece y se cierra mientras sujeta: `Marana`. |
| **La pista** | «Raíces bajo el agua. Si dudo entre ellas, me atrapan. Tengo que cruzarlas de golpe: con el Dash Acuático.» Y otra vez si una maraña lo sujeta 2 s sin haberla oído. | ✔ `ZonaDeDialogo` y `MOMENTOS_DIALOGO.PISTA`, como el hielo del Mundo 2. |

### Los números: por qué hace falta el dash y por qué sobra aire

| | Con el dash | Sin el dash |
|---|---|---|
| Maraña 1 (2 u, y el cuerpo de Kael, 0,8) | un dash: 0,18 s | 16 s, a 0,18 u/s |
| Maraña 2 (4 u, y el cuerpo) | dos dashes y 0,6 s sujeto: 1 s | 27 s |
| Cada tramo bajo el agua | unos 3 a 4 s de los 10 de aire | más de 10: se ahoga |

- **Sin el Dash Acuático no se cruza:** el aire se acaba dentro de la primera maraña. Al Nivel 3
  solo se llega con él, porque se gana al terminar el 2.
- **Con él sobra aire.** Quien duda dentro de una maraña pierde aire, no vida, salvo que dude
  mucho. La ciénaga asusta; no castiga.
- **Ningún hueco del agua necesita el dash para saltarse:** solo las marañas. Caer al agua desde
  la orilla no hace daño, y si Kael se ahoga, vuelve al CP2, a 3,5 u del agua.

### Lo que cambió en lo ya montado

- **Todo lo que había desde x 84 se corrió 28 u:** la antorcha del claro (86 → 114); el claro (89 a
  106 → 117 a 134), con sus árboles (121, 126 y 131) y el Acechador (128); la antesala (106 a 112
  → 134 a 140) y el CP3 (109 → 137); y la arena (112 a 139 → 140 a 167), con la entrada (143), el
  jefe (153,5), la salida (163,5) y sus ramas (145 a 148 y 159 a 162).
- **La ciénaga es parte del bosque cerrado:** su oscuridad completa llega hasta x 117, y la luna
  del claro empieza allí. La sección 2 va de 36 a 112; la 3, de 112 a 140.
- **El nivel mide unos 173 u** (de −6 a 167) y el tiempo con bono pasa de 15 a 16 min.
- **Las pruebas de alcance** (`DisenoNivel3Tests`) comprueban el agua: que desde el fondo se sube al
  tocón y a las orillas (3,5 u) con el doble salto bajo el agua, y con impulso para salir; que bajo
  el techo de los túneles cabe Kael y que no se saltan por arriba; y que cada maraña se cruza con
  uno y con dos dashes, y no sin ellos. `EscenaNivel3Tests` comprueba que la escena lo sigue, y
  `Nivel3PlayModeTests`, jugándolo, que la maraña sujeta y el dash la cruza (y sin el Dash Acuático,
  no).
- `DisenoNivel3.Version` subió a 8 (la ciénaga) y a 9 (el interior de sus túneles): la escena se
  volvió a montar entera.

### Las imágenes que necesita (tercera tanda: `Prompts_Mundo3.md`, apartado K)

| # | Imagen | Fotogramas | Notas |
|---|---|---|---|
| 18 | **El agua de la ciénaga** ✔ | 3, en bucle | La tira de la superficie, como el agua del Mundo 2 (su A3), en verde pantanoso, con lentejas de agua. El cuerpo del agua es un color del juego. |
| 19 | **La maraña de raíces** ✔ | 4: quieta (2, se mece) y cerrándose (2) | 2 × 2 u, colgada del techo; la de 4 u son dos seguidas, la segunda en espejo. |

**Lo demás ya existía:** las raíces del árbol hundido son las del callejón (el tileset del bosque) y
el tocón y el fondo, la tierra del tileset.

### Decisiones (aprobadas el 2026-10-19, PENDIENTES N50)

1. **La ciénaga va entre el CP2 y el claro** y alarga el nivel 28 u (unos 173 u, 16 min con bono).
   *Recomendado:* es el único sitio donde cabe sin tocar lo que ya funciona. Va justo después de un
   checkpoint (quien se ahoga vuelve a 3,5 u del agua) y antes del claro, que necesita su luna.
2. **Las marañas sujetan, no hieren:** dentro, Kael va al 5 % y no salta, y solo el dash las cruza.
   La primera, de un dash; la segunda, de dos. *Recomendado:* la parálisis es el tema del mundo, y
   el aire hace de reloj sin que haga falta otro castigo.
3. **Dos túneles, con un tocón para respirar entre ellos.** *Recomendado:* el primero enseña y el
   segundo examina; en uno solo, el primer error ya ahogaría.
4. **Una antorcha en la orilla, sin Espectros cerca.** *Recomendado:* deja ver qué es una maraña
   antes de meterse en el agua, que bajo la luz de Kael (3 u) se vería tarde.
5. **Sin enemigos en el agua.** *Recomendado:* aquí el miedo es el agua. Un Espectro, además del aire
   y de las marañas, sería demasiado para la primera vez que se usa el dash bajo el agua.
6. **El texto de la pista** (arriba). Lo escribí yo, en la voz de Kael; si quieres otro, dímelo.
7. **El agua con su propio dibujo** (3 imágenes) y la maraña (4), y el velo del agua en verde.
   *Recomendado:* el agua helada del Mundo 2 no es una ciénaga. *La otra opción:* el agua del Mundo 2
   teñida de verde, sin pedir nada; la maraña sí hay que pedirla.

Lo que difiere del Anexo A irá a N19, como lo demás: el Dash Acuático se estrena en el Mundo 3 (el
Anexo lo deja para el Jardín), y el Mundo 3 tiene agua, aire y marañas.

### Lo que cambió al montarla (2026-10-19)

- **El agua mide 3,5 u de hondo, no 4.** Al cruzar la superficie, la gravedad vuelve a ser la de
  fuera: con 4 u, lo que sobra del doble salto bajo el agua subía a Kael 0,23 u por encima de la
  orilla, y las pruebas piden 0,25, como en el Mundo 2. Con 3,5, le sobran 0,38. El techo de los
  túneles baja con el fondo (a y −1): siguen teniendo 2,5 u.
- **El tocón está a ras del agua y mide 2 u**, como el último rellano de la torre del Mundo 2: encima,
  Kael queda fuera del agua y respira. Los bloques caen en unidades enteras, como los demás.
- **Las marañas miden 2 y 4 u, no 2,5 y 5.** Para salir de una, Kael tiene que cruzarla y sacar el
  cuerpo (0,8 u): la de 2,5 habría pedido dos dashes, y la de 5, tres. Así, la primera se cruza con
  uno y la segunda con dos, como se aprobó. Su dibujo, el mismo, va a 37,5 píxeles por unidad (2 ×
  2 u: su píxel queda más cerca del de las ramas) y **cuelga del techo** del túnel, como está
  dibujado: las puntas de las raíces abajo, a medio metro del fondo. Por debajo no se pasa: la zona
  que sujeta llega al fondo.
- **Todo se corrió 28 u, no 27**, porque el tocón y las pozas crecieron hasta unidades enteras.
- **El Espectro de la bajada duerme en x 76, no en 78.** La antorcha de la orilla lo alcanzaba (7 u
  desde su llama) y lo habría despertado: la decisión 4 era «sin Espectros cerca». Ahora está a 9 u.
- **El agua de la ciénaga mide 179 píxeles de ancho, no 180.** A `Agua3` le faltaba la última columna
  en la mitad de abajo, y al repetirse habría dejado una raya: las tres se cortaron a las columnas que
  tienen todas (lo detalla `Tesis/Imagenes/limpios/Mundo3/LEEME.md`).
- **El velo bajo el agua es verde** en este nivel (`ReglasNivel.colorVeloAgua`): en los demás sigue
  el azul de la Tristeza.
- **Dentro de los túneles se ve el interior del árbol hundido**, no el bosque del fondo: sus raíces
  (las del callejón), oscurecidas. En las fotos, a través del agua se veía el bosque, y la maraña,
  raíces oscuras sobre raíces oscuras, se confundía con él. Contra el interior oscuro se lee.
