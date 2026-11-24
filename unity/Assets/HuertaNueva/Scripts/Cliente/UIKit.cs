// Kit de Interfaz RPG (uGUI) con texturas pixel-art 9-slice generadas por código:
// Tableros de madera tallada con herrajes de bronce, cintas doradas de título, placas hundidas
// y botones 3D biselados (inspirado en kits profesionales de interfaz RPG).
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HuertaNueva
{
    public static class UIKit
    {
        public static Font Fuente;
        public static readonly Color Crema = Hex("#f7f1de"), Oro = Hex("#f6c648"), Fondo = Hex("#241911f8"), Borde = Hex("#6e4d30"),
            ColorBoton = Hex("#5c3f28"), BotonPrimario = Hex("#3f8f38"), Gris = Hex("#c7b89b"), Rojo = Hex("#e25b45"), Azul = Hex("#5cc2f2"), Verde = Hex("#6fd158");

        // Sprites 9-slice del Game Interface Kit
        public static Sprite SpTablero, SpPlaca, SpCinta, SpTarjeta, SpTarjetaSel, SpCasilla, SpCasillaSel, SpCasillaMarca;
        public static Sprite SpBtnVerde, SpBtnVerdeHover, SpBtnVerdePress;
        public static Sprite SpBtnOro, SpBtnOroHover, SpBtnOroPress;
        public static Sprite SpBtnMadera, SpBtnMaderaHover, SpBtnMaderaPress;
        public static Sprite SpBtnRojo, SpBtnRojoHover, SpBtnRojoPress;
        public static Sprite SpEmblemaLinterna;
        static bool listo;

        public static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;

        static Texture2D Tex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            t.SetPixels32(new Color32[w * h]);
            return t;
        }
        static void Px(Texture2D t, int x, int y, Color c) { if (x >= 0 && y >= 0 && x < t.width && y < t.height) t.SetPixel(x, t.height - 1 - y, c); }
        static void Rect(Texture2D t, int x, int y, int w, int h, Color c)
        {
            for (int yy = y; yy < y + h; yy++) for (int xx = x; xx < x + w; xx++) Px(t, xx, yy, c);
        }
        static Sprite Crear9(Texture2D t, int l, int b, int r, int top)
        {
            t.Apply();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, t.width, t.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(l, b, r, top));
        }

        public static void Inicializar()
        {
            if (listo) return;
            listo = true;

            // 1. Tablero principal de madera tallada con esquineros de oro/bronce y remaches
            SpTablero = CrearTablero(64, 64);
            // 2. Placa interior hundida (pergamino oscuro / madera)
            SpPlaca = CrearPlaca(36, 36, Hex("#1a120c"), Hex("#0e0906"), Hex("#3e2b1d"), Hex("#6b4c32"));
            // 3. Tarjetas de selección de personaje (normal y seleccionada con brillo dorado y gemas azules)
            SpTarjeta = CrearTarjeta(40, 40, false);
            SpTarjetaSel = CrearTarjeta(40, 40, true);
            // 4. Casillas de inventario biseladas
            SpCasilla = CrearCasilla(24, 24, Hex("#291d14"), Hex("#4a3525"), Hex("#140d08"), false);
            SpCasillaSel = CrearCasilla(24, 24, Hex("#473419"), Hex("#f6c648"), Hex("#fff09c"), true);
            SpCasillaMarca = CrearCasilla(24, 24, Hex("#1c303d"), Hex("#5cc2f2"), Hex("#b3ecff"), true);
            // 5. Cinta dorada de título (RPG Ribbon Banner con colas recortadas)
            SpCinta = CrearCinta(128, 36);
            // 6. Botones 3D biselados (Esmeralda, Oro, Madera, Carmesí)
            (SpBtnVerde, SpBtnVerdeHover, SpBtnVerdePress) = CrearJuegoBoton(Hex("#49a838"), Hex("#358427"), Hex("#97ef78"), Hex("#1e5215"), Hex("#e8b84b"));
            (SpBtnOro, SpBtnOroHover, SpBtnOroPress) = CrearJuegoBoton(Hex("#e8aa25"), Hex("#c28312"), Hex("#fff199"), Hex("#6e4406"), Hex("#fff3b8"));
            (SpBtnMadera, SpBtnMaderaHover, SpBtnMaderaPress) = CrearJuegoBoton(Hex("#6b4930"), Hex("#4f3420"), Hex("#9e7350"), Hex("#29190e"), Hex("#cfa055"));
            (SpBtnRojo, SpBtnRojoHover, SpBtnRojoPress) = CrearJuegoBoton(Hex("#cc4433"), Hex("#9e2e20"), Hex("#ff9482"), Hex("#57150d"), Hex("#e8b84b"));
            // 7. Emblema de Linterna de Musgo (Mosslantern Crest)
            SpEmblemaLinterna = CrearEmblemaLinterna();
        }

        static Sprite CrearTablero(int w, int h)
        {
            var t = Tex(w, h);
            Color cBordeExt = Hex("#110a05"), cMaderaLuz = Hex("#7d5636"), cMadera = Hex("#5c3d24"), cMaderaSombra = Hex("#382313"),
                  cSurco = Hex("#1a1009"), cFondo = Hex("#261b12"), cFondoVeta = Hex("#22170f"),
                  cOro = Hex("#e0a938"), cOroLuz = Hex("##fff0a6".Replace("₠", "")), cOroSombra = Hex("#7d5611"), cGema = Hex("#5cc2f2");

            // Cuerpo exterior con esquinas redondeadas pixel-art
            Rect(t, 2, 0, w - 4, h, cBordeExt); Rect(t, 0, 2, w, h - 4, cBordeExt);
            Rect(t, 1, 1, w - 2, h - 2, cBordeExt);
            // Marco de madera grueso (6px)
            Rect(t, 2, 2, w - 4, h - 4, cMadera);
            Rect(t, 2, 2, w - 4, 2, cMaderaLuz);
            Rect(t, 2, 2, 2, h - 4, cMaderaLuz);
            Rect(t, 2, h - 4, w - 4, 2, cMaderaSombra);
            Rect(t, w - 4, 2, 2, h - 4, cMaderaSombra);
            // Surco interior y relleno de tablones
            Rect(t, 8, 8, w - 16, h - 16, cSurco);
            Rect(t, 10, 10, w - 20, h - 20, cFondo);
            for (int y = 14; y < h - 12; y += 6) Rect(t, 10, y, w - 20, 1, cFondoVeta);
            // Borde interior dorado fino
            Rect(t, 9, 9, w - 18, 1, cOroSombra);
            Rect(t, 9, h - 10, w - 18, 1, cOroSombra);
            Rect(t, 9, 9, 1, h - 18, cOroSombra);
            Rect(t, w - 10, 9, 1, h - 18, cOroSombra);

            // Esquineros metálicos de bronce/oro con gema azul de linterna
            void Esquinero(int ex, int ey, int dx, int dy)
            {
                for (int i = 0; i < 10; i++)
                {
                    Px(t, ex + dx * i, ey, cOro); Px(t, ex + dx * i, ey + dy, cOroLuz);
                    Px(t, ex, ey + dy * i, cOro); Px(t, ex + dx, ey + dy * i, cOroLuz);
                }
                Rect(t, Mathf.Min(ex, ex + dx * 4), Mathf.Min(ey, ey + dy * 4), 5, 5, cOroSombra);
                Rect(t, Mathf.Min(ex + dx, ex + dx * 3), Mathf.Min(ey + dy, ey + dy * 3), 3, 3, cOro);
                Px(t, ex + dx * 2, ey + dy * 2, cGema);
            }
            Esquinero(2, 2, 1, 1); Esquinero(w - 3, 2, -1, 1);
            Esquinero(2, h - 3, 1, -1); Esquinero(w - 3, h - 3, -1, -1);
            return Crear9(t, 14, 14, 14, 14);
        }

        static Sprite CrearPlaca(int w, int h, Color fondo, Color sombra, Color luz, Color remache)
        {
            var t = Tex(w, h);
            Rect(t, 1, 0, w - 2, h, sombra); Rect(t, 0, 1, w, h - 2, sombra);
            Rect(t, 1, 1, w - 2, h - 2, fondo);
            Rect(t, 1, 1, w - 2, 2, sombra); Rect(t, 1, 1, 2, h - 2, sombra);
            Rect(t, 1, h - 3, w - 2, 2, luz); Rect(t, w - 3, 1, 2, h - 2, luz);
            Px(t, 4, 4, remache); Px(t, w - 5, 4, remache);
            Px(t, 4, h - 5, remache); Px(t, w - 5, h - 5, remache);
            return Crear9(t, 8, 8, 8, 8);
        }

        static Sprite CrearTarjeta(int w, int h, bool sel)
        {
            var t = Tex(w, h);
            Color ext = Hex("#120b06"), marco = sel ? Hex("#f6c648") : Hex("#573c26"),
                  luz = sel ? Hex("#fff5b8") : Hex("#785437"), sombra = sel ? Hex("#9c6d12") : Hex("#332113"),
                  fondo = sel ? Hex("#2e2414") : Hex("#1c140e"), gema = sel ? Hex("#5cc2f2") : Hex("#8c6747");
            Rect(t, 1, 0, w - 2, h, ext); Rect(t, 0, 1, w, h - 2, ext);
            Rect(t, 1, 1, w - 2, h - 2, marco);
            Rect(t, 1, 1, w - 2, 2, luz); Rect(t, 1, h - 3, w - 2, 2, sombra);
            Rect(t, 4, 4, w - 8, h - 8, ext);
            Rect(t, 5, 5, w - 10, h - 10, fondo);
            Rect(t, 2, 2, 3, 3, gema); Rect(t, w - 5, 2, 3, 3, gema);
            Rect(t, 2, h - 5, 3, 3, gema); Rect(t, w - 5, h - 5, 3, 3, gema);
            return Crear9(t, 8, 8, 8, 8);
        }

        static Sprite CrearCasilla(int w, int h, Color fondo, Color borde, Color luz, bool brillante)
        {
            var t = Tex(w, h);
            Color ext = Hex("#100a06");
            Rect(t, 1, 0, w - 2, h, ext); Rect(t, 0, 1, w, h - 2, ext);
            Rect(t, 1, 1, w - 2, h - 2, borde);
            if (brillante) Rect(t, 1, 1, w - 2, 1, luz);
            Rect(t, 3, 3, w - 6, h - 6, fondo);
            Rect(t, 3, 3, w - 6, 1, new Color(0, 0, 0, 0.35f));
            return Crear9(t, 6, 6, 6, 6);
        }

        static Sprite CrearCinta(int w, int h)
        {
            var t = Tex(w, h);
            Color ext = Hex("#140b04"), ala = Hex("#8f5914"), alaSombra = Hex("#593508"),
                  oroAlto = Hex("#f2b632"), oroBajo = Hex("#c98818"), oroLuz = Hex("#fff09c"), oroBorde = Hex("#784b08"),
                  gema = Hex("#5cc2f2"), gemaLuz = Hex("#c4f3ff");

            // Alas laterales de la cinta con corte en V (swallowtail)
            for (int y = 6; y < h - 2; y++)
            {
                int muesca = Mathf.Abs(y - (6 + (h - 8) / 2));
                int xIzq = 1 + (8 - Mathf.Min(8, muesca));
                for (int x = xIzq; x < 22; x++) Px(t, x, y, y == 6 || y == h - 3 || x == xIzq ? ext : (y > h - 8 ? alaSombra : ala));
                int xDer = w - 2 - (8 - Mathf.Min(8, muesca));
                for (int x = w - 22; x <= xDer; x++) Px(t, x, y, y == 6 || y == h - 3 || x == xDer ? ext : (y > h - 8 ? alaSombra : ala));
            }
            // Pliegue oscuro detrás del cuerpo central
            Rect(t, 16, h - 6, 6, 5, ext); Rect(t, w - 22, h - 6, 6, 5, ext);

            // Cuerpo central elevado de la cinta
            int cx = 16, cw = w - 32, ch = h - 6;
            Rect(t, cx + 1, 0, cw - 2, ch, ext); Rect(t, cx, 1, cw, ch - 2, ext);
            Rect(t, cx + 2, 2, cw - 4, (ch - 4) / 2, oroAlto);
            Rect(t, cx + 2, 2 + (ch - 4) / 2, cw - 4, ch - 4 - (ch - 4) / 2, oroBajo);
            Rect(t, cx + 2, 2, cw - 4, 2, oroLuz);
            Rect(t, cx + 2, ch - 5, cw - 4, 3, oroBorde);
            // Gemas azules de linterna a ambos lados de la cinta
            Rect(t, cx + 6, ch / 2 - 2, 4, 4, gema); Px(t, cx + 7, ch / 2 - 1, gemaLuz);
            Rect(t, cx + cw - 10, ch / 2 - 2, 4, 4, gema); Px(t, cx + cw - 9, ch / 2 - 1, gemaLuz);

            return Crear9(t, 28, 8, 28, 6);
        }

        static (Sprite normal, Sprite hover, Sprite press) CrearJuegoBoton(Color alto, Color bajo, Color luz, Color labio, Color remache)
        {
            Sprite Hacer(Color cAlto, Color cBajo, Color cLuz, int hLabio, int offsetTop)
            {
                int w = 48, h = 26;
                var t = Tex(w, h);
                Color ext = Hex("#120b06");
                int y0 = offsetTop, altoUtil = h - y0;
                Rect(t, 2, y0, w - 4, altoUtil, ext);
                Rect(t, 0, y0 + 2, w, altoUtil - 4, ext);
                Rect(t, 1, y0 + 1, w - 2, altoUtil - 2, ext);

                int hCuerpo = altoUtil - 4 - hLabio;
                Rect(t, 2, y0 + 2, w - 4, hCuerpo / 2, cAlto);
                Rect(t, 2, y0 + 2 + hCuerpo / 2, w - 4, hCuerpo - hCuerpo / 2, cBajo);
                Rect(t, 3, y0 + 2, w - 6, 2, cLuz);
                if (hLabio > 0) Rect(t, 2, h - 2 - hLabio, w - 4, hLabio, labio);

                // Remaches laterales
                Rect(t, 5, y0 + altoUtil / 2 - 2, 2, 2, remache);
                Rect(t, w - 7, y0 + altoUtil / 2 - 2, 2, 2, remache);
                return Crear9(t, 10, 6, 10, 6);
            }
            return (
                Hacer(alto, bajo, luz, 4, 0),
                Hacer( Color.Lerp(alto, Color.white, 0.16f), Color.Lerp(bajo, Color.white, 0.12f), Color.white, 4, 0),
                Hacer(bajo, labio, alto, 1, 2)
            );
        }

        static Sprite CrearEmblemaLinterna()
        {
            int w = 48, h = 48;
            var t = Tex(w, h);
            Color ext = Hex("#120b06"), hierro = Hex("#3b4248"), hierroLuz = Hex("#68747d"),
                  oro = Hex("#f2c14e"), oroLuz = Hex("#fff3a8"), musgo = Hex("#4ea843"), musgoLuz = Hex("#87e06e"),
                  azul = Hex("#358fd4"), azulLuz = Hex("#6fd6ff"), nucleo = Hex("#e3fbff");

            // Halo suave exterior
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(24, 25));
                if (d < 22 && d > 11) Px(t, x, y, new Color(0.36f, 0.76f, 0.95f, Mathf.Clamp01((22 - d) / 14f) * 0.28f));
            }
            // Argolla y cadena superior
            Rect(t, 22, 2, 4, 4, oro); Rect(t, 23, 3, 2, 2, ext);
            Rect(t, 18, 6, 12, 3, hierro); Rect(t, 19, 6, 10, 1, hierroLuz);
            // Techo de la linterna con musgo
            Rect(t, 13, 9, 22, 3, ext); Rect(t, 14, 10, 20, 2, hierro);
            Rect(t, 11, 12, 26, 4, ext); Rect(t, 12, 13, 24, 2, oro); Rect(t, 13, 13, 22, 1, oroLuz);
            // Brotes de musgo sobre el techo
            Rect(t, 13, 11, 5, 2, musgo); Px(t, 14, 10, musgoLuz);
            Rect(t, 29, 11, 6, 2, musgo); Px(t, 31, 10, musgoLuz);
            // Cuerpo de cristal azul brillante
            Rect(t, 14, 16, 20, 20, ext);
            Rect(t, 16, 17, 16, 18, azul);
            Rect(t, 18, 19, 12, 14, azulLuz);
            Rect(t, 21, 21, 6, 10, nucleo);
            // Barrotes de hierro de la linterna
            Rect(t, 15, 16, 2, 20, hierro); Rect(t, 31, 16, 2, 20, hierro);
            Rect(t, 23, 16, 2, 20, hierro);
            // Base de la linterna
            Rect(t, 12, 36, 24, 4, ext); Rect(t, 13, 37, 22, 2, oro);
            Rect(t, 16, 40, 16, 3, hierro);
            // Esporas brillantes alrededor
            Px(t, 7, 18, azulLuz); Px(t, 8, 18, nucleo);
            Px(t, 40, 22, azulLuz); Px(t, 39, 22, nucleo);
            Px(t, 10, 32, musgoLuz); Px(t, 37, 13, nucleo);

            t.Apply();
            return Sprite.Create(t, new UnityEngine.Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f);
        }

        public static RectTransform Nodo(string nombre, Transform padre)
        {
            var go = new GameObject(nombre, typeof(RectTransform));
            go.transform.SetParent(padre, false);
            return (RectTransform)go.transform;
        }

        public static RectTransform Anclar(this RectTransform r, Vector2 min, Vector2 max, Vector2 pivote, Vector2 pos, Vector2 tam)
        {
            r.anchorMin = min; r.anchorMax = max; r.pivot = pivote; r.anchoredPosition = pos; r.sizeDelta = tam;
            return r;
        }
        public static RectTransform Llenar(this RectTransform r, float margen = 0)
        {
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.pivot = new Vector2(0.5f, 0.5f);
            r.offsetMin = new Vector2(margen, margen); r.offsetMax = new Vector2(-margen, -margen);
            return r;
        }

        public static Image Caja(Transform padre, string nombre, Color color, bool raycast = false)
        {
            var r = Nodo(nombre, padre);
            var im = r.gameObject.AddComponent<Image>(); im.color = color; im.raycastTarget = raycast;
            return im;
        }

        public static Image Caja9(Transform padre, string nombre, Sprite sprite9, bool raycast = false, Color? tinte = null)
        {
            Inicializar();
            var r = Nodo(nombre, padre);
            var im = r.gameObject.AddComponent<Image>();
            im.sprite = sprite9; im.type = Image.Type.Sliced; im.color = tinte ?? Color.white; im.raycastTarget = raycast;
            return im;
        }

        public static Image Tablero(Transform padre, string nombre = "tablero", bool raycast = true) => Caja9(padre, nombre, SpTablero, raycast);
        public static Image Placa(Transform padre, string nombre = "placa", bool raycast = false) => Caja9(padre, nombre, SpPlaca, raycast);

        public static Image Cinta(Transform padre, string titulo, int tamTexto = 24, float ancho = 420, float alto = 54)
        {
            Inicializar();
            var im = Caja9(padre, "cinta", SpCinta, false);
            Tam(im.gameObject, ancho, alto);
            var t = Texto(im.transform, titulo, tamTexto, Hex("#2a1604"), TextAnchor.MiddleCenter, true);
            t.rectTransform.Llenar(4);
            var ol = t.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(1f, 0.95f, 0.72f, 0.75f);
            ol.effectDistance = new Vector2(1, -1);
            return im;
        }

        public static Image Icono(Transform padre, Sprite s, float tam)
        {
            var im = Caja(padre, "icono", Color.white);
            im.sprite = s; im.preserveAspect = true;
            Tam(im.gameObject, tam, tam);
            return im;
        }

        public static Text Texto(Transform padre, string txt, int tam = 16, Color? color = null, TextAnchor alineado = TextAnchor.MiddleLeft, bool negrita = false)
        {
            var r = Nodo("texto", padre);
            var t = r.gameObject.AddComponent<Text>();
            t.font = Fuente; t.fontSize = tam; t.color = color ?? Crema; t.alignment = alineado; t.text = txt;
            t.supportRichText = true; t.raycastTarget = false;
            t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
            if (negrita) t.fontStyle = FontStyle.Bold;
            var s = r.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, 0.85f); s.effectDistance = new Vector2(1.5f, -1.5f);
            return t;
        }

        // Botón RPG 3D biselado: estilo "verde" (Play/Acción principal), "oro" (Destacado/Tab activa), "madera" (Secundario), "rojo" (Cerrar/Peligro)
        public static Button Boton(Transform padre, string txt, Action alPulsar, bool primario = false, bool activo = true, float ancho = 0, float alto = 38)
            => BotonEstilo(padre, txt, alPulsar, primario ? "verde" : "madera", activo, ancho, alto);

        public static Button BotonEstilo(Transform padre, string txt, Action alPulsar, string estilo, bool activo = true, float ancho = 0, float alto = 38, int tamFuente = 15)
        {
            Inicializar();
            var (spNorm, spHover, spPress) = estilo == "verde" ? (SpBtnVerde, SpBtnVerdeHover, SpBtnVerdePress)
                : estilo == "oro" ? (SpBtnOro, SpBtnOroHover, SpBtnOroPress)
                : estilo == "rojo" ? (SpBtnRojo, SpBtnRojoHover, SpBtnRojoPress)
                : (SpBtnMadera, SpBtnMaderaHover, SpBtnMaderaPress);

            var im = Caja9(padre, "boton", spNorm, true);
            if (!activo) im.color = new Color(0.6f, 0.6f, 0.6f, 0.65f);
            var b = im.gameObject.AddComponent<Button>();
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            b.transition = Selectable.Transition.SpriteSwap;
            b.spriteState = new SpriteState { highlightedSprite = spHover, pressedSprite = spPress, selectedSprite = spHover, disabledSprite = spNorm };
            b.interactable = activo;
            if (alPulsar != null) b.onClick.AddListener(() => alPulsar());

            Color colorTxt = estilo == "oro" ? Hex("#2b1704") : Crema;
            var t = Texto(im.transform, txt, tamFuente, colorTxt, TextAnchor.MiddleCenter, true);
            if (estilo == "oro") t.GetComponent<Shadow>().effectColor = new Color(1f, 0.95f, 0.7f, 0.6f);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.rectTransform.Llenar(6);
            var le = im.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = alto; le.minHeight = alto;
            le.preferredWidth = ancho > 0 ? ancho : Mathf.Max(52, txt.Length * 9 + 28);
            return b;
        }

        public static void Tam(GameObject go, float w, float h)
        {
            if (!go.TryGetComponent<LayoutElement>(out var le)) le = go.AddComponent<LayoutElement>();
            if (w > 0) { le.preferredWidth = w; le.minWidth = w; }
            if (h > 0) { le.preferredHeight = h; le.minHeight = h; }
            ((RectTransform)go.transform).sizeDelta = new Vector2(w > 0 ? w : ((RectTransform)go.transform).sizeDelta.x, h > 0 ? h : ((RectTransform)go.transform).sizeDelta.y);
        }

        public static HorizontalLayoutGroup Fila(Transform padre, float espacio = 8, TextAnchor alineado = TextAnchor.MiddleLeft, string nombre = "fila")
        {
            var r = Nodo(nombre, padre);
            var h = r.gameObject.AddComponent<HorizontalLayoutGroup>();
            h.spacing = espacio; h.childAlignment = alineado;
            h.childControlWidth = true; h.childControlHeight = true; h.childForceExpandWidth = false; h.childForceExpandHeight = false;
            return h;
        }

        public static VerticalLayoutGroup Columna(Transform padre, float espacio = 4, string nombre = "columna")
        {
            var r = Nodo(nombre, padre);
            var v = r.gameObject.AddComponent<VerticalLayoutGroup>();
            v.spacing = espacio; v.childAlignment = TextAnchor.UpperLeft;
            v.childControlWidth = true; v.childControlHeight = true; v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return v;
        }

        public static void Vaciar(Transform t)
        {
            for (int i = t.childCount - 1; i >= 0; i--) { var c = t.GetChild(i); c.SetParent(null, false); UnityEngine.Object.Destroy(c.gameObject); }
        }

        public static Image Barra(Transform padre, string nombre, Color color, bool vertical)
        {
            Inicializar();
            var fondo = Caja9(padre, nombre, SpPlaca);
            var relleno = Caja(fondo.transform, "relleno", color);
            relleno.rectTransform.Llenar(3);
            var textura = Texture2D.whiteTexture;
            relleno.sprite = Sprite.Create(textura, new UnityEngine.Rect(0, 0, textura.width, textura.height), new Vector2(0.5f, 0.5f));
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = vertical ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal;
            relleno.fillOrigin = 0;
            return relleno;
        }
    }

    public class Mantener : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public bool activo;
        public Action alApretar;
        public void OnPointerDown(PointerEventData e) { activo = true; alApretar?.Invoke(); }
        public void OnPointerUp(PointerEventData e) => activo = false;
        public void OnPointerExit(PointerEventData e) => activo = false;
        void OnDisable() => activo = false;
    }
}
