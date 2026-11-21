// Dibuja la zona del jugador local (granja o piso de mazmorra). Solo lee el estado: nunca lo cambia.
// Coordenadas: el núcleo usa píxeles con y hacia abajo (T = 32 px por celda); en Unity 1 unidad = 1 celda
// y la y va hacia arriba, así que el punto (px, py) del juego queda en (px / T, -py / T).
// El suelo usa Tilemaps en rejilla doble: cada pieza Wang une las esquinas de 4 celdas, por eso esas
// capas van en una rejilla corrida media celda.
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace HuertaNueva
{
    public class Vista
    {
        readonly Def def; readonly Graficos G; readonly int T, W, H;
        public Camera Camara { get; private set; }
        public float Escala { get; private set; } = 2;
        float tGlobal, herido;
        readonly Dictionary<int, float> sacudidas = new Dictionary<int, float>();
        readonly List<Vector3> golpes = new List<Vector3>();   // x, y (px) y tiempo
        readonly Dictionary<string, (float x, float y, float t, bool andando)> animJug = new Dictionary<string, (float, float, float, bool)>();

        // suelo
        readonly Tilemap baseMapa, regadoMapa; readonly Tilemap[] capas = new Tilemap[4];
        Zona zonaPintada; int firmaSuelo = int.MinValue;

        // entidades: un grupo de SpriteRenderer que se reutiliza cada cuadro
        readonly Transform raizEnt; readonly List<SpriteRenderer> pool = new List<SpriteRenderer>(); int usados;
        // luces
        Light2D luzGlobal; readonly List<Light2D> luces = new List<Light2D>(); int lucesUsadas;
        readonly Transform raizLuces;

        public float Herido => herido;

        public Vista(Def def, Graficos G)
        {
            this.def = def; this.G = G; T = def.T; W = def.W; H = def.H;
            Camara = Camera.main;
            if (Camara == null) { var c = new GameObject("Main Camera") { tag = "MainCamera" }; Camara = c.AddComponent<Camera>(); }
            Camara.orthographic = true; Camara.clearFlags = CameraClearFlags.SolidColor;
            Camara.transform.position = new Vector3(0, 0, -10);

            var mundo = new GameObject("Mundo");
            var rejilla = new GameObject("Rejilla", typeof(Grid)); rejilla.transform.SetParent(mundo.transform, false);
            var doble = new GameObject("RejillaDoble", typeof(Grid)); doble.transform.SetParent(mundo.transform, false);
            doble.transform.localPosition = new Vector3(0.5f, -0.5f, 0);
            Tilemap Mapa(GameObject padre, string nombre, int orden)
            {
                var go = new GameObject(nombre, typeof(Tilemap), typeof(TilemapRenderer));
                go.transform.SetParent(padre.transform, false);
                go.GetComponent<TilemapRenderer>().sortingOrder = orden;
                return go.GetComponent<Tilemap>();
            }
            baseMapa = Mapa(rejilla, "Base", -3000);
            for (int k = 0; k < capas.Length; k++) capas[k] = Mapa(doble, "Capa" + k, -2990 + k);
            regadoMapa = Mapa(rejilla, "Regado", -2980);

            raizEnt = new GameObject("Entidades").transform; raizEnt.SetParent(mundo.transform, false);
            raizLuces = new GameObject("Luces").transform; raizLuces.SetParent(mundo.transform, false);

            // La plantilla Universal 2D trae una "Global Light 2D" ya configurada: la usamos. Si no está, creamos una.
            foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (l.lightType == Light2D.LightType.Global && l.isActiveAndEnabled) { luzGlobal = l; break; }
            if (luzGlobal == null) luzGlobal = NuevaLuz("LuzGlobal", Light2D.LightType.Global, mundo.transform);
            luzGlobal.intensity = 1;
        }

        // Una luz creada por código no ilumina ninguna capa hasta que se le asignan: le damos todas.
        static Light2D NuevaLuz(string nombre, Light2D.LightType tipo, Transform padre)
        {
            var go = new GameObject(nombre);
            go.SetActive(false);
            go.transform.SetParent(padre, false);
            var l = go.AddComponent<Light2D>();
            l.lightType = tipo;
            var campo = typeof(Light2D).GetField("m_ApplyToSortingLayers", BindingFlags.NonPublic | BindingFlags.Instance);
            if (campo != null)
            {
                var capas = SortingLayer.layers; var ids = new int[capas.Length];
                for (int i = 0; i < capas.Length; i++) ids[i] = capas[i].id;
                campo.SetValue(l, ids);
            }
            go.SetActive(true);
            return l;
        }

        // ---------------------------------------------------------------- eventos y animación
        public void Eventos(Evento e, string yoId)
        {
            if (e.tipo == "sacudir") sacudidas[e.uid] = 0.18f;
            if (e.tipo == "golpe") golpes.Add(new Vector3(e.x, e.y, 0));
            if (e.tipo == "dano" && e.jugador == yoId) herido = 0.35f;
        }

        public void Actualizar(float dt, Estado estado)
        {
            tGlobal += dt; herido = Mathf.Max(0, herido - dt);
            foreach (var k in new List<int>(sacudidas.Keys)) { float v = sacudidas[k] - dt; if (v <= 0) sacudidas.Remove(k); else sacudidas[k] = v; }
            for (int i = golpes.Count - 1; i >= 0; i--) { var g = golpes[i]; g.z += dt; if (g.z > 0.2f) golpes.RemoveAt(i); else golpes[i] = g; }
            foreach (var j in estado.jugadores)
            {
                (float x, float y, float t, bool andando) a = animJug.TryGetValue(j.id, out var v) ? v : (j.x, j.y, 0f, false);
                bool andando = Mathf.Abs(j.x - a.x) + Mathf.Abs(j.y - a.y) > 0.01f;
                animJug[j.id] = (j.x, j.y, andando ? a.t + dt : a.t, andando);
            }
        }

        // ---------------------------------------------------------------- utilidades
        Vector3 AMundo(float px, float py, float z = 0) => new Vector3(px / T, -py / T, z);
        static Vector3Int Celda(int x, int y) => new Vector3Int(x, -y - 1, 0);

        public (int, int) ACelda(Vector2 pantalla)
        {
            var w = Camara.ScreenToWorldPoint(new Vector3(pantalla.x, pantalla.y, 10));
            return (Mathf.FloorToInt(w.x), Mathf.FloorToInt(-w.y));
        }
        public Vector2 APantalla(float px, float py) => Camara.WorldToScreenPoint(AMundo(px, py));

        SpriteRenderer Dibujar(Sprite s, Vector3 pos, int orden, bool voltear = false, float alfa = 1, Vector2? escala = null, Color? color = null)
        {
            if (usados == pool.Count)
            {
                var go = new GameObject("e"); go.transform.SetParent(raizEnt, false);
                pool.Add(go.AddComponent<SpriteRenderer>());
            }
            var r = pool[usados++];
            r.enabled = true; r.sprite = s; r.sortingOrder = orden; r.flipX = voltear;
            var c = color ?? Color.white; c.a *= alfa; r.color = c;
            r.transform.localPosition = pos;
            var e = escala ?? Vector2.one; r.transform.localScale = new Vector3(e.x, e.y, 1);
            return r;
        }

        void Luz(float px, float py, float radio, Color color, float intensidad = 1)
        {
            if (lucesUsadas == luces.Count)
            {
                var l = NuevaLuz("luz", Light2D.LightType.Point, raizLuces); l.falloffIntensity = 0.6f;
                luces.Add(l);
            }
            var luz = luces[lucesUsadas++];
            luz.enabled = true; luz.color = color; luz.intensity = intensidad;
            luz.pointLightOuterRadius = radio / T; luz.pointLightInnerRadius = radio * 0.15f / T;
            luz.transform.localPosition = AMundo(px, py);
        }

        // ---------------------------------------------------------------- suelo
        static int Valor(Def def, int[] a, int x, int y) => HuertaNueva.Mundo.Dentro(def, x, y) && Zona.En(a, HuertaNueva.Mundo.Idx(def, x, y)) ? 1 : 0;

        void PintarSuelo(Zona zona)
        {
            int firma = 17;
            unchecked
            {
                firma = firma * 31 + zona.GetHashCode();
                if (zona.suelo != null) for (int i = 0; i < zona.suelo.Length; i++) firma = firma * 31 + zona.suelo[i] * 2 + (zona.regado != null ? zona.regado[i] : 0);
            }
            if (zona == zonaPintada && firma == firmaSuelo) return;
            bool nueva = zona != zonaPintada;
            zonaPintada = zona; firmaSuelo = firma;
            bool cueva = zona.EsMazmorra;

            void Capa(Tilemap mapa, Tile[] ts, System.Func<int, int, int> v)
            {
                mapa.ClearAllTiles();
                if (ts == null) return;
                for (int j = -1; j < H; j++) for (int i = -1; i < W; i++)
                {
                    int n = v(i, j) * 8 + v(i + 1, j) * 4 + v(i, j + 1) * 2 + v(i + 1, j + 1);
                    if (n != 0) mapa.SetTile(Celda(i, j), ts[n]);
                }
            }
            Tile[] Ts(string id) => id != null && G.TS.TryGetValue(id, out var t) ? t : null;

            if (cueva)
            {
                var ts = Ts(def.MAZ.TryGetValue(zona.mazmorra, out var m) ? m.tileset : null) ?? Ts("cueva");
                if (nueva)
                {
                    baseMapa.ClearAllTiles();
                    for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) baseMapa.SetTile(Celda(x, y), ts?[0]);
                    Capa(capas[0], ts, (x, y) => HuertaNueva.Mundo.Dentro(def, x, y) ? 1 - Valor(def, zona.muro, x, y) : 0);
                    for (int k = 1; k < capas.Length; k++) capas[k].ClearAllTiles();
                }
                regadoMapa.ClearAllTiles();
                return;
            }
            if (nueva)
            {
                var cesped = Ts("tierra")?[0];
                baseMapa.ClearAllTiles();
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) baseMapa.SetTile(Celda(x, y), cesped);
                Capa(capas[0], Ts("agua"), (x, y) => Valor(def, zona.agua, x, y));
                Capa(capas[1], Ts("camino"), (x, y) => Valor(def, zona.camino, x, y));
                // el acantilado de arriba: fuera del mapa por arriba también es meseta, para que la pared sea continua
                Capa(capas[3], Ts("acantilado"), (x, y) => y < 0 ? 1 : Valor(def, zona.acantilado, x, y));
            }
            Capa(capas[2], Ts("tierra"), (x, y) => Valor(def, zona.suelo, x, y));
            regadoMapa.ClearAllTiles();
            for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                if (Valor(def, zona.suelo, x, y) == 1 && Valor(def, zona.regado, x, y) == 1) regadoMapa.SetTile(Celda(x, y), G.oscuro);
        }

        // ---------------------------------------------------------------- cuadro
        public void Dibujar(Estado estado, string yoId, bool objetivo)
        {
            var yo = estado.BuscarJugador(yoId); var zona = HuertaNueva.Mundo.ZonaDe(estado, yo); bool cueva = zona.EsMazmorra;
            usados = 0; lucesUsadas = 0;

            // cámara: zoom en múltiplos de medio píxel y siguiendo al jugador sin salir del mapa
            Escala = Mathf.Max(1, Mathf.Min(4, Mathf.Floor(Mathf.Min(Screen.width / (12f * T), Screen.height / (8f * T)) * 2) / 2));
            float vw = Screen.width / Escala, vh = Screen.height / Escala;
            float camX = W * T < vw ? (W * T - vw) / 2 : Mathf.Clamp(yo.x - vw / 2, 0, W * T - vw);
            float camY = H * T < vh ? (H * T - vh) / 2 : Mathf.Clamp(yo.y - vh / 2, 0, H * T - vh);
            camX = Mathf.Round(camX); camY = Mathf.Round(camY);
            Camara.orthographicSize = Screen.height / (2f * T * Escala);
            Camara.transform.position = new Vector3((camX + vw / 2) / T, -(camY + vh / 2) / T, -10);
            Camara.backgroundColor = cueva ? new Color(0.043f, 0.039f, 0.047f) : new Color(0.086f, 0.106f, 0.09f);

            PintarSuelo(zona);

            if (objetivo)
            {
                var (fx, fy) = Reglas.CeldaFrente(def, yo);
                Dibujar(G.marco, new Vector3(fx, -fy - 1, 0), -100);
            }

            // cultivos
            foreach (var c in zona.cultivos)
            {
                int x = c.i % W, y = c.i / W; var d = def.CULT[c.id];
                int baseY = (y + 1) * T - 2;
                if (c.dias == 0) { Dibujar(G.semillas, new Vector3(x, -y - 1, 0), baseY); continue; }
                string sp = c.dias >= d.dias ? d.sprites[2] : c.dias >= Mathf.CeilToInt(d.dias / 2f) ? d.sprites[1] : d.sprites[0];
                Dibujar(G.SPR[sp], AMundo((x + 0.5f) * T, baseY), baseY);
                if (c.dias >= d.dias) Dibujar(G.blanco, AMundo((x + 0.5f) * T - 1, y * T - 4 + Mathf.Sin(tGlobal * 4) * 1.5f), baseY + 1, false, 0.9f, new Vector2(2, 3), new Color(0.95f, 0.76f, 0.31f));
            }

            // objetos (porTileset: la pieza se pintó sobre la muralla de cada mazmorra; desfase: px bajo la huella)
            foreach (var o in zona.objetos)
            {
                var d = def.OBJ[o.id]; int w = d.ancho, h = d.alto;
                Sprite img = null; bool propia = false;
                if (d.porTileset && cueva && def.MAZ.TryGetValue(zona.mazmorra, out var mz) && G.SPR.TryGetValue($"{d.sprite}_{mz.tileset}", out var sp2)) { img = sp2; propia = true; }
                if (img == null && !G.SPR.TryGetValue(d.sprite ?? "", out img)) img = G.blanco;
                int orden = (o.y + h) * T - (d.solido ? 0 : T);
                float sac = sacudidas.TryGetValue(o.uid, out var sv) ? Mathf.Sin(sv * 80) * 2 : 0;
                float baseY = (o.y + h) * T + (d.desfase != 0 && propia ? d.desfase : 0);
                Dibujar(img, AMundo((o.x + w / 2f) * T + sac, baseY), orden);
                if (d.accion == "maquina" && Reglas.MaquinaLista(estado, o))
                {
                    var ic = G.Icono(def, o.datos.salida);
                    Dibujar(ic, AMundo((o.x + 0.5f) * T, baseY - img.rect.height - 2 + Mathf.Sin(tGlobal * 4) * 2), orden + 1, false, 1, new Vector2(0.5f, 0.5f));
                }
                if (d.luz > 0) Luz((o.x + w / 2f) * T, (o.y + 0.3f) * T, d.luz * (1 + Mathf.Sin(tGlobal * 9 + o.x) * 0.04f), new Color(1f, 0.78f, 0.55f), 1.1f);
                if (!cueva && o.id == "casa") Luz((o.x + 2) * T, (o.y + 2.6f) * T, 70, new Color(1f, 0.8f, 0.55f));
            }

            // monstruos
            foreach (var m in zona.monstruos)
            {
                var M = def.MON[m.id]; var img = G.SPR.TryGetValue(M.sprite ?? "", out var s) ? s : G.blanco;
                float bote = M.vuela ? Mathf.Sin(tGlobal * 10 + m.uid) * 4 - 10 : 0;
                float aplasta = M.vuela ? 1 : 1 + Mathf.Sin(tGlobal * 6 + m.uid) * 0.06f;
                int orden = Mathf.RoundToInt(m.y);
                Dibujar(G.sombra, AMundo(m.x, m.y), orden - 1, false, 1, new Vector2(img.rect.width * 0.6f / 20f, 1));
                Dibujar(img, AMundo(m.x, m.y + 4 + bote), orden, m.dir < 0, m.golpe > 0 ? 0.45f : 1, new Vector2(1 / aplasta, aplasta));
                if (!M.jefe && m.vida < m.vidaMax)
                {
                    Dibujar(G.blanco, AMundo(m.x - 10, m.y + 5.5f), orden + 1, false, 1, new Vector2(20, 3), Color.black);
                    Dibujar(G.blanco, AMundo(m.x - 10, m.y + 5.5f), orden + 2, false, 1, new Vector2(20 * m.vida / m.vidaMax, 3), new Color(0.89f, 0.4f, 0.31f));
                }
            }

            // jugadores
            foreach (var j in estado.jugadores)
            {
                if (HuertaNueva.Mundo.ZonaDe(estado, j) != zona) continue;
                var P = G.AspectoDe(def, j);
                var cuadros = P.caminar.TryGetValue(j.dir, out var cs) ? cs : null;
                animJug.TryGetValue(j.id, out var a);
                var img = a.andando && cuadros != null && cuadros.Count > 0 ? cuadros[(int)(a.t * 9) % cuadros.Count] : P.quieto[j.dir];
                int orden = Mathf.RoundToInt(j.y);
                Dibujar(G.sombra, AMundo(j.x, j.y), orden - 1, false, 1, new Vector2(0.9f, 0.9f));
                bool parpadeo = j.id == yoId && herido > 0 && Mathf.FloorToInt(herido * 20) % 2 == 1;
                Dibujar(img, AMundo(j.x, j.y), orden, false, parpadeo ? 0.4f : 1);
            }

            // arcos de golpe
            foreach (var g in golpes)
                Dibujar(G.golpe, AMundo(g.x, g.y - 8), 5000, false, 1 - g.z / 0.2f, Vector2.one * (1 + g.z * 4));

            // luz: de día todo claro, de noche y en la mazmorra oscuro con luces alrededor de las fuentes
            float min = estado.tiempo.min;
            float oscuridad = cueva ? 0.94f : min > 17 * 60 ? Mathf.Min(0.72f, (min - 17 * 60) / 300f * 0.72f) : 0;
            luzGlobal.intensity = 1 - oscuridad;
            luzGlobal.color = oscuridad > 0 ? Color.Lerp(Color.white, new Color(0.55f, 0.6f, 0.95f), oscuridad) : (!cueva && estado.tiempo.lluvia ? new Color(0.85f, 0.9f, 1f) : Color.white);
            if (oscuridad > 0)
            {
                float efLuz = (float)Reglas.Efecto(def, yo, "luz");
                float radioJug = cueva ? 78 * (efLuz == 0 ? 1 : efLuz) : 46;
                foreach (var j in estado.jugadores)
                    if (HuertaNueva.Mundo.ZonaDe(estado, j) == zona) Luz(j.x, j.y - 16, j.id == yoId ? radioJug : 60, new Color(1f, 0.95f, 0.85f), 0.9f);
            }

            for (int i = usados; i < pool.Count; i++) pool[i].enabled = false;
            for (int i = lucesUsadas; i < luces.Count; i++) luces[i].enabled = false;
        }
    }

}
