using System.Collections.Generic;

/// <summary>
/// 技能逻辑配置:skillId → 要跑哪些 Logic。Config 管"技能做啥",Factory 管"谁来做"。
///
/// 【已解耦】数据不再直接读鲁班,改问 BattleRuntime.Config.GetSkillLogics(skillId)。
///   —— 框架不认识鲁班;你的游戏在 IBattleConfigProvider 实现里读鲁班的 TBSkill.LogicTypes 转成枚举返回。
///
/// 首次查某技能时问 Provider 并缓存,之后直接命中(含 null 也缓存,避免重复问)。
/// 对外接口不变:GetLogics(skillId)。
/// </summary>
public class SkillLogicConfig
{
    // skillId → Logic 列表(缓存)
    private readonly Dictionary<int, List<SkillLogicType>> _cache = new Dictionary<int, List<SkillLogicType>>();

    /// <summary>查指定技能要跑的 Logic 列表;没配/没接 Provider 返回 null(调用方跳过)。</summary>
    public List<SkillLogicType> GetLogics(int skillId)
    {
        if (_cache.TryGetValue(skillId, out var cached)) return cached;

        List<SkillLogicType> list = null;
        var src = BattleRuntime.Config?.GetSkillLogics(skillId);   // ← 走接口,不碰鲁班
        if (src != null && src.Count > 0)
            list = new List<SkillLogicType>(src);

        _cache[skillId] = list;
        return list;
    }
}
