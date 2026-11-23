using System.Collections.Generic;
using UnityEngine;

namespace HideIT
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class CarryLabPlayer : MonoBehaviour
    {
        public const float WalkSpeed = 3.4f;
        public const float RunSpeed = 5.6f;
        public const float DragSpeed = 1.65f;
        public const float GrabRange = 2.7f;
        public bool Holding => heldJoint != null;
        public string HeldLabel { get; private set; } = "";
        public bool Crouched { get; private set; }
        public bool Lifted { get; private set; }
        public bool InputEnabled { get; set; }
        public float Yaw { get; private set; }
        public float Pitch { get; private set; } = 12;
        public bool FirstPerson { get; private set; }
        public Camera View { get; private set; }
        public RagdollBody Body { get; private set; }
        public float CurrentSpeed { get; private set; }
        public string OutfitId { get; private set; }

        CharacterController controller;
        Transform avatar;
        GameObject outfitModel;
        ProceduralCharacterPose outfitPose;
        GameObject placeholder;
        Rigidbody anchor;
        SpringJoint heldJoint;
        RagdollBody.Grip heldGrip;
        RagdollSkinBinding bodySkin;
        SkinnedMeshRenderer viewArms;
        readonly List<Renderer> avatarRenderers = new List<Renderer>();
        float verticalSpeed;
        Vector3 previousPosition;
        Vector3? hidingView;
        internal AsylumInteractions Interactions { get; set; }
        public Vector3 SightPoint => transform.position + Vector3.up*(Crouched ? .87f : 1.26f);
        const int WorldMask = 1 << 0;

        public void Build(RagdollBody body)
        {
            Body = body;
            bodySkin = body.GetComponent<RagdollSkinBinding>();
            gameObject.layer = 10;
            controller = GetComponent<CharacterController>();
            controller.height = 1.8f;
            controller.center = new Vector3(0, .9f, 0);
            controller.radius = .3f;
            controller.stepOffset = .28f;
            controller.slopeLimit = 48;
            controller.skinWidth = .035f;
            foreach (var collider in body.Colliders) Physics.IgnoreCollision(controller, collider);

            avatar = new GameObject("Player visuals").transform;
            avatar.SetParent(transform, false);
            placeholder = new GameObject("Fallback model");
            placeholder.transform.SetParent(avatar, false);
            var fallback = placeholder.transform;
            LabGeometry.Box(fallback, "Jacket", new Vector3(0, 1.15f, 0), new Vector3(.55f, .65f, .3f), LabGeometry.Orange, false);
            LabGeometry.Shape(fallback, "Head", PrimitiveType.Sphere, new Vector3(0, 1.68f, 0), Vector3.one * .38f, new Color(.82f, .61f, .42f), false);
            for (int side = -1; side <= 1; side += 2)
            {
                LabGeometry.Box(fallback, "Leg", new Vector3(side * .17f, .42f, 0), new Vector3(.23f, .8f, .25f), LabGeometry.Ink, false);
                LabGeometry.Box(fallback, "Arm", new Vector3(side * .38f, 1.1f, 0), new Vector3(.18f, .6f, .2f), LabGeometry.Orange, false);
            }
            avatarRenderers.AddRange(avatar.GetComponentsInChildren<Renderer>());
            CharacterArtCatalog.SetLayer(avatar, 10);

            var anchorObject = new GameObject("Kinematic carry target");
            anchor = anchorObject.AddComponent<Rigidbody>();
            anchor.isKinematic = true;
            anchor.useGravity = false;
            var viewObject = new GameObject("Player camera");
            View = viewObject.AddComponent<Camera>();
            View.tag = "MainCamera";
            View.fieldOfView = 68;
            View.nearClipPlane = .05f;
            View.farClipPlane = 150;
            View.backgroundColor = new Color(.26f, .31f, .36f);
            View.cullingMask &= ~(1 << CharacterSelectionPreview.PreviewLayer);
            viewObject.AddComponent<AudioListener>();
            ResetAt(CarryLabRoom.PlayerSpawn);
            UpdateCamera(true);
        }

        public bool SelectOutfit(int index)
        {
            var id = CharacterArtCatalog.Players[index].Id;
            if (OutfitId == id && outfitModel != null) return true;
            var prefab = CharacterArtCatalog.Load(id);
            if (prefab == null) return false;
            if (outfitModel != null)
            {
                outfitModel.SetActive(false);
                Destroy(outfitModel);
            }
            placeholder.SetActive(false);
            outfitModel = Instantiate(prefab, avatar, false);
            CharacterArtCatalog.SetLayer(outfitModel.transform, 10);
            outfitPose = outfitModel.AddComponent<ProceduralCharacterPose>();
            outfitPose.Initialize();
            avatarRenderers.Clear();
            avatarRenderers.AddRange(outfitModel.GetComponentsInChildren<Renderer>());
            foreach (var renderer in avatarRenderers) renderer.enabled = !FirstPerson && !hidingView.HasValue;
            var skin = outfitModel.GetComponentInChildren<SkinnedMeshRenderer>();
            viewArms = skin != null && skin.sharedMesh.isReadable ? FirstPersonCarryArms.Create(skin) : null;
            OutfitId = id;
            return true;
        }

        void Update()
        {
            if (!InputEnabled) return;
            Yaw += Input.GetAxisRaw("Mouse X") * 2.4f;
            Pitch = Mathf.Clamp(Pitch - Input.GetAxisRaw("Mouse Y") * 2.4f, -55, 72);
            if (hidingView.HasValue || (Interactions != null && Interactions.Stashing))
            {
                if (Input.GetKeyDown(KeyCode.E) && Interactions != null) Interactions.Interact();
                return;
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                ToggleCamera();
            }
            SetCrouched(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.C));
            SetLift(Input.GetMouseButton(0));

            var raw = new Vector2(
                (Input.GetKey(KeyCode.D) ? 1 : 0) - (Input.GetKey(KeyCode.A) ? 1 : 0),
                (Input.GetKey(KeyCode.W) ? 1 : 0) - (Input.GetKey(KeyCode.S) ? 1 : 0));
            raw = Vector2.ClampMagnitude(raw, 1);
            var direction = Quaternion.Euler(0, Yaw, 0) * new Vector3(raw.x, 0, raw.y);
            CurrentSpeed = Holding ? DragSpeed : Crouched ? 1.7f : Input.GetKey(KeyCode.LeftShift) ? RunSpeed : WalkSpeed;
            if (controller.isGrounded && verticalSpeed < 0) verticalSpeed = -2;
            verticalSpeed = Mathf.Max(-25, verticalSpeed + Physics.gravity.y * Time.deltaTime);
            controller.Move((direction * CurrentSpeed + Vector3.up * verticalSpeed) * Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Q)) Release();
            if (Input.GetKeyDown(KeyCode.G) && Interactions != null)
                Interactions.ThrowRock(Quaternion.Euler(Pitch,Yaw,0)*Vector3.forward);
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (Interactions != null && Interactions.Interact()) return;
                if (Holding) Release();
                else if (TryFindGrip(out var grip)) Grab(grip);
            }
        }

        void FixedUpdate()
        {
            if (!Holding) return;
            var forward = Quaternion.Euler(0, Yaw, 0) * Vector3.forward;
            float height = Crouched ? (Lifted ? .73f : .44f) : Lifted ? 1.13f : .69f;
            var desired = transform.position + forward * (Lifted ? .49f : .47f) + Vector3.up * height;
            // Do not pull a limb through a closed wall when the player turns.
            var origin = transform.position + Vector3.up * .6f;
            if (Physics.Linecast(origin, desired, out var hit, WorldMask, QueryTriggerInteraction.Ignore))
                desired = hit.point + hit.normal * .15f;
            anchor.MovePosition(Vector3.MoveTowards(anchor.position, desired, 6f * Time.fixedDeltaTime));
        }

        void LateUpdate()
        {
            if (View == null || Time.deltaTime <= 0) return;
            var velocity = Vector3.ProjectOnPlane((transform.position - previousPosition) / Time.deltaTime, Vector3.up);
            previousPosition = transform.position;
            if (velocity.sqrMagnitude > .02f || Holding || FirstPerson)
            {
                float targetYaw = Holding || FirstPerson ? Yaw : Mathf.Atan2(velocity.x, velocity.z) * Mathf.Rad2Deg;
                avatar.rotation = Quaternion.Slerp(avatar.rotation, Quaternion.Euler(0, targetYaw, 0), 1 - Mathf.Exp(-17f * Time.deltaTime));
            }
            if (outfitPose != null)
            {
                outfitPose.SetMotion(velocity, Crouched, Holding, Lifted);
                if (Holding)
                {
                    var point = bodySkin != null ? bodySkin.VisualGripPoint(heldGrip) : heldGrip.WorldPoint;
                    outfitPose.SetGrip(point, heldGrip.Body.transform.TransformDirection(heldGrip.LocalPoint.normalized),
                        heldGrip.Label == "Head");
                }
            }
            else avatar.localPosition = new Vector3(0, Crouched ? -.48f : 0, 0);
            if (viewArms != null) viewArms.enabled = FirstPerson && (Holding || outfitPose.GripWeight > .01f);
            UpdateCamera(false);
        }

        void UpdateCamera(bool snap)
        {
            var rotation = Quaternion.Euler(Pitch, Yaw, 0);
            if (hidingView.HasValue)
            {
                View.transform.SetPositionAndRotation(hidingView.Value,rotation);
                return;
            }
            var pivot = transform.position + Vector3.up * (Crouched ? 1.05f : 1.62f);
            Vector3 desired;
            if (FirstPerson) desired = pivot + Quaternion.Euler(0, Yaw, 0) * Vector3.forward * .12f;
            else
            {
                var offset = rotation * new Vector3(.4f, .25f, -3.6f);
                float distance = offset.magnitude;
                if (Physics.SphereCast(pivot, .14f, offset.normalized, out var hit, distance, WorldMask, QueryTriggerInteraction.Ignore))
                    distance = Mathf.Max(.15f, hit.distance - .08f);
                desired = pivot + offset.normalized * distance;
            }
            // Collision has already bounded the target. Snap inward to avoid camera clipping.
            bool blocked = Physics.Linecast(pivot, View.transform.position, WorldMask, QueryTriggerInteraction.Ignore);
            View.transform.position = snap || blocked ? desired : Vector3.Lerp(View.transform.position, desired, 1 - Mathf.Exp(-20 * Time.deltaTime));
            View.transform.rotation = rotation;
        }

        internal void SetHiddenCamera(Vector3? position)
        {
            hidingView = position;
            foreach (var renderer in avatarRenderers) renderer.enabled = !FirstPerson && !hidingView.HasValue;
            if (View != null) UpdateCamera(true);
        }

        internal void SetCrouched(bool crouch)
        {
            if (Crouched == crouch) return;
            if (!crouch && Physics.CheckCapsule(transform.position + Vector3.up * 1.08f,
                    transform.position + Vector3.up * 1.5f, .27f, WorldMask, QueryTriggerInteraction.Ignore)) return;
            Crouched = crouch;
            controller.height = crouch ? 1.15f : 1.8f;
            controller.center = Vector3.up * controller.height * .5f;
        }

        internal void SetLift(bool lift) => Lifted = Holding && lift;
        internal ProceduralCharacterPose OutfitPose => outfitPose;
        internal bool CarryArmsVisible => viewArms != null && viewArms.enabled;

        public bool TryGetHeldGrip(out RagdollBody.Grip grip)
        {
            grip = heldGrip;
            return Holding && grip.Body != null;
        }

        public bool TryFindGrip(out RagdollBody.Grip chosen)
        {
            chosen = default;
            if (Body == null || View == null) return false;
            float best = float.MaxValue;
            bool found = false;
            foreach (var grip in Body.Grips)
            {
                var point = grip.WorldPoint;
                var eye = transform.position + Vector3.up * .9f;
                float distance = Vector3.Distance(eye, point);
                if (distance > GrabRange || Physics.Linecast(eye, point, WorldMask, QueryTriggerInteraction.Ignore)) continue;
                var screen = View.WorldToViewportPoint(point);
                if (screen.z < 0 || screen.x < -.1f || screen.x > 1.1f || screen.y < -.1f || screen.y > 1.1f) continue;
                float score = Vector2.Distance(new Vector2(screen.x, screen.y), new Vector2(.5f, .45f)) * 3 + distance;
                if (score >= best) continue;
                best = score;
                chosen = grip;
                found = true;
            }
            return found;
        }

        public bool Grab(RagdollBody.Grip grip)
        {
            if (Holding || grip.Body == null || Vector3.Distance(transform.position + Vector3.up * .9f, grip.WorldPoint) > GrabRange) return false;
            anchor.position = grip.WorldPoint;
            heldJoint = grip.Body.gameObject.AddComponent<SpringJoint>();
            heldJoint.autoConfigureConnectedAnchor = false;
            heldJoint.anchor = grip.LocalPoint;
            heldJoint.connectedBody = anchor;
            heldJoint.connectedAnchor = Vector3.zero;
            heldJoint.spring = 1350;
            heldJoint.damper = 95;
            heldJoint.minDistance = 0;
            heldJoint.maxDistance = .075f;
            heldJoint.tolerance = .018f;
            heldJoint.breakForce = 2400;
            heldJoint.enableCollision = false;
            heldGrip = grip;
            HeldLabel = grip.Label;
            return true;
        }

        public void Release()
        {
            if (heldJoint != null)
            {
                // Remove force immediately; Destroy is deferred to the frame boundary.
                heldJoint.spring = 0;
                heldJoint.damper = 0;
                Destroy(heldJoint);
            }
            heldJoint = null;
            heldGrip = default;
            HeldLabel = "";
            Lifted = false;
        }

        public void ToggleCamera()
        {
            FirstPerson = !FirstPerson;
            foreach (var renderer in avatarRenderers) renderer.enabled = !FirstPerson && !hidingView.HasValue;
            if (viewArms != null) viewArms.enabled = FirstPerson && Holding;
            if (View != null) UpdateCamera(true);
        }

        public void SetViewAngles(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = Mathf.Clamp(pitch, -55, 72);
            if (View != null) UpdateCamera(true);
        }

        public void ResetAt(Vector3 position)
        {
            Release();
            controller.enabled = false;
            transform.position = position;
            controller.enabled = true;
            verticalSpeed = 0;
            previousPosition = position;
            avatar.localPosition = Vector3.zero;
            avatar.localRotation = Quaternion.identity;
            if (outfitPose != null) outfitPose.ResetMotion();
            Yaw = 0;
            Pitch = 12;
            SetCrouched(false);
            if (anchor != null) anchor.position = position + Vector3.forward;
            if (View != null) UpdateCamera(true);
        }

        void OnDestroy()
        {
            Release();
            if (anchor != null) Destroy(anchor.gameObject);
            if (View != null) Destroy(View.gameObject);
        }
    }
}
