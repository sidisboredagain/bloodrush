using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BR
{
    // A transform plus an explicit euler (radians, applied in three.js YXZ order which equals Unity's Euler order).
    // Keeping the angles ourselves avoids Unity's lossy euler read-back.
    public class N
    {
        public Transform t; public Vector3 r; public bool xyz; // pivots (three.js Groups) use the default XYZ order; meshes made by msh() use YXZ
        public N(Transform t, bool xyz = false) { this.t = t; this.xyz = xyz; }
        public float rx { get => r.x; set { r.x = value; Apply(); } }
        public float ry { get => r.y; set { r.y = value; Apply(); } }
        public float rz { get => r.z; set { r.z = value; Apply(); } }
        public void SetRot(float x, float y, float z) { r = new Vector3(x, y, z); Apply(); }
        public void Apply() { t.localRotation = xyz ? Mk.XYZ(r.x, r.y, r.z) : Quaternion.Euler(r.x * Mathf.Rad2Deg, r.y * Mathf.Rad2Deg, r.z * Mathf.Rad2Deg); }
        public Vector3 pos { get => t.localPosition; set => t.localPosition = value; }
        public float px { get => t.localPosition.x; set { var p = t.localPosition; p.x = value; t.localPosition = p; } }
        public float py { get => t.localPosition.y; set { var p = t.localPosition; p.y = value; t.localPosition = p; } }
        public float pz { get => t.localPosition.z; set { var p = t.localPosition; p.z = value; t.localPosition = p; } }
        public void Scale(float s) { t.localScale = new Vector3(s, s, s); }
        public GameObject go => t.gameObject;
    }

    public struct AABB
    {
        public Vector3 min, max;
        public AABB(Vector3 a, Vector3 b) { min = a; max = b; }
        public bool Contains(Vector3 p) => p.x >= min.x && p.x <= max.x && p.y >= min.y && p.y <= max.y && p.z >= min.z && p.z <= max.z;
    }

    // Hit-box tag on enemy parts.
    public class Part : MonoBehaviour { public Ent e; public bool head; }

    public static class Mk
    {
        public const int LayerEnemy = 8, LayerLevel = 9, LayerIgnore = 2;
        public static bool colliders;          // add hit boxes while building enemy rigs
        public static int layer = 0;
        public static readonly List<Transform> billboards = new List<Transform>();

        public static N Piv(Transform parent, float x = 0, float y = 0, float z = 0)
        {
            var g = new GameObject("p") { layer = layer };
            var t = g.transform; t.SetParent(parent, false); t.localPosition = new Vector3(x, y, z);
            return new N(t, true);
        }
        public static N Piv(N parent, float x = 0, float y = 0, float z = 0) => Piv(parent.t, x, y, z);
        // three.js Euler order XYZ (radians)
        public static Quaternion XYZ(float rx, float ry, float rz) =>
            Quaternion.AngleAxis(rx * Mathf.Rad2Deg, Vector3.right) * Quaternion.AngleAxis(ry * Mathf.Rad2Deg, Vector3.up) * Quaternion.AngleAxis(rz * Mathf.Rad2Deg, Vector3.forward);

        public static GameObject Msh(Transform parent, Mesh mesh, Material mat, float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0)
        {
            var g = new GameObject("m") { layer = layer };
            var t = g.transform; t.SetParent(parent, false); t.localPosition = new Vector3(x, y, z);
            t.localRotation = Quaternion.Euler(rx * Mathf.Rad2Deg, ry * Mathf.Rad2Deg, rz * Mathf.Rad2Deg);
            g.AddComponent<MeshFilter>().sharedMesh = mat && Mats.twoSided.Contains(mat) ? TwoSided(mesh) : mesh;
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            r.lightProbeUsage = LightProbeUsage.Off; r.reflectionProbeUsage = ReflectionProbeUsage.Off;
            if (colliders) { g.AddComponent<BoxCollider>(); g.AddComponent<Part>(); }
            return g;
        }
        public static GameObject Msh(N parent, Mesh mesh, Material mat, float x = 0, float y = 0, float z = 0, float rx = 0, float ry = 0, float rz = 0) =>
            Msh(parent.t, mesh, mat, x, y, z, rx, ry, rz);

        // three.js DoubleSide on a lit material flips the normal on back faces; URP Lit does not, so double-sided lit
        // meshes get a mirrored back copy with inverted normals and are drawn with back-face culling instead.
        static readonly Dictionary<Mesh, Mesh> twoSidedCache = new Dictionary<Mesh, Mesh>();
        public static Mesh TwoSided(Mesh m)
        {
            if (twoSidedCache.TryGetValue(m, out var d) && d) return d;
            var v = m.vertices; var n = m.normals; var uv = m.uv; var t = m.triangles; int c = v.Length;
            var v2 = new Vector3[c * 2]; var n2 = new Vector3[c * 2]; var uv2 = new Vector2[uv.Length == c ? c * 2 : 0]; var t2 = new int[t.Length * 2];
            for (int i = 0; i < c; i++) { v2[i] = v2[i + c] = v[i]; n2[i] = n.Length == c ? n[i] : Vector3.up; n2[i + c] = -n2[i]; if (uv2.Length > 0) uv2[i] = uv2[i + c] = uv[i]; }
            for (int i = 0; i < t.Length; i += 3) { t2[i] = t[i]; t2[i + 1] = t[i + 1]; t2[i + 2] = t[i + 2]; t2[t.Length + i] = t[i] + c; t2[t.Length + i + 1] = t[i + 2] + c; t2[t.Length + i + 2] = t[i + 1] + c; }
            d = new Mesh { name = m.name + "_2s", indexFormat = c * 2 > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16 };
            d.vertices = v2; d.normals = n2; if (uv2.Length > 0) d.uv = uv2; d.triangles = t2; d.RecalculateBounds();
            twoSidedCache[m] = d; return d;
        }
        public static void Head(params GameObject[] gs) { foreach (var g in gs) { var p = g.GetComponent<Part>(); if (p) p.head = true; } }

        // Camera-facing additive glow quad (three.js Sprite).
        public static GameObject Glow(Transform parent, int color, float size, float opacity = 1, bool fog = true)
        {
            var g = new GameObject("glow") { layer = LayerIgnore };
            var t = g.transform; t.SetParent(parent, false); t.localScale = new Vector3(size, size, size);
            g.AddComponent<MeshFilter>().sharedMesh = Geo.Plane(1, 1);
            var r = g.AddComponent<MeshRenderer>(); r.sharedMaterial = Mats.Add(color, opacity, Tex.Glow, fog);
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            billboards.Add(t);
            return g;
        }
        public static GameObject Glow(N parent, int color, float size, float opacity = 1, bool fog = true) => Glow(parent.t, color, size, opacity, fog);

        public static void SetLayerRecursive(GameObject g, int layer)
        {
            g.layer = layer;
            foreach (Transform c in g.transform) SetLayerRecursive(c.gameObject, layer);
        }
        public static void StripColliders(GameObject g)
        {
            foreach (var c in g.GetComponentsInChildren<Collider>()) Object.Destroy(c);
            SetLayerRecursive(g, LayerIgnore);
        }
        public static void DestroyTree(GameObject g)
        {
            if (!g) return;
            foreach (var r in g.GetComponentsInChildren<Renderer>())
                if (r.sharedMaterial && r.sharedMaterial.name.EndsWith("(BR)")) Object.Destroy(r.sharedMaterial);
            Object.Destroy(g);
        }
    }

    // Runtime materials, cloned from templates in Resources (see Editor/BRBuild.cs).
    public static class Mats
    {
        static Material lit, litD, unlit, unlitD, trans, add;
        public static void Init()
        {
            lit = Resources.Load<Material>("BR_Lit"); litD = Resources.Load<Material>("BR_LitDouble");
            unlit = Resources.Load<Material>("BR_Unlit"); unlitD = Resources.Load<Material>("BR_UnlitDouble");
            trans = Resources.Load<Material>("BR_Transparent"); add = Resources.Load<Material>("BR_Additive");
            if (!lit || !unlit || !trans || !add) Debug.LogError("BLOODRUSH: material templates missing - run BloodRush/Setup Resources");
        }
        public static Color Hex(int h) => new Color(((h >> 16) & 255) / 255f, ((h >> 8) & 255) / 255f, (h & 255) / 255f, 1);
        public static Color Css(string s) { ColorUtility.TryParseHtmlString(s, out var c); return c; }
        static Material Clone(Material src) { var m = new Material(src); m.name = src.name + "(BR)"; return m; }

        // three MeshStandardMaterial
        public static readonly HashSet<Material> twoSided = new HashSet<Material>();
        public static Material Std(int color, float rough = 1f, float metal = 0f, int emissive = 0, Texture tex = null, bool dbl = false)
        {
            var m = Clone(lit); if (dbl) twoSided.Add(m);
            m.SetColor("_BaseColor", Hex(color));
            if (tex) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Smoothness", 1 - rough); m.SetFloat("_Metallic", metal);
            m.SetColor("_EmissionColor", Hex(emissive));
            return m;
        }
        public static void Emissive(Material m, int hex, float intensity = 1) => m.SetColor("_EmissionColor", Hex(hex) * intensity);
        // three MeshBasicMaterial
        public static Material Basic(int color, Texture tex = null, bool dbl = false)
        {
            var m = Clone(dbl ? unlitD : unlit);
            m.SetColor("_BaseColor", Hex(color));
            if (tex) m.SetTexture("_BaseMap", tex);
            return m;
        }
        public static Material Trans(int color, float alpha = 1, bool dbl = false, bool fog = true)
        {
            var m = Clone(trans);
            var c = Hex(color); c.a = alpha; m.SetColor("_BaseColor", c);
            m.SetFloat("_Cull", dbl ? 0 : 2); m.SetFloat("_UseFog", fog ? 1 : 0);
            return m;
        }
        public static Material Add(int color, float opacity = 1, Texture tex = null, bool fog = true, bool dbl = true)
        {
            var m = Clone(add);
            var c = Hex(color); c.a = opacity; m.SetColor("_BaseColor", c);
            if (tex) m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Cull", dbl ? 0 : 2); m.SetFloat("_UseFog", fog ? 1 : 0);
            return m;
        }
        public static void SetAlpha(Material m, float a) { var c = m.GetColor("_BaseColor"); c.a = a; m.SetColor("_BaseColor", c); }
        public static void SetColor(Material m, int hex) { var a = m.GetColor("_BaseColor").a; var c = Hex(hex); c.a = a; m.SetColor("_BaseColor", c); }
        public static void SetColor(Material m, Color c) { var a = m.GetColor("_BaseColor").a; c.a = a; m.SetColor("_BaseColor", c); }
    }
}
