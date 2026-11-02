using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TruckSandSortDemo.Editor
{
    /// <summary>Creates the isolated demo scene and connects the supplied project prefabs.</summary>
    [InitializeOnLoad]
    public static class TruckSandSortDemoBuilder
    {
        public const string ScenePath = "Assets/TruckSandSortDemo/Scenes/TruckSandSort_Demo.unity";
        const string AutoBuildSessionKey = "TruckSandSortDemo_AutoBuild_v2";

        static TruckSandSortDemoBuilder()
        {
            EditorApplication.delayCall += AutoBuildOnce;
        }

        static void AutoBuildOnce()
        {
            if (SessionState.GetBool(AutoBuildSessionKey, false)) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                EditorApplication.delayCall += AutoBuildOnce;
                return;
            }

            SessionState.SetBool(AutoBuildSessionKey, true);
            if (!File.Exists(ScenePath)) BuildDemoScene();
        }

        [MenuItem("Tools/Truck Sand Sort/Build Demo Scene", priority = 100)]
        public static void BuildDemoScene()
        {
            EnsureFolder("Assets/TruckSandSortDemo/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("TruckSandSort_Demo");
            var controller = root.AddComponent<DemoSequenceController>();
            controller.hopperPrefab = LoadPrefab("Hopper");
            controller.conveyorPrefab = LoadPrefab("Conveyor");
            controller.bucketPrefab = LoadPrefab("Bucket");
            controller.conePrefab = LoadPrefab("Cone");
            controller.plantPrefab = LoadPrefab("Plant");
            controller.lampPrefab = LoadPrefab("light");
            controller.playbackSpeed = 1f;
            controller.autoReplay = true;
            controller.finalHoldSeconds = 3f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AddToBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;
            Debug.Log("[TruckSandSortDemo] Built portrait demo scene: " + ScenePath);
        }

        [MenuItem("Tools/Truck Sand Sort/Open Demo Scene", priority = 101)]
        public static void OpenDemoScene()
        {
            if (!File.Exists(ScenePath)) BuildDemoScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("Tools/Truck Sand Sort/Set Portrait Game View", priority = 102)]
        public static void SetPortraitGameView()
        {
            PlayerSettings.defaultScreenWidth = 1080;
            PlayerSettings.defaultScreenHeight = 1920;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = false;
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "Portrait target configured",
                "Player defaults are now 1080 x 1920. In the Game view, choose or create a 9:16 aspect before recording.",
                "OK");
        }

        static GameObject LoadPrefab(string name)
        {
            string[] guids = AssetDatabase.FindAssets(name + " t:Prefab", new[] { "Assets/3D Assets - Copy" });
            string exact = guids.Select(AssetDatabase.GUIDToAssetPath)
                .FirstOrDefault(path => Path.GetFileNameWithoutExtension(path).Equals(name, System.StringComparison.OrdinalIgnoreCase));
            if (string.IsNullOrEmpty(exact))
            {
                Debug.LogWarning("[TruckSandSortDemo] Optional supplied prefab not found: " + name);
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(exact);
        }

        static void AddToBuildSettings()
        {
            var current = EditorBuildSettings.scenes.ToList();
            int existing = current.FindIndex(scene => scene.path == ScenePath);
            if (existing >= 0) current.RemoveAt(existing);
            current.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = current.ToArray();
        }

        static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
