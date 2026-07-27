using Game.Runtime.Core;
using Game.Runtime.Data;
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
            PlayerInventory inventory =
                interaction.Player.GetComponent<PlayerInventory>();
            if (inventory == null)
            {
                Debug.LogError(
                    "Collectible treasure requires PlayerInventory.",
                    this
                );
                return false;
            }

            CampfirePanel collectiblePanel =
                UIManager.Instance?.Open<CampfirePanel>(
                    new TreasureRequest(
                        collectibleReward,
                        inventory,
                        ClaimCollectible,
                        Leave
                    )
                );
            return collectiblePanel != null;
        }

        PlayerSkillController skills =
            interaction.Player.GetComponent<PlayerSkillController>();
        if (skills == null)
        {
            skills = interaction.Player.gameObject
                .AddComponent<PlayerSkillController>();
        }

        CampfirePanel skillPanel = UIManager.Instance?.Open<CampfirePanel>(
            new TreasureRequest(
                skill,
                skills,
                LearnSkill,
                Leave
            )
        );
        return skillPanel != null;
    }

    protected override void CloseInteraction()
    {
        UIManager.Instance?.Close<CampfirePanel>();
        context = null;
        submitted = false;
    }

    private bool ClaimCollectible()
    {
        if (submitted || context == null)
        {
            return false;
        }

        PlayerInventory inventory =
            context.Player.GetComponent<PlayerInventory>();
        bool received =
            inventory != null &&
            inventory.TryAdd(collectibleReward) ==
            InventoryAddResult.Success;
        return FinishReward(received);
    }

    private bool LearnSkill()
    {
        if (submitted || context == null)
        {
            return false;
        }

        PlayerSkillController skills =
            context.Player.GetComponent<PlayerSkillController>();
        return FinishReward(skills != null && skills.Learn(skill));
    }

    private void Leave()
    {
        if (submitted || context == null)
        {
            return;
        }

        submitted = true;
        context.Complete(consume: false);
    }

    private bool FinishReward(bool received)
    {
        if (!received || submitted || context == null)
        {
            return false;
        }

        submitted = true;
        AudioManager.Instance?.PlaySFX(
            rewardType == TreasureRewardType.Skill
                ? AudioName.UiGetSkill
                : collectibleReward?.Kind == CollectibleKind.Relic
                    ? AudioName.UiGetCollectible
                    : AudioName.UiGetItem
        );
        context.Complete();
        return true;
    }
}
