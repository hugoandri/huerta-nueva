// Carga los gráficos de PixelLab desde Resources/HuertaNueva (los copia herramientas/unity/sincronizar.py).
// Lo que falte se dibuja provisional, así un contenido nuevo se puede probar antes de generar su sprite.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace HuertaNueva
{
    // Un juego de sprites del personaje: quieto y caminando en las 4 direcciones.
    public class JuegoPersonaje
    {
        public Dictionary<string, Sprite> quieto = new Dictionary<string, Sprite>();
        public Dictionary<string, List<Sprite>> caminar = new Dictionary<string, List<Sprite>>();
    }

    public class Graficos
    {
        public const float PPU = 32;
        public static readonly string[] DIRS = { "south", "west", "east", "north" };
        const string R = "HuertaNueva/";

        public readonly Dictionary<string, Sprite> SPR = new Dictionary<string, Sprite>();          // pivote abajo al centro
        public readonly Dictionary<string, Tile[]> TS = new Dictionary<string, Tile[]>();            // 16 piezas Wang por tileset
        public readonly Dictionary<string, Dictionary<string, JuegoPersonaje>> PJS = new Dictionary<string, Dictionary<string, JuegoPersonaje>>();
        public Sprite blanco, sombra, semillas, marco, corazon, golpe;
        public Tile oscuro;
        public int nReales, nProv;

        public static Sprite Crear(Texture2D t, Vector2 pivote) => Sprite.Create(t, new Rect(0, 0, t.width, t.height), pivote, PPU, 0, SpriteMeshType.FullRect);
        static Texture2D Textura(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var vacio = new Color32[w * h];
            t.SetPixels32(vacio);
            return t;
        }
        // y medida desde arriba, como en un lienzo.
        static void Pixel(Texture2D t, int x, int y, Color c) { if (x >= 0 && y >= 0 && x < t.width && y < t.height) t.SetPixel(x, t.height - 1 - y, c); }
        static void Caja(Texture2D t, int x, int y, int w, int h, Color c) { for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Pixel(t, xx, yy, c); }

        static Color H(int x) => new Color((x >> 16 & 255) / 255f, (x >> 8 & 255) / 255f, (x & 255) / 255f);
        static Color ColorDe(string id, float s = 0.42f, float v = 0.6f) => Color.HSVToRGB(Azar.HashTexto(id) % 360 / 360f, s, v);

        static Texture2D Provisional(string id, int w, int h)
        {
            var t = Textura(w, h); int m = Mathf.Max(2, Mathf.RoundToInt(Mathf.Min(w, h) * 0.12f));
            Caja(t, m, m, w - 2 * m, h - 2 * m, new Color(0.08f, 0.08f, 0.08f));
            Caja(t, m + 1, m + 1, w - 2 * m - 2, h - 2 * m - 2, ColorDe(id));
            t.Apply();
            return t;
        }

        Tile[] TilesProvisionales(Color bajo, Color alto)
        {
            var r = Azar.Mulberry32(3); var res = new Tile[16]; int T = 32, h = 16;
            for (int i = 0; i < 16; i++)
            {
                var t = Textura(T, T);
                Caja(t, 0, 0, T, T, bajo);
                if ((i & 8) != 0) Caja(t, 0, 0, h, h, alto); if ((i & 4) != 0) Caja(t, h, 0, h, h, alto);
                if ((i & 2) != 0) Caja(t, 0, h, h, h, alto); if ((i & 1) != 0) Caja(t, h, h, h, h, alto);
                for (int k = 0; k < 10; k++) Caja(t, (int)(r() * T), (int)(r() * T), 2, 1, new Color(0, 0, 0, 0.12f));
                t.Apply();
                res[i] = TileDe(Crear(t, new Vector2(0.5f, 0.5f)));
            }
            return res;
        }
        static Tile TileDe(Sprite s) { var tile = ScriptableObject.CreateInstance<Tile>(); tile.sprite = s; tile.colliderType = Tile.ColliderType.None; return tile; }

        // Personaje provisional: una figurita de colores.
        static JuegoPersonaje PersonajeProvisional()
        {
            var P = new JuegoPersonaje();
            Sprite Figura(string dir, int paso)
            {
                var t = Textura(48, 48); int p = paso == 1 ? -3 : paso == 3 ? 3 : 0;
                Caja(t, 18, 36 + p, 5, 8, H(0x4a3222)); Caja(t, 25, 36 - p, 5, 8, H(0x4a3222));
                Caja(t, 15, 22, 18, 16, H(0x3f7d4a));
                Caja(t, 17, 10, 14, 13, H(0xf0c9a0));
                Caja(t, 13, 10, 22, 4, H(0xe3c46a)); Caja(t, 18, 5, 12, 6, H(0xe3c46a));
                var ojo = H(0x222222);
                if (dir == "south") { Caja(t, 21, 17, 2, 2, ojo); Caja(t, 26, 17, 2, 2, ojo); }
                if (dir == "east") Caja(t, 27, 17, 2, 2, ojo);
                if (dir == "west") Caja(t, 19, 17, 2, 2, ojo);
                t.Apply();
                return Crear(t, new Vector2(0.5f, 4f / 48f));
            }
            foreach (var d in DIRS) { P.quieto[d] = Figura(d, 0); P.caminar[d] = new List<Sprite> { Figura(d, 1), Figura(d, 0), Figura(d, 3), Figura(d, 0) }; }
            return P;
        }

        static Texture2D Cargar(string ruta) => Resources.Load<Texture2D>(R + ruta);

        public Graficos(Def def)
        {
            var info = J.D(Json.Leer(Resources.Load<TextAsset>(R + "graficos")?.text ?? "{}"));

            foreach (var s in def.sprites.Values)
            {
                var t = Cargar("sprites/" + s.id);
                if (t != null) nReales++; else { nProv++; t = Provisional(s.id, s.ancho, s.alto); }
                SPR[s.id] = Crear(t, new Vector2(0.5f, 0));
            }

            var colores = new Dictionary<string, (Color, Color)>
            {
                ["tierra"] = (H(0x5d9e3f), H(0x6b4a2f)),
                ["agua"] = (H(0x5d9e3f), H(0x3d7fc4)),
                ["cueva"] = (H(0x2a2420), H(0x8a7058)),
                ["camino"] = (H(0x5d9e3f), H(0xb08a5a)),
                ["acantilado"] = (H(0x5d9e3f), H(0x6f6a5c)),
            };
            foreach (var id in def.tilesets)
            {
                var tiles = new Tile[16]; bool ok = true;
                for (int i = 0; i < 16 && ok; i++)
                {
                    var t = Cargar($"tilesets/{id}/{i}");
                    if (t == null) ok = false; else tiles[i] = TileDe(Crear(t, new Vector2(0.5f, 0.5f)));
                }
                if (ok) nReales++;
                else
                {
                    nProv++;
                    var c = colores.TryGetValue(id, out var cc) ? cc : (H(0x5d9e3f), ColorDe(id, 0.4f, 0.4f));
                    tiles = TilesProvisionales(c.Item1, c.Item2);
                }
                TS[id] = tiles;
            }

            // PJS[cuerpo][aspecto]: cada personaje (granjera, granjero) con su ropa base y sus variantes.
            var pjs = J.D(J.V(info, "personajes"));
            foreach (var c in def.personajes)
            {
                var juegos = PJS[c.id] = new Dictionary<string, JuegoPersonaje>();
                foreach (var kv in J.D(J.V(pjs, c.id)))
                {
                    var jp = CargarPersonaje($"personajes/{c.id}/{kv.Key}/", J.D(kv.Value));
                    if (jp != null) { juegos[kv.Key] = jp; nReales++; } else nProv++;
                }
                if (!juegos.ContainsKey("base")) { juegos["base"] = PersonajeProvisional(); nProv++; }
            }

            // piezas pequeñas dibujadas por código
            var tb = Textura(1, 1); tb.SetPixel(0, 0, Color.white); tb.Apply(); blanco = Crear(tb, new Vector2(0, 0.5f));
            var ts = Textura(20, 8);
            for (int y = 0; y < 8; y++) for (int x = 0; x < 20; x++) { float dx = (x + 0.5f - 10) / 10f, dy = (y + 0.5f - 4) / 4f; if (dx * dx + dy * dy <= 1) Pixel(ts, x, y, new Color(0, 0, 0, 0.25f)); }
            ts.Apply(); sombra = Crear(ts, new Vector2(0.5f, 0.5f));
            var tsem = Textura(32, 32);
            foreach (var (a, b) in new[] { (11, 14), (18, 12), (15, 19), (21, 18) }) Caja(tsem, a, b, 2, 2, H(0x2b1a0e));
            tsem.Apply(); semillas = Crear(tsem, new Vector2(0, 0));
            var tm = Textura(32, 32); var amarillo = new Color(0.95f, 0.76f, 0.31f, 0.85f);
            for (int k = 1; k < 31; k++) if (k % 5 < 3) { Pixel(tm, k, 1, amarillo); Pixel(tm, k, 30, amarillo); Pixel(tm, 1, k, amarillo); Pixel(tm, 30, k, amarillo); }
            tm.Apply(); marco = Crear(tm, new Vector2(0, 0));
            string[] cor = { ".XX.XX.", "XXXXXXX", "XXXXXXX", ".XXXXX.", "..XXX..", "...X..." };
            var tc = Textura(7, 6);
            for (int y = 0; y < 6; y++) for (int x = 0; x < 7; x++) if (cor[y][x] == 'X') Pixel(tc, x, y, Color.white);
            tc.Apply(); corazon = Crear(tc, new Vector2(0.5f, 0.5f));
            var tg = Textura(24, 12);
            for (int y = 0; y < 12; y++) for (int x = 0; x < 24; x++) { float dx = (x + 0.5f - 12) / 12f, dy = (y + 0.5f - 12) / 12f, r2 = dx * dx + dy * dy; if (r2 <= 1 && r2 >= 0.6f) Pixel(tg, x, y, Color.white); }
            tg.Apply(); golpe = Crear(tg, new Vector2(0.5f, 0));
            var to = Textura(32, 32); Caja(to, 0, 0, 32, 32, new Color(0.15f, 0.086f, 0.04f, 0.38f)); to.Apply();
            oscuro = TileDe(Crear(to, new Vector2(0.5f, 0.5f)));
        }

        JuegoPersonaje CargarPersonaje(string carpeta, Dictionary<string, object> info)
        {
            var P = new JuegoPersonaje();
            var cuadros = J.D(J.V(info, "cuadros"));
            foreach (var d in DIRS)
            {
                var t = Cargar(carpeta + d);
                if (t == null) return null;
                // el pie es la fila opaca más baja: el pivote queda ahí para que el personaje se apoye en el suelo
                int pie = J.I(info, "pie", Mathf.RoundToInt(t.height * 0.85f));
                var piv = new Vector2(0.5f, (t.height - pie) / (float)t.height);
                P.quieto[d] = Crear(t, piv);
                P.caminar[d] = new List<Sprite>();
                for (int i = 0; i < J.I(cuadros, d); i++) { var c = Cargar($"{carpeta}caminar_{d}_{i}"); if (c != null) P.caminar[d].Add(Crear(c, piv)); }
            }
            return P;
        }

        // Sprites según el personaje y la ropa que lleva en el lugar donde está.
        public JuegoPersonaje AspectoDe(Def def, Jugador j)
        {
            if (!PJS.TryGetValue(j.cuerpo ?? "", out var juego)) foreach (var v in PJS.Values) { juego = v; break; }
            var ropa = Mundo.RopaActual(j);
            string aspecto = ropa != null && def.ITEMS.TryGetValue(ropa, out var it) ? it.aspecto : null;
            return aspecto != null && juego.TryGetValue(aspecto, out var p) ? p : juego["base"];
        }

        public Sprite Icono(Def def, string item) => def.ITEMS.TryGetValue(item, out var it) && it.sprite != null && SPR.TryGetValue(it.sprite, out var s) ? s : blanco;
    }
}
