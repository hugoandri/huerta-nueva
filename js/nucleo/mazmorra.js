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

export function crearMonstruo(def, zona, id, cx, cy, piso) {
  const m = def.MON[id], escala = 1 + (piso - 1) * 0.15;
  const vida = Math.round(m.vida * (m.jefe ? 1 : escala));
  const mo = { uid: zona.sigUid++, id, x: (cx + 0.5) * def.T, y: (cy + 0.8) * def.T, vida, vidaMax: vida, cd: 1, dir: 1, golpe: 0 };
  zona.monstruos.push(mo);
  return mo;
}

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
    const w = entreR(r, 5, 9), h = entreR(r, 4, 7), x = entreR(r, 2, W - w - 3), y = entreR(r, 2, H - h - 3);
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

  // entrada = primera sala; sala final = la más lejana a la entrada
  const inicio = salas[0], ci = centroSala(inicio);
  let final = salas[1];
  for (const s of salas.slice(1)) { const c = centroSala(s), cf = centroSala(final); if (Math.hypot(c.x - ci.x, c.y - ci.y) > Math.hypot(cf.x - ci.x, cf.y - ci.y)) final = s; }
  z.entrada = { x: ci.x, y: ci.y + 1 };
  z.salaFinal = centroSala(final);
  colocar(def, z, 'escalera_subir', ci.x, ci.y - 1);

  const jefe = cfg.jefes[String(piso)];
  if (jefe) crearMonstruo(def, z, jefe, z.salaFinal.x, z.salaFinal.y, piso);
  else if (piso < cfg.pisos) colocar(def, z, 'escalera_bajar', z.salaFinal.x, z.salaFinal.y);

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
