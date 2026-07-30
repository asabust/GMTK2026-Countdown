namespace Game.Experimental.Rogue.Domain
{
    public readonly struct WaitAction
    {
        public WaitAction(ActorId actorId)
        {
            ActorId = actorId;
        }

        public ActorId ActorId { get; }
    }

    public enum WaitOutcome
    {
        Waited,
        InvalidActor,
        NotActorsTurn,
        GameEnded
    }

    public readonly struct WaitActionResult
    {
        public WaitActionResult(
            WaitOutcome outcome,
            ActionResolution resolution,
            ActorId actorId
        )
        {
            Outcome = outcome;
            Resolution = resolution;
            ActorId = actorId;
        }

        public WaitOutcome Outcome { get; }
        public ActionResolution Resolution { get; }
        public ActorId ActorId { get; }
    }
}
