namespace Game.Experimental.Rogue.Domain
{
    public readonly struct PickupAction
    {
        public PickupAction(ActorId actorId, ItemId itemId)
        {
            ActorId = actorId;
            ItemId = itemId;
        }

        public ActorId ActorId { get; }
        public ItemId ItemId { get; }
    }

    public enum PickupOutcome
    {
        PickedUp,
        InvalidActor,
        NotActorsTurn,
        ItemNotFound,
        InventoryFull
    }

    public readonly struct PickupActionResult
    {
        public PickupActionResult(
            PickupOutcome outcome,
            ActionResolution resolution,
            ActorId actorId,
            ItemId itemId
        )
        {
            Outcome = outcome;
            Resolution = resolution;
            ActorId = actorId;
            ItemId = itemId;
        }

        public PickupOutcome Outcome { get; }
        public ActionResolution Resolution { get; }
        public ActorId ActorId { get; }
        public ItemId ItemId { get; }
    }

    public readonly struct UseItemAction
    {
        public UseItemAction(ActorId actorId, ItemId itemId)
        {
            ActorId = actorId;
            ItemId = itemId;
        }

        public ActorId ActorId { get; }
        public ItemId ItemId { get; }
    }

    public enum UseItemOutcome
    {
        Used,
        InvalidActor,
        NotActorsTurn,
        ItemNotFound,
        NoEffect
    }

    public readonly struct UseItemActionResult
    {
        public UseItemActionResult(
            UseItemOutcome outcome,
            ActionResolution resolution,
            ActorId actorId,
            ItemId itemId,
            int healthRestored = 0
        )
        {
            Outcome = outcome;
            Resolution = resolution;
            ActorId = actorId;
            ItemId = itemId;
            HealthRestored = healthRestored;
        }

        public UseItemOutcome Outcome { get; }
        public ActionResolution Resolution { get; }
        public ActorId ActorId { get; }
        public ItemId ItemId { get; }
        public int HealthRestored { get; }
    }

    public readonly struct DropItemAction
    {
        public DropItemAction(ActorId actorId, ItemId itemId)
        {
            ActorId = actorId;
            ItemId = itemId;
        }

        public ActorId ActorId { get; }
        public ItemId ItemId { get; }
    }

    public enum DropItemOutcome
    {
        Dropped,
        InvalidActor,
        NotActorsTurn,
        ItemNotFound
    }

    public readonly struct DropItemActionResult
    {
        public DropItemActionResult(
            DropItemOutcome outcome,
            ActionResolution resolution,
            ActorId actorId,
            ItemId itemId
        )
        {
            Outcome = outcome;
            Resolution = resolution;
            ActorId = actorId;
            ItemId = itemId;
        }

        public DropItemOutcome Outcome { get; }
        public ActionResolution Resolution { get; }
        public ActorId ActorId { get; }
        public ItemId ItemId { get; }
    }
}
