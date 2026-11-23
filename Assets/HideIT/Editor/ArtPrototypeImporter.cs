using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace HideIT.Editor
{
    /// <summary>Import and validate the Carry Lab props and Characters PSX roster.</summary>
    public sealed class ArtPrototypeImporter : AssetPostprocessor
    {
        const string Models = "Assets/HideIT/ArtPrototype/Models";
        const string Psx = "Assets/HideIT/ArtPrototype/CharactersPSX";
        const string PsxModels = Psx + "/Models";
        const string PsxTextures = Psx + "/Textures";
        const string PsxMaterials = Psx + "/Materials";
        const int PsxTextureSize = 256;
        const string Palette = "Assets/HideIT/ArtPrototype/Textures/HIdeIT_Palette.png";
        const string SurfaceMask = "Assets/HideIT/ArtPrototype/Textures/HIdeIT_SurfaceMask.png";
        const string MaterialPath = "Assets/HideIT/ArtPrototype/Materials/HIdeIT_Palette.mat";
        const string Prefabs = "Assets/HideIT/Resources/ArtPrototype";
        static readonly string[] Characters = {
            "Player_Alex_Orange", "Player_Noam_Olive", "Player_Maya_Denim", "Player_Eli_Cream",
            "Player_Roni_Plum", "Security_Navy", "Guest_Plum", "Body_Blue",
            "Civilian_Amir_RustJacket", "Civilian_Ben_CreamShirt", "Civilian_Lior_OliveHoodie",
            "Civilian_Tamar_RoseCardigan", "Civilian_Nora_NavyCoat", "Civilian_Ruth_TealCoat"
        };
        static readonly string[] BoneNames = {
            "Root", "Hips", "Spine", "Chest", "Neck", "Head",
            "LeftShoulder", "LeftUpperArm", "LeftLowerArm", "LeftHand",
            "LeftUpperLeg", "LeftLowerLeg", "LeftFoot", "LeftToes",
            "RightShoulder", "RightUpperArm", "RightLowerArm", "RightHand",
            "RightUpperLeg", "RightLowerLeg", "RightFoot", "RightToes"
        };

        static bool InFolder(string path, string folder) =>
            string.Equals(Path.GetDirectoryName(path)?.Replace('\\', '/'), folder, StringComparison.OrdinalIgnoreCase);

        static bool IsFbx(string path) => string.Equals(Path.GetExtension(path), ".fbx", StringComparison.OrdinalIgnoreCase);

        static bool IsPsxModel(string path) => InFolder(path, PsxModels) && IsFbx(path);

        static bool IsPropModel(string path) => InFolder(path, Models) && IsFbx(path)
            && !IsCharacter(Path.GetFileNameWithoutExtension(path));
        static bool IsCharacter(string id) => Characters.Contains(id);

        void OnPreprocessModel()
        {
            if (!IsPropModel(assetPath) && !IsPsxModel(assetPath)) return;
            var importer = (ModelImporter)assetImporter;
            importer.useFileScale = true;
            importer.globalScale = 1f;
            importer.animationType = IsPsxModel(assetPath)
                ? ModelImporterAnimationType.Generic : ModelImporterAnimationType.None;
            importer.importAnimation = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.addCollider = false;
            importer.optimizeGameObjects = false;
            importer.optimizeBones = false;
            importer.isReadable = true; // Catalog checks inspect imported vertex and skinning data.
            importer.meshCompression = ModelImporterMeshCompression.Off;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
        }

        Material OnAssignMaterialModel(Material importedMaterial, Renderer renderer)
        {
            if (!IsPropModel(assetPath) && !IsPsxModel(assetPath)) return null;
            // Asset creation during an import callback is unsafe. BuildCatalog creates this first,
            // then explicitly reimports all models, so the saved catalog always uses this asset.
            var path = IsPsxModel(assetPath) ? PsxMaterials + "/" + Path.GetFileNameWithoutExtension(assetPath) + ".mat" : MaterialPath;
            return AssetDatabase.LoadAssetAtPath<Material>(path);
        }

        void OnPreprocessTexture()
        {
            bool surfaceMask = string.Equals(assetPath, SurfaceMask, StringComparison.OrdinalIgnoreCase);
            bool psxTexture = InFolder(assetPath, PsxTextures);
            if (!surfaceMask && !psxTexture && !string.Equals(assetPath, Palette, StringComparison.OrdinalIgnoreCase)) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = !surfaceMask;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.mipmapEnabled = psxTexture;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.crunchedCompression = false;
            importer.alphaSource = surfaceMask ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.alphaIsTransparency = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = psxTexture ? PsxTextureSize : 128;
            // Existing platform overrides must not silently reintroduce compression/resizing.
            foreach (var platform in new[] { "Standalone", "Android", "iPhone", "WebGL" })
                importer.ClearPlatformTextureSettings(platform);
        }

        [MenuItem("Hide iT/Art Prototype/Build Validated Catalog")]
        public static void BuildCatalog()
        {
            var report = new ImportReport { utc = DateTime.UtcNow.ToString("O"), unityVersion = Application.unityVersion };
            var staged = new List<GameObject>();
            try
            {
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                var material = EnsureMaterial();
                foreach (var id in Characters)
                    if (!File.Exists(PsxModels + "/" + id + ".fbx"))
                        report.errors.Add("Missing required Characters PSX model: " + id);
                Require(report.errors.Count == 0, "Import Characters PSX 1.2 with art/blender/import_psx_characters.py first.");
                if (!Directory.Exists(Models)) throw new DirectoryNotFoundException(Models);

                var psxModels = Characters.Select(id => PsxModels + "/" + id + ".fbx");
                var props = Directory.GetFiles(Models, "*").Select(path => path.Replace('\\', '/')).Where(IsPropModel);
                foreach (var path in psxModels.Concat(props).OrderBy(p => p, StringComparer.Ordinal))
                {
                    var entry = new AssetReport { id = Path.GetFileNameWithoutExtension(path), model = path };
                    entry.character = IsCharacter(entry.id);
                    report.assets.Add(entry);
                    try
                    {
                        var assetMaterial = entry.character ? EnsurePsxMaterial(entry.id, material.shader) : material;
                        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                        var source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                        Require(source != null, "FBX import did not produce a GameObject.");
                        var wrapper = new GameObject(entry.id);
                        staged.Add(wrapper);
                        var model = (GameObject)PrefabUtility.InstantiatePrefab(source, wrapper.transform);
                        model.name = "Model";
                        model.transform.localPosition = Vector3.zero;
                        // Retain the FBX's import-axis/unit conversion; only correct facing below.
                        ValidateAndNormalize(wrapper, model, assetMaterial, entry);
                    }
                    catch (Exception exception) { entry.errors.Add(exception.Message); }
                }

                foreach (var entry in report.assets)
                    foreach (var error in entry.errors) report.errors.Add(entry.id + ": " + error);
                Require(report.errors.Count == 0, "Catalog validation failed; see art/unity-import-report.json.");
                EnsureFolder(Prefabs);
                // Validate the entire set before replacing any generated prefab.
                foreach (var wrapper in staged)
                {
                    var path = Prefabs + "/" + wrapper.name + ".prefab";
                    var saved = PrefabUtility.SaveAsPrefabAsset(wrapper, path);
                    Require(saved != null, "Could not save prefab " + path);
                    report.assets.Find(a => a.id == wrapper.name).prefab = path;
                }
                AssetDatabase.SaveAssets();
                report.success = true;
                Debug.Log("HIDEIT_ART_CATALOG_OK: " + report.assets.Count + " validated prefabs.");
            }
            catch (Exception exception)
            {
                if (!report.errors.Contains(exception.Message)) report.errors.Add(exception.Message);
                Debug.LogError("HIDEIT_ART_CATALOG_FAILED: " + string.Join(" | ", report.errors));
                throw;
            }
            finally
            {
                foreach (var wrapper in staged) if (wrapper != null) UnityEngine.Object.DestroyImmediate(wrapper);
                var reportPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../art/unity-import-report.json"));
                Directory.CreateDirectory(Path.GetDirectoryName(reportPath));
                File.WriteAllText(reportPath, JsonUtility.ToJson(report, true));
            }
        }

        static Material EnsureMaterial()
        {
            Require(File.Exists(Palette), "Missing palette texture: " + Palette);
            AssetDatabase.ImportAsset(Palette, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(Palette);
            Require(texture != null && texture.width == 128 && texture.height == 128, "Palette must import as 128 x 128.");
            var shader = Shader.Find("Standard");
            Require(shader != null, "Standard shader is unavailable; this importer targets the project's Built-in pipeline.");
            var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                EnsureFolder(Path.GetDirectoryName(MaterialPath).Replace('\\', '/'));
                material = new Material(shader) { name = "HIdeIT_Palette" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            material.shader = shader;
            material.color = Color.white;
            material.mainTexture = texture;
            material.mainTextureScale = Vector2.one;
            material.mainTextureOffset = Vector2.zero;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", .16f);
            material.SetFloat("_SmoothnessTextureChannel", 0f); // Metallic-map alpha, not albedo alpha.
            if (File.Exists(SurfaceMask))
            {
                AssetDatabase.ImportAsset(SurfaceMask, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
                var mask = AssetDatabase.LoadAssetAtPath<Texture2D>(SurfaceMask);
                Require(mask != null && mask.width == 128 && mask.height == 128, "Surface mask must import as 128 x 128.");
                material.SetTexture("_MetallicGlossMap", mask);
                material.EnableKeyword("_METALLICGLOSSMAP");
                material.SetFloat("_GlossMapScale", 1f);
            }
            else
            {
                material.SetTexture("_MetallicGlossMap", null);
                material.DisableKeyword("_METALLICGLOSSMAP");
            }
            material.DisableKeyword("_SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A");
            material.SetFloat("_Mode", 0f);
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        static Material EnsurePsxMaterial(string id, Shader shader)
        {
            var texturePath = PsxTextures + "/" + id + ".png";
            AssetDatabase.ImportAsset(texturePath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            Require(texture != null && texture.filterMode == FilterMode.Point &&
                    texture.width <= PsxTextureSize && texture.height <= PsxTextureSize,
                "Missing or incorrectly imported PSX texture: " + texturePath);
            var materialPath = PsxMaterials + "/" + id + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                EnsureFolder(PsxMaterials);
                material = new Material(shader) { name = id + "_PSX" };
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.mainTexture = texture;
            material.color = Color.white;
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Glossiness", 0f);
            material.SetFloat("_SpecularHighlights", 0f);
            material.SetFloat("_GlossyReflections", 0f);
            material.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            EditorUtility.SetDirty(material);
            return material;
        }

        static void ValidateAndNormalize(GameObject wrapper, GameObject model, Material material, AssetReport entry)
        {
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            var skins = model.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            entry.skinnedRenderers = skins.Length;
            Require(model.GetComponentsInChildren<Collider>(true).Length == 0, "Unexpected imported colliders.");
            Require(model.GetComponentsInChildren<Camera>(true).Length == 0 && model.GetComponentsInChildren<Light>(true).Length == 0,
                "Unexpected imported cameras or lights.");
            Require(renderers.Length == 1, "Expected exactly one renderer, found " + renderers.Length + ".");
            Require(renderers[0].sharedMaterials.Length == 1 && renderers[0].sharedMaterial == material,
                "Expected exactly one shared palette material; check OnAssignMaterialModel remapping.");

            if (entry.character)
            {
                Require(skins.Length == 1, "Character must have one SkinnedMeshRenderer.");
                var nodes = model.GetComponentsInChildren<Transform>(true);
                var bones = new Dictionary<string, Transform>();
                foreach (var name in BoneNames)
                {
                    var matches = nodes.Where(t => t.name == name).ToArray();
                    Require(matches.Length == 1, "Expected exactly one skeleton transform named " + name + ", found " + matches.Length + ".");
                    bones.Add(name, matches[0]);
                }
                foreach (var pair in bones)
                    Require(pair.Key == "Root" || pair.Value.IsChildOf(bones["Root"]), pair.Key + " is outside the Root hierarchy.");
                entry.hierarchyBoneCount = bones.Count;
                entry.boneNames = bones.Keys.ToArray();
                var marker = nodes.SingleOrDefault(t => t.name == "FaceForward" ||
                    t.name.EndsWith("__FaceForward", StringComparison.Ordinal));
                Require(marker != null, "Missing unique FaceForward marker.");
                var forward = wrapper.transform.InverseTransformPoint(marker.position) - wrapper.transform.InverseTransformPoint(bones["Hips"].position);
                entry.forwardBefore = forward;
                Require(Mathf.Abs(forward.z) > .05f && Mathf.Abs(forward.z) > Mathf.Abs(forward.x) * 2f,
                    "FaceForward does not establish a Z-facing character.");
                if (forward.z < 0f)
                {
                    model.transform.localRotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.localRotation;
                    entry.yawCorrection = 180f;
                }
                entry.forwardAfter = wrapper.transform.InverseTransformPoint(marker.position) - wrapper.transform.InverseTransformPoint(bones["Hips"].position);
                Require(entry.forwardAfter.z > .05f, "Facing normalization did not produce +Z forward.");
                var centerX = wrapper.transform.InverseTransformPoint(bones["Hips"].position).x;
                foreach (var part in new[] { "UpperArm", "Hand", "UpperLeg", "Foot" })
                {
                    var left = wrapper.transform.InverseTransformPoint(bones["Left" + part].position);
                    var right = wrapper.transform.InverseTransformPoint(bones["Right" + part].position);
                    entry.orientation.Add(new SideReport { pair = part, leftX = left.x, rightX = right.x });
                    Require(left.x < centerX - .015f && right.x > centerX + .015f,
                        "Left/right orientation invalid for " + part + " after +Z normalization (expected Left -X / Right +X).");
                }
                ValidateSkin(skins[0], bones, entry);
            }
            else Require(skins.Length == 0, "Unrecognized rigged FBX: add its ID to Characters or remove the obsolete export.");

            var bounds = MeasureGeometry(model, entry);
            entry.sizeMeters = bounds.size;
            entry.minimumMeters = bounds.min;
            Require(entry.triangles > 0, "No imported triangles.");
            Require(Finite(bounds.size.x) && Finite(bounds.size.y) && Finite(bounds.size.z), "Non-finite imported bounds.");
            if (entry.character)
                Require(bounds.size.y >= 1.7f && bounds.size.y <= 2.05f,
                    "Character height is " + bounds.size.y.ToString("F4") + " m; expected 1.70–2.05 m. Correct source/export units, not prefab scale.");
            entry.importedRootScale = model.transform.localScale;
            entry.valid = true;
        }


        static void ValidateSkin(SkinnedMeshRenderer skin, Dictionary<string, Transform> hierarchy, AssetReport entry)
        {
            var mesh = skin.sharedMesh;
            Require(mesh != null, "Skinned renderer has no mesh.");
            var palette = skin.bones;
            entry.skinPaletteBones = palette.Length;
            Require(palette.Length > 0 && palette.Length <= hierarchy.Count && mesh.bindposes.Length == palette.Length,
                "Invalid skin palette/bindpose count.");
            Require(palette.Distinct().Count() == palette.Length, "Duplicate skin palette bones.");
            foreach (var bone in palette)
                Require(bone != null && hierarchy.TryGetValue(bone.name, out var expected) && bone == expected,
                    "Skin references a null, unexpected or wrong-identity skeleton transform.");
            entry.skinBoneNames = palette.Select(b => b.name).ToArray();
            entry.unusedHierarchyBones = hierarchy.Keys.Except(entry.skinBoneNames).ToArray();
            var counts = mesh.GetBonesPerVertex();
            var weights = mesh.GetAllBoneWeights(); // These views belong to the mesh; do not dispose them.
            Require(counts.Length == mesh.vertexCount, "Missing per-vertex skin influences.");
            int cursor = 0;
            float maxError = 0f;
            var referenced = new HashSet<int>();
            for (int vertex = 0; vertex < counts.Length; vertex++)
            {
                Require(counts[vertex] > 0, "Vertex " + vertex + " has no skin influences.");
                float sum = 0f;
                for (int influence = 0; influence < counts[vertex]; influence++)
                {
                    Require(cursor < weights.Length, "Truncated skin weight array.");
                    var weight = weights[cursor++];
                    Require(Finite(weight.weight) && weight.weight > 0f && weight.boneIndex >= 0 && weight.boneIndex < palette.Length,
                        "Invalid bone index/weight at vertex " + vertex + ".");
                    referenced.Add(weight.boneIndex);
                    sum += weight.weight;
                }
                maxError = Mathf.Max(maxError, Mathf.Abs(1f - sum));
                Require(Mathf.Abs(1f - sum) <= .002f, "Weights do not sum to one at vertex " + vertex + ": " + sum);
            }
            Require(cursor == weights.Length, "Unconsumed skin weight data.");
            entry.weightedVertices = mesh.vertexCount;
            entry.weightedBones = referenced.Count;
            entry.maximumWeightSumError = maxError;
        }

        static Bounds MeasureGeometry(GameObject model, AssetReport entry)
        {
            bool initialized = false;
            var bounds = new Bounds();
            foreach (var renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Mesh mesh;
                Mesh baked = null;
                try
                {
                    if (renderer is SkinnedMeshRenderer skin)
                    {
                        mesh = skin.sharedMesh;
                        baked = new Mesh();
                        skin.BakeMesh(baked);
                    }
                    else mesh = renderer.GetComponent<MeshFilter>()?.sharedMesh;
                    Require(mesh != null, "Renderer has no measurable mesh.");
                    entry.vertices += mesh.vertexCount;
                    for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                    {
                        Require(mesh.GetTopology(submesh) == MeshTopology.Triangles, "Non-triangle mesh topology.");
                        entry.triangles += (int)(mesh.GetIndexCount(submesh) / 3);
                    }
                    foreach (var vertex in (baked != null ? baked : mesh).vertices)
                    {
                        var position = model.transform.parent.InverseTransformPoint(renderer.transform.TransformPoint(vertex));
                        Require(Finite(position.x) && Finite(position.y) && Finite(position.z), "Non-finite vertex position.");
                        if (!initialized) { bounds = new Bounds(position, Vector3.zero); initialized = true; }
                        else bounds.Encapsulate(position);
                    }
                }
                finally { if (baked != null) UnityEngine.Object.DestroyImmediate(baked); }
            }
            Require(initialized, "No vertices to measure.");
            return bounds;
        }

        static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            var parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        [Serializable] sealed class ImportReport
        {
            public bool success;
            public string utc, unityVersion;
            public string policy = "Characters PSX 1.2; 22 shared transforms; normalized skin weights; +Z forward, Left -X; point-filtered textures.";
            public List<string> errors = new List<string>();
            public List<AssetReport> assets = new List<AssetReport>();
        }
        [Serializable] sealed class AssetReport
        {
            public string id, model, prefab;
            public bool character, valid;
            public int vertices, triangles, skinnedRenderers, hierarchyBoneCount, skinPaletteBones, weightedVertices, weightedBones;
            public float yawCorrection, maximumWeightSumError;
            public Vector3 sizeMeters, minimumMeters, importedRootScale, forwardBefore, forwardAfter;
            public string[] boneNames, skinBoneNames, unusedHierarchyBones;
            public List<SideReport> orientation = new List<SideReport>();
            public List<string> errors = new List<string>();
        }
        [Serializable] sealed class SideReport { public string pair; public float leftX, rightX; }
    }
}
