// Teclado, ratón y táctil. Funciona con el Input System nuevo (lo trae Unity 6) y, si el proyecto
// usa el sistema antiguo, con UnityEngine.Input.
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace HuertaNueva
{
    public enum Tecla { Arriba, Abajo, Izq, Der, W, A, S, D, Usar, UsarJ, UsarEnter, Comer, Menu, MenuTab, Crafteo, Escape, Anterior, Siguiente,
        N1, N2, N3, N4, N5, N6, N7, N8, N9, N0, Menos, Igual }

    public static class Input_
    {
#if ENABLE_INPUT_SYSTEM
        static readonly Key[] K = { Key.UpArrow, Key.DownArrow, Key.LeftArrow, Key.RightArrow, Key.W, Key.A, Key.S, Key.D, Key.Space, Key.J, Key.Enter, Key.F, Key.I, Key.Tab, Key.C, Key.Escape, Key.Q, Key.E,
            Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0, Key.Minus, Key.Equals };
        public static bool Pulsada(Tecla t) => Keyboard.current != null && Keyboard.current[K[(int)t]].isPressed;
        public static bool Bajada(Tecla t) => Keyboard.current != null && Keyboard.current[K[(int)t]].wasPressedThisFrame;
        public static bool Clic => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        public static Vector2 PosRaton => Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        public static float Rueda => Mouse.current != null ? Mouse.current.scroll.ReadValue().y : 0;
        public static bool HayTactil() => Touchscreen.current != null;
#else
        static readonly KeyCode[] K = { KeyCode.UpArrow, KeyCode.DownArrow, KeyCode.LeftArrow, KeyCode.RightArrow, KeyCode.W, KeyCode.A, KeyCode.S, KeyCode.D, KeyCode.Space, KeyCode.J, KeyCode.Return, KeyCode.F, KeyCode.I, KeyCode.Tab, KeyCode.C, KeyCode.Escape, KeyCode.Q, KeyCode.E,
            KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0, KeyCode.Minus, KeyCode.Equals };
        public static bool Pulsada(Tecla t) => Input.GetKey(K[(int)t]);
        public static bool Bajada(Tecla t) => Input.GetKeyDown(K[(int)t]);
        public static bool Clic => Input.GetMouseButtonDown(0) && Input.touchCount == 0;
        public static Vector2 PosRaton => Input.mousePosition;
        public static float Rueda => Input.mouseScrollDelta.y;
        public static bool HayTactil() => Input.touchSupported;
#endif
    }
}
