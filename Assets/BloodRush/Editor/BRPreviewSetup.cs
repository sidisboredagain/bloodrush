using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Makes sure the game scene has a level preview, so the editor shows the arena instead of an empty scene.
// The preview is removed automatically when you press Play (see BR.LevelPreview).
[InitializeOnLoad]
public static class BRPreviewSetup
{
    const string Scene = "Assets/Scenes/SampleScene.unity";
    static BRPreviewSetup()
    {
        EditorSceneManager.sceneOpened += (s, m) => EditorApplication.delayCall += () => Ensure(s, false);
        EditorApplication.delayCall += () => Ensure(SceneManager.GetActiveScene(), false);
    }

    [MenuItem("BloodRush/Add Level Preview to Scene")]
    static void AddFromMenu() => Ensure(SceneManager.GetActiveScene(), true);

    public static void Ensure(Scene s, bool fromMenu)
    {
        if (Application.isPlaying || EditorApplication.isPlayingOrWillChangePlaymode || !s.IsValid() || !s.isLoaded) return;
        if (!fromMenu && s.path != Scene) return;
        foreach (var root in s.GetRootGameObjects()) if (root.GetComponentInChildren<BR.LevelPreview>(true)) return;
        bool wasDirty = s.isDirty;
        var go = new GameObject("BLOODRUSH Level Preview (editor only)");
        SceneManager.MoveGameObjectToScene(go, s);
        go.AddComponent<BR.LevelPreview>();
        EditorSceneManager.MarkSceneDirty(s);
        if (!wasDirty && !Application.isBatchMode) EditorSceneManager.SaveScene(s);
        if (!Application.isBatchMode)
        {
            Selection.activeGameObject = go;
            if (SceneView.lastActiveSceneView) SceneView.lastActiveSceneView.LookAt(new Vector3(0, 6, 0), Quaternion.Euler(35, 30, 0), 90);
        }
    }
}
