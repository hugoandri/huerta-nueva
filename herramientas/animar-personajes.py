#!/usr/bin/env python3
"""
Descarga los personajes v4 (Clara y Arthur + sus trajes minera y aventurera) en vista low top-down
con poses naturales desde PixelLab, limpia halos de borde, repara detalles de capa en caminar,
y construye las animaciones expresivas de personalidad pixel-art (idle, arar, talar, golpear, cosechar)
conservando al 100% la anatomía, paleta y rostro del sprite original en las 4 direcciones.
"""
import io
import json
import math
import os
import urllib.request
from PIL import Image

RAIZ = os.path.abspath(os.path.join(os.path.dirname(__file__), '..'))
ASSETS_PJ = os.path.join(RAIZ, 'assets', 'personaje')
DIRS = ['south', 'west', 'east', 'north']

# Nuevos personajes low top-down v4 con pose relajada natural y mucha personalidad
PERSONAJES_V4 = {
    'granjera': {
        'base': {
            'id': '1efa9a18-72fc-4e8d-9480-fcca843bbff6',
            'carpeta': os.path.join(ASSETS_PJ, 'granjera'),
            'rot_t': '1795377796',
            'walk': {
                'north': ('a90275c8-d4e8-4bb7-962a-ca96c0e65292', '1795377967437'),
                'south': ('682ab1ce-90a8-440f-81a0-c794c1c44f42', '1795377962750'),
                'east':  ('91063ea3-f368-4f49-88bd-73201b1b9df7', '1795377936730'),
                'west':  ('c4ca286e-0308-458e-99fa-d620bf2abeea', '1795377936358'),
            },
        },
        'minera': {
            'id': '8228dff4-526d-4023-9665-a752b04736b0',
            'carpeta': os.path.join(ASSETS_PJ, 'granjera', 'ropa_minera'),
            'rot_t': '1795377966',
            'walk': {
                'south': ('2e0e7b6e-f6e5-479a-a5d3-ec9b1994e76e', '1795378122655'),
                'north': ('a32d7ee9-c185-4643-9eb3-d18502343ba6', '1795378116271'),
                'west':  ('4a1e4a15-ad90-4374-80ae-ad7e472768c9', '1795378106150'),
                'east':  ('d0ec6825-59b9-46c3-8494-a81f1f324422', '1795378104256'),
            },
        },
        'aventurera': {
            'id': '1dac66df-e05b-4b6a-a77e-4cdd227a2dc0',
            'carpeta': os.path.join(ASSETS_PJ, 'granjera', 'ropa_aventurera'),
            'rot_t': '1795377961',
            'walk': {
                'south': ('680c2477-b830-416e-8297-215ef40119e4', '1795378136620'),
                'west':  ('206ea5ec-99a8-4c8d-a9d3-014292868345', '1795378125970'),
                'north': ('53861c3b-900a-4a83-94a1-4958a3b58d6f', '1795378118059'),
                'east':  ('2099831d-3e13-454c-a888-23aa6ab9e311', '1795378110210'),
            },
        },
    },
    'granjero': {
        'base': {
            'id': 'a0f394a8-4637-4f1f-a8a6-2bd0191abbc1',
            'carpeta': os.path.join(ASSETS_PJ, 'granjero'),
            'rot_t': '1795377802',
            'walk': {
                'south': ('0cb9ac5a-6cfb-48f3-96a5-61d0a11ae388', '1795378036589'),
                'north': ('cdbf7b15-f40a-4f71-a9a3-1f34d2afdc76', '1795378032867'),
                'east':  ('99c8f31f-8dc7-4774-8a58-cbf49420d854', '1795378028115'),
                'west':  ('0d7e4f96-3163-4389-9fdb-bc80a2d5505b', '1795378023052'),
            },
        },
        'minera': {
            'id': 'f413c75a-8b53-49ba-8b4b-be65bd296bd6',
            'carpeta': os.path.join(ASSETS_PJ, 'granjero', 'ropa_minera'),
            'rot_t': '1795377972',
            'walk': {
                'south': ('d3b083b5-df9a-4de7-b5bc-ecbb7d74de9d', '1795378210574'),
                'west':  ('d657f86d-cb70-4b13-bbcf-e1adba5ebdfb', '1795378204849'),
                'north': ('5c53a30d-6de8-4730-8b40-f63ffd1f0a8e', '1795378204019'),
                'east':  ('f04c091a-7f0d-47af-b707-7ba71d62c2a3', '1795378194872'),
            },
        },
        'aventurera': {
            'id': '3dc79ed4-8b35-4105-bc36-88b2c0f2047b',
            'carpeta': os.path.join(ASSETS_PJ, 'granjero', 'ropa_aventurera'),
            'rot_t': '1795377970',
            'walk': {
                'south': ('d92441b9-47c3-49a3-837c-e164cb71cb76', '1795378209973'),
                'north': ('241180bb-e5c6-4347-b0c8-080bcfe7a4ac', '1795378206060'),
                'west':  ('79b39a58-3a9c-4fb9-9e65-eff88fcd454e', '1795378197639'),
                'east':  ('a1e27dd7-467c-4ce9-8437-8382bd5f9d04', '1795378194591'),
            },
        },
    },
}

BASE_URL = 'https://backblaze.pixellab.ai/file/pixellab-characters/fdcffbfd-2690-4958-a77c-2da3ba5889c6'


def descargar_img(url):
    req = urllib.request.Request(url, headers={'User-Agent': 'Mozilla/5.0'})
    with urllib.request.urlopen(req, timeout=30) as resp:
        return Image.open(io.BytesIO(resp.read())).convert('RGBA')


def limpiar_bordes(im):
    """Elimina cualquier píxel blanco/gris claro fantasma en el contorno exterior del sprite."""
    im = im.copy()
    w, h = im.size
    px = im.load()

    def alpha(x, y):
        return 0 if (x < 0 or y < 0 or x >= w or y >= h) else px[x, y][3]

    # Dos pasadas para limpiar halos de recorte en los bordes
    for _ in range(2):
        a_borrar = []
        a_repintar = []
        votos = {}
        for y in range(h):
            for x in range(w):
                r, g, b, a = px[x, y]
                if a == 0:
                    continue
                es_borde = (alpha(x - 1, y) == 0 or alpha(x + 1, y) == 0 or
                            alpha(x, y - 1) == 0 or alpha(x, y + 1) == 0)
                if not es_borde:
                    continue
                if r + g + b < 160:
                    c = (r, g, b, 255)
                    votos[c] = votos.get(c, 0) + 1
                elif r > 185 and g > 185 and b > 185:
                    # Si es una mota blanca aislada o pegada al borde exterior, ver cuántos vecinos oscuros tiene
                    vecinos_solidos = sum(1 for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))
                                          if alpha(x + dx, y + dy) > 0 and sum(px[x + dx, y + dy][:3]) <= 555)
                    if vecinos_solidos <= 1:
                        a_borrar.append((x, y))
                    else:
                        a_repintar.append((x, y))
        contorno = max(votos.items(), key=lambda kv: kv[1])[0] if votos else (18, 12, 16, 255)
        for x, y in a_borrar:
            px[x, y] = (0, 0, 0, 0)
        for x, y in a_repintar:
            px[x, y] = contorno
    return im


def bbox_alfa(im):
    w, h = im.size
    px = im.load()
    x0, y0, x1, y1 = w, h, -1, -1
    for y in range(h):
        for x in range(w):
            if px[x, y][3] > 20:
                if x < x0: x0 = x
                if x > x1: x1 = x
                if y < y0: y0 = y
                if y > y1: y1 = y
    return (x0, y0, x1, y1) if x1 >= 0 else (0, 0, w - 1, h - 1)


def reparar_capa_norte(rot_north, walk_frames):
    """
    En el traje aventurera mirando al norte, si algún frame de caminar perdió la capa roja
    que sí está en rot_north, restauramos la capa roja con un ligero balanceo al caminar.
    """
    w, h = rot_north.size
    px_rot = rot_north.load()
    # Extraer píxeles de la capa roja (rojo dominante sobre verde y azul) en la zona de espalda/piernas
    capa = Image.new('RGBA', (w, h), (0, 0, 0, 0))
    px_capa = capa.load()
    for y in range(24, h):
        for x in range(w):
            r, g, b, a = px_rot[x, y]
            if a > 0 and ((r > 85 and r > g * 1.5 and r > b * 1.35) or
                          (y >= 30 and 18 <= x <= 45 and a > 0 and (
                              (r > 60 and r > g * 1.35) or
                              (r + g + b < 90 and any(
                                  0 <= x + dx < w and 0 <= y + dy < h and
                                  px_rot[x + dx, y + dy][0] > 95 and
                                  px_rot[x + dx, y + dy][0] > px_rot[x + dx, y + dy][1] * 1.5
                                  for dx, dy in ((-1, 0), (1, 0), (0, -1), (0, 1))
                              ))
                          ))):
                px_capa[x, y] = (r, g, b, a)

    reparados = []
    sway_x = [0, 1, 1, 0, -1, -1]
    sway_y = [0, -1, 0, 0, -1, 0]
    for idx, f in enumerate(walk_frames):
        px_f = f.load()
        rojos = sum(1 for y in range(28, 52) for x in range(18, 46)
                    if px_f[x, y][3] > 0 and px_f[x, y][0] > 90 and px_f[x, y][0] > px_f[x, y][1] * 1.5)
        if rojos > 120:
            reparados.append(f)
            continue
        nuevo = f.copy()
        px_n = nuevo.load()
        dx, dy = sway_x[idx % len(sway_x)], sway_y[idx % len(sway_y)]
        for y in range(24, h):
            for x in range(w):
                cr, cg, cb, ca = px_capa[x, y]
                if ca > 0:
                    nx, ny = x + dx, y + dy
                    if 0 <= nx < w and 0 <= ny < h:
                        px_n[nx, ny] = (cr, cg, cb, ca)
        reparados.append(limpiar_bordes(nuevo))
    return reparados


def parpadear_rostro(im, dir_name):
    """
    Crea una variante con parpadeo suave y limpio en el rostro del personaje.
    Solo actúa sobre racimos verticales de 1 a 3 px de iris/pupila estrictamente dentro de la cara
    (por encima de la boca/barbilla), sin tocar jamás el cuello ni el peto verde.
    """
    if dir_name == 'north':
        return im.copy()
    out = im.copy()
    w, h = out.size
    px = out.load()
    x0, y0, x1, y1 = bbox_alfa(im)
    alto = y1 - y0 + 1
    # La línea de los ojos en low top-down 64x64 está entre el 22% y el 35% de la altura
    fy0 = y0 + int(alto * 0.22)
    fy1 = y0 + int(alto * 0.35)
    fx0 = x0 + int((x1 - x0) * 0.22)
    fx1 = x1 - int((x1 - x0) * 0.22)

    pieles = []
    for y in range(fy0, fy1 + 2):
        for x in range(fx0, fx1 + 1):
            r, g, b, a = px[x, y]
            if a > 200 and r > 185 and 120 < g < 205 and 85 < b < 170 and r > g > b:
                pieles.append((r, g, b, 255))
    color_piel = pieles[len(pieles) // 2] if pieles else (238, 178, 138, 255)
    color_pestana = (42, 20, 24, 255)

    for x in range(fx0, fx1 + 1):
        ys = []
        for y in range(fy0, fy1 + 1):
            r, g, b, a = px[x, y]
            if a < 200:
                continue
            es_iris = (g > r + 12 and g > 75) or (b > r + 12 and b > 75)
            es_brillo_ojo = (r > 220 and g > 220 and b > 220)
            if es_iris or es_brillo_ojo:
                ys.append(y)
        # Solo si es un ojo real (racimo compacto de 1 a 3 píxeles de alto)
        if 1 <= len(ys) <= 3 and (max(ys) - min(ys) <= 2):
            ymin, ymax = min(ys), max(ys)
            for y in range(ymin, ymax + 1):
                px[x, y] = color_pestana if y == ymax else color_piel
    return out


def articular_pose(
    base_im,
    piernas_im,
    dir_name,
    head_dx=0, head_dy=0,
    torso_dx=0, torso_dy=0,
    arm_l_dx=0, arm_l_dy=0,
    arm_r_dx=0, arm_r_dy=0,
    legs_dx=0, legs_dy=0,
    parpadeo=False,
):
    """
    Compone un cuadro pixel-art limpio y sin costuras articulando:
      - Tren inferior (piernas y botas desde base_im o un cuadro de zancada de caminar)
      - Tren superior (torso y brazos, con desplazamiento suave de brazos de ±1..2px respecto al hombro)
      - Cabeza y sombrero (con inclinación expresiva y parpadeo limpio)
    """
    w, h = base_im.size
    x0, y0, x1, y1 = bbox_alfa(base_im)
    cx = (x0 + x1) // 2

    y_cuello = y0 + int((y1 - y0) * 0.44)
    y_cadera = y0 + int((y1 - y0) * 0.68)

    # Limitar la separación relativa brazo-torso y cabeza-torso a ±2px para que jamás se rompa la silueta
    al_dx = torso_dx + max(-1, min(1, arm_l_dx - torso_dx))
    al_dy = torso_dy + max(-2, min(2, arm_l_dy - torso_dy))
    ar_dx = torso_dx + max(-1, min(1, arm_r_dx - torso_dx))
    ar_dy = torso_dy + max(-2, min(2, arm_r_dy - torso_dy))
    hd_dx = torso_dx + max(-1, min(1, head_dx - torso_dx))
    hd_dy = torso_dy + max(-1, min(2, head_dy - torso_dy))

    fuente_cabeza = parpadear_rostro(base_im, dir_name) if parpadeo else base_im
    px_base = base_im.load()
    px_cab = fuente_cabeza.load()
    px_leg = piernas_im.load()

    lienzo = Image.new('RGBA', (w, h), (0, 0, 0, 0))

    def pegar_region(fn_filtro, src_px, dx, dy):
        capa = lienzo.load()
        for y in range(h):
            ny = y + dy
            if ny < 0 or ny >= h:
                continue
            for x in range(w):
                nx = x + dx
                if nx < 0 or nx >= w:
                    continue
                if fn_filtro(x, y):
                    c = src_px[x, y]
                    if c[3] > 0:
                        capa[nx, ny] = c

    if dir_name in ('south', 'north'):
        x_brazo_izq = cx - 6
        x_brazo_der = cx + 6
        # 1) Piernas
        pegar_region(lambda x, y: y >= y_cadera - 1, px_leg, legs_dx, legs_dy)
        # 2) Unión cintura-cadera
        pegar_region(lambda x, y: y_cadera - 3 <= y <= y_cadera + 2 and x_brazo_izq <= x <= x_brazo_der,
                     px_base, (torso_dx + legs_dx) // 2, (torso_dy + legs_dy) // 2)
        # 3) Torso completo (incluye hombros como base bajo los brazos para evitar huecos)
        pegar_region(lambda x, y: y_cuello - 2 <= y < y_cadera + 1,
                     px_base, torso_dx, torso_dy)
        # 4) Brazos izquierdo y derecho articulados suavemente sobre el hombro
        pegar_region(lambda x, y: y_cuello + 1 <= y < y_cadera + 3 and x < x_brazo_izq,
                     px_base, al_dx, al_dy)
        pegar_region(lambda x, y: y_cuello + 1 <= y < y_cadera + 3 and x > x_brazo_der,
                     px_base, ar_dx, ar_dy)
        # 5) Cabeza y sombrero
        pegar_region(lambda x, y: y <= y_cuello, px_cab, hd_dx, hd_dy)
    else:
        # En vista lateral (east / west), el tren superior y las piernas de zancada se articulan sin partir el torso verticalmente
        pegar_region(lambda x, y: y >= y_cadera - 1, px_leg, legs_dx, legs_dy)
        pegar_region(lambda x, y: y_cadera - 3 <= y <= y_cadera + 2,
                     px_base, (torso_dx + legs_dx) // 2, (torso_dy + legs_dy) // 2)
        pegar_region(lambda x, y: y_cuello - 2 <= y < y_cadera + 1,
                     px_base, torso_dx, torso_dy)
        pegar_region(lambda x, y: y <= y_cuello, px_cab, hd_dx, hd_dy)

    return limpiar_bordes(lienzo)


def generar_clips_personalidad(rot, walk):
    """
    Genera los 5 clips de personalidad (idle, arar, talar, golpear, cosechar)
    en las 4 direcciones para un personaje/traje a partir de sus rotaciones y pasos.
    """
    clips = {'idle': {}, 'arar': {}, 'talar': {}, 'golpear': {}, 'cosechar': {}}
    for d in DIRS:
        base = rot[d]
        w = walk[d]
        paso1 = w[1] if len(w) > 1 else base
        paso2 = w[2] if len(w) > 2 else paso1
        paso4 = w[4] if len(w) > 4 else paso1
        paso5 = w[5] if len(w) > 5 else base

        fx = 1 if d == 'east' else (-1 if d == 'west' else 0)
        fy = 1 if d == 'south' else (-1 if d == 'north' else 0)

        # 1. IDLE (4 cuadros: respiración viva + parpadeo cálido + ligero balanceo de brazos/sombrero)
        clips['idle'][d] = [
            base,
            articular_pose(base, base, d, head_dy=-1, torso_dy=-1, arm_l_dy=0, arm_r_dy=-1),
            articular_pose(base, base, d, head_dy=-1, torso_dy=-1, arm_l_dy=-1, arm_r_dy=-1, parpadeo=True),
            articular_pose(base, base, d, head_dy=0, torso_dy=0, arm_l_dy=0, arm_r_dy=0),
        ]

        # 2. TALAR / PICAR (6 cuadros: anticipación alzando brazos -> golpe descendente con peso -> recuperación)
        clips['talar'][d] = [
            articular_pose(base, base, d,
                           head_dx=-fx, head_dy=-1,
                           torso_dx=-fx, torso_dy=-1,
                           arm_l_dx=-fx, arm_l_dy=-3,
                           arm_r_dx=-fx, arm_r_dy=-4),
            articular_pose(base, paso1, d,
                           head_dx=-fx, head_dy=-2,
                           torso_dx=-fx, torso_dy=-1,
                           arm_l_dx=-fx, arm_l_dy=-5,
                           arm_r_dx=-fx, arm_r_dy=-6),
            articular_pose(base, paso2, d,
                           head_dx=fx * 2, head_dy=1,
                           torso_dx=fx * 2, torso_dy=1,
                           arm_l_dx=fx * 3, arm_l_dy=0,
                           arm_r_dx=fx * 4, arm_r_dy=0,
                           legs_dx=fx),
            articular_pose(base, paso2, d,
                           head_dx=fx * 2, head_dy=3,
                           torso_dx=fx * 2, torso_dy=2,
                           arm_l_dx=fx * 3, arm_l_dy=3,
                           arm_r_dx=fx * 4, arm_r_dy=3,
                           legs_dx=fx, legs_dy=1, parpadeo=True),
            articular_pose(base, paso1, d,
                           head_dx=fx, head_dy=2,
                           torso_dx=fx, torso_dy=1,
                           arm_l_dx=fx * 2, arm_l_dy=2,
                           arm_r_dx=fx * 2, arm_r_dy=2),
            base,
        ]

        # 3. GOLPEAR / ESPADA (6 cuadros: preparación -> estocada dinámica hacia el frente -> barrido -> guardia)
        clips['golpear'][d] = [
            articular_pose(base, paso4, d,
                           head_dx=-fx, head_dy=-1,
                           torso_dx=-fx, torso_dy=0,
                           arm_l_dx=-fx, arm_l_dy=-2,
                           arm_r_dx=-fx * 2, arm_r_dy=-4),
            articular_pose(base, paso1, d,
                           head_dx=fx * 2, head_dy=1,
                           torso_dx=fx * 2, torso_dy=1,
                           arm_l_dx=fx * 2, arm_l_dy=0,
                           arm_r_dx=fx * 3, arm_r_dy=1,
                           legs_dx=fx),
            articular_pose(base, paso2, d,
                           head_dx=fx * 3, head_dy=2,
                           torso_dx=fx * 3, torso_dy=2,
                           arm_l_dx=fx * 3, arm_l_dy=2,
                           arm_r_dx=fx * 5, arm_r_dy=2,
                           legs_dx=fx * 2, legs_dy=1),
            articular_pose(base, paso2, d,
                           head_dx=fx * 2, head_dy=2,
                           torso_dx=fx * 2, torso_dy=1,
                           arm_l_dx=fx * 2, arm_l_dy=2,
                           arm_r_dx=fx * 4, arm_r_dy=3,
                           legs_dx=fx),
            articular_pose(base, paso5, d,
                           head_dx=fx, head_dy=1,
                           torso_dx=fx, torso_dy=0,
                           arm_l_dx=fx, arm_l_dy=1,
                           arm_r_dx=fx * 2, arm_r_dy=1),
            base,
        ]

        # 4. ARAR / REGAR (6 cuadros: alzar azada/regadera -> inclinarse hacia el surco frente a él -> recuperar)
        clips['arar'][d] = [
            articular_pose(base, base, d,
                           head_dy=-1, torso_dy=-1,
                           arm_l_dx=fx, arm_l_dy=-3,
                           arm_r_dx=fx, arm_r_dy=-3),
            articular_pose(base, paso1, d,
                           head_dx=fx, head_dy=-1,
                           torso_dx=fx, torso_dy=-1,
                           arm_l_dx=fx * 2, arm_l_dy=-4,
                           arm_r_dx=fx * 2, arm_r_dy=-4),
            articular_pose(base, paso2, d,
                           head_dx=fx * 2, head_dy=2,
                           torso_dx=fx * 2, torso_dy=1,
                           arm_l_dx=fx * 3, arm_l_dy=2,
                           arm_r_dx=fx * 3, arm_r_dy=2,
                           legs_dx=fx),
            articular_pose(base, paso2, d,
                           head_dx=fx * 2, head_dy=3,
                           torso_dx=fx * 2, torso_dy=2,
                           arm_l_dx=fx * 3, arm_l_dy=3,
                           arm_r_dx=fx * 4, arm_r_dy=3,
                           legs_dx=fx, legs_dy=1),
            articular_pose(base, paso1, d,
                           head_dx=fx, head_dy=1,
                           torso_dx=fx, torso_dy=1,
                           arm_l_dx=fx * 2, arm_l_dy=1,
                           arm_r_dx=fx * 2, arm_r_dy=1),
            base,
        ]

        # 5. COSECHAR (5 cuadros: agacharse natural de pie -> recoger -> salto alegre alzando la cosecha)
        clips['cosechar'][d] = [
            articular_pose(base, paso1, d,
                           head_dx=fx, head_dy=2,
                           torso_dx=fx, torso_dy=2,
                           arm_l_dx=fx * 2, arm_l_dy=3,
                           arm_r_dx=fx * 2, arm_r_dy=3,
                           legs_dy=1),
            articular_pose(base, paso2, d,
                           head_dx=fx * 2, head_dy=4,
                           torso_dx=fx * 2, torso_dy=3,
                           arm_l_dx=fx * 3, arm_l_dy=4,
                           arm_r_dx=fx * 3, arm_r_dy=4,
                           legs_dy=2, parpadeo=True),
            articular_pose(base, base, d,
                           head_dy=0, torso_dy=0,
                           arm_l_dx=fx, arm_l_dy=-2,
                           arm_r_dx=fx, arm_r_dy=-2),
            articular_pose(base, paso4, d,
                           head_dy=-2, torso_dy=-2,
                           arm_l_dx=0, arm_l_dy=-5,
                           arm_r_dx=0, arm_r_dy=-5,
                           legs_dy=-2),
            base,
        ]
    return clips


def main():
    for pj_id, aspectos in PERSONAJES_V4.items():
        for asp_id, cfg in aspectos.items():
            cid = cfg['id']
            carpeta = cfg['carpeta']
            os.makedirs(carpeta, exist_ok=True)
            ya_descargado = all(
                os.path.exists(os.path.join(carpeta, f'{d}.png')) and
                all(os.path.exists(os.path.join(carpeta, f'caminar_{d}_{i}.png')) for i in range(6))
                for d in DIRS
            )
            rot = {}
            walk = {}
            if ya_descargado:
                print(f'Usando rotaciones y caminata en caché de {pj_id} ({asp_id})...')
                for d in DIRS:
                    rot[d] = limpiar_bordes(Image.open(os.path.join(carpeta, f'{d}.png')).convert('RGBA'))
                    rot[d].save(os.path.join(carpeta, f'{d}.png'))
                    walk[d] = [
                        limpiar_bordes(Image.open(os.path.join(carpeta, f'caminar_{d}_{i}.png')).convert('RGBA'))
                        for i in range(6)
                    ]
            else:
                for f in os.listdir(carpeta):
                    ruta_f = os.path.join(carpeta, f)
                    if os.path.isfile(ruta_f) and f.endswith('.png'):
                        os.remove(ruta_f)
                print(f'Descargando {pj_id} ({asp_id}) [{cid}]...')
                for d in DIRS:
                    url_rot = f"{BASE_URL}/{cid}/rotations/{d}.png?t={cfg['rot_t']}"
                    im_rot = limpiar_bordes(descargar_img(url_rot))
                    rot[d] = im_rot
                    im_rot.save(os.path.join(carpeta, f'{d}.png'))

                    anim_id, anim_t = cfg['walk'][d]
                    frames = []
                    for i in range(6):
                        url_f = f"{BASE_URL}/{cid}/animations/{anim_id}/{d}/{i}.png?t={anim_t}"
                        frames.append(limpiar_bordes(descargar_img(url_f)))
                    walk[d] = frames

            if asp_id == 'aventurera':
                walk['north'] = reparar_capa_norte(rot['north'], walk['north'])

            cuadros = {}
            for d in DIRS:
                cuadros[d] = len(walk[d])
                for i, im_f in enumerate(walk[d]):
                    im_f.save(os.path.join(carpeta, f'caminar_{d}_{i}.png'))

            clips = generar_clips_personalidad(rot, walk)
            for nombre_clip, por_dir in clips.items():
                for d, lista_im in por_dir.items():
                    for i, im_f in enumerate(lista_im):
                        im_f.save(os.path.join(carpeta, f'{nombre_clip}_{d}_{i}.png'))

            info = {
                'tamano': {'width': 64, 'height': 64},
                'cuadros': cuadros,
                'realzado': True,
            }
            with open(os.path.join(carpeta, 'info.json'), 'w', encoding='utf-8') as f:
                json.dump(info, f)

    # Actualizar assets/pixellab-ids.json con los nuevos character IDs
    ruta_ids = os.path.join(RAIZ, 'assets', 'pixellab-ids.json')
    ids = json.load(open(ruta_ids, encoding='utf-8')) if os.path.exists(ruta_ids) else {}
    ids['personaje:granjera'] = PERSONAJES_V4['granjera']['base']['id']
    ids['ropa:granjera:minera'] = PERSONAJES_V4['granjera']['minera']['id']
    ids['ropa:granjera:aventurera'] = PERSONAJES_V4['granjera']['aventurera']['id']
    ids['personaje:granjero'] = PERSONAJES_V4['granjero']['base']['id']
    ids['ropa:granjero:minera'] = PERSONAJES_V4['granjero']['minera']['id']
    ids['ropa:granjero:aventurera'] = PERSONAJES_V4['granjero']['aventurera']['id']
    ids['personaje'] = {
        'granjera': PERSONAJES_V4['granjera']['base']['id'],
        'granjera_ropa_minera': PERSONAJES_V4['granjera']['minera']['id'],
        'granjera_ropa_aventurera': PERSONAJES_V4['granjera']['aventurera']['id'],
        'granjero': PERSONAJES_V4['granjero']['base']['id'],
        'granjero_ropa_minera': PERSONAJES_V4['granjero']['minera']['id'],
        'granjero_ropa_aventurera': PERSONAJES_V4['granjero']['aventurera']['id'],
    }
    with open(ruta_ids, 'w', encoding='utf-8') as f:
        json.dump(ids, f, indent=2)
    print('¡Personajes v4 y animaciones de personalidad generados con éxito!')


if __name__ == '__main__':
    main()
