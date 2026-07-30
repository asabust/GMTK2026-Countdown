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
            GridPosition position
        )
        {
            if (string.IsNullOrWhiteSpace(id.Value))
            {
                throw new ArgumentException(
                    "Actor ID must be valid.",
                    nameof(id)
                );
            }

            Id = id;
            Faction = faction;
            Position = position;
        }

        public ActorId Id { get; }
        public ActorFaction Faction { get; }
        public GridPosition Position { get; internal set; }
    }
}
