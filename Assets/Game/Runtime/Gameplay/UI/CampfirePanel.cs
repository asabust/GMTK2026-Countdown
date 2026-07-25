using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class CampfireRequest
{
    public CampfireRequest(
        int minimumRestore,
        int maximumRestore,
        NumberResource numberResource,
        Action rest,
        Action leave
    )
    {
        MinimumRestore = minimumRestore;
        MaximumRestore = maximumRestore;
        NumberResource = numberResource;
        Rest = rest;
        Leave = leave;
    }

    public int MinimumRestore { get; }
    public int MaximumRestore { get; }
    public NumberResource NumberResource { get; }
    public Action Rest { get; }
    public Action Leave { get; }
}

public sealed class TreasureRequest
{
    public TreasureRequest(
        SkillDefinition skill,
        PlayerSkillController skills,
        Func<bool> learn,
        Action leave
    )
    {
        Skill = skill;
        Skills = skills;
        Learn = learn;
        Leave = leave;
    }

    public TreasureRequest(
        CollectibleDefinition collectible,
        PlayerInventory inventory,
        Func<bool> claimCollectible,
        Action leave
    )
    {
        Collectible = collectible;
        Inventory = inventory;
        ClaimCollectible = claimCollectible;
        Leave = leave;
    }

    public SkillDefinition Skill { get; }
    public PlayerSkillController Skills { get; }
    public Func<bool> Learn { get; }
    public CollectibleDefinition Collectible { get; }
    public PlayerInventory Inventory { get; }
    public Func<bool> ClaimCollectible { get; }
    public bool IsCollectibleReward => Collectible != null;
    public Action Leave { get; }
}

public sealed class CampfirePanel : UIPanel
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button restButton;
    [SerializeField] private TMP_Text restButtonText;
    [SerializeField] private Button leaveButton;

    private CampfireRequest request;
    private TreasureRequest treasureRequest;
    private bool awaitingFullNumberConfirmation;
    private bool submitted;

    public override void OnInit()
    {
        restButton?.onClick.AddListener(HandleRest);
        leaveButton?.onClick.AddListener(HandleLeave);
    }

    public override void OnOpen(object data = null)
    {
        request = data as CampfireRequest;
        treasureRequest = data as TreasureRequest;
        awaitingFullNumberConfirmation = false;
        submitted = false;
        SetButtonsInteractable(true);

        if (request == null && treasureRequest == null)
        {
            Debug.LogError("CampfirePanel received invalid data.", this);
            SetButtonsInteractable(false);
            return;
        }

        if (treasureRequest != null)
        {
            OpenTreasure();
            return;
        }

        if (titleText != null)
        {
            titleText.text = "篝火";
        }

        if (descriptionText != null)
        {
            int current = request.NumberResource != null
                ? request.NumberResource.CurrentValue
                : 0;
            int maximum = request.NumberResource != null
                ? request.NumberResource.MaximumValue
                : 0;
            descriptionText.text =
                $"休息后恢复 {request.MinimumRestore}～" +
                $"{request.MaximumRestore}\n当前数字：{current}/{maximum}";
        }

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        if (restButtonText != null)
        {
            restButtonText.text = "休息";
        }
    }

    public override void OnClose()
    {
        request = null;
        treasureRequest = null;
        awaitingFullNumberConfirmation = false;
        submitted = false;
    }

    private void HandleRest()
    {
        if (submitted)
        {
            return;
        }

        if (treasureRequest != null)
        {
            string rewardName = treasureRequest.IsCollectibleReward
                ? treasureRequest.Collectible.DisplayName
                : treasureRequest.Skill != null
                    ? treasureRequest.Skill.DisplayName
                    : "未知奖励";
            submitted = true;
            SetButtonsInteractable(false);
            bool received = treasureRequest.IsCollectibleReward
                ? treasureRequest.ClaimCollectible?.Invoke() == true
                : treasureRequest.Learn?.Invoke() == true;
            if (feedbackText != null)
            {
                feedbackText.text = received
                    ? $"获得：{rewardName}"
                    : "未能获得奖励";
            }
            return;
        }
        if (request == null) return;

        NumberResource resource = request.NumberResource;
        bool isAtMaximum =
            resource != null &&
            resource.CurrentValue >= resource.MaximumValue;
        if (isAtMaximum && !awaitingFullNumberConfirmation)
        {
            awaitingFullNumberConfirmation = true;
            if (feedbackText != null)
            {
                feedbackText.text = "数字已满，仍要消耗这处篝火吗？";
            }

            if (restButtonText != null)
            {
                restButtonText.text = "仍要休息";
            }

            return;
        }

        submitted = true;
        SetButtonsInteractable(false);
        request.Rest?.Invoke();
    }

    private void HandleLeave()
    {
        if (request == null && treasureRequest == null || submitted)
        {
            return;
        }

        submitted = true;
        SetButtonsInteractable(false);
        if (treasureRequest != null) treasureRequest.Leave?.Invoke();
        else request.Leave?.Invoke();
    }

    private void OpenTreasure()
    {
        if (treasureRequest.IsCollectibleReward)
        {
            OpenCollectibleTreasure();
            return;
        }

        SkillDefinition skill = treasureRequest.Skill;
        bool owned =
            skill != null &&
            treasureRequest.Skills?.Owns(skill) == true;
        if (titleText != null)
        {
            titleText.text = skill != null
                ? $"宝藏：{skill.DisplayName}"
                : "宝藏";
        }
        if (descriptionText != null)
        {
            string damage = skill != null && skill.BaseDamage > 0
                ? $"  伤害 {skill.BaseDamage}"
                : string.Empty;
            descriptionText.text = skill == null
                ? "宝藏中没有配置技能。"
                : $"消耗 {skill.NumberCost}{damage}  CD {skill.CooldownTurns}\n" +
                  skill.Description;
        }
        if (feedbackText != null)
        {
            feedbackText.text = owned ? "已经掌握该技能" : string.Empty;
        }
        if (restButtonText != null)
        {
            restButtonText.text = owned ? "已掌握" : "学习技能";
        }
        if (restButton != null)
        {
            restButton.interactable = skill != null && !owned;
        }
        if (leaveButton != null)
        {
            leaveButton.interactable = true;
        }
    }

    private void OpenCollectibleTreasure()
    {
        CollectibleDefinition collectible = treasureRequest.Collectible;
        InventoryAddResult addResult =
            collectible != null && treasureRequest.Inventory != null
                ? treasureRequest.Inventory.CanAdd(collectible)
                : InventoryAddResult.InvalidDefinition;
        bool canClaim = addResult == InventoryAddResult.Success;
        string kind = collectible?.Kind == CollectibleKind.Relic
            ? "藏品"
            : "道具";

        if (titleText != null)
        {
            titleText.text = collectible != null
                ? $"宝藏：{collectible.DisplayName}"
                : "宝藏";
        }
        if (descriptionText != null)
        {
            descriptionText.text = collectible == null
                ? "宝藏中没有配置道具或藏品。"
                : $"【{kind}】{collectible.DisplayName}\n" +
                  collectible.Description;
        }
        if (feedbackText != null)
        {
            feedbackText.text = canClaim
                ? string.Empty
                : addResult == InventoryAddResult.MaximumStacksReached
                    ? $"该{kind}已达到持有上限"
                    : $"现在无法获得该{kind}";
        }
        if (restButtonText != null)
        {
            restButtonText.text = canClaim ? "领取" : "无法领取";
        }
        if (restButton != null)
        {
            restButton.interactable = canClaim;
        }
        if (leaveButton != null)
        {
            leaveButton.interactable = true;
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (restButton != null)
        {
            restButton.interactable = interactable;
        }

        if (leaveButton != null)
        {
            leaveButton.interactable = interactable;
        }
    }

    private void OnDestroy()
    {
        restButton?.onClick.RemoveListener(HandleRest);
        leaveButton?.onClick.RemoveListener(HandleLeave);
    }
}
