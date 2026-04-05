using UnityEngine;
using System.Collections.Generic;

namespace FightForLife.Core
{
    public class MissionManager : MonoBehaviour
    {
        public static MissionManager Instance { get; private set; }

        [SerializeField] private List<MissionData> allMissions = new List<MissionData>();
        [SerializeField] private MissionData activeMission;

        private List<MissionData> completedMissions = new List<MissionData>();
        private List<MissionData> failedMissions = new List<MissionData>();

        public MissionData ActiveMission => activeMission;
        public List<MissionData> CompletedMissions => completedMissions;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void StartMission(MissionData mission)
        {
            activeMission = mission;
            mission.status = MissionStatus.Active;
            Debug.Log($"Mission Started: {mission.missionName}");
        }

        public void CompleteMission(MissionData mission)
        {
            mission.status = MissionStatus.Completed;
            completedMissions.Add(mission);

            if (activeMission == mission)
                activeMission = null;

            if (ScoreManager.Instance != null)
                ScoreManager.Instance.CompleteMission(mission.rewardPoints);

            Debug.Log($"Mission Completed: {mission.missionName} (+{mission.rewardPoints} pts)");
        }

        public void FailMission(MissionData mission)
        {
            mission.status = MissionStatus.Failed;
            failedMissions.Add(mission);

            if (activeMission == mission)
                activeMission = null;

            Debug.Log($"Mission Failed: {mission.missionName}");
        }
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
