using System;
using System.Collections.Generic;

namespace Game.Experimental.Rogue.Domain
{
    public sealed class InventoryState
    {
        private readonly List<ItemState> items = new();

        public InventoryState(int capacity)
        {
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(capacity),
                    "Inventory capacity cannot be negative."
                );
            }

            Capacity = capacity;
        }

        public int Capacity { get; }
        public IReadOnlyList<ItemState> Items => items;
        public bool IsFull => items.Count >= Capacity;

        public bool TryAdd(ItemState item)
        {
            if (item == null ||
                IsFull ||
                Contains(item.Id))
            {
                return false;
            }

            items.Add(item);
            return true;
        }

        public bool TryGet(ItemId itemId, out ItemState item)
        {
            item = items.Find(candidate => candidate.Id == itemId);
            return item != null;
        }

        public bool TryRemove(ItemId itemId, out ItemState item)
        {
            if (!TryGet(itemId, out item))
            {
                return false;
            }

            items.Remove(item);
            return true;
        }

        public bool Contains(ItemId itemId) =>
            items.Exists(item => item.Id == itemId);
    }
}
