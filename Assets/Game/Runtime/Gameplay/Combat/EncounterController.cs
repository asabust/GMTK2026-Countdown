using System;
using System.Collections;
using Game.Runtime.Core;
using Game.Runtime.Data;
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
    private PlayerSkillController playerSkills;
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
    private int successfulGreedAttempts;
    private int currentGreedyWinnings;
    private bool hasBrokenMirror;
    private bool hasUsedStruggle;
    private int battleRound;

    public EncounterPhase Phase { get; private set; } = EncounterPhase.Exploration;
    public EnemyActor CurrentEnemy { get; private set; }
    public bool IsInEncounter => CurrentEnemy != null;
    public int AccumulatedNumberLoss => accumulatedNumberLoss;
    public int CurrentBasicAttackDamage
    {
        get
        {
            int additiveDamage =
                basicAttackDamage +
                (playerRunStats?.OfferingAttackBonus ?? 0) +
                (playerRunStats?.TimedAttackBonus ?? 0);
            return playerRunStats != null
                ? playerRunStats.ApplyBasicAttackMultiplier(additiveDamage)
                : additiveDamage;
        }
    }

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
        successfulGreedAttempts = 0;
        currentGreedyWinnings = 0;
        hasBrokenMirror = false;
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
        playerSkills = GetComponent<PlayerSkillController>();
        if (playerSkills == null)
        {
            playerSkills = gameObject.AddComponent<PlayerSkillController>();
        }
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
            !HasUsableBattleItem() &&
            !HasUsableBattleSkill();

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
                playerSkills,
                TryUseBattleSkill,
                ValidateBattleSkill,
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
        int damage = CurrentBasicAttackDamage;
        bool defeated = CurrentEnemy.ApplyDamage(damage);
        playerRunStats?.ConsumeBasicAttackEmpowerment();
        playerRunStats?.CompletePlayerAction();
        playerSkills?.CompletePlayerAction();
        UIManager.Instance?.Close<BattleActionPanel>();
        enemyTurnRoutine = StartCoroutine(
            ResolvePlayerDamagePresentation(CurrentEnemy, defeated)
        );

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
        playerSkills?.CompletePlayerAction();
        UIManager.Instance?.Close<BattleActionPanel>();
        enemyTurnRoutine = StartCoroutine(
            ResolvePlayerDamagePresentation(CurrentEnemy, defeated)
        );

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

        playerSkills?.CompletePlayerAction();
        UIManager.Instance?.Close<BattleActionPanel>();
        enemyTurnRoutine = StartCoroutine(ResolveEnemyTurn());
    }

    private IEnumerator ResolvePlayerDamagePresentation(
        EnemyActor damagedEnemy,
        bool defeated
    )
    {
        if (damagedEnemy != null)
        {
            yield return damagedEnemy.PlayHitFlash();
        }

        enemyTurnRoutine = null;
        if (CurrentEnemy == null || CurrentEnemy != damagedEnemy)
        {
            yield break;
        }

        if (defeated)
        {
            ResolveDefeatedEnemy();
            yield break;
        }

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

        int damageToPlayer = intentResolution.DamageToPlayer;
        bool playerStunned = false;
        string resolutionDescription = intentResolution.Description;
        if (intentResolution.AttemptsItemSteal)
        {
            if (playerInventory != null &&
                playerInventory.TryRemoveRandomItem(
                    out CollectibleDefinition stolenItem
                ))
            {
                resolutionDescription = GameLocalization.Get(
                    "enemy.action.item_stolen",
                    stolenItem.DisplayName
                );
            }
            else
            {
                damageToPlayer = intentResolution.NoItemFallbackDamage;
                playerStunned = intentResolution.StunsPlayerOnFallback;
                resolutionDescription = GameLocalization.Get(
                    playerStunned
                        ? "enemy.action.no_item_stunned"
                        : "enemy.action.no_item",
                    damageToPlayer
                );
            }
        }

        IncomingAttackResolution attackResolution =
            playerRunStats != null
                ? playerRunStats.ResolveIncomingAttack(
                    damageToPlayer
                )
                : new IncomingAttackResolution(
                    damageToPlayer,
                    0,
                    false,
                    damageToPlayer
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
        if (intentResolution.NumberToSteal > 0 &&
            stolenNumber != intentResolution.NumberToSteal)
        {
            resolutionDescription = GameLocalization.Get(
                "enemy.action.actual_stolen",
                intentResolution.Description,
                stolenNumber
            );
        }
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
        if (playerStunned)
        {
            playerController.SetExternalInputLocked(true);
            SetPhase(EncounterPhase.PlayerTurn);
            enemyTurnRoutine = StartCoroutine(ResolveStunnedPlayerTurn());
            yield break;
        }

        BeginPlayerTurn();
    }

    private IEnumerator ResolveStunnedPlayerTurn()
    {
        if (CurrentEnemy == null)
        {
            enemyTurnRoutine = null;
            yield break;
        }

        battleRound++;
        playerSkills?.CompletePlayerAction();
        CurrentEnemy.ShowCombatInformation(battleRound + 1);
        CurrentEnemy.ShowIntentResolution(
            GameLocalization.Get("battle.player_stunned")
        );
        if (autoPassDelay > 0f)
        {
            yield return new WaitForSeconds(autoPassDelay);
        }

        if (CurrentEnemy == null ||
            numberResource.CurrentValue <= numberResource.MinimumValue)
        {
            enemyTurnRoutine = null;
            yield break;
        }

        enemyTurnRoutine = null;
        enemyTurnRoutine = StartCoroutine(ResolveEnemyTurn());
    }

    private void HandlePlayerDefeated()
    {
        isTrackingNumberLoss = false;
        UIManager.Instance?.Close<BattleActionPanel>();
        battleStatusWorldUI?.SetCombatVisible(false);
        CurrentEnemy?.WorldUI?.HideIntent();
        playerController.SetExternalInputLocked(true);
        GameManager.Instance?.GameOver(
            GameLocalization.Get("game_over.reason.combat")
        );
    }

    private void ResolveDefeatedEnemy()
    {
        if (CurrentEnemy?.Definition?.BehaviorType ==
            EnemyBehaviorType.Boss)
        {
            ResolveBossDefeated();
            return;
        }

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

    private void ResolveBossDefeated()
    {
        EnemyActor defeatedBoss = CurrentEnemy;
        UIManager.Instance?.Close<BattleActionPanel>();
        playerController.SetExternalInputLocked(true);
        if (defeatedBoss != null &&
            defeatedBoss.TryTransitionToBossNextPhase())
        {
            SetPhase(EncounterPhase.ResolvingEnemyAction);
            defeatedBoss.ShowIntentResolution(
                "钟声未止：进入第二阶段，玩家先行动"
            );
            enemyTurnRoutine = StartCoroutine(
                ResolveBossPhaseTransition(defeatedBoss)
            );
            return;
        }

        ResolveBossVictory(defeatedBoss);
    }

    private IEnumerator ResolveBossPhaseTransition(EnemyActor boss)
    {
        float duration = Mathf.Max(enemyActionDuration, 0.7f);
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        enemyTurnRoutine = null;
        if (CurrentEnemy != boss)
        {
            yield break;
        }

        BeginPlayerTurn();
    }

    private void ResolveBossVictory(EnemyActor defeatedBoss)
    {
        isTrackingNumberLoss = false;
        SetPhase(EncounterPhase.Reward);
        battleStatusWorldUI?.SetCombatVisible(false);
        defeatedBoss?.WorldUI?.HideIntent();
        CurrentEnemy = null;
        defeatedBoss?.ReleaseAndDestroy();
        playerController.SetExternalInputLocked(true);
        GameManager.Instance?.Victory(
            GameLocalization.Get("game_over.reason.victory")
        );
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
        successfulGreedAttempts = 0;
        currentGreedyWinnings = 0;
        hasBrokenMirror = HasBrokenMirror();
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
                InventoryAddResult.Success => GameLocalization.Get(
                    "battle.drop.received",
                    drop.DisplayName
                ),
                InventoryAddResult.ItemSlotsFull =>
                    GameLocalization.Get(
                        "battle.drop.inventory_full",
                        drop.DisplayName
                    ),
                InventoryAddResult.MaximumStacksReached =>
                    GameLocalization.Get(
                        "battle.drop.maximum",
                        drop.DisplayName
                    ),
                _ => GameLocalization.Get(
                    "battle.drop.failed",
                    drop.DisplayName
                )
            };
            results.Add(resultText);
        }

        return results.Count > 0
            ? GameLocalization.Get(
                "battle.drop.summary",
                string.Join(GameLocalization.Get("common.list_separator"), results)
            )
            : string.Empty;
    }

    private BattleRewardResult ResolveRewardChoice(BattleRewardChoice choice)
    {
        if (rewardChoiceResolved)
        {
            return lockedRewardResult;
        }

        if (choice == BattleRewardChoice.Safe)
        {
            rewardChoiceResolved = true;
            int safeGain = successfulGreedAttempts > 0
                ? currentGreedyWinnings
                : currentBattleLoot;
            lockedRewardResult = new BattleRewardResult(
                choice,
                true,
                safeGain
            );
            return lockedRewardResult;
        }

        float effectiveChance = successfulGreedAttempts == 0
            ? GetEffectiveGreedySuccessChance()
            : GetMirrorAdditionalGreedChance(
                successfulGreedAttempts
            );
        float effectiveMultiplier = GetEffectiveGreedyMultiplier();
        bool succeeded = GameRandom.Chance(effectiveChance);
        if (!succeeded)
        {
            rewardChoiceResolved = true;
            lockedRewardResult = new BattleRewardResult(
                choice,
                false,
                currentGreedyWinnings
            );
            return lockedRewardResult;
        }

        int independentGain = Mathf.FloorToInt(
            currentBattleLoot * effectiveMultiplier
        );
        currentGreedyWinnings += independentGain;
        successfulGreedAttempts++;

        float nextChance = GetMirrorAdditionalGreedChance(
            successfulGreedAttempts
        );
        bool canContinue = hasBrokenMirror && nextChance > 0f;
        if (!canContinue)
        {
            rewardChoiceResolved = true;
            lockedRewardResult = new BattleRewardResult(
                choice,
                true,
                currentGreedyWinnings
            );
            return lockedRewardResult;
        }

        return new BattleRewardResult(
            choice,
            true,
            currentGreedyWinnings,
            isFinal: false,
            nextGreedSuccessChance: nextChance
        );
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
        successfulGreedAttempts = 0;
        currentGreedyWinnings = 0;
        hasBrokenMirror = false;
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
        successfulGreedAttempts = 0;
        currentGreedyWinnings = 0;
        hasBrokenMirror = false;
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

        if (battleStatusWorldUI != null)
        {
            LocalizationFontManager.ApplyTo(
                battleStatusWorldUI.gameObject
            );
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

    private bool HasBrokenMirror()
    {
        float mirrorCount = playerInventory != null
            ? playerInventory.GetRelicEffect(
                CollectibleEffectType.RepeatedGreed
            )
            : 0f;
        return mirrorCount > 0f;
    }

    private static float GetMirrorAdditionalGreedChance(
        int successfulAttempts
    ) => Mathf.Clamp01(0.5f - successfulAttempts * 0.1f);

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

    private bool HasUsableBattleSkill()
    {
        if (playerSkills == null)
        {
            return false;
        }

        foreach (LearnedSkillState state in playerSkills.GetLearnedSkills())
        {
            if (ValidateBattleSkill(state?.Definition).Succeeded)
            {
                return true;
            }
        }

        return false;
    }

    private BattleSkillUseResult ValidateBattleSkill(
        SkillDefinition definition
    )
    {
        if (Phase != EncounterPhase.PlayerTurn || CurrentEnemy == null)
        {
            return SkillResult(
                BattleSkillUseStatus.WrongPhase,
                GameLocalization.Get("battle.validation.wrong_phase")
            );
        }
        if (definition == null ||
            definition.SkillType == PlayerSkillType.BasicAttack)
        {
            return SkillResult(
                BattleSkillUseStatus.InvalidSkill,
                GameLocalization.Get("battle.skill.invalid")
            );
        }
        if (playerSkills == null || !playerSkills.Owns(definition))
        {
            return SkillResult(
                BattleSkillUseStatus.NotLearned,
                GameLocalization.Get("battle.skill.not_learned")
            );
        }

        int cooldown = playerSkills.GetCooldown(definition);
        if (cooldown > 0)
        {
            return SkillResult(
                BattleSkillUseStatus.OnCooldown,
                GameLocalization.Get("battle.skill.cooldown", cooldown)
            );
        }
        if (numberResource == null ||
            !numberResource.CanSpend(definition.NumberCost))
        {
            return SkillResult(
                BattleSkillUseStatus.NumberInsufficient,
                GameLocalization.Get("battle.validation.insufficient")
            );
        }

        return SkillResult(BattleSkillUseStatus.Success, string.Empty);
    }

    private BattleSkillUseResult TryUseBattleSkill(
        SkillDefinition definition
    )
    {
        BattleSkillUseResult validation = ValidateBattleSkill(definition);
        if (!validation.Succeeded)
        {
            return validation;
        }
        if (!numberResource.TrySpend(
                definition.NumberCost,
                NumberChangeReason.Skill,
                transform.position
            ))
        {
            return SkillResult(
                BattleSkillUseStatus.NumberInsufficient,
                GameLocalization.Get("battle.validation.insufficient")
            );
        }

        SetPhase(EncounterPhase.ResolvingPlayerAction);
        battleRound++;
        CurrentEnemy.ShowCombatInformation(battleRound);

        bool defeated = false;
        bool dealtDamage = false;
        switch (definition.SkillType)
        {
            case PlayerSkillType.Bloodlust:
                playerRunStats?.ActivateBloodlust(
                    definition.BloodlustBasicAttacks,
                    definition.BloodlustMultiplier
                );
                break;

            case PlayerSkillType.Parasite:
                dealtDamage = definition.BaseDamage > 0;
                defeated = CurrentEnemy.ApplyDamage(definition.BaseDamage);
                if (defeated && definition.KillRestore > 0)
                {
                    numberResource.Add(
                        definition.KillRestore,
                        NumberChangeReason.Skill,
                        transform.position
                    );
                }
                break;

            case PlayerSkillType.Revenge:
                dealtDamage = definition.BaseDamage > 0;
                int hitCount = definition.MinimumHits;
                if (definition.MaximumHits > definition.MinimumHits &&
                    GameRandom.Chance(definition.ExtraHitChance))
                {
                    hitCount = definition.MaximumHits;
                }
                for (int i = 0; i < hitCount && !defeated; i++)
                {
                    defeated = CurrentEnemy.ApplyDamage(
                        definition.BaseDamage
                    );
                }
                break;
        }

        playerRunStats?.CompletePlayerAction();
        playerSkills?.CompletePlayerAction(definition);
        UIManager.Instance?.Close<BattleActionPanel>();
        if (dealtDamage)
        {
            enemyTurnRoutine = StartCoroutine(
                ResolvePlayerDamagePresentation(CurrentEnemy, defeated)
            );
        }
        else
        {
            enemyTurnRoutine = StartCoroutine(ResolveEnemyTurn());
        }

        return SkillResult(
            BattleSkillUseStatus.Success,
            GameLocalization.Get(
                "battle.validation.used",
                definition.DisplayName
            )
        );
    }

    private BattleItemUseResult ValidateBattleItem(
        CollectibleDefinition definition
    )
    {
        if (Phase != EncounterPhase.PlayerTurn || CurrentEnemy == null)
        {
            return ItemResult(
                BattleItemUseStatus.WrongPhase,
                GameLocalization.Get("battle.validation.wrong_phase")
            );
        }
        if (definition == null ||
            definition.Kind != CollectibleKind.Item ||
            definition.EffectType == CollectibleEffectType.None)
        {
            return ItemResult(
                BattleItemUseStatus.InvalidItem,
                GameLocalization.Get("battle.item.invalid")
            );
        }
        if (playerInventory == null ||
            playerInventory.GetCount(definition) <= 0)
        {
            return ItemResult(
                BattleItemUseStatus.NotOwned,
                GameLocalization.Get("battle.item.not_owned")
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
                        GameLocalization.Get("battle.item.number_full")
                    );
                }
                break;

            case CollectibleEffectType.NegateNextAttack:
                if (playerRunStats == null || playerRunStats.NegateNextAttack)
                {
                    return ItemResult(
                        BattleItemUseStatus.AlreadyActive,
                        GameLocalization.Get("battle.item.already_protected")
                    );
                }
                break;

            case CollectibleEffectType.NextEnemyPhaseShield:
                if (playerRunStats == null ||
                    playerRunStats.NextEnemyPhaseShield > 0)
                {
                    return ItemResult(
                        BattleItemUseStatus.AlreadyActive,
                        GameLocalization.Get("battle.item.shield_active")
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
                GameLocalization.Get("battle.item.changed")
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

        SetPhase(EncounterPhase.ResolvingPlayerAction);
        battleRound++;
        playerSkills?.CompletePlayerAction();
        CurrentEnemy?.ShowCombatInformation(battleRound);
        UIManager.Instance?.Close<BattleActionPanel>();
        enemyTurnRoutine = StartCoroutine(ResolveEnemyTurn());
        return ItemResult(
            BattleItemUseStatus.Success,
            GameLocalization.Get(
                "battle.validation.used",
                definition.DisplayName
            )
        );
    }

    private static BattleItemUseResult ItemResult(
        BattleItemUseStatus status,
        string message
    ) => new(status, message);

    private static BattleSkillUseResult SkillResult(
        BattleSkillUseStatus status,
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
