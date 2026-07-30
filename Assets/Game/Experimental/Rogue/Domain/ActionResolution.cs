namespace Game.Experimental.Rogue.Domain
{
    /// <summary>
    /// Result of applying an action to domain state.
    /// Only actions that consume a turn advance the scheduler.
    /// </summary>
    public readonly struct ActionResolution
    {
        private ActionResolution(
            bool succeeded,
            bool consumesTurn,
            string failureReason
        )
        {
            Succeeded = succeeded;
            ConsumesTurn = consumesTurn;
            FailureReason = failureReason;
        }

        public bool Succeeded { get; }
        public bool ConsumesTurn { get; }
        public string FailureReason { get; }

        public static ActionResolution TurnConsumed() =>
            new(true, true, null);

        public static ActionResolution SucceededWithoutTurn() =>
            new(true, false, null);

        public static ActionResolution Rejected(string reason) =>
            new(false, false, reason);
    }
}
