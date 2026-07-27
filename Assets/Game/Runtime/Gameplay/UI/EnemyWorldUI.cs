using Game.Runtime.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EnemyWorldUI : MonoBehaviour
{
    [SerializeField] private GameObject rewardRoot;
    [SerializeField] private TMP_Text rewardText;
    [SerializeField] private GameObject healthRoot;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image intentImage;
    [SerializeField] private GameObject intentRoot;
    [SerializeField] private TMP_Text intentText;

    [Header("Intent icons")]
    [SerializeField] private Sprite basicAttackIntentSprite;
    [SerializeField] private Sprite drinkIntentSprite;
    [SerializeField] private Sprite waitIntentSprite;
    [SerializeField] private Sprite heavyAttackIntentSprite;
    [SerializeField] private Sprite chargeIntentSprite;
    [SerializeField] private Sprite stealIntentSprite;
    [SerializeField] private Sprite escapeIntentSprite;

    private void Awake()
    {
        EnsureIntentHover();
        HideRewardAndIntent();
    }

    private void OnEnable()
    {
        HideRewardAndIntent();
    }

    public void ShowExploration(string rewardDescription)
    {
        rewardRoot?.SetActive(false);
        healthRoot?.SetActive(false);
        HideIntent();
    }

    public void ShowCombat(
        int currentHP,
        int maxHP,
        string rewardDescription
    )
    {
        rewardRoot?.SetActive(false);
        healthRoot?.SetActive(true);
        intentRoot?.SetActive(false);

        if (healthFill != null)
        {
            healthFill.fillAmount = maxHP > 0
                ? Mathf.Clamp01((float)currentHP / maxHP)
                : 0f;
        }

        if (healthText != null)
        {
            healthText.text = GameLocalization.Get(
                "enemy.world.health",
                currentHP,
                maxHP
            );
        }
    }

    public void ShowIntent(
        EnemyBehaviorType behaviorType,
        EnemyIntentType intentType,
        string description
    )
    {
        intentRoot?.SetActive(false);
        if (intentText != null)
        {
            intentText.text = description ?? string.Empty;
        }

        if (intentImage == null)
        {
            return;
        }

        Sprite sprite = GetIntentSprite(behaviorType, intentType);
        intentImage.sprite = sprite;
        intentImage.gameObject.SetActive(sprite != null);
    }

    public void HideIntent()
    {
        intentRoot?.SetActive(false);
        intentImage?.gameObject.SetActive(false);
    }

    public void HideAll()
    {
        rewardRoot?.SetActive(false);
        healthRoot?.SetActive(false);
        intentRoot?.SetActive(false);
    }

    private void HideRewardAndIntent()
    {
        rewardRoot?.SetActive(false);
        HideIntent();
    }

    private void EnsureIntentHover()
    {
        if (intentImage == null)
        {
            return;
        }

        if (GetComponent<GraphicRaycaster>() == null)
        {
            gameObject.AddComponent<GraphicRaycaster>();
        }

        intentImage.raycastTarget = true;
        BattleSkillHoverTarget hoverTarget =
            intentImage.GetComponent<BattleSkillHoverTarget>();
        if (hoverTarget == null)
        {
            hoverTarget = intentImage.gameObject
                .AddComponent<BattleSkillHoverTarget>();
        }
        hoverTarget.Bind(ShowIntentDescription, HideIntentDescription);
    }

    private void ShowIntentDescription()
    {
        if (intentImage != null &&
            intentImage.gameObject.activeInHierarchy &&
            intentText != null &&
            !string.IsNullOrWhiteSpace(intentText.text))
        {
            intentRoot?.SetActive(true);
        }
    }

    private void HideIntentDescription()
    {
        intentRoot?.SetActive(false);
    }

    private Sprite GetIntentSprite(
        EnemyBehaviorType behaviorType,
        EnemyIntentType intentType
    )
    {
        return intentType switch
        {
            EnemyIntentType.Attack => basicAttackIntentSprite,
            EnemyIntentType.Wait => waitIntentSprite,
            EnemyIntentType.Charge => chargeIntentSprite,
            EnemyIntentType.Steal => stealIntentSprite,
            EnemyIntentType.StealItem => stealIntentSprite,
            EnemyIntentType.Special
                when behaviorType == EnemyBehaviorType.DrunkenRaider =>
                drinkIntentSprite,
            EnemyIntentType.Special
                when behaviorType == EnemyBehaviorType.Hamster =>
                escapeIntentSprite,
            EnemyIntentType.Special => heavyAttackIntentSprite,
            _ => waitIntentSprite
        };
    }
}
