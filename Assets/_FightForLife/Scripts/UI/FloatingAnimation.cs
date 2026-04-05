using UnityEngine;

namespace FightForLife.UI
{
    public class FloatingAnimation : MonoBehaviour
    {
        [SerializeField] private float amplitude = 10f;
        [SerializeField] private float frequency = 0.5f;
        [SerializeField] private float phaseOffset = 0f;

        private RectTransform rectTransform;
        private Vector2 startPos;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
                startPos = rectTransform.anchoredPosition;
        }

        private void Update()
        {
            if (rectTransform != null)
            {
                float offset = Mathf.Sin((Time.time + phaseOffset) * frequency * Mathf.PI * 2f) * amplitude;
                rectTransform.anchoredPosition = startPos + new Vector2(0, offset);
            }
        }
    }
}
