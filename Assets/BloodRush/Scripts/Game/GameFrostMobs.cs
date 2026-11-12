using UnityEngine;

namespace BR
{
    // The Frozen Circle's own creatures (levels 6-10). They use the husk skeleton (same pivots, so AnimateHusk, the hitboxes,
    // executions and gore all work unchanged) but have their own bodies:
    //   RIMEBOUND THRALL (kind "frost")   a gaunt corpse the cold got up again: hunched and starved, ribs open to the frost,
    //                                     a crown of ice shards out of the skull, an icicle jaw, long claws of ice, a broken shackle
    //   GLACIAL WARDEN (kind "glacier")   a hulking jailer half swallowed by a glacier: ice boulders growing off its back and
    //                                     shoulders, a helm of solid ice with a burning slit, horns, a fist frozen into a maul
    // The Rime Spitter keeps its eye-drone body; it just grows ice (DressDrone, GameKinds).
    public partial class Game
    {
        // a six-sided crystal (thicker at the base, tapering to a point): the frost on these creatures grows in these
        static Mesh Crystal(float r, float h) => Geo.Cyl(r * 0.15f, r, h, 6);
        HuskRig BuildFrostHusk(Ent e, bool brute)
        {
            var skin = EM(e, Mats.Std(brute ? 0x3c4a5a : 0x8ea4b6, brute ? 0.7f : 0.55f, 0, brute ? 0x04080e : 0x061220));
            var bone = EM(e, Mats.Std(0xd4e2ea, 0.45f, 0, 0x0a141c));
            var dark = EM(e, Mats.Std(brute ? 0x1a2430 : 0x26303a, 0.92f));
            var ice = EM(e, Mats.Std(0xc8ecff, 0.08f, 0.2f, 0x0c3a5a));
            var crust = EM(e, Mats.Trans(0xb8e4ff, 0.55f));
            var glow = EM(e, Mats.Basic(0x7af0ff));
            var r = new HuskRig { glow = glow, frost = true };
            r.root = Mk.Piv(e.g.transform);
            r.hips = Mk.Piv(r.root, 0, 1.0f, 0);
            if (!brute) BuildThrall(r, skin, bone, dark, ice, crust, glow); else BuildWarden(r, skin, bone, dark, ice, crust, glow);
            e.mats.Clear(); e.mats.Add(skin); e.mats.Add(bone); e.mats.Add(dark); e.mats.Add(ice);
            // the breath of something that shouldn't be breathing: frost mist from the mouth, ice glinting
            r.extra = (en, dt) =>
            {
                if (en.dead) return;
                if (rand < dt * (brute ? 2.2f : 1.6f)) { var hp = r.head.t.TransformPoint(V(0, 0.02f, 0.2f)); particles.Add(new Pt { pos = hp, scale = Vector3.one * (brute ? 0.1f : 0.06f), mat = SnowMat, v = r.head.t.forward * 1.2f + V(0, 0.4f, 0), life = 0.7f, grav = 0 }); }
                if (rand < dt * 0.8f) SpawnParticles(r.torso.t.TransformPoint(V((rand - 0.5f) * 0.4f, 0.3f + rand * 0.4f, -0.2f)), 1, 0.3f, cyanMat, 0.04f, 0.6f, -1);
            };
            return r;
        }

        void BuildThrall(HuskRig r, Material skin, Material bone, Material dark, Material ice, Material crust, Material glow)
        {
            // pelvis and the frozen rags still tied round it
            Mk.Msh(r.hips, Geo.Sphere(1, 10, 8), skin, 0, 0, 0).transform.localScale = V(0.19f, 0.11f, 0.12f);
            for (int k = 0; k < 7; k++) { float a = (k / 6f - 0.5f) * 3.6f; Mk.Msh(r.hips, Geo.Box(0.1f, 0.32f + (k % 3) * 0.1f, 0.015f), dark, Mathf.Sin(a) * 0.19f, -0.18f - (k % 3) * 0.05f, Mathf.Cos(a) * 0.13f, 0.12f * Mathf.Cos(a), a, (k % 2 - 0.5f) * 0.2f); }
            Mk.Msh(r.hips, Geo.Torus(0.18f, 0.02f, 14, 4), dark, 0, 0.04f, 0, Mathf.PI / 2, 0, 0).transform.localScale = V(1, 0.72f, 1);
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var thigh = Mk.Piv(r.hips, 0.13f * sx, -0.06f, 0);
                Mk.Msh(thigh, GeoX.Limb(0.085f, 0.06f, 0.48f), skin);
                var knee = Mk.Piv(thigh, 0, -0.48f, 0);
                Mk.Msh(knee, Geo.Sphere(0.065f, 8, 6), bone, 0, 0, 0.03f);
                Mk.Msh(knee, GeoX.Limb(0.06f, 0.04f, 0.44f), skin);
                Mk.Msh(knee, Crystal(0.04f, 0.18f), ice, 0.03f * sx, -0.2f, -0.05f, -0.6f, 0, sx * 0.3f); // frost growing off the shin
                // a long bony foot with clawed toes
                Mk.Msh(knee, Geo.Box(0.09f, 0.05f, 0.22f), skin, 0, -0.46f, 0.06f);
                for (int k = -1; k <= 1; k++) Mk.Msh(knee, Geo.Cone(0.018f, 0.1f, 4), ice, k * 0.03f, -0.47f, 0.19f, Mathf.PI / 2 + 0.2f, 0, 0);
                r.thigh[i] = thigh; r.knee[i] = knee;
            }
            // the torso: a starved belly, a ribcage split open in front with the frost-heart glowing inside, ice up the spine
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            Mk.Msh(r.torso, GeoX.Capsule(0.11f, 0.1f, 3, 10), skin, 0, 0.14f, -0.01f).transform.localScale = V(1.15f, 1, 0.8f);
            Mk.Msh(r.torso, GeoX.Capsule(0.15f, 0.2f, 3, 12), skin, 0, 0.4f, -0.02f).transform.localScale = V(1.3f, 1, 0.85f);
            for (int k = 0; k < 4; k++) { var rib = Mk.Msh(r.torso, Geo.Torus(0.17f - k * 0.008f, 0.014f, 12, 4, Mathf.PI * 0.9f), bone, 0, 0.5f - k * 0.075f, 0.02f, Mathf.PI / 2, 0, 0); rib.transform.localScale = V(1.18f, 0.9f, 1); }
            Mk.Msh(r.torso, Geo.Box(0.035f, 0.26f, 0.03f), bone, 0, 0.4f, 0.135f);
            Mk.Msh(r.torso, Geo.Sphere(0.07f, 10, 8), glow, 0, 0.36f, 0.06f);
            var hg = Gl(r.torso, 0x60e8ff, 0.7f, 0.7f); hg.transform.localPosition = V(0, 0.36f, 0.12f);
            foreach (var sx in new[] { -1f, 1f }) Mk.Msh(r.torso, Geo.Box(0.12f, 0.16f, 0.03f), bone, sx * 0.1f, 0.5f, -0.14f, 0.2f, sx * 0.3f, 0); // shoulder blades through the skin
            for (int k = 0; k < 6; k++) Mk.Msh(r.torso, Crystal(0.035f + (k % 3) * 0.015f, 0.16f + (k % 3) * 0.1f + (k == 2 ? 0.12f : 0)), ice, 0, 0.12f + k * 0.085f, -0.15f, -2.2f - k * 0.08f, 0, (k % 2 - 0.5f) * 0.4f); // the spine ridge
            Mk.Msh(r.torso, Geo.Icosahedron(1), crust, 0.16f, 0.52f, 0.02f).transform.localScale = V(0.12f, 0.1f, 0.12f);
            // the neck and the skull: long, hollow-eyed, crowned with shards of ice, an icicle jaw
            r.neck = Mk.Piv(r.torso, 0, 0.62f, 0.02f); r.neck.SetRot(0.25f, 0, 0);
            Mk.Msh(r.neck, Geo.Cyl(0.04f, 0.055f, 0.14f, 8), skin, 0, 0.05f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f);
            var skull = Mk.Msh(r.head, Geo.Sphere(0.15f, 14, 10), bone, 0, 0.08f, -0.01f); skull.transform.localScale = V(0.9f, 1.0f, 1.15f);
            var face = Mk.Msh(r.head, Geo.Sphere(0.11f, 12, 8), skin, 0, 0.04f, 0.07f); face.transform.localScale = V(1.05f, 1.1f, 0.9f);
            var brow = Mk.Msh(r.head, Geo.Box(0.22f, 0.04f, 0.06f), bone, 0, 0.13f, 0.14f, 0.3f, 0, 0);
            var sockets = Mk.Msh(r.head, Geo.Box(0.17f, 0.045f, 0.03f), dark, 0, 0.09f, 0.165f);
            var e0 = Mk.Msh(r.head, Geo.Sphere(0.022f, 8, 6), glow, -0.045f, 0.09f, 0.18f); var e1 = Mk.Msh(r.head, Geo.Sphere(0.022f, 8, 6), glow, 0.045f, 0.09f, 0.18f);
            var nose = Mk.Msh(r.head, Geo.Box(0.03f, 0.05f, 0.02f), dark, 0, 0.035f, 0.18f);
            Mk.Head(skull, face, brow, sockets, e0, e1, nose);
            for (int k = 0; k < 5; k++) { float a = (k - 2) * 0.42f; Mk.Head(Mk.Msh(r.head, Crystal(0.03f + (k == 2 ? 0.015f : 0), 0.2f + (2 - Mathf.Abs(k - 2)) * 0.08f), ice, Mathf.Sin(a) * 0.1f, 0.2f, -0.06f + Mathf.Cos(a) * 0.02f, -0.6f, 0, -a * 0.9f)); } // the crown of shards
            r.jaw = Mk.Piv(r.head, 0, 0.0f, 0.03f);
            var jaw = Mk.Msh(r.jaw, Geo.Box(0.14f, 0.04f, 0.15f), bone, 0, -0.03f, 0.06f);
            Mk.Head(jaw);
            for (int k = 0; k < 6; k++) Mk.Head(Mk.Msh(r.jaw, Geo.Cone(0.012f, 0.05f, 4), ice, (k - 2.5f) * 0.024f, 0.0f, 0.13f)); // fangs
            for (int k = 0; k < 3; k++) Mk.Head(Mk.Msh(r.jaw, Geo.Cone(0.018f, 0.12f + k % 2 * 0.06f, 5), ice, (k - 1) * 0.04f, -0.09f, 0.1f, Mathf.PI, 0, 0)); // icicles off the chin
            // arms: too long, bone-thin, ice bracers, claws of ice; a broken shackle still on the left wrist
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.33f * sx, 0.55f, 0);
                Mk.Msh(sh, Geo.Sphere(0.07f, 8, 6), bone, 0, 0, 0);
                Mk.Msh(sh, GeoX.Limb(0.06f, 0.045f, 0.42f), skin);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, GeoX.Limb(0.048f, 0.036f, 0.4f), skin);
                for (int k = 0; k < 3; k++) Mk.Msh(elbow, Crystal(0.035f, 0.14f + k * 0.04f), ice, sx * 0.04f, -0.12f - k * 0.08f, -0.03f, 0, 0, sx * (0.9f + k * 0.1f));
                var hand = Mk.Piv(elbow, 0, -0.42f, 0);
                Mk.Msh(hand, Geo.Box(0.08f, 0.07f, 0.05f), skin, 0, 0.02f, 0);
                for (int k = -1; k <= 2; k++) { var cl = Mk.Piv(hand, k * 0.025f - 0.01f, -0.02f, 0.02f); cl.SetRot(Mathf.PI + 0.35f, 0, k * 0.08f); Mk.Msh(cl, Geo.Cyl(0.004f, 0.017f, 0.28f, 6), ice, 0, 0.14f, 0); }
                if (sx < 0) { Mk.Msh(elbow, Geo.Torus(0.06f, 0.02f, 10, 4), dark, 0, -0.36f, 0, Mathf.PI / 2, 0, 0); for (int k = 0; k < 3; k++) Mk.Msh(elbow, Geo.Torus(0.035f, 0.01f, 8, 4), dark, 0, -0.42f - k * 0.06f, 0.02f, 0, k * 1.57f, 0); }
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
        }

        void BuildWarden(HuskRig r, Material skin, Material bone, Material dark, Material ice, Material crust, Material glow)
        {
            // heavy hips girded with ice plates
            Mk.Msh(r.hips, Geo.Box(0.48f, 0.24f, 0.3f), skin, 0, 0, 0);
            for (int k = 0; k < 6; k++) { float a = (k / 5f - 0.5f) * 3.2f; Mk.Msh(r.hips, Geo.Box(0.17f, 0.3f, 0.04f), ice, Mathf.Sin(a) * 0.25f, -0.14f, Mathf.Cos(a) * 0.17f, 0.18f * Mathf.Cos(a), a, 0); }
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var thigh = Mk.Piv(r.hips, 0.13f * sx, -0.06f, 0);
                Mk.Msh(thigh, GeoX.Limb(0.11f, 0.09f, 0.48f), skin);
                Mk.Msh(thigh, Geo.Box(0.17f, 0.3f, 0.06f), ice, 0.02f * sx, -0.22f, 0.08f, 0.08f, 0, 0);
                var knee = Mk.Piv(thigh, 0, -0.48f, 0);
                Mk.Msh(knee, Geo.Icosahedron(0.09f), ice, 0, 0, 0.06f);
                Mk.Msh(knee, GeoX.Limb(0.095f, 0.08f, 0.44f), skin);
                Mk.Msh(knee, Geo.Box(0.16f, 0.34f, 0.06f), ice, 0, -0.2f, 0.07f);
                Mk.Msh(knee, Geo.Box(0.19f, 0.09f, 0.28f), dark, 0, -0.45f, 0.04f);
                for (int k = 0; k < 2; k++) Mk.Msh(knee, Crystal(0.03f, 0.12f), ice, (k - 0.5f) * 0.1f, -0.4f, -0.1f, -2.4f, 0, 0);
                r.thigh[i] = thigh; r.knee[i] = knee;
            }
            // a barrel of a body, a cracked breastplate of ice with the light coming through, a glacier grown over its back
            r.torso = Mk.Piv(r.hips, 0, 0.06f, 0);
            Mk.Msh(r.torso, GeoX.Capsule(0.24f, 0.18f, 3, 12), skin, 0, 0.34f, -0.02f).transform.localScale = V(1.3f, 1, 0.95f);
            Mk.Msh(r.torso, Geo.Box(0.46f, 0.36f, 0.08f), ice, 0, 0.38f, 0.19f, -0.08f, 0, 0);
            foreach (var (cx, cy, cr) in new[] { (-0.08f, 0.42f, 0.5f), (0.07f, 0.34f, -0.7f), (0f, 0.28f, 0.1f) }) Mk.Msh(r.torso, Geo.Box(0.012f, 0.16f, 0.01f), glow, cx, cy, 0.235f, 0, 0, cr);
            Mk.Msh(r.torso, Geo.Sphere(0.08f, 10, 8), glow, 0, 0.34f, 0.2f); var cg = Gl(r.torso, 0x60e8ff, 1.0f, 0.7f); cg.transform.localPosition = V(0, 0.34f, 0.28f);
            Mk.Msh(r.torso, Geo.Icosahedron(1), ice, 0, 0.55f, -0.18f).transform.localScale = V(0.3f, 0.2f, 0.22f);
            for (int k = 0; k < 7; k++) { float a = (k - 3) * 0.35f; Mk.Msh(r.torso, Crystal(0.06f + (k % 3) * 0.02f, 0.3f + (3 - Mathf.Abs(k - 3)) * 0.12f), k % 2 == 0 ? ice : crust, Mathf.Sin(a) * 0.22f, 0.5f + Mathf.Cos(a) * 0.06f, -0.2f, -0.55f, 0, -a * 0.8f); } // the glacier on its back
            // the head sunk between the shoulders: a helm of solid ice, a burning slit, horns of ice sweeping forward
            r.neck = Mk.Piv(r.torso, 0, 0.62f, 0.02f);
            Mk.Msh(r.neck, Geo.Cyl(0.1f, 0.13f, 0.12f, 10), skin, 0, 0.03f, 0);
            r.head = Mk.Piv(r.neck, 0, 0.12f, 0.02f);
            var helm = Mk.Msh(r.head, Geo.Sphere(0.19f, 12, 10), ice, 0, 0.1f, 0); helm.transform.localScale = V(1.05f, 0.95f, 1.05f);
            var face = Mk.Msh(r.head, Geo.Box(0.24f, 0.2f, 0.06f), ice, 0, 0.07f, 0.16f);
            var slit = Mk.Msh(r.head, Geo.Box(0.18f, 0.03f, 0.02f), glow, 0, 0.1f, 0.195f); var nslit = Mk.Msh(r.head, Geo.Box(0.03f, 0.12f, 0.02f), glow, 0, 0.05f, 0.196f);
            Mk.Head(helm, face, slit, nslit);
            foreach (var sx in new[] { -1f, 1f }) { var hp = Mk.Piv(r.head, sx * 0.15f, 0.2f, 0); hp.SetRot(-0.3f, 0, -sx * 0.9f); var p = hp; for (int k = 0; k < 4; k++) { Mk.Head(Mk.Msh(p, Geo.Cyl(0.04f * (1 - (k + 1) / 4.5f), 0.04f * (1 - k / 4.5f) + 0.005f, 0.13f, 6), k < 2 ? dark : ice, 0, 0.065f, 0)); p = Mk.Piv(p, 0, 0.13f, 0); p.SetRot(0.35f, 0, sx * 0.35f); } }
            for (int k = 0; k < 4; k++) Mk.Head(Mk.Msh(r.head, Crystal(0.035f, 0.14f + k % 2 * 0.06f), ice, (k - 1.5f) * 0.07f, 0.28f, -0.06f, -0.4f, 0, (k - 1.5f) * 0.25f));
            r.jaw = Mk.Piv(r.head, 0, 0.0f, 0.03f);
            var jaw = Mk.Msh(r.jaw, Geo.Box(0.2f, 0.06f, 0.16f), skin, 0, -0.03f, 0.08f); Mk.Head(jaw);
            for (int k = 0; k < 2; k++) Mk.Head(Mk.Msh(r.jaw, Geo.Cone(0.025f, 0.12f, 5), bone, (k - 0.5f) * 0.12f, 0.03f, 0.15f, -0.2f, 0, 0)); // tusks
            // arms: thick, ice boulders for pauldrons, gauntlets of ice; the right fist frozen into a maul
            for (int i = 0; i < 2; i++)
            {
                int sx = i == 0 ? -1 : 1;
                var sh = Mk.Piv(r.torso, 0.33f * sx, 0.55f, 0);
                Mk.Msh(sh, Geo.Icosahedron(1), ice, 0.06f * sx, 0.06f, 0).transform.localScale = V(0.2f, 0.15f, 0.2f);
                for (int k = 0; k < 3; k++) Mk.Msh(sh, Crystal(0.04f, 0.18f + k * 0.06f), ice, 0.06f * sx + (k - 1) * 0.07f, 0.18f, (k - 1) * 0.04f, 0, 0, -sx * (0.25f + k * 0.15f));
                Mk.Msh(sh, GeoX.Limb(0.1f, 0.085f, 0.42f), skin);
                var elbow = Mk.Piv(sh, 0, -0.42f, 0);
                Mk.Msh(elbow, GeoX.Limb(0.088f, 0.075f, 0.4f), skin);
                Mk.Msh(elbow, Geo.Box(0.17f, 0.24f, 0.17f), ice, 0, -0.27f, 0, 0, 0.3f, 0);
                var hand = Mk.Piv(elbow, 0, -0.42f, 0);
                if (sx > 0)
                {
                    Mk.Msh(hand, Geo.Icosahedron(1), ice, 0, -0.12f, 0.02f).transform.localScale = V(0.22f, 0.2f, 0.24f);
                    for (int k = 0; k < 5; k++) { float a = k * 1.25f; Mk.Msh(hand, Crystal(0.04f, 0.14f), ice, Mathf.Cos(a) * 0.16f, -0.14f + Mathf.Sin(a * 2) * 0.05f, Mathf.Sin(a) * 0.16f, Mathf.Sin(a) * 1.4f, 0, -Mathf.Cos(a) * 1.4f); }
                    Mk.Msh(hand, Geo.Box(0.3f, 0.03f, 0.02f), glow, 0, -0.12f, 0.25f);
                }
                else { Mk.Msh(hand, Geo.Box(0.13f, 0.1f, 0.1f), skin, 0, -0.04f, 0); for (int k = -1; k <= 1; k++) Mk.Msh(hand, Geo.Cone(0.025f, 0.17f, 5), ice, k * 0.045f, -0.14f, 0.04f, Mathf.PI + 0.3f, 0, 0); }
                r.sh[i] = sh; r.elbow[i] = elbow; r.hand[i] = hand;
            }
        }
    }
}
