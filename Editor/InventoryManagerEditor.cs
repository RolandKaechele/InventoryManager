using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using InventoryManager.Runtime;

namespace InventoryManager.Editor
{
    [CustomEditor(typeof(Runtime.InventoryManager))]
    public class InventoryManagerEditor : UnityEditor.Editor
    {
        private string _lookupId = string.Empty;
        private string _lookupResult = string.Empty;
        private string _addId  = string.Empty;
        private int    _addQty = 1;

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var mgr = (Runtime.InventoryManager)target;

            EditorGUILayout.Space(8);

            // ── Runtime Inventory ────────────────────────────────────────────────
            if (Application.isPlaying)
            {
                EditorGUILayout.LabelField("Runtime Inventory", EditorStyles.boldLabel);

                var slots = mgr.GetAllItems();
                if (slots.Count == 0)
                {
                    EditorGUILayout.HelpBox("Inventory is empty.", MessageType.None);
                }
                else
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("Item ID",   EditorStyles.miniLabel, GUILayout.MinWidth(160));
                    EditorGUILayout.LabelField("Category",  EditorStyles.miniLabel, GUILayout.Width(90));
                    EditorGUILayout.LabelField("Qty",       EditorStyles.miniLabel, GUILayout.Width(40));
                    EditorGUILayout.EndHorizontal();

                    foreach (var slot in slots)
                    {
                        var data = mgr.GetItemData(slot.itemId);
                        string category = data != null ? data.category.ToString() : "—";

                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(slot.itemId, GUILayout.MinWidth(160));
                        EditorGUILayout.LabelField(category,    GUILayout.Width(90));
                        EditorGUILayout.LabelField(slot.quantity.ToString(), GUILayout.Width(40));
                        if (GUILayout.Button("Remove", GUILayout.Width(60)))
                        {
                            mgr.RemoveItem(slot.itemId, slot.quantity);
                        }
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(6);

                // ── Item Lookup ──────────────────────────────────────────────────
                EditorGUILayout.LabelField("Item Lookup", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                _lookupId = EditorGUILayout.TextField("Item ID", _lookupId);
                if (GUILayout.Button("Check", GUILayout.Width(60)))
                {
                    int qty  = mgr.GetQuantity(_lookupId);
                    var data = mgr.GetItemData(_lookupId);
                    _lookupResult = data != null
                        ? $"Qty: {qty}  |  {data.label}  ({data.category}){(data.canUse ? "  [usable]" : "")}"
                        : qty > 0
                            ? $"Qty: {qty}  |  (no definition)"
                            : "Not in inventory.";
                }
                EditorGUILayout.EndHorizontal();
                if (!string.IsNullOrEmpty(_lookupResult))
                    EditorGUILayout.HelpBox(_lookupResult, MessageType.None);

                EditorGUILayout.Space(6);

                // ── Manual Add ───────────────────────────────────────────────────
                EditorGUILayout.LabelField("Manual Add / Remove", EditorStyles.boldLabel);
                EditorGUILayout.BeginHorizontal();
                _addId  = EditorGUILayout.TextField("Item ID", _addId);
                _addQty = EditorGUILayout.IntField(_addQty, GUILayout.Width(50));
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("Add"))
                    mgr.AddItem(_addId, _addQty);
                if (GUILayout.Button("Remove"))
                    mgr.RemoveItem(_addId, _addQty);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(6);

                // ── Loaded Definitions ───────────────────────────────────────────
                EditorGUILayout.LabelField("Loaded Item Definitions", EditorStyles.boldLabel);
                var allData = mgr.GetAllItemData();
                if (allData.Count == 0)
                {
                    EditorGUILayout.HelpBox("No item definitions loaded.", MessageType.Warning);
                }
                else
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.LabelField("ID",       EditorStyles.miniLabel, GUILayout.MinWidth(160));
                    EditorGUILayout.LabelField("Category", EditorStyles.miniLabel, GUILayout.Width(90));
                    EditorGUILayout.LabelField("Stack",    EditorStyles.miniLabel, GUILayout.Width(40));
                    EditorGUILayout.EndHorizontal();
                    foreach (var kvp in allData)
                    {
                        EditorGUILayout.BeginHorizontal();
                        EditorGUILayout.LabelField(kvp.Key,                        GUILayout.MinWidth(160));
                        EditorGUILayout.LabelField(kvp.Value.category.ToString(),  GUILayout.Width(90));
                        EditorGUILayout.LabelField(kvp.Value.maxStack.ToString(),  GUILayout.Width(40));
                        EditorGUILayout.EndHorizontal();
                    }
                    EditorGUILayout.EndVertical();
                }

                EditorGUILayout.Space(4);
                if (GUILayout.Button("Reload Item Definitions"))
                    mgr.LoadAllItems();

                Repaint();
            }
            else
            {
                EditorGUILayout.HelpBox("Enter Play Mode to view and modify inventory.", MessageType.Info);
            }
        }
    }
}
