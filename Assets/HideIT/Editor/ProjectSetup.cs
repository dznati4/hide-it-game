using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace HideIT.Editor
{
    [InitializeOnLoad]
    public static class ProjectSetup
    {
        const string ScenePath = "Assets/HideIT/Scenes/CarryLab.unity";

        static ProjectSetup()
        {
            // Prepare missing material content; the test-room scene is versioned.
            EditorApplication.delayCall += EnsureScene;
        }

        [MenuItem("Hide iT/Open Playtest")]
        public static void OpenLab()
        {
            EnsureScene();
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        public static void EnsureScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
            const string materialPath = "Assets/HideIT/Resources/LabBase.mat";
            if (!File.Exists(materialPath))
            {
                Directory.CreateDirectory("Assets/HideIT/Resources");
                var material = new Material(Shader.Find("Standard"));
                material.name = "LabBase";
                material.SetFloat("_Glossiness", .12f);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            if (!File.Exists(ScenePath))
            {
                throw new FileNotFoundException("The versioned Carry Lab scene is missing.", ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            PlayerSettings.companyName = "ZanettiZONEai";
            PlayerSettings.productName = "Hide iT";
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
        }

        [MenuItem("Hide iT/Build Windows Playtest")]
        public static void BuildWindows()
        {
            EnsureScene();
            if (Resources.Load<Shader>("AsylumKit/Shaders/PSX_Lit") == null ||
                Resources.Load<GameObject>("AsylumKit/Models/asylum_modular_wall_000") == null)
                throw new InvalidOperationException("Import the local Asylum Kit with scripts/import-asylum-kit.ps1 before building.");
            var directory = "Builds/Playtest-" + PlayerSettings.bundleVersion;
            Directory.CreateDirectory(directory);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = directory + "/Hide iT.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Windows build failed: " + report.summary.result);
            Debug.Log("HIDEIT_BUILD_OK: " + report.summary.outputPath);
        }
    }
}
