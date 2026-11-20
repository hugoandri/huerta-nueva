# Arquitectura de Huerta Nueva

El juego es offline, pero está armado para pasar a online sin reescribirlo. La regla principal: **la interfaz nunca cambia el estado; manda acciones**.

```
 entrada (teclado, toque) ─▶ acción ─▶ Sesión ─▶ reglas del núcleo ─▶ estado nuevo + eventos
                                         │                                     │
                                      Almacén                         render · interfaz
```

## Capas

| Carpeta | Qué hace | Depende del navegador |
| --- | --- | --- |
| `js/nucleo/` | Estado, reglas, azar reproducible, botín. Funciones puras. | No: corre en Node y podrá correr en un servidor |
| `js/servicios/` | Sesión (aplica acciones), almacén (guarda), perfil (identidad) | Sí, pero detrás de interfaces intercambiables |
| `js/cliente/` | Gráficos, dibujo, interfaz HTML, entrada | Sí |
| `data/contenido.json` | Todo el contenido: items, cultivos, objetos, recetas, botines, prompts de PixelLab | — |
| `herramientas/` | Generador de gráficos con PixelLab | Node |

## Núcleo (`js/nucleo/`)

- **`mundo.js`**: el estado es JSON plano: `tiempo`, `granja` (con su `dueno`) y `jugadores` (varios desde ya). La granja se genera a partir de una semilla.
- **`reglas.js`**: `aplicar(estado, def, accion) → eventos`. Las acciones son `mover`, `mirar`, `usar`, `seleccionar`, `moverInv`, `comer`, `fabricar`, `comprar`, `cofreSacar`, `cofreGuardar`, `dormir` y `tiempo`. Cada regla valida lo que recibe: distancia a la casilla, materiales, dinero y espacio. Una acción falsa o repetida no rompe nada.
- **`azar.js`**: el núcleo nunca usa `Math.random`. Con la misma semilla y las mismas acciones, el resultado es idéntico.
- **`botin.js`**: `repartirBotin(tabla, jugadores, semilla)`. Cada jugador sortea su propio botín (botín propio). Es reproducible y no depende del orden de los jugadores.
- **`serializar.js`**: guardar y cargar con `version`, descartando contenido que ya no exista.

Los eventos (`aviso`, `flotar`, `abrir`, `resumen`, `sacudir`) llevan `jugador` cuando son solo para uno; la interfaz muestra los suyos.

## Cómo se pasa a online

| Hoy (offline) | Mañana (online) | Qué cambia |
| --- | --- | --- |
| `AlmacenLocal` (localStorage) | `AlmacenNube` (por ejemplo, una tabla en Supabase) | Un archivo nuevo con la misma interfaz `cargar/guardar/borrar/existe` |
| `obtenerPerfil()` con un id aleatorio | Cuenta de usuario (Supabase Auth) | El id sale de la cuenta; todo usa `perfil.id` |
| `SesionLocal` aplica la acción | `SesionRed`: aplica la acción localmente (respuesta inmediata) y la envía al servidor o al anfitrión; recibe las acciones de los demás | Un archivo nuevo con la misma interfaz `despachar/alEvento/estado/yo` |
| El cliente reparte el botín | El servidor ejecuta `repartirBotin` y escribe cada inventario | La función es la misma |

**Visitas a la granja:** el estado ya admite varios `jugadores` en una granja con `dueno`, y el dibujo muestra a todos con su nombre.

**Mazmorras en equipo:** se generan desde una semilla compartida (todos ven el mismo mapa sin enviarlo). El anfitrión aplica las acciones de los monstruos y el servidor reparte el botín.

## Pruebas

```bash
npm test
```

Prueban el núcleo sin navegador: que la granja sea reproducible, el ciclo arar → sembrar → regar → cosechar, la validación contra trampas, el crafteo, la tienda, las colisiones, el azar reproducible, varios jugadores, el reparto del botín y el guardado.
