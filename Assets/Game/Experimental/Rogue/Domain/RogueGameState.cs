using System;
using System.Collections.Generic;

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
        public ActionResolution Resolution =>
            MeleeAttack?.Resolution ?? Move.Resolution;
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
        public RogueGameProgress Progress =>
            IsPlayerDefeated
                ? RogueGameProgress.PlayerDefeated
                : Turns.Enemies.Count == 0
                    ? RogueGameProgress.FloorCleared
                    : RogueGameProgress.Ongoing;

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

        public WaitActionResult ResolveWait(WaitAction action)
        {
            if (action.ActorId != Turns.CurrentActor)
            {
                return new WaitActionResult(
                    WaitOutcome.NotActorsTurn,
                    ActionResolution.Rejected(
                        $"It is {Turns.CurrentActor}'s turn."
                    ),
                    action.ActorId
                );
            }

            if (!Map.TryGetActor(action.ActorId, out _))
            {
                return new WaitActionResult(
                    WaitOutcome.InvalidActor,
                    ActionResolution.Rejected(
                        $"Actor '{action.ActorId}' is not on the map."
                    ),
                    action.ActorId
                );
            }

            ActionResolution resolution =
                ActionResolution.TurnConsumed();
            Turns.CompleteAction(action.ActorId, resolution);
            return new WaitActionResult(
                WaitOutcome.Waited,
                resolution,
                action.ActorId
            );
        }

        public RogueRoundResult ResolvePlayerMoveRound(MoveAction action)
        {
            RogueMoveResult playerMove = ResolveMove(action);
            return CompleteRound(
                playerMove.Resolution,
                playerMove,
                null
            );
        }

        public RogueRoundResult ResolvePlayerWaitRound(WaitAction action)
        {
            WaitActionResult playerWait = ResolveWait(action);
            return CompleteRound(
                playerWait.Resolution,
                null,
                playerWait
            );
        }

        private RogueRoundResult CompleteRound(
            ActionResolution playerResolution,
            RogueMoveResult? playerMove,
            WaitActionResult? playerWait
        )
        {
            List<EnemyTurnResult> enemyTurns = new();
            if (playerResolution.ConsumesTurn)
            {
                while (Turns.Phase == TurnPhase.ResolvingEnemies &&
                       !IsPlayerDefeated)
                {
                    enemyTurns.Add(ResolveCurrentEnemyTurn());
                }
            }

            return new RogueRoundResult(
                playerResolution,
                playerMove,
                playerWait,
                enemyTurns,
                Progress
            );
        }

        private EnemyTurnResult ResolveCurrentEnemyTurn()
        {
            ActorId enemyId = Turns.CurrentActor;
            EnemyTurnDecision decision = EnemyTurnPolicy.Decide(
                Map,
                enemyId,
                Turns.PlayerId
            );

            if (decision.ShouldWait)
            {
                WaitActionResult wait = ResolveWait(
                    new WaitAction(enemyId)
                );
                return new EnemyTurnResult(enemyId, null, wait);
            }

            RogueMoveResult move = ResolveMove(decision.Move);
            if (!move.Resolution.ConsumesTurn)
            {
                WaitActionResult fallbackWait = ResolveWait(
                    new WaitAction(enemyId)
                );
                return new EnemyTurnResult(
                    enemyId,
                    move,
                    fallbackWait
                );
            }

            return new EnemyTurnResult(enemyId, move, null);
        }
    }
}
