using System.IO;
using UnityEditor;
using UnityEngine;
using Queenless.Rules;

namespace Queenless.EditorTools
{
    public static class QueenlessTools
    {
        [MenuItem("Queenless/Run Author Tests")]
        public static void RunAuthorTests()
        {
            string report;
            string dir = Path.Combine(Application.dataPath, "Levels");
            int code = RuleTests.Run(dir, out report);
            if (code == 0)
                Debug.Log(report);
            else
                Debug.LogError(report);
            Directory.CreateDirectory(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TestResults"));
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "TestResults", "author-tests.txt"), report);
        }

        [MenuItem("Queenless/Create Prefabs")]
        public static void CreatePrefabs()
        {
            SpriteFactory.Ensure();
            Directory.CreateDirectory("Assets/Prefabs");
            Save("Assets/Prefabs/Drone.prefab", typeof(DroneView));
            Save("Assets/Prefabs/Mite.prefab", typeof(MiteView));
            AssetDatabase.SaveAssets();
        }

        static void Save(string path, System.Type viewType)
        {
            var go = new GameObject(Path.GetFileNameWithoutExtension(path));
            go.AddComponent<SpriteRenderer>();
            go.AddComponent(viewType);
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        [MenuItem("Queenless/Build Windows")]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds/Windows");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Queenless.unity" },
                locationPathName = "Builds/Windows/Queenless.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            });
            Debug.Log("Windows build: " + report.summary.result);
        }

        [MenuItem("Queenless/Build WebGL")]
        public static void BuildWebGL()
        {
            Directory.CreateDirectory("Builds/WebGL");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/Queenless.unity" },
                locationPathName = "Builds/WebGL",
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            });
            Debug.Log("WebGL build: " + report.summary.result);
        }
    }

    [InitializeOnLoad]
    static class QueenlessCompileCheck
    {
        static QueenlessCompileCheck()
        {
            EditorApplication.delayCall += delegate
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                    return;
                QueenlessTools.RunAuthorTests();
                if (!File.Exists("Assets/Prefabs/Drone.prefab") || !File.Exists("Assets/Prefabs/Mite.prefab"))
                    QueenlessTools.CreatePrefabs();
            };
        }
    }
}
