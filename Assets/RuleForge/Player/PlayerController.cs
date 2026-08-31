using RuleForge.Enemies;
using RuleForge.UI;
using UnityEngine;

namespace RuleForge.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerRuntimeStats))]
    public sealed class PlayerController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform viewTransform;

        private CharacterController characterController;
        private PlayerRuntimeStats runtimeStats;
        private PlayerHealth playerHealth;
        private float verticalVelocity;
        private float pitch;
        private Vector3 pendingCollisionSlide;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            runtimeStats = GetComponent<PlayerRuntimeStats>();
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void OnEnable()
        {
            SetCursorLocked(true);
        }

        private void OnDisable()
        {
            SetCursorLocked(false);
        }

        private void Update()
        {
            if (playerHealth != null && !playerHealth.IsAlive)
            {
                pendingCollisionSlide = Vector3.zero;
                SetCursorLocked(false);
                return;
            }

            if (RuntimeInputGate.IsBlocked)
            {
                pendingCollisionSlide = Vector3.zero;
                return;
            }

            UpdateLook();
            UpdateMovement();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetCursorLocked(false);
            }
            else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
            {
                SetCursorLocked(true);
            }
        }

        public void SetView(Transform newViewTransform)
        {
            viewTransform = newViewTransform;
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (runtimeStats == null ||
                hit.normal.y < 0.5f ||
                hit.collider.GetComponentInParent<EnemyController>() == null)
            {
                return;
            }

            Vector3 slideDirection = transform.position - hit.transform.position;
            slideDirection.y = 0f;
            if (slideDirection.sqrMagnitude < 0.01f)
            {
                slideDirection = -transform.forward;
            }

            pendingCollisionSlide = slideDirection.normalized *
                                    runtimeStats.EnemyTopSlideSpeedStat.FinalValue;
        }

        private void UpdateLook()
        {
            if (viewTransform == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            if (runtimeStats == null || !runtimeStats.IsConfigured)
            {
                return;
            }

            float lookSensitivity = runtimeStats.LookSensitivityStat.FinalValue;
            float mouseX = Input.GetAxis("Mouse X") * lookSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * lookSensitivity;

            transform.Rotate(Vector3.up * mouseX);
            float maximumLookAngle = runtimeStats.MaximumLookAngleStat.FinalValue;
            pitch = Mathf.Clamp(pitch - mouseY, -maximumLookAngle, maximumLookAngle);
            viewTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        private void UpdateMovement()
        {
            if (runtimeStats == null || !runtimeStats.IsConfigured)
            {
                return;
            }

            float groundedVerticalVelocity =
                runtimeStats.GroundedVerticalVelocityStat.FinalValue;
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = groundedVerticalVelocity;
            }

            Vector2 input = new Vector2(
                Input.GetAxisRaw("Horizontal"),
                Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 planarMovement =
                (transform.right * input.x + transform.forward * input.y) *
                runtimeStats.MoveSpeedStat.FinalValue;
            planarMovement += pendingCollisionSlide;
            pendingCollisionSlide = Vector3.zero;

            if (Input.GetButtonDown("Jump") && characterController.isGrounded)
            {
                float jumpHeight = runtimeStats.JumpHeightStat.FinalValue;
                float gravity = runtimeStats.GravityStat.FinalValue;
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            float gravityAcceleration = runtimeStats.GravityStat.FinalValue;
            verticalVelocity += gravityAcceleration * Time.deltaTime;
            Vector3 movement = planarMovement + Vector3.up * verticalVelocity;
            characterController.Move(movement * Time.deltaTime);
        }

        private static void SetCursorLocked(bool locked)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}
