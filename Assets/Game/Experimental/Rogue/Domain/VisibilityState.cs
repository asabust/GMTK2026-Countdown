using System;
using System.Collections.Generic;

namespace Game.Experimental.Rogue.Domain
{
    /// <summary>
    /// Current visibility plus persistent exploration memory.
    /// Non-walkable terrain is visible itself and blocks cells behind it.
    /// </summary>
    public sealed class VisibilityState
    {
        private readonly HashSet<GridPosition> visible = new();
        private readonly HashSet<GridPosition> explored = new();

        public IReadOnlyCollection<GridPosition> Visible => visible;
        public IReadOnlyCollection<GridPosition> Explored => explored;

        public bool IsVisible(GridPosition position) =>
            visible.Contains(position);

        public bool IsExplored(GridPosition position) =>
            explored.Contains(position);

        public void Recalculate(
            RogueMapState map,
            GridPosition origin,
            int radius
        )
        {
            if (map == null)
            {
                throw new ArgumentNullException(nameof(map));
            }

            if (!map.IsInBounds(origin))
            {
                throw new ArgumentOutOfRangeException(nameof(origin));
            }

            if (radius < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(radius));
            }

            visible.Clear();
            int radiusSquared = radius * radius;
            for (int x = origin.X - radius; x <= origin.X + radius; x++)
            {
                for (int y = origin.Y - radius;
                     y <= origin.Y + radius;
                     y++)
                {
                    GridPosition target = new(x, y);
                    int deltaX = x - origin.X;
                    int deltaY = y - origin.Y;
                    if (!map.IsInBounds(target) ||
                        deltaX * deltaX + deltaY * deltaY >
                        radiusSquared ||
                        !HasLineOfSight(map, origin, target))
                    {
                        continue;
                    }

                    visible.Add(target);
                    explored.Add(target);
                }
            }
        }

        internal void RestoreExplored(
            IEnumerable<GridPosition> positions
        )
        {
            explored.Clear();
            foreach (GridPosition position in positions)
            {
                explored.Add(position);
            }
        }

        private static bool HasLineOfSight(
            RogueMapState map,
            GridPosition origin,
            GridPosition target
        )
        {
            int x = origin.X;
            int y = origin.Y;
            int deltaX = Math.Abs(target.X - origin.X);
            int deltaY = Math.Abs(target.Y - origin.Y);
            int stepX = origin.X < target.X ? 1 : -1;
            int stepY = origin.Y < target.Y ? 1 : -1;
            int error = deltaX - deltaY;

            while (x != target.X || y != target.Y)
            {
                int doubledError = error * 2;
                if (doubledError > -deltaY)
                {
                    error -= deltaY;
                    x += stepX;
                }

                if (doubledError < deltaX)
                {
                    error += deltaX;
                    y += stepY;
                }

                GridPosition current = new(x, y);
                if (current != target && !map.IsWalkable(current))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
