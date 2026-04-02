#if INVENTORYMANAGER_SM
using System;
using System.Collections.Generic;
using UnityEngine;
using SaveManager.Runtime;

namespace InventoryManager.Runtime
{
    /// <summary>
    /// Optional bridge between InventoryManager and SaveManager.
    /// Enable define INVENTORYMANAGER_SM in Player Settings > Scripting Define Symbols.
    ///
    /// Serializes/deserializes the inventory as a JSON string stored under SaveManager
    /// custom data key "inventory". Automatically saves inventory state whenever
    /// an item is added or removed (if autoSaveOnChange is true).
    /// </summary>
    [AddComponentMenu("InventoryManager/Save Inventory Bridge")]
    [DisallowMultipleComponent]
    public class SaveInventoryBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Custom data key used to store inventory in a save slot.")]
        [SerializeField] private string saveKey = "inventory";

        [Tooltip("Automatically persist inventory to the save slot on every item change.")]
        [SerializeField] private bool autoSaveOnChange = false;

        // ─── References ──────────────────────────────────────────────────────────
        private InventoryManager _inventory;
        private SaveManager.Runtime.SaveManager _save;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _inventory = GetComponent<InventoryManager>() ?? FindFirstObjectByType<InventoryManager>();
            _save      = GetComponent<SaveManager.Runtime.SaveManager>()
                         ?? FindFirstObjectByType<SaveManager.Runtime.SaveManager>();

            if (_inventory == null)
            {
                Debug.LogWarning("[SaveInventoryBridge] InventoryManager not found.");
                return;
            }
            if (_save == null)
            {
                Debug.LogWarning("[SaveInventoryBridge] SaveManager not found.");
                return;
            }

            // Wire InventoryTrigger flag delegates
            WireInventoryTriggers();

            // Wire map bridge if present
            var bridge = GetComponent<MapLoaderInventoryBridge>();
            if (bridge != null)
            {
                bridge.ConditionCheck  = flag => _save.IsSet(flag);
                bridge.FlagSetCallback = flag => _save.SetFlag(flag);
            }

            // Subscribe to save events
            _save.PostLoadCallback += OnSaveLoaded;

            // Subscribe to inventory changes for auto-save
            _inventory.OnItemAdded   += OnInventoryChanged;
            _inventory.OnItemRemoved += OnInventoryChanged;
        }

        private void OnDestroy()
        {
            if (_save != null)
                _save.PostLoadCallback -= OnSaveLoaded;

            if (_inventory != null)
            {
                _inventory.OnItemAdded   -= OnInventoryChanged;
                _inventory.OnItemRemoved -= OnInventoryChanged;
            }
        }

        // ─── Serialization ───────────────────────────────────────────────────────
        /// <summary>Persist the current inventory into the active save slot's custom data.</summary>
        public void PersistInventory()
        {
            if (_inventory == null || _save == null) return;

            var slots   = _inventory.GetSlotsCopy();
            var wrapper = new InventorySnapshot { slots = new List<InventorySlotEntry>() };

            foreach (var kvp in slots)
                wrapper.slots.Add(new InventorySlotEntry { itemId = kvp.Key, quantity = kvp.Value });

            string json = JsonUtility.ToJson(wrapper);
            _save.SetCustom(saveKey, json);
        }

        /// <summary>Restore inventory from the loaded save slot's custom data.</summary>
        public void RestoreInventory()
        {
            if (_inventory == null || _save == null) return;

            string json = _save.GetCustom(saveKey);
            if (string.IsNullOrEmpty(json)) return;

            try
            {
                var wrapper = JsonUtility.FromJson<InventorySnapshot>(json);
                if (wrapper?.slots == null) return;

                var dict = new Dictionary<string, int>();
                foreach (var entry in wrapper.slots)
                {
                    if (!string.IsNullOrEmpty(entry.itemId))
                        dict[entry.itemId] = entry.quantity;
                }

                _inventory.SetSlots(dict);
                Debug.Log($"[SaveInventoryBridge] Restored {dict.Count} inventory slot(s) from save.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[SaveInventoryBridge] Failed to restore inventory: {ex.Message}");
            }
        }

        // ─── Callbacks ───────────────────────────────────────────────────────────
        private void OnSaveLoaded(int slotIndex, SaveData data)
        {
            RestoreInventory();
        }

        private void OnInventoryChanged(string itemId, int qty)
        {
            if (autoSaveOnChange)
                PersistInventory();
        }

        // ─── Trigger Wiring ──────────────────────────────────────────────────────
        private void WireInventoryTriggers()
        {
            var triggers = FindObjectsByType<InventoryTrigger>(FindObjectsSortMode.None);
            foreach (var t in triggers)
            {
                t.ConditionCheck  = flag => _save.IsSet(flag);
                t.FlagSetCallback = flag => _save.SetFlag(flag);
            }
        }

        // ─── Serialization helpers ────────────────────────────────────────────────
        [Serializable]
        private class InventorySnapshot
        {
            public List<InventorySlotEntry> slots;
        }

        [Serializable]
        private class InventorySlotEntry
        {
            public string itemId;
            public int    quantity;
        }
    }
}
#else
// INVENTORYMANAGER_SM not defined — bridge is inactive.
namespace InventoryManager.Runtime
{
    /// <summary>No-op stub. Enable INVENTORYMANAGER_SM in Player Settings to activate the bridge.</summary>
    public class SaveInventoryBridge : UnityEngine.MonoBehaviour { }
}
#endif
