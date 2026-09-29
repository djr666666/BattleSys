using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ═══════════════ 回合制驱动 BattleTurnSystem(总纲) ═══════════════
/// 把所有零件串起来跑一场回合制战斗的"发动机"。【纯逻辑,零表现】——
/// 不含动画/立绘/特效/震屏/演出,只有逻辑 + 事件/日志。表现由使用者监听事件自己做。
///
/// 主流程:
///   StartBattleInit → RunBattleLoop(一轮轮) → RunRound(按速度排,逐个行动)
///     我方:WaitForPlayerTurn(开"门"等玩家:选技能→选目标→结束回合)
///     敌方:ExecuteAITurn(通用简单AI:随机目标+第一个技能)
///   两边最终都走 ExecuteSkill:查Config→取Factory→跑Logic→发事件。
///
/// 不认识鲁班:技能数据/Logic 走 SkillLogicConfig(背后是 IBattleConfigProvider)。
/// ═════════════════════════════════════════════════════════════
/// </summary>
public class BattleTurnSystem
{
    private List<BattleUnit> _units;
    private List<BattleUnit> _turnQueue;
    private SkillLogicConfig _logicConfig;
    private BattleContext _context;

    private bool _isBattleRunning;
    private int _currentIndex;

    // "门":玩家回合挂起等操作,点结束回合才放行
    private UniTaskCompletionSource<bool> _playerTurnEndSource;

    private int _pendingSkillId;
    private BattleUnit _pendingCaster;

    /// <summary>启动初始化,开始回合循环。</summary>
    public void StartBattleInit(List<BattleUnit> units, BattleContext context, SkillLogicConfig logicConfig = null)
    {
        _units = units;
        _context = context;
        _logicConfig = logicConfig;
        _turnQueue = new List<BattleUnit>();
        _isBattleRunning = true;

        foreach (var unit in _units)
            unit.attCom.OnSpeedChanged += () => SortRemaining(_currentIndex + 1);

        RunBattleLoop().Forget();
    }

    private void SortRemaining(int fromIndex)
    {
        if (_turnQueue == null || fromIndex >= _turnQueue.Count) return;
        var remaining = _turnQueue.GetRange(fromIndex, _turnQueue.Count - fromIndex);
        remaining.Sort((a, b) => b.attCom.Speed.CompareTo(a.attCom.Speed));
        for (int i = 0; i < remaining.Count; i++)
            _turnQueue[fromIndex + i] = remaining[i];
        BroadcastTurnOrder();
    }

    private void BroadcastTurnOrder()
    {
        if (_turnQueue == null) return;
        var order = new List<BattleUnit>();
        for (int i = _currentIndex; i < _turnQueue.Count; i++)
            if (_turnQueue[i].attCom.CurHp > 0) order.Add(_turnQueue[i]);
        for (int i = 0; i < _currentIndex; i++)
            if (_turnQueue[i].attCom.CurHp > 0) order.Add(_turnQueue[i]);
        EventManager.SendMessage(new BattleEventDefine.OnTurnOrderChanged { Order = order });
    }

    // ── 主循环 ──
    private async UniTaskVoid RunBattleLoop()
    {
        while (_isBattleRunning)
        {
            await RunRound();
            if (CheckBattleEnd()) _isBattleRunning = false;
        }
    }

    private async UniTask RunRound()
    {
        SortBySpeed();
        for (int i = 0; i < _turnQueue.Count; i++)
        {
            _currentIndex = i;
            if (_turnQueue[i].attCom.CurHp <= 0) continue;
            BroadcastTurnOrder();
            await RunUnitTurn(_turnQueue[i]);
            if (CheckBattleEnd()) return;
        }
        OnRoundEnd();
    }

    private void SortBySpeed()
    {
        _turnQueue = _units.FindAll(u => u.attCom.CurHp > 0);
        _turnQueue.Sort((a, b) => b.attCom.Speed.CompareTo(a.attCom.Speed));
    }

    private void OnUnitTurnStart(BattleUnit unit) => unit.buffCom.TickRound(unit);  // 回合开始:buff tick/减回合
    private void OnRoundEnd() => _context.Turn++;

    private async UniTask RunUnitTurn(BattleUnit unit)
    {
        _context.CurActor = unit;
        OnUnitTurnStart(unit);

        if (unit.Camp == CampType.Player) await WaitForPlayerTurn(unit);
        else                              await ExecuteAITurn(unit);
    }

    /// <summary>我方回合:灵力+2,通知,然后挂起等玩家点"结束回合"。</summary>
    private async UniTask WaitForPlayerTurn(BattleUnit unit)
    {
        _playerTurnEndSource = new UniTaskCompletionSource<bool>();

        _context.LingLi = Mathf.Min(_context.LingLi + 2, _context.LingLiMax);
        EventManager.SendMessage(new BattleEventDefine.OnResourceChanged());
        EventManager.SendMessage(new BattleEventDefine.OnPlayerTurnStart { Unit = unit });
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"轮到 {unit.Name} 行动(灵力={_context.LingLi})" });

        await _playerTurnEndSource.Task;
    }

    // ── 玩家操作入口(UI 调)──
    public void OnPlayerSelectSkill(int skillId, BattleUnit caster)
    {
        ClearPendingTarget();
        var skillInfo = caster.skillCom.GetSkill(skillId);
        if (skillInfo == null) return;

        int cost = (int)skillInfo.GetCurrentManaCost();
        if (_context.LingLi < cost) return;

        _pendingSkillId = skillId;
        _pendingCaster = caster;

        var targets = GetValidTargets(caster, skillInfo.targetType);
        EventManager.SendMessage(new BattleEventDefine.OnEnterTargetSelect { Caster = caster, Targets = targets });
    }

    private List<BattleUnit> GetValidTargets(BattleUnit caster, BattlerTargetType t)
    {
        var result = new List<BattleUnit>();
        if (t == BattlerTargetType.Self) { result.Add(caster); return result; }

        bool wantEnemy = t == BattlerTargetType.EnemyOne || t == BattlerTargetType.EnemyAll;
        bool wantAlly  = t == BattlerTargetType.AllyOne  || t == BattlerTargetType.AllyAll;
        foreach (var u in _units)
        {
            if (u.attCom.CurHp <= 0) continue;
            bool sameCamp = u.Camp == caster.Camp;
            if (wantEnemy && !sameCamp) result.Add(u);
            else if (wantAlly && sameCamp) result.Add(u);
        }
        return result;
    }

    private List<BattleUnit> GetAffectedTargets(BattleUnit caster, BattlerTargetType t, BattleUnit selected)
    {
        if (t == BattlerTargetType.EnemyAll || t == BattlerTargetType.AllyAll) return GetValidTargets(caster, t);
        if (t == BattlerTargetType.Self) return new List<BattleUnit> { caster };
        var list = new List<BattleUnit>();
        if (selected != null) list.Add(selected);
        return list;
    }

    public void OnPlayerSelectTarget(BattleUnit target)
    {
        if (_pendingCaster == null) return;
        var skillInfo = _pendingCaster.skillCom.GetSkill(_pendingSkillId);
        if (skillInfo == null) return;

        ExecuteSkill(_pendingCaster, skillInfo, target).Forget();
        ClearPendingTarget();
    }

    public void CancelPendingSkill() => ClearPendingTarget();

    private void ClearPendingTarget()
    {
        _pendingSkillId = 0;
        _pendingCaster = null;
        EventManager.SendMessage(new BattleEventDefine.OnExitTargetSelect());
    }

    public BattleContext GetContext() => _context;
    public void OnPlayerEndTurn() => _playerTurnEndSource?.TrySetResult(true);

    /// <summary>
    /// ★出招咽喉:玩家/AI 最终都走这里。纯逻辑:扣灵力 → 跑Logic(伤害/回血) → 中转站挂buff → 冷却 → 完全结算。
    /// (无任何动画/演出/延迟;想要表现,使用者监听事件自己加。)
    /// </summary>
    private async UniTask ExecuteSkill(BattleUnit caster, SkillInfo skillInfo, BattleUnit target)
    {
        int manaCost = (int)skillInfo.GetCurrentManaCost();
        if (caster.Camp == CampType.Player)
        {
            _context.LingLi -= manaCost;
            EventManager.SendMessage(new BattleEventDefine.OnResourceChanged());
        }

        var affected = GetAffectedTargets(caster, skillInfo.targetType, target);
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"{caster.Name} 使用【{skillInfo.skillData.Name}】" });

        // 查Config → 取Factory → 跑Logic(这就是那条完整调用链)
        var logics = _logicConfig?.GetLogics(skillInfo.skillData.ID);
        if (logics != null)
        {
            foreach (var logicType in logics)
            {
                var logic = SkillLogicFactory.Get(logicType);
                if (logic == null) { Debug.LogWarning($"[BattleTurnSystem] 未注册的 SkillLogicType: {logicType}"); continue; }
                foreach (var u in affected)
                    await logic.Execute(caster, u, skillInfo);
            }
        }

        // 技能执行完 → 中转站挂关联buff(SkillBuffMediator 监听;没接也不影响)
        EventManager.SendMessage(new BattleEventDefine.OnSkillExecuted { Caster = caster, Targets = affected, SkillId = skillInfo.skillData.ID });

        caster.skillCom.StartCooldown(skillInfo.skillData.ID);

        // 完全结算 → 受击后连锁(反打/反弹/领域挂这)
        EventManager.SendMessage(new BattleEventDefine.OnSkillResolved { Caster = caster });
    }

    /// <summary>通用简单AI:随机一个存活我方 + 第一个技能。游戏专属AI由使用者替换。</summary>
    private async UniTask ExecuteAITurn(BattleUnit unit)
    {
        var players = _units.FindAll(u => u.Camp == CampType.Player && u.attCom.CurHp > 0);
        if (players.Count == 0) return;
        var target = players[Random.Range(0, players.Count)];

        var skills = unit.skillCom.GetAllSkills();
        var skill = (skills != null && skills.Count > 0) ? skills[0] : null;
        if (skill == null) return;

        await ExecuteSkill(unit, skill, target);
    }

    public void Update(float deltaTime) { }

    public void StopAllCasting()
    {
        _isBattleRunning = false;
        _playerTurnEndSource?.TrySetResult(false);
    }

    private bool _battleEnded;

    private bool CheckBattleEnd()
    {
        bool playerAlive = _units.Exists(u => u.Camp == CampType.Player && u.attCom.CurHp > 0);
        bool enemyAlive  = _units.Exists(u => u.Camp == CampType.Enemy  && u.attCom.CurHp > 0);
        if (!playerAlive) { EndBattle(false); return true; }
        if (!enemyAlive)  { EndBattle(true);  return true; }
        return false;
    }

    private void EndBattle(bool win)
    {
        if (_battleEnded) return;
        _battleEnded = true;
        _isBattleRunning = false;
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = win ? "★ 战斗胜利" : "★ 战斗失败" });
        EventManager.SendMessage(new BattleEventDefine.OnBattleEnd { Win = win });
    }
}
