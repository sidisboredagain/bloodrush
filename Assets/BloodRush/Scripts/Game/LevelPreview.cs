using UnityEngine;

namespace BR
{
    // Editor-only view of the levels (and the ending's city and sea cliff), so the scene isn't empty while editing.
    // It runs the game's own building code; nothing generated is saved into the scene, and in Play mode this object
    // is removed by Boot before the game builds its real level, so gameplay is unchanged.
    [ExecuteAlways]
    public class LevelPreview : MonoBehaviour
    {
        [Tooltip("Show all 10 levels side by side (along X). Untick to show just the one picked below.")]
        public bool showAll = true;
        [Range(1, 10)] public int level = 1;
        public float spacing = 450;
        [Tooltip("Also show the ending cutscene's city and the hero's sea cliff (behind the row of levels).")]
        public bool showCutsceneSets = true;
        int built = -1; Transform root;

        int Key => (showAll ? 1000 + (int)spacing : level) + (showCutsceneSets ? 100000 : 0);
        void OnEnable() { if (!Application.isPlaying) Rebuild(); }
        void OnDisable() { Clear(); }
        void Update() { if (!Application.isPlaying && (built != Key || !root)) Rebuild(); }

        void Rebuild()
        {
            Clear();
            root = new GameObject("Levels (preview)").transform;
            root.SetParent(transform, false);
            // the game draws with three.js's mirrored camera convention; mirroring the preview makes it look the same
            // in Unity's own Scene/Game views (correct faces, same layout)
            root.localScale = new Vector3(-1, 1, 1);
            int from = showAll ? 1 : Mathf.Clamp(level, 1, 10), to = showAll ? 10 : from;
            for (int n = from; n <= to; n++)
            {
                var tmp = new GameObject("BR preview builder") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    var game = tmp.AddComponent<Game>(); // edit mode: Awake does not run, only the level code is used
                    var lv = game.BuildPreview(n);
                    lv.SetParent(root, false);
                    lv.localPosition = new Vector3(-(n - from) * spacing, 0, 0); // laid out left to right in the Scene view
                }
                finally { DestroyImmediate(tmp); }
            }
            if (showCutsceneSets)
            {
                var tmp = new GameObject("BR preview builder") { hideFlags = HideFlags.HideAndDontSave };
                try
                {
                    var sets = tmp.AddComponent<Game>().BuildCinePreview();
                    sets.SetParent(root, false);
                    sets.localPosition = new Vector3(-(to - from) * spacing * 0.5f, 0, -1800); // behind the middle of the row
                }
                catch (System.Exception e) { Debug.LogWarning("BLOODRUSH preview: cutscene sets skipped - " + e.Message); }
                finally { DestroyImmediate(tmp); }
            }
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.hideFlags = HideFlags.DontSave;
            built = Key;
        }
        void Clear()
        {
            built = -1;
            if (Application.isPlaying) return; // Boot destroys the whole object at Play
            if (root) DestroyImmediate(root.gameObject);
            for (int i = transform.childCount - 1; i >= 0; i--) DestroyImmediate(transform.GetChild(i).gameObject);
            root = null;
        }
    }
}
