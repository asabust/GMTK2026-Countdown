using System;
using System.Collections;
using Game.Runtime.Data;
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
        UILocalization.SetButtonText(
            safeButton,
            "battle.reward.button.safe"
        );
        UILocalization.SetButtonText(
            greedyButton,
            "battle.reward.button.greedy"
        );

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
                GameLocalization.Get(
                    "battle.reward.summary.turn",
                    request.ResolvedMaxHP,
                    request.BattleRound,
                    Mathf.RoundToInt(
                        EnemyDefinition.GetTurnRewardMultiplier(
                            request.BattleRound
                        ) * 100f
                    ),
                    request.BattleLoot
                ),
            EnemyRewardMode.HealthScaled =>
                GameLocalization.Get(
                    "battle.reward.summary.health",
                    request.ResolvedMaxHP,
                    request.BattleLoot
                ),
            _ => GameLocalization.Get(
                "battle.reward.summary.fixed",
                request.BattleLoot
            )
        };
        summaryText.text = string.IsNullOrWhiteSpace(request.ItemDropSummary)
            ? numberSummary
            : $"{numberSummary}\n{request.ItemDropSummary}";
        safeText.text = GameLocalization.Get(
            "battle.reward.safe",
            request.BattleLoot
        );
        greedyText.text = GameLocalization.Get(
            "battle.reward.greedy",
            successPercent,
            greedyGain,
            100 - successPercent
        );
        string itemSafety = GameLocalization.Get(
            "battle.reward.item_safety"
        );
        resultText.text = string.IsNullOrWhiteSpace(request.ItemDropSummary)
            ? itemSafety
            : $"{request.ItemDropSummary}\n{itemSafety}";
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
            safeText.text = GameLocalization.Get(
                "battle.reward.stop",
                result.GainedNumber
            );
            greedyText.text = GameLocalization.Get(
                "battle.reward.additional",
                nextSuccessPercent,
                independentGain,
                nextTotal,
                100 - nextSuccessPercent
            );
            resultText.text = GameLocalization.Get(
                "battle.reward.additional_success",
                result.GainedNumber
            );
            resolved = false;
            SetButtonsInteractable(true);
            return;
        }

        if (choice == BattleRewardChoice.Safe)
        {
            resultText.text = isAdditionalGreed
                ? GameLocalization.Get(
                    "battle.reward.stopped_result",
                    result.GainedNumber
                )
                : GameLocalization.Get(
                    "battle.reward.safe_result",
                    result.GainedNumber
                );
        }
        else if (result.Succeeded)
        {
            resultText.text = GameLocalization.Get(
                "battle.reward.result.success",
                result.GainedNumber
            );
        }
        else
        {
            resultText.text = result.GainedNumber > 0
                ? GameLocalization.Get(
                    "battle.reward.additional_failed",
                    result.GainedNumber
                )
                : GameLocalization.Get("battle.reward.result.failed");
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
