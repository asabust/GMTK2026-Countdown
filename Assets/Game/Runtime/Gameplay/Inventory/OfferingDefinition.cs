using System;
using UnityEngine;

public enum OfferingOutcomeType
{
    RandomItem,
    LoseAll,
    AttackIncrease,
    DoubleReturn,
    FullReturn
}

[Serializable]
public sealed class WeightedOfferingOutcome
{
    [SerializeField] private OfferingOutcomeType outcome;
    [SerializeField, Min(0)] private int weight;

    public OfferingOutcomeType Outcome => outcome;
    public int Weight => weight;
}

[Serializable]
public sealed class WeightedOfferingItem
{
    [SerializeField] private CollectibleDefinition collectible;
    [SerializeField, Min(0)] private int weight = 1;

    public CollectibleDefinition Collectible => collectible;
    public int Weight => weight;
}

[CreateAssetMenu(
    fileName = "OfferingDefinition",
    menuName = "Zero/Offering Definition"
)]
public sealed class OfferingDefinition : ScriptableObject
{
    [SerializeField] private string offeringId = "default_offering";
    [SerializeField, Min(1)] private int maximumAmount = 100;
    [SerializeField, Min(0)] private int attackIncrease = 1;
    [SerializeField] private WeightedOfferingOutcome[] outcomes;
    [SerializeField] private WeightedOfferingItem[] itemPool;

    public string OfferingId => offeringId;
    public int MaximumAmount => maximumAmount;
    public int AttackIncrease => attackIncrease;
    public WeightedOfferingOutcome[] Outcomes => outcomes;
    public WeightedOfferingItem[] ItemPool => itemPool;

    private void OnValidate()
    {
        maximumAmount = Mathf.Max(1, maximumAmount);
        attackIncrease = Mathf.Max(0, attackIncrease);
    }
}
