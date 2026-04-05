using UnityEngine;

namespace FightForLife.Disaster
{
    public class WeatherSystem : MonoBehaviour
    {
        public static WeatherSystem Instance { get; private set; }

        [Header("Rain")]
        [SerializeField] private ParticleSystem rainParticleSystem;
        [SerializeField] private float minRainRate = 100f;
        [SerializeField] private float maxRainRate = 5000f;

        [Header("Lightning")]
        [SerializeField] private Light lightningLight;
        [SerializeField] private AudioSource lightningAudioSource;
        [SerializeField] private AudioClip[] thunderClips;
        [SerializeField] private float minLightningInterval = 10f;
        [SerializeField] private float maxLightningInterval = 45f;
        [SerializeField] private float lightningFlashDuration = 0.15f;
        [SerializeField] private float lightningIntensity = 3f;

        [Header("Fog")]
        [SerializeField] private float minFogDensity = 0.002f;
        [SerializeField] private float maxFogDensity = 0.05f;
        [SerializeField] private Color fogColorCalm = new Color(0.7f, 0.75f, 0.8f);
        [SerializeField] private Color fogColorStorm = new Color(0.3f, 0.35f, 0.4f);

        [Header("Wind")]
        [SerializeField] private WindZone windZone;
        [SerializeField] private float minWindSpeed = 0.5f;
        [SerializeField] private float maxWindSpeed = 5f;

        [Header("Transition")]
        [SerializeField] private float transitionSpeed = 0.5f;

        // Current state
        private float targetRainIntensity;
        private float currentRainIntensity;
        private float targetFogDensity;
        private float currentFogDensity;
        private float targetWindSpeed;
        private float currentWindSpeed;

        private float nextLightningTime;
        private float lightningFlashTimer;
        private bool isFlashing;

        // Public properties
        public float RainIntensity => currentRainIntensity;
        public float WindSpeed => currentWindSpeed;
        public float Visibility => 1f - Mathf.InverseLerp(minFogDensity, maxFogDensity, currentFogDensity);

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
            if (FloodManager.Instance != null)
            {
                FloodManager.Instance.OnPhaseChanged += OnFloodPhaseChanged;
                // Set initial weather based on current phase
                OnFloodPhaseChanged(FloodManager.Instance.CurrentPhase);
            }

            if (lightningLight != null)
                lightningLight.intensity = 0f;

            RenderSettings.fog = true;
            ScheduleNextLightning();
        }

        private void OnDestroy()
        {
            if (FloodManager.Instance != null)
                FloodManager.Instance.OnPhaseChanged -= OnFloodPhaseChanged;
        }

        private void Update()
        {
            UpdateTransitions();
            UpdateRain();
            UpdateLightning();
            UpdateFog();
            UpdateWind();
        }

        private void OnFloodPhaseChanged(FloodPhase phase)
        {
            switch (phase)
            {
                case FloodPhase.PreDisaster:
                    SetWeatherTargets(0f, minFogDensity, minWindSpeed);
                    break;
                case FloodPhase.Warning:
                    SetWeatherTargets(0.3f, Mathf.Lerp(minFogDensity, maxFogDensity, 0.2f), Mathf.Lerp(minWindSpeed, maxWindSpeed, 0.3f));
                    break;
                case FloodPhase.DisasterStrikes:
                    SetWeatherTargets(0.85f, Mathf.Lerp(minFogDensity, maxFogDensity, 0.7f), Mathf.Lerp(minWindSpeed, maxWindSpeed, 0.8f));
                    break;
                case FloodPhase.Escape:
                    SetWeatherTargets(0.5f, Mathf.Lerp(minFogDensity, maxFogDensity, 0.4f), Mathf.Lerp(minWindSpeed, maxWindSpeed, 0.5f));
                    break;
            }
        }

        private void SetWeatherTargets(float rainNormalized, float fogDensity, float windSpeed)
        {
            targetRainIntensity = rainNormalized;
            targetFogDensity = fogDensity;
            targetWindSpeed = windSpeed;
        }

        private void UpdateTransitions()
        {
            float delta = transitionSpeed * Time.deltaTime;
            currentRainIntensity = Mathf.MoveTowards(currentRainIntensity, targetRainIntensity, delta);
            currentFogDensity = Mathf.MoveTowards(currentFogDensity, targetFogDensity, delta * 0.01f);
            currentWindSpeed = Mathf.MoveTowards(currentWindSpeed, targetWindSpeed, delta);
        }

        private void UpdateRain()
        {
            if (rainParticleSystem == null) return;

            var emission = rainParticleSystem.emission;
            emission.rateOverTime = Mathf.Lerp(minRainRate, maxRainRate, currentRainIntensity);

            if (currentRainIntensity > 0.01f && !rainParticleSystem.isPlaying)
                rainParticleSystem.Play();
            else if (currentRainIntensity <= 0.01f && rainParticleSystem.isPlaying)
                rainParticleSystem.Stop();
        }

        private void UpdateLightning()
        {
            // Only do lightning during storms
            if (currentRainIntensity < 0.3f)
            {
                if (lightningLight != null)
                    lightningLight.intensity = 0f;
                return;
            }

            // Handle flash
            if (isFlashing)
            {
                lightningFlashTimer -= Time.deltaTime;
                if (lightningFlashTimer <= 0f)
                {
                    isFlashing = false;
                    if (lightningLight != null)
                        lightningLight.intensity = 0f;
                }
                return;
            }

            // Check for next lightning strike
            if (Time.time >= nextLightningTime)
            {
                TriggerLightning();
                ScheduleNextLightning();
            }
        }

        private void TriggerLightning()
        {
            isFlashing = true;
            lightningFlashTimer = lightningFlashDuration;

            if (lightningLight != null)
            {
                lightningLight.intensity = lightningIntensity;
                // Randomize direction slightly
                lightningLight.transform.rotation = Quaternion.Euler(
                    50f + Random.Range(-10f, 10f),
                    Random.Range(0f, 360f),
                    0f
                );
            }

            // Play thunder after a short delay based on "distance"
            if (lightningAudioSource != null && thunderClips != null && thunderClips.Length > 0)
            {
                AudioClip clip = thunderClips[Random.Range(0, thunderClips.Length)];
                float delay = Random.Range(0.2f, 2f);
                lightningAudioSource.clip = clip;
                lightningAudioSource.PlayDelayed(delay);
            }
        }

        private void ScheduleNextLightning()
        {
            float interval = Mathf.Lerp(maxLightningInterval, minLightningInterval, currentRainIntensity);
            nextLightningTime = Time.time + interval + Random.Range(-3f, 3f);
        }

        private void UpdateFog()
        {
            RenderSettings.fogDensity = currentFogDensity;
            RenderSettings.fogColor = Color.Lerp(fogColorCalm, fogColorStorm, currentRainIntensity);
        }

        private void UpdateWind()
        {
            if (windZone == null) return;

            windZone.windMain = currentWindSpeed;
            windZone.windTurbulence = currentWindSpeed * 0.3f;
            windZone.windPulseMagnitude = currentWindSpeed * 0.2f;
        }
    }
}
