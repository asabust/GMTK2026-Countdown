using System;
using UnityEngine;

public sealed class WorldInteractionContext
{
    private readonly Action<bool> complete;

    public WorldInteractionContext(
        PlayerGridController player,
        NumberResource numberResource,
        Vector3 worldPosition,
        Action<bool> complete
    )
    {
        Player = player;
        NumberResource = numberResource;
        WorldPosition = worldPosition;
        this.complete = complete;
    }

    public PlayerGridController Player { get; }
    public NumberResource NumberResource { get; }
    public Vector3 WorldPosition { get; }

    public void Complete(bool consume = true)
    {
        complete?.Invoke(consume);
    }
}

[RequireComponent(typeof(GridObject))]
[RequireComponent(typeof(FogVisibilityTarget))]
public abstract class WorldInteractable : MonoBehaviour
{
    private bool isInteractionOpen;
    private bool isConsumed;

    public bool IsInteractionOpen => isInteractionOpen;
    public bool IsConsumed => isConsumed;

    internal bool TryBeginInteraction(WorldInteractionContext context)
    {
        if (context == null || isInteractionOpen || isConsumed)
        {
            return false;
        }

        isInteractionOpen = true;
        if (OpenInteraction(context))
        {
            return true;
        }

        isInteractionOpen = false;
        return false;
    }

    internal void FinishInteraction(bool consume)
    {
        if (!isInteractionOpen)
        {
            return;
        }

        isInteractionOpen = false;
        CloseInteraction();

        if (!consume || isConsumed)
        {
            return;
        }

        isConsumed = true;
        GetComponent<GridObject>().ReleaseAndDestroy();
    }

    protected abstract bool OpenInteraction(WorldInteractionContext context);

    protected abstract void CloseInteraction();
}
