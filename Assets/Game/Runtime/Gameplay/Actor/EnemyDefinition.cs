using UnityEngine;

public enum EnemyBehaviorType
{
    SmallChicken,
    DrunkenRaider,
    HorrorBox,
    Hamster,
    Boss
}

public enum EnemyIntentType
{
    Wait,
    Attack,
    Special
}

[CreateAssetMenu(
    fileName = "EnemyDefinition",
    menuName = "Zero/Combat/Enemy Definition"
)]
public class EnemyDefinition : ScriptableObject
{
    [SerializeField] private string enemyId;
    [SerializeField] private string displayName;
    [SerializeField, Min(1)] private int minHP = 1;
    [SerializeField, Min(1)] private int maxHP = 1;
    [SerializeField] private bool canRollHP = true;
    [SerializeField, Min(1)] private int fixedHP = 1;
    [SerializeField, Min(0)] private int rewardNumber;
    [SerializeField, Min(0)] private int attackDamage;
    [SerializeField, Min(0)] private int specialDamage;
    [SerializeField] private EnemyBehaviorType behaviorType;
    [SerializeField] private EnemyIntentType[] intentSequence;

    public string EnemyId => enemyId;
    public string DisplayName => displayName;
    public int MinHP => minHP;
    public int MaxHP => maxHP;
    public bool CanRollHP => canRollHP;
    public int FixedHP => fixedHP;
    public int RewardNumber => rewardNumber;
    public int AttackDamage => attackDamage;
    public int SpecialDamage => specialDamage;
    public EnemyBehaviorType BehaviorType => behaviorType;
    public EnemyIntentType[] IntentSequence => intentSequence;

    public int StableHP => canRollHP
        ? Mathf.Min(maxHP, Mathf.FloorToInt((minHP + maxHP) * 0.5f) + 1)
        : fixedHP;

    private void OnValidate()
    {
        minHP = Mathf.Max(1, minHP);
        maxHP = Mathf.Max(minHP, maxHP);
        fixedHP = Mathf.Max(1, fixedHP);
        rewardNumber = Mathf.Max(0, rewardNumber);
        attackDamage = Mathf.Max(0, attackDamage);
        specialDamage = Mathf.Max(0, specialDamage);
    }
}
