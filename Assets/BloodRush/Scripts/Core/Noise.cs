using UnityEngine;

namespace BR
{
    // Smooth 3D value noise exactly as in the HTML game (hash3 / vnoise3 / fbm3 / wander), thread-safe.
    public static class Noise
    {
        public static double Hash3(int x, int y, int z)
        {
            unchecked
            {
                int h = (x * 374761393) ^ (y * 668265263) ^ (z * 1274126177);
                h = (h ^ (int)((uint)h >> 13)) * 1103515245;
                return (uint)(h ^ (int)((uint)h >> 16)) / 4294967296.0;
            }
        }
        public static float V3(float x, float y, float z)
        {
            int xi = Mathf.FloorToInt(x), yi = Mathf.FloorToInt(y), zi = Mathf.FloorToInt(z);
            float xf = x - xi, yf = y - yi, zf = z - zi;
            float u = xf * xf * (3 - 2 * xf), v = yf * yf * (3 - 2 * yf), w = zf * zf * (3 - 2 * zf);
            float L(double a, double b, float t) => (float)(a + (b - a) * t);
            float x00 = L(Hash3(xi, yi, zi), Hash3(xi + 1, yi, zi), u), x10 = L(Hash3(xi, yi + 1, zi), Hash3(xi + 1, yi + 1, zi), u);
            float x01 = L(Hash3(xi, yi, zi + 1), Hash3(xi + 1, yi, zi + 1), u), x11 = L(Hash3(xi, yi + 1, zi + 1), Hash3(xi + 1, yi + 1, zi + 1), u);
            float y0 = x00 + (x10 - x00) * v, y1 = x01 + (x11 - x01) * v;
            return y0 + (y1 - y0) * w;
        }
        public static float Fbm(float x, float y, float z, int oct = 5)
        {
            float s = 0, a = 0.5f, f = 1;
            for (int i = 0; i < oct; i++) { s += a * V3(x * f, y * f, z * f); f *= 2.03f; a *= 0.5f; }
            return s / (1 - Mathf.Pow(0.5f, oct));
        }
        // smooth 1D wander in [-1,1] for natural, never-repeating micro motion
        public static float Wander(float t, float seed) => V3(t, seed * 17.3f, 0.5f) * 2 - 1;
        public static float SStep(float a, float b, float x) { float t = Mathf.Clamp01((x - a) / (b - a)); return t * t * (3 - 2 * t); }
    }
}
