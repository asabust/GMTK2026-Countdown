using System.Collections.Generic;
using Game.Experimental.Rogue.Domain;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Game.Experimental.Rogue.Unity
{
    /// <summary>
    /// Unity input adapter for the prototype. It submits commands to the
    /// domain and asks the view to redraw; it does not implement game rules.
    /// </summary>
    public sealed class RogueGameController : MonoBehaviour
    {
        private static readonly ActorId PlayerId = new("player");

        private RogueBoardView boardView;
        private RogueGameState gameState;
        private string latestMessage;

        public RogueGameState GameState => gameState;

        private void Awake()
        {
            boardView = GetComponent<RogueBoardView>();
            if (boardView == null)
            {
                boardView = gameObject.AddComponent<RogueBoardView>();
            }
        }

        private void Start()
        {
            RestartPrototype();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.rKey.wasPressedThisFrame)
            {
                RestartPrototype();
                return;
            }

            if (gameState == null || gameState.IsTerminal)
            {
                return;
            }

            if (Pressed(keyboard.upArrowKey, keyboard.wKey))
            {
                SubmitMove(0, 1);
            }
            else if (Pressed(keyboard.downArrowKey, keyboard.sKey))
            {
                SubmitMove(0, -1);
            }
            else if (Pressed(keyboard.leftArrowKey, keyboard.aKey))
            {
                SubmitMove(-1, 0);
            }
            else if (Pressed(keyboard.rightArrowKey, keyboard.dKey))
            {
                SubmitMove(1, 0);
            }
            else if (keyboard.spaceKey.wasPressedThisFrame)
            {
                SubmitWait();
            }
            else if (keyboard.gKey.wasPressedThisFrame ||
                     keyboard.eKey.wasPressedThisFrame)
            {
                SubmitPickup();
            }
            else if (keyboard.digit1Key.wasPressedThisFrame)
            {
                SubmitUseFirstItem();
            }
            else if (keyboard.enterKey.wasPressedThisFrame ||
                     keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                SubmitDescend();
            }
        }

        public void RestartPrototype()
        {
            gameState = CreatePrototypeGame();
            latestMessage =
                "Find the two enemies, clear the floor, then reach the exit.";
            Render();
        }

        public void SubmitMove(int deltaX, int deltaY)
        {
            if (!CanSubmitAction())
            {
                return;
            }

            RogueRoundResult round = gameState.ResolvePlayerMoveRound(
                new MoveAction(PlayerId, deltaX, deltaY)
            );

            if (!round.PlayerResolution.Succeeded)
            {
                latestMessage = round.PlayerResolution.FailureReason;
            }
            else if (round.PlayerMove.HasValue &&
                     round.PlayerMove.Value.MeleeAttack.HasValue)
            {
                MeleeAttackResult attack =
                    round.PlayerMove.Value.MeleeAttack.Value;
                latestMessage =
                    attack.Outcome == MeleeAttackOutcome.TargetDefeated
                        ? $"Defeated {attack.TargetId}."
                        : $"Hit {attack.TargetId} for " +
                          $"{attack.DamageDealt} damage.";
            }
            else
            {
                latestMessage = "Moved.";
            }

            AppendEnemySummary(round.EnemyTurns);
            ApplyProgressMessage(round.Progress);
            Render();
        }

        public void SubmitWait()
        {
            if (!CanSubmitAction())
            {
                return;
            }

            RogueRoundResult round = gameState.ResolvePlayerWaitRound(
                new WaitAction(PlayerId)
            );
            latestMessage = round.PlayerResolution.Succeeded
                ? "Waited one turn."
                : round.PlayerResolution.FailureReason;
            AppendEnemySummary(round.EnemyTurns);
            ApplyProgressMessage(round.Progress);
            Render();
        }

        public void SubmitPickup()
        {
            if (!CanSubmitAction() ||
                !gameState.Map.TryGetActor(PlayerId, out ActorState player))
            {
                return;
            }

            IReadOnlyList<ItemState> items =
                gameState.Map.GetItemsAt(player.Position);
            if (items.Count == 0)
            {
                latestMessage = "There is nothing here to pick up.";
                Render();
                return;
            }

            RogueActionRoundResult<PickupActionResult> round =
                gameState.ResolvePlayerPickupRound(
                    new PickupAction(PlayerId, items[0].Id)
                );
            latestMessage = round.PlayerResolution.Succeeded
                ? $"Picked up {items[0].Id}."
                : round.PlayerResolution.FailureReason;
            AppendEnemySummary(round.EnemyTurns);
            ApplyProgressMessage(round.Progress);
            Render();
        }

        public void SubmitUseFirstItem()
        {
            if (!CanSubmitAction() ||
                !gameState.Map.TryGetActor(PlayerId, out ActorState player))
            {
                return;
            }

            if (player.Inventory.Items.Count == 0)
            {
                latestMessage = "The inventory is empty.";
                Render();
                return;
            }

            ItemState item = player.Inventory.Items[0];
            RogueActionRoundResult<UseItemActionResult> round =
                gameState.ResolvePlayerUseItemRound(
                    new UseItemAction(PlayerId, item.Id)
                );
            latestMessage = round.PlayerResolution.Succeeded
                ? $"Used {item.Id}; restored " +
                  $"{round.PlayerAction.HealthRestored} HP."
                : round.PlayerResolution.FailureReason;
            AppendEnemySummary(round.EnemyTurns);
            ApplyProgressMessage(round.Progress);
            Render();
        }

        public void SubmitDescend()
        {
            if (!CanSubmitAction())
            {
                return;
            }

            RogueActionRoundResult<DescendActionResult> round =
                gameState.ResolvePlayerDescendRound(
                    new DescendAction(PlayerId)
                );
            latestMessage = round.PlayerResolution.Succeeded
                ? "Floor complete!"
                : round.PlayerResolution.FailureReason;
            ApplyProgressMessage(round.Progress);
            Render();
        }

        private bool CanSubmitAction()
        {
            if (gameState == null)
            {
                return false;
            }

            if (gameState.IsTerminal)
            {
                latestMessage = "This run has ended. Press R to restart.";
                Render();
                return false;
            }

            return true;
        }

        private void Render()
        {
            boardView.Render(gameState, latestMessage);
        }

        private void ApplyProgressMessage(RogueGameProgress progress)
        {
            switch (progress)
            {
                case RogueGameProgress.FloorCleared:
                    latestMessage += " The floor is clear; reach the exit.";
                    break;
                case RogueGameProgress.FloorCompleted:
                    latestMessage = "Floor complete! Press R to restart.";
                    break;
                case RogueGameProgress.PlayerDefeated:
                    latestMessage = "You were defeated. Press R to restart.";
                    break;
            }
        }

        private void AppendEnemySummary(
            IReadOnlyList<EnemyTurnResult> enemyTurns
        )
        {
            int attacks = 0;
            for (int index = 0; index < enemyTurns.Count; index++)
            {
                EnemyTurnResult enemyTurn = enemyTurns[index];
                if (enemyTurn.Move.HasValue &&
                    enemyTurn.Move.Value.MeleeAttack.HasValue)
                {
                    attacks++;
                }
            }

            if (attacks > 0)
            {
                latestMessage +=
                    $" {attacks} enemy attack{(attacks == 1 ? "" : "s")}.";
            }
        }

        private static bool Pressed(KeyControl first, KeyControl second) =>
            first.wasPressedThisFrame || second.wasPressedThisFrame;

        private static RogueGameState CreatePrototypeGame()
        {
            const int width = 12;
            const int height = 9;
            RogueMapState map = new(width, height);

            for (int x = 0; x < width; x++)
            {
                map.SetWalkable(new GridPosition(x, 0), false);
                map.SetWalkable(new GridPosition(x, height - 1), false);
            }

            for (int y = 1; y < height - 1; y++)
            {
                map.SetWalkable(new GridPosition(0, y), false);
                map.SetWalkable(new GridPosition(width - 1, y), false);
            }

            map.SetWalkable(new GridPosition(5, 2), false);
            map.SetWalkable(new GridPosition(5, 3), false);
            map.SetWalkable(new GridPosition(5, 5), false);
            map.SetWalkable(new GridPosition(5, 6), false);
            map.SetWalkable(new GridPosition(8, 4), false);
            map.SetWalkable(new GridPosition(9, 4), false);

            map.TryAddActor(
                new ActorState(
                    PlayerId,
                    ActorFaction.Player,
                    new GridPosition(2, 2),
                    maximumHealth: 8,
                    attackPower: 2,
                    inventoryCapacity: 3
                )
            );
            map.TryAddActor(
                new ActorState(
                    new ActorId("slime-a"),
                    ActorFaction.Enemy,
                    new GridPosition(8, 2),
                    maximumHealth: 3,
                    attackPower: 1
                )
            );
            map.TryAddActor(
                new ActorState(
                    new ActorId("slime-b"),
                    ActorFaction.Enemy,
                    new GridPosition(8, 6),
                    maximumHealth: 3,
                    attackPower: 1
                )
            );

            map.TryPlaceItem(
                new ItemState(
                    new ItemId("small-potion"),
                    ItemKind.HealingPotion,
                    effectPower: 4
                ),
                new GridPosition(3, 2)
            );
            map.SetFloorExit(new GridPosition(10, 7));

            RogueGameState game = new(
                map,
                PlayerId,
                playerSightRadius: 20
            );
            game.SynchronizeEnemyRoster();
            return game;
        }
    }
}
