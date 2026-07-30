using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class FloorExitTests
    {
        private static readonly ActorId Player = new("player");
        private static readonly ActorId Slime = new("slime");

        [Test]
        public void PlayerOnExitWithNoEnemies_CompletesFloor()
        {
            RogueMapState map = new(4, 4);
            GridPosition exit = new(2, 2);
            map.SetFloorExit(exit);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                exit
            ));
            RogueGameState game = new(map, Player);

            DescendActionResult result = game.ResolveDescend(
                new DescendAction(Player)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(DescendOutcome.FloorCompleted)
            );
            Assert.That(
                game.Progress,
                Is.EqualTo(RogueGameProgress.FloorCompleted)
            );
        }

        [Test]
        public void RemainingEnemy_BlocksDescentWithoutConsumingTurn()
        {
            RogueMapState map = new(4, 4);
            GridPosition exit = new(2, 2);
            map.SetFloorExit(exit);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                exit
            ));
            map.TryAddActor(new ActorState(
                Slime,
                ActorFaction.Enemy,
                new GridPosition(1, 1)
            ));
            RogueGameState game = new(map, Player);
            game.RegisterEnemy(Slime);

            DescendActionResult result = game.ResolveDescend(
                new DescendAction(Player)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(DescendOutcome.EnemiesRemain)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
            Assert.That(game.Turns.RoundNumber, Is.EqualTo(1));
        }
    }
}
