using System.Collections.Generic;

namespace Game.Experimental.Rogue.Domain
{
    public sealed partial class RogueGameState
    {
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

        public RogueActionRoundResult<PickupActionResult>
            ResolvePlayerPickupRound(PickupAction action)
        {
            PickupActionResult result = ResolvePickup(action);
            return CompleteActionRound(result, result.Resolution);
        }

        public RogueActionRoundResult<UseItemActionResult>
            ResolvePlayerUseItemRound(UseItemAction action)
        {
            UseItemActionResult result = ResolveUseItem(action);
            return CompleteActionRound(result, result.Resolution);
        }

        public RogueActionRoundResult<DropItemActionResult>
            ResolvePlayerDropItemRound(DropItemAction action)
        {
            DropItemActionResult result = ResolveDropItem(action);
            return CompleteActionRound(result, result.Resolution);
        }

        public RogueActionRoundResult<DescendActionResult>
            ResolvePlayerDescendRound(DescendAction action)
        {
            DescendActionResult result = ResolveDescend(action);
            return CompleteActionRound(result, result.Resolution);
        }

        private RogueRoundResult CompleteRound(
            ActionResolution playerResolution,
            RogueMoveResult? playerMove,
            WaitActionResult? playerWait
        )
        {
            IReadOnlyList<EnemyTurnResult> enemyTurns =
                ResolveEnemyPhase(playerResolution);

            return new RogueRoundResult(
                playerResolution,
                playerMove,
                playerWait,
                enemyTurns,
                Progress
            );
        }

        private RogueActionRoundResult<TActionResult>
            CompleteActionRound<TActionResult>(
                TActionResult actionResult,
                ActionResolution resolution
            )
        {
            IReadOnlyList<EnemyTurnResult> enemyTurns =
                ResolveEnemyPhase(resolution);
            return new RogueActionRoundResult<TActionResult>(
                actionResult,
                resolution,
                enemyTurns,
                Progress
            );
        }

        private IReadOnlyList<EnemyTurnResult> ResolveEnemyPhase(
            ActionResolution playerResolution
        )
        {
            List<EnemyTurnResult> enemyTurns = new();
            if (!playerResolution.ConsumesTurn)
            {
                return enemyTurns;
            }

            while (Turns.Phase == TurnPhase.ResolvingEnemies &&
                   !IsPlayerDefeated)
            {
                enemyTurns.Add(ResolveCurrentEnemyTurn());
            }

            return enemyTurns;
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
