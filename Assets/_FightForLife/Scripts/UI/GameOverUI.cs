using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using FightForLife.Core;
using System.Collections;

namespace FightForLife.UI
{
    public class GameOverUI : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private CanvasGroup panelCanvasGroup;

        [Header("Title")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Header("Stats")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI ratingText;
        [SerializeField] private TextMeshProUGUI rescuedText;
        [SerializeField] private TextMeshProUGUI timeText;
        [SerializeField] private TextMeshProUGUI missionsText;

        [Header("Animation")]
        [SerializeField] private float fadeInDuration = 1f;

        private ScoreManager scoreManager;
        private Coroutine fadeCoroutine;

        private void Start()
        {
            scoreManager = ScoreManager.Instance;

            if (gameOverPanel != null)
                gameOverPanel.SetActive(false);

            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = 0f;

            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
        }

        private void OnGameStateChanged(GameState newState)
        {
            if (newState == GameState.GameOver)
                Show(false);
            else if (newState == GameState.MissionComplete)
                Show(true);
        }

        public void Show(bool won)
        {
            if (gameOverPanel == null) return;

            gameOverPanel.SetActive(true);

            if (titleText != null)
            {
                titleText.text = won ? "MISSION COMPLETE" : "GAME OVER";
                titleText.color = won ? new Color(0.2f, 1f, 0.4f) : Color.red;
            }

            PopulateStats();

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeIn());

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void PopulateStats()
        {
            if (scoreManager == null)
                scoreManager = ScoreManager.Instance;

            if (scoreManager == null) return;

            if (scoreText != null)
                scoreText.text = $"Score: {scoreManager.CurrentScore}";

            string rating = scoreManager.GetRating();
            if (ratingText != null)
            {
                ratingText.text = rating;
                ratingText.color = GetRatingColor(rating);
                ratingText.fontSize = 72f;
            }

            if (rescuedText != null)
                rescuedText.text = $"Civilians Rescued: {scoreManager.CiviliansRescued}/{scoreManager.TotalCivilians}";

            if (timeText != null)
            {
                float totalTime = GameManager.Instance != null ? GameManager.Instance.TotalPlayTime : 0f;
                int minutes = Mathf.FloorToInt(totalTime / 60f);
                int seconds = Mathf.FloorToInt(totalTime % 60f);
                timeText.text = $"Time: {minutes:00}:{seconds:00}";
            }

            if (missionsText != null)
                missionsText.text = $"Missions Completed: {scoreManager.MissionsCompleted}";
        }

        private Color GetRatingColor(string rating)
        {
            switch (rating)
            {
                case "S": return new Color(1f, 0.84f, 0f);       // Gold
                case "A": return new Color(0.2f, 0.8f, 0.2f);    // Green
                case "B": return new Color(0.3f, 0.5f, 1f);      // Blue
                case "C": return new Color(0.6f, 0.6f, 0.6f);    // Gray
                case "D": return new Color(1f, 0.6f, 0.2f);      // Orange
                case "F": return Color.red;
                default:  return Color.white;
            }
        }

        private IEnumerator FadeIn()
        {
            if (panelCanvasGroup == null) yield break;

            panelCanvasGroup.alpha = 0f;
            float elapsed = 0f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                panelCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeInDuration);
                yield return null;
            }

            panelCanvasGroup.alpha = 1f;
        }

        public void OnRetryPressed()
        {
            Time.timeScale = 1f;

            if (GameManager.Instance != null)
                GameManager.Instance.RestartMission();
            else
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnReturnToMenuPressed()
        {
            Time.timeScale = 1f;

            if (GameManager.Instance != null)
                GameManager.Instance.ReturnToMenu();
            else
                SceneManager.LoadScene("MainMenu");
        }
    }
}
