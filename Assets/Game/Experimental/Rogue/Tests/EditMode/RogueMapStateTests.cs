using System;
using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class RogueMapStateTests
    {
        [Test]
        public void TwoActors_CannotOccupySameCell()
        {
            RogueMapState map = new(3, 3);
            bool firstAdded = map.TryAddActor(new ActorState(
                new ActorId("first"),
                ActorFaction.Player,
                new GridPosition(1, 1)
            ));

            bool secondAdded = map.TryAddActor(new ActorState(
                new ActorId("second"),
                ActorFaction.Enemy,
                new GridPosition(1, 1)
            ));

            Assert.That(firstAdded, Is.True);
            Assert.That(secondAdded, Is.False);
            Assert.That(map.Actors, Has.Count.EqualTo(1));
        }

        [Test]
        public void OccupiedCell_CannotBecomeBlocked()
        {
            RogueMapState map = new(3, 3);
            map.TryAddActor(new ActorState(
                new ActorId("player"),
                ActorFaction.Player,
                new GridPosition(1, 1)
            ));

            InvalidOperationException exception = Assert.Throws<
                InvalidOperationException
            >(() => map.SetWalkable(new GridPosition(1, 1), false));

            Assert.That(exception.Message, Does.Contain("(1, 1)"));
        }
    }
}
