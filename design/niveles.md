# Mosslantern: diseño de niveles (granja, comunidad y mazmorras)

> Rol: level-designer. Estado: **Concepto/Layout**. Basado en `Mundo.cs`, `Mazmorra.cs`, `Reglas.cs` y `data/contenido.json` (rama actual).
> Datos base: mapa 48×36 celdas, `T = 32 px`, `VELOCIDAD = 112 px/s` = **3,5 celdas/s**. Un día dura (26h−6h)/(10/7) = **840 s reales (14 min)**, así que 10 s andando ≈ 14 min de juego.

---

## 1. La granja

### 1.1 Granja actual (1 carácter = 2×2 celdas)

```
      x: 0   4   8  12  16  20  24  28  32  36  40  44
y 0   ^^^^^^^^^^^^^^^^^^^^^^^^   acantilado (filas 0-1) + árboles en y=1
y 2   T..........C..:::::::::T   C cueva (22,2)
y 4   T.HHH......=..:::::::::T   H casa (3,5) 6x3
y 6   T.HHH$PP...=:::::::::::T   $ caja (10,7)  P puesto (12,6)
y 8   T.=@========:::::::::::T   @ inicio (6, 8.6)
y10   T.==========:::::::::::T   = camino (x22-23 y3-10; y9-10 x5-23)
y12   T::::::::::::::::::::::T
y18   T:::::::::::::::~~~::::T   ~ estanque, elipse centro (35,23) rx6 ry4.2
y22   T:::::::::::::~~~~~~~::T
y26   T:::::::::::::::~~~::::T
y32   T::::::::::::::::::::::T   : 26 árboles + 45 rocas + 90 malezas al azar
y34   TTTTTTTTTTTTTTTTTTTTTTTT
```

**Crítica**
- **Todo lo útil está en la esquina noroeste.** `EnClaro` solo protege x2-17/y2-11 y x19-26/y2-6. El ~75 % del mapa es «ruido» uniforme de recursos, sin zonas ni intención.
- **No hay sitio para construir una comunidad.** El único hueco limpio es el claro inicial, que compiten huerto, casa, caja y puesto.
- **El rebrote diario se come lo que despejas.** `NuevoDia` pone 4 objetos por día en cualquier celda libre que no sea claro ni camino. Una zona que el jugador limpia para construir se vuelve a llenar sola.
- **El estanque está aislado.** Queda a unos 28 celdas de la casa (≈8 s), sin camino ni motivo para ir.
- **Los caminos no llevan a ningún sitio**, salvo de la casa a la cueva. No hay plaza ni nodo central y la lectura espacial es débil.
- **El inicio (6, 8.6) cae sobre el camino**, pegado a la casa. Está bien, pero no hay un «frente de casa» reconocible.

### 1.2 Propuesta: aldea en anillos

```
      x: 0   4   8  12  16  20  24  28  32  36  40  44
y 0   ^^^^^^^^^^^^^^^^^^^^^^^^
y 2   T.......*..C...mmmmmmmmT   * mirador del acantilado (16,2)
y 4   THHHH......=...mmmmmmmmT   m monte silvestre (rebrote solo aquí)
y 6   THHHH$.....=...mmmmmmmmT
y 8   T.=@==bP====...mmmmmmmmT   b tablón de encargos  P puesto
y10   T.=====o====...mmmmmmmmT   o farol/fuente de la plaza (14,10)
y12   Tfffff.|111222.mmmmmmmmT   f huerto fase 0   | avenida x14-15
y14   Tfffff.|111222.mmmmmmmmT   1 2 solares fase 1
y16   Tfffff.|333444.mmmmmmmmT   3 4 solares fase 2
y18   TFFFFF.|333444..~~~....T   F huerto fase 1
y20   TFFFFF.|======d~~~~~~..T   d muelle (28,21), ramal y21-22
y22   T*======.......~~~~~~~*.T  calle oeste y23; * rincones decorativos
y24   T555666|WWWWWW.~~~~~~..T   5 6 solares fase 3   W talleres
y26   T555666|WWWWWW..~~~....T
y28   T777888|WWWWWW.........T   7 8 solares fase 4
y30   T777888|WWWWWW....*....T
y32   TmmmmmmmmmmmmmmmmmmmmmmT
y34   TTTTTTTTTTTTTTTTTTTTTTTT
```

| Zona | Celdas (x, y) | Función |
|---|---|---|
| Hogar | casa (3,5) y patio x2-10 / y2-8 | Sin cambios, para no romper `CASA` ni `PosicionInicio` |
| Plaza | x11-18 / y7-12 | Caja (10,7), puesto movido a (15,8), tablón (13,8), farol 2×2 (14,10). Nodo central |
| Huerto | x2-11 / y12-17 (60 celdas); fase 1 hasta y21 | Cultivo, siempre despejado |
| Barrio | 8 solares de 6×5 (S1 (16,12), S2 (22,12), S3 (16,16), S4 (22,16), S5 (2,24), S6 (8,24), S7 (2,28), S8 (8,28)) | Casa del residente 3×2 + jardín |
| Talleres | x16-28 / y24-31 | Huecos de 4×3 para horno, telar, carpintería… |
| Estanque | elipse actual | Se mantiene. Se añaden muelle y ramal |
| Monte | x30-46 / y3-17 y la franja y32-33 | Recursos: el rebrote diario va **solo aquí** |
| Cueva | (22,2) | Sin cambios (`ENTRADA_CUEVA`, `FrenteACueva`) |

**Qué cambia en `Mundo.cs`**
1. Sustituir `EnClaro()` por una lista de rectángulos `Zona { id, x, y, w, h, tipo, fase }` leída de `contenido.json → mundo.zonas`. Habrá funciones auxiliares `EsZona(x, y, "huerto"|"barrio"|...)` y `EsMonte(x, y)`.
2. `CeldasCamino()` añade la avenida (x14-15, y11-31), el ramal al muelle (y21-22, x16-28) y la calle oeste (y23, x2-13).
3. En `CrearGranja`, `Esparcir` solo coloca dentro del monte y de las zonas bloqueadas (todavía sin desbloquear). Las zonas bloqueadas salen **cubiertas de maleza y troncos**, que se limpian al desbloquearlas.
4. En `NuevoDia`, el rebrote usa `EsMonte(x, y)` en lugar de `!EnClaro`.
5. Mover `PUESTO` a (15,8). El guardado existente ya tiene su granja serializada: aplicar el nuevo layout **solo a partidas nuevas**, o migrar subiendo `VERSION_ESTADO` y moviendo los objetos que caigan en la plaza.

## 2. Crecimiento y distancias

**Expansiones** (cada una limpia su rectángulo con una breve animación: «los vecinos despejan el solar»):

| Fase | Requisito | Desbloquea |
|---|---|---|
| 0 | Inicio | Huerto f, plaza y solares 1-2 visibles pero con maleza |
| 1 | Primer residente rescatado (mina, piso 2-3) | Solares 1-2, huerto F |
| 2 | Vencer al Rey Limo (mina, piso 5) | Solares 3-4 y primeros talleres |
| 3 | Entrar en la Cripta | Solares 5-6, resto de talleres |
| 4 | Vencer al Guardián (mina, piso 10) | Solares 7-8 y rincones decorativos del sur |

**Legibilidad**
- Cada zona tiene su suelo: camino de tierra, plaza empedrada y solar con cerca baja en el borde.
- Los solares bloqueados muestran un **cartel con el requisito**.
- La plaza se ve desde la puerta de casa y el farol sirve de punto de referencia nocturno.

**Distancias** (a 3,5 celdas/s, por el camino):

| Desde el inicio (6, 8.6) hasta | Celdas | Segundos |
|---|---|---|
| Plaza (14,10) | 8 | 2,3 |
| Centro del huerto (6,15) | 6 | 1,8 |
| Cueva (22,3), por el camino | 23 | 6,6 |
| Solar S1 (19,14) | 14 | 4,0 |
| Muelle (28,21) | 34 | 9,7 |
| Solar S8 (10,30), por la calle oeste | 24 | 6,9 |

**Regla de diseño:** lo diario (huerto, caja, cueva) a ≤7 s de casa, y lo semanal (muelle, talleres lejanos) a ≤12 s. Hoy, cruzar la granja de esquina a esquina son unos 14 s sin camino.

## 3. Mazmorra con estructura roguelite

**Base que se reutiliza:** salas y pasillos de `GenerarPiso`, `PuntoEnPared` para escaleras y `salaFinal` = la sala más lejana.

**Tipos de sala** (se añade un campo `tipo` a `Sala` y un array `z.tipoSala` paralelo a `enSala`):

| Tipo | Contenido | Regla |
|---|---|---|
| Inicio | Escalera, antorcha | Sin monstruos ni rocas a menos de 8 celdas de `z.entrada` |
| Combate | Presupuesto de enemigos (ver §4) y rocas | Es el tipo por defecto |
| Hoguera | Fogata que cura hasta el máximo al interactuar | Garantizada en los pisos 4 y 9 |
| Rescate | Un residente atrapado entre rocas: picas 3 rocas y te sigue a la salida | Pisos 2, 3, 7 y 8; alimenta el barrio |
| Tesoro | `cofre_mazmorra` y rocas de gema | Es el 60 % actual, convertido en sala propia |
| Santuario | Elegir 1 de 2 bendiciones para la expedición (+1 corazón, +20 % de daño, luz más amplia…) | 1 cada 2-3 pisos |
| Jefe | Solo el jefe | Pisos 5 y 10 |

**Cómo se eligen:** después de cavar, se ordenan las salas por distancia **de recorrido** (BFS sobre `muro == 0`) desde el inicio. La primera es el inicio y la última el final (jefe o bajadas). La sala especial que el jugador eligió en el piso anterior va a mitad de recorrido. El resto son de combate, salvo una de tesoro si sale la tirada. Hoy los pasillos se conectan en el orden de generación (`salas[i-1] → salas[i]`), lo que provoca cruces caóticos. Conviene ordenar antes las salas por x+y o conectar cada una con la más cercana ya unida (MST simple), para que el recorrido se lea mejor.

**Elección entre 2 puertas:** en los pisos sin jefe, la sala final tiene **dos escaleras de bajada** (dos `PuntoEnPared`, o la sala final y la penúltima más lejana). Cada una lleva un icono con la sala especial del piso siguiente (hoguera, rescate, tesoro o santuario). Al bajar se guarda `e.mazmorra.siguienteEspecial`, que `GenerarPiso` recibe como parámetro. Con la misma semilla se generan los mismos pisos, así que la reproducibilidad se mantiene.

**Ritmo (Mina; Cripta igual con `escala_base` 1,6):**

| Piso | Salas | Presupuesto | Enemigos | Especial | Notas |
|---|---|---|---|---|---|
| 1 | 6 | 3 | limo | tesoro | Tutorial; punto de control |
| 2 | 7 | 4 | limo, murciélago | rescate | Primer residente |
| 3 | 7 | 5 | limo, murciélago | elección | |
| 4 | 8 | 6 | limo, murciélago | **hoguera** | Pre-jefe; nuevo punto de control |
| 5 | 8 | 0 | Rey Limo | jefe | Sala de jefe vacía; salida al ganar |
| 6 | 9 | 6 | murciélago, esqueleto | santuario | Punto de control; respiro tras el jefe |
| 7 | 9 | 8 | mixto | rescate | |
| 8 | 10 | 9 | mixto | elección | |
| 9 | 10 | 10 | mixto | **hoguera** | Nuevo punto de control |
| 10 | 11 | 0 | Guardián | jefe | |

Propuesta: `puntos_de_control: [1, 4, 6, 9]`. Así nunca se repiten más de 2 pisos antes de un jefe.

**Ejemplo de piso 3** (las dos bajadas anuncian lo que viene):

```
 #################################################
 #  [S]======[C1]=========[R]          #####      #
 #  inicio   2 limos       tesoro      #   #      #
 #    ||        ||                     #   #      #
 #    ||     [C2]=====[SANT]======[C3]==[F]       #
 #    ||     limo+murc  elige 1 de 2   murc  ^ ^   #
 #  (pasillo                                 | |   #
 #   sin enemigos)              hoguera <----+ +---> rescate
 #################################################
```

## 4. Diseño de encuentros

**Monstruos** (vida / daño en medios corazones / velocidad en px/s / visión en celdas). El jugador tiene 10 medios corazones.

| Monstruo | Vida | Daño | Velocidad | Visión | Papel |
|---|---|---|---|---|---|
| limo | 3 | 1 | 42 | 5 | Lento, enseña a golpear |
| murciélago | 2 | 1 | 72 | 7 | Rápido, vuela; obliga a reaccionar |
| esqueleto | 6 | 2 | 50 | 6 | Tanque; aguanta varios golpes |
| fantasma | 4 | 2 | 58 | 8 | Vuela y ve lejos |
| Rey Limo / Guardián | 30 / 60 | 2 / 3 | 46 / 44 | 9 / 10 | Jefes |

**Mezclas:**
- Coste por enemigo: limo y murciélago 1; esqueleto y fantasma 2.
- Como máximo 3 enemigos por sala y como máximo 1 de coste 2 por sala hasta el piso 6.
- Las parejas clásicas son «lento + rápido» (limo + murciélago) y «tanque + rápido» (esqueleto + murciélago).
- **Cripta, piso 1:** hoy salen esqueletos y fantasmas (daño 2) con escala 1,6 desde el primer piso, un pico brusco. Propuesta: en los pisos 1-2 de la Cripta, como máximo 1 fantasma y el resto esqueletos lentos.

**Telegrafía:** hoy el golpe es instantáneo en cuanto `dist ≤ 16 px` y `cd ≤ 0`. Propuesta: una carga de 0,35 s (0,6 s en jefes) con parpadeo blanco y un «!». El golpe solo conecta si el jugador sigue en rango al terminar la carga. Los jefes anuncian su embestida con una sombra en el suelo.

**Amabilidad:**
- La escala actual sube la vida un 15 % por piso, pero no el daño. Es correcto y conviene mantenerlo.
- Sin línea de visión, un monstruo de la sala vecina te detecta a través de la pared y se queda pegado a ella. Propuesta: exigir LOS (comprobar `muro` en la línea) o al menos que esté en la misma sala o pasillo.
- Tras recibir un golpe, 0,8 s de invulnerabilidad del jugador.

**Problemas concretos en `Mazmorra.cs`:**
1. **¿Pueden aparecer monstruos en la sala de inicio? No.** `otras` excluye `inicio`. Pero las salas solo están separadas 2 celdas, así que un monstruo puede aparecer a 3-4 celdas de `z.entrada`. Solución: descartar posiciones a menos de 8 celdas de `z.entrada`.
2. **Aparecen monstruos normales en la sala del jefe.** Las rocas comprueban `s == final && jefe != null`, pero el bucle de monstruos no. Solución: la misma condición.
3. **Monstruos apilados:** solo se comprueba que no haya objetos en la celda, no otros monstruos. Solución: comprobar `z.monstruos` a menos de 1 celda.
4. **Tipos de monstruo elegidos de forma uniforme** (`tipos[EntreR(...)]`). Solución: usar `Ponderado` con un peso por tipo, igual que en las rocas.
5. El cálculo de `final` usa distancia euclídea. Solución: BFS sobre el recorrido real (ver §3).

## 5. Cambios de código por valor/esfuerzo

| # | Cambio | Valor | Esfuerzo |
|---|---|---|---|
| 1 | Sin monstruos en la sala del jefe ni a menos de 8 celdas de la entrada; sin apilarlos (`Mazmorra.cs`) | Alto | Muy bajo |
| 2 | Rebrote de `NuevoDia` solo en el monte (`EsMonte`) | Alto | Bajo |
| 3 | Carga de 0,35 s antes del golpe e invulnerabilidad del jugador (`Reglas.ActualizarMazmorra`) | Alto | Bajo |
| 4 | `puntos_de_control [1,4,6,9]` e hoguera en los pisos 4 y 9 (datos y un objeto `hoguera`) | Alto | Bajo |
| 5 | Zonas de la granja guiadas por datos (`mundo.zonas`) y expansiones por fase | Muy alto | Medio |
| 6 | Presupuesto de enemigos por sala y pesos por tipo | Medio | Bajo |
| 7 | Tipos de sala, BFS y conexión por vecinos | Alto | Medio |
| 8 | Dos bajadas con elección de sala especial (`siguienteEspecial`) | Alto | Medio |
| 9 | Sala de rescate con residente que se muda a un solar | Muy alto | Alto |
| 10 | Santuario con bendiciones temporales para la expedición | Medio | Medio |
| 11 | Línea de visión para la persecución | Medio | Medio |
