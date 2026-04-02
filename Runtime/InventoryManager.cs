using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace InventoryManager.Runtime
{
    [AddComponentMenu("InventoryManager/Inventory Manager")]
    [DisallowMultipleComponent]
    public class InventoryManager : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Subfolder under Resources/ where item definition JSON files are stored.")]
        [SerializeField] private string resourcesFolder = "Items";

        [Tooltip("If true, item definitions are also loaded from persistentDataPath/Items/.")]
        [SerializeField] private bool loadFromPersistentDataPath = true;

        // ─── Events ──────────────────────────────────────────────────────────────
        /// <summary>Fired when one or more of an item are added.</summary>
        public event Action<string, int> OnItemAdded;

        /// <summary>Fired when one or more of an item are removed.</summary>
        public event Action<string, int> OnItemRemoved;

        /// <summary>Fired when UseItem is called successfully.</summary>
        public event Action<string> OnItemUsed;

        // ─── Delegates ───────────────────────────────────────────────────────────
        /// <summary>Override item use behaviour. Receives itemId. If null, useEffect is logged.</summary>
        public Action<string> UseItemCallback;

        // ─── State ───────────────────────────────────────────────────────────────
        private readonly Dictionary<string, ItemData> _items   = new Dictionary<string, ItemData>();
        private readonly Dictionary<string, int>      _slots   = new Dictionary<string, int>();
        private bool _loaded;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            LoadAllItems();
        }

        // ─── Item Definition Loading ──────────────────────────────────────────────
        public void LoadAllItems()
        {
            _items.Clear();

            // Resources folder
            var textAssets = Resources.LoadAll<TextAsset>(resourcesFolder);
            foreach (var ta in textAssets)
            {
                TryParseItem(ta.text);
            }

            // Persistent data path
            if (loadFromPersistentDataPath)
            {
                string dir = Path.Combine(Application.persistentDataPath, "Items");
                if (Directory.Exists(dir))
                {
                    foreach (var file in Directory.GetFiles(dir, "*.json"))
                    {
                        try
                        {
                            TryParseItem(File.ReadAllText(file));
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[InventoryManager] Failed to load item from {file}: {ex.Message}");
                        }
                    }
                }
            }

            _loaded = true;
            Debug.Log($"[InventoryManager] Loaded {_items.Count} item definition(s).");
        }

        private void TryParseItem(string json)
        {
            try
            {
                var data = JsonUtility.FromJson<ItemData>(json);
                if (data == null || string.IsNullOrEmpty(data.id))
                {
                    Debug.LogWarning("[InventoryManager] Skipped item with missing id.");
                    return;
                }
                data.rawJson = json;
                _items[data.id] = data;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[InventoryManager] Failed to parse item JSON: {ex.Message}");
            }
        }

        // ─── Public API ───────────────────────────────────────────────────────────
        /// <summary>Add qty of itemId to the inventory.</summary>
        public void AddItem(string itemId, int qty = 1)
        {
            if (string.IsNullOrEmpty(itemId) || qty <= 0) return;

            if (_items.TryGetValue(itemId, out ItemData data))
            {
                int current = GetQuantity(itemId);
                int effective = data.maxStack > 0
                    ? Mathf.Min(qty, data.maxStack - current)
                    : qty;

                if (effective <= 0)
                {
                    Debug.Log($"[InventoryManager] Stack full for item '{itemId}' (maxStack={data.maxStack}).");
                    return;
                }

                _slots[itemId] = current + effective;
            }
            else
            {
                // Item definition not loaded — still track it
                Debug.LogWarning($"[InventoryManager] ItemData not found for '{itemId}'; adding without definition.");
                _slots[itemId] = GetQuantity(itemId) + qty;
            }

            OnItemAdded?.Invoke(itemId, qty);
        }

        /// <summary>Remove qty of itemId from inventory. Returns false if insufficient quantity.</summary>
        public bool RemoveItem(string itemId, int qty = 1)
        {
            if (string.IsNullOrEmpty(itemId) || qty <= 0) return false;

            int current = GetQuantity(itemId);
            if (current < qty) return false;

            int newQty = current - qty;
            if (newQty <= 0)
                _slots.Remove(itemId);
            else
                _slots[itemId] = newQty;

            OnItemRemoved?.Invoke(itemId, qty);
            return true;
        }

        /// <summary>Returns true if at least one of itemId is in the inventory.</summary>
        public bool HasItem(string itemId) => GetQuantity(itemId) > 0;

        /// <summary>Returns the current quantity of itemId (0 if not present).</summary>
        public int GetQuantity(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            return _slots.TryGetValue(itemId, out int qty) ? qty : 0;
        }

        /// <summary>Use an item. Fires UseItemCallback or logs the useEffect string. Returns false if item not present.</summary>
        public bool UseItem(string itemId)
        {
            if (!HasItem(itemId))
            {
                Debug.LogWarning($"[InventoryManager] UseItem: '{itemId}' not in inventory.");
                return false;
            }

            if (!_items.TryGetValue(itemId, out ItemData data) || !data.canUse)
            {
                Debug.LogWarning($"[InventoryManager] UseItem: '{itemId}' is not usable.");
                return false;
            }

            if (UseItemCallback != null)
            {
                UseItemCallback.Invoke(itemId);
            }
            else
            {
                Debug.Log($"[InventoryManager] UseItem '{itemId}' — useEffect: {data.useEffect ?? "(none)"}");
            }

            OnItemUsed?.Invoke(itemId);
            return true;
        }

        /// <summary>Returns all currently held inventory slots.</summary>
        public List<InventorySlot> GetAllItems()
        {
            return _slots.Select(kvp => new InventorySlot { itemId = kvp.Key, quantity = kvp.Value }).ToList();
        }

        /// <summary>Returns all held items that match the given category (requires item definitions to be loaded).</summary>
        public List<InventorySlot> GetItemsByCategory(ItemCategory category)
        {
            var result = new List<InventorySlot>();
            foreach (var kvp in _slots)
            {
                if (_items.TryGetValue(kvp.Key, out ItemData data) && data.category == category)
                    result.Add(new InventorySlot { itemId = kvp.Key, quantity = kvp.Value });
            }
            return result;
        }

        /// <summary>Returns the ItemData definition for the given id, or null if not loaded.</summary>
        public ItemData GetItemData(string itemId)
        {
            return _items.TryGetValue(itemId, out ItemData data) ? data : null;
        }

        /// <summary>Returns all loaded item definitions.</summary>
        public IReadOnlyDictionary<string, ItemData> GetAllItemData() => _items;

        /// <summary>Replace the inventory contents entirely (used by SaveInventoryBridge on load).</summary>
        internal void SetSlots(Dictionary<string, int> slots)
        {
            _slots.Clear();
            foreach (var kvp in slots)
                _slots[kvp.Key] = kvp.Value;
        }

        /// <summary>Returns a copy of the raw slot dictionary (used by SaveInventoryBridge).</summary>
        internal Dictionary<string, int> GetSlotsCopy()
        {
            return new Dictionary<string, int>(_slots);
        }

        /// <summary>Clear all held items without firing events.</summary>
        public void ClearInventory()
        {
            _slots.Clear();
        }
    }
}
