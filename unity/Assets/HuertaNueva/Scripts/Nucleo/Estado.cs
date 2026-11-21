// Estado del juego. Clases [Serializable] con campos públicos para guardarlo con JsonUtility.
// JsonUtility no guarda null en listas de clases: una casilla vacía se guarda como Slot con id vacío
// y Normalizar() la vuelve a null al cargar.
using System;
using System.Collections.Generic;

namespace HuertaNueva
{
    [Serializable] public class Slot { public string id; public int n; public Slot() { } public Slot(string id, int n) { this.id = id; this.n = n; } }
    [Serializable] public class Punto { public int x, y; public Punto() { } public Punto(int x, int y) { this.x = x; this.y = y; } }

    [Serializable]
    public class ObjDatos
    {
        public List<Slot> items;              // cofres (12 casillas)
        public string entrada, salida;        // máquinas
        public int listoEn;
        public bool Vacio => string.IsNullOrEmpty(salida) && (items == null || items.TrueForAll(s => s == null));
    }

    [Serializable]
    public class ObjMundo
    {
        public int uid, x, y, golpes;
        public string id;
        public ObjDatos datos = new ObjDatos();
    }

    [Serializable] public class Cultivo { public int i, dias; public string id; }

    [Serializable]
    public class Monstruo
    {
        public int uid;
        public string id;
        public float x, y, vida, vidaMax, cd, dir = 1, golpe;
    }

    // Una zona: la granja o un piso de mazmorra. Misma forma para que mover, usar y colocar sean iguales.
    [Serializable]
    public class Zona
    {
        public string tipo = "granja", mazmorra, dueno;
        public int piso, ver, sigUid = 1;
        public List<ObjMundo> objetos = new List<ObjMundo>();
        public List<Cultivo> cultivos = new List<Cultivo>();
        public List<Monstruo> monstruos = new List<Monstruo>();
        public int[] agua, suelo, regado, muro, acantilado, camino, enSala;
        public Punto entrada, salaFinal, bajada;
        public bool hayBajada;

        [NonSerialized] public int ocupaVer = -1;
        [NonSerialized] public int[] ocupa;
        [NonSerialized] public Dictionary<int, ObjMundo> porUid;

        public bool EsMazmorra => tipo == "mazmorra";
        public static bool En(int[] a, int i) => a != null && i >= 0 && i < a.Length && a[i] != 0;
        public Cultivo CultivoEn(int i) => cultivos.Find(c => c.i == i);
    }

    [Serializable]
    public class Envio { public string id; public int n; }

    [Serializable]
    public class Jugador
    {
        public string id, nombre, cuerpo, dir = "south", lugar = "granja", ropa, ropaBatalla;
        public float x, y, energia, energiaMax, vida, vidaMax, cdAtaque;
        public int dinero, sel, aguaRegadera;
        public List<Slot> inv = new List<Slot>();
        public List<Envio> envios = new List<Envio>();
    }

    [Serializable] public class Progreso { public string id; public int record, completada; }

    [Serializable]
    public class EstadoMazmorra
    {
        public string actual;
        public long semilla;
        public int piso;
        public bool enCurso;                  // hay un piso generado (nivel válido)
        public Zona nivel;
        public List<Progreso> progreso = new List<Progreso>();
    }

    [Serializable] public class Tiempo { public int dia = 1; public float min; public bool lluvia; }

    [Serializable]
    public class Estado
    {
        public int version;
        public long semilla;
        public int paso;
        public Tiempo tiempo = new Tiempo();
        public Zona granja;
        public EstadoMazmorra mazmorra = new EstadoMazmorra();
        public List<Jugador> jugadores = new List<Jugador>();

        public Jugador BuscarJugador(string id) => jugadores.Find(j => j.id == id);

        // Tras cargar con JsonUtility: casillas vacías a null y cofres vacíos a null.
        public void Normalizar()
        {
            foreach (var j in jugadores) for (int i = 0; i < j.inv.Count; i++) if (j.inv[i] != null && string.IsNullOrEmpty(j.inv[i].id)) j.inv[i] = null;
            void Limpiar(Zona z)
            {
                if (z == null) return;
                foreach (var o in z.objetos)
                {
                    if (o.datos == null) o.datos = new ObjDatos();
                    if (o.datos.items != null)
                    {
                        if (o.datos.items.Count == 0) o.datos.items = null;
                        else for (int i = 0; i < o.datos.items.Count; i++) if (o.datos.items[i] != null && string.IsNullOrEmpty(o.datos.items[i].id)) o.datos.items[i] = null;
                    }
                }
                z.ocupaVer = -1;
            }
            Limpiar(granja);
            if (mazmorra.enCurso) Limpiar(mazmorra.nivel); else mazmorra.nivel = null;
        }
    }
}
