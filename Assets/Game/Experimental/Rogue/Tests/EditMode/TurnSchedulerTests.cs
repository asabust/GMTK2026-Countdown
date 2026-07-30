using System;
using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class TurnSchedulerTests
    {
        private static readonly ActorId Player = new("player");
        private static readonly ActorId Slime = new("slime");
        private static readonly ActorId Bat = new("bat");

        [Test]
        public void ConsumedPlayerTurn_VisitsEnemiesInRegistrationOrder()
        {
            TurnScheduler scheduler = CreateSchedulerWithTwoEnemies();

            scheduler.CompleteAction(
                Player,
                ActionResolution.TurnConsumed()
            );

            Assert.That(scheduler.Phase, Is.EqualTo(TurnPhase.ResolvingEnemies));
            Assert.That(scheduler.CurrentActor, Is.EqualTo(Slime));

            scheduler.CompleteAction(
                Slime,
                ActionResolution.TurnConsumed()
            );

            Assert.That(scheduler.CurrentActor, Is.EqualTo(Bat));

            scheduler.CompleteAction(
                Bat,
                ActionResolution.TurnConsumed()
            );

            Assert.That(scheduler.Phase, Is.EqualTo(TurnPhase.AwaitingPlayer));
            Assert.That(scheduler.CurrentActor, Is.EqualTo(Player));
            Assert.That(scheduler.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void RejectedPlayerAction_DoesNotAdvanceTurn()
        {
            TurnScheduler scheduler = CreateSchedulerWithTwoEnemies();

            scheduler.CompleteAction(
                Player,
                ActionResolution.Rejected("Wall blocks movement.")
            );

            Assert.That(scheduler.CurrentActor, Is.EqualTo(Player));
            Assert.That(scheduler.Phase, Is.EqualTo(TurnPhase.AwaitingPlayer));
            Assert.That(scheduler.RoundNumber, Is.EqualTo(1));
        }

        [Test]
        public void PlayerTurnWithoutEnemies_CompletesRoundImmediately()
        {
            TurnScheduler scheduler = new(Player);

            scheduler.CompleteAction(
                Player,
                ActionResolution.TurnConsumed()
            );

            Assert.That(scheduler.CurrentActor, Is.EqualTo(Player));
            Assert.That(scheduler.Phase, Is.EqualTo(TurnPhase.AwaitingPlayer));
            Assert.That(scheduler.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void EnemyRemovedDuringRound_IsSkipped()
        {
            TurnScheduler scheduler = CreateSchedulerWithTwoEnemies();
            scheduler.CompleteAction(
                Player,
                ActionResolution.TurnConsumed()
            );

            scheduler.UnregisterEnemy(Bat);
            scheduler.CompleteAction(
                Slime,
                ActionResolution.TurnConsumed()
            );

            Assert.That(scheduler.CurrentActor, Is.EqualTo(Player));
            Assert.That(scheduler.RoundNumber, Is.EqualTo(2));
        }

        [Test]
        public void CompletingActionForWrongActor_Throws()
        {
            TurnScheduler scheduler = CreateSchedulerWithTwoEnemies();

            InvalidOperationException exception = Assert.Throws<
                InvalidOperationException
            >(() => scheduler.CompleteAction(
                Slime,
                ActionResolution.TurnConsumed()
            ));

            Assert.That(exception.Message, Does.Contain("player"));
        }

        private static TurnScheduler CreateSchedulerWithTwoEnemies()
        {
            TurnScheduler scheduler = new(Player);
            scheduler.RegisterEnemy(Slime);
            scheduler.RegisterEnemy(Bat);
            return scheduler;
        }
    }
}
