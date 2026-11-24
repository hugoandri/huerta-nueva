#!/usr/bin/env python3
"""Copia el contenido y los gráficos de PixelLab al proyecto de Unity.

  python3 herramientas/unity/sincronizar.py

Escribe en unity/Assets/HuertaNueva/Resources/HuertaNueva/:
  contenido.json                         (el mismo de data/, una sola fuente de verdad)
  sprites/<id>.png
  tilesets/<id>/<0..15>.png              (los tilesets vienen como data URL dentro de un JSON)
  personajes/<cuerpo>/<aspecto>/<dir>.png y caminar_<dir>_<i>.png   (aspecto: base, minera, aventurera…)
  graficos.json                          (qué hay y dónde apoya el pie cada personaje)
Vuelve a ejecutarlo cada vez que generes gráficos nuevos.
"""
import base64, io, json, os, shutil, sys
from PIL import Image

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..', '..'))
DEST = os.path.join(RAIZ, 'unity', 'Assets', 'HuertaNueva', 'Resources', 'HuertaNueva')
DIRS = ['south', 'west', 'east', 'north']


def pie(ruta):
    im = Image.open(ruta).convert('RGBA'); w, h = im.size; a = im.getchannel('A').load()
    for y in range(h - 1, -1, -1):
        if any(a[x, y] > 40 for x in range(w)): return y + 1
    return round(h * 0.85)


def main():
    m = json.load(open(os.path.join(RAIZ, 'assets', 'manifest.json')))
    if os.path.isdir(DEST):
        for n in ('sprites', 'tilesets', 'personajes'): shutil.rmtree(os.path.join(DEST, n), ignore_errors=True)
    os.makedirs(DEST, exist_ok=True)
    shutil.copy(os.path.join(RAIZ, 'data', 'contenido.json'), os.path.join(DEST, 'contenido.json'))
    info = {'sprites': [], 'tilesets': [], 'personajes': {}}

    os.makedirs(os.path.join(DEST, 'sprites'))
    for id_, ruta in m.get('sprites', {}).items():
        src = os.path.join(RAIZ, ruta)
        if os.path.exists(src): shutil.copy(src, os.path.join(DEST, 'sprites', id_ + '.png')); info['sprites'].append(id_)

    for id_, ruta in m.get('tilesets', {}).items():
        j = json.load(open(os.path.join(RAIZ, ruta)))
        d = os.path.join(DEST, 'tilesets', id_); os.makedirs(d)
        for i, url in enumerate(j['tiles']):
            Image.open(io.BytesIO(base64.b64decode(url.split(',', 1)[1]))).save(os.path.join(d, f'{i}.png'))
        info['tilesets'].append(id_)

    for cuerpo, pj in m.get('personajes', {}).items():
        juegos = {'base': pj, **{r: v for r, v in pj.get('ropas', {}).items()}}
        info['personajes'][cuerpo] = {}
        for aspecto, v in juegos.items():
            src = os.path.join(RAIZ, v['carpeta']); d = os.path.join(DEST, 'personajes', cuerpo, aspecto); os.makedirs(d)
            if not all(os.path.exists(os.path.join(src, f'{x}.png')) for x in DIRS): continue
            cuadros = {}
            for x in DIRS:
                shutil.copy(os.path.join(src, f'{x}.png'), os.path.join(d, f'{x}.png'))
                n = 0
                while os.path.exists(os.path.join(src, f'caminar_{x}_{n}.png')):
                    shutil.copy(os.path.join(src, f'caminar_{x}_{n}.png'), os.path.join(d, f'caminar_{x}_{n}.png')); n += 1
                cuadros[x] = n
                for pref in ('idle', 'golpear', 'talar', 'arar', 'cosechar'):
                    k = 0
                    while os.path.exists(os.path.join(src, f'{pref}_{x}_{k}.png')):
                        shutil.copy(os.path.join(src, f'{pref}_{x}_{k}.png'), os.path.join(d, f'{pref}_{x}_{k}.png')); k += 1
            info['personajes'][cuerpo][aspecto] = {'pie': pie(os.path.join(src, 'south.png')), 'cuadros': cuadros}

    json.dump(info, open(os.path.join(DEST, 'graficos.json'), 'w'), ensure_ascii=False, indent=1)
    print(f"Listo: {len(info['sprites'])} sprites, {len(info['tilesets'])} tilesets, "
          f"{sum(len(v) for v in info['personajes'].values())} juegos de personaje → {os.path.relpath(DEST, RAIZ)}")


if __name__ == '__main__':
    sys.exit(main())
