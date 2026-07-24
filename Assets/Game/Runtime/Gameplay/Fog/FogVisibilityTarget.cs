using System.Collections;
using UnityEngine;

public interface IFogVisibilityResponder
{
    void ApplyFogVisibility(CellVisibility visibility);
}

[RequireComponent(typeof(GridObject))]
public class FogVisibilityTarget : MonoBehaviour
{
    [SerializeField] private Renderer[] contentRenderers;
    [SerializeField] private Color exploredEnemyColor =
        new(0.38f, 0.48f, 0.62f, 0.8f);
    [SerializeField] private string markerSortingLayer = "Fog";
    [SerializeField] private int markerSortingOrder = 1;

    private GridEntity entity;
    private FogOfWarSystem fogOfWarSystem;
    private IFogVisibilityResponder responder;
    private Coroutine registrationRoutine;
    private SpriteRenderer enemySourceRenderer;
    private SpriteRenderer exploredEnemyMarker;
    private bool useEnemyMarker;

    public GridEntity Entity => entity;
    public CellVisibility CurrentVisibility { get; private set; } =
        CellVisibility.Unexplored;

    private void Awake()
    {
        entity = GetComponent<GridEntity>();
        responder = GetComponent<IFogVisibilityResponder>();

        if (contentRenderers == null || contentRenderers.Length == 0)
        {
            contentRenderers = GetComponentsInChildren<Renderer>(true);
        }

        ApplyVisibility(CellVisibility.Unexplored, force: true);
    }

    private void OnEnable()
    {
        registrationRoutine = StartCoroutine(RegisterWhenPlaced());
    }

    private void OnDisable()
    {
        if (registrationRoutine != null)
        {
            StopCoroutine(registrationRoutine);
            registrationRoutine = null;
        }

        fogOfWarSystem?.UnregisterVisibilityTarget(this);
        fogOfWarSystem = null;
    }

    public void ConfigureAsEnemy(EnemyActor enemy)
    {
        responder = enemy;
        useEnemyMarker = true;
        enemySourceRenderer = GetComponentInChildren<SpriteRenderer>(true);
        EnsureEnemyMarker();
        ApplyVisibility(CurrentVisibility, force: true);
    }

    public void ApplyVisibility(CellVisibility visibility)
    {
        ApplyVisibility(visibility, force: false);
    }

    public void SetContentVisible(bool visible)
    {
        if (contentRenderers == null)
        {
            return;
        }

        foreach (Renderer contentRenderer in contentRenderers)
        {
            if (contentRenderer != null &&
                contentRenderer != exploredEnemyMarker)
            {
                contentRenderer.enabled = visible;
            }
        }
    }

    public void SetEnemyMarkerVisible(bool visible)
    {
        if (!useEnemyMarker)
        {
            return;
        }

        EnsureEnemyMarker();
        if (exploredEnemyMarker == null)
        {
            return;
        }

        if (visible && enemySourceRenderer != null)
        {
            exploredEnemyMarker.sprite = enemySourceRenderer.sprite;
            exploredEnemyMarker.flipX = enemySourceRenderer.flipX;
            exploredEnemyMarker.flipY = enemySourceRenderer.flipY;
        }

        exploredEnemyMarker.enabled = visible;
    }

    private IEnumerator RegisterWhenPlaced()
    {
        while (entity != null && !entity.IsPlaced)
        {
            yield return null;
        }

        registrationRoutine = null;
        if (entity == null || entity.CurrentMap == null)
        {
            yield break;
        }

        fogOfWarSystem = entity.CurrentMap.GetComponent<FogOfWarSystem>();
        if (fogOfWarSystem == null)
        {
            Debug.LogError(
                $"{name} could not find FogOfWarSystem on its GridMap.",
                this
            );
            yield break;
        }

        fogOfWarSystem.RegisterVisibilityTarget(this);
    }

    private void ApplyVisibility(
        CellVisibility visibility,
        bool force
    )
    {
        if (!force && CurrentVisibility == visibility)
        {
            return;
        }

        CurrentVisibility = visibility;
        if (responder != null)
        {
            responder.ApplyFogVisibility(visibility);
            return;
        }

        SetContentVisible(visibility != CellVisibility.Unexplored);
        SetEnemyMarkerVisible(false);
    }

    private void EnsureEnemyMarker()
    {
        if (!useEnemyMarker ||
            exploredEnemyMarker != null ||
            enemySourceRenderer == null)
        {
            return;
        }

        GameObject markerObject = new("ExploredEnemyMarker");
        Transform markerTransform = markerObject.transform;
        markerTransform.SetParent(enemySourceRenderer.transform.parent, false);
        markerTransform.localPosition =
            enemySourceRenderer.transform.localPosition;
        markerTransform.localRotation =
            enemySourceRenderer.transform.localRotation;
        markerTransform.localScale =
            enemySourceRenderer.transform.localScale;

        exploredEnemyMarker = markerObject.AddComponent<SpriteRenderer>();
        exploredEnemyMarker.sprite = enemySourceRenderer.sprite;
        exploredEnemyMarker.color = exploredEnemyColor;
        exploredEnemyMarker.flipX = enemySourceRenderer.flipX;
        exploredEnemyMarker.flipY = enemySourceRenderer.flipY;
        exploredEnemyMarker.sortingLayerName = markerSortingLayer;
        exploredEnemyMarker.sortingOrder = markerSortingOrder;
        exploredEnemyMarker.enabled = false;
    }
}
