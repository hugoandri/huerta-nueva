// La aldea (v0.2 "Primer Mossling"): construir edificios, rescatar Mosslings en la mazmorra y su trabajo
// de cada mañana. Igual que el resto del núcleo: todo pasa por Reglas.Aplicar y se valida aquí.
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuertaNueva
{
    public static partial class Reglas
    {
        public const int RIEGO_MOSSLING = 12, LENA_MOSSLING = 6, ALCANCE_RIEGO = 10;
        static readonly string[] TRABAJOS = { "regar", "lena" };

        /// <summary>Esquina superior izquierda del edificio que se colocaría delante del jugador.</summary>
        public static (int x, int y) OrigenConstruccion(Def def, Jugador j, ObjDef od, int fx, int fy)
        {
            int w = od.ancho, h = od.alto;
            switch (j.dir)
            {
                case "north": return (fx - (w - 1) / 2, fy - (h - 1));
                case "west": return (fx - (w - 1), fy - (h - 1) / 2);
                case "east": return (fx, fy - (h - 1) / 2);
                default: return (fx - (w - 1) / 2, fy);
            }
        }

        public static bool EdificioDisponible(Estado e, Def def, EdificioDef ed) =>
            (ed.requiereEdificio == null || e.granja.objetos.Any(o => o.id == ed.requiereEdificio)) &&
            (e.residentes?.Count ?? 0) >= ed.requiereResidentes;

        /// <summary>null si se puede construir ahí; si no, el motivo para mostrárselo al jugador.</summary>
        public static string MotivoNoConstruir(Estado e, Def def, Jugador j, string id, int fx, int fy, out int x0, out int y0)
        {
            x0 = y0 = 0;
            if (id == null || !def.EDIF.TryGetValue(id, out var ed) || !def.OBJ.TryGetValue(id, out var od)) return "Unknown building";
            if (j.lugar != "granja") return "You can only build on the farm";
            var (px, py) = CeldaJugador(def, j);
            if (Math.Max(Math.Abs(fx - px), Math.Abs(fy - py)) > 1) return "Too far away";
            if (!EdificioDisponible(e, def, ed))
                return ed.requiereEdificio != null && !e.granja.objetos.Any(o => o.id == ed.requiereEdificio)
                    ? $"Build the {def.EDIF[ed.requiereEdificio].nombre} first"
                    : $"Needs {ed.requiereResidentes} Mossling{(ed.requiereResidentes > 1 ? "s" : "")} living in the glen";
            foreach (var c in ed.coste) if (Inventario.Cuenta(j, c.id) < c.n) return "Missing materials";
            (x0, y0) = OrigenConstruccion(def, j, od, fx, fy);
            var g = e.granja;
            if (!Mundo.Libre(def, g, x0, y0, od.ancho, od.alto)) return "Doesn't fit there";
            for (int y = y0; y < y0 + od.alto; y++)
                for (int x = x0; x < x0 + od.ancho; x++)
                {
                    if (Zona.En(g.camino, Mundo.Idx(def, x, y))) return "Keep the path clear";
                    foreach (var p in JugadoresEn(e, g)) { var (cx, cy) = CeldaJugador(def, p); if (cx == x && cy == y) return "Step aside first"; }
                }
            return null;
        }

        static void Construir(Estado e, Def def, Jugador j, string id, int fx, int fy, List<Evento> ev)
        {
            string motivo = MotivoNoConstruir(e, def, j, id, fx, fy, out int x0, out int y0);
            if (motivo != null) { Aviso(ev, j, motivo, true); return; }
            var ed = def.EDIF[id];
            foreach (var c in ed.coste) Inventario.Sacar(j, c.id, c.n);
            var o = Mundo.Colocar(def, e.granja, id, x0, y0);
            ev.Add(new Evento { tipo = "construido", jugador = j.id, item = id, uid = o.uid });
            ev.Add(new Evento { tipo = "accion", jugador = j.id, txt = "talar", item = "hacha", x = (x0 + 0.5f) * def.T, y = y0 * def.T });
            Aviso(ev, j, $"Built: {ed.nombre}");
            MudarResidentes(e, def, ev, j);
        }

        /// <summary>Los Mosslings sin hogar se mudan a las casitas con plaza libre.</summary>
        static void MudarResidentes(Estado e, Def def, List<Evento> ev, Jugador j)
        {
            foreach (var r in e.residentes.Where(r => r.casa == 0))
            {
                var casa = e.granja.objetos.FirstOrDefault(o => def.OBJ[o.id].hogar > 0 &&
                    e.residentes.Count(x => x.casa == o.uid) < def.OBJ[o.id].hogar);
                if (casa == null) return;
                r.casa = casa.uid;
                if (j != null) Aviso(ev, j, $"{def.RES[r.id].nombre} moved into the {def.EDIF[casa.id].nombre}!");
            }
        }

        // Demoler con hacha o pico devuelve todos los materiales (así también se "mueve" un edificio).
        static bool Demoler(Estado e, Def def, Jugador j, Zona g, ObjMundo o, float cx, float cy, List<Evento> ev)
        {
            if (!def.EDIF.TryGetValue(o.id, out var ed)) return false;
            if (o.datos.items != null && o.datos.items.Any(s => s != null)) { Aviso(ev, j, "Empty it before taking it down", true); return true; }
            if (ed.coste.Any(c => Inventario.EspacioPara(def, j, c.id) < c.n)) { Aviso(ev, j, "Backpack is full", true); return true; }
            Mundo.Quitar(g, o);
            foreach (var c in ed.coste) DarItem(def, ev, j, c.id, c.n, cx, cy);
            foreach (var r in e.residentes.Where(r => r.casa == o.uid)) r.casa = 0;
            MudarResidentes(e, def, ev, j);
            Aviso(ev, j, $"Took down the {ed.nombre} and got the materials back");
            return true;
        }

        static void Asignar(Estado e, Def def, Jugador j, int i, string trabajo, List<Evento> ev)
        {
            if (i < 0 || i >= e.residentes.Count || !TRABAJOS.Contains(trabajo)) return;
            if (trabajo == "lena" && !e.granja.objetos.Any(o => o.id == "lenera")) { Aviso(ev, j, "Build a Woodshed first", true); return; }
            var r = e.residentes[i];
            r.trabajo = trabajo;
            Aviso(ev, j, $"{def.RES[r.id].nombre} will {(trabajo == "lena" ? "stack wood in the Woodshed" : "water your crops")} from tomorrow");
        }

        // ---------------------------------------------------------------- rescate en la mazmorra
        /// <summary>Pone un farol apagado en el piso donde duerme un Mossling que todavía no se rescató.</summary>
        public static void ColocarRescates(Estado e, Def def, Zona z)
        {
            foreach (var rd in def.residentes)
            {
                if (rd.rescateMazmorra != z.mazmorra || rd.rescatePiso != z.piso) continue;
                if (e.residentes.Any(r => r.id == rd.id) || z.objetos.Any(o => o.id == "farol_apagado")) continue;
                int sx = z.salaFinal.x, sy = z.salaFinal.y;
                for (int rad = 1; rad <= 4; rad++)
                {
                    bool puesto = false;
                    for (int dy = -rad; dy <= rad && !puesto; dy++)
                        for (int dx = -rad; dx <= rad && !puesto; dx++)
                            if (Math.Max(Math.Abs(dx), Math.Abs(dy)) == rad && Mundo.Colocar(def, z, "farol_apagado", sx + dx, sy + dy) != null) puesto = true;
                    if (puesto) break;
                }
            }
        }

        static void Rescatar(Estado e, Def def, Jugador j, Zona g, ObjMundo o, ItemDef it, List<Evento> ev)
        {
            var rd = def.residentes.FirstOrDefault(r => r.rescateMazmorra == g.mazmorra && r.rescatePiso == g.piso && !e.residentes.Any(x => x.id == r.id));
            if (rd == null) { Mundo.Quitar(g, o); return; }
            if (it == null || it.id != "antorcha") { Aviso(ev, j, "Something is sleeping inside this dark lantern. Hold a torch to relight it.", true); return; }
            Inventario.GastarMano(j);
            Mundo.Quitar(g, o);
            e.residentes.Add(new Residente { id = rd.id, trabajo = rd.trabajo });
            ev.Add(new Evento { tipo = "rescate", jugador = j.id, item = rd.id, x = (o.x + 0.5f) * def.T, y = o.y * def.T });
            Flotar(ev, (o.x + 0.5f) * def.T, o.y * def.T - 10, $"{rd.nombre} woke up!", "#6fd3ff");
            MudarResidentes(e, def, ev, j);
            Aviso(ev, j, e.residentes.Last().casa != 0
                ? $"{rd.nombre} the Mossling follows your light home!"
                : $"{rd.nombre} the Mossling follows your light home. Build a Moss Hut so it can move in!");
            DesbloquearHito(e, def, ev, "rescate:" + rd.id);
        }

        // ---------------------------------------------------------------- cada mañana
        static void TrabajoDeLaManana(Estado e, Def def, List<string> notas)
        {
            var g = e.granja; int W = def.W;
            foreach (var r in e.residentes)
            {
                string nombre = def.RES[r.id].nombre;
                var casa = r.casa != 0 ? Mundo.ObjetoPorUid(def, g, r.casa) : null;
                if (casa == null) { r.casa = 0; r.siesta = false; notas.Add($"{nombre} is still waiting for a Moss Hut."); continue; }

                // comer: un cultivo o una conserva del Shared Table
                bool comio = false;
                foreach (var mesa in g.objetos.Where(o => o.id == "comedero" && o.datos.items != null))
                {
                    int k = mesa.datos.items.FindIndex(s => s != null && def.ITEMS.TryGetValue(s.id, out var it) && (it.tipo == "cultivo" || it.tipo == "artesanal"));
                    if (k < 0) continue;
                    if (--mesa.datos.items[k].n <= 0) mesa.datos.items[k] = null;
                    comio = true; break;
                }
                r.siesta = !comio;
                if (!comio) { notas.Add($"{nombre} found nothing on the Shared Table and napped all day. Leave a crop or a preserve there."); continue; }

                var od = def.OBJ[casa.id];
                float hx = casa.x + od.ancho / 2f, hy = casa.y + od.alto / 2f;
                if (r.trabajo == "lena")
                {
                    var lenera = g.objetos.FirstOrDefault(o => o.id == "lenera");
                    if (lenera == null) { r.trabajo = "regar"; }
                    else
                    {
                        if (lenera.datos.items == null) { lenera.datos.items = new List<Slot>(); for (int s = 0; s < 12; s++) lenera.datos.items.Add(null); }
                        int idx = lenera.datos.items.FindIndex(s => s != null && s.id == "madera" && s.n + LENA_MOSSLING <= Inventario.PILA_MAX);
                        if (idx < 0) idx = lenera.datos.items.FindIndex(s => s == null);
                        if (idx < 0) { notas.Add($"The Woodshed is full, so {nombre} took the day off."); continue; }
                        if (lenera.datos.items[idx] == null) lenera.datos.items[idx] = new Slot("madera", LENA_MOSSLING); else lenera.datos.items[idx].n += LENA_MOSSLING;
                        notas.Add($"{nombre} stacked {LENA_MOSSLING} wood in the Woodshed.");
                        continue;
                    }
                }
                // regar: las casillas aradas sin regar más cercanas a su casita
                var porRegar = new List<int>();
                for (int i = 0; i < g.suelo.Length; i++) if (g.suelo[i] != 0 && g.regado[i] == 0) porRegar.Add(i);
                double Dist2(int i) { double dx = i % W + 0.5 - hx, dy = i / W + 0.5 - hy; return dx * dx + dy * dy; }
                var elegidas = porRegar.Where(i => Dist2(i) <= ALCANCE_RIEGO * ALCANCE_RIEGO).OrderBy(Dist2).ThenBy(i => i).Take(RIEGO_MOSSLING).ToList();
                foreach (int i in elegidas) g.regado[i] = 1;
                notas.Add(elegidas.Count > 0 ? $"{nombre} watered {elegidas.Count} tilled tile{(elegidas.Count > 1 ? "s" : "")} near its hut."
                                             : $"{nombre} had nothing to water near its hut today.");
            }
        }
    }
}
