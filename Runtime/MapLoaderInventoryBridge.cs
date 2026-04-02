#if INVENTORYMANAGER_MLF
using UnityEngine;
using MapLoaderFramework.Runtime;

namespace InventoryManager.Runtime
{
    /// <summary>
    /// Optional bridge between InventoryManager and MapLoaderFramework.
    /// Enable define INVENTORYMANAGER_MLF in Player Settings > Scripting Define Symbols.
    ///
    /// On each map load the bridge can automatically grant a map's "first visit" items
    /// by checking for a JSON file named "{mapId}_items" in the Items/ folder.
    /// </summary>
    [AddComponentMenu("InventoryManager/Map Loader Inventory Bridge")]
    [DisallowMultipleComponent]
    public class MapLoaderInventoryBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Suffix appended to mapId to form the grant-list sequence ID. E.g. 'station_alpha_items'.")]
        [SerializeField] private string grantSuffix = "_items";

        [Tooltip("Only grant items if this is the first visit (checks flag 'items_granted_{grantId}').")]
        [SerializeField] private bool firstVisitOnly = true;

        // ─── References ──────────────────────────────────────────────────────────
        private InventoryManager _inventory;
        private MapLoaderFramework.Runtime.MapLoaderFramework _framework;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _inventory = GetComponent<InventoryManager>() ?? FindFirstObjectByType<InventoryManager>();
            _framework = GetComponent<MapLoaderFramework.Runtime.MapLoaderFramework>()
                         ?? FindFirstObjectByType<MapLoaderFramework.Runtime.MapLoaderFramework>();

            if (_inventory == null)
                Debug.LogWarning("[MapLoaderInventoryBridge] InventoryManager not found.");
            if (_framework == null)
                Debug.LogWarning("[MapLoaderInventoryBridge] MapLoaderFramework not found.");
        }

        private void OnEnable()
        {
            if (_framework != null)
                _framework.OnMapLoaded += OnMapLoaded;
        }

        private void OnDisable()
        {
            if (_framework != null)
                _framework.OnMapLoaded -= OnMapLoaded;
        }

        private void OnMapLoaded(MapData mapData)
        {
            if (_inventory == null || mapData == null) return;

            string grantId = mapData.id + grantSuffix;

            if (firstVisitOnly)
            {
                string seenFlag   = "items_granted_" + grantId;
                bool   alreadySet = _inventory.GetComponent<Runtime.InventoryManager>() != null
                    ? CheckFlag(seenFlag)
                    : CheckFlag(seenFlag);

                if (alreadySet)
                {
                    Debug.Log($"[MapLoaderInventoryBridge] Skipped grant '{grantId}': already visited.");
                    return;
                }
            }

            // Try to load a grant list JSON from Resources
            var grantAsset = Resources.Load<TextAsset>(_inventory.GetType().Name.Replace("Manager", "") + "/" + grantId);
            if (grantAsset == null) return;

            try
            {
                var grantList = JsonUtility.FromJson<MapItemGrantList>(grantAsset.text);
                if (grantList?.items == null) return;

                foreach (var entry in grantList.items)
                {
                    if (!string.IsNullOrEmpty(entry.itemId))
                        _inventory.AddItem(entry.itemId, entry.quantity > 0 ? entry.quantity : 1);
                }

                string flag = "items_granted_" + grantId;
                SetFlag(flag);
                Debug.Log($"[MapLoaderInventoryBridge] Granted {grantList.items.Length} item(s) for map '{mapData.id}'.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[MapLoaderInventoryBridge] Failed to parse grant list '{grantId}': {ex.Message}");
            }
        }

        // ─── Flag helpers (wired by SaveInventoryBridge if present) ──────────────
        public System.Func<string, bool>  ConditionCheck;
        public System.Action<string>      FlagSetCallback;

        private bool CheckFlag(string flag) => ConditionCheck != null && ConditionCheck(flag);
        private void SetFlag(string flag)   => FlagSetCallback?.Invoke(flag);
    }

    /// <summary>Deserialization container for a map item grant list JSON.</summary>
    [System.Serializable]
    internal class MapItemGrantList
    {
        public MapItemGrantEntry[] items;
    }

    [System.Serializable]
    internal class MapItemGrantEntry
    {
        public string itemId;
        public int    quantity = 1;
    }
}
#else
// INVENTORYMANAGER_MLF not defined — bridge is inactive.
namespace InventoryManager.Runtime
{
    /// <summary>No-op stub. Enable INVENTORYMANAGER_MLF in Player Settings to activate the bridge.</summary>
    public class MapLoaderInventoryBridge : UnityEngine.MonoBehaviour { }
}
#endif
