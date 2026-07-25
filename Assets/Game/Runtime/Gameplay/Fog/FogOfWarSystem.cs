using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(GridMap))]
public class FogOfWarSystem : MonoBehaviour
{
    [SerializeField, Min(0)] private int visionRadius = 1;
    [SerializeField] private Color unexploredColor =
        new(0.025f, 0.03f, 0.045f, 1f);
    [SerializeField] private Sprite[] fogSprites;
    [SerializeField] private string fogSortingLayer = "Fog";
    [SerializeField] private int fogSortingOrder;

    private GridMap gridMap;
    private PlayerGridController playerController;
    private Tilemap fogTilemap;
    private Tile unexploredTile;
    private Tile[] unexploredArtTiles;
    private Texture2D fogTexture;
    private Sprite fogSprite;
    private bool isInitialized;
    private readonly HashSet<FogVisibilityTarget> visibilityTargets = new();

    public int VisionRadius => visionRadius;

    public event Action<GridCell, CellVisibility, CellVisibility>
        CellVisibilityChanged;
    public event Action VisionRefreshed;

    private void Awake()
    {
        gridMap = GetComponent<GridMap>();
    }

    private void OnEnable()
    {
        if (fogTilemap != null)
        {
            BindPlayer(FindObjectOfType<PlayerGridController>());
        }
    }

    private void Start()
    {
        CreateOverlay();
        ResetAllCells();
        BindPlayer(FindObjectOfType<PlayerGridController>());
    }

    private void OnDisable()
    {
        UnbindPlayer();
    }

    private void OnDestroy()
    {
        DestroyRuntimeAsset(unexploredTile);
        DestroyRuntimeTiles(unexploredArtTiles);
        DestroyRuntimeAsset(fogSprite);
        DestroyRuntimeAsset(fogTexture);
    }

    public void RefreshVision(Vector2Int playerPosition)
    {
        if (gridMap == null || !gridMap.IsInitialized)
        {
            return;
        }

        foreach (GridCell cell in gridMap.GetAllCells())
        {
            if (cell.Visibility == CellVisibility.Visible)
            {
                SetCellVisibility(cell, CellVisibility.Explored);
            }
        }

        for (int offsetX = -visionRadius;
             offsetX <= visionRadius;
             offsetX++)
        {
            for (int offsetY = -visionRadius;
                 offsetY <= visionRadius;
                 offsetY++)
            {
                GridCell cell = gridMap.GetCell(
                    playerPosition + new Vector2Int(offsetX, offsetY)
                );
                if (cell != null)
                {
                    SetCellVisibility(cell, CellVisibility.Visible);
                }
            }
        }

        isInitialized = true;
        RefreshVisibilityTargets();
        VisionRefreshed?.Invoke();
    }

    public void ResetVisibility()
    {
        ResetAllCells();
        isInitialized = false;

        if (playerController != null &&
            playerController.IsPlaced &&
            playerController.CurrentMap == gridMap)
        {
            RefreshVision(playerController.GridPosition);
        }
    }

    public void RegisterVisibilityTarget(FogVisibilityTarget target)
    {
        if (target == null || !visibilityTargets.Add(target))
        {
            return;
        }

        RefreshVisibilityTarget(target);
    }

    public void UnregisterVisibilityTarget(FogVisibilityTarget target)
    {
        if (target != null)
        {
            RestoreTargetFootprintFog(target);
            visibilityTargets.Remove(target);
        }
    }

    private void BindPlayer(PlayerGridController player)
    {
        if (playerController == player)
        {
            TryInitializeFromPlayer();
            return;
        }

        UnbindPlayer();
        playerController = player;
        if (playerController == null)
        {
            Debug.LogError(
                "FogOfWarSystem could not find PlayerGridController.",
                this
            );
            return;
        }

        playerController.GridBound += HandleGridBound;
        playerController.GridUnbound += HandleGridUnbound;
        playerController.MoveCompleted += HandleMoveCompleted;
        TryInitializeFromPlayer();
    }

    private void UnbindPlayer()
    {
        if (playerController == null)
        {
            return;
        }

        playerController.GridBound -= HandleGridBound;
        playerController.GridUnbound -= HandleGridUnbound;
        playerController.MoveCompleted -= HandleMoveCompleted;
        playerController = null;
    }

    private void HandleGridBound(GridMap boundMap)
    {
        if (boundMap == gridMap)
        {
            TryInitializeFromPlayer();
        }
    }

    private void HandleGridUnbound(GridMap unboundMap)
    {
        if (unboundMap == gridMap)
        {
            isInitialized = false;
        }
    }

    private void HandleMoveCompleted(
        Vector2Int origin,
        Vector2Int destination
    )
    {
        if (playerController != null &&
            playerController.CurrentMap == gridMap)
        {
            RefreshVision(destination);
        }
    }

    private void TryInitializeFromPlayer()
    {
        if (isInitialized ||
            playerController == null ||
            !playerController.IsPlaced ||
            playerController.CurrentMap != gridMap)
        {
            return;
        }

        RefreshVision(playerController.GridPosition);
    }

    private void ResetAllCells()
    {
        if (gridMap == null || !gridMap.IsInitialized)
        {
            return;
        }

        foreach (GridCell cell in gridMap.GetAllCells())
        {
            CellVisibility previous = cell.Visibility;
            cell.Visibility = CellVisibility.Unexplored;
            UpdateCellVisual(cell);

            if (previous != CellVisibility.Unexplored)
            {
                CellVisibilityChanged?.Invoke(
                    cell,
                    previous,
                    CellVisibility.Unexplored
                );
            }
        }
    }

    private void SetCellVisibility(
        GridCell cell,
        CellVisibility visibility
    )
    {
        if (cell == null || cell.Visibility == visibility)
        {
            return;
        }

        CellVisibility previous = cell.Visibility;
        cell.Visibility = visibility;
        UpdateCellVisual(cell);
        CellVisibilityChanged?.Invoke(cell, previous, visibility);
    }

    private void RefreshVisibilityTargets()
    {
        visibilityTargets.RemoveWhere(target => target == null);
        foreach (GridCell cell in gridMap.GetAllCells())
        {
            UpdateCellVisual(cell);
        }

        foreach (FogVisibilityTarget target in visibilityTargets)
        {
            RefreshVisibilityTarget(target);
        }
    }

    private void RefreshVisibilityTarget(FogVisibilityTarget target)
    {
        GridEntity entity = target != null ? target.Entity : null;
        if (entity == null ||
            !entity.IsPlaced ||
            entity.CurrentMap != gridMap)
        {
            target?.ApplyVisibility(CellVisibility.Unexplored);
            return;
        }

        CellVisibility resolvedVisibility = CellVisibility.Unexplored;
        foreach (Vector2Int position in entity.GetOccupiedCells())
        {
            GridCell cell = gridMap.GetCell(position);
            if (cell == null)
            {
                continue;
            }

            if (cell.Visibility == CellVisibility.Visible)
            {
                resolvedVisibility = CellVisibility.Visible;
                break;
            }

            if (cell.Visibility == CellVisibility.Explored)
            {
                resolvedVisibility = CellVisibility.Explored;
            }
        }

        target.ApplyVisibility(resolvedVisibility);
        if (resolvedVisibility == CellVisibility.Visible &&
            (entity.Size.x > 1 || entity.Size.y > 1))
        {
            SuppressFogForTargetFootprint(entity);
        }
    }

    private void SuppressFogForTargetFootprint(GridEntity entity)
    {
        if (fogTilemap == null || entity == null)
        {
            return;
        }

        foreach (Vector2Int position in entity.GetOccupiedCells())
        {
            fogTilemap.SetTile(
                new Vector3Int(position.x, position.y, 0),
                null
            );
        }
    }

    private void RestoreTargetFootprintFog(FogVisibilityTarget target)
    {
        GridEntity entity = target != null ? target.Entity : null;
        if (gridMap == null || entity == null)
        {
            return;
        }

        foreach (Vector2Int position in entity.GetOccupiedCells())
        {
            UpdateCellVisual(gridMap.GetCell(position));
        }
    }

    private void UpdateCellVisual(GridCell cell)
    {
        if (fogTilemap == null || cell == null)
        {
            return;
        }

        Vector3Int tilePosition =
            new(cell.Position.x, cell.Position.y, 0);
        TileBase tile = cell.Visibility switch
        {
            CellVisibility.Unexplored =>
                GetFogTile(unexploredArtTiles, unexploredTile, cell.Position),
            CellVisibility.Explored => null,
            _ => null
        };
        fogTilemap.SetTile(tilePosition, tile);
    }

    private void CreateOverlay()
    {
        if (gridMap == null ||
            !gridMap.IsInitialized ||
            gridMap.GroundTilemap == null)
        {
            Debug.LogError(
                "FogOfWarSystem requires an initialized GridMap.",
                this
            );
            return;
        }

        Transform overlayParent = gridMap.GroundTilemap.transform.parent;
        GameObject overlayObject = new(
            "FogOverlay",
            typeof(Tilemap),
            typeof(TilemapRenderer)
        );
        overlayObject.transform.SetParent(overlayParent, false);

        fogTilemap = overlayObject.GetComponent<Tilemap>();
        TilemapRenderer tilemapRenderer =
            overlayObject.GetComponent<TilemapRenderer>();
        tilemapRenderer.sortingLayerName = fogSortingLayer;
        tilemapRenderer.sortingOrder = fogSortingOrder;

        fogTexture = new Texture2D(
            1,
            1,
            TextureFormat.RGBA32,
            false
        )
        {
            name = "Runtime Fog Texture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };
        fogTexture.SetPixel(0, 0, Color.white);
        fogTexture.Apply();

        fogSprite = Sprite.Create(
            fogTexture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );
        fogSprite.name = "Runtime Fog Sprite";
        fogSprite.hideFlags = HideFlags.HideAndDontSave;

        if (fogSprites != null && fogSprites.Length > 0)
        {
            unexploredArtTiles = CreateRuntimeTiles(
                "Unexplored Fog Art",
                new Color(1f, 1f, 1f, unexploredColor.a)
            );
        }
        else
        {
            unexploredTile = CreateRuntimeTile(
                "Unexplored Fog Tile",
                fogSprite,
                unexploredColor
            );
        }

    }

    private Tile[] CreateRuntimeTiles(string prefix, Color color)
    {
        Tile[] tiles = new Tile[fogSprites.Length];
        for (int i = 0; i < fogSprites.Length; i++)
        {
            tiles[i] = CreateRuntimeTile(
                $"{prefix} {i + 1}",
                fogSprites[i],
                color
            );
        }
        return tiles;
    }

    private static TileBase GetFogTile(
        Tile[] variants,
        Tile fallback,
        Vector2Int position
    )
    {
        if (variants == null || variants.Length == 0)
        {
            return fallback;
        }

        unchecked
        {
            int hash = position.x * 73856093 ^ position.y * 19349663;
            int index = (int)((uint)hash % (uint)variants.Length);
            return variants[index] != null ? variants[index] : fallback;
        }
    }

    private Tile CreateRuntimeTile(
        string tileName,
        Sprite sprite,
        Color color
    )
    {
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = tileName;
        tile.sprite = sprite;
        tile.color = color;
        tile.colliderType = Tile.ColliderType.None;
        tile.hideFlags = HideFlags.HideAndDontSave;
        return tile;
    }

    private static void DestroyRuntimeTiles(Tile[] tiles)
    {
        if (tiles == null)
        {
            return;
        }

        foreach (Tile tile in tiles)
        {
            DestroyRuntimeAsset(tile);
        }
    }

    private static void DestroyRuntimeAsset(UnityEngine.Object asset)
    {
        if (asset == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(asset);
        }
        else
        {
            DestroyImmediate(asset);
        }
    }

    private void OnValidate()
    {
        visionRadius = Mathf.Max(0, visionRadius);

        if (!Application.isPlaying)
        {
            return;
        }

        if (unexploredArtTiles != null)
        {
            foreach (Tile tile in unexploredArtTiles)
            {
                if (tile != null)
                    tile.color =
                        new Color(1f, 1f, 1f, unexploredColor.a);
            }
        }
        else if (unexploredTile != null)
        {
            unexploredTile.color = unexploredColor;
        }

        if (playerController != null &&
            playerController.IsPlaced &&
            playerController.CurrentMap == gridMap)
        {
            RefreshVision(playerController.GridPosition);
        }
    }
}
