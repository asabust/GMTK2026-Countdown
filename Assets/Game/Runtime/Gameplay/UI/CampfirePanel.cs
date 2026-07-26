using System;
using Game.Runtime.Data;
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
        UILocalization.SetButtonText(leaveButton, "common.leave");

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
            titleText.text = GameLocalization.Get("campfire.title");
        }

        if (descriptionText != null)
        {
            int current = request.NumberResource != null
                ? request.NumberResource.CurrentValue
                : 0;
            int maximum = request.NumberResource != null
                ? request.NumberResource.MaximumValue
                : 0;
            descriptionText.text = GameLocalization.Get(
                "campfire.description",
                request.MinimumRestore,
                request.MaximumRestore,
                current,
                maximum
            );
        }

        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        if (restButtonText != null)
        {
            restButtonText.text = GameLocalization.Get("common.rest");
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
                    : GameLocalization.Get("common.unknown_reward");
            submitted = true;
            SetButtonsInteractable(false);
            bool received = treasureRequest.IsCollectibleReward
                ? treasureRequest.ClaimCollectible?.Invoke() == true
                : treasureRequest.Learn?.Invoke() == true;
            if (feedbackText != null)
            {
                feedbackText.text = received
                    ? GameLocalization.Get("treasure.received", rewardName)
                    : GameLocalization.Get("treasure.receive_failed");
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
                feedbackText.text = GameLocalization.Get(
                    "campfire.full_confirmation"
                );
            }

            if (restButtonText != null)
            {
                restButtonText.text = GameLocalization.Get(
                    "campfire.rest_anyway"
                );
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
                ? GameLocalization.Get("treasure.title_named", skill.DisplayName)
                : GameLocalization.Get("treasure.title");
        }
        if (descriptionText != null)
        {
            string damage = skill != null && skill.BaseDamage > 0
                ? GameLocalization.Get("battle.damage_suffix", skill.BaseDamage)
                : string.Empty;
            descriptionText.text = skill == null
                ? GameLocalization.Get("treasure.skill_missing")
                : GameLocalization.Get(
                    "treasure.skill_description",
                    skill.NumberCost,
                    damage,
                    skill.CooldownTurns,
                    skill.Description
                );
        }
        if (feedbackText != null)
        {
            feedbackText.text = owned
                ? GameLocalization.Get("treasure.skill_owned")
                : string.Empty;
        }
        if (restButtonText != null)
        {
            restButtonText.text = owned
                ? GameLocalization.Get("treasure.skill_owned_short")
                : GameLocalization.Get("treasure.learn_skill");
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
        string kind = GameLocalization.Get(
            collectible?.Kind == CollectibleKind.Relic
                ? "common.relic"
                : "common.item"
        );

        if (titleText != null)
        {
            titleText.text = collectible != null
                ? GameLocalization.Get(
                    "treasure.title_named",
                    collectible.DisplayName
                )
                : GameLocalization.Get("treasure.title");
        }
        if (descriptionText != null)
        {
            descriptionText.text = collectible == null
                ? GameLocalization.Get("treasure.collectible_missing")
                : GameLocalization.Get(
                    "common.kind_description",
                    kind,
                    collectible.DisplayName,
                    collectible.Description
                );
        }
        if (feedbackText != null)
        {
            feedbackText.text = canClaim
                ? string.Empty
                : addResult == InventoryAddResult.MaximumStacksReached
                    ? GameLocalization.Get(
                        "treasure.maximum_reached",
                        kind
                    )
                    : GameLocalization.Get(
                        "treasure.cannot_claim_kind",
                        kind
                    );
        }
        if (restButtonText != null)
        {
            restButtonText.text = canClaim
                ? GameLocalization.Get("common.claim")
                : GameLocalization.Get("common.cannot_claim");
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
