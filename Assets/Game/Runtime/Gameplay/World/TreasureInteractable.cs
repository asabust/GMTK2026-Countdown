using Game.Runtime.Core;
using UnityEngine;

public enum TreasureRewardType
{
    Skill,
    Collectible
}

public sealed class TreasureInteractable : WorldInteractable
{
    [SerializeField] private TreasureRewardType rewardType;
    [SerializeField] private SkillDefinition skill;
    [SerializeField] private CollectibleDefinition collectibleReward;

    private WorldInteractionContext context;
    private PlayerSkillController skills;
    private PlayerInventory inventory;
    private bool submitted;

    public TreasureRewardType RewardType => rewardType;
    public SkillDefinition Skill => skill;
    public CollectibleDefinition CollectibleReward => collectibleReward;

    protected override bool OpenInteraction(WorldInteractionContext interaction)
    {
        context = interaction;
        submitted = false;

        if (rewardType == TreasureRewardType.Collectible)
        {
            inventory = interaction.Player.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogError(
                    "Collectible treasure requires PlayerInventory.",
                    this
                );
                return false;
            }

            return UIManager.Instance?.Open<CampfirePanel>(
                new TreasureRequest(
                    collectibleReward,
                    inventory,
                    ClaimCollectible,
                    Leave
                )
            ) != null;
        }

        skills = interaction.Player.GetComponent<PlayerSkillController>();
        if (skills == null)
        {
            skills = interaction.Player.gameObject
                .AddComponent<PlayerSkillController>();
        }

        return UIManager.Instance?.Open<CampfirePanel>(
            new TreasureRequest(skill, skills, Learn, Leave)
        ) != null;
    }

    protected override void CloseInteraction()
    {
        UIManager.Instance?.Close<CampfirePanel>();
        context = null;
        skills = null;
        inventory = null;
        submitted = false;
    }

    private bool Learn()
    {
        if (submitted || context == null || skills == null)
        {
            return false;
        }

        submitted = true;
        bool learned = skills.Learn(skill);
        if (learned)
        {
            context.Complete();
        }
        return learned;
    }

    private bool ClaimCollectible()
    {
        if (submitted || context == null || inventory == null)
        {
            return false;
        }

        submitted = true;
        bool received =
            inventory.TryAdd(collectibleReward) ==
            InventoryAddResult.Success;
        if (received)
        {
            context.Complete();
        }
        return received;
    }

    private void Leave()
    {
        if (submitted || context == null) return;
        submitted = true;
        context.Complete();
    }
}
