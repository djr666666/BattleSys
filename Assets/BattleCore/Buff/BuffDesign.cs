// BuffDesign：Buff 系统的枚举定义(对标 SkillDesign)。
// 把"buff 有哪些类型/状态/刷新方式"集中放这里，BuffData 只装某条 buff 的具体数值。
//
// buff 从哪来(生成时机)——设计时的分类，帮你想全场景：
//   技能赋予：剑圣大招 → 获得攻速/移速 buff
//   buff 赋予：杀人剑 → 击杀敌人叠层
//   道具赋予：大药 → 攻击加成 + 生命恢复
//   事件赋予：大龙死亡 → 全队大龙 buff
// buff 干什么(行为分类)：改属性 / 改状态 / 生成物(如炸弹生成小炸弹)
// 这些只是"设计备忘"，真正的类型枚举见下面 BuffType。

using System;

/// <summary>
/// buff 生命周期状态：这条 buff 现在处于哪一步。
/// 作用：挂载/结算时判断它是否生效、能否被触发。
/// </summary>
public enum BuffState
{
    Invalid, // 无效(还没初始化/已失效)
    Ready,   // 准备好了(可启动)
    Started, // 已启动(正在生效)
    Freezed, // 被冻结(暂时无法开启) —— 注:标准英文应为 Frozen，此处沿用原项目拼写
}

/// <summary>
/// buff 刷新方式:再挂一次同名 buff 时怎么处理。
/// 作用：控制"叠 buff"的行为(层数叠加 / 刷新时间 / 忽略)。
/// </summary>
public enum BuffUpdateTimeEnum
{
    Add = 0,     // 叠加:层数+1(如剑势越叠越强)
    Replace = 1, // 刷新:重置持续时间/层数(如中毒刷新为满时长)
    Keep = 2,    // 保持:什么都不做(已有就忽略)
}

/// <summary>
/// buff 清除方式:到期/被清时怎么减。
/// 作用：区分"一次清空"还是"每次只减一层"。
/// </summary>
public enum BuffRemoveStackEnum
{
    Clear = 0,   // 直接清空(整条移除)
    Reduce = 1,  // 减层:时间到只减 1 层，减到 0 才移除
}

/// <summary>
/// ★核心枚举:buff 行为类型 —— 决定这条 buff 走哪个处理【模块】(Modules/ 下)。
/// 和 SkillLogicType 一个思路:类型 → 对应一个模块干活。加新类 buff = 加一个枚举值 + 一个模块。
/// 配表存 int，加载时转成它(值要和策划表一致)。
/// </summary>
public enum BuffType
{
    None       = 0,
    AttrMod    = 1,  // 属性修改(攻/防/受伤/受治) → Mod_AttrMod
    Dot        = 2,  // 持续扣血(中毒/灼烧)
    Hot        = 3,  // 持续回血(回复法术)
    Shield     = 4,  // 护盾(吸收伤害)
    Stun       = 5,  // 控制/眩晕(不能行动)
    SwordPower = 6,  // 剑势叠层(通用"叠层增伤"，非飞月专属) → Mod_SwordPower
    DmgMark    = 7,  // 承伤标记(受击即消，如易伤印记)
    Cleanse    = 8,  // 净化(清除负面)
    Custom     = 99, // 专属机制逃生口:配 customId 指向角色自己的模块，核心枚举不膨胀
}

/// <summary>
/// 属性修改的细分类型 —— 仅当 BuffType.AttrMod 时用，说明改哪个属性。
/// 作用:AttrMod 模块按它决定把 value 加到攻/防/易伤/受治/增伤 哪个通道。
/// </summary>
public enum AttrModType
{
    None         = 0,
    Atk          = 1,  // 攻击
    Def          = 2,  // 防御
    DmgTakenPct  = 3,  // 受伤系数(易伤/减伤:正=更痛，负=减伤)
    HealTakenPct = 4,  // 受治疗系数(受到的治疗放大/削减)
    DmgDealtPct  = 5,  // 输出伤害系数(通用增伤/减伤:正=增，负=减)
}

/// <summary>
/// 一次伤害的信息包:谁打的、打谁、打多少。Dot/护盾/反弹等要传递伤害时用。
/// 注:引用了 BattleUnit —— 在 Core 里建好 BattleUnit 之前，这里会暂时报红，属正常。
/// </summary>
[Serializable]
public class DamageInfo
{
    public BattleUnit creator; // 伤害来源(施加者)
    public BattleUnit target;  // 承受者
    public float damage = 10;  // 伤害数值
}
