// Estado del mundo: una granja (con su dueño) y los jugadores presentes en ella.
// Todo es JSON plano para poder guardarlo en el navegador hoy y en un servidor mañana.
import { mulberry32, entre } from './azar.js';
import { agregar, TAM_INVENTARIO } from './inventario.js';

export const VERSION_ESTADO = 2;
export const INICIO_DIA = 6 * 60;

export const idx = (def, x, y) => y * def.W + x;
export const dentro = (def, x, y) => x >= 0 && y >= 0 && x < def.W && y < def.H;

// Índice celda → objeto. Se recalcula solo cuando cambia granja.ver (no se guarda).
const cacheOcupa = new WeakMap();
export function ocupacion(def, g) {
  let c = cacheOcupa.get(g);
  if (c && c.ver === g.ver) return c;
  const ocupa = new Int32Array(def.W * def.H), porUid = new Map();
  for (const o of g.objetos) {
    porUid.set(o.uid, o);
    const d = def.OBJ[o.id], w = d.ancho || 1, h = d.alto || 1;
    for (let y = o.y; y < o.y + h; y++) for (let x = o.x; x < o.x + w; x++) if (dentro(def, x, y)) ocupa[idx(def, x, y)] = o.uid;
  }
  c = { ver: g.ver, ocupa, porUid };
  cacheOcupa.set(g, c);
  return c;
}
export function objetoEn(def, g, x, y) {
  if (!dentro(def, x, y)) return null;
  const c = ocupacion(def, g);
  return c.porUid.get(c.ocupa[idx(def, x, y)]) || null;
}
export const objetoPorUid = (def, g, uid) => ocupacion(def, g).porUid.get(uid) || null;

export function libre(def, g, x, y, w = 1, h = 1) {
  const oc = ocupacion(def, g).ocupa;
  for (let yy = y; yy < y + h; yy++) for (let xx = x; xx < x + w; xx++) {
    if (!dentro(def, xx, yy)) return false;
    const i = idx(def, xx, yy);
    if (g.agua[i] || oc[i] || g.suelo[i] || g.cultivos[i]) return false;
  }
  return true;
}
export function colocar(def, g, id, x, y, datos) {
  const d = def.OBJ[id];
  if (!libre(def, g, x, y, d.ancho || 1, d.alto || 1)) return null;
  const o = { uid: g.sigUid++, id, x, y, golpes: d.golpes || 0, datos: datos || {} };
  g.objetos.push(o); g.ver++;
  return o;
}
export function quitar(g, o) { g.objetos = g.objetos.filter(x => x !== o); g.ver++; }

export const posicionInicio = def => ({ x: 7.5 * def.T, y: 7.6 * def.T, dir: 'south' });

export function nuevoJugador(def, { id, nombre }) {
  const j = {
    id, nombre: nombre || 'Granjera', ...posicionInicio(def),
    energia: 100, energiaMax: 100, dinero: def.C.inicio.dinero,
    inv: Array(TAM_INVENTARIO).fill(null), sel: 0,
    aguaRegadera: def.ITEMS.regadera?.capacidad || 20, envios: [],
  };
  for (const [item, n] of def.C.inicio.inventario) agregar(def, j, item, n);
  return j;
}

// Genera la granja a partir de una semilla: misma semilla → misma granja.
export function crearGranja(def, semilla, dueno) {
  const { W, H } = def, r = mulberry32(semilla);
  const g = {
    dueno, ver: 0, sigUid: 1, objetos: [], cultivos: {},
    agua: Array(W * H).fill(0), suelo: Array(W * H).fill(0), regado: Array(W * H).fill(0),
  };
  for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
    const e = ((x - 35) / 6) ** 2 + ((y - 23) / 4.2) ** 2 + Math.sin(x * 1.7 + y) * 0.08;
    if (e < 1) g.agua[idx(def, x, y)] = 1;
  }
  colocar(def, g, 'casa', 5, 3); colocar(def, g, 'caja_envios', 10, 5); colocar(def, g, 'puesto', 12, 4);
  const claro = (x, y) => x >= 3 && x <= 17 && y >= 2 && y <= 10;
  const arbolCerca = (x, y) => {
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) if (objetoEn(def, g, x + dx, y + dy)?.id === 'arbol') return true;
    return false;
  };
  for (let x = 0; x < W; x += 2) { colocar(def, g, 'arbol', x, 1); colocar(def, g, 'arbol', x + (x % 4 ? 1 : 0), H - 1); }
  for (let y = 3; y < H - 1; y += 2) { colocar(def, g, 'arbol', 0, y); colocar(def, g, 'arbol', W - 1, y); }
  const esparcir = (id, n) => {
    for (let k = 0, intentos = 0; k < n && intentos < 4000; intentos++) {
      const x = entre(r(), 2, W - 3), y = entre(r(), 3, H - 3);
      if (claro(x, y) || !libre(def, g, x, y)) continue;
      let cercaAgua = false;
      for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) if (dentro(def, x + dx, y + dy) && g.agua[idx(def, x + dx, y + dy)]) cercaAgua = true;
      if (cercaAgua && id !== 'maleza') continue;
      if (id === 'arbol' && arbolCerca(x, y)) continue;
      colocar(def, g, id, x, y); k++;
    }
  };
  esparcir('arbol', 26); esparcir('roca', 45); esparcir('maleza', 90);
  return g;
}

export function crearEstado(def, { semilla = def.C.mundo.semilla, jugador }) {
  return {
    version: VERSION_ESTADO,
    semilla: semilla >>> 0, paso: 0,
    tiempo: { dia: 1, min: INICIO_DIA, lluvia: false },
    granja: crearGranja(def, semilla, jugador.id),
    jugadores: { [jugador.id]: nuevoJugador(def, jugador) },
  };
}
