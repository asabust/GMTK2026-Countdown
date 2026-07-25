using System;
using UnityEngine;

[Serializable]
public sealed class ShopProduct
{
    [SerializeField] private CollectibleDefinition collectible;
    [SerializeField, Min(0)] private int price;
    public CollectibleDefinition Collectible => collectible;
    public int Price => price;
}

public sealed class ShopOffer
{
    public ShopOffer(CollectibleDefinition collectible, int price)
    {
        Collectible = collectible;
        Price = price;
    }

    public CollectibleDefinition Collectible { get; }
    public int Price { get; }
    public bool IsSoldOut { get; internal set; }
}

public enum ShopPurchaseResult
{
    Success,
    InvalidOffer,
    SoldOut,
    NumberInsufficient,
    ItemSlotsFull,
    MaximumStacksReached
}

public sealed class ShopInteractable : WorldInteractable
{
    [SerializeField] private ShopProduct[] products;

    private WorldInteractionContext context;
    private PlayerInventory inventory;
    private ShopOffer[] offers;
    private bool submitted;

    protected override bool OpenInteraction(WorldInteractionContext interaction)
    {
        context = interaction;
        inventory = interaction.Player.GetComponent<PlayerInventory>();
        submitted = false;
        if (inventory == null)
        {
            Debug.LogError("Shop requires PlayerInventory on the player.", this);
            return false;
        }

        offers = new ShopOffer[products?.Length ?? 0];
        for (int i = 0; i < offers.Length; i++)
        {
            ShopProduct product = products[i];
            offers[i] = new ShopOffer(
                product?.Collectible,
                product?.Price ?? 0
            );
        }

        return UIManager.Instance?.Open<ShopPanel>(
            new ShopRequest(
                offers,
                interaction.NumberResource,
                TryPurchase,
                Leave
            )
        ) != null;
    }

    protected override void CloseInteraction()
    {
        UIManager.Instance?.Close<ShopPanel>();
        context = null;
        inventory = null;
        offers = null;
        submitted = false;
    }

    private ShopPurchaseResult TryPurchase(int index)
    {
        if (submitted || context == null || inventory == null ||
            offers == null || index < 0 || index >= offers.Length)
        {
            return ShopPurchaseResult.InvalidOffer;
        }

        ShopOffer offer = offers[index];
        if (offer?.Collectible == null) return ShopPurchaseResult.InvalidOffer;
        if (offer.IsSoldOut) return ShopPurchaseResult.SoldOut;

        InventoryAddResult addResult = inventory.CanAdd(offer.Collectible);
        if (addResult == InventoryAddResult.ItemSlotsFull)
            return ShopPurchaseResult.ItemSlotsFull;
        if (addResult == InventoryAddResult.MaximumStacksReached)
            return ShopPurchaseResult.MaximumStacksReached;

        NumberResource number = context.NumberResource;
        if (number == null ||
            !number.TrySpend(
                offer.Price,
                NumberChangeReason.Shop,
                context.WorldPosition
            ))
        {
            return ShopPurchaseResult.NumberInsufficient;
        }

        if (inventory.TryAdd(offer.Collectible) != InventoryAddResult.Success)
        {
            number.Add(offer.Price, NumberChangeReason.Shop, context.WorldPosition);
            return ShopPurchaseResult.InvalidOffer;
        }

        offer.IsSoldOut = true;
        return ShopPurchaseResult.Success;
    }

    private void Leave()
    {
        if (submitted || context == null) return;
        submitted = true;
        context.Complete();
    }
}
