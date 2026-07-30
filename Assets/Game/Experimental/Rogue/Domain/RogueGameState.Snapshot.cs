using System;
using System.Linq;

namespace Game.Experimental.Rogue.Domain
{
    public sealed partial class RogueGameState
    {
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
    }
}
