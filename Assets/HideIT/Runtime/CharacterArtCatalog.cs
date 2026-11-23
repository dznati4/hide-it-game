using System.Collections.Generic;
using UnityEngine;

namespace HideIT
{
    public static class CharacterArtCatalog
    {
        public struct Outfit
        {
            public string Id, Name, Description;
            public Outfit(string id, string name, string description)
            { Id = id; Name = name; Description = description; }
        }
        public static readonly Outfit[] Players = {
            new Outfit("Player_Alex_Orange", "Alex", "Masked overalls"),
            new Outfit("Player_Noam_Olive", "Noam", "Pale mask and sweatshirt"),
            new Outfit("Player_Maya_Denim", "Maya", "Black mask and coveralls"),
            new Outfit("Player_Eli_Cream", "Eli", "Clown suit"),
            new Outfit("Player_Roni_Plum", "Roni", "Bear costume")
        };
        const string SelectionKey = "HideIT.CharacterIndex";
        public static int SavedSelection => Mathf.Clamp(PlayerPrefs.GetInt(SelectionKey, 0), 0, Players.Length - 1);
        public static GameObject Load(string id) => Resources.Load<GameObject>("ArtPrototype/" + id);
        public static void SaveSelection(int index)
        {
            PlayerPrefs.SetInt(SelectionKey, Mathf.Clamp(index, 0, Players.Length - 1));
            PlayerPrefs.Save();
        }
        public static void SetLayer(Transform root, int layer)
        {
            foreach (var child in root.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = layer;
        }
    }

    public sealed class CharacterSelectionPreview : MonoBehaviour
    {
        public const int PreviewLayer = 11;
        GameObject model;
        Camera previewCamera;
        RenderTexture texture;
        public Texture Texture => texture;
        public void Select(int index)
        {
            var prefab = CharacterArtCatalog.Load(CharacterArtCatalog.Players[index].Id);
            if (model != null)
            {
                model.SetActive(false);
                Destroy(model);
            }
            if (prefab == null) return;
            model = Instantiate(prefab, transform);
            model.transform.localPosition = new Vector3(1000, 0, 0);
            model.transform.localRotation = Quaternion.Euler(0, -12, 0);
            CharacterArtCatalog.SetLayer(model.transform, PreviewLayer);
            var pose = model.AddComponent<ProceduralCharacterPose>();
            pose.Initialize();
            if (previewCamera != null) return;
            var cameraObject = new GameObject("Outfit preview camera");
            cameraObject.transform.SetParent(transform);
            previewCamera = cameraObject.AddComponent<Camera>();
            previewCamera.transform.position = new Vector3(1002.1f, 1.4f, 3.7f);
            previewCamera.transform.LookAt(new Vector3(1000, .94f, 0));
            previewCamera.fieldOfView = 31;
            previewCamera.nearClipPlane = .1f;
            previewCamera.farClipPlane = 12;
            previewCamera.cullingMask = 1 << PreviewLayer;
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.07f, .11f, .14f, 0);
            texture = new RenderTexture(160, 240, 24, RenderTextureFormat.ARGB32) { filterMode = FilterMode.Point };
            texture.Create();
            previewCamera.targetTexture = texture;
            var previewLight = cameraObject.AddComponent<Light>();
            previewLight.type = LightType.Directional;
            previewLight.cullingMask = 1 << PreviewLayer;
            previewLight.intensity = .65f;
            previewLight.color = new Color(.98f, .96f, .91f);
            previewLight.shadows = LightShadows.None;
        }
        public void SetVisible(bool visible)
        {
            if (previewCamera != null) previewCamera.enabled = visible;
            if (model != null) model.SetActive(visible);
        }
        void OnDestroy()
        {
            if (texture != null) { texture.Release(); Destroy(texture); }
        }
    }
}
