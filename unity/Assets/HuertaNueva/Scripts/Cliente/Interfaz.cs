// Interfaz completa de Mosslantern (HUD, barra, ventanas RPG y Main Menu profesional estilo RPG Interface Kit).
// Usa las texturas pixel-art 9-slice de UIKit (tableros de madera tallada, cintas doradas, placas hundidas
// y botones 3D biselados) y muestra vista previa animada en vivo de ambos protagonistas en el Main Menu.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HuertaNueva
{
    public class Interfaz
    {
        class Panel { public Func<string> firma; public Action pintar; public bool cerrable = true; public int uid; public string ultimaFirma; }
        class Flotante { public float x, y, t; public Text texto; }
        class Aviso_ { public float t; public GameObject go; }

        readonly Def def; readonly Graficos G; readonly Func<SesionLocal> sesion; readonly Vista vista;
        Estado S => sesion().Estado;
        Jugador Yo => sesion().Yo;
        void Enviar(Accion a) => sesion().Despachar(a);

        readonly RectTransform raiz, capaHud, capaFlotantes, capaAvisos;
        Text tDia, tHora, tClima, tDinero, tNombreSel;
        RectTransform corazones; readonly List<Image> llenos = new List<Image>();
        Image barraEnergia, barraAgua, barraJefe, rojo;
        GameObject jefe; Text tJefe;
        readonly List<(Image fondo, Image icono, Text n)> barra = new List<(Image, Image, Text)>();
        readonly List<Flotante> flotantes = new List<Flotante>();
        readonly List<Aviso_> avisos = new List<Aviso_>();

        // paneles en partida
        GameObject velo; RectTransform cab, cuerpo; ScrollRect scroll; Text tTituloPanel;
        Panel actual; string pestanaMenu = "mochila"; int marcada = -1, charlaIdx = -1;
        string hudCache = "", barraCache = "";

        // Main Menu profesional (Portada RPG Kit)
        GameObject portada; RectTransform portadaCuerpo;
        string pestanaPortada = "jugar", cuerpoElegido, demoAccion = "caminar", demoDir = "south", demoAspecto = "base";
        float tPortada;
        readonly Dictionary<string, Image> retratosPortada = new Dictionary<string, Image>();
        Image retratoMochila;
        Estado guardadoPortada;
        public Action<string> AlNueva; public Action AlContinuar; public Action AlReiniciar; public Action AlSalirMenu;

        // táctil
        public Mantener tArriba, tAbajo, tIzq, tDer;
        public Action AlUsar, AlComer;

        public bool Abierto => velo.activeSelf;
        public bool Cerrable => actual == null || actual.cerrable;
        public bool EnPortada => portada.activeSelf;

        static string FmtHora(float m) => $"{Mathf.FloorToInt(m / 60) % 24}:{Mathf.FloorToInt(m % 60 / 10) * 10:00}";
        string Nombre(string item) => def.ITEMS.TryGetValue(item ?? "", out var it) ? it.nombre : "—";

        public Interfaz(Def def, Graficos G, Func<SesionLocal> sesion, Vista vista, string cuerpoInicial)
        {
            this.def = def; this.G = G; this.sesion = sesion; this.vista = vista; cuerpoElegido = cuerpoInicial;
            UIKit.Fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            UIKit.Inicializar();

            var lienzo = new GameObject("Interfaz", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = lienzo.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 10;
            var esc = lienzo.GetComponent<CanvasScaler>();
            esc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; esc.referenceResolution = new Vector2(960, 540); esc.matchWidthOrHeight = 0.5f;
            raiz = (RectTransform)lienzo.transform;

            rojo = UIKit.Caja(raiz, "herido", new Color(0.78f, 0.12f, 0.12f, 0)); rojo.rectTransform.Llenar();
            capaFlotantes = UIKit.Nodo("flotantes", raiz).Llenar();
            capaHud = UIKit.Nodo("hud", raiz).Llenar();
            CrearHud();
            CrearTactil();
            capaAvisos = UIKit.Columna(raiz, 6, "avisos").GetComponent<RectTransform>();
            capaAvisos.Anclar(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(480, 0));
            capaAvisos.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            capaAvisos.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            CrearPanel();
            CrearPortada();
        }

        // ---------------------------------------------------------------- HUD
        void CrearHud()
        {
            var caja = UIKit.Tablero(capaHud, "estado", false);
            caja.rectTransform.Anclar(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(265, 118));
            var col = UIKit.Columna(caja.transform, 2); col.padding = new RectOffset(14, 14, 10, 10); col.GetComponent<RectTransform>().Llenar();
            tDia = UIKit.Texto(col.transform, "", 15, UIKit.Crema, TextAnchor.MiddleLeft, true);
            var f = UIKit.Fila(col.transform, 10);
            tHora = UIKit.Texto(f.transform, "", 16, UIKit.Oro, TextAnchor.MiddleLeft, true);
            tClima = UIKit.Texto(f.transform, "", 13, UIKit.Azul, TextAnchor.MiddleLeft, true);
            corazones = UIKit.Fila(col.transform, 3).GetComponent<RectTransform>();
            UIKit.Tam(corazones.gameObject, 0, 18);
            var fd = UIKit.Fila(col.transform, 6);
            UIKit.Caja(fd.transform, "moneda", UIKit.Oro).gameObject.AddComponent<LayoutElement>().preferredWidth = 10;
            tDinero = UIKit.Texto(fd.transform, "", 15, UIKit.Oro, TextAnchor.MiddleLeft, true);

            barraEnergia = UIKit.Barra(capaHud, "energia", UIKit.Verde, true);
            ((RectTransform)barraEnergia.transform.parent).Anclar(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 90), new Vector2(20, 134));
            barraAgua = UIKit.Barra(capaHud, "agua", UIKit.Azul, true);
            ((RectTransform)barraAgua.transform.parent).Anclar(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-40, 90), new Vector2(14, 94));

            jefe = UIKit.Tablero(capaHud, "jefe", false).gameObject;
            ((RectTransform)jefe.transform).Anclar(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(380, 56));
            tJefe = UIKit.Texto(jefe.transform, "", 15, UIKit.Rojo, TextAnchor.UpperCenter, true);
            tJefe.rectTransform.Anclar(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -8), new Vector2(0, 20));
            barraJefe = UIKit.Barra(jefe.transform, "vida", UIKit.Rojo, false);
            ((RectTransform)barraJefe.transform.parent).Anclar(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(-28, 16));
            jefe.SetActive(false);

            // Marco de barra de herramientas
            var marcoBarra = UIKit.Tablero(capaHud, "marcoBarra", false);
            marcoBarra.rectTransform.Anclar(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(12 * 48 + 20, 62));
            var bar = UIKit.Fila(marcoBarra.transform, 4, TextAnchor.MiddleCenter, "barra");
            ((RectTransform)bar.transform).Llenar(6);
            bar.childControlWidth = false; bar.childControlHeight = false;
            for (int i = 0; i < 12; i++)
            {
                int k = i;
                var c = Casilla(bar.transform, null, () => Enviar(new Accion { tipo = "seleccionar", i = k }), i == 9 ? "0" : i == 10 ? "-" : i == 11 ? "=" : (i + 1).ToString());
                barra.Add(c);
            }
            tNombreSel = UIKit.Texto(capaHud, "", 14, UIKit.Oro, TextAnchor.MiddleCenter, true);
            tNombreSel.rectTransform.Anclar(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 74), new Vector2(500, 22));

            UIKit.BotonEstilo(capaHud, "Menu", () => { if (Abierto) { if (Cerrable) Cerrar(); } else Menu(null); }, "oro", true, 92, 40).GetComponent<RectTransform>()
                .Anclar(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -10), new Vector2(92, 40));
        }

        (Image fondo, Image icono, Text n) Casilla(Transform padre, Slot s, Action alPulsar, string tecla = null)
        {
            var fondo = UIKit.Caja9(padre, "casilla", UIKit.SpCasilla, true);
            ((RectTransform)fondo.transform).sizeDelta = new Vector2(44, 44);
            UIKit.Tam(fondo.gameObject, 44, 44);
            var b = fondo.gameObject.AddComponent<Button>(); b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() => alPulsar());
            var ic = UIKit.Caja(fondo.transform, "icono", Color.white); ic.preserveAspect = true; ic.rectTransform.Llenar(6);
            var n = UIKit.Texto(fondo.transform, "", 12, Color.white, TextAnchor.LowerRight, true); n.rectTransform.Llenar(4);
            if (tecla != null) { var t = UIKit.Texto(fondo.transform, tecla, 10, UIKit.Gris, TextAnchor.UpperLeft); t.rectTransform.Llenar(4); }
            PonerCasilla((fondo, ic, n), s);
            return (fondo, ic, n);
        }
        void PonerCasilla((Image fondo, Image icono, Text n) c, Slot s)
        {
            c.icono.enabled = s != null;
            if (s != null) c.icono.sprite = G.Icono(def, s.id);
            c.n.text = s != null && s.n > 1 ? s.n.ToString() : "";
        }
        static void Marcar(Image fondo, bool sel, bool marca = false) =>
            fondo.sprite = sel ? UIKit.SpCasillaSel : marca ? UIKit.SpCasillaMarca : UIKit.SpCasilla;

        void CrearTactil()
        {
            if (!Application.isMobilePlatform && Input_.HayTactil() == false) return;
            Mantener Flecha(string txt, Vector2 pos)
            {
                var b = UIKit.Caja9(capaHud, "flecha", UIKit.SpBtnMadera, true);
                b.rectTransform.Anclar(Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), pos, new Vector2(62, 62));
                var t = UIKit.Texto(b.transform, txt, 26, UIKit.Crema, TextAnchor.MiddleCenter, true); t.rectTransform.Llenar();
                return b.gameObject.AddComponent<Mantener>();
            }
            tArriba = Flecha("↑", new Vector2(110, 180)); tAbajo = Flecha("↓", new Vector2(110, 60));
            tIzq = Flecha("←", new Vector2(50, 120)); tDer = Flecha("→", new Vector2(170, 120));
            var usar = UIKit.Caja9(capaHud, "usar", UIKit.SpBtnVerde, true);
            usar.rectTransform.Anclar(new Vector2(1, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-120, 110), new Vector2(86, 86));
            UIKit.Texto(usar.transform, "Use", 18, UIKit.Crema, TextAnchor.MiddleCenter, true).rectTransform.Llenar();
            usar.gameObject.AddComponent<Mantener>().alApretar = () => AlUsar?.Invoke();
            var comer = UIKit.Caja9(capaHud, "comer", UIKit.SpBtnOro, true);
            comer.rectTransform.Anclar(new Vector2(1, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-200, 60), new Vector2(64, 44));
            UIKit.Texto(comer.transform, "Eat", 14, UIKit.Hex("#2a1604"), TextAnchor.MiddleCenter, true).rectTransform.Llenar();
            comer.gameObject.AddComponent<Mantener>().alApretar = () => AlComer?.Invoke();
        }

        public void Aviso(string txt, bool malo = false)
        {
            var im = UIKit.Caja9(capaAvisos, "aviso", malo ? UIKit.SpBtnRojo : UIKit.SpTablero);
            var h = im.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = new RectOffset(16, 16, 8, 8);
            h.childControlWidth = true; h.childControlHeight = true;
            var fit = im.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = UIKit.Texto(im.transform, txt, 15, UIKit.Crema, TextAnchor.MiddleCenter, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            avisos.Add(new Aviso_ { t = 2.6f, go = im.gameObject });
            while (avisos.Count > 4) { UnityEngine.Object.Destroy(avisos[0].go); avisos.RemoveAt(0); }
        }

        void Flotar(Evento e)
        {
            var t = UIKit.Texto(capaFlotantes, e.txt, 14, e.color != null ? UIKit.Hex(e.color) : UIKit.Crema, TextAnchor.MiddleCenter, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow; t.rectTransform.sizeDelta = new Vector2(200, 20);
            flotantes.Add(new Flotante { x = e.x, y = e.y, texto = t });
        }

        public void PintarHUD(float dt, bool jugando)
        {
            tPortada += dt;
            if (EnPortada) AnimarRetratosPortada();
            else if (Abierto && pestanaMenu == "mochila" && retratoMochila != null)
            {
                var pjM = G.AspectoDe(def, Yo);
                var clipM = pjM.idle.TryGetValue("south", out var lIdl) && lIdl.Count > 0 ? lIdl : pjM.caminar["south"];
                if (clipM != null && clipM.Count > 0)
                    retratoMochila.sprite = clipM[(int)(tPortada * 4.5f) % clipM.Count];
            }

            capaHud.gameObject.SetActive(jugando);
            rojo.color = new Color(0.78f, 0.12f, 0.12f, vista.Herido * 0.5f);
            for (int i = flotantes.Count - 1; i >= 0; i--)
            {
                var f = flotantes[i]; f.t += dt;
                if (f.t > 1.2f) { UnityEngine.Object.Destroy(f.texto.gameObject); flotantes.RemoveAt(i); continue; }
                f.texto.transform.position = vista.APantalla(f.x, f.y - f.t * 18);
                var c = f.texto.color; c.a = Mathf.Max(0, 1 - f.t / 1.2f); f.texto.color = c;
            }
            for (int i = avisos.Count - 1; i >= 0; i--) { avisos[i].t -= dt; if (avisos[i].t <= 0) { UnityEngine.Object.Destroy(avisos[i].go); avisos.RemoveAt(i); } }
            if (!jugando) return;

            var e = S; var j = Yo; var t = e.tiempo; bool enCueva = j.lugar == "mazmorra";
            def.MAZ.TryGetValue(e.mazmorra.actual ?? "", out var maz);
            string dia = enCueva && maz != null ? $"{maz.nombre} · floor {e.mazmorra.piso} of {maz.pisos}"
                : $"Spring · Day {(t.dia - 1) % 28 + 1}" + (t.dia > 28 ? $" · Year {(t.dia - 1) / 28 + 1}" : "");
            string clave = dia + FmtHora(t.min) + j.dinero + Mathf.RoundToInt(j.energia) + j.aguaRegadera + t.lluvia + j.vida + j.vidaMax;
            PintarJefe(e, j);
            if (clave != hudCache)
            {
                hudCache = clave;
                tDia.text = dia; tHora.text = FmtHora(t.min); tClima.text = !enCueva && t.lluvia ? "Rain: all watered" : "";
                tDinero.text = j.dinero.ToString("N0") + "g";
                int n = Mathf.CeilToInt(j.vidaMax / 2);
                if (llenos.Count != n)
                {
                    UIKit.Vaciar(corazones); llenos.Clear();
                    for (int k = 0; k < n; k++)
                    {
                        var vacio = UIKit.Caja(corazones, "corazon", new Color(0.2f, 0.1f, 0.1f, 0.9f)); vacio.sprite = G.corazon; UIKit.Tam(vacio.gameObject, 17, 15);
                        var lleno = UIKit.Caja(vacio.transform, "lleno", UIKit.Rojo); lleno.sprite = G.corazon; lleno.rectTransform.Llenar();
                        lleno.type = Image.Type.Filled; lleno.fillMethod = Image.FillMethod.Horizontal; lleno.fillOrigin = 0;
                        llenos.Add(lleno);
                    }
                }
                for (int k = 0; k < llenos.Count; k++) { float v = j.vida - k * 2; llenos[k].fillAmount = v >= 2 ? 1 : v >= 1 ? 0.5f : 0; }
                barraEnergia.fillAmount = j.energia / j.energiaMax;
                barraEnergia.color = j.energia / j.energiaMax < 0.2f ? UIKit.Rojo : UIKit.Verde;
                int cap = def.ITEMS.TryGetValue("regadera", out var reg) && reg.capacidad > 0 ? reg.capacidad : 20;
                barraAgua.fillAmount = j.aguaRegadera / (float)cap;
            }
            string cb = j.sel + "|" + j.aguaRegadera + "|" + string.Join(",", j.inv.Take(12).Select(s => s == null ? "" : s.id + s.n));
            if (cb != barraCache)
            {
                barraCache = cb;
                for (int k = 0; k < 12; k++) { PonerCasilla(barra[k], j.inv[k]); Marcar(barra[k].fondo, k == j.sel); }
                var s = j.inv[j.sel];
                tNombreSel.text = s != null ? Nombre(s.id) + (s.id == "regadera" ? $" · water {j.aguaRegadera}/{def.ITEMS["regadera"].capacidad}" : "") : "Empty hand";
            }
            if (actual?.firma != null) { var f = actual.firma(); if (f != actual.ultimaFirma) { actual.ultimaFirma = f; Repintar(); } }
        }

        void PintarJefe(Estado e, Jugador j)
        {
            var z = j.lugar == "mazmorra" && e.mazmorra.enCurso ? e.mazmorra.nivel : null;
            var m = z?.monstruos.Find(x => def.MON[x.id].jefe && Vector2.Distance(new Vector2(x.x, x.y), new Vector2(j.x, j.y)) < 11 * def.T);
            jefe.SetActive(m != null);
            if (m != null) { tJefe.text = def.MON[m.id].nombre; barraJefe.fillAmount = m.vida / m.vidaMax; }
        }

        // ---------------------------------------------------------------- paneles RPG en partida
        void CrearPanel()
        {
            var v = UIKit.Caja(raiz, "velo", new Color(0.03f, 0.05f, 0.04f, 0.68f), true);
            v.rectTransform.Llenar();
            var cerrarFuera = v.gameObject.AddComponent<Button>(); cerrarFuera.transition = Selectable.Transition.None;
            cerrarFuera.navigation = new Navigation { mode = Navigation.Mode.None };
            cerrarFuera.onClick.AddListener(() => { if (Cerrable) Cerrar(); });
            velo = v.gameObject;

            var caja = UIKit.Tablero(velo.transform, "panel", true);
            caja.rectTransform.Anclar(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(720, 470));
            var mudo = caja.gameObject.AddComponent<Button>(); mudo.transition = Selectable.Transition.None; mudo.navigation = new Navigation { mode = Navigation.Mode.None };

            // Cinta dorada superior sobresaliendo del tablero
            var cinta = UIKit.Caja9(caja.transform, "cintaCab", UIKit.SpCinta, false);
            cinta.rectTransform.Anclar(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(360, 48));
            tTituloPanel = UIKit.Texto(cinta.transform, "", 20, UIKit.Hex("#2b1604"), TextAnchor.MiddleCenter, true);
            tTituloPanel.rectTransform.Llenar(4);

            var c = UIKit.Fila(caja.transform, 8, TextAnchor.MiddleRight);
            cab = (RectTransform)c.transform; c.padding = new RectOffset(18, 16, 26, 6);
            cab.Anclar(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 68));

            var sr = UIKit.Nodo("desplazar", caja.transform);
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one; sr.offsetMin = new Vector2(16, 16); sr.offsetMax = new Vector2(-16, -70);
            scroll = sr.gameObject.AddComponent<ScrollRect>(); scroll.horizontal = false; scroll.movementType = ScrollRect.MovementType.Clamped; scroll.scrollSensitivity = 30;
            var vista_ = UIKit.Caja(sr, "vista", new Color(0, 0, 0, 0), true); vista_.rectTransform.Llenar();
            vista_.gameObject.AddComponent<RectMask2D>();
            cuerpo = UIKit.Nodo("cuerpo", vista_.transform);
            cuerpo.Anclar(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, Vector2.zero);
            var vl = cuerpo.gameObject.AddComponent<VerticalLayoutGroup>(); vl.spacing = 8; vl.padding = new RectOffset(6, 6, 4, 10);
            vl.childControlWidth = true; vl.childControlHeight = true; vl.childForceExpandWidth = true; vl.childForceExpandHeight = false;
            cuerpo.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = vista_.rectTransform; scroll.content = cuerpo;
            velo.SetActive(false);
        }

        void Abrir(Panel p) { actual = p; p.ultimaFirma = p.firma?.Invoke(); velo.SetActive(true); Repintar(); scroll.verticalNormalizedPosition = 1; }
        public void Cerrar() { velo.SetActive(false); actual = null; }
        void Repintar() { if (actual == null) return; UIKit.Vaciar(cab); UIKit.Vaciar(cuerpo); actual.pintar(); }

        void Cabecera(string titulo, Action extra = null)
        {
            tTituloPanel.text = titulo.ToUpperInvariant();
            var esp = UIKit.Nodo("esp", cab); esp.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            extra?.Invoke();
            if (actual == null || actual.cerrable) UIKit.BotonEstilo(cab, "X", () => Cerrar(), "rojo", true, 42, 34);
        }
        Text Ayuda(string txt, Transform padre = null) => UIKit.Texto(padre ?? cuerpo, txt, 14, UIKit.Gris);
        Text Sub(string txt) => UIKit.Texto(cuerpo, txt, 16, UIKit.Oro, TextAnchor.MiddleLeft, true);
        HorizontalLayoutGroup Acciones(Transform padre = null) => UIKit.Fila(padre ?? cuerpo, 8);

        Transform FilaLista(string item, string titulo, string detalle, out Transform derecha, Transform padre = null, bool apagada = false)
        {
            var placa = UIKit.Placa(padre ?? cuerpo, "filaPlaca", false);
            if (apagada) placa.color = new Color(1, 1, 1, 0.55f);
            var f = placa.gameObject.AddComponent<HorizontalLayoutGroup>();
            f.spacing = 10; f.childAlignment = TextAnchor.MiddleLeft;
            f.childControlWidth = true; f.childControlHeight = true; f.childForceExpandWidth = false; f.childForceExpandHeight = false;
            f.padding = new RectOffset(12, 12, 8, 8);
            if (item != null) UIKit.Icono(f.transform, G.Icono(def, item), 34);
            var col = UIKit.Columna(f.transform, 2);
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UIKit.Texto(col.transform, titulo, 15, apagada ? UIKit.Gris : UIKit.Oro, TextAnchor.MiddleLeft, true);
            if (!string.IsNullOrEmpty(detalle)) UIKit.Texto(col.transform, detalle, 13, UIKit.Crema);
            derecha = UIKit.Fila(f.transform, 6, TextAnchor.MiddleRight).transform;
            return col.transform;
        }

        GridLayoutGroup Grilla()
        {
            var r = UIKit.Nodo("grilla", cuerpo);
            var g = r.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(44, 44); g.spacing = new Vector2(5, 5);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 12;
            return g;
        }

        string FirmaInv() => string.Join(",", Yo.inv.Select(s => s == null ? "" : s.id + s.n)) + Yo.dinero + Yo.ropa + Yo.ropaBatalla + string.Join(",", S.hitos ?? new List<string>());

        string SaludoMercader()
        {
            var charlas = def.historia?.charlas;
            if (charlaIdx >= 0 && charlas != null && charlas.Count > 0) return charlas[charlaIdx % charlas.Count];
            var sal = def.historia?.saludos;
            if (sal == null || sal.Count == 0) return "";
            if (S.tiempo.lluvia && sal.TryGetValue("lluvia", out var sl)) return sl;
            var hitos = S.hitos ?? new List<string>();
            foreach (var k in new[] { "hito_final", "hito_4", "hito_linterna", "hito_3", "hito_2", "hito_conserva", "hito_1" })
                if (hitos.Contains(k) && sal.TryGetValue(k, out var sh)) return sh;
            return sal.TryGetValue("inicio", out var si) ? si : "";
        }

        public void Menu(string pestana)
        {
            if (pestana != null) pestanaMenu = pestana;
            marcada = -1;
            Abrir(new Panel { firma = () => FirmaInv() + "|" + string.Join(",", S.residentes.Select(r => r.trabajo + r.casa + r.siesta)), pintar = PintarMenu });
        }

        void PintarMenu()
        {
            var j = Yo;
            Cabecera("Adventurer's Pack", () =>
            {
                foreach (var (t, n) in new[] { ("mochila", "Backpack"), ("crafteo", "Crafting"), ("aldea", "Village"), ("diario", "Journal"), ("ayuda", "How to Play") })
                    UIKit.BotonEstilo(cab, n, () => { pestanaMenu = t; marcada = -1; Repintar(); }, pestanaMenu == t ? "oro" : "madera", true, 100, 34, 14);
            });
            if (pestanaMenu == "mochila")
            {
                var placaPj = UIKit.Placa(cuerpo, "fichaProtagonista", false);
                var fp = placaPj.gameObject.AddComponent<HorizontalLayoutGroup>();
                fp.spacing = 12; fp.padding = new RectOffset(12, 12, 8, 8); fp.childAlignment = TextAnchor.MiddleLeft;
                fp.childControlWidth = true; fp.childControlHeight = true; fp.childForceExpandWidth = false; fp.childForceExpandHeight = false;
                var pjDef = def.personajes.Find(p => p.id == j.cuerpo) ?? (def.personajes.Count > 0 ? def.personajes[0] : null);
                var spRet = G.AspectoDe(def, j).quieto["south"];
                retratoMochila = UIKit.Icono(fp.transform, spRet, 56);
                var colPj = UIKit.Columna(fp.transform, 2);
                colPj.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                string titPj = pjDef != null && !string.IsNullOrEmpty(pjDef.titulo) ? $"{pjDef.nombre} · {pjDef.titulo}" : (pjDef?.nombre ?? "Farmer");
                UIKit.Texto(colPj.transform, titPj, 15, UIKit.Oro, TextAnchor.MiddleLeft, true);
                UIKit.Texto(colPj.transform, $"Farm outfit: <b>{Nombre(j.ropa)}</b>   ·   Battle outfit: <b>{Nombre(j.ropaBatalla)}</b> (auto-equipped in dungeons)", 13, UIKit.Crema);
                Ayuda("Click a slot and then another to swap items. The top 12 slots are your active hotbar.");
                var g = Grilla();
                for (int i = 0; i < j.inv.Count; i++)
                {
                    int k = i;
                    var c = Casilla(g.transform, j.inv[i], () =>
                    {
                        if (marcada < 0) { if (Yo.inv[k] != null) marcada = k; }
                        else { Enviar(new Accion { tipo = "moverInv", de = marcada, a = k }); marcada = -1; }
                        Repintar();
                    });
                    Marcar(c.fondo, i == j.sel, i == marcada);
                }
                int idxDet = marcada >= 0 ? marcada : j.sel;
                var sDet = idxDet >= 0 && idxDet < j.inv.Count ? j.inv[idxDet] : null;
                if (sDet != null && def.ITEMS.TryGetValue(sDet.id, out var itDet))
                {
                    string detTxt = itDet.descripcion + (itDet.venta > 0 ? $" · Sells for: {itDet.venta}g" : "");
                    FilaLista(itDet.id, itDet.nombre + (sDet.n > 1 ? $" ×{sDet.n}" : ""), detTxt, out var derDet);
                    if (itDet.tipo == "ropa")
                        UIKit.BotonEstilo(derDet, "Equip", () => { marcada = -1; Enviar(new Accion { tipo = "vestir", i = idxDet }); }, "verde");
                    UIKit.BotonEstilo(derDet, idxDet < 12 ? "To Backpack" : "To Hotbar", () =>
                    {
                        int ini = idxDet < 12 ? 12 : 0, fin = idxDet < 12 ? 24 : 12, dest = ini;
                        for (int m = ini; m < fin; m++) if (Yo.inv[m] == null) { dest = m; break; }
                        marcada = -1;
                        Enviar(new Accion { tipo = "moverInv", de = idxDet, a = dest });
                    }, "madera");
                }
            }
            else if (pestanaMenu == "crafteo")
            {
                Ayuda("Craft farm machines, weapons, outfits, and potions. Place crafted machines in the world with Use (Space).");
                for (int i = 0; i < def.recetas.Count; i++)
                {
                    int k = i; var r = def.recetas[i]; var it = def.ITEMS[r.resultado];
                    bool puede = r.ingredientes.All(c => Inventario.Cuenta(j, c.id) >= c.n);
                    var col = FilaLista(r.resultado, it.nombre + (r.cantidad > 1 ? " ×" + r.cantidad : ""), it.descripcion, out var der);
                    var ing = UIKit.Fila(col, 10);
                    foreach (var c in r.ingredientes)
                    {
                        var f = UIKit.Fila(ing.transform, 2);
                        UIKit.Icono(f.transform, G.Icono(def, c.id), 18);
                        int tiene = Inventario.Cuenta(j, c.id);
                        UIKit.Texto(f.transform, $"{tiene}/{c.n}", 13, tiene < c.n ? UIKit.Rojo : UIKit.Crema).horizontalOverflow = HorizontalWrapMode.Overflow;
                    }
                    UIKit.BotonEstilo(der, "Craft", () => Enviar(new Accion { tipo = "fabricar", receta = k }), "verde", puede, 84, 34);
                }
            }
            else if (pestanaMenu == "aldea") PintarAldea(j);
            else if (pestanaMenu == "diario")
            {
                var hitos = S.hitos ?? new List<string> { "intro" };
                var cartas = def.historia?.cartas ?? new List<CartaDef>();
                Ayuda($"Field notes left by <b>Grandpa Aurelius</b> across Mosslantern Glen ({hitos.Count} of {cartas.Count} discovered).");
                foreach (var c in cartas)
                {
                    bool abierta = hitos.Contains(c.id);
                    string tit = abierta ? $"📜 {c.titulo}" : "🔒 Undiscovered Note";
                    string det = abierta ? $"{c.resumen}\n\"{c.texto}\"" : c.resumen;
                    FilaLista(null, tit, det, out _, null, !abierta);
                }
                Sub("Mosslantern Glen Bestiary");
                foreach (var m in def.MON.Values)
                {
                    string tag = m.jefe ? "Boss" : (m.vuela ? "Flying" : "Denizen");
                    FilaLista(null, $"{m.nombre} · {tag}", $"HP {m.vida} · Damage {m.dano / 2f} ♥\n{m.descripcion}", out _);
                }
            }
            else
            {
                Ayuda("Each day you have limited energy and hours. At 2:00 AM you pass out and pay a small care fee.");
                foreach (var (t, d) in new[]
                {
                    ("Move", "WASD or arrow keys. On mobile, use the D-pad."),
                    ("Use & Actions", "Space, J, Enter, or click an adjacent tile to strike, chop, mine, till, water, or harvest."),
                    ("Hotbar", "1-9, 0, -, = select a slot; Q/E or scroll wheel cycle through. F eats a crop or drinks a potion."),
                    ("Farming", "Till soil with the Hoe, plant seeds, and water daily. Automate with Sprinklers & Lantern Sprinklers."),
                    ("Artisan & Forge", "Preserves Jars turn crops into high-value jam. Furnaces smelt 3 ores into bars."),
                    ("Endless Dungeons", "Enter the northern cliff cave to delve into the Copper Mine and Forgotten Crypt. Each full clear scales monster strength and bonus boss loot!"),
                })
                {
                    FilaLista(null, t, d, out _);
                }
                var acc = Acciones();
                UIKit.BotonEstilo(acc.transform, "Save & Return to Title", () => { Cerrar(); AlSalirMenu?.Invoke(); }, "oro", true, 210, 38);
                Button si = null;
                UIKit.BotonEstilo(acc.transform, "Delete Save & Reset", () => { if (si != null) si.gameObject.SetActive(true); }, "rojo", true, 180, 38);
                si = UIKit.BotonEstilo(acc.transform, "Confirm Delete Save", () => { Cerrar(); AlReiniciar?.Invoke(); }, "rojo", true, 180, 38);
                si.gameObject.SetActive(false);
            }
        }

        void Tienda()
        {
            charlaIdx = -1;
            Abrir(new Panel
            {
                firma = FirmaInv,
                pintar = () =>
                {
                    var j = Yo;
                    Cabecera(def.historia?.tituloPuesto ?? "Aunt Brisa's Stall", () =>
                    {
                        UIKit.BotonEstilo(cab, $"Talk to {def.historia?.nombreMercader ?? "Brisa"}", () => { charlaIdx++; Repintar(); }, "oro", true, 165, 34);
                    });
                    string sal = SaludoMercader();
                    if (!string.IsNullOrEmpty(sal)) FilaLista(null, def.historia?.nombreMercader ?? "Aunt Brisa", sal, out _);
                    Ayuda($"You have <b>{j.dinero}g</b>. To sell crops, preserves, or gems, place them in the shipping bin.");
                    foreach (var id in def.tienda)
                    {
                        var it = def.ITEMS[id]; def.CULT.TryGetValue(it.cultivo ?? "", out var c);
                        FilaLista(id, it.nombre, c != null ? $"Grows in {c.dias} watered days · sells for {def.ITEMS[c.producto].venta}g" : it.descripcion, out var der);
                        UIKit.Texto(der, it.compra + "g", 15, UIKit.Oro, TextAnchor.MiddleRight, true).horizontalOverflow = HorizontalWrapMode.Overflow;
                        UIKit.BotonEstilo(der, "×1", () => Enviar(new Accion { tipo = "comprar", item = id, n = 1 }), "madera", j.dinero >= it.compra, 52, 34);
                        if (it.tipo == "semilla" || it.tipo == "pocion")
                            UIKit.BotonEstilo(der, "×5", () => Enviar(new Accion { tipo = "comprar", item = id, n = 5 }), "verde", j.dinero >= it.compra * 5, 56, 34);
                    }
                },
            });
        }

        // ---------------------------------------------------------------- aldea: construir y Mosslings
        /// <summary>Edificio elegido en la pestaña Village; se coloca delante del jugador al pulsar Usar.</summary>
        public string Construyendo { get; private set; }
        public void CancelarConstruccion() { if (Construyendo == null) return; Construyendo = null; Aviso("Building cancelled"); }

        void PintarAldea(Jugador j)
        {
            var e = S;
            Ayuda("Pick a building, then face an empty spot on the farm and press <b>Use</b> (Space) to place it. <b>Esc</b> cancels. Take a building down with the axe or pickaxe to get its materials back.");
            Sub("Buildings");
            foreach (var ed in def.edificios)
            {
                bool abierto = Reglas.EdificioDisponible(e, def, ed);
                bool puede = abierto && ed.coste.All(c => Inventario.Cuenta(j, c.id) >= c.n) && j.lugar == "granja";
                string req = abierto ? ed.descripcion
                    : ed.requiereEdificio != null && !e.granja.objetos.Any(o => o.id == ed.requiereEdificio)
                        ? $"Requires the {def.EDIF[ed.requiereEdificio].nombre}."
                        : $"Requires {ed.requiereResidentes} Mossling{(ed.requiereResidentes > 1 ? "s" : "")} living in the glen.";
                int hechos = e.granja.objetos.Count(o => o.id == ed.id);
                var col = FilaLista(ed.id, ed.nombre + (hechos > 0 ? $"  ·  built: {hechos}" : ""), req, out var der, null, !abierto);
                var ing = UIKit.Fila(col, 10);
                foreach (var c in ed.coste)
                {
                    var f = UIKit.Fila(ing.transform, 2);
                    UIKit.Icono(f.transform, G.Icono(def, c.id), 18);
                    int tiene = Inventario.Cuenta(j, c.id);
                    UIKit.Texto(f.transform, $"{tiene}/{c.n}", 13, tiene < c.n ? UIKit.Rojo : UIKit.Crema).horizontalOverflow = HorizontalWrapMode.Overflow;
                }
                string id = ed.id;
                UIKit.BotonEstilo(der, "Build", () =>
                {
                    Construyendo = id; Cerrar();
                    Aviso($"Placing: {def.EDIF[id].nombre}. Face an empty spot and press Use. Esc cancels.");
                }, "verde", puede, 84, 34);
            }
            Sub("Mosslings");
            if (e.residentes.Count == 0)
            {
                Ayuda("No Mosslings yet. Something glows faintly in the Copper Mine… bring a torch.");
                return;
            }
            bool hayLenera = e.granja.objetos.Any(o => o.id == "lenera");
            for (int i = 0; i < e.residentes.Count; i++)
            {
                int k = i; var r = e.residentes[i]; var rd = def.RES[r.id];
                string estado = r.casa == 0 ? "Waiting for a Moss Hut"
                    : r.siesta ? "Napping: nothing to eat on the Shared Table"
                    : r.trabajo == "lena" ? "Stacks 6 wood in the Woodshed every morning"
                    : $"Waters {Reglas.RIEGO_MOSSLING} tilled tiles near its hut every morning";
                FilaLista(rd.sprite, rd.nombre, $"{rd.descripcion}\n<b>{estado}</b>", out var der);
                UIKit.BotonEstilo(der, "Water", () => Enviar(new Accion { tipo = "asignar", i = k, item = "regar" }), r.trabajo == "regar" ? "oro" : "madera", true, 76, 34);
                UIKit.BotonEstilo(der, "Wood", () => Enviar(new Accion { tipo = "asignar", i = k, item = "lena" }), r.trabajo == "lena" ? "oro" : "madera", hayLenera, 76, 34);
            }
        }

        void Cofre(int uid)
        {
            ObjMundo O() => HuertaNueva.Mundo.ObjetoPorUid(def, HuertaNueva.Mundo.ZonaDe(S, Yo), uid);
            Abrir(new Panel
            {
                uid = uid,
                firma = () => FirmaInv() + "|" + string.Join(",", (O()?.datos.items ?? new List<Slot>()).Select(s => s == null ? "" : s.id + s.n)),
                pintar = () =>
                {
                    var o = O();
                    if (o == null) { Cerrar(); return; }
                    Cabecera("Storage Chest");
                    Ayuda("Click an item to transfer it between the chest and your backpack.");
                    Sub("Chest Slots");
                    var g = Grilla();
                    for (int i = 0; i < 12; i++)
                    {
                        int k = i; var s = o.datos.items != null && i < o.datos.items.Count ? o.datos.items[i] : null;
                        Casilla(g.transform, s, () => Enviar(new Accion { tipo = "cofreSacar", uid = uid, i = k }));
                    }
                    Sub("Your Backpack");
                    var gm = Grilla();
                    for (int i = 0; i < Yo.inv.Count; i++) { int k = i; Casilla(gm.transform, Yo.inv[i], () => Enviar(new Accion { tipo = "cofreGuardar", uid = uid, i = k })); }
                },
            });
        }

        void Dormir()
        {
            Abrir(new Panel
            {
                pintar = () =>
                {
                    Cabecera("Mosslantern Farmhouse");
                    Ayuda($"It is {FmtHora(S.tiempo.min)}. Rest until morning? Your homestead saves automatically at dawn.");
                    var a = Acciones();
                    UIKit.BotonEstilo(a.transform, "Sleep Until Dawn", () => { Cerrar(); Enviar(new Accion { tipo = "dormir" }); }, "verde", true, 180, 40);
                    UIKit.BotonEstilo(a.transform, "Stay Awake", () => Cerrar(), "madera", true, 140, 40);
                },
            });
        }

        void PanelMazmorra(Evento e)
        {
            Abrir(new Panel
            {
                pintar = () =>
                {
                    var j = Yo; def.ITEMS.TryGetValue(j.ropaBatalla ?? "", out var batalla);
                    Cabecera("Dungeon Entrance");
                    Ayuda($"Entering automatically equips your battle outfit: <b>{batalla?.nombre}</b>. {batalla?.descripcion}");
                    if (e.enCursoId != null)
                    {
                        FilaLista(j.ropaBatalla, def.MAZ[e.enCursoId].nombre, $"Your party is on Floor {e.enCursoPiso}.", out var der);
                        UIKit.BotonEstilo(der, "Join Expedition", () => { Cerrar(); Enviar(new Accion { tipo = "entrarMazmorra", mazmorra = e.enCursoId, piso = e.enCursoPiso }); }, "verde", true, 150, 36);
                    }
                    else foreach (var m in e.mazmorras)
                    {
                        string det = m.abierta ? $"{m.descripcion}\n{m.pisos} floors · best: floor {m.record} · cleared {m.completada} {(m.completada == 1 ? "time" : "times")}" : $"{m.descripcion}\nLocked: {m.requiere}";
                        FilaLista(null, m.nombre, det, out var der, null, !m.abierta);
                        if (!m.abierta) continue;
                        for (int k = 0; k < m.disponibles.Count; k++)
                        {
                            int p = m.disponibles[k]; string id = m.id;
                            UIKit.BotonEstilo(der, "Floor " + p, () => { Cerrar(); Enviar(new Accion { tipo = "entrarMazmorra", mazmorra = id, piso = p }); }, k == m.disponibles.Count - 1 ? "verde" : "madera", true, 90, 36);
                        }
                    }
                    Ayuda("Bosses guard Floors 5 and 10; defeating Floor 5 unlocks Floor 6 as a permanent shortcut. Every full clear scales monster strength and adds extra boss treasure rolls!");
                },
            });
        }

        void PanelBotin(Evento b)
        {
            Abrir(new Panel
            {
                pintar = () =>
                {
                    Cabecera(b.titulo);
                    Ayuda("Your share of the dungeon spoils:");
                    foreach (var c in b.items) { FilaLista(c.id, Nombre(c.id), null, out var der); UIKit.Texto(der, "×" + c.n, 16, UIKit.Oro, TextAnchor.MiddleRight, true); }
                    UIKit.BotonEstilo(Acciones().transform, "Claim Spoils", () => Cerrar(), "verde", true, 160, 40);
                },
            });
        }

        void Derrota(Evento d)
        {
            Abrir(new Panel
            {
                pintar = () =>
                {
                    Cabecera("Knocked Out");
                    Ayuda($"Aunt Brisa pulled you from the tunnel entrance. You paid {d.multa}g for herbal remedies.");
                    Ayuda("Tip: forge a stronger blade, brew Health Potions, and equip Adventurer Armor.");
                    UIKit.BotonEstilo(Acciones().transform, "Back to the Farm", () => Cerrar(), "verde", true, 170, 40);
                },
            });
        }

        void Resumen(Evento r)
        {
            Abrir(new Panel
            {
                cerrable = false,
                pintar = () =>
                {
                    Cabecera($"Day {(r.dia - 1) % 28 + 1} of Spring");
                    if (r.ventas.Count == 0) Ayuda("The shipping bin slept empty last night.");
                    foreach (var c in r.ventas) { FilaLista(c.id, $"{Nombre(c.id)} ×{c.n}", null, out var der); UIKit.Texto(der, (def.ITEMS[c.id].venta * c.n) + "g", 15, UIKit.Oro, TextAnchor.MiddleRight, true); }
                    if (r.ventas.Count > 0) Sub($"Total Gold Earned: {r.total}g");
                    foreach (var n in r.notas) FilaLista(null, "Glen Chronicle", n, out _);
                    Ayuda("Homestead saved.");
                    UIKit.BotonEstilo(Acciones().transform, "Start the Day", () => Cerrar(), "verde", true, 170, 40);
                },
            });
        }

        public void Eventos(Evento e)
        {
            switch (e.tipo)
            {
                case "aviso": Aviso(e.txt, e.malo); break;
                case "flotar": Flotar(e); break;
                case "abrir":
                    if (e.panel == "tienda") Tienda();
                    else if (e.panel == "dormir") Dormir();
                    else if (e.panel == "cofre") Cofre(e.uid);
                    else if (e.panel == "mazmorra") PanelMazmorra(e);
                    break;
                case "resumen": Resumen(e); break;
                case "construido": if (e.item == Construyendo) Construyendo = null; break;
                case "botin": PanelBotin(e); break;
                case "derrota": Derrota(e); break;
                case "piso": if (e.bajada || e.jefe != null) Aviso(e.jefe != null ? $"Floor {e.piso}: {e.jefe} awaits!" : $"Floor {e.piso}", e.jefe != null); break;
            }
        }

        // ---------------------------------------------------------------- MAIN MENU SIMPLE Y BONITO (Pixel-Art Home Screen)
        void CrearPortada()
        {
            Sprite spFondo = G.SPR.TryGetValue("portada_fondo", out var sf) ? sf : null;
            var p = UIKit.Caja(raiz, "portada", spFondo != null ? Color.white : new Color(0.08f, 0.06f, 0.12f, 1f), true);
            if (spFondo != null) p.sprite = spFondo;
            p.rectTransform.Llenar();
            portada = p.gameObject;

            // Placa de título PixelLab (portada_logo.png) arriba al centro sobre el cielo
            Sprite spLogo = G.SPR.TryGetValue("portada_logo", out var sl) ? sl : UIKit.SpCinta;
            var logo = UIKit.Caja(p.transform, "logoPortada", Color.white, false);
            logo.sprite = spLogo;
            logo.preserveAspect = true;
            logo.rectTransform.Anclar(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(440, 165));

            var tTit = UIKit.Texto(logo.transform, "MOSSLANTERN", 30, UIKit.Hex("#2a1606"), TextAnchor.MiddleCenter, true);
            tTit.rectTransform.Anclar(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -14), new Vector2(310, 48));
            tTit.GetComponent<Shadow>().effectColor = new Color(1f, 0.94f, 0.76f, 0.85f);
            tTit.GetComponent<Shadow>().effectDistance = new Vector2(1, -1);

            portadaCuerpo = UIKit.Nodo("cuerpoPortada", p.transform);
            portadaCuerpo.Anclar(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, -56), new Vector2(540, 320));
            RepintarPortada();
        }

        Sprite CuadroDemo(string idCuerpo)
        {
            if (!G.PJS.TryGetValue(idCuerpo, out var juegos)) return G.blanco;
            var jp = juegos.TryGetValue(demoAspecto ?? "base", out var jAsp) ? jAsp : juegos["base"];
            string d = demoDir ?? "south";
            List<Sprite> clip = demoAccion == "caminar" ? (jp.caminar.TryGetValue(d, out var cw) ? cw : null)
                : demoAccion == "idle" ? (jp.idle.TryGetValue(d, out var ci) ? ci : null)
                : jp.ClipAccion(demoAccion, d);
            if (clip != null && clip.Count > 0)
            {
                float fps = demoAccion == "idle" ? 4.5f : 7.5f;
                return clip[(int)(tPortada * fps) % clip.Count];
            }
            return jp.quieto.TryGetValue(d, out var sq) ? sq : jp.quieto["south"];
        }

        void AnimarRetratosPortada()
        {
            foreach (var kv in retratosPortada)
                if (kv.Value != null) kv.Value.sprite = CuadroDemo(kv.Key);
        }

        Button BotonPortada(Transform padre, string texto, Action alPulsar, bool destacado = false)
        {
            Sprite spBtn = G.SPR.TryGetValue("portada_boton", out var sb) ? sb : UIKit.SpBtnMadera;
            var im = UIKit.Caja(padre, "btnPortada", Color.white, true);
            im.sprite = spBtn;
            UIKit.Tam(im.gameObject, 250, 62);
            var b = im.gameObject.AddComponent<Button>();
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var cb = b.colors;
            cb.normalColor = destacado ? new Color(1f, 1f, 0.94f, 1f) : new Color(0.95f, 0.92f, 0.88f, 1f);
            cb.highlightedColor = new Color(1.15f, 1.12f, 1.02f, 1f);
            cb.pressedColor = new Color(0.78f, 0.74f, 0.68f, 1f);
            b.colors = cb;
            b.onClick.AddListener(() => alPulsar?.Invoke());

            var t = UIKit.Texto(im.transform, texto, 18, destacado ? UIKit.Oro : UIKit.Crema, TextAnchor.MiddleCenter, true);
            t.rectTransform.Llenar(8);
            var ol = t.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0.08f, 0.04f, 0.02f, 0.95f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);
            return b;
        }

        void RepintarPortada()
        {
            if (portadaCuerpo == null) return;
            UIKit.Vaciar(portadaCuerpo);
            retratosPortada.Clear();

            bool haySave = guardadoPortada != null && guardadoPortada.jugadores != null && guardadoPortada.jugadores.Count > 0;

            if (pestanaPortada == "jugar")
            {
                // Pila vertical limpia y centrada de botones sobre el paisaje pixel-art (como game-home-screen-pixel-art)
                var col = UIKit.Columna(portadaCuerpo, 8);
                col.childAlignment = TextAnchor.MiddleCenter;
                col.childForceExpandWidth = false;
                col.GetComponent<RectTransform>().Llenar();

                if (haySave)
                {
                    BotonPortada(col.transform, "CONTINUE", () => AlContinuar?.Invoke(), true);
                }
                BotonPortada(col.transform, "NEW GAME", () => { pestanaPortada = "personaje"; RepintarPortada(); }, !haySave);
                BotonPortada(col.transform, "HOW TO PLAY", () => { pestanaPortada = "controles"; RepintarPortada(); });
                BotonPortada(col.transform, "EXIT", () => Application.Quit());
            }
            else if (pestanaPortada == "personaje")
            {
                // Selector de protagonista con vista previa animada en vivo, biografía de personalidad, trajes y poses
                var tarjeta = UIKit.Tablero(portadaCuerpo, "modalPersonaje", true);
                tarjeta.rectTransform.Anclar(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0, 12), new Vector2(620, 360));
                var col = UIKit.Columna(tarjeta.transform, 6);
                col.padding = new RectOffset(18, 18, 12, 12);
                col.childAlignment = TextAnchor.UpperCenter;
                col.GetComponent<RectTransform>().Llenar();

                UIKit.Texto(col.transform, "CHOOSE YOUR HERO OF MOSSLANTERN GLEN", 17, UIKit.Oro, TextAnchor.MiddleCenter, true);

                var fila = UIKit.Fila(col.transform, 14, TextAnchor.MiddleCenter);
                foreach (var c in def.personajes)
                {
                    string id = c.id; bool sel = id == cuerpoElegido;
                    var Marco = UIKit.Caja9(fila.transform, "pj_" + id, sel ? UIKit.SpTarjetaSel : UIKit.SpTarjeta, true);
                    UIKit.Tam(Marco.gameObject, 275, 162);
                    var bt = Marco.gameObject.AddComponent<Button>(); bt.navigation = new Navigation { mode = Navigation.Mode.None };
                    bt.onClick.AddListener(() => { cuerpoElegido = id; PlayerPrefs.SetString("huerta-nueva-cuerpo", id); RepintarPortada(); });

                    var h = UIKit.Fila(Marco.transform, 10, TextAnchor.MiddleLeft);
                    h.padding = new RectOffset(10, 10, 8, 8);
                    h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false;
                    h.GetComponent<RectTransform>().Llenar();

                    var ic = UIKit.Icono(h.transform, CuadroDemo(id), 86);
                    retratosPortada[id] = ic;

                    var infoCol = UIKit.Columna(h.transform, 3);
                    infoCol.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                    UIKit.Texto(infoCol.transform, c.nombre.ToUpperInvariant(), 17, sel ? UIKit.Oro : UIKit.Crema, TextAnchor.MiddleLeft, true);
                    if (!string.IsNullOrEmpty(c.titulo))
                        UIKit.Texto(infoCol.transform, c.titulo, 12, UIKit.Oro, TextAnchor.MiddleLeft, true);
                    if (!string.IsNullOrEmpty(c.personalidad))
                        UIKit.Texto(infoCol.transform, c.personalidad, 11, UIKit.Crema, TextAnchor.UpperLeft);
                }

                // Controles interactivos para probar trajes, poses y giro de 4 direcciones
                var barraDemo = UIKit.Fila(col.transform, 5, TextAnchor.MiddleCenter);
                foreach (var (asp, etq) in new[] { ("base", "Overalls"), ("minera", "Miner"), ("aventurera", "Adventurer") })
                {
                    string a = asp;
                    UIKit.BotonEstilo(barraDemo.transform, etq, () => { demoAspecto = a; RepintarPortada(); }, demoAspecto == a ? "oro" : "madera", true, 86, 28, 12);
                }
                foreach (var (accId, etq) in new[] { ("caminar", "Walk"), ("idle", "Idle"), ("talar", "Chop"), ("golpear", "Slash"), ("cosechar", "Harvest") })
                {
                    string ac = accId;
                    UIKit.BotonEstilo(barraDemo.transform, etq, () => { demoAccion = ac; RepintarPortada(); }, demoAccion == ac ? "verde" : "madera", true, 60, 28, 12);
                }
                UIKit.BotonEstilo(barraDemo.transform, "Turn ↻", () =>
                {
                    string[] ordenDirs = { "south", "east", "north", "west" };
                    int idx = (Array.IndexOf(ordenDirs, demoDir) + 1) % ordenDirs.Length;
                    demoDir = ordenDirs[idx];
                    RepintarPortada();
                }, "madera", true, 64, 28, 12);

                var acc = UIKit.Fila(col.transform, 14, TextAnchor.MiddleCenter);
                UIKit.BotonEstilo(acc.transform, "START ADVENTURE", () => AlNueva?.Invoke(cuerpoElegido), "verde", true, 175, 38, 15);
                UIKit.BotonEstilo(acc.transform, "BACK", () => { pestanaPortada = "jugar"; RepintarPortada(); }, "madera", true, 105, 38, 14);
            }
            else if (pestanaPortada == "controles")
            {
                var tarjeta = UIKit.Tablero(portadaCuerpo, "modalControles", true);
                tarjeta.rectTransform.Anclar(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 285));
                var col = UIKit.Columna(tarjeta.transform, 8);
                col.padding = new RectOffset(22, 22, 18, 18);
                col.childAlignment = TextAnchor.UpperCenter;
                col.GetComponent<RectTransform>().Llenar();

                UIKit.Texto(col.transform, "CONTROLS", 18, UIKit.Oro, TextAnchor.MiddleCenter, true);
                UIKit.Texto(col.transform, "<b>WASD / Arrows</b> : Move", 15, UIKit.Crema, TextAnchor.MiddleCenter);
                UIKit.Texto(col.transform, "<b>Space / Left-Click</b> : Use Tool, Attack, Harvest & Interact", 15, UIKit.Crema, TextAnchor.MiddleCenter);
                UIKit.Texto(col.transform, "<b>1–0 / Mouse Wheel</b> : Select Hotbar Slot", 15, UIKit.Crema, TextAnchor.MiddleCenter);
                UIKit.Texto(col.transform, "<b>I / Tab</b> : Backpack, Crafting & Journal   ·   <b>F</b> : Eat / Potion", 15, UIKit.Crema, TextAnchor.MiddleCenter);

                UIKit.BotonEstilo(col.transform, "BACK", () => { pestanaPortada = "jugar"; RepintarPortada(); }, "oro", true, 140, 38, 15);
            }
        }

        public void MostrarPortada(bool hayGuardado, Estado guardado = null) { guardadoPortada = hayGuardado ? guardado : null; pestanaPortada = "jugar"; portada.SetActive(true); RepintarPortada(); Cerrar(); }
        public void OcultarPortada() => portada.SetActive(false);
    }
}
