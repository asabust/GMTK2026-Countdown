using Game.Runtime.Data;
using TMPro;
using UnityEngine.UI;

public static class UILocalization
{
    public static void SetButtonText(Button button, string key)
    {
        TMP_Text label = button?.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            label.text = GameLocalization.Get(key);
        }
    }
}
