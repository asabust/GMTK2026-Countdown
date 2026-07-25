using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BattleItemUseStatus
{
    Success,
    WrongPhase,
    NotOwned,
    InvalidItem,
    AlreadyActive,
    NumberAlreadyFull
}

public readonly struct BattleItemUseResult
{
    public BattleItemUseResult(BattleItemUseStatus status, string message)
    {
        Status = status;
        Message = message;
    }

    public BattleItemUseStatus Status { get; }
    public string Message { get; }
    public bool Succeeded => Status == BattleItemUseStatus.Success;
}

public sealed class BattleActionRequest
{
    public BattleActionRequest(
        EnemyActor enemy,
        int attackCost,
        Func<int> getAttackDamage,
        Func<bool> attack,
        int struggleDamage,
        Func<bool> canStruggle,
        Func<bool> struggle,
        PlayerInventory inventory,
        Func<CollectibleDefinition, BattleItemUseResult> useItem,
        Func<CollectibleDefinition, BattleItemUseResult> validateItem,
        bool isAutoPassing = false
    )
    {
        Enemy = enemy;
        AttackCost = attackCost;
        GetAttackDamage = getAttackDamage;
        Attack = attack;
        StruggleDamage = struggleDamage;
        CanStruggle = canStruggle;
        Struggle = struggle;
        Inventory = inventory;
        UseItem = useItem;
        ValidateItem = validateItem;
        IsAutoPassing = isAutoPassing;
    }

    public EnemyActor Enemy { get; }
    public int AttackCost { get; }
    public Func<int> GetAttackDamage { get; }
    public Func<bool> Attack { get; }
    public int StruggleDamage { get; }
    public Func<bool> CanStruggle { get; }
    public Func<bool> Struggle { get; }
    public PlayerInventory Inventory { get; }
    public Func<CollectibleDefinition, BattleItemUseResult> UseItem { get; }
    public Func<CollectibleDefinition, BattleItemUseResult> ValidateItem { get; }
    public bool IsAutoPassing { get; }
}

public class BattleActionPanel : UIPanel
{
    [Header("Shared")]
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private TMP_Text feedbackText;

    [Header("Primary menu")]
    [SerializeField] private GameObject primaryMenu;
    [SerializeField] private Button attackButton;
    [SerializeField] private TMP_Text attackButtonLabel;
    [SerializeField] private Button itemButton;
    [SerializeField] private TMP_Text itemButtonLabel;
    [SerializeField, HideInInspector] private Button struggleButton;

    [Header("Item menu")]
    [SerializeField] private GameObject itemMenu;
    [SerializeField] private Button[] itemSlotButtons = new Button[4];
    [SerializeField] private Image[] itemSlotIcons = new Image[4];
    [SerializeField] private TMP_Text[] itemSlotLabels = new TMP_Text[4];
    [SerializeField] private TMP_Text[] itemSlotCounts = new TMP_Text[4];
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Button backButton;

    private readonly List<CollectibleStack> displayedItems = new();
    private BattleActionRequest request;
    private int selectedItemIndex = -1;

    public override void OnInit()
    {
        attackButton?.onClick.AddListener(HandlePrimaryAction);
        itemButton?.onClick.AddListener(ShowItemMenu);
        backButton?.onClick.AddListener(ShowPrimaryMenu);

        for (int i = 0; i < itemSlotButtons.Length; i++)
        {
            int capturedIndex = i;
            itemSlotButtons[i]?.onClick.AddListener(
                () => HandleItemClicked(capturedIndex)
            );
            BattleItemSlotSelection relay =
                itemSlotButtons[i]?.GetComponent<BattleItemSlotSelection>();
            relay?.Bind(() => SelectItem(capturedIndex));
        }
    }

    public override void OnOpen(object data = null)
    {
        UnbindInventory();
        request = data as BattleActionRequest;
        if (request?.Inventory != null)
        {
            request.Inventory.Changed += HandleInventoryChanged;
        }

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        ShowPrimaryMenu();
    }

    public override void OnClose()
    {
        UnbindInventory();
        request = null;
        displayedItems.Clear();
        selectedItemIndex = -1;
    }

    private void HandlePrimaryAction()
    {
        if (request == null || request.Enemy == null)
        {
            return;
        }

        bool struggling = request.CanStruggle?.Invoke() == true;
        bool accepted = struggling
            ? request.Struggle?.Invoke() == true
            : request.Attack?.Invoke() == true;
        if (!accepted && feedbackText != null)
        {
            feedbackText.text = struggling
                ? "现在无法挣扎"
                : "数字不足，无法攻击";
        }

        RefreshPrimaryMenu();
    }

    private void ShowPrimaryMenu()
    {
        selectedItemIndex = -1;
        primaryMenu?.SetActive(true);
        itemMenu?.SetActive(false);
        RefreshPrimaryMenu();
    }

    private void ShowItemMenu()
    {
        if (request?.Inventory == null || request.IsAutoPassing)
        {
            return;
        }

        primaryMenu?.SetActive(false);
        itemMenu?.SetActive(true);
        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }
        RefreshItemMenu();
    }

    private void RefreshPrimaryMenu()
    {
        if (request == null || request.Enemy == null)
        {
            return;
        }

        if (request.IsAutoPassing)
        {
            if (previewText != null)
            {
                previewText.text =
                    "数字为 0，挣扎已经用尽\n自动跳过回合……";
            }
            primaryMenu?.SetActive(false);
            itemMenu?.SetActive(false);
            return;
        }

        bool canStruggle = request.CanStruggle?.Invoke() == true;
        int damage = canStruggle
            ? request.StruggleDamage
            : Mathf.Max(0, request.GetAttackDamage?.Invoke() ?? 0);
        int remainingHP = Mathf.Max(0, request.Enemy.CurrentHP - damage);
        if (previewText != null)
        {
            previewText.text = canStruggle
                ? $"濒死挣扎  消耗 0  伤害 {damage}\n" +
                  $"敌人生命：{request.Enemy.CurrentHP} > {remainingHP}"
                : $"普通攻击  消耗 {request.AttackCost}  伤害 {damage}\n" +
                  $"敌人生命：{request.Enemy.CurrentHP} > {remainingHP}";
        }

        if (attackButtonLabel != null)
        {
            attackButtonLabel.text = canStruggle ? "挣扎" : "普攻";
        }
        if (itemButtonLabel != null)
        {
            itemButtonLabel.text = "道具";
        }

        if (attackButton != null)
        {
            attackButton.interactable = canStruggle ||
                (NumberResource.Instance != null &&
                 NumberResource.Instance.CanSpend(request.AttackCost));
        }
        if (itemButton != null)
        {
            itemButton.interactable =
                request.Inventory != null &&
                request.Inventory.GetOrderedItemStacks().Count > 0;
        }
    }

    private void RefreshItemMenu()
    {
        displayedItems.Clear();
        if (request?.Inventory != null)
        {
            displayedItems.AddRange(
                request.Inventory.GetOrderedItemStacks()
            );
        }

        for (int i = 0; i < itemSlotButtons.Length; i++)
        {
            bool occupied = i < displayedItems.Count;
            CollectibleStack stack = occupied ? displayedItems[i] : null;
            CollectibleDefinition definition = stack?.Definition;

            if (itemSlotIcons.Length > i && itemSlotIcons[i] != null)
            {
                itemSlotIcons[i].sprite = definition?.Icon;
                itemSlotIcons[i].enabled = definition?.Icon != null;
            }
            if (itemSlotLabels.Length > i && itemSlotLabels[i] != null)
            {
                itemSlotLabels[i].text =
                    definition != null ? definition.DisplayName : "空";
            }
            if (itemSlotCounts.Length > i && itemSlotCounts[i] != null)
            {
                itemSlotCounts[i].text =
                    stack != null && stack.Count > 1
                        ? $"x{stack.Count}"
                        : string.Empty;
            }
            if (itemSlotButtons[i] != null)
            {
                BattleItemUseResult validation = definition != null
                    ? request.ValidateItem(definition)
                    : new BattleItemUseResult(
                        BattleItemUseStatus.InvalidItem,
                        string.Empty
                    );
                itemSlotButtons[i].interactable =
                    definition != null && validation.Succeeded;
            }
        }

        if (displayedItems.Count > 0)
        {
            SelectItem(Mathf.Clamp(selectedItemIndex, 0, displayedItems.Count - 1));
        }
        else
        {
            selectedItemIndex = -1;
            if (itemNameText != null)
            {
                itemNameText.text = "没有道具";
            }
            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = "背包中的道具会显示在这里。";
            }
        }
    }

    private void SelectItem(int index)
    {
        if (index < 0 || index >= displayedItems.Count)
        {
            return;
        }

        selectedItemIndex = index;
        CollectibleDefinition definition = displayedItems[index].Definition;
        BattleItemUseResult validation = request.ValidateItem(definition);
        if (itemNameText != null)
        {
            itemNameText.text = definition.DisplayName;
        }
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = validation.Succeeded
                ? definition.Description
                : $"{definition.Description}\n\n{validation.Message}";
        }
    }

    private void HandleItemClicked(int index)
    {
        if (request == null || index < 0 || index >= displayedItems.Count)
        {
            return;
        }

        CollectibleDefinition definition = displayedItems[index].Definition;
        BattleItemUseResult result = request.UseItem(definition);
        if (feedbackText != null)
        {
            feedbackText.text = result.Message;
        }

        if (result.Succeeded)
        {
            ShowPrimaryMenu();
        }
        else
        {
            RefreshItemMenu();
        }
    }

    private void HandleInventoryChanged()
    {
        if (itemMenu != null && itemMenu.activeSelf)
        {
            RefreshItemMenu();
        }
        else
        {
            RefreshPrimaryMenu();
        }
    }

    private void UnbindInventory()
    {
        if (request?.Inventory != null)
        {
            request.Inventory.Changed -= HandleInventoryChanged;
        }
    }

    private void OnDestroy()
    {
        attackButton?.onClick.RemoveListener(HandlePrimaryAction);
        itemButton?.onClick.RemoveListener(ShowItemMenu);
        backButton?.onClick.RemoveListener(ShowPrimaryMenu);
        UnbindInventory();
    }
}
