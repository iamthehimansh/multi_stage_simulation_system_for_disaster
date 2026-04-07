using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using FightForLife.Core;

namespace FightForLife.UI
{
    /// <summary>
    /// Handles main menu navigation, role selection, and async scene loading with a loading screen.
    /// Attach to the MainCanvas in the MainMenu scene.
    ///
    /// BUILD SETTINGS REQUIREMENT:
    ///   Both scenes must be added to File > Build Settings > Scenes In Build:
    ///     0 - Assets/_FightForLife/Scenes/MainMenu.unity
    ///     1 - Assets/_FightForLife/Scenes/Island_Flood.unity
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject comingSoonPanel;
        [SerializeField] private GameObject roleSelectPanel;

        [Header("Audio")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;
        [SerializeField] private AudioClip buttonClickSFX;
        [SerializeField] private AudioClip buttonHoverSFX;

        // Loading screen (built programmatically)
        private GameObject loadingPanel;
        private Slider loadingBar;
        private TextMeshProUGUI loadingTipText;
        private TextMeshProUGUI loadingPercentText;

        private const string GAMEPLAY_SCENE = "Island_Flood";

        private static readonly string[] LoadingTips = new[]
        {
            "Tip: Higher ground is always safer during a flood.",
            "Tip: As a Disaster Expert you can reveal civilians on the map.",
            "Tip: Rescue civilians before the water level rises too high.",
            "Tip: Avoid downed power lines - they can be lethal in floodwater.",
            "Tip: Stockpile supplies early. You never know when routes will be cut off.",
            "Tip: Listen for calls for help - civilians may be trapped in buildings.",
            "Tip: Civilians move slower in deep water. Plan your route carefully.",
            "Tip: Completing side missions earns bonus score and resources.",
            "Tip: Your stamina drains faster when swimming against the current.",
            "Tip: Press ESC during gameplay to access the pause menu."
        };

        // --- Colors matching the scene builder palette ---
        private static readonly Color BG_DARK = new Color(0.05f, 0.08f, 0.12f, 0.97f);
        private static readonly Color ACCENT_BLUE = new Color(0.2f, 0.5f, 0.85f, 1f);
        private static readonly Color TEXT_WHITE = new Color(0.95f, 0.95f, 0.97f, 1f);
        private static readonly Color TEXT_GRAY = new Color(0.6f, 0.65f, 0.7f, 1f);

        private void Start()
        {
            ShowMainMenu();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 1f;

            BuildLoadingScreen();
        }

        // ===================== PANEL NAVIGATION =====================

        public void ShowMainMenu()
        {
            SetAllPanelsInactive();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        public void OnNewGameClicked()
        {
            PlayClickSound();
            SetAllPanelsInactive();
            if (roleSelectPanel != null)
                roleSelectPanel.SetActive(true);
            else
                StartGameWithRole(PlayerRole.Civilian);
        }

        public void OnContinueClicked()
        {
            PlayClickSound();
            ShowComingSoon();
        }

        public void OnMissionSelectClicked()
        {
            PlayClickSound();
            ShowComingSoon();
        }

        public void OnSettingsClicked()
        {
            PlayClickSound();
            SetAllPanelsInactive();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void OnCreditsClicked()
        {
            PlayClickSound();
            SetAllPanelsInactive();
            if (creditsPanel != null) creditsPanel.SetActive(true);
        }

        public void OnQuitClicked()
        {
            PlayClickSound();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        public void OnBackClicked()
        {
            PlayClickSound();
            ShowMainMenu();
        }

        // ===================== ROLE SELECTION =====================

        public void OnRoleCivilianClicked()
        {
            PlayClickSound();
            StartGameWithRole(PlayerRole.Civilian);
        }

        public void OnRoleExpertClicked()
        {
            PlayClickSound();
            StartGameWithRole(PlayerRole.DisasterManagementExpert);
        }

        private void StartGameWithRole(PlayerRole role)
        {
            // Configure GameManager
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetRole(role);
                GameManager.Instance.SetMap(GAMEPLAY_SCENE);
            }

            // Show loading screen and begin async load
            SetAllPanelsInactive();
            StartCoroutine(LoadSceneAsync(GAMEPLAY_SCENE));
        }

        // ===================== LOADING SCREEN =====================

        private void BuildLoadingScreen()
        {
            // Create loading panel as a child of this canvas
            loadingPanel = new GameObject("LoadingPanel");
            loadingPanel.transform.SetParent(transform, false);

            var rt = loadingPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            // Dark background
            var bg = loadingPanel.AddComponent<Image>();
            bg.color = BG_DARK;

            // "LOADING" title
            var titleObj = new GameObject("LoadingTitle");
            titleObj.transform.SetParent(loadingPanel.transform, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 60);
            titleRt.sizeDelta = new Vector2(600, 60);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "LOADING";
            titleTmp.fontSize = 42;
            titleTmp.color = TEXT_WHITE;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.characterSpacing = 10;

            // Progress bar background
            var barBgObj = new GameObject("BarBackground");
            barBgObj.transform.SetParent(loadingPanel.transform, false);
            var barBgRt = barBgObj.AddComponent<RectTransform>();
            barBgRt.anchoredPosition = new Vector2(0, -10);
            barBgRt.sizeDelta = new Vector2(600, 12);
            var barBgImg = barBgObj.AddComponent<Image>();
            barBgImg.color = new Color(0.15f, 0.18f, 0.25f, 1f);

            // Progress bar fill area
            var fillAreaObj = new GameObject("FillArea");
            fillAreaObj.transform.SetParent(barBgObj.transform, false);
            var fillAreaRt = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRt.anchorMin = Vector2.zero;
            fillAreaRt.anchorMax = Vector2.one;
            fillAreaRt.offsetMin = Vector2.zero;
            fillAreaRt.offsetMax = Vector2.zero;

            var fillObj = new GameObject("Fill");
            fillObj.transform.SetParent(fillAreaObj.transform, false);
            var fillRt = fillObj.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;
            var fillImg = fillObj.AddComponent<Image>();
            fillImg.color = ACCENT_BLUE;

            // Slider component on barBg
            loadingBar = barBgObj.AddComponent<Slider>();
            loadingBar.fillRect = fillRt;
            loadingBar.minValue = 0f;
            loadingBar.maxValue = 1f;
            loadingBar.value = 0f;
            loadingBar.interactable = false;
            // Remove the default selectable navigation highlight
            var nav = loadingBar.navigation;
            nav.mode = Navigation.Mode.None;
            loadingBar.navigation = nav;

            // Percent text
            var percentObj = new GameObject("PercentText");
            percentObj.transform.SetParent(loadingPanel.transform, false);
            var percentRt = percentObj.AddComponent<RectTransform>();
            percentRt.anchoredPosition = new Vector2(0, -40);
            percentRt.sizeDelta = new Vector2(200, 30);
            loadingPercentText = percentObj.AddComponent<TextMeshProUGUI>();
            loadingPercentText.text = "0%";
            loadingPercentText.fontSize = 18;
            loadingPercentText.color = TEXT_GRAY;
            loadingPercentText.alignment = TextAlignmentOptions.Center;

            // Tip text
            var tipObj = new GameObject("TipText");
            tipObj.transform.SetParent(loadingPanel.transform, false);
            var tipRt = tipObj.AddComponent<RectTransform>();
            tipRt.anchoredPosition = new Vector2(0, -120);
            tipRt.sizeDelta = new Vector2(700, 40);
            loadingTipText = tipObj.AddComponent<TextMeshProUGUI>();
            loadingTipText.text = "";
            loadingTipText.fontSize = 18;
            loadingTipText.color = TEXT_GRAY;
            loadingTipText.fontStyle = FontStyles.Italic;
            loadingTipText.alignment = TextAlignmentOptions.Center;

            loadingPanel.SetActive(false);
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            loadingPanel.SetActive(true);

            // Pick a random tip
            loadingTipText.text = LoadingTips[Random.Range(0, LoadingTips.Length)];
            loadingBar.value = 0f;
            loadingPercentText.text = "0%";

            yield return null; // Let the UI render one frame

            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            while (!asyncOp.isDone)
            {
                // Unity reports progress from 0 to 0.9; 0.9 means ready to activate
                float progress = Mathf.Clamp01(asyncOp.progress / 0.9f);
                loadingBar.value = progress;
                loadingPercentText.text = Mathf.RoundToInt(progress * 100f) + "%";

                if (asyncOp.progress >= 0.9f)
                {
                    // Fill bar to 100% visually
                    loadingBar.value = 1f;
                    loadingPercentText.text = "100%";

                    // Brief pause so the user sees 100%
                    yield return new WaitForSecondsRealtime(0.4f);

                    asyncOp.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        // ===================== HELPERS =====================

        public void ShowComingSoon()
        {
            SetAllPanelsInactive();
            if (comingSoonPanel != null) comingSoonPanel.SetActive(true);
        }

        public void PlayClickSound()
        {
            if (sfxSource != null && buttonClickSFX != null)
                sfxSource.PlayOneShot(buttonClickSFX);
        }

        public void PlayHoverSound()
        {
            if (sfxSource != null && buttonHoverSFX != null)
                sfxSource.PlayOneShot(buttonHoverSFX);
        }

        private void SetAllPanelsInactive()
        {
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (creditsPanel != null) creditsPanel.SetActive(false);
            if (comingSoonPanel != null) comingSoonPanel.SetActive(false);
            if (roleSelectPanel != null) roleSelectPanel.SetActive(false);
            if (loadingPanel != null) loadingPanel.SetActive(false);
        }
    }
}
