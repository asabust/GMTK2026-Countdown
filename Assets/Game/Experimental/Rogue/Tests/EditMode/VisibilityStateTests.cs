using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class VisibilityStateTests
    {
        [Test]
        public void WallIsVisible_ButCellDirectlyBehindItIsHidden()
        {
            RogueMapState map = new(6, 3);
            map.SetWalkable(new GridPosition(2, 1), false);
            VisibilityState visibility = new();

            visibility.Recalculate(
                map,
                new GridPosition(1, 1),
                radius: 4
            );

            Assert.That(
                visibility.IsVisible(new GridPosition(2, 1)),
                Is.True
            );
            Assert.That(
                visibility.IsVisible(new GridPosition(3, 1)),
                Is.False
            );
        }

        [Test]
        public void PreviouslyVisibleCell_RemainsExploredAfterMovingAway()
        {
            RogueMapState map = new(8, 3);
            VisibilityState visibility = new();
            GridPosition oldCell = new(0, 1);

            visibility.Recalculate(
                map,
                new GridPosition(1, 1),
                radius: 2
            );
            visibility.Recalculate(
                map,
                new GridPosition(6, 1),
                radius: 1
            );

            Assert.That(visibility.IsVisible(oldCell), Is.False);
            Assert.That(visibility.IsExplored(oldCell), Is.True);
        }

        [Test]
        public void PlayerMove_AutomaticallyRefreshesVisibility()
        {
            ActorId playerId = new("player");
            RogueMapState map = new(6, 3);
            map.TryAddActor(new ActorState(
                playerId,
                ActorFaction.Player,
                new GridPosition(1, 1)
            ));
            RogueGameState game = new(
                map,
                playerId,
                playerSightRadius: 1
            );

            game.ResolvePlayerMoveRound(
                new MoveAction(playerId, 1, 0)
            );

            Assert.That(
                game.Visibility.IsVisible(new GridPosition(3, 1)),
                Is.True
            );
            Assert.That(
                game.Visibility.IsExplored(new GridPosition(0, 1)),
                Is.True
            );
        }
    }
}
