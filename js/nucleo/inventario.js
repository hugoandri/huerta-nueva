// Inventario de un jugador: 24 casillas {id, n}; las 12 primeras son la barra.
export const TAM_INVENTARIO = 24;
export const PILA_MAX = 999;

export const esHerramienta = (def, id) => def.ITEMS[id]?.tipo === 'herramienta';

// Agrega n unidades; devuelve las que no cupieron.
export function agregar(def, jugador, id, n = 1) {
  const inv = jugador.inv;
  if (!esHerramienta(def, id))
    for (const s of inv) if (s && s.id === id && n > 0) { const k = Math.min(n, PILA_MAX - s.n); s.n += k; n -= k; }
  for (let i = 0; i < inv.length && n > 0; i++)
    if (!inv[i]) { const k = esHerramienta(def, id) ? 1 : Math.min(n, PILA_MAX); inv[i] = { id, n: k }; n -= k; }
  return n;
}

export const cuenta = (jugador, id) => jugador.inv.reduce((a, s) => a + (s && s.id === id ? s.n : 0), 0);

export function sacar(jugador, id, n) {
  for (let i = jugador.inv.length - 1; i >= 0 && n > 0; i--) {
    const s = jugador.inv[i];
    if (s && s.id === id) { const k = Math.min(n, s.n); s.n -= k; n -= k; if (!s.n) jugador.inv[i] = null; }
  }
}

// Cuántas unidades caben (para comprar o fabricar sin perder nada).
export function espacioPara(def, jugador, id) {
  if (esHerramienta(def, id)) return jugador.inv.filter(s => !s).length;
  return jugador.inv.reduce((a, s) => a + (!s ? PILA_MAX : s.id === id ? PILA_MAX - s.n : 0), 0);
}

export function gastarMano(jugador, n = 1) {
  const s = jugador.inv[jugador.sel];
  if (!s) return;
  s.n -= n;
  if (s.n <= 0) jugador.inv[jugador.sel] = null;
}
