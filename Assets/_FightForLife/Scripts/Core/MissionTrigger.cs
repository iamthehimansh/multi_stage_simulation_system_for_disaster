using UnityEngine;
using FightForLife.Disaster;

namespace FightForLife.Core
{
    public enum MissionTriggerType
    {
        Enter,
        Interact,
        PhaseChange,
        Timer
    }

    public enum TriggerAction
    {
        ActivateMission,
        CompleteMission
    }

    public class MissionTrigger : MonoBehaviour
    {
        [Header("Mission Reference")]
        [SerializeField] private MissionData linkedMission;
        [SerializeField] private string missionId;

        [Header("Trigger Settings")]
        [SerializeField] private MissionTriggerType triggerType = MissionTriggerType.Enter;
        [SerializeField] private TriggerAction triggerAction = TriggerAction.ActivateMission;
        [SerializeField] private bool oneTime = true;
        [SerializeField] private bool isRepeatable;

        [Header("Phase Trigger")]
        [SerializeField] private FloodPhase requiredPhase;

        [Header("Timer Trigger")]
        [SerializeField] private float timerDelay = 10f;

        [Header("Linked Missions")]
        [SerializeField] private MissionTrigger[] nextTriggers;

        [Header("Marker")]
        [SerializeField] private GameObject availableMarker;   // ! marker
        [SerializeField] private GameObject inProgressMarker;  // ? marker

        private bool hasTriggered;
        private bool playerInZone;
        private float timerElapsed;
        private bool timerStarted;
        private FloodManager floodManager;

        private void Start()
        {
            floodManager = FindAnyObjectByType<FloodManager>();

            if (triggerType == MissionTriggerType.PhaseChange && floodManager != null)
            {
                floodManager.OnPhaseChanged += OnPhaseChanged;
            }

            UpdateMarkers();
        }

        private void OnDestroy()
        {
            if (floodManager != null)
                floodManager.OnPhaseChanged -= OnPhaseChanged;
        }

        private void Update()
        {
            if (hasTriggered && !isRepeatable) return;

            // Interact trigger: player presses E while in zone
            if (triggerType == MissionTriggerType.Interact && playerInZone)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    ExecuteTrigger();
                }
            }

            // Timer trigger
            if (triggerType == MissionTriggerType.Timer && timerStarted)
            {
                timerElapsed += Time.deltaTime;
                if (timerElapsed >= timerDelay)
                {
                    ExecuteTrigger();
                    timerStarted = false;
                }
            }

            UpdateMarkers();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            playerInZone = true;

            if (triggerType == MissionTriggerType.Enter)
            {
                ExecuteTrigger();
            }

            if (triggerType == MissionTriggerType.Timer && !timerStarted)
            {
                timerStarted = true;
                timerElapsed = 0f;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag("Player")) return;
            playerInZone = false;
        }

        private void OnPhaseChanged(FloodPhase newPhase)
        {
            if (triggerType != MissionTriggerType.PhaseChange) return;
            if (newPhase == requiredPhase)
            {
                ExecuteTrigger();
            }
        }

        private void ExecuteTrigger()
        {
            if (hasTriggered && oneTime && !isRepeatable) return;

            MissionManager mm = MissionManager.Instance;
            if (mm == null) return;

            MissionData mission = GetMission();
            if (mission == null) return;

            switch (triggerAction)
            {
                case TriggerAction.ActivateMission:
                    if (mission.status == MissionStatus.Locked || mission.status == MissionStatus.Available)
                    {
                        mm.StartMission(mission);
                    }
                    break;

                case TriggerAction.CompleteMission:
                    if (mission.status == MissionStatus.Active)
                    {
                        mm.CompleteMission(mission);
                        UnlockNextTriggers();
                    }
                    break;
            }

            hasTriggered = true;
        }

        private MissionData GetMission()
        {
            if (linkedMission != null) return linkedMission;

            // Try to find by ID from MissionManager
            if (!string.IsNullOrEmpty(missionId) && MissionManager.Instance != null)
            {
                var available = MissionManager.Instance.GetAvailableMissions();
                foreach (var m in available)
                {
                    if (m.missionId == missionId) return m;
                }

                var active = MissionManager.Instance.GetActiveMissions();
                foreach (var m in active)
                {
                    if (m.missionId == missionId) return m;
                }
            }

            return null;
        }

        private void UnlockNextTriggers()
        {
            if (nextTriggers == null) return;
            foreach (var trigger in nextTriggers)
            {
                if (trigger != null)
                    trigger.gameObject.SetActive(true);
            }
        }

        private void UpdateMarkers()
        {
            MissionData mission = GetMission();
            bool isAvailable = mission != null &&
                (mission.status == MissionStatus.Available || mission.status == MissionStatus.Locked);
            bool isActive = mission != null && mission.status == MissionStatus.Active;

            if (availableMarker != null)
                availableMarker.SetActive(isAvailable && !hasTriggered);

            if (inProgressMarker != null)
                inProgressMarker.SetActive(isActive);
        }

        public void ResetTrigger()
        {
            hasTriggered = false;
            timerStarted = false;
            timerElapsed = 0f;
        }
    }
}
