# Nivel 1 · Ira · Fortaleza Volcánica: el recorrido

> **Estado:** montado en `Assets/Scenes/Nivel1.unity` (2026-10-07), con las tres decisiones de la
> sección 6 aprobadas. Los números de abajo son los de `Assets/Scripts/Editor/DisenoNivel1.cs`,
> que monta `PreparadorNivel` y comprueban las pruebas (`DisenoNivel1Tests`, `EscenasNivelTests`).
> Si cambias el recorrido, cámbialo allí: la escena se rehace desde esa tabla.

![Esquema del recorrido](Nivel1_Diseno.png)

*El esquema se regenera con `python Herramientas/esquema_nivel1.py`. Es un dibujo de diseño, a
escala (1 u = 1 tile), no una captura del juego.*

---

## 1. La idea: la Ira empuja, el nivel enseña a contenerla

El Anexo A resume el mundo así: «La ira no controlada destruye el entorno y al que la porta». El
nivel lo convierte en una regla de juego:

- **Todo invita a correr:**
  - las plataformas inestables se rompen si te quedas;
  - la lava mata;
  - los Ecos atacan rápido.
- **Correr sin mirar castiga:**
  - la plataforma de combustión quema justo cuando llegas con prisa;
  - la bola de la torreta te alcanza si saltas a destiempo;
  - el Golem no se interrumpe con golpes (hiperarmadura): cambiarle golpe por golpe es perder;
  - la Centinela solo se puede castigar cuando su espada queda clavada.
- **Ganar es esperar el momento y soltar el golpe entonces.** La recompensa del mundo dice lo mismo
  con una mecánica: el **Ataque Cargado** se mantiene y se suelta cuando uno decide. La ira no
  desaparece: se canaliza.

Esto responde a la primera observación de los evaluadores («profundizar en el diseño conceptual de
los niveles»): cada decisión del recorrido sale de la emoción, no del gusto.

## 2. Cómo enseña: tres tiempos por mecánica

Cada peligro aparece tres veces, y cada vez cuesta más:

1. **Primero, sin riesgo.** Si fallas, solo pierdes tiempo.
2. **Después, sobre lava.** Si fallas, mueres y vuelves al checkpoint.
3. **Por último, junto a enemigos.** Hay que pensar dos cosas a la vez.

No hay carteles de tutorial. El nivel enseña con su forma, como pide el Anexo A («tutorial
implícito»).

## 3. El recorrido, tramo a tramo

Medidas en unidades de Unity (1 u = 1 tile). El suelo sube de y 0 a y 5 hacia la fortaleza. La
arena va de y 5 a y 20: 15 u de alto, como pide el Anexo A.

| Tramo | x | Suelo | Qué hay | Qué aprende o siente el jugador |
|---|---|---|---|---|
| **Inicio** | −6 a 12 | y 0 | Kael aparece en x 2. Un escalón de 1 u entre x 4 y x 6. | **Apertura:** Kael habla al aparecer. Primer salto, sin peligro. |
| **Primer Eco** | 7,5 a 11,2 | y 0 | Un Eco de la Ira en x 10, en llano. | A atacar: el Eco va a por ti y no hay nada más en pantalla. |
| **El foso** | 12 a 17 | y −2 | Un foso de 5 u y solo 2 de hondo, cruzado por una plataforma inestable (x 14,5) a ras del suelo. | Que la plataforma se rompe. Si caes, sales por la izquierda con un salto o por la derecha con doble salto. Asusta, pero no mata. |
| **La terraza** | 17 a 27 | y 2 | Ecos en x 18,8 y x 21. La torreta en x 23,4, mirando a la izquierda. Un Eco de guardia en x 26, al borde del lago. | Saltar la bola mientras peleas. La torreta dispara a la altura de Kael; su bloque se salta o se pisa. |
| **Lago de combustión** | 27 a 36 | lava | Dos plataformas de combustión (x 29,43 y x 33,57) desfasadas medio ciclo. | **Esperar:** cuando la primera empieza a calentarse, la segunda se apaga. Cruzar con prisa quema. |
| **La isla** | 36 a 39 | y 2 | Un Eco en x 37,5. | Pelear en poco sitio. |
| **La subida** | 39 a 42 | lava | Una plataforma inestable a y 3,5, entre la isla y la fortaleza. | Lo del foso, ahora sobre lava. No pararse. |
| **Checkpoint** | 43 | y 5 | Bandera, sin enemigos cerca. | **Eco de memoria** (cuando haya memorias, Fase 10). Respiro. |
| **El patio del Golem** | 42 a 52 | y 5 | Un Eco en x 45,5, el Golem de Ceniza en x 48,5 y otro Eco en x 51. | El Golem no se frena a golpes. Hay que leer su ataque, saltar su onda y castigar después. |
| **El salto a la puerta** | 52 a 55 | lava | Un hueco de 3 u. | El último salto antes del jefe. Se cruza con un salto normal; con el dash sobra. |
| **La arena** | 55 a 81 | y 5 | Sala cerrada de 25 × 15 u. Puerta en x 55-56; entrada en x 58. Plataformas finas a tres alturas: y 8 (x 59-63 y x 73-77), y 11 (x 65-70), y 14 (x 61-64 y x 72-75). La Centinela en x 70. Salida en x 79,5. | La **presentación** al cruzar la entrada. Subirse a una plataforma esquiva el salto aplastante y las ondas; las erupciones brotan también en ellas y obligan a bajar. |

**Recuento frente al Anexo A:**

| | Anexo A | Montado |
|---|---|---|
| Sección 1 | 3 soldados de lava y 1 torreta | 3 Ecos de la Ira y 1 torreta |
| Sección 2 | 4 soldados, el Golem y checkpoint en x 40 | 4 Ecos, el Golem y checkpoint en x 43 |
| Arena | sala de 15 u de alto, 3 alturas de plataformas | 25 × 15 u, 3 alturas de plataformas |

### Lo que cambió al montarlo

El esquema de la propuesta dibujaba las plataformas más estrechas de lo que son. Con el arte real,
la de combustión mide 3,42 u (no 1,8) y la inestable, 3,83 (no 1,6). Por eso:

- **El lago crece de 6 a 9 u,** para que quepan las dos plataformas de combustión con un hueco de
  0,7 u entre ellas.
- **La subida lleva una plataforma inestable, no dos:** dos escalonadas no caben en el hueco.
- **El checkpoint queda en x 43** (en la propuesta, 41), en el primer tramo firme después de la
  subida y antes del Golem.
- **Todos los bordes caen en unidades enteras:** el tileset pinta celdas enteras, y un bloque que
  acaba a media unidad se dibujaría medio tile fuera de su colisión.
- **La Centinela atraviesa las plataformas de la arena.** Mide 3,4 u y las más bajas empiezan a 3 u
  del suelo: chocando con ellas se quedaría atascada debajo. Su salto cae siempre al suelo, y por
  eso subirse a una plataforma es esquivarlo. Las erupciones sí brotan bajo los pies de Kael,
  también en una plataforma.
- **La arena no lleva decoración** (braseros, cadenas…): el combate se lee mejor sin nada detrás.
- **Los Ecos llevan la escala que les diste a mano** (1,8 × 1,5) y están de pie en su suelo.

### La sección 3, «la forja» (2026-10-13)

Bien jugados, los dos niveles se pasaban en diez minutos, y con el puntaje por tiempo pediste
alargarlos. Entre el Golem y la arena hay ahora **45 u más** (el nivel mide 126 u): la forja, el
examen de lo aprendido. Cada mecánica sale por tercera vez, ya junta con las otras:

| Tramo | x | Qué hay |
|---|---|---|
| Rellano | 55-60 | Se llega con el salto de la vieja puerta de la arena (3 u). **Checkpoint en x 57,5**: morir en la forja ya no obliga a repetir el Golem. |
| Escalera | 60-64 | Una inestable sobre lava, entre el rellano (y 5) y la almena (y 7): se sube pisándola, sin pararse. |
| Almena | 64-75 | Dos Ecos y, al fondo, **la segunda torreta**, que dispara a lo largo de la almena: se pelea esquivando el fuego. Detrás de la torreta quedan 2 u para mirar el lago antes de saltar. |
| Lago de la forja | 75-87 | 12 u de lava con **una inestable entre dos de combustión**. Sobre la inestable no se puede esperar (se rompe al segundo), así que se espera en la primera de combustión mirando la segunda: con medio ciclo de desfase, la segunda se apaga 0,9 s antes de que la primera empiece a quemar. |
| Cornisa | 87-92 | Un Eco más. |
| Antesala | 92-97 | **Checkpoint en x 94,5**, ante la arena: morir ante la Centinela ya no obliga a repetir el nivel. |
| Puerta de la arena | 97-100 | El mismo salto de 3 u sobre lava que antes llevaba a la arena. |

La arena entera se corrió 45 u (va de 101 a 126) y no cambia por dentro. `DisenoNivel1.Version`
(2) hace que la escena se vuelva a montar entera, como la del Nivel 2. Las secciones del
`LevelData` son ahora cuatro: el Anexo A preveía dos y la arena (N19).

### Lo que encontró el segundo tester (versión 3 del diseño, 2026-10-25)

- **Desde la plataforma baja se le pegaba a la Centinela sin que ella alcanzara.** Las dos más
  bajas de la arena estaban a 3 u del suelo, y la Centinela mide 3,4 u: su cabeza asomaba 0,4 u
  por encima. La espada de Kael baja hasta 0,25 u bajo sus pies, así que desde la plataforma le
  daba en la cabeza; ninguno de sus ataques llega a esa altura (el espadazo mide 2,6 u, y las
  ondas y el salto van por el suelo). **Ahora están a 3,5 u** (de y 8 a 8,5): desde ninguna
  plataforma se le alcanza, y siguen sirviendo para esquivar el salto y las ondas. Se suben con el
  doble salto, como antes (desde el suelo, 3,5 u de las 4,15 que da; hasta la del medio, 2,5). Lo
  comprueba `DisenoNivel1Tests`.
- **Las plataformas se atraviesan saltando desde abajo** (las de la arena, las inestables y las
  de combustión) y se pisan al caer encima, como siempre (`PlataformaDeUnSentido`). Kael no las
  pisa mientras las atraviesa: no recupera los saltos en el aire, y la inestable solo se agrieta
  cuando cae encima. El suelo sigue siendo sólido.

`DisenoNivel1.Version` (3) vuelve a montar la escena entera.

## 4. Medidas de Kael, para que todo se pueda cruzar

Estos valores salen de `ParametrosJugador` (los comprueba `DisenoNivel1Tests`):

- **Salto:** sube unas 2,45 u.
- **Doble salto:** suma 1,7 u, unas 4,15 en total.
- **Distancia de un salto corriendo:** unas 4,4 u.
- **Dash:** unas 3 u más.

El hueco más largo del nivel mide 3 u (el salto a la puerta) y se cruza sin dash. Ninguna subida
pide más de 3 u de una vez, ni siquiera las de la arena (de y 5 a y 8 y de y 11 a y 14). Las
pruebas dejan siempre un margen: lo que se pide no llega al 85 % de lo que Kael alcanza.

## 5. Dónde suena cada diálogo

| Momento | Dónde | Estado |
|---|---|---|
| Apertura | al aparecer (x 2) | ✔ con el texto del guion |
| Eco de memoria | en el checkpoint (x 43) | El texto ya está; suena con las memorias (Fase 10) |
| Antes del jefe | al cruzar la entrada de la arena (x 58) | ✔ la presentación «El que ya estaba aquí» |
| Derrota del jefe | cuando la Centinela termina de caer | ✔ los planos `CinematicaFinalLvl1` y `Lvl2` |
| Cierre | al recoger el fragmento, antes del fin de nivel | ✔ con el texto del guion |

## 6. Lo que no cuadraba en el Anexo A (decidido)

1. **El tamaño de la arena:** 25 × 15 u. El Anexo A dice «sala cerrada de 60 u × 15 u», pero también
   que va de x 55 a x 80, es decir, 25 u.
2. **«Lava en el suelo» de la arena:** suelo firme. La Centinela pelea en el suelo (espadazo, onda y
   salto) y con lava caería en ella; la lava brota en las erupciones.
3. **El checkpoint:** en x 43, no en x 40. Ver «Lo que cambió al montarlo».

Conviene corregir estas tres cosas en el Anexo A de la tesis, para que el documento y el juego
digan lo mismo (PENDIENTES).
