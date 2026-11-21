// Piezas de interfaz (uGUI) creadas por código: así el proyecto no depende de escenas ni prefabs.
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HuertaNueva
{
    public static class UIKit
    {
        public static Font Fuente;
        public static readonly Color Crema = Hex("#f3eedb"), Oro = Hex("#f2c14e"), Fondo = Hex("#2a1f17f2"), Borde = Hex("#5b4330"),
            Boton = Hex("#4a3727"), BotonPrimario = Hex("#3f7d4a"), Gris = Hex("#b9ad94"), Rojo = Hex("#e2674f"), Azul = Hex("#5aa9e6"), Verde = Hex("#79c267");

        public static Color Hex(string h) => ColorUtility.TryParseHtmlString(h, out var c) ? c : Color.magenta;

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
            var s = r.gameObject.AddComponent<Shadow>(); s.effectColor = new Color(0, 0, 0, 0.6f); s.effectDistance = new Vector2(1, -1);
            return t;
        }

        public static Button Boton(Transform padre, string txt, Action alPulsar, bool primario = false, bool activo = true, float ancho = 0, float alto = 34)
        {
            var im = Caja(padre, "boton", primario ? BotonPrimario : Boton, true);
            var b = im.gameObject.AddComponent<Button>();
            b.navigation = new Navigation { mode = Navigation.Mode.None };
            var cb = b.colors; cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f); cb.disabledColor = new Color(0.55f, 0.55f, 0.55f, 0.6f); b.colors = cb;
            b.interactable = activo;
            if (alPulsar != null) b.onClick.AddListener(() => alPulsar());
            var t = Texto(im.transform, txt, 15, Crema, TextAnchor.MiddleCenter, true);
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.rectTransform.Llenar(4);
            var le = im.gameObject.AddComponent<LayoutElement>();
            le.preferredHeight = alto; le.minHeight = alto;
            le.preferredWidth = ancho > 0 ? ancho : Mathf.Max(44, txt.Length * 9 + 22);
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

        // Barra que se llena (vida del jefe, energía…).
        public static Image Barra(Transform padre, string nombre, Color color, bool vertical)
        {
            var fondo = Caja(padre, nombre, new Color(0, 0, 0, 0.55f));
            var relleno = Caja(fondo.transform, "relleno", color);
            relleno.rectTransform.Llenar(2);
            relleno.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            relleno.type = Image.Type.Filled;
            relleno.fillMethod = vertical ? Image.FillMethod.Vertical : Image.FillMethod.Horizontal;
            relleno.fillOrigin = 0;
            return relleno;
        }
    }

    // Botón que se mantiene apretado (cruceta táctil).
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
