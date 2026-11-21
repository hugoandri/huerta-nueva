// Estado del mundo: una granja (con su dueño) y los jugadores presentes en ella.
// Todo es JSON plano para poder guardarlo en el navegador hoy y en un servidor mañana.
import { mulberry32, entre } from './azar.js';
import { agregar, TAM_INVENTARIO } from './inventario.js';

export const VERSION_ESTADO = 6;
export const VIDA_BASE = 10;   // medios corazones
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
    if (g.agua[i] || oc[i] || g.suelo[i] || g.cultivos[i] || g.muro?.[i] || g.acantilado?.[i]) return false;
  }
  return true;
}
// enPared: para escaleras y puertas que van dentro de una muralla (la celda es muro, no suelo libre).
export function colocar(def, g, id, x, y, datos, enPared = false) {
  const d = def.OBJ[id];
  if (enPared ? objetoEn(def, g, x, y) : !libre(def, g, x, y, d.ancho || 1, d.alto || 1)) return null;
  const o = { uid: g.sigUid++, id, x, y, golpes: d.golpes || 0, datos: datos || {} };
  g.objetos.push(o); g.ver++;
  return o;
}
export function quitar(g, o) { g.objetos = g.objetos.filter(x => x !== o); g.ver++; }

// Distribución de la granja: acantilado arriba (filas 0 y 1) con el túnel en su pared,
// casa grande a la izquierda, caja de envíos y puesto al lado, y un camino que lo une todo.
export const FILAS_ACANTILADO = 2;
export const CASA = { x: 3, y: 5 };                 // huella 6×3: la puerta queda en la columna 6, fila 7
export const CAJA = { x: 10, y: 7 };
export const PUESTO = { x: 12, y: 6 };
export const posicionInicio = def => ({ x: 6 * def.T, y: 8.6 * def.T, dir: 'south' });
// El túnel a las mazmorras: en la pared del acantilado, mirando hacia abajo.
export const ENTRADA_CUEVA = { x: 22, y: FILAS_ACANTILADO };
export const frenteACueva = def => ({ x: (ENTRADA_CUEVA.x + 1) * def.T, y: (ENTRADA_CUEVA.y + 1.6) * def.T, dir: 'south' });
// Zonas que se mantienen despejadas (alrededor de la casa y del túnel).
export const enClaro = (x, y) => (x >= 2 && x <= 17 && y >= 2 && y <= 11) || (x >= 19 && x <= 26 && y >= 2 && y <= 6);

// Camino de tierra: del túnel baja hasta la fila 9 y va a la puerta de la casa; pasa por la caja y el puesto.
export function celdasCamino() {
  const c = [];
  for (let y = 3; y <= 10; y++) for (const x of [22, 23]) c.push([x, y]);
  for (let x = 5; x <= 23; x++) for (const y of [9, 10]) c.push([x, y]);
  for (const x of [5, 6]) c.push([x, 8]);
  return c;
}
// Marca el acantilado (bloquea el paso) y el camino. Se usa al crear la granja y al migrar partidas.
export function trazarBordes(def, g) {
  const { W, H } = def;
  g.acantilado = Array(W * H).fill(0);
  g.camino = Array(W * H).fill(0);
  for (let y = 0; y < FILAS_ACANTILADO; y++) for (let x = 0; x < W; x++) g.acantilado[idx(def, x, y)] = 1;
  for (const [x, y] of celdasCamino()) g.camino[idx(def, x, y)] = 1;
  g.ver++;
}

// La zona donde está un jugador: su granja o el piso actual de la mazmorra.
export const zonaDe = (e, j) => (j.lugar === 'mazmorra' && e.mazmorra.nivel ? e.mazmorra.nivel : e.granja);

// Ropa que lleva puesta ahora: la de batalla en la mazmorra, la de campo en la granja.
export const ropaActual = j => (j.lugar === 'mazmorra' ? j.ropaBatalla : j.ropa);

export function nuevoJugador(def, { id, nombre, cuerpo }) {
  const c = def.C.personajes.find(p => p.id === cuerpo) || def.C.personajes[0];
  const j = {
    id, nombre: nombre || c.nombre, cuerpo: c.id, ...posicionInicio(def), lugar: 'granja',
    energia: 100, energiaMax: 100, vida: VIDA_BASE, vidaMax: VIDA_BASE, cdAtaque: 0,
    ropa: 'ropa_granjera', ropaBatalla: 'ropa_aventurera', dinero: def.C.inicio.dinero,
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
  // bosque sobre el acantilado (antes de marcarlo como bloqueado), dejando libre la zona del túnel
  for (let x = 0; x < W; x += 2) if (x < 19 || x > 26) colocar(def, g, 'arbol', x, 1);
  trazarBordes(def, g);
  colocar(def, g, 'casa', CASA.x, CASA.y); colocar(def, g, 'caja_envios', CAJA.x, CAJA.y); colocar(def, g, 'puesto', PUESTO.x, PUESTO.y);
  colocar(def, g, 'entrada_cueva', ENTRADA_CUEVA.x, ENTRADA_CUEVA.y);
  const claro = (x, y) => enClaro(x, y) || g.camino[idx(def, x, y)];
  const arbolCerca = (x, y) => {
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) if (objetoEn(def, g, x + dx, y + dy)?.id === 'arbol') return true;
    return false;
  };
  for (let x = 0; x < W; x += 2) colocar(def, g, 'arbol', x + (x % 4 ? 1 : 0), H - 1);
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
    // expedición en curso (actual, semilla, piso, nivel) y progreso por mazmorra {record, completada}
    mazmorra: { actual: null, semilla: null, piso: 0, nivel: null, progreso: {} },
    jugadores: { [jugador.id]: nuevoJugador(def, jugador) },
  };
}
