using Game.Runtime.Data;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerBattleStatusWorldUI : MonoBehaviour
{
    [SerializeField] private GameObject statusContent;
    [SerializeField] private Image wrenchIcon;
    [SerializeField] private Image shieldIcon;
    [SerializeField] private Image heartIcon;
    [SerializeField] private HoverTooltipPresenter tooltip;

    private PlayerRunStats runStats;
    private bool combatVisible;

    public void Bind(PlayerRunStats stats)
    {
        if (runStats != null)
        {
            runStats.Changed -= Refresh;
        }

        runStats = stats;
        if (runStats != null)
        {
            runStats.Changed += Refresh;
        }
        Refresh();
    }

    public void SetCombatVisible(bool visible)
    {
        combatVisible = visible;
        if (!visible)
        {
            tooltip?.Hide();
        }
        Refresh();
    }

    private void Refresh()
    {
        bool hasWrench =
            combatVisible && runStats != null && runStats.TimedAttackBonus > 0;
        bool hasShield =
            combatVisible && runStats != null &&
            runStats.NextEnemyPhaseShield > 0;
        bool hasHeart =
            combatVisible && runStats != null && runStats.NegateNextAttack;

        ConfigureIcon(
            wrenchIcon,
            hasWrench,
            hasWrench ? $"扳手 +{runStats.TimedAttackBonus}" : string.Empty
        );
        ConfigureIcon(
            shieldIcon,
            hasShield,
            hasShield
                ? GameLocalization.Get(
                    "battle.status.shield",
                    runStats.NextEnemyPhaseShield
                )
                : string.Empty
        );
        ConfigureIcon(
            heartIcon,
            hasHeart,
            hasHeart ? "替伤 x1" : string.Empty
        );
        statusContent?.SetActive(hasWrench || hasShield || hasHeart);
    }

    private void ConfigureIcon(Image icon, bool visible, string label)
    {
        if (icon == null)
        {
            return;
        }

        icon.gameObject.SetActive(visible);
        HoverTooltipTarget target =
            icon.GetComponentInParent<HoverTooltipTarget>();
        target?.Bind(tooltip, label);
    }

    private void OnDestroy()
    {
        if (runStats != null)
        {
            runStats.Changed -= Refresh;
        }
    }
}
