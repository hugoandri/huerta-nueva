// Carga los gráficos de PixelLab según assets/manifest.json. Lo que falte se dibuja
// provisional, así un contenido nuevo se puede probar antes de generar su sprite.
import { mulberry32, hashTexto } from '../nucleo/azar.js';

const DIRS = ['south', 'west', 'east', 'north'];
const cargarImg = src => new Promise(res => { const im = new Image(); im.onload = () => res(im); im.onerror = () => res(null); im.src = src; });
export const lienzo = (w, h) => { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; };
export function rrect(g, x, y, w, h, r) {
  g.beginPath(); g.moveTo(x + r, y); g.arcTo(x + w, y, x + w, y + h, r); g.arcTo(x + w, y + h, x, y + h, r);
  g.arcTo(x, y + h, x, y, r); g.arcTo(x, y, x + w, y, r); g.closePath();
}

function provisional(id, w, h) {
  const c = lienzo(w, h), g = c.getContext('2d'), m = Math.max(2, Math.round(Math.min(w, h) * 0.12));
  g.fillStyle = `hsl(${hashTexto(id) % 360} 42% 50%)`; rrect(g, m, m, w - 2 * m, h - 2 * m, 5); g.fill();
  g.strokeStyle = '#141414'; g.lineWidth = 2; g.stroke();
  g.fillStyle = '#fff'; g.font = `bold ${Math.round(Math.min(w, h) * 0.42)}px monospace`;
  g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(id[0].toUpperCase(), w / 2, h / 2 + 1);
  return c;
}
function tilesProvisionales(T, lo, up) {
  const r = mulberry32(3);
  return Array.from({ length: 16 }, (_, i) => {
    const c = lienzo(T, T), g = c.getContext('2d'), h = T / 2;
    g.fillStyle = lo; g.fillRect(0, 0, T, T);
    g.fillStyle = up;
    if (i & 8) g.fillRect(0, 0, h, h); if (i & 4) g.fillRect(h, 0, h, h);
    if (i & 2) g.fillRect(0, h, h, h); if (i & 1) g.fillRect(h, h, h, h);
    g.fillStyle = 'rgba(0,0,0,.12)';
    for (let k = 0; k < 10; k++) g.fillRect(Math.floor(r() * T), Math.floor(r() * T), 2, 1);
    return c;
  });
}
function personajeProvisional() {
  const figura = (dir, paso) => {
    const c = lienzo(48, 48), g = c.getContext('2d'), p = paso === 1 ? -3 : paso === 3 ? 3 : 0;
    g.fillStyle = '#4a3222'; g.fillRect(18, 36 + p, 5, 8); g.fillRect(25, 36 - p, 5, 8);
    g.fillStyle = '#3f7d4a'; rrect(g, 15, 22, 18, 16, 3); g.fill();
    g.fillStyle = '#f0c9a0'; g.beginPath(); g.arc(24, 17, 7, 0, 7); g.fill();
    g.fillStyle = '#e3c46a'; g.fillRect(13, 10, 22, 4); g.fillRect(18, 5, 12, 6);
    g.fillStyle = '#222';
    if (dir === 'south') { g.fillRect(21, 17, 2, 2); g.fillRect(26, 17, 2, 2); }
    if (dir === 'east') g.fillRect(27, 17, 2, 2);
    if (dir === 'west') g.fillRect(19, 17, 2, 2);
    return c;
  };
  const PJ = { quieto: {}, caminar: {}, pie: 44, alto: 48 };
  for (const d of DIRS) { PJ.quieto[d] = figura(d, 0); PJ.caminar[d] = [1, 0, 3, 0].map(p => figura(d, p)); }
  return PJ;
}

export async function cargarGraficos(def) {
  const { C, T } = def;
  let M = { sprites: {}, tilesets: {}, personaje: null };
  try { const r = await fetch('assets/manifest.json'); if (r.ok) M = await r.json(); } catch (e) { /* sin gráficos */ }
  let nReales = 0, nProv = 0;

  const SPR = {}, SPR_URL = {};
  await Promise.all(Object.entries(C.sprites).map(async ([id, s]) => {
    const im = M.sprites[id] ? await cargarImg(M.sprites[id]) : null;
    if (im) { nReales++; SPR[id] = im; SPR_URL[id] = M.sprites[id]; }
    else { nProv++; const c = provisional(id, s.ancho, s.alto); SPR[id] = c; SPR_URL[id] = c.toDataURL(); }
  }));

  const COLOR = { tierra: ['#5d9e3f', '#6b4a2f'], agua: ['#5d9e3f', '#3d7fc4'], cueva: ['#2a2420', '#8a7058'] };
  const TS = {};
  for (const t of C.tilesets) {
    let tiles = null;
    if (M.tilesets[t.id]) {
      try {
        const j = await fetch(M.tilesets[t.id]).then(r => r.json());
        tiles = await Promise.all(j.tiles.map(cargarImg));
        if (tiles.some(x => !x)) tiles = null;
      } catch (e) { tiles = null; }
    }
    if (tiles) nReales++; else { nProv++; tiles = tilesProvisionales(T, ...(COLOR[t.id] || ['#5d9e3f', `hsl(${hashTexto(t.id) % 360} 40% 40%)`])); }
    TS[t.id] = tiles;
  }

  // Personaje: la ropa base y cada variante de ropa son juegos de sprites con la misma forma.
  async function cargarPersonaje(info) {
    try {
      const p = { quieto: {}, caminar: {}, pie: 0 };
      for (const d of DIRS) {
        p.quieto[d] = await cargarImg(`${info.carpeta}/${d}.png`);
        p.caminar[d] = (await Promise.all(Array.from({ length: info.cuadros?.[d] || 0 }, (_, i) =>
          cargarImg(`${info.carpeta}/caminar_${d}_${i}.png`)))).filter(Boolean);
      }
      if (!DIRS.every(d => p.quieto[d])) return null;
      const im = p.quieto.south; p.alto = im.height;
      try {   // el pie es la fila opaca más baja: así el personaje se apoya en el suelo
        const c = lienzo(im.width, im.height), g = c.getContext('2d'); g.drawImage(im, 0, 0);
        const d = g.getImageData(0, 0, im.width, im.height).data;
        for (let y = im.height - 1; y >= 0 && !p.pie; y--) for (let x = 0; x < im.width; x++) if (d[(y * im.width + x) * 4 + 3] > 40) { p.pie = y + 1; break; }
      } catch (e) { /* lienzo bloqueado */ }
      if (!p.pie) p.pie = Math.round(im.height * 0.85);
      return p;
    } catch (e) { return null; }
  }
  // PJS[cuerpo][aspecto]: cada personaje (granjera, granjero) con su ropa base y sus variantes.
  const PJS = {}, RETRATO = {};
  const manifestPj = M.personajes || (M.personaje ? { [C.personajes[0].id]: M.personaje } : {});
  for (const c of C.personajes) {
    const info = manifestPj[c.id], juego = (PJS[c.id] = {});
    juego.base = info ? await cargarPersonaje(info) : null;
    if (juego.base) nReales++; else { nProv++; juego.base = personajeProvisional(); }
    RETRATO[c.id] = info ? `${info.carpeta}/south.png` : juego.base.quieto.south.toDataURL();
    for (const r of C.ropas || []) {
      const ri = info?.ropas?.[r.id];
      const p = ri ? await cargarPersonaje(ri) : null;
      if (p) { nReales++; juego[r.id] = p; } else nProv++;    // sin gráfico: se usa la ropa base
    }
  }

  return { SPR, SPR_URL, TS, PJS, RETRATO, nReales, nProv };
}
