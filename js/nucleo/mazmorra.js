// Generación de pisos de mazmorra. Pura y reproducible: (semilla, piso) → siempre el mismo
// piso. Así, en el modo en equipo, cada jugador puede generar el mismo mapa sin recibirlo.
import { mulberry32, hashTexto } from './azar.js';
import { idx, colocar } from './mundo.js';

const entreR = (r, a, b) => a + Math.floor(r() * (b - a + 1));
function elegirPonderado(r, lista) {
  const total = lista.reduce((a, x) => a + (x.peso || 1), 0);
  let p = r() * total;
  return lista.find(x => (p -= x.peso || 1) < 0) || lista.at(-1);
}

export const centroSala = s => ({ x: s.x + Math.floor(s.w / 2), y: s.y + Math.floor(s.h / 2) });

// Busca en la pared de arriba de una sala un tramo recto: 5 columnas con 3 filas de muro encima
// y 2 de suelo debajo. Ahí la muralla se dibuja siempre igual, así que la escalera pintada para
// ese tramo (con inpaint sobre el mismo fondo) calza sin costuras. Devuelve la celda de muro.
export function puntoEnPared(def, z, s) {
  const m = (x, y) => z.muro[idx(def, x, y)];
  const c = s.x + Math.floor(s.w / 2), orden = [0, 1, -1, 2, -2, 3, -3];
  for (const d of orden) {
    const x = c + d;
    if (x - 2 < s.x || x + 2 > s.x + s.w - 1 || s.y < 3) continue;
    let ok = true;
    for (let dx = -2; dx <= 2 && ok; dx++)
      ok = m(x + dx, s.y - 3) && m(x + dx, s.y - 2) && m(x + dx, s.y - 1) && !m(x + dx, s.y) && !m(x + dx, s.y + 1);
    if (ok && !z.objetos.some(o => o.x === x && o.y === s.y - 1)) return { x, y: s.y - 1 };
  }
  return null;
}

export function crearMonstruo(def, zona, id, cx, cy, piso) {
  const m = def.MON[id], escala = 1 + (piso - 1) * 0.15;
  const vida = Math.round(m.vida * (m.jefe ? 1 : escala));
  const mo = { uid: zona.sigUid++, id, x: (cx + 0.5) * def.T, y: (cy + 0.8) * def.T, vida, vidaMax: vida, cd: 1, dir: 1, golpe: 0 };
  zona.monstruos.push(mo);
  return mo;
}

// La escalera de mano al piso siguiente (objeto "escalera_bajar": avanza un piso) o, en el último
// piso, el arco de salida a la granja. Van en la muralla de la sala final; si no hay tramo recto, en el suelo.
function ponerEnSalaFinal(def, z, id) {
  if (z.bajada && colocar(def, z, id, z.bajada.x, z.bajada.y, null, true)) return;
  const { x, y } = z.salaFinal;
  for (let r = 0; r < 4; r++) for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++)
    if (colocar(def, z, id, x + dx, y + dy)) return;
}
export const ponerBajada = (def, z) => ponerEnSalaFinal(def, z, 'escalera_bajar');
export const ponerSalidaFinal = (def, z) => ponerEnSalaFinal(def, z, 'escalera_subir');

export function generarPiso(def, mazmorra, semilla, piso) {
  const { W, H } = def, cfg = def.MAZ[mazmorra], r = mulberry32(hashTexto(`${mazmorra}:${semilla}:${piso}`));
  const z = {
    tipo: 'mazmorra', mazmorra, piso, ver: 0, sigUid: 1, objetos: [], cultivos: {}, monstruos: [],
    agua: Array(W * H).fill(0), suelo: Array(W * H).fill(0), regado: Array(W * H).fill(0),
    muro: Array(W * H).fill(1), enSala: Array(W * H).fill(0),
  };
  const cavar = (x, y) => { if (x > 0 && y > 0 && x < W - 1 && y < H - 1) z.muro[idx(def, x, y)] = 0; };

  // salas rectangulares sin superponerse
  const salas = [], objetivo = Math.min(11, 6 + Math.floor(piso / 2));
  for (let k = 0; k < 300 && salas.length < objetivo; k++) {
    const w = entreR(r, 5, 9), h = entreR(r, 4, 7), x = entreR(r, 2, W - w - 3), y = entreR(r, 3, H - h - 3);
    if (salas.some(s => x < s.x + s.w + 2 && x + w + 2 > s.x && y < s.y + s.h + 2 && y + h + 2 > s.y)) continue;
    salas.push({ x, y, w, h });
  }
  for (const s of salas) for (let y = s.y; y < s.y + s.h; y++) for (let x = s.x; x < s.x + s.w; x++) { cavar(x, y); z.enSala[idx(def, x, y)] = 1; }
  // pasillos en L de 2 de ancho, uniendo cada sala con la siguiente
  for (let i = 1; i < salas.length; i++) {
    const a = centroSala(salas[i - 1]), b = centroSala(salas[i]);
    const hor = (y, x0, x1) => { for (let x = Math.min(x0, x1); x <= Math.max(x0, x1); x++) { cavar(x, y); cavar(x, y + 1); } };
    const ver = (x, y0, y1) => { for (let y = Math.min(y0, y1); y <= Math.max(y0, y1); y++) { cavar(x, y); cavar(x + 1, y); } };
    if (r() < 0.5) { hor(a.y, a.x, b.x); ver(b.x, a.y, b.y); } else { ver(a.x, a.y, b.y); hor(b.y, a.x, b.x); }
  }

  // entrada = la primera sala con un tramo recto de muralla arriba (la salida va en esa pared);
  // sala final = la más lejana a la entrada que también tenga ese tramo (ahí va la escalera de bajada)
  const conPared = salas.filter(s => puntoEnPared(def, z, s));
  const inicio = conPared[0] || salas[0], ci = centroSala(inicio);
  const lejos = s => { const c = centroSala(s); return Math.hypot(c.x - ci.x, c.y - ci.y); };
  const candidatas = (conPared.length > 1 ? conPared : salas).filter(s => s !== inicio);
  const final = candidatas.reduce((a, s) => (lejos(s) > lejos(a) ? s : a), candidatas[0]);
  z.salaFinal = centroSala(final);
  // El arco de salida a la granja solo está en el piso 1 (y aparece en el último al vencer al jefe).
  // En los demás pisos se llega subiendo por la escalera de mano, y se aparece al pie de la pared.
  const salida = puntoEnPared(def, z, inicio);
  z.entrada = salida ? { x: salida.x, y: salida.y + 1 } : { x: ci.x, y: ci.y + 1 };
  if (piso === 1) {
    if (salida) colocar(def, z, 'escalera_subir', salida.x, salida.y, null, true);
    else colocar(def, z, 'escalera_subir', ci.x, ci.y - 1);
  }
  z.bajada = puntoEnPared(def, z, final);          // dónde aparece la escalera de bajada (también tras vencer al jefe)

  const jefe = cfg.jefes[String(piso)];
  if (jefe) crearMonstruo(def, z, jefe, z.salaFinal.x, z.salaFinal.y, piso);
  else if (piso < cfg.pisos) ponerBajada(def, z);

  const celdaEnSala = (s, margen = 1) => ({ x: entreR(r, s.x + margen, s.x + s.w - 1 - margen), y: entreR(r, s.y + margen, s.y + s.h - 1 - margen) });
  const otras = salas.filter(s => s !== inicio);

  // una antorcha por sala, rocas con minerales según el piso, a veces un cofre
  for (const s of salas) { const c = celdaEnSala(s, 0); colocar(def, z, 'antorcha', c.x, c.y); }
  const rocas = cfg.rocas_por_piso.filter(x => piso >= x.desde && piso <= x.hasta);
  for (let k = 0, n = 0; k < 200 && n < 10 + piso; k++) {
    const s = otras[entreR(r, 0, otras.length - 1)], c = celdaEnSala(s);
    if (s === final && jefe) continue;
    if (colocar(def, z, elegirPonderado(r, rocas).id, c.x, c.y)) n++;
  }
  if (r() < 0.6) for (let k = 0; k < 30; k++) { const s = otras[entreR(r, 0, otras.length - 1)], c = celdaEnSala(s); if (s !== final && colocar(def, z, 'cofre_mazmorra', c.x, c.y)) break; }

  // monstruos (nunca en la sala de entrada)
  const tipos = cfg.monstruos_por_piso.filter(x => piso >= x.desde && piso <= x.hasta);
  const cuantos = Math.min(12, 3 + piso);
  for (let k = 0, n = 0; k < 200 && n < cuantos; k++) {
    const s = otras[entreR(r, 0, otras.length - 1)], c = celdaEnSala(s);
    if (z.objetos.some(o => o.x === c.x && o.y === c.y)) continue;
    crearMonstruo(def, z, tipos[entreR(r, 0, tipos.length - 1)].id, c.x, c.y, piso); n++;
  }
  return z;
}
