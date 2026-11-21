// Interfaz HTML (HUD, barra, paneles). Lee el estado y manda acciones a la sesión;
// nunca cambia el estado por su cuenta.
import { cuenta } from '../nucleo/inventario.js';
import { objetoPorUid } from '../nucleo/mundo.js';

const $ = s => document.querySelector(s);
const fmtHora = m => `${Math.floor(m / 60) % 24}:${String(Math.floor((m % 60) / 10) * 10).padStart(2, '0')}`;

export function crearUI({ def, G, sesion }) {
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
    if (!s) return `<button class="casilla" data-i="${i}" aria-label="Casilla vacía">${t}</button>`;
    const it = def.ITEMS[s.id];
    return `<button class="casilla" data-i="${i}" title="${it.nombre}" aria-label="${it.nombre}${s.n > 1 ? ', ' + s.n : ''}">${t}${icono(s.id)}${s.n > 1 ? `<span class="n">${s.n}</span>` : ''}</button>`;
  }

  // La barra y los paneles se repintan solo si cambió algo que muestran.
  let barraCache = '';
  function pintarBarra() {
    const j = yo(), clave = JSON.stringify(j.inv.slice(0, 12)) + j.sel + j.aguaRegadera;
    if (clave !== barraCache) {
      barraCache = clave;
      barra.innerHTML = j.inv.slice(0, 12).map((s, i) => casilla(s, i, true)).join('');
      barra.children[j.sel]?.classList.add('sel');
      const s = j.inv[j.sel];
      $('#nombre-sel').textContent = s ? def.ITEMS[s.id].nombre + (s.id === 'regadera' ? ` · agua ${j.aguaRegadera}/${def.ITEMS.regadera.capacidad}` : '') : 'Mano vacía';
    }
  }
  barra.addEventListener('click', e => { const b = e.target.closest('.casilla'); if (b) enviar({ tipo: 'seleccionar', i: +b.dataset.i }); });

  function pintarHUD() {
    const e = S(), j = yo(), t = e.tiempo, enCueva = j.lugar === 'mazmorra';
    const dia = enCueva ? `${def.MAZ[e.mazmorra.actual]?.nombre} · piso ${e.mazmorra.piso} de ${def.MAZ[e.mazmorra.actual]?.pisos}`
      : `Primavera · día ${(t.dia - 1) % 28 + 1}${t.dia > 28 ? ` · año ${Math.floor((t.dia - 1) / 28) + 1}` : ''}`;
    const clave = dia + fmtHora(t.min) + j.dinero + Math.round(j.energia) + j.aguaRegadera + t.lluvia + j.vida + j.vidaMax;
    pintarJefe(e, j);
    if (clave !== hudCache) {
      hudCache = clave;
      $('#hud-dia').textContent = dia;
      $('#hud-hora').textContent = fmtHora(t.min);
      $('#hud-clima').textContent = !enCueva && t.lluvia ? 'Lluvia: todo regado' : '';
      // corazones: cada uno vale 2 puntos de vida
      let c = '';
      for (let k = 0; k < j.vidaMax / 2; k++) {
        const v = j.vida - k * 2;
        c += `<span class="corazon ${v >= 2 ? 'lleno' : v >= 1 ? 'medio' : ''}" aria-hidden="true"></span>`;
      }
      $('#hud-vida').innerHTML = c;
      $('#hud-vida').setAttribute('aria-label', `Vida ${j.vida / 2} de ${j.vidaMax / 2} corazones`);
      $('#hud-dinero').textContent = j.dinero.toLocaleString('es');
      const pe = j.energia / j.energiaMax, pa = j.aguaRegadera / (def.ITEMS.regadera?.capacidad || 20);
      $('#m-energia .relleno').style.height = pe * 100 + '%'; $('#m-energia').classList.toggle('bajo', pe < 0.2);
      $('#m-agua .relleno').style.height = pa * 100 + '%';
    }
    pintarBarra();
    if (actual?.firma) { const f = actual.firma(); if (f !== actual._firma) { actual._firma = f; actual.pintar(); } }
  }

  // Barra del jefe: aparece cuando el jefe del piso está cerca.
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

  // ---------------- paneles
  function abrir(p) { actual = p; actual._firma = p.firma?.(); velo.hidden = false; p.pintar(); panel.querySelector('button')?.focus({ preventScroll: true }); }
  function cerrar() { velo.hidden = true; actual = null; }
  const abierto = () => !velo.hidden;
  const cerrable = () => actual?.cerrable !== false;
  velo.addEventListener('pointerdown', e => { if (e.target === velo && cerrable()) cerrar(); });
  const cab = (titulo, extra = '') => `<div class="panel-cab"><h2>${titulo}</h2>${extra}<button class="btn cerrar" data-cerrar aria-label="Cerrar">✕</button></div>`;
  const firmaInv = () => JSON.stringify(yo().inv) + yo().dinero;

  function menu(pestana) {
    if (pestana) pestanaMenu = pestana;
    marcada = -1;
    abrir({
      firma: firmaInv,
      pintar() {
        const j = yo();
        const tabs = [['mochila', 'Mochila'], ['crafteo', 'Crafteo'], ['ayuda', 'Cómo jugar']]
          .map(([t, n]) => `<button class="btn ${pestanaMenu === t ? 'activo' : ''}" data-tab="${t}">${n}</button>`).join('');
        let cuerpo;
        if (pestanaMenu === 'mochila') {
          cuerpo = `<p class="ayuda">Ropa de granja: <b>${def.ITEMS[j.ropa]?.nombre}</b> · Ropa de batalla: <b>${def.ITEMS[j.ropaBatalla]?.nombre}</b> (se pone sola en la mazmorra).</p>
            <p class="ayuda">Toca una casilla y luego otra para moverla. Las 12 primeras son tu barra.</p>
            <div class="grilla" id="g-inv">${j.inv.map((s, i) => casilla(s, i, false)).join('')}</div>`;
        } else if (pestanaMenu === 'crafteo') {
          cuerpo = `<p class="ayuda">Las recetas salen de <code>data/contenido.json</code>. Lo fabricado se coloca en el mundo con <kbd>Espacio</kbd>.</p><div class="lista">` +
            def.C.recetas.map((r, i) => {
              const it = def.ITEMS[r.resultado], puede = Object.entries(r.ingredientes).every(([id, n]) => cuenta(j, id) >= n);
              const ing = Object.entries(r.ingredientes).map(([id, n]) => `<span class="ing ${cuenta(j, id) < n ? 'falta' : ''}">${icono(id)}${cuenta(j, id)}/${n}</span>`).join('');
              return `<div class="fila">${icono(r.resultado)}<div><div class="titulo">${it.nombre}${r.cantidad > 1 ? ' ×' + r.cantidad : ''}</div><div class="det">${it.descripcion || ''}</div><div style="margin-top:4px">${ing}</div></div>
                <div class="acciones"><button class="btn primario" data-fab="${i}" ${puede ? '' : 'disabled'}>Fabricar</button></div></div>`;
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
              <dt>Casa</dt><dd>Úsala para dormir y pasar al día siguiente. La partida se guarda sola.</dd>
              <dt>Ropa</dt><dd>Tienes ropa de granja y ropa de batalla. La de batalla se pone sola al entrar a una mazmorra. Para cambiar una, tenla en la mano y pulsa Usar.</dd>
              <dt>Mazmorras</dt><dd>La cueva está a la derecha de la granja. Elige la Mina de cobre o la Cripta olvidada (se abre al llegar al piso 5 de la mina).</dd>
              <dt>Pelear</dt><dd>Usa una espada frente a un monstruo. Las pociones (F) curan corazones.</dd>
              <dt>Horno</dt><dd>Mete 3 minerales y en unas horas tendrás un lingote para armas y ropa.</dd>
            </dl>
            <button class="btn" id="btn-reiniciar">Borrar partida y empezar de nuevo</button>`;
        }
        panel.innerHTML = cab('Menú', `<div class="pestanas">${tabs}</div>`) + `<div class="panel-cuerpo">${cuerpo}</div>`;
        if (pestanaMenu === 'mochila') {
          panel.querySelector(`#g-inv [data-i="${j.sel}"]`)?.classList.add('sel');
          if (marcada >= 0) panel.querySelector(`#g-inv [data-i="${marcada}"]`)?.classList.add('marcada');
        }
      },
    });
  }

  function tienda() {
    abrir({
      firma: firmaInv,
      pintar() {
        const j = yo();
        panel.innerHTML = cab('Puesto de semillas') + `<div class="panel-cuerpo"><p class="ayuda">Tienes <span class="precio">${j.dinero}</span> monedas. Para vender, usa la caja de envíos.</p><div class="lista">` +
          def.C.tienda.map(id => {
            const it = def.ITEMS[id], c = def.CULT[it.cultivo];
            return `<div class="fila">${icono(id)}<div><div class="titulo">${it.nombre}</div><div class="det">${c ? `Crece en ${c.dias} días regados · se vende a ${def.ITEMS[c.producto].venta}` : ''}</div></div>
              <div class="acciones"><span class="precio">${it.compra}</span><button class="btn" data-comprar="${id}" data-n="1" ${j.dinero < it.compra ? 'disabled' : ''}>×1</button><button class="btn primario" data-comprar="${id}" data-n="5" ${j.dinero < it.compra * 5 ? 'disabled' : ''}>×5</button></div></div>`;
          }).join('') + '</div></div>';
      },
    });
  }

  function cofre(uid) {
    abrir({
      uid,
      firma: () => firmaInv() + JSON.stringify(objetoPorUid(def, S().granja, uid)?.datos),
      pintar() {
        const o = objetoPorUid(def, S().granja, uid);
        if (!o) return cerrar();
        const items = o.datos.items || Array(12).fill(null);
        panel.innerHTML = cab('Cofre') + `<div class="panel-cuerpo"><p class="ayuda">Toca un objeto para pasarlo entre el cofre y la mochila.</p>
          <div class="sub">Cofre</div><div class="grilla" id="g-cofre">${items.map((s, i) => casilla(s, i, false)).join('')}</div>
          <div class="sub">Mochila</div><div class="grilla" id="g-moch">${yo().inv.map((s, i) => casilla(s, i, false)).join('')}</div></div>`;
      },
    });
  }

  function dormir() {
    abrir({
      pintar() {
        panel.innerHTML = cab('Casa') + `<div class="panel-cuerpo"><p class="ayuda">Son las ${fmtHora(S().tiempo.min)}. ¿Dormir hasta mañana? La partida se guarda al despertar.</p>
          <div class="acciones" style="justify-content:flex-start"><button class="btn primario" id="btn-dormir">Dormir</button><button class="btn" data-cerrar>Seguir despierta</button></div></div>`;
      },
    });
  }

  // Panel de la cueva: elegir a qué mazmorra ir y desde qué piso.
  function mazmorra(e) {
    abrir({
      pintar() {
        const j = yo(), batalla = def.ITEMS[j.ropaBatalla];
        const filas = e.enCurso
          ? `<div class="fila">${icono(j.ropaBatalla)}<div><div class="titulo">${def.MAZ[e.enCurso.id].nombre}</div><div class="det">Tu grupo está en el piso ${e.enCurso.piso}.</div></div>
              <div class="acciones"><button class="btn primario" data-maz="${e.enCurso.id}" data-piso="${e.enCurso.piso}">Unirte</button></div></div>`
          : e.mazmorras.map(m => `<div class="fila ${m.abierta ? '' : 'cerrada'}"><div class="sello">${m.abierta ? m.record : '🔒'}</div>
              <div><div class="titulo">${m.nombre}</div><div class="det">${m.descripcion}</div>
              <div class="det">${m.abierta ? `${m.pisos} pisos · récord: piso ${m.record || 0} · completada ${m.completada} ${m.completada === 1 ? 'vez' : 'veces'}` : m.requiere}</div></div>
              <div class="acciones">${m.abierta ? m.disponibles.map((p, k) => `<button class="btn ${k === m.disponibles.length - 1 ? 'primario' : ''}" data-maz="${m.id}" data-piso="${p}">Piso ${p}</button>`).join('') : ''}</div></div>`).join('');
        panel.innerHTML = cab('¿A qué mazmorra vas?') + `<div class="panel-cuerpo">
          <p class="ayuda">Al entrar te pones la ropa de batalla: <b>${batalla?.nombre}</b>. ${batalla?.descripcion || ''} Para cambiarla, ten otra ropa de batalla en la mano y pulsa Usar.</p>
          <div class="lista">${filas}</div>
          <p class="ayuda" style="margin-top:10px">Se avanza subiendo por la escalera de mano de cada piso. Solo se sale por el arco del piso 1 o por el que aparece al vencer al jefe del último piso. Jefes en los pisos 5 y 10; vencer al del 5 permite empezar en el 6. Si te quedas sin corazones vuelves a la granja.</p></div>`;
      },
    });
  }

  function botin(b) {
    abrir({
      pintar() {
        panel.innerHTML = cab(b.titulo) + `<div class="panel-cuerpo"><p class="ayuda">Tu parte del botín (cada jugador recibe la suya):</p><div class="lista">` +
          b.items.map(({ item, n }) => `<div class="fila">${icono(item)}<div><div class="titulo">${def.ITEMS[item].nombre}</div></div><div class="acciones"><span class="precio">×${n}</span></div></div>`).join('') +
          `</div><div class="acciones" style="justify-content:flex-start;margin-top:12px"><button class="btn primario" data-cerrar>Guardar en la mochila</button></div></div>`;
      },
    });
  }

  function derrota(d) {
    abrir({
      pintar() {
        panel.innerHTML = cab('Te desmayaste') + `<div class="panel-cuerpo"><p class="ayuda">Te sacaron de la mazmorra y despertaste frente a la cueva. Pagaste ${d.multa} monedas de atención.</p>
          <p class="ayuda">Consejo: fabrica una espada, pociones y la armadura de aventurera.</p>
          <div class="acciones" style="justify-content:flex-start"><button class="btn primario" data-cerrar>Seguir</button></div></div>`;
      },
    });
  }

  function resumen(r) {
    const filas = Object.entries(r.ventas).map(([id, n]) => `<tr><td>${def.ITEMS[id].nombre} ×${n}</td><td>${def.ITEMS[id].venta * n}</td></tr>`).join('');
    abrir({
      cerrable: false,
      pintar() {
        panel.innerHTML = `<div class="panel-cab"><h2>Día ${(r.dia - 1) % 28 + 1} de primavera</h2></div><div class="panel-cuerpo">
          ${filas ? `<table class="resumen"><tbody>${filas}</tbody><tfoot><tr><td>Total ganado</td><td>${r.total}</td></tr></tfoot></table>` : '<p class="ayuda">Ayer no enviaste nada para vender.</p>'}
          ${r.notas.map(n => `<p class="ayuda" style="margin-top:8px">${n}</p>`).join('')}
          <p class="ayuda">Partida guardada.</p>
          <div class="acciones" style="justify-content:flex-start"><button class="btn primario" data-cerrar>Empezar el día</button></div></div>`;
      },
    });
  }

  // Un único manejador de clics para todos los paneles.
  panel.addEventListener('click', e => {
    const t = e.target;
    if (t.closest('[data-cerrar]')) return cerrar();
    const tab = t.closest('[data-tab]'); if (tab) { pestanaMenu = tab.dataset.tab; marcada = -1; return actual.pintar(); }
    const fab = t.closest('[data-fab]'); if (fab) return enviar({ tipo: 'fabricar', receta: +fab.dataset.fab });
    const com = t.closest('[data-comprar]'); if (com) return enviar({ tipo: 'comprar', item: com.dataset.comprar, n: +com.dataset.n });
    const inv = t.closest('#g-inv .casilla');
    if (inv) {
      const i = +inv.dataset.i;
      if (marcada < 0) { if (yo().inv[i]) marcada = i; }
      else { enviar({ tipo: 'moverInv', de: marcada, a: i }); marcada = -1; }
      return actual.pintar();
    }
    const c = t.closest('#g-cofre .casilla'); if (c) return enviar({ tipo: 'cofreSacar', uid: actual.uid, i: +c.dataset.i });
    const m = t.closest('#g-moch .casilla'); if (m) return enviar({ tipo: 'cofreGuardar', uid: actual.uid, i: +m.dataset.i });
    if (t.id === 'btn-dormir') { cerrar(); return enviar({ tipo: 'dormir' }); }
    const piso = t.closest('[data-piso]'); if (piso) { cerrar(); return enviar({ tipo: 'entrarMazmorra', mazmorra: piso.dataset.maz, piso: +piso.dataset.piso }); }
    if (t.id === 'btn-reiniciar') {
      panel.querySelector('.panel-cuerpo').insertAdjacentHTML('beforeend', `<p class="ayuda" style="margin-top:10px">¿Seguro? Se pierde la partida guardada.</p><button class="btn primario" id="btn-reiniciar-si">Sí, empezar de nuevo</button>`);
      t.disabled = true; return;
    }
    if (t.id === 'btn-reiniciar-si') { cerrar(); onReiniciar?.(); }
  });

  // Eventos del núcleo → interfaz (solo los de este jugador o los de todos).
  function eventos(ev) {
    const yoId = sesion().jugadorId;
    for (const e of ev) {
      if (e.jugador && e.jugador !== yoId) continue;
      if (e.tipo === 'aviso') aviso(e.txt, e.malo);
      if (e.tipo === 'abrir') ({ tienda, dormir, cofre: () => cofre(e.uid), mazmorra: () => mazmorra(e) })[e.panel]?.();
      if (e.tipo === 'resumen') resumen(e);
      if (e.tipo === 'botin') botin(e);
      if (e.tipo === 'derrota') derrota(e);
      if (e.tipo === 'piso' && (e.bajada || e.jefe)) aviso(e.jefe ? `Piso ${e.piso}: ¡${e.jefe} te espera!` : `Piso ${e.piso}`, !!e.jefe);
    }
  }

  let onReiniciar = null;
  return { pintarHUD, eventos, menu, cerrar, abierto, cerrable, aviso, alReiniciar: f => { onReiniciar = f; } };
}
