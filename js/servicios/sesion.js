// La sesión es lo único que el cliente usa para cambiar el juego:
//   sesion.despachar(accion)   sesion.alEvento(fn)   sesion.estado   sesion.yo
//
// SesionLocal aplica cada acción al momento con las reglas del núcleo.
// Una futura SesionRed tendrá la misma interfaz: enviará la acción al servidor (o al
// anfitrión de la mazmorra), la aplicará también localmente para que se sienta inmediata,
// y recibirá las acciones y eventos de los demás jugadores.
import { aplicar } from '../nucleo/reglas.js';

const AUTOGUARDADO_MS = 20000;

export class SesionLocal {
  constructor({ estado, def, almacen = null, jugadorId }) {
    this.estado = estado; this.def = def; this.almacen = almacen; this.jugadorId = jugadorId;
    this.oyentes = new Set();
    this.cambios = false;
    this._intervalo = almacen ? setInterval(() => this.cambios && this.guardar(), AUTOGUARDADO_MS) : null;
  }
  get yo() { return this.estado.jugadores[this.jugadorId]; }
  despachar(accion) {
    const ev = aplicar(this.estado, this.def, { jugador: this.jugadorId, ...accion });
    this.cambios = true;
    for (const f of this.oyentes) f(ev);
    if (ev.some(e => e.tipo === 'resumen')) this.guardar();   // cada día nuevo queda guardado
    return ev;
  }
  alEvento(f) { this.oyentes.add(f); return () => this.oyentes.delete(f); }
  async guardar() { if (this.almacen) { this.cambios = false; await this.almacen.guardar(this.estado); } }
  cerrar() { clearInterval(this._intervalo); this.oyentes.clear(); }
}
