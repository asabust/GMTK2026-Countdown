using System;
using System.Collections.Generic;
using Game.Runtime.Data;
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

public enum BattleSkillUseStatus
{
    Success,
    WrongPhase,
    NotLearned,
    InvalidSkill,
    OnCooldown,
    NumberInsufficient
}

public readonly struct BattleSkillUseResult
{
    public BattleSkillUseResult(BattleSkillUseStatus status, string message)
    {
        Status = status;
        Message = message;
    }

    public BattleSkillUseStatus Status { get; }
    public string Message { get; }
    public bool Succeeded => Status == BattleSkillUseStatus.Success;
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
        PlayerSkillController skills,
        Func<SkillDefinition, BattleSkillUseResult> useSkill,
        Func<SkillDefinition, BattleSkillUseResult> validateSkill,
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
        Skills = skills;
        UseSkill = useSkill;
        ValidateSkill = validateSkill;
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
    public PlayerSkillController Skills { get; }
    public Func<SkillDefinition, BattleSkillUseResult> UseSkill { get; }
    public Func<SkillDefinition, BattleSkillUseResult> ValidateSkill { get; }
    public bool IsAutoPassing { get; }
}

public class BattleActionPanel : UIPanel
{
    [Header("Shared")]
    [SerializeField] private TMP_Text previewText;

    [Header("Primary menu")]
    [SerializeField] private GameObject primaryMenu;
    [SerializeField] private Button attackButton;
    [SerializeField] private TMP_Text attackButtonLabel;
    [SerializeField] private Button itemButton;
    [SerializeField] private TMP_Text itemButtonLabel;
    [SerializeField] private GameObject skillDescriptionFrame;
    [SerializeField] private TMP_Text skillNameText;
    [SerializeField] private TMP_Text skillDescriptionText;
    private readonly Button[] skillButtons = new Button[3];
    private readonly Image[] skillButtonIcons = new Image[3];
    private readonly TMP_Text[] skillButtonLabels = new TMP_Text[3];
    private readonly TMP_Text[] skillButtonCounts = new TMP_Text[3];
    [SerializeField, HideInInspector] private Button struggleButton;

    [Header("Item menu")]
    [SerializeField] private GameObject itemMenu;
    [SerializeField] private Button[] itemSlotButtons = new Button[4];
    [SerializeField] private Image[] itemSlotIcons = new Image[4];
    [SerializeField] private TMP_Text[] itemSlotLabels = new TMP_Text[4];
    [SerializeField] private TMP_Text[] itemSlotCounts = new TMP_Text[4];
    [SerializeField] private GameObject itemDescriptionFrame;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemDescriptionText;
    [SerializeField] private Button backButton;

    private readonly List<CollectibleStack> displayedItems = new();
    private readonly List<LearnedSkillState> displayedSkills = new();
    private BattleActionRequest request;
    private int selectedItemIndex = -1;

    public override void OnInit()
    {
        for (int i = 0; i < skillButtons.Length; i++)
        {
            Transform skillTransform = primaryMenu != null
                ? primaryMenu.transform.Find($"Skill{i + 1}Button")
                : null;
            if (skillTransform == null && itemButton != null)
            {
                Button fallback = Instantiate(
                    itemButton,
                    itemButton.transform.parent
                );
                fallback.name = $"Skill{i + 1}Button";
                fallback.transform.SetSiblingIndex(
                    itemButton.transform.GetSiblingIndex()
                );
                skillTransform = fallback.transform;
            }

            skillButtons[i] = skillTransform != null
                ? skillTransform.GetComponent<Button>()
                : null;
            skillButtonIcons[i] = FindChildComponent<Image>(
                skillTransform,
                "Icon"
            );
            skillButtonLabels[i] = FindChildComponent<TMP_Text>(
                skillTransform,
                "Label"
            );
            skillButtonCounts[i] = FindChildComponent<TMP_Text>(
                skillTransform,
                "Count"
            );

            int capturedIndex = i;
            skillButtons[i]?.onClick.RemoveAllListeners();
            skillButtons[i]?.onClick.AddListener(
                () => HandlePrimarySkillClicked(capturedIndex)
            );
            BindSkillHover(
                skillButtonIcons[i],
                () => ShowSkillDescription(
                    capturedIndex,
                    skillButtons[capturedIndex]?.transform as RectTransform
                )
            );
            skillButtons[i]?.gameObject.SetActive(false);
        }

        attackButton?.onClick.AddListener(HandlePrimaryAction);
        Image attackButtonIcon = FindChildComponent<Image>(
            attackButton?.transform,
            "Icon"
        );
        BindSkillHover(
            attackButtonIcon,
            () => ShowPrimaryActionDescription(
                attackButton?.transform as RectTransform
            )
        );
        itemButton?.onClick.AddListener(ShowItemMenu);
        backButton?.onClick.AddListener(ShowPrimaryMenu);

        for (int i = 0; i < itemSlotButtons.Length; i++)
        {
            int capturedIndex = i;
            itemSlotButtons[i]?.onClick.AddListener(
                () => HandleItemClicked(capturedIndex)
            );
            Image icon = itemSlotIcons.Length > i
                ? itemSlotIcons[i]
                : null;
            if (icon == null)
            {
                continue;
            }

            icon.raycastTarget = true;
            BattleItemSlotSelection relay =
                icon.GetComponent<BattleItemSlotSelection>();
            if (relay == null)
            {
                relay = icon.gameObject.AddComponent<
                    BattleItemSlotSelection
                >();
            }
            relay.Bind(
                () => SelectItem(
                    capturedIndex,
                    itemSlotButtons[capturedIndex]?.transform
                        as RectTransform
                ),
                HideItemDescription
            );
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
        if (request?.Skills != null)
        {
            request.Skills.Changed += HandleSkillsChanged;
        }

        HideSkillDescription();
        UILocalization.SetButtonText(
            backButton,
            "battle.player.button.back"
        );
        ShowPrimaryMenu();
    }

    public override void OnClose()
    {
        UnbindInventory();
        request = null;
        displayedItems.Clear();
        displayedSkills.Clear();
        selectedItemIndex = -1;
        HideSkillDescription();
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
        if (!accepted)
        {
            AudioManager.Instance?.PlaySFX(AudioName.UiNotEnough);
            ToastPanel.Show(GameLocalization.Get(
                struggling
                    ? "battle.action.cannot_struggle"
                    : "battle.action.insufficient"
            ));
        }

        RefreshPrimaryMenu();
    }

    private void ShowPrimaryMenu()
    {
        selectedItemIndex = -1;
        HideSkillDescription();
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
        HideSkillDescription();
        HideItemDescription();
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
                previewText.text = GameLocalization.Get(
                    "battle.action.auto_pass"
                );
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
            previewText.text = GameLocalization.Get(
                canStruggle
                    ? "battle.action.struggle_preview"
                    : "battle.action.attack_preview",
                canStruggle ? 0 : request.AttackCost,
                damage,
                request.Enemy.CurrentHP,
                remainingHP
            );
        }

        if (attackButtonLabel != null)
        {
            attackButtonLabel.text = GameLocalization.Get(
                canStruggle
                    ? "battle.action.struggle"
                    : "skill.basic_attack.name"
            );
        }
        if (itemButtonLabel != null)
        {
            itemButtonLabel.text = GameLocalization.Get(
                "battle.player.items"
            );
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
        RefreshPrimarySkillButtons();
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
                    definition != null
                        ? definition.DisplayName
                        : GameLocalization.Get("common.empty");
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

        HideItemDescription();
    }

    private void RefreshPrimarySkillButtons()
    {
        displayedSkills.Clear();
        if (request?.Skills != null)
        {
            displayedSkills.AddRange(request.Skills.GetLearnedSkills());
        }

        for (int i = 0; i < skillButtons.Length; i++)
        {
            bool occupied = i < displayedSkills.Count;
            SkillDefinition definition = occupied
                ? displayedSkills[i]?.Definition
                : null;
            Button button = skillButtons[i];
            button?.gameObject.SetActive(occupied);
            if (!occupied)
            {
                continue;
            }

            if (skillButtonIcons[i] != null)
            {
                skillButtonIcons[i].sprite = definition?.Icon;
                skillButtonIcons[i].enabled = definition?.Icon != null;
            }
            if (skillButtonLabels[i] != null)
            {
                skillButtonLabels[i].text = definition?.DisplayName ??
                    GameLocalization.Get("common.skill");
            }
            if (skillButtonCounts[i] != null)
            {
                int cooldown = definition != null
                    ? request.Skills.GetCooldown(definition)
                    : 0;
                skillButtonCounts[i].text =
                    cooldown > 0 ? $"CD {cooldown}" : string.Empty;
            }
            if (button != null)
            {
                BattleSkillUseResult validation = definition != null
                    ? request.ValidateSkill(definition)
                    : new BattleSkillUseResult(
                        BattleSkillUseStatus.InvalidSkill,
                        string.Empty
                    );
                button.interactable =
                    definition != null && validation.Succeeded;
            }
        }
    }

    private void SelectItem(
        int index,
        RectTransform sourceButton
    )
    {
        if (index < 0 || index >= displayedItems.Count)
        {
            return;
        }

        selectedItemIndex = index;
        CollectibleDefinition definition = displayedItems[index].Definition;
        BattleItemUseResult validation = request.ValidateItem(definition);
        itemDescriptionFrame?.SetActive(true);
        PositionDescriptionFrame(
            itemDescriptionFrame,
            sourceButton
        );
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

    private void HideItemDescription()
    {
        selectedItemIndex = -1;
        itemDescriptionFrame?.SetActive(false);
        if (itemNameText != null)
        {
            itemNameText.text = displayedItems.Count == 0
                ? GameLocalization.Get("battle.items.none")
                : string.Empty;
        }
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = displayedItems.Count == 0
                ? GameLocalization.Get(
                    "battle.items.empty_description"
                )
                : string.Empty;
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
        ToastPanel.Show(result.Message);

        if (result.Succeeded)
        {
            if (request != null)
            {
                ShowPrimaryMenu();
            }
            return;
        }

        AudioManager.Instance?.PlaySFX(AudioName.UiNotEnough);
        RefreshItemMenu();
    }

    private void HandlePrimarySkillClicked(int index)
    {
        if (request == null || index < 0 || index >= displayedSkills.Count)
        {
            return;
        }

        SkillDefinition definition = displayedSkills[index].Definition;
        BattleSkillUseResult result = request.UseSkill(definition);
        ToastPanel.Show(result.Message);
        if (!result.Succeeded && request != null)
        {
            AudioManager.Instance?.PlaySFX(AudioName.UiNotEnough);
            RefreshPrimaryMenu();
        }
    }

    private void ShowPrimaryActionDescription(RectTransform sourceButton)
    {
        if (request?.Enemy == null)
        {
            return;
        }

        bool struggling = request.CanStruggle?.Invoke() == true;
        if (struggling)
        {
            int damage = request.StruggleDamage;
            int remainingHP = Mathf.Max(
                0,
                request.Enemy.CurrentHP - damage
            );
            ShowSkillDescription(
                GameLocalization.Get("battle.action.struggle"),
                GameLocalization.Get(
                    "battle.action.struggle_preview",
                    0,
                    damage,
                    request.Enemy.CurrentHP,
                    remainingHP
                ),
                sourceButton
            );
            return;
        }

        ShowSkillDescription(
            GameLocalization.Get("skill.basic_attack.name"),
            GameLocalization.Get("skill.basic_attack.description"),
            sourceButton
        );
    }

    private void ShowSkillDescription(
        int index,
        RectTransform sourceButton
    )
    {
        if (index < 0 || index >= displayedSkills.Count)
        {
            HideSkillDescription();
            return;
        }

        SkillDefinition definition =
            displayedSkills[index]?.Definition;
        if (definition == null)
        {
            HideSkillDescription();
            return;
        }

        ShowSkillDescription(
            definition.DisplayName,
            definition.Description,
            sourceButton
        );
    }

    private void ShowSkillDescription(
        string skillName,
        string skillDescription,
        RectTransform sourceButton
    )
    {
        if (skillNameText != null)
        {
            skillNameText.text = skillName ?? string.Empty;
        }
        if (skillDescriptionText != null)
        {
            skillDescriptionText.text =
                skillDescription ?? string.Empty;
        }
        skillDescriptionFrame?.SetActive(true);
        PositionSkillDescription(sourceButton);
    }

    private void HideSkillDescription()
    {
        skillDescriptionFrame?.SetActive(false);
    }

    private void BindSkillHover(
        Graphic hoverArea,
        Action show
    )
    {
        if (hoverArea == null)
        {
            return;
        }

        hoverArea.raycastTarget = true;
        BattleSkillHoverTarget target =
            hoverArea.GetComponent<BattleSkillHoverTarget>();
        if (target == null)
        {
            target = hoverArea.gameObject.AddComponent<
                BattleSkillHoverTarget
            >();
        }
        target.Bind(show, HideSkillDescription);
    }

    private void PositionSkillDescription(RectTransform sourceButton)
    {
        PositionDescriptionFrame(
            skillDescriptionFrame,
            sourceButton
        );
    }

    private void PositionDescriptionFrame(
        GameObject descriptionFrame,
        RectTransform sourceButton
    )
    {
        RectTransform frame =
            descriptionFrame?.transform as RectTransform;
        RectTransform parent = frame?.parent as RectTransform;
        if (frame == null || parent == null || sourceButton == null)
        {
            return;
        }

        Vector3[] corners = new Vector3[4];
        sourceButton.GetWorldCorners(corners);
        Vector3 topCenter = (corners[1] + corners[2]) * 0.5f;
        float gap = 8f;
        Vector3 offset = parent.TransformVector(
            Vector3.up * (frame.rect.height * 0.5f + gap)
        );
        frame.position = topCenter + offset;
        frame.SetAsLastSibling();
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

    private void HandleSkillsChanged()
    {
        RefreshPrimaryMenu();
    }

    private void UnbindInventory()
    {
        if (request?.Inventory != null)
        {
            request.Inventory.Changed -= HandleInventoryChanged;
        }
        if (request?.Skills != null)
        {
            request.Skills.Changed -= HandleSkillsChanged;
        }
    }

    private void OnDestroy()
    {
        attackButton?.onClick.RemoveListener(HandlePrimaryAction);
        itemButton?.onClick.RemoveListener(ShowItemMenu);
        backButton?.onClick.RemoveListener(ShowPrimaryMenu);
        UnbindInventory();
    }

    private static T FindChildComponent<T>(
        Transform parent,
        string childName
    ) where T : Component
    {
        if (parent == null)
        {
            return null;
        }

        foreach (T component in parent.GetComponentsInChildren<T>(true))
        {
            if (component.gameObject.name == childName)
            {
                return component;
            }
        }

        return null;
    }
}
