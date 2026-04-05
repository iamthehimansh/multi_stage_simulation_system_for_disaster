using UnityEngine;
using TMPro;

namespace FightForLife.UI
{
    public class CreditsUI : MonoBehaviour
    {
        [SerializeField] private RectTransform scrollContent;
        [SerializeField] private float scrollSpeed = 30f;
        [SerializeField] private float resetPosition = -1500f;

        private Vector2 startPos;
        private bool isScrolling;

        private void OnEnable()
        {
            if (scrollContent != null)
            {
                startPos = scrollContent.anchoredPosition;
                isScrolling = true;
            }
        }

        private void OnDisable()
        {
            if (scrollContent != null)
                scrollContent.anchoredPosition = startPos;
        }

        private void Update()
        {
            if (!isScrolling || scrollContent == null) return;

            var pos = scrollContent.anchoredPosition;
            pos.y += scrollSpeed * Time.deltaTime;
            scrollContent.anchoredPosition = pos;

            if (pos.y > Mathf.Abs(resetPosition))
            {
                scrollContent.anchoredPosition = startPos;
            }
        }
    }
}
