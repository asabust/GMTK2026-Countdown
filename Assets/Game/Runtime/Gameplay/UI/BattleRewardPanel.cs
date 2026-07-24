using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum BattleRewardChoice
{
    Safe,
    Greedy
}

public readonly struct BattleRewardResult
{
    public BattleRewardResult(
        BattleRewardChoice choice,
        bool succeeded,
        int gainedNumber
    )
    {
        Choice = choice;
        Succeeded = succeeded;
        GainedNumber = gainedNumber;
    }

    public BattleRewardChoice Choice { get; }
    public bool Succeeded { get; }
    public int GainedNumber { get; }
}

public sealed class BattleRewardRequest
{
    public BattleRewardRequest(
        int baseReward,
        int accumulatedLoss,
        int battleLoot,
        float greedySuccessChance,
        float greedyMultiplier,
        Func<BattleRewardChoice, BattleRewardResult> resolve,
        Action completed
    )
    {
        BaseReward = baseReward;
        AccumulatedLoss = accumulatedLoss;
        BattleLoot = battleLoot;
        GreedySuccessChance = greedySuccessChance;
        GreedyMultiplier = greedyMultiplier;
        Resolve = resolve;
        Completed = completed;
    }

    public int BaseReward { get; }
    public int AccumulatedLoss { get; }
    public int BattleLoot { get; }
    public float GreedySuccessChance { get; }
    public float GreedyMultiplier { get; }
    public Func<BattleRewardChoice, BattleRewardResult> Resolve { get; }
    public Action Completed { get; }
}

public class BattleRewardPanel : UIPanel
{
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private TMP_Text safeText;
    [SerializeField] private TMP_Text greedyText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button safeButton;
    [SerializeField] private Button greedyButton;
    [SerializeField, Min(0f)] private float resultDisplayDuration = 0.9f;

    private BattleRewardRequest request;
    private Coroutine finishRoutine;
    private bool resolved;

    public override void OnInit()
    {
        safeButton?.onClick.AddListener(ChooseSafe);
        greedyButton?.onClick.AddListener(ChooseGreedy);
    }

    public override void OnOpen(object data = null)
    {
        request = data as BattleRewardRequest;
        resolved = false;
        SetButtonsInteractable(true);

        if (request == null)
        {
            Debug.LogError("BattleRewardPanel received invalid data.", this);
            return;
        }

        int greedyGain = Mathf.FloorToInt(
            request.BattleLoot * request.GreedyMultiplier
        );
        int successPercent = Mathf.RoundToInt(
            request.GreedySuccessChance * 100f
        );

        summaryText.text =
            $"基础掉落：{request.BaseReward}\n" +
            $"本场损失：-{request.AccumulatedLoss}\n" +
            $"本场数字：{request.BattleLoot}";
        safeText.text = $"获得 {request.BattleLoot}（100%）";
        greedyText.text =
            $"{successPercent}% 获得 {greedyGain}\n" +
            $"{100 - successPercent}% 获得 0";
        resultText.text = "道具不会因贪婪失败而丢失";
    }

    public override void OnClose()
    {
        if (finishRoutine != null)
        {
            StopCoroutine(finishRoutine);
            finishRoutine = null;
        }

        request = null;
        resolved = false;
    }

    private void ChooseSafe()
    {
        Resolve(BattleRewardChoice.Safe);
    }

    private void ChooseGreedy()
    {
        Resolve(BattleRewardChoice.Greedy);
    }

    private void Resolve(BattleRewardChoice choice)
    {
        if (resolved || request == null || request.Resolve == null)
        {
            return;
        }

        resolved = true;
        SetButtonsInteractable(false);
        BattleRewardResult result = request.Resolve(choice);

        if (choice == BattleRewardChoice.Safe)
        {
            resultText.text = $"安全领取：+{result.GainedNumber}";
        }
        else if (result.Succeeded)
        {
            resultText.text = $"贪婪成功：+{result.GainedNumber}";
        }
        else
        {
            resultText.text = "贪婪失败：+0";
        }

        finishRoutine = StartCoroutine(FinishAfterResult());
    }

    private IEnumerator FinishAfterResult()
    {
        if (resultDisplayDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(resultDisplayDuration);
        }

        Action completed = request?.Completed;
        finishRoutine = null;
        UIManager.Instance.Close<BattleRewardPanel>();
        completed?.Invoke();
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (safeButton != null)
        {
            safeButton.interactable = interactable;
        }

        if (greedyButton != null)
        {
            greedyButton.interactable = interactable;
        }
    }

    private void OnDestroy()
    {
        safeButton?.onClick.RemoveListener(ChooseSafe);
        greedyButton?.onClick.RemoveListener(ChooseGreedy);
    }
}
