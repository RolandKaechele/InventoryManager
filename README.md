# InventoryManager

A standalone Unity package for managing player item inventories, with optional integration with MapLoaderFramework and SaveManager.

## Features

- Load item definitions from JSON (Resources folder or persistent data path)
- `AddItem`, `RemoveItem`, `HasItem`, `GetQuantity`, `UseItem` with max-stack enforcement
- Filter items by `ItemCategory` (KeyItem, Document, Equipment, Consumable, Collectible, Quest)
- Fully event-driven: `OnItemAdded`, `OnItemRemoved`, `OnItemUsed`
- `UseItemCallback` delegate for custom use behaviour
- `InventoryTrigger` component for scene pickups (OnStart, OnTriggerEnter, OnInteract)
- **Optional** MapLoaderFramework bridge — auto-grant `{mapId}_items` lists on map load
- **Optional** SaveManager bridge — serialize/deserialize inventory in save slots; wire trigger flags
- **Optional** LocalizationManager bridge — resolve localized item labels and descriptions via `labelLocalizationKey` / `descriptionLocalizationKey` fields (activated via `INVENTORYMANAGER_LM`)


## Installation

### A — Unity Package Manager (Git URL)

```
https://github.com/rolandkaechele/com.rolandkaechele.inventorymanager.git
```

### B — Local disk

Place the `InventoryManager/` folder anywhere under your project's `Assets/` directory.

### C — npm / postinstall

```bash
npm install
```

`postinstall.js` creates the required runtime folders under `Assets/`.


## Folder Structure

```
InventoryManager/
├── Runtime/
│   ├── InventoryData.cs              # Data classes (ItemData, InventorySlot, ItemCategory)
│   ├── InventoryManager.cs           # Main orchestrator (MonoBehaviour)
│   ├── InventoryTrigger.cs           # Scene pickup / award trigger
│   ├── MapLoaderInventoryBridge.cs   # Optional: MLF integration
│   ├── SaveInventoryBridge.cs        # Optional: SaveManager integration
│   └── LocalizationInventoryBridge.cs # Optional: LocalizationManager integration
├── Editor/
│   └── InventoryManagerEditor.cs     # Custom inspector
├── Examples/
│   ├── Items/
│   │   ├── example_item_key.json
│   │   └── example_item_document.json
│   └── Scripts/
│       └── example_item_pickup.lua
├── package.json
├── postinstall.js
├── LICENSE
└── README.md
```


## Quick Start

### 1. Scene Setup

Add `InventoryManager` to a persistent GameObject.

### 2. Item Definitions

Place JSON files in `Assets/Resources/Items/`.

```json
{
  "id": "key_reactor_room",
  "label": "Reactor Room Key",
  "description": "Opens the reinforced door to the reactor control room.",
  "iconResource": "Items/Icons/key_reactor",
  "category": 0,
  "canDrop": false,
  "canUse": false,
  "maxStack": 1
}
```

### 3. Add / Remove Items from Code

```csharp
var inv = FindFirstObjectByType<InventoryManager.Runtime.InventoryManager>();

inv.AddItem("key_reactor_room");
Debug.Log(inv.HasItem("key_reactor_room")); // true

inv.RemoveItem("key_reactor_room");
Debug.Log(inv.GetQuantity("key_reactor_room")); // 0
```

### 4. React to Changes

```csharp
inv.OnItemAdded   += (id, qty) => Debug.Log($"+{qty} {id}");
inv.OnItemRemoved += (id, qty) => Debug.Log($"-{qty} {id}");
inv.OnItemUsed    += (id)      => Debug.Log($"Used {id}");
```

### 5. InventoryTrigger Component

Add `InventoryTrigger` to any scene object to award or remove an item:

| Field | Description |
| ----- | ----------- |
| `Item Id` | Item to add or remove |
| `Quantity` | How many |
| `Add On Trigger` | True = add; false = remove |
| `Trigger Mode` | `OnStart`, `OnTriggerEnter`, or `OnInteract` |
| `Require Flag Not Set` | Only fire if this SaveManager flag is NOT set |
| `Set Flag On Pickup` | Set this SaveManager flag after firing |
| `Trigger Tag` | Collider tag filter (default: `"Player"`) |
| `Disable After Trigger` | Deactivate the GameObject after firing |


## Item JSON Format

| Field | Type | Description |
| ----- | ---- | ----------- |
| `id` | string | Unique item identifier |
| `label` | string | Display name |
| `description` | string | Long description |
| `labelLocalizationKey` | string | Localization key override for label |
| `descriptionLocalizationKey` | string | Localization key override for description |
| `iconResource` | string | `Resources.Load` path for icon sprite |
| `category` | int | `0`=KeyItem `1`=Document `2`=Equipment `3`=Consumable `4`=Collectible `5`=Quest |
| `canDrop` | bool | Whether the player can discard this item |
| `canUse` | bool | Whether `UseItem` is allowed |
| `useEffect` | string | Custom effect ID passed to `UseItemCallback` |
| `maxStack` | int | Maximum quantity per slot (default: `1`) |


## MapLoaderFramework Integration

Enable the scripting define `INVENTORYMANAGER_MLF` in Unity Player Settings.

Add `MapLoaderInventoryBridge` to the same GameObject as `InventoryManager` and `MapLoaderFramework`.

On each map load the bridge looks for a JSON file in Resources named `Items/{mapId}{grantSuffix}` (default suffix: `"_items"`). If found — and if `firstVisitOnly` is true, only on first visit — each listed item is granted.

### Grant List JSON Format

```json
{
  "items": [
    { "itemId": "doc_transmission_log", "quantity": 1 },
    { "itemId": "key_reactor_room",     "quantity": 1 }
  ]
}
```

### Inspector Fields

| Field | Default | Description |
| ----- | ------- | ----------- |
| `Grant Suffix` | `"_items"` | Appended to map ID to form the grant-list asset name |
| `First Visit Only` | `true` | Skip if `"items_granted_{grantId}"` flag is already set |


## SaveManager Integration

Enable the scripting define `INVENTORYMANAGER_SM` in Unity Player Settings.

Add `SaveInventoryBridge` to the same GameObject.

- Inventory is serialized to SaveManager custom data under key `"inventory"` (configurable)
- On `PostLoadCallback`, inventory is automatically restored
- `InventoryTrigger` flag delegates (`ConditionCheck`, `FlagSetCallback`) are wired automatically
- `MapLoaderInventoryBridge` flag delegates are wired automatically if both bridges are present

### Inspector Fields

| Field | Default | Description |
| ----- | ------- | ----------- |
| `Save Key` | `"inventory"` | Key used in SaveManager custom data |
| `Auto Save On Change` | `false` | Persist to save slot on every add/remove |

### Manual Persistence

```csharp
var bridge = FindFirstObjectByType<SaveInventoryBridge>();
bridge.PersistInventory();  // write current inventory to save slot
bridge.RestoreInventory();  // restore from save slot
```


## LocalizationManager Integration

Enable the scripting define `INVENTORYMANAGER_LM` in Unity Player Settings.

Add `LocalizationInventoryBridge` to any GameObject in your scene.

The bridge exposes helper methods for resolving item display names and descriptions in the active language. Use these instead of reading `ItemData.label` directly whenever displaying items in UI. Falls back to the raw `label` / `description` field when no localization key is set or the key resolves to `null`.

```csharp
var bridge = FindFirstObjectByType<LocalizationInventoryBridge>();

// By ItemData
string label = bridge.GetLabel(itemData);
string desc  = bridge.GetDescription(itemData);

// By item ID (looks up ItemData internally)
string label = bridge.GetLabel("key_reactor_room");
string desc  = bridge.GetDescription("key_reactor_room");
```

### `LocalizationInventoryBridge` API

| Member | Description |
| ------ | ----------- |
| `GetLabel(ItemData item) → string` | Localized label; falls back to `item.label` |
| `GetLabel(string itemId) → string` | Looks up `ItemData` then resolves label |
| `GetDescription(ItemData item) → string` | Localized description; falls back to `item.description` |
| `GetDescription(string itemId) → string` | Looks up `ItemData` then resolves description |


## Runtime API

### InventoryManager

| Member | Description |
| ------ | ----------- |
| `AddItem(string id, int qty = 1)` | Add items to inventory |
| `RemoveItem(string id, int qty = 1) → bool` | Remove items; returns false if insufficient |
| `HasItem(string id) → bool` | True if at least one is held |
| `GetQuantity(string id) → int` | Current quantity (0 if absent) |
| `UseItem(string id) → bool` | Use an item; returns false if unusable |
| `GetAllItems() → List<InventorySlot>` | All currently held slots |
| `GetItemsByCategory(ItemCategory) → List<InventorySlot>` | Filter by category |
| `GetItemData(string id) → ItemData` | Look up definition (null if not loaded) |
| `GetAllItemData()` | All loaded definitions |
| `LoadAllItems()` | Reload all item definitions from disk |
| `ClearInventory()` | Empty the inventory without firing events |
| `OnItemAdded` | `Action<string, int>` |
| `OnItemRemoved` | `Action<string, int>` |
| `OnItemUsed` | `Action<string>` |
| `UseItemCallback` | `Action<string>` — override use behaviour |

### ItemCategory Enum

| Value | Int | Description |
| ----- | --- | ----------- |
| `KeyItem` | 0 | Story-critical keys and access items |
| `Document` | 1 | Readable documents and logs |
| `Equipment` | 2 | Wearable or equippable gear |
| `Consumable` | 3 | Single-use items |
| `Collectible` | 4 | Collectible curiosities |
| `Quest` | 5 | Quest-specific items |


## Examples

See `Examples/Items/example_item_key.json` for a non-usable key item.  
See `Examples/Items/example_item_document.json` for a readable document with a `useEffect`.  
See `Examples/Scripts/example_item_pickup.lua` for a Lua-side usage example.


## Dependencies

| Dependency | Role |
| ---------- | ---- |
| Unity 2022.3+ | Required |
| MapLoaderFramework | Optional — enable `INVENTORYMANAGER_MLF` |
| SaveManager | Optional — enable `INVENTORYMANAGER_SM` |
| LocalizationManager | Optional — enable `INVENTORYMANAGER_LM` |


## Repository

`https://github.com/rolandkaechele/com.rolandkaechele.inventorymanager`


## License

MIT — see [LICENSE](LICENSE)
