using UnityEngine;

public enum CollectibleKind { Item, Relic }

public enum CollectibleEffectType
{
    None,
    RestoreNumber,
    GreedChanceBonus,
    GreedMultiplierOverride
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
    [SerializeField] private CollectibleEffectType effectType;
    [SerializeField] private float effectValue;

    public string CollectibleId => collectibleId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public CollectibleKind Kind => kind;
    public int MaximumStacks => maximumStacks;
    public CollectibleEffectType EffectType => effectType;
    public float EffectValue => effectValue;

    private void OnValidate() => maximumStacks = Mathf.Max(1, maximumStacks);
}
