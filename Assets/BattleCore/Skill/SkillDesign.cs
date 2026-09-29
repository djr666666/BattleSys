// 技能设计：把"技能有哪些类型 / 状态 / 目标方式"这类枚举集中放这里。
//
// 为什么要单独一个 Design 文件放枚举？
//   1) 数据类(SkillData)只装"这一个技能的具体数值"，而"世界上一共有哪几种类型"
//      属于全局定义，散在数据类里会重复、难维护 → 抽出来集中管。
//   2) 配表里存的是 int(0/1/2…)，加载时转成这些枚举。枚举名可读，
//      比在代码里到处写魔法数字 2、3 清楚得多。
//   3) 框架独立后，这些枚举是"框架自有"的，不依赖策划的鲁班表；
//      接入时只要保证"值(=int)和策划表一致"即可(见下方注释)。

/// <summary>
/// 技能逻辑类型：一个技能能挂多个 Logic(在 SkillLogicConfig 登记)，
/// 按 List 顺序依次执行(比如 先 Attack 造成伤害，再 AddBuff 挂减益)。
/// 类型 → 具体执行者(工人) 的映射在 SkillLogicFactory。
/// 作用：让"一个技能做哪些事"变成可组合的搭积木，加新效果只加一个类型+一个Logic。
/// 例 ： 我有一个技能可以在 赞成 物理伤害的同时，恢复目标血量
/// </summary>
public enum SkillLogicType
{
    Attack,     // 物理攻击伤害
    Fireball,   // 魔法/元素伤害(系数更高)
    Heal,       // 回复目标血量
    AddBuff,    // 给目标施加 buff(关联 buff 走 SkillBuffMediator 读表)
    //
    //
    //
}

/// <summary>
/// 技能大类：决定这个技能"怎么被驱动"。
/// 作用：驱动层(回合/ARPG)按它选不同处理流程——被动不主动放、引导要持续、蓄力要按住。
/// </summary>
public enum SkillType
{
    Active,      // 主动技能(点一下就放)
    Passive,     // 被动技能(不主动放，满足条件自动生效)
    Channeling,  // 引导技能(持续施放，可被打断)
    Charge,      // 蓄力技能(按住蓄力，松手释放)
    Combo,       // 连招技能
    Custom       // 自定义/特殊机制
}

/// <summary>
/// 技能状态机：一个技能实例当前处在哪一步。
/// 作用：ARPG 实时驱动靠它判断"现在能不能放/能不能打断/是否在冷却"。
/// 回合制用得少，但保留以便统一。
/// </summary>
public enum SkillState
{
    Idle,           // 空闲，可释放
    PreCasting,     // 前摇阶段
    Casting,        // 施法阶段(引导)
    PostCasting,    // 后摇阶段
    Cooldown,       // 冷却中
    Disabled,       // 被禁用(沉默等)
    Interrupted     // 被打断
}

/// <summary>
/// 目标"形状/范围"：这个技能打出去覆盖的几何形态。
/// 注意：它和下面的 BattlerTargetType 是【两个维度】——
///   这个管"覆盖形状"(单体/一片/一个方向)，
///   BattlerTargetType 管"选哪一方的人"(敌方/我方)。
/// 别混。(ARPG/横版按形状做命中判定时用得上；回合制常只用 BattlerTargetType。)
/// </summary>
public enum TargetType
{
    Single,     // 单体
    Area,       // 范围(一片)
    Self,       // 自身
    Direction   // 方向(一条线/扇形)
}

/// <summary>技能释放结果：尝试放技能后返回，告诉调用方成功还是为什么失败。</summary>
public enum SkillCastResult
{
    Success = 0,              // 成功
    Queued = 1,               // 已加入队列
    InvalidSkillId = 10,      // 无效技能ID
    SkillNotFound = 11,       // 找不到技能
    NoProcessor = 12,         // 没有处理器
    OnCooldown = 20,          // 冷却中
    GlobalCooldown = 21,      // 公共冷却中
    InsufficientCost = 30,    // 消耗不足
    ConditionNotMet = 31,     // 条件不满足
    CharacterCannotCast = 40, // 角色无法施法
    AnotherSkillCasting = 41, // 正在施放其他技能
    QueueFull = 42,           // 队列已满
}

// ─────────────────────────────────────────────────────────────
// 下面两个是"框架自有"的枚举，用来【替代原项目里鲁班生成的 cfg 枚举】。
// 战斗框架独立出去后不该认识鲁班，所以在框架里自定义同名/同值的版本。
// ★接入你自己的游戏时：这里的【值(=int)必须和策划配表里的一致】，
//   因为读表时是 (BattlerTargetType)读到的int，值错位就选错目标了。
//   —— 所以要和策划沟通、对齐数值定义。
// ─────────────────────────────────────────────────────────────

/// <summary>
/// 技能"选谁"：按阵营决定作用对象。SkillData.targetType 用它。
/// 作用：回合制选目标、AI 找对象、判断能不能对某单位生效，都读它。
/// (替代原 cfg.BattlerTargetType，值保持一致)
/// </summary>
public enum BattlerTargetType
{
    Self     = 0,   // 只对自己
    AllyOne  = 1,   // 友方单体
    EnemyOne = 2,   // 敌方单体
    EnemyAll = 3,   // 敌方全体
    AllyAll  = 4,   // 友方全体
}

/// <summary>
/// 飘字类型：命中/回血时头顶飘的数字用哪种样式。
/// 作用：表现层(飘字系统)按它选颜色/字体——红=伤害、白=普通、绿=回血、None=不飘。
/// (替代原 cfg.BttleDmgFontType，值保持一致)
/// </summary>
public enum DmgFontType
{
    None  = 0,   // 不飘字
    Red   = 1,   // 暴击/大招伤害
    White = 2,   // 普通伤害
    Green = 3,   // 回血
}
