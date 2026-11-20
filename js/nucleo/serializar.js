// Guardado y carga del estado como JSON. "version" permite migrar partidas viejas.
import { VERSION_ESTADO } from './mundo.js';

export const aJSON = estado => JSON.stringify(estado);

export function deJSON(texto, def) {
  const e = typeof texto === 'string' ? JSON.parse(texto) : texto;
  if (!e || e.version !== VERSION_ESTADO) return null;     // la v1 (prototipo) no se migra
  // descartar lo que ya no exista en el contenido (por ejemplo, un objeto borrado del JSON)
  e.granja.objetos = e.granja.objetos.filter(o => def.OBJ[o.id]);
  for (const j of Object.values(e.jugadores)) j.inv = j.inv.map(s => (s && def.ITEMS[s.id] ? s : null));
  for (const k of Object.keys(e.granja.cultivos)) if (!def.CULT[e.granja.cultivos[k].id]) delete e.granja.cultivos[k];
  e.granja.ver = (e.granja.ver || 0) + 1;
  return e;
}
