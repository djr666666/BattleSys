using System.Collections.Generic;

/// <summary>
/// 战斗事件定义:逻辑层对外广播的"合同"。框架只发【逻辑事件】(谁行动/血变了/挂了buff/战斗结束…),
/// 用的人监听这些事件自己做表现(UI/动画/特效/音效)。框架本身不含任何表现。
///
/// 纯框架原则:这里【不放】立绘、招式名、特效、震屏、动画之类的"表现事件"——
///   那些是使用者的事。框架保持纯逻辑,Console 打日志即可看懂全过程。
/// </summary>
public class BattleEventDefine
{
    /// <summary>轮到某单位行动。</summary>
    public class OnPlayerTurnStart { public BattleUnit Unit; }

    /// <summary>行动顺序变化 → 刷新行动条。Order[0]=当前行动者。</summary>
    public class OnTurnOrderChanged { public List<BattleUnit> Order; }

    /// <summary>进入选目标模式 → UI 显示可选目标。</summary>
    public class OnEnterTargetSelect { public BattleUnit Caster; public List<BattleUnit> Targets; }

    /// <summary>退出选目标模式。</summary>
    public class OnExitTargetSelect { }

    /// <summary>战斗日志:整场战斗的文字流水。纯 log 展示的核心。</summary>
    public class OnBattleLog { public string Text; }

    /// <summary>资源(灵力等)变化 → 刷新资源显示。</summary>
    public class OnResourceChanged { }

    /// <summary>单位血量/护盾变化 → 刷新血条。触发点:DamageCalculator(改血唯一咽喉)。</summary>
    public class OnUnitHpChanged { public BattleUnit Unit; }

    /// <summary>造成伤害。Amount=伤害值;FontType=类型(用的人决定飘什么字/要不要飘)。</summary>
    public class OnDamageDealt { public BattleUnit Unit; public int Amount; public DmgFontType FontType; }

    /// <summary>回复生命。</summary>
    public class OnHealDealt { public BattleUnit Unit; public int Amount; public DmgFontType FontType; }

    /// <summary>战斗结束。Win=true战胜/false战败。</summary>
    public class OnBattleEnd { public bool Win; }

    /// <summary>技能已执行 → 中转站监听挂关联buff。Caster/Targets/SkillId。</summary>
    public class OnSkillExecuted { public BattleUnit Caster; public List<BattleUnit> Targets; public int SkillId; }

    /// <summary>一次出招完全结算完 → 受击后连锁(反打/反弹/领域等)挂这里。</summary>
    public class OnSkillResolved { public BattleUnit Caster; }

    /// <summary>某单位 buff 变化(挂/叠/移除) → 刷新 buff 显示。</summary>
    public class OnBuffChanged { public BattleUnit Unit; }
}
