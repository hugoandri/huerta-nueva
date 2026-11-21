// Lector de JSON mínimo, sin dependencias: objetos → Dictionary<string, object>, listas → List<object>,
// números → double, true/false → bool, null → null. Se usa para leer contenido.json y los tilesets.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace HuertaNueva
{
    public static class Json
    {
        public static object Leer(string texto)
        {
            int i = 0;
            return Valor(texto, ref i);
        }

        static void Espacios(string s, ref int i)
        {
            while (i < s.Length && char.IsWhiteSpace(s[i])) i++;
        }

        static object Valor(string s, ref int i)
        {
            Espacios(s, ref i);
            char c = s[i];
            if (c == '{')
            {
                i++;
                var d = new Dictionary<string, object>();
                Espacios(s, ref i);
                if (s[i] == '}') { i++; return d; }
                while (true)
                {
                    Espacios(s, ref i);
                    string clave = Cadena(s, ref i);
                    Espacios(s, ref i);
                    i++; // ':'
                    d[clave] = Valor(s, ref i);
                    Espacios(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; // '}'
                    return d;
                }
            }
            if (c == '[')
            {
                i++;
                var l = new List<object>();
                Espacios(s, ref i);
                if (s[i] == ']') { i++; return l; }
                while (true)
                {
                    l.Add(Valor(s, ref i));
                    Espacios(s, ref i);
                    if (s[i] == ',') { i++; continue; }
                    i++; // ']'
                    return l;
                }
            }
            if (c == '"') return Cadena(s, ref i);
            if (string.CompareOrdinal(s, i, "true", 0, 4) == 0) { i += 4; return true; }
            if (string.CompareOrdinal(s, i, "false", 0, 5) == 0) { i += 5; return false; }
            if (string.CompareOrdinal(s, i, "null", 0, 4) == 0) { i += 4; return null; }
            int inicio = i;
            while (i < s.Length && "+-0123456789.eE".IndexOf(s[i]) >= 0) i++;
            return double.Parse(s.Substring(inicio, i - inicio), CultureInfo.InvariantCulture);
        }

        static string Cadena(string s, ref int i)
        {
            var sb = new StringBuilder();
            i++; // comilla de apertura
            while (s[i] != '"')
            {
                char c = s[i++];
                if (c != '\\') { sb.Append(c); continue; }
                char e = s[i++];
                switch (e)
                {
                    case 'n': sb.Append('\n'); break;
                    case 't': sb.Append('\t'); break;
                    case 'r': sb.Append('\r'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'u': sb.Append((char)Convert.ToInt32(s.Substring(i, 4), 16)); i += 4; break;
                    default: sb.Append(e); break;
                }
            }
            i++; // comilla de cierre
            return sb.ToString();
        }
    }

    // Accesos cómodos y tolerantes a valores ausentes.
    public static class J
    {
        public static Dictionary<string, object> D(object o) => o as Dictionary<string, object> ?? new Dictionary<string, object>();
        public static List<object> L(object o) => o as List<object> ?? new List<object>();
        public static object V(Dictionary<string, object> d, string k) => d != null && d.TryGetValue(k, out var v) ? v : null;
        public static string S(Dictionary<string, object> d, string k, string def = null) => V(d, k) as string ?? def;
        public static double F(Dictionary<string, object> d, string k, double def = 0) => V(d, k) is double x ? x : def;
        public static int I(Dictionary<string, object> d, string k, int def = 0) => V(d, k) is double x ? (int)Math.Round(x) : def;
        public static bool B(Dictionary<string, object> d, string k, bool def = false) => V(d, k) is bool x ? x : def;
    }
}
