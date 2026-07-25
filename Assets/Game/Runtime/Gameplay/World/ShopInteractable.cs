using UnityEngine;

public sealed class ShopOffer
{
    public ShopOffer(
        CollectibleDefinition collectible,
        int price,
        int stock
    )
    {
        Collectible = collectible;
        Price = price;
        RemainingStock = Mathf.Max(0, stock);
    }

    public CollectibleDefinition Collectible { get; }
    public int Price { get; }
    public int RemainingStock { get; private set; }
    public bool IsSoldOut => RemainingStock <= 0;

    public void ConsumeOne()
    {
        RemainingStock = Mathf.Max(0, RemainingStock - 1);
    }
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
    [SerializeField] private ShopInventoryDefinition inventoryDefinition;

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

        ShopProduct[] products = inventoryDefinition != null
            ? inventoryDefinition.Products
            : null;
        if (products == null || products.Length == 0)
        {
            Debug.LogError("Shop has no inventory configuration.", this);
            return false;
        }

        offers = new ShopOffer[products.Length];
        for (int i = 0; i < offers.Length; i++)
        {
            ShopProduct product = products[i];
            offers[i] = new ShopOffer(
                product?.Collectible,
                product?.Price ?? 0,
                product?.Stock ?? 0
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

        offer.ConsumeOne();
        return ShopPurchaseResult.Success;
    }

    private void Leave()
    {
        if (submitted || context == null) return;
        submitted = true;
        context.Complete();
    }
}
