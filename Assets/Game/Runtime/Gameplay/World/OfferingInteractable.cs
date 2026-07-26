using System;
using Game.Runtime.Data;
using UnityEngine;

public enum OfferingResolutionStatus
{
    Success,
    InvalidAmount,
    NumberInsufficient,
    InvalidConfiguration
}

public sealed class OfferingResolution
{
    public OfferingResolutionStatus Status { get; set; }
    public OfferingOutcomeType Outcome { get; set; }
    public int OfferedAmount { get; set; }
    public int ReturnedAmount { get; set; }
    public int AttackIncrease { get; set; }
    public int FinalNumber { get; set; }
    public CollectibleDefinition Item { get; set; }
    public InventoryAddResult ItemAddResult { get; set; }
}

public sealed class OfferingInteractable : WorldInteractable
{
    [SerializeField] private OfferingDefinition definition;

    private WorldInteractionContext context;
    private PlayerInventory inventory;
    private PlayerRunStats runStats;
    private bool resolved;

    protected override bool OpenInteraction(WorldInteractionContext interaction)
    {
        if (definition == null)
        {
            Debug.LogError("Offering has no configuration.", this);
            return false;
        }

        context = interaction;
        inventory = interaction.Player.GetComponent<PlayerInventory>();
        runStats = interaction.Player.GetComponent<PlayerRunStats>();
        resolved = false;
        AudioManager.Instance?.PlaySFX(AudioName.UiInteractOffering);

        if (inventory == null || runStats == null)
        {
            Debug.LogError(
                "Offering requires PlayerInventory and PlayerRunStats.",
                this
            );
            return false;
        }

        OfferingPanel panel = UIManager.Instance?.Open<OfferingPanel>(
            new OfferingRequest(
                definition.MaximumAmount,
                interaction.NumberResource,
                Resolve,
                Complete,
                Complete
            )
        );
        return panel != null;
    }

    protected override void CloseInteraction()
    {
        UIManager.Instance?.Close<OfferingPanel>();
        context = null;
        inventory = null;
        runStats = null;
        resolved = false;
    }

    private OfferingResolution Resolve(int amount)
    {
        if (resolved || context?.NumberResource == null)
        {
            return Invalid(OfferingResolutionStatus.InvalidAmount);
        }

        NumberResource number = context.NumberResource;
        int maximum = Mathf.Min(definition.MaximumAmount, number.CurrentValue);
        if (amount < 1 || amount > maximum)
        {
            return Invalid(OfferingResolutionStatus.InvalidAmount);
        }

        if (!HasValidConfiguration())
        {
            return Invalid(OfferingResolutionStatus.InvalidConfiguration);
        }

        if (!number.TrySpend(
                amount,
                NumberChangeReason.Offering,
                context.WorldPosition
            ))
        {
            return Invalid(OfferingResolutionStatus.NumberInsufficient);
        }

        if (!TryRollOutcome(out OfferingOutcomeType outcome))
        {
            number.Add(amount, NumberChangeReason.Offering, context.WorldPosition);
            return Invalid(OfferingResolutionStatus.InvalidConfiguration);
        }

        resolved = true;
        int returnedAmount = 0;
        int attackIncrease = 0;
        CollectibleDefinition item = null;
        InventoryAddResult itemAddResult = InventoryAddResult.Success;

        switch (outcome)
        {
            case OfferingOutcomeType.RandomItem:
                item = RollItem();
                itemAddResult = item != null
                    ? inventory.TryAdd(item)
                    : InventoryAddResult.InvalidDefinition;
                break;

            case OfferingOutcomeType.AttackIncrease:
                attackIncrease = definition.AttackIncrease;
                runStats.AddOfferingAttackBonus(attackIncrease);
                break;

            case OfferingOutcomeType.DoubleReturn:
                int beforeDoubleReturn = number.CurrentValue;
                number.Add(
                    amount * 2,
                    NumberChangeReason.Offering,
                    context.WorldPosition
                );
                returnedAmount = number.CurrentValue - beforeDoubleReturn;
                break;

            case OfferingOutcomeType.FullReturn:
                int beforeFullReturn = number.CurrentValue;
                number.Add(
                    amount,
                    NumberChangeReason.Offering,
                    context.WorldPosition
                );
                returnedAmount = number.CurrentValue - beforeFullReturn;
                break;

            case OfferingOutcomeType.LoseAll:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        return new OfferingResolution
        {
            Status = OfferingResolutionStatus.Success,
            Outcome = outcome,
            OfferedAmount = amount,
            ReturnedAmount = returnedAmount,
            AttackIncrease = attackIncrease,
            FinalNumber = number.CurrentValue,
            Item = item,
            ItemAddResult = itemAddResult
        };
    }

    private void Complete()
    {
        if (context == null)
        {
            return;
        }

        context.Complete();
    }

    private OfferingResolution Invalid(OfferingResolutionStatus status) =>
        new()
        {
            Status = status,
            FinalNumber = context?.NumberResource?.CurrentValue ?? 0
        };

    private bool TryRollOutcome(out OfferingOutcomeType outcome)
    {
        outcome = default;
        WeightedOfferingOutcome[] outcomes = definition.Outcomes;
        int totalWeight = 0;
        foreach (WeightedOfferingOutcome entry in outcomes ??
                 Array.Empty<WeightedOfferingOutcome>())
        {
            if (entry != null)
            {
                totalWeight += Mathf.Max(0, entry.Weight);
            }
        }

        if (totalWeight != 100)
        {
            Debug.LogError(
                $"Offering outcome weights must total 100, got {totalWeight}.",
                this
            );
            return false;
        }

        int roll = GameRandom.RangeInclusive(1, totalWeight);
        foreach (WeightedOfferingOutcome entry in outcomes)
        {
            if (entry == null || entry.Weight <= 0)
            {
                continue;
            }

            roll -= entry.Weight;
            if (roll <= 0)
            {
                outcome = entry.Outcome;
                return true;
            }
        }

        return false;
    }

    private CollectibleDefinition RollItem()
    {
        WeightedOfferingItem[] pool = definition.ItemPool;
        int totalWeight = 0;
        foreach (WeightedOfferingItem entry in pool ??
                 Array.Empty<WeightedOfferingItem>())
        {
            if (IsValidItemEntry(entry))
            {
                totalWeight += entry.Weight;
            }
        }

        if (totalWeight <= 0)
        {
            Debug.LogError("Offering item pool has no valid item.", this);
            return null;
        }

        int roll = GameRandom.RangeInclusive(1, totalWeight);
        foreach (WeightedOfferingItem entry in pool)
        {
            if (!IsValidItemEntry(entry))
            {
                continue;
            }

            roll -= entry.Weight;
            if (roll <= 0)
            {
                return entry.Collectible;
            }
        }

        return null;
    }

    private static bool IsValidItemEntry(WeightedOfferingItem entry) =>
        entry?.Collectible != null &&
        entry.Collectible.Kind == CollectibleKind.Item &&
        entry.Weight > 0;

    private bool HasValidConfiguration()
    {
        int totalWeight = 0;
        int randomItemWeight = 0;
        foreach (WeightedOfferingOutcome entry in definition.Outcomes ??
                 Array.Empty<WeightedOfferingOutcome>())
        {
            if (entry == null || entry.Weight <= 0)
            {
                continue;
            }

            totalWeight += entry.Weight;
            if (entry.Outcome == OfferingOutcomeType.RandomItem)
            {
                randomItemWeight += entry.Weight;
            }
        }

        if (totalWeight != 100)
        {
            Debug.LogError(
                $"Offering outcome weights must total 100, got {totalWeight}.",
                this
            );
            return false;
        }

        if (randomItemWeight <= 0)
        {
            return true;
        }

        foreach (WeightedOfferingItem entry in definition.ItemPool ??
                 Array.Empty<WeightedOfferingItem>())
        {
            if (IsValidItemEntry(entry))
            {
                return true;
            }
        }

        Debug.LogError(
            "Offering has a random-item outcome but no valid item pool.",
            this
        );
        return false;
    }
}
