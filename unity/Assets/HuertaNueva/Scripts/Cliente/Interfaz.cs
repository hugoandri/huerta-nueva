// Interfaz (HUD, barra, paneles y portada) hecha con uGUI por código. Lee el estado y manda acciones
// a la sesión; nunca cambia el estado por su cuenta. Es la traducción de js/cliente/ui.js.
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

        // paneles
        GameObject velo; RectTransform cab, cuerpo; ScrollRect scroll;
        Panel actual; string pestanaMenu = "mochila"; int marcada = -1;
        string hudCache = "", barraCache = "";

        // portada
        GameObject portada; RectTransform eligeFila; Button bContinuar; string cuerpoElegido;
        public Action<string> AlNueva; public Action AlContinuar; public Action AlReiniciar;

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
            capaAvisos.Anclar(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(460, 0));
            capaAvisos.GetComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.UpperCenter;
            capaAvisos.GetComponent<VerticalLayoutGroup>().childForceExpandWidth = false;
            CrearPanel();
            CrearPortada();
        }

        // ---------------------------------------------------------------- HUD
        void CrearHud()
        {
            var caja = UIKit.Caja(capaHud, "estado", UIKit.Fondo);
            caja.rectTransform.Anclar(new Vector2(0, 1), new Vector2(0, 1), new Vector2(0, 1), new Vector2(10, -10), new Vector2(250, 108));
            var col = UIKit.Columna(caja.transform, 2); col.padding = new RectOffset(10, 10, 6, 6); col.GetComponent<RectTransform>().Llenar();
            tDia = UIKit.Texto(col.transform, "", 15, UIKit.Crema, TextAnchor.MiddleLeft, true);
            var f = UIKit.Fila(col.transform, 10);
            tHora = UIKit.Texto(f.transform, "", 15, UIKit.Oro, TextAnchor.MiddleLeft, true);
            tClima = UIKit.Texto(f.transform, "", 13, UIKit.Azul);
            corazones = UIKit.Fila(col.transform, 3).GetComponent<RectTransform>();
            UIKit.Tam(corazones.gameObject, 0, 18);
            var fd = UIKit.Fila(col.transform, 6);
            UIKit.Caja(fd.transform, "moneda", UIKit.Oro).gameObject.AddComponent<LayoutElement>().preferredWidth = 10;
            tDinero = UIKit.Texto(fd.transform, "", 15, UIKit.Oro, TextAnchor.MiddleLeft, true);

            // energía y agua: barras verticales a la derecha
            barraEnergia = UIKit.Barra(capaHud, "energia", UIKit.Verde, true);
            ((RectTransform)barraEnergia.transform.parent).Anclar(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-14, 90), new Vector2(18, 130));
            barraAgua = UIKit.Barra(capaHud, "agua", UIKit.Azul, true);
            ((RectTransform)barraAgua.transform.parent).Anclar(new Vector2(1, 0), new Vector2(1, 0), new Vector2(1, 0), new Vector2(-38, 90), new Vector2(12, 90));

            // barra del jefe
            jefe = UIKit.Caja(capaHud, "jefe", UIKit.Fondo).gameObject;
            ((RectTransform)jefe.transform).Anclar(new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -10), new Vector2(360, 50));
            tJefe = UIKit.Texto(jefe.transform, "", 14, UIKit.Rojo, TextAnchor.UpperCenter, true);
            tJefe.rectTransform.Anclar(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -4), new Vector2(0, 20));
            barraJefe = UIKit.Barra(jefe.transform, "vida", UIKit.Rojo, false);
            ((RectTransform)barraJefe.transform.parent).Anclar(new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0), new Vector2(0, 8), new Vector2(-20, 14));
            jefe.SetActive(false);

            // barra de herramientas
            var bar = UIKit.Fila(capaHud, 4, TextAnchor.MiddleCenter, "barra");
            ((RectTransform)bar.transform).Anclar(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 10), new Vector2(12 * 48, 48));
            bar.childControlWidth = false; bar.childControlHeight = false;
            for (int i = 0; i < 12; i++)
            {
                int k = i;
                var c = Casilla(bar.transform, null, () => Enviar(new Accion { tipo = "seleccionar", i = k }), i == 9 ? "0" : i == 10 ? "-" : i == 11 ? "=" : (i + 1).ToString());
                barra.Add(c);
            }
            tNombreSel = UIKit.Texto(capaHud, "", 14, UIKit.Crema, TextAnchor.MiddleCenter, true);
            tNombreSel.rectTransform.Anclar(new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 62), new Vector2(500, 22));

            UIKit.Boton(capaHud, "Menú", () => { if (Abierto) { if (Cerrable) Cerrar(); } else Menu(null); }).GetComponent<RectTransform>()
                .Anclar(new Vector2(1, 1), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -10), new Vector2(80, 36));
        }

        // Una casilla de inventario: fondo, icono, cantidad y tecla.
        (Image fondo, Image icono, Text n) Casilla(Transform padre, Slot s, Action alPulsar, string tecla = null)
        {
            var fondo = UIKit.Caja(padre, "casilla", new Color(0.23f, 0.17f, 0.12f, 0.92f), true);
            ((RectTransform)fondo.transform).sizeDelta = new Vector2(44, 44);
            UIKit.Tam(fondo.gameObject, 44, 44);
            var b = fondo.gameObject.AddComponent<Button>(); b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.onClick.AddListener(() => alPulsar());
            var ic = UIKit.Caja(fondo.transform, "icono", Color.white); ic.preserveAspect = true; ic.rectTransform.Llenar(5);
            var n = UIKit.Texto(fondo.transform, "", 12, Color.white, TextAnchor.LowerRight, true); n.rectTransform.Llenar(3);
            if (tecla != null) { var t = UIKit.Texto(fondo.transform, tecla, 10, UIKit.Gris, TextAnchor.UpperLeft); t.rectTransform.Llenar(3); }
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
            fondo.color = sel ? new Color(0.62f, 0.48f, 0.18f, 0.95f) : marca ? new Color(0.35f, 0.5f, 0.62f, 0.95f) : new Color(0.23f, 0.17f, 0.12f, 0.92f);

        void CrearTactil()
        {
            if (!Application.isMobilePlatform && Input_.HayTactil() == false) return;
            Mantener Flecha(string txt, Vector2 pos)
            {
                var b = UIKit.Caja(capaHud, "flecha", new Color(0, 0, 0, 0.35f), true);
                b.rectTransform.Anclar(Vector2.zero, Vector2.zero, new Vector2(0.5f, 0.5f), pos, new Vector2(62, 62));
                var t = UIKit.Texto(b.transform, txt, 26, UIKit.Crema, TextAnchor.MiddleCenter, true); t.rectTransform.Llenar();
                return b.gameObject.AddComponent<Mantener>();
            }
            tArriba = Flecha("↑", new Vector2(110, 180)); tAbajo = Flecha("↓", new Vector2(110, 60));
            tIzq = Flecha("←", new Vector2(50, 120)); tDer = Flecha("→", new Vector2(170, 120));
            var usar = UIKit.Caja(capaHud, "usar", new Color(0.25f, 0.49f, 0.29f, 0.8f), true);
            usar.rectTransform.Anclar(new Vector2(1, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-120, 110), new Vector2(86, 86));
            UIKit.Texto(usar.transform, "Usar", 18, UIKit.Crema, TextAnchor.MiddleCenter, true).rectTransform.Llenar();
            usar.gameObject.AddComponent<Mantener>().alApretar = () => AlUsar?.Invoke();
            var comer = UIKit.Caja(capaHud, "comer", new Color(0, 0, 0, 0.45f), true);
            comer.rectTransform.Anclar(new Vector2(1, 0), new Vector2(1, 0), new Vector2(0.5f, 0.5f), new Vector2(-200, 60), new Vector2(64, 44));
            UIKit.Texto(comer.transform, "Comer", 14, UIKit.Crema, TextAnchor.MiddleCenter, true).rectTransform.Llenar();
            comer.gameObject.AddComponent<Mantener>().alApretar = () => AlComer?.Invoke();
        }

        public void Aviso(string txt, bool malo = false)
        {
            var im = UIKit.Caja(capaAvisos, "aviso", malo ? new Color(0.45f, 0.14f, 0.1f, 0.92f) : UIKit.Fondo);
            var h = im.gameObject.AddComponent<HorizontalLayoutGroup>(); h.padding = new RectOffset(12, 12, 6, 6);
            h.childControlWidth = true; h.childControlHeight = true;
            var fit = im.gameObject.AddComponent<ContentSizeFitter>(); fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            var t = UIKit.Texto(im.transform, txt, 15, UIKit.Crema, TextAnchor.MiddleCenter);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            avisos.Add(new Aviso_ { t = 2.4f, go = im.gameObject });
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
            string dia = enCueva && maz != null ? $"{maz.nombre} · piso {e.mazmorra.piso} de {maz.pisos}"
                : $"Primavera · día {(t.dia - 1) % 28 + 1}" + (t.dia > 28 ? $" · año {(t.dia - 1) / 28 + 1}" : "");
            string clave = dia + FmtHora(t.min) + j.dinero + Mathf.RoundToInt(j.energia) + j.aguaRegadera + t.lluvia + j.vida + j.vidaMax;
            PintarJefe(e, j);
            if (clave != hudCache)
            {
                hudCache = clave;
                tDia.text = dia; tHora.text = FmtHora(t.min); tClima.text = !enCueva && t.lluvia ? "Lluvia: todo regado" : "";
                tDinero.text = j.dinero.ToString("N0");
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
                tNombreSel.text = s != null ? Nombre(s.id) + (s.id == "regadera" ? $" · agua {j.aguaRegadera}/{def.ITEMS["regadera"].capacidad}" : "") : "Mano vacía";
            }
            if (actual?.firma != null) { var f = actual.firma(); if (f != actual.ultimaFirma) { actual.ultimaFirma = f; Repintar(); } }
        }

        // Barra del jefe: aparece cuando el jefe del piso está cerca.
        void PintarJefe(Estado e, Jugador j)
        {
            var z = j.lugar == "mazmorra" && e.mazmorra.enCurso ? e.mazmorra.nivel : null;
            var m = z?.monstruos.Find(x => def.MON[x.id].jefe && Vector2.Distance(new Vector2(x.x, x.y), new Vector2(j.x, j.y)) < 11 * def.T);
            jefe.SetActive(m != null);
            if (m != null) { tJefe.text = def.MON[m.id].nombre; barraJefe.fillAmount = m.vida / m.vidaMax; }
        }

        // ---------------------------------------------------------------- paneles
        void CrearPanel()
        {
            var v = UIKit.Caja(raiz, "velo", new Color(0, 0, 0, 0.55f), true);
            v.rectTransform.Llenar();
            var cerrarFuera = v.gameObject.AddComponent<Button>(); cerrarFuera.transition = Selectable.Transition.None;
            cerrarFuera.navigation = new Navigation { mode = Navigation.Mode.None };
            cerrarFuera.onClick.AddListener(() => { if (Cerrable) Cerrar(); });
            velo = v.gameObject;
            var caja = UIKit.Caja(velo.transform, "panel", UIKit.Fondo, true);
            caja.rectTransform.Anclar(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(700, 470));
            // un botón mudo en el panel: así un clic dentro no llega al velo (que cierra al tocar afuera)
            var mudo = caja.gameObject.AddComponent<Button>(); mudo.transition = Selectable.Transition.None; mudo.navigation = new Navigation { mode = Navigation.Mode.None };
            var borde = caja.gameObject.AddComponent<Outline>(); borde.effectColor = UIKit.Borde; borde.effectDistance = new Vector2(3, -3);

            var c = UIKit.Fila(caja.transform, 8);
            cab = (RectTransform)c.transform; c.padding = new RectOffset(14, 10, 8, 8);
            cab.Anclar(new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), Vector2.zero, new Vector2(0, 52));

            var sr = UIKit.Nodo("desplazar", caja.transform);
            sr.anchorMin = Vector2.zero; sr.anchorMax = Vector2.one; sr.offsetMin = new Vector2(10, 10); sr.offsetMax = new Vector2(-10, -56);
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

        void Abrir(Panel p) { actual = p; p.ultimaFirma = p.firma?.(); velo.SetActive(true); Repintar(); scroll.verticalNormalizedPosition = 1; }
        public void Cerrar() { velo.SetActive(false); actual = null; }
        void Repintar() { if (actual == null) return; UIKit.Vaciar(cab); UIKit.Vaciar(cuerpo); actual.pintar(); }

        void Cabecera(string titulo, Action extra = null)
        {
            var t = UIKit.Texto(cab, titulo, 20, UIKit.Oro, TextAnchor.MiddleLeft, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            extra?.Invoke();
            if (actual == null || actual.cerrable) UIKit.Boton(cab, "X", () => Cerrar(), false, true, 40);
        }
        Text Ayuda(string txt, Transform padre = null) => UIKit.Texto(padre ?? cuerpo, txt, 14, UIKit.Gris);
        Text Sub(string txt) => UIKit.Texto(cuerpo, txt, 15, UIKit.Oro, TextAnchor.MiddleLeft, true);
        HorizontalLayoutGroup Acciones(Transform padre = null) => UIKit.Fila(padre ?? cuerpo, 8);

        // Fila de lista: icono, título, detalles y botones a la derecha.
        Transform FilaLista(string item, string titulo, string detalle, out Transform derecha, Transform padre = null, bool apagada = false)
        {
            var f = UIKit.Fila(padre ?? cuerpo, 10);
            var bg = f.gameObject.AddComponent<Image>(); bg.color = new Color(1, 1, 1, apagada ? 0.02f : 0.05f); bg.raycastTarget = false;
            f.padding = new RectOffset(8, 8, 6, 6);
            if (item != null) UIKit.Icono(f.transform, G.Icono(def, item), 32);
            var col = UIKit.Columna(f.transform, 2);
            col.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            UIKit.Texto(col.transform, titulo, 15, apagada ? UIKit.Gris : UIKit.Crema, TextAnchor.MiddleLeft, true);
            if (!string.IsNullOrEmpty(detalle)) UIKit.Texto(col.transform, detalle, 13, UIKit.Gris);
            derecha = UIKit.Fila(f.transform, 6, TextAnchor.MiddleRight).transform;
            return col.transform;
        }

        GridLayoutGroup Grilla()
        {
            var r = UIKit.Nodo("grilla", cuerpo);
            var g = r.gameObject.AddComponent<GridLayoutGroup>();
            g.cellSize = new Vector2(44, 44); g.spacing = new Vector2(4, 4);
            g.constraint = GridLayoutGroup.Constraint.FixedColumnCount; g.constraintCount = 12;
            return g;
        }

        string FirmaInv() => string.Join(",", Yo.inv.Select(s => s == null ? "" : s.id + s.n)) + Yo.dinero + Yo.ropa + Yo.ropaBatalla;

        public void Menu(string pestana)
        {
            if (pestana != null) pestanaMenu = pestana;
            marcada = -1;
            Abrir(new Panel { firma = FirmaInv, pintar = PintarMenu });
        }

        void PintarMenu()
        {
            var j = Yo;
            Cabecera("Menú", () =>
            {
                foreach (var (t, n) in new[] { ("mochila", "Mochila"), ("crafteo", "Crafteo"), ("ayuda", "Cómo jugar") })
                    UIKit.Boton(cab, n, () => { pestanaMenu = t; marcada = -1; Repintar(); }, pestanaMenu == t);
            });
            if (pestanaMenu == "mochila")
            {
                Ayuda($"Ropa de granja: <b>{Nombre(j.ropa)}</b> · Ropa de batalla: <b>{Nombre(j.ropaBatalla)}</b> (se pone sola en la mazmorra).");
                Ayuda("Toca una casilla y luego otra para moverla. Las 12 primeras son tu barra.");
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
            }
            else if (pestanaMenu == "crafteo")
            {
                Ayuda("Las recetas salen de data/contenido.json. Lo fabricado se coloca en el mundo con Usar (Espacio).");
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
                    UIKit.Boton(der, "Fabricar", () => Enviar(new Accion { tipo = "fabricar", receta = k }), true, puede);
                }
            }
            else
            {
                Ayuda("Cada día tienes energía y horas limitadas. A las 2:00 te desmayas y pierdes algo de dinero.");
                foreach (var (t, d) in new[]
                {
                    ("Moverse", "WASD o flechas. En el celular, la cruceta."),
                    ("Usar", "Espacio, J o Enter usa lo que tienes en la mano en la casilla de enfrente. Con el ratón, haz clic en una casilla vecina."),
                    ("Barra", "1-9, 0, -, = eligen casilla; Q y E o la rueda la recorren. F come o toma una poción. I o Tab abre el menú, C el crafteo."),
                    ("Azada", "Ara el césped. No se puede arar junto al estanque."),
                    ("Semillas", "Siémbralas en tierra arada. Riega cada día para que crezcan."),
                    ("Regadera", "Riega la tierra. Rellénala usándola frente al agua."),
                    ("Hacha · pico", "Madera de los árboles, piedra de las rocas, fibra de la maleza. También recogen lo que colocaste."),
                    ("Caja de envíos", "Usa un objeto frente a ella para venderlo. Cobras al dormir."),
                    ("Tarro", "Mete un cultivo y al día siguiente tendrás una conserva que vale mucho más."),
                    ("Cuervos", "Con 8 o más cultivos sin espantapájaros cerca, alguno puede desaparecer de noche."),
                    ("Casa", "Úsala para dormir y pasar al día siguiente. La partida se guarda sola."),
                    ("Ropa", "Tienes ropa de granja y ropa de batalla. La de batalla se pone sola al entrar a una mazmorra. Para cambiar una, tenla en la mano y pulsa Usar."),
                    ("Mazmorras", "La cueva está arriba a la derecha de la granja. Elige la Mina de cobre o la Cripta olvidada (se abre al llegar al piso 5 de la mina)."),
                    ("Pelear", "Usa una espada frente a un monstruo. Las pociones (F) curan corazones."),
                    ("Horno", "Mete 3 minerales y en unas horas tendrás un lingote para armas y ropa."),
                })
                {
                    var f = UIKit.Fila(cuerpo, 10);
                    var a = UIKit.Texto(f.transform, t, 14, UIKit.Oro, TextAnchor.UpperLeft, true); UIKit.Tam(a.gameObject, 120, 0);
                    UIKit.Texto(f.transform, d, 14, UIKit.Crema, TextAnchor.UpperLeft).gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
                }
                var acc = Acciones();
                Button si = null;
                UIKit.Boton(acc.transform, "Borrar partida y empezar de nuevo", () => { if (si != null) si.gameObject.SetActive(true); });
                si = UIKit.Boton(acc.transform, "Sí, se pierde la partida", () => { Cerrar(); AlReiniciar?.Invoke(); }, true);
                si.gameObject.SetActive(false);
            }
        }

        void Tienda()
        {
            Abrir(new Panel
            {
                firma = FirmaInv,
                pintar = () =>
                {
                    var j = Yo;
                    Cabecera("Puesto de semillas");
                    Ayuda($"Tienes <b>{j.dinero}</b> monedas. Para vender, usa la caja de envíos.");
                    foreach (var id in def.tienda)
                    {
                        var it = def.ITEMS[id]; def.CULT.TryGetValue(it.cultivo ?? "", out var c);
                        FilaLista(id, it.nombre, c != null ? $"Crece en {c.dias} días regados · se vende a {def.ITEMS[c.producto].venta}" : it.descripcion, out var der);
                        UIKit.Texto(der, it.compra.ToString(), 15, UIKit.Oro, TextAnchor.MiddleRight, true).horizontalOverflow = HorizontalWrapMode.Overflow;
                        UIKit.Boton(der, "×1", () => Enviar(new Accion { tipo = "comprar", item = id, n = 1 }), false, j.dinero >= it.compra);
                        UIKit.Boton(der, "×5", () => Enviar(new Accion { tipo = "comprar", item = id, n = 5 }), true, j.dinero >= it.compra * 5);
                    }
                },
            });
        }

        void Cofre(int uid)
        {
            ObjMundo O() => HuertaNueva.Mundo.ObjetoPorUid(def, S.granja, uid);
            Abrir(new Panel
            {
                uid = uid,
                firma = () => FirmaInv() + "|" + string.Join(",", (O()?.datos.items ?? new List<Slot>()).Select(s => s == null ? "" : s.id + s.n)),
                pintar = () =>
                {
                    var o = O();
                    if (o == null) { Cerrar(); return; }
                    Cabecera("Cofre");
                    Ayuda("Toca un objeto para pasarlo entre el cofre y la mochila.");
                    Sub("Cofre");
                    var g = Grilla();
                    for (int i = 0; i < 12; i++)
                    {
                        int k = i; var s = o.datos.items != null && i < o.datos.items.Count ? o.datos.items[i] : null;
                        Casilla(g.transform, s, () => Enviar(new Accion { tipo = "cofreSacar", uid = uid, i = k }));
                    }
                    Sub("Mochila");
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
                    Cabecera("Casa");
                    Ayuda($"Son las {FmtHora(S.tiempo.min)}. ¿Dormir hasta mañana? La partida se guarda al despertar.");
                    var a = Acciones();
                    UIKit.Boton(a.transform, "Dormir", () => { Cerrar(); Enviar(new Accion { tipo = "dormir" }); }, true);
                    UIKit.Boton(a.transform, "Seguir despierta", () => Cerrar());
                },
            });
        }

        // Panel de la cueva: elegir a qué mazmorra ir y desde qué piso.
        void PanelMazmorra(Evento e)
        {
            Abrir(new Panel
            {
                pintar = () =>
                {
                    var j = Yo; def.ITEMS.TryGetValue(j.ropaBatalla ?? "", out var batalla);
                    Cabecera("¿A qué mazmorra vas?");
                    Ayuda($"Al entrar te pones la ropa de batalla: <b>{batalla?.nombre}</b>. {batalla?.descripcion} Para cambiarla, ten otra ropa de batalla en la mano y pulsa Usar.");
                    if (e.enCursoId != null)
                    {
                        FilaLista(j.ropaBatalla, def.MAZ[e.enCursoId].nombre, $"Tu grupo está en el piso {e.enCursoPiso}.", out var der);
                        UIKit.Boton(der, "Unirte", () => { Cerrar(); Enviar(new Accion { tipo = "entrarMazmorra", mazmorra = e.enCursoId, piso = e.enCursoPiso }); }, true);
                    }
                    else foreach (var m in e.mazmorras)
                    {
                        string det = m.abierta ? $"{m.descripcion}\n{m.pisos} pisos · récord: piso {m.record} · completada {m.completada} {(m.completada == 1 ? "vez" : "veces")}" : $"{m.descripcion}\nCerrada: {m.requiere}";
                        FilaLista(null, m.nombre, det, out var der, null, !m.abierta);
                        if (!m.abierta) continue;
                        for (int k = 0; k < m.disponibles.Count; k++)
                        {
                            int p = m.disponibles[k]; string id = m.id;
                            UIKit.Boton(der, "Piso " + p, () => { Cerrar(); Enviar(new Accion { tipo = "entrarMazmorra", mazmorra = id, piso = p }); }, k == m.disponibles.Count - 1);
                        }
                    }
                    Ayuda("Se avanza subiendo por la escalera de mano de cada piso. Solo se sale por el arco del piso 1 o por el que aparece al vencer al jefe del último piso. Jefes en los pisos 5 y 10; vencer al del 5 permite empezar en el 6. Si te quedas sin corazones vuelves a la granja.");
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
                    Ayuda("Tu parte del botín (cada jugador recibe la suya):");
                    foreach (var c in b.items) { FilaLista(c.id, Nombre(c.id), null, out var der); UIKit.Texto(der, "×" + c.n, 15, UIKit.Oro, TextAnchor.MiddleRight, true); }
                    UIKit.Boton(Acciones().transform, "Guardar en la mochila", () => Cerrar(), true);
                },
            });
        }

        void Derrota(Evento d)
        {
            Abrir(new Panel
            {
                pintar = () =>
                {
                    Cabecera("Te desmayaste");
                    Ayuda($"Te sacaron de la mazmorra y despertaste frente a la cueva. Pagaste {d.multa} monedas de atención.");
                    Ayuda("Consejo: fabrica una espada, pociones y la armadura de aventurera.");
                    UIKit.Boton(Acciones().transform, "Seguir", () => Cerrar(), true);
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
                    Cabecera($"Día {(r.dia - 1) % 28 + 1} de primavera");
                    if (r.ventas.Count == 0) Ayuda("Ayer no enviaste nada para vender.");
                    foreach (var c in r.ventas) { FilaLista(c.id, $"{Nombre(c.id)} ×{c.n}", null, out var der); UIKit.Texto(der, (def.ITEMS[c.id].venta * c.n).ToString(), 15, UIKit.Oro, TextAnchor.MiddleRight, true); }
                    if (r.ventas.Count > 0) Sub($"Total ganado: {r.total}");
                    foreach (var n in r.notas) Ayuda(n);
                    Ayuda("Partida guardada.");
                    UIKit.Boton(Acciones().transform, "Empezar el día", () => Cerrar(), true);
                },
            });
        }

        // Eventos del núcleo → interfaz (la sesión ya filtró los de otros jugadores).
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
                case "botin": PanelBotin(e); break;
                case "derrota": Derrota(e); break;
                case "piso": if (e.bajada || e.jefe != null) Aviso(e.jefe != null ? $"Piso {e.piso}: ¡{e.jefe} te espera!" : $"Piso {e.piso}", e.jefe != null); break;
            }
        }

        // ---------------------------------------------------------------- portada
        void CrearPortada()
        {
            var p = UIKit.Caja(raiz, "portada", new Color(0.05f, 0.07f, 0.05f, 0.55f), true);
            p.rectTransform.Llenar();
            portada = p.gameObject;
            var caja = UIKit.Caja(p.transform, "caja", UIKit.Fondo);
            caja.rectTransform.Anclar(new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 400));
            caja.gameObject.AddComponent<Outline>().effectColor = UIKit.Borde;
            var col = UIKit.Columna(caja.transform, 10); col.padding = new RectOffset(24, 24, 20, 20); col.childAlignment = TextAnchor.UpperCenter;
            col.GetComponent<RectTransform>().Llenar();
            UIKit.Texto(col.transform, "Huerta Nueva", 40, UIKit.Oro, TextAnchor.MiddleCenter, true);
            UIKit.Texto(col.transform, "Cultiva, fabrica y baja a las mazmorras.", 16, UIKit.Crema, TextAnchor.MiddleCenter);
            UIKit.Texto(col.transform, "Elige tu personaje", 14, UIKit.Gris, TextAnchor.MiddleCenter);
            eligeFila = (RectTransform)UIKit.Fila(col.transform, 16, TextAnchor.MiddleCenter).transform;
            PintarElige();
            var acc = UIKit.Fila(col.transform, 12, TextAnchor.MiddleCenter);
            UIKit.Boton(acc.transform, "Nueva partida", () => AlNueva?.Invoke(cuerpoElegido), true, true, 170, 42);
            bContinuar = UIKit.Boton(acc.transform, "Continuar", () => AlContinuar?.Invoke(), false, true, 150, 42);
            UIKit.Texto(col.transform, $"Gráficos: {G.nReales} de PixelLab" + (G.nProv > 0 ? $" · {G.nProv} provisionales" : ""), 12, UIKit.Gris, TextAnchor.MiddleCenter);
        }

        void PintarElige()
        {
            UIKit.Vaciar(eligeFila);
            foreach (var c in def.personajes)
            {
                string id = c.id; bool sel = id == cuerpoElegido;
                var b = UIKit.Caja(eligeFila, "elige", sel ? new Color(0.62f, 0.48f, 0.18f, 0.95f) : new Color(1, 1, 1, 0.06f), true);
                UIKit.Tam(b.gameObject, 120, 130);
                var bt = b.gameObject.AddComponent<Button>(); bt.navigation = new Navigation { mode = Navigation.Mode.None };
                bt.onClick.AddListener(() => { cuerpoElegido = id; PlayerPrefs.SetString("huerta-nueva-cuerpo", id); PintarElige(); });
                var v = UIKit.Columna(b.transform, 2); v.childAlignment = TextAnchor.MiddleCenter; v.padding = new RectOffset(6, 6, 6, 6);
                v.GetComponent<RectTransform>().Llenar();
                var retrato = G.PJS.TryGetValue(id, out var juegos) ? juegos["base"].quieto["south"] : G.blanco;
                UIKit.Icono(v.transform, retrato, 96);
                UIKit.Texto(v.transform, c.nombre, 15, UIKit.Crema, TextAnchor.MiddleCenter, true);
            }
        }

        public void MostrarPortada(bool hayGuardado) { portada.SetActive(true); bContinuar.gameObject.SetActive(hayGuardado); Cerrar(); }
        public void OcultarPortada() => portada.SetActive(false);
    }
}
