/// <summary>
/// Dot(持续伤害:中毒/灼烧)模块。
/// 时机:IOnTick —— 每隔 data.tickRound 回合被 BuffComponent 调一次,对目标造成伤害。
/// 伤害走 DamageCalculator.ApplyDamage(改血唯一咽喉),不自己减血 →
///   这样血条刷新/飘字/死亡连锁都统一处理,和普通伤害一致。
/// 伤害量:优先用实例的 effectValue(可能被缩放),否则用模板 value。
/// 无状态:全场共享一个实例(BuffModuleFactory 里 new 一次)。
/// </summary>
public class Mod_Dot : IOnTick
{
    public void OnTick(BuffInfo buff, BattleUnit target)
    {
        if (target?.attCom == null || buff?.data == null) return;

        int dmg = buff.effectValue > 0 ? buff.effectValue : buff.data.value;
        if (dmg <= 0) return;

        DamageCalculator.ApplyDamage(target, dmg);   // 走改血咽喉(会发血条刷新事件)
    }
}
