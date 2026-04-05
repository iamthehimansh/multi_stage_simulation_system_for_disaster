using UnityEngine;

namespace FightForLife.Player
{
    public class PlayerHealth : MonoBehaviour
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;

        [Header("Stamina")]
        [SerializeField] private float maxStamina = 100f;
        [SerializeField] private float currentStamina;
        [SerializeField] private float staminaRegenRate = 10f;
        [SerializeField] private float staminaRegenDelay = 2f;

        [Header("Oxygen")]
        [SerializeField] private float maxOxygen = 30f;
        [SerializeField] private float currentOxygen;

        public float Health => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercent => currentHealth / maxHealth;
        public float Stamina => currentStamina;
        public float MaxStamina => maxStamina;
        public float StaminaPercent => currentStamina / maxStamina;
        public float Oxygen => currentOxygen;
        public float MaxOxygen => maxOxygen;
        public float OxygenPercent => currentOxygen / maxOxygen;
        public bool IsAlive => currentHealth > 0;
        public bool IsSubmerged { get; set; }

        public bool IsInvincible { get; private set; }

        public event System.Action OnDeath;
        public event System.Action<float> OnHealthChanged;
        public event System.Action<float> OnStaminaChanged;
        public event System.Action<float> OnOxygenChanged;

        private float lastStaminaUseTime;

        private void Start()
        {
            currentHealth = maxHealth;
            currentStamina = maxStamina;
            currentOxygen = maxOxygen;
        }

        private void Update()
        {
            // Stamina regeneration
            if (Time.time - lastStaminaUseTime > staminaRegenDelay && currentStamina < maxStamina)
            {
                currentStamina = Mathf.Min(maxStamina, currentStamina + staminaRegenRate * Time.deltaTime);
                OnStaminaChanged?.Invoke(StaminaPercent);
            }

            // Oxygen management
            if (IsSubmerged)
            {
                currentOxygen = Mathf.Max(0, currentOxygen - Time.deltaTime);
                OnOxygenChanged?.Invoke(OxygenPercent);

                if (currentOxygen <= 0)
                    TakeDamage(10f * Time.deltaTime); // Drowning
            }
            else if (currentOxygen < maxOxygen)
            {
                currentOxygen = Mathf.Min(maxOxygen, currentOxygen + Time.deltaTime * 5f);
                OnOxygenChanged?.Invoke(OxygenPercent);
            }
        }

        public void SetInvincible(bool invincible)
        {
            IsInvincible = invincible;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || IsInvincible) return;

            currentHealth = Mathf.Max(0, currentHealth - amount);
            OnHealthChanged?.Invoke(HealthPercent);

            if (currentHealth <= 0)
                Die();
        }

        public void Heal(float amount)
        {
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(HealthPercent);
        }

        public bool UseStamina(float amount)
        {
            if (currentStamina < amount) return false;

            currentStamina -= amount;
            lastStaminaUseTime = Time.time;
            OnStaminaChanged?.Invoke(StaminaPercent);
            return true;
        }

        private void Die()
        {
            Debug.Log("[Player] Player has died.");
            OnDeath?.Invoke();
        }
    }
}
