// Motor de audio reactivo procedural (Web Audio API, 0 dependencias externas, 0 KB).
// Traduce los eventos del núcleo y las acciones de UI en efectos de sonido cálidos y melodía ambiental.
export function crearAudio() {
  let ctx = null, silencio = localStorage.getItem('huerta-nueva-mute') === '1';

  function iniciar() {
    if (silencio) return null;
    if (!ctx) {
      const AC = window.AudioContext || window.webkitAudioContext;
      if (AC) ctx = new AC();
    }
    if (ctx && ctx.state === 'suspended') ctx.resume().catch(() => {});
    return ctx;
  }

  function tono(f0, f1, dur = 0.1, tipo = 'triangle', vol = 0.14, delay = 0) {
    const c = iniciar(); if (!c) return;
    const t0 = c.currentTime + delay, t1 = t0 + dur;
    const osc = c.createOscillator(), g = c.createGain();
    osc.type = tipo;
    osc.frequency.setValueAtTime(f0, t0);
    if (f1 && f1 !== f0) osc.frequency.exponentialRampToValueAtTime(Math.max(20, f1), t1);
    g.gain.setValueAtTime(0.001, t0);
    g.gain.linearRampToValueAtTime(vol, t0 + Math.min(0.015, dur * 0.2));
    g.gain.exponentialRampToValueAtTime(0.001, t1);
    osc.connect(g); g.connect(c.destination);
    osc.start(t0); osc.stop(t1 + 0.01);
  }

  function ruido(dur = 0.08, freq = 900, vol = 0.12, tipoFiltro = 'bandpass') {
    const c = iniciar(); if (!c) return;
    const t0 = c.currentTime, n = Math.max(1, Math.floor(c.sampleRate * dur));
    const buf = c.createBuffer(1, n, c.sampleRate), d = buf.getChannelData(0);
    for (let i = 0; i < n; i++) d[i] = (Math.random() * 2 - 1) * (1 - i / n);
    const src = c.createBufferSource(), f = c.createBiquadFilter(), g = c.createGain();
    src.buffer = buf; f.type = tipoFiltro; f.frequency.setValueAtTime(freq, t0);
    g.gain.setValueAtTime(vol, t0); g.gain.exponentialRampToValueAtTime(0.001, t0 + dur);
    src.connect(f); f.connect(g); g.connect(c.destination);
    src.start(t0);
  }

  const sfx = {
    clic: () => tono(520, 640, 0.04, 'triangle', 0.08),
    paso: (cueva) => ruido(0.035, cueva ? 1400 : 650, 0.04, 'bandpass'),
    arar: () => { ruido(0.09, 420, 0.14, 'lowpass'); tono(140, 90, 0.08, 'triangle', 0.1); },
    regar: () => { ruido(0.14, 1600, 0.1, 'bandpass'); tono(380, 560, 0.12, 'sine', 0.07); },
    sembrar: () => { tono(440, 660, 0.06, 'sine', 0.08); },
    cosechar: () => { tono(523, 659, 0.07, 'triangle', 0.13); tono(659, 880, 0.09, 'triangle', 0.13, 0.06); },
    sacudir: () => { ruido(0.06, 950, 0.14); tono(180, 110, 0.07, 'square', 0.08); },
    golpe: () => { ruido(0.08, 2200, 0.12, 'highpass'); tono(320, 140, 0.09, 'sawtooth', 0.1); },
    dano: () => { tono(190, 75, 0.18, 'sawtooth', 0.18); ruido(0.12, 350, 0.15, 'lowpass'); },
    moneda: () => { tono(987, 1318, 0.06, 'sine', 0.11); tono(1318, 1567, 0.12, 'sine', 0.11, 0.06); },
    comer: () => { tono(392, 523, 0.08, 'sine', 0.11); tono(523, 659, 0.1, 'sine', 0.11, 0.08); },
    ropa: () => { ruido(0.09, 1200, 0.09); tono(440, 587, 0.09, 'triangle', 0.09); },
    error: () => { tono(180, 130, 0.12, 'square', 0.09); },
    jefe: () => { tono(146, 138, 0.22, 'sawtooth', 0.15); tono(110, 98, 0.35, 'sawtooth', 0.16, 0.2); },
    logro: () => {
      [523.25, 659.25, 783.99, 1046.5].forEach((f, i) => tono(f, f * 1.01, 0.14, 'triangle', 0.13, i * 0.08));
    },
  };

  function eventos(ev, yoId) {
    for (const e of ev) {
      if (e.jugador && e.jugador !== yoId) continue;
      if (e.tipo === 'golpe') sfx.golpe();
      else if (e.tipo === 'dano') sfx.dano();
      else if (e.tipo === 'sacudir') sfx.sacudir();
      else if (e.tipo === 'ropa') sfx.ropa();
      else if (e.tipo === 'botin' || e.tipo === 'carta') sfx.logro();
      else if (e.tipo === 'resumen') sfx.moneda();
      else if (e.tipo === 'piso') e.jefe ? sfx.jefe() : sfx.paso(true);
      else if (e.tipo === 'aviso' && e.malo) sfx.error();
      else if (e.tipo === 'flotar') {
        if (e.txt?.includes('♥') || e.txt?.includes('energy')) sfx.comer();
        else if (e.txt?.includes('Can full') || e.txt?.includes('Watering')) sfx.regar();
        else if (e.txt?.startsWith('+')) sfx.cosechar();
      }
    }
  }

  function alternarMute() {
    silencio = !silencio;
    try { localStorage.setItem('huerta-nueva-mute', silencio ? '1' : '0'); } catch (e) { /* sin storage */ }
    if (!silencio) sfx.clic();
    return silencio;
  }

  return { sfx, eventos, alternarMute, get silencio() { return silencio; } };
}
