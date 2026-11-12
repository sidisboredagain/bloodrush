using System;
using System.Collections.Generic;
using UnityEngine;
using MB = BR.Geo.MB;

namespace BR
{
    // More three.js geometry, ported from its source: LatheGeometry, CapsuleGeometry, Shape/ShapeGeometry,
    // ExtrudeGeometry (with bevel), CatmullRomCurve3, TubeGeometry and a segmented PlaneGeometry.
    public static class GeoX
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();
        public static Mesh Cached(string key, Func<Mesh> make)
        {
            if (!cache.TryGetValue(key, out var m) || !m) { m = make(); m.name = key; cache[key] = m; } // (re)make if the editor unloaded it
            return m;
        }

        // ---------- LatheGeometry (points are (radius, y)) ----------
        public static MB LatheMB(IList<Vector2> points, int segments = 12, float phiStart = 0, float phiLength = Mathf.PI * 2)
        {
            var b = new MB(); int np = points.Count;
            phiLength = Mathf.Clamp(phiLength, 0, Mathf.PI * 2);
            float inv = 1f / segments;
            var init = new Vector3[np]; Vector3 prev = Vector3.zero;
            for (int j = 0; j < np; j++)
            {
                if (j == 0)
                {
                    float dx = points[1].x - points[0].x, dy = points[1].y - points[0].y;
                    var nn = new Vector3(dy, -dx, 0); prev = nn; init[j] = nn.normalized;
                }
                else if (j == np - 1) init[j] = prev;
                else
                {
                    float dx = points[j + 1].x - points[j].x, dy = points[j + 1].y - points[j].y;
                    var cur = new Vector3(dy, -dx, 0); init[j] = (cur + prev).normalized; prev = cur;
                }
            }
            for (int i = 0; i <= segments; i++)
            {
                float phi = phiStart + i * inv * phiLength, sin = Mathf.Sin(phi), cos = Mathf.Cos(phi);
                for (int j = 0; j < np; j++)
                {
                    b.v.Add(new Vector3(points[j].x * sin, points[j].y, points[j].x * cos));
                    b.uv.Add(new Vector2((float)i / segments, (float)j / (np - 1)));
                    b.n.Add(new Vector3(init[j].x * sin, init[j].y, init[j].x * cos));
                }
            }
            for (int i = 0; i < segments; i++)
                for (int j = 0; j < np - 1; j++)
                {
                    int bs = j + i * np, a = bs, bb = bs + np, c = bs + np + 1, d = bs + 1;
                    b.Tri(a, bb, d); b.Tri(c, d, bb);
                }
            return b;
        }
        // pairs of (y, radius), radius clamped to 1e-4 like the HTML game's LATHE helper
        public static Vector2[] YR(params float[] yr)
        {
            var p = new Vector2[yr.Length / 2];
            for (int i = 0; i < p.Length; i++) p[i] = new Vector2(Mathf.Max(1e-4f, yr[i * 2 + 1]), yr[i * 2]);
            return p;
        }
        public static Mesh Lathe(string key, Vector2[] pts, int seg = 24, float p0 = 0, float pl = Mathf.PI * 2) =>
            Cached("la" + key + seg + "," + p0 + "," + pl, () => LatheMB(pts, seg, p0, pl).Build());
        // rounded tapered limb hanging from its pivot (LIMB / limbGeo)
        public static Mesh Limb(float r1, float r2, float len, int seg = 12, int cap = 4) => Cached($"L{r1},{r2},{len},{seg},{cap}", () =>
        {
            var pts = new List<Vector2>();
            for (int i = 0; i <= cap; i++) { float a = -Mathf.PI / 2 + (float)i / cap * Mathf.PI / 2; pts.Add(new Vector2(Mathf.Max(1e-4f, r2 * Mathf.Cos(a)), -len + r2 * Mathf.Sin(a))); }
            for (int i = 1; i <= cap; i++) { float a = (float)i / cap * Mathf.PI / 2; pts.Add(new Vector2(Mathf.Max(1e-4f, r1 * Mathf.Cos(a)), r1 * Mathf.Sin(a))); }
            return LatheMB(pts, seg).Build();
        });

        // ---------- CapsuleGeometry(radius, length, capSegments, radialSegments) ----------
        public static Mesh Capsule(float r, float len, int capSeg = 4, int radSeg = 10) => Cached($"cap{r},{len},{capSeg},{radSeg}", () =>
        {
            var pts = new List<Vector2>();
            void Arc(float cy, float a0, float a1, bool skipFirst)
            {
                float delta = a1 - a0; while (delta < 0) delta += Mathf.PI * 2;
                int res = capSeg * 2;
                for (int i = skipFirst ? 1 : 0; i <= res; i++) { float a = a0 + (float)i / res * delta; pts.Add(new Vector2(r * Mathf.Cos(a), cy + r * Mathf.Sin(a))); }
            }
            Arc(-len / 2, Mathf.PI * 1.5f, 0, false);
            pts.Add(new Vector2(r, len / 2)); // the straight side (LineCurve)
            Arc(len / 2, 0, Mathf.PI * 0.5f, true);
            return LatheMB(pts, radSeg).Build();
        });

        // ---------- Shape (moveTo / lineTo / quadraticCurveTo / bezierCurveTo) ----------
        public class Shape
        {
            readonly List<Func<int, List<Vector2>>> curves = new List<Func<int, List<Vector2>>>();
            readonly List<bool> isLine = new List<bool>();
            Vector2 cur;
            public Shape MoveTo(float x, float y) { cur = new Vector2(x, y); return this; }
            public Shape LineTo(float x, float y)
            {
                Vector2 a = cur, b = new Vector2(x, y);
                curves.Add(n => new List<Vector2> { a, b }); isLine.Add(true); cur = b; return this;
            }
            public Shape QuadTo(float cx, float cy, float x, float y)
            {
                Vector2 a = cur, c = new Vector2(cx, cy), b = new Vector2(x, y);
                curves.Add(n => { var l = new List<Vector2>(); for (int i = 0; i <= n; i++) { float t = (float)i / n, k = 1 - t; l.Add(k * k * a + 2 * k * t * c + t * t * b); } return l; });
                isLine.Add(false); cur = b; return this;
            }
            public Shape BezierTo(float c1x, float c1y, float c2x, float c2y, float x, float y)
            {
                Vector2 a = cur, c1 = new Vector2(c1x, c1y), c2 = new Vector2(c2x, c2y), b = new Vector2(x, y);
                curves.Add(n => { var l = new List<Vector2>(); for (int i = 0; i <= n; i++) { float t = (float)i / n, k = 1 - t; l.Add(k * k * k * a + 3 * k * k * t * c1 + 3 * k * t * t * c2 + t * t * t * b); } return l; });
                isLine.Add(false); cur = b; return this;
            }
            public List<Vector2> GetPoints(int divisions)
            {
                var pts = new List<Vector2>(); Vector2? last = null;
                for (int c = 0; c < curves.Count; c++)
                    foreach (var p in curves[c](isLine[c] ? 1 : divisions))
                    {
                        if (last.HasValue && (last.Value - p).sqrMagnitude < 1e-14f) continue;
                        pts.Add(p); last = p;
                    }
                return pts;
            }
        }
        static float Area(List<Vector2> c) { float a = 0; for (int p = c.Count - 1, q = 0; q < c.Count; p = q++) a += c[p].x * c[q].y - c[q].x * c[p].y; return a * 0.5f; }
        static void RemoveDupEnd(List<Vector2> c) { if (c.Count > 2 && (c[c.Count - 1] - c[0]).sqrMagnitude < 1e-14f) c.RemoveAt(c.Count - 1); }
        // ear clipping; returns triangles with counter-clockwise (+z facing) winding over the given contour indices
        static List<int> Triangulate(List<Vector2> c)
        {
            var res = new List<int>(); int n = c.Count; if (n < 3) return res;
            var idx = new List<int>(); bool ccw = Area(c) > 0;
            for (int i = 0; i < n; i++) idx.Add(ccw ? i : n - 1 - i);
            int guard = 0;
            while (idx.Count > 3 && guard++ < 10000)
            {
                bool clipped = false;
                for (int i = 0; i < idx.Count; i++)
                {
                    int ia = idx[(i + idx.Count - 1) % idx.Count], ib = idx[i], ic = idx[(i + 1) % idx.Count];
                    Vector2 a = c[ia], b = c[ib], cc = c[ic];
                    if ((b.x - a.x) * (cc.y - a.y) - (b.y - a.y) * (cc.x - a.x) <= 1e-12f) continue; // reflex
                    bool inside = false;
                    foreach (int j in idx)
                    {
                        if (j == ia || j == ib || j == ic) continue;
                        if (InTri(c[j], a, b, cc)) { inside = true; break; }
                    }
                    if (inside) continue;
                    res.Add(ia); res.Add(ib); res.Add(ic); idx.RemoveAt(i); clipped = true; break;
                }
                if (!clipped) { // degenerate leftovers: fan them
                    for (int i = 1; i < idx.Count - 1; i++) { res.Add(idx[0]); res.Add(idx[i]); res.Add(idx[i + 1]); }
                    return res;
                }
            }
            if (idx.Count == 3) { res.Add(idx[0]); res.Add(idx[1]); res.Add(idx[2]); }
            return res;
        }
        static bool InTri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float d1 = (p.x - b.x) * (a.y - b.y) - (a.x - b.x) * (p.y - b.y), d2 = (p.x - c.x) * (b.y - c.y) - (b.x - c.x) * (p.y - c.y), d3 = (p.x - a.x) * (c.y - a.y) - (c.x - a.x) * (p.y - a.y);
            bool neg = d1 < 0 || d2 < 0 || d3 < 0, pos = d1 > 0 || d2 > 0 || d3 > 0;
            return !(neg && pos);
        }
        // ShapeGeometry: flat in XY, normal +z
        public static MB ShapeMB(Shape s, int curveSegments = 12)
        {
            var pts = s.GetPoints(curveSegments);
            if (Area(pts) > 0) pts.Reverse(); // three makes the outline clockwise
            RemoveDupEnd(pts);
            var b = new MB();
            float minx = float.MaxValue, miny = float.MaxValue; foreach (var p in pts) { minx = Mathf.Min(minx, p.x); miny = Mathf.Min(miny, p.y); }
            foreach (var p in pts) { b.v.Add(new Vector3(p.x, p.y, 0)); b.n.Add(new Vector3(0, 0, 1)); b.uv.Add(p); }
            var tri = Triangulate(pts);
            for (int i = 0; i < tri.Count; i += 3) b.Tri(tri[i], tri[i + 1], tri[i + 2]);
            return b;
        }
        // ExtrudeGeometry (bevelled when bevelSegments > 0); flat-shaded like three's non-indexed result
        public static MB ExtrudeMB(Shape s, float depth, int curveSegments, bool bevel, float thick = 0.2f, float size = 0.1f, int bevelSegs = 3)
        {
            var contour = s.GetPoints(curveSegments);
            if (!(Area(contour) < 0)) contour.Reverse();
            RemoveDupEnd(contour);
            var faces = Triangulate(contour); // CCW triangles over contour indices
            int vlen = contour.Count; if (!bevel) { bevelSegs = 0; thick = 0; size = 0; }
            var move = new Vector2[vlen];
            for (int i = 0; i < vlen; i++) { int j = (i - 1 + vlen) % vlen, k = (i + 1) % vlen; move[i] = BevelVec(contour[i], contour[j], contour[k]); }
            var b = new MB();
            void Layer(float z, float bs) { for (int i = 0; i < vlen; i++) { var p = contour[i] + move[i] * bs; b.v.Add(new Vector3(p.x, p.y, z)); b.uv.Add(p); } }
            for (int k = 0; k < bevelSegs; k++) { float t = (float)k / bevelSegs, z = thick * Mathf.Cos(t * Mathf.PI / 2), bs = size * Mathf.Sin(t * Mathf.PI / 2); Layer(-z, bs); }
            Layer(0, size);
            Layer(depth, size); // steps = 1
            for (int k = bevelSegs - 1; k >= 0; k--) { float t = (float)k / bevelSegs, z = thick * Mathf.Cos(t * Mathf.PI / 2), bs = size * Mathf.Sin(t * Mathf.PI / 2); Layer(depth + z, bs); }
            int layers = 1 + bevelSegs * 2; // number of side bands
            // lids
            for (int i = 0; i < faces.Count; i += 3) b.Tri(faces[i + 2], faces[i + 1], faces[i]);
            int top = vlen * layers;
            for (int i = 0; i < faces.Count; i += 3) b.Tri(faces[i] + top, faces[i + 1] + top, faces[i + 2] + top);
            // side walls
            for (int i = vlen - 1; i >= 0; i--)
            {
                int j = i, k = i - 1 < 0 ? vlen - 1 : i - 1;
                for (int sl = 0; sl < layers; sl++)
                {
                    int s1 = vlen * sl, s2 = vlen * (sl + 1);
                    int a = j + s1, bb = k + s1, c = k + s2, d = j + s2;
                    b.Tri(a, bb, d); b.Tri(bb, c, d);
                }
            }
            for (int i = 0; i < b.v.Count; i++) b.n.Add(Vector3.up);
            return b.Flat();
        }
        static Vector2 BevelVec(Vector2 pt, Vector2 prev, Vector2 next)
        {
            float tx, ty, shrink;
            float vpx = pt.x - prev.x, vpy = pt.y - prev.y, vnx = next.x - pt.x, vny = next.y - pt.y;
            float vplsq = vpx * vpx + vpy * vpy, col = vpx * vny - vpy * vnx;
            if (Mathf.Abs(col) > 1e-7f)
            {
                float vpl = Mathf.Sqrt(vplsq), vnl = Mathf.Sqrt(vnx * vnx + vny * vny);
                float ppx = prev.x - vpy / vpl, ppy = prev.y + vpx / vpl, pnx = next.x - vny / vnl, pny = next.y + vnx / vnl;
                float sf = ((pnx - ppx) * vny - (pny - ppy) * vnx) / (vpx * vny - vpy * vnx);
                tx = ppx + vpx * sf - pt.x; ty = ppy + vpy * sf - pt.y;
                float tl = tx * tx + ty * ty;
                if (tl <= 2) return new Vector2(tx, ty);
                shrink = Mathf.Sqrt(tl / 2);
            }
            else
            {
                bool same;
                if (vpx > 1e-7f) same = vnx > 1e-7f;
                else if (vpx < -1e-7f) same = vnx < -1e-7f;
                else same = Mathf.Sign(vpy) == Mathf.Sign(vny);
                if (same) { tx = -vpy; ty = vpx; shrink = Mathf.Sqrt(vplsq); }
                else { tx = vpx; ty = vpy; shrink = Mathf.Sqrt(vplsq / 2); }
            }
            return new Vector2(tx / shrink, ty / shrink);
        }

        // ---------- CatmullRomCurve3 (centripetal by default) ----------
        public class CatmullRom
        {
            readonly Vector3[] pts; readonly float pow;
            public CatmullRom(IList<Vector3> p, string type = "centripetal") { pts = new Vector3[p.Count]; p.CopyTo(pts, 0); pow = type == "chordal" ? 0.5f : 0.25f; }
            static float Calc(float x0, float x1, float x2, float x3, float dt0, float dt1, float dt2, float w)
            {
                float t1 = (x1 - x0) / dt0 - (x2 - x0) / (dt0 + dt1) + (x2 - x1) / dt1;
                float t2 = (x2 - x1) / dt1 - (x3 - x1) / (dt1 + dt2) + (x3 - x2) / dt2;
                t1 *= dt1; t2 *= dt1;
                float c0 = x1, c1 = t1, c2 = -3 * x1 + 3 * x2 - 2 * t1 - t2, c3 = 2 * x1 - 2 * x2 + t1 + t2;
                return c0 + c1 * w + c2 * w * w + c3 * w * w * w;
            }
            public Vector3 GetPoint(float t)
            {
                int l = pts.Length; float p = (l - 1) * t; int ip = Mathf.FloorToInt(p); float w = p - ip;
                if (w == 0 && ip == l - 1) { ip = l - 2; w = 1; }
                ip = Mathf.Clamp(ip, 0, l - 2);
                Vector3 p0 = ip > 0 ? pts[ip - 1] : pts[0] - pts[1] + pts[0];
                Vector3 p1 = pts[ip], p2 = pts[ip + 1];
                Vector3 p3 = ip + 2 < l ? pts[ip + 2] : pts[l - 1] - pts[l - 2] + pts[l - 1];
                float dt0 = Mathf.Pow((p0 - p1).sqrMagnitude, pow), dt1 = Mathf.Pow((p1 - p2).sqrMagnitude, pow), dt2 = Mathf.Pow((p2 - p3).sqrMagnitude, pow);
                if (dt1 < 1e-4f) dt1 = 1; if (dt0 < 1e-4f) dt0 = dt1; if (dt2 < 1e-4f) dt2 = dt1;
                return new Vector3(Calc(p0.x, p1.x, p2.x, p3.x, dt0, dt1, dt2, w), Calc(p0.y, p1.y, p2.y, p3.y, dt0, dt1, dt2, w), Calc(p0.z, p1.z, p2.z, p3.z, dt0, dt1, dt2, w));
            }
        }
        // TubeGeometry along a curve (parallel-transport frames)
        public static MB TubeMB(CatmullRom c, int tubular, float radius, int radial)
        {
            var b = new MB();
            Vector3 prevN = Vector3.zero;
            for (int i = 0; i <= tubular; i++)
            {
                float u = (float)i / tubular;
                var p = c.GetPoint(u); var tg = (c.GetPoint(Mathf.Min(1, u + 1e-3f)) - c.GetPoint(Mathf.Max(0, u - 1e-3f))).normalized;
                Vector3 nrm = i == 0 ? Vector3.Cross(tg, Mathf.Abs(tg.y) < 0.9f ? Vector3.up : Vector3.right).normalized : Vector3.Cross(Vector3.Cross(tg, prevN), tg).normalized;
                prevN = nrm; var bin = Vector3.Cross(tg, nrm);
                for (int j = 0; j <= radial; j++)
                {
                    float v = (float)j / radial * Mathf.PI * 2, sn = Mathf.Sin(v), cs = -Mathf.Cos(v);
                    var n = (cs * nrm + sn * bin).normalized;
                    b.v.Add(p + n * radius); b.n.Add(n); b.uv.Add(new Vector2(u, (float)j / radial));
                }
            }
            for (int j = 1; j <= tubular; j++)
                for (int i = 1; i <= radial; i++)
                {
                    int a = (radial + 1) * (j - 1) + (i - 1), bb = (radial + 1) * j + (i - 1), cc = (radial + 1) * j + i, d = (radial + 1) * (j - 1) + i;
                    b.Tri(a, bb, d); b.Tri(bb, cc, d);
                }
            return b;
        }
        // PlaneGeometry with segments (XY plane, +z normal)
        public static MB PlaneMB(float w, float h, int ws = 1, int hs = 1)
        {
            var b = new MB();
            for (int iy = 0; iy <= hs; iy++)
                for (int ix = 0; ix <= ws; ix++)
                {
                    b.v.Add(new Vector3(ix * w / ws - w / 2, -(iy * h / hs - h / 2), 0)); b.n.Add(new Vector3(0, 0, 1)); b.uv.Add(new Vector2((float)ix / ws, 1 - (float)iy / hs));
                }
            for (int iy = 0; iy < hs; iy++)
                for (int ix = 0; ix < ws; ix++)
                {
                    int a = ix + (ws + 1) * iy, bb = ix + (ws + 1) * (iy + 1), c = ix + 1 + (ws + 1) * (iy + 1), d = ix + 1 + (ws + 1) * iy;
                    b.Tri(a, bb, d); b.Tri(bb, c, d);
                }
            return b;
        }
    }

    // A mesh whose vertices are rewritten every frame (cloth, scarf ribbons, flags); normals follow three's formula.
    // twoSided adds a mirrored back copy with inverted normals (three.js DoubleSide on a lit material).
    public class DynMesh
    {
        public readonly Mesh mesh = new Mesh(); public readonly Geo.MB mb; public readonly Vector3[] basePos; readonly bool two;
        readonly Vector3[] vbuf, nbuf;
        public DynMesh(Geo.MB m, bool twoSided = false)
        {
            mb = m; basePos = m.v.ToArray(); two = twoSided; int c = m.v.Count, k = two ? 2 : 1;
            vbuf = new Vector3[c * k]; nbuf = new Vector3[c * k];
            mesh.MarkDynamic();
            Fill(); mesh.vertices = vbuf; mesh.normals = nbuf;
            if (m.uv.Count == c) { var uv = new Vector2[c * k]; for (int i = 0; i < c * k; i++) uv[i] = m.uv[i % c]; mesh.uv = uv; }
            var t = m.UnityTriangles(); var tt = new int[t.Length * k];
            for (int i = 0; i < t.Length; i++) tt[i] = t[i];
            if (two) for (int i = 0; i < t.Length; i += 3) { tt[t.Length + i] = t[i] + c; tt[t.Length + i + 1] = t[i + 2] + c; tt[t.Length + i + 2] = t[i + 1] + c; }
            mesh.triangles = tt; mesh.RecalculateBounds();
        }
        void Fill()
        {
            int c = mb.v.Count;
            for (int i = 0; i < c; i++) { vbuf[i] = mb.v[i]; nbuf[i] = i < mb.n.Count ? mb.n[i] : Vector3.up; if (two) { vbuf[i + c] = vbuf[i]; nbuf[i + c] = -nbuf[i]; } }
        }
        public void Apply(bool recomputeNormals = true)
        {
            if (recomputeNormals) mb.ComputeNormals();
            Fill(); mesh.vertices = vbuf; mesh.normals = nbuf; mesh.RecalculateBounds();
        }
    }
}