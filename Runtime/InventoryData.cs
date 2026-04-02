using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventoryManager.Runtime
{
    public enum ItemCategory
    {
        KeyItem     = 0,
        Document    = 1,
        Equipment   = 2,
        Consumable  = 3,
        Collectible = 4,
        Quest       = 5
    }

    [Serializable]
    public class ItemData
    {
        public string id;
        public string label;
        public string description;
        public string labelLocalizationKey;
        public string descriptionLocalizationKey;

        /// <summary>Resources-relative path to the item's icon sprite.</summary>
        public string iconResource;

        public ItemCategory category;
        public bool canDrop;
        public bool canUse;

        /// <summary>Name of a Lua script or custom event key passed to UseItemCallback.</summary>
        public string useEffect;

        public int maxStack = 1;

        [NonSerialized] public string rawJson;
    }

    [Serializable]
    public class InventorySlot
    {
        public string itemId;
        public int quantity;
    }
}
