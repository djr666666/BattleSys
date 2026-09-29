using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 治疗模板:回复目标血量。回血量 = 施法者攻击 × 威力/100 × 受治疗系数(buff)。
/// 目标为空时治自己(适用于自身回血/吸血的回血部分)。
/// </summary>
public class Logic_Heal : ISkillLogic
{
    public async UniTask Execute(BattleUnit caster, BattleUnit target, SkillInfo skill)
    {
        var healTarget = target ?? caster;

        float baseHeal = caster.attCom.Atk * skill.skillData.power / 100f;
        float healMul  = 1f + (healTarget.attCom != null ? healTarget.attCom.HealTakenPctMod : 0) / 100f;
        int healAmount = Mathf.RoundToInt(baseHeal * healMul);

        DamageCalculator.ApplyHeal(healTarget, healAmount);   // 走改血咽喉

        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"{caster.Name} 为 {healTarget.Name} 回复 {healAmount} 生命" });
        EventManager.SendMessage(new BattleEventDefine.OnHealDealt { Unit = healTarget, Amount = healAmount, FontType = skill.skillData.fontTipType });

        await UniTask.CompletedTask;
    }
}
