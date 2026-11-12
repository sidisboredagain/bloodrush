using System.Collections.Generic;
using UnityEngine;

namespace BR
{
    // Procedural meshes that reproduce three.js geometry (same vertex layout, CCW front faces in three's
    // right-handed convention). The game camera renders with three's view convention, so triangles are
    // reversed on upload to match Unity's culling.
    public static class Geo
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
        static Mesh Cached(string key, System.Func<Mesh> make)
        {
            if (!cache.TryGetValue(key, out var m) || !m) { m = make(); m.name = key; cache[key] = m; } // (re)make if the editor unloaded it
            return m;
        }

        // Mesh under construction, in three.js order (CCW front faces); Build() reverses the triangles for Unity.
        public class MB
        {
            public readonly List<Vector3> v = new List<Vector3>();
            public readonly List<Vector3> n = new List<Vector3>();
            public readonly List<Vector2> uv = new List<Vector2>();
            public readonly List<int> t = new List<int>();
            public void Tri(int a, int b, int c) { t.Add(a); t.Add(b); t.Add(c); }
            // three BufferGeometry.computeVertexNormals on an indexed geometry (area-weighted face normals)
            public void ComputeNormals()
            {
                var acc = new Vector3[v.Count];
                for (int i = 0; i < t.Count; i += 3)
                {
                    Vector3 pa = v[t[i]], pb = v[t[i + 1]], pc = v[t[i + 2]];
                    var fn = Vector3.Cross(pc - pb, pa - pb);
                    acc[t[i]] += fn; acc[t[i + 1]] += fn; acc[t[i + 2]] += fn;
                }
                n.Clear(); foreach (var a in acc) n.Add(a.sqrMagnitude > 0 ? a.normalized : Vector3.up);
            }
            // non-indexed geometry: every triangle gets its own vertices and face normal (three ExtrudeGeometry)
            public MB Flat()
            {
                var o = new MB();
                for (int i = 0; i < t.Count; i += 3)
                {
                    Vector3 pa = v[t[i]], pb = v[t[i + 1]], pc = v[t[i + 2]];
                    var fn = Vector3.Cross(pc - pb, pa - pb); fn = fn.sqrMagnitude > 0 ? fn.normalized : Vector3.up;
                    int s = o.v.Count;
                    for (int k = 0; k < 3; k++) { o.v.Add(v[t[i + k]]); o.n.Add(fn); o.uv.Add(uv.Count == v.Count ? uv[t[i + k]] : Vector2.zero); }
                    o.Tri(s, s + 1, s + 2);
                }
                return o;
            }
            public MB Clone() { var o = new MB(); o.v.AddRange(v); o.n.AddRange(n); o.uv.AddRange(uv); o.t.AddRange(t); return o; }
            public void Translate(Vector3 d) { for (int i = 0; i < v.Count; i++) v[i] += d; }
            public void Transform(Matrix4x4 m)
            {
                var nm = m.inverse.transpose;
                for (int i = 0; i < v.Count; i++) { v[i] = m.MultiplyPoint3x4(v[i]); if (i < n.Count) n[i] = nm.MultiplyVector(n[i]).normalized; }
            }
            public int[] UnityTriangles()
            {
                var rev = new int[t.Count];
                for (int i = 0; i < t.Count; i += 3) { rev[i] = t[i]; rev[i + 1] = t[i + 2]; rev[i + 2] = t[i + 1]; }
                return rev;
            }
            public Mesh Build()
            {
                var m = new Mesh();
                if (v.Count > 65000) m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                m.SetVertices(v);
                m.SetNormals(n);
                if (uv.Count == v.Count) m.SetUVs(0, uv);
                var rev = new int[t.Count];
                for (int i = 0; i < t.Count; i += 3) { rev[i] = t[i]; rev[i + 1] = t[i + 2]; rev[i + 2] = t[i + 1]; }
                m.SetTriangles(rev, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        // ---------- box ----------
        public static Mesh Box(float w, float h, float d) => Cached($"b{w},{h},{d}", () => MakeBox(w, h, d, false));
        public static Mesh TiledBox(float w, float h, float d) => MakeBox(w, h, d, true);
        static Mesh MakeBox(float w, float h, float d, bool tile)
        {
            var b = new MB();
            void Face(Vector3 n, Vector3 u, Vector3 vv, float fw, float fh, float depth)
            {
                int s = b.v.Count; Vector3 c = n * depth;
                b.v.Add(c - u * fw / 2 - vv * fh / 2); b.v.Add(c + u * fw / 2 - vv * fh / 2);
                b.v.Add(c + u * fw / 2 + vv * fh / 2); b.v.Add(c - u * fw / 2 + vv * fh / 2);
                float su = tile ? fw / 4f : 1, sv = tile ? fh / 4f : 1;
                b.uv.Add(new Vector2(0, 0)); b.uv.Add(new Vector2(su, 0)); b.uv.Add(new Vector2(su, sv)); b.uv.Add(new Vector2(0, sv));
                for (int i = 0; i < 4; i++) b.n.Add(n);
                b.Tri(s, s + 1, s + 2); b.Tri(s, s + 2, s + 3);
            }
            Face(new Vector3(1, 0, 0), new Vector3(0, 0, -1), Vector3.up, d, h, w / 2);
            Face(new Vector3(-1, 0, 0), new Vector3(0, 0, 1), Vector3.up, d, h, w / 2);
            Face(Vector3.up, new Vector3(1, 0, 0), new Vector3(0, 0, -1), w, d, h / 2);
            Face(Vector3.down, new Vector3(1, 0, 0), new Vector3(0, 0, 1), w, d, h / 2);
            Face(new Vector3(0, 0, 1), new Vector3(1, 0, 0), Vector3.up, w, h, d / 2);
            Face(new Vector3(0, 0, -1), new Vector3(-1, 0, 0), Vector3.up, w, h, d / 2);
            return b.Build();
        }

        // ---------- cylinder / cone (three CylinderGeometry) ----------
        public static Mesh Cyl(float rt, float rb, float h, int seg = 10, bool open = false) =>
            Cached($"c{rt},{rb},{h},{seg},{open}", () => MakeCyl(rt, rb, h, seg, open));
        public static Mesh Cone(float r, float h, int seg = 6) => Cyl(0, r, h, seg, false);
        public static Mesh OpenCone(float r, float h, int seg = 10) => Cyl(0, r, h, seg, true);
        static Mesh MakeCyl(float rt, float rb, float h, int rs, bool open) => CylMB(rt, rb, h, rs, open).Build();
        public static MB CylMB(float rt, float rb, float h, int rs, bool open = false)
        {
            var b = new MB();
            float half = h / 2, slope = (rb - rt) / h;
            var idx = new int[2, rs + 1];
            for (int y = 0; y <= 1; y++)
            {
                float radius = y * (rb - rt) + rt;
                for (int x = 0; x <= rs; x++)
                {
                    float u = (float)x / rs, th = u * Mathf.PI * 2, s = Mathf.Sin(th), c = Mathf.Cos(th);
                    b.v.Add(new Vector3(radius * s, -y * h + half, radius * c));
                    b.n.Add(new Vector3(s, slope, c).normalized);
                    b.uv.Add(new Vector2(u, 1 - y));
                    idx[y, x] = b.v.Count - 1;
                }
            }
            for (int x = 0; x < rs; x++)
            {
                int a = idx[0, x], bb = idx[1, x], c = idx[1, x + 1], d = idx[0, x + 1];
                b.Tri(a, bb, d); b.Tri(bb, c, d);
            }
            if (!open)
            {
                void Cap(bool top)
                {
                    float radius = top ? rt : rb, sign = top ? 1 : -1;
                    if (radius <= 0) return;
                    int cs = b.v.Count;
                    for (int x = 1; x <= rs; x++) { b.v.Add(new Vector3(0, half * sign, 0)); b.n.Add(new Vector3(0, sign, 0)); b.uv.Add(new Vector2(0.5f, 0.5f)); }
                    int ce = b.v.Count;
                    for (int x = 0; x <= rs; x++)
                    {
                        float th = (float)x / rs * Mathf.PI * 2, s = Mathf.Sin(th), c = Mathf.Cos(th);
                        b.v.Add(new Vector3(radius * s, half * sign, radius * c)); b.n.Add(new Vector3(0, sign, 0));
                        b.uv.Add(new Vector2(c * 0.5f + 0.5f, s * 0.5f * sign + 0.5f));
                    }
                    for (int x = 0; x < rs; x++)
                    {
                        int c = cs + x, i = ce + x;
                        if (top) b.Tri(i, i + 1, c); else b.Tri(i + 1, i, c);
                    }
                }
                Cap(true); Cap(false);
            }
            return b;
        }

        // ---------- sphere (three SphereGeometry, supports partial sweeps) ----------
        public static Mesh Sphere(float r, int ws = 12, int hs = 10) => Sphere(r, ws, hs, 0, Mathf.PI * 2, 0, Mathf.PI);
        public static Mesh Sphere(float r, int ws, int hs, float phiStart, float phiLen, float thStart, float thLen) =>
            Cached($"s{r},{ws},{hs},{phiStart},{phiLen},{thStart},{thLen}", () => MakeSphere(r, ws, hs, phiStart, phiLen, thStart, thLen));
        static Mesh MakeSphere(float r, int ws, int hs, float phiStart, float phiLen, float thStart, float thLen) => SphereMB(r, ws, hs, phiStart, phiLen, thStart, thLen).Build();
        public static MB SphereMB(float r, int ws, int hs, float phiStart = 0, float phiLen = Mathf.PI * 2, float thStart = 0, float thLen = Mathf.PI)
        {
            var b = new MB();
            float thEnd = Mathf.Min(thStart + thLen, Mathf.PI);
            var grid = new int[hs + 1, ws + 1];
            for (int iy = 0; iy <= hs; iy++)
            {
                float v = (float)iy / hs;
                for (int ix = 0; ix <= ws; ix++)
                {
                    float u = (float)ix / ws;
                    var p = new Vector3(-r * Mathf.Cos(phiStart + u * phiLen) * Mathf.Sin(thStart + v * thLen),
                                        r * Mathf.Cos(thStart + v * thLen),
                                        r * Mathf.Sin(phiStart + u * phiLen) * Mathf.Sin(thStart + v * thLen));
                    b.v.Add(p); b.n.Add(p.sqrMagnitude > 0 ? p.normalized : Vector3.up); b.uv.Add(new Vector2(u, 1 - v));
                    grid[iy, ix] = b.v.Count - 1;
                }
            }
            for (int iy = 0; iy < hs; iy++)
                for (int ix = 0; ix < ws; ix++)
                {
                    int a = grid[iy, ix + 1], bb = grid[iy, ix], c = grid[iy + 1, ix], d = grid[iy + 1, ix + 1];
                    if (iy != 0 || thStart > 0) b.Tri(a, bb, d);
                    if (iy != hs - 1 || thEnd < Mathf.PI) b.Tri(bb, c, d);
                }
            return b;
        }

        // ---------- torus (three TorusGeometry, lies in the XY plane) ----------
        public static Mesh Torus(float R, float tube, int tubSeg = 24, int radSeg = 5, float arc = Mathf.PI * 2) =>
            Cached($"t{R},{tube},{tubSeg},{radSeg},{arc}", () => MakeTorus(R, tube, radSeg, tubSeg, arc));
        public static Mesh Arc(float R, float tube, int tubSeg = 24) => Torus(R, tube, tubSeg, 6, Mathf.PI);
        static Mesh MakeTorus(float R, float tube, int rad, int tub, float arc)
        {
            var b = new MB();
            for (int j = 0; j <= rad; j++)
                for (int i = 0; i <= tub; i++)
                {
                    float u = (float)i / tub * arc, v = (float)j / rad * Mathf.PI * 2;
                    var p = new Vector3((R + tube * Mathf.Cos(v)) * Mathf.Cos(u), (R + tube * Mathf.Cos(v)) * Mathf.Sin(u), tube * Mathf.Sin(v));
                    var c = new Vector3(R * Mathf.Cos(u), R * Mathf.Sin(u), 0);
                    b.v.Add(p); b.n.Add((p - c).normalized); b.uv.Add(new Vector2((float)i / tub, (float)j / rad));
                }
            for (int j = 1; j <= rad; j++)
                for (int i = 1; i <= tub; i++)
                {
                    int a = (tub + 1) * j + i - 1, bb = (tub + 1) * (j - 1) + i - 1, c = (tub + 1) * (j - 1) + i, d = (tub + 1) * j + i;
                    b.Tri(a, bb, d); b.Tri(bb, c, d);
                }
            return b.Build();
        }

        // ---------- flat shapes (XY plane, facing +z) ----------
        public static Mesh Plane(float w, float h) => Cached($"p{w},{h}", () =>
        {
            var b = new MB();
            b.v.Add(new Vector3(-w / 2, -h / 2, 0)); b.v.Add(new Vector3(w / 2, -h / 2, 0)); b.v.Add(new Vector3(w / 2, h / 2, 0)); b.v.Add(new Vector3(-w / 2, h / 2, 0));
            b.uv.Add(new Vector2(0, 0)); b.uv.Add(new Vector2(1, 0)); b.uv.Add(new Vector2(1, 1)); b.uv.Add(new Vector2(0, 1));
            for (int i = 0; i < 4; i++) b.n.Add(new Vector3(0, 0, 1));
            b.Tri(0, 1, 2); b.Tri(0, 2, 3);
            return b.Build();
        });
        public static Mesh Ring(float inner, float outer, int seg = 40) => Cached($"r{inner},{outer},{seg}", () =>
        {
            var b = new MB();
            for (int j = 0; j <= 1; j++)
            {
                float radius = inner + j * (outer - inner);
                for (int i = 0; i <= seg; i++)
                {
                    float a = (float)i / seg * Mathf.PI * 2;
                    b.v.Add(new Vector3(radius * Mathf.Cos(a), radius * Mathf.Sin(a), 0)); b.n.Add(new Vector3(0, 0, 1));
                    b.uv.Add(new Vector2((radius * Mathf.Cos(a) / outer + 1) / 2, (radius * Mathf.Sin(a) / outer + 1) / 2));
                }
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i, bb = i + seg + 1, c = i + seg + 2, d = i + 1;
                b.Tri(a, bb, d); b.Tri(bb, c, d);
            }
            return b.Build();
        });
        public static Mesh Circle(float r, int seg = 32) => Cached($"ci{r},{seg}", () =>
        {
            var b = new MB();
            b.v.Add(Vector3.zero); b.n.Add(new Vector3(0, 0, 1)); b.uv.Add(new Vector2(0.5f, 0.5f));
            for (int i = 0; i <= seg; i++)
            {
                float a = (float)i / seg * Mathf.PI * 2;
                b.v.Add(new Vector3(r * Mathf.Cos(a), r * Mathf.Sin(a), 0)); b.n.Add(new Vector3(0, 0, 1));
                b.uv.Add(new Vector2((Mathf.Cos(a) + 1) / 2, (Mathf.Sin(a) + 1) / 2));
            }
            for (int i = 1; i <= seg; i++) b.Tri(i, i + 1, 0);
            return b.Build();
        });

        // ---------- flat-shaded polyhedra ----------
        public static Mesh Octahedron(float r) => Cached($"oct{r}", () => Poly(new float[] { 1, 0, 0, -1, 0, 0, 0, 1, 0, 0, -1, 0, 0, 0, 1, 0, 0, -1 },
            new[] { 0, 2, 4, 0, 4, 3, 0, 3, 5, 0, 5, 2, 1, 2, 5, 1, 5, 3, 1, 3, 4, 1, 4, 2 }, r));
        public static Mesh Icosahedron(float r) => Cached($"ico{r}", () =>
        {
            float t = (1 + Mathf.Sqrt(5)) / 2;
            return Poly(new float[] { -1, t, 0, 1, t, 0, -1, -t, 0, 1, -t, 0, 0, -1, t, 0, 1, t, 0, -1, -t, 0, 1, -t, t, 0, -1, t, 0, 1, -t, 0, -1, -t, 0, 1 },
                new[] { 0, 11, 5, 0, 5, 1, 0, 1, 7, 0, 7, 10, 0, 10, 11, 1, 5, 9, 5, 11, 4, 11, 10, 2, 10, 7, 6, 7, 1, 8, 3, 9, 4, 3, 4, 2, 3, 2, 6, 3, 6, 8, 3, 8, 9, 4, 9, 5, 2, 4, 11, 6, 2, 10, 8, 6, 7, 9, 8, 1 }, r);
        });
        static Mesh Poly(float[] verts, int[] ids, float r)
        {
            var b = new MB();
            for (int i = 0; i < ids.Length; i += 3)
            {
                var a = new Vector3(verts[ids[i] * 3], verts[ids[i] * 3 + 1], verts[ids[i] * 3 + 2]).normalized * r;
                var bb = new Vector3(verts[ids[i + 1] * 3], verts[ids[i + 1] * 3 + 1], verts[ids[i + 1] * 3 + 2]).normalized * r;
                var c = new Vector3(verts[ids[i + 2] * 3], verts[ids[i + 2] * 3 + 1], verts[ids[i + 2] * 3 + 2]).normalized * r;
                var n = Vector3.Cross(bb - a, c - a).normalized;
                if (Vector3.Dot(n, a + bb + c) < 0) { var tmp = bb; bb = c; c = tmp; n = -n; }
                int s = b.v.Count;
                b.v.Add(a); b.v.Add(bb); b.v.Add(c);
                for (int k = 0; k < 3; k++) { b.n.Add(n); b.uv.Add(Vector2.zero); }
                b.Tri(s, s + 1, s + 2);
            }
            return b.Build();
        }
    }
}
