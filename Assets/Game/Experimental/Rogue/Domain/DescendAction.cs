namespace Game.Experimental.Rogue.Domain
{
    public readonly struct DescendAction
    {
        public DescendAction(ActorId actorId)
        {
            ActorId = actorId;
        }

        public ActorId ActorId { get; }
    }

    public enum DescendOutcome
    {
        FloorCompleted,
        InvalidActor,
        NotActorsTurn,
        ExitUnavailable,
        NotOnExit,
        EnemiesRemain,
        GameEnded
    }

    public readonly struct DescendActionResult
    {
        public DescendActionResult(
            DescendOutcome outcome,
            ActionResolution resolution,
            ActorId actorId
        )
        {
            Outcome = outcome;
            Resolution = resolution;
            ActorId = actorId;
        }

        public DescendOutcome Outcome { get; }
        public ActionResolution Resolution { get; }
        public ActorId ActorId { get; }
    }
}
