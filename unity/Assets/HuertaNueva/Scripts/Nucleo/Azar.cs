// Azar reproducible: mismo algoritmo (mulberry32) y mismo hash (FNV-1a) que la versión web,
// así una misma semilla genera la misma granja y los mismos pisos en los dos motores.
using System;

namespace HuertaNueva
{
    public static class Azar
    {
        public static Func<double> Mulberry32(uint a)
        {
            return () =>
            {
                unchecked
                {
                    a += 0x6D2B79F5u;
                    uint t = (a ^ (a >> 15)) * (1u | a);
                    t = (t + ((t ^ (t >> 7)) * (61u | t))) ^ t;
                    return (t ^ (t >> 14)) / 4294967296.0;
                }
            };
        }

        public static uint HashTexto(string s)
        {
            unchecked
            {
                uint h = 2166136261u;
                foreach (char c in s) { h ^= c; h *= 16777619u; }
                return h;
            }
        }

        // Siguiente número al azar del estado (avanza su contador "paso").
        public static double Siguiente(Estado e)
        {
            unchecked
            {
                e.paso++;
                return Mulberry32((uint)e.semilla ^ ((uint)e.paso * 0x9E3779B1u))();
            }
        }

        public static int Entre(double r, int a, int b) => a + (int)Math.Floor(r * (b - a + 1));
    }
}
