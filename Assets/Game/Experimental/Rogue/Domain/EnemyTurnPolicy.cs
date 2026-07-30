using System;

namespace Game.Experimental.Rogue.Domain
{
    public readonly struct EnemyTurnDecision
    {
        private EnemyTurnDecision(bool shouldWait, MoveAction move)
        {
            ShouldWait = shouldWait;
            Move = move;
        }

        public bool ShouldWait { get; }
        public MoveAction Move { get; }

        public static EnemyTurnDecision Wait(ActorId actorId) =>
            new(true, new MoveAction(actorId, 0, 0));

        public static EnemyTurnDecision MoveBy(
            ActorId actorId,
            int deltaX,
            int deltaY
        ) => new(false, new MoveAction(actorId, deltaX, deltaY));
    }

    /// <summary>
    /// Deterministic prototype policy: approach the player one cardinal cell.
    /// The larger distance axis is preferred; horizontal wins ties.
    /// If the preferred cell is blocked, the other axis is attempted.
    /// </summary>
    public static class EnemyTurnPolicy
    {
        public static EnemyTurnDecision Decide(
            RogueMapState map,
            ActorId enemyId,
            ActorId playerId
        )
        {
            if (map == null ||
                !map.TryGetActor(enemyId, out ActorState enemy) ||
                !map.TryGetActor(playerId, out ActorState player))
            {
                return EnemyTurnDecision.Wait(enemyId);
            }

            int distanceX = player.Position.X - enemy.Position.X;
            int distanceY = player.Position.Y - enemy.Position.Y;
            int stepX = Math.Sign(distanceX);
            int stepY = Math.Sign(distanceY);

            bool horizontalFirst =
                Math.Abs(distanceX) >= Math.Abs(distanceY);

            if (horizontalFirst)
            {
                if (CanEnter(map, enemy, stepX, 0))
                {
                    return EnemyTurnDecision.MoveBy(enemyId, stepX, 0);
                }

                if (CanEnter(map, enemy, 0, stepY))
                {
                    return EnemyTurnDecision.MoveBy(enemyId, 0, stepY);
                }
            }
            else
            {
                if (CanEnter(map, enemy, 0, stepY))
                {
                    return EnemyTurnDecision.MoveBy(enemyId, 0, stepY);
                }

                if (CanEnter(map, enemy, stepX, 0))
                {
                    return EnemyTurnDecision.MoveBy(enemyId, stepX, 0);
                }
            }

            return EnemyTurnDecision.Wait(enemyId);
        }

        private static bool CanEnter(
            RogueMapState map,
            ActorState actor,
            int deltaX,
            int deltaY
        )
        {
            if (Math.Abs(deltaX) + Math.Abs(deltaY) != 1)
            {
                return false;
            }

            GridPosition destination = actor.Position.Offset(
                deltaX,
                deltaY
            );
            if (!map.IsWalkable(destination))
            {
                return false;
            }

            return !map.TryGetActorAt(
                       destination,
                       out ActorState occupant
                   ) ||
                   FactionRules.AreHostile(
                       actor.Faction,
                       occupant.Faction
                   );
        }
    }
}
