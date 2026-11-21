// Reglas del juego: toda modificación del estado pasa por Reglas.Aplicar(estado, def, accion).
// Es la traducción directa de js/nucleo/reglas.js. Las reglas validan lo que llega (distancias,
// materiales, dinero, vida), así que en un juego online el servidor puede usar este mismo código.
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuertaNueva
{
    public class Accion
    {
        public string tipo, jugador, dir, item, mazmorra;
        public float dx, dy, dt;
        public int x, y, i = -1, de, a, receta, n, uid, piso;
    }

    public class InfoMazmorra
    {
        public string id, nombre, descripcion, requiere;
        public int pisos, record, completada;
        public bool abierta;
        public List<int> disponibles;
    }

    public class Evento
    {
        public string tipo, jugador, txt, panel, color, titulo, jefe;
        public bool malo, bajada;
        public float x, y, n;
        public int uid, dia, total, multa, piso;
        public List<string> notas;
        public List<Cantidad> ventas, items;
        public List<InfoMazmorra> mazmorras;
        public string enCursoId;
        public int enCursoPiso;
    }

    public static class Reglas
    {
        public const float FIN_DIA = 26 * 60, MIN_POR_SEG = 10f / 7f, VELOCIDAD = 112, DT_MAX = 0.1f;
        static readonly Dictionary<string, (int, int)> VEC = new Dictionary<string, (int, int)>
        {
            ["south"] = (0, 1), ["north"] = (0, -1), ["west"] = (-1, 0), ["east"] = (1, 0),
        };

        public static (int, int) CeldaJugador(Def def, Jugador j) => ((int)Math.Floor(j.x / def.T), (int)Math.Floor((j.y - 6) / def.T));
        public static (int, int) CeldaFrente(Def def, Jugador j) { var (cx, cy) = CeldaJugador(def, j); var (dx, dy) = VEC[j.dir]; return (cx + dx, cy + dy); }
        public static int TiempoAbs(Estado e) => e.tiempo.dia * 1440 + (int)e.tiempo.min;
        public static double Efecto(Def def, Jugador j, string k) => def.ITEMS.TryGetValue(Mundo.RopaActual(j) ?? "", out var it) ? it.Efecto(k) : 0;
        static void RecalcularVida(Def def, Jugador j) { j.vidaMax = Mundo.VIDA_BASE + (float)Efecto(def, j, "vida"); j.vida = Math.Min(j.vida, j.vidaMax); }
        public static Progreso ProgresoDe(Estado e, string id)
        {
            var p = e.mazmorra.progreso.Find(x => x.id == id);
            if (p == null) { p = new Progreso { id = id }; e.mazmorra.progreso.Add(p); }
            return p;
        }
        public static bool MaquinaLista(Estado e, ObjMundo o) => !string.IsNullOrEmpty(o.datos.salida) && TiempoAbs(e) >= o.datos.listoEn;
        public static List<Jugador> JugadoresEn(Estado e, Zona z) => e.jugadores.Where(j => Mundo.ZonaDe(e, j) == z).ToList();

        public static bool Solido(Def def, Zona g, float px, float py)
        {
            int x = (int)Math.Floor(px / def.T), y = (int)Math.Floor(py / def.T);
            if (!Mundo.Dentro(def, x, y)) return true;
            int i = Mundo.Idx(def, x, y);
            if (Zona.En(g.agua, i) || Zona.En(g.muro, i) || Zona.En(g.acantilado, i)) return true;
            var o = Mundo.ObjetoEn(def, g, x, y);
            return o != null && def.OBJ[o.id].solido;
        }
        public static bool Choca(Def def, Zona g, float x, float y, float r = 8) =>
            Solido(def, g, x - r, y - 6) || Solido(def, g, x + r, y - 6) || Solido(def, g, x - r, y + 2) || Solido(def, g, x + r, y + 2);

        // ---------------------------------------------------------------- eventos
        static void Aviso(List<Evento> ev, Jugador j, string txt, bool malo = false) => ev.Add(new Evento { tipo = "aviso", jugador = j.id, txt = txt, malo = malo });
        static void Flotar(List<Evento> ev, float x, float y, string txt, string color = null) => ev.Add(new Evento { tipo = "flotar", x = x, y = y, txt = txt, color = color });
        static void Abrir(List<Evento> ev, Jugador j, string panel, int uid = 0) => ev.Add(new Evento { tipo = "abrir", jugador = j.id, panel = panel, uid = uid });

        static int DarItem(Def def, List<Evento> ev, Jugador j, string id, int n, float x, float y, string color = null)
        {
            if (n <= 0) return 0;
            int sobra = Inventario.Agregar(def, j, id, n);
            if (n - sobra > 0) Flotar(ev, x, y, $"+{n - sobra} {def.ITEMS[id].nombre}", color);
            if (sobra > 0) Aviso(ev, j, "La mochila está llena", true);
            return sobra;
        }
        static bool GastarEnergia(List<Evento> ev, Jugador j, float e)
        {
            if (j.energia < e) { Aviso(ev, j, "Estás agotada. Come algo o duerme.", true); return false; }
            j.energia = Math.Max(0, j.energia - e);
            return true;
        }

        // ---------------------------------------------------------------- punto de entrada
        public static List<Evento> Aplicar(Estado e, Def def, Accion a)
        {
            var ev = new List<Evento>();
            Jugador j = a.jugador != null ? e.BuscarJugador(a.jugador) : null;
            if (a.jugador != null && j == null) { ev.Add(new Evento { tipo = "error", txt = "Jugador desconocido" }); return ev; }
            if (j == null && a.tipo != "tiempo" && a.tipo != "dormir") { ev.Add(new Evento { tipo = "error", txt = "Falta el jugador" }); return ev; }
            switch (a.tipo)
            {
                case "mover": Mover(e, def, a, j); break;
                case "mirar": if (a.dir != null && VEC.ContainsKey(a.dir)) j.dir = a.dir; break;
                case "seleccionar": if (a.i >= 0 && a.i < 12) j.sel = a.i; break;
                case "moverInv":
                    if (a.de >= 0 && a.a >= 0 && a.de < j.inv.Count && a.a < j.inv.Count && a.de != a.a) { var t = j.inv[a.de]; j.inv[a.de] = j.inv[a.a]; j.inv[a.a] = t; }
                    break;
                case "tiempo": Tiempo(e, def, a, ev); break;
                case "dormir": NuevoDia(e, def, ev, false); break;
                case "comer": Comer(def, j, ev); break;
                case "vestir": Vestir(def, j, a.i >= 0 ? a.i : j.sel, ev); break;
                case "fabricar": Fabricar(def, j, a.receta, ev); break;
                case "comprar": Comprar(def, j, a.item, a.n, ev); break;
                case "cofreSacar": CofreSacar(e, def, j, a.uid, a.i, ev); break;
                case "cofreGuardar": CofreGuardar(e, def, j, a.uid, a.i, ev); break;
                case "entrarMazmorra": EntrarMazmorra(e, def, j, a.mazmorra, a.piso, ev); break;
                case "usar": Usar(e, def, j, a.x, a.y, ev); break;
                default: ev.Add(new Evento { tipo = "error", txt = "Acción desconocida: " + a.tipo }); break;
            }
            return ev;
        }

        // ---------------------------------------------------------------- acciones
        static void Mover(Estado e, Def def, Accion a, Jugador j)
        {
            float dx = Math.Sign(a.dx), dy = Math.Sign(a.dy), dt = Math.Min(DT_MAX, Math.Max(0, a.dt));
            if (dx == 0 && dy == 0) return;
            j.dir = Math.Abs(dx) >= Math.Abs(dy) && dx != 0 ? (dx > 0 ? "east" : "west") : (dy > 0 ? "south" : "north");
            if (dx != 0 && dy != 0) { dx *= 0.70710678f; dy *= 0.70710678f; }
            float v = VELOCIDAD * dt;
            var g = Mundo.ZonaDe(e, j);
            if (!Choca(def, g, j.x + dx * v, j.y)) j.x += dx * v;
            if (!Choca(def, g, j.x, j.y + dy * v)) j.y += dy * v;
        }

        static void Tiempo(Estado e, Def def, Accion a, List<Evento> ev)
        {
            float dt = Math.Min(DT_MAX, Math.Max(0, a.dt));
            e.tiempo.min += dt * MIN_POR_SEG;
            foreach (var p in e.jugadores) p.cdAtaque = Math.Max(0, p.cdAtaque - dt);
            if (e.mazmorra.enCurso) ActualizarMazmorra(e, def, dt, ev);
            if (e.tiempo.min >= FIN_DIA) NuevoDia(e, def, ev, true);
        }

        static void Comer(Def def, Jugador j, List<Evento> ev)
        {
            var s = j.inv[j.sel];
            ItemDef it = s != null ? def.ITEMS[s.id] : null;
            if (it != null && it.tipo == "pocion")
            {
                if (j.vida >= j.vidaMax) { Aviso(ev, j, "Tienes la vida al máximo"); return; }
                j.vida = Math.Min(j.vidaMax, j.vida + it.cura); Inventario.GastarMano(j);
                Flotar(ev, j.x, j.y - 40, $"+{it.cura / 2f} ♥", "#e2674f");
                return;
            }
            if (it == null || (it.tipo != "cultivo" && it.tipo != "artesanal")) { Aviso(ev, j, "Ten en la mano un cultivo, una conserva o una poción", true); return; }
            if (j.energia >= j.energiaMax) { Aviso(ev, j, "Tienes la energía al máximo"); return; }
            int ganancia = Math.Max(10, (int)Math.Round(it.venta / 4.0, MidpointRounding.AwayFromZero));
            j.energia = Math.Min(j.energiaMax, j.energia + ganancia);
            Inventario.GastarMano(j);
            Flotar(ev, j.x, j.y - 40, $"+{ganancia} energía", "#79c267");
        }

        // La ropa va a su puesto: de batalla (mazmorra) o de granja; la anterior vuelve a la casilla.
        static void Vestir(Def def, Jugador j, int i, List<Evento> ev)
        {
            if (i < 0 || i >= j.inv.Count) return;
            var s = j.inv[i];
            if (s == null || def.ITEMS[s.id].tipo != "ropa") return;
            var it = def.ITEMS[s.id];
            bool batalla = it.uso == "batalla";
            string anterior = batalla ? j.ropaBatalla : j.ropa;
            if (batalla) j.ropaBatalla = s.id; else j.ropa = s.id;
            j.inv[i] = anterior != null ? new Slot(anterior, 1) : null;
            RecalcularVida(def, j);
            Aviso(ev, j, $"{(batalla ? "Ropa de batalla" : "Ropa de granja")}: {it.nombre}");
            ev.Add(new Evento { tipo = "ropa", jugador = j.id });
        }

        static void Fabricar(Def def, Jugador j, int indice, List<Evento> ev)
        {
            if (indice < 0 || indice >= def.recetas.Count) return;
            var r = def.recetas[indice];
            if (!r.ingredientes.All(c => Inventario.Cuenta(j, c.id) >= c.n)) { Aviso(ev, j, "Te faltan materiales", true); return; }
            if (Inventario.EspacioPara(def, j, r.resultado) < r.cantidad) { Aviso(ev, j, "La mochila está llena", true); return; }
            foreach (var c in r.ingredientes) Inventario.Sacar(j, c.id, c.n);
            Inventario.Agregar(def, j, r.resultado, r.cantidad);
            Aviso(ev, j, $"Fabricaste: {def.ITEMS[r.resultado].nombre}{(r.cantidad > 1 ? " ×" + r.cantidad : "")}");
        }

        static void Comprar(Def def, Jugador j, string item, int n, List<Evento> ev)
        {
            if (item == null || !def.ITEMS.TryGetValue(item, out var it) || !def.tienda.Contains(item)) return;
            n = Math.Max(1, Math.Min(99, n));
            int caben = Math.Min(n, Inventario.EspacioPara(def, j, item));
            if (caben == 0) { Aviso(ev, j, "La mochila está llena", true); return; }
            if (j.dinero < it.compra * caben) { Aviso(ev, j, "No te alcanza el dinero", true); return; }
            j.dinero -= it.compra * caben;
            Inventario.Agregar(def, j, item, caben);
            Aviso(ev, j, $"Compraste {caben} {it.nombre.ToLower()}");
        }

        static void CofreSacar(Estado e, Def def, Jugador j, int uid, int i, List<Evento> ev)
        {
            var o = Mundo.ObjetoPorUid(def, Mundo.ZonaDe(e, j), uid);
            if (o == null || def.OBJ[o.id].accion != "cofre" || !Cerca(def, j, o) || o.datos.items == null || i < 0 || i >= o.datos.items.Count) return;
            var s = o.datos.items[i];
            if (s == null) return;
            s.n = Inventario.Agregar(def, j, s.id, s.n);
            if (s.n == 0) o.datos.items[i] = null; else Aviso(ev, j, "La mochila está llena", true);
        }

        static void CofreGuardar(Estado e, Def def, Jugador j, int uid, int i, List<Evento> ev)
        {
            var o = Mundo.ObjetoPorUid(def, Mundo.ZonaDe(e, j), uid);
            if (o == null || def.OBJ[o.id].accion != "cofre" || !Cerca(def, j, o) || i < 0 || i >= j.inv.Count) return;
            var s = j.inv[i];
            if (s == null) return;
            if (o.datos.items == null) { o.datos.items = new List<Slot>(); for (int k2 = 0; k2 < 12; k2++) o.datos.items.Add(null); }
            var items = o.datos.items;
            int k = Inventario.EsHerramienta(def, s.id) ? -1 : items.FindIndex(x => x != null && x.id == s.id);
            if (k < 0) k = items.FindIndex(x => x == null);
            if (k < 0) { Aviso(ev, j, "El cofre está lleno", true); return; }
            if (items[k] != null) items[k].n += s.n; else items[k] = new Slot(s.id, s.n);
            j.inv[i] = null;
        }

        // ---------------------------------------------------------------- mazmorras
        public static bool MazmorraAbierta(Estado e, MazDef cfg) => cfg.requiereMazmorra == null || ProgresoDe(e, cfg.requiereMazmorra).record >= cfg.requierePiso;
        public static List<int> PisosDisponibles(Estado e, MazDef cfg)
        {
            int record = ProgresoDe(e, cfg.id).record;
            return cfg.puntosDeControl.Where(p => p == 1 || record >= p - 1).ToList();
        }
        public static List<InfoMazmorra> InfoMazmorras(Estado e, Def def) => def.mazmorras.Select(cfg =>
        {
            var p = ProgresoDe(e, cfg.id);
            return new InfoMazmorra
            {
                id = cfg.id, nombre = cfg.nombre, descripcion = cfg.descripcion, pisos = cfg.pisos, abierta = MazmorraAbierta(e, cfg),
                requiere = cfg.requiereMazmorra != null ? $"Llega al piso {cfg.requierePiso} de {def.MAZ[cfg.requiereMazmorra].nombre}" : null,
                disponibles = PisosDisponibles(e, cfg), record = p.record, completada = p.completada,
            };
        }).ToList();

        static void EntradaDe(Def def, Zona nivel, Jugador j) { j.x = (nivel.entrada.x + 0.5f) * def.T; j.y = (nivel.entrada.y + 0.6f) * def.T; j.dir = "south"; }

        static void EntrarMazmorra(Estado e, Def def, Jugador j, string id, int piso, List<Evento> ev)
        {
            var cueva = e.granja.objetos.Find(o => o.id == "entrada_cueva");
            if (j.lugar != "granja" || cueva == null || !Cerca(def, j, cueva)) return;
            var m = e.mazmorra;
            if (!m.enCurso)
            {
                if (id == null || !def.MAZ.TryGetValue(id, out var cfg)) return;
                if (!MazmorraAbierta(e, cfg)) { Aviso(ev, j, "Esa mazmorra todavía está cerrada", true); return; }
                if (!PisosDisponibles(e, cfg).Contains(piso)) { Aviso(ev, j, "Todavía no llegaste a ese piso", true); return; }
                m.actual = cfg.id;
                m.semilla = (long)Math.Floor(Azar.Siguiente(e) * 4294967296.0);
                IrAPiso(e, def, piso, ev, false);
            }
            j.lugar = "mazmorra";
            RecalcularVida(def, j);
            EntradaDe(def, m.nivel, j);
            Aviso(ev, j, $"{def.MAZ[m.actual].nombre} · piso {m.piso} · llevas: {def.ITEMS[j.ropaBatalla].nombre}");
            ev.Add(new Evento { tipo = "ropa", jugador = j.id });
        }

        static void IrAPiso(Estado e, Def def, int piso, List<Evento> ev, bool bajada)
        {
            var m = e.mazmorra; var cfg = def.MAZ[m.actual]; var prog = ProgresoDe(e, m.actual);
            m.piso = piso;
            prog.record = Math.Max(prog.record, piso);
            m.nivel = Mazmorra.GenerarPiso(def, m.actual, m.semilla, piso);
            m.enCurso = true;
            foreach (var p in e.jugadores) if (p.lugar == "mazmorra") EntradaDe(def, m.nivel, p);
            cfg.jefes.TryGetValue(piso, out var jefe);
            ev.Add(new Evento { tipo = "piso", piso = piso, bajada = bajada, jefe = jefe != null ? def.MON[jefe].nombre : null });
        }

        static void TerminarExpedicion(Estado e) { var m = e.mazmorra; m.actual = null; m.semilla = 0; m.piso = 0; m.nivel = null; m.enCurso = false; }
        static void SalirDeMazmorra(Estado e, Def def, Jugador j)
        {
            j.lugar = "granja";
            RecalcularVida(def, j);
            Mundo.FrenteACueva(def, j);
            if (!e.jugadores.Any(p => p.lugar == "mazmorra")) TerminarExpedicion(e);
        }

        // El jugador debe estar junto al objeto (anti-trampas).
        static bool Cerca(Def def, Jugador j, ObjMundo o)
        {
            var (px, py) = CeldaJugador(def, j); var d = def.OBJ[o.id];
            int dx = px < o.x ? o.x - px : px >= o.x + d.ancho ? px - (o.x + d.ancho - 1) : 0;
            int dy = py < o.y ? o.y - py : py >= o.y + d.alto ? py - (o.y + d.alto - 1) : 0;
            return Math.Max(dx, dy) <= 1;
        }

        // ---------------------------------------------------------------- combate
        static float Dist(float ax, float ay, float bx, float by) => (float)Math.Sqrt((ax - bx) * (ax - bx) + (ay - by) * (ay - by));

        static bool Atacar(Estado e, Def def, Jugador j, Zona zona, int x, int y, List<Evento> ev)
        {
            float cx = (x + 0.5f) * def.T, cy = (y + 0.6f) * def.T;
            var blancos = zona.monstruos.Where(m => Dist(m.x, m.y, cx, cy) <= (def.MON[m.id].jefe ? 44 : 26)).ToList();
            if (blancos.Count == 0) return false;
            if (j.cdAtaque > 0) return true;
            var s = j.inv[j.sel]; ItemDef it = s != null ? def.ITEMS[s.id] : null;
            bool arma = it != null && it.tipo == "arma";
            int dano = arma ? it.dano : 1;
            if (!arma && !GastarEnergia(ev, j, 1)) return true;
            j.cdAtaque = 0.35f;
            ev.Add(new Evento { tipo = "golpe", x = cx, y = cy });
            foreach (var m in blancos)
            {
                m.vida -= dano; m.golpe = 0.15f;
                float d = Dist(m.x, m.y, j.x, j.y); if (d == 0) d = 1;
                float kx = m.x + (m.x - j.x) / d * 12, ky = m.y + (m.y - j.y) / d * 12;
                if (!def.MON[m.id].jefe && !Choca(def, zona, kx, ky, 6)) { m.x = kx; m.y = ky; }
                Flotar(ev, m.x, m.y - 30, $"-{dano}", "#f3eedb");
                if (m.vida <= 0) MorirMonstruo(e, def, j, zona, m, ev);
            }
            return true;
        }

        static void MorirMonstruo(Estado e, Def def, Jugador j, Zona zona, Monstruo m, List<Evento> ev)
        {
            var M = def.MON[m.id];
            zona.monstruos.Remove(m);
            foreach (var s in M.suelta)
                if (Azar.Siguiente(e) < s.prob) DarItem(def, ev, j, s.item, s.min + (int)Math.Floor(Azar.Siguiente(e) * (s.max - s.min + 1)), m.x, m.y - 20, "#f2c14e");
            if (!M.jefe) return;
            var cfg = def.MAZ[zona.mazmorra]; int piso = zona.piso;
            if (piso < cfg.pisos)
            {
                Mazmorra.PonerBajada(def, zona);
                foreach (var p in JugadoresEn(e, zona)) Aviso(ev, p, $"¡Venciste a {M.nombre}! Bajó una escalera hacia el piso {piso + 1}.");
                return;
            }
            // último jefe: cada jugador presente recibe su propio botín
            var presentes = JugadoresEn(e, zona);
            var reparto = Botin.Repartir(def.botines[cfg.botinFinal], presentes.Select(p => p.id), e.mazmorra.semilla);
            foreach (var p in presentes)
            {
                var items = reparto[p.id];
                foreach (var c in items) Inventario.Agregar(def, p, c.id, c.n);
                ev.Add(new Evento { tipo = "botin", jugador = p.id, titulo = $"¡{cfg.nombre} completada!", items = items });
            }
            ProgresoDe(e, cfg.id).completada++;
            Mazmorra.PonerSalidaFinal(def, zona);
        }

        // Monstruos: persiguen al jugador más cercano que ven y golpean al estar al lado.
        static void ActualizarMazmorra(Estado e, Def def, float dt, List<Evento> ev)
        {
            var zona = e.mazmorra.nivel; var jugadores = JugadoresEn(e, zona);
            if (jugadores.Count == 0) return;
            foreach (var m in zona.monstruos.ToList())
            {
                var M = def.MON[m.id];
                m.cd = Math.Max(0, m.cd - dt); m.golpe = Math.Max(0, m.golpe - dt);
                Jugador obj = null; float dist = float.MaxValue;
                foreach (var p in jugadores) { float d = Dist(p.x, p.y, m.x, m.y); if (d < dist) { dist = d; obj = p; } }
                if (obj == null || dist > M.vision * def.T) continue;
                float alcance = M.jefe ? 30 : 16;
                if (dist > alcance)
                {
                    float v = M.velocidad * dt, ux = (obj.x - m.x) / dist, uy = (obj.y - m.y) / dist, r = M.jefe ? 14 : 6;
                    if (!Choca(def, zona, m.x + ux * v, m.y, r)) m.x += ux * v;
                    if (!Choca(def, zona, m.x, m.y + uy * v, r)) m.y += uy * v;
                    if (Math.Abs(ux) > 0.1f) m.dir = Math.Sign(ux);
                }
                else if (m.cd <= 0)
                {
                    m.cd = M.jefe ? 1.4f : 1.1f;
                    float dano = Math.Max(0.5f, (float)Math.Round(M.dano * (1 - Efecto(def, obj, "defensa")) * 2, MidpointRounding.AwayFromZero) / 2f);
                    obj.vida -= dano;
                    ev.Add(new Evento { tipo = "dano", jugador = obj.id, n = dano });
                    Flotar(ev, obj.x, obj.y - 44, $"-{dano / 2f} ♥", "#e2674f");
                    if (obj.vida <= 0) Derrota(e, def, obj, ev);
                    if (!e.mazmorra.enCurso) return;
                }
            }
        }

        static void Derrota(Estado e, Def def, Jugador j, List<Evento> ev)
        {
            int multa = Math.Min(300, (int)Math.Floor(j.dinero * 0.1));
            j.dinero -= multa;
            SalirDeMazmorra(e, def, j);
            j.vida = (float)Math.Ceiling(j.vidaMax / 2);
            j.energia = Math.Min(j.energia, (float)Math.Round(j.energiaMax / 2));
            ev.Add(new Evento { tipo = "derrota", jugador = j.id, multa = multa });
        }

        // ---------------------------------------------------------------- usar la casilla de al lado
        static void Usar(Estado e, Def def, Jugador j, int x, int y, List<Evento> ev)
        {
            var g = Mundo.ZonaDe(e, j);
            if (!Mundo.Dentro(def, x, y)) return;
            var (px, py) = CeldaJugador(def, j);
            if (Math.Max(Math.Abs(x - px), Math.Abs(y - py)) > 1) return;
            var s = j.inv[j.sel]; ItemDef it = s != null ? def.ITEMS[s.id] : null;
            int i = Mundo.Idx(def, x, y);
            float cx = (x + 0.5f) * def.T, cy = y * def.T;
            bool hachaOPico = s != null && (s.id == "hacha" || s.id == "pico");

            if (g.EsMazmorra && Atacar(e, def, j, g, x, y, ev)) return;

            var o = Mundo.ObjetoEn(def, g, x, y);
            if (it != null && it.tipo == "ropa" && !(o != null && def.OBJ[o.id].accion != null)) { Vestir(def, j, j.sel, ev); return; }
            if (o != null)
            {
                var d = def.OBJ[o.id];
                switch (d.accion)
                {
                    case "dormir": Abrir(ev, j, "dormir"); return;
                    case "tienda": Abrir(ev, j, "tienda"); return;
                    case "mazmorra":
                        ev.Add(new Evento
                        {
                            tipo = "abrir", jugador = j.id, panel = "mazmorra", mazmorras = InfoMazmorras(e, def),
                            enCursoId = e.mazmorra.enCurso ? e.mazmorra.actual : null, enCursoPiso = e.mazmorra.piso,
                        });
                        return;
                    case "salir": SalirDeMazmorra(e, def, j); Aviso(ev, j, "Volviste a la granja"); return;
                    case "bajar":
                        if (e.mazmorra.piso + 1 > def.MAZ[e.mazmorra.actual].pisos) return;
                        IrAPiso(e, def, e.mazmorra.piso + 1, ev, true);
                        return;
                    case "botin":
                        Mundo.Quitar(g, o);
                        foreach (var c in Botin.Tirar(def.botines[d.botin], () => Azar.Siguiente(e))) DarItem(def, ev, j, c.id, c.n, cx, cy, "#f2c14e");
                        return;
                }
                if (d.accion == "cofre" && !hachaOPico) { Abrir(ev, j, "cofre", o.uid); return; }
                if (d.accion == "vender")
                {
                    if (it == null || it.tipo == "herramienta" || it.venta == 0) { Aviso(ev, j, "Ten en la mano lo que quieras vender", true); return; }
                    j.envios.Add(new Envio { id = s.id, n = s.n });
                    Aviso(ev, j, $"Enviaste {s.n} {it.nombre.ToLower()} · cobras al dormir");
                    j.inv[j.sel] = null;
                    return;
                }
                if (d.accion == "maquina")
                {
                    var md = o.datos;
                    if (MaquinaLista(e, o)) { if (DarItem(def, ev, j, md.salida, 1, cx, cy) == 0) o.datos = new ObjDatos(); return; }
                    if (!string.IsNullOrEmpty(md.salida)) { Aviso(ev, j, $"Listo en {(int)Math.Ceiling((md.listoEn - TiempoAbs(e)) / 60.0)} h de juego"); return; }
                    var receta = s != null ? d.procesa.Find(p => p.entrada == s.id) : null;
                    if (receta != null)
                    {
                        if (Inventario.Cuenta(j, s.id) < receta.n) { Aviso(ev, j, $"Necesitas {receta.n} {it.nombre.ToLower()}", true); return; }
                        Inventario.Sacar(j, s.id, receta.n);
                        o.datos = new ObjDatos { entrada = s.id, salida = receta.salida, listoEn = TiempoAbs(e) + receta.minutos };
                        Aviso(ev, j, $"Preparando: {def.ITEMS[receta.salida].nombre}");
                        return;
                    }
                    if (!hachaOPico) { Aviso(ev, j, "Acepta: " + string.Join(", ", d.procesa.Select(p => $"{p.n} {def.ITEMS[p.entrada].nombre.ToLower()}")), true); return; }
                }
                if (d.herramienta != null && it != null && it.tipo == "herramienta" && (d.herramienta == s.id || d.herramienta == "cualquiera"))
                {
                    double ep = Efecto(def, j, "energia_pico");
                    float coste = s.id == "pico" ? Math.Max(1, (float)Math.Round(2 * (ep == 0 ? 1 : ep), MidpointRounding.AwayFromZero)) : 2;
                    if (!GastarEnergia(ev, j, coste)) return;
                    o.golpes--;
                    ev.Add(new Evento { tipo = "sacudir", uid = o.uid });
                    if (o.golpes <= 0)
                    {
                        Mundo.Quitar(g, o);
                        foreach (var su in d.suelta) DarItem(def, ev, j, su.item, su.min + (int)Math.Floor(Azar.Siguiente(e) * (su.max - su.min + 1)), cx, cy);
                    }
                    return;
                }
                if (d.recoger && hachaOPico)
                {
                    if (o.datos.items != null && o.datos.items.Any(x2 => x2 != null)) { Aviso(ev, j, "Vacía el cofre antes de recogerlo", true); return; }
                    if (!string.IsNullOrEmpty(o.datos.salida)) { Aviso(ev, j, "Espera a que termine", true); return; }
                    if (Inventario.EspacioPara(def, j, o.id) < 1) { Aviso(ev, j, "La mochila está llena", true); return; }
                    Mundo.Quitar(g, o); DarItem(def, ev, j, o.id, 1, cx, cy);
                    return;
                }
                if (d.herramienta != null) Aviso(ev, j, d.herramienta == "cualquiera" ? "Necesitas una herramienta" : "Necesitas el " + def.ITEMS[d.herramienta].nombre.ToLower(), true);
                return;
            }

            var cult = g.CultivoEn(i);
            if (cult != null && cult.dias >= def.CULT[cult.id].dias)
            {
                string prod = def.CULT[cult.id].producto;
                if (Inventario.EspacioPara(def, j, prod) < 1) { Aviso(ev, j, "La mochila está llena", true); return; }
                g.cultivos.Remove(cult);
                DarItem(def, ev, j, prod, 1, cx, cy, "#f2c14e");
                return;
            }
            if (it == null) return;
            bool enGranja = g == e.granja;
            double ec = Efecto(def, j, "energia_campo");
            float costeCampo = Math.Max(1, (float)Math.Round(2 * (ec == 0 ? 1 : ec), MidpointRounding.AwayFromZero));

            if (s.id == "azada" && enGranja)
            {
                if (Zona.En(g.agua, i) || Zona.En(g.suelo, i)) return;
                if (Zona.En(g.camino, i)) { Aviso(ev, j, "No se puede arar el camino", true); return; }
                for (int dy = -1; dy <= 1; dy++) for (int dx = -1; dx <= 1; dx++)
                    if (Mundo.Dentro(def, x + dx, y + dy) && g.agua[Mundo.Idx(def, x + dx, y + dy)] != 0) { Aviso(ev, j, "Muy cerca del agua para arar", true); return; }
                if (!GastarEnergia(ev, j, costeCampo)) return;
                g.suelo[i] = 1;
                if (e.tiempo.lluvia) g.regado[i] = 1;
                return;
            }
            if (s.id == "regadera" && enGranja)
            {
                if (Zona.En(g.agua, i)) { j.aguaRegadera = def.ITEMS["regadera"].capacidad; Flotar(ev, cx, cy, "Regadera llena", "#5aa9e6"); return; }
                if (!Zona.En(g.suelo, i) || Zona.En(g.regado, i)) return;
                if (j.aguaRegadera <= 0) { Aviso(ev, j, "La regadera está vacía: úsala frente al estanque", true); return; }
                if (!GastarEnergia(ev, j, Math.Max(1, costeCampo / 2))) return;
                j.aguaRegadera--; g.regado[i] = 1;
                return;
            }
            if (it.tipo == "semilla" && enGranja)
            {
                if (!Zona.En(g.suelo, i)) { Aviso(ev, j, "Primero ara la tierra con la azada", true); return; }
                if (cult != null) return;
                g.cultivos.Add(new Cultivo { i = i, id = it.cultivo, dias = 0 });
                Inventario.GastarMano(j);
                return;
            }
            if (it.tipo == "colocable")
            {
                if (px == x && py == y) return;
                if (JugadoresEn(e, g).Any(o2 => { var (ox, oy) = CeldaJugador(def, o2); return ox == x && oy == y; })) return;
                if (!enGranja && it.objeto != "antorcha") { Aviso(ev, j, "En la mazmorra solo puedes colocar antorchas", true); return; }
                if (!Mundo.Libre(def, g, x, y)) { Aviso(ev, j, "No cabe ahí", true); return; }
                Mundo.Colocar(def, g, it.objeto, x, y);
                Inventario.GastarMano(j);
            }
        }

        // ---------------------------------------------------------------- el paso de los días
        static void NuevoDia(Estado e, Def def, List<Evento> ev, bool desmayo)
        {
            var g = e.granja; int W = def.W, H = def.H;
            var notas = new List<string>();
            var cobros = new Dictionary<string, (List<Cantidad> ventas, int total, int multa)>();
            foreach (var j in e.jugadores)
            {
                var ventas = new List<Cantidad>();
                foreach (var s in j.envios) { var c = ventas.Find(v => v.id == s.id); if (c != null) c.n += s.n; else ventas.Add(new Cantidad(s.id, s.n)); }
                int total = ventas.Sum(c => def.ITEMS[c.id].venta * c.n);
                j.dinero += total; j.envios.Clear();
                int multa = 0;
                if (desmayo) { multa = Math.Min(500, (int)Math.Floor(j.dinero * 0.1)); j.dinero -= multa; }
                cobros[j.id] = (ventas, total, multa);
                j.energia = desmayo ? (float)Math.Round(j.energiaMax * 0.6) : j.energiaMax;
                j.lugar = "granja";
                RecalcularVida(def, j);
                j.vida = j.vidaMax;
                Mundo.PosicionInicio(def, j);
            }
            TerminarExpedicion(e);

            foreach (var c in g.cultivos) if (g.regado[c.i] != 0 && c.dias < def.CULT[c.id].dias) c.dias++;
            bool Protegido(int k)
            {
                int x = k % W, y = k / W;
                return g.objetos.Any(o => def.OBJ[o.id].protege > 0 && Math.Sqrt((o.x - x) * (o.x - x) + (o.y - y) * (o.y - y)) <= def.OBJ[o.id].protege);
            }
            var sinProteger = g.cultivos.Where(c => !Protegido(c.i)).ToList();
            if (sinProteger.Count >= 8 && Azar.Siguiente(e) < 0.6)
            {
                var c = sinProteger[(int)Math.Floor(Azar.Siguiente(e) * sinProteger.Count)];
                notas.Add($"Un cuervo se comió una planta de {def.ITEMS[def.CULT[c.id].producto].nombre.ToLower()}. Un espantapájaros lo evita.");
                g.cultivos.Remove(c);
            }
            for (int i = 0; i < W * H; i++)
            {
                if (g.suelo[i] != 0 && g.CultivoEn(i) == null && g.regado[i] == 0 && Azar.Siguiente(e) < 0.12) g.suelo[i] = 0;
                g.regado[i] = 0;
            }

            e.tiempo.dia++;
            e.tiempo.min = Mundo.INICIO_DIA;
            e.tiempo.lluvia = Azar.Siguiente(e) < 0.2;
            if (e.tiempo.lluvia) { for (int i = 0; i < W * H; i++) if (g.suelo[i] != 0) g.regado[i] = 1; notas.Add("Llueve: la tierra arada amaneció regada."); }
            foreach (var o in g.objetos)
            {
                var d = def.OBJ[o.id];
                if (d.riega > 0)
                    foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                    {
                        int x = o.x + dx, y = o.y + dy;
                        if (Mundo.Dentro(def, x, y) && g.suelo[Mundo.Idx(def, x, y)] != 0) g.regado[Mundo.Idx(def, x, y)] = 1;
                    }
                if (d.accion == "maquina" && MaquinaLista(e, o)) notas.Add($"Tienes lista una {def.ITEMS[o.datos.salida].nombre.ToLower()}.");
            }
            for (int k = 0, n = 0; k < 200 && n < 4; k++)
            {
                int x = 2 + (int)Math.Floor(Azar.Siguiente(e) * (W - 4)), y = 3 + (int)Math.Floor(Azar.Siguiente(e) * (H - 5));
                if (!Mundo.EnClaro(x, y) && !Zona.En(g.camino, Mundo.Idx(def, x, y)) && Mundo.Colocar(def, g, n == 0 ? "roca" : "maleza", x, y) != null) n++;
            }

            foreach (var j in e.jugadores)
            {
                var c = cobros[j.id];
                var lista = new List<string>();
                if (desmayo) lista.Add($"Te desmayaste a las 2:00 y alguien te llevó a casa. Pagaste {c.multa} de atención.");
                lista.AddRange(notas);
                ev.Add(new Evento { tipo = "resumen", jugador = j.id, dia = e.tiempo.dia, ventas = c.ventas, total = c.total, notas = lista });
            }
        }
    }
}
