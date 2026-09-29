using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能"模板数据"：描述"这个技能本身长什么样"(不变的配置)。
/// 一个技能只有一份 SkillData(相当于图纸)，全项目共用。
/// 数据来源：读策划配表 → new 出一个 SkillData 填进去(框架不认识配表，由宿主转)。
/// 对比下面的 SkillInfo：SkillData=模板(共享)，SkillInfo=某个角色手里这个技能的运行时状态(每人一份)。
/// </summary>
public class SkillData
{
    // ── 基础信息 ──
    public int ID;                                   // 技能唯一id(查表、连招、AI都靠它认技能)
    public string Name;                              // 技能名(招式名横幅、UI显示)
    public string Des;                               // 技能描述(UI里给玩家看的说明)
    public SkillType skillType = SkillType.Active;   // 技能大类:主动/被动/引导/蓄力… 决定"怎么被驱动"
    public bool affectedByGCD = true;                // 是否受公共冷却(GCD)限制。false=可无视GCD瞬发(如闪现)。仅ARPG用

    // ── 施法时间配置(秒) ──
    // 一次出招的三段节奏:前摇(抬手)→ 施法(引导技持续)→ 后摇(收招)。驱动层按这些时长推进。
    public float preCastTime = 0.5f;                 // 前摇:抬手到生效前的时间
    public float castTime = 0.0f;                    // 施法:引导类技能持续施放的时间(非引导=0)
    public float postCastTime = 0.2f;                // 后摇:生效后到能做下一个动作的时间

    // ── 等级 / 消耗 ──
    public int maxLv = 5;                            // 技能最大等级(升级系统用)
    public float cooldown = 3f;                      // 冷却:放完隔多久才能再放(秒)
    public float mp_Cost = 10f;                      // 释放消耗(法力/灵力),不够则放不出

    // ── 战斗数值(核心,来自配表) ──
    public BattlerTargetType targetType = BattlerTargetType.EnemyOne;  // 选谁:敌方单体/全体/我方/自身,决定选目标逻辑
    public int power = 100;                          // 威力系数:实际伤害/回血 = 攻击 × power/100(100=打满攻击)
    public int isBig = 0;                            // 是否大招:1=是。用于AI决策、是否播cut-in/招式名/顿感
    public DmgFontType fontTipType = DmgFontType.White; // 命中飘字样式:红=伤害/白=普通/绿=回血/None=不飘
    public int impactDelay = 1300;                   // 命中延迟(毫秒):出手动画挥到"命中点"的耗时,伤害在此刻结算(动画和数据对齐)

    // ── 连招系统 ──
    public bool isCombo;                             // 这个技能是否属于连招
    public string[] comboIds;                        // 连招的技能id序列(按顺序放才接得上)
    public float comboWindow = 1.5f;                 // 连招输入窗口(秒):上一段放完多久内接下一段才算连招

    // ── 打断系统 ──
    public bool canBeInterrupted = true;             // 施法过程能否被打断(受击/控制时)
    public string[] interruptibleByTags;             // 能被哪些"效果标签"打断(如 眩晕/击飞)
}


/// <summary>
/// 技能"运行时实例":某个角色手里的这个技能【当前状态】。
/// SkillData 是共享模板(图纸),SkillInfo 是"我这把技能现在冷却剩多少、处在哪一步"(每个角色各一份)。
/// 分开的好处:模板不变、状态各自维护,不会你放技能把别人的冷却也改了。
/// </summary>
[System.Serializable]
public class SkillInfo
{
    public SkillData skillData;                       // 指向模板(读威力/冷却/目标等固定配置)

    // ── 运行时状态 ──
    public int curLv = 0;                         // 当前等级(0=还没学会)
    public float cooldownRemaining = 0f;              // 剩余冷却(每帧递减,到0才能再放)
    public SkillState CurrentState = SkillState.Idle; // 当前状态机:空闲/前摇/施法/后摇/冷却…
    public GameObject creator;                        // 谁放出来的(技能的拥有者/施法者)

    // ── 连招相关 ──
    public int comboStep = 0;                         // 当前连到第几段
    public float lastCastTime = -999f;                // 上次释放时间(判断是否还在连招窗口内)

    // ── 目标信息 ──
    public List<GameObject> selectedTargets = new List<GameObject>();  // 本次选中的目标

    /// <summary>能不能放:学会了 + 不在冷却 + 处于空闲。UI 按钮亮不亮就看它。</summary>
    public bool IsAvailable => curLv > 0 && cooldownRemaining <= 0 && CurrentState == SkillState.Idle;

    // 当前冷却/消耗(现在直接读模板,以后可按等级加成)
    public float GetCurrentCooldown() => skillData.cooldown;
    public float GetCurrentManaCost() => skillData.mp_Cost;

    // 目标类型直接读模板,不在这里再存一份 → 避免"两处数据不一致"的坑
    public BattlerTargetType targetType => skillData.targetType;
    public LayerMask targetLayer;                     // 目标所在层(空间检测/选目标过滤用)
}
