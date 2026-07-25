using System;
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

public sealed class ShopPanel : UIPanel
{
    [SerializeField] private Image merchantPortrait;
    [SerializeField] private TMP_Text numberText;
    [SerializeField] private ShopProductView[] productViews;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button buyButton;
    [SerializeField] private TMP_Text buyButtonText;
    [SerializeField] private Button leaveButton;

    private ShopRequest request;
    private int selectedIndex = -1;
    private bool submitted;

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
        submitted = false;
        selectedIndex = -1;
        if (feedbackText != null) feedbackText.text = string.Empty;

        if (request == null)
        {
            Debug.LogError("ShopPanel received invalid data.", this);
            if (buyButton != null) buyButton.interactable = false;
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
        selectedIndex = -1;
        submitted = false;
    }

    private void Select(int index)
    {
        if (submitted || request?.Offers == null ||
            index < 0 || index >= request.Offers.Length)
            return;
        selectedIndex = index;
        if (feedbackText != null) feedbackText.text = string.Empty;
        RefreshSelection();
    }

    private void Buy()
    {
        if (submitted || request == null || selectedIndex < 0) return;
        ShopPurchaseResult result =
            request.Purchase?.Invoke(selectedIndex) ??
            ShopPurchaseResult.InvalidOffer;
        if (feedbackText != null)
        {
            feedbackText.text = result switch
            {
                ShopPurchaseResult.Success => "购买成功",
                ShopPurchaseResult.NumberInsufficient =>
                    "数字不足，购买后不能低于 0",
                ShopPurchaseResult.ItemSlotsFull => "道具栏已满，无法购买",
                ShopPurchaseResult.MaximumStacksReached =>
                    GetMaximumStacksMessage(),
                ShopPurchaseResult.SoldOut => "这件商品已经售罄",
                _ => "当前无法购买"
            };
        }
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
            ? "该道具已达到最大数量"
            : "该藏品已达到最大层数";
    }

    private void Leave()
    {
        if (submitted || request == null) return;
        submitted = true;
        SetButtons(false);
        request.Leave?.Invoke();
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
            view.stateText.text = offer.IsSoldOut ? "售罄" : string.Empty;
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
            string kind = offer.Collectible.Kind == CollectibleKind.Relic
                ? "藏品"
                : "道具";
            dialogueText.text =
                $"【{kind}】{offer.Collectible.DisplayName}\n" +
                $"{offer.Collectible.Description}\n数字：{current} > {after}";
        }

        bool affordable = request.Number?.CanSpend(offer.Price) == true;
        if (buyButton != null)
            buyButton.interactable = !submitted && !offer.IsSoldOut && affordable;
        if (buyButtonText != null)
            buyButtonText.text = offer.IsSoldOut ? "已售罄" : $"购买  {offer.Price}";
    }

    private void RefreshNumber()
    {
        if (numberText != null)
            numberText.text = request?.Number?.CurrentValue.ToString() ?? "—";
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
