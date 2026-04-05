using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FightForLife.Core;
using FightForLife.Player;
using FightForLife.NPC;
using FightForLife.Disaster;

namespace FightForLife.UI
{
    public class HUDManager : MonoBehaviour
    {
        [Header("Health / Stamina / Oxygen Bars")]
        [SerializeField] private Image healthBarFill;
        [SerializeField] private Image healthBarSmooth;
        [SerializeField] private Image staminaBarFill;
        [SerializeField] private Image oxygenBarFill;
        [SerializeField] private GameObject oxygenBarGroup;

        [Header("Minimap")]
        [SerializeField] private RawImage minimapImage;

        [Header("Rescue Counter")]
        [SerializeField] private TextMeshProUGUI rescueCounterText;

        [Header("Active Item")]
        [SerializeField] private Image activeItemIcon;
        [SerializeField] private GameObject activeItemGroup;

        [Header("Interaction Prompt")]
        [SerializeField] private GameObject interactionPromptGroup;
        [SerializeField] private TextMeshProUGUI interactionPromptText;
        [SerializeField] private Image holdProgressFill;

        [Header("Phase Indicator")]
        [SerializeField] private TextMeshProUGUI phaseIndicatorText;
        [SerializeField] private GameObject phaseIndicatorGroup;

        [Header("Mission Objective")]
        [SerializeField] private TextMeshProUGUI missionNameText;
        [SerializeField] private TextMeshProUGUI missionDescriptionText;
        [SerializeField] private GameObject missionObjectiveGroup;

        [Header("Score & Timer")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI timerText;

        [Header("Low Health Warning")]
        [SerializeField] private Image lowHealthVignette;
        [SerializeField] private float vignetteMinAlpha = 0.1f;
        [SerializeField] private float vignetteMaxAlpha = 0.5f;
        [SerializeField] private float vignettePulseSpeed = 2f;

        [Header("Compass")]
        [SerializeField] private RectTransform compassBar;
        [SerializeField] private RawImage compassImage;

        [Header("Settings")]
        [SerializeField] private float healthBarSmoothSpeed = 5f;
        [SerializeField] private float lowHealthThreshold = 0.25f;

        private PlayerHealth playerHealth;
        private InteractionSystem interactionSystem;
        private NPCSpawner npcSpawner;
        private FloodManager floodManager;
        private ScoreManager scoreManager;
        private MissionManager missionManager;
        private Transform playerTransform;

        private float smoothHealthTarget;
        private float currentSmoothHealth;
        private bool isInitialized;

        private void Start()
        {
            FindReferences();
        }

        private void FindReferences()
        {
            playerHealth = FindAnyObjectByType<PlayerHealth>();
            interactionSystem = FindAnyObjectByType<InteractionSystem>();
            npcSpawner = FindAnyObjectByType<NPCSpawner>();
            floodManager = FindAnyObjectByType<FloodManager>();
            scoreManager = ScoreManager.Instance;
            missionManager = MissionManager.Instance;

            if (playerHealth != null)
            {
                playerTransform = playerHealth.transform;
                playerHealth.OnHealthChanged += OnHealthChanged;
                playerHealth.OnStaminaChanged += OnStaminaChanged;
                playerHealth.OnOxygenChanged += OnOxygenChanged;
                playerHealth.OnDeath += OnPlayerDeath;

                smoothHealthTarget = playerHealth.HealthPercent;
                currentSmoothHealth = smoothHealthTarget;
                UpdateHealthBar(playerHealth.HealthPercent);
                UpdateStaminaBar(playerHealth.StaminaPercent);
                UpdateOxygenBar(playerHealth.OxygenPercent);
            }

            if (lowHealthVignette != null)
            {
                SetVignetteAlpha(0f);
            }

            isInitialized = true;
        }

        private void Update()
        {
            if (!isInitialized) return;

            UpdateSmoothHealthBar();
            UpdateInteractionPrompt();
            UpdateRescueCounter();
            UpdatePhaseIndicator();
            UpdateMissionObjective();
            UpdateScoreDisplay();
            UpdateTimerDisplay();
            UpdateLowHealthWarning();
            UpdateCompass();
            UpdateOxygenVisibility();
        }

        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged -= OnHealthChanged;
                playerHealth.OnStaminaChanged -= OnStaminaChanged;
                playerHealth.OnOxygenChanged -= OnOxygenChanged;
                playerHealth.OnDeath -= OnPlayerDeath;
            }
        }

        #region Health / Stamina / Oxygen

        private void OnHealthChanged(float percent)
        {
            smoothHealthTarget = percent;
            UpdateHealthBar(percent);
        }

        private void OnStaminaChanged(float percent)
        {
            UpdateStaminaBar(percent);
        }

        private void OnOxygenChanged(float percent)
        {
            UpdateOxygenBar(percent);
        }

        private void UpdateHealthBar(float percent)
        {
            if (healthBarFill != null)
                healthBarFill.fillAmount = percent;
        }

        private void UpdateSmoothHealthBar()
        {
            if (healthBarSmooth == null) return;

            currentSmoothHealth = Mathf.Lerp(currentSmoothHealth, smoothHealthTarget,
                Time.deltaTime * healthBarSmoothSpeed);
            healthBarSmooth.fillAmount = currentSmoothHealth;
        }

        private void UpdateStaminaBar(float percent)
        {
            if (staminaBarFill != null)
                staminaBarFill.fillAmount = percent;
        }

        private void UpdateOxygenBar(float percent)
        {
            if (oxygenBarFill != null)
                oxygenBarFill.fillAmount = percent;
        }

        private void UpdateOxygenVisibility()
        {
            if (oxygenBarGroup == null || playerHealth == null) return;

            bool showOxygen = playerHealth.OxygenPercent < 1f;
            if (oxygenBarGroup.activeSelf != showOxygen)
                oxygenBarGroup.SetActive(showOxygen);
        }

        #endregion

        #region Interaction Prompt

        private void UpdateInteractionPrompt()
        {
            if (interactionSystem == null || interactionPromptGroup == null) return;

            string prompt = interactionSystem.CurrentPrompt;
            bool hasPrompt = !string.IsNullOrEmpty(prompt);

            if (interactionPromptGroup.activeSelf != hasPrompt)
                interactionPromptGroup.SetActive(hasPrompt);

            if (hasPrompt)
            {
                if (interactionPromptText != null)
                    interactionPromptText.text = prompt;

                if (holdProgressFill != null)
                {
                    holdProgressFill.fillAmount = interactionSystem.HoldProgress;
                    holdProgressFill.gameObject.SetActive(interactionSystem.IsHolding);
                }
            }
        }

        #endregion

        #region Rescue Counter

        private void UpdateRescueCounter()
        {
            if (rescueCounterText == null || npcSpawner == null) return;
            rescueCounterText.text = $"Rescued: {npcSpawner.GetRescuedCount()}/{npcSpawner.GetTotalCount()}";
        }

        #endregion

        #region Phase Indicator

        private void UpdatePhaseIndicator()
        {
            if (phaseIndicatorText == null || floodManager == null) return;

            if (phaseIndicatorGroup != null)
            {
                bool showPhase = floodManager.CurrentPhase != FloodPhase.PreDisaster;
                if (phaseIndicatorGroup.activeSelf != showPhase)
                    phaseIndicatorGroup.SetActive(showPhase);
            }

            switch (floodManager.CurrentPhase)
            {
                case FloodPhase.Warning:
                    phaseIndicatorText.text = "WARNING";
                    phaseIndicatorText.color = new Color(1f, 0.8f, 0f);
                    break;
                case FloodPhase.DisasterStrikes:
                    phaseIndicatorText.text = "DISASTER";
                    phaseIndicatorText.color = Color.red;
                    break;
                case FloodPhase.Escape:
                    phaseIndicatorText.text = "ESCAPE";
                    phaseIndicatorText.color = new Color(1f, 0.4f, 0f);
                    break;
                default:
                    phaseIndicatorText.text = "";
                    break;
            }
        }

        #endregion

        #region Mission Objective

        private void UpdateMissionObjective()
        {
            if (missionManager == null) return;

            MissionData active = missionManager.ActiveMission;
            bool hasMission = active != null && active.status == MissionStatus.Active;

            if (missionObjectiveGroup != null)
            {
                if (missionObjectiveGroup.activeSelf != hasMission)
                    missionObjectiveGroup.SetActive(hasMission);
            }

            if (hasMission)
            {
                if (missionNameText != null)
                    missionNameText.text = active.missionName;
                if (missionDescriptionText != null)
                    missionDescriptionText.text = active.description;
            }
        }

        #endregion

        #region Score & Timer

        private void UpdateScoreDisplay()
        {
            if (scoreText == null || scoreManager == null) return;
            scoreText.text = $"Score: {scoreManager.CurrentScore}";
        }

        private void UpdateTimerDisplay()
        {
            if (timerText == null || floodManager == null) return;

            float elapsed = floodManager.ElapsedTime;
            int minutes = Mathf.FloorToInt(elapsed / 60f);
            int seconds = Mathf.FloorToInt(elapsed % 60f);
            timerText.text = $"{minutes:00}:{seconds:00}";
        }

        #endregion

        #region Low Health Warning

        private void UpdateLowHealthWarning()
        {
            if (lowHealthVignette == null || playerHealth == null) return;

            if (playerHealth.HealthPercent < lowHealthThreshold && playerHealth.IsAlive)
            {
                float pulse = Mathf.Lerp(vignetteMinAlpha, vignetteMaxAlpha,
                    (Mathf.Sin(Time.time * vignettePulseSpeed * Mathf.PI) + 1f) * 0.5f);

                float intensity = 1f - (playerHealth.HealthPercent / lowHealthThreshold);
                SetVignetteAlpha(pulse * intensity);
            }
            else
            {
                SetVignetteAlpha(0f);
            }
        }

        private void SetVignetteAlpha(float alpha)
        {
            if (lowHealthVignette == null) return;
            Color c = lowHealthVignette.color;
            c.a = alpha;
            lowHealthVignette.color = c;
        }

        #endregion

        #region Compass

        private void UpdateCompass()
        {
            if (compassImage == null || playerTransform == null) return;

            float yRotation = playerTransform.eulerAngles.y;
            float uvOffset = yRotation / 360f;
            compassImage.uvRect = new Rect(uvOffset, 0f, 1f, 1f);
        }

        #endregion

        #region Active Item

        public void SetActiveItem(Sprite icon)
        {
            if (activeItemGroup == null) return;

            if (icon != null)
            {
                activeItemGroup.SetActive(true);
                if (activeItemIcon != null)
                    activeItemIcon.sprite = icon;
            }
            else
            {
                activeItemGroup.SetActive(false);
            }
        }

        #endregion

        private void OnPlayerDeath()
        {
            if (lowHealthVignette != null)
                SetVignetteAlpha(vignetteMaxAlpha);
        }
    }
}
