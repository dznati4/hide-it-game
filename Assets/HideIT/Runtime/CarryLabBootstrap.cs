using UnityEngine;

namespace HideIT
{
    public sealed class CarryLabBootstrap : MonoBehaviour
    {
        public static CarryLabBootstrap Instance { get; private set; }
        public CarryLabPlayer Player { get; private set; }
        public RagdollBody Body { get; private set; }
        public bool Started { get; private set; }
        public bool Paused { get; private set; }
        public bool Delivered { get; private set; }
        public bool Caught { get; private set; }
        public bool InAsylum { get; private set; }
        public bool Automated { get; private set; }
        public float Elapsed { get; private set; }
        public string Notice { get; private set; } = "";
        public int SelectedOutfit { get; private set; }
        public AmbientCivilians Civilians { get; private set; }
        public RagdollSkinBinding BodySkin { get; private set; }
        public AsylumGuard[] Guards { get; private set; } = System.Array.Empty<AsylumGuard>();
        public Bounds DropZone => InAsylum ? AsylumWard.DeliveryBounds : CarryLabRoom.DeliveryBounds;
        Vector3 PlayerSpawn => InAsylum ? AsylumWard.PlayerSpawn : CarryLabRoom.PlayerSpawn;
        Vector3 BodySpawn => InAsylum ? AsylumWard.BodySpawn : CarryLabRoom.BodySpawn;
        string MissionName => InAsylum ? "ASYLUM / NIGHT SHIFT" : "CARRY LAB";
        CharacterSelectionPreview characterPreview;
        bool choosingOutfit;
        float noticeTime;
        GUIStyle title, heading, regular, small, button, successTitle, failureTitle, successText;
        Texture2D panelTexture;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ClearStatic() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Launch()
        {
            if (Instance != null || Object.FindFirstObjectByType<CarryLabBootstrap>() != null) return;
            new GameObject("Hide iT / Carry Lab").AddComponent<CarryLabBootstrap>();
        }

        void Awake()
        {
            Instance = this;
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Automated = System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-hideit-smoke") >= 0;
#endif
            var args = System.Environment.GetCommandLineArgs();
            InAsylum = System.Array.IndexOf(args,"-hideit-lab") < 0;
            Time.timeScale = 1;
            Time.fixedDeltaTime = 1f / 60f;
            Application.targetFrameRate = 120;
            var environment = new GameObject(InAsylum ? "Asylum ward" : "Carry lab room");
            environment.transform.SetParent(transform,false);
            AsylumWard ward = null;
            if (InAsylum)
            {
                ward = environment.AddComponent<AsylumWard>();
                ward.Build();
            }
            else environment.AddComponent<CarryLabRoom>().Build();
            Body = new GameObject("Ragdoll test subject").AddComponent<RagdollBody>();
            Body.transform.SetParent(transform);
            Body.Build(BodySpawn);
            BodySkin = RagdollSkinBinding.Attach(Body);
            Player = new GameObject("Local test player").AddComponent<CarryLabPlayer>();
            Player.transform.SetParent(transform);
            Player.Build(Body);
            Player.ResetAt(PlayerSpawn);
            characterPreview = new GameObject("Character selection preview").AddComponent<CharacterSelectionPreview>();
            characterPreview.transform.SetParent(transform, false);
            SelectOutfit(CharacterArtCatalog.SavedSelection);
            if (ward != null) Guards = ward.CreatePatrols(this);
            SetCursor(false);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Automated)
            {
                if (InAsylum) gameObject.AddComponent<AsylumSmokeTest>();
                else gameObject.AddComponent<CarryLabSmokeTest>();
            }
#endif
        }

        void Update()
        {
            if (!Started)
            {
                if (!Automated)
                {
                    if (Input.GetKeyDown(KeyCode.LeftArrow)) CycleOutfit(-1);
                    if (Input.GetKeyDown(KeyCode.RightArrow)) CycleOutfit(1);
                    if (Input.GetKeyDown(KeyCode.Return)) Begin();
                }
                return;
            }
            if (Caught)
            {
                if (!Automated && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))) Retry();
                return;
            }
            if (!Automated && Input.GetKeyDown(KeyCode.Escape))
            {
                if (choosingOutfit) ShowPauseOutfits(false);
                else Pause(!Paused);
            }
            if (choosingOutfit && !Automated)
            {
                if (Input.GetKeyDown(KeyCode.LeftArrow)) CycleOutfit(-1);
                if (Input.GetKeyDown(KeyCode.RightArrow)) CycleOutfit(1);
            }
            if (Paused) return;
            if (!Automated && Input.GetKeyDown(KeyCode.R)) ResetLab();
            Elapsed += Delivered ? 0 : Time.deltaTime;
            noticeTime -= Time.deltaTime;
            if (!Body.IsFiniteAndContained() || Player.transform.position.y < -4)
            {
                ResetLab();
                Notice = "The mission was reset after a physics error.";
                noticeTime = 4;
            }
            if (!Delivered && Body.IsEntirelyIn(DropZone))
            {
                Delivered = true;
                Debug.Log("HIDEIT_LAB_DELIVERED");
            }
        }

        public void Begin()
        {
            if (Started) return;
            Started = true;
            if (!Automated && !Application.isBatchMode) CharacterArtCatalog.SaveSelection(SelectedOutfit);
            characterPreview.SetVisible(false);
            Civilians = gameObject.AddComponent<AmbientCivilians>();
            if (InAsylum) Civilians.UseIntakeRoutes();
            Pause(false);
        }

        public bool SelectOutfit(int index)
        {
            if ((Started && (!Paused || Caught)) || !Player.SelectOutfit(index)) return false;
            SelectedOutfit = index;
            characterPreview.Select(index);
            if (Started && !Automated && !Application.isBatchMode) CharacterArtCatalog.SaveSelection(index);
            return true;
        }

        void CycleOutfit(int direction)
        {
            var count = CharacterArtCatalog.Players.Length;
            SelectOutfit((SelectedOutfit + direction + count) % count);
        }

        public void Pause(bool pause)
        {
            if (Caught) return;
            ShowPauseOutfits(false);
            Paused = pause;
            Player.InputEnabled = Started && !pause;
            Time.timeScale = pause ? 0 : 1;
            SetCursor(Started && !pause);
        }

        public void ShowPauseOutfits(bool visible)
        {
            choosingOutfit = visible && Paused && !Caught;
            characterPreview.SetVisible(!Started || choosingOutfit);
        }

        public void Catch()
        {
            if (Caught || Paused || Delivered || !Started) return;
            Caught = true;
            Player.Release();
            Player.InputEnabled = false;
            Time.timeScale = 0;
            SetCursor(false);
            Debug.Log("HIDEIT_ASYLUM_CAUGHT");
        }

        public void Retry()
        {
            ResetLab();
            Pause(false);
        }

        static void SetCursor(bool locked)
        {
            if (Application.isBatchMode || (Instance != null && Instance.Automated)) return;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }

        public void ResetLab()
        {
            Player.ResetAt(PlayerSpawn);
            Body.ResetAt(BodySpawn);
            foreach (var guard in Guards) guard.Rewind();
            if (InAsylum && Civilians != null) Civilians.ResetRoutes();
            Caught = false;
            Delivered = false;
            Elapsed = 0;
            Notice = "";
            noticeTime = 0;
        }

        void EnsureStyles()
        {
            if (regular != null) return;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            regular = new GUIStyle(GUI.skin.label) { font = font, fontSize = 18, wordWrap = true };
            regular.normal.textColor = new Color(.91f, .94f, .92f);
            small = new GUIStyle(regular) { fontSize = 14 };
            title = new GUIStyle(regular) { fontSize = 64, fontStyle = FontStyle.Bold };
            title.normal.textColor = LabGeometry.Orange;
            heading = new GUIStyle(regular) { fontSize = 23, fontStyle = FontStyle.Bold };
            successTitle = new GUIStyle(heading) { fontSize = 34, alignment = TextAnchor.MiddleCenter };
            successTitle.normal.textColor = new Color(.63f, .95f, .82f);
            failureTitle = new GUIStyle(successTitle);
            failureTitle.normal.textColor = new Color(1f,.47f,.39f);
            successText = new GUIStyle(regular) { alignment = TextAnchor.MiddleCenter };
            button = new GUIStyle(GUI.skin.button) { font = font, fontSize = 19, padding = new RectOffset(16, 16, 12, 12) };
            panelTexture = new Texture2D(1, 1);
            panelTexture.SetPixel(0, 0, new Color(.055f, .085f, .11f, .96f));
            panelTexture.Apply();
        }

        void Panel(Rect rect) { GUI.DrawTexture(rect, panelTexture); }
        void Text(float x, float y, float w, float h, string text, GUIStyle style = null)
        { GUI.Label(new Rect(x, y, w, h), text, style ?? regular); }

        void OnGUI()
        {
            if (Player == null) return;
            EnsureStyles();
            var oldMatrix = GUI.matrix;
            float scale = Mathf.Min(Screen.width / 1280f, Screen.height / 720f);
            GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280 * scale) * .5f, (Screen.height - 720 * scale) * .5f), Quaternion.identity, Vector3.one * scale);
            if (!Started)
            {
                Panel(new Rect(48, 45, 565, 630));
                Text(78, 62, 500, 90, "Hide iT", title);
                Text(82, 155, 470, 25, "ZanettiZONEai  /  PLAY TEST " + Application.version, small);
                Text(82, 205, 470, 40, MissionName, heading);
                Text(82, 257, 460, 76, InAsylum ? "Get the body to the service exit. Watch the guards’ floor cones and use the wards for cover." :
                    "Drag the body through the doorway and get it entirely inside the green drop zone.");
                Text(82, 350, 470, 145, "WASD  Move     Shift  Run\nCtrl / C  Crouch     Mouse  Look\nE  Grab head / hand / ankle     Q  Drop\nHold left mouse  Raise your grip\nV  First / third person     R  Reset");
                if (GUI.Button(new Rect(82, 523, 465, 56), InAsylum ? "ENTER ASYLUM  [Enter]" : "PLAY CARRY LAB  [Enter]", button)) Begin();
                Text(82, 596, 470, 48, "Offline playtest", small);
                if (characterPreview.Texture != null)
                {
                    DrawOutfits(652);
                    Text(685, 618, 480, 26, "Left / Right to browse", small);
                }
            }
            else
            {
                Panel(new Rect(22, 20, 380, 95));
                Text(40, 31, 355, 30, MissionName, heading);
                Text(40, 69, 350, 35, Delivered ? "Objective complete" : InAsylum ? "Bring the body to the green service exit." : "Get the body into the green drop zone.", small);
                Panel(new Rect(22, 595, 595, 105));
                Text(40, 607, 560, 25, "WASD move  /  Shift run  /  Ctrl crouch  /  V camera", small);
                Text(40, 636, 560, 26, "E grab/release  /  Q drop  /  Left mouse raise  /  R reset", small);
                Text(40, 668, 560, 24, "Hide iT  -  ZanettiZONEai  -  Offline prototype", small);
                Panel(new Rect(1158, 20, 100, 46));
                Text(1175, 29, 78, 30, System.TimeSpan.FromSeconds(Elapsed).ToString(@"mm\:ss"), heading);

                if (!Paused && !Caught)
                {
                    if (Player.Holding)
                        Prompt("Holding " + Player.HeldLabel + (Player.Lifted ? " / RAISED" : " / DRAGGING"));
                    else if (Player.TryFindGrip(out var grip)) Prompt("[ E ] Grab " + grip.Label);
                    Text(631, 342, 30, 30, "+", heading);
                    DrawExposure();
                }
                if (Delivered && !Paused)
                {
                    Panel(new Rect(360, 114, 560, 130));
                    Text(374, 126, 532, 46, "LEVEL COMPLETE", successTitle);
                    Text(374, 176, 532, 25, "The body reached the drop zone.", successText);
                    Text(374, 204, 532, 23, "Press R to play again", successText);
                }
                else if (noticeTime > 0)
                {
                    Panel(new Rect(370, 130, 540, 72));
                    Text(390, 146, 500, 56, Notice);
                }
                if (Caught)
                {
                    Panel(new Rect(370, 238, 540, 217));
                    Text(388, 263, 504, 47, "CAUGHT", failureTitle);
                    Text(393, 320, 494, 42, "A guard saw you carrying the body.", successText);
                    if (GUI.Button(new Rect(420, 377, 440, 49), "RESTART  [R / Enter]", button)) Retry();
                }
                else if (choosingOutfit && characterPreview.Texture != null)
                {
                    DrawOutfits(350);
                    if (GUI.Button(new Rect(385, 612, 510, 41), "BACK  [Esc]", button)) ShowPauseOutfits(false);
                }
                else if (Paused)
                {
                    Panel(new Rect(390, 202, 500, 326));
                    Text(425, 230, 430, 45, "PAUSED", heading);
                    if (GUI.Button(new Rect(425, 286, 430, 50), "RESUME  [Esc]", button)) Pause(false);
                    if (GUI.Button(new Rect(425, 349, 430, 50), "CHANGE CHARACTER", button)) ShowPauseOutfits(true);
                    if (GUI.Button(new Rect(425, 412, 430, 50), "RESTART MISSION", button)) Retry();
                }
            }
            GUI.matrix = oldMatrix;
        }

        void DrawOutfits(float x)
        {
            Panel(new Rect(x,45,580,630));
            Text(x+33,74,480,36,"CHOOSE YOUR CHARACTER",heading);
            GUI.DrawTexture(new Rect(x+108,127,364,400),characterPreview.Texture,ScaleMode.ScaleToFit);
            var outfit = CharacterArtCatalog.Players[SelectedOutfit];
            Text(x+33,501,480,35,outfit.Name+"  /  "+outfit.Description,heading);
            for (int i = 0; i < CharacterArtCatalog.Players.Length; i++)
            {
                var color = GUI.backgroundColor;
                if (i == SelectedOutfit) GUI.backgroundColor = LabGeometry.Teal;
                if (GUI.Button(new Rect(x+29+i*104,555,98,41),CharacterArtCatalog.Players[i].Name,button)) SelectOutfit(i);
                GUI.backgroundColor = color;
            }
        }

        void DrawExposure()
        {
            float seen = 0;
            foreach (var guard in Guards) seen = Mathf.Max(seen,guard.Exposure);
            if (seen <= .025f) return;
            Panel(new Rect(477, 75, 326, 65));
            Text(499,83,281,26,"SPOTTED",heading);
            var color = GUI.color;
            GUI.color = new Color(1f,.36f,.19f);
            GUI.DrawTexture(new Rect(500,117,280*seen,7),Texture2D.whiteTexture);
            GUI.color = color;
        }

        void Prompt(string value)
        {
            Panel(new Rect(410, 525, 460, 49));
            Text(429, 536, 430, 34, value);
        }

        void OnApplicationFocus(bool focus) { if (!Application.isBatchMode && !Automated && !focus && Started) Pause(true); }
        void OnDestroy()
        {
            Time.timeScale = 1;
            SetCursor(false);
            if (panelTexture != null) Destroy(panelTexture);
            LabGeometry.ClearMaterialCache();
            if (Instance == this) Instance = null;
        }
    }
}
