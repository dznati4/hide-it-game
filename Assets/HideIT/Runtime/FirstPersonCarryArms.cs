using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HideIT
{
    // The view uses the same skinned arms and grip pose as the third-person model.
    internal sealed class FirstPersonCarryArms : MonoBehaviour
    {
        Mesh armMesh;

        internal static SkinnedMeshRenderer Create(SkinnedMeshRenderer source)
        {
            var mesh = source.sharedMesh;
            var names = source.bones;
            var isArmBone = new bool[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                string name = names[i].name;
                isArmBone[i] = name.EndsWith("Arm") || name.EndsWith("Hand");
            }

            var counts = mesh.GetBonesPerVertex();
            var skin = mesh.GetAllBoneWeights();
            var isArmVertex = new bool[mesh.vertexCount];
            int index = 0;
            for (int vertex = 0; vertex < counts.Length; vertex++)
            {
                float armWeight = 0;
                for (int part = 0; part < counts[vertex]; part++)
                {
                    var influence = skin[index++];
                    if (isArmBone[influence.boneIndex]) armWeight += influence.weight;
                }
                isArmVertex[vertex] = armWeight > .45f;
            }

            var triangles = mesh.triangles;
            var arms = new List<int>();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                if (!isArmVertex[triangles[i]] || !isArmVertex[triangles[i+1]] || !isArmVertex[triangles[i+2]]) continue;
                arms.Add(triangles[i]);
                arms.Add(triangles[i+1]);
                arms.Add(triangles[i+2]);
            }

            var view = new GameObject("First-person carry arms");
            view.layer = source.gameObject.layer;
            view.transform.SetParent(source.transform,false);
            var ownedMesh = view.AddComponent<FirstPersonCarryArms>();
            ownedMesh.armMesh = Instantiate(mesh);
            ownedMesh.armMesh.name = "First-person carry mesh";
            ownedMesh.armMesh.SetTriangles(arms,0);
            var renderer = view.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = ownedMesh.armMesh;
            renderer.sharedMaterial = source.sharedMaterial;
            renderer.rootBone = source.rootBone;
            renderer.bones = names;
            renderer.localBounds = source.localBounds;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.enabled = false;
            return renderer;
        }

        void OnDestroy() { if (armMesh != null) Destroy(armMesh); }
    }
}
