using System;
using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public sealed class ShopProductView
{
    public Button selectButton;
    public Image icon;
    public TMP_Text nameText;
    public TMP_Text priceText;
    public TMP_Text stateText;
}

public sealed class ShopRequest
{
    public ShopRequest(
        ShopOffer[] offers,
        NumberResource number,
        Func<int, ShopPurchaseResult> purchase,
        Action leave
    )
    {
        Offers = offers;
        Number = number;
        Purchase = purchase;
        Leave = leave;
    }

    public ShopOffer[] Offers { get; }
    public NumberResource Number { get; }
    public Func<int, ShopPurchaseResult> Purchase { get; }
    public Action Leave { get; }
}

public enum ExchangeResultStatus
{
    Success,
    InvalidSelection,
    NoAlternativeAvailable
}

public readonly struct ExchangeResult
{
    public ExchangeResult(
        ExchangeResultStatus status,
        CollectibleDefinition given,
        CollectibleDefinition received
    )
    {
        Status = status;
        Given = given;
        Received = received;
    }

    public ExchangeResultStatus Status { get; }
    public CollectibleDefinition Given { get; }
    public CollectibleDefinition Received { get; }
    public bool Succeeded => Status == ExchangeResultStatus.Success;
}

public sealed class ExchangeRequest
{
    public ExchangeRequest(
        PlayerInventory inventory,
        Func<CollectibleDefinition, ExchangeResult> exchange,
        Action complete
    )
    {
        Inventory = inventory;
        Exchange = exchange;
        Complete = complete;
    }

    public PlayerInventory Inventory { get; }
    public Func<CollectibleDefinition, ExchangeResult> Exchange { get; }
    public Action Complete { get; }
}

public sealed class ShopPanel : UIPanel
{
    [SerializeField] private Image merchantPortrait;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private ShopProductView[] productViews;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;
    [SerializeField] private Button leaveButton;

    private ShopRequest request;
    private ExchangeRequest exchangeRequest;
    private readonly System.Collections.Generic.List<CollectibleStack>
        exchangeItems = new();
    private int selectedIndex = -1;
    private bool submitted;
    private bool exchangeResolved;

    public override void OnInit()
    {
        for (int i = 0; i < productViews?.Length; i++)
        {
            int index = i;
            productViews[i]?.selectButton?.onClick.AddListener(
                () => Select(index)
            );
        }
        buyButton?.onClick.AddListener(Buy);
        leaveButton?.onClick.AddListener(Leave);
    }

    public override void OnOpen(object data = null)
    {
        request = data as ShopRequest;
        exchangeRequest = data as ExchangeRequest;
        submitted = false;
        exchangeResolved = false;
        selectedIndex = -1;
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(true);
            leaveButton.interactable = true;
        }
        UILocalization.SetButtonText(
            leaveButton,
            exchangeRequest != null
                ? "exchange.button.leave"
                : "shop.button.leave"
        );

        if (request == null && exchangeRequest == null)
        {
            Debug.LogError("ShopPanel received invalid data.", this);
            if (buyButton != null) buyButton.interactable = false;
            return;
        }

        if (exchangeRequest != null)
        {
            OpenExchange();
            return;
        }

        RefreshOffers();
        for (int i = 0; i < request.Offers.Length; i++)
        {
            if (request.Offers[i]?.Collectible != null)
            {
                Select(i);
                break;
            }
        }
    }

    public override void OnClose()
    {
        request = null;
        exchangeRequest = null;
        exchangeItems.Clear();
        selectedIndex = -1;
        submitted = false;
        exchangeResolved = false;
    }

    private void Select(int index)
    {
        if (submitted)
            return;
        if (exchangeRequest != null)
        {
            if (exchangeResolved || index < 0 || index >= exchangeItems.Count)
                return;
            selectedIndex = index;
            RefreshExchangeSelection();
            return;
        }
        if (request?.Offers == null ||
            index < 0 || index >= request.Offers.Length) return;
        selectedIndex = index;
        RefreshSelection();
    }

    private void Buy()
    {
        if (submitted) return;
        if (exchangeRequest != null)
        {
            Exchange();
            return;
        }
        if (request == null || selectedIndex < 0) return;
        ShopPurchaseResult result =
            request.Purchase?.Invoke(selectedIndex) ??
            ShopPurchaseResult.InvalidOffer;
        string feedback = result switch
        {
            ShopPurchaseResult.Success =>
                GameLocalization.Get("shop.feedback.purchase_success"),
            ShopPurchaseResult.NumberInsufficient =>
                GameLocalization.Get("shop.feedback.insufficient"),
            ShopPurchaseResult.ItemSlotsFull =>
                GameLocalization.Get("shop.purchase.inventory_full"),
            ShopPurchaseResult.MaximumStacksReached =>
                GetMaximumStacksMessage(),
            ShopPurchaseResult.SoldOut =>
                GameLocalization.Get("shop.feedback.sold_out"),
            _ => GameLocalization.Get("shop.purchase.unavailable")
        };
        ToastPanel.Show(feedback);
        AudioManager.Instance?.PlaySFX(
            result == ShopPurchaseResult.Success
                ? AudioName.UiBuy
                : result == ShopPurchaseResult.SoldOut
                    ? AudioName.UiSoldOut
                    : AudioName.UiNotEnough
        );
        RefreshOffers();
        RefreshSelection();
    }

    private string GetMaximumStacksMessage()
    {
        ShopOffer offer = request?.Offers != null &&
                          selectedIndex >= 0 &&
                          selectedIndex < request.Offers.Length
            ? request.Offers[selectedIndex]
            : null;
        return offer?.Collectible?.Kind == CollectibleKind.Item
            ? GameLocalization.Get("shop.purchase.item_maximum")
            : GameLocalization.Get("shop.purchase.relic_maximum");
    }

    private void Leave()
    {
        if (submitted || request == null && exchangeRequest == null) return;
        submitted = true;
        SetButtons(false);
        if (exchangeRequest != null) exchangeRequest.Complete?.Invoke();
        else request.Leave?.Invoke();
    }

    private void RefreshOffers()
    {
        RefreshNumber();
        for (int i = 0; i < productViews?.Length; i++)
        {
            ShopProductView view = productViews[i];
            ShopOffer offer = request?.Offers != null && i < request.Offers.Length
                ? request.Offers[i]
                : null;
            bool valid = view != null && offer?.Collectible != null;
            view?.selectButton?.gameObject.SetActive(valid);
            if (!valid) continue;

            view.icon.sprite = offer.Collectible.Icon;
            view.icon.enabled = offer.Collectible.Icon != null;
            view.nameText.text = offer.Collectible.DisplayName;
            view.priceText.text = offer.Price.ToString();
            view.stateText.text = offer.IsSoldOut
                ? GameLocalization.Get("shop.state.sold_out")
                : string.Empty;
            view.selectButton.interactable = !submitted && !offer.IsSoldOut;
        }
    }

    private void RefreshSelection()
    {
        RefreshNumber();
        ShopOffer offer = request?.Offers != null &&
                          selectedIndex >= 0 &&
                          selectedIndex < request.Offers.Length
            ? request.Offers[selectedIndex]
            : null;
        if (offer?.Collectible == null)
        {
            if (buyButton != null) buyButton.interactable = false;
            return;
        }

        int current = request.Number?.CurrentValue ?? 0;
        int after = current - offer.Price;
        if (dialogueText != null)
        {
            string kind = GameLocalization.Get(
                offer.Collectible.Kind == CollectibleKind.Relic
                    ? "common.relic"
                    : "common.item"
            );
            dialogueText.text = GameLocalization.Get(
                "shop.product_description",
                kind,
                offer.Collectible.DisplayName,
                offer.Collectible.Description,
                current,
                after
            );
        }

        bool affordable = request.Number?.CanSpend(offer.Price) == true;
        if (buyButton != null)
            buyButton.interactable = !submitted && !offer.IsSoldOut && affordable;
        if (buyButtonText != null)
            buyButtonText.text = offer.IsSoldOut
                ? GameLocalization.Get("shop.button.sold_out")
                : GameLocalization.Get("shop.buy_for", offer.Price);
    }

    private void RefreshNumber()
    {
        if (numberText != null)
        {
            int currentNumber =
                request?.Number?.CurrentValue ??
                NumberResource.Instance?.CurrentValue ??
                0;
            numberText.text = GameLocalization.Get(
                "common.current_number",
                currentNumber
            );
        }
    }

    private void OpenExchange()
    {
        exchangeItems.Clear();
        if (exchangeRequest.Inventory != null)
        {
            exchangeItems.AddRange(
                exchangeRequest.Inventory.GetOrderedItemStacks()
            );
        }

        if (merchantPortrait != null)
        {
            merchantPortrait.gameObject.SetActive(true);
        }
        if (numberText != null)
        {
            RefreshNumber();
        }
        if (leaveButton != null)
        {
            leaveButton.gameObject.SetActive(true);
            leaveButton.interactable = true;
        }

        RefreshExchangeItems();
        if (exchangeItems.Count == 0)
        {
            if (dialogueText != null)
                dialogueText.text = GameLocalization.Get(
                    "exchange.result.no_items"
                );
            if (buyButton != null) buyButton.interactable = false;
            if (buyButtonText != null)
                buyButtonText.text = GameLocalization.Get(
                    "exchange.button.confirm"
                );
            return;
        }

        Select(0);
    }

    private void RefreshExchangeItems()
    {
        for (int i = 0; i < productViews?.Length; i++)
        {
            ShopProductView view = productViews[i];
            CollectibleStack stack = i < exchangeItems.Count
                ? exchangeItems[i]
                : null;
            CollectibleDefinition item = stack?.Definition;
            bool valid = view != null && item != null;
            view?.selectButton?.gameObject.SetActive(valid);
            if (!valid) continue;

            view.icon.sprite = item.Icon;
            view.icon.enabled = item.Icon != null;
            view.nameText.text = item.DisplayName;
            view.priceText.text = $"x{stack.Count}";
            view.stateText.text = string.Empty;
            view.selectButton.interactable = !submitted && !exchangeResolved;
        }
    }

    private void RefreshExchangeSelection()
    {
        CollectibleDefinition item =
            selectedIndex >= 0 && selectedIndex < exchangeItems.Count
                ? exchangeItems[selectedIndex]?.Definition
                : null;
        if (item == null)
        {
            if (buyButton != null) buyButton.interactable = false;
            return;
        }

        if (dialogueText != null)
        {
            dialogueText.text = GameLocalization.Get(
                "exchange.item_description",
                GameLocalization.Get("common.item"),
                item.DisplayName,
                item.Description
            );
        }
        if (buyButton != null) buyButton.interactable = !exchangeResolved;
        if (buyButtonText != null)
            buyButtonText.text = GameLocalization.Get(
                "exchange.button.confirm"
            );
    }

    private void Exchange()
    {
        if (exchangeResolved)
        {
            Leave();
            return;
        }
        if (selectedIndex < 0 || selectedIndex >= exchangeItems.Count)
        {
            return;
        }

        ExchangeResult result = exchangeRequest.Exchange?.Invoke(
            exchangeItems[selectedIndex].Definition
        ) ?? new ExchangeResult(
            ExchangeResultStatus.InvalidSelection,
            null,
            null
        );
        if (!result.Succeeded)
        {
            ToastPanel.Show(
                result.Status == ExchangeResultStatus.NoAlternativeAvailable
                    ? GameLocalization.Get("exchange.no_alternative")
                    : GameLocalization.Get("exchange.failed")
            );
            return;
        }

        exchangeResolved = true;
        submitted = true;
        SetButtons(false);
        ToastPanel.Show(
            GameLocalization.Get(
                "exchange.result.success",
                result.Received.DisplayName
            )
        );
        exchangeRequest.Complete?.Invoke();
    }

    private void SetButtons(bool value)
    {
        if (buyButton != null) buyButton.interactable = value;
        if (leaveButton != null) leaveButton.interactable = value;
        foreach (ShopProductView view in productViews ?? Array.Empty<ShopProductView>())
            if (view?.selectButton != null) view.selectButton.interactable = value;
    }

    private void OnDestroy()
    {
        buyButton?.onClick.RemoveListener(Buy);
        leaveButton?.onClick.RemoveListener(Leave);
    }
}
