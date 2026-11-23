using UnityEngine;

namespace HideIT
{
    public sealed class CarryLabRoom : MonoBehaviour
    {
        public static readonly Vector3 PlayerSpawn = new Vector3(-2f, .08f, -4f);
        public static readonly Vector3 BodySpawn = new Vector3(0, .65f, -1.5f);
        public static readonly Bounds DeliveryBounds = new Bounds(new Vector3(3.65f, 1.5f, 6.8f), new Vector3(3.6f, 4f, 3.8f));

        public void Build()
        {
            var wall = new Color(.72f, .76f, .73f);
            var floor = new Color(.23f, .28f, .30f);
            LabGeometry.Box(transform, "Floor", new Vector3(0, -.2f, 2), new Vector3(16, .4f, 18), floor);
            LabGeometry.Box(transform, "Back wall", new Vector3(0, 1.7f, 10.9f), new Vector3(16, 3.4f, .3f), wall);
            LabGeometry.Box(transform, "Front wall", new Vector3(0, 1.7f, -6.9f), new Vector3(16, 3.4f, .3f), wall);
            LabGeometry.Box(transform, "West wall", new Vector3(-7.9f, 1.7f, 2), new Vector3(.3f, 3.4f, 18), wall);
            LabGeometry.Box(transform, "East wall", new Vector3(7.9f, 1.7f, 2), new Vector3(.3f, 3.4f, 18), wall);

            // A genuine 1.5m opening: the body has to rotate to fit through.
            LabGeometry.Box(transform, "Door left", new Vector3(-4.325f, 1.4f, 3), new Vector3(7.15f, 2.8f, .3f), wall);
            LabGeometry.Box(transform, "Door right", new Vector3(4.325f, 1.4f, 3), new Vector3(7.15f, 2.8f, .3f), wall);
            LabGeometry.Box(transform, "Door lintel", new Vector3(0, 2.65f, 3), new Vector3(1.5f, .3f, .3f), LabGeometry.Teal);
            LabGeometry.Label(transform, "STORAGE / 01", new Vector3(0, 3.1f, 2.75f), .32f, LabGeometry.Ink);

            // Blocks and steps are deliberate physics obstacles, not finished level art.
            LabGeometry.Box(transform, "Low crate", new Vector3(-4, .4f, 0), new Vector3(1.6f, .8f, 1.1f), LabGeometry.Orange);
            LabGeometry.Box(transform, "Tall crate", new Vector3(-5.1f, .8f, .3f), new Vector3(1.1f, 1.6f, 1.4f), LabGeometry.Teal);
            for (int i = 0; i < 4; i++)
                LabGeometry.Box(transform, "Step " + i, new Vector3(-4.5f, (i + 1) * .075f, 5 + i * .4f),
                    new Vector3(2.5f, (i + 1) * .15f, .4f), wall);
            LabGeometry.Box(transform, "Stair landing", new Vector3(-4.5f, .3f, 7.3f), new Vector3(2.5f, .6f, 2.2f), wall);

            LabGeometry.Box(transform, "Delivery marking", new Vector3(3.65f, .012f, 6.8f),
                new Vector3(3.6f, .02f, 3.8f), LabGeometry.Teal, false);
            LabGeometry.Label(transform, "DROP ZONE", new Vector3(3.65f, 1.4f, 9f), .4f, LabGeometry.Teal);
            LabGeometry.Label(transform, "Hide iT", new Vector3(-4.3f, 2.1f, 10.65f), .65f, LabGeometry.Ink);

            var sun = new GameObject("Lab daylight").AddComponent<Light>();
            sun.transform.SetParent(transform);
            sun.type = LightType.Directional;
            sun.intensity = 1.15f;
            sun.color = new Color(1f, .95f, .88f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = .72f;
            sun.transform.rotation = Quaternion.Euler(48, -32, 0);

            var fill = new GameObject("Lab fill light").AddComponent<Light>();
            fill.transform.SetParent(transform);
            fill.type = LightType.Directional;
            fill.intensity = .25f;
            fill.color = new Color(.76f, .86f, 1f);
            fill.transform.rotation = Quaternion.Euler(30, 145, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.58f, .64f, .69f);
            RenderSettings.ambientIntensity = 1;
            RenderSettings.fog = false;
        }
    }
}
