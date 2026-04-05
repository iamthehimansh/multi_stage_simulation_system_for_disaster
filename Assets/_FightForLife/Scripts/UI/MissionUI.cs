using UnityEngine;
using TMPro;
using FightForLife.Core;
using System.Collections;

namespace FightForLife.UI
{
    public class MissionUI : MonoBehaviour
    {
        [Header("Mission Briefing Popup")]
        [SerializeField] private RectTransform briefingPanel;
        [SerializeField] private TextMeshProUGUI briefingNameText;
        [SerializeField] private TextMeshProUGUI briefingDescriptionText;
        [SerializeField] private TextMeshProUGUI briefingRewardText;
        [SerializeField] private CanvasGroup briefingCanvasGroup;

        [Header("Mission Complete Popup")]
        [SerializeField] private RectTransform completePanel;
        [SerializeField] private TextMeshProUGUI completeNameText;
        [SerializeField] private TextMeshProUGUI completeScoreText;
        [SerializeField] private CanvasGroup completeCanvasGroup;

        [Header("Mission Failed Popup")]
        [SerializeField] private RectTransform failedPanel;
        [SerializeField] private TextMeshProUGUI failedNameText;
        [SerializeField] private CanvasGroup failedCanvasGroup;

        [Header("Animation Settings")]
        [SerializeField] private float slideDistance = 400f;
        [SerializeField] private float slideDuration = 0.4f;
        [SerializeField] private float displayDuration = 5f;

        private Coroutine briefingCoroutine;
        private Coroutine completeCoroutine;
        private Coroutine failedCoroutine;

        private Vector2 briefingHiddenPos;
        private Vector2 briefingShownPos;
        private Vector2 completeHiddenPos;
        private Vector2 completeShownPos;
        private Vector2 failedHiddenPos;
        private Vector2 failedShownPos;

        private void Awake()
        {
            CachePositions();
            HideAllImmediate();
        }

        private void Start()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionStarted += ShowMissionBriefing;
                MissionManager.Instance.OnMissionCompleted += ShowMissionComplete;
                MissionManager.Instance.OnMissionFailed += ShowMissionFailed;
            }
        }

        private void OnDestroy()
        {
            if (MissionManager.Instance != null)
            {
                MissionManager.Instance.OnMissionStarted -= ShowMissionBriefing;
                MissionManager.Instance.OnMissionCompleted -= ShowMissionComplete;
                MissionManager.Instance.OnMissionFailed -= ShowMissionFailed;
            }
        }

        private void CachePositions()
        {
            if (briefingPanel != null)
            {
                briefingShownPos = briefingPanel.anchoredPosition;
                briefingHiddenPos = briefingShownPos + new Vector2(slideDistance, 0f);
            }

            if (completePanel != null)
            {
                completeShownPos = completePanel.anchoredPosition;
                completeHiddenPos = completeShownPos + new Vector2(slideDistance, 0f);
            }

            if (failedPanel != null)
            {
                failedShownPos = failedPanel.anchoredPosition;
                failedHiddenPos = failedShownPos + new Vector2(slideDistance, 0f);
            }
        }

        private void HideAllImmediate()
        {
            SetPanelHidden(briefingPanel, briefingCanvasGroup, briefingHiddenPos);
            SetPanelHidden(completePanel, completeCanvasGroup, completeHiddenPos);
            SetPanelHidden(failedPanel, failedCanvasGroup, failedHiddenPos);
        }

        private void SetPanelHidden(RectTransform panel, CanvasGroup group, Vector2 hiddenPos)
        {
            if (panel == null) return;
            panel.anchoredPosition = hiddenPos;
            panel.gameObject.SetActive(false);
            if (group != null) group.alpha = 0f;
        }

        public void ShowMissionBriefing(MissionData mission)
        {
            if (briefingPanel == null) return;

            if (briefingCoroutine != null)
                StopCoroutine(briefingCoroutine);

            if (briefingNameText != null)
                briefingNameText.text = mission.missionName;
            if (briefingDescriptionText != null)
                briefingDescriptionText.text = mission.description;
            if (briefingRewardText != null)
                briefingRewardText.text = $"+{mission.rewardPoints} pts";

            briefingCoroutine = StartCoroutine(SlidePopup(
                briefingPanel, briefingCanvasGroup, briefingHiddenPos, briefingShownPos));
        }

        public void ShowMissionComplete(MissionData mission)
        {
            if (completePanel == null) return;

            if (completeCoroutine != null)
                StopCoroutine(completeCoroutine);

            if (completeNameText != null)
                completeNameText.text = mission.missionName;
            if (completeScoreText != null)
                completeScoreText.text = $"+{mission.rewardPoints}";

            completeCoroutine = StartCoroutine(SlidePopup(
                completePanel, completeCanvasGroup, completeHiddenPos, completeShownPos));
        }

        public void ShowMissionFailed(MissionData mission)
        {
            if (failedPanel == null) return;

            if (failedCoroutine != null)
                StopCoroutine(failedCoroutine);

            if (failedNameText != null)
                failedNameText.text = $"FAILED: {mission.missionName}";

            failedCoroutine = StartCoroutine(SlidePopup(
                failedPanel, failedCanvasGroup, failedHiddenPos, failedShownPos));
        }

        private IEnumerator SlidePopup(RectTransform panel, CanvasGroup group,
            Vector2 hiddenPos, Vector2 shownPos)
        {
            panel.gameObject.SetActive(true);

            // Slide in
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = EaseOutBack(Mathf.Clamp01(elapsed / slideDuration));

                panel.anchoredPosition = Vector2.Lerp(hiddenPos, shownPos, t);
                if (group != null) group.alpha = t;

                yield return null;
            }

            panel.anchoredPosition = shownPos;
            if (group != null) group.alpha = 1f;

            // Hold
            yield return new WaitForSecondsRealtime(displayDuration);

            // Slide out
            elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);

                panel.anchoredPosition = Vector2.Lerp(shownPos, hiddenPos, t);
                if (group != null) group.alpha = 1f - t;

                yield return null;
            }

            panel.anchoredPosition = hiddenPos;
            if (group != null) group.alpha = 0f;
            panel.gameObject.SetActive(false);
        }

        private float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
        }
    }
}
