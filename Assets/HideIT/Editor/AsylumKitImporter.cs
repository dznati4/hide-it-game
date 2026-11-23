using System;
using UnityEditor;
using UnityEngine;

namespace HideIT.Editor
{
    public sealed class AsylumKitImporter : AssetPostprocessor
    {
        const string Root = "Assets/HideIT/Resources/AsylumKit/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;
            var texture = (TextureImporter)assetImporter;
            texture.filterMode = FilterMode.Point;
            texture.mipmapEnabled = false;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
            texture.sRGBTexture = true;
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.maxTextureSize = 1024;
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal)) return;
            var model = (ModelImporter)assetImporter;
            model.importAnimation = false;
            model.importCameras = false;
            model.importLights = false;
            model.addCollider = false;
            model.materialImportMode = ModelImporterMaterialImportMode.None;
        }
    }
}
