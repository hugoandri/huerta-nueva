# Game Brief (PRD): Mosslantern

> Documento de producto armado con las plantillas de Claude-Code-Game-Studios (`game-brief` + `milestone-definition`).
> Junta lo que ya existe en el repo, las decisiones del dueño (`design/README.md`), ideas adoptadas del GDD
> **Whisper Valley** (@Carlos, 2026-11-21) y las lecciones del devlog de ISO-CORE (JohnBrx).
> Los encabezados quedan en inglés como pide la plantilla. Marcas: *(guessed)* = propuesta del equipo;
> *(inferred (strong))* = sale del código o de otro documento; *(from Whisper Valley)* = idea adoptada de ese GDD;
> sin marca = lo dijo el dueño.
> Fecha: 2026-11-24 · Rama: `mosslantern-unity` · Detalle: `vision.md`, `sistemas-nucleo.md`, `niveles.md`, `biblia-de-arte.md`.

## In your words

- "un juego tipo Cult of the Lamb junto con Stardew Valley en construcción, dungeons, y que los monitos, sprites y ambiente sea igual de amigable y agradable a la vista"
- "todos los personajes deben seguir un mismo arte… ahí el arte del slime es el que predomina"
- Primera versión: "Un jugador". Idioma: "debe ser todo en inglés".
- Protagonistas: "si lo dejas con el arte del slime" → "Sí, calzan". Tras probar el .exe: "Se ve bien".
- Sobre el GDD de Whisper Valley: "debe tener sentido con esto" → elegiste **mezclar ideas**.

**Pitch:** Heredas la granja del abuelo en Mosslantern Glen, un pueblo pequeño donde todos se enteran de todo: cultivas de día, bajas a mazmorras a rescatar Mosslings y, cada noche, el boletín anónimo del pueblo cuenta lo que hiciste.

**Genre:** granja cozy + vida de pueblo con relaciones y chisme + construcción de aldea + mazmorras cortas tipo roguelite.

**Controls:** WASD/flechas caminar · Espacio/J/Enter/clic usar · 1-0, -, =, Q/E o rueda para la barra · I/Tab mochila · C crafteo · F comer. *(inferred (strong))*

**Question:** ¿Rescatar un Mossling, verlo trabajar en la granja y leer a la mañana siguiente qué dice el pueblo de ti hace que quieras jugar otro día? *(guessed)*

**Hooks:** los Mosslings que viven y trabajan en tu granja; *The Lantern Post*, el boletín nocturno de un autor anónimo que convierte tus actos (en la granja, en el pueblo y en la mazmorra) en chisme. *(guessed; el boletín, from Whisper Valley)*

**Intent:** soft or no failure — caer en la mazmorra o un mal rumor nunca te quitan lo construido · payoff builds — la aldea y tu reputación crecen día a día.

## Look and references

- Look: pixel art top-down con el **estilo del slime** (`assets/sprites/limo.png`) para todo personaje y criatura: chibi, ojos grandes con brillo, mejillas rosadas, pocos colores vivos, contorno oscuro continuo. Entorno cálido (césped con tréboles, árbol redondo, cueva cálida, cripta lila). Firma: farolillos azules de musgo. El boletín se ve como una hoja de papel pixelada clavada en el tablón del pueblo, con un sello azul de farol. *(sello y hoja: from Whisper Valley, adaptado)*
- References: Stardew Valley (granja y pueblo), Cult of the Lamb (la base que crece con expediciones), Whisper Valley (chisme con testigos, columna nocturna, estaciones con festival, corazones de amistad), ISO-CORE (alcance pequeño, peligro ligero). Solo de género y sensación.
- View: 2D · Camera: top-down siguiendo al jugador, tiles de 32 px. *(Whisper Valley usa isométrico 2:1; aquí se mantiene top-down porque el juego, el arte y el código ya lo son.)*

## First minute

Despiertas frente a la casa del abuelo con azada, regadera y semillas. Aras, siembras y riegas tu primer surco. En el tablón junto al puesto de Aunt Brisa hay una hoja nueva: *"Hay luz otra vez en la granja de Aurelius… Quien la ocupa no sabe ni dónde va la regadera."* Firmado: **The Night Moth**. A lo lejos, en la boca de la cueva, un farol azul apagado parpadea. *(guessed; la primera columna, from Whisper Valley)*

## Challenge

Desde el día 2-3 la Mina pide energía y vida (limos y murciélagos en los pisos 1-4, el Rey Slime en el 5). Caer te devuelve a casa con media vida y sin multa; se pierden los Destellos de esa expedición… y esa noche The Night Moth lo publica. En el pueblo el reto es social: lo que haces tiene testigos, y un rumor puede bajar corazones si no lo atiendes. *(guessed; testigos, from Whisper Valley)*

## Core loop

- **Mañana:** leer *The Lantern Post* en el tablón (1-3 rumores) → marca la agenda del día. *(from Whisper Valley)*
- **Día:** granja (cultivar, procesar, vender) y pueblo (hablar, regalar, encargos) → dinero, comida y corazones. Las acciones sociales no gastan energía. *(from Whisper Valley)*
- **Expedición (opcional cada día):** minar, pelear, rescatar Mosslings y juntar `musgo_brillante` → materiales y residentes.
- **Noche:** construir y alimentar a los Mosslings; dormir → el juego procesa lo que pasó y The Night Moth escribe.

## Goal & fail state

- Meta del Año 1: alojar 6 Mosslings, encender la Torre de Faroles y llegar a 6 corazones con 3 vecinos. Una sesión se siente completa al terminar un día con algo construido, rescatado o un corazón ganado. *(guessed)*
- Fallo: desmayarse a las 2:00 o caer en la mazmorra → vuelves a casa sin perder residentes ni edificios; un rumor ignorado se apaga en 3 días. *(guessed; los 3 días, from Whisper Valley)*

## Features

- [x] Granja: arar, regar, 3 cultivos, lluvia, cuervos y espantapájaros
- [x] Crafteo (16 recetas), horno, tarro, tienda de Aunt Brisa, caja de envíos
- [x] 2 mazmorras de 10 pisos con jefes en 5 y 10, puntos de control y rangos
- [x] Guardado con respaldos y arte unificado en el estilo del slime
- [ ] Mosslings: rescate en la mazmorra, hogar, comida y un trabajo cada uno
- [ ] Construcción mínima en la granja (`construir`, `mover`)
- [ ] Vecinos con corazones de amistad, regalos y un hito a los 2/4/6 corazones
- [ ] *The Lantern Post*: rumores con testigos y una columna nocturna hecha con plantillas

## Your systems

- granja y cultivos · in the slice
- crafteo, horno y tarro · in the slice
- tienda y caja de envíos · in the slice
- mazmorras con jefes · in the slice
- residentes Mosslings · thin stand-in *(guessed)*
- construcción de aldea · thin stand-in *(guessed: gestión ligera)*
- vecinos y corazones de amistad · thin stand-in *(from Whisper Valley)*
- chisme con testigos y columna nocturna · thin stand-in *(from Whisper Valley)*
- reputación en dos ejes · later *(from Whisper Valley)*
- estaciones de 14 días con festival · later *(from Whisper Valley)*
- misiones (historia, encargos del tablón, arcos de vecino) · later *(from Whisper Valley)*
- Destellos en la expedición · later *(guessed)*
- historia del abuelo y diario · in the slice *(inferred (strong))*

## Keep

- El arte del slime en todos los personajes y monstruos *(you said)*.
- Clara y Arthur como quedaron (`assets/referencias/`, `herramientas/estilo_slime.py`) *(you said)*.
- El juego tal como se ve en el build del 24-11 *(you said)*.

## Avoid

- Mezclar personajes realistas con tiernos *(you said)*.
- Los protagonistas de ojos de punto de PixelLab y los dibujados a mano de 16×24 *(you said)*.
- Copiar personajes, nombres o arte de Cult of the Lamb, Stardew Valley o Whisper Valley: las ideas se adaptan con nombres propios de Mosslantern.
- Chisme cruel: los rumores hablan de **acciones y decisiones**, nunca de cuerpo, salud, discapacidad, orientación o identidad; picante y teatral, no bullying *(from Whisper Valley, regla de tono)*.
- Castigos duros: los Mosslings nunca mueren ni se van; un rumor nunca te quita lo construido.
- Textos del juego en otro idioma que no sea inglés *(you said)*.

## Out of scope

- Lo que Whisper Valley tiene y Mosslantern **no** adopta: iPhone y diseño vertical, isométrico 2:1, Godot, free-to-play con pase diario y Pétalos, backend en la nube e IA que escribe la columna, protagonista única personalizable, moda como sistema de juego y red social in-game. *(decidido al mezclar: chocan con PC/Unity, un jugador y Clara/Arthur)*
- Romance y boda: después de que la amistad funcione *(guessed)*.
- Cooperativo *(you said: un jugador)*.
- Constructor de aldea completo, mazmorras o biomas nuevos, mapa abierto.
- Rehacer la casa, el puesto y los objetos de la granja.

## Build order

1. **Primer Mossling (la prueba del gancho):** un farol apagado en el piso 3 de la Mina; al encenderlo con una antorcha el Mossling vuelve contigo, vive en una Moss Hut y riega 12 casillas cada mañana.
2. **Construcción mínima:** Lantern Hearth, Moss Hut, Shared Table y Woodshed con `construir`/`mover`.
3. **Vecinos:** Aunt Brisa y 2 vecinos nuevos con corazones de amistad (0-10), gustos de regalo y un evento a los 2, 4 y 6 corazones.
4. **The Lantern Post:** testigos por lugar y hora, rumores con etiqueta, verdad y jugosidad, y una columna de 1-3 rumores cada mañana hecha con plantillas. Respuestas: ignorar o desmentir.
5. Pulido para la demo: ajustes de comodidad, textos en inglés revisados, tutorial del primer día.

---
**Kill condition:** si después de dos rondas de ajuste, rescatar al primer Mossling no da ganas de bajar otra vez, o leer la columna por la mañana no cambia lo que el jugador decide hacer ese día, se replantea ese sistema antes de sumar contenido. *(guessed)*

**Who it's for / what they feel:** jugadores de granjas cozy que disfrutan las relaciones y un poco de aventura sin estrés; se sienten la persona nueva de la que todos hablan y que decide qué cuenta el pueblo de ella. *(guessed; la fantasía, from Whisper Valley)*

**Games you love / don't:** Stardew Valley (la granja), Cult of the Lamb (la base que crece) *(you said)*; Whisper Valley como fuente de ideas de pueblo y chisme *(you said: mezclar ideas)*.

---

## Qué se adopta de Whisper Valley y cómo encaja

| Idea de Whisper Valley | Versión Mosslantern | Encaje con lo que ya existe |
| --- | --- | --- |
| Lady Lavanda y El Susurro (columna anónima nocturna) | **The Night Moth** escribe ***The Lantern Post***, una hoja clavada en el tablón del pueblo cada mañana. | La ventana de "resumen" de `NuevoDia` ya junta ventas y notas del día: la columna sale de ahí. |
| El misterio de quién escribe, que nunca se resuelve | The Night Moth ya escribía cuando vivía el abuelo Aurelius; las notas del abuelo dan pistas que nunca confirman a nadie. | Engancha con `historia.cartas` (las notas del abuelo) y los `murmullos` nocturnos. |
| Rumores con testigos, etiqueta, verdad, jugosidad y alcance | Igual, con testigos por lugar y hora (granja, puesto, pueblo, boca de la cueva). La mazmorra también genera rumores: vencer a un jefe, caer, rescatar un Mossling. | Las reglas viven en el núcleo (`Reglas.Aplicar` devuelve eventos); se pueden probar sin Unity como el resto del núcleo. |
| Respuestas: desmentir, abrazar, ignorar, contraatacar, pedir defensa | Demo: ignorar y desmentir. Después: abrazar y pedir defensa a un amigo con 6+ corazones. Contraatacar queda fuera (choca con el tono amable). | — |
| Reputación en dos ejes (Encanto / Integridad) | Dos ejes con nombres propios, por ejemplo *Glow* (fama) y *Heart* (confianza). | Se guarda en `Estado` junto a `hitos`. |
| Corazones de amistad y romance, hitos a 2/4/6/8/10 | Amistad primero; romance más adelante. | Los regalos usan los productos que ya existen: conservas, lingotes, gemas. |
| Las acciones sociales no gastan energía | Igual: hablar y regalar no gastan energía. | Respeta el sistema de energía actual. |
| Estaciones de 14 días con festival y "drama de temporada" | Más adelante: 4 estaciones de 14 días; el primer festival sería la **Noche de Faroles**, cuando los Mosslings rescatados iluminan el pueblo. | Hoy el día ya cuenta días y lluvia (`Tiempo`). |
| Tipos de misión (historia, arco de personaje, encargo, chisme, proyecto, festival) | Encargos en el tablón del pueblo y arcos de vecino primero. | Las recompensas usan los items actuales. |
| Regla de tono del chisme | Adoptada tal cual (ver Avoid). | — |

## Lecciones del devlog de ISO-CORE

| Lo que cuenta el devlog | Cómo lo aplicamos |
| --- | --- |
| Abandonó muchos proyectos; esta vez mantuvo el alcance pequeño y realista. | Out of scope explícito, un hito a la vez y una *kill condition*. Con las ideas de Whisper Valley el riesgo de alcance crece: solo entra lo del Build order. |
| Eligió el arte según lo que sabía hacer. | La receta del slime ya está escrita y se usa para todo personaje y vecino nuevo. |
| Clic para moverse con A\* porque WASD no encajaba en isométrico. | Top-down con WASD funciona; los Mosslings y vecinos que caminan solos usan A\* sobre la rejilla de ocupación que ya existe (`Mundo.Ocupacion`). |
| Las islas desbloqueables no fueron divertidas; pivotó tras un mes. | La granja por zonas queda en *later* y se prueba barata antes de invertir. |
| El mundo hecho de piezas repetidas se volvía monótono. | Tipos de sala en la mazmorra y regeneración nocturna solo en el "monte". |
| Peligro ligero y drops de monstruos necesarios en recetas. | Ya pasa con `baba`, `ala` y `hueso`; los regalos favoritos de algunos vecinos también pueden pedirlos. |
| Rejilla y sacudida de cámara ajustables. | Menú de opciones: sacudida, cuadrícula, volumen, tamaño de texto. |
| Construir desde el inventario en vez de un menú aparte. | `construir` desde el panel de crafteo/inventario. |
| Devlogs, comentarios, página de Steam temprano y probadores. | Build de prueba ya publicada (`v0.1-prueba`); probadores en cada hito y página de Steam con la demo. |

## Milestone: Demo "Primer Mossling"

**Goal:** que un jugador nuevo juegue 30-45 minutos, rescate al primer Mossling, le construya casa, haga un amigo en el pueblo y lea al menos dos columnas que hablen de lo que hizo.

**Success criteria**
- 3 de 5 probadores dicen que querían volver a bajar después de alojar al Mossling.
- 3 de 5 probadores cambiaron lo que hacían en un día por algo que leyeron en *The Lantern Post*.
- Ningún probador pierde la partida ni se queda atascado.
- Todos los personajes, vecinos y criaturas pasan `biblia-de-arte.md` §7 con el estilo del slime.

**Must ship:** pasos 1-3 del Build order, textos en inglés, build de Windows.
**Should ship:** paso 4 (*The Lantern Post* con ignorar/desmentir), menú de opciones.
**Stretch:** segundo Mossling con otro trabajo, primer encargo del tablón.

**Quality gates:** `npm test` (núcleo C#) en verde con pruebas nuevas para `construir`, residentes, corazones y rumores; `VERSION_ESTADO` subido con migración probada; el build abre sin excepciones; prueba manual de tres días completos.

**Risks**
- **Alcance:** las ideas de Whisper Valley duplican el tamaño del juego. Solo entran las del Build order; el resto queda en *later*.
- **Nombres:** Whisper Valley tiene un personaje llamado *Don Aurelio*, y el abuelo de Mosslantern se llama *Aurelius*. Si los dos proyectos conviven, conviene que no se confundan.
- **Escritura:** la columna necesita muchas plantillas de texto en inglés con buen tono; es trabajo de escritura, no de código.
- **Guardado:** `VERSION_ESTADO` sube y el respaldo automático lo cubre, pero hay que probar la migración.
- **Arte:** cada vecino nuevo cuesta generaciones de PixelLab (~930 disponibles); prueba barata antes de generar en serie.

## Open questions

- ¿El autor anónimo se llama *The Night Moth* y el boletín *The Lantern Post*, o prefieres otros nombres?
- ¿Romance entra en el Año 1 o solo amistad?
- ¿Cuántos vecinos en la demo: Aunt Brisa + 2, o más?
- ¿Los Mosslings también chismean (testigos en la granja) o solo los vecinos?
