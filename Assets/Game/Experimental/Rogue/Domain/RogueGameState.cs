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
        public ActionResolution Resolution =>
            MeleeAttack?.Resolution ?? Move.Resolution;
    }

    /// <summary>
    /// Coordinates map rules, actions, combat, and deterministic turns.
    /// Feature-specific operations are split across partial files.
    /// </summary>
    public sealed partial class RogueGameState
    {
        public RogueGameState(
            RogueMapState map,
            ActorId playerId,
            int playerSightRadius = 0
        )
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

            if (playerSightRadius < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(playerSightRadius)
                );
            }

            Turns = new TurnScheduler(playerId);
            PlayerSightRadius = playerSightRadius;
            Visibility = new VisibilityState();
            RefreshVisibility();
        }

        public RogueMapState Map { get; }
        public TurnScheduler Turns { get; }
        public VisibilityState Visibility { get; }
        public int PlayerSightRadius { get; }
        public bool IsFloorCompleted { get; private set; }
        public bool IsPlayerDefeated =>
            !Map.TryGetActor(Turns.PlayerId, out _);
        public bool IsTerminal =>
            IsPlayerDefeated || IsFloorCompleted;
        public RogueGameProgress Progress =>
            IsPlayerDefeated
                ? RogueGameProgress.PlayerDefeated
                : IsFloorCompleted
                    ? RogueGameProgress.FloorCompleted
                    : !Map.HasEnemies
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

        public bool RemoveActor(ActorId actorId)
        {
            bool removed = Map.RemoveActor(actorId);
            if (removed)
            {
                Turns.UnregisterEnemy(actorId);
            }

            return removed;
        }

        public void SynchronizeEnemyRoster()
        {
            for (int index = Turns.Enemies.Count - 1;
                 index >= 0;
                 index--)
            {
                ActorId enemyId = Turns.Enemies[index];
                if (!Map.TryGetActor(
                        enemyId,
                        out ActorState actor
                    ) ||
                    actor.Faction != ActorFaction.Enemy)
                {
                    Turns.UnregisterEnemy(enemyId);
                }
            }

            foreach (ActorState actor in Map.Actors)
            {
                if (actor.Faction == ActorFaction.Enemy)
                {
                    Turns.RegisterEnemy(actor.Id);
                }
            }
        }

        public RogueMoveResult ResolveMove(MoveAction action)
        {
            if (TryRejectTerminal(out ActionResolution terminal))
            {
                return RejectedMove(
                    MoveOutcome.GameEnded,
                    terminal,
                    action.ActorId
                );
            }

            if (action.ActorId != Turns.CurrentActor)
            {
                return RejectedMove(
                    MoveOutcome.NotActorsTurn,
                    ActionResolution.Rejected(
                        $"It is {Turns.CurrentActor}'s turn."
                    ),
                    action.ActorId
                );
            }

            if (action.ActorId == Turns.PlayerId)
            {
                SynchronizeEnemyRoster();
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
            if (action.ActorId == Turns.PlayerId)
            {
                RefreshVisibility();
            }

            return new RogueMoveResult(move, melee);
        }

        public WaitActionResult ResolveWait(WaitAction action)
        {
            if (TryRejectTerminal(out ActionResolution terminal))
            {
                return new WaitActionResult(
                    WaitOutcome.GameEnded,
                    terminal,
                    action.ActorId
                );
            }

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

            if (action.ActorId == Turns.PlayerId)
            {
                SynchronizeEnemyRoster();
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

        public void RefreshVisibility()
        {
            if (Map.TryGetActor(
                    Turns.PlayerId,
                    out ActorState player
                ))
            {
                Visibility.Recalculate(
                    Map,
                    player.Position,
                    PlayerSightRadius
                );
            }
        }

        private bool TryRejectTerminal(
            out ActionResolution resolution
        )
        {
            if (!IsTerminal)
            {
                resolution = default;
                return false;
            }

            string reason = IsPlayerDefeated
                ? "The player has been defeated."
                : "The floor has already been completed.";
            resolution = ActionResolution.Rejected(reason);
            return true;
        }

        private void SynchronizeBeforePlayerTurn(
            ActorId actorId,
            ActionResolution resolution
        )
        {
            if (actorId == Turns.PlayerId &&
                resolution.ConsumesTurn)
            {
                SynchronizeEnemyRoster();
            }
        }

        private RogueMoveResult RejectedMove(
            MoveOutcome outcome,
            ActionResolution resolution,
            ActorId actorId
        )
        {
            GridPosition position = default;
            if (Map.TryGetActor(actorId, out ActorState actor))
            {
                position = actor.Position;
            }

            return new RogueMoveResult(new MoveActionResult(
                outcome,
                resolution,
                position,
                position
            ));
        }
    }
}
