using UnityEngine;

namespace FightForLife.Player
{
    public enum ItemType
    {
        Consumable,
        Equipment,
        Craftable,
        Quest
    }

    [CreateAssetMenu(fileName = "NewItem", menuName = "Fight For Life/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Identity")]
        public string itemId;
        public string itemName;
        [Tooltip("Legacy alias for itemName")]
        public string displayName;
        [TextArea(2, 4)]
        public string description;

        [Header("Visuals")]
        public Sprite icon;
        public GameObject worldPrefab;
        public GameObject heldPrefab;

        [Header("Type")]
        public ItemType itemType = ItemType.Consumable;

        [Header("Properties")]
        public float weight = 1f;
        public bool isConsumable;
        public bool isStackable;
        public int maxStack = 1;

        [Header("Effects")]
        public float healAmount;
        public float staminaRestore;
        public float oxygenRestore;
        public float swimSpeedBonus;
        public float damageReduction;
        public float waterResistance;
        public float visionBonus;
        public float warmthBonus;

        /// <summary>
        /// Returns the display name, preferring itemName over legacy displayName.
        /// </summary>
        public string DisplayName => !string.IsNullOrEmpty(itemName) ? itemName : displayName;
    }
}
