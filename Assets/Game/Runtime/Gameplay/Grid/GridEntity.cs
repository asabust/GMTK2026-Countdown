using System.Collections.Generic;
using UnityEngine;

public abstract class GridEntity : MonoBehaviour
{
    [SerializeField] private Vector2Int size = Vector2Int.one;

    public Vector2Int GridPosition { get; private set; }
    public Vector2Int Size => size;
    public GridMap CurrentMap { get; private set; }
    public bool IsPlaced => CurrentMap != null;
    public virtual GridOccupantType OccupantType => GridOccupantType.Blocking;

    public IEnumerable<Vector2Int> GetOccupiedCells()
    {
        return GetOccupiedCells(GridPosition);
    }

    public IEnumerable<Vector2Int> GetOccupiedCells(Vector2Int origin)
    {
        for (int x = 0; x < Size.x; x++)
        {
            for (int y = 0; y < Size.y; y++)
            {
                yield return origin + new Vector2Int(x, y);
            }
        }
    }

    internal void PlaceOnMap(GridMap map, Vector2Int position)
    {
        CurrentMap = map;
        GridPosition = position;
    }

    internal void RemoveFromMap(GridMap map)
    {
        if (CurrentMap != map)
        {
            return;
        }

        CurrentMap = null;
    }

    protected virtual void OnValidate()
    {
        size.x = Mathf.Max(1, size.x);
        size.y = Mathf.Max(1, size.y);
    }
}
