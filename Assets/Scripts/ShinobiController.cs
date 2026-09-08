using UnityEngine;

namespace Konohagakure
{
    /// <summary>
    /// Player-controlled locomotion for the Shinobi character. Reads WASD/arrow input and
    /// Space for jump, moves via CharacterController, and drives the Animator's Speed (float,
    /// -1 backward / 0 idle / 1 walk / 2 run) and Strafe (float, -1 left / 1 right) parameters
    /// that feed a 2D Freeform Directional blend tree. Footstep/jump SFX are fired via Animation
    /// Events calling back into this script, keeping audio frame-accurate to the animation.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Animator))]
    public class ShinobiController : MonoBehaviour
    {
        [Header("Movement (meters/sec)")]
        [SerializeField] private float walkMetersPerSec = 2.5f;
        [SerializeField] private float runMetersPerSec = 6f;
        [SerializeField] private float strafeMetersPerSec = 2.5f;
        [SerializeField] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float rotationSpeed = 10f;
        [SerializeField] private float blendDamping = 20f; // how quickly Speed/Strafe ease toward their target

        [Header("Animator Params")]
        [SerializeField] private string speedParam = "Speed";
        [SerializeField] private string strafeParam = "Strafe";
        [SerializeField] private string jumpParam = "Jump";
        [SerializeField] private string groundedParam = "IsGrounded";

        private CharacterController controller;
        private Animator animator;
        private Camera mainCamera;

        private Vector3 velocity;
        private float currentSpeed;   // signed: -1..2, matches blend tree Y axis
        private float currentStrafe;  // signed: -1..1, matches blend tree X axis
        private bool inputEnabled = true;

        /// <summary>Current signed Speed value (-2 run backward, -1 walk backward, 0 idle, 1 walk, matches blend tree Y).</summary>
        public float CurrentSpeed => currentSpeed;

        /// <summary>Current signed Strafe value (-1 left, 1 right, matches blend tree X).</summary>
        public float CurrentStrafe => currentStrafe;

        /// <summary>True while the CharacterController is touching the ground.</summary>
        public bool IsGrounded => controller.isGrounded;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            animator = GetComponent<Animator>();
            mainCamera = Camera.main;
        }

        private void Update()
        {
            if (!inputEnabled)
            {
                ApplyGravityOnly();
                return;
            }

            HandleMovement();
            HandleJump();
        }

        private void HandleMovement()
        {
            float h = Input.GetAxis("Horizontal"); // A/D — strafe axis
            float v = Input.GetAxis("Vertical");   // W/S — forward/back axis
            bool wantsRun = Input.GetKey(KeyCode.LeftShift);

            // Target Speed: forward is walk-only; backward has both walk and run tiers.
            float targetSpeed = 0f;
            if (v > 0.1f) targetSpeed = 1f;
            else if (v < -0.1f) targetSpeed = wantsRun ? -2f : -1f;

            float targetStrafe = Mathf.Abs(h) > 0.1f ? Mathf.Sign(h) * Mathf.Clamp01(Mathf.Abs(h)) : 0f;

            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, blendDamping * Time.deltaTime);
            currentStrafe = Mathf.Lerp(currentStrafe, targetStrafe, blendDamping * Time.deltaTime);

            // Face the camera's forward direction while there's movement input. Rotating only
            // while moving (not constantly) avoids a feedback loop with a camera that follows
            // the character's facing.
            Vector3 camForward = mainCamera != null ? Vector3.Scale(mainCamera.transform.forward, new Vector3(1, 0, 1)).normalized : Vector3.forward;
            Vector3 camRight = mainCamera != null ? mainCamera.transform.right : Vector3.right;

            if (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(camForward);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
            }

            // Convert the signed animator values into an actual world-space move vector.
            float forwardMps = currentSpeed switch
            {
                >= 0.5f => walkMetersPerSec,
                <= -1.5f => -runMetersPerSec,
                <= -0.1f => -walkMetersPerSec,
                _ => 0f
            };
            float strafeMps = currentStrafe * strafeMetersPerSec;
            Vector3 planarVelocity = camForward * forwardMps + camRight * strafeMps;

            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;

            Vector3 motion = planarVelocity + Vector3.up * velocity.y;
            controller.Move(motion * Time.deltaTime);

            animator.SetFloat(speedParam, currentSpeed);
            animator.SetFloat(strafeParam, currentStrafe);
            animator.SetBool(groundedParam, controller.isGrounded);
        }

        private void HandleJump()
        {
            if (Input.GetButtonDown("Jump") && controller.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                animator.SetTrigger(jumpParam);
            }
        }

        private void ApplyGravityOnly()
        {
            if (controller.isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
            velocity.y += gravity * Time.deltaTime;
            controller.Move(Vector3.up * velocity.y * Time.deltaTime);
            currentSpeed = 0f;
            currentStrafe = 0f;
            animator.SetFloat(speedParam, 0f);
            animator.SetFloat(strafeParam, 0f);
        }

        /// <summary>Called by KonohaManager to lock/unlock player input between drills.</summary>
        public void SetInputEnabled(bool enabled)
        {
            inputEnabled = enabled;
            if (!enabled)
            {
                currentSpeed = 0f;
                currentStrafe = 0f;
                animator.SetFloat(speedParam, 0f);
                animator.SetFloat(strafeParam, 0f);
            }
        }

        /// <summary>Called by KonohaManager at the start of a drill to place the Shinobi at the course start.</summary>
        public void ResetToStart(Vector3 position, Quaternion rotation)
        {
            controller.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            controller.enabled = true;
            velocity = Vector3.zero;
            currentSpeed = 0f;
            currentStrafe = 0f;
        }

        // --- Animation Event callbacks (hook these to the Walk/Run/Jump clips in the Animator) ---

        public void AnimEvent_Footstep()
        {
            AudioManager.Instance.PlayCue(AudioManager.CueType.Footstep);
        }

        public void AnimEvent_JumpSound()
        {
            AudioManager.Instance.PlayCue(AudioManager.CueType.Jump);
        }
    }
}