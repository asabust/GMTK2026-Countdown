using System;
using UnityEngine;

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

    private EnemyWorldUI worldUI;
    private Animator characterAnimator;
    private SpriteRenderer characterSpriteRenderer;
    private FogVisibilityTarget fogVisibilityTarget;
    private int intentIndex = -1;
    private CellVisibility fogVisibility = CellVisibility.Unexplored;
    private bool showingCombatInformation;

    public EnemyDefinition Definition => definition;
    public bool IsHealthResolved { get; private set; }
    public int ResolvedMaxHP { get; private set; }
    public int CurrentHP { get; private set; }
    public EnemyWorldUI WorldUI => worldUI;
    public EnemyIntentType CurrentIntent { get; private set; }
    public bool HasLockedIntent { get; private set; }

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

    private void Start()
    {
        characterAnimator = GetComponentInChildren<Animator>(true);
        characterSpriteRenderer = characterAnimator != null
            ? characterAnimator.GetComponent<SpriteRenderer>()
            : GetComponentInChildren<SpriteRenderer>(true);
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
            playerGridPosition.x < gridObject.GridPosition.x;
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
            EnemyIntentType.Attack => definition.AttackDamage,
            EnemyIntentType.Special => definition.SpecialDamage,
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
        string description = CurrentIntent switch
        {
            EnemyIntentType.Attack => $"意图：攻击 -{damage}",
            EnemyIntentType.Special => $"意图：啄地 -{damage}",
            _ => "意图：等待"
        };
        worldUI?.ShowIntent(description);
    }

    public void PlayCurrentIntentAnimation()
    {
        string stateName = CurrentIntent switch
        {
            EnemyIntentType.Attack => attackAnimationState,
            EnemyIntentType.Special => specialAnimationState,
            _ => idleAnimationState
        };
        PlayAnimation(stateName);
    }

    public void PlayIdleAnimation()
    {
        PlayAnimation(idleAnimationState);
    }

    public float GetCurrentIntentAnimationDuration()
    {
        string stateName = CurrentIntent switch
        {
            EnemyIntentType.Attack => attackAnimationState,
            EnemyIntentType.Special => specialAnimationState,
            _ => idleAnimationState
        };

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

    public void ShowCombatInformation()
    {
        showingCombatInformation = true;
        if (fogVisibility != CellVisibility.Visible)
        {
            return;
        }

        EnsureWorldUI();
        worldUI?.ShowCombat(
            CurrentHP,
            ResolvedMaxHP,
            definition != null ? definition.RewardNumber : 0
        );
    }

    public bool ApplyDamage(int amount)
    {
        if (!IsHealthResolved || amount <= 0 || CurrentHP <= 0)
        {
            return false;
        }

        CurrentHP = Mathf.Max(0, CurrentHP - amount);
        HealthChanged?.Invoke();
        ShowCombatInformation();
        return CurrentHP == 0;
    }

    public void ReleaseAndDestroy()
    {
        GetComponent<GridObject>().ReleaseAndDestroy();
    }

    public void ShowExplorationInformation()
    {
        showingCombatInformation = false;
        if (fogVisibility != CellVisibility.Visible)
        {
            return;
        }

        EnsureWorldUI();
        worldUI?.ShowExploration(definition != null ? definition.RewardNumber : 0);
    }

    public void ApplyFogVisibility(CellVisibility visibility)
    {
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
                        definition != null ? definition.RewardNumber : 0
                    );
                    if (HasLockedIntent)
                    {
                        ShowLockedIntent();
                    }
                }
                else
                {
                    worldUI?.ShowExploration(
                        definition != null ? definition.RewardNumber : 0
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

        if (characterAnimator == null ||
            characterAnimator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        int stateHash = Animator.StringToHash(stateName);
        if (characterAnimator.HasState(0, stateHash))
        {
            characterAnimator.Play(stateHash, 0, 0f);
        }
    }
}
