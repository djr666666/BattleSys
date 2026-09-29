/// <summary>
/// 剑势:叠层容器。
///   OnHit —— 造成伤害时按 层数×value% 加成(乘在本次伤害上,伤害计算不认识剑势)。
///   OnCasterCast —— 放完伤害技后自己清零(消耗规则住在剑势模块里,不写死中转站)。
///   无状态:全场共享一个实例。
/// </summary>
public class Mod_SwordPower : IOnHit, IOnCasterCast
{
    public void OnHit(BuffInfo buff, BattleUnit attacker, DamageInfo dmg)
    {
        if (dmg == null) return;
        dmg.damage *= 1f + buff.curStack * buff.data.value / 100f;   // 每层 +value%
    }

    public void OnCasterCast(BuffInfo buff, BattleUnit caster, bool isDamageSkill)
    {
        if (isDamageSkill) caster.buffCom.RemoveBuff(buff);   // 放完伤害技,剑势用完清零
    }
}
