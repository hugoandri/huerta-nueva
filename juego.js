// Huerta Nueva — prototipo de granja. Todo el contenido viene de data/contenido.json;
// los gráficos, de assets/manifest.json (generados con PixelLab). Lo que falte se dibuja provisional.
'use strict';
(async function () {
  const $ = s => document.querySelector(s);
  const C = await fetch('data/contenido.json').then(r => r.json());
  let M = { sprites: {}, tilesets: {}, personaje: null };
  try { const r = await fetch('assets/manifest.json'); if (r.ok) M = await r.json(); } catch (e) { /* sin gráficos: provisionales */ }

  const T = C.mundo.tile, W = C.mundo.ancho, H = C.mundo.alto;
  const ITEMS = Object.fromEntries(C.items.map(i => [i.id, i]));
  const OBJ = Object.fromEntries(C.objetos.map(o => [o.id, o]));
  const CULT = Object.fromEntries(C.cultivos.map(c => [c.id, c]));
  const DIRS = ['south', 'west', 'east', 'north'];
  const VEC = { south: [0, 1], north: [0, -1], west: [-1, 0], east: [1, 0] };
  const INICIO_DIA = 6 * 60, FIN_DIA = 26 * 60, MIN_POR_SEG = 10 / 7;
  const CLAVE = 'huerta-nueva-v1';

  // ---------------- utilidades ----------------
  const cargarImg = src => new Promise(res => { const im = new Image(); im.onload = () => res(im); im.onerror = () => res(null); im.src = src; });
  function mulberry32(a) { return () => { a |= 0; a = a + 0x6D2B79F5 | 0; let t = Math.imul(a ^ a >>> 15, 1 | a); t = t + Math.imul(t ^ t >>> 7, 61 | t) ^ t; return ((t ^ t >>> 14) >>> 0) / 4294967296; }; }
  const hash = s => { let h = 0; for (const ch of s) h = (h * 31 + ch.charCodeAt(0)) >>> 0; return h; };
  const lienzo = (w, h) => { const c = document.createElement('canvas'); c.width = w; c.height = h; return c; };
  const entre = (a, b, r) => a + Math.floor(r() * (b - a + 1));
  const fmtHora = m => { const h = Math.floor(m / 60) % 24, mm = Math.floor(m % 60 / 10) * 10; return `${h}:${String(mm).padStart(2, '0')}`; };
  function rrect(g, x, y, w, h, r) { g.beginPath(); g.moveTo(x + r, y); g.arcTo(x + w, y, x + w, y + h, r); g.arcTo(x + w, y + h, x, y + h, r); g.arcTo(x, y + h, x, y, r); g.arcTo(x, y, x + w, y, r); g.closePath(); }

  // ---------------- gráficos ----------------
  function provisional(id, w, h) {
    const c = lienzo(w, h), g = c.getContext('2d'), m = Math.max(2, Math.round(Math.min(w, h) * 0.12));
    g.fillStyle = `hsl(${hash(id) % 360} 42% 50%)`; rrect(g, m, m, w - 2 * m, h - 2 * m, 5); g.fill();
    g.strokeStyle = '#141414'; g.lineWidth = 2; g.stroke();
    g.fillStyle = '#fff'; g.font = `bold ${Math.round(Math.min(w, h) * 0.42)}px monospace`;
    g.textAlign = 'center'; g.textBaseline = 'middle'; g.fillText(id[0].toUpperCase(), w / 2, h / 2 + 1);
    return c;
  }
  function tilesProvisionales(lo, up) {
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

  const SPR = {}, SPR_URL = {};
  let nReales = 0, nProv = 0;
  await Promise.all(Object.entries(C.sprites).map(async ([id, s]) => {
    const im = M.sprites[id] ? await cargarImg(M.sprites[id]) : null;
    if (im) { nReales++; SPR[id] = im; SPR_URL[id] = M.sprites[id]; }
    else { nProv++; const c = provisional(id, s.ancho, s.alto); SPR[id] = c; SPR_URL[id] = c.toDataURL(); }
  }));
  const COLOR_TS = { tierra: ['#5d9e3f', '#6b4a2f'], agua: ['#5d9e3f', '#3d7fc4'] };
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
    if (tiles) nReales++; else { nProv++; tiles = tilesProvisionales(...(COLOR_TS[t.id] || ['#5d9e3f', `hsl(${hash(t.id) % 360} 40% 40%)`])); }
    TS[t.id] = tiles;
  }

  // Personaje: 4 rotaciones + cuadros de caminar. Pie = fila opaca más baja (para apoyarlo en el suelo).
  const PJ = { quieto: {}, caminar: {}, pie: 0, ancho: 48, alto: 48, real: false };
  if (M.personaje) {
    try {
      for (const d of DIRS) {
        PJ.quieto[d] = await cargarImg(`${M.personaje.carpeta}/${d}.png`);
        PJ.caminar[d] = [];
        for (let i = 0; i < (M.personaje.cuadros?.[d] || 0); i++) {
          const im = await cargarImg(`${M.personaje.carpeta}/caminar_${d}_${i}.png`);
          if (im) PJ.caminar[d].push(im);
        }
      }
      PJ.real = DIRS.every(d => PJ.quieto[d]);
    } catch (e) { PJ.real = false; }
  }
  if (PJ.real) {
    nReales++;
    const im = PJ.quieto.south; PJ.ancho = im.width; PJ.alto = im.height;
    try {
      const c = lienzo(im.width, im.height), g = c.getContext('2d'); g.drawImage(im, 0, 0);
      const d = g.getImageData(0, 0, im.width, im.height).data;
      for (let y = im.height - 1; y >= 0 && !PJ.pie; y--) for (let x = 0; x < im.width; x++) if (d[(y * im.width + x) * 4 + 3] > 40) { PJ.pie = y + 1; break; }
    } catch (e) { /* lienzo bloqueado: se usa una estimación */ }
    if (!PJ.pie) PJ.pie = Math.round(im.height * 0.85);
  } else {
    nProv++;
    const figura = (dir, paso) => {
      const c = lienzo(48, 48), g = c.getContext('2d');
      const piernaA = paso === 1 ? -3 : paso === 3 ? 3 : 0;
      g.fillStyle = '#4a3222'; g.fillRect(18, 36 + piernaA, 5, 8); g.fillRect(25, 36 - piernaA, 5, 8);
      g.fillStyle = '#3f7d4a'; rrect(g, 15, 22, 18, 16, 3); g.fill();
      g.fillStyle = '#f0c9a0'; g.beginPath(); g.arc(24, 17, 7, 0, 7); g.fill();
      g.fillStyle = '#e3c46a'; g.fillRect(13, 10, 22, 4); g.fillRect(18, 5, 12, 6);
      g.fillStyle = '#222';
      if (dir === 'south') { g.fillRect(21, 17, 2, 2); g.fillRect(26, 17, 2, 2); }
      if (dir === 'east') g.fillRect(27, 17, 2, 2);
      if (dir === 'west') g.fillRect(19, 17, 2, 2);
      return c;
    };
    for (const d of DIRS) { PJ.quieto[d] = figura(d, 0); PJ.caminar[d] = [1, 0, 3, 0].map(p => figura(d, p)); }
    PJ.pie = 44;
  }
  $('#estado-assets').innerHTML = nProv === 0
    ? `Gráficos: <b>${nReales} de PixelLab</b> (personaje, ${Object.keys(TS).length} tilesets y ${Object.keys(SPR).length} sprites).`
    : `Gráficos: <b>${nReales} de PixelLab</b> · ${nProv} provisionales (genera los que faltan con <code>herramientas/generar-assets.mjs</code>).`;

  // ---------------- estado del mundo ----------------
  const idx = (x, y) => y * W + x;
  const dentro = (x, y) => x >= 0 && y >= 0 && x < W && y < H;
  let S = null;            // estado guardable
  let ocupa = null;        // índice de celda → uid de objeto
  let porUid = new Map();

  function reindexar() {
    ocupa = new Int32Array(W * H); porUid = new Map();
    for (const o of S.objetos) {
      porUid.set(o.uid, o);
      const def = OBJ[o.id], w = def.ancho || 1, h = def.alto || 1;
      for (let y = o.y; y < o.y + h; y++) for (let x = o.x; x < o.x + w; x++) if (dentro(x, y)) ocupa[idx(x, y)] = o.uid;
    }
  }
  const objetoEn = (x, y) => dentro(x, y) ? porUid.get(ocupa[idx(x, y)]) || null : null;
  function libre(x, y, w = 1, h = 1) {
    for (let yy = y; yy < y + h; yy++) for (let xx = x; xx < x + w; xx++)
      if (!dentro(xx, yy) || S.agua[idx(xx, yy)] || ocupa[idx(xx, yy)] || S.suelo[idx(xx, yy)] || S.cultivos[idx(xx, yy)]) return false;
    return true;
  }
  function colocar(id, x, y, datos) {
    const def = OBJ[id];
    if (!libre(x, y, def.ancho || 1, def.alto || 1)) return null;
    const o = { uid: S.sigUid++, id, x, y, golpes: def.golpes || 0, datos: datos || {} };
    S.objetos.push(o); reindexar(); return o;
  }
  function quitar(o) { S.objetos = S.objetos.filter(x => x !== o); reindexar(); }

  function nuevoMundo() {
    const r = mulberry32(C.mundo.semilla);
    S = {
      dia: 1, min: INICIO_DIA, dinero: C.inicio.dinero, energia: 100, energiaMax: 100, agua: 0,
      inv: Array(24).fill(null), sel: 0, sigUid: 1, objetos: [], cultivos: {}, envios: [], lluvia: false,
      suelo: new Uint8Array(W * H), regado: new Uint8Array(W * H),
      pj: { x: 7.5 * T, y: 7.6 * T, dir: 'south' },
    };
    S.agua = new Uint8Array(W * H);
    const capAgua = ITEMS.regadera?.capacidad || 20; S.aguaRegadera = capAgua;
    for (let y = 0; y < H; y++) for (let x = 0; x < W; x++) {
      const e = ((x - 35) / 6) ** 2 + ((y - 23) / 4.2) ** 2 + Math.sin(x * 1.7 + y) * 0.08;
      if (e < 1) S.agua[idx(x, y)] = 1;
    }
    reindexar();
    colocar('casa', 5, 3); colocar('caja_envios', 10, 5); colocar('puesto', 12, 4);
    const claro = (x, y) => x >= 3 && x <= 17 && y >= 2 && y <= 10;
    const cercaDeArbol = (x, y) => { for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) { const o = objetoEn(x + dx, y + dy); if (o && o.id === 'arbol') return true; } return false; };
    // borde de árboles
    for (let x = 0; x < W; x += 2) { colocar('arbol', x, 1); colocar('arbol', x + (x % 4 ? 1 : 0), H - 1); }
    for (let y = 3; y < H - 1; y += 2) { colocar('arbol', 0, y); colocar('arbol', W - 1, y); }
    const sembrar = (id, n) => {
      for (let k = 0, intentos = 0; k < n && intentos < 4000; intentos++) {
        const x = entre(2, W - 3, r), y = entre(3, H - 3, r);
        if (claro(x, y) || !libre(x, y)) continue;
        let cercaAgua = false;
        for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) if (dentro(x + dx, y + dy) && S.agua[idx(x + dx, y + dy)]) cercaAgua = true;
        if (cercaAgua && id !== 'maleza') continue;
        if (id === 'arbol' && cercaDeArbol(x, y)) continue;
        colocar(id, x, y); k++;
      }
    };
    sembrar('arbol', 26); sembrar('roca', 45); sembrar('maleza', 90);
    for (const [id, n] of C.inicio.inventario) agregar(id, n, true);
  }

  // ---------------- inventario ----------------
  const esHerramienta = id => ITEMS[id]?.tipo === 'herramienta';
  function agregar(id, n = 1, silencio = false) {
    for (const s of S.inv) if (s && s.id === id && !esHerramienta(id) && n > 0) { const k = Math.min(n, 999 - s.n); s.n += k; n -= k; }
    for (let i = 0; i < S.inv.length && n > 0; i++) if (!S.inv[i]) { const k = esHerramienta(id) ? 1 : Math.min(n, 999); S.inv[i] = { id, n: k }; n -= k; }
    if (n > 0 && !silencio) aviso('La mochila está llena', true);
    pintarBarra();
    return n;
  }
  const cuenta = id => S.inv.reduce((a, s) => a + (s && s.id === id ? s.n : 0), 0);
  function sacar(id, n) {
    for (let i = S.inv.length - 1; i >= 0 && n > 0; i--) { const s = S.inv[i]; if (s && s.id === id) { const k = Math.min(n, s.n); s.n -= k; n -= k; if (!s.n) S.inv[i] = null; } }
    pintarBarra();
  }
  const enMano = () => S.inv[S.sel];
  function gastarMano(n = 1) { const s = enMano(); if (!s) return; s.n -= n; if (s.n <= 0) S.inv[S.sel] = null; pintarBarra(); }

  // ---------------- interfaz ----------------
  const avisos = $('#avisos');
  function aviso(txt, malo = false) {
    const d = document.createElement('div'); d.className = 'aviso' + (malo ? ' malo' : ''); d.textContent = txt;
    avisos.appendChild(d); while (avisos.children.length > 4) avisos.firstChild.remove();
    setTimeout(() => d.remove(), 2400);
  }
  const flotantes = [];
  const flotar = (x, y, txt, color = '#f3eedb') => flotantes.push({ x, y, txt, color, t: 0 });

  const iconoHTML = (id, extra = '') => `<img src="${SPR_URL[ITEMS[id].sprite]}" alt="" ${extra}>`;
  function casillaHTML(s, i, conTecla) {
    const tecla = conTecla ? `<span class="tecla">${i === 9 ? 0 : i === 10 ? '-' : i === 11 ? '=' : i + 1}</span>` : '';
    if (!s) return `<button class="casilla" data-i="${i}" aria-label="Casilla vacía">${tecla}</button>`;
    const it = ITEMS[s.id];
    return `<button class="casilla" data-i="${i}" title="${it.nombre}" aria-label="${it.nombre}${s.n > 1 ? ', ' + s.n : ''}">${tecla}${iconoHTML(s.id)}${s.n > 1 ? `<span class="n">${s.n}</span>` : ''}</button>`;
  }
  const barra = $('#barra');
  function pintarBarra() {
    if (!S) return;
    barra.innerHTML = S.inv.slice(0, 12).map((s, i) => casillaHTML(s, i, true)).join('');
    barra.children[S.sel]?.classList.add('sel');
    const s = enMano();
    $('#nombre-sel').textContent = s ? ITEMS[s.id].nombre + (s.id === 'regadera' ? ` · agua ${S.aguaRegadera}/${ITEMS.regadera.capacidad}` : '') : 'Mano vacía';
    if (!$('#velo').hidden && panelActual?.refrescar) panelActual.refrescar();
  }
  barra.addEventListener('click', e => { const b = e.target.closest('.casilla'); if (b) { S.sel = +b.dataset.i; pintarBarra(); } });

  let hudCache = '';
  function pintarHUD() {
    const dia = `Primavera · día ${(S.dia - 1) % 28 + 1}${S.dia > 28 ? ` · año ${Math.floor((S.dia - 1) / 28) + 1}` : ''}`;
    const clave = dia + fmtHora(S.min) + S.dinero + Math.round(S.energia) + S.aguaRegadera + S.lluvia;
    if (clave === hudCache) return; hudCache = clave;
    $('#hud-dia').textContent = dia;
    $('#hud-hora').textContent = fmtHora(S.min);
    $('#hud-clima').textContent = S.lluvia ? 'Lluvia: todo regado' : '';
    $('#hud-dinero').textContent = S.dinero.toLocaleString('es');
    const pe = S.energia / S.energiaMax, pa = S.aguaRegadera / (ITEMS.regadera?.capacidad || 20);
    $('#m-energia .relleno').style.height = pe * 100 + '%'; $('#m-energia').classList.toggle('bajo', pe < 0.2);
    $('#m-agua .relleno').style.height = pa * 100 + '%';
  }

  // ---------------- paneles ----------------
  const velo = $('#velo'), panel = $('#panel');
  let panelActual = null;
  function abrir(def) { panelActual = def; velo.hidden = false; def.pintar(); panel.querySelector('button')?.focus({ preventScroll: true }); }
  function cerrar() { velo.hidden = true; panelActual = null; lienzoEl.focus?.(); }
  velo.addEventListener('pointerdown', e => { if (e.target === velo && panelActual?.cerrable !== false) cerrar(); });
  const cab = (titulo, pestanas = '') => `<div class="panel-cab"><h2>${titulo}</h2>${pestanas}<button class="btn cerrar" data-cerrar aria-label="Cerrar">✕</button></div>`;
  panel.addEventListener('click', e => { if (e.target.closest('[data-cerrar]')) cerrar(); });

  function puedeFabricar(r) { return Object.entries(r.ingredientes).every(([id, n]) => cuenta(id) >= n); }
  let pestanaMenu = 'mochila', marcada = -1;
  function menu(pestana) {
    if (pestana) pestanaMenu = pestana;
    const def = {
      pintar() {
        const tabs = ['mochila', 'crafteo', 'ayuda'].map(t => `<button class="btn ${pestanaMenu === t ? 'activo' : ''}" data-tab="${t}">${{ mochila: 'Mochila', crafteo: 'Crafteo', ayuda: 'Cómo jugar' }[t]}</button>`).join('');
        let cuerpo = '';
        if (pestanaMenu === 'mochila') {
          cuerpo = `<p class="ayuda">Toca una casilla y luego otra para moverla. Las 12 primeras son tu barra.</p>
            <div class="grilla" id="g-inv">${S.inv.map((s, i) => casillaHTML(s, i, false)).join('')}</div>`;
        } else if (pestanaMenu === 'crafteo') {
          cuerpo = `<p class="ayuda">Las recetas salen de <code>data/contenido.json</code>. Lo fabricado se coloca en el mundo con <kbd>Espacio</kbd>.</p><div class="lista">` +
            C.recetas.map((r, i) => {
              const it = ITEMS[r.resultado];
              const ing = Object.entries(r.ingredientes).map(([id, n]) => `<span class="ing ${cuenta(id) < n ? 'falta' : ''}">${iconoHTML(id)}${cuenta(id)}/${n}</span>`).join('');
              return `<div class="fila">${iconoHTML(r.resultado)}<div><div class="titulo">${it.nombre}${r.cantidad > 1 ? ' ×' + r.cantidad : ''}</div><div class="det">${it.descripcion || ''}</div><div style="margin-top:4px">${ing}</div></div>
                <div class="acciones"><button class="btn primario" data-fab="${i}" ${puedeFabricar(r) ? '' : 'disabled'}>Fabricar</button></div></div>`;
            }).join('') + '</div>';
        } else {
          cuerpo = `<p class="ayuda">Cada día tienes energía y horas limitadas. A las 2:00 te desmayas y pierdes algo de dinero.</p>
            <dl class="controles">
              <dt>Azada</dt><dd>Ara el césped. No se puede arar junto al estanque.</dd>
              <dt>Semillas</dt><dd>Siémbralas en tierra arada. Riega cada día para que crezcan.</dd>
              <dt>Regadera</dt><dd>Riega la tierra. Rellénala usándola frente al agua.</dd>
              <dt>Hacha · pico</dt><dd>Madera de los árboles, piedra de las rocas, fibra de la maleza. También recogen lo que colocaste.</dd>
              <dt>Caja de envíos</dt><dd>Usa un objeto frente a ella para venderlo. Cobras al dormir.</dd>
              <dt>Tarro</dt><dd>Mete un cultivo y al día siguiente tendrás una conserva que vale mucho más.</dd>
              <dt>Cuervos</dt><dd>Con 8 o más cultivos sin espantapájaros cerca, alguno puede desaparecer de noche.</dd>
              <dt>Casa</dt><dd>Úsala para dormir y pasar al día siguiente. Se guarda la partida.</dd>
            </dl>
            <button class="btn" id="btn-reiniciar">Borrar partida y empezar de nuevo</button>`;
        }
        panel.innerHTML = cab('Menú', `<div class="pestanas">${tabs}</div>`) + `<div class="panel-cuerpo">${cuerpo}</div>`;
        if (pestanaMenu === 'mochila' && marcada >= 0) panel.querySelector(`#g-inv [data-i="${marcada}"]`)?.classList.add('marcada');
        panel.querySelector(`#g-inv [data-i="${S.sel}"]`)?.classList.add('sel');
      },
      refrescar() { this.pintar(); },
    };
    marcada = -1;
    abrir(def);
  }
  panel.addEventListener('click', e => {
    const tab = e.target.closest('[data-tab]'); if (tab) { pestanaMenu = tab.dataset.tab; marcada = -1; panelActual.pintar(); return; }
    const fab = e.target.closest('[data-fab]');
    if (fab) {
      const r = C.recetas[+fab.dataset.fab];
      if (!puedeFabricar(r)) return;
      for (const [id, n] of Object.entries(r.ingredientes)) sacar(id, n);
      agregar(r.resultado, r.cantidad);
      aviso(`Fabricaste: ${ITEMS[r.resultado].nombre}${r.cantidad > 1 ? ' ×' + r.cantidad : ''}`);
      return;
    }
    const cas = e.target.closest('#g-inv .casilla');
    if (cas) {
      const i = +cas.dataset.i;
      if (marcada < 0) { if (S.inv[i]) marcada = i; }
      else { [S.inv[marcada], S.inv[i]] = [S.inv[i], S.inv[marcada]]; marcada = -1; }
      pintarBarra(); panelActual.pintar(); return;
    }
    if (e.target.id === 'btn-reiniciar') {
      panel.querySelector('.panel-cuerpo').insertAdjacentHTML('beforeend', `<p class="ayuda" style="margin-top:10px">¿Seguro? Se pierde la partida guardada.</p><button class="btn primario" id="btn-reiniciar-si">Sí, empezar de nuevo</button>`);
      e.target.disabled = true; return;
    }
    if (e.target.id === 'btn-reiniciar-si') { try { localStorage.removeItem(CLAVE); } catch (er) { /* sin almacenamiento */ } nuevoMundo(); cerrar(); pintarBarra(); aviso('Partida nueva'); }
  });

  function tienda() {
    abrir({
      pintar() {
        panel.innerHTML = cab('Puesto de semillas') + `<div class="panel-cuerpo"><p class="ayuda">Tienes <span class="precio">${S.dinero}</span> monedas. Para vender, usa la caja de envíos.</p><div class="lista">` +
          C.tienda.map(id => {
            const it = ITEMS[id], cult = CULT[it.cultivo];
            return `<div class="fila">${iconoHTML(id)}<div><div class="titulo">${it.nombre}</div><div class="det">${cult ? `Crece en ${cult.dias} días regados · se vende a ${ITEMS[cult.producto].venta}` : ''}</div></div>
              <div class="acciones"><span class="precio">${it.compra}</span><button class="btn" data-comprar="${id}" data-n="1" ${S.dinero < it.compra ? 'disabled' : ''}>×1</button><button class="btn primario" data-comprar="${id}" data-n="5" ${S.dinero < it.compra * 5 ? 'disabled' : ''}>×5</button></div></div>`;
          }).join('') + '</div></div>';
      },
      refrescar() { this.pintar(); },
    });
  }
  panel.addEventListener('click', e => {
    const b = e.target.closest('[data-comprar]'); if (!b) return;
    const id = b.dataset.comprar, n = +b.dataset.n, costo = ITEMS[id].compra * n;
    if (S.dinero < costo) return;
    const sobra = agregar(id, n);
    S.dinero -= ITEMS[id].compra * (n - sobra);
    if (n - sobra) aviso(`Compraste ${n - sobra} ${ITEMS[id].nombre.toLowerCase()}`);
    panelActual.pintar();
  });

  function cofre(o) {
    o.datos.items = o.datos.items || Array(12).fill(null);
    abrir({
      pintar() {
        panel.innerHTML = cab('Cofre') + `<div class="panel-cuerpo"><p class="ayuda">Toca un objeto para pasarlo entre el cofre y la mochila.</p>
          <div class="sub">Cofre</div><div class="grilla" id="g-cofre">${o.datos.items.map((s, i) => casillaHTML(s, i, false)).join('')}</div>
          <div class="sub">Mochila</div><div class="grilla" id="g-moch">${S.inv.map((s, i) => casillaHTML(s, i, false)).join('')}</div></div>`;
      },
      refrescar() { this.pintar(); },
      cofre: o,
    });
  }
  panel.addEventListener('click', e => {
    if (!panelActual?.cofre) return;
    const items = panelActual.cofre.datos.items;
    const a = e.target.closest('#g-cofre .casilla'), b = e.target.closest('#g-moch .casilla');
    if (a && items[+a.dataset.i]) { const s = items[+a.dataset.i]; const sobra = agregar(s.id, s.n); s.n = sobra; if (!sobra) items[+a.dataset.i] = null; panelActual.pintar(); }
    if (b && S.inv[+b.dataset.i]) {
      const s = S.inv[+b.dataset.i];
      let j = items.findIndex(x => x && x.id === s.id && !esHerramienta(s.id)); if (j < 0) j = items.findIndex(x => !x);
      if (j < 0) { aviso('El cofre está lleno', true); return; }
      if (items[j]) items[j].n += s.n; else items[j] = { id: s.id, n: s.n };
      S.inv[+b.dataset.i] = null; pintarBarra(); panelActual.pintar();
    }
  });

  function preguntarDormir() {
    abrir({
      pintar() {
        panel.innerHTML = cab('Casa') + `<div class="panel-cuerpo"><p class="ayuda">Son las ${fmtHora(S.min)}. ¿Dormir hasta mañana? La partida se guarda al despertar.</p>
          <div class="acciones" style="justify-content:flex-start"><button class="btn primario" id="btn-dormir">Dormir</button><button class="btn" data-cerrar>Seguir despierta</button></div></div>`;
      },
    });
  }
  panel.addEventListener('click', e => { if (e.target.id === 'btn-dormir') dormir(false); });

  // ---------------- acciones en el mundo ----------------
  function celdaPj() { return [Math.floor(S.pj.x / T), Math.floor((S.pj.y - 6) / T)]; }
  function celdaFrente() { const [cx, cy] = celdaPj(), [dx, dy] = VEC[S.pj.dir]; return [cx + dx, cy + dy]; }
  const sacudidas = new Map();

  function gastar(e) {
    if (S.energia < e) { aviso('Estás agotada. Come algo o duerme.', true); return false; }
    S.energia = Math.max(0, S.energia - e); return true;
  }

  function usar(x, y) {
    if (!dentro(x, y)) return;
    const o = objetoEn(x, y), s = enMano(), it = s && ITEMS[s.id], i = idx(x, y);
    const cx = (x + 0.5) * T, cy = y * T;
    if (o) {
      const def = OBJ[o.id];
      if (def.accion === 'dormir') return preguntarDormir();
      if (def.accion === 'tienda') return tienda();
      if (def.accion === 'cofre' && !(it && (s.id === 'hacha' || s.id === 'pico'))) return cofre(o);
      if (def.accion === 'vender') {
        if (!it || it.tipo === 'herramienta' || !it.venta) return aviso('Ten en la mano lo que quieras vender', true);
        S.envios.push({ id: s.id, n: s.n }); aviso(`Enviaste ${s.n} ${it.nombre.toLowerCase()} · cobras al dormir`);
        S.inv[S.sel] = null; pintarBarra(); return;
      }
      if (def.accion === 'maquina') {
        const d = o.datos;
        if (d.salida && d.listo <= S.dia) { const sobra = agregar(d.salida); if (!sobra) { flotar(cx, cy, '+1 ' + ITEMS[d.salida].nombre); o.datos = {}; } return; }
        if (d.salida) return aviso('La conserva estará lista mañana');
        if (it && it.conserva) { o.datos = { entrada: s.id, salida: it.conserva, listo: S.dia + (def.dias || 1) }; gastarMano(); return aviso(`El tarro prepara ${ITEMS[it.conserva].nombre.toLowerCase()}`); }
        if (!(it && (s.id === 'hacha' || s.id === 'pico'))) return aviso('Ten un cultivo en la mano para hacer conserva', true);
      }
      if (def.herramienta && it && it.tipo === 'herramienta' && (def.herramienta === s.id || def.herramienta === 'cualquiera')) {
        if (!gastar(2)) return;
        o.golpes--; sacudidas.set(o.uid, 0.18);
        if (o.golpes <= 0) {
          quitar(o);
          for (const [id, [a, b]] of Object.entries(def.suelta || {})) { const n = a + Math.floor(Math.random() * (b - a + 1)); agregar(id, n); flotar(cx, cy, `+${n} ${ITEMS[id].nombre}`); }
        }
        return;
      }
      if (def.recoger && it && (s.id === 'hacha' || s.id === 'pico')) {
        if (o.datos.items?.some(Boolean)) return aviso('Vacía el cofre antes de recogerlo', true);
        if (o.datos.salida) return aviso('Espera a que termine la conserva', true);
        quitar(o); agregar(o.id); flotar(cx, cy, '+1 ' + ITEMS[o.id].nombre); return;
      }
      if (def.herramienta) return aviso(`Necesitas ${def.herramienta === 'cualquiera' ? 'una herramienta' : 'el ' + ITEMS[def.herramienta].nombre.toLowerCase()}`, true);
      return;
    }
    const cult = S.cultivos[i];
    if (cult && cult.dias >= CULT[cult.id].dias) {
      const prod = CULT[cult.id].producto;
      if (agregar(prod)) return;
      delete S.cultivos[i]; flotar(cx, cy, '+1 ' + ITEMS[prod].nombre, '#f2c14e'); return;
    }
    if (!it) return;
    if (s.id === 'azada') {
      if (S.agua[i] || S.suelo[i]) return;
      for (let dy = -1; dy <= 1; dy++) for (let dx = -1; dx <= 1; dx++) if (dentro(x + dx, y + dy) && S.agua[idx(x + dx, y + dy)]) return aviso('Muy cerca del agua para arar', true);
      if (!gastar(2)) return;
      S.suelo[i] = 1; if (S.lluvia) S.regado[i] = 1; return;
    }
    if (s.id === 'regadera') {
      if (S.agua[i]) { S.aguaRegadera = ITEMS.regadera.capacidad; flotar(cx, cy, 'Regadera llena', '#5aa9e6'); pintarBarra(); return; }
      if (!S.suelo[i] || S.regado[i]) return;
      if (S.aguaRegadera <= 0) return aviso('La regadera está vacía: úsala frente al estanque', true);
      if (!gastar(1)) return;
      S.aguaRegadera--; S.regado[i] = 1; pintarBarra(); return;
    }
    if (it.tipo === 'semilla') {
      if (!S.suelo[i]) return aviso('Primero ara la tierra con la azada', true);
      if (cult) return;
      S.cultivos[i] = { id: it.cultivo, dias: 0 }; gastarMano(); return;
    }
    if (it.tipo === 'colocable') {
      const [px, py] = celdaPj();
      if (px === x && py === y) return;
      if (!libre(x, y)) return aviso('No cabe ahí', true);
      colocar(it.objeto, x, y); gastarMano(); return;
    }
  }

  function comer() {
    const s = enMano(), it = s && ITEMS[s.id];
    if (!it || (it.tipo !== 'cultivo' && it.tipo !== 'artesanal')) return aviso('Ten un cultivo o una conserva en la mano para comer', true);
    if (S.energia >= S.energiaMax) return aviso('Tienes la energía al máximo');
    const e = Math.max(10, Math.round(it.venta / 4));
    S.energia = Math.min(S.energiaMax, S.energia + e); gastarMano();
    flotar(S.pj.x, S.pj.y - PJ.pie, `+${e} energía`, '#79c267');
  }

  // ---------------- el paso de los días ----------------
  function dormir(desmayo) {
    const ventas = {};
    for (const e of S.envios) ventas[e.id] = (ventas[e.id] || 0) + e.n;
    const total = Object.entries(ventas).reduce((a, [id, n]) => a + ITEMS[id].venta * n, 0);
    S.dinero += total; S.envios = [];
    const eventos = [];
    let multa = 0;
    if (desmayo) { multa = Math.min(500, Math.floor(S.dinero * 0.1)); S.dinero -= multa; eventos.push(`Te desmayaste a las 2:00 y alguien te llevó a casa. Pagaste ${multa} de atención.`); }

    // crecimiento y cuervos
    for (const k of Object.keys(S.cultivos)) if (S.regado[k] && S.cultivos[k].dias < CULT[S.cultivos[k].id].dias) S.cultivos[k].dias++;
    const protegido = k => { const x = k % W, y = Math.floor(k / W); return S.objetos.some(o => OBJ[o.id].protege && Math.hypot(o.x - x, o.y - y) <= OBJ[o.id].protege); };
    const desprotegidos = Object.keys(S.cultivos).filter(k => !protegido(+k));
    if (desprotegidos.length >= 8 && Math.random() < 0.6) {
      const k = desprotegidos[Math.floor(Math.random() * desprotegidos.length)];
      eventos.push(`Un cuervo se comió una planta de ${ITEMS[CULT[S.cultivos[k].id].producto].nombre.toLowerCase()}. Un espantapájaros lo evita.`);
      delete S.cultivos[k];
    }
    // tierra sin cultivo vuelve a césped a veces; el riego se seca
    for (let i = 0; i < W * H; i++) { if (S.suelo[i] && !S.cultivos[i] && !S.regado[i] && Math.random() < 0.12) S.suelo[i] = 0; S.regado[i] = 0; }
    S.dia++;
    S.lluvia = Math.random() < 0.2;
    if (S.lluvia) { for (let i = 0; i < W * H; i++) if (S.suelo[i]) S.regado[i] = 1; eventos.push('Llueve: la tierra arada amaneció regada.'); }
    for (const o of S.objetos) {
      const def = OBJ[o.id];
      if (def.riega) for (const [dx, dy] of [[1, 0], [-1, 0], [0, 1], [0, -1]]) { const x = o.x + dx, y = o.y + dy; if (dentro(x, y) && S.suelo[idx(x, y)]) S.regado[idx(x, y)] = 1; }
      if (def.accion === 'maquina' && o.datos.salida && o.datos.listo <= S.dia) eventos.push(`Tu tarro tiene lista una ${ITEMS[o.datos.salida].nombre.toLowerCase()}.`);
    }
    // vuelve a salir algo de maleza y alguna roca
    const r = Math.random;
    for (let k = 0, n = 0; k < 200 && n < 4; k++) { const x = 2 + Math.floor(r() * (W - 4)), y = 3 + Math.floor(r() * (H - 5)); if (!(x >= 3 && x <= 17 && y >= 2 && y <= 10) && colocar(n === 0 ? 'roca' : 'maleza', x, y)) n++; }

    S.min = INICIO_DIA;
    S.energia = desmayo ? Math.round(S.energiaMax * 0.6) : S.energiaMax;
    S.pj = { x: 7.5 * T, y: 7.6 * T, dir: 'south' };
    guardar();
    resumen(ventas, total, eventos);
  }
  function resumen(ventas, total, eventos) {
    const filas = Object.entries(ventas).map(([id, n]) => `<tr><td>${ITEMS[id].nombre} ×${n}</td><td>${ITEMS[id].venta * n}</td></tr>`).join('');
    abrir({
      cerrable: false,
      pintar() {
        panel.innerHTML = `<div class="panel-cab"><h2>Día ${(S.dia - 1) % 28 + 1} de primavera</h2></div><div class="panel-cuerpo">
          ${filas ? `<table class="resumen"><tbody>${filas}</tbody><tfoot><tr><td>Total ganado</td><td>${total}</td></tr></tfoot></table>` : '<p class="ayuda">Ayer no enviaste nada para vender.</p>'}
          ${eventos.map(e => `<p class="ayuda" style="margin-top:8px">${e}</p>`).join('')}
          <p class="ayuda">Partida guardada.</p>
          <div class="acciones" style="justify-content:flex-start"><button class="btn primario" data-cerrar>Empezar el día</button></div></div>`;
      },
    });
  }

  // ---------------- guardado ----------------
  const aTexto = a => Array.from(a).join('');
  const deTexto = t => Uint8Array.from(t, ch => +ch);
  function guardar() {
    try {
      localStorage.setItem(CLAVE, JSON.stringify({ ...S, suelo: aTexto(S.suelo), regado: aTexto(S.regado), agua: aTexto(S.agua) }));
    } catch (e) { /* sin almacenamiento: la partida dura mientras la página siga abierta */ }
  }
  function cargar() {
    try {
      const t = localStorage.getItem(CLAVE); if (!t) return false;
      const d = JSON.parse(t);
      S = { ...d, suelo: deTexto(d.suelo), regado: deTexto(d.regado), agua: deTexto(d.agua) };
      S.objetos = S.objetos.filter(o => OBJ[o.id]);
      S.inv = S.inv.map(s => (s && ITEMS[s.id] ? s : null));
      reindexar(); return true;
    } catch (e) { return false; }
  }
  let hayGuardado = false;
  try { hayGuardado = !!localStorage.getItem(CLAVE); } catch (e) { hayGuardado = false; }

  // ---------------- entrada ----------------
  const lienzoEl = $('#lienzo'), g = lienzoEl.getContext('2d');
  const teclas = new Set();
  let jugando = false;
  addEventListener('keydown', e => {
    if (!jugando) return;
    const k = e.key.toLowerCase();
    if (k === 'escape') { if (!velo.hidden && panelActual?.cerrable !== false) cerrar(); return; }
    if (!velo.hidden) { if ((k === 'i' || k === 'tab' || k === 'c') && panelActual?.cerrable !== false) { e.preventDefault(); cerrar(); } return; }
    if (k === 'i' || k === 'tab') { e.preventDefault(); menu('mochila'); return; }
    if (k === 'c') { menu('crafteo'); return; }
    if (k === 'f') { comer(); return; }
    if (k === ' ' || k === 'j' || k === 'enter') { e.preventDefault(); if (!e.repeat) usar(...celdaFrente()); return; }
    const n = '1234567890-='.indexOf(e.key); if (n >= 0) { S.sel = n; pintarBarra(); return; }
    if (k === 'q' || k === 'e') { S.sel = (S.sel + (k === 'e' ? 1 : 11)) % 12; pintarBarra(); return; }
    if (['arrowup', 'arrowdown', 'arrowleft', 'arrowright'].includes(k)) e.preventDefault();
    teclas.add(k);
  });
  addEventListener('keyup', e => teclas.delete(e.key.toLowerCase()));
  addEventListener('blur', () => teclas.clear());
  lienzoEl.addEventListener('wheel', e => { if (!jugando || !velo.hidden) return; e.preventDefault(); S.sel = (S.sel + (e.deltaY > 0 ? 1 : 11)) % 12; pintarBarra(); }, { passive: false });

  let camX = 0, camY = 0, escala = 2;
  lienzoEl.addEventListener('pointerdown', e => {
    if (!jugando || !velo.hidden || e.pointerType === 'touch') return;
    const x = Math.floor((e.clientX * devicePixelRatio / escala + camX) / T), y = Math.floor((e.clientY * devicePixelRatio / escala + camY) / T);
    const [px, py] = celdaPj();
    const dx = x - px, dy = y - py;
    if (Math.max(Math.abs(dx), Math.abs(dy)) > 1) return;
    if (dx || dy) S.pj.dir = Math.abs(dx) > Math.abs(dy) ? (dx > 0 ? 'east' : 'west') : (dy > 0 ? 'south' : 'north');
    usar(x, y);
  });
  // controles táctiles
  const tactil = { arriba: false, abajo: false, izq: false, der: false };
  document.querySelectorAll('.cruceta [data-dir]').forEach(b => {
    const on = v => e => { e.preventDefault(); tactil[b.dataset.dir] = v; };
    b.addEventListener('pointerdown', on(true)); b.addEventListener('pointerup', on(false));
    b.addEventListener('pointercancel', on(false)); b.addEventListener('pointerleave', on(false));
  });
  $('#t-accion').addEventListener('pointerdown', e => { e.preventDefault(); if (jugando && velo.hidden) usar(...celdaFrente()); });
  $('#t-comer').addEventListener('click', () => { if (jugando && velo.hidden) comer(); });
  $('#t-menu').addEventListener('click', () => { if (jugando) velo.hidden ? menu('mochila') : cerrar(); });

  // ---------------- movimiento ----------------
  function solido(px, py) {
    const x = Math.floor(px / T), y = Math.floor(py / T);
    if (!dentro(x, y) || S.agua[idx(x, y)]) return true;
    const o = objetoEn(x, y); return !!(o && OBJ[o.id].solido);
  }
  const choca = (x, y) => solido(x - 8, y - 6) || solido(x + 8, y - 6) || solido(x - 8, y + 2) || solido(x + 8, y + 2);
  let andando = false, tAnim = 0;
  function mover(dt) {
    let vx = 0, vy = 0;
    if (teclas.has('a') || teclas.has('arrowleft') || tactil.izq) vx--;
    if (teclas.has('d') || teclas.has('arrowright') || tactil.der) vx++;
    if (teclas.has('w') || teclas.has('arrowup') || tactil.arriba) vy--;
    if (teclas.has('s') || teclas.has('arrowdown') || tactil.abajo) vy++;
    andando = !!(vx || vy);
    if (!andando) return;
    if (vx && vy) { vx *= Math.SQRT1_2; vy *= Math.SQRT1_2; }
    S.pj.dir = Math.abs(vx) > Math.abs(vy) ? (vx > 0 ? 'east' : 'west') : (vy > 0 ? 'south' : 'north');
    const v = 112 * dt;
    if (!choca(S.pj.x + vx * v, S.pj.y)) S.pj.x += vx * v;
    if (!choca(S.pj.x, S.pj.y + vy * v)) S.pj.y += vy * v;
    tAnim += dt;
  }

  // ---------------- dibujo ----------------
  function ajustar() {
    const dpr = devicePixelRatio || 1;
    lienzoEl.width = Math.round(innerWidth * dpr); lienzoEl.height = Math.round(innerHeight * dpr);
    const porAncho = lienzoEl.width / (12 * T), porAlto = lienzoEl.height / (8 * T);
    escala = Math.max(1, Math.min(4, Math.floor(Math.min(porAncho, porAlto) * 2) / 2));
  }
  addEventListener('resize', ajustar); ajustar();

  const esp = (x, y, k) => (dentro(x, y) ? S[k][idx(x, y)] : 0);
  function capa(ts, k, x0, y0, x1, y1) {
    for (let j = y0 - 1; j <= y1; j++) for (let i = x0 - 1; i <= x1; i++) {
      const n = esp(i, j, k) * 8 + esp(i + 1, j, k) * 4 + esp(i, j + 1, k) * 2 + esp(i + 1, j + 1, k);
      if (n) g.drawImage(ts[n], i * T + T / 2, j * T + T / 2, T, T);
    }
  }
  function dibujarSprite(img, cxp, basey, sacude = 0) {
    const off = sacude > 0 ? Math.sin(sacude * 80) * 2 : 0;
    g.drawImage(img, Math.round(cxp - img.width / 2 + off), Math.round(basey - img.height));
  }

  let tiempoLluvia = 0;
  function dibujar() {
    const vw = lienzoEl.width / escala, vh = lienzoEl.height / escala;
    camX = Math.max(0, Math.min(W * T - vw, S.pj.x - vw / 2)); camY = Math.max(0, Math.min(H * T - vh, S.pj.y - vh / 2));
    if (W * T < vw) camX = (W * T - vw) / 2; if (H * T < vh) camY = (H * T - vh) / 2;
    camX = Math.round(camX); camY = Math.round(camY);
    g.setTransform(1, 0, 0, 1, 0, 0); g.imageSmoothingEnabled = false;
    g.fillStyle = '#161b17'; g.fillRect(0, 0, lienzoEl.width, lienzoEl.height);
    g.setTransform(escala, 0, 0, escala, -camX * escala, -camY * escala);
    const x0 = Math.max(0, Math.floor(camX / T)), y0 = Math.max(0, Math.floor(camY / T));
    const x1 = Math.min(W - 1, Math.ceil((camX + vw) / T)), y1 = Math.min(H - 1, Math.ceil((camY + vh) / T));

    // suelo: césped base + capas Wang (rejilla doble: cada tile une 4 celdas)
    const cesped = TS.tierra[0];
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) g.drawImage(cesped, x * T, y * T, T, T);
    capa(TS.agua, 'agua', x0, y0, x1, y1);
    capa(TS.tierra, 'suelo', x0, y0, x1, y1);
    g.fillStyle = 'rgba(38,22,10,.38)';
    for (let y = y0; y <= y1; y++) for (let x = x0; x <= x1; x++) {
      if (!S.regado[idx(x, y)] || !S.suelo[idx(x, y)]) continue;
      const l = esp(x - 1, y, 'regado') && esp(x - 1, y, 'suelo'), r = esp(x + 1, y, 'regado') && esp(x + 1, y, 'suelo');
      const u = esp(x, y - 1, 'regado') && esp(x, y - 1, 'suelo'), d = esp(x, y + 1, 'regado') && esp(x, y + 1, 'suelo');
      const m = 5;
      g.fillRect(x * T + (l ? 0 : m), y * T + (u ? 0 : m), T - (l ? 0 : m) - (r ? 0 : m), T - (u ? 0 : m) - (d ? 0 : m));
    }

    // casilla objetivo
    if (velo.hidden) {
      const [fx, fy] = celdaFrente();
      g.strokeStyle = 'rgba(242,193,78,.85)'; g.lineWidth = 1; g.setLineDash([3, 2]);
      g.strokeRect(fx * T + 1.5, fy * T + 1.5, T - 3, T - 3); g.setLineDash([]);
    }

    // entidades ordenadas por su base
    const ents = [];
    for (const [k, c] of Object.entries(S.cultivos)) {
      const x = k % W, y = Math.floor(k / W);
      if (x < x0 - 1 || x > x1 + 1 || y < y0 - 1 || y > y1 + 3) continue;
      ents.push({ base: (y + 1) * T - 2, f: () => {
        const def = CULT[c.id], n = c.dias;
        if (n === 0) { g.fillStyle = '#2b1a0e'; for (const [a, b] of [[11, 14], [18, 12], [15, 19], [21, 18]]) g.fillRect(x * T + a, y * T + b, 2, 2); return; }
        const sp = n >= def.dias ? def.sprites[2] : n >= Math.ceil(def.dias / 2) ? def.sprites[1] : def.sprites[0];
        dibujarSprite(SPR[sp], (x + 0.5) * T, (y + 1) * T - 2);
        if (n >= def.dias) { g.fillStyle = 'rgba(242,193,78,.9)'; g.fillRect((x + 0.5) * T - 1, y * T - 6 + Math.sin(performance.now() / 250) * 1.5, 2, 3); }
      } });
    }
    for (const o of S.objetos) {
      const def = OBJ[o.id], w = def.ancho || 1, h = def.alto || 1;
      if (o.x + w < x0 - 2 || o.x > x1 + 2 || o.y > y1 + 1 || o.y + h < y0 - 4) continue;
      ents.push({ base: (o.y + h) * T, f: () => {
        dibujarSprite(SPR[def.sprite], (o.x + w / 2) * T, (o.y + h) * T, sacudidas.get(o.uid) || 0);
        if (def.accion === 'maquina' && o.datos.salida && o.datos.listo <= S.dia) {
          const yy = o.y * T - 10 + Math.sin(performance.now() / 250) * 2;
          g.drawImage(SPR.conserva, (o.x + 0.5) * T - 8, yy - 8, 16, 16);
        }
      } });
    }
    const quieto = PJ.quieto[S.pj.dir], cuadros = PJ.caminar[S.pj.dir];
    const img = andando && cuadros.length ? cuadros[Math.floor(tAnim * 9) % cuadros.length] : quieto;
    ents.push({ base: S.pj.y, f: () => {
      g.fillStyle = 'rgba(0,0,0,.22)'; g.beginPath(); g.ellipse(S.pj.x, S.pj.y, 9, 3.5, 0, 0, 7); g.fill();
      g.drawImage(img, Math.round(S.pj.x - img.width / 2), Math.round(S.pj.y - PJ.pie * (img.height / PJ.alto)));
    } });
    ents.sort((a, b) => a.base - b.base).forEach(e => e.f());

    // textos flotantes
    g.font = '600 9px "Pixelify Sans", monospace'; g.textAlign = 'center';
    for (const f of flotantes) {
      g.globalAlpha = Math.max(0, 1 - f.t / 1.2);
      g.fillStyle = '#000'; g.fillText(f.txt, f.x + 1, f.y - f.t * 18 + 1);
      g.fillStyle = f.color; g.fillText(f.txt, f.x, f.y - f.t * 18);
    }
    g.globalAlpha = 1;

    // lluvia y anochecer (en pantalla)
    g.setTransform(1, 0, 0, 1, 0, 0);
    if (S.lluvia) {
      g.strokeStyle = 'rgba(170,200,255,.35)'; g.lineWidth = 1 * escala;
      const n = 90, t = tiempoLluvia;
      g.beginPath();
      for (let k = 0; k < n; k++) {
        const x = ((k * 97.3 + t * 260) % (lienzoEl.width + 60)) - 30, y = ((k * 53.1 + t * 900 + k * k) % (lienzoEl.height + 40)) - 20;
        g.moveTo(x, y); g.lineTo(x - 4 * escala, y + 12 * escala);
      }
      g.stroke();
      g.fillStyle = 'rgba(30,40,70,.12)'; g.fillRect(0, 0, lienzoEl.width, lienzoEl.height);
    }
    if (S.min > 17 * 60) {
      const a = Math.min(0.5, (S.min - 17 * 60) / (5 * 60) * 0.5);
      g.fillStyle = `rgba(16,20,58,${a})`; g.fillRect(0, 0, lienzoEl.width, lienzoEl.height);
    }
  }

  // ---------------- bucle ----------------
  let ultimo = performance.now();
  function cuadro(ahora) {
    const dt = Math.min(0.05, (ahora - ultimo) / 1000); ultimo = ahora;
    if (jugando) {
      if (velo.hidden) {
        mover(dt);
        S.min += dt * MIN_POR_SEG;
        if (S.min >= FIN_DIA) dormir(true);
      }
      for (const [k, v] of sacudidas) { if (v - dt <= 0) sacudidas.delete(k); else sacudidas.set(k, v - dt); }
      for (let i = flotantes.length - 1; i >= 0; i--) { flotantes[i].t += dt; if (flotantes[i].t > 1.2) flotantes.splice(i, 1); }
      tiempoLluvia += dt;
      pintarHUD();
    }
    if (S) dibujar();
    requestAnimationFrame(cuadro);
  }

  // ---------------- arranque ----------------
  nuevoMundo();                       // mundo de fondo detrás de la portada
  pintarBarra(); requestAnimationFrame(cuadro);
  const portada = $('#portada');
  if (hayGuardado) $('#btn-continuar').hidden = false;
  const empezar = () => { portada.hidden = true; jugando = true; pintarBarra(); pintarHUD(); aviso('Ara con la azada, siembra y riega. El puesto vende semillas.'); };
  $('#btn-nueva').addEventListener('click', () => { nuevoMundo(); empezar(); });
  $('#btn-continuar').addEventListener('click', () => { if (!cargar()) { nuevoMundo(); aviso('No se pudo leer la partida guardada', true); } empezar(); });
  window.__huerta = { get S() { return S; }, usar, dormir, agregar };   // para pruebas
})();
