using System;
using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitlePanel : MonoBehaviour
{
    public Button startButton;
    [SerializeField] private Button languageButton;
    [SerializeField] private TMP_Text startButtonText;
    [SerializeField] private TMP_Text languageButtonText;

    private void Start()
    {
        if (languageButton == null)
        {
            languageButton = transform.Find("Setting")?.GetComponent<Button>();
        }
        startButtonText ??= startButton?.GetComponentInChildren<TMP_Text>(true);
        languageButtonText ??=
            languageButton?.GetComponentInChildren<TMP_Text>(true);

        startButton?.onClick.AddListener(StartNewGame);
        languageButton?.onClick.AddListener(CycleLanguage);
        GameLocalization.LanguageChanged += RefreshText;
        RefreshText();
    }

    private void OnDestroy()
    {
        startButton?.onClick.RemoveListener(StartNewGame);
        languageButton?.onClick.RemoveListener(CycleLanguage);
        GameLocalization.LanguageChanged -= RefreshText;
    }

    private static void StartNewGame()
    {
        GameManager.Instance?.StartNewGame();
    }

    private static void CycleLanguage()
    {
        Language next = GameLocalization.CurrentLanguage switch
        {
            Language.Chinese => Language.English,
            Language.English => Language.Japanese,
            _ => Language.Chinese
        };
        GameLocalization.SetLanguage(next);
    }

    private void RefreshText()
    {
        if (startButtonText != null)
        {
            startButtonText.text = GameLocalization.Get("title.new_game");
        }
        if (languageButtonText != null)
        {
            languageButtonText.text = GameLocalization.Get(
                "title.language",
                GameLocalization.Get(
                    $"language.{GameLocalization.CurrentLanguage.ToString().ToLowerInvariant()}"
                )
            );
        }
    }
}
