using System;
using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class PreBattleRollRequest
{
    public PreBattleRollRequest(EnemyActor enemy, Action<bool> completed)
    {
        Enemy = enemy;
        Completed = completed;
    }

    public EnemyActor Enemy { get; }
    public Action<bool> Completed { get; }
}

public class PreBattleRollPanel : UIPanel
{
    [SerializeField] private TMP_Text enemyNameText;
    [SerializeField] private TMP_Text healthRangeText;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private TMP_Text stableHealthText;
    [SerializeField] private Button rollButton;
    [SerializeField] private Button stableButton;

    private PreBattleRollRequest request;

    public override void OnInit()
    {
        rollButton?.onClick.AddListener(ChooseRoll);
        stableButton?.onClick.AddListener(ChooseStable);
    }

    public override void OnOpen(object data = null)
    {
        request = data as PreBattleRollRequest;
        EnemyDefinition definition = request?.Enemy != null
            ? request.Enemy.Definition
            : null;

        if (definition == null)
        {
            Debug.LogError("PreBattleRollPanel received invalid data.", this);
            return;
        }

        UILocalization.SetButtonText(
            rollButton,
            "battle.roll.button.roll"
        );
        UILocalization.SetButtonText(
            stableButton,
            "battle.roll.button.stable"
        );
        enemyNameText.text = GameLocalization.Get(
            "battle.roll.encounter",
            definition.DisplayName
        );
        healthRangeText.text = GameLocalization.Get(
            "battle.roll.health_range_with_description",
            definition.Description,
            definition.MinHP,
            definition.MaxHP
        );
        rewardText.text = GameLocalization.Get(
            "battle.roll.reward",
            definition.RewardPreview
        );
        stableHealthText.text = GameLocalization.Get(
            "battle.roll.stable_health",
            definition.StableHP
        );
        SetButtonsInteractable(true);
    }

    public override void OnClose()
    {
        request = null;
    }

    private void ChooseRoll()
    {
        Resolve(true);
    }

    private void ChooseStable()
    {
        Resolve(false);
    }

    private void Resolve(bool roll)
    {
        if (request == null)
        {
            return;
        }

        SetButtonsInteractable(false);
        Action<bool> completed = request.Completed;
        UIManager.Instance.Close<PreBattleRollPanel>();
        completed?.Invoke(roll);
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (rollButton != null)
        {
            rollButton.interactable = interactable;
        }

        if (stableButton != null)
        {
            stableButton.interactable = interactable;
        }
    }
}
