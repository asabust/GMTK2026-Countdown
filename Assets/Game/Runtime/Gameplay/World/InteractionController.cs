using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(PlayerGridController))]
[RequireComponent(typeof(NumberResource))]
public sealed class InteractionController : MonoBehaviour
{
    private PlayerGridController playerController;
    private NumberResource numberResource;
    private WorldInteractable currentInteractable;

    public bool IsInteracting => currentInteractable != null;
    public WorldInteractable CurrentInteractable => currentInteractable;

    private void Awake()
    {
        playerController = GetComponent<PlayerGridController>();
        numberResource = GetComponent<NumberResource>();
    }

    private void OnEnable()
    {
        if (playerController != null)
        {
            playerController.ContactRequested += HandleContactRequested;
        }
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.ContactRequested -= HandleContactRequested;
        }

        CancelActiveInteraction();
    }

    public void PrepareForSceneTransition()
    {
        CancelActiveInteraction();
        if (playerController != null)
        {
            playerController.SetExternalInputLocked(true);
        }
    }

    private void HandleContactRequested(
        GridEntity target,
        GridOccupantType occupantType
    )
    {
        if (occupantType != GridOccupantType.Interactable ||
            currentInteractable != null)
        {
            return;
        }

        WorldInteractable interactable =
            target != null ? target.GetComponent<WorldInteractable>() : null;
        if (interactable == null)
        {
            Debug.LogError(
                $"Interactable grid object '{target?.name}' has no " +
                $"{nameof(WorldInteractable)} component.",
                target
            );
            playerController.CompleteContact();
            return;
        }

        currentInteractable = interactable;
        playerController.SetExternalInputLocked(true);
        WorldInteractionContext context = new(
            playerController,
            numberResource,
            interactable.transform.position,
            consume => CompleteInteraction(interactable, consume)
        );

        if (interactable.TryBeginInteraction(context))
        {
            return;
        }

        currentInteractable = null;
        playerController.SetExternalInputLocked(false);
        playerController.CompleteContact();
    }

    private void CompleteInteraction(
        WorldInteractable interactable,
        bool consume
    )
    {
        if (currentInteractable == null ||
            currentInteractable != interactable)
        {
            return;
        }

        currentInteractable = null;
        interactable.FinishInteraction(consume);
        playerController.SetExternalInputLocked(false);
        playerController.CompleteContact();
    }

    private void CancelActiveInteraction()
    {
        if (currentInteractable != null)
        {
            currentInteractable.FinishInteraction(consume: false);
            currentInteractable = null;
        }

        if (playerController != null)
        {
            playerController.CompleteContact();
        }
    }
}
