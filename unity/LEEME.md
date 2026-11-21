# Huerta Nueva en Unity 6

Esta carpeta es el mismo juego que la versión web, traducido a C#. Todo se construye por código al dar
Play: no hay escenas, prefabs ni configuración que hacer a mano.

## Cómo abrirlo (Unity Hub + Unity 6)

1. En Unity Hub: **New project** → plantilla **Universal 2D** → crea el proyecto.
2. Cierra Unity y copia la carpeta `unity/Assets/HuertaNueva` de este repositorio dentro de la carpeta
   `Assets` del proyecto nuevo (queda `Assets/HuertaNueva/...`).
3. Abre el proyecto. Unity importa los gráficos como pixel art (lo hace `Editor/TexturasImport.cs`).
4. Abre cualquier escena (la `SampleScene` de la plantilla sirve) y pulsa **Play**.

Si copiaste los gráficos con Unity abierto y se ven borrosos: clic derecho en
`Assets/HuertaNueva/Resources` → **Reimport**.

## Controles

- Moverse: WASD o flechas. En el celular aparece una cruceta.
- Usar lo que tienes en la mano: Espacio, J o Enter (o clic en una casilla vecina).
- Barra: 1-9, 0, -, =, Q/E o la rueda. F come o toma una poción. I/Tab menú, C crafteo, Esc cierra.

## Cómo está armado

```
Assets/HuertaNueva/
  Scripts/Nucleo/     reglas puras (sin UnityEngine): estado, acciones, mazmorras, botín
  Scripts/Servicios/  sesión local y guardado (JSON en Application.persistentDataPath)
  Scripts/Cliente/    gráficos, vista (Tilemaps + luz 2D de URP), interfaz (uGUI) y entrada
  Editor/             ajustes de importación de los PNG
  Resources/HuertaNueva/  contenido.json y los gráficos de PixelLab
```

- **Núcleo**: es una traducción directa de `js/nucleo`. Toda acción pasa por `Reglas.Aplicar(estado, def, accion)`
  y devuelve eventos. Las reglas validan lo que llega, así que un servidor online puede usar este mismo código.
- **Contenido**: `Resources/HuertaNueva/contenido.json` es copia de `data/contenido.json`; las recetas,
  objetos, mazmorras y botines se cambian ahí y valen para las dos versiones.
- **Suelo**: rejilla doble con piezas Wang de 16 variantes (agua, camino, tierra arada, acantilado, cueva).
- **Luz**: de día luz global plena; de noche y en las mazmorras baja la luz global y antorchas, casa y
  jugador llevan luces puntuales (`Light2D`).
- **Guardado**: `huerta-nueva.json` en `Application.persistentDataPath`; se guarda al dormir, al cambiar de piso,
  cada 20 s y al pasar la app a segundo plano. Es un formato propio de Unity (no lee las partidas de la web).

## Actualizar los gráficos

Cuando generes gráficos nuevos con `herramientas/generar-assets.mjs`, cópialos al proyecto de Unity:

```bash
python3 herramientas/unity/sincronizar.py
```

y vuelve a copiar `Assets/HuertaNueva/Resources` a tu proyecto.

## Estado

El código C# se escribió sin poder compilarlo aquí (solo se verificó la sintaxis). Si al abrirlo Unity
muestra algún error de compilación en la consola, cópialo tal cual y lo corrijo.
Requisitos: los paquetes que trae la plantilla Universal 2D (URP, 2D Tilemap, uGUI, Input System).
