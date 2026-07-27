using System;
using System.Collections;
using Game.Runtime.Data;
using UnityEngine;

public readonly struct EnemyIntentResolution
{
    public EnemyIntentResolution(
        int damageToPlayer,
        int selfDamage,
        string description,
        int numberToSteal = 0,
        bool escapes = false,
        bool attemptsItemSteal = false,
        int noItemFallbackDamage = 0,
        bool stunsPlayerOnFallback = false
    )
    {
        DamageToPlayer = damageToPlayer;
        SelfDamage = selfDamage;
        Description = description;
        NumberToSteal = numberToSteal;
        Escapes = escapes;
        AttemptsItemSteal = attemptsItemSteal;
        NoItemFallbackDamage = noItemFallbackDamage;
        StunsPlayerOnFallback = stunsPlayerOnFallback;
    }

    public int DamageToPlayer { get; }
    public int SelfDamage { get; }
    public string Description { get; }
    public int NumberToSteal { get; }
    public bool Escapes { get; }
    public bool AttemptsItemSteal { get; }
    public int NoItemFallbackDamage { get; }
    public bool StunsPlayerOnFallback { get; }
}

[RequireComponent(typeof(GridObject))]
public class EnemyActor : MonoBehaviour, IFogVisibilityResponder
{
    [SerializeField] private EnemyDefinition definition;
    [SerializeField] private EnemyWorldUI worldUIPrefab;
    [SerializeField] private Vector3 worldUIOffset = new(0f, 1.4f, 0f);
    [Header("Animation States")]
    [SerializeField] private string idleAnimationState = "SmallChicken_idle";
    [SerializeField] private string attackAnimationState = "SmallChicken_attack";
    [SerializeField] private string specialAnimationState = "SmallChicken_attack";
    [Header("Boss Phase 2 Animation States")]
    [SerializeField] private string bossPhase2IdleAnimationState = "Boss_idle2";
    [SerializeField] private string bossPhase2AttackAnimationState = "Boss_attack2";
    [SerializeField] private string bossPhase2SpecialAnimationState = "Boss_attack2";
    [SerializeField] private string bossLockedIdleAnimationState = "Boss_idle";
    [Header("Hit Flash")]
    [SerializeField, Min(1)] private int hitFlashCount = 2;
    [SerializeField, Min(0f)] private float hitFlashDuration = 0.14f;
    [SerializeField, Min(0f)] private float hitFlashRecoveryDuration = 0.1f;

    private EnemyWorldUI worldUI;
    private Animator characterAnimator;
    private SpriteRenderer characterSpriteRenderer;
    private FogVisibilityTarget fogVisibilityTarget;
    private int intentIndex = -1;
    private int projectedRewardRound = 1;
    private int nextAttackBonus;
    private CellVisibility fogVisibility = CellVisibility.Unexplored;
    private bool showingCombatInformation;

    public EnemyDefinition Definition => definition;
    public bool IsBoss =>
        definition != null &&
        definition.BehaviorType == EnemyBehaviorType.Boss;
    public bool IgnoresFog => IsBoss;
    private bool ShouldLockBossExplorationAnimation =>
        IsBoss && !showingCombatInformation;
    public bool IsHealthResolved { get; private set; }
    public int ResolvedMaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public EnemyWorldUI WorldUI => worldUI;
    public EnemyIntentType CurrentIntent { get; private set; }
    public bool HasLockedIntent { get; private set; }
    public int NextAttackBonus => nextAttackBonus;
    public int StolenNumberEscrow { get; private set; }

    public event Action HealthResolved;
    public event Action HealthChanged;

    private void Awake()
    {
        fogVisibilityTarget = GetComponent<FogVisibilityTarget>();
        if (fogVisibilityTarget == null)
        {
            fogVisibilityTarget = gameObject.AddComponent<FogVisibilityTarget>();
        }

        fogVisibilityTarget.ConfigureAsEnemy(this);
    }

    private void OnEnable()
    {
        GameLocalization.LanguageChanged += HandleLanguageChanged;
    }

    private void OnDisable()
    {
        GameLocalization.LanguageChanged -= HandleLanguageChanged;
    }

    private void Start()
    {
        characterAnimator = GetComponentInChildren<Animator>(true);
        characterSpriteRenderer = characterAnimator != null
            ? characterAnimator.GetComponent<SpriteRenderer>()
            : GetComponentInChildren<SpriteRenderer>(true);
        if (IsBoss)
        {
            PlayAnimation(bossLockedIdleAnimationState);
        }
        EnsureWorldUI();
        ApplyFogVisibility(fogVisibilityTarget.CurrentVisibility);
    }

    public void FacePlayer(Vector2Int playerGridPosition)
    {
        if (characterSpriteRenderer == null)
        {
            characterSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        GridObject gridObject = GetComponent<GridObject>();
        if (characterSpriteRenderer == null || !gridObject.IsPlaced)
        {
            return;
        }

        characterSpriteRenderer.flipX =
            playerGridPosition.x > gridObject.GridPosition.x;
    }

    public void LockFirstIntent()
    {
        intentIndex = 0;
        LockIntentAtCurrentIndex();
    }

    public void LockNextIntent()
    {
        EnemyIntentType[] sequence = definition != null
            ? definition.IntentSequence
            : null;
        if (sequence == null || sequence.Length == 0)
        {
            CurrentIntent = EnemyIntentType.Wait;
            HasLockedIntent = true;
            ShowLockedIntent();
            return;
        }

        intentIndex = (intentIndex + 1) % sequence.Length;
        LockIntentAtCurrentIndex();
    }

    public int GetCurrentIntentDamage()
    {
        if (definition == null)
        {
            return 0;
        }

        return CurrentIntent switch
        {
            EnemyIntentType.Attack =>
                definition.AttackDamage + nextAttackBonus,
            EnemyIntentType.Special
                when definition.BehaviorType == EnemyBehaviorType.SmallChicken =>
                definition.SpecialDamage,
            EnemyIntentType.Special
                when definition.BehaviorType == EnemyBehaviorType.Boss =>
                definition.SpecialDamage,
            EnemyIntentType.Steal
                when definition.BehaviorType == EnemyBehaviorType.Hamster =>
                definition.AttackDamage,
            _ => 0
        };
    }

    public void ShowLockedIntent()
    {
        if (fogVisibility != CellVisibility.Visible)
        {
            return;
        }

        EnsureWorldUI();
        int damage = GetCurrentIntentDamage();
        worldUI?.ShowIntent(
            definition.BehaviorType,
            CurrentIntent,
            GetLockedIntentDescription(damage)
        );
    }

    public EnemyIntentResolution ResolveLockedIntent()
    {
        if (definition == null || !HasLockedIntent)
        {
            return new EnemyIntentResolution(
                0,
                0,
                GameLocalization.Get("enemy.action.wait")
            );
        }

        if (definition.BehaviorType == EnemyBehaviorType.DrunkenRaider)
        {
            return ResolveDrunkenRaiderIntent();
        }

        if (definition.BehaviorType == EnemyBehaviorType.Hamster)
        {
            return ResolveHamsterIntent();
        }

        if (definition.BehaviorType == EnemyBehaviorType.Boss)
        {
            return ResolveBossIntent();
        }

        int damage = GetCurrentIntentDamage();
        string description = CurrentIntent switch
        {
            EnemyIntentType.Attack =>
                GameLocalization.Get("enemy.action.attack", damage),
            EnemyIntentType.Special =>
                GameLocalization.Get("enemy.action.peck", damage),
            _ => GameLocalization.Get("enemy.action.wait")
        };
        return new EnemyIntentResolution(damage, 0, description);
    }

    public void ShowIntentResolution(string description)
    {
        if (fogVisibility != CellVisibility.Visible ||
            string.IsNullOrWhiteSpace(description))
        {
            return;
        }

        worldUI?.HideIntent();
        ToastPanel.Show(description);
    }

    public void PlayCurrentIntentAnimation()
    {
        string stateName = CurrentIntent switch
        {
            EnemyIntentType.Attack => attackAnimationState,
            EnemyIntentType.Steal => attackAnimationState,
            EnemyIntentType.StealItem => specialAnimationState,
            EnemyIntentType.Charge => specialAnimationState,
            EnemyIntentType.Special => specialAnimationState,
            _ => idleAnimationState
        };
        PlayAnimation(stateName);
    }

    public void PlayIdleAnimation()
    {
        PlayAnimation(idleAnimationState);
    }

    public void PlaySpecialAnimation()
    {
        PlayAnimation(specialAnimationState);
    }

    public float GetSpecialAnimationDuration()
    {
        return GetAnimationDuration(specialAnimationState);
    }

    public float GetCurrentIntentAnimationDuration()
    {
        string stateName = CurrentIntent switch
        {
            EnemyIntentType.Attack => attackAnimationState,
            EnemyIntentType.Steal => attackAnimationState,
            EnemyIntentType.StealItem => specialAnimationState,
            EnemyIntentType.Charge => specialAnimationState,
            EnemyIntentType.Special => specialAnimationState,
            _ => idleAnimationState
        };

        return GetAnimationDuration(stateName);
    }

    private float GetAnimationDuration(string stateName)
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>(true);
        }

        RuntimeAnimatorController controller =
            characterAnimator != null
                ? characterAnimator.runtimeAnimatorController
                : null;
        if (controller == null || string.IsNullOrWhiteSpace(stateName))
        {
            return 0f;
        }

        foreach (AnimationClip clip in controller.animationClips)
        {
            if (clip != null && clip.name == stateName)
            {
                return clip.length;
            }
        }

        return 0f;
    }

    public void ResolveHealth(bool roll)
    {
        if (IsHealthResolved || definition == null)
        {
            return;
        }

        int resolvedHP;
        if (!definition.CanRollHP)
        {
            resolvedHP = definition.FixedHP;
        }
        else if (roll)
        {
            resolvedHP = GameRandom.RangeInclusive(
                definition.MinHP,
                definition.MaxHP
            );
        }
        else
        {
            resolvedHP = definition.StableHP;
        }

        ResolvedMaxHP = Mathf.Max(1, resolvedHP);
        CurrentHP = ResolvedMaxHP;
        IsHealthResolved = true;
        HealthResolved?.Invoke();
        HealthChanged?.Invoke();
    }

    public void ShowCombatInformation(int rewardRound = 1)
    {
        showingCombatInformation = true;
        projectedRewardRound = Mathf.Max(1, rewardRound);
        if (fogVisibility != CellVisibility.Visible)
        {
            return;
        }

        EnsureWorldUI();
        worldUI?.ShowCombat(
            CurrentHP,
            ResolvedMaxHP,
            GetRewardDisplay(projectedRewardRound)
        );
    }

    public bool ApplyDamage(int amount)
    {
        if (!IsHealthResolved || amount <= 0 || CurrentHP <= 0)
        {
            return false;
        }

        int previousHP = CurrentHP;
        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        int actualDamage = previousHP - CurrentHP;
        GameHUDPanel.Active?.ShowWorldDelta(
            -actualDamage,
            NumberChangeReason.Damage,
            transform.position
        );
        HealthChanged?.Invoke();
        ShowCombatInformation(projectedRewardRound);
        return CurrentHP == 0;
    }

    public IEnumerator PlayHitFlash()
    {
        if (characterSpriteRenderer == null)
        {
            characterSpriteRenderer =
                GetComponentInChildren<SpriteRenderer>(true);
        }
        if (characterSpriteRenderer == null)
        {
            yield break;
        }

        Color originalColor = characterSpriteRenderer.color;
        Color hitColor = new(1f, 0.12f, 0.12f, originalColor.a);
        int count = Mathf.Max(1, hitFlashCount);
        for (int i = 0; i < count; i++)
        {
            characterSpriteRenderer.color = hitColor;
            if (hitFlashDuration > 0f)
            {
                yield return new WaitForSeconds(hitFlashDuration);
            }

            characterSpriteRenderer.color = originalColor;
            if (i < count - 1 && hitFlashRecoveryDuration > 0f)
            {
                yield return new WaitForSeconds(
                    hitFlashRecoveryDuration
                );
            }
        }

        characterSpriteRenderer.color = originalColor;
    }

    public void ReleaseAndDestroy()
    {
        GetComponent<GridObject>().ReleaseAndDestroy();
    }

    public void RecordStolenNumber(int amount)
    {
        StolenNumberEscrow += Mathf.Max(0, amount);
    }

    public bool TryTransitionToBossNextPhase()
    {
        EnemyDefinition nextPhase = definition != null
            ? definition.BossNextPhase
            : null;
        if (definition == null ||
            definition.BehaviorType != EnemyBehaviorType.Boss ||
            nextPhase == null)
        {
            return false;
        }

        definition = nextPhase;
        idleAnimationState = bossPhase2IdleAnimationState;
        attackAnimationState = bossPhase2AttackAnimationState;
        specialAnimationState = bossPhase2SpecialAnimationState;
        intentIndex = -1;
        HasLockedIntent = false;
        CurrentIntent = EnemyIntentType.Wait;
        nextAttackBonus = 0;
        StolenNumberEscrow = 0;
        IsHealthResolved = false;
        ResolvedMaxHP = 0;
        CurrentHP = 0;
        ResolveHealth(false);
        PlayIdleAnimation();
        ShowCombatInformation(1);
        return true;
    }

    public void ShowExplorationInformation()
    {
        showingCombatInformation = false;
        if (IsBoss)
        {
            PlayAnimation(bossLockedIdleAnimationState);
        }

        if (fogVisibility != CellVisibility.Visible)
        {
            return;
        }

        EnsureWorldUI();
        worldUI?.ShowExploration(
            definition != null ? definition.RewardPreview : "0"
        );
    }

    public void ApplyFogVisibility(CellVisibility visibility)
    {
        if (IgnoresFog)
        {
            visibility = CellVisibility.Visible;
        }

        fogVisibility = visibility;
        if (fogVisibilityTarget == null)
        {
            return;
        }

        switch (visibility)
        {
            case CellVisibility.Visible:
                fogVisibilityTarget.SetContentVisible(true);
                fogVisibilityTarget.SetEnemyMarkerVisible(false);
                EnsureWorldUI();
                if (showingCombatInformation && IsHealthResolved)
                {
                    worldUI?.ShowCombat(
                        CurrentHP,
                        ResolvedMaxHP,
                        GetRewardDisplay(projectedRewardRound)
                    );
                    if (HasLockedIntent)
                    {
                        ShowLockedIntent();
                    }
                }
                else
                {
                    worldUI?.ShowExploration(
                        definition != null ? definition.RewardPreview : "0"
                    );
                }
                break;

            case CellVisibility.Explored:
                fogVisibilityTarget.SetContentVisible(false);
                fogVisibilityTarget.SetEnemyMarkerVisible(true);
                worldUI?.HideAll();
                break;

            default:
                fogVisibilityTarget.SetContentVisible(false);
                fogVisibilityTarget.SetEnemyMarkerVisible(false);
                worldUI?.HideAll();
                break;
        }
    }

    private void EnsureWorldUI()
    {
        if (worldUI != null)
        {
            return;
        }

        if (worldUIPrefab == null)
        {
            worldUIPrefab = Resources.Load<EnemyWorldUI>("UI/EnemyWorldUI");
        }

        if (worldUIPrefab == null)
        {
            return;
        }

        worldUI = Instantiate(worldUIPrefab, transform);
        LocalizationFontManager.ApplyTo(worldUI.gameObject);
        worldUI.transform.localPosition = worldUIOffset;
        worldUI.transform.localRotation = Quaternion.identity;
    }

    private void LockIntentAtCurrentIndex()
    {
        EnemyIntentType[] sequence = definition != null
            ? definition.IntentSequence
            : null;
        CurrentIntent = sequence != null && sequence.Length > 0
            ? sequence[Mathf.Clamp(intentIndex, 0, sequence.Length - 1)]
            : EnemyIntentType.Wait;
        HasLockedIntent = true;
        ShowLockedIntent();
    }

    private void PlayAnimation(string stateName)
    {
        if (characterAnimator == null)
        {
            characterAnimator = GetComponentInChildren<Animator>(true);
        }

        if (ShouldLockBossExplorationAnimation)
        {
            stateName = bossLockedIdleAnimationState;
        }

        if (characterAnimator == null ||
            characterAnimator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (characterAnimator.HasState(0, stateHash))
        {
            if (ShouldLockBossExplorationAnimation &&
                characterAnimator.GetCurrentAnimatorStateInfo(0)
                    .shortNameHash == stateHash)
            {
                return;
            }

            characterAnimator.Play(stateHash, 0, 0f);
        }
    }

    private string GetLockedIntentDescription(int damage)
    {
        if (definition == null)
        {
            return GameLocalization.Get("enemy.intent.wait");
        }

        if (definition.BehaviorType == EnemyBehaviorType.Boss)
        {
            return CurrentIntent switch
            {
                EnemyIntentType.Attack =>
                    GameLocalization.Get("enemy.intent.attack", damage),
                EnemyIntentType.Charge =>
                    GameLocalization.Get("enemy.intent.charge"),
                EnemyIntentType.Special =>
                    GameLocalization.Get(
                        "enemy.intent.heavy_attack",
                        damage
                    ),
                EnemyIntentType.StealItem =>
                    GameLocalization.Get(
                        "enemy.intent.steal_item",
                        definition.BossNoItemDamage
                    ),
                _ => GameLocalization.Get("enemy.intent.wait")
            };
        }

        if (definition.BehaviorType == EnemyBehaviorType.DrunkenRaider &&
            CurrentIntent == EnemyIntentType.Special)
        {
            return GameLocalization.Get("enemy.intent.drink");
        }

        if (definition.BehaviorType == EnemyBehaviorType.Hamster)
        {
            return CurrentIntent switch
            {
                EnemyIntentType.Special =>
                    GameLocalization.Get(
                        "enemy.intent.escape_with",
                        StolenNumberEscrow
                    ),
                EnemyIntentType.Steal =>
                    GameLocalization.Get(
                        "enemy.intent.attack_and_steal",
                        damage,
                        definition.HamsterStealAmount
                    ),
                _ => GameLocalization.Get("enemy.intent.wait")
            };
        }

        return CurrentIntent switch
        {
            EnemyIntentType.Attack =>
                GameLocalization.Get("enemy.intent.attack", damage),
            EnemyIntentType.Special =>
                GameLocalization.Get("enemy.intent.peck", damage),
            _ => GameLocalization.Get("enemy.intent.wait")
        };
    }

    private void HandleLanguageChanged()
    {
        if (HasLockedIntent && fogVisibility == CellVisibility.Visible)
        {
            ShowLockedIntent();
        }
    }

    private EnemyIntentResolution ResolveDrunkenRaiderIntent()
    {
        if (CurrentIntent == EnemyIntentType.Attack)
        {
            int damage = GetCurrentIntentDamage();
            nextAttackBonus = 0;
            return new EnemyIntentResolution(
                damage,
                0,
                GameLocalization.Get(
                    "enemy.action.basic_attack",
                    damage
                )
            );
        }

        if (CurrentIntent != EnemyIntentType.Special)
        {
            return new EnemyIntentResolution(0, 0, "等待");
        }

        switch (definition.RollDrunkenRaiderDrinkOutcome())
        {
            case DrunkenRaiderDrinkOutcome.Strengthen:
                nextAttackBonus = definition.RaiderNextAttackBonus;
                return new EnemyIntentResolution(
                    0,
                    0,
                    GameLocalization.Get(
                        "enemy.action.drink_strengthen",
                        nextAttackBonus
                    )
                );

            case DrunkenRaiderDrinkOutcome.SelfDamage:
                return new EnemyIntentResolution(
                    0,
                    definition.RaiderSelfDamage,
                    GameLocalization.Get(
                        "enemy.action.drink_self_damage",
                        definition.RaiderSelfDamage
                    )
                );

            default:
                return new EnemyIntentResolution(
                    0,
                    0,
                    GameLocalization.Get("enemy.action.drink_stunned")
                );
        }
    }

    private EnemyIntentResolution ResolveHamsterIntent()
    {
        if (CurrentIntent == EnemyIntentType.Special)
        {
            return new EnemyIntentResolution(
                0,
                0,
                GameLocalization.Get(
                    "enemy.action.escape",
                    StolenNumberEscrow
                ),
                escapes: true
            );
        }

        if (CurrentIntent != EnemyIntentType.Steal)
        {
            return new EnemyIntentResolution(
                0,
                0,
                GameLocalization.Get("enemy.action.wait")
            );
        }

        return new EnemyIntentResolution(
            definition.AttackDamage,
            0,
            GameLocalization.Get(
                "enemy.action.attack_and_steal",
                definition.AttackDamage,
                definition.HamsterStealAmount
            ),
            definition.HamsterStealAmount
        );
    }

    private EnemyIntentResolution ResolveBossIntent()
    {
        switch (CurrentIntent)
        {
            case EnemyIntentType.Attack:
                return new EnemyIntentResolution(
                    definition.AttackDamage,
                    0,
                    GameLocalization.Get(
                        "enemy.action.attack",
                        definition.AttackDamage
                    )
                );

            case EnemyIntentType.Charge:
                return new EnemyIntentResolution(
                    0,
                    0,
                    GameLocalization.Get("enemy.action.charge")
                );

            case EnemyIntentType.Special:
                return new EnemyIntentResolution(
                    definition.SpecialDamage,
                    0,
                    GameLocalization.Get(
                        "enemy.action.heavy_attack",
                        definition.SpecialDamage
                    )
                );

            case EnemyIntentType.StealItem:
                return new EnemyIntentResolution(
                    0,
                    0,
                    GameLocalization.Get("enemy.action.try_steal_item"),
                    attemptsItemSteal: true,
                    noItemFallbackDamage: definition.BossNoItemDamage,
                    stunsPlayerOnFallback: true
                );

            default:
                return new EnemyIntentResolution(
                    0,
                    0,
                    GameLocalization.Get("enemy.action.wait")
                );
        }
    }

    private string GetRewardDisplay(int battleRound)
    {
        if (definition == null)
        {
            return "0";
        }

        if (definition.RewardMode != EnemyRewardMode.TurnScaled ||
            !IsHealthResolved)
        {
            return definition.RewardPreview;
        }

        int reward = definition.CalculateNumberReward(
            ResolvedMaxHP,
            battleRound,
            0
        );
        return GameLocalization.Get(
            "enemy.reward.turn_result",
            reward,
            battleRound
        );
    }
}
