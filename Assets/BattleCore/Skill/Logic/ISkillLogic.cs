using Cysharp.Threading.Tasks;

/// <summary>
/// 技能效果模板接口(对标 buff 的 IBuffModule)。
/// 每个实现类 = 一种技能效果(攻击/治疗/火球…)。一个技能可挂多个 Logic,依次执行。
/// 和 buff 一样是"面向接口":执行方拿到类型 → Factory 给实例 → 调 Execute,不认识具体类。
/// Execute 是 async(UniTask):技能效果可能要 await(前摇/命中延迟/连段),所以是异步。
/// </summary>
public interface ISkillLogic
{
    /// <summary>
    /// caster = 施法者;target = 目标(群体技可为 null,Logic 内部自己处理范围);
    /// skill  = 技能运行时实例(含等级/数据)。
    /// </summary>
    UniTask Execute(BattleUnit caster, BattleUnit target, SkillInfo skill);
}
