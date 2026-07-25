using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class CollectibleStack
{
    [SerializeField] private CollectibleDefinition definition;
    [SerializeField, Min(1)] private int count;

    public CollectibleStack(CollectibleDefinition definition)
    {
        this.definition = definition;
        count = 1;
    }

    public CollectibleDefinition Definition => definition;
    public int Count => count;
    internal void AddOne() => count++;
    internal bool RemoveOne()
    {
        count = Mathf.Max(0, count - 1);
        return count == 0;
    }
}

public enum InventoryAddResult
{
    Success,
    InvalidDefinition,
    ItemSlotsFull,
    MaximumStacksReached
}

[DisallowMultipleComponent]
public sealed class PlayerInventory : MonoBehaviour
{
    [SerializeField, Min(1)] private int itemSlotCapacity = 4;
    [SerializeField] private List<CollectibleStack> stacks = new();

    public IReadOnlyList<CollectibleStack> Stacks => stacks;
    public int ItemSlotCapacity => itemSlotCapacity;
    public int UsedItemSlots { get; private set; }
    public event Action Changed;

    private void Awake() => RecountItemSlots();

    public InventoryAddResult CanAdd(CollectibleDefinition definition)
    {
        if (definition == null || string.IsNullOrWhiteSpace(definition.CollectibleId))
        {
            return InventoryAddResult.InvalidDefinition;
        }

        CollectibleStack existing = FindStack(definition.CollectibleId);
        if (existing != null)
        {
            return existing.Count < definition.MaximumStacks
                ? InventoryAddResult.Success
                : InventoryAddResult.MaximumStacksReached;
        }

        return definition.Kind == CollectibleKind.Item &&
               UsedItemSlots >= itemSlotCapacity
            ? InventoryAddResult.ItemSlotsFull
            : InventoryAddResult.Success;
    }

    public InventoryAddResult TryAdd(CollectibleDefinition definition)
    {
        InventoryAddResult result = CanAdd(definition);
        if (result != InventoryAddResult.Success)
        {
            return result;
        }

        CollectibleStack existing = FindStack(definition.CollectibleId);
        if (existing != null)
        {
            existing.AddOne();
        }
        else
        {
            stacks.Add(new CollectibleStack(definition));
            if (definition.Kind == CollectibleKind.Item)
            {
                UsedItemSlots++;
            }
        }

        Changed?.Invoke();
        return InventoryAddResult.Success;
    }

    public int GetCount(CollectibleDefinition definition) =>
        definition == null ? 0 : GetCount(definition.CollectibleId);

    public int GetCount(string collectibleId)
    {
        CollectibleStack stack = FindStack(collectibleId);
        return stack?.Count ?? 0;
    }

    public bool TryConsume(CollectibleDefinition definition)
    {
        if (definition == null || definition.Kind != CollectibleKind.Item)
        {
            return false;
        }

        CollectibleStack stack = FindStack(definition.CollectibleId);
        if (stack == null)
        {
            return false;
        }

        if (stack.RemoveOne())
        {
            stacks.Remove(stack);
            UsedItemSlots = Mathf.Max(0, UsedItemSlots - 1);
        }

        Changed?.Invoke();
        return true;
    }

    public List<CollectibleStack> GetOrderedItemStacks()
    {
        List<CollectibleStack> items = stacks.FindAll(stack =>
            stack?.Definition != null &&
            stack.Definition.Kind == CollectibleKind.Item);
        items.Sort((left, right) =>
        {
            int order = left.Definition.InventoryOrder.CompareTo(
                right.Definition.InventoryOrder
            );
            return order != 0
                ? order
                : string.CompareOrdinal(
                    left.Definition.CollectibleId,
                    right.Definition.CollectibleId
                );
        });
        return items;
    }

    public float GetRelicEffect(CollectibleEffectType type, bool highest = false)
    {
        float value = 0f;
        foreach (CollectibleStack stack in stacks)
        {
            CollectibleDefinition definition = stack.Definition;
            if (definition == null ||
                definition.Kind != CollectibleKind.Relic ||
                definition.EffectType != type)
            {
                continue;
            }

            float stackValue = definition.EffectValue * stack.Count;
            value = highest ? Mathf.Max(value, stackValue) : value + stackValue;
        }
        return value;
    }

    public void ResetForNewRun()
    {
        stacks.Clear();
        UsedItemSlots = 0;
        Changed?.Invoke();
    }

    private CollectibleStack FindStack(string id) =>
        stacks.Find(stack =>
            stack.Definition != null &&
            stack.Definition.CollectibleId == id);

    private void RecountItemSlots()
    {
        stacks.RemoveAll(stack => stack == null || stack.Definition == null);
        UsedItemSlots = 0;
        foreach (CollectibleStack stack in stacks)
        {
            if (stack.Definition.Kind == CollectibleKind.Item)
            {
                UsedItemSlots++;
            }
        }
    }

    private void OnValidate() => itemSlotCapacity = Mathf.Max(1, itemSlotCapacity);
}
