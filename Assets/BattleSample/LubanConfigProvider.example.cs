// ═══════════════════════════════════════════════════════════════════
// 【参考示例，非框架代码】接入鲁班的 Provider 长什么样。
// 整个文件是注释：因为它用到鲁班(cfg / Models.tabs)，那些只在你游戏(OneGame)里有，
// 战斗框架这边没有，所以放这当"照着抄"的模板，不参与编译。
//
// 用法：把下面代码拷到你游戏(OneGame)里，去掉注释，能编译；
//       开战前执行:  BattleRuntime.Config = new LubanConfigProvider();
//       之后框架里所有 GetSkill/GetSkillLogics 都会走到这里读鲁班。
// ═══════════════════════════════════════════════════════════════════

/*
using System.Collections.Generic;
using cfg;   // 鲁班命名空间(只在你游戏里有)

/// <summary>
/// 鲁班版数据提供者:实现框架的 IBattleConfigProvider,内部读鲁班配表。
/// 框架不认识鲁班,鲁班只出现在这一个类里 —— 这就是"依赖倒置":框架定接口,游戏来满足。
/// </summary>
public class LubanConfigProvider : IBattleConfigProvider
{
    // 1) 技能数据:鲁班 TBSkill → 框架的 SkillData(把字段抄过去)
    public SkillData GetSkill(int skillId)
    {
        var cfg = Models.tabs?.TBSkill?.GetOrDefault(skillId);
        if (cfg == null) return null;

        return new SkillData
        {
            ID          = cfg.Id,
            Name        = cfg.Name,
            Des         = cfg.Desc,
            mp_Cost     = cfg.ManaCost,
            targetType  = (BattlerTargetType)cfg.TargetType,   // 鲁班int → 框架枚举(值一致才对得上)
            power       = cfg.Power,
            isBig       = cfg.IsBig,
            fontTipType = (DmgFontType)cfg.FontTipType,
            // …其它字段按需补
        };
    }

    // 2) buff 数据:鲁班 TBBuff → 框架的 BuffData
    public BuffData GetBuff(int buffId)
    {
        var cfg = Models.tabs?.TBBuff?.GetOrDefault(buffId);
        if (cfg == null) return null;

        return new BuffData
        {
            id       = cfg.Id,
            buffName = cfg.Name,
            buffType = (BuffType)cfg.BuffType,
            attrType = (AttrModType)cfg.AttrType,
            value    = cfg.Value,
            maxStack = cfg.MaxStack,
            RoundTimes = cfg.RoundTimes,
            tickRound  = cfg.TickRound,
            // …其它字段按需补
        };
    }

    // 3) 技能要跑哪些 Logic:鲁班存的是 int 列表 → 转成框架的 SkillLogicType
    public IReadOnlyList<SkillLogicType> GetSkillLogics(int skillId)
    {
        var cfg = Models.tabs?.TBSkill?.GetOrDefault(skillId);
        if (cfg?.LogicTypes == null) return null;

        var list = new List<SkillLogicType>(cfg.LogicTypes.Count);
        foreach (var v in cfg.LogicTypes)
            list.Add((SkillLogicType)v);   // int → 枚举
        return list;
    }

    // 4) 技能关联的 buff id 列表(中转站挂 buff 用)
    public IReadOnlyList<int> GetSkillBuffIds(int skillId)
    {
        var cfg = Models.tabs?.TBSkill?.GetOrDefault(skillId);
        return cfg?.BuffIds;   // 鲁班已经是 List<int>,直接给
    }
}

// —— 游戏启动 / 进战斗前,注册一次 ——
//   BattleRuntime.Config = new LubanConfigProvider();
// 从此框架里 SkillLogicConfig.GetLogics → BattleRuntime.Config.GetSkillLogics → 读鲁班。
*/
