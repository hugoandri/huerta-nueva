# Huerta Nueva en Unity 6

Esta carpeta contiene un proyecto Unity completo con la versión C# del juego web. Incluye paquetes,
configuración URP con Renderer 2D y una escena inicial. El mundo y la interfaz se construyen por código
al dar Play.

## Cómo abrirlo (Unity Hub + Unity 6000.6.9f1)

1. Instala **Unity 6000.6.9f1** desde Unity Hub y activa una licencia válida para tu cuenta.
2. En Unity Hub: **Add → Add project from disk** y selecciona la carpeta `unity` de este repositorio.
3. Abre el proyecto y espera la instalación de paquetes y la importación de gráficos. La primera
   apertura necesita conexión para descargar los paquetes. Los PNG se importan como pixel art
   mediante `Editor/TexturasImport.cs`.
4. Abre `Assets/Scenes/SampleScene.unity` y pulsa **Play**. En la portada elige personaje y
   **Nueva partida**, o **Continuar** si tienes un guardado de Unity.

No necesitas crear otro proyecto ni copiar archivos. `ProjectSettings/ProjectVersion.txt` fija la
versión del editor y `Packages/manifest.json` declara URP, Tilemap, sprites, uGUI e Input System.
La escena inicial está incluida en la lista de escenas de compilación.

Si copiaste los gráficos con Unity abierto y se ven borrosos: clic derecho en
`Assets/HuertaNueva/Resources` → **Reimport**.

## Controles

- Moverse: WASD o flechas. En el celular aparece una cruceta.
- Usar lo que tienes en la mano: Espacio, J o Enter (o clic en una casilla vecina).
- Barra: 1-9, 0, -, =, Q/E o la rueda. F come o toma una poción. I/Tab menú, C crafteo, Esc cierra.

## Crear el ejecutable para Windows

En el editor usa **Huerta Nueva → Crear ejecutable de Windows**. Requiere el módulo Windows Build
Support instalado en Unity Hub. El resultado queda en `unity/Builds/Windows/HuertaNueva.exe`.
Para compartir el juego, entrega toda la carpeta `Windows`: el `.exe` necesita su carpeta
`HuertaNueva_Data`, `UnityPlayer.dll` y los demás archivos generados junto a él.

También puedes compilar desde PowerShell, en la raíz del repositorio:

```powershell
& 'C:\Program Files\Unity\Hub\Editor\6000.6.9f1\Editor\Unity.exe' -batchmode -nographics -quit -projectPath "$PWD\unity" -buildTarget Win64 -executeMethod HuertaNueva.Editor.CompilarJuego.Windows -logFile "$PWD\unity\compilacion-windows.log"
```

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

Unity importa los cambios automáticamente al volver al editor. El script necesita Python 3 y Pillow
(`python3 -m pip install Pillow`).

## Verificación

Los scripts de cliente, servicios, núcleo y editor se compilaron con Roslyn y las bibliotecas incluidas
en Unity 6000.6.9f1. Se corrigieron errores de compilación del menú y del constructor de botones, y
el recorte del sprite de las barras de estado. Quedan advertencias por una API de búsqueda de luces
obsoleta, compatible con esta versión.

Las siete pruebas C# comprueban generación reproducible, ciclo completo de cultivo, alcance de las
acciones, fabricación, compras, generación de mazmorras con jefes y entrada desde la granja, y botín
por jugador. Se ejecutan sin licencia del editor, usando su compilador y runtime .NET incluidos:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File herramientas/unity/verificar-nucleo.ps1
```

Ejecuta el comando desde la raíz del repositorio. Si tu editor está en otra ruta, añade
`-Editor 'D:\Unity\6000.6.9f1\Editor'`. Los resultados se generan en `unity/Logs/PruebasNucleo`
y no se incluyen en Git. Las pruebas web siguen disponibles con `npm test`.

Con la licencia Personal activa se completaron la importación y la compilación de Windows x64 en
Unity 6000.6.9f1. El ejecutable abrió su ventana sin registrar excepciones de arranque. Esta prueba
no sustituye una partida completa de validación. Las partidas web no se convierten al formato de
guardado de Unity.
