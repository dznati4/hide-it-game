using System.Collections.Generic;
using UnityEngine;

namespace HideIT
{
    public sealed class AmbientCivilians : MonoBehaviour
    {
        public static readonly string[] ModelIds = {
            "Civilian_Amir_RustJacket", "Civilian_Ben_CreamShirt", "Civilian_Lior_OliveHoodie",
            "Civilian_Tamar_RoseCardigan", "Civilian_Nora_NavyCoat", "Civilian_Ruth_TealCoat"
        };

        sealed class Walker
        {
            public GameObject Model;
            public ProceduralCharacterPose Pose;
            public Vector3 Entrance, Exit;
            public float Distance, Speed, Length;
        }

        // Separate lanes leave the doorway, stairs, and delivery square free.
        static readonly float[] LaneDepths = { 1.7f, 9.7f };
        const float EndX = 7.5f;
        readonly Walker[] lanes = new Walker[LaneDepths.Length];
        readonly List<string> bag = new List<string>(ModelIds.Length);
        string previousId;
        float untilEntrance = 5;
        int nextLane;
        bool intakeRoutes;
        public int CompletedPasses { get; private set; }
        public int ActiveCount { get; private set; }
        public Vector3 LastWalkerPosition { get; private set; }

        void Update()
        {
            if (Time.deltaTime <= 0) return;
            for (int i = 0; i < lanes.Length; i++)
            {
                var walker = lanes[i];
                if (walker == null) continue;
                walker.Distance += walker.Speed * Time.deltaTime;
                if (walker.Distance >= walker.Length)
                {
                    Destroy(walker.Model);
                    lanes[i] = null;
                    ActiveCount--;
                    CompletedPasses++;
                    continue;
                }
                walker.Model.transform.position = Vector3.Lerp(walker.Entrance, walker.Exit, walker.Distance / walker.Length);
                LastWalkerPosition = walker.Model.transform.position;
                walker.Pose.SetMotion(walker.Model.transform.forward * walker.Speed, false, false, false);
            }

            untilEntrance -= Time.deltaTime;
            if (untilEntrance > 0) return;
            if (Spawn(nextLane)) nextLane = (nextLane + 1) % lanes.Length;
            untilEntrance = Random.Range(12f, 19f);
        }

        public void UseIntakeRoutes() { intakeRoutes = true; }

        public void ResetRoutes()
        {
            foreach (var walker in lanes)
                if (walker != null) Destroy(walker.Model);
            System.Array.Clear(lanes,0,lanes.Length);
            ActiveCount = CompletedPasses = nextLane = 0;
            LastWalkerPosition = Vector3.zero;
            untilEntrance = 5;
        }

        bool Spawn(int lane)
        {
            if (lanes[lane] != null) return false;
            if (bag.Count == 0) bag.AddRange(ModelIds);
            int choice = Random.Range(0, bag.Count);
            if (bag.Count > 1 && bag[choice] == previousId) choice = (choice + 1) % bag.Count;
            var id = bag[choice];
            bag.RemoveAt(choice);
            var prefab = CharacterArtCatalog.Load(id);
            if (prefab == null) return false;
            previousId = id;
            float direction = lane == 0 ? 1 : -1;
            float endX = intakeRoutes ? 6f : EndX;
            float depth = intakeRoutes ? (lane == 0 ? -8.83f : -7.03f) : LaneDepths[lane];
            var start = new Vector3(-endX * direction, .01f, depth);
            var end = new Vector3(endX * direction, .01f, depth);
            var model = Instantiate(prefab, start, Quaternion.LookRotation(end - start), transform);
            var pose = model.AddComponent<ProceduralCharacterPose>();
            pose.Initialize();
            lanes[lane] = new Walker { Model = model, Pose = pose, Entrance = start, Exit = end,
                Length = Vector3.Distance(start,end), Speed = Random.Range(1.45f, 1.8f) };
            ActiveCount++;
            LastWalkerPosition = start;
            return true;
        }
    }
}
