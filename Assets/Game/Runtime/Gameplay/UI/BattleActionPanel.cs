using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BattleActionRequest
{
    public BattleActionRequest(
        EnemyActor enemy,
        int attackCost,
        int attackDamage,
        Func<bool> attack,
        int struggleDamage,
        bool canStruggle,
        Func<bool> struggle,
        bool isAutoPassing = false
    )
    {
        Enemy = enemy;
        AttackCost = attackCost;
        AttackDamage = attackDamage;
        Attack = attack;
        StruggleDamage = struggleDamage;
        CanStruggle = canStruggle;
        Struggle = struggle;
        IsAutoPassing = isAutoPassing;
    }

    public EnemyActor Enemy { get; }
    public int AttackCost { get; }
    public int AttackDamage { get; }
    public Func<bool> Attack { get; }
    public int StruggleDamage { get; }
    public bool CanStruggle { get; }
    public Func<bool> Struggle { get; }
    public bool IsAutoPassing { get; }
}

public class BattleActionPanel : UIPanel
{
    [SerializeField] private TMP_Text previewText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Button attackButton;
    [SerializeField] private Button struggleButton;

    private BattleActionRequest request;

    public override void OnInit()
    {
        EnsureStruggleButton();
        attackButton?.onClick.AddListener(HandleAttack);
        struggleButton?.onClick.AddListener(HandleStruggle);
    }

    public override void OnOpen(object data = null)
    {
        request = data as BattleActionRequest;
        if (feedbackText != null)
        {
            feedbackText.text = string.Empty;
        }

        Refresh();
    }

    public override void OnClose()
    {
        request = null;
    }

    private void HandleAttack()
    {
        if (request == null || request.Enemy == null)
        {
            return;
        }

        bool accepted = request.Attack?.Invoke() == true;
        if (!accepted && feedbackText != null)
        {
            feedbackText.text = "数字不足，无法攻击";
        }

        Refresh();
    }

    private void HandleStruggle()
    {
        if (request == null || request.Enemy == null)
        {
            return;
        }

        bool accepted = request.Struggle?.Invoke() == true;
        if (!accepted && feedbackText != null)
        {
            feedbackText.text = "现在无法挣扎";
        }

        Refresh();
    }

    private void Refresh()
    {
        if (request == null || request.Enemy == null)
        {
            return;
        }

        if (request.IsAutoPassing)
        {
            if (previewText != null)
            {
                previewText.text =
                    "数字为 0，挣扎已经用尽\n自动跳过回合……";
            }

            if (feedbackText != null)
            {
                feedbackText.text = string.Empty;
            }

            attackButton?.gameObject.SetActive(false);
            struggleButton?.gameObject.SetActive(false);
            return;
        }

        int displayedDamage = request.CanStruggle
            ? request.StruggleDamage
            : request.AttackDamage;
        int remainingHP = Mathf.Max(
            0,
            request.Enemy.CurrentHP - displayedDamage
        );
        if (previewText != null)
        {
            previewText.text = request.CanStruggle
                ? $"濒死挣扎  消耗 0  伤害 {request.StruggleDamage}\n" +
                  $"敌人生命：{request.Enemy.CurrentHP} → {remainingHP}"
                : $"普通攻击  消耗 {request.AttackCost}  伤害 {request.AttackDamage}\n" +
                  $"敌人生命：{request.Enemy.CurrentHP} → {remainingHP}";
        }

        if (attackButton != null)
        {
            attackButton.gameObject.SetActive(true);
            attackButton.interactable =
                NumberResource.Instance != null &&
                NumberResource.Instance.CanSpend(request.AttackCost);
        }

        if (struggleButton != null)
        {
            struggleButton.gameObject.SetActive(request.CanStruggle);
            struggleButton.interactable = request.CanStruggle;
        }
    }

    private void OnDestroy()
    {
        attackButton?.onClick.RemoveListener(HandleAttack);
        struggleButton?.onClick.RemoveListener(HandleStruggle);
    }

    private void EnsureStruggleButton()
    {
        if (struggleButton != null || attackButton == null)
        {
            return;
        }

        struggleButton = Instantiate(
            attackButton,
            attackButton.transform.parent
        );
        struggleButton.name = "StruggleButton";

        RectTransform rect = struggleButton.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition += new Vector2(0f, -100f);
        }

        TMP_Text label = struggleButton.GetComponentInChildren<TMP_Text>();
        if (label != null)
        {
            label.text = "挣扎";
        }
    }
}
