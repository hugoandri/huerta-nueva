// Guardado y carga del estado como JSON. "version" permite migrar partidas viejas.
import { VERSION_ESTADO, VIDA_BASE, colocar, quitar, ENTRADA_CUEVA, CASA, CAJA, PUESTO, enClaro, trazarBordes, objetoEn, idx } from './mundo.js';

export const aJSON = estado => JSON.stringify(estado);

// Mueve un objeto a (x, y), o al lugar libre más cercano a la derecha.
function mover(def, g, id, x, y) {
  const o = g.objetos.find(o => o.id === id);
  if (o) quitar(g, o);
  for (let r = 0; r < 8; r++) if (colocar(def, g, id, x + r, y, o?.datos)) return;
}

// v2 → v3: vida, ropa, lugar, mazmorra, máquinas por minutos.
function migrarV2(e) {
  for (const j of Object.values(e.jugadores)) Object.assign(j, { vida: VIDA_BASE, vidaMax: VIDA_BASE, cdAtaque: 0, ropa: 'ropa_granjera', lugar: 'granja' });
  e.mazmorra = { semilla: null, piso: 0, nivel: null, record: 0, completada: 0 };
  for (const o of e.granja.objetos) if (o.datos?.listo != null) { o.datos.listoEn = o.datos.listo * 1440 + 6 * 60; delete o.datos.listo; }
  e.version = 3;
  return e;
}

// v3 → v4: dos personajes, ropa de campo y de batalla, varias mazmorras, cueva a la derecha
// y la casa con su nueva huella.
function migrarV3(e, def) {
  const viejo = e.mazmorra || {};
  e.mazmorra = { actual: null, semilla: null, piso: 0, nivel: null, progreso: { mina: { record: viejo.record || 0, completada: viejo.completada || 0 } } };
  for (const j of Object.values(e.jugadores)) {
    j.cuerpo ||= 'granjera';
    const puesta = def.ITEMS[j.ropa];
    if (puesta?.uso === 'batalla') { j.ropaBatalla = j.ropa; j.ropa = 'ropa_granjera'; } else j.ropaBatalla ||= 'ropa_aventurera';
    j.lugar = 'granja'; j.vidaMax = VIDA_BASE; j.vida = Math.min(j.vida ?? VIDA_BASE, VIDA_BASE);
  }
  e.granja.ver = (e.granja.ver || 0) + 1;
  mover(def, e.granja, 'casa', CASA.x, CASA.y);
  mover(def, e.granja, 'entrada_cueva', ENTRADA_CUEVA.x, ENTRADA_CUEVA.y);
  e.version = 4;
  return e;
}

// v4 → v5: el túnel pasa al borde de arriba; se despeja el lugar (árboles, rocas, maleza).
function migrarV4(e, def) {
  const g = e.granja;
  g.objetos = g.objetos.filter(o => !(['arbol', 'roca', 'maleza'].includes(o.id) && enClaro(o.x, o.y) && o.y <= 7 && o.x >= 19));
  g.ver = (g.ver || 0) + 1;
  mover(def, g, 'entrada_cueva', ENTRADA_CUEVA.x, ENTRADA_CUEVA.y);
  e.version = 5;
  return e;
}

// v5 → v6: acantilado arriba con el túnel en su pared, camino de tierra, casa y puesto más grandes.
function migrarV5(e, def) {
  const g = e.granja;
  trazarBordes(def, g);
  // despejar lo que estorbe en el acantilado, el camino y las nuevas huellas (sin tocar lo que construyó el jugador)
  const natural = o => ['arbol', 'roca', 'maleza'].includes(o.id);
  const huellas = [[CASA.x, CASA.y, 6, 3], [CAJA.x, CAJA.y, 1, 1], [PUESTO.x, PUESTO.y, 3, 2], [ENTRADA_CUEVA.x, ENTRADA_CUEVA.y, 2, 1]];
  const enHuella = (x, y) => huellas.some(([hx, hy, w, h]) => x >= hx && x < hx + w && y >= hy && y < hy + h);
  g.objetos = g.objetos.filter(o => !(natural(o) && (o.y < 2 ? (o.x >= 19 && o.x <= 26) : (enClaro(o.x, o.y) || g.camino[idx(def, o.x, o.y)] || enHuella(o.x, o.y)))));
  for (let i = 0; i < g.camino.length; i++) if (g.camino[i] || g.acantilado[i]) { g.suelo[i] = 0; g.regado[i] = 0; delete g.cultivos[i]; }
  g.ver++;
  for (const [id, p] of [['casa', CASA], ['caja_envios', CAJA], ['puesto', PUESTO], ['entrada_cueva', ENTRADA_CUEVA]]) mover(def, g, id, p.x, p.y);
  e.version = 6;
  return e;
}

export function deJSON(texto, def) {
  let e = typeof texto === 'string' ? JSON.parse(texto) : texto;
  if (!e) return null;
  if (e.version === 2) e = migrarV2(e);
  if (e.version === 3) e = migrarV3(e, def);
  if (e.version === 4) e = migrarV4(e, def);
  if (e.version === 5) e = migrarV5(e, def);
  if (e.version !== VERSION_ESTADO) return null;     // la v1 (prototipo) no se migra
  // descartar lo que ya no exista en el contenido (por ejemplo, un objeto borrado del JSON)
  e.granja.objetos = e.granja.objetos.filter(o => def.OBJ[o.id]);
  for (const j of Object.values(e.jugadores)) j.inv = j.inv.map(s => (s && def.ITEMS[s.id] ? s : null));
  for (const k of Object.keys(e.granja.cultivos)) if (!def.CULT[e.granja.cultivos[k].id]) delete e.granja.cultivos[k];
  if (e.mazmorra.actual && !def.MAZ[e.mazmorra.actual]) Object.assign(e.mazmorra, { actual: null, nivel: null });
  e.granja.ver = (e.granja.ver || 0) + 1;
  if (e.mazmorra.nivel) e.mazmorra.nivel.ver = (e.mazmorra.nivel.ver || 0) + 1;
  return e;
}
