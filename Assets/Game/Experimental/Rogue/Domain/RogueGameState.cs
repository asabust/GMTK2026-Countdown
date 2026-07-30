using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Experimental.Rogue.Domain
{
    public readonly struct RogueMoveResult
    {
        public RogueMoveResult(
            MoveActionResult move,
            MeleeAttackResult? meleeAttack = null
        )
        {
            Move = move;
            MeleeAttack = meleeAttack;
        }

        public MoveActionResult Move { get; }
        public MeleeAttackResult? MeleeAttack { get; }
        public ActionResolution Resolution =>
            MeleeAttack?.Resolution ?? Move.Resolution;
    }

    /// <summary>
    /// Coordinates map rules, combat resolution, and deterministic turns.
    /// Presentation code submits actions here instead of advancing the
    /// scheduler separately.
    /// </summary>
    public sealed class RogueGameState
    {
        public RogueGameState(
            RogueMapState map,
            ActorId playerId,
            int playerSightRadius = 0
        )
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));

            if (!map.TryGetActor(playerId, out ActorState player) ||
                player.Faction != ActorFaction.Player)
            {
                throw new ArgumentException(
                    "The player must exist on the map.",
                    nameof(playerId)
                );
            }

            if (playerSightRadius < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerSightRadius)
                );
            }

            Turns = new TurnScheduler(playerId);
            PlayerSightRadius = playerSightRadius;
            Visibility = new VisibilityState();
            RefreshVisibility();
        }

        public RogueMapState Map { get; }
        public TurnScheduler Turns { get; }
        public VisibilityState Visibility { get; }
        public int PlayerSightRadius { get; }
        public bool IsFloorCompleted { get; private set; }
        public bool IsPlayerDefeated =>
            !Map.TryGetActor(Turns.PlayerId, out _);
        public RogueGameProgress Progress =>
            IsPlayerDefeated
                ? RogueGameProgress.PlayerDefeated
                : IsFloorCompleted
                    ? RogueGameProgress.FloorCompleted
                : !Map.HasEnemies
                    ? RogueGameProgress.FloorCleared
                    : RogueGameProgress.Ongoing;

        public bool RegisterEnemy(ActorId enemyId)
        {
            if (!Map.TryGetActor(enemyId, out ActorState enemy) ||
                enemy.Faction != ActorFaction.Enemy)
            {
                return false;
            }

            return Turns.RegisterEnemy(enemyId);
        }

        public RogueMoveResult ResolveMove(MoveAction action)
        {
            if (action.ActorId != Turns.CurrentActor)
            {
                GridPosition position = default;
                if (Map.TryGetActor(action.ActorId, out ActorState actor))
                {
                    position = actor.Position;
                }

                return new RogueMoveResult(new MoveActionResult(
                    MoveOutcome.NotActorsTurn,
                    ActionResolution.Rejected(
                        $"It is {Turns.CurrentActor}'s turn."
                    ),
                    position,
                    position
                ));
            }

            MoveActionResult move = MoveActionResolver.Resolve(Map, action);
            MeleeAttackResult? melee = null;
            ActionResolution resolution = move.Resolution;

            if (move.Outcome == MoveOutcome.MeleeAttack)
            {
                MeleeAttackResult attack = MeleeAttackResolver.Resolve(
                    Map,
                    new MeleeAttackAction(
                        action.ActorId,
                        move.TargetActorId
                    )
                );
                melee = attack;
                resolution = attack.Resolution;

                if (attack.Outcome ==
                    MeleeAttackOutcome.TargetDefeated)
                {
                    Turns.UnregisterEnemy(attack.TargetId);
                }
            }

            Turns.CompleteAction(action.ActorId, resolution);
            if (action.ActorId == Turns.PlayerId)
            {
                RefreshVisibility();
            }

            return new RogueMoveResult(move, melee);
        }

        public void RefreshVisibility()
        {
            if (Map.TryGetActor(
                    Turns.PlayerId,
                    out ActorState player
                ))
            {
                Visibility.Recalculate(
                    Map,
                    player.Position,
                    PlayerSightRadius
                );
            }
        }

        public RogueGameSnapshot CreateSnapshot()
        {
            if (Turns.Phase != TurnPhase.AwaitingPlayer ||
                Turns.CurrentActor != Turns.PlayerId)
            {
                throw new InvalidOperationException(
                    "Snapshots require a stable player turn."
                );
            }

            bool[] walkable = new bool[Map.Width * Map.Height];
            for (int y = 0; y < Map.Height; y++)
            {
                for (int x = 0; x < Map.Width; x++)
                {
                    walkable[y * Map.Width + x] =
                        Map.IsWalkable(new GridPosition(x, y));
                }
            }

            ActorSnapshot[] actors = Map.Actors
                .Select(actor => new ActorSnapshot
                {
                    Id = actor.Id.Value,
                    Faction = actor.Faction,
                    Position = ToSnapshot(actor.Position),
                    MaximumHealth = actor.MaximumHealth,
                    CurrentHealth = actor.CurrentHealth,
                    AttackPower = actor.AttackPower,
                    InventoryCapacity = actor.Inventory.Capacity,
                    Inventory = actor.Inventory.Items
                        .Select(ToSnapshot)
                        .ToArray()
                })
                .ToArray();

            return new RogueGameSnapshot
            {
                Width = Map.Width,
                Height = Map.Height,
                Walkable = walkable,
                PlayerId = Turns.PlayerId.Value,
                Actors = actors,
                EnemyIds = Turns.Enemies
                    .Select(enemy => enemy.Value)
                    .ToArray(),
                GroundItems = Map.GroundItems
                    .Select(groundItem => new GroundItemSnapshot
                    {
                        Item = ToSnapshot(groundItem.Item),
                        Position = ToSnapshot(groundItem.Position)
                    })
                    .ToArray(),
                HasFloorExit = Map.FloorExit.HasValue,
                FloorExit = Map.FloorExit.HasValue
                    ? ToSnapshot(Map.FloorExit.Value)
                    : null,
                IsFloorCompleted = IsFloorCompleted,
                RoundNumber = Turns.RoundNumber,
                PlayerSightRadius = PlayerSightRadius,
                Explored = Visibility.Explored
                    .Select(ToSnapshot)
                    .ToArray()
            };
        }

        public static RogueGameState FromSnapshot(
            RogueGameSnapshot snapshot
        )
        {
            if (snapshot == null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            if (snapshot.Width <= 0 ||
                snapshot.Height <= 0 ||
                snapshot.Walkable == null ||
                snapshot.Walkable.Length !=
                snapshot.Width * snapshot.Height)
            {
                throw new ArgumentException(
                    "Snapshot map data is invalid.",
                    nameof(snapshot)
                );
            }

            RogueMapState map = new(snapshot.Width, snapshot.Height);
            for (int y = 0; y < snapshot.Height; y++)
            {
                for (int x = 0; x < snapshot.Width; x++)
                {
                    if (!snapshot.Walkable[y * snapshot.Width + x])
                    {
                        map.SetWalkable(
                            new GridPosition(x, y),
                            false
                        );
                    }
                }
            }

            foreach (ActorSnapshot actorData in
                     snapshot.Actors ?? Array.Empty<ActorSnapshot>())
            {
                ActorState actor = new(
                    new ActorId(actorData.Id),
                    actorData.Faction,
                    FromSnapshot(actorData.Position),
                    actorData.MaximumHealth,
                    actorData.AttackPower,
                    actorData.InventoryCapacity,
                    actorData.CurrentHealth
                );
                foreach (ItemSnapshot itemData in
                         actorData.Inventory ??
                         Array.Empty<ItemSnapshot>())
                {
                    if (!actor.Inventory.TryAdd(
                            FromSnapshot(itemData)
                        ))
                    {
                        throw new ArgumentException(
                            "Snapshot inventory data is invalid.",
                            nameof(snapshot)
                        );
                    }
                }

                if (!map.TryAddActor(actor))
                {
                    throw new ArgumentException(
                        "Snapshot actor data is invalid.",
                        nameof(snapshot)
                    );
                }
            }

            foreach (GroundItemSnapshot groundItem in
                     snapshot.GroundItems ??
                     Array.Empty<GroundItemSnapshot>())
            {
                if (!map.TryPlaceItem(
                        FromSnapshot(groundItem.Item),
                        FromSnapshot(groundItem.Position)
                    ))
                {
                    throw new ArgumentException(
                        "Snapshot ground item data is invalid.",
                        nameof(snapshot)
                    );
                }
            }

            if (snapshot.HasFloorExit)
            {
                map.SetFloorExit(FromSnapshot(snapshot.FloorExit));
            }

            RogueGameState game = new(
                map,
                new ActorId(snapshot.PlayerId),
                snapshot.PlayerSightRadius
            );
            foreach (string enemyId in
                     snapshot.EnemyIds ?? Array.Empty<string>())
            {
                if (!game.RegisterEnemy(new ActorId(enemyId)))
                {
                    throw new ArgumentException(
                        "Snapshot enemy data is invalid.",
                        nameof(snapshot)
                    );
                }
            }

            game.Turns.RestoreStableRound(snapshot.RoundNumber);
            game.IsFloorCompleted = snapshot.IsFloorCompleted;
            game.Visibility.RestoreExplored(
                (snapshot.Explored ??
                 Array.Empty<GridPositionSnapshot>())
                .Select(FromSnapshot)
            );
            game.RefreshVisibility();
            return game;
        }

        private static GridPositionSnapshot ToSnapshot(
            GridPosition position
        ) => new()
        {
            X = position.X,
            Y = position.Y
        };

        private static ItemSnapshot ToSnapshot(ItemState item) =>
            new()
            {
                Id = item.Id.Value,
                Kind = item.Kind,
                EffectPower = item.EffectPower
            };

        private static GridPosition FromSnapshot(
            GridPositionSnapshot position
        )
        {
            if (position == null)
            {
                throw new ArgumentException(
                    "Snapshot position is missing."
                );
            }

            return new GridPosition(position.X, position.Y);
        }

        private static ItemState FromSnapshot(ItemSnapshot item)
        {
            if (item == null)
            {
                throw new ArgumentException(
                    "Snapshot item is missing."
                );
            }

            return new ItemState(
                new ItemId(item.Id),
                item.Kind,
                item.EffectPower
            );
        }

        public WaitActionResult ResolveWait(WaitAction action)
        {
            if (action.ActorId != Turns.CurrentActor)
            {
                return new WaitActionResult(
                    WaitOutcome.NotActorsTurn,
                    ActionResolution.Rejected(
                        $"It is {Turns.CurrentActor}'s turn."
                    ),
                    action.ActorId
                );
            }

            if (!Map.TryGetActor(action.ActorId, out _))
            {
                return new WaitActionResult(
                    WaitOutcome.InvalidActor,
                    ActionResolution.Rejected(
                        $"Actor '{action.ActorId}' is not on the map."
                    ),
                    action.ActorId
                );
            }

            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            Turns.CompleteAction(action.ActorId, resolution);
            return new WaitActionResult(
                WaitOutcome.Waited,
                resolution,
                action.ActorId
            );
        }

        public PickupActionResult ResolvePickup(PickupAction action)
        {
            if (!TryValidateInventoryActor(
                    action.ActorId,
                    out ActorState actor,
                    out ActionResolution rejection
                ))
            {
                PickupOutcome outcome =
                    action.ActorId == Turns.CurrentActor
                        ? PickupOutcome.InvalidActor
                        : PickupOutcome.NotActorsTurn;
                return new PickupActionResult(
                    outcome,
                    rejection,
                    action.ActorId,
                    action.ItemId
                );
            }

            IReadOnlyList<ItemState> groundItems =
                Map.GetItemsAt(actor.Position);
            ItemState item = null;
            for (int index = 0; index < groundItems.Count; index++)
            {
                if (groundItems[index].Id == action.ItemId)
                {
                    item = groundItems[index];
                    break;
                }
            }

            if (item == null)
            {
                return RejectedPickup(
                    PickupOutcome.ItemNotFound,
                    action,
                    "Item is not on the actor's cell."
                );
            }

            if (actor.Inventory.IsFull)
            {
                return RejectedPickup(
                    PickupOutcome.InventoryFull,
                    action,
                    "Inventory is full."
                );
            }

            if (!actor.Inventory.TryAdd(item) ||
                !Map.TryTakeItem(
                    actor.Position,
                    item.Id,
                    out _
                ))
            {
                actor.Inventory.TryRemove(item.Id, out _);
                return RejectedPickup(
                    PickupOutcome.ItemNotFound,
                    action,
                    "Item could not be picked up."
                );
            }

            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            Turns.CompleteAction(action.ActorId, resolution);
            return new PickupActionResult(
                PickupOutcome.PickedUp,
                resolution,
                action.ActorId,
                action.ItemId
            );
        }

        public UseItemActionResult ResolveUseItem(UseItemAction action)
        {
            if (!TryValidateInventoryActor(
                    action.ActorId,
                    out ActorState actor,
                    out ActionResolution rejection
                ))
            {
                UseItemOutcome outcome =
                    action.ActorId == Turns.CurrentActor
                        ? UseItemOutcome.InvalidActor
                        : UseItemOutcome.NotActorsTurn;
                return new UseItemActionResult(
                    outcome,
                    rejection,
                    action.ActorId,
                    action.ItemId
                );
            }

            if (!actor.Inventory.TryGet(
                    action.ItemId,
                    out ItemState item
                ))
            {
                return RejectedUse(
                    UseItemOutcome.ItemNotFound,
                    action,
                    "Item is not in the inventory."
                );
            }

            int healthRestored = item.Kind switch
            {
                ItemKind.HealingPotion =>
                    actor.Heal(item.EffectPower),
                _ => 0
            };
            if (healthRestored == 0)
            {
                return RejectedUse(
                    UseItemOutcome.NoEffect,
                    action,
                    "Item would have no effect."
                );
            }

            actor.Inventory.TryRemove(item.Id, out _);
            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            Turns.CompleteAction(action.ActorId, resolution);
            return new UseItemActionResult(
                UseItemOutcome.Used,
                resolution,
                action.ActorId,
                action.ItemId,
                healthRestored
            );
        }

        public DropItemActionResult ResolveDropItem(DropItemAction action)
        {
            if (!TryValidateInventoryActor(
                    action.ActorId,
                    out ActorState actor,
                    out ActionResolution rejection
                ))
            {
                DropItemOutcome outcome =
                    action.ActorId == Turns.CurrentActor
                        ? DropItemOutcome.InvalidActor
                        : DropItemOutcome.NotActorsTurn;
                return new DropItemActionResult(
                    outcome,
                    rejection,
                    action.ActorId,
                    action.ItemId
                );
            }

            if (!actor.Inventory.TryRemove(
                    action.ItemId,
                    out ItemState item
                ))
            {
                return new DropItemActionResult(
                    DropItemOutcome.ItemNotFound,
                    ActionResolution.Rejected(
                        "Item is not in the inventory."
                    ),
                    action.ActorId,
                    action.ItemId
                );
            }

            if (!Map.TryPlaceItem(item, actor.Position))
            {
                actor.Inventory.TryAdd(item);
                return new DropItemActionResult(
                    DropItemOutcome.ItemNotFound,
                    ActionResolution.Rejected(
                        "Item could not be placed on the map."
                    ),
                    action.ActorId,
                    action.ItemId
                );
            }

            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            Turns.CompleteAction(action.ActorId, resolution);
            return new DropItemActionResult(
                DropItemOutcome.Dropped,
                resolution,
                action.ActorId,
                action.ItemId
            );
        }

        public DescendActionResult ResolveDescend(DescendAction action)
        {
            if (action.ActorId != Turns.CurrentActor)
            {
                return RejectedDescend(
                    DescendOutcome.NotActorsTurn,
                    action,
                    $"It is {Turns.CurrentActor}'s turn."
                );
            }

            if (!Map.TryGetActor(action.ActorId, out ActorState actor))
            {
                return RejectedDescend(
                    DescendOutcome.InvalidActor,
                    action,
                    "Actor is not on the map."
                );
            }

            if (!Map.FloorExit.HasValue)
            {
                return RejectedDescend(
                    DescendOutcome.ExitUnavailable,
                    action,
                    "This floor has no exit."
                );
            }

            if (actor.Position != Map.FloorExit.Value)
            {
                return RejectedDescend(
                    DescendOutcome.NotOnExit,
                    action,
                    "Actor is not standing on the exit."
                );
            }

            if (Map.HasEnemies)
            {
                return RejectedDescend(
                    DescendOutcome.EnemiesRemain,
                    action,
                    "Hostile actors remain on this floor."
                );
            }

            IsFloorCompleted = true;
            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            Turns.CompleteAction(action.ActorId, resolution);
            return new DescendActionResult(
                DescendOutcome.FloorCompleted,
                resolution,
                action.ActorId
            );
        }

        public RogueRoundResult ResolvePlayerMoveRound(MoveAction action)
        {
            RogueMoveResult playerMove = ResolveMove(action);
            return CompleteRound(
                playerMove.Resolution,
                playerMove,
                null
            );
        }

        public RogueRoundResult ResolvePlayerWaitRound(WaitAction action)
        {
            WaitActionResult playerWait = ResolveWait(action);
            return CompleteRound(
                playerWait.Resolution,
                null,
                playerWait
            );
        }

        public RogueActionRoundResult<PickupActionResult>
            ResolvePlayerPickupRound(PickupAction action)
        {
            PickupActionResult result = ResolvePickup(action);
            return CompleteActionRound(result, result.Resolution);
        }

        public RogueActionRoundResult<UseItemActionResult>
            ResolvePlayerUseItemRound(UseItemAction action)
        {
            UseItemActionResult result = ResolveUseItem(action);
            return CompleteActionRound(result, result.Resolution);
        }

        public RogueActionRoundResult<DropItemActionResult>
            ResolvePlayerDropItemRound(DropItemAction action)
        {
            DropItemActionResult result = ResolveDropItem(action);
            return CompleteActionRound(result, result.Resolution);
        }

        public RogueActionRoundResult<DescendActionResult>
            ResolvePlayerDescendRound(DescendAction action)
        {
            DescendActionResult result = ResolveDescend(action);
            return CompleteActionRound(result, result.Resolution);
        }

        private RogueRoundResult CompleteRound(
            ActionResolution playerResolution,
            RogueMoveResult? playerMove,
            WaitActionResult? playerWait
        )
        {
            IReadOnlyList<EnemyTurnResult> enemyTurns =
                ResolveEnemyPhase(playerResolution);

            return new RogueRoundResult(
                playerResolution,
                playerMove,
                playerWait,
                enemyTurns,
                Progress
            );
        }

        private RogueActionRoundResult<TActionResult>
            CompleteActionRound<TActionResult>(
                TActionResult actionResult,
                ActionResolution resolution
            )
        {
            IReadOnlyList<EnemyTurnResult> enemyTurns =
                ResolveEnemyPhase(resolution);
            return new RogueActionRoundResult<TActionResult>(
                actionResult,
                resolution,
                enemyTurns,
                Progress
            );
        }

        private IReadOnlyList<EnemyTurnResult> ResolveEnemyPhase(
            ActionResolution playerResolution
        )
        {
            List<EnemyTurnResult> enemyTurns = new();
            if (!playerResolution.ConsumesTurn)
            {
                return enemyTurns;
            }

            while (Turns.Phase == TurnPhase.ResolvingEnemies &&
                   !IsPlayerDefeated)
            {
                enemyTurns.Add(ResolveCurrentEnemyTurn());
            }

            return enemyTurns;
        }

        private EnemyTurnResult ResolveCurrentEnemyTurn()
        {
            ActorId enemyId = Turns.CurrentActor;
            EnemyTurnDecision decision = EnemyTurnPolicy.Decide(
                Map,
                enemyId,
                Turns.PlayerId
            );

            if (decision.ShouldWait)
            {
                WaitActionResult wait = ResolveWait(
                    new WaitAction(enemyId)
                );
                return new EnemyTurnResult(enemyId, null, wait);
            }

            RogueMoveResult move = ResolveMove(decision.Move);
            if (!move.Resolution.ConsumesTurn)
            {
                WaitActionResult fallbackWait = ResolveWait(
                    new WaitAction(enemyId)
                );
                return new EnemyTurnResult(
                    enemyId,
                    move,
                    fallbackWait
                );
            }

            return new EnemyTurnResult(enemyId, move, null);
        }

        private bool TryValidateInventoryActor(
            ActorId actorId,
            out ActorState actor,
            out ActionResolution rejection
        )
        {
            actor = null;
            if (actorId != Turns.CurrentActor)
            {
                rejection = ActionResolution.Rejected(
                    $"It is {Turns.CurrentActor}'s turn."
                );
                return false;
            }

            if (!Map.TryGetActor(actorId, out actor))
            {
                rejection = ActionResolution.Rejected(
                    $"Actor '{actorId}' is not on the map."
                );
                return false;
            }

            rejection = default;
            return true;
        }

        private static PickupActionResult RejectedPickup(
            PickupOutcome outcome,
            PickupAction action,
            string reason
        ) => new(
            outcome,
            ActionResolution.Rejected(reason),
            action.ActorId,
            action.ItemId
        );

        private static UseItemActionResult RejectedUse(
            UseItemOutcome outcome,
            UseItemAction action,
            string reason
        ) => new(
            outcome,
            ActionResolution.Rejected(reason),
            action.ActorId,
            action.ItemId
        );

        private static DescendActionResult RejectedDescend(
            DescendOutcome outcome,
            DescendAction action,
            string reason
        ) => new(
            outcome,
            ActionResolution.Rejected(reason),
            action.ActorId
        );
    }
}
