using System;
using UnityEngine;

public enum NumberChangeReason
{
    Move,
    Wait,
    Attack,
    Damage,
    Item,
    Shop,
    Offering,
    Campfire,
    Reward,
    Stolen,
    StolenReturn,
    Other,
    Skill
}

public readonly struct NumberChange
{
    public NumberChange(
        int previousValue,
        int currentValue,
        NumberChangeReason reason,
        Vector3 worldPosition
    )
    {
        PreviousValue = previousValue;
        CurrentValue = currentValue;
        Reason = reason;
        WorldPosition = worldPosition;
    }

    public int PreviousValue { get; }
    public int CurrentValue { get; }
    public int Delta => CurrentValue - PreviousValue;
    public NumberChangeReason Reason { get; }
    public Vector3 WorldPosition { get; }
}

public class NumberResource : MonoBehaviour
{
    [SerializeField, Min(1)] private int initialValue = 100;
    [SerializeField] private int minimumValue = -1;
    [SerializeField, Min(1)] private int maximumValue = 100;

    public static NumberResource Instance { get; private set; }

    public int CurrentValue { get; private set; }
    public int MinimumValue => minimumValue;
    public int MaximumValue => maximumValue;

    public event Action<NumberChange> Changed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogError("More than one NumberResource exists.", this);
            enabled = false;
            return;
        }

        Instance = this;
        ValidateConfiguration();
        CurrentValue = Mathf.Clamp(initialValue, 0, maximumValue);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public bool CanSpend(int amount, bool allowFatalSpend = false)
    {
        if (amount < 0)
        {
            return false;
        }

        int result = CurrentValue - amount;
        return result > minimumValue ||
               allowFatalSpend && result == minimumValue;
    }

    public bool TrySpend(
        int amount,
        NumberChangeReason reason,
        Vector3 worldPosition,
        bool allowFatalSpend = false
    )
    {
        if (!CanSpend(amount, allowFatalSpend))
        {
            return false;
        }

        if (amount == 0)
        {
            return true;
        }

        ApplyValue(CurrentValue - amount, reason, worldPosition);
        return true;
    }

    public void Add(
        int amount,
        NumberChangeReason reason,
        Vector3 worldPosition
    )
    {
        if (amount <= 0)
        {
            return;
        }

        ApplyValue(CurrentValue + amount, reason, worldPosition);
    }

    public void TakeDamage(int amount, Vector3 worldPosition)
    {
        if (amount <= 0)
        {
            return;
        }

        ApplyValue(
            CurrentValue - amount,
            NumberChangeReason.Damage,
            worldPosition
        );
    }

    public int TakeUpTo(
        int amount,
        NumberChangeReason reason,
        Vector3 worldPosition
    )
    {
        int taken = Mathf.Min(Mathf.Max(0, amount), CurrentValue);
        if (taken <= 0)
        {
            return 0;
        }

        ApplyValue(CurrentValue - taken, reason, worldPosition);
        return taken;
    }

    public void ResetForNewRun()
    {
        ValidateConfiguration();
        ApplyValue(
            Mathf.Clamp(initialValue, 0, maximumValue),
            NumberChangeReason.Other,
            transform.position
        );
    }

    private void ApplyValue(
        int requestedValue,
        NumberChangeReason reason,
        Vector3 worldPosition
    )
    {
        int nextValue = Mathf.Clamp(requestedValue, minimumValue, maximumValue);
        if (nextValue == CurrentValue)
        {
            return;
        }

        int previousValue = CurrentValue;
        CurrentValue = nextValue;
        Changed?.Invoke(new NumberChange(
            previousValue,
            CurrentValue,
            reason,
            worldPosition
        ));
    }

    private void OnValidate()
    {
        ValidateConfiguration();
    }

    private void ValidateConfiguration()
    {
        minimumValue = -1;
        maximumValue = Mathf.Max(1, maximumValue);
        initialValue = Mathf.Clamp(initialValue, 1, maximumValue);
    }
}
