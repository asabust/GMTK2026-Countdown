using System;
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
    [SerializeField] private Slider amountSlider;
    [SerializeField] private TMP_Text amountText;
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button decreaseTenButton;
    [SerializeField] private Button decreaseOneButton;
    [SerializeField] private Button increaseOneButton;
    [SerializeField] private Button increaseTenButton;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text confirmButtonText;
    [SerializeField] private Button leaveButton;

    private OfferingRequest request;
    private bool resolved;

    public override void OnInit()
    {
        amountSlider?.onValueChanged.AddListener(HandleAmountChanged);
        decreaseTenButton?.onClick.AddListener(() => AdjustAmount(-10));
        decreaseOneButton?.onClick.AddListener(() => AdjustAmount(-1));
        increaseOneButton?.onClick.AddListener(() => AdjustAmount(1));
        increaseTenButton?.onClick.AddListener(() => AdjustAmount(10));
        confirmButton?.onClick.AddListener(Confirm);
        leaveButton?.onClick.AddListener(Leave);
    }

    public override void OnOpen(object data = null)
    {
        request = data as OfferingRequest;
        resolved = false;
        if (feedbackText != null) feedbackText.text = string.Empty;
        if (confirmButtonText != null) confirmButtonText.text = "确认供奉";
        if (leaveButton != null) leaveButton.gameObject.SetActive(true);

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
            dialogueText.text = canOffer
                ? "选择你要供奉的数字。"
                : "你已没有可以供奉的数字，正处于濒死状态。";
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

    private void AdjustAmount(int delta)
    {
        if (resolved || amountSlider == null)
        {
            return;
        }

        amountSlider.value = Mathf.Clamp(
            GetAmount() + delta,
            1,
            Mathf.Max(1, GetMaximumAmount())
        );
    }

    private void RefreshAmount()
    {
        int current = request?.Number?.CurrentValue ?? 0;
        int maximum = GetMaximumAmount();
        int amount = maximum > 0
            ? Mathf.Clamp(GetAmount(), 1, maximum)
            : 0;

        if (amountText != null)
        {
            amountText.text = $"{amount} / {current}";
        }

        if (previewText != null)
        {
            previewText.text = maximum > 0
                ? $"供奉后：{current} > {current - amount}"
                : "当前没有可以供奉的数字";
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
            if (feedbackText != null)
            {
                feedbackText.text = result?.Status switch
                {
                    OfferingResolutionStatus.NumberInsufficient =>
                        "数字不足，无法完成供奉。",
                    OfferingResolutionStatus.InvalidConfiguration =>
                        "供奉配置无效，请检查结果概率。",
                    _ => "供奉数字必须至少为 1，且不能超过当前数字。"
                };
            }
            RefreshAmount();
            return;
        }

        resolved = true;
        SetInputInteractable(false);
        if (leaveButton != null) leaveButton.gameObject.SetActive(false);
        if (confirmButton != null) confirmButton.interactable = true;
        if (confirmButtonText != null) confirmButtonText.text = "继续";
        if (dialogueText != null) dialogueText.text = "供奉结果";
        if (feedbackText != null) feedbackText.text = FormatResult(result);
        if (amountText != null)
            amountText.text = $"当前数字：{result.FinalNumber}";
        if (previewText != null) previewText.text = string.Empty;
    }

    private static string FormatResult(OfferingResolution result)
    {
        return result.Outcome switch
        {
            OfferingOutcomeType.RandomItem =>
                FormatItemResult(result.Item, result.ItemAddResult),
            OfferingOutcomeType.LoseAll =>
                $"供奉没有得到回报，失去了 {result.OfferedAmount} 点数字。",
            OfferingOutcomeType.AttackIncrease =>
                $"本局基础攻击力提高 {result.AttackIncrease} 点。",
            OfferingOutcomeType.DoubleReturn =>
                $"获得双倍返还：{result.ReturnedAmount} 点数字。",
            OfferingOutcomeType.FullReturn =>
                $"祭坛返还了供奉的 {result.ReturnedAmount} 点数字。",
            _ => "供奉已经完成。"
        };
    }

    private static string FormatItemResult(
        CollectibleDefinition item,
        InventoryAddResult result
    )
    {
        string itemName = item != null ? item.DisplayName : "未知道具";
        return result switch
        {
            InventoryAddResult.Success => $"获得了道具：{itemName}。",
            InventoryAddResult.MaximumStacksReached =>
                $"{itemName} 已达到最大数量，本次奖励已放弃。",
            InventoryAddResult.ItemSlotsFull =>
                "道具栏已满，本次奖励已放弃。",
            _ => "没有可用的供奉道具奖励，请检查配置。"
        };
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
        if (decreaseTenButton != null) decreaseTenButton.interactable = value;
        if (decreaseOneButton != null) decreaseOneButton.interactable = value;
        if (increaseOneButton != null) increaseOneButton.interactable = value;
        if (increaseTenButton != null) increaseTenButton.interactable = value;
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
