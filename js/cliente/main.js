// Arranque del cliente: une contenido, gráficos, sesión, interfaz, entrada y bucle.
import { indexar } from '../nucleo/contenido.js';
import { crearEstado } from '../nucleo/mundo.js';
import { celdaJugador, celdaFrente } from '../nucleo/reglas.js';
import { AlmacenLocal } from '../servicios/almacen.js';
import { obtenerPerfil } from '../servicios/perfil.js';
import { SesionLocal } from '../servicios/sesion.js';
import { cargarGraficos } from './graficos.js';
import { crearRender } from './render.js';
import { crearUI } from './ui.js';

const $ = s => document.querySelector(s);
const def = indexar(await fetch('data/contenido.json').then(r => r.json()));
const G = await cargarGraficos(def);
const perfil = obtenerPerfil();
perfil.cuerpo = def.C.personajes.some(p => p.id === perfil.cuerpo) ? perfil.cuerpo : def.C.personajes[0].id;
const almacen = new AlmacenLocal(def);

$('#estado-assets').innerHTML = G.nProv === 0
  ? `Gráficos: <b>${G.nReales} de PixelLab</b> (${Object.keys(G.PJS).length} personajes con sus ropas, ${Object.keys(G.TS).length} tilesets y ${Object.keys(G.SPR).length} sprites).`
  : `Gráficos: <b>${G.nReales} de PixelLab</b> · ${G.nProv} provisionales (genera los que faltan con <code>herramientas/generar-assets.mjs</code>).`;

// Elegir personaje en la portada (se recuerda en el perfil del navegador).
function pintarElige() {
  $('#elige').innerHTML = def.C.personajes.map(p => `<button type="button" role="radio" aria-checked="${p.id === perfil.cuerpo}" data-cuerpo="${p.id}">
    <img src="${G.RETRATO[p.id]}" alt="">${p.nombre}</button>`).join('');
}
pintarElige();
$('#elige').addEventListener('click', e => {
  const b = e.target.closest('[data-cuerpo]'); if (!b) return;
  perfil.cuerpo = b.dataset.cuerpo;
  try { localStorage.setItem('huerta-nueva-perfil', JSON.stringify(perfil)); } catch (er) { /* sin almacenamiento */ }
  pintarElige();
});
const nuevaPartida = () => crearEstado(def, { semilla: (Math.random() * 2 ** 32) >>> 0, jugador: { id: perfil.id, cuerpo: perfil.cuerpo } });

// Granja de fondo detrás de la portada (no se guarda).
let sesion = new SesionLocal({ estado: crearEstado(def, { jugador: perfil }), def, jugadorId: perfil.id });
const render = crearRender($('#lienzo'), def, G);
const ui = crearUI({ def, G, sesion: () => sesion });
let jugando = false;

function conectar(s) {
  sesion?.cerrar();
  sesion = s;
  sesion.alEvento(ev => { render.eventos(ev, sesion.jugadorId); ui.eventos(ev); });
}
conectar(sesion);

async function empezar(estado) {
  conectar(new SesionLocal({ estado, def, almacen, jugadorId: perfil.id }));
  await sesion.guardar();
  $('#portada').hidden = true; jugando = true;
  ui.aviso('Ara con la azada, siembra y riega. El puesto vende semillas.');
}
ui.alReiniciar(async () => { await almacen.borrar(); empezar(nuevaPartida()); ui.aviso('Partida nueva'); });
$('#btn-nueva').addEventListener('click', () => empezar(nuevaPartida()));
$('#btn-continuar').addEventListener('click', async () => {
  const e = await almacen.cargar();
  if (e && e.jugadores[perfil.id]) empezar(e);
  else { ui.aviso('No se pudo leer la partida guardada', true); empezar(nuevaPartida()); }
});
if (await almacen.existe()) $('#btn-continuar').hidden = false;

// En el celular la página se cierra sin avisar: guardamos al pasar a segundo plano.
document.addEventListener('visibilitychange', () => { if (document.hidden && jugando) sesion.guardar(); });

// ---------------- entrada
const teclas = new Set(), tactil = { arriba: false, abajo: false, izq: false, der: false };
const usarFrente = () => { const [x, y] = celdaFrente(def, sesion.yo); sesion.despachar({ tipo: 'usar', x, y }); };
const comer = () => sesion.despachar({ tipo: 'comer' });

addEventListener('keydown', e => {
  if (!jugando) return;
  const k = e.key.toLowerCase();
  if (ui.abierto()) {
    if ((k === 'escape' || k === 'i' || k === 'tab' || k === 'c') && ui.cerrable()) { e.preventDefault(); ui.cerrar(); }
    return;
  }
  if (k === 'i' || k === 'tab') { e.preventDefault(); return ui.menu('mochila'); }
  if (k === 'c') return ui.menu('crafteo');
  if (k === 'f') return comer();
  if (k === ' ' || k === 'j' || k === 'enter') { e.preventDefault(); if (!e.repeat) usarFrente(); return; }
  const n = '1234567890-='.indexOf(e.key); if (n >= 0) return sesion.despachar({ tipo: 'seleccionar', i: n });
  if (k === 'q' || k === 'e') return sesion.despachar({ tipo: 'seleccionar', i: (sesion.yo.sel + (k === 'e' ? 1 : 11)) % 12 });
  if (k.startsWith('arrow')) e.preventDefault();
  teclas.add(k);
});
addEventListener('keyup', e => teclas.delete(e.key.toLowerCase()));
addEventListener('blur', () => teclas.clear());

const lienzoEl = $('#lienzo');
lienzoEl.addEventListener('wheel', e => {
  if (!jugando || ui.abierto()) return;
  e.preventDefault();
  sesion.despachar({ tipo: 'seleccionar', i: (sesion.yo.sel + (e.deltaY > 0 ? 1 : 11)) % 12 });
}, { passive: false });
lienzoEl.addEventListener('pointerdown', e => {
  if (!jugando || ui.abierto() || e.pointerType === 'touch') return;
  const [x, y] = render.aCelda(e.clientX, e.clientY), [px, py] = celdaJugador(def, sesion.yo);
  const dx = x - px, dy = y - py;
  if (Math.max(Math.abs(dx), Math.abs(dy)) > 1) return;
  if (dx || dy) sesion.despachar({ tipo: 'mirar', dir: Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? 'east' : 'west') : (dy > 0 ? 'south' : 'north') });
  sesion.despachar({ tipo: 'usar', x, y });
});
document.querySelectorAll('.cruceta [data-dir]').forEach(b => {
  const poner = v => e => { e.preventDefault(); tactil[b.dataset.dir] = v; };
  b.addEventListener('pointerdown', poner(true)); b.addEventListener('pointerup', poner(false));
  b.addEventListener('pointercancel', poner(false)); b.addEventListener('pointerleave', poner(false));
});
$('#t-accion').addEventListener('pointerdown', e => { e.preventDefault(); if (jugando && !ui.abierto()) usarFrente(); });
$('#t-comer').addEventListener('click', () => { if (jugando && !ui.abierto()) comer(); });
$('#t-menu').addEventListener('click', () => { if (jugando) ui.abierto() ? ui.cerrable() && ui.cerrar() : ui.menu('mochila'); });

// ---------------- bucle
let ultimo = performance.now();
function cuadro(ahora) {
  const dt = Math.min(0.05, (ahora - ultimo) / 1000); ultimo = ahora;
  if (jugando && !ui.abierto()) {
    const dx = (teclas.has('d') || teclas.has('arrowright') || tactil.der) - (teclas.has('a') || teclas.has('arrowleft') || tactil.izq);
    const dy = (teclas.has('s') || teclas.has('arrowdown') || tactil.abajo) - (teclas.has('w') || teclas.has('arrowup') || tactil.arriba);
    if (dx || dy) sesion.despachar({ tipo: 'mover', dx, dy, dt });
    sesion.despachar({ tipo: 'tiempo', dt });
  }
  render.actualizar(dt, sesion.estado);
  if (jugando) ui.pintarHUD();
  render.dibujar(sesion.estado, sesion.jugadorId, { objetivo: jugando && !ui.abierto() });
  requestAnimationFrame(cuadro);
}
requestAnimationFrame(cuadro);

// Gancho para pruebas automáticas.
window.__huerta = { get sesion() { return sesion; }, def, empezar: () => $('#btn-nueva').click() };
