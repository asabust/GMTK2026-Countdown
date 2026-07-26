using Game.Runtime.Data;
using TMPro;
using UnityEngine;

[CreateAssetMenu(
    fileName = "LocalizationFontSettings",
    menuName = "Zero/UI/Localization Font Settings"
)]
public sealed class LocalizationFontSettings : ScriptableObject
{
    [SerializeField] private TMP_FontAsset englishFont;
    [SerializeField] private TMP_FontAsset cjkFont;

    public TMP_FontAsset GetFont(Language language) =>
        language == Language.English ? englishFont : cjkFont;
}
