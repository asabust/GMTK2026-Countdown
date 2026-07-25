using TMPro;
using UnityEngine;

public sealed class HoverTooltipPresenter : MonoBehaviour
{
    [SerializeField] private GameObject tooltipRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;

    private void Awake() => Hide();

    public void Show(string title, string description)
    {
        if (titleText != null)
        {
            titleText.text = title ?? string.Empty;
        }
        if (descriptionText != null)
        {
            descriptionText.text = description ?? string.Empty;
            descriptionText.gameObject.SetActive(
                !string.IsNullOrWhiteSpace(description)
            );
        }
        tooltipRoot?.SetActive(true);
    }

    public void Hide() => tooltipRoot?.SetActive(false);
}
