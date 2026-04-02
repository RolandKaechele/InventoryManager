using UnityEngine;

namespace InventoryManager.Runtime
{
    public enum InventoryTriggerMode
    {
        OnStart,
        OnTriggerEnter,
        OnInteract
    }

    [AddComponentMenu("InventoryManager/Inventory Trigger")]
    public class InventoryTrigger : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Item ID to add or remove.")]
        [SerializeField] private string itemId;

        [Tooltip("Number of items to add or remove.")]
        [SerializeField] private int quantity = 1;

        [Tooltip("If true, item is added. If false, item is removed.")]
        [SerializeField] private bool addOnTrigger = true;

        [SerializeField] private InventoryTriggerMode triggerMode = InventoryTriggerMode.OnTriggerEnter;

        [Tooltip("Only trigger if this flag is NOT already set (prevents re-pickup).")]
        [SerializeField] private string requireFlagNotSet;

        [Tooltip("Set this flag via FlagSetCallback after the trigger fires.")]
        [SerializeField] private string setFlagOnPickup;

        [Tooltip("Collider tag that activates this trigger (OnTriggerEnter mode).")]
        [SerializeField] private string triggerTag = "Player";

        [Tooltip("Disable or destroy this GameObject after triggering.")]
        [SerializeField] private bool disableAfterTrigger = true;

        // ─── Delegates ───────────────────────────────────────────────────────────
        /// <summary>Check a flag condition. Wired by SaveInventoryBridge when INVENTORYMANAGER_SM is active.</summary>
        public System.Func<string, bool> ConditionCheck;

        /// <summary>Set a flag by name. Wired by SaveInventoryBridge when INVENTORYMANAGER_SM is active.</summary>
        public System.Action<string> FlagSetCallback;

        // ─── Internal ────────────────────────────────────────────────────────────
        private InventoryManager _inventory;
        private bool _triggered;

        private void Start()
        {
            _inventory = FindFirstObjectByType<InventoryManager>();
            if (_inventory == null)
                Debug.LogWarning("[InventoryTrigger] No InventoryManager found in scene.");

            if (triggerMode == InventoryTriggerMode.OnStart)
                Fire();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (triggerMode != InventoryTriggerMode.OnTriggerEnter) return;
            if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
            Fire();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (triggerMode != InventoryTriggerMode.OnTriggerEnter) return;
            if (!string.IsNullOrEmpty(triggerTag) && !other.CompareTag(triggerTag)) return;
            Fire();
        }

        /// <summary>Call this manually to trigger an OnInteract-mode pickup.</summary>
        public void Interact() => Fire();

        private void Fire()
        {
            if (_triggered) return;
            if (_inventory == null) return;

            // Check flag condition
            if (!string.IsNullOrEmpty(requireFlagNotSet) && ConditionCheck != null)
            {
                if (ConditionCheck(requireFlagNotSet))
                {
                    Debug.Log($"[InventoryTrigger] Skipped '{itemId}': flag '{requireFlagNotSet}' is already set.");
                    return;
                }
            }

            if (addOnTrigger)
                _inventory.AddItem(itemId, quantity);
            else
                _inventory.RemoveItem(itemId, quantity);

            _triggered = true;

            if (!string.IsNullOrEmpty(setFlagOnPickup))
                FlagSetCallback?.Invoke(setFlagOnPickup);

            if (disableAfterTrigger)
                gameObject.SetActive(false);
        }
    }
}
