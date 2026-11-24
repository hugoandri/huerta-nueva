# Biblia de arte — Mosslantern

> Objetivo del dueño: que los personajes ("monitos"), sprites y escenarios resulten tan **amables, tiernos y agradables de mirar** como los grandes juegos acogedores de granja y de mazmorra adorable. Son referencia de *sensación* (legibilidad, calidez, siluetas gorditas, paletas suaves, idles expresivos), no de diseño: Mosslantern tiene identidad propia — **musgo, farolillos, el Valle (Glen) y la cripta de cristal azul espectral**.

---

## 1. Auditoría honesta del arte actual

Revisado a escala real (1 tile = 32 px, PPU 32, cámara ×1–×4) y ampliado.

### Lo que ya funciona
- **Monstruos pequeños**: `assets/sprites/limo.png`, `fantasma.png` y `murcielago.png` son el modelo a seguir: cuerpo redondo, ojos grandes, rubor, contorno oscuro grueso, 2–3 tonos por material. Se leen al instante.
- **Objetos y cultivos**: `calabaza_madura.png`, `zanahoria.png`, `rabano_maduro.png`, `pocion.png`, `espada_espectral.png`, `antorcha.png` son gorditos, saturados y coherentes entre sí.
- **Edificios**: `casa.png` (cálida, jardineras con flores) y `puesto.png` tienen el tono acogedor correcto.
- **Firma visual**: la flor azul del sombrero de Clara (`assets/personaje/granjera/south.png`) y el cristal azul (`roca_gema.png`, `aspersor_gema.png`) ya son un hilo de identidad. Hay que explotarlo.

### Lo que no funciona
1. **Proporciones de los protagonistas**: `granjera/south.png`, `granjera/east.png`, `granjero/south.png` ocupan 26×56 px con proporción casi realista (~3,5 cabezas). Los ojos quedan bajo el ala del sombrero; la cara de Arthur es una mancha oscura. El color más frecuente del sprite es casi negro (`#0a0704`): demasiado contorno y sombra interior. `ropa_aventurera/caminar_south_1.png` empeora: pelo rojo, armadura marrón y capa roja se funden en una silueta parda sin lectura. Son menos tiernos que el limo que combaten.
2. **Dos estilos de monstruo**: los comunes son chibi; los jefes `guardian.png`, `nigromante.png`, `caballero_espectral.png` son realistas, oscuros (`#080304`, `#120321` dominantes), con mucho más detalle y sombreado suave. Parecen de otro juego. `esqueleto.png` es flaco y anatómico, no simpático. `guardian.png` y `arbol.png` traen el suelo pintado debajo.
3. **Escala incoherente**: el `cofre.png` (32×32 lleno) mide casi 2/3 de la jugadora; el `arbol.png` (figura de 52×82) apenas dobla su altura; `rey_limo.png` (58 px) y `caballero_espectral.png` (77 px) no imponen como jefes frente a una jugadora de 56 px. `casa.png` mide 205×232: no es múltiplo de 32 y no encaja en la cuadrícula.
4. **Tiles planos frente a sprites ricos**: el césped de `assets/tilesets/tierra.json` es un verde lima plano (`#68b450`) casi sin textura, mientras los props piden "rich 3D shading". `cueva.json` es marrón-negro de bajo contraste (`#392a27`, `#1b1417`): el empedrado se lee como barro. `cripta.json` usa vacío casi negro (`#070314`) con un borde verde tóxico chillón: inquietante, no adorable.
5. **Perspectiva mixta**: `estilo.view` dice `high top-down`, los edificios usan `low top-down`, la casa tiene tejado en 3/4 y el puesto es frontal plano.
6. **Portada** (`portada_fondo.png`): bonita pero genérica (atardecer rosa-violeta, cabaña); no hay musgo ni farolillos.

---

## 2. Identidad visual

**Tres adjetivos:** *mullido* (todo parece blandito y redondeado), *luminoso* (pequeñas luces cálidas y azules en cada escena), *musgoso* (el verde suave lo cubre todo, incluso las ruinas).

### Lenguaje de formas
| Categoría | Forma | Regla |
|---|---|---|
| Residentes y protagonistas | Círculo + gota | Cabeza grande, cuerpo de pera, manos de bolita, sin ángulos. |
| Monstruos comunes | Círculo, gelatina | Siempre ojos visibles y expresión (sorpresa, enfado cómico, sueño). |
| Jefes | Triángulo redondeado | Más grandes y con un rasgo afilado (corona, espada, capucha), pero conservan ojos legibles y proporción chibi. |
| Edificios y props | Rectángulo blando | Esquinas biseladas, tejados inflados, musgo en bordes. |
| Peligro (pinchos, ataques) | Triángulo | Lo único realmente puntiagudo: así el jugador lo distingue. |

### Proporciones
- **Chibi de 2–2,5 cabezas**. Cabeza ≥ 40 % de la altura de la figura.
- Ojos: 2×3 px como mínimo con brillo blanco de 1 px; nunca tapados por ala de sombrero o capucha. El ala del sombrero sube por encima de las cejas.
- Jefes: 2,5–3 cabezas como máximo.

### Contorno
- Contorno exterior de **1 px, color oscuro cálido `#2b1d1a`**, no negro puro. En el Valle, el contorno de las zonas de luz puede tomar el tono oscuro del propio material (*selective outline*).
- Sin líneas negras interiores: el interior se separa con color, no con línea.

### Luz y sombreado
- Luz principal desde **arriba a la izquierda**; sombra de contacto ovalada bajo cada sprite (ya existe `Graficos.sombra`; **no pintar suelo dentro del sprite**).
- **3 niveles por material**: base, sombra (más fría y saturada) y brillo (más cálido). 4 solo en jefes y edificios grandes. Nada de degradados suaves.

---

## 3. Paleta

### Valle de día (base de todo el juego)
| Hex | Rol |
|---|---|
| `#2b1d1a` | Contorno universal |
| `#3f6b3a` | Musgo profundo (sombra de vegetación) |
| `#5e9a4b` | Musgo base / césped |
| `#8cc65a` | Hierba clara |
| `#c8e67a` | Brillo de hoja, brotes |
| `#6b4532` | Tierra arada |
| `#9c6a43` | Madera |
| `#f2d9a0` | Paja, crema, lino |
| `#f4c18e` | Piel cálida |
| `#d0573a` | Teja, rábano, pañuelo de Arthur |
| `#f0913a` | Calabaza, zanahoria |
| `#6cb6d9` | Agua, cielo |
| `#6fd3ff` | **Azul farolillo** (color de marca: flores, gemas, magia) |
| `#fff6d8` | Brillo cálido |

El césped pasa de lima `#68b450` a `#5e9a4b`/`#8cc65a` con motas de flor: menos ácido y más "musgo".

### Noche (tinte global + luces)
`#26325a` (ambiente), `#3b4a7a` (sombras), `#2f4f4a` (musgo nocturno), `#ffcf6b` (farol y ventanas), `#ffb35c` (halo de ventana), `#7fe0ff` (flores azules que brillan). Regla: de noche la escena baja de valor, pero **cada 6–8 tiles hay un punto de luz** (farolillo, ventana, flor, luciérnaga).

### Mina de Cobre
`#4a3a33` (suelo), `#6e5545` (piedra del empedrado), `#a07e62` (borde iluminado de la piedra), `#2a2024` (muro, nunca negro), `#e08a4f` (vetas de cobre), `#ffc08a` (brillo de cobre), `#6b8f4a` (musgo de galería), `#ffb347` (antorcha), `#a9b4c0` (hierro).
El empedrado necesita un borde claro `#a07e62` para no leerse como barro.

### Cripta Olvidada
`#1a1430` (vacío: violeta oscuro, no `#070314`), `#5a6a78` (losa), `#8597a3` (borde de losa), `#7aa35a` (musgo apagado, sustituye al verde tóxico), `#9b86c9` (niebla lavanda), `#7fe6ff` (cristal espectral), `#ffd27a` (velas), `#b07a5a` (urna de barro).

### Cómo la mazmorra da "miedito" sin dejar de ser amable
- Valor mínimo ~12 %: nada de negro absoluto; la oscuridad es violeta o marrón.
- El miedo viene del **tono frío y la niebla**, no de sangre, huesos expuestos realistas ni rojo intenso.
- Siempre hay luz cálida cerca del jugador (velas, antorchas, cristales).
- Los enemigos al morir **se deshacen en chispas o burbujas**, no en cadáveres.
- Esqueletos y fantasmas tienen cara expresiva (ojos redondos, boca "o").

---

## 4. Tamaños y cuadrícula

Tile del mundo: **32 px** (`mundo.tile`, `Graficos.PPU = 32`). Escala de cámara entera o media (×1–×4): no usar escalas fraccionarias en sprites.

| Tipo | Lienzo | Figura visible | Notas |
|---|---|---|---|
| Protagonistas | 64×64 | 30×46 aprox. (~1,5 tiles) | Pies en la fila 58; chibi. |
| Residentes | 48×48 | 24–32 de alto | Más bajos que la jugadora: sensación de "cuidarlos". |
| Monstruo común | 32×32 | 20–26 | limo, murciélago, fantasma. |
| Monstruo mediano | 32×48 | 24×36 | esqueleto. |
| Jefe | 96×96 | 72–88 de alto | ≥ 1,6× la jugadora. Hoy 64–88 de lienzo se queda corto. |
| Ítem / icono | 32×32 | 22–26 | Margen de 3 px para contorno y brillo. |
| Prop de 1 tile | 32×32 | ≤ 26×24 | cofre, aspersor, urna. |
| Árbol | 64×96 | 56×90 | ~3 tiles: debe superar con holgura a la jugadora. |
| Edificio | múltiplos de 32 | casa 192×224 (6×7 tiles), puesto 96×64, horno 64×64 | Huella (base) alineada a tiles. |

Pivote abajo-centro para todo lo que se pisa (ya lo hace `Graficos.SPR`).

---

## 5. Animación y sensación

PixelLab `animate-character` genera ciclos por dirección (hoy 4 cuadros en `idle`, `caminar`, `arar`, `cosechar`, `talar`, `golpear`). Los monstruos y props de `create-image-pixflux` son **estáticos**: su "vida" se añade en Unity por código.

- **Idle**: 4 cuadros, respiración visible de 1 px (hombros/sombrero), 0,6–0,8 s por ciclo. Pedir en el prompt "gentle bouncy breathing".
- **Parpadeo**: cada 3–5 s aleatorio, 1 cuadro con ojos cerrados (edición manual de 2–4 px o inpaint sobre `idle_*_0`).
- **Caminar**: 4 cuadros por dirección con rebote vertical de 1 px; 8 cuadros solo si sobra presupuesto.
- **Monstruos estáticos (código)**: squash & stretch senoidal (escala Y 0,94↔1,06, X inversa, 1–1,4 s); salto de 2 px al moverse; fantasmas flotan ±2 px.
- **Golpe**: destello blanco 2 cuadros (sprite `blanco`), retroceso de 2–3 px y *hit-stop* de 60 ms; estrellitas en lugar de sangre.
- **Recompensas**: corazones (`Graficos.corazon`) al regar o cosechar, chispas azules al recoger gemas, hojitas al talar, polvo al arar. Partículas de 2–4 px con colores de la paleta.
- **Jefes**: anticipación exagerada (agacharse 2 cuadros antes de atacar) y ojos que brillan en el aviso.

---

## 6. Receta de prompts para PixelLab

### Parámetros recomendados
| Uso | view | outline | shading | detail |
|---|---|---|---|---|
| Personajes y residentes | `low top-down` | `single color outline` | `basic shading` | `low detail` |
| Monstruos y jefes | `low top-down` | `single color outline` | `basic shading` (jefes `medium shading`) | `medium detail` |
| Ítems / iconos | `low top-down` | `single color outline` | `basic shading` | `medium detail` |
| Edificios y props | `low top-down` | `selective outline` | `medium shading` | `medium detail` |
| Tilesets | `high top-down` | `lineless` | `basic shading` | `medium detail` |

Tras generar, **remapear negro puro a `#2b1d1a`** y comprobar la paleta.

### Prefijo y sufijo
- **Prefijo personajes/criaturas:** `cute chibi, big round head, large shiny expressive eyes, soft rounded chunky silhouette, small body,`
- **Prefijo props:** `chunky cute rounded, soft bevelled edges, a little moss on the edges,`
- **Sufijo (sustituye al actual `estilo.sufijo`):** `cozy whimsical farming game pixel art, soft warm palette, mossy green and warm cream tones with small glowing blue lantern accents, dark brown outline, clear readable silhouette, no ground, transparent background`

Se elimina "vibrant colors" (empuja hacia verdes lima) y "rich 3D shading and depth" (choca con los tiles).

### Cinco prompts reescritos
1. **esqueleto** (32×48): `cute chibi skeleton miner, big round skull with large round dark eye holes and tiny glowing blue pupils, stubby rounded bones, wearing a dented little mining helmet with a candle, holding a small rusty pickaxe, sleepy confused expression` + sufijo.
2. **guardian** (96×96): `cute chibi stone golem guardian, huge round boulder body and small head, big round glowing blue crystal eyes, soft moss blanket and tiny flowers on shoulders and head, stubby rounded arms, gentle grumpy face, a small blue lantern hanging from one hand` + sufijo (sin suelo).
3. **nigromante** (96×96): `cute chibi hooded necromancer, oversized soft lavender robe hood with two big round glowing eyes inside, small round body, holding a crooked wooden staff topped with a smiling skull candle with blue flame, floating little purple mist wisps` + sufijo.
4. **caballero_espectral** (96×96): `cute chibi spectral knight, big rounded translucent pale blue helmet with two round glowing eyes, chunky rounded armor plates with moss in the joints, short stubby legs, oversized glowing blue crystal sword, flowing ghostly cape` + sufijo.
5. **cofre** (32×32, figura ≤ 26×24): `small chunky rounded wooden chest with soft bevelled lid, warm honey wood, brass corners, a tiny blue lantern charm on the lock, a little moss on top` + sufijo de props.

### Tres residentes nuevos (construcción de comunidad, 48×48, `create-character-with-4-directions` + idle)
1. **Musguito** (jardinero): `cute chibi moss sprite villager, round fluffy body made of soft green moss, two tiny leaf sprouts on the head, big shiny dark eyes and rosy cheeks, tiny stubby arms holding a small watering can, happy expression` + sufijo.
2. **Polilla Farolera** (ilumina el Valle de noche): `cute chibi round fuzzy cream moth villager, big fluffy antennae, soft round wings with blue spots, its round belly glows like a warm lantern, big kind eyes, tiny scarf` + sufijo.
3. **Don Caracolo** (artesano/comerciante): `cute chibi snail craftsman villager, small rounded body with big shiny eyes and a friendly smile, his shell is a tiny cozy wooden cottage with a chimney and a lit window, little straw hat, holding a tiny hammer` + sufijo.

### Protagonistas (sección `personajes`)
Añadir al inicio de su `descripcion`: `cute chibi proportions, big round head, large expressive eyes clearly visible under the hat brim, hat brim tilted up,` y quitar detalles finos (botones, hebillas, amuleto) que a 1× se vuelven ruido. Para `ropa_aventurera`: cambiar a cuero claro `#c49a6c` y capa `#d0573a` para separarlas del pelo.

---

## 7. Lista de revisión de cada asset nuevo

**Sí**
- [ ] Se entiende qué es en 1× y a contraluz (silueta en negro reconocible).
- [ ] Ojos visibles y expresión clara en todo ser vivo.
- [ ] Proporción chibi (cabeza ≥ 40 %) en personajes y criaturas.
- [ ] Contorno `#2b1d1a` de 1 px; sin negro puro.
- [ ] 3 tonos por material, luz arriba-izquierda.
- [ ] Colores dentro de la paleta de su zona; al menos un acento cálido o azul farolillo.
- [ ] Tamaño de figura según la tabla de la sección 4; huella en múltiplos de 32.
- [ ] Fondo transparente, sin suelo pintado, pivote abajo-centro.
- [ ] Comparado junto a la jugadora y el limo en una misma captura.

**No**
- [ ] Proporciones realistas, caras tapadas, cuerpos delgados y angulosos.
- [ ] Degradados suaves o sombreado "3D" que no casa con los tiles.
- [ ] Rojo sangre, gore, calaveras realistas, negro absoluto.
- [ ] Verde lima o neón fuera de las partes mágicas.
- [ ] Copiar personajes, logotipos o paletas de otros juegos, o nombrarlos en los prompts.
- [ ] Detalles de 1 px que desaparecen a la escala de juego.
