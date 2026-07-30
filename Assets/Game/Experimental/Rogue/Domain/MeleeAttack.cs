namespace Game.Experimental.Rogue.Domain
{
    public readonly struct MeleeAttackAction
    {
        public MeleeAttackAction(ActorId attackerId, ActorId targetId)
        {
            AttackerId = attackerId;
            TargetId = targetId;
        }

        public ActorId AttackerId { get; }
        public ActorId TargetId { get; }
    }

    public enum MeleeAttackOutcome
    {
        Hit,
        TargetDefeated,
        InvalidAttacker,
        InvalidTarget,
        NotHostile,
        NotAdjacent
    }

    public readonly struct MeleeAttackResult
    {
        public MeleeAttackResult(
            MeleeAttackOutcome outcome,
            ActionResolution resolution,
            ActorId attackerId,
            ActorId targetId,
            int damageDealt = 0,
            int targetHealthRemaining = 0
        )
        {
            Outcome = outcome;
            Resolution = resolution;
            AttackerId = attackerId;
            TargetId = targetId;
            DamageDealt = damageDealt;
            TargetHealthRemaining = targetHealthRemaining;
        }

        public MeleeAttackOutcome Outcome { get; }
        public ActionResolution Resolution { get; }
        public ActorId AttackerId { get; }
        public ActorId TargetId { get; }
        public int DamageDealt { get; }
        public int TargetHealthRemaining { get; }
    }

    public static class MeleeAttackResolver
    {
        public static MeleeAttackResult Resolve(
            RogueMapState map,
            MeleeAttackAction action
        )
        {
            if (map == null ||
                !map.TryGetActor(action.AttackerId, out ActorState attacker))
            {
                return Rejected(
                    MeleeAttackOutcome.InvalidAttacker,
                    action,
                    "Attacker is not on the map."
                );
            }

            if (!map.TryGetActor(action.TargetId, out ActorState target))
            {
                return Rejected(
                    MeleeAttackOutcome.InvalidTarget,
                    action,
                    "Target is not on the map."
                );
            }

            if (!FactionRules.AreHostile(
                    attacker.Faction,
                    target.Faction
                ))
            {
                return Rejected(
                    MeleeAttackOutcome.NotHostile,
                    action,
                    "Melee attacks require hostile factions."
                );
            }

            int distance =
                System.Math.Abs(attacker.Position.X - target.Position.X) +
                System.Math.Abs(attacker.Position.Y - target.Position.Y);
            if (distance != 1)
            {
                return Rejected(
                    MeleeAttackOutcome.NotAdjacent,
                    action,
                    "Melee attacks require cardinal adjacency."
                );
            }

            int damageDealt = target.ApplyDamage(attacker.AttackPower);
            bool defeated = target.IsDefeated;
            int remainingHealth = target.CurrentHealth;
            if (defeated)
            {
                map.RemoveActor(target.Id);
            }

            return new MeleeAttackResult(
                defeated
                    ? MeleeAttackOutcome.TargetDefeated
                    : MeleeAttackOutcome.Hit,
                ActionResolution.TurnConsumed(),
                attacker.Id,
                target.Id,
                damageDealt,
                remainingHealth
            );
        }

        private static MeleeAttackResult Rejected(
            MeleeAttackOutcome outcome,
            MeleeAttackAction action,
            string reason
        ) => new(
            outcome,
            ActionResolution.Rejected(reason),
            action.AttackerId,
            action.TargetId
        );
    }
}
