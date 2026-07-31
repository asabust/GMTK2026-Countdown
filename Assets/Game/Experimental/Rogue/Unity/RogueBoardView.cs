using Game.Experimental.Rogue.Domain;
using UnityEngine;

namespace Game.Experimental.Rogue.Unity
{
    /// <summary>
    /// Disposable graybox renderer. It rebuilds a tiny board from the current
    /// domain state so no Unity object becomes authoritative game state.
    /// </summary>
    public sealed class RogueBoardView : MonoBehaviour
    {
        private static readonly Color UnseenColor =
            new(0.02f, 0.025f, 0.035f);
        private static readonly Color WallColor =
            new(0.12f, 0.15f, 0.19f);
        private static readonly Color FloorColor =
            new(0.24f, 0.28f, 0.32f);
        private static readonly Color ExitColor =
            new(0.95f, 0.72f, 0.2f);
        private static readonly Color ItemColor =
            new(0.25f, 0.85f, 0.75f);
        private static readonly Color PlayerColor =
            new(0.3f, 0.7f, 1f);
        private static readonly Color EnemyColor =
            new(0.95f, 0.3f, 0.35f);

        [SerializeField, Min(0.1f)] private float tileSize = 1f;

        private Transform boardRoot;
        private RogueGameState displayedGame;
        private string displayedMessage;
        private Sprite squareSprite;
        private Texture2D squareTexture;

        public void Render(RogueGameState game, string message)
        {
            displayedGame = game;
            displayedMessage = message;
            ClearBoard();

            if (game == null)
            {
                return;
            }

            EnsureSprite();
            boardRoot = new GameObject("Rendered Board").transform;
            boardRoot.SetParent(transform, false);

            RogueMapState map = game.Map;
            for (int x = 0; x < map.Width; x++)
            {
                for (int y = 0; y < map.Height; y++)
                {
                    GridPosition position = new(x, y);
                    bool explored =
                        game.Visibility.IsExplored(position);
                    Color color = !explored
                        ? UnseenColor
                        : map.IsWalkable(position)
                            ? FloorColor
                            : WallColor;
                    CreateSquare(
                        $"Terrain {x},{y}",
                        position,
                        color,
                        sortingOrder: 0,
                        scale: 0.96f
                    );
                }
            }

            if (map.FloorExit.HasValue &&
                game.Visibility.IsExplored(map.FloorExit.Value))
            {
                CreateSquare(
                    "Exit",
                    map.FloorExit.Value,
                    ExitColor,
                    sortingOrder: 1,
                    scale: 0.58f
                );
            }

            foreach (GroundItemState groundItem in map.GroundItems)
            {
                if (game.Visibility.IsVisible(groundItem.Position))
                {
                    CreateSquare(
                        $"Item {groundItem.Item.Id}",
                        groundItem.Position,
                        ItemColor,
                        sortingOrder: 2,
                        scale: 0.32f
                    );
                }
            }

            foreach (ActorState actor in map.Actors)
            {
                if (!game.Visibility.IsVisible(actor.Position))
                {
                    continue;
                }

                CreateSquare(
                    actor.Id.ToString(),
                    actor.Position,
                    actor.Faction == ActorFaction.Player
                        ? PlayerColor
                        : EnemyColor,
                    sortingOrder: 3,
                    scale: actor.Faction == ActorFaction.Player
                        ? 0.72f
                        : 0.66f
                );
            }

            FrameCamera(map);
        }

        private void OnGUI()
        {
            if (displayedGame == null)
            {
                return;
            }

            const float panelWidth = 450f;
            const float panelHeight = 150f;
            Rect panel = new(18f, 18f, panelWidth, panelHeight);
            GUI.Box(panel, "Rogue Domain Graybox");

            string status = "Player unavailable";
            if (displayedGame.Map.TryGetActor(
                    displayedGame.Turns.PlayerId,
                    out ActorState player
                ))
            {
                status =
                    $"HP {player.CurrentHealth}/{player.MaximumHealth}    " +
                    $"Inventory {player.Inventory.Items.Count}/" +
                    $"{player.Inventory.Capacity}    " +
                    $"Enemies: {CountEnemies(displayedGame.Map)}";
            }

            GUI.Label(
                new Rect(34f, 48f, panelWidth - 32f, 24f),
                status
            );
            GUI.Label(
                new Rect(34f, 72f, panelWidth - 32f, 24f),
                "Move: WASD / arrows   Wait: Space   Pickup: E/G"
            );
            GUI.Label(
                new Rect(34f, 94f, panelWidth - 32f, 24f),
                "Use potion: 1   Descend: Enter   Restart: R"
            );
            GUI.Label(
                new Rect(34f, 120f, panelWidth - 32f, 38f),
                displayedMessage ?? string.Empty
            );

            DrawLegend(Screen.width - 220f, 18f);
        }

        private void CreateSquare(
            string objectName,
            GridPosition position,
            Color color,
            int sortingOrder,
            float scale
        )
        {
            GameObject square = new(objectName);
            square.transform.SetParent(boardRoot, false);
            square.transform.localPosition = new Vector3(
                position.X * tileSize,
                position.Y * tileSize,
                0f
            );
            square.transform.localScale =
                Vector3.one * tileSize * scale;

            SpriteRenderer renderer =
                square.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        private void FrameCamera(RogueMapState map)
        {
            Camera camera = Camera.main;
            if (camera == null)
            {
                GameObject cameraObject = new("Main Camera");
                cameraObject.tag = "MainCamera";
                camera = cameraObject.AddComponent<Camera>();
            }

            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.035f, 0.045f, 0.06f);
            camera.transform.position = new Vector3(
                (map.Width - 1) * tileSize * 0.5f,
                (map.Height - 1) * tileSize * 0.5f,
                -10f
            );
            camera.orthographicSize =
                Mathf.Max(5.2f, map.Height * tileSize * 0.62f);
        }

        private void EnsureSprite()
        {
            if (squareSprite != null)
            {
                return;
            }

            squareTexture = new Texture2D(
                1,
                1,
                TextureFormat.RGBA32,
                mipChain: false
            )
            {
                name = "Rogue Graybox Pixel",
                filterMode = FilterMode.Point,
                hideFlags = HideFlags.HideAndDontSave
            };
            squareTexture.SetPixel(0, 0, Color.white);
            squareTexture.Apply();
            squareSprite = Sprite.Create(
                squareTexture,
                new Rect(0f, 0f, 1f, 1f),
                new Vector2(0.5f, 0.5f),
                pixelsPerUnit: 1f
            );
            squareSprite.name = "Rogue Graybox Square";
            squareSprite.hideFlags = HideFlags.HideAndDontSave;
        }

        private void ClearBoard()
        {
            if (boardRoot != null)
            {
                Destroy(boardRoot.gameObject);
                boardRoot = null;
            }
        }

        private void OnDestroy()
        {
            if (squareSprite != null)
            {
                Destroy(squareSprite);
            }

            if (squareTexture != null)
            {
                Destroy(squareTexture);
            }
        }

        private static int CountEnemies(RogueMapState map)
        {
            int count = 0;
            foreach (ActorState actor in map.Actors)
            {
                if (actor.Faction == ActorFaction.Enemy)
                {
                    count++;
                }
            }

            return count;
        }

        private static void DrawLegend(float x, float y)
        {
            Rect area = new(x, y, 200f, 128f);
            GUI.Box(area, "Legend");
            DrawLegendRow(x + 16f, y + 32f, PlayerColor, "Player");
            DrawLegendRow(x + 16f, y + 54f, EnemyColor, "Enemy");
            DrawLegendRow(x + 16f, y + 76f, ItemColor, "Potion");
            DrawLegendRow(x + 16f, y + 98f, ExitColor, "Exit");
        }

        private static void DrawLegendRow(
            float x,
            float y,
            Color color,
            string label
        )
        {
            Color previous = GUI.color;
            GUI.color = color;
            GUI.Box(new Rect(x, y, 16f, 16f), GUIContent.none);
            GUI.color = previous;
            GUI.Label(new Rect(x + 24f, y - 2f, 130f, 20f), label);
        }
    }
}
