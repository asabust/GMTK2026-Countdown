using System;
using System.Collections;
using Game.Runtime.Core;
using UnityEngine;

public enum EncounterPhase
{
    Exploration,
    PreBattleHealthChoice,
    PlayerTurn,
    ResolvingPlayerAction,
    EnemyTurn,
    ResolvingEnemyAction,
    Reward
}

[RequireComponent(typeof(PlayerGridController))]
public class EncounterController : MonoBehaviour
{
    private PlayerGridController playerController;
    private NumberResource numberResource;
    private PlayerInventory playerInventory;
    private PlayerRunStats playerRunStats;
    private PlayerBattleStatusWorldUI battleStatusWorldUI;

    [SerializeField, Min(1)] private int basicAttackCost = 1;
    [SerializeField, Min(1)] private int basicAttackDamage = 3;
    [SerializeField, Min(1)] private int struggleDamage = 1;
    [SerializeField, Min(0f)] private float enemyActionDuration = 0.45f;
    [SerializeField, Min(0f)] private float autoPassDelay = 0.7f;
    [Header("Reward")]
    [SerializeField, Range(0f, 1f)] private float greedySuccessChance = 0.5f;
    [SerializeField, Min(1f)] private float greedyMultiplier = 2.5f;

    private Coroutine enemyTurnRoutine;
    private bool isTrackingNumberLoss;
    private int accumulatedNumberLoss;
    private bool rewardChoiceResolved;
    private BattleRewardResult lockedRewardResult;
    private Vector3 rewardWorldPosition;
    private int currentBattleLoot;
    private bool hasUsedStruggle;
    private int battleRound;

    public EncounterPhase Phase { get; private set; } = EncounterPhase.Exploration;
    public EnemyActor CurrentEnemy { get; private set; }
    public bool IsInEncounter => CurrentEnemy != null;
    public int AccumulatedNumberLoss => accumulatedNumberLoss;
    public int CurrentBasicAttackDamage =>
        basicAttackDamage +
        (playerRunStats?.OfferingAttackBonus ?? 0) +
        (playerRunStats?.TimedAttackBonus ?? 0);

    public event Action<EncounterPhase> PhaseChanged;
    public event Action<EnemyActor> EncounterStarted;

    public void PrepareForSceneTransition()
    {
        if (enemyTurnRoutine != null)
        {
            StopCoroutine(enemyTurnRoutine);
            enemyTurnRoutine = null;
        }

        isTrackingNumberLoss = false;
        CurrentEnemy = null;
        accumulatedNumberLoss = 0;
        rewardChoiceResolved = false;
        lockedRewardResult = default;
        currentBattleLoot = 0;
        hasUsedStruggle = false;
        battleRound = 0;
        battleStatusWorldUI?.SetCombatVisible(false);

        UIManager.Instance?.Close<PreBattleRollPanel>();
        UIManager.Instance?.Close<BattleActionPanel>();
        UIManager.Instance?.Close<BattleRewardPanel>();

        if (playerController != null)
        {
            playerController.CompleteContact();
            playerController.SetExternalInputLocked(true);
        }

        SetPhase(EncounterPhase.Exploration);
    }

    private void Awake()
    {
        playerController = GetComponent<PlayerGridController>();
        numberResource = GetComponent<NumberResource>();
        playerInventory = GetComponent<PlayerInventory>();
        playerRunStats = GetComponent<PlayerRunStats>();
        EnsureBattleStatusWorldUI();
    }

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.ContactRequested += HandleContactRequested;
        }

        if (numberResource != null)
        {
            numberResource.Changed += HandleNumberChanged;
        }
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.ContactRequested -= HandleContactRequested;
        }

        if (numberResource != null)
        {
            numberResource.Changed -= HandleNumberChanged;
        }

        UIManager.Instance?.Close<PreBattleRollPanel>();
        UIManager.Instance?.Close<BattleActionPanel>();
        UIManager.Instance?.Close<BattleRewardPanel>();
        isTrackingNumberLoss = false;
        battleStatusWorldUI?.SetCombatVisible(false);
        if (enemyTurnRoutine != null)
        {
            StopCoroutine(enemyTurnRoutine);
            enemyTurnRoutine = null;
        }
    }

    private void HandleContactRequested(
        GridEntity target,
        GridOccupantType occupantType
    )
    {
        if (occupantType != GridOccupantType.Enemy || IsInEncounter)
        {
            return;
        }

        EnemyActor enemy = target.GetComponent<EnemyActor>();
        if (enemy == null || enemy.Definition == null)
        {
            Debug.LogError(
                $"Enemy grid object '{target.name}' has no valid EnemyActor.",
                target
            );
            playerController.CompleteContact();
            return;
        }

        CurrentEnemy = enemy;
        hasUsedStruggle = false;
        battleRound = 0;
        battleStatusWorldUI?.SetCombatVisible(true);
        CurrentEnemy.FacePlayer(playerController.GridPosition);
        EncounterStarted?.Invoke(enemy);

        if (!enemy.IsHealthResolved && enemy.Definition.CanRollHP)
        {
            SetPhase(EncounterPhase.PreBattleHealthChoice);
            PreBattleRollPanel panel = UIManager.Instance?.Open<PreBattleRollPanel>(
                new PreBattleRollRequest(enemy, ResolveHealthChoice)
            );
            if (panel == null)
            {
                ResolveHealthChoice(false);
            }

            return;
        }

        if (!enemy.IsHealthResolved)
        {
            enemy.ResolveHealth(false);
        }

        BeginPlayerTurn();
    }

    private void ResolveHealthChoice(bool roll)
    {
        if (CurrentEnemy == null ||
            Phase != EncounterPhase.PreBattleHealthChoice)
        {
            return;
        }

        CurrentEnemy.ResolveHealth(roll);
        BeginPlayerTurn();
    }

    private void BeginPlayerTurn()
    {
        if (CurrentEnemy == null)
        {
            return;
        }

        if (!isTrackingNumberLoss)
        {
            accumulatedNumberLoss = 0;
            isTrackingNumberLoss = true;
        }

        CurrentEnemy.ShowCombatInformation(battleRound + 1);
        if (!CurrentEnemy.HasLockedIntent)
        {
            CurrentEnemy.LockFirstIntent();
        }
        else
        {
            CurrentEnemy.ShowLockedIntent();
        }

        playerController.SetExternalInputLocked(false);
        SetPhase(EncounterPhase.PlayerTurn);

        bool canUseBasicAttack =
            numberResource != null &&
            numberResource.CanSpend(basicAttackCost);
        bool canStruggle =
            numberResource != null &&
            numberResource.CurrentValue == 0 &&
            !hasUsedStruggle;
        bool shouldAutoPass =
            !canUseBasicAttack &&
            !canStruggle &&
            !HasUsableBattleItem();

        UIManager.Instance?.Open<BattleActionPanel>(
            new BattleActionRequest(
                CurrentEnemy,
                basicAttackCost,
                () => CurrentBasicAttackDamage,
                TryBasicAttack,
                struggleDamage,
                CanStruggleNow,
                TryStruggle,
                playerInventory,
                TryUseBattleItem,
                ValidateBattleItem,
                shouldAutoPass
            )
        );

        if (shouldAutoPass)
        {
            playerController.SetExternalInputLocked(true);
            enemyTurnRoutine = StartCoroutine(AutoPassPlayerTurn());
        }
    }

    private bool TryBasicAttack()
    {
        if (Phase != EncounterPhase.PlayerTurn ||
            CurrentEnemy == null ||
            numberResource == null)
        {
            return false;
        }

        if (!numberResource.TrySpend(
                basicAttackCost,
                NumberChangeReason.Attack,
                transform.position
            ))
        {
            return false;
        }

        SetPhase(EncounterPhase.ResolvingPlayerAction);
        battleRound++;
        CurrentEnemy.ShowCombatInformation(battleRound);
        bool defeated = CurrentEnemy.ApplyDamage(CurrentBasicAttackDamage);
        playerRunStats?.CompletePlayerAction();
        if (defeated)
        {
            ResolveDefeatedEnemy();
        }
        else
        {
            UIManager.Instance?.Close<BattleActionPanel>();
            enemyTurnRoutine = StartCoroutine(ResolveEnemyTurn());
        }

        return true;
    }

    private bool TryStruggle()
    {
        if (Phase != EncounterPhase.PlayerTurn ||
            CurrentEnemy == null ||
            numberResource == null ||
            numberResource.CurrentValue != 0 ||
            hasUsedStruggle)
        {
            return false;
        }

        hasUsedStruggle = true;
        SetPhase(EncounterPhase.ResolvingPlayerAction);
        battleRound++;
        CurrentEnemy.ShowCombatInformation(battleRound);
        bool defeated = CurrentEnemy.ApplyDamage(struggleDamage);
        playerRunStats?.CompletePlayerAction();
        if (defeated)
        {
            ResolveDefeatedEnemy();
        }
        else
        {
            UIManager.Instance?.Close<BattleActionPanel>();
            enemyTurnRoutine = StartCoroutine(ResolveEnemyTurn());
        }

        return true;
    }

    private IEnumerator AutoPassPlayerTurn()
    {
        if (autoPassDelay > 0f)
        {
            yield return new WaitForSeconds(autoPassDelay);
        }

        enemyTurnRoutine = null;
        if (Phase != EncounterPhase.PlayerTurn ||
            CurrentEnemy == null ||
            numberResource == null ||
            numberResource.CurrentValue != 0)
        {
            yield break;
        }

        UIManager.Instance?.Close<BattleActionPanel>();
        enemyTurnRoutine = StartCoroutine(ResolveEnemyTurn());
    }

    private IEnumerator ResolveEnemyTurn()
    {
        if (CurrentEnemy == null)
        {
            yield break;
        }

        playerController.SetExternalInputLocked(true);
        SetPhase(EncounterPhase.EnemyTurn);

        EnemyActor actingEnemy = CurrentEnemy;
        SetPhase(EncounterPhase.ResolvingEnemyAction);
        actingEnemy.PlayCurrentIntentAnimation();
        EnemyIntentResolution intentResolution =
            actingEnemy.ResolveLockedIntent();

        int stolenNumber = numberResource.TakeUpTo(
            intentResolution.NumberToSteal,
            NumberChangeReason.Stolen,
            transform.position
        );
        actingEnemy.RecordStolenNumber(stolenNumber);

        IncomingAttackResolution attackResolution =
            playerRunStats != null
                ? playerRunStats.ResolveIncomingAttack(
                    intentResolution.DamageToPlayer
                )
                : new IncomingAttackResolution(
                    intentResolution.DamageToPlayer,
                    0,
                    false,
                    intentResolution.DamageToPlayer
                );
        if (attackResolution.FinalDamage > 0)
        {
            numberResource.TakeDamage(
                attackResolution.FinalDamage,
                transform.position
            );
        }

        bool enemyDefeatedBySelfDamage =
            intentResolution.SelfDamage > 0 &&
            actingEnemy.ApplyDamage(intentResolution.SelfDamage);
        string resolutionDescription =
            intentResolution.NumberToSteal > 0 &&
            stolenNumber != intentResolution.NumberToSteal
                ? $"{intentResolution.Description}（实际偷取 {stolenNumber}）"
                : intentResolution.Description;
        actingEnemy.ShowIntentResolution(resolutionDescription);

        float presentationDuration = Mathf.Max(
            enemyActionDuration,
            actingEnemy.GetCurrentIntentAnimationDuration()
        );
        if (presentationDuration > 0f)
        {
            yield return new WaitForSeconds(presentationDuration);
        }

        actingEnemy.PlayIdleAnimation();
        playerRunStats?.CompleteEnemyPhase();
        enemyTurnRoutine = null;

        if (numberResource.CurrentValue <= numberResource.MinimumValue)
        {
            HandlePlayerDefeated();
            yield break;
        }

        if (enemyDefeatedBySelfDamage)
        {
            ResolveEnemyDefeated();
            yield break;
        }

        if (intentResolution.Escapes)
        {
            ResolveEnemyEscaped(actingEnemy);
            yield break;
        }

        if (CurrentEnemy != actingEnemy)
        {
            yield break;
        }

        actingEnemy.LockNextIntent();
        BeginPlayerTurn();
    }

    private void HandlePlayerDefeated()
    {
        isTrackingNumberLoss = false;
        UIManager.Instance?.Close<BattleActionPanel>();
        battleStatusWorldUI?.SetCombatVisible(false);
        CurrentEnemy?.WorldUI?.HideIntent();
        playerController.SetExternalInputLocked(true);
        GameManager.Instance?.GameOver("数字跌破 0");
    }

    private void ResolveDefeatedEnemy()
    {
        if (CurrentEnemy?.Definition?.BehaviorType ==
            EnemyBehaviorType.HorrorBox)
        {
            UIManager.Instance?.Close<BattleActionPanel>();
            playerController.SetExternalInputLocked(true);
            enemyTurnRoutine = StartCoroutine(ResolveHorrorBoxExplosion());
            return;
        }

        ResolveEnemyDefeated();
    }

    private IEnumerator ResolveHorrorBoxExplosion()
    {
        EnemyActor actingEnemy = CurrentEnemy;
        if (actingEnemy == null)
        {
            enemyTurnRoutine = null;
            yield break;
        }

        int explosionDamage =
            actingEnemy.Definition.CalculateHorrorExplosionDamage(
                actingEnemy.ResolvedMaxHP
            );
        actingEnemy.PlaySpecialAnimation();
        actingEnemy.ShowIntentResolution($"负荷爆炸：-{explosionDamage}");

        IncomingAttackResolution attackResolution =
            playerRunStats != null
                ? playerRunStats.ResolveIncomingAttack(explosionDamage)
                : new IncomingAttackResolution(
                    explosionDamage,
                    0,
                    false,
                    explosionDamage
                );
        if (attackResolution.FinalDamage > 0)
        {
            numberResource.TakeDamage(
                attackResolution.FinalDamage,
                actingEnemy.transform.position
            );
        }

        float presentationDuration = Mathf.Max(
            enemyActionDuration,
            actingEnemy.GetSpecialAnimationDuration()
        );
        if (presentationDuration > 0f)
        {
            yield return new WaitForSeconds(presentationDuration);
        }

        playerRunStats?.CompleteEnemyPhase();
        enemyTurnRoutine = null;
        if (numberResource.CurrentValue <= numberResource.MinimumValue)
        {
            HandlePlayerDefeated();
            yield break;
        }

        ResolveEnemyDefeated();
    }

    private void ResolveEnemyDefeated()
    {
        EnemyActor defeatedEnemy = CurrentEnemy;
        int resolvedRound = Mathf.Max(1, battleRound);
        int battleLoot = defeatedEnemy.Definition.CalculateNumberReward(
            defeatedEnemy.ResolvedMaxHP,
            resolvedRound,
            accumulatedNumberLoss
        );
        if (defeatedEnemy.StolenNumberEscrow > 0)
        {
            numberResource.Add(
                defeatedEnemy.StolenNumberEscrow,
                NumberChangeReason.StolenReturn,
                defeatedEnemy.transform.position
            );
        }
        string itemDropSummary = ResolveItemDrops(defeatedEnemy.Definition);

        SetPhase(EncounterPhase.Reward);
        isTrackingNumberLoss = false;
        UIManager.Instance?.Close<BattleActionPanel>();
        playerController.SetExternalInputLocked(true);
        rewardWorldPosition = defeatedEnemy.transform.position;
        currentBattleLoot = battleLoot;
        CurrentEnemy = null;
        battleStatusWorldUI?.SetCombatVisible(false);
        defeatedEnemy.ReleaseAndDestroy();

        rewardChoiceResolved = false;
        lockedRewardResult = default;
        if (battleLoot <= 0)
        {
            CompleteReward();
            return;
        }

        BattleRewardPanel panel = UIManager.Instance?.Open<BattleRewardPanel>(
            new BattleRewardRequest(
                defeatedEnemy.ResolvedMaxHP,
                resolvedRound,
                defeatedEnemy.Definition.RewardMode,
                battleLoot,
                itemDropSummary,
                GetEffectiveGreedySuccessChance(),
                GetEffectiveGreedyMultiplier(),
                ResolveRewardChoice,
                CompleteReward
            )
        );
        if (panel == null)
        {
            lockedRewardResult = ResolveRewardChoice(BattleRewardChoice.Safe);
            CompleteReward();
        }
    }

    private string ResolveItemDrops(EnemyDefinition definition)
    {
        if (definition == null || definition.ItemDropCount <= 0)
        {
            return string.Empty;
        }

        System.Collections.Generic.List<string> results = new();
        for (int i = 0; i < definition.ItemDropCount; i++)
        {
            CollectibleDefinition drop = definition.RollItemDrop();
            if (drop == null)
            {
                continue;
            }

            InventoryAddResult result = playerInventory != null
                ? playerInventory.TryAdd(drop)
                : InventoryAddResult.InvalidDefinition;
            string resultText = result switch
            {
                InventoryAddResult.Success => $"获得 {drop.DisplayName}",
                InventoryAddResult.ItemSlotsFull =>
                    $"{drop.DisplayName}：道具栏已满，未获得",
                InventoryAddResult.MaximumStacksReached =>
                    $"{drop.DisplayName}：已达持有上限，未获得",
                _ => $"{drop.DisplayName}：未获得"
            };
            results.Add(resultText);
        }

        return results.Count > 0
            ? $"道具：{string.Join("；", results)}"
            : string.Empty;
    }

    private BattleRewardResult ResolveRewardChoice(BattleRewardChoice choice)
    {
        if (rewardChoiceResolved)
        {
            return lockedRewardResult;
        }

        rewardChoiceResolved = true;
        if (choice == BattleRewardChoice.Safe)
        {
            lockedRewardResult = new BattleRewardResult(
                choice,
                true,
                currentBattleLoot
            );
            return lockedRewardResult;
        }

        float effectiveChance = GetEffectiveGreedySuccessChance();
        float effectiveMultiplier = GetEffectiveGreedyMultiplier();
        bool succeeded = GameRandom.Chance(effectiveChance);
        int gain = succeeded
            ? Mathf.FloorToInt(currentBattleLoot * effectiveMultiplier)
            : 0;
        lockedRewardResult = new BattleRewardResult(choice, succeeded, gain);
        return lockedRewardResult;
    }

    private void CompleteReward()
    {
        if (lockedRewardResult.GainedNumber > 0)
        {
            numberResource.Add(
                lockedRewardResult.GainedNumber,
                NumberChangeReason.Reward,
                rewardWorldPosition
            );
        }

        playerController.SetExternalInputLocked(false);
        playerController.CompleteContact();
        accumulatedNumberLoss = 0;
        rewardChoiceResolved = false;
        lockedRewardResult = default;
        currentBattleLoot = 0;
        hasUsedStruggle = false;
        battleRound = 0;
        SetPhase(EncounterPhase.Exploration);
    }

    private void ResolveEnemyEscaped(EnemyActor escapedEnemy)
    {
        isTrackingNumberLoss = false;
        UIManager.Instance?.Close<BattleActionPanel>();
        playerController.SetExternalInputLocked(false);
        battleStatusWorldUI?.SetCombatVisible(false);
        escapedEnemy.WorldUI?.HideIntent();
        CurrentEnemy = null;
        escapedEnemy.ReleaseAndDestroy();
        playerController.CompleteContact();
        accumulatedNumberLoss = 0;
        rewardChoiceResolved = false;
        lockedRewardResult = default;
        currentBattleLoot = 0;
        hasUsedStruggle = false;
        battleRound = 0;
        SetPhase(EncounterPhase.Exploration);
    }

    private void EnsureBattleStatusWorldUI()
    {
        battleStatusWorldUI =
            GetComponentInChildren<PlayerBattleStatusWorldUI>(true);
        if (battleStatusWorldUI == null)
        {
            GameObject prefab = Resources.Load<GameObject>(
                "UI/PlayerBattleStatusWorldUI"
            );
            if (prefab != null)
            {
                battleStatusWorldUI = Instantiate(
                    prefab,
                    transform,
                    false
                ).GetComponent<PlayerBattleStatusWorldUI>();
            }
        }

        battleStatusWorldUI?.Bind(playerRunStats);
        battleStatusWorldUI?.SetCombatVisible(false);
    }

    private float GetEffectiveGreedySuccessChance()
    {
        float bonus = playerInventory != null
            ? playerInventory.GetRelicEffect(
                CollectibleEffectType.GreedChanceBonus
            )
            : 0f;
        return Mathf.Clamp(greedySuccessChance + bonus, 0f, 0.9f);
    }

    private float GetEffectiveGreedyMultiplier()
    {
        float overrideValue = playerInventory != null
            ? playerInventory.GetRelicEffect(
                CollectibleEffectType.GreedMultiplierOverride,
                highest: true
            )
            : 0f;
        return overrideValue > 0f ? overrideValue : greedyMultiplier;
    }

    private bool CanStruggleNow() =>
        Phase == EncounterPhase.PlayerTurn &&
        numberResource != null &&
        numberResource.CurrentValue == 0 &&
        !hasUsedStruggle;

    private bool HasUsableBattleItem()
    {
        if (playerInventory == null)
        {
            return false;
        }

        foreach (CollectibleStack stack in playerInventory.GetOrderedItemStacks())
        {
            if (ValidateBattleItem(stack.Definition).Succeeded)
            {
                return true;
            }
        }

        return false;
    }

    private BattleItemUseResult ValidateBattleItem(
        CollectibleDefinition definition
    )
    {
        if (Phase != EncounterPhase.PlayerTurn || CurrentEnemy == null)
        {
            return ItemResult(
                BattleItemUseStatus.WrongPhase,
                "只能在玩家战斗回合使用"
            );
        }
        if (definition == null ||
            definition.Kind != CollectibleKind.Item ||
            definition.EffectType == CollectibleEffectType.None)
        {
            return ItemResult(
                BattleItemUseStatus.InvalidItem,
                "这个道具还没有配置战斗效果"
            );
        }
        if (playerInventory == null ||
            playerInventory.GetCount(definition) <= 0)
        {
            return ItemResult(
                BattleItemUseStatus.NotOwned,
                "背包中没有这个道具"
            );
        }

        switch (definition.EffectType)
        {
            case CollectibleEffectType.RestoreNumber:
                if (numberResource == null ||
                    numberResource.CurrentValue >= numberResource.MaximumValue)
                {
                    return ItemResult(
                        BattleItemUseStatus.NumberAlreadyFull,
                        "数字已满，暂时无法使用"
                    );
                }
                break;

            case CollectibleEffectType.NegateNextAttack:
                if (playerRunStats == null || playerRunStats.NegateNextAttack)
                {
                    return ItemResult(
                        BattleItemUseStatus.AlreadyActive,
                        "少女的心事已经在保护你"
                    );
                }
                break;

            case CollectibleEffectType.NextEnemyPhaseShield:
                if (playerRunStats == null ||
                    playerRunStats.NextEnemyPhaseShield > 0)
                {
                    return ItemResult(
                        BattleItemUseStatus.AlreadyActive,
                        "本回合的护盾已经生效"
                    );
                }
                break;
        }

        return ItemResult(BattleItemUseStatus.Success, string.Empty);
    }

    private BattleItemUseResult TryUseBattleItem(
        CollectibleDefinition definition
    )
    {
        BattleItemUseResult validation = ValidateBattleItem(definition);
        if (!validation.Succeeded)
        {
            return validation;
        }
        if (!playerInventory.TryConsume(definition))
        {
            return ItemResult(
                BattleItemUseStatus.NotOwned,
                "道具数量发生变化，请重新选择"
            );
        }

        int value = Mathf.Max(0, Mathf.RoundToInt(definition.EffectValue));
        switch (definition.EffectType)
        {
            case CollectibleEffectType.RestoreNumber:
                numberResource.Add(
                    value,
                    NumberChangeReason.Item,
                    transform.position
                );
                break;

            case CollectibleEffectType.TimedAttackBonus:
                playerRunStats.AddTimedAttackBonus(
                    value,
                    Mathf.Max(1, definition.EffectDuration)
                );
                break;

            case CollectibleEffectType.NegateNextAttack:
                playerRunStats.TryActivateNegateNextAttack();
                break;

            case CollectibleEffectType.NextEnemyPhaseShield:
                playerRunStats.TryActivateShield(value);
                break;
        }

        ScheduleAutoPassIfNecessary();
        return ItemResult(
            BattleItemUseStatus.Success,
            $"{definition.DisplayName}已使用"
        );
    }

    private void ScheduleAutoPassIfNecessary()
    {
        if (Phase != EncounterPhase.PlayerTurn ||
            enemyTurnRoutine != null ||
            (numberResource != null &&
             numberResource.CanSpend(basicAttackCost)) ||
            CanStruggleNow() ||
            HasUsableBattleItem())
        {
            return;
        }

        enemyTurnRoutine = StartCoroutine(AutoPassPlayerTurn());
    }

    private static BattleItemUseResult ItemResult(
        BattleItemUseStatus status,
        string message
    ) => new(status, message);

    private void HandleNumberChanged(NumberChange change)
    {
        if (!isTrackingNumberLoss ||
            change.Delta >= 0 ||
            change.Reason == NumberChangeReason.Stolen)
        {
            return;
        }

        accumulatedNumberLoss += -change.Delta;
    }

    private void SetPhase(EncounterPhase phase)
    {
        if (Phase == phase)
        {
            return;
        }

        Phase = phase;
        PhaseChanged?.Invoke(phase);
    }
}
