using System;

namespace Game.Experimental.Rogue.Domain
{
    public readonly struct ItemId : IEquatable<ItemId>
    {
        public ItemId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(
                    "Item ID cannot be empty.",
                    nameof(value)
                );
            }

            Value = value;
        }

        public string Value { get; }

        public bool Equals(ItemId other) =>
            string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) =>
            obj is ItemId other && Equals(other);

        public override int GetHashCode() =>
            Value == null
                ? 0
                : StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => Value ?? "<invalid item>";

        public static bool operator ==(ItemId left, ItemId right) =>
            left.Equals(right);

        public static bool operator !=(ItemId left, ItemId right) =>
            !left.Equals(right);
    }

    public enum ItemKind
    {
        HealingPotion
    }

    public sealed class ItemState
    {
        public ItemState(
            ItemId id,
            ItemKind kind,
            int effectPower
        )
        {
            if (string.IsNullOrWhiteSpace(id.Value))
            {
                throw new ArgumentException(
                    "Item ID must be valid.",
                    nameof(id)
                );
            }

            if (effectPower <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(effectPower),
                    "Item effect power must be positive."
                );
            }

            Id = id;
            Kind = kind;
            EffectPower = effectPower;
        }

        public ItemId Id { get; }
        public ItemKind Kind { get; }
        public int EffectPower { get; }
    }

    public sealed class GroundItemState
    {
        public GroundItemState(ItemState item, GridPosition position)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            Position = position;
        }

        public ItemState Item { get; }
        public GridPosition Position { get; }
    }
}
