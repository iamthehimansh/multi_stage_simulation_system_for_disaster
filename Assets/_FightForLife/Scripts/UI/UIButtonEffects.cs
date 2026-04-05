using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

namespace FightForLife.UI
{
    [RequireComponent(typeof(Button))]
    public class UIButtonEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("Scale")]
        [SerializeField] private float hoverScale = 1.05f;
        [SerializeField] private float pressScale = 0.95f;
        [SerializeField] private float scaleSpeed = 12f;

        [Header("Color")]
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.9f);
        [SerializeField] private Color hoverColor = Color.white;
        [SerializeField] private Color pressColor = new Color(0.8f, 0.8f, 0.8f, 1f);

        [Header("Audio")]
        [SerializeField] private MainMenuController menuController;

        private Vector3 originalScale;
        private float targetScale = 1f;
        private Image image;
        private TextMeshProUGUI tmpText;
        private Color targetColor;

        private void Awake()
        {
            originalScale = transform.localScale;
            image = GetComponent<Image>();
            tmpText = GetComponentInChildren<TextMeshProUGUI>();
            targetColor = normalColor;
        }

        private void Update()
        {
            transform.localScale = Vector3.Lerp(transform.localScale,
                originalScale * targetScale, Time.deltaTime * scaleSpeed);

            if (image != null)
                image.color = Color.Lerp(image.color, targetColor, Time.deltaTime * scaleSpeed);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = hoverScale;
            targetColor = hoverColor;
            if (menuController != null) menuController.PlayHoverSound();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = 1f;
            targetColor = normalColor;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = pressScale;
            targetColor = pressColor;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = hoverScale;
            targetColor = hoverColor;
        }
    }
}
