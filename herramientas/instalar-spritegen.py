#!/usr/bin/env python3
"""
Instala los sprites extraídos por sprite-gen (Clara y Arthur) en assets/personaje/<id>/
con la convención del juego (south/west/east/north + caminar, idle, golpear, talar, arar, cosechar)
y actualiza assets/manifest.json.
"""
import json
import os
import shutil
from PIL import Image, ImageOps

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
RUN_BASE = '/home/hugo/.gemini/antigravity-cli/brain/225af55f-6b4b-4e2c-a06c-13420fca5b5f/scratch/sprite_runs'

MAPA_PJ = {
    'granjera': {
        'base':       (os.path.join(RUN_BASE, 'clara', 'run'),            os.path.join(RAIZ, 'assets', 'personaje', 'granjera')),
        'minera':     (os.path.join(RUN_BASE, 'clara_minera', 'run'),     os.path.join(RAIZ, 'assets', 'personaje', 'granjera', 'ropa_minera')),
        'aventurera': (os.path.join(RUN_BASE, 'clara_aventurera', 'run'), os.path.join(RAIZ, 'assets', 'personaje', 'granjera', 'ropa_aventurera')),
    },
    'granjero': {
        'base':       (os.path.join(RUN_BASE, 'arthur', 'run'),            os.path.join(RAIZ, 'assets', 'personaje', 'granjero')),
        'minera':     (os.path.join(RUN_BASE, 'arthur_minera', 'run'),     os.path.join(RAIZ, 'assets', 'personaje', 'granjero', 'ropa_minera')),
        'aventurera': (os.path.join(RUN_BASE, 'arthur_aventurera', 'run'), os.path.join(RAIZ, 'assets', 'personaje', 'granjero', 'ropa_aventurera')),
    },
}

# Mapeo estado de juego -> estado de sprite-gen
MAPA_ESTADOS = {
    'idle': 'idle',
    'caminar': 'walk',
    'golpear': 'attack',
    'talar': 'work',
    'arar': 'work',
    'cosechar': 'harvest',
}

DIRS_ORIGEN = {
    'south': ('down', False),
    'east': ('side', False),
    'west': ('side', True),
    'north': ('up', False),
}


def cargar_frames(carpeta_frames, es_mirror):
    frames = []
    i = 0
    while True:
        ruta = os.path.join(carpeta_frames, f'frame-{i}.png')
        if not os.path.exists(ruta):
            break
        im = Image.open(ruta).convert('RGBA')
        if es_mirror:
            im = ImageOps.mirror(im)
        frames.append(im)
        i += 1
    return frames


def instalar_carpeta(etiqueta, run_dir, dest):
    os.makedirs(dest, exist_ok=True)
    # Limpiar PNGs antiguos en la carpeta (sin tocar subcarpetas ropa_*)
    for nombre in os.listdir(dest):
        ruta = os.path.join(dest, nombre)
        if os.path.isfile(ruta) and nombre.endswith('.png'):
            os.remove(ruta)

    cuadros = {}
    for d_juego, (d_sg, mirror) in DIRS_ORIGEN.items():
        frames_idle = cargar_frames(os.path.join(run_dir, 'frames', d_sg, 'idle'), mirror)
        if not frames_idle:
            raise RuntimeError(f'Faltan frames idle para {etiqueta} ({d_sg})')
        frames_idle[0].save(os.path.join(dest, f'{d_juego}.png'))

        for est_juego, est_sg in MAPA_ESTADOS.items():
            lista = cargar_frames(os.path.join(run_dir, 'frames', d_sg, est_sg), mirror)
            if not lista:
                raise RuntimeError(f'Faltan frames {est_sg} para {etiqueta} ({d_sg})')
            if est_juego == 'caminar':
                cuadros[d_juego] = len(lista)
            for idx, im in enumerate(lista):
                im.save(os.path.join(dest, f'{est_juego}_{d_juego}_{idx}.png'))

    info = {'tamano': {'width': 64, 'height': 64}, 'cuadros': cuadros, 'realzado': True}
    with open(os.path.join(dest, 'info.json'), 'w', encoding='utf-8') as f:
        json.dump(info, f)
    return info


def main():
    ruta_manifest = os.path.join(RAIZ, 'assets', 'manifest.json')
    manifest = json.load(open(ruta_manifest, encoding='utf-8'))
    manifest.setdefault('personajes', {})
    for pj_id, variantes in MAPA_PJ.items():
        base_run, base_dest = variantes['base']
        info_base = instalar_carpeta(f'{pj_id}:base', base_run, base_dest)
        ropas_manifest = {}
        for asp_id, (run_dir, dest) in variantes.items():
            if asp_id == 'base':
                continue
            info_asp = instalar_carpeta(f'{pj_id}:{asp_id}', run_dir, dest)
            ropas_manifest[asp_id] = {
                'carpeta': f'assets/personaje/{pj_id}/ropa_{asp_id}',
                **info_asp,
            }
        manifest['personajes'][pj_id] = {
            'carpeta': f'assets/personaje/{pj_id}',
            **info_base,
            'ropas': ropas_manifest,
        }
        print(f'Instalado {pj_id} (base + {list(ropas_manifest.keys())}): cuadros={info_base["cuadros"]}')
    with open(ruta_manifest, 'w', encoding='utf-8') as f:
        json.dump(manifest, f, indent=2)


if __name__ == '__main__':
    main()
