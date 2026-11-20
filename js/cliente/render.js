// Dibuja el estado. Solo lee: nunca modifica el juego.
// Dibuja a todos los jugadores del estado, así las visitas y el modo en equipo ya se ven.
import { idx, dentro } from '../nucleo/mundo.js';
import { celdaFrente } from '../nucleo/reglas.js';

export function crearRender(el, def, G) {
  const { T, W, H } = def, g = el.getContext('2d');
  let escala = 2, camX = 0, camY = 0, tLluvia = 0;
  const flotantes = [], sacudidas = new Map(), animJug = new Map();

  function ajustar() {
    const dpr = devicePixelRatio || 1;
    el.width = Math.round(innerWidth * dpr); el.height = Math.round(innerHeight * dpr);
    escala = Math.max(1, Math.min(4, Math.floor(Math.min(el.width / (12 * T), el.height / (8 * T)) * 2) / 2));
  }
  addEventListener('resize', ajustar); ajustar();

  function eventos(ev) {
    for (const e of ev) {
      if (e.tipo === 'flotar') flotantes.push({ x: e.x, y: e.y, txt: e.txt, color: e.color || '#f3eedb', t: 0 });
      if (e.tipo === 'sacudir') sacudidas.set(e.uid, 0.18);
    }
  }

  function actualizar(dt, estado) {
    for (const [k, v] of sacudidas) { if (v - dt <= 0) sacudidas.delete(k); else sacudidas.set(k, v - dt); }
    for (let i = flotantes.length - 1; i >= 0; i--) { flotantes[i].t += dt; if (flotantes[i].t > 1.2) flotantes.splice(i, 1); }
    // animación de caminar según cuánto se movió cada jugador desde el cuadro anterior
    for (const j of Object.values(estado.jugadores)) {
      const a = animJug.get(j.id) || { x: j.x, y: j.y, t: 0, andando: false };
      a.andando = Math.hypot(j.x - a.x, j.y - a.y) > 0.01;
      if (a.andando) a.t += dt;
      a.x = j.x; a.y = j.y; animJug.set(j.id, a);
    }
    tLluvia += dt;
  }

  function sprite(img, cx, basey, sac = 0) {
    const off = sac > 0 ? Math.sin(sac * 80) * 2 : 0;
    g.drawImage(img, Math.round(cx - img.width / 2 + off), Math.round(basey - img.height));
  }

  function dibujar(estado, yoId, { objetivo = true } = {}) {
    const gr = estado.granja, yo = estado.jugadores[yoId];
    const esp = (x, y, k) => (dentro(def, x, y) ? gr[k][idx(def, x, y)] : 0);
    const vw = el.width / escala, vh = el.height / escala;
    camX = W * T < vw ? (W * T - vw) / 2 : Math.max(0, Math.min(W * T - vw, yo.x - vw / 2));
    camY = H * T < vh ? (H * T - vh) / 2 : Math.max(0, Math.min(H * T - vh, yo.y - vh / 2));
    camX = Math.round(camX); camY = Math.round(camY);
    g.setTransform(1, 0, 0, 1, 0, 0); g.imageSmoothingEnabled = false;
    g.fillStyle = '#161b17'; g.fillRect(0, 0, el.width, el.height);
    g.setTransform(escala, 0, 0, escala, -camX * escala, -camY * escala);
    const x0 = Math.max(0, Math.floor(camX / T)), y0 = Math.max(0, Math.floor(camY / T));
    const x1 = Math.min(W - 1, Math.ceil((camX + vw) / T)), y1 = Math.min(H - 1, Math.ceil((camY + vh) / T));

    // suelo: césped + capas Wang en rejilla doble (cada tile une las esquinas de 4 celdas)
    const cesped = G.TS.tierra[0];
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) g.drawImage(cesped, x * T, y * T, T, T);
    const capa = (ts, k) => {
      for (let j = y0 - 1; j <= y1; j++) for (let i = x0 - 1; i <= x1; i++) {
        const n = esp(i, j, k) * 8 + esp(i + 1, j, k) * 4 + esp(i, j + 1, k) * 2 + esp(i + 1, j + 1, k);
        if (n) g.drawImage(ts[n], i * T + T / 2, j * T + T / 2, T, T);
      }
    };
    capa(G.TS.agua, 'agua'); capa(G.TS.tierra, 'suelo');
    g.fillStyle = 'rgba(38,22,10,.38)';
    const mojada = (x, y) => esp(x, y, 'regado') && esp(x, y, 'suelo');
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      if (!mojada(x, y)) continue;
      const l = mojada(x - 1, y), r = mojada(x + 1, y), u = mojada(x, y - 1), d = mojada(x, y + 1), m = 5;
      g.fillRect(x * T + (l ? 0 : m), y * T + (u ? 0 : m), T - (l ? 0 : m) - (r ? 0 : m), T - (u ? 0 : m) - (d ? 0 : m));
    }

    if (objetivo) {
      const [fx, fy] = celdaFrente(def, yo);
      g.strokeStyle = 'rgba(242,193,78,.85)'; g.lineWidth = 1; g.setLineDash([3, 2]);
      g.strokeRect(fx * T + 1.5, fy * T + 1.5, T - 3, T - 3); g.setLineDash([]);
    }

    // entidades ordenadas por la línea de su base
    const ents = [];
    for (const [k, c] of Object.entries(gr.cultivos)) {
      const x = k % W, y = Math.floor(k / W);
      if (x < x0 - 1 || x > x1 + 1 || y < y0 - 1 || y > y1 + 3) continue;
      ents.push([(y + 1) * T - 2, () => {
        const d = def.CULT[c.id], n = c.dias;
        if (n === 0) { g.fillStyle = '#2b1a0e'; for (const [a, b] of [[11, 14], [18, 12], [15, 19], [21, 18]]) g.fillRect(x * T + a, y * T + b, 2, 2); return; }
        const sp = n >= d.dias ? d.sprites[2] : n >= Math.ceil(d.dias / 2) ? d.sprites[1] : d.sprites[0];
        sprite(G.SPR[sp], (x + 0.5) * T, (y + 1) * T - 2);
        if (n >= d.dias) { g.fillStyle = 'rgba(242,193,78,.9)'; g.fillRect((x + 0.5) * T - 1, y * T - 6 + Math.sin(performance.now() / 250) * 1.5, 2, 3); }
      }]);
    }
    for (const o of gr.objetos) {
      const d = def.OBJ[o.id], w = d.ancho || 1, h = d.alto || 1;
      if (o.x + w < x0 - 2 || o.x > x1 + 2 || o.y > y1 + 1 || o.y + h < y0 - 4) continue;
      ents.push([(o.y + h) * T, () => {
        sprite(G.SPR[d.sprite], (o.x + w / 2) * T, (o.y + h) * T, sacudidas.get(o.uid) || 0);
        if (d.accion === 'maquina' && o.datos.salida && o.datos.listo <= estado.tiempo.dia)
          g.drawImage(G.SPR.conserva, (o.x + 0.5) * T - 8, o.y * T - 18 + Math.sin(performance.now() / 250) * 2, 16, 16);
      }]);
    }
    for (const j of Object.values(estado.jugadores)) {
      const a = animJug.get(j.id), cuadros = G.PJ.caminar[j.dir];
      const img = a?.andando && cuadros.length ? cuadros[Math.floor(a.t * 9) % cuadros.length] : G.PJ.quieto[j.dir];
      ents.push([j.y, () => {
        g.fillStyle = 'rgba(0,0,0,.22)'; g.beginPath(); g.ellipse(j.x, j.y, 9, 3.5, 0, 0, 7); g.fill();
        g.drawImage(img, Math.round(j.x - img.width / 2), Math.round(j.y - G.PJ.pie * (img.height / G.PJ.alto)));
        if (j.id !== yoId) {   // nombre sobre los demás jugadores
          g.font = '600 8px "Pixelify Sans", monospace'; g.textAlign = 'center';
          g.fillStyle = '#000'; g.fillText(j.nombre, j.x + 1, j.y - G.PJ.pie + 1);
          g.fillStyle = '#f2c14e'; g.fillText(j.nombre, j.x, j.y - G.PJ.pie);
        }
      }]);
    }
    ents.sort((a, b) => a[0] - b[0]).forEach(e => e[1]());

    g.font = '600 9px "Pixelify Sans", monospace'; g.textAlign = 'center';
    for (const f of flotantes) {
      g.globalAlpha = Math.max(0, 1 - f.t / 1.2);
      g.fillStyle = '#000'; g.fillText(f.txt, f.x + 1, f.y - f.t * 18 + 1);
      g.fillStyle = f.color; g.fillText(f.txt, f.x, f.y - f.t * 18);
    }
    g.globalAlpha = 1;

    g.setTransform(1, 0, 0, 1, 0, 0);
    if (estado.tiempo.lluvia) {
      g.strokeStyle = 'rgba(170,200,255,.35)'; g.lineWidth = escala; g.beginPath();
      for (let k = 0; k < 90; k++) {
        const x = ((k * 97.3 + tLluvia * 260) % (el.width + 60)) - 30, y = ((k * 53.1 + tLluvia * 900 + k * k) % (el.height + 40)) - 20;
        g.moveTo(x, y); g.lineTo(x - 4 * escala, y + 12 * escala);
      }
      g.stroke();
      g.fillStyle = 'rgba(30,40,70,.12)'; g.fillRect(0, 0, el.width, el.height);
    }
    if (estado.tiempo.min > 17 * 60) {
      g.fillStyle = `rgba(16,20,58,${Math.min(0.5, (estado.tiempo.min - 17 * 60) / 300 * 0.5)})`;
      g.fillRect(0, 0, el.width, el.height);
    }
  }

  // Pantalla → celda del mundo (para tocar o hacer clic).
  const aCelda = (cx, cy) => {
    const dpr = devicePixelRatio || 1;
    return [Math.floor((cx * dpr / escala + camX) / T), Math.floor((cy * dpr / escala + camY) / T)];
  };

  return { eventos, actualizar, dibujar, aCelda };
}
