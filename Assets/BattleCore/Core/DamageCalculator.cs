using UnityEngine;

/// <summary>
/// ═══════════════ 伤害计算器 DamageCalculator(总纲) ═══════════════
/// 纯静态工具类,不持有状态。战斗里【所有算伤害 / 改血】都走这里,是"改血唯一咽喉"。
///
/// 三个对外接口:
///   Calculate(攻,受,技能) → 算出最终伤害数字(攻击×倍率×暴击×防御×易伤)
///   ApplyDamage(受, 伤害) → 真正扣血(先扣护盾,再扣HP)+ 发 OnUnitHpChanged
///   ApplyHeal(受, 回血)   → 真正加血(不超MaxHp)+ 发 OnUnitHpChanged
///
/// 为什么"只走这一处":血量变化集中一个入口,飘字/血条刷新/死亡判定都好挂钩子,不会漏。
/// 注意:剑势等 buff 增伤【不在这里】——它们走 Mod_SwordPower.OnHit 改 DamageInfo.damage,
///       本计算器不认识剑势,保持"通用计算 + buff 各自加成"解耦。
/// 公式可按你的数值设计随时调,全集中在这一个文件。
/// ═════════════════════════════════════════════════════════════
/// </summary>
public static class DamageCalculator
{
    // ── 对外接口 ──

    /// <summary>计算最终伤害(含命中/暴击/防御减免/易伤)。未命中返回 0,最低 1 点。</summary>
    public static int Calculate(BattleUnit attacker, BattleUnit target, SkillInfo skill)
    {
        if (attacker == null || target == null || skill == null) return 0;

        // 1. 攻击(含buff攻击%) × 技能倍率 × 输出加成(buff输出%)
        float atk = attacker.attCom.Atk * (1f + attacker.attCom.AtkPctMod / 100f);
        float outMul = 1f + attacker.attCom.DmgDealtPctMod / 100f;
        float baseDamage = atk * GetSkillMultiplier(skill) * outMul;

        // 2. 命中判断:没中直接 0
        if (!RollHit(attacker, target)) return 0;

        // 3. 暴击判断
        bool isCrit = RollCrit(attacker);
        if (isCrit) baseDamage *= GetCritMultiplier(attacker);

        // 4. 防御减免(含buff防御%)
        int def = Mathf.RoundToInt(target.attCom.Def * (1f + target.attCom.DefPctMod / 100f));
        float finalDamage = ApplyDefense(baseDamage, def);

        // 5. 受伤系数(易伤+/减伤-)
        finalDamage *= 1f + target.attCom.DmgTakenPctMod / 100f;

        // 6. 最低 1 点,不允许 0/负
        int result = Mathf.Max(1, Mathf.RoundToInt(finalDamage));

        if (isCrit)
        {
            Debug.Log($"[暴击] {attacker.Name} → {target.Name}，伤害 {result}");
            EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"💥 {attacker.Name} 暴击！{result} 伤害" });

            // ── 意境系统专用(游戏专属,IntentEventType/OnCombatEvent 还没搬),做意境时再打开 ──
            // EventManager.SendMessage(new BattleEventDefine.OnCombatEvent { Type = IntentEventType.Crit, Source = attacker });
        }

        return result;
    }

    /// <summary>把伤害应用到目标:先扣护盾,破盾后扣血。改血唯一咽喉。</summary>
    public static void ApplyDamage(BattleUnit target, int damage)
    {
        if (target == null || damage <= 0) return;

        // 先扣护盾
        if (target.attCom.CurShield > 0)
        {
            int shieldAbsorb = Mathf.Min(target.attCom.CurShield, damage);
            target.attCom.CurShield -= shieldAbsorb;
            damage -= shieldAbsorb;
        }

        // 剩余扣血
        if (damage > 0)
            target.attCom.CurHp = Mathf.Max(0, target.attCom.CurHp - damage);

        Debug.Log($"{target.Name} 受到伤害，剩余HP：{target.attCom.CurHp}/{target.attCom.MaxHp}");
        EventManager.SendMessage(new BattleEventDefine.OnUnitHpChanged { Unit = target });  // 刷血条(飘字由 AttackLogic 另发)
    }

    /// <summary>治疗:加血,不超 MaxHp。</summary>
    public static void ApplyHeal(BattleUnit target, int amount)
    {
        if (target == null || amount <= 0) return;
        target.attCom.CurHp = Mathf.Min(target.attCom.MaxHp, target.attCom.CurHp + amount);
        Debug.Log($"{target.Name} 恢复 {amount} HP，当前：{target.attCom.CurHp}/{target.attCom.MaxHp}");
        EventManager.SendMessage(new BattleEventDefine.OnUnitHpChanged { Unit = target });  // 刷血条(回血飘字由 HealLogic 另发)
    }

    // ── 内部计算 ──

    /// <summary>技能倍率:威力=攻击百分比。power=150 → 1.5 倍。配表 Power 驱动。</summary>
    private static float GetSkillMultiplier(SkillInfo skill) => skill.skillData.power / 100f;

    /// <summary>命中判断:命中率 = Hit/(Hit+Dodge);都为0则必中。</summary>
    private static bool RollHit(BattleUnit attacker, BattleUnit target)
    {
        int hit = attacker.attCom.Hit;
        int dodge = target.attCom.Dodge;
        if (hit == 0 && dodge == 0) return true;
        float hitRate = (float)hit / (hit + dodge);
        return Random.value <= hitRate;
    }

    /// <summary>暴击判断:Crit 是百分比整数(15=15%)。</summary>
    private static bool RollCrit(BattleUnit attacker) => Random.value <= attacker.attCom.Crit / 100f;

    /// <summary>暴击倍率:100% + CritDamage%。CritDamage=50 → 1.5 倍。</summary>
    private static float GetCritMultiplier(BattleUnit attacker) => 1f + attacker.attCom.CritDamage / 100f;

    /// <summary>防御减免:伤害 × 100/(100+防御)。防御100减伤50%,收益递减无上限。</summary>
    private static float ApplyDefense(float damage, int def)
    {
        if (def <= 0) return damage;
        return damage * 100f / (100f + def);
    }
}
