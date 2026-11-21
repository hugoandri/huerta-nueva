# Escaleras en la muralla de las mazmorras

Las escaleras de salida y de bajada van **dentro de la muralla**, no en el suelo. El generador de pisos
(`puntoEnPared` en `js/nucleo/mazmorra.js`) las pone en un tramo recto de la pared de arriba de una sala:
5 columnas con 3 filas de muro encima y 2 de suelo debajo. Ese tramo se dibuja siempre igual, así que
una escalera pintada una vez sobre él calza en cualquier piso.

1. `python3 componer.py` compone ese tramo (96×80) con el tileset de cada mazmorra (`fondo_<tileset>.png`) y la máscara.
2. `PIXELLAB_API_KEY=... node pintar.mjs [tileset]` pinta las dos escaleras con `POST /inpaint-v3` (`SOLO=escalera_bajar` para una sola).
3. `python3 fundir.py 60` restaura el fondo fuera de la máscara y donde casi no cambió, y escribe
   `assets/sprites/<pieza>_<tileset>.png`.

Al agregar una mazmorra con otro tileset, repetir los tres pasos con ese tileset.
