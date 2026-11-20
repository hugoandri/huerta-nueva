# Huerta Nueva

Prototipo de juego de granja (HTML5 canvas, sin dependencias). Todo el contenido se define en `data/contenido.json` y los gráficos se generan con la API de [PixelLab](https://www.pixellab.ai).

## Jugar

Sirve la carpeta con cualquier servidor estático y abre `index.html`:

```bash
npx serve .
```

Controles: `WASD`/flechas para caminar, `Espacio` o clic para usar, `1`–`0` para elegir objeto, `I` mochila, `C` crafteo, `F` comer.

## Cómo está armado

| Archivo | Qué contiene |
| --- | --- |
| `data/contenido.json` | Sprites (con su prompt), items, cultivos, objetos del mundo, recetas, tienda e inventario inicial |
| `herramientas/generar-assets.mjs` | Llama a PixelLab y escribe `assets/` y `assets/manifest.json` |
| `juego.js` | Motor: mundo, autotiling Wang, cultivos, días, energía, crafteo, guardado |
| `assets/` | Gráficos generados. Lo que falte se dibuja provisional |

Qué endpoint de PixelLab genera cada cosa:

- **Mundo**: `POST /create-tileset` crea tilesets Wang de 16 tiles (césped→tierra arada, césped→agua). El juego los dibuja con una rejilla doble: cada tile une las esquinas de 4 celdas.
- **Personaje**: `POST /create-character-with-4-directions` + `POST /animate-character` (plantilla `walking`), descargado con `GET /characters/{id}/zip`.
- **Objetos, cultivos e íconos**: `POST /create-image-pixflux` con fondo transparente.

## Agregar contenido nuevo

1. En `data/contenido.json`, agrega un sprite con su descripción para PixelLab:
   ```json
   "fresa": { "descripcion": "ripe red strawberry", "ancho": 32, "alto": 32 }
   ```
2. Úsalo en un item, un cultivo, un objeto o una receta.
3. Genera solo lo que falta:
   ```bash
   PIXELLAB_API_KEY=tu_token node herramientas/generar-assets.mjs --lista   # ver qué va a generar, sin gastar
   PIXELLAB_API_KEY=tu_token node herramientas/generar-assets.mjs
   ```
   `--solo=id1,id2 --forzar` regenera sprites concretos.

Opciones de tileset: `"referencia_inferior": "agua"` usa el césped de otro tileset como referencia para que combinen; `"invertir": true` corrige un tileset que PixelLab pintó con los terrenos al revés.

`assets/pixellab-ids.json` guarda los ids de PixelLab para retomar una generación sin pagarla dos veces.
