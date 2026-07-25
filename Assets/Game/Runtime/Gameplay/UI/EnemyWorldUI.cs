using TMPro;
using UnityEngine;

public class EnemyWorldUI : MonoBehaviour
{
    [SerializeField] private GameObject rewardRoot;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private GameObject healthRoot;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private GameObject intentRoot;
    [SerializeField] private TMP_Text intentText;

    public void ShowExploration(string rewardDescription)
    {
        rewardRoot?.SetActive(true);
        healthRoot?.SetActive(false);
        intentRoot?.SetActive(false);
        if (rewardText != null)
        {
            rewardText.text = $"掉落 {rewardDescription}";
        }
    }

    public void ShowCombat(
        int currentHP,
        int maxHP,
        string rewardDescription
    )
    {
        rewardRoot?.SetActive(true);
        healthRoot?.SetActive(true);
        intentRoot?.SetActive(false);

        if (rewardText != null)
        {
            rewardText.text = $"掉落 {rewardDescription}";
        }

        if (healthText != null)
        {
            healthText.text = $"HP {currentHP}/{maxHP}";
        }
    }

    public void ShowIntent(string description)
    {
        intentRoot?.SetActive(true);
        if (intentText != null)
        {
            intentText.text = description;
        }
    }

    public void HideIntent()
    {
        intentRoot?.SetActive(false);
    }

    public void HideAll()
    {
        rewardRoot?.SetActive(false);
        healthRoot?.SetActive(false);
        intentRoot?.SetActive(false);
    }
}
