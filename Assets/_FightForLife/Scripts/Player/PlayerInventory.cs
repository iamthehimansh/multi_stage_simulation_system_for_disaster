using System.Collections.Generic;
using UnityEngine;

namespace FightForLife.Player
{
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private int maxSlots = 3;
        [SerializeField] private Transform dropPoint;
        [SerializeField] private float dropForce = 3f;

        // ───────────────────────── Events ──────────────────────────────────
        public event System.Action<ItemData, int> OnItemPickup;
        public event System.Action<ItemData, int> OnItemDrop;
        public event System.Action<ItemData> OnItemUse;
        public event System.Action<int> OnActiveItemChanged;

        // ───────────────────────── Public State ────────────────────────────
        public int MaxSlots => maxSlots;
        public int ActiveSlotIndex { get; private set; }
        public int ItemCount => items.Count;

        // ───────────────────────── Private ─────────────────────────────────
        private readonly List<InventorySlot> items = new List<InventorySlot>();

        // ───────────────────────── Lifecycle ───────────────────────────────

        private void Update()
        {
            HandleSlotSelection();
            HandleUseInput();
        }

        // ───────────────────────── Slot Selection ──────────────────────────

        private void HandleSlotSelection()
        {
            // Number keys
            for (int i = 0; i < maxSlots && i < 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1 + i))
                {
                    SetActiveSlot(i);
                    return;
                }
            }

            // Scroll wheel
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll > 0.01f)
            {
                SetActiveSlot((ActiveSlotIndex + 1) % Mathf.Max(1, items.Count));
            }
            else if (scroll < -0.01f)
            {
                SetActiveSlot((ActiveSlotIndex - 1 + Mathf.Max(1, items.Count)) % Mathf.Max(1, items.Count));
            }
        }

        private void SetActiveSlot(int index)
        {
            if (index == ActiveSlotIndex) return;
            ActiveSlotIndex = index;
            OnActiveItemChanged?.Invoke(ActiveSlotIndex);
        }

        private void HandleUseInput()
        {
            // Use active item with the Use key (can be triggered externally too)
            if (Input.GetKeyDown(KeyCode.G))
            {
                UseActiveItem();
            }
        }

        // ───────────────────────── Public API ──────────────────────────────

        /// <summary>
        /// Attempts to add an item to the inventory. Returns true if successful.
        /// </summary>
        public bool TryAddItem(ItemData data)
        {
            if (data == null) return false;

            // Try to stack with existing
            if (data.isStackable)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    if (items[i].Data.itemId == data.itemId && items[i].Count < data.maxStack)
                    {
                        items[i] = new InventorySlot(items[i].Data, items[i].Count + 1);
                        OnItemPickup?.Invoke(data, i);
                        return true;
                    }
                }
            }

            // Try to use a new slot
            if (items.Count >= maxSlots) return false;

            int slotIndex = items.Count;
            items.Add(new InventorySlot(data, 1));
            OnItemPickup?.Invoke(data, slotIndex);
            return true;
        }

        /// <summary>
        /// Returns the ItemData in the active slot, or null if empty.
        /// </summary>
        public ItemData GetActiveItem()
        {
            if (ActiveSlotIndex < 0 || ActiveSlotIndex >= items.Count) return null;
            return items[ActiveSlotIndex].Data;
        }

        /// <summary>
        /// Returns the InventorySlot at the given index, or default if out of range.
        /// </summary>
        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= items.Count) return default;
            return items[index];
        }

        /// <summary>
        /// Check if the inventory contains an item with the given id.
        /// </summary>
        public bool HasItem(string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Data != null && items[i].Data.itemId == id)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a read-only list of all inventory slots.
        /// </summary>
        public IReadOnlyList<InventorySlot> GetAllItems()
        {
            return items.AsReadOnly();
        }

        /// <summary>
        /// Uses the active item. Consumables are removed after use.
        /// </summary>
        public void UseActiveItem()
        {
            if (ActiveSlotIndex < 0 || ActiveSlotIndex >= items.Count) return;

            InventorySlot slot = items[ActiveSlotIndex];
            if (slot.Data == null) return;

            OnItemUse?.Invoke(slot.Data);

            if (slot.Data.isConsumable)
            {
                int newCount = slot.Count - 1;
                if (newCount <= 0)
                {
                    items.RemoveAt(ActiveSlotIndex);
                    ActiveSlotIndex = Mathf.Clamp(ActiveSlotIndex, 0, Mathf.Max(0, items.Count - 1));
                    OnActiveItemChanged?.Invoke(ActiveSlotIndex);
                }
                else
                {
                    items[ActiveSlotIndex] = new InventorySlot(slot.Data, newCount);
                }
            }
        }

        /// <summary>
        /// Drops the currently active item into the world.
        /// </summary>
        public void DropActiveItem()
        {
            if (ActiveSlotIndex < 0 || ActiveSlotIndex >= items.Count) return;

            InventorySlot slot = items[ActiveSlotIndex];
            if (slot.Data == null) return;

            // Spawn world prefab
            if (slot.Data.worldPrefab != null)
            {
                Vector3 spawnPos = dropPoint != null ? dropPoint.position : transform.position + transform.forward + Vector3.up;
                GameObject dropped = Instantiate(slot.Data.worldPrefab, spawnPos, Quaternion.identity);

                Rigidbody rb = dropped.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(transform.forward * dropForce, ForceMode.VelocityChange);
                }
            }

            OnItemDrop?.Invoke(slot.Data, ActiveSlotIndex);

            int newCount = slot.Count - 1;
            if (newCount <= 0)
            {
                items.RemoveAt(ActiveSlotIndex);
                ActiveSlotIndex = Mathf.Clamp(ActiveSlotIndex, 0, Mathf.Max(0, items.Count - 1));
                OnActiveItemChanged?.Invoke(ActiveSlotIndex);
            }
            else
            {
                items[ActiveSlotIndex] = new InventorySlot(slot.Data, newCount);
            }
        }

        /// <summary>
        /// Removes a specific item by id. Returns true if removed.
        /// </summary>
        public bool RemoveItem(string id)
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (items[i].Data != null && items[i].Data.itemId == id)
                {
                    OnItemDrop?.Invoke(items[i].Data, i);
                    items.RemoveAt(i);
                    ActiveSlotIndex = Mathf.Clamp(ActiveSlotIndex, 0, Mathf.Max(0, items.Count - 1));
                    OnActiveItemChanged?.Invoke(ActiveSlotIndex);
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Sets the maximum number of inventory slots at runtime.
        /// </summary>
        public void SetMaxSlots(int slots)
        {
            maxSlots = Mathf.Max(1, slots);

            // Drop excess items
            while (items.Count > maxSlots)
            {
                int last = items.Count - 1;
                OnItemDrop?.Invoke(items[last].Data, last);
                items.RemoveAt(last);
            }

            ActiveSlotIndex = Mathf.Clamp(ActiveSlotIndex, 0, Mathf.Max(0, items.Count - 1));
        }
    }

    /// <summary>
    /// Represents a single slot in the inventory.
    /// </summary>
    public readonly struct InventorySlot
    {
        public readonly ItemData Data;
        public readonly int Count;

        public InventorySlot(ItemData data, int count)
        {
            Data = data;
            Count = count;
        }

        public bool IsEmpty => Data == null;
    }
}
