-- example_item_pickup.lua
-- Demonstrates awarding an item, checking inventory, and using an item via InventoryManager.
-- In a full integration this would be called from a Unity event or a custom Lua host.

-- Award a key item when this script runs
InventoryManager.AddItem("key_reactor_room", 1)

-- Check that the player now has it
if InventoryManager.HasItem("key_reactor_room") then
    Debug.Log("Player picked up the reactor room key.")
end

-- Award a readable document
InventoryManager.AddItem("doc_transmission_log", 1)

-- Use the document (triggers UseItemCallback -> "read_document" effect)
if InventoryManager.HasItem("doc_transmission_log") then
    InventoryManager.UseItem("doc_transmission_log")
end

-- Check quantity
local qty = InventoryManager.GetQuantity("doc_transmission_log")
Debug.Log("Transmission logs in inventory: " .. tostring(qty))
