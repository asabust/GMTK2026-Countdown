namespace Game.Experimental.Rogue.Domain
{
    public readonly struct MoveAction
    {
        public MoveAction(ActorId actorId, int deltaX, int deltaY)
        {
            ActorId = actorId;
            DeltaX = deltaX;
            DeltaY = deltaY;
        }

        public ActorId ActorId { get; }
        public int DeltaX { get; }
        public int DeltaY { get; }
    }

    public enum MoveOutcome
    {
        Moved,
        MeleeAttack,
        BlockedByTerrain,
        BlockedByActor,
        InvalidActor,
        InvalidDirection,
        NotActorsTurn
    }

    public readonly struct MoveActionResult
    {
        public MoveActionResult(
            MoveOutcome outcome,
            ActionResolution resolution,
            GridPosition origin,
            GridPosition destination,
            ActorId targetActorId = default
        )
        {
            Outcome = outcome;
            Resolution = resolution;
            Origin = origin;
            Destination = destination;
            TargetActorId = targetActorId;
        }

        public MoveOutcome Outcome { get; }
        public ActionResolution Resolution { get; }
        public GridPosition Origin { get; }
        public GridPosition Destination { get; }
        public ActorId TargetActorId { get; }
    }

    public static class MoveActionResolver
    {
        public static MoveActionResult Resolve(
            RogueMapState map,
            MoveAction action
        )
        {
            if (map == null)
            {
                return Rejected(
                    MoveOutcome.InvalidActor,
                    "Map is unavailable."
                );
            }

            if (!map.TryGetActor(action.ActorId, out ActorState actor))
            {
                return Rejected(
                    MoveOutcome.InvalidActor,
                    $"Actor '{action.ActorId}' is not on the map."
                );
            }

            GridPosition origin = actor.Position;
            if (!IsCardinalStep(action.DeltaX, action.DeltaY))
            {
                return Rejected(
                    MoveOutcome.InvalidDirection,
                    "Movement must be exactly one cardinal cell.",
                    origin,
                    origin
                );
            }

            GridPosition destination = origin.Offset(
                action.DeltaX,
                action.DeltaY
            );

            if (!map.IsWalkable(destination))
            {
                return Rejected(
                    MoveOutcome.BlockedByTerrain,
                    $"Terrain blocks movement to {destination}.",
                    origin,
                    destination
                );
            }

            if (map.TryGetActorAt(destination, out ActorState target))
            {
                if (FactionRules.AreHostile(
                    actor.Faction,
                    target.Faction
                ))
                {
                    return new MoveActionResult(
                        MoveOutcome.MeleeAttack,
                        ActionResolution.TurnConsumed(),
                        origin,
                        destination,
                        target.Id
                    );
                }

                return Rejected(
                    MoveOutcome.BlockedByActor,
                    $"Actor '{target.Id}' blocks {destination}.",
                    origin,
                    destination,
                    target.Id
                );
            }

            if (!map.TryMoveActor(actor.Id, destination))
            {
                return Rejected(
                    MoveOutcome.BlockedByTerrain,
                    $"Movement to {destination} could not be committed.",
                    origin,
                    destination
                );
            }

            return new MoveActionResult(
                MoveOutcome.Moved,
                ActionResolution.TurnConsumed(),
                origin,
                destination
            );
        }

        private static bool IsCardinalStep(int deltaX, int deltaY) =>
            System.Math.Abs(deltaX) + System.Math.Abs(deltaY) == 1;

        private static MoveActionResult Rejected(
            MoveOutcome outcome,
            string reason,
            GridPosition origin = default,
            GridPosition destination = default,
            ActorId targetActorId = default
        ) => new(
            outcome,
            ActionResolution.Rejected(reason),
            origin,
            destination,
            targetActorId
        );
    }
}
