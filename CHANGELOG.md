# CHANGELOG — Fragmentum: Ecos de la Identidad (v2)

> Registro de la refactorización, en formato **problema → causa → solución**, pensado para el
> capítulo de desarrollo de la tesis. Una entrada por fase.
>
> - **v1**: proyecto original `Tesis` (Unity 6000.2.14f1), congelado tras la auditoría de la Fase 0.
> - **v2**: este proyecto `Tesis2.0` (Unity 6000.6.0f1), donde se construye la versión refactorizada.

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
