using System;
using System.Collections;
using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(NumberResource))]
public class PlayerGridController : GridEntity
{
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private string moveActionPath = "Player/Move";
    [SerializeField, Min(0)] private int moveCost = 1;
    [SerializeField, Min(0f)] private float moveDuration = 0.12f;
    [SerializeField] private string idleAnimationState = "Player_idle";
    [SerializeField] private string walkAnimationState = "player_walk";
    [SerializeField] private string attackAnimationState = "player_attack";

    private InputAction moveAction;
    private GridMap gridMap;
    private NumberResource numberResource;
    private Animator characterAnimator;
    private SpriteRenderer characterSpriteRenderer;
    private Scene boundScene;
    private Coroutine moveRoutine;
    private Renderer[] renderers;
    private bool inputArmed = true;
    private bool isAnimating;
    private bool contactLocked;
    private bool externalInputLocked;

    public override GridOccupantType OccupantType => GridOccupantType.Player;
    public bool CanAcceptMovement =>
        gridMap != null &&
        IsPlaced &&
        !isAnimating &&
        !contactLocked &&
        !externalInputLocked;

    public event Action<Vector2Int, Vector2Int> MoveCommitted;
    public event Action<Vector2Int, Vector2Int> MoveCompleted;
    public event Action<GridEntity, GridOccupantType> ContactRequested;
    public event Action<GridMap> GridBound;
    public event Action<GridMap> GridUnbound;

    private void Awake()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        numberResource = GetComponent<NumberResource>();
        characterAnimator = GetComponentInChildren<Animator>(true);
        characterSpriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        SetVisualsVisible(false);

        if (inputActions == null)
        {
            Debug.LogError("PlayerGridController has no InputActionAsset.", this);
            return;
        }

        moveAction = inputActions.FindAction(moveActionPath, false);
        if (moveAction == null)
        {
            Debug.LogError(
                $"Input action '{moveActionPath}' was not found.",
                this
            );
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        SceneManager.sceneUnloaded += OnSceneUnloaded;

        if (moveAction != null)
        {
            moveAction.performed += OnMovePerformed;
            moveAction.canceled += OnMoveCanceled;
        }
    }

    private void Start()
    {
        TryBindLoadedGrid();
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;

        if (moveAction != null)
        {
            moveAction.performed -= OnMovePerformed;
            moveAction.canceled -= OnMoveCanceled;
            moveAction.Disable();
        }
    }

    public void SetExternalInputLocked(bool locked)
    {
        externalInputLocked = locked;
    }

    public void CompleteContact()
    {
        contactLocked = false;
        inputArmed = moveAction == null || moveAction.ReadValue<Vector2>().sqrMagnitude < 0.01f;
    }

    public IEnumerator PlayAttackAnimation()
    {
        bool wasAnimating = isAnimating;
        isAnimating = true;
        PlayAnimation(attackAnimationState);

        try
        {
            float duration = GetAnimationDuration(attackAnimationState);
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
            else
            {
                yield return null;
            }
        }
        finally
        {
            PlayAnimation(idleAnimationState);
            isAnimating = wasAnimating;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        GridMap map = FindGridMap(scene);
        if (map != null)
        {
            BindToGrid(map);
        }
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (boundScene != scene)
        {
            return;
        }

        StopMovement();
        GridMap previousMap = gridMap;
        if (IsPlaced)
        {
            previousMap.RemoveEntity(this);
        }

        gridMap = null;
        boundScene = default;
        contactLocked = false;
        SetVisualsVisible(false);
        moveAction?.Disable();
        GridUnbound?.Invoke(previousMap);
    }

    private void TryBindLoadedGrid()
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            GridMap map = FindGridMap(scene);
            if (map != null)
            {
                BindToGrid(map);
                return;
            }
        }
    }

    private void BindToGrid(GridMap map)
    {
        if (gridMap != null && IsPlaced)
        {
            GridMap previousMap = gridMap;
            previousMap.RemoveEntity(this);
            GridUnbound?.Invoke(previousMap);
        }

        StopMovement();
        gridMap = map;
        boundScene = map.gameObject.scene;
        contactLocked = false;
        externalInputLocked = false;

        if (!gridMap.TryPlaceEntity(
                this,
                gridMap.PlayerStartCell,
                out GridPlacementResult result
            ))
        {
            Debug.LogError(
                $"Could not place player at {gridMap.PlayerStartCell}: {result}.",
                gridMap
            );
            gridMap = null;
            boundScene = default;
            SetVisualsVisible(true);
            return;
        }

        transform.position = gridMap.GetCellCenterWorld(GridPosition);
        inputArmed = true;
        SetVisualsVisible(true);
        moveAction?.Enable();
        GridBound?.Invoke(gridMap);
    }

    private void OnMovePerformed(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();
        if (value.sqrMagnitude < 0.25f || !inputArmed)
        {
            return;
        }

        inputArmed = false;
        if (!CanAcceptMovement || !TryGetCardinalDirection(value, out Vector2Int direction))
        {
            return;
        }

        TryStep(direction);
    }

    private void OnMoveCanceled(InputAction.CallbackContext context)
    {
        inputArmed = true;
    }

    private void TryStep(Vector2Int direction)
    {
        Vector2Int destination = GridPosition + direction;
        GridCell targetCell = gridMap.GetCell(destination);

        if (targetCell == null || !targetCell.IsWalkable)
        {
            return;
        }

        GridEntity target = targetCell.Occupant;
        if (target != null && target != this)
        {
            TryStartContact(target);
            return;
        }

        Vector2Int origin = GridPosition;
        if (numberResource == null ||
            !numberResource.CanSpend(moveCost, allowFatalSpend: true))
        {
            return;
        }

        if (!gridMap.TryMoveEntity(this, destination, out _))
        {
            return;
        }

        if (!numberResource.TrySpend(
            moveCost,
            NumberChangeReason.Move,
            transform.position,
            allowFatalSpend: true
        ))
        {
            gridMap.TryMoveEntity(this, origin, out _);
            return;
        }

        UpdateFacing(direction);
        MoveCommitted?.Invoke(origin, destination);
        moveRoutine = StartCoroutine(AnimateMove(origin, destination));
    }

    private void TryStartContact(GridEntity target)
    {
        if (target.OccupantType != GridOccupantType.Enemy &&
            target.OccupantType != GridOccupantType.Interactable)
        {
            return;
        }

        Action<GridEntity, GridOccupantType> handler = ContactRequested;
        if (handler == null)
        {
            Debug.LogWarning(
                $"No contact handler is registered for {target.name}.",
                target
            );
            return;
        }

        contactLocked = true;
        handler.Invoke(target, target.OccupantType);
    }

    private IEnumerator AnimateMove(Vector2Int origin, Vector2Int destination)
    {
        isAnimating = true;
        PlayAnimation(walkAnimationState);
        Vector3 start = transform.position;
        Vector3 end = gridMap.GetCellCenterWorld(destination);
        float animationDuration = GetAnimationDuration(walkAnimationState);
        float stepDuration = Mathf.Max(moveDuration, animationDuration);

        if (stepDuration <= 0f)
        {
            transform.position = end;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / stepDuration);
                t = t * t * (3f - 2f * t);
                transform.position = Vector3.LerpUnclamped(start, end, t);
                yield return null;
            }

            transform.position = end;
        }

        isAnimating = false;
        moveRoutine = null;
        PlayAnimation(idleAnimationState);
        MoveCompleted?.Invoke(origin, destination);

        if (numberResource != null &&
            numberResource.CurrentValue <= numberResource.MinimumValue)
        {
            externalInputLocked = true;
            GameManager.Instance?.GameOver(
                GameLocalization.Get("game_over.reason.movement")
            );
        }
    }

    private void StopMovement()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        isAnimating = false;
        PlayAnimation(idleAnimationState);
    }

    private void PlayAnimation(string stateName)
    {
        if (characterAnimator == null ||
            characterAnimator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(stateName))
        {
            return;
        }

        characterAnimator.Play(stateName, 0, 0f);
    }

    private float GetAnimationDuration(string clipName)
    {
        if (characterAnimator == null ||
            characterAnimator.runtimeAnimatorController == null ||
            string.IsNullOrWhiteSpace(clipName))
        {
            return 0f;
        }

        foreach (AnimationClip clip in characterAnimator.runtimeAnimatorController.animationClips)
        {
            if (clip != null && clip.name == clipName)
            {
                float playbackSpeed = Mathf.Abs(characterAnimator.speed);
                return playbackSpeed > 0f ? clip.length / playbackSpeed : clip.length;
            }
        }

        return 0f;
    }

    private void UpdateFacing(Vector2Int direction)
    {
        if (characterSpriteRenderer == null || direction.x == 0)
        {
            return;
        }

        // The source sprites face left, so only rightward movement is flipped.
        characterSpriteRenderer.flipX = direction.x > 0;
    }

    private void SetVisualsVisible(bool visible)
    {
        if (renderers == null)
        {
            return;
        }

        foreach (Renderer item in renderers)
        {
            item.enabled = visible;
        }
    }

    private static bool TryGetCardinalDirection(Vector2 value, out Vector2Int direction)
    {
        float absX = Mathf.Abs(value.x);
        float absY = Mathf.Abs(value.y);

        if (Mathf.Approximately(absX, absY))
        {
            direction = Vector2Int.zero;
            return false;
        }

        direction = absX > absY
            ? new Vector2Int(value.x > 0f ? 1 : -1, 0)
            : new Vector2Int(0, value.y > 0f ? 1 : -1);
        return true;
    }

    private static GridMap FindGridMap(Scene scene)
    {
        if (!scene.IsValid() || !scene.isLoaded)
        {
            return null;
        }

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            GridMap map = root.GetComponentInChildren<GridMap>(true);
            if (map != null)
            {
                return map;
            }
        }

        return null;
    }
}
