using UnityEngine;
using System;
using System.Collections.Generic;
using FightForLife.Disaster;

namespace FightForLife.Core
{
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [SerializeField] private List<MissionData> allMissions = new List<MissionData>();
        [SerializeField] private MissionData activeMission;

        private List<MissionData> completedMissions = new List<MissionData>();
        private List<MissionData> failedMissions = new List<MissionData>();
        private Dictionary<MissionData, float> activeMissionTimers = new Dictionary<MissionData, float>();

        public MissionData ActiveMission => activeMission;
        public List<MissionData> CompletedMissions => completedMissions;

        // Events
        public event Action<MissionData> OnMissionStarted;
        public event Action<MissionData> OnMissionCompleted;
        public event Action<MissionData> OnMissionFailed;

        private FloodManager floodManager;

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
            floodManager = FindAnyObjectByType<FloodManager>();

            if (floodManager != null)
                floodManager.OnPhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (floodManager != null)
                floodManager.OnPhaseChanged -= OnPhaseChanged;
        }

        private void Update()
        {
            UpdateMissionTimers();
        }

        #region Mission Lifecycle

        public void StartMission(MissionData mission)
        {
            mission.status = MissionStatus.Active;
            activeMission = mission;

            // Start timer if mission has a time limit
            if (mission.timeLimit > 0f)
            {
                activeMissionTimers[mission] = mission.timeLimit;
            }

            Debug.Log($"Mission Started: {mission.missionName}");
            OnMissionStarted?.Invoke(mission);
        }

        public void CompleteMission(MissionData mission)
        {
            mission.status = MissionStatus.Completed;
            completedMissions.Add(mission);
            activeMissionTimers.Remove(mission);

            if (activeMission == mission)
                activeMission = null;

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.CompleteMission(mission.rewardPoints);

            Debug.Log($"Mission Completed: {mission.missionName} (+{mission.rewardPoints} pts)");
            OnMissionCompleted?.Invoke(mission);
        }

        public void FailMission(MissionData mission)
        {
            mission.status = MissionStatus.Failed;
            failedMissions.Add(mission);
            activeMissionTimers.Remove(mission);

            if (activeMission == mission)
                activeMission = null;

            Debug.Log($"Mission Failed: {mission.missionName}");
            OnMissionFailed?.Invoke(mission);
        }

        #endregion

        #region Queries

        public List<MissionData> GetAvailableMissions()
        {
            List<MissionData> available = new List<MissionData>();
            foreach (var mission in allMissions)
            {
                if (mission.status == MissionStatus.Available)
                    available.Add(mission);
            }
            return available;
        }

        public List<MissionData> GetActiveMissions()
        {
            List<MissionData> active = new List<MissionData>();
            foreach (var mission in allMissions)
            {
                if (mission.status == MissionStatus.Active)
                    active.Add(mission);
            }
            return active;
        }

        public MissionData FindMissionById(string id)
        {
            foreach (var mission in allMissions)
            {
                if (mission.missionId == id)
                    return mission;
            }
            return null;
        }

        public void UpdateObjective(string missionId, string objectiveId, int increment = 1)
        {
            var mission = FindMissionById(missionId);
            if (mission == null || mission.status != MissionStatus.Active) return;

            foreach (var obj in mission.objectives)
            {
                if (obj.objectiveId == objectiveId)
                {
                    obj.currentCount = Mathf.Min(obj.currentCount + increment, obj.requiredCount);
                    Debug.Log($"[Mission] {mission.missionName}: {obj.description} ({obj.currentCount}/{obj.requiredCount})");

                    if (!mission.parallelObjectives && obj.isComplete)
                        mission.currentObjectiveIndex++;

                    if (mission.AllObjectivesComplete())
                        CompleteMission(mission);
                    break;
                }
            }
        }

        #endregion

        #region Phase-Based Activation

        public void ActivateMissionsByPhase(FloodPhase phase)
        {
            int phaseIndex = (int)phase;
            foreach (var mission in allMissions)
            {
                if (mission.disasterPhase == phaseIndex &&
                    (mission.status == MissionStatus.Locked || mission.status == MissionStatus.Available))
                {
                    // Check expert-only restriction
                    if (mission.expertOnly && GameManager.Instance != null &&
                        GameManager.Instance.SelectedRole != PlayerRole.DisasterManagementExpert)
                    {
                        continue;
                    }

                    mission.status = MissionStatus.Available;
                    Debug.Log($"Mission available: {mission.missionName} (Phase: {phase})");
                }
            }
        }

        private void OnPhaseChanged(FloodPhase newPhase)
        {
            ActivateMissionsByPhase(newPhase);
        }

        #endregion

        #region Timers

        private void UpdateMissionTimers()
        {
            if (activeMissionTimers.Count == 0) return;

            // Copy keys to avoid modification during iteration
            var missions = new List<MissionData>(activeMissionTimers.Keys);

            foreach (var mission in missions)
            {
                if (!activeMissionTimers.ContainsKey(mission)) continue;

                activeMissionTimers[mission] -= Time.deltaTime;

                if (activeMissionTimers[mission] <= 0f)
                {
                    FailMission(mission);
                }
            }
        }

        public float GetMissionTimeRemaining(MissionData mission)
        {
            if (activeMissionTimers.TryGetValue(mission, out float remaining))
                return remaining;
            return -1f;
        }

        #endregion

        #region Reset

        public void ResetAllMissions()
        {
            foreach (var mission in allMissions)
            {
                mission.status = MissionStatus.Locked;
            }

            completedMissions.Clear();
            failedMissions.Clear();
            activeMissionTimers.Clear();
            activeMission = null;
        }

        #endregion
    }

    [System.Serializable]
    public class MissionData
    {
        public string missionId;
        public string missionName;
        public string description;
        public MissionType type;
        public MissionStatus status;
        public int rewardPoints;
        public float timeLimit;
        public int disasterPhase;
        public bool expertOnly;

        // Objective tracking
        public List<ObjectiveData> objectives = new List<ObjectiveData>();
        public bool parallelObjectives = true;
        public int currentObjectiveIndex;

        // Waypoint for HUD marker
        public Vector3 waypointPosition;
        public string waypointLabel;

        public ObjectiveData GetActiveObjective()
        {
            if (objectives.Count == 0) return null;
            if (parallelObjectives)
            {
                foreach (var obj in objectives)
                    if (!obj.isComplete) return obj;
                return null;
            }
            if (currentObjectiveIndex < objectives.Count)
                return objectives[currentObjectiveIndex];
            return null;
        }

        public bool AllObjectivesComplete()
        {
            if (objectives.Count == 0) return false;
            foreach (var obj in objectives)
                if (!obj.isComplete) return false;
            return true;
        }
    }

    [System.Serializable]
    public class ObjectiveData
    {
        public string objectiveId;
        public string description;
        public int requiredCount = 1;
        public int currentCount;
        public bool isComplete => currentCount >= requiredCount;
    }

    public enum MissionType
    {
        Primary,
        Secondary,
        ExpertExclusive,
        Hidden
    }

    public enum MissionStatus
    {
        Locked,
        Available,
        Active,
        Completed,
        Failed
    }
}
