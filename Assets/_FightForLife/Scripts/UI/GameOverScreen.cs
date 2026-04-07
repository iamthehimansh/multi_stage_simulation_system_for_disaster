using UnityEngine;
using UnityEngine.UI;
using TMPro;
using FightForLife.Core;

namespace FightForLife.UI
{
    /// <summary>
    /// End-of-game screen that appears when GameManager enters GameOver or MissionComplete state.
    /// Displays score, rating (S/A/B/C/D/F), civilians rescued, play time, and action buttons.
    /// Add this component to any persistent GameObject in the gameplay scene; it builds its own Canvas.
    ///
    /// Rating thresholds (by score):
    ///   S = 5000+, A = 3500+, B = 2000+, C = 1000+, D = 500+, F = below 500
    /// </summary>
    public class GameOverScreen : MonoBehaviour
    {
        private GameObject screenCanvas;
        private TextMeshProUGUI titleText;
        private TextMeshProUGUI scoreText;
        private TextMeshProUGUI ratingText;
        private TextMeshProUGUI rescueText;
        private TextMeshProUGUI timeText;
        private TextMeshProUGUI subtitleText;

        // Colors
        private static readonly Color BG_OVERLAY = new Color(0.02f, 0.04f, 0.08f, 0.9f);
        private static readonly Color PANEL_BG = new Color(0.08f, 0.12f, 0.18f, 0.95f);
        private static readonly Color ACCENT_BLUE = new Color(0.2f, 0.5f, 0.85f, 1f);
        private static readonly Color ACCENT_GREEN = new Color(0.15f, 0.7f, 0.3f, 1f);
        private static readonly Color ACCENT_RED = new Color(0.85f, 0.2f, 0.15f, 1f);
        private static readonly Color ACCENT_GOLD = new Color(1f, 0.84f, 0f, 1f);
        private static readonly Color TEXT_WHITE = new Color(0.95f, 0.95f, 0.97f, 1f);
        private static readonly Color TEXT_GRAY = new Color(0.6f, 0.65f, 0.7f, 1f);
        private static readonly Color BUTTON_BG = new Color(0.15f, 0.2f, 0.3f, 0.9f);

        private void Awake()
        {
            BuildUI();
            screenCanvas.SetActive(false);
        }

        private void OnEnable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDisable()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver || newState == GameState.MissionComplete)
            {
                Show(newState == GameState.MissionComplete);
            }
        }

        public void Show(bool won)
        {
            screenCanvas.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            // Gather data
            int score = 0;
            int rescued = 0;
            int totalCivilians = 0;
            float playTime = 0f;

            if (ScoreManager.Instance != null)
            {
                score = ScoreManager.Instance.CurrentScore;
                rescued = ScoreManager.Instance.CiviliansRescued;
                totalCivilians = ScoreManager.Instance.TotalCivilians;
            }

            if (GameManager.Instance != null)
                playTime = GameManager.Instance.TotalPlayTime;

            string rating = CalculateRating(score);

            // Populate UI
            titleText.text = won ? "MISSION COMPLETE" : "MISSION FAILED";
            titleText.color = won ? ACCENT_GREEN : ACCENT_RED;

            subtitleText.text = won
                ? "You made it through the disaster."
                : "The flood claimed too much. Try again.";

            scoreText.text = $"SCORE: {score:N0}";

            ratingText.text = rating;
            ratingText.color = GetRatingColor(rating);

            rescueText.text = totalCivilians > 0
                ? $"Civilians Rescued: {rescued} / {totalCivilians}"
                : $"Civilians Rescued: {rescued}";

            int minutes = Mathf.FloorToInt(playTime / 60f);
            int seconds = Mathf.FloorToInt(playTime % 60f);
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        public void Hide()
        {
            screenCanvas.SetActive(false);
        }

        // ===================== RATING =====================

        /// <summary>
        /// S = 5000+, A = 3500+, B = 2000+, C = 1000+, D = 500+, F = below 500
        /// </summary>
        public static string CalculateRating(int score)
        {
            if (score >= 5000) return "S";
            if (score >= 3500) return "A";
            if (score >= 2000) return "B";
            if (score >= 1000) return "C";
            if (score >= 500) return "D";
            return "F";
        }

        private Color GetRatingColor(string rating)
        {
            switch (rating)
            {
                case "S": return ACCENT_GOLD;
                case "A": return ACCENT_GREEN;
                case "B": return ACCENT_BLUE;
                case "C": return TEXT_WHITE;
                case "D": return TEXT_GRAY;
                case "F": return ACCENT_RED;
                default: return TEXT_WHITE;
            }
        }

        // ===================== ACTIONS =====================

        private void OnPlayAgainClicked()
        {
            screenCanvas.SetActive(false);
            if (GameManager.Instance != null)
                GameManager.Instance.RestartMission();
        }

        private void OnMainMenuClicked()
        {
            screenCanvas.SetActive(false);
            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMenu();
        }

        // ===================== BUILD UI =====================

        private void BuildUI()
        {
            screenCanvas = new GameObject("GameOverCanvas");
            screenCanvas.transform.SetParent(transform, false);

            var canvas = screenCanvas.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 110; // Above pause menu

            var scaler = screenCanvas.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            screenCanvas.AddComponent<GraphicRaycaster>();

            // Full-screen overlay
            var overlay = CreateRect(screenCanvas.transform, "Overlay");
            overlay.anchorMin = Vector2.zero;
            overlay.anchorMax = Vector2.one;
            overlay.offsetMin = Vector2.zero;
            overlay.offsetMax = Vector2.zero;
            overlay.gameObject.AddComponent<Image>().color = BG_OVERLAY;

            // Center panel
            var panel = new GameObject("Panel");
            panel.transform.SetParent(screenCanvas.transform, false);
            var panelRt = panel.AddComponent<RectTransform>();
            panelRt.anchoredPosition = Vector2.zero;
            panelRt.sizeDelta = new Vector2(520, 520);
            panel.AddComponent<Image>().color = PANEL_BG;

            // Accent bar
            var accent = CreateRect(panel.transform, "AccentBar");
            accent.anchoredPosition = new Vector2(0, 255);
            accent.sizeDelta = new Vector2(520, 6);
            accent.gameObject.AddComponent<Image>().color = ACCENT_BLUE;

            // Title
            titleText = CreateText(panel.transform, "Title", "MISSION COMPLETE", 38, ACCENT_GREEN,
                FontStyles.Bold, new Vector2(0, 200), new Vector2(480, 55));

            // Subtitle
            subtitleText = CreateText(panel.transform, "Subtitle", "", 16, TEXT_GRAY,
                FontStyles.Italic, new Vector2(0, 160), new Vector2(480, 30));

            // Divider
            var div = CreateRect(panel.transform, "Divider");
            div.anchoredPosition = new Vector2(0, 135);
            div.sizeDelta = new Vector2(400, 2);
            div.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.15f);

            // Rating (big center text)
            ratingText = CreateText(panel.transform, "Rating", "S", 96, ACCENT_GOLD,
                FontStyles.Bold, new Vector2(0, 60), new Vector2(200, 120));

            // Score
            scoreText = CreateText(panel.transform, "Score", "SCORE: 0", 26, TEXT_WHITE,
                FontStyles.Bold, new Vector2(0, -15), new Vector2(400, 40));

            // Rescue count
            rescueText = CreateText(panel.transform, "RescueCount", "Civilians Rescued: 0", 18, TEXT_GRAY,
                FontStyles.Normal, new Vector2(0, -55), new Vector2(400, 30));

            // Time
            timeText = CreateText(panel.transform, "Time", "Time: 00:00", 18, TEXT_GRAY,
                FontStyles.Normal, new Vector2(0, -85), new Vector2(400, 30));

            // Divider 2
            var div2 = CreateRect(panel.transform, "Divider2");
            div2.anchoredPosition = new Vector2(0, -115);
            div2.sizeDelta = new Vector2(400, 2);
            div2.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.1f);

            // Buttons
            CreateButton(panel.transform, "PLAY AGAIN", new Vector2(0, -160),
                new Vector2(300, 50), ACCENT_BLUE, TEXT_WHITE, 22, OnPlayAgainClicked);

            CreateButton(panel.transform, "MAIN MENU", new Vector2(0, -225),
                new Vector2(300, 50), BUTTON_BG, TEXT_WHITE, 22, OnMainMenuClicked);
        }

        // ===================== HELPERS =====================

        private RectTransform CreateRect(Transform parent, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj.AddComponent<RectTransform>();
        }

        private TextMeshProUGUI CreateText(Transform parent, string name, string text,
            float fontSize, Color color, FontStyles style, Vector2 pos, Vector2 size)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private void CreateButton(Transform parent, string label, Vector2 pos, Vector2 size,
            Color bgColor, Color textColor, float fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var obj = new GameObject($"Btn_{label.Replace(" ", "")}");
            obj.transform.SetParent(parent, false);
            var rt = obj.AddComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;

            var img = obj.AddComponent<Image>();
            img.color = bgColor;

            var btn = obj.AddComponent<Button>();
            var colors = btn.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = new Color(bgColor.r + 0.1f, bgColor.g + 0.15f, bgColor.b + 0.2f, 1f);
            colors.pressedColor = new Color(bgColor.r - 0.05f, bgColor.g - 0.05f, bgColor.b - 0.05f, 1f);
            colors.fadeDuration = 0.1f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var outline = obj.AddComponent<Outline>();
            outline.effectColor = new Color(ACCENT_BLUE.r, ACCENT_BLUE.g, ACCENT_BLUE.b, 0.3f);
            outline.effectDistance = new Vector2(1, -1);

            CreateText(obj.transform, "Label", label, fontSize, textColor, FontStyles.Bold,
                Vector2.zero, size);
        }
    }
}
