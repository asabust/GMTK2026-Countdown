using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class RogueRoundTests
    {
        private static readonly ActorId Player = new("player");
        private static readonly ActorId Slime = new("slime");

        [Test]
        public void PlayerWait_AutomaticallyRunsEnemyMoveAndReturnsTurn()
        {
            RogueGameState game = CreateGame(
                new GridPosition(3, 1),
                new GridPosition(1, 1)
            );

            RogueRoundResult round = game.ResolvePlayerWaitRound(
                new WaitAction(Player)
            );

            Assert.That(round.EnemyTurns.Count, Is.EqualTo(1));
            Assert.That(
                round.EnemyTurns[0].Move.Value.Move.Outcome,
                Is.EqualTo(MoveOutcome.Moved)
            );
            Assert.That(game.Map.TryGetActor(Slime, out ActorState slime), Is.True);
            Assert.That(
                slime.Position,
                Is.EqualTo(new GridPosition(2, 1))
            );
            Assert.That(game.Turns.CurrentActor, Is.EqualTo(Player));
            Assert.That(game.Turns.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void AdjacentEnemy_AttacksPlayerDuringAutomaticRound()
        {
            RogueGameState game = CreateGame(
                new GridPosition(2, 1),
                new GridPosition(1, 1)
            );

            RogueRoundResult round = game.ResolvePlayerWaitRound(
                new WaitAction(Player)
            );

            Assert.That(
                round.EnemyTurns[0].Move.Value.MeleeAttack.Value.Outcome,
                Is.EqualTo(MeleeAttackOutcome.Hit)
            );
            Assert.That(game.Map.TryGetActor(Player, out ActorState player), Is.True);
            Assert.That(player.CurrentHealth, Is.EqualTo(4));
        }

        [Test]
        public void EnemyLethalAttack_EndsRoundAsPlayerDefeated()
        {
            RogueGameState game = CreateGame(
                new GridPosition(2, 1),
                new GridPosition(1, 1),
                playerHealth: 1
            );

            RogueRoundResult round = game.ResolvePlayerWaitRound(
                new WaitAction(Player)
            );

            Assert.That(
                round.Progress,
                Is.EqualTo(RogueGameProgress.PlayerDefeated)
            );
            Assert.That(game.IsPlayerDefeated, Is.True);
        }

        [Test]
        public void RejectedPlayerMove_DoesNotRunEnemyTurns()
        {
            RogueGameState game = CreateGame(
                new GridPosition(3, 1),
                new GridPosition(1, 1)
            );

            RogueRoundResult round = game.ResolvePlayerMoveRound(
                new MoveAction(Player, 1, 1)
            );

            Assert.That(round.PlayerResolution.ConsumesTurn, Is.False);
            Assert.That(round.EnemyTurns, Is.Empty);
            Assert.That(game.Turns.RoundNumber, Is.EqualTo(1));
        }

        private static RogueGameState CreateGame(
            GridPosition playerPosition,
            GridPosition enemyPosition,
            int playerHealth = 5
        )
        {
            RogueMapState map = new(5, 4);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                playerPosition,
                maximumHealth: playerHealth,
                attackPower: 2
            ));
            map.TryAddActor(new ActorState(
                Slime,
                ActorFaction.Enemy,
                enemyPosition,
                maximumHealth: 3,
                attackPower: 1
            ));

            RogueGameState game = new(map, Player);
            game.RegisterEnemy(Slime);
            return game;
        }
    }
}
