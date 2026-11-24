// Azar reproducible. Nada del núcleo usa Math.random: con la misma semilla y las mismas
// acciones, el resultado es idéntico. Eso permite que un servidor repita y valide una
// partida, y que varios jugadores generen la misma mazmorra a partir de una semilla.

export function mulberry32(a) {
  return function () {
    a |= 0; a = (a + 0x6D2B79F5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

// Hash estable de texto → entero de 32 bits (para derivar semillas: "semilla:jugador").
export function hashTexto(s) {
  let h = 2166136261;
  for (let i = 0; i < s.length; i++) { h ^= s.charCodeAt(i); h = Math.imul(h, 16777619); }
  return h >>> 0;
}

// Siguiente número al azar del estado (avanza su contador "paso").
export function azar(estado) {
  estado.paso = (estado.paso || 0) + 1;
  return mulberry32((estado.semilla ^ Math.imul(estado.paso, 0x9E3779B1)) >>> 0)();
}

export const entre = (r, a, b) => a + Math.floor(r * (b - a + 1));
