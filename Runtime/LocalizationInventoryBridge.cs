#if INVENTORYMANAGER_LM
using UnityEngine;
using LocalizationManager.Runtime;

namespace InventoryManager.Runtime
{
    /// <summary>
    /// Optional bridge between InventoryManager and LocalizationManager.
    /// Enable define <c>INVENTORYMANAGER_LM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// Provides helper methods for resolving localized item labels and descriptions.
    /// Item display names in the game UI should be requested through this bridge
    /// rather than reading <c>ItemData.label</c> directly, so the active language is applied.
    /// </para>
    /// </summary>
    [AddComponentMenu("InventoryManager/Localization Inventory Bridge")]
    [DisallowMultipleComponent]
    public class LocalizationInventoryBridge : MonoBehaviour
    {
        private InventoryManager _inventory;
        private LocalizationManager.Runtime.LocalizationManager _localization;

        private void Awake()
        {
            _inventory    = GetComponent<InventoryManager>() ?? FindFirstObjectByType<InventoryManager>();
            _localization = GetComponent<LocalizationManager.Runtime.LocalizationManager>()
                            ?? FindFirstObjectByType<LocalizationManager.Runtime.LocalizationManager>();

            if (_inventory    == null) Debug.LogWarning("[LocalizationInventoryBridge] InventoryManager not found.");
            if (_localization == null) Debug.LogWarning("[LocalizationInventoryBridge] LocalizationManager not found.");
        }

        // -------------------------------------------------------------------------
        // Public API
        // -------------------------------------------------------------------------

        /// <summary>
        /// Returns the localized display label for the given item data.
        /// Falls back to <c>ItemData.label</c> when no localization key is set or the
        /// key resolves to <see langword="null"/>.
        /// </summary>
        public string GetLabel(ItemData item)
        {
            if (item == null) return string.Empty;
            return ResolveField(item.labelLocalizationKey, item.label);
        }

        /// <summary>
        /// Returns the localized display label for the item with the given id.
        /// Returns an empty string when the item is not found.
        /// </summary>
        public string GetLabel(string itemId)
        {
            var item = _inventory?.GetItemData(itemId);
            return GetLabel(item);
        }

        /// <summary>
        /// Returns the localized description for the given item data.
        /// Falls back to <c>ItemData.description</c> when no localization key is set or the
        /// key resolves to <see langword="null"/>.
        /// </summary>
        public string GetDescription(ItemData item)
        {
            if (item == null) return string.Empty;
            return ResolveField(item.descriptionLocalizationKey, item.description);
        }

        /// <summary>
        /// Returns the localized description for the item with the given id.
        /// Returns an empty string when the item is not found.
        /// </summary>
        public string GetDescription(string itemId)
        {
            var item = _inventory?.GetItemData(itemId);
            return GetDescription(item);
        }

        // -------------------------------------------------------------------------
        // Internal
        // -------------------------------------------------------------------------

        private string ResolveField(string locKey, string fallback)
        {
            if (!string.IsNullOrEmpty(locKey) && _localization != null)
                return _localization.GetText(locKey) ?? fallback;
            return fallback;
        }
    }
}
#else
namespace InventoryManager.Runtime
{
    /// <summary>No-op stub. Enable INVENTORYMANAGER_LM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("InventoryManager/Localization Inventory Bridge")]
    public class LocalizationInventoryBridge : UnityEngine.MonoBehaviour
    {
        private void Awake() =>
            UnityEngine.Debug.Log("[LocalizationInventoryBridge] LocalizationManager integration is disabled. " +
                                  "Add the scripting define INVENTORYMANAGER_LM to enable it.");

        public string GetLabel(object item)       => string.Empty;
        public string GetLabel(string itemId)     => string.Empty;
        public string GetDescription(object item) => string.Empty;
        public string GetDescription(string id)   => string.Empty;
    }
}
#endif
