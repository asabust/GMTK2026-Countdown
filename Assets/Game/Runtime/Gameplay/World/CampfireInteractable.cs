using Game.Runtime.Core;
using Game.Runtime.Data;
using UnityEngine;

public sealed class CampfireInteractable : WorldInteractable
{
    [SerializeField, Min(0)] private int minimumRestore = 5;
    [SerializeField, Min(0)] private int maximumRestore = 10;

    private WorldInteractionContext context;
    private bool submitted;

    public int MinimumRestore => minimumRestore;
    public int MaximumRestore => maximumRestore;

    protected override bool OpenInteraction(WorldInteractionContext interaction)
    {
        context = interaction;
        submitted = false;
        AudioManager.Instance?.PlaySFX(AudioName.UiInteractRest);

        CampfirePanel panel = UIManager.Instance?.Open<CampfirePanel>(
            new CampfireRequest(
                minimumRestore,
                maximumRestore,
                interaction.NumberResource,
                Rest,
                Leave
            )
        );
        return panel != null;
    }

    protected override void CloseInteraction()
    {
        UIManager.Instance?.Close<CampfirePanel>();
        context = null;
        submitted = false;
    }

    private void Rest()
    {
        if (submitted || context == null)
        {
            return;
        }

        submitted = true;
        int restoreAmount = GameRandom.RangeInclusive(
            minimumRestore,
            maximumRestore
        );
        NumberResource numberResource = context.NumberResource;
        int previousValue = numberResource != null
            ? numberResource.CurrentValue
            : 0;
        numberResource?.Add(
            restoreAmount,
            NumberChangeReason.Campfire,
            context.WorldPosition
        );
        int actualRestore = numberResource != null
            ? Mathf.Max(0, numberResource.CurrentValue - previousValue)
            : 0;
        context.Complete();
        ToastPanel.Show(GameLocalization.Get(
            "campfire.result",
            actualRestore
        ));
    }

    private void Leave()
    {
        if (submitted || context == null)
        {
            return;
        }

        submitted = true;
        context.Complete(consume: false);
    }

    private void OnValidate()
    {
        minimumRestore = Mathf.Max(0, minimumRestore);
        maximumRestore = Mathf.Max(minimumRestore, maximumRestore);
    }
}
