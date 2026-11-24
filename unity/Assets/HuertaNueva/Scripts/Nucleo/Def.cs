// Definiciones de contenido leídas de contenido.json (el mismo archivo que usa la versión web).
using System.Collections.Generic;
using System.Linq;

namespace HuertaNueva
{
    public class Cantidad { public string id; public int n; public Cantidad(string id, int n) { this.id = id; this.n = n; } }

    public class ItemDef
    {
        public string id, nombre, tipo, sprite, cultivo, objeto, aspecto, uso, descripcion;
        public int compra, venta, capacidad, dano, cura;
        public Dictionary<string, double> efecto = new Dictionary<string, double>();
        public double Efecto(string k) => efecto.TryGetValue(k, out var v) ? v : 0;
    }

    public class Proceso { public string entrada, salida; public int n, minutos; }

    public class Suelta { public string item; public double prob; public int min, max; }

    public class ObjDef
    {
        public string id, sprite, accion, herramienta, botin;
        public bool solido, recoger, porTileset;
        public int ancho = 1, alto = 1, golpes, riega, protege, luz, desfase;
        public List<Suelta> suelta = new List<Suelta>();
        public List<Proceso> procesa = new List<Proceso>();
    }

    public class CultDef { public string id, producto; public int dias; public List<string> sprites = new List<string>(); }

    public class MonDef
    {
        public string id, nombre, sprite, descripcion;
        public int vida, dano, velocidad, vision;
        public bool jefe, vuela;
        public List<Suelta> suelta = new List<Suelta>();
    }

    public class Rango { public int desde, hasta; public string id; public double peso = 1; }

    public class MazDef
    {
        public string id, nombre, tileset, descripcion, botinFinal, requiereMazmorra;
        public int pisos, requierePiso;
        public double escalaBase = 1;
        public Dictionary<int, string> jefes = new Dictionary<int, string>();
        public List<int> puntosDeControl = new List<int>();
        public List<Rango> monstruos = new List<Rango>(), rocas = new List<Rango>();
    }

    public class CartaDef { public string id, titulo, autor, disparador, resumen, texto; }

    public class HistoriaDef
    {
        public string subtitulo, lemaPortada, nombreMercader = "Aunt Brisa", tituloPuesto = "Aunt Brisa's Stall";
        public Dictionary<string, string> saludos = new Dictionary<string, string>();
        public List<string> charlas = new List<string>();
        public List<string> murmullos = new List<string>();
        public List<CartaDef> cartas = new List<CartaDef>();
    }

    public class Receta { public string resultado; public int cantidad; public List<Cantidad> ingredientes = new List<Cantidad>(); }

    public class EntradaBotin { public string item; public int peso, min, max; }
    public class TablaBotin { public int tiradas; public List<EntradaBotin> entradas = new List<EntradaBotin>(); public List<Cantidad> garantizado = new List<Cantidad>(); }

    public class PersonajeDef { public string id, nombre, titulo, personalidad; }
    public class SpriteDef { public string id; public int ancho, alto; }

    public class Def
    {
        public int T, W, H, semillaMundo, dineroInicio;
        public Dictionary<string, ItemDef> ITEMS = new Dictionary<string, ItemDef>();
        public Dictionary<string, ObjDef> OBJ = new Dictionary<string, ObjDef>();
        public Dictionary<string, CultDef> CULT = new Dictionary<string, CultDef>();
        public Dictionary<string, MonDef> MON = new Dictionary<string, MonDef>();
        public Dictionary<string, MazDef> MAZ = new Dictionary<string, MazDef>();
        public List<MazDef> mazmorras = new List<MazDef>();
        public List<Receta> recetas = new List<Receta>();
        public List<string> tienda = new List<string>();
        public Dictionary<string, TablaBotin> botines = new Dictionary<string, TablaBotin>();
        public List<PersonajeDef> personajes = new List<PersonajeDef>();
        public List<string> ropas = new List<string>();
        public List<Cantidad> inventarioInicio = new List<Cantidad>();
        public Dictionary<string, SpriteDef> sprites = new Dictionary<string, SpriteDef>();
        public List<string> tilesets = new List<string>();
        public HistoriaDef historia = new HistoriaDef();

        static List<Suelta> LeerSuelta(object o, bool conProbabilidad)
        {
            var r = new List<Suelta>();
            foreach (var kv in J.D(o))
            {
                var v = J.L(kv.Value).Select(x => (double)x).ToList();
                if (conProbabilidad) r.Add(new Suelta { item = kv.Key, prob = v[0], min = (int)v[1], max = (int)v[2] });
                else r.Add(new Suelta { item = kv.Key, prob = 1, min = (int)v[0], max = (int)v[1] });
            }
            return r;
        }

        public static Def Cargar(string json)
        {
            var C = J.D(Json.Leer(json));
            var d = new Def();
            var mundo = J.D(J.V(C, "mundo"));
            d.T = J.I(mundo, "tile", 32); d.W = J.I(mundo, "ancho", 48); d.H = J.I(mundo, "alto", 36); d.semillaMundo = J.I(mundo, "semilla", 7);

            foreach (var kv in J.D(J.V(C, "sprites")))
            {
                var s = J.D(kv.Value);
                d.sprites[kv.Key] = new SpriteDef { id = kv.Key, ancho = J.I(s, "ancho", 32), alto = J.I(s, "alto", 32) };
            }
            foreach (var o in J.L(J.V(C, "tilesets"))) d.tilesets.Add(J.S(J.D(o), "id"));

            foreach (var o in J.L(J.V(C, "items")))
            {
                var x = J.D(o);
                var it = new ItemDef
                {
                    id = J.S(x, "id"), nombre = J.S(x, "nombre"), tipo = J.S(x, "tipo"), sprite = J.S(x, "sprite"),
                    cultivo = J.S(x, "cultivo"), objeto = J.S(x, "objeto"), aspecto = J.S(x, "aspecto"), uso = J.S(x, "uso"),
                    descripcion = J.S(x, "descripcion", ""), compra = J.I(x, "compra"), venta = J.I(x, "venta"),
                    capacidad = J.I(x, "capacidad"), dano = J.I(x, "dano"), cura = J.I(x, "cura"),
                };
                foreach (var e in J.D(J.V(x, "efecto"))) it.efecto[e.Key] = (double)e.Value;
                d.ITEMS[it.id] = it;
            }
            foreach (var o in J.L(J.V(C, "objetos")))
            {
                var x = J.D(o);
                var ob = new ObjDef
                {
                    id = J.S(x, "id"), sprite = J.S(x, "sprite"), accion = J.S(x, "accion"), herramienta = J.S(x, "herramienta"),
                    botin = J.S(x, "botin"), solido = J.B(x, "solido"), recoger = J.B(x, "recoger"), porTileset = J.B(x, "porTileset"),
                    ancho = J.I(x, "ancho", 1), alto = J.I(x, "alto", 1), golpes = J.I(x, "golpes"), riega = J.I(x, "riega"),
                    protege = J.I(x, "protege"), luz = J.I(x, "luz"), desfase = J.I(x, "desfase"),
                    suelta = LeerSuelta(J.V(x, "suelta"), false),
                };
                foreach (var p in J.L(J.V(x, "procesa")))
                {
                    var q = J.D(p);
                    ob.procesa.Add(new Proceso { entrada = J.S(q, "entrada"), salida = J.S(q, "salida"), n = J.I(q, "n", 1), minutos = J.I(q, "minutos") });
                }
                d.OBJ[ob.id] = ob;
            }
            foreach (var o in J.L(J.V(C, "cultivos")))
            {
                var x = J.D(o);
                d.CULT[J.S(x, "id")] = new CultDef { id = J.S(x, "id"), producto = J.S(x, "producto"), dias = J.I(x, "dias"), sprites = J.L(J.V(x, "sprites")).Select(s => (string)s).ToList() };
            }
            foreach (var o in J.L(J.V(C, "monstruos")))
            {
                var x = J.D(o);
                d.MON[J.S(x, "id")] = new MonDef
                {
                    id = J.S(x, "id"), nombre = J.S(x, "nombre"), sprite = J.S(x, "sprite"), descripcion = J.S(x, "descripcion", ""),
                    vida = J.I(x, "vida"), dano = J.I(x, "dano"),
                    velocidad = J.I(x, "velocidad"), vision = J.I(x, "vision"), jefe = J.B(x, "jefe"), vuela = J.B(x, "vuela"),
                    suelta = LeerSuelta(J.V(x, "suelta"), true),
                };
            }
            foreach (var o in J.L(J.V(C, "mazmorras")))
            {
                var x = J.D(o);
                var m = new MazDef
                {
                    id = J.S(x, "id"), nombre = J.S(x, "nombre"), tileset = J.S(x, "tileset"), descripcion = J.S(x, "descripcion", ""),
                    botinFinal = J.S(x, "botin_final"), pisos = J.I(x, "pisos", 10), escalaBase = J.F(x, "escala_base", 1),
                };
                var req = J.V(x, "requiere") as Dictionary<string, object>;
                if (req != null) { m.requiereMazmorra = J.S(req, "mazmorra"); m.requierePiso = J.I(req, "piso"); }
                foreach (var kv in J.D(J.V(x, "jefes"))) m.jefes[int.Parse(kv.Key)] = (string)kv.Value;
                foreach (var p in J.L(J.V(x, "puntos_de_control"))) m.puntosDeControl.Add((int)(double)p);
                foreach (var r in J.L(J.V(x, "monstruos_por_piso"))) { var q = J.D(r); m.monstruos.Add(new Rango { desde = J.I(q, "desde"), hasta = J.I(q, "hasta"), id = J.S(q, "id"), peso = J.F(q, "peso", 1) }); }
                foreach (var r in J.L(J.V(x, "rocas_por_piso"))) { var q = J.D(r); m.rocas.Add(new Rango { desde = J.I(q, "desde"), hasta = J.I(q, "hasta"), id = J.S(q, "id"), peso = J.F(q, "peso", 1) }); }
                d.MAZ[m.id] = m; d.mazmorras.Add(m);
            }
            foreach (var o in J.L(J.V(C, "recetas")))
            {
                var x = J.D(o);
                var r = new Receta { resultado = J.S(x, "resultado"), cantidad = J.I(x, "cantidad", 1) };
                foreach (var kv in J.D(J.V(x, "ingredientes"))) r.ingredientes.Add(new Cantidad(kv.Key, (int)(double)kv.Value));
                d.recetas.Add(r);
            }
            foreach (var o in J.L(J.V(C, "tienda"))) d.tienda.Add((string)o);
            foreach (var kv in J.D(J.V(C, "botines")))
            {
                var x = J.D(kv.Value);
                var t = new TablaBotin { tiradas = J.I(x, "tiradas", 1) };
                foreach (var e in J.L(J.V(x, "entradas"))) { var q = J.D(e); t.entradas.Add(new EntradaBotin { item = J.S(q, "item"), peso = J.I(q, "peso"), min = J.I(q, "min"), max = J.I(q, "max") }); }
                foreach (var e in J.L(J.V(x, "garantizado"))) { var q = J.D(e); t.garantizado.Add(new Cantidad(J.S(q, "item"), J.I(q, "n"))); }
                d.botines[kv.Key] = t;
            }
            foreach (var o in J.L(J.V(C, "personajes")))
            {
                var x = J.D(o);
                d.personajes.Add(new PersonajeDef
                {
                    id = J.S(x, "id"),
                    nombre = J.S(x, "nombre"),
                    titulo = J.S(x, "titulo", "Homestead Farmer"),
                    personalidad = J.S(x, "personalidad", ""),
                });
            }
            foreach (var o in J.L(J.V(C, "ropas"))) d.ropas.Add(J.S(J.D(o), "id"));
            var inicio = J.D(J.V(C, "inicio"));
            d.dineroInicio = J.I(inicio, "dinero", 300);
            foreach (var o in J.L(J.V(inicio, "inventario"))) { var l = J.L(o); d.inventarioInicio.Add(new Cantidad((string)l[0], (int)(double)l[1])); }
            var hist = J.V(C, "historia") as Dictionary<string, object>;
            if (hist != null)
            {
                d.historia.subtitulo = J.S(hist, "subtitulo", "Mosslantern Glen");
                d.historia.lemaPortada = J.S(hist, "lema_portada", "");
                var merc = J.V(hist, "mercader") as Dictionary<string, object>;
                if (merc != null)
                {
                    d.historia.nombreMercader = J.S(merc, "nombre", "Aunt Brisa");
                    d.historia.tituloPuesto = J.S(merc, "titulo_puesto", "Aunt Brisa's Stall");
                    foreach (var kv in J.D(J.V(merc, "saludos"))) d.historia.saludos[kv.Key] = (string)kv.Value;
                    foreach (var ch in J.L(J.V(merc, "charlas"))) d.historia.charlas.Add((string)ch);
                }
                foreach (var mu in J.L(J.V(hist, "murmullos"))) d.historia.murmullos.Add((string)mu);
                foreach (var o in J.L(J.V(hist, "cartas")))
                {
                    var cx = J.D(o);
                    d.historia.cartas.Add(new CartaDef
                    {
                        id = J.S(cx, "id"), titulo = J.S(cx, "titulo"), autor = J.S(cx, "autor"),
                        disparador = J.S(cx, "disparador"), resumen = J.S(cx, "resumen"), texto = J.S(cx, "texto"),
                    });
                }
            }
            return d;
        }
    }
}
