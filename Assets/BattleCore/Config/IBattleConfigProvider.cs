using System.Collections.Generic;

/// <summary>
/// 战斗框架的"数据入口"抽象。框架不认识鲁班/你的 cfg,只通过这个接口拿数据。
/// 【解耦的关键】原来框架直接读 Models.tabs.TBSkill(鲁班),现在改成问这个接口。
/// 你的游戏写一个实现类去读鲁班/JSON/SO,开战前塞进 BattleRuntime.Config。
/// 别人用你框架的,写自己的实现读自己的数据源,框架一行不用改。
/// </summary>
public interface IBattleConfigProvider
{
    /// <summary>按技能 id 取技能数据(名/威力/目标/命中延迟…)。</summary>
    SkillData GetSkill(int skillId);

    /// <summary>按 buff id 取 buff 数据。</summary>
    BuffData GetBuff(int buffId);

    /// <summary>某技能要跑哪些 Logic(原来的 TBSkill.LogicTypes)。给 SkillLogicConfig 用。</summary>
    IReadOnlyList<SkillLogicType> GetSkillLogics(int skillId);

    /// <summary>某技能关联的 buff id 列表(原来的 TBSkill.BuffIds)。给中转站挂 buff 用。</summary>
    IReadOnlyList<int> GetSkillBuffIds(int skillId);
}

/// <summary>
/// 框架运行时全局接入点。宿主开战前塞入自己的 Provider 实现:
///   BattleRuntime.Config = new LubanConfigProvider();
/// 框架内部一律用 BattleRuntime.Config.GetXxx(...),不碰具体配表。
/// </summary>
public static class BattleRuntime
{
    public static IBattleConfigProvider Config;
}
