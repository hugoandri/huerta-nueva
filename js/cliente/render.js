// Dibuja el estado. Solo lee: nunca modifica el juego.
// Dibuja la zona del jugador local (granja o piso de mazmorra) y a todos los jugadores en ella.
import { idx, dentro, zonaDe, ropaActual } from '../nucleo/mundo.js';
import { celdaFrente, efecto, maquinaLista } from '../nucleo/reglas.js';
import { lienzo } from './graficos.js';

export function crearRender(el, def, G) {
  const { T, W, H } = def, g = el.getContext('2d');
  let escala = 2, camX = 0, camY = 0, tGlobal = 0;
  const flotantes = [], sacudidas = new Map(), animJug = new Map(), golpes = [];
  let sombra = lienzo(1, 1), sg = sombra.getContext('2d'), herido = 0;

  function ajustar() {
    const dpr = devicePixelRatio || 1;
    el.width = Math.round(innerWidth * dpr); el.height = Math.round(innerHeight * dpr);
    escala = Math.max(1, Math.min(4, Math.floor(Math.min(el.width / (12 * T), el.height / (8 * T)) * 2) / 2));
    sombra = lienzo(el.width, el.height); sg = sombra.getContext('2d');
  }
  addEventListener('resize', ajustar); ajustar();

  function eventos(ev, yoId) {
    for (const e of ev) {
      if (e.tipo === 'flotar') flotantes.push({ x: e.x, y: e.y, txt: e.txt, color: e.color || '#f3eedb', t: 0 });
      if (e.tipo === 'sacudir') sacudidas.set(e.uid, 0.18);
      if (e.tipo === 'golpe') golpes.push({ x: e.x, y: e.y, t: 0 });
      if (e.tipo === 'dano' && e.jugador === yoId) herido = 0.35;
    }
  }

  function actualizar(dt, estado) {
    tGlobal += dt; herido = Math.max(0, herido - dt);
    for (const [k, v] of sacudidas) { if (v - dt <= 0) sacudidas.delete(k); else sacudidas.set(k, v - dt); }
    for (let i = flotantes.length - 1; i >= 0; i--) { flotantes[i].t += dt; if (flotantes[i].t > 1.2) flotantes.splice(i, 1); }
    for (let i = golpes.length - 1; i >= 0; i--) { golpes[i].t += dt; if (golpes[i].t > 0.2) golpes.splice(i, 1); }
    for (const j of Object.values(estado.jugadores)) {
      const a = animJug.get(j.id) || { x: j.x, y: j.y, t: 0, andando: false };
      a.andando = Math.hypot(j.x - a.x, j.y - a.y) > 0.01;
      if (a.andando) a.t += dt;
      a.x = j.x; a.y = j.y; animJug.set(j.id, a);
    }
  }

  function sprite(img, cx, basey, sac = 0) {
    const off = sac > 0 ? Math.sin(sac * 80) * 2 : 0;
    g.drawImage(img, Math.round(cx - img.width / 2 + off), Math.round(basey - img.height));
  }
  // Sprites según el personaje (granjera o granjero) y la ropa que lleva en el lugar donde está.
  const aspectoDe = j => {
    const juego = G.PJS[j.cuerpo] || Object.values(G.PJS)[0];
    return juego[def.ITEMS[ropaActual(j)]?.aspecto] || juego.base;
  };

  function dibujar(estado, yoId, { objetivo = true } = {}) {
    const yo = estado.jugadores[yoId], zona = zonaDe(estado, yo), cueva = zona.tipo === 'mazmorra';
    const esp = (x, y, k) => (dentro(def, x, y) ? zona[k][idx(def, x, y)] : 0);
    const vw = el.width / escala, vh = el.height / escala;
    camX = Math.round(W * T < vw ? (W * T - vw) / 2 : Math.max(0, Math.min(W * T - vw, yo.x - vw / 2)));
    camY = Math.round(H * T < vh ? (H * T - vh) / 2 : Math.max(0, Math.min(H * T - vh, yo.y - vh / 2)));
    g.setTransform(1, 0, 0, 1, 0, 0); g.imageSmoothingEnabled = false;
    g.fillStyle = cueva ? '#0b0a0c' : '#161b17'; g.fillRect(0, 0, el.width, el.height);
    g.setTransform(escala, 0, 0, escala, -camX * escala, -camY * escala);
    const x0 = Math.max(0, Math.floor(camX / T)), y0 = Math.max(0, Math.floor(camY / T));
    const x1 = Math.min(W - 1, Math.ceil((camX + vw) / T)), y1 = Math.min(H - 1, Math.ceil((camY + vh) / T));

    // suelo: tile base + capas Wang en rejilla doble (cada tile une las esquinas de 4 celdas)
    const capa = (ts, valor) => {
      for (let j = y0 - 1; j <= y1; j++) for (let i = x0 - 1; i <= x1; i++) {
        const n = valor(i, j) * 8 + valor(i + 1, j) * 4 + valor(i, j + 1) * 2 + valor(i + 1, j + 1);
        if (n) g.drawImage(ts[n], i * T + T / 2, j * T + T / 2, T, T);
      }
    };
    if (cueva) {
      const ts = G.TS[def.MAZ[zona.mazmorra]?.tileset] || G.TS.cueva, base = ts[0];
      for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) g.drawImage(base, x * T, y * T, T, T);
      capa(ts, (x, y) => (dentro(def, x, y) ? 1 - zona.muro[idx(def, x, y)] : 0));
    } else {
      const cesped = G.TS.tierra[0];
      for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) g.drawImage(cesped, x * T, y * T, T, T);
      capa(G.TS.agua, (x, y) => esp(x, y, 'agua'));
      capa(G.TS.tierra, (x, y) => esp(x, y, 'suelo'));
      g.fillStyle = 'rgba(38,22,10,.38)';
      const mojada = (x, y) => esp(x, y, 'regado') && esp(x, y, 'suelo');
      for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
        if (!mojada(x, y)) continue;
        const l = mojada(x - 1, y), r = mojada(x + 1, y), u = mojada(x, y - 1), d = mojada(x, y + 1), m = 5;
        g.fillRect(x * T + (l ? 0 : m), y * T + (u ? 0 : m), T - (l ? 0 : m) - (r ? 0 : m), T - (u ? 0 : m) - (d ? 0 : m));
      }
    }

    if (objetivo) {
      const [fx, fy] = celdaFrente(def, yo);
      g.strokeStyle = 'rgba(242,193,78,.85)'; g.lineWidth = 1; g.setLineDash([3, 2]);
      g.strokeRect(fx * T + 1.5, fy * T + 1.5, T - 3, T - 3); g.setLineDash([]);
    }

    // entidades ordenadas por la línea de su base
    const ents = [];
    for (const [k, c] of Object.entries(zona.cultivos)) {
      const x = k % W, y = Math.floor(k / W);
      if (x < x0 - 1 || x > x1 + 1 || y < y0 - 1 || y > y1 + 3) continue;
      ents.push([(y + 1) * T - 2, () => {
        const d = def.CULT[c.id], n = c.dias;
        if (n === 0) { g.fillStyle = '#2b1a0e'; for (const [a, b] of [[11, 14], [18, 12], [15, 19], [21, 18]]) g.fillRect(x * T + a, y * T + b, 2, 2); return; }
        const sp = n >= d.dias ? d.sprites[2] : n >= Math.ceil(d.dias / 2) ? d.sprites[1] : d.sprites[0];
        sprite(G.SPR[sp], (x + 0.5) * T, (y + 1) * T - 2);
        if (n >= d.dias) { g.fillStyle = 'rgba(242,193,78,.9)'; g.fillRect((x + 0.5) * T - 1, y * T - 6 + Math.sin(tGlobal * 4) * 1.5, 2, 3); }
      }]);
    }
    for (const o of zona.objetos) {
      const d = def.OBJ[o.id], w = d.ancho || 1, h = d.alto || 1;
      if (o.x + w < x0 - 2 || o.x > x1 + 2 || o.y > y1 + 1 || o.y + h < y0 - 4) continue;
      ents.push([(o.y + h) * T - (d.solido ? 0 : T), () => {
        const img = G.SPR[d.sprite];
        if (d.solido && img.width >= T) {   // sombra proyectada hacia abajo a la derecha: da volumen
          g.fillStyle = 'rgba(0,0,0,.22)';
          g.beginPath(); g.ellipse((o.x + w / 2) * T + w * 4, (o.y + h) * T - 3, img.width * 0.48, 6 + h * 3, 0, 0, 7); g.fill();
        }
        sprite(img, (o.x + w / 2) * T, (o.y + h) * T, sacudidas.get(o.uid) || 0);
        if (d.accion === 'maquina' && maquinaLista(estado, o))
          g.drawImage(G.SPR[def.ITEMS[o.datos.salida].sprite], (o.x + 0.5) * T - 8, (o.y + h) * T - img.height - 18 + Math.sin(tGlobal * 4) * 2, 16, 16);
      }]);
    }
    for (const m of zona.monstruos || []) {
      const M = def.MON[m.id], img = G.SPR[M.sprite];
      ents.push([m.y, () => {
        const bote = M.vuela ? Math.sin(tGlobal * 10 + m.uid) * 4 - 10 : 0;
        const aplasta = M.vuela ? 1 : 1 + Math.sin(tGlobal * 6 + m.uid) * 0.06;
        g.fillStyle = 'rgba(0,0,0,.28)'; g.beginPath(); g.ellipse(m.x, m.y, img.width * 0.3, 4, 0, 0, 7); g.fill();
        g.save();
        g.translate(Math.round(m.x), Math.round(m.y + bote));
        g.scale(m.dir < 0 ? -1 : 1, 1); g.scale(1 / aplasta, aplasta);
        if (m.golpe > 0) g.globalAlpha = 0.45;
        g.drawImage(img, -img.width / 2, -img.height + 4);
        g.restore();
        if (!M.jefe && m.vida < m.vidaMax) {
          g.fillStyle = '#000'; g.fillRect(m.x - 10, m.y + 4, 20, 3);
          g.fillStyle = '#e2674f'; g.fillRect(m.x - 10, m.y + 4, 20 * m.vida / m.vidaMax, 3);
        }
      }]);
    }
    for (const j of Object.values(estado.jugadores)) {
      if (zonaDe(estado, j) !== zona) continue;
      const P = aspectoDe(j), a = animJug.get(j.id), cuadros = P.caminar[j.dir];
      const img = a?.andando && cuadros.length ? cuadros[Math.floor(a.t * 9) % cuadros.length] : P.quieto[j.dir];
      ents.push([j.y, () => {
        g.fillStyle = 'rgba(0,0,0,.22)'; g.beginPath(); g.ellipse(j.x, j.y, 9, 3.5, 0, 0, 7); g.fill();
        if (j.id === yoId && herido > 0 && Math.floor(herido * 20) % 2) g.globalAlpha = 0.4;
        g.drawImage(img, Math.round(j.x - img.width / 2), Math.round(j.y - P.pie * (img.height / P.alto)));
        g.globalAlpha = 1;
        if (j.id !== yoId) {
          g.font = '600 8px "Pixelify Sans", monospace'; g.textAlign = 'center';
          g.fillStyle = '#000'; g.fillText(j.nombre, j.x + 1, j.y - P.pie + 1);
          g.fillStyle = '#f2c14e'; g.fillText(j.nombre, j.x, j.y - P.pie);
        }
      }]);
    }
    ents.sort((a, b) => a[0] - b[0]).forEach(e => e[1]());

    for (const h of golpes) {           // arco del golpe
      g.strokeStyle = `rgba(255,255,255,${1 - h.t / 0.2})`; g.lineWidth = 2;
      g.beginPath(); g.arc(h.x, h.y - 8, 10 + h.t * 40, -2.4, -0.7); g.stroke();
    }

    // ----- luz: oscuridad con agujeros alrededor de las fuentes de luz
    const min = estado.tiempo.min;
    const oscuridad = cueva ? 0.94 : min > 17 * 60 ? Math.min(0.72, (min - 17 * 60) / 300 * 0.72) : 0;
    if (oscuridad > 0) {
      const luces = [];
      const radioJug = (cueva ? 78 : 46) * (cueva ? (efecto(def, yo).luz || 1) : 1);
      for (const j of Object.values(estado.jugadores)) if (zonaDe(estado, j) === zona) luces.push([j.x, j.y - 16, j.id === yoId ? radioJug : 60, 0]);
      for (const o of zona.objetos) { const d = def.OBJ[o.id]; if (d.luz) luces.push([(o.x + (d.ancho || 1) / 2) * T, (o.y + 0.3) * T, d.luz, 1]); }
      if (!cueva) for (const o of zona.objetos) if (o.id === 'casa') luces.push([(o.x + 2) * T, (o.y + 2.6) * T, 70, 1]);
      sg.setTransform(1, 0, 0, 1, 0, 0);
      sg.globalCompositeOperation = 'source-over';
      sg.clearRect(0, 0, sombra.width, sombra.height);
      sg.fillStyle = `rgba(8,8,22,${oscuridad})`; sg.fillRect(0, 0, sombra.width, sombra.height);
      sg.setTransform(escala, 0, 0, escala, -camX * escala, -camY * escala);
      sg.globalCompositeOperation = 'destination-out';
      for (const [lx, ly, r, parpadea] of luces) {
        const rr = r * (parpadea ? 1 + Math.sin(tGlobal * 9 + lx) * 0.04 : 1);
        const gr = sg.createRadialGradient(lx, ly, rr * 0.15, lx, ly, rr);
        gr.addColorStop(0, 'rgba(0,0,0,1)'); gr.addColorStop(1, 'rgba(0,0,0,0)');
        sg.fillStyle = gr; sg.fillRect(lx - rr, ly - rr, rr * 2, rr * 2);
      }
      g.setTransform(1, 0, 0, 1, 0, 0);
      g.drawImage(sombra, 0, 0);
      // brillo cálido de las llamas
      g.setTransform(escala, 0, 0, escala, -camX * escala, -camY * escala);
      g.globalCompositeOperation = 'lighter';
      for (const [lx, ly, r, parpadea] of luces) if (parpadea) {
        const gr = g.createRadialGradient(lx, ly, 0, lx, ly, r * 0.6);
        gr.addColorStop(0, 'rgba(255,150,60,.22)'); gr.addColorStop(1, 'rgba(255,150,60,0)');
        g.fillStyle = gr; g.fillRect(lx - r, ly - r, r * 2, r * 2);
      }
      g.globalCompositeOperation = 'source-over';
    }

    // textos flotantes (encima de la oscuridad, para que se lean)
    g.setTransform(escala, 0, 0, escala, -camX * escala, -camY * escala);
    g.font = '600 9px "Pixelify Sans", monospace'; g.textAlign = 'center';
    for (const f of flotantes) {
      g.globalAlpha = Math.max(0, 1 - f.t / 1.2);
      g.fillStyle = '#000'; g.fillText(f.txt, f.x + 1, f.y - f.t * 18 + 1);
      g.fillStyle = f.color; g.fillText(f.txt, f.x, f.y - f.t * 18);
    }
    g.globalAlpha = 1;

    g.setTransform(1, 0, 0, 1, 0, 0);
    if (!cueva && estado.tiempo.lluvia) {
      g.strokeStyle = 'rgba(170,200,255,.35)'; g.lineWidth = escala; g.beginPath();
      for (let k = 0; k < 90; k++) {
        const x = ((k * 97.3 + tGlobal * 260) % (el.width + 60)) - 30, y = ((k * 53.1 + tGlobal * 900 + k * k) % (el.height + 40)) - 20;
        g.moveTo(x, y); g.lineTo(x - 4 * escala, y + 12 * escala);
      }
      g.stroke();
    }
    if (herido > 0) { g.fillStyle = `rgba(200,30,30,${herido * 0.5})`; g.fillRect(0, 0, el.width, el.height); }
  }

  const aCelda = (cx, cy) => {
    const dpr = devicePixelRatio || 1;
    return [Math.floor((cx * dpr / escala + camX) / T), Math.floor((cy * dpr / escala + camY) / T)];
  };

  return { eventos, actualizar, dibujar, aCelda };
}
