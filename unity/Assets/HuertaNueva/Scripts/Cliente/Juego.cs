// Arranque: al dar Play en cualquier escena se crea el juego entero por código (contenido, gráficos,
// vista, interfaz y sesión). Es la traducción de js/cliente/main.js.
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

namespace HuertaNueva
{
    public class Juego : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Arrancar()
        {
            if (FindAnyObjectByType<Juego>() == null) new GameObject("Mosslantern").AddComponent<Juego>();
        }

        Def def; Graficos G; Vista vista; Interfaz ui; AlmacenLocal almacen;
        SesionLocal sesion; string perfil; bool jugando;

        void Awake()
        {
            Application.targetFrameRate = 60;
            var txt = Resources.Load<TextAsset>("HuertaNueva/contenido");
            if (txt == null) { Debug.LogError("Falta Resources/HuertaNueva/contenido.json: ejecuta herramientas/unity/sincronizar.py"); enabled = false; return; }
            def = Def.Cargar(txt.text);
            G = new Graficos(def);
            perfil = Perfil.Id();
            string cuerpo = PlayerPrefs.GetString("huerta-nueva-cuerpo", def.personajes[0].id);
            if (!def.personajes.Exists(p => p.id == cuerpo)) cuerpo = def.personajes[0].id;
            almacen = new AlmacenLocal(def);

            if (FindAnyObjectByType<EventSystem>() == null)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem));
#if ENABLE_INPUT_SYSTEM
                es.AddComponent<InputSystemUIInputModule>();
#else
                es.AddComponent<StandaloneInputModule>();
#endif
            }

            vista = new Vista(def, G);
            // granja de fondo detrás de la portada (no se guarda)
            Conectar(new SesionLocal(HuertaNueva.Mundo.CrearEstado(def, (uint)def.semillaMundo, perfil, cuerpo), def, perfil, null));
            ui = new Interfaz(def, G, () => sesion, vista, cuerpo);
            // Una partida nueva se guarda encima del archivo: antes se copia el guardado que hubiera.
            ui.AlNueva = c => { bool respaldo = almacen.Respaldar() != null; Empezar(NuevaPartida(c)); if (respaldo) ui.Aviso("Your previous save was backed up"); };
            ui.AlContinuar = () =>
            {
                var e = almacen.Cargar();
                if (e != null && e.BuscarJugador(perfil) == null)
                {
                    // Partida local de otro id (p. ej. se borraron los PlayerPrefs): se sigue jugando con ese jugador.
                    perfil = e.jugadores[0].id;
                    Perfil.Fijar(perfil);
                }
                if (e != null) { Empezar(e); return; }
                almacen.Respaldar();
                Empezar(NuevaPartida(PlayerPrefs.GetString("huerta-nueva-cuerpo", def.personajes[0].id)));
                ui.Aviso("Could not read saved game. A backup copy was kept.", true);
            };
            ui.AlReiniciar = () => { almacen.Borrar(); Empezar(NuevaPartida(sesion.Yo.cuerpo)); ui.Aviso("New game started"); };
            ui.AlSalirMenu = () => { if (jugando) sesion.Guardar(); jugando = false; ui.MostrarPortada(almacen.Existe(), almacen.Cargar()); };
            ui.AlUsar = () => { if (jugando && !ui.Abierto) UsarFrente(); };
            ui.AlComer = () => { if (jugando && !ui.Abierto) sesion.Despachar(new Accion { tipo = "comer" }); };
            ui.MostrarPortada(almacen.Existe(), almacen.Cargar());
        }

        Estado NuevaPartida(string cuerpo) => HuertaNueva.Mundo.CrearEstado(def, (uint)Random.Range(1, int.MaxValue), perfil, cuerpo);

        void Conectar(SesionLocal s)
        {
            sesion = s;
            sesion.AlEvento += ev => { vista.Eventos(ev, perfil); ui?.Eventos(ev); };
        }

        void Empezar(Estado e)
        {
            Conectar(new SesionLocal(e, def, perfil, almacen));
            sesion.Guardar();
            ui.OcultarPortada(); ui.Cerrar(); jugando = true;
            ui.Aviso("Till with the hoe, plant seeds, and water them. The stall sells seeds.");
        }

        void UsarFrente() { var (x, y) = Reglas.CeldaFrente(def, sesion.Yo); sesion.Despachar(new Accion { tipo = "usar", x = x, y = y }); }
        void Seleccionar(int i) => sesion.Despachar(new Accion { tipo = "seleccionar", i = i });

        // En el celular la app se cierra sin avisar: guardamos al pasar a segundo plano.
        void OnApplicationPause(bool pausa) { if (pausa && jugando) sesion.Guardar(); }
        void OnApplicationQuit() { if (jugando) sesion.Guardar(); }

        void Update()
        {
            float dt = Mathf.Min(0.05f, Time.deltaTime);
            // un botón pulsado con el ratón queda "seleccionado" y Espacio lo volvería a pulsar
            if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null) EventSystem.current.SetSelectedGameObject(null);

            if (jugando) Entrada(dt);
            vista.Actualizar(dt, sesion.Estado);
            vista.Dibujar(sesion.Estado, perfil, jugando && !ui.Abierto);
            ui.PintarHUD(dt, jugando);
            if (jugando) sesion.Tick(dt);
        }

        void Entrada(float dt)
        {
            if (ui.Abierto)
            {
                if ((Input_.Bajada(Tecla.Escape) || Input_.Bajada(Tecla.Menu) || Input_.Bajada(Tecla.MenuTab) || Input_.Bajada(Tecla.Crafteo)) && ui.Cerrable) ui.Cerrar();
                return;
            }
            if (Input_.Bajada(Tecla.Menu) || Input_.Bajada(Tecla.MenuTab)) { ui.Menu("mochila"); return; }
            if (Input_.Bajada(Tecla.Crafteo)) { ui.Menu("crafteo"); return; }
            if (Input_.Bajada(Tecla.Comer)) sesion.Despachar(new Accion { tipo = "comer" });
            if (Input_.Bajada(Tecla.Usar) || Input_.Bajada(Tecla.UsarJ) || Input_.Bajada(Tecla.UsarEnter)) UsarFrente();
            if (ui.Abierto) return;   // usar pudo abrir un panel
            for (int n = 0; n < 12; n++) if (Input_.Bajada(Tecla.N1 + n)) Seleccionar(n);
            if (Input_.Bajada(Tecla.Anterior)) Seleccionar((sesion.Yo.sel + 11) % 12);
            if (Input_.Bajada(Tecla.Siguiente)) Seleccionar((sesion.Yo.sel + 1) % 12);
            float rueda = Input_.Rueda;
            if (Mathf.Abs(rueda) > 0.01f) Seleccionar((sesion.Yo.sel + (rueda < 0 ? 1 : 11)) % 12);

            // clic con el ratón en una casilla vecina: mirar hacia ella y usarla
            bool sobreUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (Input_.Clic && !sobreUI)
            {
                var (x, y) = vista.ACelda(Input_.PosRaton); var (px, py) = Reglas.CeldaJugador(def, sesion.Yo);
                int dx = x - px, dy = y - py;
                if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) <= 1)
                {
                    if (dx != 0 || dy != 0) sesion.Despachar(new Accion { tipo = "mirar", dir = Mathf.Abs(dx) > Mathf.Abs(dy) ? (dx > 0 ? "east" : "west") : (dy > 0 ? "south" : "north") });
                    sesion.Despachar(new Accion { tipo = "usar", x = x, y = y });
                }
            }
            if (ui.Abierto) return;

            bool T(Mantener m) => m != null && m.activo;
            int mx = (Input_.Pulsada(Tecla.D) || Input_.Pulsada(Tecla.Der) || T(ui.tDer) ? 1 : 0) - (Input_.Pulsada(Tecla.A) || Input_.Pulsada(Tecla.Izq) || T(ui.tIzq) ? 1 : 0);
            int my = (Input_.Pulsada(Tecla.S) || Input_.Pulsada(Tecla.Abajo) || T(ui.tAbajo) ? 1 : 0) - (Input_.Pulsada(Tecla.W) || Input_.Pulsada(Tecla.Arriba) || T(ui.tArriba) ? 1 : 0);
            if (mx != 0 || my != 0) sesion.Despachar(new Accion { tipo = "mover", dx = mx, dy = my, dt = dt });
            sesion.Despachar(new Accion { tipo = "tiempo", dt = dt });
        }
    }
}
