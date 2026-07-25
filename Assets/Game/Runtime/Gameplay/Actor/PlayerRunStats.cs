using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRunStats : MonoBehaviour
{
    private readonly List<TimedBonusEntry> timedAttackBonuses = new();

    public int OfferingAttackBonus { get; private set; }
    public int TimedAttackBonus { get; private set; }
    public bool NegateNextAttack { get; private set; }
    public int NextEnemyPhaseShield { get; private set; }

    public event Action Changed;

    public void AddOfferingAttackBonus(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        OfferingAttackBonus += amount;
        Changed?.Invoke();
    }

    public void AddTimedAttackBonus(int amount, int playerActions)
    {
        if (amount <= 0 || playerActions <= 0)
        {
            return;
        }

        timedAttackBonuses.Add(new TimedBonusEntry(amount, playerActions));
        RecalculateTimedAttackBonus();
    }

    public bool TryActivateNegateNextAttack()
    {
        if (NegateNextAttack)
        {
            return false;
        }

        NegateNextAttack = true;
        Changed?.Invoke();
        return true;
    }

    public bool TryActivateShield(int amount)
    {
        if (amount <= 0 || NextEnemyPhaseShield > 0)
        {
            return false;
        }

        NextEnemyPhaseShield = amount;
        Changed?.Invoke();
        return true;
    }

    public IncomingAttackResolution ResolveIncomingAttack(int damage)
    {
        int incoming = Mathf.Max(0, damage);
        int blockedByShield = Mathf.Min(NextEnemyPhaseShield, incoming);
        int remaining = incoming - blockedByShield;
        bool negated = false;

        if (remaining > 0 && NegateNextAttack)
        {
            remaining = 0;
            NegateNextAttack = false;
            negated = true;
        }

        if (blockedByShield > 0 || negated)
        {
            Changed?.Invoke();
        }

        return new IncomingAttackResolution(
            incoming,
            blockedByShield,
            negated,
            remaining
        );
    }

    public void CompleteEnemyPhase()
    {
        if (NextEnemyPhaseShield == 0)
        {
            return;
        }

        NextEnemyPhaseShield = 0;
        Changed?.Invoke();
    }

    public void CompletePlayerAction()
    {
        bool changed = false;
        for (int i = timedAttackBonuses.Count - 1; i >= 0; i--)
        {
            TimedBonusEntry bonus = timedAttackBonuses[i];
            bonus.RemainingPlayerActions--;
            if (bonus.RemainingPlayerActions <= 0)
            {
                timedAttackBonuses.RemoveAt(i);
            }
            changed = true;
        }

        if (changed)
        {
            RecalculateTimedAttackBonus();
        }
    }

    public IReadOnlyList<int> GetTimedAttackBonusDurations()
    {
        List<int> durations = new(timedAttackBonuses.Count);
        foreach (TimedBonusEntry bonus in timedAttackBonuses)
        {
            durations.Add(bonus.RemainingPlayerActions);
        }
        durations.Sort((left, right) => right.CompareTo(left));
        return durations;
    }

    public void ResetForNewRun()
    {
        if (OfferingAttackBonus == 0 &&
            timedAttackBonuses.Count == 0 &&
            !NegateNextAttack &&
            NextEnemyPhaseShield == 0)
        {
            return;
        }

        OfferingAttackBonus = 0;
        timedAttackBonuses.Clear();
        TimedAttackBonus = 0;
        NegateNextAttack = false;
        NextEnemyPhaseShield = 0;
        Changed?.Invoke();
    }

    private void RecalculateTimedAttackBonus()
    {
        TimedAttackBonus = 0;
        foreach (TimedBonusEntry bonus in timedAttackBonuses)
        {
            TimedAttackBonus += bonus.Amount;
        }
        Changed?.Invoke();
    }

    private sealed class TimedBonusEntry
    {
        public TimedBonusEntry(int amount, int remainingPlayerActions)
        {
            Amount = amount;
            RemainingPlayerActions = remainingPlayerActions;
        }

        public int Amount { get; }
        public int RemainingPlayerActions { get; set; }
    }
}

public readonly struct IncomingAttackResolution
{
    public IncomingAttackResolution(
        int incomingDamage,
        int blockedByShield,
        bool negatedByHeart,
        int finalDamage
    )
    {
        IncomingDamage = incomingDamage;
        BlockedByShield = blockedByShield;
        NegatedByHeart = negatedByHeart;
        FinalDamage = finalDamage;
    }

    public int IncomingDamage { get; }
    public int BlockedByShield { get; }
    public bool NegatedByHeart { get; }
    public int FinalDamage { get; }
}
