using UnityEngine;
using UnityEngine.EventSystems;

public sealed class HoverTooltipTarget :
    MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler
{
    private HoverTooltipPresenter presenter;
    private string title;
    private string description;

    public void Bind(
        HoverTooltipPresenter tooltipPresenter,
        string tooltipTitle,
        string tooltipDescription = ""
    )
    {
        presenter = tooltipPresenter;
        title = tooltipTitle;
        description = tooltipDescription;
        enabled = presenter != null && !string.IsNullOrWhiteSpace(title);
        if (!enabled)
        {
            presenter?.Hide();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (enabled)
        {
            presenter?.Show(title, description);
        }
    }

    public void OnPointerExit(PointerEventData eventData) => presenter?.Hide();

    private void OnDisable() => presenter?.Hide();
}
