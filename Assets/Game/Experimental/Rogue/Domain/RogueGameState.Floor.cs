namespace Game.Experimental.Rogue.Domain
{
    public sealed partial class RogueGameState
    {
        public DescendActionResult ResolveDescend(DescendAction action)
        {
            if (TryRejectTerminal(out ActionResolution terminal))
            {
                return new DescendActionResult(
                    DescendOutcome.GameEnded,
                    terminal,
                    action.ActorId
                );
            }

            if (action.ActorId != Turns.CurrentActor)
            {
                return RejectedDescend(
                    DescendOutcome.NotActorsTurn,
                    action,
                    $"It is {Turns.CurrentActor}'s turn."
                );
            }

            if (!Map.TryGetActor(action.ActorId, out ActorState actor))
            {
                return RejectedDescend(
                    DescendOutcome.InvalidActor,
                    action,
                    "Actor is not on the map."
                );
            }

            if (!Map.FloorExit.HasValue)
            {
                return RejectedDescend(
                    DescendOutcome.ExitUnavailable,
                    action,
                    "This floor has no exit."
                );
            }

            if (actor.Position != Map.FloorExit.Value)
            {
                return RejectedDescend(
                    DescendOutcome.NotOnExit,
                    action,
                    "Actor is not standing on the exit."
                );
            }

            if (Map.HasEnemies)
            {
                return RejectedDescend(
                    DescendOutcome.EnemiesRemain,
                    action,
                    "Hostile actors remain on this floor."
                );
            }

            IsFloorCompleted = true;
            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            Turns.CompleteAction(action.ActorId, resolution);
            return new DescendActionResult(
                DescendOutcome.FloorCompleted,
                resolution,
                action.ActorId
            );
        }

        private static DescendActionResult RejectedDescend(
            DescendOutcome outcome,
            DescendAction action,
            string reason
        ) => new(
            outcome,
            ActionResolution.Rejected(reason),
            action.ActorId
        );
    }
}
