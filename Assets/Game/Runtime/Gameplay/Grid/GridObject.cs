using UnityEngine;

public class GridObject : GridEntity
{
    [SerializeField] private GridOccupantType occupantType = GridOccupantType.Enemy;
    [SerializeField] private bool snapToCellCenter = true;

    public override GridOccupantType OccupantType => occupantType;

    private void Start()
    {
        GridMap map = FindGridMapInScene();
        if (map == null)
        {
            Debug.LogError($"No GridMap found for {name}.", this);
            return;
        }

        Vector2Int origin = map.WorldToCell(transform.position);
        if (!map.TryPlaceEntity(this, origin, out GridPlacementResult result))
        {
            Debug.LogError(
                $"Could not place {name} at {origin}: {result}.",
                this
            );
            return;
        }

        if (snapToCellCenter)
        {
            transform.position = map.GetFootprintCenterWorld(origin, Size);
        }

    }

    public void ReleaseAndDestroy()
    {
        if (CurrentMap != null)
        {
            CurrentMap.RemoveEntity(this);
        }

        Destroy(gameObject);
    }

    private GridMap FindGridMapInScene()
    {
        foreach (GameObject root in gameObject.scene.GetRootGameObjects())
        {
            GridMap map = root.GetComponentInChildren<GridMap>(true);
            if (map != null)
            {
                return map;
            }
        }

        return null;
    }

    private void OnDestroy()
    {
        if (CurrentMap != null)
        {
            CurrentMap.RemoveEntity(this);
        }
    }
}
