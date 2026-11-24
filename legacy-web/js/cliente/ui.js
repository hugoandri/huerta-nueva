// HTML Interface (HUD, hotbar, panels). Reads state and dispatches actions to the session;
// never mutates state on its own.
import { cuenta } from '../nucleo/inventario.js';
import { objetoPorUid, zonaDe } from '../nucleo/mundo.js';

const $ = s => document.querySelector(s);
const fmtHora = m => `${Math.floor(m / 60) % 24}:${String(Math.floor((m % 60) / 10) * 10).padStart(2, '0')}`;

export function crearUI({ def, G, sesion, audio }) {
  const S = () => sesion().estado, yo = () => sesion().yo, enviar = a => sesion().despachar(a);
  const velo = $('#velo'), panel = $('#panel'), barra = $('#barra'), avisos = $('#avisos');
  let actual = null, pestanaMenu = 'mochila', marcada = -1, hudCache = '';

  function aviso(txt, malo = false) {
    const d = document.createElement('div');
    d.className = 'aviso' + (malo ? ' malo' : ''); d.textContent = txt;
    avisos.appendChild(d); while (avisos.children.length > 4) avisos.firstChild.remove();
    setTimeout(() => d.remove(), 2400);
  }

  const icono = id => `<img src="${G.SPR_URL[def.ITEMS[id].sprite]}" alt="">`;
  function casilla(s, i, tecla) {
    const t = tecla ? `<span class="tecla">${i === 9 ? 0 : i === 10 ? '-' : i === 11 ? '=' : i + 1}</span>` : '';
    if (!s) return `<button class="casilla" data-i="${i}" aria-label="Empty slot">${t}</button>`;
    const it = def.ITEMS[s.id];
    return `<button class="casilla" data-i="${i}" title="${it.nombre}" aria-label="${it.nombre}${s.n > 1 ? ', ' + s.n : ''}">${t}${icono(s.id)}${s.n > 1 ? `<span class="n">${s.n}</span>` : ''}</button>`;
  }

  // Hotbar and panels repaint only when their displayed state changes.
  let barraCache = '';
  function pintarBarra() {
    const j = yo(), clave = JSON.stringify(j.inv.slice(0, 12)) + j.sel + j.aguaRegadera;
    if (clave !== barraCache) {
      barraCache = clave;
      barra.innerHTML = j.inv.slice(0, 12).map((s, i) => casilla(s, i, true)).join('');
      barra.children[j.sel]?.classList.add('sel');
      const s = j.inv[j.sel];
      $('#nombre-sel').textContent = s ? def.ITEMS[s.id].nombre + (s.id === 'regadera' ? ` · water ${j.aguaRegadera}/${def.ITEMS.regadera.capacidad}` : '') : 'Empty hand';
    }
  }
  barra.addEventListener('click', e => { const b = e.target.closest('.casilla'); if (b) enviar({ tipo: 'seleccionar', i: +b.dataset.i }); });

  function pintarHUD() {
    const e = S(), j = yo(), t = e.tiempo, enCueva = j.lugar === 'mazmorra';
    const dia = enCueva ? `${def.MAZ[e.mazmorra.actual]?.nombre} · Floor ${e.mazmorra.piso} of ${def.MAZ[e.mazmorra.actual]?.pisos}`
      : `Spring · Day ${(t.dia - 1) % 28 + 1}${t.dia > 28 ? ` · Year ${Math.floor((t.dia - 1) / 28) + 1}` : ''}`;
    const clave = dia + fmtHora(t.min) + j.dinero + Math.round(j.energia) + j.aguaRegadera + t.lluvia + j.vida + j.vidaMax;
    pintarJefe(e, j);
    if (clave !== hudCache) {
      hudCache = clave;
      $('#hud-dia').textContent = dia;
      $('#hud-hora').textContent = fmtHora(t.min);
      $('#hud-clima').textContent = !enCueva && t.lluvia ? 'Rain: crops watered' : '';
      // Hearts: each equals 2 health points
      let c = '';
      for (let k = 0; k < j.vidaMax / 2; k++) {
        const v = j.vida - k * 2;
        c += `<span class="corazon ${v >= 2 ? 'lleno' : v >= 1 ? 'medio' : ''}" aria-hidden="true"></span>`;
      }
      $('#hud-vida').innerHTML = c;
      $('#hud-vida').setAttribute('aria-label', `Health ${j.vida / 2} of ${j.vidaMax / 2} hearts`);
      $('#hud-dinero').textContent = j.dinero.toLocaleString('en-US');
      const pe = j.energia / j.energiaMax, pa = j.aguaRegadera / (def.ITEMS.regadera?.capacidad || 20);
      $('#m-energia .relleno').style.height = pe * 100 + '%'; $('#m-energia').classList.toggle('bajo', pe < 0.2);
      $('#m-agua .relleno').style.height = pa * 100 + '%';
    }
    pintarBarra();
    if (actual?.firma) { const f = actual.firma(); if (f !== actual._firma) { actual._firma = f; actual.pintar(); } }
  }

  // Boss health bar: appears when the floor boss is nearby.
  let jefeCache = '';
  function pintarJefe(e, j) {
    const z = j.lugar === 'mazmorra' ? e.mazmorra.nivel : null;
    const m = z?.monstruos.find(x => def.MON[x.id].jefe && Math.hypot(x.x - j.x, x.y - j.y) < 11 * def.T);
    const clave = m ? m.id + m.vida : '';
    if (clave === jefeCache) return;
    jefeCache = clave;
    $('#jefe').hidden = !m;
    if (m) { $('#jefe-nombre').textContent = def.MON[m.id].nombre; $('#jefe-vida').style.width = `${100 * m.vida / m.vidaMax}%`; }
  }

  // ---------------- panels
  function abrir(p) { actual = p; actual._firma = p.firma?.(); velo.hidden = false; p.pintar(); panel.querySelector('button')?.focus({ preventScroll: true }); }
  function cerrar() { velo.hidden = true; actual = null; }
  const abierto = () => !velo.hidden;
  const cerrable = () => actual?.cerrable !== false;
  velo.addEventListener('pointerdown', e => { if (e.target === velo && cerrable()) cerrar(); });
  const cab = (titulo, extra = '') => `<div class="panel-cab"><h2>${titulo}</h2>${extra}<button class="btn cerrar" data-cerrar aria-label="Close">✕</button></div>`;
  const firmaInv = () => JSON.stringify(yo().inv) + yo().dinero + yo().ropa + yo().ropaBatalla + JSON.stringify(S().hitos || []);

  let charlaIdx = -1;
  function saludoMercader(e) {
    const merc = def.C.historia?.mercader;
    const charlas = merc?.charlas || [];
    if (charlaIdx >= 0 && charlas.length) return charlas[charlaIdx % charlas.length];
    const sal = merc?.saludos;
    if (!sal) return '';
    if (e.tiempo.lluvia && sal.lluvia) return sal.lluvia;
    const hitos = e.hitos || [];
    for (const k of ['hito_final', 'hito_4', 'hito_linterna', 'hito_3', 'hito_2', 'hito_conserva', 'hito_1']) if (hitos.includes(k) && sal[k]) return sal[k];
    return sal.inicio || '';
  }

  function menu(pestana) {
    if (pestana) pestanaMenu = pestana;
    marcada = -1;
    abrir({
      firma: firmaInv,
      pintar() {
        const j = yo(), e = S();
        const tabs = [['mochila', 'Backpack'], ['crafteo', 'Crafting'], ['diario', 'Journal'], ['ayuda', 'How to Play']]
          .map(([t, n]) => `<button class="btn ${pestanaMenu === t ? 'activo' : ''}" data-tab="${t}">${n}</button>`).join('');
        let cuerpo;
        if (pestanaMenu === 'mochila') {
          const idxDet = marcada >= 0 ? marcada : j.sel;
          const sDet = j.inv[idxDet], itDet = sDet && def.ITEMS[sDet.id];
          const detalle = itDet ? `<div class="fila" style="margin-top:10px">${icono(itDet.id)}<div><div class="titulo">${itDet.nombre}${sDet.n > 1 ? ` ×${sDet.n}` : ''}</div><div class="det">${itDet.descripcion || ''}${itDet.venta ? ` · Sell: ${itDet.venta}g` : ''}</div></div>
            <div class="acciones">${itDet.tipo === 'ropa' ? `<button class="btn primario" data-vestir="${idxDet}">Equip</button>` : ''}<button class="btn" data-rapido="${idxDet}">${idxDet < 12 ? 'To Bag' : 'To Hotbar'}</button></div></div>` : '';
          cuerpo = `<p class="ayuda">Farm Outfit: <b>${def.ITEMS[j.ropa]?.nombre}</b> · Dungeon Outfit: <b>${def.ITEMS[j.ropaBatalla]?.nombre}</b> (equipped automatically in dungeons).</p>
            <p class="ayuda">Click a slot and then another to swap (or <kbd>Shift</kbd>+click to move between hotbar and bag).</p>
            <div class="grilla" id="g-inv">${j.inv.map((s, i) => casilla(s, i, false)).join('')}</div>${detalle}`;
        } else if (pestanaMenu === 'crafteo') {
          cuerpo = `<p class="ayuda">Craft machines, weapons, outfits, and potions. Place crafted objects in the world with <kbd>Space</kbd>.</p><div class="lista">` +
            def.C.recetas.map((r, i) => {
              const it = def.ITEMS[r.resultado], puede = Object.entries(r.ingredientes).every(([id, n]) => cuenta(j, id) >= n);
              const ing = Object.entries(r.ingredientes).map(([id, n]) => `<span class="ing ${cuenta(j, id) < n ? 'falta' : ''}">${icono(id)}${cuenta(j, id)}/${n}</span>`).join('');
              return `<div class="fila">${icono(r.resultado)}<div><div class="titulo">${it.nombre}${r.cantidad > 1 ? ' ×' + r.cantidad : ''}</div><div class="det">${it.descripcion || ''}</div><div style="margin-top:4px">${ing}</div></div>
                <div class="acciones"><button class="btn primario" data-fab="${i}" ${puede ? '' : 'disabled'}>Craft</button></div></div>`;
            }).join('') + '</div>';
        } else if (pestanaMenu === 'diario') {
          const hitos = e.hitos || ['intro'], cartas = def.C.historia?.cartas || [];
          const bestiario = (def.C.monstruos || []).map(m => {
            const spr = G.SPR_URL[m.sprite];
            const tag = m.jefe ? '👑 Boss' : (m.vuela ? '🪽 Flying' : '⚔️ Denizen');
            return `<div class="fila">${spr ? `<img src="${spr}" alt="">` : '<div class="sello">?</div>'}<div><div class="titulo">${m.nombre} <span style="font-size:12px;color:var(--acento)">· ${tag}</span></div><div class="det">HP ${m.vida} · Damage ${m.dano / 2} ♥</div><div class="det" style="color:var(--tinta)">${m.descripcion || ''}</div></div><div></div></div>`;
          }).join('');
          cuerpo = `<p class="ayuda">Field notes left by <b>Grandpa Aurelius</b> across Mosslantern Glen (${hitos.length} of ${cartas.length} discovered).</p><div class="lista">` +
            cartas.map(c => {
              const abierta = hitos.includes(c.id);
              return `<div class="fila ${abierta ? '' : 'cerrada'}"><div class="sello">${abierta ? '📜' : '🔒'}</div><div><div class="titulo">${abierta ? c.titulo : 'Undiscovered Note'}</div><div class="det"><b>${c.resumen}</b></div>${abierta ? `<div class="det" style="margin-top:4px;color:var(--tinta)">"${c.texto}"</div>` : ''}</div><div></div></div>`;
            }).join('') + `</div><div class="sub" style="margin-top:16px">Mosslantern Glen Bestiary</div><div class="lista">${bestiario}</div>`;
        } else {
          cuerpo = `<p class="ayuda">Each day you have limited energy and hours. At 2:00 AM you pass out and pay a small fee.</p>
            <dl class="controles">
              <dt>Hoe</dt><dd>Tills grass. Cannot till right next to the pond.</dd>
              <dt>Seeds</dt><dd>Plant in tilled soil. Water every day so they grow.</dd>
              <dt>Watering Can</dt><dd>Waters tilled soil. Refill it at the pond.</dd>
              <dt>Axe · Pickaxe</dt><dd>Chop trees for wood, mine rocks for stone & ore, clear weeds for fiber. Also pick up placed machines.</dd>
              <dt>Shipping Bin</dt><dd>Use an item in front of it to ship it (or use with an empty hand to undo your last shipment). Paid when you sleep.</dd>
              <dt>Preserves Jar</dt><dd>Load a crop and in 20 hours you'll have artisan preserves worth much more.</dd>
              <dt>Crows</dt><dd>With 8 or more crops and no scarecrow nearby, a crow may eat one overnight.</dd>
              <dt>Farmhouse</dt><dd>Use the door to sleep until morning. Your game saves automatically.</dd>
              <dt>Outfits</dt><dd>You have a Farm Outfit and a Dungeon Outfit (auto-equipped in dungeons). Equip them from your Backpack or by pressing Use.</dd>
              <dt>Dungeons</dt><dd>Enter the cave in the northern cliff. Delve into the Copper Mine or the Forgotten Crypt (unlocks at Mine Floor 5).</dd>
              <dt>Combat</dt><dd>Use a sword in front of a monster. Potions (F) restore hearts.</dd>
              <dt>Furnace</dt><dd>Load 3 ores and in a few hours you'll have an ingot for weapons, sprinklers, and gear.</dd>
            </dl>
            <div class="acciones" style="justify-content:flex-start">
              <button class="btn primario" id="btn-salir-menu">🏠 Save & Title Screen</button>
              <button class="btn" id="btn-mute">${audio?.silencio ? '🔇 Unmute Audio' : '🔊 Mute Audio'}</button>
              <button class="btn" id="btn-reiniciar">Delete save and start over</button>
            </div>`;
        }
        panel.innerHTML = cab('Menu', `<div class="pestanas">${tabs}</div>`) + `<div class="panel-cuerpo">${cuerpo}</div>`;
        if (pestanaMenu === 'mochila') {
          panel.querySelector(`#g-inv [data-i="${j.sel}"]`)?.classList.add('sel');
          if (marcada >= 0) panel.querySelector(`#g-inv [data-i="${marcada}"]`)?.classList.add('marcada');
        }
      },
    });
  }

  function tienda() {
    charlaIdx = -1;
    abrir({
      firma: firmaInv,
      pintar() {
        const j = yo(), e = S(), merc = def.C.historia?.mercader;
        const sal = saludoMercader(e);
        panel.innerHTML = cab(merc?.titulo_puesto || 'Seed Stall', `<button class="btn" id="btn-charlar">💬 Chat with ${merc?.nombre || 'Brisa'}</button>`) + `<div class="panel-cuerpo">
          ${sal ? `<p class="ayuda" style="color:var(--tinta);font-style:italic">${sal}</p>` : ''}
          <p class="ayuda">You have <span class="precio">${j.dinero}</span> gold. To sell items, use the shipping bin.</p><div class="lista">` +
          def.C.tienda.map(id => {
            const it = def.ITEMS[id], c = def.CULT[it.cultivo];
            const det = c ? `Grows in ${c.dias} watered days · sells for ${def.ITEMS[c.producto].venta}g` : (it.descripcion || '');
            return `<div class="fila">${icono(id)}<div><div class="titulo">${it.nombre}</div><div class="det">${det}</div></div>
              <div class="acciones"><span class="precio">${it.compra}</span><button class="btn" data-comprar="${id}" data-n="1" ${j.dinero < it.compra ? 'disabled' : ''}>×1</button>${it.tipo === 'semilla' || it.tipo === 'pocion' ? `<button class="btn primario" data-comprar="${id}" data-n="5" ${j.dinero < it.compra * 5 ? 'disabled' : ''}>×5</button>` : ''}</div></div>`;
          }).join('') + '</div></div>';
      },
    });
  }

  function cofre(uid) {
    abrir({
      uid,
      firma: () => firmaInv() + JSON.stringify(objetoPorUid(def, zonaDe(S(), yo()), uid)?.datos),
      pintar() {
        const o = objetoPorUid(def, zonaDe(S(), yo()), uid);
        if (!o) return cerrar();
        const items = o.datos.items || Array(12).fill(null);
        panel.innerHTML = cab('Chest') + `<div class="panel-cuerpo"><p class="ayuda">Click an item to move it between the chest and your backpack.</p>
          <div class="sub">Chest</div><div class="grilla" id="g-cofre">${items.map((s, i) => casilla(s, i, false)).join('')}</div>
          <div class="sub">Backpack</div><div class="grilla" id="g-moch">${yo().inv.map((s, i) => casilla(s, i, false)).join('')}</div></div>`;
      },
    });
  }

  function dormir() {
    abrir({
      pintar() {
        panel.innerHTML = cab('Farmhouse') + `<div class="panel-cuerpo"><p class="ayuda">It is ${fmtHora(S().tiempo.min)}. Sleep until morning? Your game saves when you wake up.</p>
          <div class="acciones" style="justify-content:flex-start"><button class="btn primario" id="btn-dormir">Sleep</button><button class="btn" data-cerrar>Stay Awake</button></div></div>`;
      },
    });
  }

  // Cave panel: choose which dungeon and checkpoint floor to enter.
  function mazmorra(e) {
    abrir({
      pintar() {
        const j = yo(), batalla = def.ITEMS[j.ropaBatalla];
        const filas = e.enCurso
          ? `<div class="fila">${icono(j.ropaBatalla)}<div><div class="titulo">${def.MAZ[e.enCurso.id].nombre}</div><div class="det">Your party is on Floor ${e.enCurso.piso}.</div></div>
              <div class="acciones"><button class="btn primario" data-maz="${e.enCurso.id}" data-piso="${e.enCurso.piso}">Join</button></div></div>`
          : e.mazmorras.map(m => `<div class="fila ${m.abierta ? '' : 'cerrada'}"><div class="sello">${m.abierta ? m.record : '🔒'}</div>
              <div><div class="titulo">${m.nombre}</div><div class="det">${m.descripcion}</div>
              <div class="det">${m.abierta ? `${m.pisos} floors · best: floor ${m.record || 0} · cleared ${m.completada} ${m.completada === 1 ? 'time' : 'times'}` : m.requiere}</div></div>
              <div class="acciones">${m.abierta ? m.disponibles.map((p, k) => `<button class="btn ${k === m.disponibles.length - 1 ? 'primario' : ''}" data-maz="${m.id}" data-piso="${p}">Floor ${p}</button>`).join('') : ''}</div></div>`).join('');
        panel.innerHTML = cab('Choose a Dungeon') + `<div class="panel-cuerpo">
          <p class="ayuda">Entering automatically equips your Dungeon Outfit: <b>${batalla?.nombre}</b>. ${batalla?.descripcion || ''}</p>
          <div class="lista">${filas}</div>
          <p class="ayuda" style="margin-top:10px">Advance by climbing each floor's ladder. You can only exit through the Floor 1 arch or after defeating the Floor 10 boss. Bosses guard Floors 5 and 10; beating Floor 5 unlocks the Floor 6 shortcut. Each full clear increases enemy strength and bonus loot rolls!</p></div>`;
      },
    });
  }

  function botin(b) {
    abrir({
      pintar() {
        panel.innerHTML = cab(b.titulo) + `<div class="panel-cuerpo"><p class="ayuda">Your share of the spoils (each player rolls their own):</p><div class="lista">` +
          b.items.map(({ item, n }) => `<div class="fila">${icono(item)}<div><div class="titulo">${def.ITEMS[item].nombre}</div></div><div class="acciones"><span class="precio">×${n}</span></div></div>`).join('') +
          `</div><div class="acciones" style="justify-content:flex-start;margin-top:12px"><button class="btn primario" data-cerrar>Take Spoils</button></div></div>`;
      },
    });
  }

  function derrota(d) {
    abrir({
      pintar() {
        panel.innerHTML = cab('You Knocked Out') + `<div class="panel-cuerpo"><p class="ayuda">You were rescued from the dungeon and woke up outside the cave. You paid ${d.multa} gold for treatment.</p>
          <p class="ayuda">Tip: craft a stronger sword, brew health potions, and wear Adventurer Armor.</p>
          <div class="acciones" style="justify-content:flex-start"><button class="btn primario" data-cerrar>Continue</button></div></div>`;
      },
    });
  }

  function resumen(r) {
    const filas = Object.entries(r.ventas).map(([id, n]) => `<tr><td>${def.ITEMS[id].nombre} ×${n}</td><td>${def.ITEMS[id].venta * n}</td></tr>`).join('');
    abrir({
      cerrable: false,
      pintar() {
        panel.innerHTML = `<div class="panel-cab"><h2>Day ${(r.dia - 1) % 28 + 1} of Spring</h2></div><div class="panel-cuerpo">
          ${filas ? `<table class="resumen"><tbody>${filas}</tbody><tfoot><tr><td>Total Earned</td><td>${r.total}</td></tr></tfoot></table>` : '<p class="ayuda">You did not ship anything yesterday.</p>'}
          ${r.notas.map(n => `<p class="ayuda" style="margin-top:8px">${n}</p>`).join('')}
          <p class="ayuda">Game saved.</p>
          <div class="acciones" style="justify-content:flex-start"><button class="btn primario" data-cerrar>Start the Day</button></div></div>`;
      },
    });
  }

  // Single click handler for all panels.
  panel.addEventListener('click', e => {
    const t = e.target;
    if (t.closest('[data-cerrar]')) { audio?.sfx.clic(); return cerrar(); }
    const tab = t.closest('[data-tab]'); if (tab) { audio?.sfx.clic(); pestanaMenu = tab.dataset.tab; marcada = -1; return actual.pintar(); }
    const fab = t.closest('[data-fab]'); if (fab) { audio?.sfx.clic(); return enviar({ tipo: 'fabricar', receta: +fab.dataset.fab }); }
    const com = t.closest('[data-comprar]'); if (com) { audio?.sfx.moneda(); return enviar({ tipo: 'comprar', item: com.dataset.comprar, n: +com.dataset.n }); }
    const ves = t.closest('[data-vestir]'); if (ves) { marcada = -1; return enviar({ tipo: 'vestir', i: +ves.dataset.vestir }); }
    const rap = t.closest('[data-rapido]');
    if (rap) {
      const i = +rap.dataset.rapido, rango = i < 12 ? [12, 24] : [0, 12];
      let dest = -1;
      for (let k = rango[0]; k < rango[1]; k++) if (!yo().inv[k]) { dest = k; break; }
      if (dest < 0) dest = rango[0];
      marcada = -1;
      return enviar({ tipo: 'moverInv', de: i, a: dest });
    }
    const inv = t.closest('#g-inv .casilla');
    if (inv) {
      const i = +inv.dataset.i;
      if (e.shiftKey && yo().inv[i]) {
        const rango = i < 12 ? [12, 24] : [0, 12];
        let dest = -1;
        for (let k = rango[0]; k < rango[1]; k++) if (!yo().inv[k]) { dest = k; break; }
        if (dest < 0) dest = rango[0];
        marcada = -1;
        return enviar({ tipo: 'moverInv', de: i, a: dest });
      }
      if (marcada < 0) { if (yo().inv[i]) marcada = i; }
      else { enviar({ tipo: 'moverInv', de: marcada, a: i }); marcada = -1; }
      return actual.pintar();
    }
    const c = t.closest('#g-cofre .casilla'); if (c) return enviar({ tipo: 'cofreSacar', uid: actual.uid, i: +c.dataset.i });
    const m = t.closest('#g-moch .casilla'); if (m) return enviar({ tipo: 'cofreGuardar', uid: actual.uid, i: +m.dataset.i });
    if (t.id === 'btn-dormir') { cerrar(); return enviar({ tipo: 'dormir' }); }
    const piso = t.closest('[data-piso]'); if (piso) { cerrar(); return enviar({ tipo: 'entrarMazmorra', mazmorra: piso.dataset.maz, piso: +piso.dataset.piso }); }
    if (t.id === 'btn-charlar') { audio?.sfx.clic(); charlaIdx++; return actual.pintar(); }
    if (t.id === 'btn-salir-menu') { audio?.sfx.clic(); cerrar(); onSalirMenu?.(); return; }
    if (t.id === 'btn-mute') { audio?.alternarMute(); return actual.pintar(); }
    if (t.id === 'btn-reiniciar') {
      panel.querySelector('.panel-cuerpo').insertAdjacentHTML('beforeend', `<p class="ayuda" style="margin-top:10px">Are you sure? Your current save will be lost.</p><button class="btn primario" id="btn-reiniciar-si">Yes, start over</button>`);
      t.disabled = true; return;
    }
    if (t.id === 'btn-reiniciar-si') { cerrar(); onReiniciar?.(); }
  });

  // Core events → UI (only for this player or global).
  function eventos(ev) {
    const yoId = sesion().jugadorId;
    for (const e of ev) {
      if (e.jugador && e.jugador !== yoId) continue;
      if (e.tipo === 'aviso') aviso(e.txt, e.malo);
      if (e.tipo === 'abrir') ({ tienda, dormir, cofre: () => cofre(e.uid), mazmorra: () => mazmorra(e) })[e.panel]?.();
      if (e.tipo === 'resumen') resumen(e);
      if (e.tipo === 'botin') botin(e);
      if (e.tipo === 'derrota') derrota(e);
      if (e.tipo === 'piso' && (e.bajada || e.jefe)) aviso(e.jefe ? `Floor ${e.piso}: ${e.jefe} awaits!` : `Floor ${e.piso}`, !!e.jefe);
    }
  }

  let onReiniciar = null, onSalirMenu = null;
  return { pintarHUD, eventos, menu, cerrar, abierto, cerrable, aviso, alReiniciar: f => { onReiniciar = f; }, alSalirMenu: f => { onSalirMenu = f; } };
}
