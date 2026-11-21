// Botín propio: cada jugador sortea el suyo, reproducible a partir de la semilla de la mazmorra.
using System;
using System.Collections.Generic;

namespace HuertaNueva
{
    public static class Botin
    {
        public static List<Cantidad> Tirar(TablaBotin t, Func<double> r)
        {
            var res = new List<Cantidad>();
            void Sumar(string item, int n) { var c = res.Find(x => x.id == item); if (c != null) c.n += n; else res.Add(new Cantidad(item, n)); }
            foreach (var g in t.garantizado) Sumar(g.id, g.n);
            int total = 0; foreach (var x in t.entradas) total += x.peso;
            for (int k = 0; k < t.tiradas; k++)
            {
                double p = r() * total;
                EntradaBotin elegida = t.entradas[t.entradas.Count - 1];
                foreach (var x in t.entradas) { p -= x.peso; if (p < 0) { elegida = x; break; } }
                Sumar(elegida.item, elegida.min + (int)Math.Floor(r() * (elegida.max - elegida.min + 1)));
            }
            return res;
        }

        public static Dictionary<string, List<Cantidad>> Repartir(TablaBotin t, IEnumerable<string> jugadores, long semillaMazmorra)
        {
            var reparto = new Dictionary<string, List<Cantidad>>();
            foreach (var id in jugadores) reparto[id] = Tirar(t, Azar.Mulberry32(Azar.HashTexto($"{semillaMazmorra}:{id}")));
            return reparto;
        }
    }
}
