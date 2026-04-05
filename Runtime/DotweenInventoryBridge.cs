#if INVENTORYMANAGER_DOTWEEN
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace InventoryManager.Runtime
{
    /// <summary>
    /// Optional bridge that adds DOTween-driven animations to inventory events:
    /// item-pickup toasts slide in when items are added, and item slots pulse-scale
    /// in the inventory grid when quantities change.
    /// Enable define <c>INVENTORYMANAGER_DOTWEEN</c> in Player Settings › Scripting Define Symbols.
    /// Requires <b>DOTween Pro</b>.
    /// <para>
    /// Assign <see cref="pickupToastRoot"/> and <see cref="pickupToastGroup"/> to your
    /// item-pickup notification UI. Optionally assign <see cref="inventoryGrid"/> to the
    /// parent transform of your inventory slot cells for per-slot punch animations.
    /// </para>
    /// </summary>
    [AddComponentMenu("InventoryManager/DOTween Bridge")]
    [DisallowMultipleComponent]
    public class DotweenInventoryBridge : MonoBehaviour
    {
        [Header("Pickup Toast")]
        [Tooltip("RectTransform of the item-pickup notification panel.")]
        [SerializeField] private RectTransform pickupToastRoot;

        [Tooltip("CanvasGroup on the pickup toast panel.")]
        [SerializeField] private CanvasGroup pickupToastGroup;

        [Tooltip("Vertical pixel offset from which the toast slides in from below.")]
        [SerializeField] private float toastSlideOffset = 40f;

        [Tooltip("Duration for the toast to slide and fade in.")]
        [SerializeField] private float toastInDuration = 0.25f;

        [Tooltip("How long the toast is held visible.")]
        [SerializeField] private float toastHoldDuration = 1.5f;

        [Tooltip("Duration for the toast to fade out.")]
        [SerializeField] private float toastOutDuration = 0.2f;

        [Tooltip("DOTween ease for the toast slide-in.")]
        [SerializeField] private Ease toastEase = Ease.OutBack;

        [Header("Inventory Grid")]
        [Tooltip("Parent transform holding inventory slot cells. When set, the slot matching " +
                 "an added item's index is punch-scaled.")]
        [SerializeField] private Transform inventoryGrid;

        [Tooltip("Scale punch applied to the affected slot.")]
        [SerializeField] private Vector3 slotPunch = new Vector3(0.12f, 0.12f, 0f);

        [Tooltip("Duration of the slot punch animation.")]
        [SerializeField] private float slotPunchDuration = 0.3f;

        // -------------------------------------------------------------------------

        private InventoryManager _inv;
        private Sequence          _toastSeq;

        private void Awake()
        {
            _inv = GetComponent<InventoryManager>() ?? FindFirstObjectByType<InventoryManager>();
            if (_inv == null) Debug.LogWarning("[InventoryManager/DotweenInventoryBridge] InventoryManager not found.");

            if (pickupToastGroup != null) pickupToastGroup.alpha = 0f;
        }

        private void OnEnable()
        {
            if (_inv == null) return;
            _inv.OnItemAdded   += OnItemAdded;
            _inv.OnItemRemoved += OnItemRemoved;
            _inv.OnItemUsed    += OnItemUsed;
        }

        private void OnDisable()
        {
            if (_inv == null) return;
            _inv.OnItemAdded   -= OnItemAdded;
            _inv.OnItemRemoved -= OnItemRemoved;
            _inv.OnItemUsed    -= OnItemUsed;
        }

        // -------------------------------------------------------------------------

        private void OnItemAdded(string itemId, int quantity)
        {
            ShowPickupToast();
            PunchInventorySlot(itemId);
        }

        private void OnItemRemoved(string itemId, int quantity)
        {
            PunchInventorySlot(itemId);
        }

        private void OnItemUsed(string itemId)
        {
            PunchInventorySlot(itemId);
        }

        // -------------------------------------------------------------------------

        private void ShowPickupToast()
        {
            if (pickupToastRoot == null) return;

            _toastSeq?.Kill();
            _toastSeq = DOTween.Sequence();

            Vector2 finalPos = pickupToastRoot.anchoredPosition;
            pickupToastRoot.anchoredPosition = finalPos + Vector2.down * toastSlideOffset;
            if (pickupToastGroup != null) pickupToastGroup.alpha = 0f;

            _toastSeq
                .Join(pickupToastRoot.DOAnchorPos(finalPos, toastInDuration).SetEase(toastEase));

            if (pickupToastGroup != null)
                _toastSeq.Join(pickupToastGroup.DOFade(1f, toastInDuration));

            _toastSeq.AppendInterval(toastHoldDuration);

            if (pickupToastGroup != null)
                _toastSeq.Append(pickupToastGroup.DOFade(0f, toastOutDuration));
        }

        private void PunchInventorySlot(string itemId)
        {
            if (inventoryGrid == null) return;
            // Find slot by name convention "Slot_<itemId>".
            var slot = inventoryGrid.Find("Slot_" + itemId);
            if (slot == null && inventoryGrid.childCount > 0)
                slot = inventoryGrid.GetChild(0); // fallback: first slot
            if (slot == null) return;
            DOTween.Kill(slot);
            slot.DOPunchScale(slotPunch, slotPunchDuration, 5, 0.3f);
        }
    }
}
#else
namespace InventoryManager.Runtime
{
    /// <summary>No-op stub — enable define <c>INVENTORYMANAGER_DOTWEEN</c> to activate.</summary>
    [UnityEngine.AddComponentMenu("InventoryManager/DOTween Bridge")]
    [UnityEngine.DisallowMultipleComponent]
    public class DotweenInventoryBridge : UnityEngine.MonoBehaviour { }
}
#endif
