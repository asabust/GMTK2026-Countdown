using System;

namespace Game.Experimental.Rogue.Domain
{
    public enum ActorFaction
    {
        Player,
        Enemy,
        Neutral
    }

    /// <summary>
    /// Minimal actor data required by map and movement rules.
    /// </summary>
    public sealed class ActorState
    {
        public ActorState(
            ActorId id,
            ActorFaction faction,
            GridPosition position,
            int maximumHealth = 1,
            int attackPower = 1,
            int inventoryCapacity = 0,
            int? currentHealth = null
        )
        {
            if (string.IsNullOrWhiteSpace(id.Value))
            {
                throw new ArgumentException(
                    "Actor ID must be valid.",
                    nameof(id)
                );
            }

            if (maximumHealth <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(maximumHealth),
                    "Maximum health must be positive."
                );
            }

            if (attackPower < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(attackPower),
                    "Attack power cannot be negative."
                );
            }

            int startingHealth = currentHealth ?? maximumHealth;
            if (startingHealth <= 0 || startingHealth > maximumHealth)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(currentHealth),
                    "Current health must be within maximum health."
                );
            }

            Id = id;
            Faction = faction;
            Position = position;
            MaximumHealth = maximumHealth;
            CurrentHealth = startingHealth;
            AttackPower = attackPower;
            Inventory = new InventoryState(inventoryCapacity);
        }

        public ActorId Id { get; }
        public ActorFaction Faction { get; }
        public GridPosition Position { get; internal set; }
        public int MaximumHealth { get; }
        public int CurrentHealth { get; private set; }
        public int AttackPower { get; }
        public InventoryState Inventory { get; }
        public bool IsDefeated => CurrentHealth == 0;

        internal int ApplyDamage(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Damage cannot be negative."
                );
            }

            int previousHealth = CurrentHealth;
            CurrentHealth = Math.Max(0, CurrentHealth - amount);
            return previousHealth - CurrentHealth;
        }

        internal int Heal(int amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(amount),
                    "Healing cannot be negative."
                );
            }

            int previousHealth = CurrentHealth;
            CurrentHealth = Math.Min(
                MaximumHealth,
                CurrentHealth + amount
            );
            return CurrentHealth - previousHealth;
        }
    }
}
