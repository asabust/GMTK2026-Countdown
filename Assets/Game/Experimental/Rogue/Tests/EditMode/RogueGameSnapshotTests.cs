using System;
using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class RogueGameSnapshotTests
    {
        private static readonly ActorId Player = new("player");
        private static readonly ActorId Slime = new("slime");
        private static readonly ItemId HeldPotion = new("held-potion");
        private static readonly ItemId GroundPotion = new("ground-potion");

        [Test]
        public void RoundTrip_RestoresMapActorsItemsExitAndExploration()
        {
            RogueGameState original = CreatePopulatedGame();
            original.ResolvePlayerMoveRound(
                new MoveAction(Player, 1, 0)
            );

            RogueGameSnapshot snapshot = original.CreateSnapshot();
            RogueGameState restored =
                RogueGameState.FromSnapshot(snapshot);

            Assert.That(restored.Map.Width, Is.EqualTo(6));
            Assert.That(
                restored.Map.IsWalkable(new GridPosition(3, 2)),
                Is.False
            );
            Assert.That(restored.Map.FloorExit, Is.EqualTo(
                new GridPosition(5, 2)
            ));
            Assert.That(
                restored.Map.TryGetActor(Player, out ActorState player),
                Is.True
            );
            Assert.That(player.Position, Is.EqualTo(new GridPosition(2, 1)));
            Assert.That(player.CurrentHealth, Is.EqualTo(4));
            Assert.That(player.Inventory.Contains(HeldPotion), Is.True);
            Assert.That(
                restored.Map.GetItemsAt(new GridPosition(4, 1))[0].Id,
                Is.EqualTo(GroundPotion)
            );
            Assert.That(restored.Turns.Enemies[0], Is.EqualTo(Slime));
            Assert.That(
                restored.Turns.RoundNumber,
                Is.EqualTo(original.Turns.RoundNumber)
            );
            Assert.That(
                restored.Visibility.IsExplored(new GridPosition(1, 1)),
                Is.True
            );
        }

        [Test]
        public void EnemyPhaseSnapshot_IsRejected()
        {
            RogueGameState game = CreatePopulatedGame();
            game.ResolveWait(new WaitAction(Player));

            Assert.Throws<InvalidOperationException>(
                () => game.CreateSnapshot()
            );
        }

        private static RogueGameState CreatePopulatedGame()
        {
            RogueMapState map = new(6, 4);
            map.SetWalkable(new GridPosition(3, 2), false);
            map.SetFloorExit(new GridPosition(5, 2));
            ActorState player = new(
                Player,
                ActorFaction.Player,
                new GridPosition(1, 1),
                maximumHealth: 5,
                attackPower: 2,
                inventoryCapacity: 2,
                currentHealth: 4
            );
            player.Inventory.TryAdd(new ItemState(
                HeldPotion,
                ItemKind.HealingPotion,
                2
            ));
            map.TryAddActor(player);
            map.TryAddActor(new ActorState(
                Slime,
                ActorFaction.Enemy,
                new GridPosition(5, 3),
                maximumHealth: 3
            ));
            map.TryPlaceItem(
                new ItemState(
                    GroundPotion,
                    ItemKind.HealingPotion,
                    3
                ),
                new GridPosition(4, 1)
            );

            RogueGameState game = new(
                map,
                Player,
                playerSightRadius: 2
            );
            game.RegisterEnemy(Slime);
            return game;
        }
    }
}
