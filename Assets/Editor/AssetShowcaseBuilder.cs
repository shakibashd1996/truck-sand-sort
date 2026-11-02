using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Builds a stylized showcase scene (purple backdrop, glossy plastic, bloom) with every model in
// "3D Assets - Copy", each using a URP Lit material made from its own textures.
[InitializeOnLoad]
public static class AssetShowcaseBuilder
{
    const string AssetsRoot = "Assets/3D Assets - Copy";
    const string ScenePath = "Assets/Scenes/AssetShowcase.unity";
    const string ShowcaseFolder = "Assets/Scenes/AssetShowcase";
    const string AutoRunKey = "AssetShowcaseBuilder_AutoRun_v5";

    const float CameraPitch = 24f;
    const float ItemGap = 0.5f;
    const float CardPadding = 0.35f;

    static readonly Color Backdrop = new Color(0.17f, 0.08f, 0.32f);
    static readonly Color FloorColor = new Color(0.22f, 0.11f, 0.40f);
    static readonly Color CardColor = new Color(0.33f, 0.20f, 0.56f);

    // One card per entry; a card holds one or more items. Size = largest dimension after scaling.
    class Item { public string Model; public float Size; public float Yaw; public float Hue = -1f; public string Variant; }
    class Card { public string Label; public Item[] Items; }

    static readonly Card[][] Rows =
    {
        new[]
        {
            new Card { Label = "Truck", Items = new[] { new Item { Model = "Truck", Size = 2.6f, Yaw = 150f } } },
            new Card { Label = "Hopper", Items = new[] { new Item { Model = "Hopper", Size = 2.0f, Yaw = 0f } } },
            new Card { Label = "Conveyor", Items = new[] { new Item { Model = "Conveyor", Size = 3.0f, Yaw = 0f } } },
        },
        new[]
        {
            new Card
            {
                Label = "Buckets (Color Variations)",
                Items = new[]
                {
                    new Item { Model = "Bucket", Size = 0.9f },
                    new Item { Model = "Bucket", Size = 0.9f, Hue = 0.13f, Variant = "Yellow" },
                    new Item { Model = "Bucket", Size = 0.9f, Hue = 0.99f, Variant = "Red" },
                    new Item { Model = "Bucket", Size = 0.9f, Hue = 0.30f, Variant = "Green" },
                },
            },
            new Card { Label = "Cone", Items = new[] { new Item { Model = "Cone", Size = 1.0f, Yaw = -20f } } },
            new Card { Label = "Lamp", Items = new[] { new Item { Model = "Light", Size = 1.5f, Yaw = 0f } } },
            new Card { Label = "Plant", Items = new[] { new Item { Model = "Plant", Size = 1.1f, Yaw = 0f } } },
        },
    };

    static AssetShowcaseBuilder()
    {
        if (EditorPrefs.GetBool(AutoRunKey, false)) return;
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorPrefs.SetBool(AutoRunKey, true);
            Build();
        };
    }

    [MenuItem("Tools/Build Asset Showcase")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!AssetDatabase.IsValidFolder(ShowcaseFolder)) AssetDatabase.CreateFolder("Assets/Scenes", "AssetShowcase");

        var prefabs = new Dictionary<string, GameObject>();
        var materials = new Dictionary<string, Material>();
        foreach (var folder in AssetDatabase.GetSubFolders(AssetsRoot))
        {
            var prefab = PrepareModel(folder, out var mat);
            if (prefab == null) continue;
            prefabs[Path.GetFileName(folder)] = prefab;
            materials[Path.GetFileName(folder)] = mat;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Showcase");
        var cardMat = SaveMaterial("Card", CardColor, 0.45f);
        var floorMat = SaveMaterial("Floor", FloorColor, 0.25f);

        var all = new Bounds(Vector3.zero, Vector3.zero);
        float rowZ = 0f;
        // Back row first (furthest from camera), then step towards the camera.
        foreach (var row in Rows)
        {
            var placedCards = new List<(GameObject go, Bounds b, Card card)>();
            float x = 0f;
            float rowDepth = 0f;
            foreach (var card in row)
            {
                var cardRoot = new GameObject(card.Label);
                cardRoot.transform.SetParent(root.transform, false);
                float ix = x;
                foreach (var item in card.Items)
                {
                    if (!prefabs.TryGetValue(item.Model, out var prefab)) continue;
                    var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, cardRoot.transform);
                    if (item.Variant != null)
                    {
                        go.name += "_" + item.Variant;
                        var vmat = BucketVariant(materials[item.Model], item.Variant, item.Hue);
                        foreach (var r in go.GetComponentsInChildren<Renderer>())
                            r.sharedMaterials = Enumerable.Repeat(vmat, r.sharedMaterials.Length).ToArray();
                    }
                    // Keep the FBX root's own rotation (axis conversion from the DCC), only add the yaw.
                    go.transform.rotation = Quaternion.Euler(0f, item.Yaw, 0f) * prefab.transform.rotation;
                    var b = GetBounds(go);
                    go.transform.localScale *= item.Size / Mathf.Max(b.size.x, b.size.y, b.size.z);
                    b = GetBounds(go);
                    go.transform.position += new Vector3(ix - b.min.x, -b.min.y, rowZ - b.center.z);
                    b = GetBounds(go);
                    ix = b.max.x + ItemGap * 0.6f;
                    rowDepth = Mathf.Max(rowDepth, b.size.z);
                    if (item.Model == "Light") AddLampGlow(b, go.transform);
                }
                var cb = GetBounds(cardRoot);
                placedCards.Add((cardRoot, cb, card));
                x = cb.max.x + CardPadding * 2f + ItemGap;
            }

            // Center the row on X.
            float shift = -(x - CardPadding * 2f - ItemGap) * 0.5f;
            foreach (var (go, _, card) in placedCards)
            {
                go.transform.position += Vector3.right * shift;
                var b = GetBounds(go);
                AddCard(go.transform, b, cardMat);
                AddLabel(card.Label, new Vector3(b.min.x + 0.1f, 0.03f, b.min.z - CardPadding + 0.08f), go.transform);
                all.Encapsulate(b);
            }
            rowZ -= rowDepth + CardPadding * 2f + 0.9f;
        }

        var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.position = new Vector3(all.center.x, -0.06f, all.center.z);
        floor.transform.localScale = Vector3.one * 6f;
        floor.GetComponent<Renderer>().sharedMaterial = floorMat;

        SetupLighting();
        var cam = SetupCamera(all);
        SetupPostProcessing();

        EditorSceneManager.SaveScene(scene, ScenePath);

        var sv = SceneView.lastActiveSceneView;
        if (sv != null) sv.AlignViewToObject(cam.transform);
        Selection.activeGameObject = root;
        Debug.Log($"[AssetShowcase] Built {ScenePath} — check the Game view for post-processing.");
    }

    // ---------- models & materials ----------

    static GameObject PrepareModel(string folder, out Material mat)
    {
        mat = null;
        var fbxPath = AssetDatabase.FindAssets("t:Model", new[] { folder })
            .Select(AssetDatabase.GUIDToAssetPath).FirstOrDefault();
        if (fbxPath == null) return null;

        var texPaths = AssetDatabase.FindAssets("t:Texture2D", new[] { folder + "/Textures" })
            .Select(AssetDatabase.GUIDToAssetPath).ToArray();
        string baseMap = texPaths.FirstOrDefault(p => p.EndsWith("_BaseMap.png"));
        string normal = texPaths.FirstOrDefault(p => p.Contains("_Normal"));
        string mask = texPaths.FirstOrDefault(p => p.Contains("_MaskMap"));

        if (normal != null)
            FixImporter(normal, ti => { ti.textureType = TextureImporterType.NormalMap; ti.sRGBTexture = false; });
        if (mask != null)
            FixImporter(mask, ti => ti.sRGBTexture = false);

        mat = CreateMaterial(folder, Path.GetFileNameWithoutExtension(fbxPath), baseMap, normal);

        // Remap any material embedded in the FBX (only the Cone has one).
        var importer = (ModelImporter)AssetImporter.GetAtPath(fbxPath);
        bool changed = false;
        foreach (var m in AssetDatabase.LoadAllAssetsAtPath(fbxPath).OfType<Material>())
        {
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), mat);
            changed = true;
        }
        if (changed) importer.SaveAndReimport();

        // Most of these FBX files carry no material at all (Unity falls back to its default), so a remap
        // can't reach them. Save a prefab per model with the material assigned directly.
        var fbx = AssetDatabase.LoadAssetAtPath<GameObject>(fbxPath);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(fbx);
        foreach (var r in instance.GetComponentsInChildren<Renderer>())
            r.sharedMaterials = Enumerable.Repeat(mat, r.sharedMaterials.Length).ToArray();
        var prefab = PrefabUtility.SaveAsPrefabAsset(instance, $"{folder}/{fbx.name}.prefab");
        Object.DestroyImmediate(instance);
        return prefab;
    }

    static void FixImporter(string path, System.Action<TextureImporter> apply)
    {
        var ti = (TextureImporter)AssetImporter.GetAtPath(path);
        var before = (ti.textureType, ti.sRGBTexture);
        apply(ti);
        if (before != (ti.textureType, ti.sRGBTexture)) ti.SaveAndReimport();
    }

    static Material CreateMaterial(string folder, string name, string baseMap, string normal)
    {
        string matFolder = folder + "/Materials";
        if (!AssetDatabase.IsValidFolder(matFolder)) AssetDatabase.CreateFolder(folder, "Materials");
        var mat = LoadOrCreateMaterial($"{matFolder}/{name}_Mat.mat");

        mat.SetColor("_BaseColor", Color.white);
        if (baseMap != null)
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(baseMap));

        if (normal != null)
        {
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normal));
            mat.SetFloat("_BumpScale", 1f);
            mat.EnableKeyword("_NORMALMAP");
        }

        // Stylized glossy plastic: the mask maps hold no metal and low smoothness, and their AO doubles
        // the shading already baked into the BaseMaps, so drive the surface with constants instead.
        mat.SetTexture("_MetallicGlossMap", null);
        mat.DisableKeyword("_METALLICSPECGLOSSMAP");
        mat.SetFloat("_Metallic", 0f);
        mat.SetFloat("_Smoothness", 0.62f);
        mat.SetTexture("_OcclusionMap", null);
        mat.SetFloat("_OcclusionStrength", 0f);
        mat.DisableKeyword("_OCCLUSIONMAP");

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    static Material LoadOrCreateMaterial(string path)
    {
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null) return mat;
        mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    static Material SaveMaterial(string name, Color color, float smoothness)
    {
        var mat = LoadOrCreateMaterial($"{ShowcaseFolder}/{name}.mat");
        mat.SetColor("_BaseColor", color);
        mat.SetFloat("_Smoothness", smoothness);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    // The bucket's BaseMap is blue; make hue-shifted copies for the color variations.
    static Material BucketVariant(Material source, string variant, float targetHue)
    {
        var src = (Texture2D)source.GetTexture("_BaseMap");
        string srcPath = AssetDatabase.GetAssetPath(src);
        string dstPath = $"{Path.GetDirectoryName(srcPath).Replace('\\', '/')}/Bucket_BaseMap_{variant}.png";

        if (!File.Exists(dstPath))
        {
            var tex = new Texture2D(2, 2);
            tex.LoadImage(File.ReadAllBytes(srcPath));
            var px = tex.GetPixels();
            Color.RGBToHSV(AverageColor(px), out float srcHue, out _, out _);
            for (int i = 0; i < px.Length; i++)
            {
                Color.RGBToHSV(px[i], out float h, out float s, out float v);
                h = Mathf.Repeat(h + targetHue - srcHue, 1f);
                px[i] = Color.HSVToRGB(h, s, v);
            }
            tex.SetPixels(px);
            File.WriteAllBytes(dstPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(dstPath);
        }

        var mat = LoadOrCreateMaterial($"{Path.GetDirectoryName(AssetDatabase.GetAssetPath(source)).Replace('\\', '/')}/Bucket_Mat_{variant}.mat");
        mat.CopyPropertiesFromMaterial(source);
        mat.shaderKeywords = source.shaderKeywords;
        mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(dstPath));
        EditorUtility.SetDirty(mat);
        return mat;
    }

    static Color AverageColor(Color[] px)
    {
        Color sum = Color.clear;
        int step = Mathf.Max(1, px.Length / 4096);
        int n = 0;
        for (int i = 0; i < px.Length; i += step) { sum += px[i]; n++; }
        return sum / n;
    }

    // ---------- scene dressing ----------

    static void AddCard(Transform parent, Bounds b, Material mat)
    {
        var card = GameObject.CreatePrimitive(PrimitiveType.Cube);
        card.name = "Card";
        Object.DestroyImmediate(card.GetComponent<Collider>());
        card.transform.SetParent(parent, true);
        card.transform.position = new Vector3(b.center.x, -0.05f, b.center.z);
        card.transform.localScale = new Vector3(b.size.x + CardPadding * 2f, 0.1f, b.size.z + CardPadding * 2f);
        card.GetComponent<Renderer>().sharedMaterial = mat;
    }

    static void AddLabel(string text, Vector3 pos, Transform parent)
    {
        var label = new GameObject("Label");
        label.transform.SetParent(parent, true);
        label.transform.position = pos;
        label.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        var tm = label.AddComponent<TextMesh>();
        tm.text = text;
        tm.anchor = TextAnchor.LowerLeft;
        tm.characterSize = 0.05f;
        tm.fontSize = 64;
        tm.color = Color.white;
    }

    static void AddLampGlow(Bounds b, Transform parent)
    {
        var glow = new GameObject("LampGlow");
        glow.transform.SetParent(parent, true);
        glow.transform.position = new Vector3(b.center.x, b.max.y - b.size.y * 0.15f, b.center.z - 0.15f);
        var l = glow.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.72f, 0.3f);
        l.intensity = 2.5f;
        l.range = 1.6f;
    }

    static void SetupLighting()
    {
        var key = new GameObject("Key Light").AddComponent<Light>();
        key.type = LightType.Directional;
        key.color = new Color(1f, 0.96f, 0.92f);
        key.intensity = 1.5f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = 0.6f;
        key.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        // Cool back light for the bright rim on the edges.
        var rim = new GameObject("Rim Light").AddComponent<Light>();
        rim.type = LightType.Directional;
        rim.color = new Color(0.75f, 0.6f, 1f);
        rim.intensity = 0.9f;
        rim.shadows = LightShadows.None;
        rim.transform.rotation = Quaternion.Euler(25f, 160f, 0f);

        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.78f, 0.68f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.52f, 0.40f, 0.78f);
        RenderSettings.ambientGroundColor = new Color(0.28f, 0.16f, 0.45f);
        RenderSettings.fog = false;
    }

    static Camera SetupCamera(Bounds all)
    {
        var cam = new GameObject("Main Camera").AddComponent<Camera>();
        cam.tag = "MainCamera";
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Backdrop;
        cam.fieldOfView = 30f;
        cam.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        float aspect = 16f / 9f;
        float halfH = Mathf.Atan(Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad) * aspect);
        float dist = (all.extents.x + 0.4f) / Mathf.Tan(halfH) + all.extents.z;
        var rot = Quaternion.Euler(CameraPitch, 0f, 0f);
        var target = all.center + Vector3.down * all.extents.y * 0.3f;
        cam.transform.SetPositionAndRotation(target - rot * Vector3.forward * dist, rot);
        return cam;
    }

    static void SetupPostProcessing()
    {
        string path = $"{ShowcaseFolder}/ShowcaseVolume.asset";
        AssetDatabase.DeleteAsset(path);
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, path);

        var tone = profile.Add<Tonemapping>(true);
        tone.mode.Override(TonemappingMode.Neutral);

        var color = profile.Add<ColorAdjustments>(true);
        color.postExposure.Override(0.35f);
        color.contrast.Override(12f);
        color.saturation.Override(22f);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(0.7f);
        bloom.scatter.Override(0.65f);

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.28f);
        vignette.smoothness.Override(0.5f);

        foreach (var c in profile.components) AssetDatabase.AddObjectToAsset(c, profile);
        AssetDatabase.SaveAssets();

        var volume = new GameObject("Global Volume").AddComponent<Volume>();
        volume.isGlobal = true;
        volume.sharedProfile = profile;
    }

    static Bounds GetBounds(GameObject go)
    {
        var renderers = go.GetComponentsInChildren<Renderer>().Where(r => !(r is ParticleSystemRenderer)).ToArray();
        if (renderers.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
        var b = renderers[0].bounds;
        foreach (var r in renderers) b.Encapsulate(r.bounds);
        return b;
    }
}
