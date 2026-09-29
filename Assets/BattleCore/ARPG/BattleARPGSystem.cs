using Cysharp.Threading.Tasks;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

/// <summary>
/// ═══════════════ ARPG 释放流程状态机 BattleARPGSystem(总纲) ═══════════════
/// 管【一个角色】ARPG 模式下的技能释放流程:前摇 → 主效果 → 后摇,支持打断/GCD/连招队列。
/// 和回合制的区别:回合制管"谁的回合、同步执行";ARPG 只管"这个角色的技能异步流程(有真实前后摇)"。
///
/// 共用核心:主效果(ExecuteCast)复用 SkillLogic → 和回合制打同一套伤害/buff。ARPG 只是换"释放方式"。
/// 纯逻辑:无特效/无表现,全程发 OnBattleLog,靠日志看流程走到哪。
/// 每个参战角色各一个 BattleARPGSystem;由外部(宿主)每帧调 Update 驱动 GCD/队列。
/// ═════════════════════════════════════════════════════════════
/// </summary>
public class BattleARPGSystem
{
    // ── 依赖 ──
    private BattleUnit _owner;              // 这个系统管的角色(施法者)
    private SkillLogicConfig _logicConfig;  // 技能→Logic(主效果复用)

    // ── 运行时 ──
    private SkillInfo _currentCastingSkill;         // 正在释放的技能,null=空闲
    private BattleUnit _currentTarget;
    private SkillProcessor _currentProcessor;
    private CancellationTokenSource _currentCastCTS; // 打断用:Cancel() → await 抛异常 → 终止

    // ── 处理器注册表(技能类型 → 处理器)──
    private readonly Dictionary<SkillType, SkillProcessor> _processorCache = new();

    // ── 连招队列 ──
    private readonly Queue<QueueItem> _skillQueue = new();
    private bool _isProcessingQueue;
    private readonly bool _enableSkillQueue = true;
    private readonly int _maxQueueSize = 3;

    // ── GCD 公共冷却 ──
    private readonly float _globalCooldown = 0.5f;
    private float _globalCooldownRemaining;

    // ── 事件(供表现层监听)──
    public event Action<SkillInfo> OnSkillCastStart;
    public event Action<SkillInfo, bool> OnSkillCastComplete;
    public event Action<SkillInfo> OnSkillInterrupted;

    private class QueueItem { public int skillId; public BattleUnit target; }

    // ── 初始化 ──
    public void Initialize(BattleUnit owner, SkillLogicConfig logicConfig)
    {
        _owner = owner;
        _logicConfig = logicConfig;
        RegisterProcessor(SkillType.Active,     new ARPG_ActiveSkillProcessor(logicConfig));
        RegisterProcessor(SkillType.Passive,    new ARPG_PassiveSkillProcessor(logicConfig));
        RegisterProcessor(SkillType.Channeling, new ARPG_ChannelingSkillProcessor(logicConfig));
    }

    public void RegisterProcessor(SkillType type, SkillProcessor processor)
    {
        if (processor != null) _processorCache[type] = processor;
    }

    /// <summary>由宿主每帧调:GCD 倒计时 + 队列检查。</summary>
    public void Update(float deltaTime)
    {
        if (_globalCooldownRemaining > 0) _globalCooldownRemaining -= deltaTime;
        ProcessSkillQueueIfPossible();
    }

    // ── 唯一入口 ──
    public SkillCastResult TryCastSkill(int skillId, BattleUnit target = null)
    {
        if (skillId <= 0) return SkillCastResult.InvalidSkillId;

        var check = CanCastSkill(skillId);
        if (check != SkillCastResult.Success) return check;

        var skillInfo = _owner.skillCom.GetSkill(skillId);
        if (skillInfo == null) return SkillCastResult.SkillNotFound;

        if (_currentCastingSkill != null)   // 正在施法:进队列或拒绝
            return _enableSkillQueue ? QueueSkill(skillId, target) : SkillCastResult.AnotherSkillCasting;

        StartSkillCast(skillInfo, target);
        return SkillCastResult.Success;
    }

    // ── 释放流程 ──
    private void StartSkillCast(SkillInfo skillInfo, BattleUnit target)
    {
        var processor = GetSkillProcessor(skillInfo.skillData.skillType);
        if (processor == null) { Debug.LogError($"找不到处理器: {skillInfo.skillData.skillType}"); return; }

        _currentCastingSkill = skillInfo;
        _currentTarget = target;
        _currentProcessor = processor;
        _currentCastCTS = new CancellationTokenSource();

        OnSkillCastStart?.Invoke(skillInfo);
        SkillCastRoutine(skillInfo, target, processor, _currentCastCTS.Token).Forget();
    }

    /// <summary>
    /// 技能释放主流程(异步):前摇 → 主效果 → 扣CD → 后摇。为什么这么写:
    ///
    /// 【为了什么】ARPG 技能有"真实时间的前后摇",且随时可能被打断(被控/受击硬直)。
    ///   用异步(UniTask)才能"等一会儿又能中途取消",同步代码做不到。
    ///
    /// 【打断怎么回事】靠 CancellationToken(令牌):
    ///   1. 每个 await 都挂上 token(AttachExternalCancellation)。
    ///   2. 外部调 InterruptCurrentSkill() → _currentCastCTS.Cancel() → token 变"已取消"。
    ///   3. 正在等待的那个 await 立刻抛出 OperationCanceledException。
    ///   4. 被 catch 接住(打断是正常流程,不是报错)。
    ///   5. finally 一定执行 → CompleteSkillCast 清理状态,角色不会卡在"施法中"。
    ///
    /// 【扣CD时机】主效果成功后才扣冷却/GCD:被打断(没到主效果)就不惩罚冷却。
    /// </summary>
    private async UniTaskVoid SkillCastRoutine(SkillInfo skill, BattleUnit target, SkillProcessor processor, CancellationToken token)
    {
        bool success = false;
        try
        {
            // 1. 前摇:等真实时间,期间 token 一取消就跳到 catch
            await processor.ExecutePreCast(skill, _owner, target).AttachExternalCancellation(token);
            if (token.IsCancellationRequested) return;

            // 2. 主效果:真正生效(复用 SkillLogic 打伤害/回血)
            success = await processor.ExecuteCast(skill, _owner, target).AttachExternalCancellation(token);
            if (token.IsCancellationRequested) return;

            // 3. 扣CD:只有主效果成功才扣(被打断不罚冷却)
            if (success)
            {
                _owner.skillCom.StartCooldown(skill.skillData.ID);
                _globalCooldownRemaining = _globalCooldown;   // 触发公共冷却,防瞬间连发
            }

            // 4. 后摇:收招,同样可被打断
            await processor.ExecutePostCast(skill, _owner, target).AttachExternalCancellation(token);
        }
        catch (OperationCanceledException)
        {
            // 被打断从这退出 —— 属于正常流程,不是错误,所以静默接住
        }
        finally
        {
            // 无论 成功/失败/打断,都要清理"施法中"状态,否则角色永远卡着放不了下一个
            CompleteSkillCast(skill, success);
        }
    }

    // ── 打断 ──
    public void InterruptCurrentSkill()
    {
        if (_currentCastingSkill == null) return;
        _currentProcessor?.Interrupt();
        OnSkillInterrupted?.Invoke(_currentCastingSkill);
        _currentCastCTS?.Cancel();
    }

    public void StopAllCasting()
    {
        _currentCastCTS?.Cancel();
        _currentProcessor?.Interrupt();
        _currentCastingSkill = null;
        _currentProcessor = null;
        ClearSkillQueue();
    }

    // ── 连招队列 ──
    private SkillCastResult QueueSkill(int skillId, BattleUnit target)
    {
        if (_skillQueue.Count >= _maxQueueSize) return SkillCastResult.QueueFull;
        _skillQueue.Enqueue(new QueueItem { skillId = skillId, target = target });
        return SkillCastResult.Queued;
    }

    private void ProcessSkillQueueIfPossible()
    {
        if (_isProcessingQueue || _skillQueue.Count == 0 || !CanCastAnySkill()) return;

        var next = _skillQueue.Peek();
        if (!CanCastSkillConsideringGCD(next.skillId)) return;

        _isProcessingQueue = true;
        _skillQueue.Dequeue();
        var skillInfo = _owner.skillCom.GetSkill(next.skillId);
        if (skillInfo != null && CanCastSkill(next.skillId) == SkillCastResult.Success)
            StartSkillCast(skillInfo, next.target);
        _isProcessingQueue = false;
    }

    public void ClearSkillQueue() { _skillQueue.Clear(); _isProcessingQueue = false; }

    // ── 条件检查 ──
    private SkillCastResult CanCastSkill(int skillId)
    {
        if (!CanCastAnySkill())                   return SkillCastResult.CharacterCannotCast;
        if (!CanCastSkillConsideringGCD(skillId)) return SkillCastResult.GlobalCooldown;
        if (_owner.skillCom.GetSkill(skillId) == null) return SkillCastResult.SkillNotFound;
        if (!_owner.skillCom.CanCast(skillId))    return SkillCastResult.OnCooldown;
        return SkillCastResult.Success;
    }

    private bool CanCastAnySkill()
    {
        if (_currentCastingSkill != null) return false;
        if (_owner.attCom.CurHp <= 0)     return false;
        return true;
    }

    private bool CanCastSkillConsideringGCD(int skillId)
    {
        if (!CanCastAnySkill()) return false;
        if (_globalCooldownRemaining <= 0) return true;
        var s = _owner.skillCom.GetSkill(skillId);
        return s != null && !s.skillData.affectedByGCD;   // 免GCD技能可无视
    }

    public void ResetAllCooldowns()
    {
        _owner.skillCom.ResetAllCooldowns();
        _globalCooldownRemaining = 0f;
    }

    private void CompleteSkillCast(SkillInfo skill, bool success)
    {
        if (skill == null) return;
        OnSkillCastComplete?.Invoke(skill, success);
        if (_currentCastingSkill == skill)
        {
            _currentCastingSkill = null;
            _currentTarget = null;
            _currentProcessor = null;
            _currentCastCTS = null;
        }
    }

    private SkillProcessor GetSkillProcessor(SkillType type)
    {
        if (_processorCache.TryGetValue(type, out var p)) return p;
        return _processorCache.TryGetValue(SkillType.Active, out var def) ? def : null;
    }

    public bool IsCasting() => _currentCastingSkill != null;
    public float GetGlobalCooldownRemaining() => _globalCooldownRemaining;
    public int GetQueueCount() => _skillQueue.Count;
}
