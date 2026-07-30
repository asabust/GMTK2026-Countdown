using System;
using System.Collections.Generic;

namespace Game.Experimental.Rogue.Domain
{
    public enum TurnPhase
    {
        AwaitingPlayer,
        ResolvingEnemies
    }

    /// <summary>
    /// Deterministic round order for the prototype.
    /// A round consists of one consumed player turn followed by one turn for
    /// every enemy that existed when the enemy phase began.
    /// </summary>
    public sealed class TurnScheduler
    {
        private readonly List<ActorId> enemies = new();
        private readonly Queue<ActorId> pendingEnemies = new();

        public TurnScheduler(ActorId playerId)
        {
            if (string.IsNullOrWhiteSpace(playerId.Value))
            {
                throw new ArgumentException(
                    "Player ID must be valid.",
                    nameof(playerId)
                );
            }

            PlayerId = playerId;
            CurrentActor = playerId;
        }

        public ActorId PlayerId { get; }
        public ActorId CurrentActor { get; private set; }
        public TurnPhase Phase { get; private set; } =
            TurnPhase.AwaitingPlayer;
        public int RoundNumber { get; private set; } = 1;
        public IReadOnlyList<ActorId> Enemies => enemies;

        public bool RegisterEnemy(ActorId enemyId)
        {
            ValidateEnemyId(enemyId);
            if (enemies.Contains(enemyId))
            {
                return false;
            }

            enemies.Add(enemyId);
            return true;
        }

        public bool UnregisterEnemy(ActorId enemyId) =>
            enemies.Remove(enemyId);

        public void CompleteAction(
            ActorId actorId,
            ActionResolution resolution
        )
        {
            if (actorId != CurrentActor)
            {
                throw new InvalidOperationException(
                    $"It is {CurrentActor}'s turn, not {actorId}'s."
                );
            }

            if (!resolution.ConsumesTurn)
            {
                return;
            }

            if (Phase == TurnPhase.AwaitingPlayer)
            {
                BeginEnemyPhase();
                return;
            }

            AdvanceEnemyPhase();
        }

        private void BeginEnemyPhase()
        {
            pendingEnemies.Clear();
            foreach (ActorId enemy in enemies)
            {
                pendingEnemies.Enqueue(enemy);
            }

            Phase = TurnPhase.ResolvingEnemies;
            AdvanceEnemyPhase();
        }

        private void AdvanceEnemyPhase()
        {
            while (pendingEnemies.Count > 0)
            {
                ActorId next = pendingEnemies.Dequeue();
                if (!enemies.Contains(next))
                {
                    continue;
                }

                CurrentActor = next;
                return;
            }

            RoundNumber++;
            Phase = TurnPhase.AwaitingPlayer;
            CurrentActor = PlayerId;
        }

        private void ValidateEnemyId(ActorId enemyId)
        {
            if (string.IsNullOrWhiteSpace(enemyId.Value))
            {
                throw new ArgumentException(
                    "Enemy ID must be valid.",
                    nameof(enemyId)
                );
            }

            if (enemyId == PlayerId)
            {
                throw new ArgumentException(
                    "The player cannot be registered as an enemy.",
                    nameof(enemyId)
                );
            }
        }
    }
}
