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

        // Map select screen (built programmatically)
        private GameObject mapSelectPanel;

        // Currently selected gameplay scene, chosen from the map-select panel.
        private string selectedMapScene = "Island_Flood";

        private const string MAP_ISLAND_FLOOD = "Island_Flood";
        private const string MAP_ISLAND_FLOOD_M = "Island_flood_m";

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
            BuildMapSelectPanel();
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
            // Show the map select panel first. After the player picks a map,
            // we advance to role select, then load the selected scene.
            SetAllPanelsInactive();
            if (mapSelectPanel != null)
                mapSelectPanel.SetActive(true);
            else if (roleSelectPanel != null)
                roleSelectPanel.SetActive(true);
            else
                StartGameWithRole(PlayerRole.Civilian);
        }

        // Called by the Island Flood button on the map select panel.
        public void OnMapIslandFloodClicked()
        {
            PlayClickSound();
            selectedMapScene = MAP_ISLAND_FLOOD;
            TryStartSelectedMap();
        }

        // Called by the Island Flood M button on the map select panel.
        public void OnMapIslandFloodMClicked()
        {
            PlayClickSound();
            selectedMapScene = MAP_ISLAND_FLOOD_M;
            TryStartSelectedMap();
        }

        private void TryStartSelectedMap()
        {
            if (!IsSceneInBuild(selectedMapScene))
            {
                SetAllPanelsInactive();
                if (comingSoonPanel != null) comingSoonPanel.SetActive(true);
                else if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
                return;
            }
            SetAllPanelsInactive();
            if (roleSelectPanel != null) roleSelectPanel.SetActive(true);
            else StartGameWithRole(PlayerRole.Civilian);
        }

        private static bool IsSceneInBuild(string sceneName)
        {
            int count = SceneManager.sceneCountInBuildSettings;
            for (int i = 0; i < count; i++)
            {
                string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
                if (string.IsNullOrEmpty(path)) continue;
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName) return true;
            }
            return false;
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
            // Configure GameManager with the map the player selected.
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetRole(role);
                GameManager.Instance.SetMap(selectedMapScene);
            }

            // Show loading screen and begin async load
            SetAllPanelsInactive();
            StartCoroutine(LoadSceneAsync(selectedMapScene));
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
            if (mapSelectPanel != null) mapSelectPanel.SetActive(false);
        }

        // ===================== MAP SELECT SCREEN =====================

        private void BuildMapSelectPanel()
        {
            mapSelectPanel = new GameObject("MapSelectPanel");
            mapSelectPanel.transform.SetParent(transform, false);

            var rt = mapSelectPanel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var bg = mapSelectPanel.AddComponent<Image>();
            bg.color = new Color(0.03f, 0.06f, 0.1f, 0.95f);
            bg.raycastTarget = true;

            // Title
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(mapSelectPanel.transform, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 350);
            titleRt.sizeDelta = new Vector2(800, 70);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "SELECT MAP";
            titleTmp.fontSize = 48;
            titleTmp.color = TEXT_WHITE;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Subtitle
            var subObj = new GameObject("Subtitle");
            subObj.transform.SetParent(mapSelectPanel.transform, false);
            var subRt = subObj.AddComponent<RectTransform>();
            subRt.anchoredPosition = new Vector2(0, 300);
            subRt.sizeDelta = new Vector2(800, 40);
            var subTmp = subObj.AddComponent<TextMeshProUGUI>();
            subTmp.text = "Choose the region you want to save";
            subTmp.fontSize = 20;
            subTmp.color = TEXT_GRAY;
            subTmp.fontStyle = FontStyles.Italic;
            subTmp.alignment = TextAlignmentOptions.Center;

            // Island Flood card (left)
            CreateMapCard(mapSelectPanel.transform,
                "IslandFloodCard",
                new Vector2(-260, 0),
                "ISLAND FLOOD",
                "The original flooded village.\nNavigate streets, ring the bell,\nand escort civilians to high ground.",
                new Color(0.2f, 0.5f, 0.85f, 1f),
                OnMapIslandFloodClicked);

            // Island Flood M card (right)
            CreateMapCard(mapSelectPanel.transform,
                "IslandFloodMCard",
                new Vector2(260, 0),
                "ISLAND FLOOD M",
                "The mountainous variant.\nHigher terrain, different hazards,\nand an alternate mission routing.",
                new Color(0.9f, 0.5f, 0.1f, 1f),
                OnMapIslandFloodMClicked);

            // Back button
            var backObj = new GameObject("BackButton");
            backObj.transform.SetParent(mapSelectPanel.transform, false);
            var backRt = backObj.AddComponent<RectTransform>();
            backRt.anchoredPosition = new Vector2(0, -400);
            backRt.sizeDelta = new Vector2(200, 50);
            var backImg = backObj.AddComponent<Image>();
            backImg.color = new Color(0.15f, 0.2f, 0.3f, 0.9f);
            var backBtn = backObj.AddComponent<Button>();
            backBtn.targetGraphic = backImg;
            var backTxtObj = new GameObject("Text");
            backTxtObj.transform.SetParent(backObj.transform, false);
            var backTxtRt = backTxtObj.AddComponent<RectTransform>();
            backTxtRt.anchorMin = Vector2.zero;
            backTxtRt.anchorMax = Vector2.one;
            backTxtRt.offsetMin = Vector2.zero;
            backTxtRt.offsetMax = Vector2.zero;
            var backTxt = backTxtObj.AddComponent<TextMeshProUGUI>();
            backTxt.text = "BACK";
            backTxt.fontSize = 20;
            backTxt.color = TEXT_WHITE;
            backTxt.fontStyle = FontStyles.Bold;
            backTxt.alignment = TextAlignmentOptions.Center;
            backBtn.onClick.AddListener(OnBackClicked);

            mapSelectPanel.SetActive(false);
        }

        private void CreateMapCard(Transform parent, string name, Vector2 pos, string title,
                                    string description, Color accent, UnityEngine.Events.UnityAction onClick)
        {
            var card = new GameObject(name);
            card.transform.SetParent(parent, false);
            var rt = card.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(450, 520);
            var cardBg = card.AddComponent<Image>();
            cardBg.color = new Color(0.08f, 0.12f, 0.18f, 0.95f);

            // Accent stripe at top
            var stripeObj = new GameObject("AccentStripe");
            stripeObj.transform.SetParent(card.transform, false);
            var stripeRt = stripeObj.AddComponent<RectTransform>();
            stripeRt.anchorMin = new Vector2(0, 1);
            stripeRt.anchorMax = new Vector2(1, 1);
            stripeRt.pivot = new Vector2(0.5f, 1f);
            stripeRt.anchoredPosition = Vector2.zero;
            stripeRt.sizeDelta = new Vector2(0, 8);
            var stripeImg = stripeObj.AddComponent<Image>();
            stripeImg.color = accent;

            // Title
            var titleObj = new GameObject("CardTitle");
            titleObj.transform.SetParent(card.transform, false);
            var titleRt = titleObj.AddComponent<RectTransform>();
            titleRt.anchoredPosition = new Vector2(0, 170);
            titleRt.sizeDelta = new Vector2(420, 60);
            var titleTmp = titleObj.AddComponent<TextMeshProUGUI>();
            titleTmp.text = title;
            titleTmp.fontSize = 32;
            titleTmp.color = TEXT_WHITE;
            titleTmp.fontStyle = FontStyles.Bold;
            titleTmp.alignment = TextAlignmentOptions.Center;

            // Description
            var descObj = new GameObject("CardDesc");
            descObj.transform.SetParent(card.transform, false);
            var descRt = descObj.AddComponent<RectTransform>();
            descRt.anchoredPosition = new Vector2(0, 40);
            descRt.sizeDelta = new Vector2(400, 200);
            var descTmp = descObj.AddComponent<TextMeshProUGUI>();
            descTmp.text = description;
            descTmp.fontSize = 18;
            descTmp.color = TEXT_GRAY;
            descTmp.alignment = TextAlignmentOptions.Center;

            // Select button
            var btnObj = new GameObject("SelectButton");
            btnObj.transform.SetParent(card.transform, false);
            var btnRt = btnObj.AddComponent<RectTransform>();
            btnRt.anchoredPosition = new Vector2(0, -180);
            btnRt.sizeDelta = new Vector2(300, 55);
            var btnImg = btnObj.AddComponent<Image>();
            btnImg.color = accent;
            var btn = btnObj.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(onClick);

            var btnTxtObj = new GameObject("Text");
            btnTxtObj.transform.SetParent(btnObj.transform, false);
            var btnTxtRt = btnTxtObj.AddComponent<RectTransform>();
            btnTxtRt.anchorMin = Vector2.zero;
            btnTxtRt.anchorMax = Vector2.one;
            btnTxtRt.offsetMin = Vector2.zero;
            btnTxtRt.offsetMax = Vector2.zero;
            var btnTxt = btnTxtObj.AddComponent<TextMeshProUGUI>();
            btnTxt.text = "PLAY THIS MAP";
            btnTxt.fontSize = 22;
            btnTxt.color = TEXT_WHITE;
            btnTxt.fontStyle = FontStyles.Bold;
            btnTxt.alignment = TextAlignmentOptions.Center;
        }
    }
}
