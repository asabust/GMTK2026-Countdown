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

    public TreasureRewardType RewardType => rewardType;
    public SkillDefinition Skill => skill;
    public CollectibleDefinition CollectibleReward => collectibleReward;

    protected override bool OpenInteraction(WorldInteractionContext interaction)
    {
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

            bool received =
                inventory.TryAdd(collectibleReward) ==
                InventoryAddResult.Success;
            return FinishReward(
                interaction,
                received,
                collectibleReward?.DisplayName
            );
        }

        PlayerSkillController skills =
            interaction.Player.GetComponent<PlayerSkillController>();
        if (skills == null)
        {
            skills = interaction.Player.gameObject
                .AddComponent<PlayerSkillController>();
        }

        return FinishReward(
            interaction,
            skills.Learn(skill),
            skill?.DisplayName
        );
    }

    protected override void CloseInteraction() { }

    private bool FinishReward(
        WorldInteractionContext interaction,
        bool received,
        string rewardName
    )
    {
        if (!received)
        {
            ToastPanel.Show(GameLocalization.Get(
                "treasure.receive_failed"
            ));
            return false;
        }

        ToastPanel.Show(GameLocalization.Get(
            "treasure.received_kind",
            rewardName
        ));
        interaction.Complete();
        return true;
    }
}
