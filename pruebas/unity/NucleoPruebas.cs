using System;
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
            Console.WriteLine("8 pruebas C# correctas.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }

    static void Prueba(string nombre, Action accion) { accion(); Console.WriteLine("OK: " + nombre); }
}
