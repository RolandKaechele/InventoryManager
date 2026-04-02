#if INVENTORYMANAGER_DLC
using System;
using UnityEngine;
using DlcManager.Runtime;

namespace InventoryManager.Runtime
{
    /// <summary>
    /// Optional bridge between InventoryManager and DlcManager.
    /// Enable define <c>INVENTORYMANAGER_DLC</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// When a DLC pack is unlocked, the bridge looks for a grant list JSON file in
    /// <c>Resources/DlcPacks/</c> named <c>"{packId}_items.json"</c> and adds the listed
    /// items to the player's inventory. Items in the pack's own <c>itemIds</c> list are also
    /// added automatically (quantity 1 each) unless <see cref="usePackItemIds"/> is disabled.
    /// </para>
    /// <para><b>Grant list JSON format:</b></para>
    /// <code>
    /// {
    ///   "items": [
    ///     { "itemId": "vip_badge", "quantity": 1 },
    ///     { "itemId": "bonus_credits", "quantity": 500 }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    [AddComponentMenu("InventoryManager/DLC Inventory Bridge")]
    [DisallowMultipleComponent]
    public class DlcInventoryBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Suffix appended to the pack id to form the item grant list resource name.")]
        [SerializeField] private string grantSuffix = "_items";

        [Tooltip("If true, also grant all itemIds listed in the DlcPackData.itemIds list (qty 1 each).")]
        [SerializeField] private bool usePackItemIds = true;

        // ─── References ──────────────────────────────────────────────────────────
        private InventoryManager _inventory;
        private DlcManager.Runtime.DlcManager _dlc;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _inventory = GetComponent<InventoryManager>() ?? FindFirstObjectByType<InventoryManager>();
            _dlc       = GetComponent<DlcManager.Runtime.DlcManager>()
                         ?? FindFirstObjectByType<DlcManager.Runtime.DlcManager>();

            if (_inventory == null) Debug.LogWarning("[DlcInventoryBridge] InventoryManager not found.");
            if (_dlc       == null) Debug.LogWarning("[DlcInventoryBridge] DlcManager not found.");
        }

        private void OnEnable()
        {
            if (_dlc != null) _dlc.OnPackUnlocked += OnPackUnlocked;
        }

        private void OnDisable()
        {
            if (_dlc != null) _dlc.OnPackUnlocked -= OnPackUnlocked;
        }

        // ─── Handler ─────────────────────────────────────────────────────────────
        private void OnPackUnlocked(string packId)
        {
            if (_inventory == null) return;

            // 1. Grant itemIds from the pack definition
            if (usePackItemIds)
            {
                var pack = _dlc?.GetPack(packId);
                if (pack?.itemIds != null)
                {
                    foreach (var itemId in pack.itemIds)
                    {
                        if (!string.IsNullOrEmpty(itemId))
                            _inventory.AddItem(itemId, 1);
                    }
                }
            }

            // 2. Grant items from an optional JSON grant list
            string grantId   = packId + grantSuffix;
            var    grantAsset = Resources.Load<TextAsset>("DlcPacks/" + grantId);
            if (grantAsset == null) return;

            try
            {
                var list = JsonUtility.FromJson<DlcItemGrantList>(grantAsset.text);
                if (list?.items == null) return;

                foreach (var entry in list.items)
                {
                    if (!string.IsNullOrEmpty(entry.itemId))
                        _inventory.AddItem(entry.itemId, entry.quantity > 0 ? entry.quantity : 1);
                }

                Debug.Log($"[DlcInventoryBridge] Granted {list.items.Length} item(s) for DLC pack '{packId}'.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DlcInventoryBridge] Failed to parse grant list '{grantId}': {ex.Message}");
            }
        }
    }

    [Serializable]
    internal class DlcItemGrantList
    {
        public DlcItemGrantEntry[] items;
    }

    [Serializable]
    internal class DlcItemGrantEntry
    {
        public string itemId;
        public int    quantity = 1;
    }
}
#else
namespace InventoryManager.Runtime
{
    /// <summary>No-op stub. Enable INVENTORYMANAGER_DLC in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("InventoryManager/DLC Inventory Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DlcInventoryBridge : UnityEngine.MonoBehaviour { }
}
#endif
