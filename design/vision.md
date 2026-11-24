# Mosslantern: visión creativa

*Versión 0.1 · Borrador del creative-director · 2026-11-24 · Estado: pendiente de aprobación del dueño*

Basado en `data/contenido.json`, `README.md`, `unity/LEEME.md` y `Scripts/Nucleo/Reglas.cs`.

---

## 1. Pitch

> Heredas la granja del abuelo Aurelius en Mosslantern Glen, un valle donde el musgo come polvo de cristal y brilla como faroles azules. Pero los faroles se están apagando: bajo el acantilado, las minas y criptas se han tragado a los **Candelillos**, pequeñas criaturas de musgo y luz que antes cuidaban el valle. De día cultivas, cocinas conservas y fabricas. Cuando quieres, bajas a mazmorras que cambian en cada visita y rescatas Candelillos dormidos. Ellos vuelven contigo, se instalan en el valle, te ayudan con la granja y piden cosas a cambio. Cada farol que se vuelve a encender hace el valle más grande, más vivo y más bonito.

**En una línea:** un juego de granja y comunidad acogedor donde las incursiones roguelite sirven para rescatar a los vecinos que harán crecer tu valle.

---

## 2. Pilares

### P1. El valle se enciende contigo
El progreso se *ve*. Cada cosecha, rescate o edificio deja más luz, más color y más vida en el mapa.
- **Significa:** faroles que se vuelven a encender, caminos que se ensanchan, Candelillos paseando, flores donde antes había maleza. El estado del valle cuenta la historia sin texto.
- **No significa:** progreso que solo existe como un número en un menú (dinero, rangos de prestigio) y no cambia nada en pantalla.
- **Prueba:** *¿Alguien que mira una captura del día 30 y otra del día 3 nota la diferencia sin leer la interfaz?* Si una función no cambia el mundo visible, hay que darle un reflejo visual o recortarla.

### P2. Dos ritmos, una sola vida
La granja es calma y la mazmorra es tensión, y cada una alimenta a la otra.
- **Significa:** la mazmorra da materiales y Candelillos que la granja necesita, y la granja da comida, pociones y equipo que la mazmorra necesita. Bajar siempre es opcional y se decide al empezar el día (lo de Aurelius: "each morning you decide").
- **No significa:** obligar a hacer incursiones cada día, ni una granja que sea solo un menú de mejoras entre incursiones.
- **Prueba:** *¿Este recurso o función crea un motivo para cruzar al otro modo?* Si solo se usa dentro de su propio modo, está aislado y hay que conectarlo.

### P3. Los vecinos importan
Los Candelillos son personajes con nombre, casa, gustos y una función, no unidades.
- **Significa:** cada uno tiene un nombre, una manía, una petición y un trabajo preferido. Se les puede saludar, regalar cosas y ver cómo cambian. Si no tienen lo que necesitan, se desaniman y trabajan menos, pero **nunca mueren ni se van para siempre**.
- **No significa:** gestionar recursos humanos con castigos, sacrificios, lealtad, miedo ni obediencia.
- **Prueba:** *¿El jugador recordaría el nombre de este Candelillo una semana después?* Si es intercambiable con otro, le falta personalidad o sobra.

### P4. Peligro con mejillas
Las mazmorras tienen riesgo real y el combate pide habilidad, pero todo se ve tierno y legible.
- **Significa:** monstruos de siluetas redondas, ojos grandes y colores saturados. Morir te devuelve a la granja con lo que ya aseguraste en los puntos de control. Los jefes son memorables, no grotescos.
- **No significa:** gore, terror corporal, calaveras realistas, oscuridad que no deja ver, ni perder horas de progreso.
- **Prueba:** *¿Un niño de 8 años querría un peluche de este monstruo?* Si no, se rediseña.

### P5. El abuelo sigue hablando
La historia vive en objetos y en el lugar: notas, charlas de la tía Brisa y el propio valle.
- **Significa:** notas que se desbloquean con hitos (ya existen en `historia.cartas`), recuerdos de Aurelius ligados a cada Candelillo rescatado y una pregunta de fondo: por qué se apagaron los faroles.
- **No significa:** cinemáticas largas, muros de diálogo ni una historia que se "termina".
- **Prueba:** *¿Esto se puede contar con un objeto o un cambio del valle en vez de con texto?* Si sí, se hace así.

---

## 3. Sensación buscada

**Tono:** cálido y curioso, con una melancolía suave (el abuelo ya no está, los faroles se apagan) que siempre termina en esperanza. Humor amable en los Candelillos y en Brisa.

**Balance:** aproximadamente un 60 % del tiempo en el valle (granja y comunidad) y un 40 % en mazmorras para un jugador típico, y el jugador puede cambiarlo. Los días de lluvia ya empujan a bajar ("Rainy day, easy day"), y eso está bien.

**Qué es concretamente "amigable y agradable a la vista":**
- **Proporciones:** personajes con cabeza grande (aprox. 1:2 cabeza/cuerpo), ojos grandes y brillantes, extremidades cortas. Clara y Arthur ya van por ahí; hay que fijarlo como norma.
- **Formas:** redondeadas, sin puntas agresivas. Los Candelillos tienen forma de gota o bellota con musgo encima y una lucecita azul en la cabeza o la panza.
- **Paleta:** verdes musgo, cremas, ocres cálidos y el **azul farol** como color de marca. Las mazmorras se oscurecen pero nunca llegan al negro: violetas y azules profundos con luz 2D cálida en las antorchas.
- **Vida en pantalla:** cultivos que se balancean, faroles que parpadean, rebote al caminar y brillos al cosechar o rescatar.
- **Contorno y sombreado:** se mantiene el contorno negro de un color. Se cambia `basic shading` por un sombreado suave de 2 o 3 tonos con luz cálida desde arriba a la izquierda, para todo el arte de PixelLab.
- **Legibilidad:** jugador, peligros y objetos usables se distinguen al instante, incluso en la cripta.

---

## 4. Bucle principal

```
              ┌──────────── comida, pociones, equipo ────────────┐
              ▼                                                  │
   ┌───────────────────┐   cosechas, conservas,    ┌───────────────────────┐
   │  DÍA EN LA GRANJA │──── regalos, materiales ─▶│ COMUNIDAD (el valle)  │
   │ arar, regar,      │                           │ casas, Gran Farol,    │
   │ cosechar, fabricar│◀─ Candelillos trabajan ───│ peticiones, ánimo,    │
   └───────────────────┘   (riego, cosecha, horno) │ nuevos edificios      │
              │                                    └───────────────────────┘
              │ ¿bajo hoy?                            ▲            │
              ▼                                       │            │ ventajas de incursión
   ┌───────────────────┐  Candelillos dormidos,       │            │ (un compañero,
   │ INCURSIÓN         │──── minerales, gemas, ───────┘            │  mapas, mejoras)
   │ mina / cripta,    │     recuerdos de Aurelius                 │
   │ pisos que cambian │◀──────────────────────────────────────────┘
   └───────────────────┘
```

**Cómo se alimentan:**
- **Granja → Mazmorra:** pociones (baba + rábano, ya existe), espadas y ropa de combate, y conservas como comida de viaje.
- **Mazmorra → Comunidad:** cada piso puede tener un Candelillo dormido (en un "farol apagado" dentro del mapa). Al llevarlo a casa entra al valle. Los jefes de los pisos 5 y 10 guardan Candelillos especiales con oficio único.
- **Comunidad → Granja:** los Candelillos trabajan en un oficio (regadores, recolectores, fogoneros del horno, espantacuervos). Así se automatiza la granja con *personajes*, no solo con aspersores.
- **Comunidad → Mazmorra:** el **Gran Farol** (edificio central, reemplaza el concepto de "base") sube de nivel con donaciones y desbloquea ventajas: llevar un Candelillo de compañero, ver un piso por adelantado o empezar en un punto de control más profundo.
- **Granja → Comunidad:** los Candelillos piden cosas concretas ("quiero conserva de calabaza", "mi casa necesita una cerca") y dan ánimo, que se traduce en trabajo y en luz del valle.

---

## 5. Qué hay hoy: encaja, cambia, se recorta

### Ya encaja (se queda)
- Granja completa: arar, regar, 3 cultivos, días, lluvia, cuervos y espantapájaros.
- 16 recetas, horno, tarro de conservas, aspersores y cofres: son la base de la economía del valle.
- 2 mazmorras de 10 pisos con jefes en los pisos 5 y 10 y puntos de control en 1 y 6. Su estructura es perfecta para meter rescates.
- Botín por jugador y `Reglas.Aplicar`: dejan abierto el cooperativo.
- Notas de Aurelius y la tía Brisa: son la columna de P5.
- El lore del musgo que brilla ("the damp moss feeds on crystal dust... glows like little blue lanterns") y los "faroles que se apagan" en `murmullos`: de aquí nacen los Candelillos.
- Ropa de granja y de combate: es la señal visual del cambio de ritmo (P2).

### Hay que cambiar
- **Rangos de prestigio** → que se vean en el valle (P1): cada rango enciende un farol o abre una zona, no solo sube números.
- **Monstruos de la cripta** (Bone Miner, Seal Necromancer, esqueletos): rediseñarlos en clave "peligro con mejillas". Por ejemplo, el esqueleto pasa a ser un "minero de huesitos" con casco y farol, y el nigromante un guardián del sello melancólico, no macabro.
- **Mapa de la granja (48×36):** necesita una zona de aldea para casas de Candelillos y el Gran Farol, con espacio para crecer.
- **Estilo de PixelLab (`estilo` en `contenido.json`):** cambiar el sufijo a algo como "cute cozy chibi pixel art, soft 3-tone shading, warm light" y regenerar con un criterio común.
- **Textos de la tienda y de los hitos:** ahora hablan de "loot never runs dry" y de "endless" runs. Hay que reorientarlos hacia el rescate y el valle.
- **Economía:** hoy casi todo termina en dinero para la tienda. Parte de las cosechas y conservas debe ir a la comunidad (regalos y donaciones al Gran Farol).

### Se recorta o se pospone
- La idea de mazmorras infinitas como meta principal: se recorta. Las incursiones repetidas siguen, pero la meta es el valle.
- Más mazmorras, cultivos, recetas o combate más profundo: se pospone hasta que el bucle de comunidad funcione.

---

## 6. Antipilares: lo que no haremos

- **No copiaremos** personajes, nombres, arte, interfaz, música ni mecánicas emblemáticas de Stardew Valley ni de Cult of the Lamb. Son referencia de género, bucle y sensación, nada más. Mosslantern tiene su mundo, sus nombres (Aurelius, Brisa, Clara, Arthur, Candelillos, Gran Farol) y su estética propia.
- **Sin temática de culto ni religiosa:** nada de sermones, adoración, rituales, sacrificios, fe como recurso, adoctrinamiento ni líder venerado. Los Candelillos son vecinos y amigos, no seguidores.
- **Sin castigo cruel a los vecinos:** no mueren, no se les explota, no hay "disidentes" que eliminar.
- **Sin gore ni horror.** El peligro es real, pero tierno.
- **Sin presión de calendario:** ningún evento que se pierda para siempre por no jugar un día, y nada de FOMO.

---

## 7. Las 5 decisiones abiertas para el dueño

1. **Identidad de los Candelillos.** ¿Te gusta la propuesta (criaturas de musgo y luz rescatadas en las mazmorras)? ¿O prefieres que la comunidad sean aldeanos humanos rescatados, o una mezcla (Candelillos como trabajadores y unos pocos aldeanos con historia)? El nombre también está abierto (alternativas: Musgüelos, Farolitos, Lumbritas, y en inglés Mosslings o Glowlings).
2. **¿Cuánta gestión?** ¿Prefieres una comunidad ligera (cada Candelillo tiene un trabajo y una petición, estilo vecinos) o un constructor de aldea con colocación libre de edificios, necesidades múltiples y una barra de ánimo general?
3. **¿Cómo se pierde en la mazmorra?** ¿Se pierde parte de lo recogido al caer (más tensión, más roguelite) o se conserva todo (más acogedor)? Hoy los puntos de control ya suavizan el castigo.
4. **¿Un jugador o cooperativo?** El núcleo ya soporta varios jugadores y botín por jugador. ¿El cooperativo (Clara y Arthur juntos) es un objetivo de primera versión o algo para después?
5. **Idioma y público.** El texto del juego está en inglés y la documentación en español. ¿En qué idioma se lanza primero? ¿El público es general, incluidos niños (lo que refuerza P4), o adulto joven acogedor?

---

*Siguiente paso recomendado: cuando el dueño apruebe las decisiones 1 y 2, el game-designer redacta el GDD de la comunidad (Candelillos y Gran Farol), y el art-director fija la guía de estilo con un Candelillo de muestra generado en PixelLab.*
