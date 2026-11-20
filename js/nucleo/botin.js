// Botín propio: al terminar una mazmorra, cada jugador recibe su propio botín sorteado
// por separado. Nadie le quita nada a nadie y no hay que votar.
//
// Es una función pura y reproducible: con la misma semilla de la mazmorra y los mismos
// jugadores, el resultado es siempre el mismo. Hoy la llama el cliente; en la versión online
// la llamará el servidor y escribirá el resultado en el inventario de cada jugador, para que
// nadie pueda inventarse objetos.
import { mulberry32, hashTexto } from './azar.js';

// tabla: { tiradas, entradas: [{ item, peso, min, max }], garantizado?: [{ item, n }] }
export function tirarBotin(tabla, r) {
  const total = tabla.entradas.reduce((a, x) => a + x.peso, 0);
  const res = {};
  for (const g of tabla.garantizado || []) res[g.item] = (res[g.item] || 0) + g.n;
  for (let t = 0; t < tabla.tiradas; t++) {
    let p = r() * total;
    const x = tabla.entradas.find(x => (p -= x.peso) < 0) || tabla.entradas.at(-1);
    const n = x.min + Math.floor(r() * (x.max - x.min + 1));
    res[x.item] = (res[x.item] || 0) + n;
  }
  return Object.entries(res).map(([item, n]) => ({ item, n }));
}

// Devuelve { jugadorId: [{item, n}] }. Cada jugador tira con una semilla derivada de la
// semilla de la mazmorra y su id, así el resultado no depende del orden de los jugadores.
export function repartirBotin(tabla, jugadorIds, semillaMazmorra) {
  const reparto = {};
  for (const id of jugadorIds) reparto[id] = tirarBotin(tabla, mulberry32(hashTexto(`${semillaMazmorra}:${id}`)));
  return reparto;
}
