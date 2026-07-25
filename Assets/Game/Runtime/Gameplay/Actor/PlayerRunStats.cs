using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class PlayerRunStats : MonoBehaviour
{
    public int OfferingAttackBonus { get; private set; }

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

    public void ResetForNewRun()
    {
        if (OfferingAttackBonus == 0)
        {
            return;
        }

        OfferingAttackBonus = 0;
        Changed?.Invoke();
    }
}
