// Generación de pisos de mazmorra (misma lógica que js/nucleo/mazmorra.js): reproducible por semilla.
using System;
using System.Collections.Generic;
using System.Linq;

namespace HuertaNueva
{
    public static class Mazmorra
    {
        class Sala { public int x, y, w, h; }

        static int EntreR(Func<double> r, int a, int b) => a + (int)Math.Floor(r() * (b - a + 1));
        static Punto Centro(Sala s) => new Punto(s.x + s.w / 2, s.y + s.h / 2);

        static Rango Ponderado(Func<double> r, List<Rango> lista)
        {
            double total = lista.Sum(x => x.peso), p = r() * total;
            foreach (var x in lista) { p -= x.peso; if (p < 0) return x; }
            return lista[lista.Count - 1];
        }

        // Tramo recto en la pared de arriba de una sala: 5 columnas con 3 filas de muro encima y 2 de suelo debajo.
        static Punto PuntoEnPared(Def def, Zona z, Sala s)
        {
            bool M(int x, int y) => z.muro[Mundo.Idx(def, x, y)] != 0;
            int c = s.x + s.w / 2;
            foreach (int d in new[] { 0, 1, -1, 2, -2, 3, -3 })
            {
                int x = c + d;
                if (x - 2 < s.x || x + 2 > s.x + s.w - 1 || s.y < 3) continue;
                bool ok = true;
                for (int dx = -2; dx <= 2 && ok; dx++)
                    ok = M(x + dx, s.y - 3) && M(x + dx, s.y - 2) && M(x + dx, s.y - 1) && !M(x + dx, s.y) && !M(x + dx, s.y + 1);
                if (ok && !z.objetos.Any(o => o.x == x && o.y == s.y - 1)) return new Punto(x, s.y - 1);
            }
            return null;
        }

        public static Monstruo CrearMonstruo(Def def, Zona z, string id, int cx, int cy, int piso, int prestigio = 0)
        {
            var m = def.MON[id];
            double baseEsc = (z.mazmorra != null && def.MAZ.TryGetValue(z.mazmorra, out var mz) ? mz.escalaBase : 1) + prestigio * 0.25;
            double escala = baseEsc + (piso - 1) * 0.15;
            float vida = (float)Math.Round(m.vida * (m.jefe ? 1 + prestigio * 0.25 : escala), MidpointRounding.AwayFromZero);
            var mo = new Monstruo { uid = z.sigUid++, id = id, x = (cx + 0.5f) * def.T, y = (cy + 0.8f) * def.T, vida = vida, vidaMax = vida, cd = 1, dir = 1 };
            z.monstruos.Add(mo);
            return mo;
        }

        // Escalera de mano al piso siguiente ("escalera_bajar") o, en el último piso, el arco de salida.
        static void PonerEnSalaFinal(Def def, Zona z, string id)
        {
            if (z.hayBajada && Mundo.Colocar(def, z, id, z.bajada.x, z.bajada.y, null, true) != null) return;
            int x = z.salaFinal.x, y = z.salaFinal.y;
            for (int r = 0; r < 4; r++) for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                if (Mundo.Colocar(def, z, id, x + dx, y + dy) != null) return;
        }
        public static void PonerBajada(Def def, Zona z) => PonerEnSalaFinal(def, z, "escalera_bajar");
        public static void PonerSalidaFinal(Def def, Zona z) => PonerEnSalaFinal(def, z, "escalera_subir");

        public static Zona GenerarPiso(Def def, string mazmorra, long semilla, int piso, int prestigio = 0)
        {
            int W = def.W, H = def.H;
            var cfg = def.MAZ[mazmorra];
            var r = Azar.Mulberry32(Azar.HashTexto($"{mazmorra}:{semilla}:{piso}"));
            var z = new Zona
            {
                tipo = "mazmorra", mazmorra = mazmorra, piso = piso,
                agua = new int[W * H], suelo = new int[W * H], regado = new int[W * H], muro = new int[W * H], enSala = new int[W * H],
            };
            for (int i = 0; i < W * H; i++) z.muro[i] = 1;
            void Cavar(int x, int y) { if (x > 0 && y > 0 && x < W - 1 && y < H - 1) z.muro[Mundo.Idx(def, x, y)] = 0; }

            var salas = new List<Sala>();
            int objetivo = Math.Min(11, 6 + piso / 2);
            for (int k = 0; k < 300 && salas.Count < objetivo; k++)
            {
                int w = EntreR(r, 5, 9), h = EntreR(r, 4, 7), x = EntreR(r, 2, W - w - 3), y = EntreR(r, 3, H - h - 3);
                if (salas.Any(s => x < s.x + s.w + 2 && x + w + 2 > s.x && y < s.y + s.h + 2 && y + h + 2 > s.y)) continue;
                salas.Add(new Sala { x = x, y = y, w = w, h = h });
            }
            foreach (var s in salas) for (int y = s.y; y < s.y + s.h; y++) for (int x = s.x; x < s.x + s.w; x++) { Cavar(x, y); z.enSala[Mundo.Idx(def, x, y)] = 1; }
            for (int i = 1; i < salas.Count; i++)
            {
                Punto a = Centro(salas[i - 1]), b = Centro(salas[i]);
                void Hor(int y, int x0, int x1) { for (int x = Math.Min(x0, x1); x <= Math.Max(x0, x1); x++) { Cavar(x, y); Cavar(x, y + 1); } }
                void Ver(int x, int y0, int y1) { for (int y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++) { Cavar(x, y); Cavar(x + 1, y); } }
                if (r() < 0.5) { Hor(a.y, a.x, b.x); Ver(b.x, a.y, b.y); } else { Ver(a.x, a.y, b.y); Hor(b.y, a.x, b.x); }
            }

            var conPared = salas.Where(s => PuntoEnPared(def, z, s) != null).ToList();
            var inicio = conPared.Count > 0 ? conPared[0] : salas[0];
            var ci = Centro(inicio);
            double Lejos(Sala s) { var c = Centro(s); return Math.Sqrt(Math.Pow(c.x - ci.x, 2) + Math.Pow(c.y - ci.y, 2)); }
            var candidatas = (conPared.Count > 1 ? conPared : salas).Where(s => s != inicio).ToList();
            var final = candidatas[0];
            foreach (var s in candidatas) if (Lejos(s) > Lejos(final)) final = s;
            z.salaFinal = Centro(final);
            var salida = PuntoEnPared(def, z, inicio);
            z.entrada = salida != null ? new Punto(salida.x, salida.y + 1) : new Punto(ci.x, ci.y + 1);
            if (piso == 1)
            {
                if (salida != null) Mundo.Colocar(def, z, "escalera_subir", salida.x, salida.y, null, true);
                else Mundo.Colocar(def, z, "escalera_subir", ci.x, ci.y - 1);
            }
            var bajada = PuntoEnPared(def, z, final);
            z.hayBajada = bajada != null;
            z.bajada = bajada ?? new Punto(0, 0);

            cfg.jefes.TryGetValue(piso, out var jefe);
            if (jefe != null) CrearMonstruo(def, z, jefe, z.salaFinal.x, z.salaFinal.y, piso, prestigio);
            else if (piso < cfg.pisos) PonerBajada(def, z);

            Punto CeldaEnSala(Sala s, int margen = 1) => new Punto(EntreR(r, s.x + margen, s.x + s.w - 1 - margen), EntreR(r, s.y + margen, s.y + s.h - 1 - margen));
            var otras = salas.Where(s => s != inicio).ToList();

            foreach (var s in salas) { var c = CeldaEnSala(s, 0); Mundo.Colocar(def, z, "antorcha", c.x, c.y); }
            var rocas = cfg.rocas.Where(x => piso >= x.desde && piso <= x.hasta).ToList();
            for (int k = 0, n = 0; k < 200 && n < 10 + piso; k++)
            {
                var s = otras[EntreR(r, 0, otras.Count - 1)]; var c = CeldaEnSala(s);
                if (s == final && jefe != null) continue;
                if (Mundo.Colocar(def, z, Ponderado(r, rocas).id, c.x, c.y) != null) n++;
            }
            if (r() < 0.6) for (int k = 0; k < 30; k++)
            {
                var s = otras[EntreR(r, 0, otras.Count - 1)]; var c = CeldaEnSala(s);
                if (s != final && Mundo.Colocar(def, z, "cofre_mazmorra", c.x, c.y) != null) break;
            }

            var tipos = cfg.monstruos.Where(x => piso >= x.desde && piso <= x.hasta).ToList();
            int cuantos = Math.Min(12, 3 + piso);
            for (int k = 0, n = 0; k < 200 && n < cuantos; k++)
            {
                var s = otras[EntreR(r, 0, otras.Count - 1)]; var c = CeldaEnSala(s);
                if (z.objetos.Any(o => o.x == c.x && o.y == c.y)) continue;
                CrearMonstruo(def, z, tipos[EntreR(r, 0, tipos.Count - 1)].id, c.x, c.y, piso, prestigio); n++;
            }
            return z;
        }
    }
}
