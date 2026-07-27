using System;
using System.Collections.Generic;
using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class OfferingRequest
{
    public OfferingRequest(
        int configuredMaximum,
        NumberResource number,
        Func<int, OfferingResolution> resolve,
        Action continueAfterResult,
        Action leave
    )
    {
        ConfiguredMaximum = configuredMaximum;
        Number = number;
        Resolve = resolve;
        ContinueAfterResult = continueAfterResult;
        Leave = leave;
    }

    public int ConfiguredMaximum { get; }
    public NumberResource Number { get; }
    public Func<int, OfferingResolution> Resolve { get; }
    public Action ContinueAfterResult { get; }
    public Action Leave { get; }
}

public sealed class OfferingPanel : UIPanel
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private Button leaveButton;

    private OfferingRequest request;
    private bool resolved;

    public override void OnInit()
    {
        amountSlider?.onValueChanged.AddListener(HandleAmountChanged);
        confirmButton?.onClick.AddListener(Confirm);
        leaveButton?.onClick.AddListener(Leave);
    }

    public override void OnOpen(object data = null)
    {
        request = data as OfferingRequest;
        resolved = false;
        if (titleText != null)
            titleText.text = GameLocalization.Get("offering.title");
        if (confirmButtonText != null)
            confirmButtonText.text = GameLocalization.Get(
                "offering.button.confirm"
            );
        if (leaveButton != null) leaveButton.gameObject.SetActive(true);
        UILocalization.SetButtonText(
            leaveButton,
            "offering.button.leave"
        );

        if (request?.Number == null)
        {
            Debug.LogError("OfferingPanel received invalid data.", this);
            SetInputInteractable(false);
            return;
        }

        int maximum = GetMaximumAmount();
        if (amountSlider != null)
        {
            amountSlider.wholeNumbers = true;
            amountSlider.minValue = 1f;
            amountSlider.maxValue = Mathf.Max(1, maximum);
            amountSlider.SetValueWithoutNotify(1f);
        }

        bool canOffer = maximum >= 1;
        SetInputInteractable(canOffer);
        if (dialogueText != null)
        {
            dialogueText.text = GameLocalization.Get(
                canOffer
                    ? "offering.prompt"
                    : "offering.no_number"
            );
        }
        RefreshAmount();
    }

    public override void OnClose()
    {
        request = null;
        resolved = false;
    }

    private int GetMaximumAmount()
    {
        if (request?.Number == null)
        {
            return 0;
        }

        return Mathf.Max(
            0,
            Mathf.Min(request.ConfiguredMaximum, request.Number.CurrentValue)
        );
    }

    private int GetAmount() =>
        amountSlider != null ? Mathf.RoundToInt(amountSlider.value) : 1;

    private void HandleAmountChanged(float _) => RefreshAmount();

    private void RefreshAmount()
    {
        int maximum = GetMaximumAmount();
        int amount = maximum > 0
            ? Mathf.Clamp(GetAmount(), 1, maximum)
            : 0;

        if (amountText != null)
        {
            amountText.text = $"{amount} / {maximum}";
        }
    }

    private void Confirm()
    {
        if (request == null)
        {
            return;
        }

        if (resolved)
        {
            request.ContinueAfterResult?.Invoke();
            return;
        }

        OfferingResolution result = request.Resolve?.Invoke(GetAmount());
        if (result == null || result.Status != OfferingResolutionStatus.Success)
        {
            ToastPanel.Show(
                result?.Status switch
                {
                    OfferingResolutionStatus.NumberInsufficient =>
                        GameLocalization.Get("offering.error.insufficient"),
                    OfferingResolutionStatus.InvalidConfiguration =>
                        GameLocalization.Get("offering.error.configuration"),
                    _ => GameLocalization.Get("offering.error.amount")
                }
            );
            AudioManager.Instance?.PlaySFX(AudioName.UiOfferingFail);
            resolved = true;
            SetInputInteractable(false);
            request.ContinueAfterResult?.Invoke();
            return;
        }

        resolved = true;
        SetInputInteractable(false);
        ToastPanel.Show(FormatResult(result));
        AudioManager.Instance?.PlaySFX(
            result.Outcome == OfferingOutcomeType.FullReturn
                ? AudioName.UiOfferingRefund
                : result.Outcome == OfferingOutcomeType.LoseAll
                    ? AudioName.UiOfferingFail
                    : AudioName.UiOfferingSuccess
        );
        request.ContinueAfterResult?.Invoke();
    }

    private static string FormatResult(OfferingResolution result)
    {
        return result.Outcome switch
        {
            OfferingOutcomeType.RandomItem =>
                FormatItemResult(result.ItemRewards),
            OfferingOutcomeType.LoseAll =>
                GameLocalization.Get(
                    "offering.result.lose_all",
                    result.OfferedAmount
                ),
            OfferingOutcomeType.AttackIncrease =>
                GameLocalization.Get(
                    "offering.result.attack",
                    result.AttackIncrease
                ),
            OfferingOutcomeType.DoubleReturn =>
                GameLocalization.Get(
                    "offering.result.double",
                    result.ReturnedAmount
                ),
            OfferingOutcomeType.FullReturn =>
                GameLocalization.Get(
                    "offering.result.return",
                    result.ReturnedAmount
                ),
            _ => GameLocalization.Get("offering.result.complete")
        };
    }

    private static string FormatItemResult(
        IReadOnlyList<OfferingItemReward> rewards
    )
    {
        if (rewards == null || rewards.Count == 0)
        {
            return GameLocalization.Get("offering.item.invalid");
        }

        List<string> receivedNames = new();
        List<string> failureMessages = new();
        foreach (OfferingItemReward reward in rewards)
        {
            string itemName = reward?.Item != null
                ? reward.Item.DisplayName
                : GameLocalization.Get("common.unknown_item");
            switch (reward?.AddResult)
            {
                case InventoryAddResult.Success:
                    receivedNames.Add(itemName);
                    break;

                case InventoryAddResult.MaximumStacksReached:
                    failureMessages.Add(GameLocalization.Get(
                        "offering.item.maximum",
                        itemName
                    ));
                    break;

                case InventoryAddResult.ItemSlotsFull:
                    failureMessages.Add(GameLocalization.Get(
                        "offering.item.inventory_full"
                    ));
                    break;

                default:
                    failureMessages.Add(GameLocalization.Get(
                        "offering.item.invalid"
                    ));
                    break;
            }
        }

        List<string> messages = new();
        if (receivedNames.Count > 0)
        {
            messages.Add(GameLocalization.Get(
                "offering.result.item",
                receivedNames.Count,
                string.Join(", ", receivedNames)
            ));
        }
        messages.AddRange(failureMessages);
        return string.Join("\n", messages);
    }

    private void Leave()
    {
        if (request == null || resolved)
        {
            return;
        }

        SetInputInteractable(false);
        request.Leave?.Invoke();
    }

    private void SetInputInteractable(bool value)
    {
        if (amountSlider != null) amountSlider.interactable = value;
        if (confirmButton != null) confirmButton.interactable = value;
        if (leaveButton != null) leaveButton.interactable = true;
    }

    private void OnDestroy()
    {
        amountSlider?.onValueChanged.RemoveListener(HandleAmountChanged);
        confirmButton?.onClick.RemoveListener(Confirm);
        leaveButton?.onClick.RemoveListener(Leave);
    }
}
