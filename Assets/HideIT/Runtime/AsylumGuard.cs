using UnityEngine;
using UnityEngine.Rendering;

namespace HideIT
{
    public sealed class AsylumGuard : MonoBehaviour
    {
        const int Obstructions = 1 << 0;
        const int Rays = 42;
        const float HalfAngle = 36f;
        const float EyeHeight = 1.52f;
        const float CatchTime = .55f;
        static readonly Color PatrolColor = new Color(1f,.66f,.15f,.31f);
        static readonly Color AlertColor = new Color(1f,.17f,.10f,.50f);
        readonly Vector3[] fan = new Vector3[1+(Rays+1)*2];
        CarryLabBootstrap mission;
        ProceduralCharacterPose pose;
        Vector2[] route;
        float speed,range,wait,exposure;
        int waypoint;
        Mesh sightMesh;
        Material sightMaterial;
        Vector3[] rayDirections;
        public float Exposure => exposure / CatchTime;
        public bool HasSightDisplay => sightMesh != null && sightMesh.vertexCount == fan.Length;

        public void Initialize(CarryLabBootstrap owner,Vector2[] points,float metresPerSecond,float sightDistance)
        {
            mission = owner;
            route = points;
            speed = metresPerSecond;
            range = sightDistance;
            var model = CharacterArtCatalog.Load("Security_Navy");
            if (model != null)
            {
                var actor = Instantiate(model,transform,false);
                pose = actor.AddComponent<ProceduralCharacterPose>();
                pose.Initialize();
            }
            else LabGeometry.Box(transform,"Guard",Vector3.up*.9f,new Vector3(.46f,1.8f,.37f),LabGeometry.Ink,false);
            BuildSightDisplay();
            Rewind();
        }

        public void Rewind()
        {
            waypoint = 1;
            wait = .55f;
            exposure = 0;
            var position = Point(0);
            transform.SetPositionAndRotation(position,Quaternion.LookRotation(Point(1)-position));
            if (pose != null) pose.ResetMotion();
            DrawSight();
        }

        void Update()
        {
            if (!mission.Started || mission.Paused || mission.Caught || mission.Delivered || Time.deltaTime <= 0) return;
            float delta = Time.deltaTime;
            Patrol(delta);
            bool visible = SeesCarry();
            exposure = Mathf.MoveTowards(exposure,visible ? CatchTime : 0,delta*(visible ? 1f : 2f));
            DrawSight();
            if (exposure >= CatchTime) mission.Catch();
        }

        void Patrol(float delta)
        {
            var previous = transform.position;
            var target = Point(waypoint);
            var to = target-previous;
            if (to.sqrMagnitude < .001f)
            {
                waypoint = (waypoint+1)%route.Length;
                target = Point(waypoint);
                to = target-previous;
                wait = 1.35f;
            }
            var facing = Quaternion.LookRotation(to);
            transform.rotation = Quaternion.RotateTowards(transform.rotation,facing,116f*delta);
            wait -= delta;
            if (wait <= 0 && Quaternion.Angle(transform.rotation,facing) < 19f)
                transform.position = Vector3.MoveTowards(previous,target,speed*delta);
            if (pose != null) pose.SetMotion((transform.position-previous)/delta,false,false,false);
        }

        Vector3 Point(int index) => new Vector3(route[index].x,.025f,route[index].y);

        public bool CanSee(Vector3 point)
        {
            var eye = transform.position+Vector3.up*EyeHeight;
            var flat = Vector3.ProjectOnPlane(point-eye,Vector3.up);
            if (flat.sqrMagnitude > range*range || Vector3.Angle(transform.forward,flat) > HalfAngle) return false;
            return !Physics.Linecast(eye,point,Obstructions,QueryTriggerInteraction.Ignore);
        }

        bool SeesCarry()
        {
            var player = mission.Player;
            if (!player.TryGetHeldGrip(out var grip)) return false;
            var torso = player.transform.position+Vector3.up*(player.Crouched ? .86f : 1.23f);
            if (!CanSee(torso)) return false;
            if (CanSee(grip.WorldPoint)) return true;
            foreach (var bodyPart in mission.Body.Parts)
                if (CanSee(bodyPart.transform.position)) return true;
            return false;
        }

        void BuildSightDisplay()
        {
            var shader = Resources.Load<Shader>("GuardSight");
            if (shader == null) return;
            sightMaterial = new Material(shader);
            var display = new GameObject("Guard field of view");
            display.transform.SetParent(transform,false);
            var renderer = display.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = sightMaterial;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            sightMesh = new Mesh { name = "Sight cone with wall clipping" };
            sightMesh.MarkDynamic();
            rayDirections = new Vector3[Rays+1];
            var colors = new Color32[fan.Length];
            var indices = new int[Rays*9];
            colors[0] = new Color32(255,255,255,220);
            for (int n = 0; n <= Rays; n++)
            {
                rayDirections[n] = Quaternion.Euler(0,Mathf.Lerp(-HalfAngle,HalfAngle,n/(float)Rays),0)*Vector3.forward;
                int inner = 1+n*2;
                colors[inner] = new Color32(255,255,255,(byte)(n==0 || n==Rays ? 234 : 137));
                colors[inner+1] = new Color32(255,255,255,246);
                if (n == Rays) break;
                int f = n*9;
                indices[f] = 0; indices[f+1] = inner; indices[f+2] = inner+2;
                indices[f+3] = inner; indices[f+4] = inner+1; indices[f+5] = inner+3;
                indices[f+6] = inner; indices[f+7] = inner+3; indices[f+8] = inner+2;
            }
            sightMesh.vertices = fan;
            sightMesh.colors32 = colors;
            sightMesh.triangles = indices;
            sightMesh.bounds = new Bounds(Vector3.zero,new Vector3(range*2,.3f,range*2));
            display.AddComponent<MeshFilter>().sharedMesh = sightMesh;
        }

        void DrawSight()
        {
            if (sightMesh == null) return;
            var eye = transform.position+Vector3.up*EyeHeight;
            var height = Vector3.up*.016f;
            fan[0] = height;
            for (int n = 0; n <= Rays; n++)
            {
                var local = rayDirections[n];
                float distance = range;
                if (Physics.Raycast(eye,transform.TransformDirection(local),out var hit,range,Obstructions,QueryTriggerInteraction.Ignore))
                    distance = Mathf.Max(.02f,hit.distance-.035f);
                fan[1+n*2] = local*Mathf.Max(0,distance-.11f)+height;
                fan[2+n*2] = local*distance+height;
            }
            sightMesh.SetVertices(fan,0,fan.Length,MeshUpdateFlags.DontRecalculateBounds);
            sightMaterial.color = Color.Lerp(PatrolColor,AlertColor,Exposure);
        }

        void OnDestroy()
        {
            if (sightMaterial != null) Destroy(sightMaterial);
            if (sightMesh != null) Destroy(sightMesh);
        }
    }
}
