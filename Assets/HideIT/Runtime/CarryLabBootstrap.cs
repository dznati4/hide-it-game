using UnityEngine;

namespace HideIT
{
    public enum HideItStage { Asylum, Garage, CarryLab }

    public sealed partial class CarryLabBootstrap : MonoBehaviour
    {
        public static CarryLabBootstrap Instance { get; private set; }
        public CarryLabPlayer Player { get; private set; }
        public RagdollBody Body { get; private set; }
        public bool Started { get; private set; }
        public bool Paused { get; private set; }
        public bool Delivered { get; private set; }
        public bool Caught { get; private set; }
        public HideItStage CurrentStage { get; private set; }
        bool StealthMission => CurrentStage != HideItStage.CarryLab;
        public bool LegacyMode { get; set; }
        public bool Automated { get; private set; }
        public float Elapsed { get; private set; }
        public string Notice { get; private set; } = "";
        public int SelectedOutfit { get; private set; }
        public AmbientCivilians Civilians { get; private set; }
        public RagdollSkinBinding BodySkin { get; private set; }
        public AsylumGuard[] Guards { get; private set; } = System.Array.Empty<AsylumGuard>();
        public AsylumNavigation Navigation { get; private set; }
        public AsylumInteractions Interactions { get; private set; }
        public Bounds DropZone => CarryLabRoom.DeliveryBounds;
        public string CaughtReason { get; private set; }
        Vector3 PlayerSpawn => CurrentStage switch {
            HideItStage.Garage => ParkingGarage.PlayerSpawn,
            HideItStage.Asylum => AsylumWard.PlayerSpawn,
            _ => CarryLabRoom.PlayerSpawn
        };
        Vector3 BodySpawn => CurrentStage switch {
            HideItStage.Garage => ParkingGarage.BodySpawn,
            HideItStage.Asylum => AsylumWard.BodySpawn,
            _ => CarryLabRoom.BodySpawn
        };
        static string StageName(HideItStage stage) => stage switch {
            HideItStage.Garage => "Garage / Lower Level",
            HideItStage.Asylum => "Asylum / Night Shift",
            _ => "Carry Lab"
        };
        GameObject environment;
        CharacterSelectionPreview characterPreview;
        bool choosingOutfit, showingGuide;
        float noticeTime;

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
            CurrentStage = LegacyMode || System.Array.IndexOf(args,"-hideit-lab") >= 0 ? HideItStage.CarryLab :
                System.Array.IndexOf(args,"-hideit-garage") >= 0 ? HideItStage.Garage : HideItStage.Asylum;
            Time.timeScale = 1;
            Time.fixedDeltaTime = 1f / 60f;
            Application.targetFrameRate = 120;
            Body = new GameObject("Ragdoll test subject").AddComponent<RagdollBody>();
            Body.transform.SetParent(transform);
            Body.Build(BodySpawn);
            BodySkin = RagdollSkinBinding.Attach(Body);
            Player = new GameObject("Local test player").AddComponent<CarryLabPlayer>();
            Player.transform.SetParent(transform);
            Player.Build(Body);
            characterPreview = new GameObject("Character selection preview").AddComponent<CharacterSelectionPreview>();
            characterPreview.transform.SetParent(transform, false);
            SelectOutfit(CharacterArtCatalog.SavedSelection);
            BuildStage(CurrentStage);
            SetCursor(false);
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (Automated)
            {
                if (CurrentStage == HideItStage.CarryLab) gameObject.AddComponent<CarryLabSmokeTest>();
                else if (CurrentStage == HideItStage.Asylum) gameObject.AddComponent<AsylumSmokeTest>();
                else gameObject.AddComponent<GarageSmokeTest>();
            }
#endif
        }

        void BuildStage(HideItStage stage)
        {
            if (environment != null)
            {
                environment.SetActive(false);
                Destroy(environment);
            }
            CurrentStage = stage;
            environment = new GameObject(StageName(stage));
            environment.transform.SetParent(transform,false);
            if (StealthMission)
            {
                Interactions = environment.AddComponent<AsylumInteractions>();
                if (stage == HideItStage.Garage)
                {
                    var garage = environment.AddComponent<ParkingGarage>();
                    garage.Build();
                    garage.ConfigureInteractions(this,Interactions);
                    Physics.SyncTransforms();
                    Navigation = new AsylumNavigation();
                    Guards = garage.CreatePatrols(this);
                }
                else
                {
                    var ward = environment.AddComponent<AsylumWard>();
                    ward.Build();
                    ward.ConfigureInteractions(this,Interactions);
                    Physics.SyncTransforms();
                    Navigation = new AsylumNavigation();
                    Guards = ward.CreatePatrols(this);
                }
            }
            else environment.AddComponent<CarryLabRoom>().Build();
            Player.View.clearFlags = stage == HideItStage.Garage ? CameraClearFlags.SolidColor : CameraClearFlags.Skybox;
            Player.View.backgroundColor = stage == HideItStage.Garage ? new Color(.11f,.15f,.19f) : new Color(.26f,.31f,.36f);
            Player.Interactions = Interactions;
            ResetLab();
        }

        void Update()
        {
            if (!Started)
            {
                if (!Automated)
                {
                    if (showingGuide)
                    {
                        if (Input.GetKeyDown(KeyCode.Escape)) ShowGuide(false);
                    }
                    else
                    {
                        if (Input.GetKeyDown(KeyCode.LeftArrow)) CycleOutfit(-1);
                        if (Input.GetKeyDown(KeyCode.RightArrow)) CycleOutfit(1);
                        if (Input.GetKeyDown(KeyCode.Return)) Begin();
                    }
                }
                return;
            }
            if (Caught || (StealthMission && Delivered))
            {
                if (!Automated && (Input.GetKeyDown(KeyCode.R) || Input.GetKeyDown(KeyCode.Return))) Retry();
                return;
            }
            if (!Automated && Input.GetKeyDown(KeyCode.Escape))
            {
                if (showingGuide) ShowGuide(false);
                else if (choosingOutfit) ShowPauseOutfits(false);
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
            if (!StealthMission && !Delivered && Body.IsEntirelyIn(DropZone))
            {
                Delivered = true;
                Debug.Log("HIDEIT_LAB_DELIVERED");
            }
        }

        public void Begin()
        {
            if (Started) return;
            Started = true;
            showingGuide = false;
            if (!Automated && !Application.isBatchMode) CharacterArtCatalog.SaveSelection(SelectedOutfit);
            characterPreview.SetVisible(false);
            Civilians = gameObject.AddComponent<AmbientCivilians>();
            if (CurrentStage == HideItStage.Asylum) Civilians.UseIntakeRoutes();
            if (CurrentStage == HideItStage.Garage) Civilians.UseGarageRoutes();
            Pause(false);
        }

        public void SelectStage(HideItStage stage)
        {
            if (Started || !StealthMission || stage == HideItStage.CarryLab || stage == CurrentStage) return;
            BuildStage(stage);
        }

        public void ReturnToMenu()
        {
            if (!Started) return;
            ResetLab();
            if (Civilians != null)
            {
                Civilians.enabled = false;
                Destroy(Civilians);
                Civilians = null;
            }
            Started = false;
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
            if (Caught || (StealthMission && Delivered)) return;
            showingGuide = false;
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

        public void ShowGuide(bool visible)
        {
            if (visible && Started && !Paused) return;
            showingGuide = visible;
            characterPreview.SetVisible(!Started && !visible);
        }

        public void Catch(string reason)
        {
            if (Caught || Paused || Delivered || !Started) return;
            Caught = true;
            CaughtReason = reason;
            Player.Release();
            Player.InputEnabled = false;
            Time.timeScale = 0;
            SetCursor(false);
            Debug.Log("HIDEIT_ASYLUM_CAUGHT");
        }

        public void CompleteStash()
        {
            if (Caught || !StealthMission || Delivered) return;
            Delivered = true;
            Player.InputEnabled = false;
            SetCursor(false);
            Debug.Log("HIDEIT_ASYLUM_STASHED");
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
            if (Interactions != null) Interactions.ResetRun();
            Player.ResetAt(PlayerSpawn);
            Body.ResetAt(BodySpawn);
            foreach (var guard in Guards) guard.Rewind();
            if (Civilians != null) Civilians.ResetRoutes();
            Caught = false;
            CaughtReason = null;
            Delivered = false;
            Elapsed = 0;
            Notice = "";
            noticeTime = 0;
        }

        void OnApplicationFocus(bool focus) { if (!Application.isBatchMode && !Automated && !focus && Started) Pause(true); }
        void OnDestroy()
        {
            Time.timeScale = 1;
            SetCursor(false);
            LabGeometry.ClearMaterialCache();
            if (Instance == this) Instance = null;
        }
    }
}
