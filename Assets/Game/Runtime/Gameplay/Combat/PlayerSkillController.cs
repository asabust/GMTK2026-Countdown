using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class LearnedSkillState
{
    [SerializeField] private SkillDefinition definition;
    [SerializeField, Min(0)] private int cooldownRemaining;

    public LearnedSkillState(SkillDefinition definition)
    {
        this.definition = definition;
    }

    public SkillDefinition Definition => definition;
    public int CooldownRemaining => cooldownRemaining;
    internal void SetCooldown(int value) =>
        cooldownRemaining = Mathf.Max(0, value);
}

[DisallowMultipleComponent]
public sealed class PlayerSkillController : MonoBehaviour
{
    [SerializeField] private List<LearnedSkillState> learnedSkills = new();

    public event Action Changed;

    public IReadOnlyList<LearnedSkillState> GetLearnedSkills() =>
        learnedSkills;

    public bool Owns(SkillDefinition definition) =>
        FindState(definition) != null;

    public bool Learn(SkillDefinition definition)
    {
        if (definition == null ||
            definition.SkillType == PlayerSkillType.BasicAttack ||
            string.IsNullOrWhiteSpace(definition.SkillId) ||
            Owns(definition))
        {
            return false;
        }

        learnedSkills.Add(new LearnedSkillState(definition));
        Changed?.Invoke();
        return true;
    }

    public int GetCooldown(SkillDefinition definition) =>
        FindState(definition)?.CooldownRemaining ?? 0;

    public void CompletePlayerAction(SkillDefinition usedSkill = null)
    {
        bool changed = false;
        foreach (LearnedSkillState state in learnedSkills)
        {
            if (state?.Definition == null)
            {
                continue;
            }

            if (state.Definition == usedSkill)
            {
                int cooldown = usedSkill.CooldownTurns;
                if (state.CooldownRemaining != cooldown)
                {
                    state.SetCooldown(cooldown);
                    changed = true;
                }
                continue;
            }

            if (state.CooldownRemaining > 0)
            {
                state.SetCooldown(state.CooldownRemaining - 1);
                changed = true;
            }
        }

        if (changed)
        {
            Changed?.Invoke();
        }
    }

    public void ResetForNewRun()
    {
        if (learnedSkills.Count == 0)
        {
            return;
        }

        learnedSkills.Clear();
        Changed?.Invoke();
    }

    private LearnedSkillState FindState(SkillDefinition definition)
    {
        if (definition == null)
        {
            return null;
        }

        return learnedSkills.Find(state =>
            state?.Definition != null &&
            state.Definition.SkillId == definition.SkillId);
    }
}
