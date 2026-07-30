using System.Collections.Generic;

namespace Game.Experimental.Rogue.Domain
{
    public sealed partial class RogueGameState
    {
        public PickupActionResult ResolvePickup(PickupAction action)
        {
            if (TryRejectTerminal(out ActionResolution terminal))
            {
                return new PickupActionResult(
                    PickupOutcome.GameEnded,
                    terminal,
                    action.ActorId,
                    action.ItemId
                );
            }

            if (!TryValidateInventoryActor(
                    action.ActorId,
                    out ActorState actor,
                    out ActionResolution rejection
                ))
            {
                PickupOutcome outcome =
                    action.ActorId == Turns.CurrentActor
                        ? PickupOutcome.InvalidActor
                        : PickupOutcome.NotActorsTurn;
                return new PickupActionResult(
                    outcome,
                    rejection,
                    action.ActorId,
                    action.ItemId
                );
            }

            IReadOnlyList<ItemState> groundItems =
                Map.GetItemsAt(actor.Position);
            ItemState item = null;
            for (int index = 0; index < groundItems.Count; index++)
            {
                if (groundItems[index].Id == action.ItemId)
                {
                    item = groundItems[index];
                    break;
                }
            }

            if (item == null)
            {
                return RejectedPickup(
                    PickupOutcome.ItemNotFound,
                    action,
                    "Item is not on the actor's cell."
                );
            }

            if (actor.Inventory.IsFull)
            {
                return RejectedPickup(
                    PickupOutcome.InventoryFull,
                    action,
                    "Inventory is full."
                );
            }

            if (!actor.Inventory.TryAdd(item) ||
                !Map.TryTakeItem(
                    actor.Position,
                    item.Id,
                    out _
                ))
            {
                actor.Inventory.TryRemove(item.Id, out _);
                return RejectedPickup(
                    PickupOutcome.ItemNotFound,
                    action,
                    "Item could not be picked up."
                );
            }

            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            SynchronizeBeforePlayerTurn(action.ActorId, resolution);
            Turns.CompleteAction(action.ActorId, resolution);
            return new PickupActionResult(
                PickupOutcome.PickedUp,
                resolution,
                action.ActorId,
                action.ItemId
            );
        }

        public UseItemActionResult ResolveUseItem(UseItemAction action)
        {
            if (TryRejectTerminal(out ActionResolution terminal))
            {
                return new UseItemActionResult(
                    UseItemOutcome.GameEnded,
                    terminal,
                    action.ActorId,
                    action.ItemId
                );
            }

            if (!TryValidateInventoryActor(
                    action.ActorId,
                    out ActorState actor,
                    out ActionResolution rejection
                ))
            {
                UseItemOutcome outcome =
                    action.ActorId == Turns.CurrentActor
                        ? UseItemOutcome.InvalidActor
                        : UseItemOutcome.NotActorsTurn;
                return new UseItemActionResult(
                    outcome,
                    rejection,
                    action.ActorId,
                    action.ItemId
                );
            }

            if (!actor.Inventory.TryGet(
                    action.ItemId,
                    out ItemState item
                ))
            {
                return RejectedUse(
                    UseItemOutcome.ItemNotFound,
                    action,
                    "Item is not in the inventory."
                );
            }

            int healthRestored = item.Kind switch
            {
                ItemKind.HealingPotion =>
                    actor.Heal(item.EffectPower),
                _ => 0
            };
            if (healthRestored == 0)
            {
                return RejectedUse(
                    UseItemOutcome.NoEffect,
                    action,
                    "Item would have no effect."
                );
            }

            actor.Inventory.TryRemove(item.Id, out _);
            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            SynchronizeBeforePlayerTurn(action.ActorId, resolution);
            Turns.CompleteAction(action.ActorId, resolution);
            return new UseItemActionResult(
                UseItemOutcome.Used,
                resolution,
                action.ActorId,
                action.ItemId,
                healthRestored
            );
        }

        public DropItemActionResult ResolveDropItem(DropItemAction action)
        {
            if (TryRejectTerminal(out ActionResolution terminal))
            {
                return new DropItemActionResult(
                    DropItemOutcome.GameEnded,
                    terminal,
                    action.ActorId,
                    action.ItemId
                );
            }

            if (!TryValidateInventoryActor(
                    action.ActorId,
                    out ActorState actor,
                    out ActionResolution rejection
                ))
            {
                DropItemOutcome outcome =
                    action.ActorId == Turns.CurrentActor
                        ? DropItemOutcome.InvalidActor
                        : DropItemOutcome.NotActorsTurn;
                return new DropItemActionResult(
                    outcome,
                    rejection,
                    action.ActorId,
                    action.ItemId
                );
            }

            if (!actor.Inventory.TryRemove(
                    action.ItemId,
                    out ItemState item
                ))
            {
                return new DropItemActionResult(
                    DropItemOutcome.ItemNotFound,
                    ActionResolution.Rejected(
                        "Item is not in the inventory."
                    ),
                    action.ActorId,
                    action.ItemId
                );
            }

            if (!Map.TryPlaceItem(item, actor.Position))
            {
                actor.Inventory.TryAdd(item);
                return new DropItemActionResult(
                    DropItemOutcome.ItemNotFound,
                    ActionResolution.Rejected(
                        "Item could not be placed on the map."
                    ),
                    action.ActorId,
                    action.ItemId
                );
            }

            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            SynchronizeBeforePlayerTurn(action.ActorId, resolution);
            Turns.CompleteAction(action.ActorId, resolution);
            return new DropItemActionResult(
                DropItemOutcome.Dropped,
                resolution,
                action.ActorId,
                action.ItemId
            );
        }

        private bool TryValidateInventoryActor(
            ActorId actorId,
            out ActorState actor,
            out ActionResolution rejection
        )
        {
            actor = null;
            if (actorId != Turns.CurrentActor)
            {
                rejection = ActionResolution.Rejected(
                    $"It is {Turns.CurrentActor}'s turn."
                );
                return false;
            }

            if (!Map.TryGetActor(actorId, out actor))
            {
                rejection = ActionResolution.Rejected(
                    $"Actor '{actorId}' is not on the map."
                );
                return false;
            }

            rejection = default;
            return true;
        }

        private static PickupActionResult RejectedPickup(
            PickupOutcome outcome,
            PickupAction action,
            string reason
        ) => new(
            outcome,
            ActionResolution.Rejected(reason),
            action.ActorId,
            action.ItemId
        );

        private static UseItemActionResult RejectedUse(
            UseItemOutcome outcome,
            UseItemAction action,
            string reason
        ) => new(
            outcome,
            ActionResolution.Rejected(reason),
            action.ActorId,
            action.ItemId
        );
    }
}
