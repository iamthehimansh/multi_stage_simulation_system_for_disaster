using UnityEngine;
using UnityEngine.AI;
using FightForLife.Disaster;
using FightForLife.Player;

namespace FightForLife.NPC
{
    public enum NPCState
    {
        Normal,
        Alert,
        Panicking,
        Struggling,
        Drowning,
        Following,
        Trapped,
        Injured,
        Rescued,
        Dead
    }

    public enum NPCType
    {
        Adult,
        Child,
        Elderly,
        Disabled
    }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(AudioSource))]
    public class CivilianAI : MonoBehaviour, IInteractable
    {
        [Header("NPC Configuration")]
        [SerializeField] private NPCType npcType = NPCType.Adult;
        [SerializeField] private NPCState currentState = NPCState.Normal;

        [Header("Movement")]
        [SerializeField] private float baseSpeed = 3.5f;
        [SerializeField] private float panicSpeedMultiplier = 1.5f;
        [SerializeField] private float followDistance = 2f;
        [SerializeField] private float highGroundSearchRadius = 50f;

        [Header("Survival")]
        [SerializeField] private float struggleWaterDepth = 0.7f;
        [SerializeField] private float struggleDuration = 10f;
        [SerializeField] private float drowningDuration = 15f;

        [Header("Visual Indicator")]
        [SerializeField] private GameObject stateIndicator;
        [SerializeField] private float indicatorHeight = 2.2f;

        [Header("Audio")]
        [SerializeField] private AudioClip helpCallClip;
        [SerializeField] private AudioClip panicClip;
        [SerializeField] private AudioClip rescuedClip;
        [SerializeField] private float helpCallInterval = 5f;

        public NPCType Type => npcType;
        public NPCState CurrentState => currentState;
        public float HoldDuration => 0f;

        public event System.Action<CivilianAI, NPCState> OnStateChanged;

        // Set to true the first time the player successfully calms/rescues/frees this NPC,
        // so the rescue counter only ticks once per NPC even if state changes later.
        private bool _rescueCounted;
        public bool IsRescueCounted => _rescueCounted;
        public void MarkRescueCounted() { _rescueCounted = true; }

        private NavMeshAgent agent;
        private AudioSource audioSource;
        private Animator animator;
        private Renderer stateIndicatorRenderer;
        private Transform followTarget;
        private float timeInWater;
        private float timeStruggling;
        private float timeDrowning;
        private float lastHelpCallTime;
        private float currentWaterDepth;
        private bool needsMedicalItem;

        private static readonly int HashSpeed = Animator.StringToHash("Speed");
        private static readonly int HashMoveZ = Animator.StringToHash("MoveZ");
        private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            audioSource = GetComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
            audioSource.maxDistance = 30f;

            animator = GetComponentInChildren<Animator>();
            if (animator != null) animator.applyRootMotion = false;

            ApplyNPCTypeModifiers();
            CreateStateIndicator();
        }

        private void Start()
        {
            if (FloodManager.Instance != null)
            {
                FloodManager.Instance.OnPhaseChanged += OnFloodPhaseChanged;
            }
        }

        private void OnDestroy()
        {
            if (FloodManager.Instance != null)
            {
                FloodManager.Instance.OnPhaseChanged -= OnFloodPhaseChanged;
            }
        }

        private void Update()
        {
            if (currentState == NPCState.Rescued || currentState == NPCState.Dead)
                return;

            UpdateWaterDepth();
            UpdateStateMachine();
            UpdateStateIndicatorColor();
            UpdateHelpCalls();
            UpdateAnimation();
        }

        private void ApplyNPCTypeModifiers()
        {
            switch (npcType)
            {
                case NPCType.Child:
                    baseSpeed *= 0.7f;
                    struggleDuration *= 0.6f;
                    drowningDuration *= 0.7f;
                    struggleWaterDepth = 0.4f;
                    break;
                case NPCType.Elderly:
                    baseSpeed *= 0.6f;
                    struggleWaterDepth = 0.5f;
                    break;
                case NPCType.Disabled:
                    baseSpeed *= 0.5f;
                    struggleWaterDepth = 0.4f;
                    break;
            }

            agent.speed = baseSpeed;
        }

        private void CreateStateIndicator()
        {
            // Disabled - no floating sphere above NPC heads
        }

        private void UpdateStateIndicatorColor()
        {
            if (stateIndicatorRenderer == null) return;

            Color color = currentState switch
            {
                NPCState.Normal => Color.white,
                NPCState.Alert => new Color(1f, 0.65f, 0f), // Orange
                NPCState.Panicking => Color.yellow,
                NPCState.Struggling => new Color(1f, 0.5f, 0f),
                NPCState.Drowning => Color.red,
                NPCState.Following => Color.green,
                NPCState.Trapped => new Color(0.6f, 0.3f, 0f), // Brown
                NPCState.Injured => new Color(1f, 0.3f, 0.3f),
                NPCState.Rescued => Color.blue,
                NPCState.Dead => Color.black,
                _ => Color.white
            };

            stateIndicatorRenderer.material.color = color;
            stateIndicatorRenderer.material.SetColor("_EmissionColor", color * 0.5f);
        }

        private void UpdateWaterDepth()
        {
            if (FloodManager.Instance == null)
            {
                currentWaterDepth = 0f;
                return;
            }

            float waterLevel = FloodManager.Instance.WaterLevel;
            currentWaterDepth = waterLevel - transform.position.y;
            if (currentWaterDepth < 0f) currentWaterDepth = 0f;

            // Slow agent in water
            if (currentWaterDepth > 0.1f)
            {
                float waterPenalty = Mathf.Clamp01(currentWaterDepth / 2f);
                agent.speed = baseSpeed * (1f - waterPenalty * 0.6f);
            }
            else
            {
                agent.speed = baseSpeed;
            }

            if (currentWaterDepth > 0.1f)
                timeInWater += Time.deltaTime;
            else
                timeInWater = 0f;
        }

        private void UpdateStateMachine()
        {
            switch (currentState)
            {
                case NPCState.Normal:
                    UpdateNormalState();
                    break;
                case NPCState.Alert:
                    UpdateAlertState();
                    break;
                case NPCState.Panicking:
                    UpdatePanickingState();
                    break;
                case NPCState.Struggling:
                    UpdateStrugglingState();
                    break;
                case NPCState.Drowning:
                    UpdateDrowningState();
                    break;
                case NPCState.Following:
                    UpdateFollowingState();
                    break;
                case NPCState.Trapped:
                    break; // Wait for player interaction
                case NPCState.Injured:
                    break; // Wait for medical item
            }
        }

        private void UpdateNormalState()
        {
            // Wander around slowly
            if (!agent.hasPath || agent.remainingDistance < 0.5f)
            {
                Vector3 randomPoint = transform.position + Random.insideUnitSphere * 10f;
                if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
                {
                    if (IsOnTerrain(hit.position))
                        agent.SetDestination(hit.position);
                }
            }

            // Transition to Alert if water nearby
            if (currentWaterDepth > 0.05f)
            {
                SetState(NPCState.Alert);
            }
        }

        private void UpdateAlertState()
        {
            // Look around, slow movement
            agent.speed = baseSpeed * 0.5f;

            if (FloodManager.Instance != null &&
                FloodManager.Instance.CurrentPhase >= FloodPhase.DisasterStrikes)
            {
                SetState(NPCState.Panicking);
                return;
            }

            if (currentWaterDepth > 0.2f)
            {
                SetState(NPCState.Panicking);
            }
        }

        private void UpdatePanickingState()
        {
            agent.speed = baseSpeed * panicSpeedMultiplier;

            // Run to high ground
            if (!agent.hasPath || agent.remainingDistance < 1f)
            {
                Vector3 highGround = FindHighGround();
                if (highGround != Vector3.zero)
                {
                    agent.SetDestination(highGround);
                }
            }

            // Transition to Struggling
            if (currentWaterDepth > struggleWaterDepth)
            {
                SetState(NPCState.Struggling);
            }
        }

        private void UpdateStrugglingState()
        {
            agent.speed = baseSpeed * 0.3f;
            timeStruggling += Time.deltaTime;

            // Try to move to nearest shallow area
            if (!agent.hasPath || agent.remainingDistance < 1f)
            {
                Vector3 highGround = FindHighGround();
                if (highGround != Vector3.zero)
                {
                    agent.SetDestination(highGround);
                }
            }

            // Return to panicking if water recedes
            if (currentWaterDepth < struggleWaterDepth * 0.5f)
            {
                timeStruggling = 0f;
                SetState(NPCState.Panicking);
                return;
            }

            // Transition to Drowning
            if (timeStruggling >= struggleDuration)
            {
                SetState(NPCState.Drowning);
            }
        }

        private void UpdateDrowningState()
        {
            agent.isStopped = true;
            timeDrowning += Time.deltaTime;

            if (timeDrowning >= drowningDuration)
            {
                SetState(NPCState.Dead);
            }
        }

        private void UpdateFollowingState()
        {
            if (followTarget == null)
            {
                SetState(NPCState.Panicking);
                return;
            }

            agent.isStopped = false;
            float distToTarget = Vector3.Distance(transform.position, followTarget.position);

            if (distToTarget > followDistance)
            {
                agent.SetDestination(followTarget.position);
                agent.speed = baseSpeed * 1.2f;
            }
            else
            {
                agent.ResetPath();
            }

            // If water gets too deep while following, struggle
            if (currentWaterDepth > struggleWaterDepth * 1.5f &&
                npcType != NPCType.Adult)
            {
                SetState(NPCState.Struggling);
            }
        }

        private Vector3 FindHighGround()
        {
            Vector3 bestPoint = Vector3.zero;
            float bestHeight = transform.position.y;

            int sampleCount = 20;
            for (int i = 0; i < sampleCount; i++)
            {
                Vector3 randomDir = Random.insideUnitSphere * highGroundSearchRadius;
                randomDir.y = 0f;
                Vector3 samplePos = transform.position + randomDir;

                if (NavMesh.SamplePosition(samplePos, out NavMeshHit hit, highGroundSearchRadius, NavMesh.AllAreas))
                {
                    // Reject points that sit on top of a building / non-terrain surface.
                    if (!IsOnTerrain(hit.position)) continue;
                    if (hit.position.y > bestHeight)
                    {
                        bestHeight = hit.position.y;
                        bestPoint = hit.position;
                    }
                }
            }

            return bestPoint;
        }

        private static bool IsOnTerrain(Vector3 point)
        {
            // Raycast down from just above the point. If the first thing we hit
            // is named like a building, the navmesh point is on a roof, not terrain.
            Vector3 origin = point + Vector3.up * 0.6f;
            RaycastHit h;
            if (!Physics.Raycast(origin, Vector3.down, out h, 5f, ~0, QueryTriggerInteraction.Ignore))
                return true; // nothing under it -- treat as terrain
            string n = h.collider.gameObject.name.ToLowerInvariant();
            if (n.Contains("house") || n.Contains("temple") || n.Contains("barn") ||
                n.Contains("hut") || n.Contains("cabin") || n.Contains("shed") ||
                n.Contains("tower") || n.Contains("wall") || n.Contains("roof") ||
                n.Contains("mayor") || n.Contains("building") || n.Contains("struct"))
                return false;
            return true;
        }

        private void SetState(NPCState newState)
        {
            if (currentState == newState) return;
            if (currentState == NPCState.Dead || currentState == NPCState.Rescued) return;

            NPCState oldState = currentState;
            currentState = newState;

            // Reset state-specific timers
            switch (newState)
            {
                case NPCState.Struggling:
                    timeStruggling = 0f;
                    break;
                case NPCState.Drowning:
                    timeDrowning = 0f;
                    agent.isStopped = true;
                    break;
                case NPCState.Dead:
                    agent.isStopped = true;
                    agent.enabled = false;
                    break;
                case NPCState.Rescued:
                    agent.isStopped = true;
                    if (rescuedClip != null)
                        audioSource.PlayOneShot(rescuedClip);
                    break;
                case NPCState.Following:
                    agent.isStopped = false;
                    break;
            }

            OnStateChanged?.Invoke(this, newState);
            Debug.Log($"[NPC] {gameObject.name} state: {oldState} -> {newState}");
        }

        private void UpdateAnimation()
        {
            if (animator == null) return;

            float speed = agent.enabled && !agent.isStopped ? agent.velocity.magnitude : 0f;
            // Snap to zero when barely moving to prevent idle walking animation
            float moveZ = speed > 0.2f ? 1f : 0f;
            animator.SetFloat(HashSpeed, speed > 0.2f ? speed : 0f, 0.05f, Time.deltaTime);
            animator.SetFloat(HashMoveZ, moveZ, 0.05f, Time.deltaTime);
            animator.SetBool(HashIsGrounded, true);
        }

        private void UpdateHelpCalls()
        {
            bool shouldCallForHelp = currentState == NPCState.Struggling ||
                                     currentState == NPCState.Drowning ||
                                     currentState == NPCState.Trapped ||
                                     currentState == NPCState.Injured;

            if (!shouldCallForHelp) return;

            if (Time.time - lastHelpCallTime >= helpCallInterval)
            {
                lastHelpCallTime = Time.time;
                if (helpCallClip != null && !audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(helpCallClip);
                }
            }
        }

        private void OnFloodPhaseChanged(FloodPhase newPhase)
        {
            switch (newPhase)
            {
                case FloodPhase.Warning:
                    if (currentState == NPCState.Normal)
                        SetState(NPCState.Alert);
                    break;
                case FloodPhase.DisasterStrikes:
                    if (currentState == NPCState.Normal || currentState == NPCState.Alert)
                        SetState(NPCState.Panicking);
                    break;
            }
        }

        // --- IInteractable Implementation ---

        public string GetPrompt()
        {
            return GetInteractionPrompt();
        }

        public bool CanInteract()
        {
            return currentState == NPCState.Drowning ||
                   currentState == NPCState.Struggling ||
                   currentState == NPCState.Panicking ||
                   currentState == NPCState.Alert ||
                   currentState == NPCState.Trapped ||
                   currentState == NPCState.Injured;
        }

        public void Interact(GameObject interactor)
        {
            switch (currentState)
            {
                case NPCState.Drowning:
                case NPCState.Struggling:
                    Rescue(interactor.transform);
                    break;
                case NPCState.Panicking:
                case NPCState.Alert:
                    Calm(interactor.transform);
                    break;
                case NPCState.Trapped:
                    Free(interactor.transform);
                    break;
                case NPCState.Injured:
                    // Requires medical item - caller should check inventory
                    needsMedicalItem = true;
                    break;
            }
        }

        public void Rescue(Transform rescuer)
        {
            followTarget = rescuer;
            SetState(NPCState.Following);
            CountRescueOnce();
        }

        public void Calm(Transform calmer)
        {
            followTarget = calmer;
            SetState(NPCState.Following);
            CountRescueOnce();
        }

        public void Free(Transform freer)
        {
            followTarget = freer;
            SetState(NPCState.Following);
            CountRescueOnce();
        }

        private void CountRescueOnce()
        {
            if (_rescueCounted) return;
            _rescueCounted = true;
            var spawner = NPCSpawner.Instance;
            if (spawner == null)
            {
                // Fallback: singleton backing field can be null after scene reload
                // even when an active spawner exists in the scene. Find it directly.
                spawner = Object.FindObjectOfType<NPCSpawner>();
                if (spawner != null)
                    NPCSpawner.SetInstance(spawner);
            }
            if (spawner != null)
            {
                spawner.NotifyRescue(this);
            }
            else
            {
                // Last-resort: still tick score + missions so player effort isn't lost
                if (FightForLife.Core.ScoreManager.Instance != null)
                    FightForLife.Core.ScoreManager.Instance.RescueCivilian();
                if (FightForLife.Core.MissionManager.Instance != null)
                {
                    FightForLife.Core.MissionManager.Instance.UpdateObjective("VM02", "rescue_families");
                    FightForLife.Core.MissionManager.Instance.UpdateObjective("VM03", "guide_across");
                }
            }
        }

        public void HealWithMedicalItem()
        {
            if (currentState != NPCState.Injured) return;

            needsMedicalItem = false;
            SetState(NPCState.Following);
        }

        public void SetTrapped()
        {
            SetState(NPCState.Trapped);
            agent.isStopped = true;
        }

        public void SetInjured()
        {
            SetState(NPCState.Injured);
            needsMedicalItem = true;
            agent.speed = baseSpeed * 0.2f;
        }

        public void MarkRescued()
        {
            SetState(NPCState.Rescued);
        }

        public bool NeedsMedicalItem => needsMedicalItem;
        public float WaterDepth => currentWaterDepth;

        private string GetInteractionPrompt()
        {
            return currentState switch
            {
                NPCState.Drowning => "[E] Rescue",
                NPCState.Struggling => "[E] Help",
                NPCState.Panicking => "[E] Calm Down",
                NPCState.Alert => "[E] Guide",
                NPCState.Trapped => "[E] Free",
                NPCState.Injured => needsMedicalItem ? "[F] Use Medical Item" : "[E] Help",
                _ => ""
            };
        }
    }
}
