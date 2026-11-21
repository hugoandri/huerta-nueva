// Dónde se guarda la partida: un archivo JSON en Application.persistentDataPath.
// Para el online se agrega un AlmacenNube con la misma interfaz (Cargar/Guardar/Borrar/Existe).
using System;
using System.IO;
using UnityEngine;

namespace HuertaNueva
{
    public class AlmacenLocal
    {
        readonly string ruta;
        public AlmacenLocal(string archivo = "huerta-nueva.json") { ruta = Path.Combine(Application.persistentDataPath, archivo); }

        public bool Existe() => File.Exists(ruta);

        public Estado Cargar()
        {
            try
            {
                if (!File.Exists(ruta)) return null;
                var e = JsonUtility.FromJson<Estado>(File.ReadAllText(ruta));
                if (e == null || e.version != Mundo.VERSION_ESTADO || e.granja == null || e.jugadores.Count == 0) return null;
                e.Normalizar();
                return e;
            }
            catch (Exception ex) { Debug.LogWarning("No se pudo cargar la partida: " + ex.Message); return null; }
        }

        public bool Guardar(Estado e)
        {
            try
            {
                var tmp = ruta + ".tmp";
                File.WriteAllText(tmp, JsonUtility.ToJson(e));
                if (File.Exists(ruta)) File.Delete(ruta);
                File.Move(tmp, ruta);
                return true;
            }
            catch (Exception ex) { Debug.LogWarning("No se pudo guardar: " + ex.Message); return false; }
        }

        public void Borrar() { try { if (File.Exists(ruta)) File.Delete(ruta); } catch { } }
    }

    // Identidad del jugador: un id aleatorio guardado en PlayerPrefs. En el online vendrá de la cuenta.
    public static class Perfil
    {
        public static string Id()
        {
            var id = PlayerPrefs.GetString("huerta-nueva-perfil", "");
            if (string.IsNullOrEmpty(id)) { id = "j-" + Guid.NewGuid().ToString("N").Substring(0, 12); PlayerPrefs.SetString("huerta-nueva-perfil", id); PlayerPrefs.Save(); }
            return id;
        }
    }
}
