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

            // Use raw input from PlayerController (not transformed by camera)
            // This gives the correct directional animation: W=forward, A=left, D=right
            float inputX = pc.InputX;
            float inputZ = pc.InputZ;

            Vector2 input = new Vector2(inputX, inputZ);
            if (input.magnitude > 1f) input.Normalize();

            // Dead zone - snap to zero when input is negligible to prevent idle walking
            if (input.magnitude < 0.05f)
                input = Vector2.zero;

            animator.SetFloat(HashMoveX, input.x, dampTime, Time.deltaTime);
            animator.SetFloat(HashMoveZ, input.y, dampTime, Time.deltaTime);
            animator.SetFloat(HashSpeed, input.magnitude, dampTime, Time.deltaTime);
            animator.SetBool(HashIsGrounded, pc.IsGrounded);
            animator.SetBool(HashIsCrouching, pc.IsCrouching);
        }
    }
}
