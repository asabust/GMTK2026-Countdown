using System;

namespace Game.Experimental.Rogue.Domain
{
    [Serializable]
    public sealed class GridPositionSnapshot
    {
        public int X;
        public int Y;
    }

    [Serializable]
    public sealed class ItemSnapshot
    {
        public string Id;
        public ItemKind Kind;
        public int EffectPower;
    }

    [Serializable]
    public sealed class GroundItemSnapshot
    {
        public ItemSnapshot Item;
        public GridPositionSnapshot Position;
    }

    [Serializable]
    public sealed class ActorSnapshot
    {
        public string Id;
        public ActorFaction Faction;
        public GridPositionSnapshot Position;
        public int MaximumHealth;
        public int CurrentHealth;
        public int AttackPower;
        public int InventoryCapacity;
        public ItemSnapshot[] Inventory;
    }

    /// <summary>
    /// Serializer-neutral stable-turn save data. It intentionally contains
    /// arrays and primitive fields instead of live domain references.
    /// </summary>
    [Serializable]
    public sealed class RogueGameSnapshot
    {
        public int Width;
        public int Height;
        public bool[] Walkable;
        public string PlayerId;
        public ActorSnapshot[] Actors;
        public string[] EnemyIds;
        public GroundItemSnapshot[] GroundItems;
        public bool HasFloorExit;
        public GridPositionSnapshot FloorExit;
        public bool IsFloorCompleted;
        public int RoundNumber;
        public int PlayerSightRadius;
        public GridPositionSnapshot[] Explored;
    }
}
