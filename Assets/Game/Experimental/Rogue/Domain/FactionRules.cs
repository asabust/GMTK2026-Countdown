namespace Game.Experimental.Rogue.Domain
{
    public static class FactionRules
    {
        public static bool AreHostile(
            ActorFaction first,
            ActorFaction second
        ) =>
            first != second &&
            first != ActorFaction.Neutral &&
            second != ActorFaction.Neutral;
    }
}
