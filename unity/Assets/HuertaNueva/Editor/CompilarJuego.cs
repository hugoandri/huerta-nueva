using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace HuertaNueva.Editor
{
    public static class CompilarJuego
    {
        [MenuItem("Mosslantern/Build Windows Executable")]
        public static void Windows()
        {
            var escenas = EditorBuildSettings.scenes.Where(e => e.enabled).Select(e => e.path).ToArray();
            if (escenas.Length == 0) throw new InvalidOperationException("No hay escenas habilitadas para compilar.");
            string carpeta = Path.GetFullPath(Path.Combine(Application.dataPath, "../Builds/Windows"));
            Directory.CreateDirectory(carpeta);
            var resultado = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = escenas,
                locationPathName = Path.Combine(carpeta, "Mosslantern.exe"),
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            if (resultado.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Falló la compilación: " + resultado.summary.result
                    + " (errores: " + resultado.summary.totalErrors + ")");
            Debug.Log("Ejecutable creado: " + resultado.summary.outputPath);
        }
    }
}
