// La sesión es lo único que el cliente usa para cambiar el juego: Despachar(accion) y el evento AlEvento.
// SesionLocal aplica cada acción al momento con las reglas del núcleo. Una futura SesionRed tendrá la
// misma interfaz: enviará la acción al servidor, la aplicará también localmente y recibirá la de los demás.
using System;
using System.Collections.Generic;

namespace HuertaNueva
{
    public interface ISesion
    {
        Estado Estado { get; }
        Def Def { get; }
        Jugador Yo { get; }
        event Action<Evento> AlEvento;
        void Despachar(Accion a);
        void Guardar();
    }

    public class SesionLocal : ISesion
    {
        const float AUTOGUARDADO = 20;
        public Estado Estado { get; private set; }
        public Def Def { get; private set; }
        public string JugadorId { get; private set; }
        public Jugador Yo => Estado.BuscarJugador(JugadorId);
        public event Action<Evento> AlEvento;
        readonly AlmacenLocal almacen;
        bool cambios;
        float reloj;

        public SesionLocal(Estado estado, Def def, string jugadorId, AlmacenLocal almacen)
        {
            Estado = estado; Def = def; JugadorId = jugadorId; this.almacen = almacen;
        }

        public void Despachar(Accion a)
        {
            a.jugador = a.jugador ?? JugadorId;
            List<Evento> ev = Reglas.Aplicar(Estado, Def, a);
            cambios = true;
            foreach (var e in ev)
            {
                if (e.jugador != null && e.jugador != JugadorId) continue;   // eventos de otro jugador
                AlEvento?.Invoke(e);
                if (e.tipo == "resumen" || e.tipo == "piso" || e.tipo == "derrota" || e.tipo == "botin") Guardar();
            }
        }

        // Llamar cada cuadro: autoguardado.
        public void Tick(float dt)
        {
            reloj += dt;
            if (reloj >= AUTOGUARDADO) { reloj = 0; if (cambios) Guardar(); }
        }

        public void Guardar() { if (almacen != null && almacen.Guardar(Estado)) cambios = false; }
    }
}
