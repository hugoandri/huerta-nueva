using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using HuertaNueva;

// Ejecutable independiente: prueba el C# real del juego sin arrancar Unity.
public static class NucleoPruebas
{
    static Def def;
    static string Foto(object valor) => JsonSerializer.Serialize(valor, new JsonSerializerOptions { IncludeFields = true });
    static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion) throw new Exception(mensaje);
    }
    static Estado Nuevo(uint semilla = 7) => Mundo.CrearEstado(def, semilla, "yo", def.personajes[0].id);
    static void Actuar(Estado e, Accion a) { a.jugador = "yo"; Reglas.Aplicar(e, def, a); }
    static void Campo(Estado e)
    {
        var j = e.BuscarJugador("yo");
        j.x = 8.5f * def.T; j.y = 11 * def.T + 6; j.dir = "east";
    }

    public static int Main(string[] args)
    {
        try
        {
            def = Def.Cargar(File.ReadAllText(args[0]));
            Prueba("generación reproducible", () =>
            {
                Exigir(Foto(Nuevo()) == Foto(Nuevo()), "La misma semilla debe repetir la granja");
                Exigir(Foto(Nuevo(7).granja) != Foto(Nuevo(8).granja), "Otra semilla debe cambiar la granja");
            });
            Prueba("ciclo completo de cultivo", () =>
            {
                var e = Nuevo(); Campo(e); var j = e.BuscarJugador("yo");
                var (x, y) = Reglas.CeldaFrente(def, j); int i = Mundo.Idx(def, x, y);
                Actuar(e, new Accion { tipo = "seleccionar", i = 0 });
                Actuar(e, new Accion { tipo = "usar", x = x, y = y });
                Exigir(e.granja.suelo[i] == 1, "Arar");
                Actuar(e, new Accion { tipo = "seleccionar", i = 4 });
                Actuar(e, new Accion { tipo = "usar", x = x, y = y });
                Exigir(e.granja.CultivoEn(i)?.id == "rabano", "Sembrar");
                for (int d = 0; d < def.CULT["rabano"].dias; d++)
                {
                    Campo(e);
                    Actuar(e, new Accion { tipo = "seleccionar", i = 1 });
                    Actuar(e, new Accion { tipo = "usar", x = x, y = y });
                    Exigir(e.granja.regado[i] == 1, "Regar");
                    Actuar(e, new Accion { tipo = "dormir" });
                }
                Campo(e); Actuar(e, new Accion { tipo = "usar", x = x, y = y });
                Exigir(Inventario.Cuenta(j, "rabano") == 1 && e.granja.CultivoEn(i) == null, "Cosechar");
            });
            Prueba("rechazo de acciones fuera de alcance", () =>
            {
                var e = Nuevo(); Campo(e); string antes = Foto(e);
                Actuar(e, new Accion { tipo = "usar", x = 14, y = 11 });
                Exigir(Foto(e) == antes, "Una acción lejana no debe modificar el estado");
            });
            Prueba("fabricación y materiales", () =>
            {
                var e = Nuevo(); var j = e.BuscarJugador("yo"); string antes = Foto(e);
                Actuar(e, new Accion { tipo = "fabricar", receta = 2 });
                Exigir(Foto(e) == antes, "No fabricar sin materiales");
                Inventario.Agregar(def, j, "madera", 25);
                Actuar(e, new Accion { tipo = "fabricar", receta = 2 });
                Exigir(Inventario.Cuenta(j, "madera") == 5 && Inventario.Cuenta(j, "cofre") == 1, "Consumir materiales y entregar cofre");
            });
            Prueba("compra y saldo", () =>
            {
                var e = Nuevo(); var j = e.BuscarJugador("yo"); int saldo = j.dinero;
                Actuar(e, new Accion { tipo = "comprar", item = "semilla_zanahoria", n = 5 });
                Exigir(j.dinero == saldo - 5 * def.ITEMS["semilla_zanahoria"].compra, "Descontar precio");
                j.dinero = 0; string antes = Foto(e);
                Actuar(e, new Accion { tipo = "comprar", item = "semilla_calabaza", n = 1 });
                Exigir(Foto(e) == antes, "No comprar sin saldo");
            });
            Prueba("mazmorras, jefes y entrada", () =>
            {
                foreach (var cfg in def.mazmorras)
                {
                    foreach (int piso in new[] { 1, 5, 10 })
                    {
                        var z = Mazmorra.GenerarPiso(def, cfg.id, 123, piso);
                        Exigir(Foto(z) == Foto(Mazmorra.GenerarPiso(def, cfg.id, 123, piso)), "Piso reproducible");
                        Exigir(z.entrada != null && z.muro.Length == def.W * def.H, "Mapa y entrada válidos");
                        if (cfg.jefes.TryGetValue(piso, out var jefe))
                            Exigir(z.monstruos.Any(m => m.id == jefe), "Jefe presente");
                    }
                }
                var e = Nuevo(); var j = e.BuscarJugador("yo"); Mundo.FrenteACueva(def, j);
                var inicial = def.mazmorras.First(m => Reglas.MazmorraAbierta(e, m));
                Actuar(e, new Accion { tipo = "entrarMazmorra", mazmorra = inicial.id, piso = 1 });
                Exigir(j.lugar == "mazmorra" && e.mazmorra.enCurso, "Entrar desde la granja");
            });
            Prueba("botín independiente del orden de jugadores", () =>
            {
                foreach (var tabla in def.botines.Values)
                {
                    var a = Botin.Repartir(tabla, new[] { "ana", "luis" }, 123);
                    var b = Botin.Repartir(tabla, new[] { "luis", "ana" }, 123);
                    Exigir(Foto(a["ana"]) == Foto(b["ana"]) && Foto(a["luis"]) == Foto(b["luis"]), "Botín propio reproducible");
                }
            });
            Prueba("guardado con contenido que ya no existe", () =>
            {
                var e = Nuevo(); var j = e.BuscarJugador("yo");
                j.inv[5] = new Slot("item_borrado", 3);
                e.granja.objetos.Add(new ObjMundo { uid = 9999, id = "objeto_borrado", x = 3, y = 12, datos = new ObjDatos() });
                e.granja.cultivos.Add(new Cultivo { i = Mundo.Idx(def, 4, 12), id = "cultivo_borrado", dias = 0 });
                e.mazmorra.enCurso = true; e.mazmorra.actual = "mazmorra_borrada"; e.mazmorra.nivel = new Zona();
                e.Normalizar(def);
                Exigir(j.inv[5] == null, "Quita items desconocidos del inventario");
                Exigir(e.granja.objetos.All(o => def.OBJ.ContainsKey(o.id)), "Quita objetos desconocidos");
                Exigir(e.granja.cultivos.All(c => def.CULT.ContainsKey(c.id)), "Quita cultivos desconocidos");
                Exigir(!e.mazmorra.enCurso && e.mazmorra.nivel == null, "Cierra una expedición a una mazmorra que ya no existe");
                Reglas.Aplicar(e, def, new Accion { tipo = "tiempo", dt = 0.05f });
            });
            Prueba("monstruos bien repartidos en la mazmorra", () =>
            {
                (int, int) Celda(Monstruo m) => ((int)Math.Floor(m.x / def.T), (int)Math.Floor(m.y / def.T));
                foreach (var cfg in def.mazmorras)
                    for (long semilla = 1; semilla <= 40; semilla++)
                        for (int piso = 1; piso <= cfg.pisos; piso++)
                        {
                            var z = Mazmorra.GenerarPiso(def, cfg.id, semilla, piso);
                            var donde = $"{cfg.id} semilla {semilla} piso {piso}";
                            var normales = z.monstruos.Where(m => !def.MON[m.id].jefe).ToList();
                            var celdas = z.monstruos.Select(Celda).ToList();
                            Exigir(celdas.Distinct().Count() == celdas.Count, "Dos monstruos en la misma celda: " + donde);
                            foreach (var m in normales)
                            {
                                var (mx, my) = Celda(m);
                                Exigir(Math.Max(Math.Abs(mx - z.entrada.x), Math.Abs(my - z.entrada.y)) >= Mazmorra.DISTANCIA_ENTRADA, "Monstruo junto a la entrada: " + donde);
                                if (cfg.jefes.ContainsKey(piso))
                                    Exigir(Math.Abs(mx - z.salaFinal.x) > 4 || Math.Abs(my - z.salaFinal.y) > 3, "Monstruo normal en la sala del jefe: " + donde);
                            }
                        }
            });
            Prueba("colocar un objeto que no cabe no lo gasta", () =>
            {
                var e = Nuevo(); Campo(e); var j = e.BuscarJugador("yo");
                var horno = def.OBJ["horno"]; int ancho = horno.ancho, alto = horno.alto;
                try
                {
                    horno.ancho = 3; horno.alto = 3;
                    var (x, y) = Reglas.CeldaFrente(def, j);
                    Mundo.Colocar(def, e.granja, "antorcha", x + 1, y);   // ocupa una celda que el horno necesitaría
                    Inventario.Agregar(def, j, "horno", 1);
                    j.sel = j.inv.FindIndex(s => s != null && s.id == "horno");
                    Exigir(j.sel >= 0 && j.sel < 12, "El horno debe estar en la barra");
                    int antes = Inventario.Cuenta(j, "horno");
                    Actuar(e, new Accion { tipo = "usar", x = x, y = y });
                    Exigir(Inventario.Cuenta(j, "horno") == antes, "No debe gastar el horno si no se colocó");
                    Exigir(!e.granja.objetos.Any(o => o.id == "horno"), "No debe colocarlo encima de otro objeto");
                }
                finally { horno.ancho = ancho; horno.alto = alto; }
            });
            Prueba("construir edificios de la aldea", () =>
            {
                var e = Nuevo(); Campo(e); var j = e.BuscarJugador("yo"); var g = e.granja;
                var (fx, fy) = Reglas.CeldaFrente(def, j);
                Despejar(e, fx - 2, fy - 2, 6, 6);
                Actuar(e, new Accion { tipo = "construir", item = "hoguera", x = fx, y = fy });
                Exigir(!g.objetos.Any(o => o.id == "hoguera"), "Sin materiales no se construye");
                Inventario.Agregar(def, j, "madera", 200); Inventario.Agregar(def, j, "piedra", 200); Inventario.Agregar(def, j, "fibra", 50);
                Actuar(e, new Accion { tipo = "construir", item = "casita_musgo", x = fx, y = fy });
                Exigir(!g.objetos.Any(o => o.id == "casita_musgo"), "La Moss Hut pide antes el Lantern Hearth");
                Actuar(e, new Accion { tipo = "construir", item = "lenera", x = fx, y = fy });
                Exigir(!g.objetos.Any(o => o.id == "lenera"), "La Woodshed pide un Mossling");
                Actuar(e, new Accion { tipo = "construir", item = "hoguera", x = fx, y = fy });
                var h = g.objetos.FirstOrDefault(o => o.id == "hoguera");
                Exigir(h != null && h.x == fx && Inventario.Cuenta(j, "madera") == 180 && Inventario.Cuenta(j, "piedra") == 180, "Construye delante y cobra los materiales");
                Actuar(e, new Accion { tipo = "construir", item = "hoguera", x = fx, y = fy });
                Exigir(g.objetos.Count(o => o.id == "hoguera") == 1, "No se construye encima de otro edificio");
                Actuar(e, new Accion { tipo = "construir", item = "hoguera", x = fx + 4, y = fy });
                Exigir(g.objetos.Count(o => o.id == "hoguera") == 1, "No se construye lejos del jugador");
                j.sel = j.inv.FindIndex(s => s != null && s.id == "hacha");
                Actuar(e, new Accion { tipo = "usar", x = fx, y = fy });
                Exigir(!g.objetos.Any(o => o.id == "hoguera") && Inventario.Cuenta(j, "madera") == 200, "Demoler con el hacha devuelve los materiales");
            });
            Prueba("rescatar un Mossling en la Mina", () =>
            {
                var e = Nuevo(); var j = e.BuscarJugador("yo");
                var rd = def.residentes.First();
                var z = Mazmorra.GenerarPiso(def, rd.rescateMazmorra, 5, rd.rescatePiso);
                e.mazmorra.actual = rd.rescateMazmorra; e.mazmorra.piso = rd.rescatePiso; e.mazmorra.nivel = z; e.mazmorra.enCurso = true; j.lugar = "mazmorra";
                Reglas.ColocarRescates(e, def, z);
                var farol = z.objetos.FirstOrDefault(o => o.id == "farol_apagado");
                Exigir(farol != null, "El farol apagado aparece en su piso");
                j.x = (farol.x + 0.5f) * def.T; j.y = (farol.y + 1) * def.T + 16; j.dir = "north";
                j.sel = j.inv.FindIndex(s => s != null && s.id == "hacha");
                Actuar(e, new Accion { tipo = "usar", x = farol.x, y = farol.y });
                Exigir(e.residentes.Count == 0 && z.objetos.Contains(farol), "Sin antorcha no se rescata");
                Inventario.Agregar(def, j, "antorcha", 1); j.sel = j.inv.FindIndex(s => s != null && s.id == "antorcha");
                Actuar(e, new Accion { tipo = "usar", x = farol.x, y = farol.y });
                Exigir(e.residentes.Count == 1 && e.residentes[0].id == rd.id && e.residentes[0].casa == 0, "La antorcha despierta al Mossling");
                Exigir(!z.objetos.Contains(farol) && Inventario.Cuenta(j, "antorcha") == 0, "Gasta la antorcha y quita el farol");
                var z2 = Mazmorra.GenerarPiso(def, rd.rescateMazmorra, 6, rd.rescatePiso);
                Reglas.ColocarRescates(e, def, z2);
                Exigir(!z2.objetos.Any(o => o.id == "farol_apagado"), "Un Mossling rescatado no vuelve a aparecer");
            });
            Prueba("el Mossling come, riega y duerme la siesta", () =>
            {
                var e = Nuevo(); var g = e.granja;
                var (hx, hy) = LugarLibre(e, 8, 8);
                var casa = Mundo.Colocar(def, g, "casita_musgo", hx, hy);
                var mesa = Mundo.Colocar(def, g, "comedero", hx + 3, hy);
                var r = new Residente { id = def.residentes.First().id, trabajo = "regar", casa = casa.uid };
                e.residentes.Add(r);
                var aradas = new List<int>();
                for (int y = hy + 3; y < hy + 6; y++) for (int x = hx; x < hx + 5; x++) { int i = Mundo.Idx(def, x, y); g.suelo[i] = 1; aradas.Add(i); }
                mesa.datos.items = new List<Slot>(); for (int k = 0; k < 12; k++) mesa.datos.items.Add(null);
                mesa.datos.items[0] = new Slot("rabano", 1);
                var ev = Reglas.Aplicar(e, def, new Accion { tipo = "dormir" });
                Exigir(!r.siesta && mesa.datos.items[0] == null, "Come un cultivo del Shared Table");
                Exigir(aradas.Count(i => g.regado[i] != 0) >= Reglas.RIEGO_MOSSLING, "Riega las casillas aradas cerca de su casita");
                ev = Reglas.Aplicar(e, def, new Accion { tipo = "dormir" });
                Exigir(r.siesta && ev.Any(x => x.tipo == "resumen" && x.notas.Any(n => n.Contains("napped"))), "Sin comida duerme la siesta y lo avisa");
            });
            Prueba("partidas anteriores sin residentes", () =>
            {
                var e = Nuevo(); e.residentes = null; e.Normalizar(def);
                Exigir(e.residentes != null && e.residentes.Count == 0, "Una partida de la v0.1 carga con la aldea vacía");
                e.residentes.Add(new Residente { id = "no_existe" }); e.Normalizar(def);
                Exigir(e.residentes.Count == 0, "Quita Mosslings que ya no existen en el contenido");
            });
            Console.WriteLine("14 pruebas C# correctas.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    static void Prueba(string nombre, Action accion) { accion(); Console.WriteLine("OK: " + nombre); }

    // Quita lo que haya en un rectángulo de la granja (árboles, rocas) para construir en las pruebas.
    static void Despejar(Estado e, int x0, int y0, int w, int h)
    {
        e.granja.objetos.RemoveAll(o => o.x >= x0 && o.x < x0 + w && o.y >= y0 && o.y < y0 + h && def.OBJ[o.id].accion == null);
        e.granja.ver++;
    }

    // Busca un rectángulo libre, fuera del camino y del agua, en la granja.
    static (int, int) LugarLibre(Estado e, int w, int h)
    {
        for (int y = 3; y + h < def.H - 1; y++)
            for (int x = 2; x + w < def.W - 1; x++)
            {
                Despejar(e, x, y, w, h);
                bool ok = Mundo.Libre(def, e.granja, x, y, w, h);
                for (int yy = y; yy < y + h && ok; yy++) for (int xx = x; xx < x + w && ok; xx++) if (Zona.En(e.granja.camino, Mundo.Idx(def, xx, yy))) ok = false;
                if (ok) return (x, y);
            }
        throw new Exception("No hay lugar libre en la granja");
    }
}
