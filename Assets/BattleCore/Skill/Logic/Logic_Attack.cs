using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 普通攻击模板:算伤害 → 触发攻击者 OnHit buff(改伤害) → 扣血 → 触发目标 OnBehurt buff → 死亡/受重击判断。
/// 这就是"一次伤害的完整流水线",把 DamageCalculator(算/扣)和 buff 时机(OnHit/OnBehurt)串起来。
/// </summary>
public class Logic_Attack : ISkillLogic
{
    // 受重击存活阈值:单次伤害 ≥ 目标最大血的这个比例,算"受重击"(意境事件用)
    private const float SURVIVE_HEAVY_PCT = 0.25f;

    public async UniTask Execute(BattleUnit caster, BattleUnit target, SkillInfo skill)
    {
        if (target == null) { Debug.LogWarning("[Logic_Attack] 目标为空"); return; }

        // 1. 算基础伤害 → 装进 DamageInfo 盒子
        int baseDamage = DamageCalculator.Calculate(caster, target, skill);
        var dmgInfo = new DamageInfo { creator = caster, target = target, damage = baseDamage };

        // 2. 攻击者 OnHit buff(剑势增伤/吸血…)在这里改 dmgInfo.damage
        caster.buffCom.TriggerOnHit(caster, dmgInfo);

        // 3. OnHit 之后的最终伤害 → 扣血(走改血咽喉)
        int damage = Mathf.Max(1, Mathf.RoundToInt(dmgInfo.damage));
        DamageCalculator.ApplyDamage(target, damage);

        // 4. 播报 + 飘字(颜色由技能配表 fontTipType 决定)
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"{caster.Name} 对 {target.Name} 造成 {damage} 伤害" });
        EventManager.SendMessage(new BattleEventDefine.OnDamageDealt { Unit = target, Amount = damage, FontType = skill.skillData.fontTipType });

        // 5. 目标 OnBehurt buff(荆棘反弹/护盾…)
        target.buffCom.TriggerOnBehurt(target, dmgInfo);

        // 6. 死亡 / 受重击判断
        if (target.attCom.CurHp <= 0)
        {
            target.buffCom.TriggerOnKill(target);
            caster.buffCom.TriggerOnBeKill(caster, target);
            // ── 意境系统专用(游戏专属,还没搬),做意境时打开 ──
            // EventManager.SendMessage(new BattleEventDefine.OnCombatEvent { Type = IntentEventType.Kill, Source = caster });
        }
        else if (damage >= Mathf.RoundToInt(target.attCom.MaxHp * SURVIVE_HEAVY_PCT))
        {
            // EventManager.SendMessage(new BattleEventDefine.OnCombatEvent { Type = IntentEventType.SurviveHeavy, Source = target });
        }

        await UniTask.CompletedTask;
    }
}
