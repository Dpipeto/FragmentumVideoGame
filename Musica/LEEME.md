# La música de Fragmentum

Los proyectos de LMMS (1.2.2) de la música del juego, hechos solo con los sonidos que trae LMMS. Se
abren y se editan en LMMS; el juego usa el OGG que sale de cada uno, en `Assets/Audio/Musica`.

Hay dos clases de proyecto, y se distinguen por la barra de bucle de arriba del Song Editor:

- **Bucles** (la barra encendida): la música de un nivel y la de su jefe. Se repiten sin costura.
- **Piezas que suenan una vez** (la barra apagada): la de cada cinemática, que dura justo lo que ella, y
  los golpes (la antorcha). Acaban en la marca de fin de la barra.

## El menú

| Proyecto | Para | Cómo es | Dura |
|---|---|---|---|
| `Menu/Menu.mmp` | El menú inicial y el selector de niveles (la misma en los dos) | El tema de Kael, tranquilo, en bucle. Re menor, 60 BPM: un colchón y notas sueltas; el tema en el piano; en las cuerdas, con el piano en arpegios; un puente; y la cabeza del tema, que se apaga hacia el principio. | 96 s |

## Los niveles y sus jefes

Todos en bucle. El tema de la trompeta de la marcha vuelve en el órgano del Juez.

| Proyecto | Para | Cómo es (Anexo A) | Dura |
|---|---|---|---|
| `Nivel1/Nivel1_Ira.mmp` | El Nivel 1 | Percusión industrial y un sinte distorsionado, sin melodía. Mi frigio, 160 BPM: golpes de metal; la caja y un bajo ácido; un respiro; todo, con golpes de sinte; el bajo sube a Fa. | 96 s |
| `Nivel1/Nivel1_Centinela.mmp` | La Centinela Ardiente | Lo mismo, más pesado: doble bombo, un riff de acordes de quinta y un yunque a contratiempo. Mi frigio, 168 BPM. | 68,6 s |
| `Nivel2/Nivel2_Tristeza.mmp` | El Nivel 2 | Una caja de música lenta, torcida (como una cinta vieja) y bajo el agua, y un piano con una reverb enorme. Si menor, 70 BPM: la nana en la caja de música; en el piano, con cuerdas; la caja de música sola, cada vez con menos notas. | 82,3 s |
| `Nivel2/Nivel2_Doncella.mmp` | La Doncella del Lamento | Un vals (3/4): el lamento en las cuerdas sobre el piano y el coro; en la caja de música, con timbales; arriba, cuerdas y coro. Si menor, 140 BPM. | 61,7 s |
| `Nivel3/Nivel3_Miedo.mmp` | El Nivel 3 | Casi silencio: un zumbido grave que va y viene, viento a ratos y, cada 8 a 16 s, un ruido del bosque (una rama, un crujido, un aliento, una campana lejana, un carillón torcido, un golpe sordo). Al azar, pero siempre el mismo azar: no se repite en 3 minutos. Suena a -30 dB. | 192 s |
| `Nivel3/Nivel3_Pavor.mmp` | El Espectro del Pavor | Percusión irregular y graves: un latido; toms en grupos de 3 y 2 que no caen donde se espera; cuerdas en trémolo; racimos de cuerdas graves; y los toms se aceleran hasta el principio. Do# menor, 105 BPM. | 73,1 s |
| `Nivel4/Nivel4_Marcha.mmp` | El Nivel 4 | Marcha militar lenta y distorsionada. Re menor, 64 BPM: tambores y campana; el tema en la trompeta; con una segunda trompeta; un eco lejano que vuelve a crecer. | 120 s |
| `Nivel4/Nivel4_Juez.mmp` | El Juez Silente | Órgano de iglesia y bajo profundo, sobre un bajo que baja de Re a La: el órgano a golpes; el tema de la marcha en el órgano; arriba, con el coro; el pedal solo. Re menor, 96 BPM. | 80 s |

## Las cinemáticas (`Cinematicas/`)

Cada una suena una vez y dura lo que su cinemática; sus golpes caen donde cambia el plano.

**El tema de Kael** (Re menor, ocho compases) es el tema principal: lo presenta la cinemática inicial
y vuelve en cada cierre de mundo, con los instrumentos de ese mundo. Es el que el Anexo A pide para
el final («versión distorsionada del tema principal», y al acabar «un piano simple»).

| Proyecto | La cinemática | Cómo es | Dura |
|---|---|---|---|
| `Inicial.mmp` | La inicial | «Hubo una guerra»: tambores lejanos. El tema de Kael en el piano, que se corta en seco con «Le sacaron todo lo que pesaba». Vuelve un latido («lo que negó no desapareció»); las cuatro emociones toman forma, una a una (un golpe de metal, la caja de música, un aliento, una campana); crece; y el tema, agudo y frágil, para «alguien nuevo», hasta el título. Acaba en una pregunta: la cabeza del tema, sin resolver. | 80 s |
| `Jefe_<mundo>.mmp` | La presentación del jefe | Crece sobre el primer plano y golpea con el del jefe; acaba subiendo, porque detrás entra el combate (el tema del jefe empieza de golpe). La del Juez baja Re, Do, Si bemol, La y resuelve en el primer compás de su tema. | 9,6-9,8 s |
| `Derrota_<mundo>.mmp` | La derrota | El golpe, que se apaga; en el segundo plano asoma la cabeza del tema de Kael. | 9 s |
| `Cierre_<mundo>.mmp` | El cierre del mundo | El tema de Kael: en el piano (Ira), la caja de música (Tristeza), las cuerdas (Miedo) o la trompeta de la marcha (Culpa). Cuando Kael integra el fragmento, resuelve en Re mayor. | 14 s |

Los mundos: `Ira`, `Tristeza`, `Miedo` y `Culpa`. **Las memorias de la Culpa no llevan música**: el
Anexo A pide silencio. Las del Jardín y el final esperan a que existan esos niveles.

**Si una cinemática cambia de duración, su pieza ya no cuadra**: hay que cambiarla y volver a
exportarla (`MusicaTests` lo comprueba).

## Los golpes (`Golpes/`)

| Proyecto | Cuándo | Dura |
|---|---|---|
| `Antorcha.mmp` | Al encender una antorcha (Anexo A: «stinger de cuerdas»): un golpe de cuerdas en Do# menor que se queda en la reverb. | 3 s |

## Para cambiar algo

1. Ábrelo en LMMS, cámbialo y guárdalo con el mismo nombre (vale también como `.mmpz`).
2. Pásalo al juego:

   ```
   python Herramientas/exportar_musica.py               # todos
   python Herramientas/exportar_musica.py Nivel4_Juez   # uno
   ```

3. Unity lo importa solo; el nivel y la cinemática ya lo tienen puesto.

Cada pista de LMMS es un instrumento («Trompeta», «Caja», «Organo tema»…) y va a un canal del
mezclador (F9), donde está su volumen y sus efectos (la distorsión, el filtro «bajo el agua», la reverb).

**No vuelvas a ejecutar `componer_*.py --forzar`**: esos scripts son la partitura en Python con la que se
escribieron los proyectos, y con `--forzar` borran lo que se haya cambiado en LMMS. Sin `--forzar` no tocan
un proyecto que ya exista.

| Script | Escribe |
|---|---|
| `Herramientas/componer_nivel1.py` | El Nivel 1 y la Centinela |
| `Herramientas/componer_nivel2.py` | El Nivel 2 y la Doncella |
| `Herramientas/componer_nivel3.py` | El Nivel 3, el Pavor y la antorcha |
| `Herramientas/componer_nivel4.py` | El Nivel 4 y el Juez |
| `Herramientas/componer_cinematicas.py` | Las 13 cinemáticas (y su mezcla, en `MEZCLA`) |
| `Herramientas/componer_menu.py` | El menú y el selector |
| `Herramientas/musica_lmms.py` | Lo común: cómo se escribe un proyecto de LMMS, los instrumentos medidos |

## Cómo sale el OGG (`Herramientas/exportar_musica.py`)

- **Un bucle**: LMMS lo toca **dos veces seguidas** y se guarda la segunda vuelta: así su principio lleva
  la cola de la reverb y de las notas del final, como cuando el juego lo repite. La reverb de LMMS no
  suena igual dos veces (mueve sus ecos al azar): el último segundo **se funde** con el de la primera
  vuelta, que es el que de verdad va antes del principio. Lo seco es igual en las dos.
- **Una pieza**: se corta en la marca de fin y se apaga en sus últimos 0,3 s.
- Se **normaliza**: los bucles a un RMS de -18 dB y las piezas a -20 (sin pasar de -1 dB de pico), salvo
  el Miedo, a -30 (`NIVELES`). En el juego, la música del nivel y la de las cinemáticas suenan a 0,6.
- El codificador de OGG de LMMS tira el último bloque si queda a medias: se codifica con un poco de
  más y se corta el OGG en su sitio, con su número de muestras justo (`recortar_ogg`). Unity lo respeta.
- Si LMMS exporta muestras rotas (le pasa a veces al empezar), se para en vez de llevarlas al juego.
- Lo único que no es exacto: para codificarlo, LMMS lo toca como una muestra y lo adelanta media muestra
  (igual en todo el bucle, así que la costura sigue siendo continua). Las diez primeras muestras
  (0,2 ms) no ven lo que sonaba antes: un error de unos -35 dB que cae en el golpe del compás 1.
- LMMS se llama con una copia de su configuración: con la del usuario, su lista de proyectos recientes
  se llenaría de proyectos temporales.

## En el juego

- La del menú suena en el menú inicial y en el selector, sin cortarse al pasar de uno a otro (`MusicaMenu`, que vive
  con los sistemas); al entrar en un nivel se apaga.
- La música del nivel suena en bucle (`LevelManager`, `MusicaDelNivel`).
- **Calla en todas las cinemáticas** (se apaga en 0,6 s) y al volver sigue donde iba. Las cinemáticas
  traen la suya (`CutsceneData.musica`, por `PantallaCinematica`), y las memorias, ninguna.
- En la arena cambia a la del jefe, y de vuelta al terminar: la que suena se apaga y la otra empieza
  desde el principio. Al acabar la presentación del jefe, su tema entra de golpe.
- Los bucles se leen del disco mientras suenan; las piezas cortas se cargan antes, para sonar a la vez
  que su imagen (`MusicaNiveles`).
- **Las opciones** (en el menú y en la pausa) tienen el volumen general (todo lo que suena), el de la
  música (la del menú, la de los niveles y la de las cinemáticas), sobre el 0,6 de cada una, y el de los
  efectos (los de Kael, `Sonidos/LEEME.md`, y el golpe de la antorcha) (`AjustesAudio`, `PantallaOpciones`).
  Se guardan en `fragmentum_ajustes.json`, aparte de la partida.

## Las muestras de LMMS que se usan

Están en `C:\Program Files\LMMS\data\samples`. La nota en que está grabada cada una se midió en su
espectro, para que suene en la nota escrita (`musica_lmms.py`, «Instrumentos medidos»):

| Muestra | Nota | Para |
|---|---|---|
| `instruments/piano02.ogg` | Do 3 (suena 8 s) | El piano: el tema de Kael, la Tristeza |
| `instruments/piano01.ogg` | Do 4 (corto) | El piano agudo |
| `instruments/trumpet01.ogg` | La 4 (trae un zumbido de 50 Hz, que se le quita) | La trompeta de la marcha |
| `stringsnpads/strings01.ogg` | Do 4 | Las cuerdas |
| `stringsnpads/chorus02.ogg` | La 3 | El coro |
| `instruments/church_organ01.ogg` / `02` | La 3 / La 2 | El órgano y el pedal |
| `misc/dong02.ogg`, `dong03.ogg` | Re 3, Re 5 | La campana, el yunque |
| `misc/metalish_dong01.ogg` | Do 4 | El metal de la Ira |
| `drums/tom_low01.ogg`, `tom_mid01`, `tom_hi01` | Sol 2, La 2, Re# 3 | Los timbales y los toms |
| `drums/…`, `misc/hit01`, `effects/explode01`, `misc/breath01`, `effects/wind_chimes01` | — | Percusión, golpes, el aliento, el carillón |

La caja de música, la tuba, los bajos, los colchones, el viento y la lengüeta del órgano son
sintetizadores de LMMS (TripleOscillator, LB302 y Organic).
