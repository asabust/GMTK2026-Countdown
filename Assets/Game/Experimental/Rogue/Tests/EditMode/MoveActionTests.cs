using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class MoveActionTests
    {
        private static readonly ActorId Player = new("player");
        private static readonly ActorId Slime = new("slime");
        private static readonly ActorId Companion = new("companion");

        [Test]
        public void EmptyDestination_MovesActorAndConsumesTurn()
        {
            RogueMapState map = CreateMapWithPlayer();

            MoveActionResult result = MoveActionResolver.Resolve(
                map,
                new MoveAction(Player, 1, 0)
            );

            Assert.That(result.Outcome, Is.EqualTo(MoveOutcome.Moved));
            Assert.That(result.Resolution.ConsumesTurn, Is.True);
            Assert.That(result.Origin, Is.EqualTo(new GridPosition(1, 1)));
            Assert.That(
                result.Destination,
                Is.EqualTo(new GridPosition(2, 1))
            );
            Assert.That(map.TryGetActor(Player, out ActorState player), Is.True);
            Assert.That(
                player.Position,
                Is.EqualTo(new GridPosition(2, 1))
            );
        }

        [Test]
        public void WallDestination_DoesNotMoveOrConsumeTurn()
        {
            RogueMapState map = CreateMapWithPlayer();
            map.SetWalkable(new GridPosition(2, 1), false);
            TurnScheduler scheduler = new(Player);

            MoveActionResult result = MoveActionResolver.Resolve(
                map,
                new MoveAction(Player, 1, 0)
            );
            scheduler.CompleteAction(Player, result.Resolution);

            Assert.That(
                result.Outcome,
                Is.EqualTo(MoveOutcome.BlockedByTerrain)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
            Assert.That(scheduler.CurrentActor, Is.EqualTo(Player));
            Assert.That(scheduler.RoundNumber, Is.EqualTo(1));
            Assert.That(map.TryGetActor(Player, out ActorState player), Is.True);
            Assert.That(
                player.Position,
                Is.EqualTo(new GridPosition(1, 1))
            );
        }

        [Test]
        public void EnemyDestination_BecomesMeleeAttackWithoutMoving()
        {
            RogueMapState map = CreateMapWithPlayer();
            map.TryAddActor(new ActorState(
                Slime,
                ActorFaction.Enemy,
                new GridPosition(2, 1)
            ));

            MoveActionResult result = MoveActionResolver.Resolve(
                map,
                new MoveAction(Player, 1, 0)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(MoveOutcome.MeleeAttack)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.True);
            Assert.That(result.TargetActorId, Is.EqualTo(Slime));
            Assert.That(map.TryGetActor(Player, out ActorState player), Is.True);
            Assert.That(
                player.Position,
                Is.EqualTo(new GridPosition(1, 1))
            );
        }

        [Test]
        public void FriendlyDestination_BlocksWithoutConsumingTurn()
        {
            RogueMapState map = CreateMapWithPlayer();
            map.TryAddActor(new ActorState(
                Companion,
                ActorFaction.Player,
                new GridPosition(2, 1)
            ));

            MoveActionResult result = MoveActionResolver.Resolve(
                map,
                new MoveAction(Player, 1, 0)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(MoveOutcome.BlockedByActor)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
            Assert.That(result.TargetActorId, Is.EqualTo(Companion));
        }

        [Test]
        public void DiagonalMovement_IsRejected()
        {
            RogueMapState map = CreateMapWithPlayer();

            MoveActionResult result = MoveActionResolver.Resolve(
                map,
                new MoveAction(Player, 1, 1)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(MoveOutcome.InvalidDirection)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
        }

        [Test]
        public void OutOfBoundsDestination_IsBlockedTerrain()
        {
            RogueMapState map = new(2, 2);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                new GridPosition(0, 0)
            ));

            MoveActionResult result = MoveActionResolver.Resolve(
                map,
                new MoveAction(Player, -1, 0)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(MoveOutcome.BlockedByTerrain)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
        }

        private static RogueMapState CreateMapWithPlayer()
        {
            RogueMapState map = new(4, 4);
            bool added = map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                new GridPosition(1, 1)
            ));
            Assert.That(added, Is.True);
            return map;
        }
    }
}
