using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUDPanel : UIPanel
{
    [Header("Number")]
    [SerializeField] private TMP_Text currentNumberText;
    [SerializeField] private RectTransform deltaPopupRoot;
    [SerializeField] private NumberDeltaPopup numberDeltaPopupPrefab;
    [SerializeField] private Vector3 popupWorldOffset = new(0f, 1.1f, 0f);

    [Header("Controls")]
    [SerializeField] private Button settingsButton;

    [Header("Inventory display")]
    [SerializeField] private Image[] itemIcons = new Image[4];
    [SerializeField] private TMP_Text[] itemCounts = new TMP_Text[4];
    [SerializeField] private Image[] relicIcons = new Image[3];
    [SerializeField] private TMP_Text[] relicCounts = new TMP_Text[3];

    private readonly Queue<NumberDeltaPopup> popupPool = new();
    private readonly Queue<NumberChange> pendingChanges = new();
    private readonly List<NumberDeltaPopup> activePopups = new();
    private NumberDeltaPopup activePopup;
    private NumberResource numberResource;
    private PlayerInventory playerInventory;
    private Canvas parentCanvas;

    public event Action SettingsRequested;

    public override void OnInit()
    {
        parentCanvas = GetComponentInParent<Canvas>();
        if (deltaPopupRoot == null)
        {
            deltaPopupRoot = transform as RectTransform;
        }

        settingsButton?.onClick.AddListener(HandleSettingsClicked);
    }

    public override void OnOpen(object data = null)
    {
        BindNumberResource(NumberResource.Instance);
        BindInventory(
            NumberResource.Instance != null
                ? NumberResource.Instance.GetComponent<PlayerInventory>()
                : null
        );
    }

    public override void OnClose()
    {
        BindNumberResource(null);
        BindInventory(null);
        RecycleAllPopups();
    }

    private void OnDestroy()
    {
        settingsButton?.onClick.RemoveListener(HandleSettingsClicked);
        BindNumberResource(null);
        BindInventory(null);
    }

    private void BindNumberResource(NumberResource resource)
    {
        if (numberResource != null)
        {
            numberResource.Changed -= HandleNumberChanged;
        }

        numberResource = resource;
        if (numberResource == null)
        {
            return;
        }

        numberResource.Changed += HandleNumberChanged;
        RefreshNumber(numberResource.CurrentValue);
    }

    private void HandleNumberChanged(NumberChange change)
    {
        RefreshNumber(change.CurrentValue);
        if (change.Delta != 0)
        {
            ShowDelta(change);
        }
    }

    private void BindInventory(PlayerInventory inventory)
    {
        if (playerInventory != null)
        {
            playerInventory.Changed -= RefreshInventory;
        }

        playerInventory = inventory;
        if (playerInventory != null)
        {
            playerInventory.Changed += RefreshInventory;
        }
        RefreshInventory();
    }

    private void RefreshInventory()
    {
        List<CollectibleStack> items =
            playerInventory?.GetOrderedItemStacks() ?? new List<CollectibleStack>();
        List<CollectibleStack> relics = new();
        if (playerInventory != null)
        {
            foreach (CollectibleStack stack in playerInventory.Stacks)
            {
                if (stack?.Definition != null &&
                    stack.Definition.Kind == CollectibleKind.Relic)
                {
                    relics.Add(stack);
                }
            }
            relics.Sort((left, right) =>
                left.Definition.InventoryOrder.CompareTo(
                    right.Definition.InventoryOrder
                ));
        }

        RefreshSlots(itemIcons, itemCounts, items);
        RefreshSlots(relicIcons, relicCounts, relics);
    }

    private static void RefreshSlots(
        Image[] icons,
        TMP_Text[] counts,
        IReadOnlyList<CollectibleStack> stacks
    )
    {
        for (int i = 0; i < icons.Length; i++)
        {
            CollectibleStack stack = i < stacks.Count ? stacks[i] : null;
            CollectibleDefinition definition = stack?.Definition;
            if (icons[i] != null)
            {
                icons[i].sprite = definition?.Icon;
                icons[i].enabled = definition?.Icon != null;
            }
            if (counts.Length > i && counts[i] != null)
            {
                counts[i].text =
                    stack != null && stack.Count > 1
                        ? $"x{stack.Count}"
                        : string.Empty;
            }
        }
    }

    private void RefreshNumber(int value)
    {
        if (currentNumberText != null)
        {
            currentNumberText.text = value.ToString();
        }
    }

    private void ShowDelta(NumberChange change)
    {
        if (numberDeltaPopupPrefab == null || deltaPopupRoot == null)
        {
            return;
        }

        pendingChanges.Enqueue(change);
        TryPlayNextDelta();
    }

    private void TryPlayNextDelta()
    {
        if (activePopup != null || pendingChanges.Count == 0)
        {
            return;
        }

        NumberChange change = pendingChanges.Dequeue();
        NumberDeltaPopup popup = popupPool.Count > 0
            ? popupPool.Dequeue()
            : Instantiate(numberDeltaPopupPrefab, deltaPopupRoot);

        activePopup = popup;
        activePopups.Add(popup);
        popup.Play(
            change,
            WorldToPopupPosition(change.WorldPosition + popupWorldOffset),
            RecyclePopup
        );
    }

    private Vector2 WorldToPopupPosition(Vector3 worldPosition)
    {
        Camera worldCamera = Camera.main;
        if (worldCamera == null)
        {
            return Vector2.zero;
        }

        Vector2 screenPosition = worldCamera.WorldToScreenPoint(worldPosition);
        Camera uiCamera = parentCanvas != null &&
                          parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay
            ? parentCanvas.worldCamera
            : null;

        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            deltaPopupRoot,
            screenPosition,
            uiCamera,
            out Vector2 localPosition
        )
            ? localPosition
            : Vector2.zero;
    }

    private void RecyclePopup(NumberDeltaPopup popup)
    {
        if (popup == null)
        {
            return;
        }

        activePopups.Remove(popup);
        popup.gameObject.SetActive(false);
        popupPool.Enqueue(popup);
        if (activePopup == popup)
        {
            activePopup = null;
        }

        TryPlayNextDelta();
    }

    private void RecycleAllPopups()
    {
        pendingChanges.Clear();
        for (int i = activePopups.Count - 1; i >= 0; i--)
        {
            NumberDeltaPopup popup = activePopups[i];
            if (popup == null)
            {
                continue;
            }

            popup.StopAndHide();
            popupPool.Enqueue(popup);
        }

        activePopups.Clear();
        activePopup = null;
    }

    private void HandleSettingsClicked()
    {
        SettingsRequested?.Invoke();
    }
}
