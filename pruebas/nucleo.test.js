// Pruebas del núcleo: corren en Node, sin navegador. El mismo código correrá en un servidor.
import { test } from 'node:test';
import assert from 'node:assert/strict';
import fs from 'node:fs';
import { indexar } from '../js/nucleo/contenido.js';
import { crearEstado, idx, objetoEn, nuevoJugador } from '../js/nucleo/mundo.js';
import { aplicar, celdaJugador } from '../js/nucleo/reglas.js';
import { cuenta, agregar } from '../js/nucleo/inventario.js';
import { repartirBotin } from '../js/nucleo/botin.js';
import { aJSON, deJSON } from '../js/nucleo/serializar.js';

const def = indexar(JSON.parse(fs.readFileSync(new URL('../data/contenido.json', import.meta.url))));
const YO = { id: 'yo', nombre: 'Prueba' };
const nuevo = (semilla = 7) => crearEstado(def, { semilla, jugador: YO });
const act = (e, a) => aplicar(e, def, { jugador: 'yo', ...a });

// Pone al jugador en el centro de una celda libre del claro, mirando a la derecha.
function enCampo(e) {
  const j = e.jugadores.yo;
  j.x = 8.5 * def.T; j.y = 11 * def.T + 6; j.dir = 'east';
  return celdaJugador(def, j);
}

test('misma semilla, misma granja; otra semilla, otra granja', () => {
  assert.equal(aJSON(nuevo(7)), aJSON(nuevo(7)));
  assert.notEqual(aJSON(nuevo(7).granja), aJSON(nuevo(8).granja));
});

test('arar, sembrar, regar y dormir hace crecer y madurar un rábano', () => {
  const e = nuevo(), [x, y] = enCampo(e), tx = x + 1, i = idx(def, tx, y);
  act(e, { tipo: 'seleccionar', i: 0 }); act(e, { tipo: 'usar', x: tx, y });
  assert.equal(e.granja.suelo[i], 1, 'arado');
  act(e, { tipo: 'seleccionar', i: 4 }); act(e, { tipo: 'usar', x: tx, y });
  assert.equal(e.granja.cultivos[i]?.id, 'rabano', 'sembrado');
  for (let d = 0; d < def.CULT.rabano.dias; d++) {
    enCampo(e);
    act(e, { tipo: 'seleccionar', i: 1 }); act(e, { tipo: 'usar', x: tx, y });
    assert.equal(e.granja.regado[i], 1, `regado día ${d + 1}`);
    aplicar(e, def, { tipo: 'dormir' });
  }
  enCampo(e);
  act(e, { tipo: 'usar', x: tx, y });
  assert.equal(cuenta(e.jugadores.yo, 'rabano'), 1, 'cosechado');
  assert.equal(e.granja.cultivos[i], undefined);
});

test('no se puede usar una casilla lejana (validación anti-trampas)', () => {
  const e = nuevo(), [x, y] = enCampo(e), antes = aJSON(e);
  act(e, { tipo: 'usar', x: x + 5, y });
  assert.equal(aJSON(e), antes);
});

test('fabricar consume materiales y sin materiales no hace nada', () => {
  const e = nuevo(), j = e.jugadores.yo;
  const antes = aJSON(e);
  const ev = act(e, { tipo: 'fabricar', receta: 2 }); // cofre: 20 madera
  assert.equal(aJSON(e), antes);
  assert.ok(ev.some(x => x.tipo === 'aviso' && x.malo));
  agregar(def, j, 'madera', 25);
  act(e, { tipo: 'fabricar', receta: 2 });
  assert.equal(cuenta(j, 'madera'), 5);
  assert.equal(cuenta(j, 'cofre'), 1);
});

test('comprar descuenta dinero y respeta el saldo', () => {
  const e = nuevo(), j = e.jugadores.yo, dinero = j.dinero;
  act(e, { tipo: 'comprar', item: 'semilla_zanahoria', n: 5 });
  assert.equal(j.dinero, dinero - 5 * def.ITEMS.semilla_zanahoria.compra);
  j.dinero = 10;
  act(e, { tipo: 'comprar', item: 'semilla_calabaza', n: 1 });
  assert.equal(j.dinero, 10);
  act(e, { tipo: 'comprar', item: 'madera', n: 1 }); // no está en la tienda
  assert.equal(cuenta(j, 'madera'), 0);
});

test('el movimiento no atraviesa el agua', () => {
  const e = nuevo(), j = e.jugadores.yo;
  let bordeX = null;
  for (let x = 0; x < def.W && bordeX == null; x++) if (e.granja.agua[idx(def, x, 23)]) bordeX = x;
  j.x = (bordeX - 1) * def.T + 4; j.y = 23 * def.T + 16;
  for (let k = 0; k < 50; k++) act(e, { tipo: 'mover', dx: 1, dy: 0, dt: 0.1 });
  assert.ok(j.x < bordeX * def.T, 'se detuvo antes del agua');
});

test('el azar es reproducible: mismas acciones, mismo resultado', () => {
  const jugar = () => { const e = nuevo(); for (let d = 0; d < 5; d++) aplicar(e, def, { tipo: 'dormir' }); return aJSON(e); };
  assert.equal(jugar(), jugar());
});

test('varios jugadores en la misma granja (base para las visitas)', () => {
  const e = nuevo();
  e.jugadores.amiga = nuevoJugador(def, { id: 'amiga', nombre: 'Amiga' });
  agregar(def, e.jugadores.amiga, 'rabano', 3);
  e.jugadores.amiga.envios.push({ id: 'rabano', n: 3 });
  const ev = aplicar(e, def, { tipo: 'dormir' });
  const resAmiga = ev.find(x => x.tipo === 'resumen' && x.jugador === 'amiga');
  assert.equal(resAmiga.total, 3 * def.ITEMS.rabano.venta);
  assert.ok(ev.some(x => x.tipo === 'resumen' && x.jugador === 'yo'));
  assert.equal(aplicar(e, def, { tipo: 'usar', jugador: 'nadie', x: 1, y: 1 })[0].tipo, 'error');
});

test('botín propio: cada jugador recibe el suyo y es reproducible', () => {
  const tabla = def.C.botines.mazmorra_prueba;
  const a = repartirBotin(tabla, ['ana', 'beto', 'cata'], 12345);
  const b = repartirBotin(tabla, ['cata', 'ana', 'beto'], 12345);
  assert.deepEqual(a.ana, b.ana, 'no depende del orden de los jugadores');
  for (const id of ['ana', 'beto', 'cata']) {
    assert.ok(a[id].find(x => x.item === 'piedra' && x.n >= 5), 'incluye lo garantizado');
    for (const { item } of a[id]) assert.ok(def.ITEMS[item], `${item} existe en el contenido`);
  }
  assert.notDeepEqual(repartirBotin(tabla, ['ana'], 1).ana, repartirBotin(tabla, ['ana'], 99999).ana);
});

test('guardar y cargar conserva el estado', () => {
  const e = nuevo(); enCampo(e);
  act(e, { tipo: 'usar', x: 9, y: 11 });
  const copia = deJSON(aJSON(e), def);
  assert.equal(aJSON({ ...copia, granja: { ...copia.granja, ver: 0 } }), aJSON({ ...e, granja: { ...e.granja, ver: 0 } }));
  assert.equal(objetoEn(def, copia.granja, 5, 6)?.id, 'casa', 'la casa sigue ahí');
  assert.equal(deJSON('{"version":1}', def), null, 'una versión vieja no se carga');
});
