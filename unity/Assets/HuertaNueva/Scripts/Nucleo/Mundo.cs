// Mundo: celdas, objetos, la granja y los jugadores. Misma lógica que js/nucleo/mundo.js.
using System;
using System.Collections.Generic;

namespace HuertaNueva
{
    public static class Mundo
    {
        public const int VERSION_ESTADO = 1;          // versión del guardado de Unity (independiente de la web)
        public const float VIDA_BASE = 10;            // medios corazones
        public const int INICIO_DIA = 6 * 60;
        public const int FILAS_ACANTILADO = 2;
        public static readonly Punto CASA = new Punto(3, 5), CAJA = new Punto(10, 7), PUESTO = new Punto(12, 6);
        public static readonly Punto ENTRADA_CUEVA = new Punto(22, FILAS_ACANTILADO);

        public static int Idx(Def def, int x, int y) => y * def.W + x;
        public static bool Dentro(Def def, int x, int y) => x >= 0 && y >= 0 && x < def.W && y < def.H;

        public static void PosicionInicio(Def def, Jugador j) { j.x = 6 * def.T; j.y = 8.6f * def.T; j.dir = "south"; }
        public static void FrenteACueva(Def def, Jugador j) { j.x = (ENTRADA_CUEVA.x + 1) * def.T; j.y = (ENTRADA_CUEVA.y + 1.6f) * def.T; j.dir = "south"; }
        public static bool EnClaro(int x, int y) => (x >= 2 && x <= 17 && y >= 2 && y <= 11) || (x >= 19 && x <= 26 && y >= 2 && y <= 6);

        // ---------------------------------------------------------------- ocupación
        static void Ocupacion(Def def, Zona g)
        {
            if (g.ocupaVer == g.ver && g.ocupa != null) return;
            g.ocupa = new int[def.W * def.H];
            g.porUid = new Dictionary<int, ObjMundo>();
            foreach (var o in g.objetos)
            {
                g.porUid[o.uid] = o;
                var d = def.OBJ[o.id];
                for (int y = o.y; y < o.y + d.alto; y++) for (int x = o.x; x < o.x + d.ancho; x++) if (Dentro(def, x, y)) g.ocupa[Idx(def, x, y)] = o.uid;
            }
            g.ocupaVer = g.ver;
        }

        public static ObjMundo ObjetoEn(Def def, Zona g, int x, int y)
        {
            if (!Dentro(def, x, y)) return null;
            Ocupacion(def, g);
            return g.porUid.TryGetValue(g.ocupa[Idx(def, x, y)], out var o) ? o : null;
        }
        public static ObjMundo ObjetoPorUid(Def def, Zona g, int uid) { Ocupacion(def, g); return g.porUid.TryGetValue(uid, out var o) ? o : null; }

        public static bool Libre(Def def, Zona g, int x, int y, int w = 1, int h = 1)
        {
            Ocupacion(def, g);
            for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++)
            {
                if (!Dentro(def, xx, yy)) return false;
                int i = Idx(def, xx, yy);
                if (Zona.En(g.agua, i) || g.ocupa[i] != 0 || Zona.En(g.suelo, i) || g.CultivoEn(i) != null || Zona.En(g.muro, i) || Zona.En(g.acantilado, i)) return false;
            }
            return true;
        }

        // enPared: escaleras y puertas que van dentro de una muralla (la celda es muro, no suelo libre).
        public static ObjMundo Colocar(Def def, Zona g, string id, int x, int y, ObjDatos datos = null, bool enPared = false)
        {
            var d = def.OBJ[id];
            if (enPared ? ObjetoEn(def, g, x, y) != null : !Libre(def, g, x, y, d.ancho, d.alto)) return null;
            var o = new ObjMundo { uid = g.sigUid++, id = id, x = x, y = y, golpes = d.golpes, datos = datos ?? new ObjDatos() };
            g.objetos.Add(o); g.ver++;
            return o;
        }
        public static void Quitar(Zona g, ObjMundo o) { g.objetos.Remove(o); g.ver++; }

        // ---------------------------------------------------------------- granja
        public static List<Punto> CeldasCamino()
        {
            var c = new List<Punto>();
            for (int y = 3; y <= 10; y++) foreach (int x in new[] { 22, 23 }) c.Add(new Punto(x, y));
            for (int x = 5; x <= 23; x++) foreach (int y in new[] { 9, 10 }) c.Add(new Punto(x, y));
            foreach (int x in new[] { 5, 6 }) c.Add(new Punto(x, 8));
            return c;
        }

        public static void TrazarBordes(Def def, Zona g)
        {
            g.acantilado = new int[def.W * def.H];
            g.camino = new int[def.W * def.H];
            for (int y = 0; y < FILAS_ACANTILADO; y++) for (int x = 0; x < def.W; x++) g.acantilado[Idx(def, x, y)] = 1;
            foreach (var p in CeldasCamino()) g.camino[Idx(def, p.x, p.y)] = 1;
            g.ver++;
        }

        public static Zona CrearGranja(Def def, uint semilla, string dueno)
        {
            int W = def.W, H = def.H;
            var r = Azar.Mulberry32(semilla);
            var g = new Zona { tipo = "granja", dueno = dueno, agua = new int[W * H], suelo = new int[W * H], regado = new int[W * H] };
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                double e = Math.Pow((x - 35) / 6.0, 2) + Math.Pow((y - 23) / 4.2, 2) + Math.Sin(x * 1.7 + y) * 0.08;
                if (e < 1) g.agua[Idx(def, x, y)] = 1;
            }
            for (int x = 0; x < W; x += 2) if (x < 19 || x > 26) Colocar(def, g, "arbol", x, 1);
            TrazarBordes(def, g);
            Colocar(def, g, "casa", CASA.x, CASA.y); Colocar(def, g, "caja_envios", CAJA.x, CAJA.y); Colocar(def, g, "puesto", PUESTO.x, PUESTO.y);
            Colocar(def, g, "entrada_cueva", ENTRADA_CUEVA.x, ENTRADA_CUEVA.y);
            bool Claro(int x, int y) => EnClaro(x, y) || g.camino[Idx(def, x, y)] != 0;
            bool ArbolCerca(int x, int y)
            {
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (ObjetoEn(def, g, x + dx, y + dy)?.id == "arbol") return true;
                return false;
            }
            for (int x = 0; x < W; x += 2) Colocar(def, g, "arbol", x + (x % 4 != 0 ? 1 : 0), H - 1);
            for (int y = 3; y < H - 1; y += 2) { Colocar(def, g, "arbol", 0, y); Colocar(def, g, "arbol", W - 1, y); }
            void Esparcir(string id, int n)
            {
                for (int k = 0, intentos = 0; k < n && intentos < 4000; intentos++)
                {
                    int x = Azar.Entre(r(), 2, W - 3), y = Azar.Entre(r(), 3, H - 3);
                    if (Claro(x, y) || !Libre(def, g, x, y)) continue;
                    bool cercaAgua = false;
                    for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++) if (Dentro(def, x + dx, y + dy) && g.agua[Idx(def, x + dx, y + dy)] != 0) cercaAgua = true;
                    if (cercaAgua && id != "maleza") continue;
                    if (id == "arbol" && ArbolCerca(x, y)) continue;
                    Colocar(def, g, id, x, y); k++;
                }
            }
            Esparcir("arbol", 26); Esparcir("roca", 45); Esparcir("maleza", 90);
            return g;
        }

        // ---------------------------------------------------------------- jugadores
        public static Zona ZonaDe(Estado e, Jugador j) => j.lugar == "mazmorra" && e.mazmorra.enCurso ? e.mazmorra.nivel : e.granja;
        public static string RopaActual(Jugador j) => j.lugar == "mazmorra" ? j.ropaBatalla : j.ropa;

        public static Jugador NuevoJugador(Def def, string id, string nombre, string cuerpo)
        {
            var c = def.personajes.Find(p => p.id == cuerpo) ?? def.personajes[0];
            var j = new Jugador
            {
                id = id, nombre = nombre ?? c.nombre, cuerpo = c.id, lugar = "granja",
                energia = 100, energiaMax = 100, vida = VIDA_BASE, vidaMax = VIDA_BASE,
                ropa = "ropa_granjera", ropaBatalla = "ropa_aventurera", dinero = def.dineroInicio,
                aguaRegadera = def.ITEMS.TryGetValue("regadera", out var reg) && reg.capacidad > 0 ? reg.capacidad : 20,
            };
            PosicionInicio(def, j);
            for (int i = 0; i < Inventario.TAM; i++) j.inv.Add(null);
            foreach (var c2 in def.inventarioInicio) Inventario.Agregar(def, j, c2.id, c2.n);
            return j;
        }

        public static Estado CrearEstado(Def def, uint semilla, string jugadorId, string cuerpo)
        {
            var e = new Estado { version = VERSION_ESTADO, semilla = semilla, paso = 0 };
            e.tiempo.dia = 1; e.tiempo.min = INICIO_DIA;
            e.granja = CrearGranja(def, semilla, jugadorId);
            e.jugadores.Add(NuevoJugador(def, jugadorId, null, cuerpo));
            return e;
        }
    }
}
