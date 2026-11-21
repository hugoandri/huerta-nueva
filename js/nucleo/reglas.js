// Reglas del juego. Toda modificación del estado pasa por aplicar(estado, def, accion).
// Offline la aplica la sesión local; online la aplicará el servidor (o el anfitrión de la
// mazmorra) con este mismo código. Las reglas validan todo lo que llega (distancias,
// materiales, dinero, vida), así que una acción falsa no rompe el estado.
//
// Devuelven eventos para la interfaz: avisos, textos flotantes, paneles, golpes.
// Los eventos llevan "jugador" cuando son solo para un jugador.
import { azar } from './azar.js';
import { idx, dentro, objetoEn, objetoPorUid, libre, colocar, quitar, posicionInicio, frenteACueva, zonaDe, ropaActual, enClaro, INICIO_DIA, VIDA_BASE } from './mundo.js';
import { agregar, cuenta, sacar, gastarMano, espacioPara, esHerramienta } from './inventario.js';
import { generarPiso } from './mazmorra.js';
import { tirarBotin, repartirBotin } from './botin.js';

export const FIN_DIA = 26 * 60;              // 2:00
export const MIN_POR_SEG = 10 / 7;           // minutos de juego por segundo real
export const VELOCIDAD = 112;                // píxeles por segundo
export const DT_MAX = 0.1;                   // un "mover" o "tiempo" no puede saltar más que esto
const VEC = { south: [0, 1], north: [0, -1], west: [-1, 0], east: [1, 0] };

export const celdaJugador = (def, j) => [Math.floor(j.x / def.T), Math.floor((j.y - 6) / def.T)];
export function celdaFrente(def, j) { const [cx, cy] = celdaJugador(def, j), [dx, dy] = VEC[j.dir]; return [cx + dx, cy + dy]; }
export const tiempoAbs = e => e.tiempo.dia * 1440 + e.tiempo.min;
// Efecto de la ropa que lleva puesta ahora (la de batalla en la mazmorra, la de campo en la granja).
export const efecto = (def, j) => def.ITEMS[ropaActual(j)]?.efecto || {};
function recalcularVida(def, j) {
  j.vidaMax = VIDA_BASE + (efecto(def, j).vida || 0);
  j.vida = Math.min(j.vida, j.vidaMax);
}
export const progresoDe = (e, id) => (e.mazmorra.progreso[id] ||= { record: 0, completada: 0 });
export const maquinaLista = (e, o) => !!(o.datos.salida && tiempoAbs(e) >= o.datos.listoEn);
export const jugadoresEn = (e, zona) => Object.values(e.jugadores).filter(j => zonaDe(e, j) === zona);

export function solido(def, g, px, py) {
  const x = Math.floor(px / def.T), y = Math.floor(py / def.T);
  if (!dentro(def, x, y)) return true;
  const i = idx(def, x, y);
  if (g.agua[i] || g.muro?.[i]) return true;
  const o = objetoEn(def, g, x, y);
  return !!(o && def.OBJ[o.id].solido);
}
export const choca = (def, g, x, y, r = 8) =>
  solido(def, g, x - r, y - 6) || solido(def, g, x + r, y - 6) || solido(def, g, x - r, y + 2) || solido(def, g, x + r, y + 2);

// ---------------------------------------------------------------- utilidades de eventos
const aviso = (ev, j, txt, malo = false) => ev.push({ tipo: 'aviso', jugador: j.id, txt, malo });
const flotar = (ev, x, y, txt, color) => ev.push({ tipo: 'flotar', x, y, txt, color });
const abrir = (ev, j, panel, extra = {}) => ev.push({ tipo: 'abrir', jugador: j.id, panel, ...extra });

function darItem(def, ev, j, id, n, x, y, color) {
  if (n <= 0) return 0;
  const sobra = agregar(def, j, id, n);
  if (n - sobra > 0) flotar(ev, x, y, `+${n - sobra} ${def.ITEMS[id].nombre}`, color);
  if (sobra) aviso(ev, j, 'La mochila está llena', true);
  return sobra;
}
function gastarEnergia(ev, j, e) {
  if (j.energia < e) { aviso(ev, j, 'Estás agotada. Come algo o duerme.', true); return false; }
  j.energia = Math.max(0, j.energia - e);
  return true;
}
const rngDe = e => () => azar(e);

// ---------------------------------------------------------------- acciones
const ACCIONES = {
  // Movimiento: el cliente manda la dirección y el tiempo; la regla calcula la posición y choca.
  mover(e, def, a, j) {
    let dx = Math.sign(a.dx || 0), dy = Math.sign(a.dy || 0);
    const dt = Math.min(DT_MAX, Math.max(0, +a.dt || 0));
    if (!dx && !dy) return;
    j.dir = Math.abs(dx) >= Math.abs(dy) && dx ? (dx > 0 ? 'east' : 'west') : (dy > 0 ? 'south' : 'north');
    if (dx && dy) { dx *= Math.SQRT1_2; dy *= Math.SQRT1_2; }
    const v = VELOCIDAD * dt, g = zonaDe(e, j);
    if (!choca(def, g, j.x + dx * v, j.y)) j.x += dx * v;
    if (!choca(def, g, j.x, j.y + dy * v)) j.y += dy * v;
  },

  mirar(e, def, a, j) { if (VEC[a.dir]) j.dir = a.dir; },

  seleccionar(e, def, a, j) { const i = a.i | 0; if (i >= 0 && i < 12) j.sel = i; },

  moverInv(e, def, a, j) {
    const { de, a: hacia } = a;
    if (!(de in j.inv) || !(hacia in j.inv) || de === hacia) return;
    [j.inv[de], j.inv[hacia]] = [j.inv[hacia], j.inv[de]];
  },

  tiempo(e, def, a, j, ev) {
    const dt = Math.min(DT_MAX, Math.max(0, +a.dt || 0));
    e.tiempo.min += dt * MIN_POR_SEG;
    for (const p of Object.values(e.jugadores)) p.cdAtaque = Math.max(0, (p.cdAtaque || 0) - dt);
    if (e.mazmorra.nivel) actualizarMazmorra(e, def, dt, ev);
    if (e.tiempo.min >= FIN_DIA) nuevoDia(e, def, ev, true);
  },

  dormir(e, def, a, j, ev) { nuevoDia(e, def, ev, false); },

  // Comer un cultivo o conserva (energía) o beber una poción (vida).
  comer(e, def, a, j, ev) {
    const s = j.inv[j.sel], it = s && def.ITEMS[s.id];
    if (it?.tipo === 'pocion') {
      if (j.vida >= j.vidaMax) return aviso(ev, j, 'Tienes la vida al máximo');
      j.vida = Math.min(j.vidaMax, j.vida + it.cura); gastarMano(j);
      return flotar(ev, j.x, j.y - 40, `+${it.cura / 2} ♥`, '#e2674f');
    }
    if (!it || (it.tipo !== 'cultivo' && it.tipo !== 'artesanal')) return aviso(ev, j, 'Ten en la mano un cultivo, una conserva o una poción', true);
    if (j.energia >= j.energiaMax) return aviso(ev, j, 'Tienes la energía al máximo');
    const ganancia = Math.max(10, Math.round(it.venta / 4));
    j.energia = Math.min(j.energiaMax, j.energia + ganancia);
    gastarMano(j);
    flotar(ev, j.x, j.y - 40, `+${ganancia} energía`, '#79c267');
  },

  // Equipar la ropa de la casilla i en su puesto: de campo (granja) o de batalla (mazmorra).
  // La que estaba en ese puesto vuelve a la misma casilla.
  vestir(e, def, a, j, ev) {
    const i = a.i ?? j.sel, s = j.inv[i], it = s && def.ITEMS[s.id];
    if (!it || it.tipo !== 'ropa') return;
    const puesto = it.uso === 'batalla' ? 'ropaBatalla' : 'ropa';
    const anterior = j[puesto];
    j[puesto] = s.id;
    j.inv[i] = anterior ? { id: anterior, n: 1 } : null;
    recalcularVida(def, j);
    aviso(ev, j, `${it.uso === 'batalla' ? 'Ropa de batalla' : 'Ropa de granja'}: ${it.nombre}`);
    ev.push({ tipo: 'ropa', jugador: j.id });
  },

  fabricar(e, def, a, j, ev) {
    const r = def.C.recetas[a.receta];
    if (!r) return;
    if (!Object.entries(r.ingredientes).every(([id, n]) => cuenta(j, id) >= n)) return aviso(ev, j, 'Te faltan materiales', true);
    if (espacioPara(def, j, r.resultado) < r.cantidad) return aviso(ev, j, 'La mochila está llena', true);
    for (const [id, n] of Object.entries(r.ingredientes)) sacar(j, id, n);
    agregar(def, j, r.resultado, r.cantidad);
    aviso(ev, j, `Fabricaste: ${def.ITEMS[r.resultado].nombre}${r.cantidad > 1 ? ' ×' + r.cantidad : ''}`);
  },

  comprar(e, def, a, j, ev) {
    const it = def.ITEMS[a.item], n = Math.max(1, Math.min(99, a.n | 0));
    if (!it || !def.C.tienda.includes(a.item)) return;
    const caben = Math.min(n, espacioPara(def, j, a.item));
    if (!caben) return aviso(ev, j, 'La mochila está llena', true);
    if (j.dinero < it.compra * caben) return aviso(ev, j, 'No te alcanza el dinero', true);
    j.dinero -= it.compra * caben;
    agregar(def, j, a.item, caben);
    aviso(ev, j, `Compraste ${caben} ${it.nombre.toLowerCase()}`);
  },

  cofreSacar(e, def, a, j, ev) {
    const o = objetoPorUid(def, zonaDe(e, j), a.uid);
    if (!o || def.OBJ[o.id].accion !== 'cofre' || !cerca(def, j, o)) return;
    const items = o.datos.items || [], s = items[a.i];
    if (!s) return;
    s.n = agregar(def, j, s.id, s.n);
    if (!s.n) items[a.i] = null; else aviso(ev, j, 'La mochila está llena', true);
  },

  cofreGuardar(e, def, a, j, ev) {
    const o = objetoPorUid(def, zonaDe(e, j), a.uid);
    if (!o || def.OBJ[o.id].accion !== 'cofre' || !cerca(def, j, o)) return;
    const s = j.inv[a.i];
    if (!s) return;
    const items = (o.datos.items ||= Array(12).fill(null));
    let k = esHerramienta(def, s.id) ? -1 : items.findIndex(x => x && x.id === s.id);
    if (k < 0) k = items.findIndex(x => !x);
    if (k < 0) return aviso(ev, j, 'El cofre está lleno', true);
    if (items[k]) items[k].n += s.n; else items[k] = { id: s.id, n: s.n };
    j.inv[a.i] = null;
  },

  // Entrar desde la cueva a una mazmorra, en el piso 1 o en un punto de control alcanzado.
  // Si el grupo ya está en una expedición, el jugador se une a ella.
  entrarMazmorra(e, def, a, j, ev) {
    const cueva = e.granja.objetos.find(o => o.id === 'entrada_cueva');
    if (j.lugar !== 'granja' || !cueva || !cerca(def, j, cueva)) return;
    const m = e.mazmorra;
    if (!m.nivel) {
      const cfg = def.MAZ[a.mazmorra];
      if (!cfg) return;
      if (!mazmorraAbierta(e, def, cfg)) return aviso(ev, j, 'Esa mazmorra todavía está cerrada', true);
      const piso = a.piso | 0;
      if (!pisosDisponibles(e, def, cfg.id).includes(piso)) return aviso(ev, j, 'Todavía no llegaste a ese piso', true);
      m.actual = cfg.id;
      m.semilla = Math.floor(azar(e) * 2 ** 32);     // nueva expedición: semilla compartida por el grupo
      irAPiso(e, def, piso, ev);
    }
    j.lugar = 'mazmorra';
    recalcularVida(def, j);
    Object.assign(j, entradaDe(def, m.nivel));
    aviso(ev, j, `${def.MAZ[m.actual].nombre} · piso ${m.piso} · llevas: ${def.ITEMS[j.ropaBatalla]?.nombre}`);
    ev.push({ tipo: 'ropa', jugador: j.id });
  },

  usar(e, def, a, j, ev) { usar(e, def, j, a.x | 0, a.y | 0, ev); },
};

export function mazmorraAbierta(e, def, cfg) {
  const r = cfg.requiere;
  return !r || progresoDe(e, r.mazmorra).record >= r.piso;
}
export function pisosDisponibles(e, def, id) {
  const cfg = def.MAZ[id], record = progresoDe(e, id).record;
  return cfg.puntos_de_control.filter(p => p === 1 || record >= p - 1);
}
// Lo que muestra el panel de la cueva.
export function infoMazmorras(e, def) {
  return def.C.mazmorras.map(cfg => ({
    id: cfg.id, nombre: cfg.nombre, descripcion: cfg.descripcion, pisos: cfg.pisos,
    abierta: mazmorraAbierta(e, def, cfg),
    requiere: cfg.requiere ? `Llega al piso ${cfg.requiere.piso} de ${def.MAZ[cfg.requiere.mazmorra].nombre}` : null,
    disponibles: pisosDisponibles(e, def, cfg.id), ...progresoDe(e, cfg.id),
  }));
}
const entradaDe = (def, nivel) => ({ x: (nivel.entrada.x + 0.5) * def.T, y: (nivel.entrada.y + 0.6) * def.T, dir: 'south' });

function irAPiso(e, def, piso, ev, bajada = false) {
  const m = e.mazmorra, cfg = def.MAZ[m.actual], prog = progresoDe(e, m.actual);
  m.piso = piso;
  prog.record = Math.max(prog.record, piso);
  m.nivel = generarPiso(def, m.actual, m.semilla, piso);
  for (const p of Object.values(e.jugadores)) if (p.lugar === 'mazmorra') Object.assign(p, entradaDe(def, m.nivel));
  const jefe = cfg.jefes[String(piso)];
  ev.push({ tipo: 'piso', piso, bajada, jefe: jefe ? def.MON[jefe].nombre : null });
}

function terminarExpedicion(e) { Object.assign(e.mazmorra, { actual: null, semilla: null, piso: 0, nivel: null }); }
function salirDeMazmorra(e, def, j) {
  j.lugar = 'granja';
  recalcularVida(def, j);
  Object.assign(j, frenteACueva(def));
  if (!Object.values(e.jugadores).some(p => p.lugar === 'mazmorra')) terminarExpedicion(e);
}

// El jugador debe estar junto al objeto (anti-trampas: valida aunque la acción venga de la red).
function cerca(def, j, o) {
  const [px, py] = celdaJugador(def, j), d = def.OBJ[o.id];
  const w = d.ancho || 1, h = d.alto || 1;
  const dx = px < o.x ? o.x - px : px >= o.x + w ? px - (o.x + w - 1) : 0;
  const dy = py < o.y ? o.y - py : py >= o.y + h ? py - (o.y + h - 1) : 0;
  return Math.max(dx, dy) <= 1;
}

// ---------------------------------------------------------------- combate
function atacar(e, def, j, zona, x, y, ev) {
  const cx = (x + 0.5) * def.T, cy = (y + 0.6) * def.T;
  const blancos = zona.monstruos.filter(m => Math.hypot(m.x - cx, m.y - cy) <= (def.MON[m.id].jefe ? 44 : 26));
  if (!blancos.length) return false;
  if (j.cdAtaque > 0) return true;
  const s = j.inv[j.sel], it = s && def.ITEMS[s.id];
  const dano = it?.tipo === 'arma' ? it.dano : 1;
  if (it?.tipo !== 'arma' && !gastarEnergia(ev, j, 1)) return true;
  j.cdAtaque = 0.35;
  ev.push({ tipo: 'golpe', x: cx, y: cy });
  for (const m of blancos) {
    m.vida -= dano; m.golpe = 0.15;
    const d = Math.hypot(m.x - j.x, m.y - j.y) || 1, kx = m.x + (m.x - j.x) / d * 12, ky = m.y + (m.y - j.y) / d * 12;
    if (!def.MON[m.id].jefe && !choca(def, zona, kx, ky, 6)) { m.x = kx; m.y = ky; }
    flotar(ev, m.x, m.y - 30, `-${dano}`, '#f3eedb');
    if (m.vida <= 0) morirMonstruo(e, def, j, zona, m, ev);
  }
  return true;
}

function morirMonstruo(e, def, j, zona, m, ev) {
  const M = def.MON[m.id];
  zona.monstruos = zona.monstruos.filter(x => x !== m);
  for (const [item, [p, mn, mx]] of Object.entries(M.suelta || {}))
    if (azar(e) < p) darItem(def, ev, j, item, mn + Math.floor(azar(e) * (mx - mn + 1)), m.x, m.y - 20, '#f2c14e');
  if (!M.jefe) return;
  const sf = zona.salaFinal, piso = zona.piso, cfg = def.MAZ[zona.mazmorra];
  if (piso < cfg.pisos) {
    colocarCerca(def, zona, 'escalera_bajar', sf.x, sf.y);
    for (const p of jugadoresEn(e, zona)) aviso(ev, p, `¡Venciste a ${M.nombre}! Se abrió la escalera.`);
    return;
  }
  // último jefe: cada jugador presente recibe su propio botín (el servidor lo hará online)
  const presentes = jugadoresEn(e, zona);
  const reparto = repartirBotin(def.C.botines[cfg.botin_final], presentes.map(p => p.id), e.mazmorra.semilla);
  for (const p of presentes) {
    const items = reparto[p.id];
    for (const { item, n } of items) agregar(def, p, item, n);
    ev.push({ tipo: 'botin', jugador: p.id, titulo: `¡${cfg.nombre} completada!`, items });
  }
  progresoDe(e, cfg.id).completada++;
  colocarCerca(def, zona, 'escalera_subir', sf.x, sf.y);
}

function colocarCerca(def, zona, id, x, y) {
  for (let r = 0; r < 4; r++) for (let dy = -r; dy <= r; dy++) for (let dx = -r; dx <= r; dx++)
    if (colocar(def, zona, id, x + dx, y + dy)) return;
}

// Monstruos: persiguen al jugador más cercano que ven y golpean al estar al lado.
function actualizarMazmorra(e, def, dt, ev) {
  const zona = e.mazmorra.nivel, jugadores = jugadoresEn(e, zona);
  if (!jugadores.length) return;
  for (const m of zona.monstruos) {
    const M = def.MON[m.id];
    m.cd = Math.max(0, m.cd - dt); m.golpe = Math.max(0, m.golpe - dt);
    let obj = null, dist = Infinity;
    for (const p of jugadores) { const d = Math.hypot(p.x - m.x, p.y - m.y); if (d < dist) { dist = d; obj = p; } }
    if (!obj || dist > M.vision * def.T) continue;
    const alcance = M.jefe ? 30 : 16;
    if (dist > alcance) {
      const v = M.velocidad * dt, ux = (obj.x - m.x) / dist, uy = (obj.y - m.y) / dist, r = M.jefe ? 14 : 6;
      if (!choca(def, zona, m.x + ux * v, m.y, r)) m.x += ux * v;
      if (!choca(def, zona, m.x, m.y + uy * v, r)) m.y += uy * v;
      if (Math.abs(ux) > 0.1) m.dir = Math.sign(ux);
    } else if (m.cd <= 0) {
      m.cd = M.jefe ? 1.4 : 1.1;
      const dano = Math.max(0.5, Math.round(M.dano * (1 - (efecto(def, obj).defensa || 0)) * 2) / 2);
      obj.vida -= dano;
      ev.push({ tipo: 'dano', jugador: obj.id, n: dano });
      flotar(ev, obj.x, obj.y - 44, `-${dano / 2} ♥`, '#e2674f');
      if (obj.vida <= 0) derrota(e, def, obj, ev);
    }
  }
}

function derrota(e, def, j, ev) {
  const multa = Math.min(300, Math.floor(j.dinero * 0.1));
  j.dinero -= multa;
  salirDeMazmorra(e, def, j);
  j.vida = Math.ceil(j.vidaMax / 2);
  j.energia = Math.min(j.energia, Math.round(j.energiaMax / 2));
  ev.push({ tipo: 'derrota', jugador: j.id, multa });
}

// ---------------------------------------------------------------- usar la casilla de al lado
function usar(e, def, j, x, y, ev) {
  const g = zonaDe(e, j);
  if (!dentro(def, x, y)) return;
  const [px, py] = celdaJugador(def, j);
  if (Math.max(Math.abs(x - px), Math.abs(y - py)) > 1) return;      // solo la casilla de al lado
  const s = j.inv[j.sel], it = s && def.ITEMS[s.id], i = idx(def, x, y);
  const cx = (x + 0.5) * def.T, cy = y * def.T;
  const esHachaOPico = s && (s.id === 'hacha' || s.id === 'pico');

  if (g.monstruos && atacar(e, def, j, g, x, y, ev)) return;

  const o = objetoEn(def, g, x, y);
  // con ropa en la mano: ponérsela, salvo que enfrente haya algo con qué interactuar
  if (it?.tipo === 'ropa' && !(o && def.OBJ[o.id].accion)) return ACCIONES.vestir(e, def, { i: j.sel }, j, ev);
  if (o) {
    const d = def.OBJ[o.id];
    if (d.accion === 'dormir') return abrir(ev, j, 'dormir');
    if (d.accion === 'tienda') return abrir(ev, j, 'tienda');
    if (d.accion === 'mazmorra') return abrir(ev, j, 'mazmorra', { mazmorras: infoMazmorras(e, def), enCurso: e.mazmorra.nivel ? { id: e.mazmorra.actual, piso: e.mazmorra.piso } : null });
    if (d.accion === 'salir') { salirDeMazmorra(e, def, j); return aviso(ev, j, 'Volviste a la granja'); }
    if (d.accion === 'bajar') {
      const siguiente = e.mazmorra.piso + 1;
      if (siguiente > def.MAZ[e.mazmorra.actual].pisos) return;
      return irAPiso(e, def, siguiente, ev, true);
    }
    if (d.accion === 'botin') {
      quitar(g, o);
      for (const { item, n } of tirarBotin(def.C.botines[d.botin], rngDe(e))) darItem(def, ev, j, item, n, cx, cy, '#f2c14e');
      return;
    }
    if (d.accion === 'cofre' && !esHachaOPico) return abrir(ev, j, 'cofre', { uid: o.uid });
    if (d.accion === 'vender') {
      if (!it || it.tipo === 'herramienta' || !it.venta) return aviso(ev, j, 'Ten en la mano lo que quieras vender', true);
      j.envios.push({ id: s.id, n: s.n });
      aviso(ev, j, `Enviaste ${s.n} ${it.nombre.toLowerCase()} · cobras al dormir`);
      j.inv[j.sel] = null;
      return;
    }
    if (d.accion === 'maquina') {
      const m = o.datos;
      if (maquinaLista(e, o)) { if (!darItem(def, ev, j, m.salida, 1, cx, cy)) o.datos = {}; return; }
      if (m.salida) { const falta = Math.ceil((m.listoEn - tiempoAbs(e)) / 60); return aviso(ev, j, `Listo en ${falta} h de juego`); }
      const receta = s && d.procesa?.find(p => p.entrada === s.id);
      if (receta) {
        if (cuenta(j, s.id) < receta.n) return aviso(ev, j, `Necesitas ${receta.n} ${it.nombre.toLowerCase()}`, true);
        sacar(j, s.id, receta.n);
        o.datos = { entrada: s.id, salida: receta.salida, listoEn: tiempoAbs(e) + receta.minutos };
        return aviso(ev, j, `Preparando: ${def.ITEMS[receta.salida].nombre}`);
      }
      if (!esHachaOPico) return aviso(ev, j, `Acepta: ${d.procesa.map(p => `${p.n} ${def.ITEMS[p.entrada].nombre.toLowerCase()}`).join(', ')}`, true);
    }
    if (d.herramienta && it && it.tipo === 'herramienta' && (d.herramienta === s.id || d.herramienta === 'cualquiera')) {
      const coste = s.id === 'pico' ? Math.max(1, Math.round(2 * (efecto(def, j).energia_pico || 1))) : 2;
      if (!gastarEnergia(ev, j, coste)) return;
      o.golpes--;
      ev.push({ tipo: 'sacudir', uid: o.uid });
      if (o.golpes <= 0) {
        quitar(g, o);
        for (const [id, [mn, mx]] of Object.entries(d.suelta || {})) darItem(def, ev, j, id, mn + Math.floor(azar(e) * (mx - mn + 1)), cx, cy);
      }
      return;
    }
    if (d.recoger && esHachaOPico) {
      if (o.datos.items?.some(Boolean)) return aviso(ev, j, 'Vacía el cofre antes de recogerlo', true);
      if (o.datos.salida) return aviso(ev, j, 'Espera a que termine', true);
      if (espacioPara(def, j, o.id) < 1) return aviso(ev, j, 'La mochila está llena', true);
      quitar(g, o); darItem(def, ev, j, o.id, 1, cx, cy);
      return;
    }
    if (d.herramienta) return aviso(ev, j, `Necesitas ${d.herramienta === 'cualquiera' ? 'una herramienta' : 'el ' + def.ITEMS[d.herramienta].nombre.toLowerCase()}`, true);
    return;
  }

  const cult = g.cultivos[i];
  if (cult && cult.dias >= def.CULT[cult.id].dias) {
    const prod = def.CULT[cult.id].producto;
    if (espacioPara(def, j, prod) < 1) return aviso(ev, j, 'La mochila está llena', true);
    delete g.cultivos[i];
    darItem(def, ev, j, prod, 1, cx, cy, '#f2c14e');
    return;
  }
  if (!it) return;
  const enGranja = g === e.granja;
  const costeCampo = Math.max(1, Math.round(2 * (efecto(def, j).energia_campo || 1)));

  if (s.id === 'azada' && enGranja) {
    if (g.agua[i] || g.suelo[i]) return;
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++)
      if (dentro(def, x + dx, y + dy) && g.agua[idx(def, x + dx, y + dy)]) return aviso(ev, j, 'Muy cerca del agua para arar', true);
    if (!gastarEnergia(ev, j, costeCampo)) return;
    g.suelo[i] = 1;
    if (e.tiempo.lluvia) g.regado[i] = 1;
    return;
  }
  if (s.id === 'regadera' && enGranja) {
    if (g.agua[i]) { j.aguaRegadera = def.ITEMS.regadera.capacidad; flotar(ev, cx, cy, 'Regadera llena', '#5aa9e6'); return; }
    if (!g.suelo[i] || g.regado[i]) return;
    if (j.aguaRegadera <= 0) return aviso(ev, j, 'La regadera está vacía: úsala frente al estanque', true);
    if (!gastarEnergia(ev, j, Math.max(1, costeCampo / 2))) return;
    j.aguaRegadera--; g.regado[i] = 1;
    return;
  }
  if (it.tipo === 'semilla' && enGranja) {
    if (!g.suelo[i]) return aviso(ev, j, 'Primero ara la tierra con la azada', true);
    if (cult) return;
    g.cultivos[i] = { id: it.cultivo, dias: 0 };
    gastarMano(j);
    return;
  }
  if (it.tipo === 'colocable') {
    if (px === x && py === y) return;
    if (jugadoresEn(e, g).some(o2 => { const [ox, oy] = celdaJugador(def, o2); return ox === x && oy === y; })) return;
    if (!enGranja && it.objeto !== 'antorcha') return aviso(ev, j, 'En la mazmorra solo puedes colocar antorchas', true);
    if (!libre(def, g, x, y)) return aviso(ev, j, 'No cabe ahí', true);
    colocar(def, g, it.objeto, x, y);
    gastarMano(j);
  }
}

// ---------------------------------------------------------------- el paso de los días
function nuevoDia(e, def, ev, desmayo) {
  const g = e.granja, { W, H } = def;
  const notas = [];

  const cobros = {};
  for (const j of Object.values(e.jugadores)) {
    const ventas = {};
    for (const s of j.envios) ventas[s.id] = (ventas[s.id] || 0) + s.n;
    const total = Object.entries(ventas).reduce((a, [id, n]) => a + def.ITEMS[id].venta * n, 0);
    j.dinero += total; j.envios = [];
    let multa = 0;
    if (desmayo) { multa = Math.min(500, Math.floor(j.dinero * 0.1)); j.dinero -= multa; }
    cobros[j.id] = { ventas, total, multa };
    j.energia = desmayo ? Math.round(j.energiaMax * 0.6) : j.energiaMax;
    j.lugar = 'granja';
    recalcularVida(def, j);
    j.vida = j.vidaMax;
    Object.assign(j, posicionInicio(def));
  }
  terminarExpedicion(e);     // dormir (o desmayarse) termina la expedición

  for (const k of Object.keys(g.cultivos)) if (g.regado[k] && g.cultivos[k].dias < def.CULT[g.cultivos[k].id].dias) g.cultivos[k].dias++;
  const protegido = k => { const x = k % W, y = Math.floor(k / W); return g.objetos.some(o => def.OBJ[o.id].protege && Math.hypot(o.x - x, o.y - y) <= def.OBJ[o.id].protege); };
  const sinProteger = Object.keys(g.cultivos).filter(k => !protegido(+k));
  if (sinProteger.length >= 8 && azar(e) < 0.6) {
    const k = sinProteger[Math.floor(azar(e) * sinProteger.length)];
    notas.push(`Un cuervo se comió una planta de ${def.ITEMS[def.CULT[g.cultivos[k].id].producto].nombre.toLowerCase()}. Un espantapájaros lo evita.`);
    delete g.cultivos[k];
  }
  for (let i = 0; i < W * H; i++) { if (g.suelo[i] && !g.cultivos[i] && !g.regado[i] && azar(e) < 0.12) g.suelo[i] = 0; g.regado[i] = 0; }

  e.tiempo.dia++;
  e.tiempo.min = INICIO_DIA;
  e.tiempo.lluvia = azar(e) < 0.2;
  if (e.tiempo.lluvia) { for (let i = 0; i < W * H; i++) if (g.suelo[i]) g.regado[i] = 1; notas.push('Llueve: la tierra arada amaneció regada.'); }
  for (const o of g.objetos) {
    const d = def.OBJ[o.id];
    if (d.riega) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const x = o.x + dx, y = o.y + dy; if (dentro(def, x, y) && g.suelo[idx(def, x, y)]) g.regado[idx(def, x, y)] = 1; }
    if (d.accion === 'maquina' && maquinaLista(e, o)) notas.push(`Tienes lista una ${def.ITEMS[o.datos.salida].nombre.toLowerCase()}.`);
  }
  for (let k = 0, n = 0; k < 200 && n < 4; k++) {
    const x = 2 + Math.floor(azar(e) * (W - 4)), y = 3 + Math.floor(azar(e) * (H - 5));
    if (!enClaro(x, y) && colocar(def, g, n === 0 ? 'roca' : 'maleza', x, y)) n++;
  }

  for (const j of Object.values(e.jugadores)) {
    const c = cobros[j.id];
    ev.push({
      tipo: 'resumen', jugador: j.id, dia: e.tiempo.dia, ventas: c.ventas, total: c.total,
      notas: [...(desmayo ? [`Te desmayaste a las 2:00 y alguien te llevó a casa. Pagaste ${c.multa} de atención.`] : []), ...notas],
    });
  }
}

// ---------------------------------------------------------------- punto de entrada
export function aplicar(estado, def, accion) {
  const ev = [];
  const h = ACCIONES[accion?.tipo];
  if (!h) return [{ tipo: 'error', txt: `Acción desconocida: ${accion?.tipo}` }];
  const j = accion.jugador != null ? estado.jugadores[accion.jugador] : null;
  if (accion.jugador != null && !j) return [{ tipo: 'error', txt: 'Jugador desconocido' }];
  if (!j && h !== ACCIONES.tiempo && h !== ACCIONES.dormir) return [{ tipo: 'error', txt: 'Falta el jugador' }];
  h(estado, def, accion, j, ev);
  return ev;
}

export const TIPOS_DE_ACCION = Object.keys(ACCIONES);
