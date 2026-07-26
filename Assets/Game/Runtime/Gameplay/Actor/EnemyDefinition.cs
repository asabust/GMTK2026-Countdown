using System.Collections.Generic;
using Game.Runtime.Data;
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
    Special,
    Steal,
    Charge,
    StealItem
}

public enum EnemyRewardMode
{
    FixedDeductBattleLoss,
    TurnScaled,
    HealthScaled
}

public enum DrunkenRaiderDrinkOutcome
{
    Strengthen,
    SelfDamage,
    Stunned
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
    [SerializeField] private EnemyRewardMode rewardMode;
    [SerializeField, Min(0)] private int attackDamage;
    [SerializeField, Min(0)] private int specialDamage;
    [SerializeField] private EnemyBehaviorType behaviorType;
    [SerializeField] private EnemyIntentType[] intentSequence;
    [Header("Drunken Raider")]
    [SerializeField, Min(0)] private int raiderNextAttackBonus = 1;
    [SerializeField, Min(0)] private int raiderSelfDamage = 2;
    [SerializeField, Min(0)] private int raiderStrengthenWeight = 1;
    [SerializeField, Min(0)] private int raiderSelfDamageWeight = 1;
    [SerializeField, Min(0)] private int raiderStunnedWeight = 1;
    [Header("Hamster")]
    [SerializeField, Min(0)] private int hamsterStealAmount = 3;
    [Header("Health Scaled Reward")]
    [SerializeField, Min(0f)] private float healthRewardMultiplier = 0.5f;
    [Header("Horror Box")]
    [SerializeField, Min(0f)] private float horrorExplosionMultiplier = 0.8f;
    [SerializeField] private CollectibleDefinition[] itemDropTable;
    [SerializeField, Min(0)] private int itemDropCount = 1;
    [Header("Boss")]
    [SerializeField, Range(0, 2)] private int bossPhase;
    [SerializeField] private EnemyDefinition bossNextPhase;
    [SerializeField, Min(0)] private int bossNoItemDamage = 5;

    public string EnemyId => enemyId;
    private string LocalizationId =>
        behaviorType == EnemyBehaviorType.Boss ? "Boss" : enemyId;

    public string DisplayName => GameLocalization.GetOrDefault(
        $"enemy.{LocalizationId}.name",
        displayName
    );
    public string Description => GameLocalization.GetOrDefault(
        $"enemy.{LocalizationId}.description",
        string.Empty
    );
    public int MinHP => minHP;
    public int MaxHP => maxHP;
    public bool CanRollHP => canRollHP;
    public int FixedHP => fixedHP;
    public int RewardNumber => rewardNumber;
    public EnemyRewardMode RewardMode => rewardMode;
    public int AttackDamage => attackDamage;
    public int SpecialDamage => specialDamage;
    public EnemyBehaviorType BehaviorType => behaviorType;
    public EnemyIntentType[] IntentSequence => intentSequence;
    public int RaiderNextAttackBonus => raiderNextAttackBonus;
    public int RaiderSelfDamage => raiderSelfDamage;
    public int HamsterStealAmount => hamsterStealAmount;
    public float HealthRewardMultiplier => healthRewardMultiplier;
    public float HorrorExplosionMultiplier => horrorExplosionMultiplier;
    public int ItemDropCount => itemDropCount;
    public int BossPhase => bossPhase;
    public EnemyDefinition BossNextPhase => bossNextPhase;
    public int BossNoItemDamage => bossNoItemDamage;

    public int StableHP => canRollHP
        ? Mathf.Min(maxHP, Mathf.FloorToInt((minHP + maxHP) * 0.5f) + 1)
        : fixedHP;

    public string RewardPreview =>
        behaviorType == EnemyBehaviorType.Boss
            ? GameLocalization.Get("enemy.reward.none")
            : rewardMode switch
            {
                EnemyRewardMode.TurnScaled =>
                    GameLocalization.Get("enemy.reward.turn_scaled"),
                EnemyRewardMode.HealthScaled =>
                    GameLocalization.Get(
                        "enemy.reward.health_scaled",
                        Mathf.RoundToInt(healthRewardMultiplier * 100f)
                    ) +
                    (itemDropCount > 0
                        ? GameLocalization.Get(
                            "enemy.reward.items",
                            itemDropCount
                        )
                        : string.Empty),
                _ => rewardNumber.ToString()
            };

    public int CalculateNumberReward(
        int resolvedMaxHP,
        int battleRound,
        int accumulatedBattleLoss
    )
    {
        if (rewardMode == EnemyRewardMode.HealthScaled)
        {
            return Mathf.CeilToInt(
                Mathf.Max(0, resolvedMaxHP) * healthRewardMultiplier
            );
        }

        if (rewardMode == EnemyRewardMode.FixedDeductBattleLoss)
        {
            return Mathf.Max(0, rewardNumber - accumulatedBattleLoss);
        }

        float multiplier = GetTurnRewardMultiplier(battleRound);
        return Mathf.CeilToInt(Mathf.Max(0, resolvedMaxHP) * multiplier);
    }

    public static float GetTurnRewardMultiplier(int battleRound)
    {
        return Mathf.Max(1, battleRound) switch
        {
            1 => 0.8f,
            2 => 0.7f,
            3 => 0.6f,
            4 => 0.5f,
            _ => 0.45f
        };
    }

    public int CalculateHorrorExplosionDamage(int resolvedMaxHP) =>
        Mathf.CeilToInt(
            Mathf.Max(0, resolvedMaxHP) * horrorExplosionMultiplier
        );

    public CollectibleDefinition RollItemDrop()
    {
        if (itemDropTable == null || itemDropTable.Length == 0)
        {
            return null;
        }

        List<CollectibleDefinition> validDrops = new();
        foreach (CollectibleDefinition collectible in itemDropTable)
        {
            if (collectible != null &&
                collectible.Kind == CollectibleKind.Item)
            {
                validDrops.Add(collectible);
            }
        }

        return validDrops.Count == 0
            ? null
            : validDrops[GameRandom.RangeInclusive(0, validDrops.Count - 1)];
    }

    public DrunkenRaiderDrinkOutcome RollDrunkenRaiderDrinkOutcome()
    {
        int totalWeight =
            raiderStrengthenWeight +
            raiderSelfDamageWeight +
            raiderStunnedWeight;
        if (totalWeight <= 0)
        {
            return DrunkenRaiderDrinkOutcome.Stunned;
        }

        int roll = GameRandom.RangeInclusive(1, totalWeight);
        if (roll <= raiderStrengthenWeight)
        {
            return DrunkenRaiderDrinkOutcome.Strengthen;
        }

        roll -= raiderStrengthenWeight;
        return roll <= raiderSelfDamageWeight
            ? DrunkenRaiderDrinkOutcome.SelfDamage
            : DrunkenRaiderDrinkOutcome.Stunned;
    }

    private void OnValidate()
    {
        minHP = Mathf.Max(1, minHP);
        maxHP = Mathf.Max(minHP, maxHP);
        fixedHP = Mathf.Max(1, fixedHP);
        rewardNumber = Mathf.Max(0, rewardNumber);
        attackDamage = Mathf.Max(0, attackDamage);
        specialDamage = Mathf.Max(0, specialDamage);
        raiderNextAttackBonus = Mathf.Max(0, raiderNextAttackBonus);
        raiderSelfDamage = Mathf.Max(0, raiderSelfDamage);
        raiderStrengthenWeight = Mathf.Max(0, raiderStrengthenWeight);
        raiderSelfDamageWeight = Mathf.Max(0, raiderSelfDamageWeight);
        raiderStunnedWeight = Mathf.Max(0, raiderStunnedWeight);
        hamsterStealAmount = Mathf.Max(0, hamsterStealAmount);
        healthRewardMultiplier = Mathf.Max(0f, healthRewardMultiplier);
        horrorExplosionMultiplier = Mathf.Max(0f, horrorExplosionMultiplier);
        itemDropCount = Mathf.Max(0, itemDropCount);
        bossPhase = Mathf.Clamp(bossPhase, 0, 2);
        bossNoItemDamage = Mathf.Max(0, bossNoItemDamage);
    }
}
