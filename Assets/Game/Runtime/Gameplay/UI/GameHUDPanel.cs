using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameHUDPanel : UIPanel
{
    [Header("Number")]
    [SerializeField] private TMP_Text currentNumberText;
    [SerializeField] private Image numberFill;
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
    [SerializeField] private HoverTooltipPresenter inventoryTooltip;

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
        EnsureRelicSlotCapacity(4);
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
        inventoryTooltip?.Hide();
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
        RefreshNumber(
            numberResource.CurrentValue,
            numberResource.MaximumValue
        );
    }

    private void HandleNumberChanged(NumberChange change)
    {
        RefreshNumber(change.CurrentValue, numberResource.MaximumValue);
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
        List<CollectibleDefinition> relics = new();
        if (playerInventory != null)
        {
            foreach (CollectibleStack stack in playerInventory.Stacks)
            {
                if (stack?.Definition != null &&
                    stack.Definition.Kind == CollectibleKind.Relic)
                {
                    for (int i = 0; i < stack.Count; i++)
                    {
                        relics.Add(stack.Definition);
                    }
                }
            }
            relics.Sort((left, right) =>
                left.InventoryOrder.CompareTo(
                    right.InventoryOrder
                ));
        }

        RefreshSlots(itemIcons, itemCounts, items, inventoryTooltip);
        RefreshRelicSlots(relics);
    }

    private void RefreshRelicSlots(
        IReadOnlyList<CollectibleDefinition> relics
    )
    {
        for (int i = 0; i < relicIcons.Length; i++)
        {
            CollectibleDefinition definition =
                i < relics.Count ? relics[i] : null;
            if (relicIcons[i] != null)
            {
                relicIcons[i].sprite = definition?.Icon;
                relicIcons[i].enabled = definition?.Icon != null;
            }
            if (relicCounts.Length > i && relicCounts[i] != null)
            {
                relicCounts[i].text = string.Empty;
            }

            HoverTooltipTarget target =
                relicIcons[i]?.GetComponentInParent<HoverTooltipTarget>();
            target?.Bind(
                inventoryTooltip,
                definition?.DisplayName,
                definition?.Description
            );
        }
    }

    private void EnsureRelicSlotCapacity(int requiredCapacity)
    {
        if (relicIcons == null ||
            relicCounts == null ||
            relicIcons.Length >= requiredCapacity ||
            relicIcons.Length == 0)
        {
            return;
        }

        int originalLength = relicIcons.Length;
        Image sourceIcon = relicIcons[originalLength - 1];
        HoverTooltipTarget sourceSlot =
            sourceIcon?.GetComponentInParent<HoverTooltipTarget>();
        if (sourceSlot == null)
        {
            return;
        }

        Array.Resize(ref relicIcons, requiredCapacity);
        Array.Resize(ref relicCounts, requiredCapacity);
        RectTransform previousRect = sourceSlot.transform as RectTransform;
        for (int i = originalLength; i < requiredCapacity; i++)
        {
            GameObject clone = Instantiate(
                sourceSlot.gameObject,
                sourceSlot.transform.parent
            );
            clone.name = $"RelicSlot{i + 1}";
            RectTransform cloneRect = clone.transform as RectTransform;
            if (cloneRect != null && previousRect != null)
            {
                float spacing = previousRect.rect.width + 8f;
                cloneRect.anchoredPosition =
                    previousRect.anchoredPosition +
                    new Vector2(spacing, 0f);
            }

            relicIcons[i] = FindNamedChild<Image>(clone.transform, "Icon");
            relicCounts[i] = FindNamedChild<TMP_Text>(
                clone.transform,
                "Count"
            );
            previousRect = cloneRect;
        }
    }

    private static T FindNamedChild<T>(
        Transform parent,
        string childName
    ) where T : Component
    {
        foreach (T component in parent.GetComponentsInChildren<T>(true))
        {
            if (component.gameObject.name == childName)
            {
                return component;
            }
        }

        return null;
    }

    private static void RefreshSlots(
        Image[] icons,
        TMP_Text[] counts,
        IReadOnlyList<CollectibleStack> stacks,
        HoverTooltipPresenter tooltip
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

            HoverTooltipTarget target =
                icons[i]?.GetComponentInParent<HoverTooltipTarget>();
            target?.Bind(
                tooltip,
                definition?.DisplayName,
                definition?.Description
            );
        }
    }

    private void RefreshNumber(int value, int maximumValue)
    {
        if (currentNumberText != null)
        {
            currentNumberText.text = $"{value}/{maximumValue}";
        }

        if (numberFill != null)
        {
            numberFill.fillAmount = maximumValue > 0
                ? Mathf.Clamp01((float)value / maximumValue)
                : 0f;
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
        LocalizationFontManager.ApplyTo(popup.gameObject);

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
