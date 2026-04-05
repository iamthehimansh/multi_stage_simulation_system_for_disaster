using UnityEngine;
using TMPro;

namespace FightForLife.UI
{
    public class ComingSoonUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private TextMeshProUGUI versionText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Pulse Animation")]
        [SerializeField] private TextMeshProUGUI pulseText;
        [SerializeField] private float pulseSpeed = 1.5f;
        [SerializeField] private float pulseMinAlpha = 0.3f;

        [Header("Typewriter Effect")]
        [SerializeField] private bool useTypewriter = true;
        [SerializeField] private float typewriterSpeed = 0.04f;

        [Header("Fade In")]
        [SerializeField] private float fadeInDuration = 1f;

        private float fadeTimer;
        private bool isFading;
        private string fullDescription;
        private int charIndex;
        private float typeTimer;
        private bool isTyping;

        private void OnEnable()
        {
            fadeTimer = 0f;
            isFading = true;

            if (canvasGroup != null)
                canvasGroup.alpha = 0f;

            if (useTypewriter && descriptionText != null)
            {
                fullDescription = descriptionText.text;
                descriptionText.text = "";
                charIndex = 0;
                typeTimer = 0f;
                isTyping = true;
            }
        }

        private void Update()
        {
            // Fade in
            if (isFading && canvasGroup != null)
            {
                fadeTimer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(fadeTimer / fadeInDuration);
                if (fadeTimer >= fadeInDuration)
                    isFading = false;
            }

            // Pulse "Coming Soon" text
            if (pulseText != null)
            {
                float alpha = Mathf.Lerp(pulseMinAlpha, 1f,
                    (Mathf.Sin(Time.time * pulseSpeed * Mathf.PI) + 1f) * 0.5f);
                Color c = pulseText.color;
                c.a = alpha;
                pulseText.color = c;
            }

            // Typewriter effect
            if (isTyping && descriptionText != null && fullDescription != null)
            {
                typeTimer += Time.deltaTime;
                if (typeTimer >= typewriterSpeed)
                {
                    typeTimer = 0f;
                    if (charIndex < fullDescription.Length)
                    {
                        // Handle rich text tags
                        if (fullDescription[charIndex] == '<')
                        {
                            int closeIndex = fullDescription.IndexOf('>', charIndex);
                            if (closeIndex != -1)
                            {
                                charIndex = closeIndex + 1;
                            }
                        }
                        else
                        {
                            charIndex++;
                        }
                        descriptionText.text = fullDescription.Substring(0, charIndex);
                    }
                    else
                    {
                        isTyping = false;
                    }
                }
            }
        }
    }
}
