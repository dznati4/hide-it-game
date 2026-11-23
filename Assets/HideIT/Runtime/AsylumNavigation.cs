using System.Collections.Generic;
using UnityEngine;

namespace HideIT
{
    // A room-scale grid generated from the same solid colliders the player uses.
    public sealed class AsylumNavigation
    {
        const int Width = 47, Depth = 55, World = 1 << 0;
        const float Cell = .5f, Radius = .26f;
        static readonly Vector3 Origin = new Vector3(-11.5f,.025f,-9.5f);
        static readonly Vector2Int[] Offsets = { Vector2Int.right, Vector2Int.up, Vector2Int.left, Vector2Int.down };
        readonly bool[] open = new bool[Width*Depth];
        readonly int[] costs = new int[Width*Depth];
        readonly int[] previous = new int[Width*Depth];
        readonly bool[] visited = new bool[Width*Depth];
        readonly List<int> frontier = new List<int>();
        readonly List<Vector3> reversePath = new List<Vector3>();

        public AsylumNavigation()
        {
            for (int i = 0; i < open.Length; i++)
            {
                var p = Position(i);
                open[i] = !Physics.CheckCapsule(p+Vector3.up*.33f,p+Vector3.up*1.37f,Radius,World,QueryTriggerInteraction.Ignore);
            }
        }

        public bool TryFindPath(Vector3 start,Vector3 target,List<Vector3> result)
        {
            result.Clear();
            start.y = target.y = Origin.y;
            if (Clear(start,target)) { result.Add(target); return true; }
            int from = Nearest(start), to = Nearest(target);
            if (from < 0 || to < 0) return false;
            for (int i = 0; i < costs.Length; i++) { costs[i] = int.MaxValue; previous[i] = -1; visited[i] = false; }
            frontier.Clear();
            frontier.Add(from);
            costs[from] = 0;
            while (frontier.Count > 0)
            {
                int best = 0;
                for (int i = 1; i < frontier.Count; i++)
                    if (Score(frontier[i],to) < Score(frontier[best],to)) best = i;
                int index = frontier[best];
                frontier[best] = frontier[frontier.Count-1];
                frontier.RemoveAt(frontier.Count-1);
                if (index == to) break;
                visited[index] = true;
                foreach (var offset in Offsets)
                {
                    int x = index%Width+offset.x, z = index/Width+offset.y;
                    if (x < 0 || x >= Width || z < 0 || z >= Depth) continue;
                    int neighbour = x+z*Width;
                    if (!open[neighbour] || visited[neighbour] || costs[index]+1 >= costs[neighbour] ||
                        !Clear(Position(index),Position(neighbour))) continue;
                    if (costs[neighbour] == int.MaxValue) frontier.Add(neighbour);
                    costs[neighbour] = costs[index]+1;
                    previous[neighbour] = index;
                }
            }
            if (costs[to] == int.MaxValue) return false;
            reversePath.Clear();
            reversePath.Add(target);
            for (int index = to; index != -1; index = previous[index]) reversePath.Add(Position(index));
            var last = start;
            for (int i = reversePath.Count-1; i >= 0;)
            {
                int next = i;
                while (next > 0 && Clear(last,reversePath[next-1])) next--;
                if (!Clear(last,reversePath[next])) break;
                last = reversePath[next];
                result.Add(last);
                i = next-1;
            }
            return result.Count > 0;
        }

        public static bool Clear(Vector3 start,Vector3 end)
        {
            var horizontal = Vector3.ProjectOnPlane(end-start,Vector3.up);
            return horizontal.sqrMagnitude < .0001f ||
                !Physics.CapsuleCast(start+Vector3.up*.33f,start+Vector3.up*1.37f,Radius,horizontal.normalized,
                    horizontal.magnitude,World,QueryTriggerInteraction.Ignore);
        }

        int Nearest(Vector3 p)
        {
            int x = Mathf.RoundToInt((p.x-Origin.x)/Cell);
            int z = Mathf.RoundToInt((p.z-Origin.z)/Cell);
            int best = -1;
            float distance = float.PositiveInfinity;
            for (int rz = Mathf.Max(0,z-4); rz <= Mathf.Min(Depth-1,z+4); rz++)
                for (int rx = Mathf.Max(0,x-4); rx <= Mathf.Min(Width-1,x+4); rx++)
                {
                    int id = rx+rz*Width;
                    float squared = (Position(id)-p).sqrMagnitude;
                    if (!open[id] || squared >= distance) continue;
                    best = id;
                    distance = squared;
                }
            return best;
        }

        static Vector3 Position(int index) => Origin+new Vector3((index%Width)*Cell,0,(index/Width)*Cell);
        int Score(int index,int goal) => costs[index]+Mathf.Abs(index%Width-goal%Width)+Mathf.Abs(index/Width-goal/Width);
    }
}
