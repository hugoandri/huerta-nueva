#!/usr/bin/env python3
"""
Iguala el tamaño de la pose quieta con el de la caminata (técnica de "content bounds" de
sprite-sheet-creator: medir el contenido visible de cada cuadro y anclarlo abajo al centro).

PixelLab entrega la rotación (<dir>.png) algo más grande que los cuadros de caminar, y achicarla
estropea el pixel art. Por eso la pose quieta pasa a ser el cuadro más neutro de la propia
caminata (piernas juntas: el más angosto y, a igualdad, el más alto). Así todos los cuadros miden lo
mismo y se mantienen nítidos.

  python herramientas/igualar_escala.py assets/personaje/<id> assets/personaje/<id>/ropa_* ...
  python herramientas/igualar_escala.py --informe <carpetas>     # solo mide
"""
import os
import sys
from PIL import Image

DIRS = ['south', 'west', 'east', 'north']


def contenido(im):
    """Caja del contenido visible (alfa > 10), como getContentBounds de sprite-sheet-creator."""
    return im.getchannel('A').point(lambda a: 255 if a > 10 else 0).getbbox()


def cuadros_caminar(carpeta, d):
    i, res = 0, []
    while os.path.exists(os.path.join(carpeta, f'caminar_{d}_{i}.png')):
        res.append(os.path.join(carpeta, f'caminar_{d}_{i}.png'))
        i += 1
    return res


def neutro(rutas):
    def clave(r):
        x0, y0, x1, y1 = contenido(Image.open(r).convert('RGBA'))
        return (x1 - x0, -(y1 - y0))
    return min(rutas, key=clave)


def informe(carpeta):
    for d in DIRS:
        q = contenido(Image.open(os.path.join(carpeta, f'{d}.png')).convert('RGBA'))
        alt = [contenido(Image.open(r).convert('RGBA')) for r in cuadros_caminar(carpeta, d)]
        print(f'{os.path.relpath(carpeta)} {d}: quieto {q[3] - q[1]}px  caminando {[b[3] - b[1] for b in alt]}')


def igualar(carpeta):
    for d in DIRS:
        rutas = cuadros_caminar(carpeta, d)
        if not rutas:
            continue
        Image.open(neutro(rutas)).convert('RGBA').save(os.path.join(carpeta, f'{d}.png'))


if __name__ == '__main__':
    args = [a for a in sys.argv[1:] if not a.startswith('--')]
    for c in args:
        (informe if '--informe' in sys.argv else igualar)(c)
