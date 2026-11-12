using UnityEngine;

namespace BR
{
    // batchmode check: builds every level's editor preview (and the cutscene sets) and reports any exception
    public static class BRPreviewCheck
    {
        public static void Run()
        {
            int fails = 0;
            for (int n = 1; n <= 25; n++)
            {
                var tmp = new GameObject("check");
                try { var lv = tmp.AddComponent<Game>().BuildPreview(n); Debug.Log($"BR_PREVIEW level {n} OK ({lv.GetComponentsInChildren<Renderer>(true).Length} renderers)"); Object.DestroyImmediate(lv.gameObject); }
                catch (System.Exception e) { fails++; Debug.Log($"BR_PREVIEW level {n} FAILED: {e}"); }
                finally { Object.DestroyImmediate(tmp); }
            }
            var t2 = new GameObject("check");
            try { var s = t2.AddComponent<Game>().BuildCinePreview(); Debug.Log("BR_PREVIEW cutscene sets OK"); Object.DestroyImmediate(s.gameObject); }
            catch (System.Exception e) { fails++; Debug.Log($"BR_PREVIEW cutscene FAILED: {e}"); }
            finally { Object.DestroyImmediate(t2); }
            Debug.Log($"BR_PREVIEW_RESULT fails={fails}");
        }
    }
}
