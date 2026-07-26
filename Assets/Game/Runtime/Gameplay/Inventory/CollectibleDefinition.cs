using System.Globalization;
using Game.Runtime.Data;
using UnityEngine;

public enum CollectibleKind { Item, Relic }

public enum CollectibleEffectType
{
    None,
    RestoreNumber,
    GreedChanceBonus,
    GreedMultiplierOverride,
    TimedAttackBonus,
    NegateNextAttack,
    NextEnemyPhaseShield,
    RepeatedGreed
}

[CreateAssetMenu(fileName = "CollectibleDefinition", menuName = "Zero/Collectible Definition")]
public sealed class CollectibleDefinition : ScriptableObject
{
    [SerializeField] private string collectibleId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private CollectibleKind kind;
    [SerializeField, Min(1)] private int maximumStacks = 1;
    [SerializeField, Min(0)] private int inventoryOrder;
    [SerializeField] private CollectibleEffectType effectType;
    [SerializeField] private float effectValue;
    [SerializeField, Min(0)] private int effectDuration;

    public string CollectibleId => collectibleId;
    public string DisplayName => GameLocalization.GetOrDefault(
        $"collectible.{collectibleId}.name",
        displayName
    );
    public string Description =>
        effectType == CollectibleEffectType.GreedMultiplierOverride
            ? GameLocalization.GetOrDefault(
                $"collectible.{collectibleId}.description",
                description,
                effectValue.ToString(
                    "0.#",
                    CultureInfo.InvariantCulture
                )
            )
            : GameLocalization.GetOrDefault(
                $"collectible.{collectibleId}.description",
                description
            );
    public Sprite Icon => icon;
    public CollectibleKind Kind => kind;
    public int MaximumStacks => maximumStacks;
    public int InventoryOrder => inventoryOrder;
    public CollectibleEffectType EffectType => effectType;
    public float EffectValue => effectValue;
    public int EffectDuration => effectDuration;

    private void OnValidate()
    {
        maximumStacks = Mathf.Max(1, maximumStacks);
        inventoryOrder = Mathf.Max(0, inventoryOrder);
        effectDuration = Mathf.Max(0, effectDuration);
    }
}
