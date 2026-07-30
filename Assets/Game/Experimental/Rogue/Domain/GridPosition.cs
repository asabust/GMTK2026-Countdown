using System;

namespace Game.Experimental.Rogue.Domain
{
    /// <summary>
    /// Engine-independent grid coordinate used by prototype rules.
    /// </summary>
    public readonly struct GridPosition : IEquatable<GridPosition>
    {
        public GridPosition(int x, int y)
        {
            X = x;
            Y = y;
        }

        public int X { get; }
        public int Y { get; }

        public GridPosition Offset(int deltaX, int deltaY) =>
            new(X + deltaX, Y + deltaY);

        public bool Equals(GridPosition other) =>
            X == other.X && Y == other.Y;

        public override bool Equals(object obj) =>
            obj is GridPosition other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(X, Y);

        public override string ToString() => $"({X}, {Y})";

        public static bool operator ==(
            GridPosition left,
            GridPosition right
        ) => left.Equals(right);

        public static bool operator !=(
            GridPosition left,
            GridPosition right
        ) => !left.Equals(right);
    }
}
