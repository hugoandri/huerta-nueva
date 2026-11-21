using System;

namespace HuertaNueva
{
    public static class Inventario
    {
        public const int TAM = 24, PILA_MAX = 999;

        public static bool EsHerramienta(Def def, string id) => def.ITEMS.TryGetValue(id, out var it) && it.tipo == "herramienta";

        // Agrega n unidades; devuelve las que no cupieron.
        public static int Agregar(Def def, Jugador j, string id, int n = 1)
        {
            var inv = j.inv;
            if (!EsHerramienta(def, id))
                foreach (var s in inv) if (s != null && s.id == id && n > 0) { int k = Math.Min(n, PILA_MAX - s.n); s.n += k; n -= k; }
            for (int i = 0; i < inv.Count && n > 0; i++)
                if (inv[i] == null) { int k = EsHerramienta(def, id) ? 1 : Math.Min(n, PILA_MAX); inv[i] = new Slot(id, k); n -= k; }
            return n;
        }

        public static int Cuenta(Jugador j, string id)
        {
            int t = 0;
            foreach (var s in j.inv) if (s != null && s.id == id) t += s.n;
            return t;
        }

        public static void Sacar(Jugador j, string id, int n)
        {
            for (int i = j.inv.Count - 1; i >= 0 && n > 0; i--)
            {
                var s = j.inv[i];
                if (s != null && s.id == id) { int k = Math.Min(n, s.n); s.n -= k; n -= k; if (s.n == 0) j.inv[i] = null; }
            }
        }

        public static int EspacioPara(Def def, Jugador j, string id)
        {
            int t = 0;
            if (EsHerramienta(def, id)) { foreach (var s in j.inv) if (s == null) t++; return t; }
            foreach (var s in j.inv) t += s == null ? PILA_MAX : s.id == id ? PILA_MAX - s.n : 0;
            return t;
        }

        public static void GastarMano(Jugador j, int n = 1)
        {
            var s = j.inv[j.sel];
            if (s == null) return;
            s.n -= n;
            if (s.n <= 0) j.inv[j.sel] = null;
        }
    }
}
