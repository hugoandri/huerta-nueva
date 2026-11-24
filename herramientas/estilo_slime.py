#!/usr/bin/env python3
"""
Lleva un sprite al estilo del slime (assets/sprites/limo.png): pocos colores, colores vivos y un
contorno oscuro continuo teñido del color vecino.

  python herramientas/estilo_slime.py <png o carpeta> [...]     # modifica los PNG en su sitio
  python herramientas/estilo_slime.py --prueba entrada.png salida.png

Medido en los monstruos que ya tienen el estilo: 8-15 colores y 70-100 % del borde oscuro.
"""
import colorsys
import os
import sys
from PIL import Image

COLORES = int(os.environ.get('ESTILO_COLORES', 24))         # colores máximos (sin contar el contorno)
SATURACION = float(os.environ.get('ESTILO_SATURACION', 1.1)) # realce de saturación
OSCURIDAD = float(os.environ.get('ESTILO_OSCURIDAD', 0.32))  # brillo del contorno respecto al color vecino


def quitar_sombra(im):
    """Borra la sombra gris que PixelLab pinta bajo los pies (el juego dibuja la suya)."""
    w, h = im.size
    px = im.load()
    def gris(c):
        r, g, b, a = c
        return a and max(r, g, b) - min(r, g, b) < 26 and 60 < (r + g + b) / 3 < 240
    pila = [(x, y) for y in range(int(h * 0.8), h) for x in range(w) if gris(px[x, y])]
    vistos = set()
    while pila:
        x, y = pila.pop()
        if (x, y) in vistos or not (0 <= x < w and 0 <= y < h) or y < h * 0.8 or not gris(px[x, y]):
            continue
        vistos.add((x, y))
        px[x, y] = (0, 0, 0, 0)
        pila += _vecinos4(x, y)
    return im


def _vecinos4(x, y):
    return ((x + 1, y), (x - 1, y), (x, y + 1), (x, y - 1))


def estilizar(im):
    im = im.convert('RGBA')
    w, h = im.size
    px = im.load()
    # alfa binario: el pixel art no lleva semitransparencias
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            px[x, y] = (r, g, b, 255) if a >= 128 else (0, 0, 0, 0)
    quitar_sombra(im)

    # 1) realce de saturación
    for y in range(h):
        for x in range(w):
            r, g, b, a = px[x, y]
            if not a:
                continue
            hh, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
            r2, g2, b2 = colorsys.hsv_to_rgb(hh, min(1, s * SATURACION), v)
            px[x, y] = (round(r2 * 255), round(g2 * 255), round(b2 * 255), 255)

    # 2) pocos colores (solo los píxeles opacos)
    rgb = Image.new('RGB', (w, h), (0, 0, 0))
    rgb.paste(im, mask=im.getchannel('A'))
    q = rgb.quantize(colors=COLORES, method=Image.Quantize.FASTOCTREE, dither=Image.Dither.NONE).convert('RGB')
    qp = q.load()
    for y in range(h):
        for x in range(w):
            if px[x, y][3]:
                px[x, y] = qp[x, y] + (255,)

    # 3) contorno oscuro continuo: el anillo exterior de la figura toma el color vecino oscurecido
    fuera = lambda x, y: not (0 <= x < w and 0 <= y < h) or px[x, y][3] == 0
    borde = [(x, y) for y in range(h) for x in range(w)
             if px[x, y][3] and any(fuera(nx, ny) for nx, ny in _vecinos4(x, y))]
    nuevos = {}
    for x, y in borde:
        interior = [px[nx, ny] for nx, ny in _vecinos4(x, y) if not fuera(nx, ny) and (nx, ny) not in borde]
        r, g, b, _ = interior[0] if interior else px[x, y]
        hh, s, v = colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)
        r2, g2, b2 = colorsys.hsv_to_rgb(hh, min(1, s * 0.9 + 0.1), min(v, 1) * OSCURIDAD)
        nuevos[(x, y)] = (round(r2 * 255), round(g2 * 255), round(b2 * 255), 255)
    for (x, y), c in nuevos.items():
        px[x, y] = c
    return im


def procesar(ruta):
    rutas = []
    if os.path.isdir(ruta):
        rutas = [os.path.join(ruta, f) for f in sorted(os.listdir(ruta)) if f.endswith('.png')]
    elif ruta.endswith('.png'):
        rutas = [ruta]
    for r in rutas:
        estilizar(Image.open(r)).save(r)
    return len(rutas)


if __name__ == '__main__':
    args = sys.argv[1:]
    if args and args[0] == '--prueba':
        estilizar(Image.open(args[1])).save(args[2])
    else:
        print('Estilizados:', sum(procesar(a) for a in args))
