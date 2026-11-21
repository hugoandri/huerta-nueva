# Entrada a la cueva

La entrada no es un sprite suelto: se pinta **sobre el acantilado del juego** para que forme parte de él.

1. `fondo.png` es el recorte exacto (128×128) que dibuja el juego en el lugar del túnel (mundo x 672–800, y −32–96): pasto, camino y acantilado con los mismos tiles y capas que `js/cliente/render.js`.
2. `mascara.png` marca en blanco la zona donde PixelLab puede dibujar.
3. `pintar.mjs` llama a `POST /inpaint-v3` con el fondo, la máscara y la descripción.
4. Fuera de la máscara se restauran los píxeles originales, así el borde calza exacto.

Si cambian los tilesets del acantilado, el camino o la posición del túnel (`ENTRADA_CUEVA` en `js/nucleo/mundo.js`), hay que volver a componer el fondo y repetir el proceso.
