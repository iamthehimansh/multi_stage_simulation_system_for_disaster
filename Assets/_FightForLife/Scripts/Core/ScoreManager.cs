using UnityEngine;

namespace FightForLife.Core
{
    [DefaultExecutionOrder(-80)]
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        [Header("Score")]
        [SerializeField] private int currentScore;
        [SerializeField] private int civiliansRescued;
        [SerializeField] private int totalCivilians;
        [SerializeField] private int missionsCompleted;

        public int CurrentScore => currentScore;
        public int CiviliansRescued => civiliansRescued;
        public int TotalCivilians => totalCivilians;
        public int MissionsCompleted => missionsCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void AddScore(int points)
        {
            currentScore += points;
        }

        public void RescueCivilian()
        {
            civiliansRescued++;
            AddScore(200);
        }

        public void CompleteMission(int bonusPoints)
        {
            missionsCompleted++;
            AddScore(bonusPoints);
        }

        public string GetRating()
        {
            if (totalCivilians == 0) return "B";

            float rescuePercent = (float)civiliansRescued / totalCivilians;

            if (rescuePercent >= 0.95f) return "S";
            if (rescuePercent >= 0.75f) return "A";
            if (rescuePercent >= 0.50f) return "B";
            if (rescuePercent >= 0.25f) return "C";
            return "D";
        }

        public void ResetScore()
        {
            currentScore = 0;
            civiliansRescued = 0;
            missionsCompleted = 0;
        }
    }
}
