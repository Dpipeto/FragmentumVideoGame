# Guion de los mundos

> **Estado:** cargado en el juego (2026-10-07). Los textos viven en
> `Assets/Scripts/Editor/GuionMundos.cs`, y el generador de narrativa los pone en
> `Assets/Data/Narrativa`: `Dialogos_*`, `Cinematica_Cierre_*`, `Cinematica_Entrada_Final`,
> `Cinematica_Final` y las presentaciones de los jefes 2, 3 y 4. La cinemática inicial y la
> presentación de la Centinela («El que ya estaba aquí») tienen su propio guion y no cambian.
>
> **Para corregir una línea**, cámbiala en el asset desde el inspector: el generador solo sustituye
> los textos provisionales («pendiente de guion») y respeta lo escrito a mano. Si prefieres que
> la cambie yo, dímelo y la cambio en `GuionMundos.cs` y en este documento.

## Cómo está escrito

- **La voz de Kael:** frases cortas, en presente y sin certezas. Kael no sabe quién es: descubre
  cada emoción como algo ajeno que, poco a poco, reconoce suyo. Nunca explica la moraleja: la
  vive.
- **Los Arquitectos del Alma:** guardianes sin rostro, no villanos. Hablan del soldado en tercera
  persona («él») y a Kael de tú. Cada uno guarda lo que el soldado no quiso sentir y lo defiende
  porque se lo encargaron.
- **El cierre de cada mundo nombra la habilidad como un gesto emocional:** mantener y soltar el
  golpe, avanzar bajo el agua, ocultarse sin perderse y resistir sin negar. Así la habilidad no es
  un premio: es lo que Kael aprendió.
- **Cada apertura y cada cierre nombran la emoción del mundo,** que es lo que pide la tarea G y lo
  que comprueban las pruebas.
- **Las líneas del HUD duran 4,5 s:** ninguna pasa de unas 120 letras.

| Mundo | Mensaje (Anexo A, tabla 3) | Mecánica | Habilidad | Cómo lo dice el guion |
|---|---|---|---|---|
| Ira | La ira no controlada destruye el entorno y al que la porta | Prisa contra espera | Ataque Cargado | Contener el golpe y soltarlo cuando uno decide |
| Tristeza | La tristeza requiere ser sentida, no suprimida | Agua, lentitud | Dash Acuático | Dejarse hundir y seguir avanzando |
| Miedo | El miedo paraliza cuando se enfrenta en la oscuridad | Visibilidad, sigilo | Invisibilidad Parcial | Ocultarse sin perderse |
| Culpa | La culpa pesa, pero no define la identidad completa | Memorias, consecuencias | Defensa Reforzada | Mirar lo que pasó y resistirlo sin negarlo |
| Esperanza | La integración da paso a la reconstrucción | Las cuatro habilidades | — | Cada fragmento encuentra su sitio |
| Final | La identidad no se encuentra, se construye | Combate espejo y fase sin violencia | — | Caminar juntos hacia la misma luz |

---

## Mundo 1 · Ira · Fortaleza Volcánica

| Momento | Quién | Texto |
|---|---|---|
| Apertura | Kael | Este calor no es mío… y aun así me empuja a correr. Si esto es la Ira, alguien la dejó encendida. |
| Eco de memoria | Kael | Una voz grita órdenes que no entiendo. Mis manos sí las entienden: se cierran solas. |
| Antes del jefe | Centinela Ardiente | Él me dejó aquí para que nadie tocara su rabia. Tú tampoco la tocarás. *(Solo se lee si no hay presentación; hoy la hay.)* |
| Cierre | Kael | La Ira no era el fuego. Era lo que él nunca dijo. La llevo conmigo, pero ya no me lleva ella. |

**Cinemática de cierre** (tres textos, 4 s cada uno; sin imágenes todavía):

1. El fuego se apaga sin que nadie lo sople.
2. En la ceniza queda una forma: un puño que, por fin, se abre.
3. Kael integra el Fragmento de la Ira. Ahora puede contener un golpe… y soltarlo cuando decida.

## Mundo 2 · Tristeza · Ciudad Sumergida

| Momento | Quién | Texto |
|---|---|---|
| Apertura | Kael | Una ciudad entera bajo el agua y ni una lágrima en la superficie. Así de callada es la Tristeza. |
| Eco de memoria | Kael | Alguien deja una silla vacía en la mesa. Nadie la quita. Nadie la nombra. |
| Antes del jefe | Doncella del Lamento | Él cerró los ojos para no llorar, y la ciudad se hundió. ¿Vienes a cerrarlos tú también? |
| Cierre | Kael | Me dejé hundir y no me ahogué. La Tristeza pesa, pero también sostiene. |

**Cinemática de cierre:**

1. El agua baja despacio, como quien termina de llorar.
2. La ciudad respira por primera vez en años.
3. Kael integra el Fragmento de la Tristeza. Ya puede avanzar también bajo el agua.

## Mundo 3 · Miedo · Bosque Tenebroso

| Momento | Quién | Texto |
|---|---|---|
| Apertura | Kael | No veo mis propios pasos. Algo respira entre los árboles… o soy yo. Esto es el Miedo. |
| Eco de memoria | Kael | Una trinchera. Un silencio demasiado largo. Él se quedó quieto para que no lo encontraran. |
| Antes del jefe | Espectro del Pavor | Él aprendió a desaparecer para seguir vivo. Yo soy todo lo que dejó escondido. |
| Cierre | Kael | El Miedo no se fue: dejó de decidir por mí. Puedo andar en la sombra sin perderme en ella. |

**Cinemática de cierre:**

1. La oscuridad retrocede un paso. Luego otro.
2. El bosque no era un laberinto: era un lugar donde nadie había encendido una luz.
3. Kael integra el Fragmento del Miedo. Puede ocultarse sin dejar de avanzar.

## Mundo 4 · Culpa · Campos de Batalla Arrasados

| Momento | Quién | Texto |
|---|---|---|
| Apertura | Kael | Aquí nada se movió desde el último disparo. Es la Culpa de otro… y la siento mía. |
| Eco de memoria | Kael | Una orden. Un sí. Un pueblo que ya no está. Él lo recuerda cada día con los ojos abiertos. |
| Antes del jefe | Juez Silente | No dicto sentencia. Solo te muestro lo que él se negó a mirar. |
| Cierre | Kael | Mirarlo no me destruyó. La Culpa es un peso, no un nombre. Puedo cargarlo y seguir de pie. |

**Cinemática de cierre:**

1. Las memorias se encienden una a una sobre la plaza.
2. Nadie las borra. Por primera vez, alguien las sostiene.
3. Kael integra el Fragmento de la Culpa. Un escudo que no niega el golpe: lo resiste.

## Mundo 5 · Esperanza · Jardín Etéreo (sin jefe)

| Momento | Quién | Texto |
|---|---|---|
| Apertura | Kael | Por primera vez nada quiere herirme. Me cuesta creerlo. Quizá así empieza la Esperanza. |
| Eco de memoria | Kael | Una casa con flores en la ventana. Antes de la guerra, él también reía. |
| Cierre | Kael | Cada fragmento encontró su sitio. La Esperanza no borra lo que pasó: lo deja crecer en otro lugar. |

**Cinemática de cierre** (~30 s, no interactiva; el Anexo A dice que Kael ve al Soldado Original a lo
lejos por primera vez). Cinco textos de 6 s:

1. El jardín termina en un borde de niebla.
2. Al otro lado, alguien espera de espaldas. Lleva el mismo abrigo gastado.
3. Kael lo reconoce sin haberlo visto nunca.
4. Es él. El que se quedó con todo lo que Kael no recuerda.
5. El soldado no se da la vuelta. Todavía no.

## Mundo 6 · Final · El Espejo del Origen

| Momento | Quién | Texto |
|---|---|---|
| Apertura | Kael | Todo lo que vine a buscar estaba aquí, esperando. La Integración no es ganarle: es dejar de huir. |
| Antes del jefe | El Verdadero Soldado | Eres lo que sobró cuando me arranqué el dolor. Eres un error. |
| Cierre | Kael | No vine a reemplazarte ni a destruirte. La Integración era esto: caminar juntos hacia la misma luz. |

**Cinemática de entrada** (~45 s). El Anexo A pide que el soldado confronte a Kael y que Kael
responda con actos, no con palabras. Siete textos de 6 s:

1. Un espejo del tamaño del cielo.
2. Del otro lado, el soldado. Del mismo lado, también.
3. **Soldado:** Te hice para no sentir.
4. **Soldado:** Y aun así lo sentiste todo.
5. **Soldado:** Eres un error.
6. Kael no responde.
7. Da un paso al frente.

**Cinemática final:** se queda la que ya hay. Solo cambio el orden, para que la pregunta llegue
sola y al final:

1. El soldado se fragmenta en luz.
2. Kael no la esquiva: la recibe.
3. El mundo se reconstruye, pieza a pieza.
4. ¿Eres un error… o una nueva identidad?

---

## Lo que queda abierto

- **Las presentaciones de los jefes 2, 3 y 4** llevan su línea de «Antes del jefe», con el nombre
  del jefe como título y el de su mundo debajo. Cuando tengan planos, cada una puede tener su guion
  aparte, como «El que ya estaba aquí».
- **La pregunta final** se queda 6 s en pantalla (los demás textos, 4): no tiene respuesta.
- **Los ecos de memoria** no suenan todavía: llegan con las memorias interactuables de la Fase 10
  (4 en la Culpa y 4 en el Jardín). Esas memorias necesitarán su propio texto, de unos 5 s cada
  una; lo escribo cuando lleguemos.
- **Nombres:** uso «Juez Silente» y «El Verdadero Soldado», que son los canónicos de los datos.
  En los textos el jefe final se llama «el soldado».
