# Diseño de Mosslantern

Propuesta del equipo del estudio (Claude-Code-Game-Studios) para llevar el juego hacia una mezcla de
**vida de granja tranquila**, **construir una comunidad que crece** y **mazmorras por expediciones**,
con personajes y ambiente tan amables y agradables a la vista como los mejores juegos del género.
Las referencias (Stardew Valley, Cult of the Lamb) son de género y sensación: Mosslantern no copia
sus personajes, nombres, temas ni arte.

| Documento | Quién | Qué decide |
| --- | --- | --- |
| [vision.md](vision.md) | Dirección creativa | Pilares, tono, bucle principal, qué se corta |
| [sistemas-nucleo.md](sistemas-nucleo.md) | Diseño de juego | Construcción, residentes, recursos, expediciones, primer hito jugable |
| [biblia-de-arte.md](biblia-de-arte.md) | Dirección de arte | Auditoría del arte actual, estilo, paletas, tamaños, recetas de PixelLab |
| [niveles.md](niveles.md) | Diseño de niveles | Mapa de la granja por zonas, salas de mazmorra, encuentros |

## En lo que coinciden los cuatro

- **La comunidad es el centro nuevo.** Pequeñas criaturas de musgo y luz se rescatan en las mazmorras,
  se mudan al valle, trabajan (riegan, cosechan, producen materiales) y piden cosas. Nunca mueren ni se
  van: si están tristes, solo dejan de trabajar.
- **Las mazmorras alimentan la granja y la granja alimenta las mazmorras.** Materiales y residentes
  salen de las expediciones; comida, equipo y ventajas salen de la granja y la comunidad.
- **El progreso se ve en el mapa**, no solo en números: faroles encendidos, casas, caminos, gente.
- **Peligro con cara amable.** Los monstruos pequeños (`limo`, `fantasma`, `murcielago`) ya tienen el
  tono buscado; los jefes, el esqueleto y los protagonistas no.

## Decisiones del dueño (2026-11-24)

1. **Un solo estilo de arte para todo**: protagonistas, residentes, monstruos y jefes siguen el estilo del
   slime (`assets/sprites/limo.png`): redondo, simpático, chibi. No puede haber personajes realistas junto a
   otros tiernos. Se aprueba `biblia-de-arte.md`.
2. **Primera versión para un jugador.** El cooperativo queda para después (el núcleo ya lo permite).
3. **Todo el juego en inglés.** Como el juego va en inglés, los residentes se llaman **Mosslings**.
4. El dueño confía en las propuestas del equipo: se sigue `sistemas-nucleo.md` (sin multa de dinero al caer
   en la mazmorra) y se empieza con **gestión ligera** de la aldea (el primer hito jugable), dejando el
   constructor completo para cuando el bucle de la comunidad funcione.

## Receta aprobada para personajes (2026-11-24)

El dueño eligió como guía de estilo los RPG retro de 8/16 bits: cabeza enorme, **ojos de punto negros**,
mejillas rosadas, contorno oscuro grueso y colores planos. Clara y Arthur (con sus trajes) ya están hechos así.
Para que PixelLab lo respete no basta el texto del prompt: hace falta su ajuste de proporciones.

- `contenido.json` → `estilo.personajes`: lienzo 48, `proportions` personalizadas (cabeza ×1,7, piernas ×0,55,
  brazos ×0,6), `low top-down`, contorno negro, `low detail`, sin realce de color.
- Si la animación de caminar de PixelLab cambia la ropa de un traje (pasó con los dos de Arthur), la caminata se
  arma desde el propio dibujo: `python herramientas/animar-personajes.py --locales --caminar-simple=granjero/ropa_minera`.
- Después: `python herramientas/animar-personajes.py --locales` (acciones) y `python herramientas/unity/sincronizar.py`.

## Orden de trabajo recomendado

1. **Arreglos baratos ya comprobados en el código** (no necesitan decisiones):
   - `Mazmorra.cs`: monstruos normales aparecen en la sala del jefe y pueden amontonarse en la misma celda.
   - `Reglas.cs` (`Usar`, colocables): se gasta el objeto aunque `Mundo.Colocar` falle. Hoy no pasa porque
     todo lo colocable ocupa 1×1, pero romperá con edificios grandes.
   - `NuevoDia` vuelve a sembrar rocas, árboles y maleza en cualquier sitio, lo que tapa lo que el jugador limpia.
2. **Arte de los protagonistas**: rehacer granjera y granjero en chibi (cabeza ≥ 40 % de la figura,
   contorno marrón oscuro `#2b1d1a`) con la receta de `biblia-de-arte.md`. Es el cambio que más se nota.
3. **Primer hito jugable** (`sistemas-nucleo.md` § 5): construcción, residentes, rescate en la mazmorra,
   ventajas temporales por expedición y el material `musgo_brillante`. Necesita subir `VERSION_ESTADO`.
4. Granja por zonas y salas con tipo en la mazmorra (`niveles.md`).
