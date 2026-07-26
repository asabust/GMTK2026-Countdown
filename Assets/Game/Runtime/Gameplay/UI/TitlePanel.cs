using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitlePanel : MonoBehaviour
{
    [SerializeField] private Button startButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private TMP_Text startButtonText;
    [SerializeField] private TMP_Text quitButtonText;
    [SerializeField] private TMP_Text settingsButtonText;

    private void Start()
    {
        startButton ??= transform.Find("NewGame")?.GetComponent<Button>();
        quitButton ??= transform.Find("Quit")?.GetComponent<Button>();
        settingsButton ??= transform.Find("Setting")?.GetComponent<Button>();
        startButtonText ??= startButton?.GetComponentInChildren<TMP_Text>(true);
        quitButtonText ??= quitButton?.GetComponentInChildren<TMP_Text>(true);
        settingsButtonText ??=
            settingsButton?.GetComponentInChildren<TMP_Text>(true);

        startButton?.onClick.AddListener(StartNewGame);
        quitButton?.onClick.AddListener(QuitGame);
        settingsButton?.onClick.AddListener(ToggleSettings);
        GameLocalization.LanguageChanged += RefreshText;
        RefreshText();
    }

    private void OnDestroy()
    {
        startButton?.onClick.RemoveListener(StartNewGame);
        quitButton?.onClick.RemoveListener(QuitGame);
        settingsButton?.onClick.RemoveListener(ToggleSettings);
        GameLocalization.LanguageChanged -= RefreshText;
    }

    private static void StartNewGame()
    {
        GameManager.Instance?.StartNewGame();
    }

    private static void QuitGame()
    {
        GameManager.Instance?.QuitGame();
    }

    private static void ToggleSettings()
    {
        if (UIManager.Instance == null)
        {
            return;
        }

        if (UIManager.Instance.IsPanelOpen<SettingsPanel>())
        {
            UIManager.Instance.Close<SettingsPanel>();
        }
        else
        {
            UIManager.Instance.Open<SettingsPanel>();
        }
    }

    private void RefreshText()
    {
        if (startButtonText != null)
        {
            startButtonText.text = GameLocalization.Get("title.new_game");
        }
        if (quitButtonText != null)
        {
            quitButtonText.text = GameLocalization.Get("title.quit");
        }
        if (settingsButtonText != null)
        {
            settingsButtonText.text =
                GameLocalization.Get("title.settings");
        }
    }
}
