# Tras el inpaint: fuera de la máscara se restaura el fondo; dentro, todo pixel que quedó casi igual
# al fondo (diferencia de color pequeña) también vuelve al original, así no se nota el rectángulo.
# Uso: python3 fundir.py <umbral>   (escribe ../../assets/sprites/<pieza>_<tileset>.png)
import sys, os
exec(open(os.path.join(os.path.dirname(__file__), 'componer.py')).read().split("T=32;")[0])
umbral = int(sys.argv[1]) if len(sys.argv) > 1 else 60
_, _, mask = dec(open('mascara.png', 'rb').read())
for ts in ('cueva', 'cripta'):
    _, _, fondo = dec(open(f'fondo_{ts}.png', 'rb').read())
    for pieza in ('escalera_subir', 'escalera_bajar'):
        w, h, v = dec(open(f'{pieza}_{ts}.png', 'rb').read())
        restaurados = 0
        for y in range(h):
            for x in range(w):
                o = fondo[y][x*4:x*4+4]; n = v[y][x*4:x*4+4]
                if mask[y][x*4] < 128 or sum(abs(n[k]-o[k]) for k in range(3)) < umbral:
                    v[y][x*4:x*4+4] = o; restaurados += 1
                else: v[y][x*4+3] = 255
        open(f'../../assets/sprites/{pieza}_{ts}.png', 'wb').write(png(w, h, v))
        print(pieza, ts, 'restaurados', restaurados)
