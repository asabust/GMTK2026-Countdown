using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class MeleeAttackTests
    {
        private static readonly ActorId Player = new("player");
        private static readonly ActorId Slime = new("slime");

        [Test]
        public void AdjacentHostileTarget_TakesAttackPowerAsDamage()
        {
            RogueMapState map = CreateCombatMap(
                playerAttack: 2,
                slimeHealth: 3
            );

            MeleeAttackResult result = MeleeAttackResolver.Resolve(
                map,
                new MeleeAttackAction(Player, Slime)
            );

            Assert.That(result.Outcome, Is.EqualTo(MeleeAttackOutcome.Hit));
            Assert.That(result.DamageDealt, Is.EqualTo(2));
            Assert.That(result.TargetHealthRemaining, Is.EqualTo(1));
            Assert.That(result.Resolution.ConsumesTurn, Is.True);
            Assert.That(map.TryGetActor(Slime, out ActorState slime), Is.True);
            Assert.That(slime.CurrentHealth, Is.EqualTo(1));
        }

        [Test]
        public void LethalDamage_RemovesTargetFromMap()
        {
            RogueMapState map = CreateCombatMap(
                playerAttack: 3,
                slimeHealth: 3
            );

            MeleeAttackResult result = MeleeAttackResolver.Resolve(
                map,
                new MeleeAttackAction(Player, Slime)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(MeleeAttackOutcome.TargetDefeated)
            );
            Assert.That(result.TargetHealthRemaining, Is.Zero);
            Assert.That(map.TryGetActor(Slime, out _), Is.False);
        }

        [Test]
        public void NonAdjacentTarget_IsRejectedWithoutDamage()
        {
            RogueMapState map = new(5, 5);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                new GridPosition(1, 1),
                attackPower: 2
            ));
            map.TryAddActor(new ActorState(
                Slime,
                ActorFaction.Enemy,
                new GridPosition(3, 1),
                maximumHealth: 3
            ));

            MeleeAttackResult result = MeleeAttackResolver.Resolve(
                map,
                new MeleeAttackAction(Player, Slime)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(MeleeAttackOutcome.NotAdjacent)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
            Assert.That(map.TryGetActor(Slime, out ActorState slime), Is.True);
            Assert.That(slime.CurrentHealth, Is.EqualTo(3));
        }

        private static RogueMapState CreateCombatMap(
            int playerAttack,
            int slimeHealth
        )
        {
            RogueMapState map = new(4, 4);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                new GridPosition(1, 1),
                attackPower: playerAttack
            ));
            map.TryAddActor(new ActorState(
                Slime,
                ActorFaction.Enemy,
                new GridPosition(2, 1),
                maximumHealth: slimeHealth
            ));
            return map;
        }
    }
}
