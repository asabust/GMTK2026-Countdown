using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMap : MonoBehaviour
{
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap collisionTilemap;
    [SerializeField] private bool useCollisionTilemap = true;
    [SerializeField] private Vector2Int playerStartCell;

    private GridCell[,] cells;

    private Vector3Int minCell;
    private int width;
    private int height;

    public bool IsInitialized { get; private set; }
    public Vector2Int PlayerStartCell => playerStartCell;
    public Tilemap GroundTilemap => groundTilemap;

    private void Awake()
    {
        InitializeGrid();
    }

    public void InitializeGrid()
    {
        IsInitialized = false;

        if (groundTilemap == null)
        {
            Debug.LogError("GridMap requires a Ground Tilemap.", this);
            cells = null;
            return;
        }

        BoundsInt groundBounds = groundTilemap.cellBounds;

        int minX = groundBounds.xMin;
        int minY = groundBounds.yMin;
        int maxX = groundBounds.xMax;
        int maxY = groundBounds.yMax;

        minCell = new Vector3Int(minX, minY, 0);

        width = maxX - minX;
        height = maxY - minY;

        cells = new GridCell[width, height];

        for (int arrayX = 0; arrayX < width; arrayX++)
        {
            for (int arrayY = 0; arrayY < height; arrayY++)
            {
                Vector3Int tilePosition = new Vector3Int(
                    minX + arrayX,
                    minY + arrayY,
                    0
                );

                Vector2Int gridPosition = new Vector2Int(
                    tilePosition.x,
                    tilePosition.y
                );

                GridCell cell = new GridCell(gridPosition);

                bool hasGround = groundTilemap.HasTile(tilePosition);
                bool hasCollision =
                    useCollisionTilemap &&
                    collisionTilemap != null &&
                    collisionTilemap.HasTile(tilePosition);

                if (hasCollision)
                {
                    cell.TerrainType = TerrainType.Wall;
                    cell.IsWalkable = false;
                }
                else if (hasGround)
                {
                    cell.TerrainType = TerrainType.Ground;
                    cell.IsWalkable = true;
                }

                cells[arrayX, arrayY] = cell;
            }
        }

        IsInitialized = true;
    }

    public GridCell GetCell(Vector2Int gridPosition)
    {
        if (!IsInitialized || cells == null)
        {
            return null;
        }

        int arrayX = gridPosition.x - minCell.x;
        int arrayY = gridPosition.y - minCell.y;

        if (arrayX < 0 || arrayX >= width ||
            arrayY < 0 || arrayY >= height)
        {
            return null;
        }

        return cells[arrayX, arrayY];
    }

    public IEnumerable<GridCell> GetAllCells()
    {
        if (!IsInitialized || cells == null)
        {
            yield break;
        }

        for (int arrayX = 0; arrayX < width; arrayX++)
        {
            for (int arrayY = 0; arrayY < height; arrayY++)
            {
                yield return cells[arrayX, arrayY];
            }
        }
    }

    public Vector2Int WorldToCell(Vector3 worldPosition)
    {
        Vector3Int cell = groundTilemap.WorldToCell(worldPosition);
        return new Vector2Int(cell.x, cell.y);
    }

    public Vector3 GetCellCenterWorld(Vector2Int gridPosition)
    {
        return groundTilemap.GetCellCenterWorld(
            new Vector3Int(gridPosition.x, gridPosition.y, 0)
        );
    }

    public Vector3 GetFootprintCenterWorld(Vector2Int origin, Vector2Int size)
    {
        Vector2Int oppositeCorner = origin + size - Vector2Int.one;
        Vector3 first = GetCellCenterWorld(origin);
        Vector3 last = GetCellCenterWorld(oppositeCorner);
        return (first + last) * 0.5f;
    }

    public GridPlacementResult CanOccupy(GridEntity entity, Vector2Int origin)
    {
        if (!IsInitialized)
        {
            return GridPlacementResult.NotInitialized;
        }

        if (entity == null)
        {
            return GridPlacementResult.InvalidEntity;
        }

        if (entity.Size.x < 1 || entity.Size.y < 1)
        {
            return GridPlacementResult.InvalidSize;
        }

        foreach (Vector2Int position in entity.GetOccupiedCells(origin))
        {
            GridCell cell = GetCell(position);

            if (cell == null)
            {
                return GridPlacementResult.OutOfBounds;
            }

            if (!cell.IsWalkable)
            {
                return GridPlacementResult.NotWalkable;
            }

            if (cell.Occupant != null && cell.Occupant != entity)
            {
                return GridPlacementResult.Occupied;
            }
        }

        return GridPlacementResult.Success;
    }

    public bool TryPlaceEntity(
        GridEntity entity,
        Vector2Int origin,
        out GridPlacementResult result
    )
    {
        if (entity != null && entity.IsPlaced && entity.CurrentMap != this)
        {
            result = GridPlacementResult.AlreadyPlacedOnAnotherMap;
            return false;
        }

        result = CanOccupy(entity, origin);
        if (result != GridPlacementResult.Success)
        {
            return false;
        }

        if (entity.IsPlaced)
        {
            ClearOccupiedCells(entity);
        }

        entity.PlaceOnMap(this, origin);
        SetOccupiedCells(entity);
        return true;
    }

    public bool TryMoveEntity(
        GridEntity entity,
        Vector2Int destination,
        out GridPlacementResult result
    )
    {
        if (entity == null)
        {
            result = GridPlacementResult.InvalidEntity;
            return false;
        }

        if (!entity.IsPlaced || entity.CurrentMap != this)
        {
            result = GridPlacementResult.EntityNotPlaced;
            return false;
        }

        result = CanOccupy(entity, destination);
        if (result != GridPlacementResult.Success)
        {
            return false;
        }

        ClearOccupiedCells(entity);
        entity.PlaceOnMap(this, destination);
        SetOccupiedCells(entity);
        return true;
    }

    public void RemoveEntity(GridEntity entity)
    {
        if (entity == null || entity.CurrentMap != this)
        {
            return;
        }

        ClearOccupiedCells(entity);
        entity.RemoveFromMap(this);
    }

    private void SetOccupiedCells(GridEntity entity)
    {
        foreach (Vector2Int position in entity.GetOccupiedCells())
        {
            GetCell(position).Occupant = entity;
        }
    }

    private void ClearOccupiedCells(GridEntity entity)
    {
        foreach (Vector2Int position in entity.GetOccupiedCells())
        {
            GridCell cell = GetCell(position);

            if (cell != null && cell.Occupant == entity)
            {
                cell.Occupant = null;
            }
        }
    }
}
