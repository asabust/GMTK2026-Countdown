using Game.Runtime.Data;
using UnityEngine;

public enum PlayerSkillType
{
    BasicAttack,
    Bloodlust,
    Parasite,
    Revenge
}

[CreateAssetMenu(
    fileName = "SkillDefinition",
    menuName = "Zero/Skill Definition"
)]
public sealed class SkillDefinition : ScriptableObject
{
    [SerializeField] private string skillId;
    [SerializeField] private string displayName;
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private PlayerSkillType skillType;
    [SerializeField, Min(0)] private int numberCost;
    [SerializeField, Min(0)] private int baseDamage;
    [SerializeField, Min(0)] private int cooldownTurns;
    [SerializeField, Min(1)] private int bloodlustBasicAttacks = 2;
    [SerializeField, Min(1f)] private float bloodlustMultiplier = 2f;
    [SerializeField, Min(0)] private int killRestore;
    [SerializeField, Min(1)] private int minimumHits = 2;
    [SerializeField, Min(1)] private int maximumHits = 3;
    [SerializeField, Range(0f, 1f)] private float extraHitChance = 0.5f;

    public string SkillId => skillId;
    public string DisplayName => GameLocalization.GetOrDefault(
        $"skill.{skillId}.name",
        displayName
    );
    public string Description =>
        skillType == PlayerSkillType.Revenge
            ? GameLocalization.GetOrDefault(
                $"skill.{skillId}.description",
                description,
                numberCost,
                minimumHits,
                maximumHits,
                baseDamage,
                cooldownTurns
            )
            : GameLocalization.GetOrDefault(
                $"skill.{skillId}.description",
                description
            );
    public Sprite Icon => icon;
    public PlayerSkillType SkillType => skillType;
    public int NumberCost => numberCost;
    public int BaseDamage => baseDamage;
    public int CooldownTurns => cooldownTurns;
    public int BloodlustBasicAttacks => bloodlustBasicAttacks;
    public float BloodlustMultiplier => bloodlustMultiplier;
    public int KillRestore => killRestore;
    public int MinimumHits => minimumHits;
    public int MaximumHits => maximumHits;
    public float ExtraHitChance => extraHitChance;

    private void OnValidate()
    {
        numberCost = Mathf.Max(0, numberCost);
        baseDamage = Mathf.Max(0, baseDamage);
        cooldownTurns = Mathf.Max(0, cooldownTurns);
        bloodlustBasicAttacks = Mathf.Max(1, bloodlustBasicAttacks);
        bloodlustMultiplier = Mathf.Max(1f, bloodlustMultiplier);
        killRestore = Mathf.Max(0, killRestore);
        minimumHits = Mathf.Max(1, minimumHits);
        maximumHits = Mathf.Max(minimumHits, maximumHits);
    }
}
