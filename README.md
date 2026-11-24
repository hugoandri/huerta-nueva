# Huerta Nueva

Prototipo de juego de granja (HTML5 canvas, sin dependencias). Todo el contenido se define en `data/contenido.json` y los gráficos se generan con la API de [PixelLab](https://www.pixellab.ai).

## Jugar

Sirve la carpeta con cualquier servidor estático y abre `index.html`:

```bash
npx serve .
```

Controles: `WASD`/flechas para caminar, `Espacio` o clic para usar, `1`–`0` para elegir objeto, `I` mochila, `C` crafteo, `F` comer.

### Versión Unity 6

El proyecto completo en C# está en [`unity/`](unity/LEEME.md). En Unity Hub usa **Add → Add project from disk**, selecciona esa carpeta y ábrela con **Unity 6000.6.9f1**. Abre `Assets/Scenes/SampleScene.unity` y pulsa **Play**. Incluye la configuración de URP 2D, paquetes, gráficos e interfaz.

## Cómo está armado

| Archivo | Qué contiene |
| --- | --- |
| `data/contenido.json` | Sprites (con su prompt), items, cultivos, objetos del mundo, recetas, botines, tienda e inventario inicial |
| `js/nucleo/` | Reglas del juego, sin navegador: estado, acciones, azar reproducible, botín |
| `js/servicios/` | Sesión, guardado y perfil (intercambiables por versiones online) |
| `js/cliente/` | Gráficos, dibujo (autotiling Wang), interfaz y controles |
| `herramientas/generar-assets.mjs` | Llama a PixelLab y escribe `assets/` y `assets/manifest.json` |
| `assets/` | Gráficos generados. Lo que falte se dibuja provisional |
| `pruebas/` | Pruebas del núcleo (`npm test`) |
| `unity/` | Versión Unity 6 (C#): núcleo, sesión, Tilemaps, luz 2D y uGUI por código |
| `herramientas/unity/sincronizar.py` | Copia `contenido.json` y los gráficos al proyecto de Unity |

La arquitectura y el camino a online están en [ARQUITECTURA.md](ARQUITECTURA.md).

Qué endpoint de PixelLab genera cada cosa:

- **Mundo**: `POST /create-tileset` crea tilesets Wang de 16 tiles (césped→tierra arada, césped→agua). El juego los dibuja con una rejilla doble: cada tile une las esquinas de 4 celdas.
- **Personajes** (granjera y granjero): `POST /create-character-with-4-directions` + `POST /animate-character` (plantilla `walking`), descargados con `GET /characters/{id}/zip`.
- **Ropas**: `POST /create-character-state` crea una variante del mismo personaje con otra ropa, que después se anima igual.
- **Objetos, cultivos, monstruos e íconos**: `POST /create-image-pixflux` con fondo transparente. Las construcciones usan vista `low top-down` con sombreado detallado y `"recortar": true`, que quita los márgenes vacíos.

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
