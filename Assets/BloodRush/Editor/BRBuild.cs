using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

// Build tooling for BLOODRUSH.
//   BloodRush/Setup Resources  - creates the runtime material templates the game clones at startup
//   BloodRush/Build Windows    - setup + builds Build/BloodRush.exe
// Both can run headless:  Unity.exe -batchmode -projectPath <proj> -executeMethod BRBuild.BuildWindows -quit
public static class BRBuild
{
    const string ResDir = "Assets/BloodRush/Resources";
    const string Scene = "Assets/Scenes/SampleScene.unity";

    [MenuItem("BloodRush/Setup Resources")]
    public static void Setup()
    {
        Directory.CreateDirectory(ResDir);
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        var unlit = Shader.Find("Universal Render Pipeline/Unlit");
        Make("BR_Lit", lit, m => { m.SetFloat("_Smoothness", 0f); m.SetFloat("_Metallic", 0f); EnableEmission(m); });
        Make("BR_LitDouble", lit, m => { m.SetFloat("_Smoothness", 0f); EnableEmission(m); m.SetFloat("_Cull", 0f); m.doubleSidedGI = true; });
        Make("BR_Unlit", unlit, m => { });
        Make("BR_UnlitDouble", unlit, m => { m.SetFloat("_Cull", 0f); });
        Make("BR_Transparent", Shader.Find("BR/Transparent"), m => { });
        Make("BR_Additive", Shader.Find("BR/Additive"), m => { });
        Make("BR_XRay", Shader.Find("BR/XRay"), m => { });
        Make("BR_XRayMask", Shader.Find("BR/XRayMask"), m => { });
        // cutscene: sky dome, planet, atmosphere, points; a normal-mapped Lit for the sea
        Make("BR_Sky", Shader.Find("BR/Sky"), m => { });
        Make("BR_Planet", Shader.Find("BR/Planet"), m => { });
        Make("BR_Atmo", Shader.Find("BR/Atmo"), m => { });
        Make("BR_Points", Shader.Find("BR/Points"), m => { });
        Make("BR_LitNormal", lit, m => { m.SetFloat("_Smoothness", 0f); m.EnableKeyword("_NORMALMAP"); });
        // post-processing for the cinematic camera (bloom + ACES tone mapping + exposure); an asset so its shader variants ship
        string vpPath = $"{ResDir}/BR_CineVolume.asset";
        AssetDatabase.DeleteAsset(vpPath);
        var vp = ScriptableObject.CreateInstance<UnityEngine.Rendering.VolumeProfile>();
        AssetDatabase.CreateAsset(vp, vpPath);
        var bl = vp.Add<UnityEngine.Rendering.Universal.Bloom>(true); bl.intensity.Override(0.5f); bl.threshold.Override(0.95f); bl.scatter.Override(0.7f);
        var tm = vp.Add<UnityEngine.Rendering.Universal.Tonemapping>(true); tm.mode.Override(UnityEngine.Rendering.Universal.TonemappingMode.ACES);
        var ca = vp.Add<UnityEngine.Rendering.Universal.ColorAdjustments>(true); ca.postExposure.Override(0.5f);
        foreach (var c in vp.components) { c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; AssetDatabase.AddObjectToAsset(c, vp); }
        EditorUtility.SetDirty(vp);

        // The game uses linear fog: make sure the scene asks for it so the fog shader variants are not stripped.
        var scene = EditorSceneManager.OpenScene(Scene, OpenSceneMode.Single);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 35;
        RenderSettings.fogEndDistance = 175;
        BRPreviewSetup.Ensure(scene, true); // editor-only level preview (removed at Play by Boot)
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    static void EnableEmission(Material m)
    {
        m.EnableKeyword("_EMISSION");
        // not pure black: URP's material validation strips _EMISSION from materials whose emission colour is black,
        // and the game sets the real emission colour at runtime
        m.SetColor("_EmissionColor", new Color(0.002f, 0.002f, 0.002f));
        m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
    }

    static void Make(string name, Shader sh, System.Action<Material> setup)
    {
        if (sh == null) throw new System.Exception("BRBuild: shader missing for " + name);
        var m = new Material(sh) { enableInstancing = true };
        setup(m);
        string path = $"{ResDir}/{name}.mat";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(m, path);
    }

    [MenuItem("BloodRush/Build Windows")]
    public static void BuildWindows()
    {
        Setup();
        PlayerSettings.productName = "BLOODRUSH COOP"; // its own settings/save slot, apart from the solo game
        PlayerSettings.companyName = "BloodRush";
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        PlayerSettings.resizableWindow = true;
        PlayerSettings.runInBackground = true;
        PlayerSettings.visibleInBackground = true;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Standalone, ScriptingImplementation.Mono2x);
        var opts = new BuildPlayerOptions
        {
            scenes = new[] { Scene },
            locationPathName = "Build/BLOODRUSH.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };
        var rep = BuildPipeline.BuildPlayer(opts);
        Debug.Log($"BR_BUILD_RESULT {rep.summary.result} errors={rep.summary.totalErrors} size={rep.summary.totalSize}");
        if (Application.isBatchMode) EditorApplication.Exit(rep.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
