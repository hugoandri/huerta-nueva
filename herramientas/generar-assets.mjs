#!/usr/bin/env node
// Genera los gráficos del juego con la API de PixelLab (https://api.pixellab.ai/v2)
// a partir de data/contenido.json. Solo genera lo que todavía no existe en assets/.
//
// Uso:
//   PIXELLAB_API_KEY=tu_token node herramientas/generar-assets.mjs            genera lo que falta
//   ... node herramientas/generar-assets.mjs --lista                             muestra qué generaría, sin gastar
//   ... node herramientas/generar-assets.mjs --solo=arbol,roca --forzar          regenera esos sprites
//
// Requiere Node 18+ (usa fetch). Si estás detrás de un proxy en Node 22.21+: NODE_USE_ENV_PROXY=1.

import fs from 'node:fs/promises';
import zlib from 'node:zlib';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const RAIZ = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const API = 'https://api.pixellab.ai/v2';
const TOKEN = process.env.PIXELLAB_API_KEY;
const args = Object.fromEntries(process.argv.slice(2).map(a => {
  const [k, v] = a.replace(/^--/, '').split('=');
  return [k, v ?? true];
}));
const SOLO = args.solo ? new Set(String(args.solo).split(',')) : null;
const FORZAR = !!args.forzar;
const LISTA = !!args.lista;
const PARALELO = Number(args.paralelo || 4);

const contenido = JSON.parse(await fs.readFile(path.join(RAIZ, 'data/contenido.json'), 'utf8'));
const estilo = contenido.estilo;
const dir = p => path.join(RAIZ, 'assets', p);
const existe = async p => fs.access(p).then(() => true, () => false);
const esperar = ms => new Promise(r => setTimeout(r, ms));
const quiere = id => !SOLO || SOLO.has(id);

async function api(metodo, ruta, cuerpo) {
  for (let intento = 0; ; intento++) {
    const r = await fetch(API + ruta, {
      method: metodo,
      headers: { Authorization: `Bearer ${TOKEN}`, 'Content-Type': 'application/json' },
      body: cuerpo ? JSON.stringify(cuerpo) : undefined,
    });
    const texto = await r.text();
    let datos; try { datos = JSON.parse(texto); } catch { datos = texto; }
    if (r.status === 423) return { pendiente: true, datos };            // todavía generando
    if ((r.status === 429 || r.status >= 500) && intento < 5) {          // límite o error temporal
      await esperar(5000 * (intento + 1));
      continue;
    }
    if (!r.ok) throw new Error(`${metodo} ${ruta} → ${r.status}: ${texto.slice(0, 400)}`);
    return { datos };
  }
}

// Descarga binaria desde la API (423 = todavía generando → null).
async function apiBinario(ruta) {
  const r = await fetch(API + ruta, { headers: { Authorization: `Bearer ${TOKEN}` } });
  if (r.status === 423) return null;
  if (!r.ok) throw new Error(`GET ${ruta} → ${r.status}: ${(await r.text()).slice(0, 300)}`);
  return Buffer.from(await r.arrayBuffer());
}

// Lector mínimo de ZIP (directorio central + deflate), para no depender de paquetes.
function leerZip(buf) {
  let fin = buf.length - 22;
  while (fin >= 0 && buf.readUInt32LE(fin) !== 0x06054b50) fin--;
  if (fin < 0) throw new Error('ZIP inválido');
  const total = buf.readUInt16LE(fin + 10);
  let p = buf.readUInt32LE(fin + 16);
  const archivos = {};
  for (let i = 0; i < total; i++) {
    const metodo = buf.readUInt16LE(p + 10), comprimido = buf.readUInt32LE(p + 20);
    const largoNombre = buf.readUInt16LE(p + 28), largoExtra = buf.readUInt16LE(p + 30), largoComent = buf.readUInt16LE(p + 32);
    const local = buf.readUInt32LE(p + 42);
    const nombre = buf.toString('utf8', p + 46, p + 46 + largoNombre);
    const inicio = local + 30 + buf.readUInt16LE(local + 26) + buf.readUInt16LE(local + 28);
    const datos = buf.subarray(inicio, inicio + comprimido);
    if (!nombre.endsWith('/')) archivos[nombre] = metodo === 8 ? zlib.inflateRawSync(datos) : datos;
    p += 46 + largoNombre + largoExtra + largoComent;
  }
  return archivos;
}

async function guardarBase64(b64, destino) {
  await fs.mkdir(path.dirname(destino), { recursive: true });
  await fs.writeFile(destino, limpiarBordes(Buffer.from(b64, 'base64')));
}

// ---------- limpieza de bordes ----------
// PixelLab a veces deja píxeles blancos opacos en el contorno (un halo del recorte del fondo).
// Los pintamos con el color de contorno más común del propio sprite. Solo PNG RGBA de 8 bits.
function leerPNG(buf) {
  let p = 8, w = 0, h = 0, bd = 0, ct = 0, il = 0; const idat = [];
  while (p < buf.length) {
    const largo = buf.readUInt32BE(p), tipo = buf.toString('ascii', p + 4, p + 8), d = buf.subarray(p + 8, p + 8 + largo);
    if (tipo === 'IHDR') { w = d.readUInt32BE(0); h = d.readUInt32BE(4); bd = d[8]; ct = d[9]; il = d[12]; }
    if (tipo === 'IDAT') idat.push(d);
    p += 12 + largo;
  }
  if (bd !== 8 || ct !== 6 || il !== 0) return null;
  const crudo = zlib.inflateSync(Buffer.concat(idat)), fila = w * 4, px = Buffer.alloc(w * h * 4);
  for (let y = 0, i = 0; y < h; y++) {
    const f = crudo[i++];
    for (let x = 0; x < fila; x++, i++) {
      const a = x >= 4 ? px[y * fila + x - 4] : 0, b = y ? px[(y - 1) * fila + x] : 0, c = x >= 4 && y ? px[(y - 1) * fila + x - 4] : 0;
      let v = crudo[i];
      if (f === 1) v += a; else if (f === 2) v += b; else if (f === 3) v += (a + b) >> 1;
      else if (f === 4) { const q = a + b - c, pa = Math.abs(q - a), pb = Math.abs(q - b), pc = Math.abs(q - c); v += pa <= pb && pa <= pc ? a : pb <= pc ? b : c; }
      px[y * fila + x] = v & 255;
    }
  }
  return { w, h, px };
}
function escribirPNG({ w, h, px }) {
  const trozo = (tipo, d) => {
    const t = Buffer.concat([Buffer.from(tipo, 'ascii'), d]), b = Buffer.alloc(4); b.writeUInt32BE(d.length);
    const crc = Buffer.alloc(4); crc.writeUInt32BE(zlib.crc32(t) >>> 0); return Buffer.concat([b, t, crc]);
  };
  const ihdr = Buffer.alloc(13); ihdr.writeUInt32BE(w, 0); ihdr.writeUInt32BE(h, 4); ihdr[8] = 8; ihdr[9] = 6;
  const crudo = Buffer.alloc(h * (w * 4 + 1));
  for (let y = 0; y < h; y++) px.copy(crudo, y * (w * 4 + 1) + 1, y * w * 4, (y + 1) * w * 4);
  return Buffer.concat([Buffer.from([137, 80, 78, 71, 13, 10, 26, 10]), trozo('IHDR', ihdr), trozo('IDAT', zlib.deflateSync(crudo)), trozo('IEND', Buffer.alloc(0))]);
}
function limpiarBordes(buf) {
  const img = leerPNG(buf); if (!img) return buf;
  const { w, h, px } = img, A = (x, y) => (x < 0 || y < 0 || x >= w || y >= h ? 0 : px[(y * w + x) * 4 + 3]);
  const esBorde = (x, y) => A(x, y) > 0 && (!A(x - 1, y) || !A(x + 1, y) || !A(x, y - 1) || !A(x, y + 1));
  const blanco = i => px[i] > 200 && px[i + 1] > 200 && px[i + 2] > 200;
  const votos = new Map(), blancos = [];
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) {
    if (!esBorde(x, y)) continue;
    const i = (y * w + x) * 4;
    if (blanco(i)) { blancos.push(i); continue; }
    if (px[i] + px[i + 1] + px[i + 2] < 180) { const k = px.readUInt32BE(i); votos.set(k, (votos.get(k) || 0) + 1); }
  }
  if (!blancos.length) return buf;
  const contorno = [...votos].sort((a, b) => b[1] - a[1])[0]?.[0] ?? 0x1a1612ff;
  for (const i of blancos) px.writeUInt32BE(contorno, i);
  return escribirPNG(img);
}

// ---------- estado (ids de PixelLab, para reanudar sin pagar dos veces) ----------
const RUTA_ESTADO = dir('pixellab-ids.json');
const estado = JSON.parse(await fs.readFile(RUTA_ESTADO, 'utf8').catch(() => '{}'));
const guardarEstado = () => fs.writeFile(RUTA_ESTADO, JSON.stringify(estado, null, 2));

// ---------- sprites (objetos, cultivos, íconos): create-image-pixflux, síncrono ----------
async function generarSprite(id, s) {
  const destino = dir(`sprites/${id}.png`);
  // "modelo": "pixen" → modelo Pixen: mejor para construcciones de frente, con volumen y mirando hacia abajo.
  const { datos } = s.modelo === 'pixen' ? await api('POST', '/create-image-pixen', {
    description: `${s.descripcion}, ${estilo.sufijo}`,
    image_size: { width: s.ancho, height: s.alto },
    no_background: true,
    outline: s.outline || estilo.outline,
    detail: s.detail || 'highly detailed',
    view: s.view || 'high top-down',
    direction: 'south',
    ...(s.semilla != null ? { seed: s.semilla } : {}),
  }) : await api('POST', '/create-image-pixflux', {
    description: `${s.descripcion}, ${estilo.sufijo}`,
    image_size: { width: s.ancho, height: s.alto },
    no_background: true,
    outline: s.outline || estilo.outline,
    shading: s.shading || estilo.shading,
    detail: s.detail || estilo.detail,
    view: s.view || estilo.view,
    direction: 'south',
    ...(s.semilla != null ? { seed: s.semilla } : {}),
  });
  await guardarBase64(datos.image.base64, destino);
  if (s.rellenar_huecos) await fs.writeFile(destino, rellenarHuecos(await fs.readFile(destino)));
  if (s.recortar) await fs.writeFile(destino, recortar(await fs.readFile(destino)));
  return datos.usage;
}

// Al quitar el fondo, PixelLab a veces borra también el interior oscuro de una abertura
// (la boca de un túnel). Todo hueco transparente que no toca el borde de la imagen se pinta oscuro.
function rellenarHuecos(buf) {
  const img = leerPNG(buf); if (!img) return buf;
  const { w, h, px } = img, fuera = new Uint8Array(w * h), pila = [];
  const transp = i => px[i * 4 + 3] < 20;
  for (let x = 0; x < w; x++) { pila.push(x, (h - 1) * w + x); }
  for (let y = 0; y < h; y++) { pila.push(y * w, y * w + w - 1); }
  while (pila.length) {
    const i = pila.pop();
    if (fuera[i] || !transp(i)) continue;
    fuera[i] = 1;
    const x = i % w, y = (i / w) | 0;
    if (x > 0) pila.push(i - 1); if (x < w - 1) pila.push(i + 1); if (y > 0) pila.push(i - w); if (y < h - 1) pila.push(i + w);
  }
  let n = 0;
  for (let i = 0; i < w * h; i++) if (transp(i) && !fuera[i]) { px.writeUInt32BE(0x0d0a0cff, i * 4); n++; }
  return n ? escribirPNG(img) : buf;
}

// Realce de color para los personajes: PixelLab los entrega con colores más apagados que los
// objetos. Sube la saturación y un poco el contraste (en HSL) sin tocar la transparencia.
function realzar(buf, sat = 1.45, con = 1.15) {
  const img = leerPNG(buf); if (!img) return buf;
  const { px } = img;
  for (let i = 0; i < px.length; i += 4) {
    if (!px[i + 3]) continue;
    let r = px[i] / 255, g = px[i + 1] / 255, b = px[i + 2] / 255;
    const max = Math.max(r, g, b), min = Math.min(r, g, b);
    let h = 0, s = 0, l = (max + min) / 2;
    if (max !== min) {
      const d = max - min;
      s = l > 0.5 ? d / (2 - max - min) : d / (max + min);
      h = max === r ? (g - b) / d + (g < b ? 6 : 0) : max === g ? (b - r) / d + 2 : (r - g) / d + 4;
      h /= 6;
    }
    s = Math.min(1, s * sat); l = Math.min(1, Math.max(0, (l - 0.5) * con + 0.5));
    const q = l < 0.5 ? l * (1 + s) : l + s - l * s, p = 2 * l - q;
    const c = t => { t = (t + 1) % 1; return t < 1 / 6 ? p + (q - p) * 6 * t : t < 1 / 2 ? q : t < 2 / 3 ? p + (q - p) * (2 / 3 - t) * 6 : p; };
    const [rr, gg, bb] = s ? [c(h + 1 / 3), c(h), c(h - 1 / 3)] : [l, l, l];
    px[i] = Math.round(rr * 255); px[i + 1] = Math.round(gg * 255); px[i + 2] = Math.round(bb * 255);
  }
  return escribirPNG(img);
}
async function realzarCarpeta(carpeta) {
  const infoRuta = path.join(carpeta, 'info.json');
  const info = JSON.parse(await fs.readFile(infoRuta, 'utf8').catch(() => 'null'));
  if (!info || info.realzado) return 0;
  let n = 0;
  for (const f of await fs.readdir(carpeta)) if (f.endsWith('.png')) { const r = path.join(carpeta, f); await fs.writeFile(r, realzar(await fs.readFile(r))); n++; }
  info.realzado = true;
  await fs.writeFile(infoRuta, JSON.stringify(info));
  return n;
}

// Quita los márgenes transparentes: así el dibujo ocupa solo lo que se ve y se apoya en su base.
function recortar(buf) {
  const img = leerPNG(buf); if (!img) return buf;
  const { w, h, px } = img;
  let x0 = w, y0 = h, x1 = -1, y1 = -1;
  for (let y = 0; y < h; y++) for (let x = 0; x < w; x++) if (px[(y * w + x) * 4 + 3] > 20) {
    if (x < x0) x0 = x; if (x > x1) x1 = x; if (y < y0) y0 = y; if (y > y1) y1 = y;
  }
  if (x1 < 0 || (x0 === 0 && y0 === 0 && x1 === w - 1 && y1 === h - 1)) return buf;
  const nw = x1 - x0 + 1, nh = y1 - y0 + 1, out = Buffer.alloc(nw * nh * 4);
  for (let y = 0; y < nh; y++) px.copy(out, y * nw * 4, ((y + y0) * w + x0) * 4, ((y + y0) * w + x1 + 1) * 4);
  return escribirPNG({ w: nw, h: nh, px: out });
}

// ---------- tilesets Wang (16 tiles por esquinas): create-tileset, asíncrono ----------
async function generarTileset(t) {
  const destino = dir(`tilesets/${t.id}.json`);
  let id = estado[`tileset:${t.id}`];
  if (!id || FORZAR) {
    // "referencia_inferior": "<otro tileset>" usa su césped como referencia, para que los terrenos combinen.
    let referencia = null;
    if (t.referencia_inferior) {
      const otro = JSON.parse(await fs.readFile(dir(`tilesets/${t.referencia_inferior}.json`), 'utf8'));
      referencia = { type: 'base64', base64: otro.tiles[0].split(',')[1], format: 'png' };
    }
    const { datos } = await api('POST', '/create-tileset', {
      ...(referencia ? { lower_reference_image: referencia } : {}),
      lower_description: t.inferior,
      upper_description: t.superior,
      transition_description: t.transicion || '',
      ...(t.transicion_tamano ? { transition_size: t.transicion_tamano } : {}),
      tile_size: { width: contenido.mundo.tile, height: contenido.mundo.tile },
      shading: estilo.shading,
      detail: estilo.detail,
      lower_walkable: true,
      upper_walkable: t.superior_caminable !== false,
    });
    id = estado[`tileset:${t.id}`] = datos.tileset_id;
    await guardarEstado();
  }
  let res;
  for (;;) {
    res = await api('GET', `/tilesets/${id}`);
    if (!res.pendiente) break;
    await esperar(8000);
  }
  // índice Wang = NW*8 + NE*4 + SW*2 + SE*1  (1 = terreno superior).
  // A veces PixelLab pinta los dos terrenos al revés: "invertir": true en contenido.json lo corrige.
  const bit = c => ((c === 'upper') !== !!t.invertir ? 1 : 0);
  const tiles = new Array(16);
  for (const tile of res.datos.tileset.tiles) {
    const c = tile.corners;
    const i = bit(c.NW) * 8 + bit(c.NE) * 4 + bit(c.SW) * 2 + bit(c.SE);
    tiles[i] = `data:image/png;base64,${tile.image.base64}`;
  }
  await fs.mkdir(path.dirname(destino), { recursive: true });
  await fs.writeFile(destino, JSON.stringify({ id: t.id, tile: contenido.mundo.tile, tiles }));
}

// ---------- personaje: 4 direcciones + animación de caminar ----------
const DIRS = ['south', 'west', 'east', 'north'];
async function generarPersonaje(p) {
  const clave = `personaje:${p.id}`;
  let cid = estado[clave];
  if (!cid || FORZAR) {
    if (p.basadoEn) {
      // Variante de otro personaje: mismo estilo de dibujo, proporciones y paleta.
      // si el personaje de origen se está generando en paralelo, se espera a que exista
      for (let k = 0; k < 120 && !(await existe(dir(`personaje/${p.basadoEn}/info.json`))); k++) await esperar(5000);
      const origen = estado[`personaje:${p.basadoEn}`];
      if (!origen) throw new Error(`primero hay que generar ${p.basadoEn}`);
      const { datos } = await api('POST', '/create-character-state', {
        character_id: origen, edit_description: p.edicion, state_name: p.id, no_background: true, use_color_palette_from_reference: true,
      });
      cid = datos.character_id;
    } else {
      const { datos } = await api('POST', '/create-character-with-4-directions', {
        description: p.descripcion,
        image_size: { width: p.tamano, height: p.tamano },
        view: 'high top-down',
        outline: estilo.outline,
        shading: estilo.shading,
        detail: estilo.detail,
      });
      cid = datos.character_id;
    }
    estado[clave] = cid;
    delete estado[`${clave}:anim`];
    await guardarEstado();
  }
  // ?states= limita el ZIP a este personaje (si no, trae todo su grupo de variantes)
  await animarYDescargar(p, cid, `${clave}:anim`, dir(`personaje/${p.id}`), `?states=${cid}`);
}

// ---------- ropas: variantes (estados) del mismo personaje + su animación de caminar ----------
// Cada ropa es un "estado" de PixelLab: misma persona, otra ropa. Se guarda en
// assets/personaje/<id>/ropa_<ropa>/ con los mismos nombres de archivo que la base.
async function generarRopa(p, r) {
  const base = estado[`personaje:${p.id}`];
  if (!base) throw new Error('primero hay que generar el personaje base');
  const carpeta = dir(`personaje/${p.id}/ropa_${r.id}`);
  const clave = `ropa:${p.id}:${r.id}`;
  let sid = estado[clave];
  if (!sid || FORZAR) {
    const { datos } = await api('POST', '/create-character-state', {
      character_id: base, edit_description: r.edicion, state_name: r.id, no_background: true,
    });
    sid = estado[clave] = datos.character_id;
    delete estado[`${clave}:anim`];
    await guardarEstado();
  }
  await animarYDescargar(p, sid, `${clave}:anim`, carpeta, `?states=${sid}`);
}

// Espera a que el personaje (o estado) termine, lo anima con la plantilla y baja el ZIP.
async function animarYDescargar(p, cid, claveAnim, carpeta, consultaZip = '') {
  let info;
  for (;;) {
    info = (await api('GET', `/characters/${cid}`)).datos;
    if (info.status === 'completed' && info.rotation_urls) break;
    if (info.status === 'failed') throw new Error(`PixelLab no pudo crear ${cid}`);
    await esperar(8000);
  }
  if (!estado[claveAnim]) {
    const { datos } = await api('POST', '/animate-character', { character_id: cid, template_animation_id: p.animacion, directions: DIRS });
    estado[claveAnim] = datos.animation_group_id || true;
    await guardarEstado();
  }
  for (;;) {
    info = (await api('GET', `/characters/${cid}`)).datos;
    const anim = (info.animations || []).find(a => a.animation_group_id === estado[claveAnim] || a.animation_type === p.animacion);
    if (anim && DIRS.every(d => anim.directions.find(x => x.direction === d && x.frames?.length))) {
      const zip = await apiBinario(`/characters/${cid}/zip${consultaZip}`);
      if (zip) {
        const archivos = leerZip(zip), buscar = re => Object.keys(archivos).filter(n => re.test(n)).sort();
        await fs.mkdir(carpeta, { recursive: true });
        const cuadros = {};
        for (const d of DIRS) {
          const rot = buscar(new RegExp(`(^|/)rotations/${d}\\.png$`))[0];
          if (!rot) throw new Error(`el ZIP no trae la rotación ${d}`);
          await fs.writeFile(`${carpeta}/${d}.png`, limpiarBordes(archivos[rot]));
          const frames = buscar(new RegExp(`(^|/)animations/[^/]+/${d}/[^/]+\\.png$`));
          cuadros[d] = frames.length;
          for (let i = 0; i < frames.length; i++) await fs.writeFile(`${carpeta}/caminar_${d}_${i}.png`, limpiarBordes(archivos[frames[i]]));
        }
        await fs.writeFile(`${carpeta}/info.json`, JSON.stringify({ tamano: info.size, cuadros }));
        await realzarCarpeta(carpeta);
        return;
      }
    }
    await esperar(8000);
  }
}

// ---------- manifest: qué gráficos existen (el juego usa provisionales para el resto) ----------
async function escribirManifest() {
  const m = { sprites: {}, tilesets: {}, personajes: {} };
  for (const id of Object.keys(contenido.sprites))
    if (await existe(dir(`sprites/${id}.png`))) m.sprites[id] = `assets/sprites/${id}.png`;
  for (const t of contenido.tilesets)
    if (await existe(dir(`tilesets/${t.id}.json`))) m.tilesets[t.id] = `assets/tilesets/${t.id}.json`;
  for (const p of contenido.personajes) {
    const infoRuta = dir(`personaje/${p.id}/info.json`);
    if (!(await existe(infoRuta))) continue;
    const info = JSON.parse(await fs.readFile(infoRuta, 'utf8'));
    const pm = m.personajes[p.id] = { carpeta: `assets/personaje/${p.id}`, ...info, ropas: {} };
    for (const r of contenido.ropas || []) {
      const ri = dir(`personaje/${p.id}/ropa_${r.id}/info.json`);
      if (await existe(ri)) pm.ropas[r.id] = { carpeta: `assets/personaje/${p.id}/ropa_${r.id}`, ...JSON.parse(await fs.readFile(ri, 'utf8')) };
    }
  }
  await fs.writeFile(dir('manifest.json'), JSON.stringify(m, null, 2));
  return m;
}

// ---------- principal ----------
if (args.realzar) {   // realce de color de los personajes ya generados (una sola vez por carpeta)
  let n = 0;
  for (const p of contenido.personajes) {
    n += await realzarCarpeta(dir(`personaje/${p.id}`));
    for (const r of contenido.ropas || []) n += await realzarCarpeta(dir(`personaje/${p.id}/ropa_${r.id}`));
  }
  console.log(`Realzadas ${n} imágenes de personajes`);
  process.exit(0);
}
if (args.limpiar) {   // limpia los bordes de lo ya generado, sin llamar a la API
  const carpetas = [dir('sprites')];
  for (const p of contenido.personajes) {
    carpetas.push(dir(`personaje/${p.id}`));
    for (const r of contenido.ropas || []) carpetas.push(dir(`personaje/${p.id}/ropa_${r.id}`));
  }
  let n = 0;
  for (const c of carpetas) for (const f of await fs.readdir(c).catch(() => [])) {
    if (!f.endsWith('.png')) continue;
    const ruta = path.join(c, f), antes = await fs.readFile(ruta);
    let despues = limpiarBordes(antes);
    if (c === dir('sprites') && contenido.sprites[f.slice(0, -4)]?.recortar) despues = recortar(despues);
    if (despues !== antes) { await fs.writeFile(ruta, despues); n++; }
  }
  console.log(`Bordes limpiados en ${n} imágenes`);
  process.exit(0);
}
const tareas = [];
for (const [id, s] of Object.entries(contenido.sprites))
  if (quiere(id) && (FORZAR || !(await existe(dir(`sprites/${id}.png`)))))
    tareas.push({ tipo: 'sprite', id, costo: 1, run: () => generarSprite(id, s) });
for (const t of contenido.tilesets)
  if (quiere(t.id) && (FORZAR || !(await existe(dir(`tilesets/${t.id}.json`)))))
    tareas.push({ tipo: 'tileset', id: t.id, costo: 1, run: () => generarTileset(t) });
// Cada personaje va en una sola tarea: primero la base y después sus ropas (que parten de la base).
for (const p of contenido.personajes) {
  const faltaBase = quiere(p.id) && (FORZAR || !(await existe(dir(`personaje/${p.id}/info.json`))));
  const ropas = [];
  for (const r of contenido.ropas || [])
    if ((quiere(p.id) || quiere(`ropa_${r.id}`)) && (FORZAR || !(await existe(dir(`personaje/${p.id}/ropa_${r.id}/info.json`))))) ropas.push(r);
  if (!faltaBase && !ropas.length) continue;
  tareas.push({
    tipo: 'personaje', id: `${p.id}${ropas.length ? ` + ropas ${ropas.map(r => r.id).join(', ')}` : ''}`,
    costo: (faltaBase ? 5 : 0) + 5 * ropas.length,
    run: async () => { if (faltaBase) await generarPersonaje(p); for (const r of ropas) await generarRopa(p, r); },
  });
}

console.log(`Por generar: ${tareas.length} (≈${tareas.reduce((a, t) => a + t.costo, 0)} generaciones)`);
for (const t of tareas) console.log(`  · ${t.tipo.padEnd(9)} ${t.id}`);
if (LISTA || !tareas.length) { await escribirManifest(); process.exit(0); }
if (!TOKEN) { console.error('Falta PIXELLAB_API_KEY'); process.exit(1); }

const errores = [];
const cola = [...tareas];
await Promise.all(Array.from({ length: PARALELO }, async () => {
  while (cola.length) {
    const t = cola.shift();
    try { await t.run(); console.log(`✓ ${t.tipo} ${t.id}`); }
    catch (e) { errores.push(t.id); console.error(`✗ ${t.tipo} ${t.id}: ${e.message}`); }
  }
}));
const m = await escribirManifest();
console.log(`Listo. Sprites: ${Object.keys(m.sprites).length}, tilesets: ${Object.keys(m.tilesets).length}, personajes: ${Object.keys(m.personajes).join(', ') || 'ninguno'}`);
if (errores.length) { console.error('Fallaron:', errores.join(', ')); process.exit(1); }
