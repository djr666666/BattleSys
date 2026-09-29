using System.Collections.Generic;

/// <summary>
/// 技能效果工厂:枚举 SkillLogicType → ISkillLogic 实例(对标 BuffModuleFactory)。
///
/// 加新效果三步:
///   1) SkillLogicType 加枚举值
///   2) 写一个实现 ISkillLogic 的类
///   3) 这里 _registry 注册一行
/// (老代码一行不改——开闭原则)
/// </summary>
public static class SkillLogicFactory
{
    private static readonly Dictionary<SkillLogicType, ISkillLogic> _registry
        = new Dictionary<SkillLogicType, ISkillLogic>
        {
            { SkillLogicType.Attack,   new Logic_Attack()   },
            { SkillLogicType.Fireball, new Logic_Fireball() },
            { SkillLogicType.Heal,     new Logic_Heal()     },
            { SkillLogicType.AddBuff,  new Logic_AddBuff()  },
        };

    /// <summary>类型 → 工人。找不到返回 null(调用方跳过,不抛异常)。</summary>
    public static ISkillLogic Get(SkillLogicType type)
    {
        _registry.TryGetValue(type, out var logic);
        return logic;
    }
}
