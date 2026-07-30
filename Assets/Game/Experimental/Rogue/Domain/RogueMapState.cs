using System;
using System.Collections.Generic;

namespace Game.Experimental.Rogue.Domain
{
    /// <summary>
    /// Minimal rectangular map containing terrain and one actor per cell.
    /// Every cell starts walkable.
    /// </summary>
    public sealed class RogueMapState
    {
        private readonly bool[,] walkable;
        private readonly Dictionary<ActorId, ActorState> actorsById = new();
        private readonly Dictionary<GridPosition, ActorId> actorsByPosition =
            new();

        public RogueMapState(int width, int height)
        {
            if (width <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(width),
                    "Map width must be positive."
                );
            }

            if (height <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(height),
                    "Map height must be positive."
                );
            }

            Width = width;
            Height = height;
            walkable = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    walkable[x, y] = true;
                }
            }
        }

        public int Width { get; }
        public int Height { get; }
        public IReadOnlyCollection<ActorState> Actors => actorsById.Values;

        public bool IsInBounds(GridPosition position) =>
            position.X >= 0 &&
            position.X < Width &&
            position.Y >= 0 &&
            position.Y < Height;

        public bool IsWalkable(GridPosition position) =>
            IsInBounds(position) &&
            walkable[position.X, position.Y];

        public void SetWalkable(GridPosition position, bool value)
        {
            EnsureInBounds(position);
            if (!value && actorsByPosition.ContainsKey(position))
            {
                throw new InvalidOperationException(
                    $"Cannot block occupied cell {position}."
                );
            }

            walkable[position.X, position.Y] = value;
        }

        public bool TryAddActor(ActorState actor)
        {
            if (actor == null)
            {
                return false;
            }

            if (!IsWalkable(actor.Position) ||
                actorsById.ContainsKey(actor.Id) ||
                actorsByPosition.ContainsKey(actor.Position))
            {
                return false;
            }

            actorsById.Add(actor.Id, actor);
            actorsByPosition.Add(actor.Position, actor.Id);
            return true;
        }

        public bool RemoveActor(ActorId actorId)
        {
            if (!actorsById.TryGetValue(actorId, out ActorState actor))
            {
                return false;
            }

            actorsById.Remove(actorId);
            actorsByPosition.Remove(actor.Position);
            return true;
        }

        public bool TryGetActor(
            ActorId actorId,
            out ActorState actor
        ) => actorsById.TryGetValue(actorId, out actor);

        public bool TryGetActorAt(
            GridPosition position,
            out ActorState actor
        )
        {
            actor = null;
            return actorsByPosition.TryGetValue(
                       position,
                       out ActorId actorId
                   ) &&
                   actorsById.TryGetValue(actorId, out actor);
        }

        internal bool TryMoveActor(
            ActorId actorId,
            GridPosition destination
        )
        {
            if (!actorsById.TryGetValue(actorId, out ActorState actor) ||
                !IsWalkable(destination) ||
                actorsByPosition.ContainsKey(destination))
            {
                return false;
            }

            actorsByPosition.Remove(actor.Position);
            actor.Position = destination;
            actorsByPosition.Add(destination, actorId);
            return true;
        }

        private void EnsureInBounds(GridPosition position)
        {
            if (!IsInBounds(position))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    $"Cell {position} is outside the map."
                );
            }
        }
    }
}
