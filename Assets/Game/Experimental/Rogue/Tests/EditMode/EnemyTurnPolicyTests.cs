using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class EnemyTurnPolicyTests
    {
        private static readonly ActorId Player = new("player");
        private static readonly ActorId Slime = new("slime");

        [Test]
        public void LargerHorizontalDistance_MovesHorizontally()
        {
            RogueMapState map = CreateMap(
                new GridPosition(4, 2),
                new GridPosition(1, 1)
            );

            EnemyTurnDecision decision = EnemyTurnPolicy.Decide(
                map,
                Slime,
                Player
            );

            Assert.That(decision.ShouldWait, Is.False);
            Assert.That(decision.Move.DeltaX, Is.EqualTo(1));
            Assert.That(decision.Move.DeltaY, Is.Zero);
        }

        [Test]
        public void PreferredAxisBlocked_TriesOtherAxis()
        {
            RogueMapState map = CreateMap(
                new GridPosition(4, 2),
                new GridPosition(1, 1)
            );
            map.SetWalkable(new GridPosition(2, 1), false);

            EnemyTurnDecision decision = EnemyTurnPolicy.Decide(
                map,
                Slime,
                Player
            );

            Assert.That(decision.ShouldWait, Is.False);
            Assert.That(decision.Move.DeltaX, Is.Zero);
            Assert.That(decision.Move.DeltaY, Is.EqualTo(1));
        }

        [Test]
        public void BothApproachCellsBlocked_Waits()
        {
            RogueMapState map = CreateMap(
                new GridPosition(4, 2),
                new GridPosition(1, 1)
            );
            map.SetWalkable(new GridPosition(2, 1), false);
            map.SetWalkable(new GridPosition(1, 2), false);

            EnemyTurnDecision decision = EnemyTurnPolicy.Decide(
                map,
                Slime,
                Player
            );

            Assert.That(decision.ShouldWait, Is.True);
        }

        private static RogueMapState CreateMap(
            GridPosition playerPosition,
            GridPosition enemyPosition
        )
        {
            RogueMapState map = new(6, 4);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                playerPosition
            ));
            map.TryAddActor(new ActorState(
                Slime,
                ActorFaction.Enemy,
                enemyPosition
            ));
            return map;
        }
    }
}
