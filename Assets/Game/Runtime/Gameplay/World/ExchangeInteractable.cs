using System.Collections.Generic;
using Game.Runtime.Core;
using UnityEngine;

public sealed class ExchangeInteractable : WorldInteractable
{
    [SerializeField] private CollectibleDefinition[] itemPool;

    private WorldInteractionContext context;
    private PlayerInventory inventory;
    private bool submitted;

    protected override bool OpenInteraction(WorldInteractionContext interaction)
    {
        context = interaction;
        inventory = interaction.Player.GetComponent<PlayerInventory>();
        submitted = false;
        if (inventory == null)
        {
            Debug.LogError("Exchange requires PlayerInventory.", this);
            return false;
        }

        return UIManager.Instance?.Open<ShopPanel>(
            new ExchangeRequest(inventory, TryExchange, Complete)
        ) != null;
    }

    protected override void CloseInteraction()
    {
        UIManager.Instance?.Close<ShopPanel>();
        context = null;
        inventory = null;
        submitted = false;
    }

    public ExchangeResult TryExchange(CollectibleDefinition selected)
    {
        if (submitted || inventory == null || selected == null ||
            selected.Kind != CollectibleKind.Item ||
            inventory.GetCount(selected) <= 0)
        {
            return new ExchangeResult(
                ExchangeResultStatus.InvalidSelection,
                null,
                null
            );
        }

        List<CollectibleDefinition> candidates = new();
        foreach (CollectibleDefinition candidate in
                 itemPool ?? System.Array.Empty<CollectibleDefinition>())
        {
            if (candidate == null ||
                candidate.Kind != CollectibleKind.Item ||
                candidate.CollectibleId == selected.CollectibleId ||
                inventory.CanAdd(candidate) != InventoryAddResult.Success)
            {
                continue;
            }
            candidates.Add(candidate);
        }
        if (candidates.Count == 0)
        {
            return new ExchangeResult(
                ExchangeResultStatus.NoAlternativeAvailable,
                selected,
                null
            );
        }

        CollectibleDefinition received = candidates[
            GameRandom.RangeInclusive(0, candidates.Count - 1)
        ];
        if (!inventory.TryConsume(selected))
        {
            return new ExchangeResult(
                ExchangeResultStatus.InvalidSelection,
                null,
                null
            );
        }
        if (inventory.TryAdd(received) != InventoryAddResult.Success)
        {
            inventory.TryAdd(selected);
            return new ExchangeResult(
                ExchangeResultStatus.InvalidSelection,
                selected,
                null
            );
        }

        return new ExchangeResult(
            ExchangeResultStatus.Success,
            selected,
            received
        );
    }

    private void Complete()
    {
        if (submitted || context == null) return;
        submitted = true;
        context.Complete();
    }
}
