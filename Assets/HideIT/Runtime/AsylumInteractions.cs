using System.Collections.Generic;
using UnityEngine;

namespace HideIT
{
    public sealed class AsylumInteractions : MonoBehaviour
    {
        public const int MaxRocks = 4;
        const float Reach = 1.32f;
        const int World = 1 << 0;
        Vector3 dumpsterCenter;
        Vector3 DumpsterFront => dumpsterCenter-Vector3.forward*1.13f;
        static readonly Color Steel = new Color(.13f,.28f,.23f);
        static readonly Color Rim = new Color(.38f,.54f,.43f);
        static readonly Color Stone = new Color(.55f,.52f,.39f);
        readonly List<HideSpot> hidingSpots = new List<HideSpot>();
        readonly List<Pickup> pickups = new List<Pickup>();
        readonly List<DistractionRock> thrown = new List<DistractionRock>();
        CarryLabBootstrap mission;
        Transform lid;
        AudioClip click;
        HideSpot hidden;
        float stashTimer;
        bool stashing;
        public int Rocks { get; private set; }
        public bool Hidden => hidden != null;
        public bool Stashing => stashing;

        sealed class HideSpot
        {
            public string Name;
            public Vector3 Entrance, ViewPoint;
            public float PeekYaw;
        }
        sealed class Pickup
        {
            public GameObject Model;
            public Vector3 Position;
        }

        public void Build(CarryLabBootstrap owner,Vector3 center,Material dumpsterPaint = null)
        {
            mission = owner;
            dumpsterCenter = center;
            BuildDumpster(dumpsterPaint);
            click = MakeImpactSound();
        }

        void BuildDumpster(Material paint)
        {
            var root = new GameObject("Service dumpster").transform;
            root.SetParent(transform,false);
            root.position = dumpsterCenter;
            LabGeometry.Box(root,"Dumpster floor",new Vector3(0,.25f,0),new Vector3(2.62f,.17f,1.82f),Steel);
            for (int side = -1; side <= 1; side += 2)
            {
                LabGeometry.Box(root,"Side wall",new Vector3(side*1.27f,.78f,0),new Vector3(.13f,1.03f,1.83f),Steel);
                LabGeometry.Box(root,"End wall",new Vector3(0,.78f,side*.86f),new Vector3(2.58f,1.03f,.14f),Steel);
                LabGeometry.Box(root,"Top rim",new Vector3(0,1.29f,side*.86f),new Vector3(2.70f,.12f,.22f),Rim,false);
                LabGeometry.Box(root,"Lifting rail",new Vector3(side*1.39f,.91f,0),new Vector3(.18f,.11f,1.35f),Rim,false);
                for (int end = -1; end <= 1; end += 2)
                    LabGeometry.Box(root,"Caster",new Vector3(side*1.07f,.12f,end*.67f),new Vector3(.20f,.23f,.21f),LabGeometry.Ink,false);
            }
            lid = new GameObject("Hinged lid").transform;
            lid.SetParent(root,false);
            lid.localPosition = new Vector3(0,1.35f,.88f);
            LabGeometry.Box(lid,"Lid panel",new Vector3(0,.035f,-.89f),new Vector3(2.70f,.11f,1.84f),LabGeometry.Ink,false);
            for (int i = -3; i <= 3; i++)
                LabGeometry.Box(lid,"Lid rib",new Vector3(i*.32f,.10f,-.88f),new Vector3(.044f,.035f,1.55f),Rim,false);
            LabGeometry.Box(root,"Deposit marker",new Vector3(0,.018f,-1.13f),new Vector3(2.62f,.026f,.09f),LabGeometry.Teal,false);
            if (paint != null)
                foreach (var renderer in root.GetComponentsInChildren<MeshRenderer>())
                    if (renderer.sharedMaterial == LabGeometry.Material(Steel)) renderer.sharedMaterial = paint;
        }

        public void AddCloset(float x,float z,float yaw)
        {
            var cabinet = new GameObject("Hiding closet").transform;
            cabinet.SetParent(transform,false);
            cabinet.SetPositionAndRotation(new Vector3(x,0,z),Quaternion.Euler(0,yaw,0));
            var painted = new Color(.24f,.32f,.29f);
            LabGeometry.Box(cabinet,"Closet cabinet",Vector3.up*1.06f,new Vector3(.94f,2.12f,.57f),painted);
            for (int side = -1; side <= 1; side += 2)
            {
                LabGeometry.Box(cabinet,"Closet door",new Vector3(side*.231f,1.06f,-.294f),new Vector3(.443f,2.0f,.035f),painted,false);
                LabGeometry.Box(cabinet,"Handle",new Vector3(side*.086f,1.03f,-.327f),new Vector3(.03f,.18f,.06f),Rim,false);
            }
            for (int n = 0; n < 5; n++)
                LabGeometry.Box(cabinet,"Vent",new Vector3(0,1.70f+n*.047f,-.316f),new Vector3(.57f,.013f,.015f),LabGeometry.Ink,false);
            AddHidingSpot("Hide in closet",cabinet.TransformPoint(0,0,-.74f),
                cabinet.TransformPoint(0,1.23f,-.34f),yaw+180f);
        }

        public void AddBedHide(Vector3 bed,Vector3 entrance) =>
            AddHidingSpot("Hide under bed",entrance,Vector3.Lerp(bed,entrance,.40f)+Vector3.up*.23f,
                Mathf.Atan2(entrance.x-bed.x,entrance.z-bed.z)*Mathf.Rad2Deg);

        public void AddHidingSpot(string name,Vector3 entrance,Vector3 viewPoint,float yaw) =>
            hidingSpots.Add(new HideSpot { Name = name,Entrance = entrance,ViewPoint = viewPoint,PeekYaw = yaw });

        public void AddPickup(Vector3 position)
        {
            var model = LabGeometry.Shape(transform,"Pick up rocks",PrimitiveType.Sphere,position,new Vector3(.19f,.13f,.17f),Stone,false);
            pickups.Add(new Pickup { Model = model,Position = position });
        }

        public string Prompt
        {
            get
            {
                if (Hidden) return "[ E ] Leave hiding";
                if (stashing) return "Stashing body";
                if (mission.Player.Holding && CanStash()) return "[ E ] Stash body";
                if (mission.Player.Holding) return null;
                var spot = FindHideSpot();
                if (spot != null) return "[ E ] " + spot.Name;
                return FindPickup() != null && Rocks < MaxRocks ? "[ E ] Pick up rocks" : null;
            }
        }

        public bool Interact()
        {
            if (mission.Caught || mission.Delivered || mission.Paused || stashing) return true;
            if (Hidden)
            {
                ExitHiding();
                return true;
            }
            if (mission.Player.Holding)
            {
                if (!CanStash()) return false;
                StartStash();
                return true;
            }
            var spot = FindHideSpot();
            if (spot != null)
            {
                foreach (var guard in mission.Guards)
                    if (guard.CanSee(mission.Player.SightPoint))
                    {
                        mission.Catch("A guard watched you hide.");
                        return true;
                    }
                hidden = spot;
                mission.Player.SetViewAngles(spot.PeekYaw,0);
                mission.Player.SetHiddenCamera(spot.ViewPoint);
                return true;
            }
            var pickup = FindPickup();
            if (pickup == null || Rocks >= MaxRocks) return false;
            pickup.Model.SetActive(false);
            Rocks = Mathf.Min(MaxRocks,Rocks+2);
            return true;
        }

        bool CanStash() => Vector3.Distance(AtFloor(mission.Player.transform.position),DumpsterFront) <= 1.57f &&
            Vector3.Distance(AtFloor(mission.Body.Position),DumpsterFront) <= 2.75f && VisibleInteraction(DumpsterFront);

        bool VisibleInteraction(Vector3 position)
        {
            var local = AtFloor(mission.Player.transform.position);
            return !Physics.Linecast(local+Vector3.up*.71f,AtFloor(position)+Vector3.up*.65f,World,QueryTriggerInteraction.Ignore);
        }

        HideSpot FindHideSpot()
        {
            HideSpot best = null;
            float distance = Reach*Reach;
            foreach (var spot in hidingSpots)
            {
                float squared = (AtFloor(mission.Player.transform.position)-spot.Entrance).sqrMagnitude;
                if (squared >= distance || !VisibleInteraction(spot.Entrance)) continue;
                distance = squared;
                best = spot;
            }
            return best;
        }

        Pickup FindPickup()
        {
            foreach (var pickup in pickups)
                if (pickup.Model.activeSelf && (AtFloor(mission.Player.transform.position)-AtFloor(pickup.Position)).sqrMagnitude < 1.45f &&
                    VisibleInteraction(pickup.Position)) return pickup;
            return null;
        }

        static Vector3 AtFloor(Vector3 p) => new Vector3(p.x,0,p.z);

        public bool ThrowRock(Vector3 direction)
        {
            if (Rocks <= 0 || Hidden || stashing || mission.Caught || mission.Paused || mission.Delivered) return false;
            var origin = mission.Player.SightPoint;
            var trajectory = direction.normalized;
            var start = origin+trajectory*.32f;
            if (Physics.Linecast(origin,start,World,QueryTriggerInteraction.Ignore)) start = origin;
            var rock = LabGeometry.Shape(transform,"Thrown rock",PrimitiveType.Sphere,start,Vector3.one*.14f,Stone);
            rock.layer = 12;
            var motion = rock.AddComponent<Rigidbody>();
            motion.mass = .16f;
            motion.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            // Start clear of the throwing player and the body still attached to the player.
            var collider = rock.GetComponent<Collider>();
            Physics.IgnoreCollision(collider,mission.Player.GetComponent<CharacterController>());
            foreach (var part in mission.Body.Colliders) Physics.IgnoreCollision(collider,part);
            motion.linearVelocity = trajectory*10f+Vector3.up*2.7f;
            var projectile = rock.AddComponent<DistractionRock>();
            projectile.Owner = this;
            thrown.RemoveAll(item => item == null);
            thrown.Add(projectile);
            Rocks--;
            Destroy(rock,5f);
            return true;
        }

        internal void NoiseAt(Vector3 where)
        {
            AudioSource.PlayClipAtPoint(click,where,.52f);
            foreach (var guard in mission.Guards) guard.HearNoise(where);
        }

        void StartStash()
        {
            mission.Player.Release();
            stashing = true;
            stashTimer = .62f;
            lid.localRotation = Quaternion.Euler(77,0,0);
            mission.Body.ResetAt(dumpsterCenter+Vector3.up*1.46f);
        }

        void Update()
        {
            if (!stashing || mission.Paused || mission.Caught) return;
            stashTimer -= Time.deltaTime;
            lid.localRotation = Quaternion.Euler(Mathf.Lerp(0,77,Mathf.InverseLerp(0,.32f,stashTimer)),0,0);
            if (stashTimer > 0) return;
            stashing = false;
            mission.Body.gameObject.SetActive(false);
            mission.CompleteStash();
        }

        public void ResetRun()
        {
            ExitHiding();
            Rocks = 3;
            stashing = false;
            lid.localRotation = Quaternion.identity;
            foreach (var item in pickups) item.Model.SetActive(true);
            foreach (var projectile in thrown)
                if (projectile != null) { projectile.enabled = false; Destroy(projectile.gameObject); }
            thrown.Clear();
            mission.Body.gameObject.SetActive(true);
        }

        void ExitHiding()
        {
            if (hidden == null) return;
            hidden = null;
            mission.Player.SetHiddenCamera(null);
        }

        static AudioClip MakeImpactSound()
        {
            const int sampleRate = 22050;
            var samples = new float[2205];
            var random = new System.Random(19);
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i/(float)sampleRate;
                samples[i] = (float)(random.NextDouble()*2-1)*Mathf.Exp(-time*83)*.45f+
                    Mathf.Sin(time*1530)*Mathf.Exp(-time*40)*.18f;
            }
            var audio = AudioClip.Create("Rock impact",samples.Length,1,sampleRate,false);
            audio.SetData(samples,0);
            return audio;
        }

        void OnDestroy() { if (click != null) Destroy(click); }
    }

    sealed class DistractionRock : MonoBehaviour
    {
        internal AsylumInteractions Owner;
        void OnCollisionEnter(Collision other)
        {
            if (!enabled || other.gameObject.layer != 0) return;
            enabled = false;
            Owner.NoiseAt(transform.position);
        }
    }
}
