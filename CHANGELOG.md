# CHANGELOG — Fragmentum: Ecos de la Identidad (v2)

> Registro de la refactorización, en formato **problema → causa → solución**, pensado para el
> capítulo de desarrollo de la tesis. Una entrada por fase.
>
> - **v1**: proyecto original `Tesis` (Unity 6000.2.14f1), congelado tras la auditoría de la Fase 0.
> - **v2**: este proyecto `Tesis2.0` (Unity 6000.6.0f1), donde se construye la versión refactorizada.

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
