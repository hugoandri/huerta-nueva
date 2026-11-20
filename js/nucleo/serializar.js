// Guardado y carga del estado como JSON. "version" permite migrar partidas viejas.
import { VERSION_ESTADO, VIDA_BASE, colocar, ENTRADA_CUEVA } from './mundo.js';

export const aJSON = estado => JSON.stringify(estado);

// v2 → v3: vida, ropa, lugar, mazmorra, máquinas por minutos y la cueva en la granja.
function migrarV2(e, def) {
  for (const j of Object.values(e.jugadores)) {
    Object.assign(j, { vida: VIDA_BASE, vidaMax: VIDA_BASE, cdAtaque: 0, ropa: 'ropa_granjera', lugar: 'granja' });
  }
  e.mazmorra = { semilla: null, piso: 0, nivel: null, record: 0, completada: 0 };
  for (const o of e.granja.objetos) if (o.datos?.listo != null) { o.datos.listoEn = o.datos.listo * 1440 + 6 * 60; delete o.datos.listo; }
  if (!e.granja.objetos.some(o => o.id === 'entrada_cueva')) {
    // si el lugar está ocupado se busca uno cercano
    for (let r = 0; r < 6; r++) if (colocar(def, e.granja, 'entrada_cueva', ENTRADA_CUEVA.x + r, ENTRADA_CUEVA.y)) break;
  }
  e.version = 3;
  return e;
}

export function deJSON(texto, def) {
  let e = typeof texto === 'string' ? JSON.parse(texto) : texto;
  if (!e) return null;
  if (e.version === 2) e = migrarV2(e, def);
  if (e.version !== VERSION_ESTADO) return null;     // la v1 (prototipo) no se migra
  // descartar lo que ya no exista en el contenido (por ejemplo, un objeto borrado del JSON)
  e.granja.objetos = e.granja.objetos.filter(o => def.OBJ[o.id]);
  for (const j of Object.values(e.jugadores)) j.inv = j.inv.map(s => (s && def.ITEMS[s.id] ? s : null));
  for (const k of Object.keys(e.granja.cultivos)) if (!def.CULT[e.granja.cultivos[k].id]) delete e.granja.cultivos[k];
  e.granja.ver = (e.granja.ver || 0) + 1;
  if (e.mazmorra.nivel) e.mazmorra.nivel.ver = (e.mazmorra.nivel.ver || 0) + 1;
  return e;
}
