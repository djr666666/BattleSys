using System.Collections.Generic;
using UnityEngine;



/// <summary>
/// ═══════════════ 技能组件 SkillComponent(总纲) ═══════════════
/// 定位:挂在 BattleUnit 上的"技能背包",管理【这个单位拥有的技能 + 它们的运行时状态】。
///       和 BuffComponent 是对称设计(一个管技能、一个管buff)。
///
/// 它负责什么(4 件事):
///   1) 存:持有该单位所有技能(SkillInfo 列表),提供 增/查/遍历。
///   2) 冷却:每帧递减冷却、冷却归零时通知 UI(单技能精准回调 + 全局广播)。
///   3) 状态:能否释放(CanCast)、升级(UpgradeSkill)、被动开关。
///   4) 通知:技能冷却变化 → 发事件,让 UI 画冷却转圈/刷按钮。
///
/// 它【不】负责什么(边界,很重要):
///   - 不执行技能效果(伤害/回血/挂buff)——那是 SkillLogic / 回合系统的事;
///   - 不选目标、不判灵力够不够——由调用方(回合系统/UI)决定;
///   本类只是"背包 + 状态记录",职责单一。
///
/// 数据流:AddSkill(模板SkillData) → 包成运行时SkillInfo存起来
///        → 释放成功后 StartCooldown → 每帧 Update 递减 → 归零发事件 → UI刷新。
///
/// 通用性:回合制 & ARPG 共用。冷却读秒主要服务 ARPG 实时线,回合制可不依赖 Update。
/// ═════════════════════════════════════════════════════════════
/// </summary>
public class SkillComponent
{

    public SkillComponent() { }


    // 单位组件 拥有的技能(每个是 SkillInfo 运行时实例)
    private List<SkillInfo> _skills = new List<SkillInfo>();

    // 单技能精准冷却回调：key=skillId，只通知盯着这个技能的人(传 技能/剩余CD/总CD)
    // 细粒度，主要给 ARPG 单技能按钮画冷却转圈用
    private Dictionary<int, System.Action<SkillInfo, float, float>> _skillCooldownCallbacks = new();

    // 任意技能冷却变化的粗粒度广播：谁订阅谁收，UI 靠它统一刷冷却
    public event System.Action<SkillInfo> OnAnySkillCooldownChanged;


    /// <summary>每帧驱动(ARPG 实时线)：推进所有技能冷却读秒。回合制线可不依赖它。</summary>
    public void Update(float deltaTime)
    {
        UpdateCooldowns(deltaTime);
    }

    /// <summary>逐个技能递减冷却；某技能冷却归零瞬间，触发它登记的单技能精准回调(通知 UI 可用了)。</summary>
    private void UpdateCooldowns(float deltaTime)
    {
        foreach (var skill in _skills)
        {
            if (skill.cooldownRemaining > 0)
            {
                skill.cooldownRemaining -= deltaTime;
                if (skill.cooldownRemaining <= 0)
                {
                    skill.cooldownRemaining = 0;
                    if (_skillCooldownCallbacks.TryGetValue(skill.skillData.ID, out var callback))
                        callback?.Invoke(skill, skill.cooldownRemaining, skill.GetCurrentCooldown());
                }
            }
        }
    }


    /// <summary>
    /// 往背包加技能：拿模板(SkillData)包成运行时实例(SkillInfo)、初始 1 级。
    /// 已有同 ID 直接忽略。(= BuffComponent.AddBuff)
    /// </summary>
    public void AddSkill(SkillData data)
    {
        if (data == null) return;
        if (GetSkill(data.ID) != null) return; // 已存在直接忽略

        var newSkill = new SkillInfo
        {
            skillData = data,
            curLv = 1,
            selectedTargets = new List<GameObject>()
        };

        _skills.Add(newSkill);
    }

    /// <summary>按 ID 找技能实例，找不到返回 null。(= BuffComponent.GetBuff)</summary>
    public SkillInfo GetSkill(int id)
    {
        foreach (var skill in _skills)
        {
            if (skill.skillData != null && skill.skillData.ID == id)
                return skill;
        }
        return null;
    }

    /// <summary>返回全部技能的拷贝列表(拷贝，防止外部改到内部 _skills)。</summary>
    public List<SkillInfo> GetAllSkills() => new List<SkillInfo>(_skills);

    /// <summary>能否释放：技能存在且 IsAvailable(有等级/冷却好/状态空闲)。灵力门槛由调用方另判。</summary>
    public bool CanCast(int id)
    {
        var skill = GetSkill(id);
        if (skill == null) return false;
        return skill.IsAvailable;
    }

    /// <summary>
    /// 开始冷却：技能成功释放后调。剩余冷却拉满、记录释放时间，
    /// 并立刻通知(单技能精准回调 + 全局广播)让 UI 开始画冷却。
    /// </summary>
    public void StartCooldown(int id)
    {
        var skill = GetSkill(id);
        if (skill == null) return;

        skill.cooldownRemaining = skill.GetCurrentCooldown();
        skill.lastCastTime = Time.time;

        if (_skillCooldownCallbacks.TryGetValue(id, out var callback))
            callback?.Invoke(skill, skill.cooldownRemaining, skill.GetCurrentCooldown());

        OnAnySkillCooldownChanged?.Invoke(skill);
    }


    /// <summary>清空所有技能冷却(战斗开始/重置用)。</summary>
    public void ResetAllCooldowns()
    {
        foreach (var skill in _skills)
            skill.cooldownRemaining = 0;
    }

    // ── 技能升级 ──────────────────────────────────────────────

    /// <summary>
    /// 升级指定技能，返回是否成功
    /// 已满级或技能不存在时返回 false (buff 只有层数，skill 有等级)
    /// </summary>
    public bool UpgradeSkill(int id)
    {
        var skill = GetSkill(id);
        if (skill == null) return false;
        if (skill.curLv >= skill.skillData.maxLv) return false;

        skill.curLv++;
        return true;
    }

    // ── 被动技能激活状态 ──────────────────────────────────────

    /// <summary>
    /// 被动技能激活状态表：key=skillId，value=是否激活
    /// 被动技能不主动释放，但可以被"关闭"（如特定buff解除被动效果）
    /// </summary>
    private Dictionary<int, bool> _passiveActiveStates = new Dictionary<int, bool>();

    /// <summary>设置某被动技能的激活/关闭状态。</summary>
    public void SetPassiveSkillActive(int id, bool isActive)
    {
        _passiveActiveStates[id] = isActive;
    }

    /// <summary>查询被动技能是否激活；没登记过 → 默认激活(true)。</summary>
    public bool IsPassiveSkillActive(int id)
    {
        return !_passiveActiveStates.TryGetValue(id, out bool isActive) || isActive;
    }
}
