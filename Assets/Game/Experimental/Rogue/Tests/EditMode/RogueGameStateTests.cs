using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class RogueGameStateTests
    {
        private static readonly ActorId Player = new("player");
        private static readonly ActorId Slime = new("slime");

        [Test]
        public void BumpAttackThatDefeatsOnlyEnemy_ReturnsToNextPlayerRound()
        {
            RogueGameState game = CreateGame(
                playerAttack: 3,
                slimeHealth: 3
            );

            RogueMoveResult result = game.ResolveMove(
                new MoveAction(Player, 1, 0)
            );

            Assert.That(
                result.Move.Outcome,
                Is.EqualTo(MoveOutcome.MeleeAttack)
            );
            Assert.That(result.MeleeAttack.HasValue, Is.True);
            Assert.That(
                result.MeleeAttack.Value.Outcome,
                Is.EqualTo(MeleeAttackOutcome.TargetDefeated)
            );
            Assert.That(game.Map.TryGetActor(Slime, out _), Is.False);
            Assert.That(game.Turns.Enemies, Is.Empty);
            Assert.That(game.Turns.CurrentActor, Is.EqualTo(Player));
            Assert.That(game.Turns.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void NonLethalBumpAttack_AdvancesToEnemyTurn()
        {
            RogueGameState game = CreateGame(
                playerAttack: 2,
                slimeHealth: 3
            );

            RogueMoveResult result = game.ResolveMove(
                new MoveAction(Player, 1, 0)
            );

            Assert.That(
                result.MeleeAttack.Value.Outcome,
                Is.EqualTo(MeleeAttackOutcome.Hit)
            );
            Assert.That(game.Map.TryGetActor(Slime, out ActorState slime), Is.True);
            Assert.That(slime.CurrentHealth, Is.EqualTo(1));
            Assert.That(game.Turns.CurrentActor, Is.EqualTo(Slime));
        }

        [Test]
        public void ActionSubmittedOutOfTurn_DoesNotChangeState()
        {
            RogueGameState game = CreateGame(
                playerAttack: 2,
                slimeHealth: 3
            );

            RogueMoveResult result = game.ResolveMove(
                new MoveAction(Slime, -1, 0)
            );

            Assert.That(
                result.Move.Outcome,
                Is.EqualTo(MoveOutcome.NotActorsTurn)
            );
            Assert.That(result.Move.Resolution.ConsumesTurn, Is.False);
            Assert.That(game.Turns.CurrentActor, Is.EqualTo(Player));
            Assert.That(game.Map.TryGetActor(Player, out ActorState player), Is.True);
            Assert.That(
                player.Position,
                Is.EqualTo(new GridPosition(1, 1))
            );
        }

        [Test]
        public void CompletedFloor_RejectsFurtherMovement()
        {
            RogueMapState map = new(4, 4);
            GridPosition exit = new(1, 1);
            map.SetFloorExit(exit);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                exit
            ));
            RogueGameState game = new(map, Player);
            game.ResolveDescend(new DescendAction(Player));

            RogueMoveResult result = game.ResolveMove(
                new MoveAction(Player, 1, 0)
            );

            Assert.That(
                result.Move.Outcome,
                Is.EqualTo(MoveOutcome.GameEnded)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
            Assert.That(map.TryGetActor(Player, out ActorState player), Is.True);
            Assert.That(player.Position, Is.EqualTo(exit));
        }

        [Test]
        public void DefeatedPlayer_RejectsFurtherActions()
        {
            RogueGameState game = CreateUnregisteredEnemyGame();
            game.RemoveActor(Player);

            WaitActionResult result = game.ResolveWait(
                new WaitAction(Player)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(WaitOutcome.GameEnded)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
            Assert.That(
                game.Progress,
                Is.EqualTo(RogueGameProgress.PlayerDefeated)
            );
        }

        [Test]
        public void MapEnemy_IsAutomaticallyAddedBeforePlayerTurnEnds()
        {
            RogueGameState game = CreateUnregisteredEnemyGame();

            RogueRoundResult round = game.ResolvePlayerWaitRound(
                new WaitAction(Player)
            );

            Assert.That(round.EnemyTurns.Count, Is.EqualTo(1));
            Assert.That(round.EnemyTurns[0].ActorId, Is.EqualTo(Slime));
            Assert.That(game.Turns.Enemies, Does.Contain(Slime));
        }

        [Test]
        public void SafeActorRemoval_AlsoRemovesEnemyFromTurnRoster()
        {
            RogueGameState game = CreateUnregisteredEnemyGame();
            game.RegisterEnemy(Slime);

            bool removed = game.RemoveActor(Slime);

            Assert.That(removed, Is.True);
            Assert.That(game.Map.TryGetActor(Slime, out _), Is.False);
            Assert.That(game.Turns.Enemies, Is.Empty);
            Assert.That(
                game.Progress,
                Is.EqualTo(RogueGameProgress.FloorCleared)
            );
        }

        private static RogueGameState CreateUnregisteredEnemyGame()
        {
            RogueMapState map = new(5, 4);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                new GridPosition(1, 1),
                maximumHealth: 5
            ));
            map.TryAddActor(new ActorState(
                Slime,
                ActorFaction.Enemy,
                new GridPosition(3, 1)
            ));
            return new RogueGameState(map, Player);
        }

        private static RogueGameState CreateGame(
            int playerAttack,
            int slimeHealth
        )
        {
            RogueMapState map = new(4, 4);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                new GridPosition(1, 1),
                maximumHealth: 5,
                attackPower: playerAttack
            ));
            map.TryAddActor(new ActorState(
                Slime,
                ActorFaction.Enemy,
                new GridPosition(2, 1),
                maximumHealth: slimeHealth,
                attackPower: 1
            ));

            RogueGameState game = new(map, Player);
            Assert.That(game.RegisterEnemy(Slime), Is.True);
            return game;
        }
    }
}
