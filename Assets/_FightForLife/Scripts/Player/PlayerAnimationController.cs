using UnityEngine;

namespace FightForLife.Player
{
    [RequireComponent(typeof(PlayerController))]
    public class PlayerAnimationController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Animator animator;

        [Header("Smoothing")]
        [SerializeField] private float dampTime = 0.05f;

        private static readonly int HashMoveX = Animator.StringToHash("MoveX");
        private static readonly int HashMoveZ = Animator.StringToHash("MoveZ");
        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int HashIsCrouching = Animator.StringToHash("IsCrouching");
        private static readonly int HashIsSwimming = Animator.StringToHash("IsSwimming");

        private PlayerController pc;

        private void Awake()
        {
            pc = GetComponent<PlayerController>();
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            if (animator != null)
            {
                animator.applyRootMotion = false;
                // Always animate even when off-screen / camera clipping into mesh,
                // otherwise the walk cycle freezes when colliding with walls.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            }
        }

        private void Update()
        {
            if (animator == null || pc == null) return;

            // Use actual movement speed to drive animations.
            // This ensures the player stops animating when blocked by a wall/tree.
            float actualSpeed = pc.CurrentSpeed;
            bool isActuallyMoving = actualSpeed > 0.15f;

            float inputX = pc.InputX;
            float inputZ = pc.InputZ;

            Vector2 input = new Vector2(inputX, inputZ);
            if (input.magnitude > 1f) input.Normalize();

            // If blocked by collision (input but no movement), zero out animation
            if (!isActuallyMoving)
                input = Vector2.zero;

            animator.SetFloat(HashMoveX, input.x, dampTime, Time.deltaTime);
            animator.SetFloat(HashMoveZ, input.y, dampTime, Time.deltaTime);
            animator.SetFloat(HashSpeed, isActuallyMoving ? input.magnitude : 0f, dampTime, Time.deltaTime);
            // In water (wading or swimming) → treat as swimming for animation
            // In water (wading) → force grounded so Jump doesn't trigger
            bool inWater = pc.IsSwimming || pc.IsDiving || pc.IsWading;
            animator.SetBool(HashIsSwimming, inWater);
            animator.SetBool(HashIsGrounded, inWater || pc.IsGrounded);
            animator.SetBool(HashIsCrouching, pc.IsCrouching);
        }
    }
}
