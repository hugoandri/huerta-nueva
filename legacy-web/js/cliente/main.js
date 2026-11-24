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
import { crearAudio } from './audio.js';

const $ = s => document.querySelector(s);
const def = indexar(await fetch('data/contenido.json').then(r => r.json()));
const G = await cargarGraficos(def);
const perfil = obtenerPerfil();
perfil.cuerpo = def.C.personajes.some(p => p.id === perfil.cuerpo) ? perfil.cuerpo : def.C.personajes[0].id;
const almacen = new AlmacenLocal(def);

$('#estado-assets').innerHTML = G.nProv === 0
  ? `Graphics: <b>${G.nReales} from PixelLab</b> (${Object.keys(G.PJS).length} characters with outfits, ${Object.keys(G.TS).length} tilesets, and ${Object.keys(G.SPR).length} sprites).`
  : `Graphics: <b>${G.nReales} from PixelLab</b> · ${G.nProv} placeholders (generate missing assets with <code>herramientas/generar-assets.mjs</code>).`;

// Elegir personaje en la portada (se recuerda en el perfil del navegador).
const inpNombre = $('#inp-nombre'), inpSemilla = $('#inp-semilla');
function nombrePorDefecto(cuerpo) {
  return def.C.personajes.find(p => p.id === cuerpo)?.nombre || 'Farmer';
}
function pintarElige() {
  $('#elige').innerHTML = def.C.personajes.map(p => `<button type="button" role="radio" aria-checked="${p.id === perfil.cuerpo}" data-cuerpo="${p.id}">
    <img src="${G.RETRATO[p.id]}" alt="">${p.nombre}</button>`).join('');
  if (inpNombre && (!inpNombre.value || def.C.personajes.some(p => p.nombre === inpNombre.value))) {
    inpNombre.value = perfil.nombre || nombrePorDefecto(perfil.cuerpo);
  }
}
pintarElige();
$('#elige').addEventListener('click', e => {
  const b = e.target.closest('[data-cuerpo]'); if (!b) return;
  perfil.cuerpo = b.dataset.cuerpo;
  if (inpNombre && def.C.personajes.some(p => p.nombre === inpNombre.value)) inpNombre.value = nombrePorDefecto(perfil.cuerpo);
  try { localStorage.setItem('huerta-nueva-perfil', JSON.stringify(perfil)); } catch (er) { /* sin almacenamiento */ }
  pintarElige();
});

function leerSemilla() {
  const txt = inpSemilla?.value?.trim();
  if (!txt) return (Math.random() * 2 ** 32) >>> 0;
  if (/^\d+$/.test(txt)) return (Number(txt) >>> 0) || 7;
  let h = 2166136261 >>> 0;
  for (let i = 0; i < txt.length; i++) h = Math.imul(h ^ txt.charCodeAt(i), 16777619) >>> 0;
  return h || 7;
}
function nuevaPartida() {
  const nombre = inpNombre?.value?.trim() || nombrePorDefecto(perfil.cuerpo);
  perfil.nombre = nombre;
  try { localStorage.setItem('huerta-nueva-perfil', JSON.stringify(perfil)); } catch (er) { /* sin almacenamiento */ }
  return crearEstado(def, { semilla: leerSemilla(), jugador: { id: perfil.id, cuerpo: perfil.cuerpo, nombre } });
}

// Granja de fondo detrás de la portada (no se guarda).
let sesion = new SesionLocal({ estado: crearEstado(def, { jugador: perfil }), def, jugadorId: perfil.id });
const audio = crearAudio();
const render = crearRender($('#lienzo'), def, G);
const ui = crearUI({ def, G, sesion: () => sesion, audio });
let jugando = false;

function conectar(s) {
  sesion?.cerrar();
  sesion = s;
  sesion.alEvento(ev => { render.eventos(ev, sesion.jugadorId); ui.eventos(ev); audio.eventos(ev, sesion.jugadorId); });
}
conectar(sesion);

async function refrescarPortada() {
  const e = await almacen.cargar();
  const j = e?.jugadores?.[perfil.id];
  const tarjeta = $('#tarjeta-guardado'), btnCont = $('#btn-continuar');
  if (e && j) {
    const dia = (e.tiempo.dia - 1) % 28 + 1, anio = Math.floor((e.tiempo.dia - 1) / 28) + 1;
    const mina = e.mazmorra?.progreso?.mina?.record || 0, cripta = e.mazmorra?.progreso?.cripta?.record || 0;
    const hitos = (e.hitos || ['intro']).length, totalCartas = def.C.historia?.cartas?.length || 8;
    $('#tg-retrato').src = G.RETRATO[j.cuerpo] || G.RETRATO[def.C.personajes[0].id];
    $('#tg-titulo').textContent = `${j.nombre || nombrePorDefecto(j.cuerpo)}'s Homestead`;
    $('#tg-meta').innerHTML = `Spring · Day <b>${dia}</b> (Year ${anio}) · <b>${j.dinero.toLocaleString('en-US')}g</b><br>📜 ${hitos}/${totalCartas} Notes · ⛏️ Mine Fl. ${mina}${cripta ? ` · Crypt Fl. ${cripta}` : ''}`;
    tarjeta.hidden = false;
    btnCont.hidden = false;
  } else {
    tarjeta.hidden = true;
    btnCont.hidden = true;
  }
}

async function empezar(estado, esNueva = false) {
  conectar(new SesionLocal({ estado, def, almacen, jugadorId: perfil.id }));
  await sesion.guardar();
  $('#portada').hidden = true; jugando = true;
  const intro = def.C.historia?.cartas?.[0];
  ui.aviso(esNueva && intro ? `📜 "${intro.titulo}": open your Journal (I) to read Grandpa's note.` : 'Welcome back to Mosslantern Glen!');
}

async function volverAPortada() {
  if (jugando) await sesion.guardar();
  jugando = false;
  ui.cerrar();
  await refrescarPortada();
  $('#portada').hidden = false;
}

ui.alReiniciar(async () => { await almacen.borrar(); await refrescarPortada(); empezar(nuevaPartida(), true); ui.aviso('New farm started in Mosslantern Glen'); });
ui.alSalirMenu(() => volverAPortada());
$('#btn-hud-menu')?.addEventListener('click', () => { if (jugando) ui.abierto() ? ui.cerrable() && ui.cerrar() : ui.menu('mochila'); });

// Navegación por pestañas del Main Menu
$('#portada-tabs')?.addEventListener('click', e => {
  const b = e.target.closest('[data-ptab]'); if (!b) return;
  audio?.sfx.clic();
  const tab = b.dataset.ptab;
  document.querySelectorAll('#portada-tabs [data-ptab]').forEach(el => el.classList.toggle('activo', el === b));
  document.querySelectorAll('#portada [data-pvista]').forEach(el => { el.hidden = el.dataset.pvista !== tab; });
});

// Ajustes en el Main Menu
function actualizarBotonAudioPortada() {
  const b = $('#btn-portada-audio');
  if (b) b.textContent = audio?.silencio ? '🔇 Audio: Muted' : '🔊 Audio: On';
}
actualizarBotonAudioPortada();
$('#btn-portada-audio')?.addEventListener('click', () => { audio?.alternarMute(); actualizarBotonAudioPortada(); });
$('#btn-portada-sfx')?.addEventListener('click', () => { audio?.sfx.moneda(); ui.aviso('🕯️ Mosslantern chime OK'); });
$('#btn-portada-full')?.addEventListener('click', () => {
  if (!document.fullscreenElement) document.documentElement.requestFullscreen?.().catch(() => {});
  else document.exitFullscreen?.().catch(() => {});
});
$('#btn-exportar')?.addEventListener('click', async () => {
  const raw = localStorage.getItem('huerta-nueva-partida-v1');
  if (!raw) return ui.aviso('No saved farm to export yet', true);
  const blob = new Blob([raw], { type: 'application/json' });
  const a = document.createElement('a');
  a.href = URL.createObjectURL(blob);
  a.download = `mosslantern-save-day-${sesion?.estado?.tiempo?.dia || 1}.json`;
  a.click();
  URL.revokeObjectURL(a.href);
  ui.aviso('Save file exported');
});
$('#inp-importar')?.addEventListener('change', async e => {
  const f = e.target.files?.[0]; if (!f) return;
  try {
    const txt = await f.text();
    localStorage.setItem('huerta-nueva-partida-v1', txt);
    const cargado = await almacen.cargar();
    if (!cargado) throw new Error('Invalid save');
    await refrescarPortada();
    ui.aviso('Save imported! Click Continue to play.');
  } catch (er) {
    ui.aviso('Could not import save file', true);
  }
  e.target.value = '';
});

$('#btn-nueva').addEventListener('click', () => empezar(nuevaPartida(), true));
$('#btn-continuar').addEventListener('click', async () => {
  const e = await almacen.cargar();
  if (e && e.jugadores[perfil.id]) empezar(e);
  else { ui.aviso('Could not load saved game', true); empezar(nuevaPartida(), true); }
});
await refrescarPortada();

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
  if (!jugando || ui.abierto()) return;
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
