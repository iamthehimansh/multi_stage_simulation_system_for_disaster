using UnityEngine;

namespace FightForLife.Disaster
{
    [CreateAssetMenu(fileName = "FloodConfig", menuName = "Fight For Life/Flood Config")]
    public class FloodConfig : ScriptableObject
    {
        [Header("Water Levels (meters)")]
        public float initialWaterLevel = -2f;
        public float phase1WaterLevel = 0f;
        public float phase2WaterLevel = 2f;
        public float peakWaterLevel = 4f;
        public float surgeAdditional = 1.5f;

        [Header("Rise Rates (meters per minute)")]
        public float phase1RiseRate = 0.05f;
        public float phase2RiseRate = 0.2f;
        public float surgeRiseRate = 0.75f;
        public float phase3RiseRate = 0f;

        [Header("Current")]
        public Vector2 baseFlowDirection = new Vector2(1, 0);
        public float phase1CurrentSpeed = 0.5f;
        public float phase2CurrentSpeed = 3f;
        public float surgeCurrentSpeed = 6f;
        public float phase3CurrentSpeed = 2f;

        [Header("Timing (seconds)")]
        public float phase1Duration = 480f;
        public float phase2Duration = 900f;
        public float surgeMoment = 720f;
        public float surgeDuration = 120f;
        public float phase3Duration = 480f;

        [Header("Visuals")]
        public Gradient waterColorOverTime;
        public AnimationCurve turbidityOverTime = AnimationCurve.Linear(0, 0, 1, 1);
        public AnimationCurve waveAmplitudeOverTime = AnimationCurve.Linear(0, 0.1f, 1, 1f);
        public AnimationCurve foamIntensityOverTime = AnimationCurve.Linear(0, 0, 1, 0.8f);

        [Header("Damage")]
        public float drowningDamagePerSecond = 10f;
        public float hypothermiaDamagePerSecond = 2f;
        public float hypothermiaOnsetTime = 180f;
        public float currentDamageMultiplier = 5f;
        public float debrisImpactDamage = 25f;

        [Header("Structural")]
        public float structureWeakeningRate = 0.01f;
        public float collapseThreshold = 0.8f;
        public float mudHouseWeakMultiplier = 3f;
    }
}
