# Los efectos de sonido de Fragmentum

Los proyectos de LMMS (1.2.2) de los efectos de sonido del juego, hechos solo con los sonidos que trae
LMMS. Se abren y se editan en LMMS; el juego usa el WAV que sale de cada uno, en `Assets/Audio/Sonidos`
(en la misma carpeta: `Sonidos/Kael/Salto.mmp` → `Assets/Audio/Sonidos/Kael/Salto.wav`).

La música está aparte, en `Musica/` (su `LEEME.md`).

## Kael (`Kael/`)

| Proyecto | Cuándo suena | Cómo es | Dura |
|---|---|---|---|
| `Salto.mmp` | Al saltar, desde el suelo o desde la pared | El pie que empuja (un golpe grave, Kicker) y el aire, que sube (ruido por un filtro que se abre). | 0,23 s |
| `DobleSalto.mmp` | El segundo salto, en el aire | Sin pie: el aire más alto, otra capa que aletea detrás y un brillo que sube (sfxr, un seno). | 0,21 s |
| `Dash.mmp` | El dash | Un arranque grave y una ráfaga de aire que crece y se va, con su cuerpo grave. | 0,34 s |
| `Ataque.mmp` | El tajo de la espada | Un silbido de aire, rápido, y el filo, que baja (sfxr, un seno). | 0,17 s |
| `AtaqueCargado.mmp` | El tajo cargado (al soltar la carga) | Más lento y más grave: con un golpe sordo donde corta, el filo y el metal de la hoja. | 0,32 s |
| `Danio.mmp` | Un golpe que le quita vida (el que para el escudo, no) | Un impacto, un crujido (sfxr, ruido) y un quejido que cae (sfxr, una cuadrada apagada). | 0,24 s |

En el juego, cada vez suenan hasta un 5 % más agudos o más graves, para que repetidos no suenen a
máquina (`SonidosKael`).

## Para cambiar algo

1. Ábrelo en LMMS, cámbialo y guárdalo con el mismo nombre (vale también como `.mmpz`). Cada pista es una
   capa del efecto («Impulso», «Aire», «Filo»…): su volumen, en la pista; lo que hace, en su instrumento.
2. Pásalo al juego:

   ```
   python Herramientas/exportar_sonidos.py          # todos
   python Herramientas/exportar_sonidos.py Salto    # uno
   ```

3. Unity lo importa solo; Kael ya lo tiene puesto.

Cada efecto mide un compás a 125 BPM (1,92 s; un tick son 10 ms) y suena desde el principio: el
exportador lo corta donde se apaga. Tiene que durar menos de 2 s.

**No vuelvas a ejecutar `componer_sonidos_kael.py --forzar`**: es la partitura en Python con la que se
escribieron los proyectos, y con `--forzar` borra lo que se haya cambiado en LMMS.

## Cómo sale el WAV (`Herramientas/exportar_sonidos.py`)

- LMMS lo toca hasta la marca de fin, y se corta donde se apaga (a -60 dB de su pico), con un fundido de
  10 ms.
- En mono y a 44,1 kHz, en 16 bits: como pide PENDIENTES («SFX: WAV mono, 44,1 kHz, menos de 2 s»).
- Se normaliza por lo más fuerte del efecto (el RMS en 50 ms), sin pasar de -1 dB de pico. No todos igual
  (`NIVELES`): el salto, que se oye a cada rato, a -17 dB; el dash y el tajo, a -14; el cargado, a -12; y
  el daño, que tiene que oírse siempre, a -11.
- El ruido de LMMS es al azar: cada vez que se exporta sale un poco distinto (igual de fuerte y del mismo
  color).

## En el juego

- `SonidosKael` (en el prefab del jugador) escucha a la FSM de Kael y a su vida: suena al entrar en JUMP,
  WALL_JUMP, DOUBLE_JUMP, DASH y ATTACK, y al recibir un golpe. Pueden sonar cuatro a la vez.
- Se importan descomprimidos y cargados antes (`SonidosDelJuego`): un efecto tiene que sonar en el mismo
  frame que su acción.
- Su volumen, a 0,8, y sobre él el de **«Efectos»** de las opciones (`AjustesAudio.Efectos`), que también
  baja el golpe de cuerdas de la antorcha.

## Los instrumentos de LMMS que se usan

- **sfxr** (el generador de efectos de los juegos retro): `musica_lmms.sfxr()`. Suena entero mientras
  dure la nota; en la nota 57 suena tal cual. Sus perillas en segundos y hercios: `sfxr_segundos()` y
  `sfxr_hz()` (medido: 0,3 suena a 324 Hz).
- **TripleOscillator** con ruido, por el filtro de la pista (BandPass) que mueve su envolvente: el aire.
  LMMS sube el filtro (barrido² × 6000) Hz (medido).
- **Kicker**: los golpes graves (un seno que cae).
- **AudioFileProcessor** con `misc/metalish_dong01.ogg`: el metal de la hoja.
