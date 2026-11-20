// Dónde se guarda la partida. Hoy: el navegador (localStorage).
// Para el online se agrega un AlmacenNube con la MISMA interfaz:
//   cargar(): Promise<estado|null>   guardar(estado): Promise   borrar(): Promise   existe(): Promise<boolean>
// por ejemplo leyendo y escribiendo una fila "partidas" en Supabase con el id del jugador.
import { aJSON, deJSON } from '../nucleo/serializar.js';

export class AlmacenLocal {
  constructor(def, clave = 'huerta-nueva-v2') { this.def = def; this.clave = clave; }
  async cargar() {
    try { const t = localStorage.getItem(this.clave); return t ? deJSON(t, this.def) : null; }
    catch (e) { return null; }
  }
  async guardar(estado) {
    try { localStorage.setItem(this.clave, aJSON(estado)); return true; }
    catch (e) { return false; }   // sin almacenamiento: la partida dura mientras la página siga abierta
  }
  async borrar() { try { localStorage.removeItem(this.clave); } catch (e) { /* nada que borrar */ } }
  async existe() { try { return !!localStorage.getItem(this.clave); } catch (e) { return false; } }
}
