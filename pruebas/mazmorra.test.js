import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { indexar } from '../js/nucleo/contenido.js';
import { crearEstado, idx, frenteACueva, nuevoJugador } from '../js/nucleo/mundo.js';
import { aplicar, infoMazmorras } from '../js/nucleo/reglas.js';
import { generarPiso } from '../js/nucleo/mazmorra.js';
import { cuenta, agregar } from '../js/nucleo/inventario.js';
import { aJSON, deJSON } from '../js/nucleo/serializar.js';

const def = indexar(JSON.parse(fs.readFileSync(new URL('../data/contenido.json', import.meta.url))));
const nuevo = () => crearEstado(def, { semilla: 7, jugador: { id: 'yo', nombre: 'Prueba' } });
const act = (e, a, quien = 'yo') => aplicar(e, def, { jugador: quien, ...a });

function entrar(e, quien = 'yo') {
  Object.assign(e.jugadores[quien], frenteACueva(def));
  return act(e, { tipo: 'entrarMazmorra', mazmorra: 'mina', piso: 1 }, quien);
}
// Pone al jugador justo encima (norte) de una celda y mirando hacia ella.
function junto(e, x, y, quien = 'yo') {
  Object.assign(e.jugadores[quien], { x: (x + 0.5) * def.T, y: y * def.T - 2, dir: 'south' });
}

test('los 10 pisos son reproducibles y siempre se llega de la entrada a la salida', () => {
  for (const maz of def.C.mazmorras) for (let piso = 1; piso <= maz.pisos; piso++) {
    const z = generarPiso(def, maz.id, 123, piso);
    assert.equal(aJSON(z), aJSON(generarPiso(def, maz.id, 123, piso)), `${maz.id} piso ${piso} reproducible`);
    // recorrido a lo ancho por el suelo (sin muros; los objetos sólidos se ignoran: se pueden romper)
    const vis = new Set([idx(def, z.entrada.x, z.entrada.y)]), cola = [[z.entrada.x, z.entrada.y]];
    while (cola.length) {
      const [x, y] = cola.pop();
      for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) {
        const k = idx(def, x + dx, y + dy);
        if (!z.muro[k] && !vis.has(k)) { vis.add(k); cola.push([x + dx, y + dy]); }
      }
    }
    assert.ok(vis.has(idx(def, z.salaFinal.x, z.salaFinal.y)), `piso ${piso}: la sala final es alcanzable`);
    const jefe = maz.jefes[String(piso)];
    if (jefe) assert.ok(z.monstruos.some(m => m.id === jefe), `piso ${piso}: está el jefe`);
    else if (piso < 10) assert.ok(z.objetos.some(o => o.id === 'escalera_bajar'), `piso ${piso}: hay escalera`);
  }
});

test('entrar, bajar con la escalera y volver a la granja', () => {
  const e = nuevo();
  entrar(e);
  assert.equal(e.jugadores.yo.lugar, 'mazmorra');
  assert.equal(e.mazmorra.piso, 1);
  const esc = e.mazmorra.nivel.objetos.find(o => o.id === 'escalera_bajar');
  e.mazmorra.nivel.monstruos = [];
  junto(e, esc.x, esc.y);
  act(e, { tipo: 'usar', x: esc.x, y: esc.y });
  assert.equal(e.mazmorra.piso, 2);
  assert.equal(e.mazmorra.progreso.mina.record, 2);
  const sube = e.mazmorra.nivel.objetos.find(o => o.id === 'escalera_subir');
  junto(e, sube.x, sube.y + 2); e.jugadores.yo.dir = 'north';
  e.jugadores.yo.y = (sube.y + 1) * def.T + 20;
  act(e, { tipo: 'usar', x: sube.x, y: sube.y });
  assert.equal(e.jugadores.yo.lugar, 'granja');
  assert.equal(e.mazmorra.nivel, null);
});

test('no se puede entrar a un punto de control no alcanzado', () => {
  const e = nuevo();
  Object.assign(e.jugadores.yo, frenteACueva(def));
  act(e, { tipo: 'entrarMazmorra', mazmorra: 'mina', piso: 6 });
  assert.equal(e.jugadores.yo.lugar, 'granja');
  e.mazmorra.progreso.mina = { record: 5, completada: 0 };
  act(e, { tipo: 'entrarMazmorra', mazmorra: 'mina', piso: 6 });
  assert.equal(e.mazmorra.piso, 6);
});

test('combatir: la espada mata al limo y suelta baba; el limo hace daño', () => {
  const e = nuevo(), j = e.jugadores.yo;
  entrar(e);
  const z = e.mazmorra.nivel;
  z.monstruos = [];
  const { x, y } = z.entrada;
  z.monstruos.push({ uid: 999, id: 'limo', x: (x + 0.5) * def.T, y: (y + 1 + 0.6) * def.T, vida: 3, vidaMax: 3, cd: 0, dir: 1, golpe: 0 });
  agregar(def, j, 'espada_madera', 1);
  j.sel = j.inv.findIndex(s => s?.id === 'espada_madera');
  j.x = (x + 0.5) * def.T; j.y = (y + 0.6) * def.T; j.dir = 'south';
  const vidaAntes = j.vida;
  for (let k = 0; k < 30; k++) aplicar(e, def, { tipo: 'tiempo', dt: 0.1 });   // el limo ataca
  assert.ok(j.vida < vidaAntes, 'el limo hizo daño');
  for (let k = 0; k < 6 && z.monstruos.length; k++) { act(e, { tipo: 'usar', x, y: y + 1 }); j.cdAtaque = 0; }
  assert.equal(z.monstruos.length, 0, 'el limo murió');
});

test('ropa de campo y de batalla: la de batalla se usa sola al entrar a la mazmorra', () => {
  const e = nuevo(), j = e.jugadores.yo;
  assert.equal(j.ropa, 'ropa_granjera');
  assert.equal(j.ropaBatalla, 'ropa_aventurera');
  assert.equal(j.vidaMax, 10, 'en la granja no cuenta la armadura');
  entrar(e);
  assert.equal(j.vidaMax, 12, 'en la mazmorra cuenta la armadura (+1 corazón)');
  agregar(def, j, 'ropa_minera', 1);
  const i = j.inv.findIndex(s => s?.id === 'ropa_minera');
  act(e, { tipo: 'vestir', i });
  assert.equal(j.ropaBatalla, 'ropa_minera');
  assert.equal(j.inv[i].id, 'ropa_aventurera', 'la ropa anterior vuelve a la mochila');
  assert.equal(j.ropa, 'ropa_granjera', 'la de campo no cambia');
});

test('la cripta se abre al llegar al piso 5 de la mina', () => {
  const e = nuevo();
  Object.assign(e.jugadores.yo, frenteACueva(def));
  assert.equal(infoMazmorras(e, def).find(m => m.id === 'cripta').abierta, false);
  act(e, { tipo: 'entrarMazmorra', mazmorra: 'cripta', piso: 1 });
  assert.equal(e.jugadores.yo.lugar, 'granja');
  e.mazmorra.progreso.mina = { record: 5, completada: 0 };
  act(e, { tipo: 'entrarMazmorra', mazmorra: 'cripta', piso: 1 });
  assert.equal(e.mazmorra.actual, 'cripta');
  assert.ok(e.mazmorra.nivel.objetos.some(o => o.id === 'urna') || e.mazmorra.nivel.objetos.length > 0);
});

test('vencer al último jefe reparte un botín propio a cada jugador presente', () => {
  const e = nuevo();
  e.jugadores.amiga = nuevoJugador(def, { id: 'amiga', nombre: 'Amiga' });
  entrar(e); entrar(e, 'amiga');
  e.mazmorra.progreso.mina = { record: 9, completada: 0 };
  const ev0 = [];
  // saltar al piso 10
  e.mazmorra.nivel = generarPiso(def, 'mina', e.mazmorra.semilla, 10); e.mazmorra.piso = 10;
  const z = e.mazmorra.nivel, jefe = z.monstruos.find(m => m.id === 'guardian');
  z.monstruos = [jefe]; jefe.vida = 1;
  const j = e.jugadores.yo;
  j.x = jefe.x; j.y = jefe.y - def.T; j.dir = 'south';
  const [cx, cy] = [Math.floor(jefe.x / def.T), Math.floor((jefe.y - 6) / def.T)];
  const ev = act(e, { tipo: 'usar', x: cx, y: cy });
  const botines = ev.filter(x => x.tipo === 'botin');
  assert.deepEqual(botines.map(b => b.jugador).sort(), ['amiga', 'yo']);
  assert.ok(cuenta(e.jugadores.amiga, 'gema') >= 3, 'la amiga recibió lo garantizado');
  assert.ok(z.objetos.some(o => o.id === 'escalera_subir'), 'apareció la salida');
  assert.equal(e.mazmorra.progreso.mina.completada, 1);
  void ev0;
});

test('el horno funde 3 minerales en un lingote con el paso del tiempo', () => {
  const e = nuevo(), j = e.jugadores.yo;
  j.x = 8.5 * def.T; j.y = 11 * def.T + 6; j.dir = 'east';
  agregar(def, j, 'horno', 1); agregar(def, j, 'mineral_cobre', 4);
  j.sel = j.inv.findIndex(s => s?.id === 'horno');
  act(e, { tipo: 'usar', x: 9, y: 11 });
  j.sel = j.inv.findIndex(s => s?.id === 'mineral_cobre');
  act(e, { tipo: 'usar', x: 9, y: 11 });
  assert.equal(cuenta(j, 'mineral_cobre'), 1);
  e.tiempo.min += 130;
  act(e, { tipo: 'usar', x: 9, y: 11 });
  assert.equal(cuenta(j, 'lingote_cobre'), 1);
});

test('morir en la mazmorra te devuelve a la granja con media vida', () => {
  const e = nuevo(), j = e.jugadores.yo;
  entrar(e);
  const z = e.mazmorra.nivel;
  z.monstruos = [{ uid: 998, id: 'esqueleto', x: j.x, y: j.y + 8, vida: 99, vidaMax: 99, cd: 0, dir: 1, golpe: 0 }];
  j.vida = 1;
  aplicar(e, def, { tipo: 'tiempo', dt: 0.05 });
  assert.equal(j.lugar, 'granja');
  assert.equal(j.vida, Math.ceil(j.vidaMax / 2));
});

test('una partida v3 se migra hasta v5: túnel arriba y ropa separada', () => {
  const e = nuevo();
  const v3 = JSON.parse(aJSON(e));
  v3.version = 3; v3.mazmorra = { semilla: null, piso: 0, nivel: null, record: 7, completada: 1 };
  for (const j of Object.values(v3.jugadores)) { j.ropa = 'ropa_minera'; delete j.ropaBatalla; delete j.cuerpo; }
  const cueva = v3.granja.objetos.find(o => o.id === 'entrada_cueva'); cueva.x = 20; cueva.y = 4;
  const m = deJSON(JSON.stringify(v3), def);
  assert.equal(m.version, 6);
  assert.equal(m.jugadores.yo.ropa, 'ropa_granjera');
  assert.equal(m.jugadores.yo.ropaBatalla, 'ropa_minera');
  assert.equal(m.jugadores.yo.cuerpo, 'granjera');
  assert.equal(m.mazmorra.progreso.mina.record, 7);
  const t = m.granja.objetos.find(o => o.id === 'entrada_cueva');
  assert.deepEqual([t.x, t.y], [22, 2], 'el túnel está en la pared del acantilado');
});

test('una partida v2 también llega a v5', () => {
  const e = nuevo();
  const v2 = JSON.parse(aJSON(e));
  v2.version = 2; delete v2.mazmorra;
  for (const j of Object.values(v2.jugadores)) { delete j.vida; delete j.ropa; delete j.lugar; delete j.ropaBatalla; }
  assert.equal(deJSON(JSON.stringify(v2), def).version, 6);
});

test('se puede jugar con el granjero', () => {
  const e = crearEstado(def, { semilla: 7, jugador: { id: 'el', cuerpo: 'granjero' } });
  assert.equal(e.jugadores.el.cuerpo, 'granjero');
  assert.equal(e.jugadores.el.nombre, 'Granjero');
});
