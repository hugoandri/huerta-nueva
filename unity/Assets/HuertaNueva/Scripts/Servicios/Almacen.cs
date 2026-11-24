// Dónde se guarda la partida: un archivo JSON en Application.persistentDataPath.
// Para el online se agrega un AlmacenNube con la misma interfaz (Cargar/Guardar/Borrar/Existe).
using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace HuertaNueva
{
    public class AlmacenLocal
    {
        const int MAX_RESPALDOS = 5;
        readonly string ruta, tmp;
        readonly Def def;

        public AlmacenLocal(Def def, string archivo = "huerta-nueva.json")
        {
            this.def = def;
            ruta = Path.Combine(Application.persistentDataPath, archivo);
            tmp = ruta + ".tmp";
        }

        public bool Existe() => File.Exists(ruta) || File.Exists(tmp);

        public Estado Cargar()
        {
            try
            {
                // Si la app se cerró a mitad de un guardado, el .tmp es la copia completa más reciente.
                if (!File.Exists(ruta) && File.Exists(tmp)) File.Move(tmp, ruta);
                if (!File.Exists(ruta)) return null;
                var e = JsonUtility.FromJson<Estado>(File.ReadAllText(ruta));
                if (e == null || e.version != Mundo.VERSION_ESTADO || e.granja == null || e.jugadores.Count == 0) return null;
                // Con def se quitan items, objetos, cultivos y mazmorras que ya no existen en contenido.json.
                e.Normalizar(def);
                return e;
            }
            catch (Exception ex) { Debug.LogWarning("No se pudo cargar la partida: " + ex.Message); return null; }
        }

        public bool Guardar(Estado e)
        {
            try
            {
                File.WriteAllText(tmp, JsonUtility.ToJson(e));
                // Replace cambia el archivo de una vez: nunca queda un momento sin guardado.
                if (File.Exists(ruta)) File.Replace(tmp, ruta, null);
                else File.Move(tmp, ruta);
                return true;
            }
            catch (Exception ex) { Debug.LogWarning("No se pudo guardar: " + ex.Message); return false; }
        }

        // Copia el guardado actual antes de sobrescribirlo, para no perder una partida que no se pudo leer.
        // Conserva los últimos MAX_RESPALDOS. Devuelve la ruta de la copia, o null si no había nada que copiar.
        public string Respaldar()
        {
            try
            {
                string origen = File.Exists(ruta) ? ruta : File.Exists(tmp) ? tmp : null;
                if (origen == null) return null;
                string dir = Path.GetDirectoryName(ruta), nombre = Path.GetFileNameWithoutExtension(ruta);
                string copia = Path.Combine(dir, $"{nombre}.respaldo-{DateTime.Now:yyyyMMdd-HHmmss}.json");
                File.Copy(origen, copia, true);
                foreach (var viejo in Directory.GetFiles(dir, nombre + ".respaldo-*.json").OrderByDescending(f => f).Skip(MAX_RESPALDOS))
                    File.Delete(viejo);
                return copia;
            }
            catch (Exception ex) { Debug.LogWarning("No se pudo respaldar la partida: " + ex.Message); return null; }
        }

        public void Borrar() { try { if (File.Exists(ruta)) File.Delete(ruta); if (File.Exists(tmp)) File.Delete(tmp); } catch { } }
    }

    // Identidad del jugador: un id aleatorio guardado en PlayerPrefs. En el online vendrá de la cuenta.
    public static class Perfil
    {
        const string CLAVE = "huerta-nueva-perfil";

        public static string Id()
        {
            var id = PlayerPrefs.GetString(CLAVE, "");
            if (string.IsNullOrEmpty(id)) { id = "j-" + Guid.NewGuid().ToString("N").Substring(0, 12); Fijar(id); }
            return id;
        }

        // Usado al continuar una partida local cuyo jugador tiene otro id (p. ej. se borraron los PlayerPrefs).
        public static void Fijar(string id) { PlayerPrefs.SetString(CLAVE, id); PlayerPrefs.Save(); }
    }
}
