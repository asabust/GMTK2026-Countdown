using System;

namespace Game.Experimental.Rogue.Domain
{
    public readonly struct RogueMoveResult
    {
        public RogueMoveResult(
            MoveActionResult move,
            MeleeAttackResult? meleeAttack = null
        )
        {
            Move = move;
            MeleeAttack = meleeAttack;
        }

        public MoveActionResult Move { get; }
        public MeleeAttackResult? MeleeAttack { get; }
    }

    /// <summary>
    /// Coordinates map rules, combat resolution, and deterministic turns.
    /// Presentation code submits actions here instead of advancing the
    /// scheduler separately.
    /// </summary>
    public sealed class RogueGameState
    {
        public RogueGameState(RogueMapState map, ActorId playerId)
        {
            Map = map ?? throw new ArgumentNullException(nameof(map));

            if (!map.TryGetActor(playerId, out ActorState player) ||
                player.Faction != ActorFaction.Player)
            {
                throw new ArgumentException(
                    "The player must exist on the map.",
                    nameof(playerId)
                );
            }

            Turns = new TurnScheduler(playerId);
        }

        public RogueMapState Map { get; }
        public TurnScheduler Turns { get; }
        public bool IsPlayerDefeated =>
            !Map.TryGetActor(Turns.PlayerId, out _);

        public bool RegisterEnemy(ActorId enemyId)
        {
            if (!Map.TryGetActor(enemyId, out ActorState enemy) ||
                enemy.Faction != ActorFaction.Enemy)
            {
                return false;
            }

            return Turns.RegisterEnemy(enemyId);
        }

        public RogueMoveResult ResolveMove(MoveAction action)
        {
            if (action.ActorId != Turns.CurrentActor)
            {
                GridPosition position = default;
                if (Map.TryGetActor(action.ActorId, out ActorState actor))
                {
                    position = actor.Position;
                }

                return new RogueMoveResult(new MoveActionResult(
                    MoveOutcome.NotActorsTurn,
                    ActionResolution.Rejected(
                        $"It is {Turns.CurrentActor}'s turn."
                    ),
                    position,
                    position
                ));
            }

            MoveActionResult move = MoveActionResolver.Resolve(Map, action);
            MeleeAttackResult? melee = null;
            ActionResolution resolution = move.Resolution;

            if (move.Outcome == MoveOutcome.MeleeAttack)
            {
                MeleeAttackResult attack = MeleeAttackResolver.Resolve(
                    Map,
                    new MeleeAttackAction(
                        action.ActorId,
                        move.TargetActorId
                    )
                );
                melee = attack;
                resolution = attack.Resolution;

                if (attack.Outcome ==
                    MeleeAttackOutcome.TargetDefeated)
                {
                    Turns.UnregisterEnemy(attack.TargetId);
                }
            }

            Turns.CompleteAction(action.ActorId, resolution);
            return new RogueMoveResult(move, melee);
        }
    }
}
