/// <summary>
/// ═══════════════ 技能↔Buff 中转站 SkillBuffMediator(总纲) ═══════════════
/// 【为了什么】让 skill 和 buff 两个模块互不认识,却能协作:
///   - skill 只发 OnSkillExecuted(我放完了),不认识 buff;
///   - buff 只提供 AddBuff,不认识 skill;
///   - "技能→挂哪些buff"这条关联只住在这个中转站里。
///
/// 【怎么回事】监听 OnSkillExecuted → 查这个技能关联哪些 buffId(走接口)→ 给目标挂上。
///   顺带处理跨模块协调:易伤按施法者"剑势层数"缩放;放完技能通知施法者 buff 自管(剑势清零)。
///
/// 【已解耦】不读鲁班,全走 BattleRuntime.Config(IBattleConfigProvider):
///   GetSkillBuffIds(技能关联buff) / GetBuff(buff数据) / GetSkill(判断是否伤害技)。
///
/// 用法:开战前 new SkillBuffMediator().Init();  结束时 Dispose()。
/// ═════════════════════════════════════════════════════════════
/// </summary>
public class SkillBuffMediator
{
    public void Init() => EventManager.AddEvent<BattleEventDefine.OnSkillExecuted>(OnSkillExecuted);
    public void Dispose() => EventManager.Remove<BattleEventDefine.OnSkillExecuted>(OnSkillExecuted);

    private void OnSkillExecuted(BattleEventDefine.OnSkillExecuted e)
    {
        if (e?.Caster == null || e.Targets == null) return;
        if (BattleRuntime.Config == null) return;   // 没接数据源,啥也不挂

        // 施法者剑势层(易伤按它缩放用)。必须在"挂buff"和"剑势消耗"之前读。
        var sword = e.Caster.buffCom.GetBuffByType(BuffType.SwordPower);
        int swordStacks = sword != null ? sword.curStack : 0;

        // ── 挂关联 buff:技能关联哪些 buffId 由配表说了算(走接口拿)──
        var buffIds = BattleRuntime.Config.GetSkillBuffIds(e.SkillId);
        if (buffIds != null)
        {
            foreach (var buffId in buffIds)
            {
                var buffData = BattleRuntime.Config.GetBuff(buffId);   // 接口拿 BuffData(不读鲁班)
                if (buffData == null) continue;

                // 实际生效值:默认=模板value;若是"易伤"(受伤系数)且持剑势 → ×剑势层数
                int effect = buffData.value;
                bool isYiShang = buffData.buffType == BuffType.AttrMod
                              && buffData.attrType == AttrModType.DmgTakenPct;
                if (isYiShang && swordStacks > 0) effect = buffData.value * swordStacks;

                foreach (var t in e.Targets)
                {
                    if (t == null) continue;
                    t.buffCom.AddBuff(new BuffInfo
                    {
                        data        = buffData,
                        caster      = e.Caster,
                        target      = t,
                        curStack    = 1,
                        effectValue = effect,
                    });
                }
            }
        }

        // ── 放完技能 → 通知施法者身上的 buff 自管(如剑势:放完伤害技就清零)──
        //    对所有技能都发,由各 buff 自己判断要不要响应(剑势看 isDamageSkill)。
        var skill = BattleRuntime.Config.GetSkill(e.SkillId);
        bool isDamageSkill = skill != null
            && (skill.targetType == BattlerTargetType.EnemyOne || skill.targetType == BattlerTargetType.EnemyAll);
        e.Caster.buffCom.TriggerOnCasterCast(e.Caster, isDamageSkill);
    }
}
