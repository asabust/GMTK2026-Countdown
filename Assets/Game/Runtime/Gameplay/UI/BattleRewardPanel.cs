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
        int gainedNumber,
        bool isFinal = true,
        float nextGreedSuccessChance = 0f
    )
    {
        Choice = choice;
        Succeeded = succeeded;
        GainedNumber = gainedNumber;
        IsFinal = isFinal;
        NextGreedSuccessChance = nextGreedSuccessChance;
    }

    public BattleRewardChoice Choice { get; }
    public bool Succeeded { get; }
    public int GainedNumber { get; }
    public bool IsFinal { get; }
    public float NextGreedSuccessChance { get; }
}

public sealed class BattleRewardRequest
{
    public BattleRewardRequest(
        int resolvedMaxHP,
        int battleRound,
        EnemyRewardMode rewardMode,
        int battleLoot,
        string itemDropSummary,
        float greedySuccessChance,
        float greedyMultiplier,
        Func<BattleRewardChoice, BattleRewardResult> resolve,
        Action completed
    )
    {
        ResolvedMaxHP = resolvedMaxHP;
        BattleRound = battleRound;
        RewardMode = rewardMode;
        BattleLoot = battleLoot;
        ItemDropSummary = itemDropSummary;
        GreedySuccessChance = greedySuccessChance;
        GreedyMultiplier = greedyMultiplier;
        Resolve = resolve;
        Completed = completed;
    }

    public int ResolvedMaxHP { get; }
    public int BattleRound { get; }
    public EnemyRewardMode RewardMode { get; }
    public int BattleLoot { get; }
    public string ItemDropSummary { get; }
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
    private bool isAdditionalGreed;

    public override void OnInit()
    {
        safeButton?.onClick.AddListener(ChooseSafe);
        greedyButton?.onClick.AddListener(ChooseGreedy);
    }

    public override void OnOpen(object data = null)
    {
        request = data as BattleRewardRequest;
        resolved = false;
        isAdditionalGreed = false;
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

        string numberSummary = request.RewardMode switch
        {
            EnemyRewardMode.TurnScaled =>
                $"锁定生命：{request.ResolvedMaxHP}\n" +
                $"击杀回合：{request.BattleRound}\n" +
                $"掉落倍率：{Mathf.RoundToInt(EnemyDefinition.GetTurnRewardMultiplier(request.BattleRound) * 100f)}%\n" +
                $"本场数字：{request.BattleLoot}",
            EnemyRewardMode.HealthScaled =>
                $"锁定生命：{request.ResolvedMaxHP}\n" +
                $"生命掉落：50%\n" +
                $"本场数字：{request.BattleLoot}",
            _ => $"本场数字：{request.BattleLoot}"
        };
        summaryText.text = string.IsNullOrWhiteSpace(request.ItemDropSummary)
            ? numberSummary
            : $"{numberSummary}\n{request.ItemDropSummary}";
        safeText.text = $"获得 {request.BattleLoot}（100%）";
        greedyText.text =
            $"{successPercent}% 获得 {greedyGain}\n" +
            $"{100 - successPercent}% 获得 0";
        resultText.text = string.IsNullOrWhiteSpace(request.ItemDropSummary)
            ? "道具不会因贪婪失败而丢失"
            : $"{request.ItemDropSummary}\n道具不会因贪婪失败而丢失";
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
        isAdditionalGreed = false;
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

        if (!result.IsFinal)
        {
            int nextSuccessPercent = Mathf.RoundToInt(
                result.NextGreedSuccessChance * 100f
            );
            int independentGain = Mathf.FloorToInt(
                request.BattleLoot * request.GreedyMultiplier
            );
            int nextTotal = result.GainedNumber + independentGain;
            isAdditionalGreed = true;
            safeText.text = $"收手，获得 {result.GainedNumber}";
            greedyText.text =
                $"追加贪婪\n{nextSuccessPercent}% 本次 +{independentGain}" +
                $"（累计 {nextTotal}）\n" +
                $"{100 - nextSuccessPercent}% 本次 +0";
            resultText.text =
                $"贪婪成功，当前累计：{result.GainedNumber}\n" +
                "可以收手，或继续追加贪婪";
            resolved = false;
            SetButtonsInteractable(true);
            return;
        }

        if (choice == BattleRewardChoice.Safe)
        {
            resultText.text = isAdditionalGreed
                ? $"收手领取：+{result.GainedNumber}"
                : $"安全领取：+{result.GainedNumber}";
        }
        else if (result.Succeeded)
        {
            resultText.text = $"贪婪成功：+{result.GainedNumber}";
        }
        else
        {
            resultText.text = result.GainedNumber > 0
                ? $"本次贪婪失败，追加结束\n已获得：+{result.GainedNumber}"
                : "贪婪失败：+0";
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
