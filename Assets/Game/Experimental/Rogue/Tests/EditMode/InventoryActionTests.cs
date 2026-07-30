using Game.Experimental.Rogue.Domain;
using NUnit.Framework;

namespace Game.Experimental.Rogue.Tests
{
    public sealed class InventoryActionTests
    {
        private static readonly ActorId Player = new("player");
        private static readonly ItemId Potion = new("potion-1");

        [Test]
        public void Pickup_MovesGroundItemIntoInventory()
        {
            RogueGameState game = CreateGame();
            game.Map.TryPlaceItem(
                CreatePotion(),
                new GridPosition(1, 1)
            );

            PickupActionResult result = game.ResolvePickup(
                new PickupAction(Player, Potion)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(PickupOutcome.PickedUp)
            );
            Assert.That(
                game.Map.GetItemsAt(new GridPosition(1, 1)),
                Is.Empty
            );
            Assert.That(
                game.Map.TryGetActor(Player, out ActorState player),
                Is.True
            );
            Assert.That(player.Inventory.Contains(Potion), Is.True);
        }

        [Test]
        public void FullInventory_RejectsPickupAndLeavesItemOnGround()
        {
            RogueGameState game = CreateGame(capacity: 1);
            game.Map.TryGetActor(Player, out ActorState player);
            player.Inventory.TryAdd(new ItemState(
                new ItemId("held"),
                ItemKind.HealingPotion,
                1
            ));
            game.Map.TryPlaceItem(
                CreatePotion(),
                new GridPosition(1, 1)
            );

            PickupActionResult result = game.ResolvePickup(
                new PickupAction(Player, Potion)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(PickupOutcome.InventoryFull)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
            Assert.That(
                game.Map.GetItemsAt(new GridPosition(1, 1)).Count,
                Is.EqualTo(1)
            );
        }

        [Test]
        public void HealingPotion_RestoresHealthAndIsConsumed()
        {
            RogueGameState game = CreateGame(currentHealth: 2);
            game.Map.TryGetActor(Player, out ActorState player);
            player.Inventory.TryAdd(CreatePotion(power: 3));

            UseItemActionResult result = game.ResolveUseItem(
                new UseItemAction(Player, Potion)
            );

            Assert.That(result.Outcome, Is.EqualTo(UseItemOutcome.Used));
            Assert.That(result.HealthRestored, Is.EqualTo(3));
            Assert.That(player.CurrentHealth, Is.EqualTo(5));
            Assert.That(player.Inventory.Contains(Potion), Is.False);
        }

        [Test]
        public void PotionAtFullHealth_IsNotConsumed()
        {
            RogueGameState game = CreateGame();
            game.Map.TryGetActor(Player, out ActorState player);
            player.Inventory.TryAdd(CreatePotion());

            UseItemActionResult result = game.ResolveUseItem(
                new UseItemAction(Player, Potion)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(UseItemOutcome.NoEffect)
            );
            Assert.That(result.Resolution.ConsumesTurn, Is.False);
            Assert.That(player.Inventory.Contains(Potion), Is.True);
        }

        [Test]
        public void Drop_MovesInventoryItemOntoActorCell()
        {
            RogueGameState game = CreateGame();
            game.Map.TryGetActor(Player, out ActorState player);
            player.Inventory.TryAdd(CreatePotion());

            DropItemActionResult result = game.ResolveDropItem(
                new DropItemAction(Player, Potion)
            );

            Assert.That(
                result.Outcome,
                Is.EqualTo(DropItemOutcome.Dropped)
            );
            Assert.That(player.Inventory.Contains(Potion), Is.False);
            Assert.That(
                game.Map.GetItemsAt(player.Position).Count,
                Is.EqualTo(1)
            );
        }

        private static RogueGameState CreateGame(
            int capacity = 2,
            int currentHealth = 5
        )
        {
            RogueMapState map = new(4, 4);
            map.TryAddActor(new ActorState(
                Player,
                ActorFaction.Player,
                new GridPosition(1, 1),
                maximumHealth: 5,
                attackPower: 2,
                inventoryCapacity: capacity,
                currentHealth: currentHealth
            ));
            return new RogueGameState(map, Player);
        }

        private static ItemState CreatePotion(int power = 3) =>
            new(Potion, ItemKind.HealingPotion, power);
    }
}
