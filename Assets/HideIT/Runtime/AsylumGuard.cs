using System.Collections.Generic;
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
        const float SearchTime = 4.5f;
        const float NoiseWait = 5.5f;
        static readonly Color PatrolColor = new Color(1f,.66f,.15f,.31f);
        static readonly Color AlertColor = new Color(1f,.17f,.10f,.50f);
        static readonly Color InvestigatingColor = new Color(.44f,.77f,.98f,.33f);
        readonly Vector3[] fan = new Vector3[1+(Rays+1)*2];
        CarryLabBootstrap mission;
        AsylumNavigation navigation;
        readonly List<Vector3> path = new List<Vector3>();
        ProceduralCharacterPose pose;
        Vector2[] route;
        float speed,range,wait,untilRepath,guardTimer,ignoreBodyUntil;
        int waypoint,pathStep;
        Vector3 lastSeen,bodySite,noiseSite,goal;
        Quaternion noiseFacing;
        bool reachedNoise;
        Mesh sightMesh;
        Material sightMaterial;
        Vector3[] rayDirections;
        public enum Activity { Patrol, InspectBody, InvestigateNoise, Chase, Search }
        public Activity State { get; private set; }
        public float DistractionSeconds => State == Activity.InvestigateNoise ? guardTimer : 0;
        public bool HasSightDisplay => sightMesh != null && sightMesh.vertexCount == fan.Length;

        public void Initialize(CarryLabBootstrap owner,AsylumNavigation paths,Vector2[] points,float metresPerSecond,float sightDistance)
        {
            mission = owner;
            navigation = paths;
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
            ignoreBodyUntil = 0;
            State = Activity.Patrol;
            ResetPath();
            var position = Point(0);
            transform.SetPositionAndRotation(position,Quaternion.LookRotation(Point(1)-position));
            if (pose != null) pose.ResetMotion();
            DrawSight();
        }

        void Update()
        {
            if (!mission.Started || mission.Paused || mission.Caught || mission.Delivered || Time.deltaTime <= 0) return;
            float delta = Time.deltaTime;
            var previous = transform.position;
            untilRepath -= delta;
            var player = mission.Player;
            bool hidden = mission.Interactions.Hidden;
            bool seesPlayer = !hidden && CanSee(player.SightPoint);
            if (seesPlayer)
            {
                lastSeen = player.transform.position;
                ChangeState(Activity.Chase);
            }
            else if (State == Activity.Chase)
            {
                ChangeState(Activity.Search);
                guardTimer = SearchTime;
            }

            switch (State)
            {
                case Activity.Chase:
                    MoveTo(lastSeen,4.18f,delta);
                    if (!hidden && HorizontalDistance(transform.position,player.transform.position) <= .73f &&
                        !Physics.Linecast(transform.position+Vector3.up,player.SightPoint,Obstructions,QueryTriggerInteraction.Ignore))
                        mission.Catch("A guard caught up with you.");
                    break;
                case Activity.Search:
                    guardTimer -= delta;
                    if (guardTimer <= 0) ChangeState(Activity.Patrol);
                    else if (MoveTo(lastSeen,2.28f,delta)) transform.Rotate(0,50f*delta,0,Space.World);
                    break;
                case Activity.InvestigateNoise:
                    if (!reachedNoise && MoveTo(noiseSite,2.32f,delta))
                    {
                        reachedNoise = true;
                        noiseFacing = transform.rotation;
                    }
                    if (reachedNoise)
                    {
                        guardTimer -= delta;
                        float sweep = Mathf.Sin((NoiseWait-guardTimer)*2.1f)*24f;
                        transform.rotation = Quaternion.RotateTowards(transform.rotation,Quaternion.Euler(0,sweep,0)*noiseFacing,82f*delta);
                        if (guardTimer <= 0) ChangeState(Activity.Patrol);
                    }
                    break;
                case Activity.InspectBody:
                    InspectBody(delta);
                    break;
                default:
                    if (Time.time >= ignoreBodyUntil && TrySeeBody(out var point))
                    {
                        bodySite = point;
                        ChangeState(Activity.InspectBody);
                    }
                    else Patrol(delta);
                    break;
            }
            if (pose != null) pose.SetMotion((transform.position-previous)/delta,false,false,false);
            DrawSight();
        }

        void Patrol(float delta)
        {
            if (wait > 0)
            {
                wait -= delta;
                Face(Point(waypoint),delta,102f);
                return;
            }
            if (!MoveTo(Point(waypoint),speed,delta)) return;
            waypoint = (waypoint+1)%route.Length;
            wait = .95f;
        }

        Vector3 Point(int index) => new Vector3(route[index].x,.025f,route[index].y);

        public bool CanSee(Vector3 point)
        {
            var eye = transform.position+Vector3.up*EyeHeight;
            var flat = Vector3.ProjectOnPlane(point-eye,Vector3.up);
            if (flat.sqrMagnitude > range*range || Vector3.Angle(transform.forward,flat) > HalfAngle) return false;
            return !Physics.Linecast(eye,point,Obstructions,QueryTriggerInteraction.Ignore);
        }

        bool TrySeeBody(out Vector3 position)
        {
            position = mission.Body.Position;
            if (mission.Player.Holding || !mission.Body.gameObject.activeInHierarchy || mission.Interactions.Stashing) return false;
            if (CanSee(mission.Body.Parts[1].transform.position)) return true;
            foreach (var bodyPart in mission.Body.Parts)
                if (CanSee(bodyPart.transform.position)) return true;
            return false;
        }

        void InspectBody(float delta)
        {
            if (TrySeeBody(out var point)) bodySite = point;
            if (HorizontalDistance(transform.position,bodySite) <= .95f) Face(bodySite,delta,155f);
            else MoveTo(bodySite,1.75f,delta);
        }

        public void HearNoise(Vector3 where)
        {
            if (!mission.Started || mission.Caught || mission.Delivered || State == Activity.Chase ||
                HorizontalDistance(transform.position,where) > 8.5f) return;
            ChangeState(Activity.InvestigateNoise);
            untilRepath = 0;
            noiseSite = where;
            reachedNoise = false;
            guardTimer = NoiseWait;
            ignoreBodyUntil = Time.time+15f;
        }

        void ChangeState(Activity next)
        {
            if (next == State) return;
            State = next;
            ResetPath();
        }

        void ResetPath()
        {
            untilRepath = 0;
            path.Clear();
            pathStep = 0;
        }

        bool MoveTo(Vector3 destination,float moveSpeed,float delta)
        {
            destination.y = transform.position.y;
            if (HorizontalDistance(transform.position,destination) < .085f) return true;
            if (untilRepath <= 0 && (pathStep >= path.Count || HorizontalDistance(goal,destination) > .25f))
            {
                goal = destination;
                navigation.TryFindPath(transform.position,destination,path);
                pathStep = 0;
                untilRepath = .28f;
            }
            if (pathStep >= path.Count)
                return untilRepath > 0 && HorizontalDistance(goal,destination) < .26f;
            var next = path[pathStep];
            if (HorizontalDistance(transform.position,next) < .075f)
            {
                pathStep++;
                if (pathStep >= path.Count) return true;
                next = path[pathStep];
            }
            if (Face(next,delta,State == Activity.Chase ? 320f : 205f))
                transform.position = Vector3.MoveTowards(transform.position,next,moveSpeed*delta);
            return false;
        }

        bool Face(Vector3 point,float delta,float turnSpeed)
        {
            var direction = Vector3.ProjectOnPlane(point-transform.position,Vector3.up);
            if (direction.sqrMagnitude < .0001f) return false;
            var facing = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.RotateTowards(transform.rotation,facing,turnSpeed*delta);
            return Quaternion.Angle(transform.rotation,facing) < 36f;
        }

        static float HorizontalDistance(Vector3 a,Vector3 b) => Vector3.ProjectOnPlane(a-b,Vector3.up).magnitude;

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
            sightMaterial.color = State == Activity.Chase || State == Activity.Search ? AlertColor :
                State == Activity.Patrol ? PatrolColor : InvestigatingColor;
        }

        void OnDestroy()
        {
            if (sightMaterial != null) Destroy(sightMaterial);
            if (sightMesh != null) Destroy(sightMesh);
        }
    }
}
