// Índices de búsqueda sobre data/contenido.json. "def" viaja junto al estado a todas las reglas.
export function indexar(C) {
  const porId = lista => Object.fromEntries(lista.map(x => [x.id, x]));
  return {
    C,
    T: C.mundo.tile, W: C.mundo.ancho, H: C.mundo.alto,
    ITEMS: porId(C.items),
    OBJ: porId(C.objetos),
    CULT: porId(C.cultivos),
    MON: porId(C.monstruos || []),
  };
}
