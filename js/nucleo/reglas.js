// Reglas del juego. Toda modificación del estado pasa por aplicar(estado, def, accion).
// Offline la aplica la sesión local; online la aplicará el servidor (o el anfitrión) con este
// mismo código, después de recibir la acción por la red. Las reglas validan todo lo que
// llega (distancias, materiales, dinero), así que una acción falsa no rompe el estado.
//
// Devuelven una lista de eventos para la interfaz: avisos, textos flotantes, paneles a abrir.
// Los eventos llevan "jugador" cuando son solo para un jugador.
import { azar } from './azar.js';
import { idx, dentro, objetoEn, objetoPorUid, libre, colocar, quitar, posicionInicio, INICIO_DIA } from './mundo.js';
import { agregar, cuenta, sacar, gastarMano, espacioPara, esHerramienta } from './inventario.js';

export const FIN_DIA = 26 * 60;              // 2:00
export const MIN_POR_SEG = 10 / 7;           // minutos de juego por segundo real
export const VELOCIDAD = 112;                // píxeles por segundo
export const DT_MAX = 0.1;                   // un "mover" no puede saltar más que esto
const VEC = { south: [0, 1], north: [0, -1], west: [-1, 0], east: [1, 0] };

export const celdaJugador = (def, j) => [Math.floor(j.x / def.T), Math.floor((j.y - 6) / def.T)];
export function celdaFrente(def, j) { const [cx, cy] = celdaJugador(def, j), [dx, dy] = VEC[j.dir]; return [cx + dx, cy + dy]; }

export function solido(def, g, px, py) {
  const x = Math.floor(px / def.T), y = Math.floor(py / def.T);
  if (!dentro(def, x, y) || g.agua[idx(def, x, y)]) return true;
  const o = objetoEn(def, g, x, y);
  return !!(o && def.OBJ[o.id].solido);
}
export const choca = (def, g, x, y) =>
  solido(def, g, x - 8, y - 6) || solido(def, g, x + 8, y - 6) || solido(def, g, x - 8, y + 2) || solido(def, g, x + 8, y + 2);

// ---------------------------------------------------------------- utilidades de eventos
const aviso = (ev, j, txt, malo = false) => ev.push({ tipo: 'aviso', jugador: j.id, txt, malo });
const flotar = (ev, x, y, txt, color) => ev.push({ tipo: 'flotar', x, y, txt, color });
const abrir = (ev, j, panel, extra = {}) => ev.push({ tipo: 'abrir', jugador: j.id, panel, ...extra });

function darItem(def, ev, j, id, n, x, y, color) {
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

// ---------------------------------------------------------------- acciones
const ACCIONES = {
  // Movimiento: el cliente manda la dirección y el tiempo; la regla calcula la posición y choca.
  mover(e, def, a, j) {
    let dx = Math.sign(a.dx || 0), dy = Math.sign(a.dy || 0);
    const dt = Math.min(DT_MAX, Math.max(0, +a.dt || 0));
    if (!dx && !dy) return;
    j.dir = Math.abs(dx) >= Math.abs(dy) && dx ? (dx > 0 ? 'east' : 'west') : (dy > 0 ? 'south' : 'north');
    if (dx && dy) { dx *= Math.SQRT1_2; dy *= Math.SQRT1_2; }
    const v = VELOCIDAD * dt, g = e.granja;
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
    e.tiempo.min += Math.min(DT_MAX, Math.max(0, +a.dt || 0)) * MIN_POR_SEG;
    if (e.tiempo.min >= FIN_DIA) nuevoDia(e, def, ev, true);
  },

  dormir(e, def, a, j, ev) { nuevoDia(e, def, ev, false); },

  comer(e, def, a, j, ev) {
    const s = j.inv[j.sel], it = s && def.ITEMS[s.id];
    if (!it || (it.tipo !== 'cultivo' && it.tipo !== 'artesanal')) return aviso(ev, j, 'Ten un cultivo o una conserva en la mano para comer', true);
    if (j.energia >= j.energiaMax) return aviso(ev, j, 'Tienes la energía al máximo');
    const ganancia = Math.max(10, Math.round(it.venta / 4));
    j.energia = Math.min(j.energiaMax, j.energia + ganancia);
    gastarMano(j);
    flotar(ev, j.x, j.y - 40, `+${ganancia} energía`, '#79c267');
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
    const o = objetoPorUid(def, e.granja, a.uid);
    if (!o || def.OBJ[o.id].accion !== 'cofre' || !cerca(def, j, o)) return;
    const items = o.datos.items || [], s = items[a.i];
    if (!s) return;
    s.n = agregar(def, j, s.id, s.n);
    if (!s.n) items[a.i] = null; else aviso(ev, j, 'La mochila está llena', true);
  },

  cofreGuardar(e, def, a, j, ev) {
    const o = objetoPorUid(def, e.granja, a.uid);
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

  usar(e, def, a, j, ev) { usar(e, def, j, a.x | 0, a.y | 0, ev); },
};

// El jugador debe estar junto al objeto (anti-trampas: valida aunque la acción venga de la red).
function cerca(def, j, o) {
  const [px, py] = celdaJugador(def, j), d = def.OBJ[o.id];
  const w = d.ancho || 1, h = d.alto || 1;
  const dx = px < o.x ? o.x - px : px >= o.x + w ? px - (o.x + w - 1) : 0;
  const dy = py < o.y ? o.y - py : py >= o.y + h ? py - (o.y + h - 1) : 0;
  return Math.max(dx, dy) <= 1;
}

function usar(e, def, j, x, y, ev) {
  const g = e.granja;
  if (!dentro(def, x, y)) return;
  const [px, py] = celdaJugador(def, j);
  if (Math.max(Math.abs(x - px), Math.abs(y - py)) > 1) return;      // solo la casilla de al lado
  const o = objetoEn(def, g, x, y), s = j.inv[j.sel], it = s && def.ITEMS[s.id], i = idx(def, x, y);
  const cx = (x + 0.5) * def.T, cy = y * def.T;
  const esHachaOPico = s && (s.id === 'hacha' || s.id === 'pico');

  if (o) {
    const d = def.OBJ[o.id];
    if (d.accion === 'dormir') return abrir(ev, j, 'dormir');
    if (d.accion === 'tienda') return abrir(ev, j, 'tienda');
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
      if (m.salida && m.listo <= e.tiempo.dia) { if (!darItem(def, ev, j, m.salida, 1, cx, cy)) o.datos = {}; return; }
      if (m.salida) return aviso(ev, j, 'La conserva estará lista mañana');
      if (it && it.conserva) {
        o.datos = { entrada: s.id, salida: it.conserva, listo: e.tiempo.dia + (d.dias || 1) };
        gastarMano(j);
        return aviso(ev, j, `El tarro prepara ${def.ITEMS[it.conserva].nombre.toLowerCase()}`);
      }
      if (!esHachaOPico) return aviso(ev, j, 'Ten un cultivo en la mano para hacer conserva', true);
    }
    if (d.herramienta && it && it.tipo === 'herramienta' && (d.herramienta === s.id || d.herramienta === 'cualquiera')) {
      if (!gastarEnergia(ev, j, 2)) return;
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
      if (o.datos.salida) return aviso(ev, j, 'Espera a que termine la conserva', true);
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

  if (s.id === 'azada') {
    if (g.agua[i] || g.suelo[i]) return;
    for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++)
      if (dentro(def, x + dx, y + dy) && g.agua[idx(def, x + dx, y + dy)]) return aviso(ev, j, 'Muy cerca del agua para arar', true);
    if (!gastarEnergia(ev, j, 2)) return;
    g.suelo[i] = 1;
    if (e.tiempo.lluvia) g.regado[i] = 1;
    return;
  }
  if (s.id === 'regadera') {
    if (g.agua[i]) { j.aguaRegadera = def.ITEMS.regadera.capacidad; flotar(ev, cx, cy, 'Regadera llena', '#5aa9e6'); return; }
    if (!g.suelo[i] || g.regado[i]) return;
    if (j.aguaRegadera <= 0) return aviso(ev, j, 'La regadera está vacía: úsala frente al estanque', true);
    if (!gastarEnergia(ev, j, 1)) return;
    j.aguaRegadera--; g.regado[i] = 1;
    return;
  }
  if (it.tipo === 'semilla') {
    if (!g.suelo[i]) return aviso(ev, j, 'Primero ara la tierra con la azada', true);
    if (cult) return;
    g.cultivos[i] = { id: it.cultivo, dias: 0 };
    gastarMano(j);
    return;
  }
  if (it.tipo === 'colocable') {
    if (px === x && py === y) return;
    if (Object.values(e.jugadores).some(o2 => { const [ox, oy] = celdaJugador(def, o2); return ox === x && oy === y; })) return;
    if (!libre(def, g, x, y)) return aviso(ev, j, 'No cabe ahí', true);
    colocar(def, g, it.objeto, x, y);
    gastarMano(j);
  }
}

// ---------------------------------------------------------------- el paso de los días
function nuevoDia(e, def, ev, desmayo) {
  const g = e.granja, { W, H } = def;
  const notas = [];

  // cobro de envíos y multa por desmayo, por jugador
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
    Object.assign(j, posicionInicio(def));
  }

  // crecimiento
  for (const k of Object.keys(g.cultivos)) if (g.regado[k] && g.cultivos[k].dias < def.CULT[g.cultivos[k].id].dias) g.cultivos[k].dias++;
  // cuervos
  const protegido = k => { const x = k % W, y = Math.floor(k / W); return g.objetos.some(o => def.OBJ[o.id].protege && Math.hypot(o.x - x, o.y - y) <= def.OBJ[o.id].protege); };
  const sinProteger = Object.keys(g.cultivos).filter(k => !protegido(+k));
  if (sinProteger.length >= 8 && azar(e) < 0.6) {
    const k = sinProteger[Math.floor(azar(e) * sinProteger.length)];
    notas.push(`Un cuervo se comió una planta de ${def.ITEMS[def.CULT[g.cultivos[k].id].producto].nombre.toLowerCase()}. Un espantapájaros lo evita.`);
    delete g.cultivos[k];
  }
  // la tierra sin cultivo vuelve a césped a veces; el riego se seca
  for (let i = 0; i < W * H; i++) { if (g.suelo[i] && !g.cultivos[i] && !g.regado[i] && azar(e) < 0.12) g.suelo[i] = 0; g.regado[i] = 0; }

  e.tiempo.dia++;
  e.tiempo.min = INICIO_DIA;
  e.tiempo.lluvia = azar(e) < 0.2;
  if (e.tiempo.lluvia) { for (let i = 0; i < W * H; i++) if (g.suelo[i]) g.regado[i] = 1; notas.push('Llueve: la tierra arada amaneció regada.'); }
  for (const o of g.objetos) {
    const d = def.OBJ[o.id];
    if (d.riega) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const x = o.x + dx, y = o.y + dy; if (dentro(def, x, y) && g.suelo[idx(def, x, y)]) g.regado[idx(def, x, y)] = 1; }
    if (d.accion === 'maquina' && o.datos.salida && o.datos.listo <= e.tiempo.dia) notas.push(`Tu tarro tiene lista una ${def.ITEMS[o.datos.salida].nombre.toLowerCase()}.`);
  }
  // reaparece algo de maleza y alguna roca fuera del claro de la casa
  for (let k = 0, n = 0; k < 200 && n < 4; k++) {
    const x = 2 + Math.floor(azar(e) * (W - 4)), y = 3 + Math.floor(azar(e) * (H - 5));
    if (!(x >= 3 && x <= 17 && y >= 2 && y <= 10) && colocar(def, g, n === 0 ? 'roca' : 'maleza', x, y)) n++;
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
