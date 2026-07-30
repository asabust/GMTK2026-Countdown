# Rogue Prototype

This folder is an isolated gameplay experiment. It must not change the
behaviour of the completed game until the prototype direction is approved.

## Boundaries

- `Domain` contains deterministic game rules and does not reference Unity.
- `Unity` adapts domain state to Unity input, scenes, animation, and audio.
- `Editor` contains prototype-only authoring helpers.
- `Tests` contains fast EditMode tests for the domain rules.

The prototype scene is `Assets/Scenes/RoguePrototype.unity`. It is deliberately
excluded from Build Settings.

## First vertical slice

1. Submit a move or wait command.
2. Resolve one player action through `TurnScheduler`, followed by enemy
   actions in deterministic registration order.
3. Render the result without putting presentation code in `Domain`.
4. Add one pickup, one hostile actor, fog of war, and stairs.

Do not migrate the existing encounter, item, or enemy systems until this slice
has proved the direction.

## Current domain rules

- A rejected action does not consume a turn.
- A consumed player turn begins the enemy phase.
- Each registered enemy acts at most once in that phase.
- Enemies removed before their pending action are skipped.
- Enemies added during an enemy phase first act in the next round.
- Maps are rectangular, start walkable, and allow one actor per cell.
- Movement is limited to one cardinal cell.
- Walls and map boundaries reject movement without consuming a turn.
- Moving into an empty cell updates the actor position and consumes a turn.
- Moving into a hostile actor produces a melee-attack result without moving.
- Actors have maximum health, current health, and attack power.
- A cardinally adjacent hostile target takes damage equal to attack power.
- Defeated actors are removed from both the map and pending enemy turns.
- `RogueGameState` is the single entry point that coordinates movement,
  bump attacks, death cleanup, and turn advancement.
- Waiting consumes a turn without changing position.
- A submitted player move or wait can resolve the complete enemy phase.
- Enemies deterministically approach the player by one cardinal cell, try
  the other axis when blocked, and wait when neither approach is possible.
- Domain progress reports ongoing play, a cleared floor, or player defeat.
