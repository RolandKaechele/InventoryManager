#if INVENTORYMANAGER_MGM
using System;
using UnityEngine;
using MiniGameManager.Runtime;

namespace InventoryManager.Runtime
{
    /// <summary>
    /// Optional bridge between InventoryManager and MiniGameManager.
    /// Enable define <c>INVENTORYMANAGER_MGM</c> in Player Settings › Scripting Define Symbols.
    /// <para>
    /// When a mini-game completes, the bridge looks for a reward grant list JSON file in
    /// <c>Resources/MiniGames/</c> named <c>"{miniGameId}_rewards.json"</c> and adds the listed
    /// items to the player's inventory.  Rewards are only granted once per mini-game id unless
    /// <see cref="rewardOnEveryCompletion"/> is enabled.
    /// </para>
    /// <para><b>Reward list JSON format:</b></para>
    /// <code>
    /// {
    ///   "items": [
    ///     { "itemId": "energy_crystal", "quantity": 3 },
    ///     { "itemId": "key_card" }
    ///   ]
    /// }
    /// </code>
    /// </summary>
    [AddComponentMenu("InventoryManager/Mini Game Inventory Bridge")]
    [DisallowMultipleComponent]
    public class MiniGameInventoryBridge : MonoBehaviour
    {
        // ─── Inspector ───────────────────────────────────────────────────────────
        [Tooltip("Suffix appended to the mini-game id to form the reward resource name.")]
        [SerializeField] private string rewardSuffix = "_rewards";

        [Tooltip("If false (default), rewards are granted only once per mini-game id. " +
                 "If true, every completion grants rewards.")]
        [SerializeField] private bool rewardOnEveryCompletion = false;

        // ─── References ──────────────────────────────────────────────────────────
        private InventoryManager _inventory;
        private MiniGameManager.Runtime.MiniGameManager _mgr;

        /// <summary>
        /// Optional condition check — wired by <c>SaveInventoryBridge</c> to check
        /// whether a reward flag is already set.
        /// </summary>
        public Func<string, bool> ConditionCheck;

        /// <summary>
        /// Optional flag set callback — wired by <c>SaveInventoryBridge</c> to persist
        /// reward-granted flags.
        /// </summary>
        public Action<string> FlagSetCallback;

        // ─── Unity ───────────────────────────────────────────────────────────────
        private void Awake()
        {
            _inventory = GetComponent<InventoryManager>() ?? FindFirstObjectByType<InventoryManager>();
            _mgr       = GetComponent<MiniGameManager.Runtime.MiniGameManager>()
                         ?? FindFirstObjectByType<MiniGameManager.Runtime.MiniGameManager>();

            if (_inventory == null) Debug.LogWarning("[MiniGameInventoryBridge] InventoryManager not found.");
            if (_mgr       == null) Debug.LogWarning("[MiniGameInventoryBridge] MiniGameManager not found.");
        }

        private void OnEnable()
        {
            if (_mgr != null) _mgr.OnMiniGameCompleted += OnCompleted;
        }

        private void OnDisable()
        {
            if (_mgr != null) _mgr.OnMiniGameCompleted -= OnCompleted;
        }

        // ─── Handler ─────────────────────────────────────────────────────────────
        private void OnCompleted(MiniGameResult result)
        {
            if (_inventory == null || result == null) return;

            string rewardId  = result.miniGameId + rewardSuffix;
            string flag      = "rewards_granted_" + rewardId;

            if (!rewardOnEveryCompletion && CheckFlag(flag))
            {
                Debug.Log($"[MiniGameInventoryBridge] Rewards already granted for '{result.miniGameId}'.");
                return;
            }

            var asset = Resources.Load<TextAsset>("MiniGames/" + rewardId);
            if (asset == null) return; // No reward file — silent skip

            try
            {
                var list = JsonUtility.FromJson<MiniGameRewardList>(asset.text);
                if (list?.items == null) return;

                foreach (var entry in list.items)
                {
                    if (!string.IsNullOrEmpty(entry.itemId))
                        _inventory.AddItem(entry.itemId, entry.quantity > 0 ? entry.quantity : 1);
                }

                if (!rewardOnEveryCompletion) SetFlag(flag);
                Debug.Log($"[MiniGameInventoryBridge] Granted {list.items.Length} item(s) for mini-game '{result.miniGameId}'.");
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[MiniGameInventoryBridge] Failed to parse reward list '{rewardId}': {ex.Message}");
            }
        }

        // ─── Flag helpers ─────────────────────────────────────────────────────────
        private bool CheckFlag(string flag) => ConditionCheck != null && ConditionCheck(flag);
        private void SetFlag(string flag)   => FlagSetCallback?.Invoke(flag);
    }

    [Serializable]
    internal class MiniGameRewardList
    {
        public MiniGameRewardEntry[] items;
    }

    [Serializable]
    internal class MiniGameRewardEntry
    {
        public string itemId;
        public int    quantity = 1;
    }
}
#else
namespace InventoryManager.Runtime
{
    /// <summary>No-op stub. Enable INVENTORYMANAGER_MGM in Player Settings to activate the bridge.</summary>
    [UnityEngine.AddComponentMenu("InventoryManager/Mini Game Inventory Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class MiniGameInventoryBridge : UnityEngine.MonoBehaviour { }
}
#endif
