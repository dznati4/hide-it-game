using System.Collections.Generic;
using UnityEngine;

namespace HideIT
{
    // Deliberately primitive geometry: tune the mechanic before commissioning art.
    public static class LabGeometry
    {
        static readonly Dictionary<Color, Material> Materials = new Dictionary<Color, Material>();
        public static readonly Color Ink = new Color(.09f, .13f, .16f);
        public static readonly Color Teal = new Color(.17f, .67f, .59f);
        public static readonly Color Orange = new Color(.98f, .54f, .27f);

        public static Material Material(Color color)
        {
            if (Materials.TryGetValue(color, out var cached) && cached != null) return cached;
            // A Resources material keeps the shader in standalone builds even though
            // all room meshes are generated at runtime.
            var template = Resources.Load<Material>("LabBase");
            var material = template != null ? new Material(template) : new Material(Shader.Find("Standard"));
            material.name = "Lab " + ColorUtility.ToHtmlStringRGB(color);
            material.color = color;
            material.SetFloat("_Glossiness", .12f);
            Materials[color] = material;
            return material;
        }

        public static GameObject Box(Transform parent, string name, Vector3 position, Vector3 size, Color color, bool solid = true)
        {
            return Shape(parent, name, PrimitiveType.Cube, position, size, color, solid);
        }

        public static GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 position,
            Vector3 size, Color color, bool solid = true)
        {
            var obj = GameObject.CreatePrimitive(type);
            obj.name = name;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            obj.GetComponent<Renderer>().sharedMaterial = Material(color);
            if (!solid)
            {
                var collider = obj.GetComponent<Collider>();
                collider.enabled = false;
                Object.Destroy(collider);
            }
            return obj;
        }

        public static void Label(Transform parent, string text, Vector3 position, float size, Color color, float yaw = 180f)
        {
            var obj = new GameObject(text);
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localRotation = Quaternion.Euler(0, yaw, 0);
            var label = obj.AddComponent<TextMesh>();
            label.text = text;
            label.fontSize = 64;
            label.characterSize = size / 64f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
        }

        public static void ClearMaterialCache()
        {
            foreach (var material in Materials.Values)
                if (material != null) Object.Destroy(material);
            Materials.Clear();
        }
    }
}
