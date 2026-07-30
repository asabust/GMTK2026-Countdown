using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class GridPositionTests
    {
        [Test]
        public void Offset_ReturnsNewPositionWithoutChangingOrigin()
        {
            GridPosition origin = new(2, 3);

            GridPosition destination = origin.Offset(-1, 4);

            Assert.That(origin, Is.EqualTo(new GridPosition(2, 3)));
            Assert.That(destination, Is.EqualTo(new GridPosition(1, 7)));
        }
    }
}
