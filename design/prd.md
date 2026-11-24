# Game Brief (PRD): Mosslantern

> Documento de producto armado con las plantillas de Claude-Code-Game-Studios (`game-brief` + `milestone-definition`).
> Junta lo que ya existe en el repo, las decisiones del dueño (`design/README.md`) y las lecciones del devlog
> *"I Spent 2 Years Developing My Indie Game – Was It Worth It?"* (JohnBrx, ISO-CORE).
> Los encabezados quedan en inglés como pide la plantilla. Marcas: *(guessed)* = lo propuso el equipo;
> *(inferred (strong))* = sale del código o de otro documento; sin marca = lo dijo el dueño.
> Fecha: 2026-11-24 · Rama: `mosslantern-unity` · Detalle de cada sistema: `vision.md`, `sistemas-nucleo.md`,
> `niveles.md`, `biblia-de-arte.md`.

## In your words

- "un juego tipo Cult of the Lamb junto con Stardew Valley en construcción, dungeons, y que los monitos, sprites y ambiente sea igual de amigable y agradable a la vista"
- "todos los personajes deben seguir un mismo arte. no pueden haber artes más lindos y simpáticos y otros más realistas… ahí el arte del slime es el que predomina"
- Primera versión: "Un jugador". Idioma: "debe ser todo en inglés".
- Sobre los protagonistas: "si lo dejas con el arte del slime" → aprobado tras el filtro: "Sí, calzan". Tras probar el .exe: "Se ve bien".

**Pitch:** Heredas la granja del abuelo en Mosslantern Glen: cultivas de día, bajas a mazmorras cambiantes a rescatar Mosslings y, con ellos, conviertes la granja en una aldea que se ilumina.

**Genre:** granja cozy + construcción de aldea + mazmorras cortas tipo roguelite.

**Controls:** WASD/flechas caminar · Espacio/J/Enter/clic usar · 1-0, -, =, Q/E o rueda para la barra · I/Tab mochila · C crafteo · F comer. *(inferred (strong): README y LEEME.txt del build)*

**Question:** ¿Rescatar un Mossling en la mazmorra y verlo trabajar en la granja al día siguiente hace que quieras volver a bajar? *(guessed)*

**Hooks:** los Mosslings rescatados que viven y trabajan en tu granja; la aldea que se ve crecer e iluminarse en el mapa. *(guessed, de `vision.md`)*

**Intent:** soft or no failure — "fallar es una siesta", sin multa de dinero al caer *(you said: confías en `sistemas-nucleo.md`)* · payoff builds — la aldea crece con cada expedición *(from Cult of the Lamb)*.

## Look and references

- Look: pixel art top-down con el **estilo del slime** (`assets/sprites/limo.png`) para todo personaje y criatura: chibi, ojos grandes con brillo, mejillas rosadas, pocos colores vivos, contorno oscuro continuo. Entorno cálido: césped con tréboles, árbol de copa redonda, cueva de piedra cálida, cripta lila con vacío estrellado. Firma: farolillos azules de musgo.
- References: Stardew Valley (la vida de granja y el día a día), Cult of the Lamb (la base que crece alimentada por expediciones), ISO-CORE (alcance pequeño, peligro ligero, recetas que piden drops de monstruos). Solo de género y sensación: nada copiado.
- View: 2D · Camera: top-down siguiendo al jugador, tiles de 32 px.

## First minute

Despiertas frente a la casa del abuelo con azada, regadera y semillas de rábano. Aunt Brisa saluda desde su puesto. Aras tres casillas, siembras, riegas en el estanque y ves el primer brote; al lado del acantilado un farol azul apagado parpadea en la boca de la cueva: el gancho (algo te espera abajo) aparece antes de terminar el primer día. *(guessed)*

## Challenge

Desde el día 2-3 la Mina pide energía y vida: limos y murciélagos en los pisos 1-4, el Rey Slime en el 5. Caer te devuelve a la granja con media vida y sin multa; los **Destellos** de esa expedición se pierden. Más adelante la Cripta (se abre al llegar al piso 5 de la Mina) sube la escala de los monstruos ×1,6. *(inferred (strong): `contenido.json`, `sistemas-nucleo.md`)*

## Core loop

- Día en la granja: cultivar, procesar (horno, tarro), vender → dinero y comida.
- Expedición: bajar 3-5 pisos, minar, pelear, rescatar Mosslings y juntar `musgo_brillante` → materiales y residentes.
- Aldea: construir casas y talleres, alimentar a los Mosslings → producen materiales y ventajas para la próxima expedición.
- Cada bucle alimenta a los otros dos; se repite con un día nuevo. *(inferred (strong): `sistemas-nucleo.md` §1)*

## Goal & fail state

- Meta: tener los 6 Mosslings de la primera tanda viviendo en la aldea y la Torre de Faroles encendida; una sesión se siente completa al terminar un día con algo nuevo construido o rescatado. *(guessed)*
- Fallo: desmayarse a las 2:00 o caer en la mazmorra → vuelves a casa; se pierden los Destellos, nunca residentes ni edificios.

## Features

- [x] Granja: arar, regar, 3 cultivos, lluvia, cuervos y espantapájaros
- [x] Crafteo (16 recetas), horno, tarro, tienda de Aunt Brisa, caja de envíos
- [x] 2 mazmorras de 10 pisos con jefes en 5 y 10, puntos de control, rangos y botín por jugador
- [x] Guardado con respaldos (no pierde la partida) y arte unificado en el estilo del slime
- [ ] Construcción en la granja (`construir`, `mejorar`, `mover`) con los primeros edificios
- [ ] Mosslings: rescate en la mazmorra, hogar, comida y un trabajo cada uno
- [ ] Destellos: elegir 1 de 3 ventajas temporales por expedición

## Your systems

- granja y cultivos · in the slice
- crafteo, horno y tarro · in the slice
- tienda y caja de envíos · in the slice
- mazmorras con jefes · in the slice
- construcción de aldea · thin stand-in *(guessed: gestión ligera primero)*
- residentes Mosslings · thin stand-in *(guessed)*
- Destellos · thin stand-in *(guessed)*
- granja por zonas con expansiones · later *(guessed)*
- salas con tipo en la mazmorra (fogata, rescate, santuario) · later *(guessed)*
- historia del abuelo y diario · in the slice *(inferred (strong))*

## Keep

- El arte del slime en todos los personajes y monstruos *(you said)*.
- Clara y Arthur como quedaron: referencias en `assets/referencias/`, filtro `herramientas/estilo_slime.py` *(you said: "Sí, calzan")*.
- El juego tal como se ve en el build del 24-11 *(you said: "Se ve bien")*.

## Avoid

- Mezclar personajes realistas con tiernos *(you said)*.
- Los protagonistas de ojos de punto y proporciones forzadas de PixelLab ("no está bien hecho") y los dibujados a mano de 16×24 ("feo") *(you said)*.
- Copiar personajes, nombres, temas de secta o arte de Cult of the Lamb o Stardew Valley.
- Castigos duros: los residentes nunca mueren ni se van.
- Textos del juego en otro idioma que no sea inglés *(you said)*.

## Out of scope

- Cooperativo (el núcleo ya lo permite; queda para después) *(you said: un jugador)*.
- Constructor de aldea completo (rutas, logística, edificios libres): primero gestión ligera.
- Mazmorras nuevas, biomas nuevos o un mapa abierto: primero que funcione el bucle de la comunidad.
- Versión móvil y localización a otros idiomas.
- Rehacer la casa, el puesto y los objetos de la granja (ya funcionan).

## Build order

1. **Primer Mossling (la prueba del gancho):** un farol apagado en el piso 3 de la Mina; al encenderlo con una antorcha el Mossling vuelve contigo, vive en una Moss Hut y riega 12 casillas cada mañana. Se gana al verlo trabajar; se pierde si caes antes de salir (lo vuelves a buscar).
2. Construcción mínima: Lantern Hearth, Moss Hut, Shared Table y Woodshed con `construir`/`mover`.
3. Comida y felicidad: el Mossling come del Shared Table; si no come, duerme la siesta y no trabaja.
4. Destellos: 1 de 3 al bajar y al vencer al jefe del piso 5.
5. Pulido para la demo: ajustes de comodidad, textos en inglés revisados, tutorial del primer día.

---
**Kill condition:** si después de dos rondas de ajuste, rescatar y alojar al primer Mossling no da ganas de bajar otra vez, se replantea la comunidad antes de construir más edificios. *(guessed)*

**Who it's for / what they feel:** jugadores de granjas cozy que quieren un poco de aventura sin estrés; se sienten cuidadores de una aldea que crece gracias a ellos. *(guessed)*

**Games you love / don't:** Stardew Valley (la granja), Cult of the Lamb (la base que crece) *(you said)*; EarthBound como guía de proporciones, que al final se descartó para los protagonistas *(you said)*.

---

## Lecciones del video aplicadas a Mosslantern

| Lo que cuenta el devlog | Cómo lo aplicamos |
| --- | --- |
| Abandonó muchos proyectos; esta vez mantuvo el alcance pequeño y realista. | Out of scope explícito, un solo hito a la vez y una *kill condition*. La demo es "Primer Mossling", no el juego entero. |
| Eligió el estilo de arte según lo que sabía hacer (pixel art isométrico en vez de 3D). | La receta que funciona ya está escrita: referencia pixflux → personaje v3 → `igualar_escala.py` → `estilo_slime.py`. Todo personaje nuevo sale por ahí, sin experimentos caros. |
| Los controles WASD no se sentían naturales en isométrico; usó clic para moverse con A\*. | Nuestra vista es top-down y WASD funciona. Los Mosslings sí necesitarán caminar solos: A\* sobre la rejilla de ocupación que ya existe (`Mundo.Ocupacion`). |
| Un mapa de celdas ocupadas, invisible, para el pathfinding. | Ya existe (`g.ocupa`); se reutiliza para residentes y construcción. |
| El mundo en islas que se desbloquean no resultó divertido; probó un mes y pivotó a mundo abierto. | La granja por zonas con expansiones (`niveles.md`) es parecida a sus islas: se deja en *later* y se prueba barata antes de invertir. Si no divierte, se cambia rápido. |
| Los chunks prehechos hacían el mundo repetitivo; regeneró cada aspecto por separado y añadió biomas. | Las mazmorras ganan tipos de sala (fogata, rescate, santuario) y la regeneración nocturna de la granja se limita al "monte" para que lo limpiado se quede limpio. |
| Peligro ligero, pocos monstruos, y algunas recetas piden drops para que no se puedan evitar siempre. | Ya pasa: `baba`, `ala` y `hueso` aparecen en recetas. Se mantiene en los edificios nuevos (Expedition Board, Moss Bath). |
| La rejilla y la sacudida de cámara molestaban; las hizo ajustables. | Añadir un menú de opciones: sacudida de cámara, mostrar cuadrícula, volumen, tamaño de texto. |
| Cambió el menú de construcción por el inventario, que maneja construcción e items. | Al diseñar `construir`, evaluar hacerlo desde el panel de crafteo/inventario en vez de un menú aparte. |
| Los devlogs y los comentarios le dieron motivación y feedback; le pidieron página de Steam pronto para wishlists; playtesters por Patreon. | Ya hay una build de prueba (`v0.1-prueba`). Siguiente: probadores con la build de cada hito y, cuando la demo esté lista, página de Steam para wishlists. |
| Meta cercana: demo en Steam. | Hito "Primer Mossling" (abajo) como demo. |

## Milestone: Demo "Primer Mossling"

**Goal:** que un jugador nuevo juegue 30-45 minutos, rescate al primer Mossling, le construya casa y lo vea trabajar.

**Success criteria**
- 3 de 5 probadores dicen que querían volver a bajar después de alojar al Mossling.
- Ningún probador pierde la partida ni se queda atascado (guardado con respaldos ya hecho).
- Todos los personajes y criaturas pasan la lista de `biblia-de-arte.md` §7 con el estilo del slime.

**Must ship:** pasos 1-3 del Build order, textos en inglés, build de Windows.
**Should ship:** Destellos (paso 4), menú de opciones.
**Stretch:** segundo Mossling con otro trabajo (Woodshed), tipo de sala "rescate".

**Quality gates:** `npm test` (núcleo C#) en verde con pruebas nuevas para `construir` y residentes; `VERSION_ESTADO` subido con migración probada; build abre sin excepciones; prueba manual de un día completo.

**Risks**
- Subir `VERSION_ESTADO` puede invalidar guardados: el respaldo automático lo cubre, pero hay que probarlo.
- Los Mosslings necesitan arte con la receta del slime (cuesta generaciones de PixelLab; ~930 disponibles).
- El arte de PixelLab puede variar entre generaciones: mostrar una prueba barata antes de generar en serie.
- Alcance: la comunidad completa (11 edificios) es tentadora; solo entran los 4 del Build order.
