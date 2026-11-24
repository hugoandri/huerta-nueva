// Dibuja la zona del jugador local (granja o piso de mazmorra), las animaciones completas
// de personalidad de los protagonistas (respirar, caminar, golpear, picar, talar, arar, regar, cosechar),
// la herramienta/arma en mano con arco de golpe y partículas pixel-art.
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace HuertaNueva
{
    public class Vista
    {
        struct AccionAnim { public string tipo, item; public float t, dur, tx, ty; }
        class Particula { public float x, y, vx, vy, t, dur, tam; public Color color; }

        readonly Def def; readonly Graficos G; readonly int T, W, H;
        public Camera Camara { get; private set; }
        public float Escala { get; private set; } = 2;
        float tGlobal, herido, empujon;
        readonly Dictionary<int, float> sacudidas = new Dictionary<int, float>();
        readonly List<int> clavesSacudida = new List<int>();
        readonly List<Vector3> golpes = new List<Vector3>();
        readonly Dictionary<string, (float x, float y, float t, bool andando)> animJug = new Dictionary<string, (float, float, float, bool)>();
        readonly Dictionary<string, AccionAnim> animAccion = new Dictionary<string, AccionAnim>();
        readonly List<string> clavesAccion = new List<string>();
        readonly List<Particula> particulas = new List<Particula>();

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

            foreach (var l in Object.FindObjectsByType<Light2D>(FindObjectsSortMode.None))
                if (l.lightType == Light2D.LightType.Global && l.isActiveAndEnabled) { luzGlobal = l; break; }
            if (luzGlobal == null) luzGlobal = NuevaLuz("LuzGlobal", Light2D.LightType.Global, mundo.transform);
            luzGlobal.intensity = 1;
        }

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
            if (e.tipo == "sacudir") { sacudidas[e.uid] = 0.18f; empujon = 0.16f; }
            if (e.tipo == "golpe") { golpes.Add(new Vector3(e.x, e.y, 0)); empujon = 0.18f; }
            if (e.tipo == "dano" && e.jugador == yoId) herido = 0.35f;
            if (e.tipo == "accion" && !string.IsNullOrEmpty(e.jugador))
            {
                float dur = e.txt == "cosechar" ? 0.38f : 0.32f;
                animAccion[e.jugador] = new AccionAnim { tipo = e.txt, item = e.item, t = 0, dur = dur, tx = e.x, ty = e.y };
                if (e.jugador == yoId) empujon = 0.18f;
                EmitirParticulasAccion(e.txt, e.x, e.y + 10);
            }
        }

        void EmitirParticulasAccion(string tipo, float x, float y)
        {
            int n = tipo == "cosechar" ? 10 : 8;
            for (int i = 0; i < n; i++)
            {
                float ang = Random.Range(0f, Mathf.PI * 2f), vel = Random.Range(24f, 68f);
                Color col = tipo == "picar" ? (i % 3 == 0 ? new Color(0.95f, 0.78f, 0.35f) : new Color(0.75f, 0.78f, 0.82f))
                    : tipo == "talar" ? (i % 2 == 0 ? new Color(0.68f, 0.45f, 0.24f) : new Color(0.45f, 0.8f, 0.36f))
                    : tipo == "arar" ? (i % 2 == 0 ? new Color(0.46f, 0.31f, 0.18f) : new Color(0.32f, 0.21f, 0.12f))
                    : tipo == "regar" ? (i % 2 == 0 ? new Color(0.36f, 0.76f, 0.96f) : new Color(0.78f, 0.95f, 1f))
                    : tipo == "cosechar" ? (i % 2 == 0 ? new Color(0.98f, 0.82f, 0.32f) : new Color(0.55f, 0.9f, 0.45f))
                    : new Color(1f, 0.92f, 0.65f);
                particulas.Add(new Particula
                {
                    x = x + Random.Range(-5f, 5f),
                    y = y + Random.Range(-5f, 5f),
                    vx = Mathf.Cos(ang) * vel,
                    vy = Mathf.Sin(ang) * vel - Random.Range(18f, 42f),
                    t = 0,
                    dur = Random.Range(0.24f, 0.44f),
                    tam = Random.Range(2f, 3.5f),
                    color = col,
                });
            }
        }

        public void Actualizar(float dt, Estado estado)
        {
            tGlobal += dt; herido = Mathf.Max(0, herido - dt); empujon = Mathf.Max(0, empujon - dt);
            if (sacudidas.Count > 0)
            {
                clavesSacudida.Clear();
                foreach (var kv in sacudidas) clavesSacudida.Add(kv.Key);
                for (int i = 0; i < clavesSacudida.Count; i++)
                {
                    int k = clavesSacudida[i]; float v = sacudidas[k] - dt;
                    if (v <= 0) sacudidas.Remove(k); else sacudidas[k] = v;
                }
            }
            if (animAccion.Count > 0)
            {
                clavesAccion.Clear();
                foreach (var kv in animAccion) clavesAccion.Add(kv.Key);
                for (int i = 0; i < clavesAccion.Count; i++)
                {
                    string k = clavesAccion[i]; var a = animAccion[k]; a.t += dt;
                    if (a.t >= a.dur) animAccion.Remove(k); else animAccion[k] = a;
                }
            }
            for (int i = particulas.Count - 1; i >= 0; i--)
            {
                var p = particulas[i]; p.t += dt;
                if (p.t >= p.dur) { particulas.RemoveAt(i); continue; }
                p.x += p.vx * dt; p.y += p.vy * dt; p.vy += 150f * dt;
            }
            for (int i = golpes.Count - 1; i >= 0; i--) { var g = golpes[i]; g.z += dt; if (g.z > 0.22f) golpes.RemoveAt(i); else golpes[i] = g; }
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

        SpriteRenderer Dibujar(Sprite s, Vector3 pos, int orden, bool voltear = false, float alfa = 1, Vector2? escala = null, Color? color = null, float rotZ = 0)
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
            r.transform.localRotation = rotZ != 0 ? Quaternion.Euler(0, 0, rotZ) : Quaternion.identity;
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
                var s = yo.inv[yo.sel];
                ObjDef objCol = s != null && def.ITEMS.TryGetValue(s.id, out var itCol) && itCol.tipo == "colocable" && itCol.objeto != null && def.OBJ.TryGetValue(itCol.objeto, out var od) ? od : null;
                bool bloqueado = objCol != null && !HuertaNueva.Mundo.Libre(def, zona, fx, fy);
                if (objCol != null && objCol.riega > 0)
                {
                    var dirs = objCol.riega >= 2
                        ? new[] { (1, 0), (-1, 0), (0, 1), (0, -1), (1, 1), (1, -1), (-1, 1), (-1, -1) }
                        : new[] { (1, 0), (-1, 0), (0, 1), (0, -1) };
                    foreach (var (dx, dy) in dirs)
                        if (HuertaNueva.Mundo.Dentro(def, fx + dx, fy + dy))
                            Dibujar(G.blanco, AMundo((fx + dx) * T + 2, (fy + dy + 0.5f) * T), -110, false, 0.26f, new Vector2(T - 4, T - 4), new Color(0.35f, 0.66f, 0.9f));
                }
                else if (objCol != null && objCol.protege > 0)
                {
                    int r = objCol.protege;
                    for (int dy = -r; dy <= r; dy++) for (int dx = -r; dx <= r; dx++)
                        if (dx * dx + dy * dy <= r * r && HuertaNueva.Mundo.Dentro(def, fx + dx, fy + dy))
                            Dibujar(G.blanco, AMundo((fx + dx) * T + 1, (fy + dy + 0.5f) * T), -110, false, 0.16f, new Vector2(T - 2, T - 2), new Color(0.47f, 0.76f, 0.4f));
                }
                Dibujar(G.marco, new Vector3(fx, -fy - 1, 0), -100, false, 1, null, bloqueado ? new Color(0.89f, 0.4f, 0.31f) : Color.white);
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

            // objetos
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
                else if (d.accion == "maquina" && !string.IsNullOrEmpty(o.datos?.salida))
                {
                    int ahora = Reglas.TiempoAbs(estado), ini = o.datos.inicioEn > 0 ? o.datos.inicioEn : (o.datos.listoEn - 180);
                    float p = Mathf.Clamp((ahora - ini) / (float)Mathf.Max(1, o.datos.listoEn - ini), 0.08f, 0.98f);
                    float bx = (o.x + 0.5f) * T - 11, by = baseY - img.rect.height - 6;
                    Dibujar(G.blanco, AMundo(bx - 1, by), orden + 1, false, 0.7f, new Vector2(24, 5), Color.black);
                    Dibujar(G.blanco, AMundo(bx, by), orden + 2, false, 1f, new Vector2(Mathf.Round(22 * p), 3), new Color(0.95f, 0.76f, 0.31f));
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

            // jugadores con todas sus animaciones de acción y herramienta en mano
            foreach (var j in estado.jugadores)
            {
                if (HuertaNueva.Mundo.ZonaDe(estado, j) != zona) continue;
                var P = G.AspectoDe(def, j);
                animJug.TryGetValue(j.id, out var a);
                bool enAccion = animAccion.TryGetValue(j.id, out var acc);

                Sprite img;
                float progAcc = 0;
                if (enAccion)
                {
                    progAcc = Mathf.Clamp01(acc.t / Mathf.Max(0.01f, acc.dur));
                    var clip = P.ClipAccion(acc.tipo, j.dir);
                    if (clip != null && clip.Count > 0)
                        img = clip[Mathf.Min(clip.Count - 1, Mathf.FloorToInt(progAcc * clip.Count))];
                    else
                        img = P.quieto[j.dir];
                }
                else if (a.andando && P.caminar.TryGetValue(j.dir, out var cs) && cs.Count > 0)
                {
                    img = cs[(int)(a.t * 9) % cs.Count];
                }
                else if (P.idle.TryGetValue(j.dir, out var idl) && idl.Count > 1)
                {
                    img = idl[(int)(tGlobal * 4.5f) % idl.Count];
                }
                else
                {
                    img = P.quieto[j.dir];
                }

                int orden = Mathf.RoundToInt(j.y);
                float emp = enAccion ? Mathf.Sin(progAcc * Mathf.PI) * 6f : (j.id == yoId && empujon > 0 ? Mathf.Sin((empujon / 0.18f) * Mathf.PI) * 5f : 0f);
                float ex = emp > 0 ? (j.dir == "east" ? emp : j.dir == "west" ? -emp : 0) : 0;
                float ey = emp > 0 ? (j.dir == "south" ? emp : j.dir == "north" ? -emp : 0) : 0;
                float resp = !a.andando && !enAccion ? Mathf.Sin(tGlobal * 3.2f) * 0.6f : 0f;

                // Squash & stretch expresivo durante la acción (talar, picar, arar, cosechar, golpear)
                Vector2 escJug = Vector2.one;
                if (enAccion)
                {
                    float onda = Mathf.Sin(progAcc * Mathf.PI);
                    if (acc.tipo == "cosechar") escJug = new Vector2(1 + onda * 0.08f, 1 - onda * 0.08f);
                    else escJug = new Vector2(1 - onda * 0.05f, 1 + onda * 0.07f);
                }

                Dibujar(G.sombra, AMundo(j.x + ex, j.y + ey), orden - 1, false, 1, new Vector2(0.9f, 0.9f));
                bool parpadeo = j.id == yoId && herido > 0 && Mathf.FloorToInt(herido * 20) % 2 == 1;
                Dibujar(img, AMundo(j.x + ex, j.y + ey + resp), orden, false, parpadeo ? 0.4f : 1, escJug);

                // Dibujo de la herramienta, espada o cultivo en las manos durante la acción
                if (enAccion && !string.IsNullOrEmpty(acc.item) && def.ITEMS.TryGetValue(acc.item, out var itAcc) && itAcc.sprite != null)
                {
                    DibujarHerramientaEnMano(j, acc, progAcc, ex, ey, orden, itAcc.sprite);
                }
            }

            // arcos de golpe y partículas de acción
            foreach (var g in golpes)
                Dibujar(G.golpe, AMundo(g.x, g.y - 8), 5000, false, 1 - g.z / 0.22f, Vector2.one * (1 + g.z * 4));
            foreach (var p in particulas)
                Dibujar(G.blanco, AMundo(p.x, p.y), 5005, false, 1 - p.t / p.dur, new Vector2(p.tam, p.tam), p.color);

            // luz ambiental y linternas
            float min = estado.tiempo.min;
            float oscuridad = cueva ? 0.94f : min > 17 * 60 ? Mathf.Min(0.72f, (min - 17 * 60) / 300f * 0.72f) : 0;
            luzGlobal.intensity = 1 - oscuridad;
            luzGlobal.color = oscuridad > 0 ? Color.Lerp(Color.white, new Color(0.55f, 0.6f, 0.95f), oscuridad) : (!cueva && estado.tiempo.lluvia ? new Color(0.85f, 0.9f, 1f) : Color.white);
            if (oscuridad > 0)
            {
                foreach (var j in estado.jugadores)
                {
                    if (HuertaNueva.Mundo.ZonaDe(estado, j) != zona) continue;
                    float efLuz = (float)Reglas.Efecto(def, j, "luz");
                    float radioJug = cueva ? 78 * (efLuz == 0 ? 1 : efLuz) : 46;
                    Luz(j.x, j.y - 16, radioJug, new Color(1f, 0.95f, 0.85f), 0.9f);
                }
            }

            for (int i = usados; i < pool.Count; i++) pool[i].enabled = false;
            for (int i = lucesUsadas; i < luces.Count; i++) luces[i].enabled = false;
        }

        void DibujarHerramientaEnMano(Jugador j, AccionAnim acc, float prog, float ex, float ey, int ordenBase, string idSprite)
        {
            if (acc.tipo == "cosechar")
            {
                if (!G.SPR.TryGetValue(idSprite, out var spCrop)) return;
                // Primero se agacha a recoger (prog < 0.35) y luego alza la cosecha con alegría sobre su cabeza
                float tLev = Mathf.Clamp01((prog - 0.22f) / 0.78f);
                float arco = Mathf.Sin(tLev * Mathf.PI);
                float dyCrop = Mathf.Lerp(-12f, -44f, arco);
                float esc = Mathf.Lerp(0.55f, 0.92f, Mathf.Sin(prog * Mathf.PI));
                Dibujar(spCrop, AMundo(j.x + ex, j.y + ey + dyCrop), ordenBase + 3, false, 1, new Vector2(esc, esc));
                return;
            }

            bool oeste = j.dir == "west";
            if (!G.SPR_CENTRO.TryGetValue(idSprite, out var spTool)) return;

            int ord = j.dir == "north" ? ordenBase - 1 : ordenBase + 2;

            if (acc.tipo == "regar")
            {
                float onda = Mathf.Sin(prog * Mathf.PI);
                float dxR = j.dir == "east" ? 14f + onda * 3f : oeste ? -14f - onda * 3f : 0f;
                float dyR = j.dir == "south" ? -16f + onda * 3f : j.dir == "north" ? -28f - onda * 2f : -20f + onda * 2f;
                float incl = onda * (oeste ? 32f : -32f);
                Dibujar(spTool, AMundo(j.x + ex + dxR, j.y + ey + dyR), ord, oeste, 1, new Vector2(0.82f, 0.82f), null, incl);
                return;
            }

            // Curva de golpe con anticipación (0..0.28) -> impacto rápido (0.28..0.68) -> recuperación (0.68..1)
            float faseGolpe = prog < 0.28f
                ? -0.35f * Mathf.Sin((prog / 0.28f) * Mathf.PI * 0.5f)
                : Mathf.Sin(((prog - 0.28f) / 0.72f) * Mathf.PI * 0.85f);
            float tImpacto = Mathf.Clamp01((prog - 0.2f) / 0.55f);

            float dx, dy, rot;
            if (idSprite == "azada")
            {
                // Azada: el mango está arriba y los dientes apuntan hacia abajo en el sprite original
                if (j.dir == "east")
                {
                    dx = Mathf.Lerp(6f, 15f, tImpacto);
                    dy = Mathf.Lerp(-28f, -18f, tImpacto);
                    rot = Mathf.Lerp(75f, 15f, tImpacto);
                }
                else if (oeste)
                {
                    dx = Mathf.Lerp(-6f, -15f, tImpacto);
                    dy = Mathf.Lerp(-28f, -18f, tImpacto);
                    rot = Mathf.Lerp(-75f, -15f, tImpacto);
                }
                else if (j.dir == "north")
                {
                    dx = 4f;
                    dy = Mathf.Lerp(-32f, -24f, tImpacto);
                    rot = Mathf.Lerp(140f, 175f, tImpacto);
                }
                else
                {
                    dx = 3f;
                    dy = Mathf.Lerp(-30f, -16f, tImpacto);
                    rot = Mathf.Lerp(35f, -8f, tImpacto);
                }
            }
            else
            {
                // Hacha, pico y espadas: empuñadura abajo-izquierda (o abajo-derecha en SPR_MANO_IZQ) y hoja a 45°
                if (j.dir == "east")
                {
                    dx = Mathf.Lerp(4f, 14f, tImpacto);
                    dy = Mathf.Lerp(-26f, -17f, tImpacto);
                    rot = Mathf.Lerp(55f, -65f, tImpacto);
                }
                else if (oeste)
                {
                    dx = Mathf.Lerp(-4f, -14f, tImpacto);
                    dy = Mathf.Lerp(-26f, -17f, tImpacto);
                    rot = Mathf.Lerp(-55f, 65f, tImpacto);
                }
                else if (j.dir == "north")
                {
                    dx = Mathf.Lerp(6f, -5f, tImpacto);
                    dy = Mathf.Lerp(-24f, -30f, tImpacto);
                    rot = Mathf.Lerp(-20f, 75f, tImpacto);
                }
                else
                {
                    // Mirando al sur: barrido claro desde el hombro derecho hacia delante y abajo
                    dx = Mathf.Lerp(8f, -4f, tImpacto);
                    dy = Mathf.Lerp(-27f, -15f, tImpacto);
                    rot = Mathf.Lerp(35f, -110f, tImpacto);
                }
            }

            float escTool = 0.88f + Mathf.Sin(prog * Mathf.PI) * 0.08f;
            Dibujar(spTool, AMundo(j.x + ex + dx, j.y + ey + dy), ord, oeste, 1, new Vector2(escTool, escTool), null, rot);
        }
    }
}
