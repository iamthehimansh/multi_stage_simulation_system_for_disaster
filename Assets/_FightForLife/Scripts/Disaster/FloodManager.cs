using UnityEngine;

namespace FightForLife.Disaster
{
    public class FloodManager : MonoBehaviour
    {
        public static FloodManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private FloodConfig config;

        [Header("Water")]
        [SerializeField] private Transform waterPlane;

        [Header("State")]
        [SerializeField] private FloodPhase currentPhase = FloodPhase.PreDisaster;
        [SerializeField] private float currentWaterLevel;
        [SerializeField] private float elapsedTime;

        public FloodPhase CurrentPhase => currentPhase;
        public float WaterLevel => currentWaterLevel;
        public float ElapsedTime => elapsedTime;

        public event System.Action<FloodPhase> OnPhaseChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (config != null)
                currentWaterLevel = config.initialWaterLevel;
        }

        private void Update()
        {
            if (config == null || waterPlane == null) return;

            elapsedTime += Time.deltaTime;
            UpdatePhase();
            UpdateWaterLevel();

            waterPlane.position = new Vector3(
                waterPlane.position.x,
                currentWaterLevel,
                waterPlane.position.z
            );
        }

        private void UpdatePhase()
        {
            FloodPhase newPhase = currentPhase;

            if (elapsedTime < config.phase1Duration)
                newPhase = FloodPhase.Warning;
            else if (elapsedTime < config.phase1Duration + config.phase2Duration)
                newPhase = FloodPhase.DisasterStrikes;
            else
                newPhase = FloodPhase.Escape;

            if (newPhase != currentPhase)
            {
                currentPhase = newPhase;
                OnPhaseChanged?.Invoke(currentPhase);
                Debug.Log($"[Flood] Phase changed to: {currentPhase}");
            }
        }

        private void UpdateWaterLevel()
        {
            float riseRate = currentPhase switch
            {
                FloodPhase.Warning => config.phase1RiseRate,
                FloodPhase.DisasterStrikes => config.phase2RiseRate,
                FloodPhase.Escape => config.phase3RiseRate,
                _ => 0f
            };

            // Check for surge
            float phase2Start = config.phase1Duration;
            float surgeStart = phase2Start + config.surgeMoment;
            float surgeEnd = surgeStart + config.surgeDuration;

            if (elapsedTime >= surgeStart && elapsedTime <= surgeEnd)
                riseRate = config.surgeRiseRate;

            currentWaterLevel += riseRate * (Time.deltaTime / 60f);
            currentWaterLevel = Mathf.Min(currentWaterLevel, config.peakWaterLevel + config.surgeAdditional);
        }

        public float GetCurrentStrength()
        {
            if (config == null) return 0f;

            return currentPhase switch
            {
                FloodPhase.Warning => config.phase1CurrentSpeed,
                FloodPhase.DisasterStrikes => config.phase2CurrentSpeed,
                FloodPhase.Escape => config.phase3CurrentSpeed,
                _ => 0f
            };
        }

        public Vector2 GetFlowDirection()
        {
            return config != null ? config.baseFlowDirection.normalized : Vector2.right;
        }
    }

    public enum FloodPhase
    {
        PreDisaster,
        Warning,
        DisasterStrikes,
        Escape
    }
}
