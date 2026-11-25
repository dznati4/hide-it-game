using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace HideIT
{
    public sealed class ParkingGarage : MonoBehaviour
    {
        public static readonly Vector3 PlayerSpawn = new Vector3(-8.55f,.045f,-7.75f);
        public static readonly Vector3 BodySpawn = new Vector3(-8.35f,.63f,-6.02f);
        public static readonly Vector3 Dumpster = new Vector3(-7.6f,0,16.55f);
        const float Ceiling = 3.65f;
        const string Concrete = "Concrete_Weathered", Asphalt = "Asphalt_Road", Deck = "T_FloorPlate_A";
        static readonly Color Paint = new Color(.81f,.61f,.26f), White = new Color(.76f,.78f,.71f);
        static readonly Color ConcreteTint = new Color(.68f,.68f,.61f);
        static readonly Color Steel = new Color(.18f,.22f,.22f), Rubber = new Color(.09f,.10f,.10f);
        static readonly Color Glass = new Color(.15f,.23f,.25f);
        readonly Dictionary<(string,Color,Vector2),Material> surfaces = new();
        readonly Dictionary<string,GameObject> prefabs = new();
        Material lettering;
        Font font;
        public int ImportedProps { get; private set; }

        public void Build()
        {
            var signShader = Resources.Load<Shader>("AsylumSignText");
            if (signShader != null)
            {
                font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lettering = new Material(signShader) { mainTexture = font.material.mainTexture };
                Font.textureRebuilt += RefreshFont;
            }

            Block("Street",new Vector3(0,-.14f,-7),new Vector3(24,.28f,6),Asphalt,White,new Vector2(8,2));
            Block("Lower garage floor",new Vector3(0,-.14f,3),new Vector3(24,.28f,14),Asphalt,White,new Vector2(8,5));
            Block("Loading bay floor",new Vector3(0,-.14f,14),new Vector3(24,.28f,8),Concrete,White,new Vector2(8,3));
            Block("Overhead slab",new Vector3(0,Ceiling+.17f,7),new Vector3(24,.34f,22),Concrete,ConcreteTint,new Vector2(8,7));
            Block("Street boundary",new Vector3(0,1.85f,-10.12f),new Vector3(24,3.7f,.24f),null,Steel,Vector2.one)
                .GetComponent<MeshRenderer>().enabled = false;
            for (int side = -1; side <= 1; side += 2)
                Block("Side retaining wall",new Vector3(12*side,Ceiling*.5f,4),new Vector3(.24f,Ceiling,28),Concrete,ConcreteTint,new Vector2(9,2));
            Wall(18,-12,12);
            Portal(-4,-12,12,-10,-6,6,9.6f);
            Portal(10,-12,12,-9.5f,-6.0f,6.45f,9.65f);
            for (int side = -1; side <= 1; side += 2)
                Block("Entrance canopy",new Vector3(8*side,3.47f,-5.63f),new Vector3(4.68f,.27f,3.48f),
                    Concrete,ConcreteTint,new Vector2(2,1));

            for (int i = 0; i < 3; i++)
            {
                float z = -.75f+i*4.1f;
                Pillar(-3.55f,z,"A"+(i+1));
                Pillar(3.55f,z,"B"+(i+1));
                Bay(-5.0f,z);
                Bay(5.0f,z);
            }
            Car(-5.05f,-.68f,new Color(.36f,.24f,.18f),0);
            Car(-5.07f,7.29f,new Color(.32f,.36f,.26f),180);
            Car(5.09f,.26f,new Color(.22f,.32f,.36f),180);
            Car(5.06f,6.59f,new Color(.37f,.36f,.31f),0);
            Block("Security booth",new Vector3(0,1.21f,2.0f),new Vector3(2.64f,2.42f,2.65f),Concrete,ConcreteTint,Vector2.one);
            LabGeometry.Box(transform,"Booth window",new Vector3(0,1.52f,.666f),new Vector3(2.14f,.72f,.03f),Glass,false);
            LabGeometry.Box(transform,"Booth metal trim",new Vector3(0,2.44f,2),new Vector3(2.82f,.12f,2.8f),Steel,false);
            Sign("SECURITY",new Vector3(0,2.13f,.62f),180,White,1.72f);
            Sign("B1  PARKING",new Vector3(-8,2.97f,-4.16f),180,White,2.35f);
            Sign("B1  PARKING",new Vector3(7.8f,2.97f,-4.16f),180,White,2.35f);
            Sign("LOADING BAY",new Vector3(-7.75f,2.96f,9.84f),180,Paint,2.35f);
            Sign("MAINTENANCE",new Vector3(8,2.96f,9.84f),180,White,2.35f);

            // Offset walls leave two routes across the loading floor while screening the dumpster.
            Block("Loading partition",new Vector3(-2.45f,1.43f,13),new Vector3(.25f,2.86f,5.65f),Concrete,ConcreteTint,new Vector2(3,1));
            Block("Maintenance divider",new Vector3(3.35f,1.43f,15.83f),new Vector3(.24f,2.86f,4.3f),Concrete,ConcreteTint,new Vector2(2,1));
            Block("Loading screen",new Vector3(-7.7f,1.33f,12.25f),new Vector3(3.2f,2.66f,.32f),Concrete,ConcreteTint,new Vector2(2,1));
            Block("Loading crates",new Vector3(-10.88f,.66f,16.12f),new Vector3(1.21f,1.32f,2.02f),Concrete,
                new Color(.42f,.35f,.23f),Vector2.one);
            Block("Maintenance deck",new Vector3(7.7f,.002f,14),new Vector3(6.7f,.011f,7.9f),Deck,White,new Vector2(3,4),false);
            Sign("WASTE  <",new Vector3(-7.7f,1.82f,12.064f),180,Paint,1.63f);
            Sign("WASTE / LOAD BAY",new Vector3(-7.6f,2.47f,17.82f),180,Paint,2.56f);
            AddMaintenanceProps();
            AddStreetProps();
            Lighting();
        }

        public void ConfigureInteractions(CarryLabBootstrap mission,AsylumInteractions actions)
        {
            actions.Build(mission,Dumpster,Surface("Paint_Green_Dumpster",White,Vector2.one));
            AddLocker(actions,-10.75f,4.65f,-90);
            AddLocker(actions,10.84f,4.2f,90);
            AddLocker(actions,-.9f,14.85f,-90);
            AddLocker(actions,10.85f,14.8f,90);
            actions.AddPickup(new Vector3(-10.82f,.12f,-6.1f));
            actions.AddPickup(new Vector3(-8.86f,.12f,4.55f));
            actions.AddPickup(new Vector3(8.62f,.12f,3.1f));
            actions.AddPickup(new Vector3(8.78f,.12f,13.35f));
            actions.AddPickup(new Vector3(-4.02f,.12f,16.6f));
        }

        public AsylumGuard[] CreatePatrols(CarryLabBootstrap mission) => new[] {
            Patrol(mission,"West parking patrol",1.25f,
                new Vector2(-4.42f,3.40f),new Vector2(-1.9f,5.6f),new Vector2(-1.7f,8.52f),
                new Vector2(-7.82f,8.55f),new Vector2(-7.72f,5.1f),new Vector2(-8.65f,-2.45f),new Vector2(-1.6f,-2.5f)),
            Patrol(mission,"East parking patrol",1.20f,
                new Vector2(8.6f,.52f),new Vector2(8.14f,8.45f),new Vector2(2.24f,8.52f),
                new Vector2(1.91f,4.12f),new Vector2(1.93f,-2.4f),new Vector2(7.9f,-2.1f)),
            Patrol(mission,"Loading patrol",1.13f,
                new Vector2(8.05f,15.85f),new Vector2(5.8f,16.05f),new Vector2(4.72f,11.45f),
                new Vector2(-.5f,11f),new Vector2(-5.21f,11.72f),new Vector2(-7.3f,13.85f),new Vector2(-3.7f,16.38f))
        };

        AsylumGuard Patrol(CarryLabBootstrap mission,string label,float speed,params Vector2[] route)
        {
            var guard = new GameObject(label).AddComponent<AsylumGuard>();
            guard.transform.SetParent(transform,false);
            guard.Initialize(mission,mission.Navigation,route,speed,7.0f);
            return guard;
        }

        void Portal(float z,float left,float right,float a,float b,float c,float d)
        {
            Wall(z,left,a);
            Wall(z,b,c);
            Wall(z,d,right);
            Header(z,a,b);
            Header(z,c,d);
        }

        void Wall(float z,float left,float right) =>
            Block("Concrete partition",new Vector3((left+right)*.5f,Ceiling*.5f,z),
                new Vector3(right-left,Ceiling,.26f),Concrete,ConcreteTint,new Vector2((right-left)/2.8f,1.5f));

        void Header(float z,float left,float right) =>
            Block("Portal lintel",new Vector3((left+right)*.5f,3.14f,z),
                new Vector3(right-left,1.02f,.26f),Concrete,ConcreteTint,new Vector2(2,1));

        void Pillar(float x,float z,string number)
        {
            Block("Concrete support",new Vector3(x,Ceiling*.5f,z),new Vector3(.77f,Ceiling,.77f),Concrete,ConcreteTint,Vector2.one);
            LabGeometry.Box(transform,"Parking pillar band",new Vector3(x,1.76f,z),new Vector3(.80f,.36f,.80f),Paint,false);
            Sign(number,new Vector3(x,1.77f,z-.41f),180,Steel,.58f,false);
        }

        void Bay(float x,float z)
        {
            foreach (int side in new[] { -1,1 })
                LabGeometry.Box(transform,"Parking stripe",new Vector3(x+side*1.34f,.013f,z),
                    new Vector3(.065f,.016f,3.79f),White,false);
        }

        void Car(float x,float z,Color paint,float yaw)
        {
            var car = new GameObject("Parked car").transform;
            car.SetParent(transform,false);
            car.SetLocalPositionAndRotation(new Vector3(x,0,z),Quaternion.Euler(0,yaw,0));
            LabGeometry.Box(car,"Vehicle collision",new Vector3(0,.78f,0),new Vector3(1.79f,1.50f,3.77f),paint)
                .GetComponent<MeshRenderer>().enabled = false;
            Shell(car,"Faceted body",paint,(-1.89f,.28f,.69f,.74f,.79f),(-1.56f,.26f,.76f,.9f,.84f),
                (1.56f,.26f,.76f,.9f,.84f),(1.92f,.28f,.70f,.75f,.79f));
            Shell(car,"Tinted cabin",Glass,(-1.10f,.73f,.79f,.77f,.72f),(-.56f,.73f,1.37f,.77f,.63f),
                (.66f,.73f,1.39f,.77f,.63f),(1.18f,.73f,.76f,.77f,.72f));
            LabGeometry.Box(car,"Roof",new Vector3(0,1.378f,.055f),new Vector3(1.28f,.058f,1.29f),paint,false);
            LabGeometry.Box(car,"Bumper",new Vector3(0,.40f,1.94f),new Vector3(1.65f,.095f,.10f),Rubber,false);
            LabGeometry.Box(car,"Bumper",new Vector3(0,.40f,-1.92f),new Vector3(1.65f,.095f,.10f),Rubber,false);
            LabGeometry.Box(car,"Rear plate",new Vector3(0,.59f,-1.91f),new Vector3(.31f,.105f,.022f),White,false);
            foreach (int side in new[] { -1,1 })
            {
                LabGeometry.Box(car,"Window pillar",new Vector3(side*.708f,1.045f,.08f),new Vector3(.055f,.56f,.11f),paint,false);
                LabGeometry.Box(car,"Headlamp",new Vector3(side*.55f,.62f,1.92f),new Vector3(.34f,.15f,.035f),White,false);
                LabGeometry.Box(car,"Tail lamp",new Vector3(side*.56f,.61f,-1.895f),new Vector3(.31f,.14f,.035f),new Color(.51f,.19f,.15f),false);
                foreach (int end in new[] { -1,1 })
                {
                    var pos = new Vector3(side*.815f,.31f,end*1.18f);
                    var tire = LabGeometry.Shape(car,"Tire",PrimitiveType.Cylinder,pos,new Vector3(.61f,.115f,.61f),Rubber,false);
                    tire.transform.localRotation = Quaternion.Euler(0,0,90);
                    var hub = LabGeometry.Shape(car,"Hubcap",PrimitiveType.Cylinder,pos+Vector3.right*side*.117f,
                        new Vector3(.31f,.018f,.31f),Steel,false);
                    hub.transform.localRotation = tire.transform.localRotation;
                }
            }
        }

        void Shell(Transform parent,string name,Color paint,params (float z,float bottom,float top,float baseWidth,float topWidth)[] rings)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            void Quad(Vector3 a,Vector3 b,Vector3 c,Vector3 d)
            {
                int i = vertices.Count;
                vertices.Add(a); vertices.Add(b); vertices.Add(c); vertices.Add(d);
                triangles.Add(i); triangles.Add(i+1); triangles.Add(i+2);
                triangles.Add(i); triangles.Add(i+2); triangles.Add(i+3);
            }
            Vector3[] Corners((float z,float bottom,float top,float baseWidth,float topWidth) ring) => new[] {
                new Vector3(-ring.baseWidth,ring.bottom,ring.z),new Vector3(-ring.topWidth,ring.top,ring.z),
                new Vector3(ring.topWidth,ring.top,ring.z),new Vector3(ring.baseWidth,ring.bottom,ring.z)
            };
            var edge = Corners(rings[0]);
            Quad(edge[0],edge[1],edge[2],edge[3]);
            for (int i = 1; i < rings.Length; i++)
            {
                var next = Corners(rings[i]);
                for (int j = 0; j < 4; j++)
                    Quad(edge[j],next[j],next[(j+1)%4],edge[(j+1)%4]);
                edge = next;
            }
            Quad(edge[3],edge[2],edge[1],edge[0]);
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles,0);
            mesh.RecalculateNormals();
            var obj = new GameObject(name);
            obj.transform.SetParent(parent,false);
            obj.AddComponent<MeshFilter>().sharedMesh = mesh;
            obj.AddComponent<MeshRenderer>().sharedMaterial = LabGeometry.Material(paint);
        }

        void AddStreetProps()
        {
            Block("Street curb",new Vector3(0,.06f,-5.07f),new Vector3(11.2f,.12f,1.64f),
                "Concrete_Sidewalk",White,new Vector2(5,1),false);
            Block("Street backdrop",new Vector3(0,-.2f,-17.06f),new Vector3(61,.30f,14),Asphalt,White,new Vector2(19,4),false);
            Block("Opposite sidewalk",new Vector3(0,-.08f,-13.35f),new Vector3(61,.18f,3.65f),
                "Concrete_Sidewalk",White,new Vector2(20,2),false);
            Block("Brick street facade",new Vector3(0,4.62f,-4),new Vector3(11.7f,2.16f,.35f),
                "Brick_Brown_Apartment",White,new Vector2(4,1),false);
            Prop("PSX_Commercial_PizzaRestaurant",new Vector3(1,0,-15.5f),new Vector3(11.7f,6.8f,6.35f),0);
            for (int side = -1; side <= 1; side += 2)
            {
                Block("Neighbouring brick building",new Vector3(side*13f,2.94f,-17.6f),new Vector3(12,5.9f,6f),
                    "Brick_Brown_Apartment",new Color(.55f,.53f,.52f),new Vector2(5,3),false);
                LabGeometry.Box(transform,"Alley rail",new Vector3(side*8.9f,.58f,-10.02f),
                    new Vector3(5.7f,.075f,.06f),Steel,false);
                foreach (float x in new[] { 6.15f,8.9f,11.65f })
                    LabGeometry.Box(transform,"Alley rail post",new Vector3(side*x,.37f,-10.02f),
                        new Vector3(.09f,.74f,.09f),Steel,false);
            }
            Prop("PSX_Prop_StopSign_StreetNames",new Vector3(-10.6f,0,-8.98f),new Vector3(.95f,2.8f,.47f),0);
            Prop("PSX_Prop_Streetlight_CobraHead",new Vector3(-6.5f,0,-9.12f),new Vector3(2.6f,5.8f,1.4f),90);
            Prop("PSX_Prop_FireHydrant",new Vector3(5.13f,0,-7.6f),new Vector3(.55f,.83f,.58f),20);
            Prop("PSX_Debris_TrashBag_Full",new Vector3(-11.43f,0,-5.30f),new Vector3(.72f,.67f,.6f),12);
            Prop("PSX_Debris_TrashBag_Full",new Vector3(-9.70f,0,17.05f),new Vector3(.74f,.63f,.59f),60);
        }

        void AddMaintenanceProps()
        {
            Prop("SM_Bulkhead_Door",new Vector3(9.93f,0,17.73f),new Vector3(1.87f,2.60f,.39f),180);
            Prop("SM_Station_Fuse_Box",new Vector3(11.66f,1.01f,13.82f),new Vector3(.72f,.95f,.29f),90);
            Prop("SM_Light_Wall_Caged",new Vector3(8.44f,2.27f,17.62f),new Vector3(.47f,.34f,.19f),180);
            for (int i = 0; i < 4; i++)
                Prop("SM_Pipe_Straight_1m",new Vector3(11.43f,2.79f,11.5f+i*1.4f),
                    new Vector3(.38f,.40f,1.42f),0);
            Prop("SM_Pipe_Elbow90",new Vector3(11.50f,2.36f,17.35f),new Vector3(.66f,.72f,.56f),90);
        }

        void AddLocker(AsylumInteractions actions,float x,float z,float yaw)
        {
            var position = new Vector3(x,0,z);
            if (Prop("SM_Locker",position,new Vector3(.94f,2.13f,.53f),yaw) == null)
            {
                actions.AddCloset(x,z,yaw);
                return;
            }
            LabGeometry.Box(transform,"Locker collision",position+Vector3.up*1.065f,
                new Vector3(.56f,2.13f,.94f),Steel).GetComponent<MeshRenderer>().enabled = false;
            var front = Quaternion.Euler(0,yaw,0)*Vector3.back;
            actions.AddHidingSpot("Hide in locker",position+front*.75f,position+front*.34f+Vector3.up*1.23f,yaw+180f);
        }

        GameObject Prop(string id,Vector3 position,Vector3 size,float yaw)
        {
            if (!prefabs.TryGetValue(id,out var prefab))
                prefabs[id] = prefab = Resources.Load<GameObject>("GarageKit/Models/"+id);
            if (prefab == null) return null;
            var wrapper = new GameObject(id).transform;
            wrapper.SetParent(transform,false);
            var model = Instantiate(prefab,wrapper,false);
            var renderers = model.GetComponentsInChildren<MeshRenderer>();
            if (renderers.Length == 0)
            {
                Destroy(wrapper.gameObject);
                return null;
            }
            var bounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var materialName = materials[i] != null ? materials[i].name : null;
                    if (!string.IsNullOrEmpty(materialName))
                        materials[i] = Surface(materialName,Color.white,Vector2.one);
                }
                renderer.sharedMaterials = materials;
            }
            var scale = new Vector3(size.x/Mathf.Max(.001f,bounds.size.x),size.y/Mathf.Max(.001f,bounds.size.y),
                size.z/Mathf.Max(.001f,bounds.size.z));
            var ground = new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
            wrapper.localScale = scale;
            wrapper.localRotation = Quaternion.Euler(0,yaw,0);
            wrapper.localPosition = position-wrapper.localRotation*Vector3.Scale(scale,ground);
            ImportedProps++;
            return wrapper.gameObject;
        }

        GameObject Block(string label,Vector3 position,Vector3 size,string texture,Color tint,Vector2 repeats,bool solid = true)
        {
            var block = LabGeometry.Box(transform,label,position,size,tint,solid);
            if (texture != null) block.GetComponent<MeshRenderer>().sharedMaterial = Surface(texture,tint,repeats);
            return block;
        }

        Material Surface(string texture,Color tint,Vector2 repeats)
        {
            var key = (texture,tint,repeats);
            if (surfaces.TryGetValue(key,out var material)) return material;
            material = new Material(LabGeometry.Material(tint)) { name = texture,mainTextureScale = repeats };
            material.SetFloat("_Glossiness",0);
            material.SetFloat("_GlossyReflections",0);
            material.EnableKeyword("_GLOSSYREFLECTIONS_OFF");
            var image = Resources.Load<Texture2D>("GarageKit/Textures/"+texture);
            if (image != null)
            {
                image.filterMode = FilterMode.Point;
                image.wrapMode = TextureWrapMode.Repeat;
                material.mainTexture = image;
            }
            surfaces.Add(key,material);
            return material;
        }

        void Sign(string text,Vector3 position,float yaw,Color tint,float width,bool backing = true)
        {
            if (backing)
                LabGeometry.Box(transform,"Wayfinding panel",position-Vector3.forward*Mathf.Sign(90-yaw)*.025f,
                    new Vector3(width+.12f,.37f,.05f),Steel,false);
            var label = new GameObject(text).AddComponent<TextMesh>();
            label.transform.SetParent(transform,false);
            label.transform.SetLocalPositionAndRotation(position+Vector3.forward*Mathf.Sign(90-yaw)*.019f,Quaternion.Euler(0,yaw+180,0));
            if (lettering != null) label.font = font;
            label.text = text;
            label.fontSize = 64;
            label.characterSize = .095f;
            label.anchor = TextAnchor.MiddleCenter;
            label.alignment = TextAlignment.Center;
            label.color = tint;
            var renderer = label.GetComponent<MeshRenderer>();
            if (lettering != null) renderer.sharedMaterial = lettering;
            label.characterSize *= Mathf.Min(width/Mathf.Max(.001f,renderer.bounds.size.x),.235f/Mathf.Max(.001f,renderer.bounds.size.y));
        }

        void Lighting()
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.40f,.44f,.49f);
            RenderSettings.ambientIntensity = 1;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(.10f,.135f,.16f);
            RenderSettings.fogStartDistance = 8f;
            RenderSettings.fogEndDistance = 34f;
            var fill = new GameObject("Garage fill").AddComponent<Light>();
            fill.transform.SetParent(transform,false);
            fill.type = LightType.Directional;
            fill.intensity = .50f;
            fill.color = new Color(.78f,.87f,1);
            fill.transform.rotation = Quaternion.Euler(59,-24,0);
            Fixture(-8.25f,-6.9f,new Color(1,.73f,.41f),2.7f,3.25f);
            Fixture(7.2f,-6.6f,new Color(1,.74f,.47f),2.5f,3.25f);
            for (int z = -2; z <= 8; z += 5)
            {
                Fixture(-7.5f,z,new Color(.81f,.90f,1),2.05f);
                Fixture(7.5f,z,new Color(.76f,.90f,1),2.03f);
            }
            Fixture(0,3.1f,new Color(.72f,.90f,1),1.75f);
            Fixture(-7.55f,14.7f,new Color(1,.79f,.50f),2.65f);
            Fixture(7.9f,14.55f,new Color(.53f,.88f,.81f),2.45f);
        }

        void Fixture(float x,float z,Color tint,float intensity,float y = Ceiling-.12f)
        {
            var p = new Vector3(x,y,z);
            LabGeometry.Box(transform,"Strip light frame",p,new Vector3(1.82f,.10f,.22f),Steel,false);
            var glow = LabGeometry.Material(tint);
            glow.EnableKeyword("_EMISSION");
            glow.SetColor("_EmissionColor",tint*.83f);
            LabGeometry.Box(transform,"Strip light tube",p-Vector3.up*.064f,new Vector3(1.68f,.035f,.115f),tint,false);
            var lamp = new GameObject("Overhead light").AddComponent<Light>();
            lamp.transform.SetParent(transform,false);
            lamp.transform.localPosition = p-Vector3.up*.22f;
            lamp.type = LightType.Point;
            lamp.color = tint;
            lamp.range = 8.7f;
            lamp.intensity = intensity;
            lamp.renderMode = LightRenderMode.ForceVertex;
        }

        void RefreshFont(Font changed)
        {
            if (changed == font) lettering.mainTexture = font.material.mainTexture;
        }

        void OnDestroy()
        {
            if (lettering != null)
            {
                Font.textureRebuilt -= RefreshFont;
                Destroy(lettering);
            }
            foreach (var material in surfaces.Values) Destroy(material);
            foreach (var filter in GetComponentsInChildren<MeshFilter>(true))
                if (filter.gameObject.name is "Faceted body" or "Tinted cabin") Destroy(filter.sharedMesh);
        }
    }
}
