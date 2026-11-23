using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HideIT
{
    public sealed class AsylumWard : MonoBehaviour
    {
        public static readonly Vector3 PlayerSpawn = new Vector3(-1.65f,.045f,-8.15f);
        public static readonly Vector3 BodySpawn = new Vector3(.35f,.64f,-6.55f);
        public static readonly Bounds DeliveryBounds = new Bounds(new Vector3(0,1.45f,16.2f),new Vector3(5.7f,3.3f,3.15f));
        const float WallHeight = 3.25f;
        const float WallThickness = .17f;
        static readonly Color Metal = new Color(.115f,.15f,.14f);
        static readonly Color SignGreen = new Color(.54f,.88f,.73f);
        static readonly Color SignIvory = new Color(.82f,.84f,.71f);
        readonly Dictionary<string,GameObject> pieces = new Dictionary<string,GameObject>();
        Material kitMaterial;
        Material signMaterial;
        Font signFont;

        public void Build()
        {
            var shader = Resources.Load<Shader>("AsylumKit/Shaders/PSX_Lit");
            var atlas = Resources.Load<Texture2D>("AsylumKit/Textures/PSX_Asylum_Kit_atlas");
            if (shader != null && atlas != null)
            {
                kitMaterial = new Material(shader) { name = "Asylum kit / shared atlas", mainTexture = atlas };
                kitMaterial.SetVector("_SnapResolution",new Vector4(640,360,0,0));
                kitMaterial.SetFloat("_Affine",.44f);
                kitMaterial.SetFloat("_Glow",.16f);
            }
            var signShader = Resources.Load<Shader>("AsylumSignText");
            if (signShader != null)
            {
                signFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                signMaterial = new Material(signShader) { mainTexture = signFont.material.mainTexture };
                Font.textureRebuilt += UpdateSignAtlas;
            }
            ColliderBox("Floor collision",new Vector3(0,-.13f,4),new Vector3(24,.26f,28));
            ColliderBox("Ceiling collision",new Vector3(0,WallHeight+.12f,4),new Vector3(24,.22f,28));
            for (int z = -9; z < 18; z += 2)
                for (int x = -11; x < 12; x += 2)
                {
                    Module("modular_floor_000",new Vector3(x,-.003f,z),0,new Vector3(2,.1f,2),true,false);
                    Module((x + z) % 8 == 0 ? "modular_ceiling_001" : "modular_ceiling_000",
                        new Vector3(x,WallHeight,z),0,new Vector3(2,.13f,2),false,false);
                }

            Wall(false,-10,-12,12); Wall(false,18,-12,12);
            Wall(true,-12,-10,18); Wall(true,12,-10,18);
            Partition(false,-4,-12,12,-9.5f,-6.2f,-1.6f,1.6f,6.4f,9.7f);
            Partition(true,-3.1f,-4,14,-.9f,2.1f,9f,12f);
            Partition(true,3.1f,-4,14,2.5f,5.5f,9.8f,12.8f);
            Partition(false,4,-12,-3.1f,-10.7f,-7.3f);
            Partition(false,7.5f,-12,-3.1f,-8.9f,-5.6f);
            Partition(false,6.9f,3.1f,12,8.8f,11.55f);
            Partition(false,14,-12,12,-9.5f,-6.2f,-1.6f,1.6f,8.8f,11.55f);
            Wall(true,10.30f,7.58f,13.72f);

            // Tall screens sit outside the walking lines and give a break in the sight cones.
            Screen(-1.80f,1.5f,1.5f,0); Screen(1.78f,8.1f,1.55f,0);
            Screen(-6.15f,5.65f,2.35f,90); Screen(6.2f,1.1f,2.40f,90);
            Screen(-5.53f,10.7f,2.2f,90); Screen(5.48f,11.35f,2.3f,90);

            Bed(-9.75f,-1.75f,90); Bed(-9.75f,.75f,90); Bed(-9.9f,5.5f,90);
            Bed(10.72f,.40f,0); Bed(10.70f,3.65f,0); Bed(10.8f,-1.8f,0);
            Bed(-10.7f,12.4f,0); Bed(4.35f,10.5f,0);
            Prop("bedside_table_000", -2.57f,-6.07f,0,true);
            Prop("desk_lamp_000",-2.57f,-6.07f,10,false,.74f);
            Prop("wooden_chair_000",2.82f,-5.22f,195,true);
            Prop("medical_crate_001",-3.2f,-5.37f,0,true);
            Prop("file_boxes_000",6.12f,-8.15f,0,true);
            Prop("trash_bin_000",5.18f,-8.4f,0,true);
            Prop("linen_pile_000",-10.8f,2.75f,20,false);
            Prop("medical_crate_000",-5.43f,3.55f,0,true);
            Prop("wooden_chair_000",-4.25f,8.8f,205,true);
            Prop("iv_stand_000",10.55f,5.05f,0,false);
            Prop("trash_bin_000",4.02f,7.36f,0,true);
            Prop("bucket_000",4.12f,10.18f,15,false);
            Prop("medical_crate_000",4.95f,15.46f,0,true);
            Prop("medical_crate_001",5.73f,15.54f,0,true);
            Prop("file_boxes_000",-4.62f,16.3f,10,true);
            Prop("wall_clock_000",-.05f,-9.89f,0,false,2.15f);

            Sign("INTAKE",0,-9.81f,0,SignIvory);
            Sign("WEST WARD",-7.85f,-4.15f,180,SignIvory);
            Sign("CENTRAL HALL",0,-4.15f,180,SignIvory);
            Sign("TREATMENT",8.08f,-4.15f,180,SignIvory);
            Sign("SERVICE EXIT",0,13.85f,180,SignGreen);
            Sign("DROP ZONE",0,17.82f,180,SignGreen);
            Sign("SERVICE EXIT",-7.85f,13.85f,180,SignGreen);
            Sign("SERVICE EXIT",10.17f,13.85f,180,SignGreen);
            Sign("SERVICE PASSAGE",10.17f,6.74f,180,SignGreen);
            ExitMarking();

            Lighting();
            Physics.SyncTransforms();
        }

        public AsylumGuard[] CreatePatrols(CarryLabBootstrap mission) => new[] {
            Patrol(mission,"Hall patrol",1.30f,7.3f,new Vector2(.35f,9.9f),new Vector2(.35f,12.6f),
                new Vector2(-.70f,12.6f),new Vector2(-.70f,3.05f),new Vector2(.35f,3.05f)),
            Patrol(mission,"West ward patrol",1.18f,6.7f,new Vector2(-9.65f,10.0f),new Vector2(-9.65f,12.95f),
                new Vector2(-7.12f,12.95f),new Vector2(-7.12f,8.72f),new Vector2(-9.65f,8.72f)),
            Patrol(mission,"Treatment patrol",1.21f,6.7f,new Vector2(9.85f,8.2f),new Vector2(9.85f,12.6f),
                new Vector2(7.35f,12.6f),new Vector2(7.35f,8.2f))
        };

        AsylumGuard Patrol(CarryLabBootstrap mission,string name,float speed,float range,params Vector2[] route)
        {
            var guard = new GameObject(name).AddComponent<AsylumGuard>();
            guard.transform.SetParent(transform,false);
            guard.Initialize(mission,route,speed,range);
            return guard;
        }

        void Partition(bool alongZ,float axis,float minimum,float maximum,params float[] openings)
        {
            float cursor = minimum;
            for (int i = 0; i < openings.Length; i += 2)
            {
                Wall(alongZ,axis,cursor,openings[i]);
                float width = openings[i+1]-openings[i];
                var center = WallPosition(alongZ,axis,(openings[i]+openings[i+1])*.5f);
                Module("modular_wall_000",center + Vector3.up*2.75f,alongZ ? 90 : 0,
                    new Vector3(width,WallHeight-2.75f,WallThickness),false,true);
                ColliderBox("Door lintel collision",center+Vector3.up*((WallHeight+2.75f)*.5f),
                    new Vector3(width,WallHeight-2.75f,WallThickness),alongZ ? 90 : 0);
                var trim = alongZ ? new Vector3(.24f,.12f,width+.12f) : new Vector3(width+.12f,.12f,.24f);
                LabGeometry.Box(transform,"Door header",center+Vector3.up*2.72f,trim,Metal,false);
                cursor = openings[i+1];
            }
            Wall(alongZ,axis,cursor,maximum);
        }

        void Wall(bool alongZ,float axis,float minimum,float maximum)
        {
            float length = maximum-minimum;
            if (length < .02f) return;
            int segments = Mathf.CeilToInt(length/2f);
            float width = length/segments;
            for (int i = 0; i < segments; i++)
                Module("modular_wall_000",WallPosition(alongZ,axis,minimum+(i+.5f)*width),alongZ ? 90 : 0,
                    new Vector3(width,WallHeight,WallThickness),false,true);
            var center = WallPosition(alongZ,axis,(minimum+maximum)*.5f) + Vector3.up*WallHeight*.5f;
            ColliderBox("Tiled wall collision",center,alongZ ? new Vector3(WallThickness,WallHeight,length) :
                new Vector3(length,WallHeight,WallThickness));
        }

        static Vector3 WallPosition(bool alongZ,float axis,float length) =>
            alongZ ? new Vector3(axis,0,length) : new Vector3(length,0,axis);

        void Screen(float x,float z,float width,float yaw)
        {
            var p = new Vector3(x,0,z);
            Module("modular_wall_000",p,yaw,new Vector3(width,2.08f,.18f),false,true);
            ColliderBox("Ward screen collision",p+Vector3.up*1.04f,new Vector3(width,2.08f,.18f),yaw);
        }

        void Bed(float x,float z,float yaw)
        {
            Prop("iron_bed_000",x,z,yaw,false);
            ColliderBox("Bed frame collision",new Vector3(x,.39f,z),new Vector3(.94f,.78f,1.88f),yaw);
            Prop("bedside_table_001",x+.72f,z+.87f,0,true);
        }

        void Prop(string id,float x,float z,float yaw,bool solid,float y=0)
        {
            var prop = Place("prop_"+id,new Vector3(x,y,z),yaw);
            if (prop == null || !solid) return;
            var bounds = Measure(prop);
            if (bounds.size.sqrMagnitude > .01f) ColliderBox(id+" collision",bounds.center,bounds.size*.93f);
        }

        void Sign(string text,float x,float z,float yaw,Color color)
        {
            var position = new Vector3(x,2.49f,z);
            var towardsReader = Vector3.forward*(yaw == 0 ? 1 : -1);
            LabGeometry.Box(transform,"Sign backing",position-towardsReader*.035f,
                new Vector3(2.25f,.44f,.045f),Metal,false);
            WorldLabel(text,position+towardsReader*.015f,yaw,color,2f);
        }

        void WorldLabel(string text,Vector3 position,float yaw,Color color,float maxWidth)
        {
            var label = new GameObject(text).AddComponent<TextMesh>();
            label.transform.SetParent(transform,false);
            label.transform.SetPositionAndRotation(position,Quaternion.Euler(0,yaw+180f,0));
            if (signMaterial != null)
            {
                label.font = signFont;
                label.GetComponent<MeshRenderer>().sharedMaterial = signMaterial;
            }
            label.text = text;
            label.fontSize = 64;
            label.characterSize = .1f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = color;
            var size = label.GetComponent<MeshRenderer>().bounds.size;
            if (size.x > .001f && size.y > .001f)
                label.characterSize *= Mathf.Min(maxWidth/size.x,.26f/size.y);
        }

        void UpdateSignAtlas(Font font)
        {
            if (font == signFont) signMaterial.mainTexture = font.material.mainTexture;
        }

        void ExitMarking()
        {
            var bounds = DeliveryBounds;
            Vector3 center = bounds.center;
            center.y = .021f;
            var green = new Color(.19f,.74f,.50f);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                LabGeometry.Box(transform,"Drop zone stripe",center + Vector3.right*(bounds.extents.x*sign),
                    new Vector3(.10f,.018f,bounds.size.z),green,false);
                LabGeometry.Box(transform,"Drop zone stripe",center + Vector3.forward*(bounds.extents.z*sign),
                    new Vector3(bounds.size.x,.018f,.10f),green,false);
            }
            LabGeometry.Box(transform,"Service shutter",new Vector3(0,1.42f,17.88f),new Vector3(3.4f,2.84f,.15f),Metal,false);
            for (int i = 0; i < 11; i++)
                LabGeometry.Box(transform,"Shutter rib",new Vector3(0,.24f+i*.238f,17.786f),
                    new Vector3(3.26f,.026f,.037f),new Color(.23f,.28f,.24f),false);
            WorldLabel("EXIT / LOAD BAY",new Vector3(0,2.47f,17.71f),180,SignGreen,2.58f);
        }

        void Lighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.38f,.43f,.40f);
            RenderSettings.ambientIntensity = 1;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.12f,.15f,.148f);
            RenderSettings.fogStartDistance = 7;
            RenderSettings.fogEndDistance = 29;
            var fill = new GameObject("Cool ward fill").AddComponent<Light>();
            fill.transform.SetParent(transform,false);
            fill.type = LightType.Directional;
            fill.intensity = .43f;
            fill.color = new Color(.84f,.94f,1f);
            fill.transform.rotation = Quaternion.Euler(65,-30,0);

            Fixture(-.8f,-7.2f,new Color(1f,.84f,.61f),2.1f);
            Fixture(0,-1.6f,new Color(.84f,.97f,1f),2.0f);
            Fixture(0,5.8f,new Color(.76f,.91f,1f),1.80f);
            Fixture(0,11.6f,new Color(.88f,.98f,.91f),1.80f);
            Fixture(-8.1f,-.8f,new Color(.88f,.94f,.76f),2.1f);
            Fixture(-8.5f,5.7f,new Color(.95f,.89f,.68f),1.55f);
            Fixture(-8.2f,10.4f,new Color(.69f,.83f,.99f),1.8f);
            Fixture(8.5f,-.4f,new Color(.79f,.91f,.98f),2.1f);
            Fixture(8.3f,8.9f,new Color(.89f,.95f,.83f),1.8f);
            Fixture(0,16.25f,new Color(.52f,1f,.74f),2.4f);
        }

        void Fixture(float x,float z,Color tint,float intensity)
        {
            var p = new Vector3(x,3.10f,z);
            LabGeometry.Box(transform,"Fluorescent housing",p,new Vector3(1.65f,.15f,.32f),Metal,false);
            var glow = LabGeometry.Material(new Color(.83f,.91f,.81f));
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor",new Color(.80f,.91f,.79f));
            var tube = LabGeometry.Box(transform,"Fluorescent tube",p-Vector3.up*.087f,new Vector3(1.48f,.047f,.13f),Color.gray,false);
            tube.GetComponent<Renderer>().sharedMaterial = glow;
            var light = new GameObject("Fluorescent light").AddComponent<Light>();
            light.transform.SetParent(transform,false);
            light.transform.position = p-Vector3.up*.18f;
            light.type = LightType.Point;
            light.renderMode = LightRenderMode.ForceVertex;
            light.range = 6.9f;
            light.intensity = intensity;
            light.color = tint;
        }

        GameObject Place(string id,Vector3 position,float yaw)
        {
            if (!pieces.TryGetValue(id,out var prefab))
            {
                prefab = Resources.Load<GameObject>("AsylumKit/Models/asylum_"+id);
                pieces[id] = prefab;
                if (prefab == null) Debug.LogWarning("Asylum kit piece is missing: "+id);
            }
            if (prefab == null) return null;
            var instance = Instantiate(prefab,position,Quaternion.Euler(0,yaw,0),transform);
            foreach (var renderer in instance.GetComponentsInChildren<Renderer>())
                if (kitMaterial != null) renderer.sharedMaterial = kitMaterial;
            return instance;
        }

        void Module(string id,Vector3 p,float yaw,Vector3 size,bool alignTop,bool isWall)
        {
            var model = Place(id,Vector3.zero,0);
            if (model == null)
            {
                var degraded = new Color(.20f,.24f,.23f);
                var position = p + Vector3.up*(alignTop ? -size.y*.5f : size.y*.5f);
                LabGeometry.Box(transform,id,position,yaw == 0 ? size : new Vector3(size.z,size.y,size.x),degraded,false);
                return;
            }
            var wrapper = new GameObject(id).transform;
            wrapper.SetParent(transform,false);
            model.transform.SetParent(wrapper,false);
            var bounds = Measure(model);
            if (isWall && bounds.size.x < bounds.size.z)
            {
                model.transform.localRotation = Quaternion.Euler(0,90,0);
                bounds = Measure(model);
            }
            var scale = new Vector3(size.x/Mathf.Max(.001f,bounds.size.x),size.y/Mathf.Max(.001f,bounds.size.y),size.z/Mathf.Max(.001f,bounds.size.z));
            var basePoint = new Vector3(bounds.center.x,alignTop ? bounds.max.y : bounds.min.y,bounds.center.z);
            wrapper.localScale = scale;
            wrapper.rotation = Quaternion.Euler(0,yaw,0);
            wrapper.position = p - wrapper.rotation*Vector3.Scale(scale,basePoint);
        }

        static Bounds Measure(GameObject piece)
        {
            var renderers = piece.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds(piece.transform.position,Vector3.zero);
            var bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
            return bounds;
        }

        void ColliderBox(string name,Vector3 center,Vector3 size,float yaw=0)
        {
            var box = new GameObject(name).AddComponent<BoxCollider>();
            box.transform.SetParent(transform,false);
            box.transform.SetPositionAndRotation(center,Quaternion.Euler(0,yaw,0));
            box.size = size;
        }

        void OnDestroy()
        {
            if (kitMaterial != null) Destroy(kitMaterial);
            if (signMaterial != null)
            {
                Font.textureRebuilt -= UpdateSignAtlas;
                Destroy(signMaterial);
            }
        }
    }
}
