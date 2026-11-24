# Mosslantern — Sistemas núcleo: granja, comunidad y expediciones

> Documento de diseño (game-designer). Propuesta, no implementado. Ids en español como en `data/contenido.json`; nombres visibles en inglés como el resto del contenido.

**Fantasía:** heredas la granja del abuelo Aurelius en Mosslantern Glen. Bajo el acantilado, la luz de los faroles se apagó y los **Mosslings** (musguitos: criaturitas de musgo con un farolillo en la cabeza) quedaron atrapados en faroles apagados. Los rescatas, les construyes un hogar y la granja se convierte en una aldea iluminada. Tono: amable, sin muerte ni castigos duros; los residentes nunca se van ni mueren.

**Pilares:** (1) cada sistema alimenta a los otros dos; (2) fallar es una siesta, no una pérdida; (3) todo pasa por `Reglas.Aplicar` y por datos en `contenido.json`.

---

## 1. Los tres bucles

| Bucle | Momento a momento | Diario (6:00→2:00) | Semanal (7 días) |
|---|---|---|---|
| **Granja** | arar, regar, cosechar, talar (2 energía/golpe) | sembrar, enviar en la caja, rellenar tarro/horno | cosecha de calabaza (6 días), conservas |
| **Comunidad** | colocar/mejorar edificios, hablar con residentes | alimentar el Comedero, asignar trabajos, recoger producción al amanecer | subir de **Nivel de aldea**, desbloquear edificios |
| **Expedición** | combate, minar, elegir sala, elegir Destello | 1 expedición de 3–5 pisos | jefe (piso 5/10), rescates, subir Rango |

**Conexiones:** la granja da comida → los residentes comen y trabajan → producen madera/piedra/lingotes y dan Ánimos para la expedición → la expedición da minerales, musgo brillante y nuevos residentes → se construyen más edificios → más automatización en la granja.

Dato clave del código: el mundo solo regenera 1 roca, 1 árbol y 2 malezas por noche (`NuevoDia`), es decir, ~6,5 madera y ~2,5 piedra sostenibles al día. Hoy la construcción se quedaría sin madera en una semana; por eso la **Leñera** y la **Cantera** (residentes) son los grifos sostenibles de material.

### Tabla de flujos (grifos / sumideros)

| Recurso | Grifos | Sumideros |
|---|---|---|
| Dinero | caja de envíos (cultivos 45–220, conservas 140–490) | semillas, tienda, **Mercado de la aldea** (nuevo sumidero: decoraciones) |
| Madera | árboles (5–8), Leñera (+6/día por residente) | edificios, cercas, cofres, antorchas |
| Piedra | rocas (2–3), Cantera (+5/día) | edificios, horno, aspersores |
| Lingotes cobre/hierro | horno (3 minerales), Forja de faroles (automática) | edificios de nivel 2–3, espadas |
| Gema | roca_gema, jefes, botines | aspersor_gema, Torre de faroles, mejoras |
| Cultivos/conservas | campo | **comida de residentes**, energía, venta |
| `musgo_brillante` (nuevo) | urnas, limos, cofres de mazmorra, faroles rescatados | edificios de comunidad, decoraciones |
| Felicidad | comida, confort, belleza, hablar | hambre, sin hogar |
| Destellos (temporal) | jefes, cofres, salas de santuario de luz | se pierden al salir o desmayarse |

---

## 2. Sistema de construcción

**Regla:** los edificios no son items de mochila. Se colocan con una nueva acción `construir {receta, x, y}` que valida materiales, celdas libres (`Mundo.Libre(def,g,x,y,ancho,alto)` ya soporta huella) y que estés a ≤3 celdas; descuenta materiales y llama `Mundo.Colocar`. `mejorar {uid}` sustituye el objeto por su versión superior en la misma celda. `mover {uid,x,y}` gratis (cozy: reorganizar sin coste). Solo en la granja y fuera del camino.

Cada edificio tiene en `objetos`: `confort`, `belleza`, `hogar` (plazas), `trabajo` (qué produce) y `radio` de aura (3 celdas para decoración).

| # | id | Nombre | Huella | Coste | Efecto | Desbloqueo |
|---|---|---|---|---|---|---|
| 1 | `hoguera` | Lantern Hearth | 2×2 | madera 20, piedra 20 | centro de la aldea, luz 90; permite recibir residentes | día 1 (tutorial) |
| 2 | `casita_musgo` | Moss Hut | 2×2 | madera 30, piedra 10, fibra 10 | hogar 1, confort +1 | hoguera |
| 3 | `comedero` | Shared Table | 2×1 | madera 25, fibra 5 | baúl de 12 casillas de comida para residentes | 1 residente |
| 4 | `lenera` | Woodshed | 2×2 | madera 15, piedra 10 | trabajo: +6 madera/día por residente (máx. 2) | 1 residente |
| 5 | `cantera` | Stone Yard | 2×2 | madera 10, piedra 25 | +5 piedra/día (+1 mineral_cobre 30%) | 2 residentes |
| 6 | `cobertizo_riego` | Watering Shed | 1×1 | madera 20, piedra 10, lingote_cobre 2 | el residente riega 12 casillas aradas en radio 3 | 2 residentes |
| 7 | `forja_faroles` | Lantern Forge | 2×2 | piedra 30, lingote_cobre 4, musgo_brillante 5 | funde 9 minerales/día solo (pone en cola lo del baúl) | 3 residentes + jefe piso 5 de la Mina |
| 8 | `tablon_expediciones` | Expedition Board | 1×1 | madera 20, hueso 5, ala 3 | elige 1 Ánimo de residente antes de bajar | 3 residentes |
| 9 | `cabana` (mejora de 2) | Cozy Cottage | 2×2 | madera 40, piedra 20, lingote_hierro 2 | hogar 2, confort +3 | 4 residentes |
| 10 | `banos_musgo` | Moss Bath | 2×2 | piedra 25, baba 10, gema 1 | confort +3 en radio 4, +10 felicidad diaria a quien la usa | 5 residentes |
| 11 | `torre_faroles` | Lantern Tower | 1×2 | piedra 30, lingote_hierro 4, gema 2 | +1 Destello inicial por expedición, luz 140 | 6 residentes + Cripta piso 5 |
| — | Decoraciones | Bench / Flower Planter / Garden Lantern / Moss Arch | 1×1 | madera 6 / piedra 2+fibra 3 / madera 2+lingote_cobre 1 / musgo_brillante 4+madera 4 | confort +1 / belleza +1 / belleza +2 y luz / belleza +3 | Bench y Planter desde el día 1 |

**Nivel de aldea** = residentes + (belleza total / 10). Niveles 1–5 en 2, 4, 6, 8, 10 → cada nivel abre edificios y una línea nueva de Aunt Brisa (`historia.saludos`).

---

## 3. Residentes (Mosslings)

**Cómo se consiguen:** en la mazmorra aparece un `farol_apagado` (objeto con `accion: "rescatar"`). Lo relincas con 1 antorcha de la mochila (sumidero amable de antorchas). El Mossling vuelve solo a la granja (rescate seguro: no hay escolta que pueda fallar) y espera junto a la Hoguera hasta tener hogar. Garantías: 1 rescate en el piso 3 y 1 tras cada jefe en la primera victoria; después 25% por piso. Máximo 12 residentes en el slice (6 en la versión mínima).

**Ficha** (`contenido.json` → `residentes`): `id`, `nombre`, `sprite`, `oficio` (leña, piedra, riego, forja, cocina), `gusto` (un cultivo favorito), `don` (Ánimo de expedición).

Ejemplos: Pip (leña, adora el rábano, *+1 corazón*), Fennel (riego, zanahoria, *antorcha dura más/+luz*), Bramble (piedra, calabaza, *+20% minerales*), Wick (forja, conservas, *+1 de daño*), Clover (cocina, zanahoria, *poción gratis al entrar*), Moth (riego, calabaza, *ver cofres en el piso*).

**Necesidades** (felicidad 0–100, empieza en 60, se calcula al amanecer):
- **Hogar:** sin cama −15/día (duerme junto a la Hoguera, nunca se va).
- **Comida:** come 1 cultivo/día del Comedero. Rábano +0, zanahoria +5, calabaza +10, conserva +15, su favorito ×2. Sin comida −10.
- **Entorno:** + confort de su casa + belleza en radio 3 de su casa (tope +15).
- **Hablar** (`usar` sobre él, 1 vez/día): +5 y una frase de `historia`.

**Lo que devuelven:**
- **Trabajo** al amanecer en su edificio asignado (`asignar {residente, uid}`). Producción × (0,5 si felicidad < 30; 1 normal; 1,5 si es su oficio y felicidad ≥ 70).
- **Ánimos** para expediciones: con el Tablón eliges 1 don de un residente con felicidad ≥ 50 (2 dones con Torre de faroles). Duran toda la expedición.
- **Regalos:** con felicidad ≥ 80, 20% de dejar un regalo en la caja de envíos (semillas, gema rara).

**Fallo amable:** felicidad < 30 → "está de siesta" (no trabaja, da 0 Ánimos) y se sienta en la Hoguera con una burbuja de pensamiento que dice qué le falta. No hay abandono, muerte ni pérdida de progreso; un día bien atendido lo recupera (+25 tope de recuperación diaria).

---

## 4. Expediciones (cambios roguelite)

Se mantienen Mina y Cripta, 10 pisos, jefes en 5 y 10, puntos de control 1 y 6, prestigio que escala +25% vida/daño por rango (`Mazmorra.CrearMonstruo`).

**Estructura de una expedición:** entras desde el control (1 o 6) → 4–5 pisos hasta el jefe → sales por la escalera final o al desmayarte. Pensada para 6–10 min reales (de un día de ~14 min).

**Elección de salas:** en pisos sin jefe la sala final tiene **2 bajadas** con icono visible: *Tesoro* (cofre garantizado, +2 monstruos), *Farol* (sala de rescate o santuario de luz) o *Veta* (+50% rocas de mineral). Implementación: `GenerarPiso` ya calcula `PuntoEnPared`; se añade una segunda pared y el tipo se guarda en la semilla del piso siguiente (`$"{mazmorra}:{semilla}:{piso}:{rama}"`), lo que sigue siendo reproducible.

**Destellos (bendiciones temporales):** al vencer un jefe, abrir un cofre de mazmorra o entrar en un santuario de luz eliges 1 de 3. Se apilan, máx. 5, se pierden al salir. Pool inicial (10):
`+1 corazón`, `+1 daño`, `golpes de pico −1`, `+30% luz`, `curar ½ corazón por cada 5 monstruos`, `+1 tirada de botín del jefe`, `monstruos sueltan musgo_brillante`, `poción al 25% de vida (1 vez)`, `+15% velocidad`, `rocas de gema más probables`.
Los Destellos se leen en `Reglas.Efecto()` sumándolos a la ropa, así que el resto del código no cambia.

**Derrota (más amable que hoy):** ahora `Derrota` cobra 10% (máx. 300). Propuesta: sin multa de dinero; pierdes los Destellos y **el 50% de los minerales y musgo de esa expedición** (se guarda lo del inventario al entrar). Los residentes rescatados ya están a salvo. La energía queda a la mitad como hoy.

**Lazo con construcción:** nuevo item `musgo_brillante` (Glowmoss, venta 4): limo 30%, urna 50%, cofre de mazmorra (peso 25, 2–4), 3–6 por jefe. Es el material de la Forja, el Arco de musgo y las mejoras: obliga a bajar, pero en cantidades pequeñas.

---

## 5. Primer hito jugable (vertical slice)

Objetivo: en 3 días de juego el jugador rescata 2 Mosslings, les construye casa y comedor, y uno produce madera. Cinco features:

1. **Construir edificios** — `contenido.json`: nueva sección `construcciones` (`{id, objeto, coste, requiere}`), 4 objetos nuevos (`hoguera`, `casita_musgo`, `comedero`, `lenera`) + 2 decoraciones con `confort/belleza`; sprites con su descripción para PixelLab. Código: `Def.cs` (leer sección y campos), `Reglas.cs` (`case "construir"`, `case "mover"` de edificio → usar otro nombre, p. ej. `"reubicar"`, porque `"mover"` ya es caminar).
2. **Residentes** — `contenido.json`: sección `residentes` (2–4). `Estado.cs`: `List<Residente> residentes {id, casaUid, trabajoUid, felicidad, hablado}`, subir `Mundo.VERSION_ESTADO` a 2 y migrar en `Normalizar`. `Reglas.cs`: `case "asignar"`, hablar desde `Usar`, y un paso nuevo en `NuevoDia` (comer → felicidad → producir) con notas para el resumen del amanecer.
3. **Rescate en mazmorra** — objeto `farol_apagado` con `accion: "rescatar"` en `objetos`; colocación garantizada en `Mazmorra.GenerarPiso` (piso 3) y tras el jefe; `case "rescatar"` dentro del `switch (d.accion)` de `Usar`.
4. **Destellos** — sección `destellos` en `contenido.json`; `Jugador.destellos` (lista); `case "elegirDestello"`; `Efecto()` suma destellos; se limpian en `SalirDeMazmorra`/`Derrota`. Evento `{tipo:"destellos", items}` para que la UI abra el panel de 3 cartas.
5. **Musgo brillante** — item, sprite, entradas en `monstruos.limo.suelta`, `objetos.urna.suelta` y `botines.cofre_mazmorra`.

Fuera del slice: bifurcación de salas, Ánimos/Tablón, Cantera/Forja/Torre, Nivel de aldea, derrota sin multa (se ajusta después de jugar).

Observación técnica: en `Usar` (rama `colocable`) se comprueba `Mundo.Libre` solo 1×1 y no se mira si `Mundo.Colocar` devolvió `null` antes de `Inventario.GastarMano`. Con objetos de más de 1×1 podría gastarse el item sin colocarse; `construir` debe comprobar el retorno.

---

## 6. Riesgos y preguntas de balance

**Riesgos**
- **Dos núcleos:** `js/nucleo` (web) y C# deben seguir iguales o la web queda congelada. Decidir si la web deja de recibir funciones.
- **Espacio:** la granja es 48×36 con el claro inicial limitado (`EnClaro`). 11 edificios de 2×2 + decoración pueden competir con el campo. ¿Zona de aldea dedicada (p. ej. x 27–40)?
- **Guardado:** cambia `Estado`; hace falta migración y probar cargar partidas viejas (último commit ya trata pérdidas al cargar).
- **Multijugador/botín por jugador:** ¿los residentes son de la granja (compartidos) y los Destellos por jugador? Propuesta: sí, así.
- **Arte:** ~12 sprites nuevos de edificio + Mosslings con 4 direcciones en PixelLab; coste de generación.
- **Alcance de UI:** paneles de construir, residentes y Destellos son uGUI por código; es la parte más cara del slice.

**Preguntas de balance**
1. ¿6 madera/día por residente desbalancea las cercas/cofres? Con 2 leñeros = 12/día ≈ 2 árboles; parece sano frente a ~100 madera por día de tala manual con toda la energía.
2. Comida: 6 residentes comen 6 cultivos/día ≈ 270–1.320 monedas/día no vendidas. ¿Es demasiado sumidero al principio? Alternativa: comen cada 2 días.
3. ¿Los Destellos hacen trivial el rango 1? Probar con prestigio 0 y 2; tal vez el pool se amplía por rango.
4. ¿Quitar la multa de derrota elimina tensión? Medir cuántas veces se desmaya un jugador en la Cripta.
5. Coste de la Hoguera (20/20) el día 1: el inventario inicial no trae madera; ¿regalar 20 madera en `inicio` o entrar por el primer rescate?
6. ¿Ritmo de rescates? Meta: 2 residentes el día 3, 6 el día 14, 12 hacia el día 30.
