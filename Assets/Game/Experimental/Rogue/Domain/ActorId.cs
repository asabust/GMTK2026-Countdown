using System;

namespace Game.Experimental.Rogue.Domain
{
    /// <summary>
    /// Stable identity for an actor in one run.
    /// </summary>
    public readonly struct ActorId : IEquatable<ActorId>
    {
        public ActorId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Actor ID cannot be empty.",
                    nameof(value)
                );
            }

            Value = value;
        }

        public string Value { get; }

        public bool Equals(ActorId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is ActorId other && Equals(other);

        public override int GetHashCode() =>
            Value == null
                ? 0
                : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? "<invalid actor>";

        public static bool operator ==(ActorId left, ActorId right) =>
            left.Equals(right);

        public static bool operator !=(ActorId left, ActorId right) =>
            !left.Equals(right);
    }
}
