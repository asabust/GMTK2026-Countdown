using UnityEngine;

public enum TerrainType
{
    None,
    Ground,
    Wall
}

public enum GridOccupantType
{
    Blocking,
    Player,
    Enemy,
    Interactable
}

public enum GridPlacementResult
{
    Success,
    NotInitialized,
    InvalidEntity,
    InvalidSize,
    EntityNotPlaced,
    AlreadyPlacedOnAnotherMap,
    OutOfBounds,
    NotWalkable,
    Occupied
}

public class GridCell
{
    public Vector2Int Position { get; }
    public TerrainType TerrainType { get; set; }
    public bool IsWalkable { get; set; }
    public GridEntity Occupant { get; internal set; }

    public GridCell(Vector2Int position)
    {
        Position = position;
        TerrainType = TerrainType.None;
        IsWalkable = false;
    }
}
