using System.Collections.Generic;

namespace Game.Experimental.Rogue.Domain
{
    public enum RogueGameProgress
    {
        Ongoing,
        FloorCleared,
        FloorCompleted,
        PlayerDefeated
    }

    public readonly struct EnemyTurnResult
    {
        public EnemyTurnResult(
            ActorId actorId,
            RogueMoveResult? move,
            WaitActionResult? wait
        )
        {
            ActorId = actorId;
            Move = move;
            Wait = wait;
        }

        public ActorId ActorId { get; }
        public RogueMoveResult? Move { get; }
        public WaitActionResult? Wait { get; }
    }

    public readonly struct RogueRoundResult
    {
        public RogueRoundResult(
            ActionResolution playerResolution,
            RogueMoveResult? playerMove,
            WaitActionResult? playerWait,
            IReadOnlyList<EnemyTurnResult> enemyTurns,
            RogueGameProgress progress
        )
        {
            PlayerResolution = playerResolution;
            PlayerMove = playerMove;
            PlayerWait = playerWait;
            EnemyTurns = enemyTurns;
            Progress = progress;
        }

        public ActionResolution PlayerResolution { get; }
        public RogueMoveResult? PlayerMove { get; }
        public WaitActionResult? PlayerWait { get; }
        public IReadOnlyList<EnemyTurnResult> EnemyTurns { get; }
        public RogueGameProgress Progress { get; }
    }

    public readonly struct RogueActionRoundResult<TActionResult>
    {
        public RogueActionRoundResult(
            TActionResult playerAction,
            ActionResolution playerResolution,
            IReadOnlyList<EnemyTurnResult> enemyTurns,
            RogueGameProgress progress
        )
        {
            PlayerAction = playerAction;
            PlayerResolution = playerResolution;
            EnemyTurns = enemyTurns;
            Progress = progress;
        }

        public TActionResult PlayerAction { get; }
        public ActionResolution PlayerResolution { get; }
        public IReadOnlyList<EnemyTurnResult> EnemyTurns { get; }
        public RogueGameProgress Progress { get; }
    }
}
