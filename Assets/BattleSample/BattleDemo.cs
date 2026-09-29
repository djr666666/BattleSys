using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗 Demo(挂到场景空物体上,Play 即跑)。Inspector 里切 Mode 选 回合制 / ARPG。
/// 纯代码 + 假数据源,无 UI/美术,Console 看整场 log —— 证明框架独立可跑。
/// </summary>
public class BattleDemo : MonoBehaviour
{
    public enum BattleMode { Turn, ARPG }

    [Header("选战斗模式")]
    public BattleMode mode = BattleMode.Turn;

    private BattleTurnSystem _turn;
    private BattleARPGSystem _arpg;
    private SkillBuffMediator _mediator;
    private List<BattleUnit> _units;

    private void Start()
    {
        BattleRuntime.Config = new DemoConfigProvider();   // 接入假数据源

        _mediator = new SkillBuffMediator();               // 中转站:自动挂关联buff
        _mediator.Init();

        EventManager.AddEvent<BattleEventDefine.OnBattleLog>(OnLog);
        EventManager.AddEvent<BattleEventDefine.OnBuffChanged>(OnBuff);

        if (mode == BattleMode.Turn) StartTurn();
        else                         StartArpg();
    }

    private void Update()
    {
        _arpg?.Update(Time.deltaTime);   // ARPG 要每帧驱动 GCD/队列;回合制不需要
    }

    private void OnDestroy()
    {
        _mediator?.Dispose();
        _arpg?.StopAllCasting();
        EventManager.Remove<BattleEventDefine.OnBattleLog>(OnLog);
        EventManager.Remove<BattleEventDefine.OnBuffChanged>(OnBuff);
        EventManager.Remove<BattleEventDefine.OnPlayerTurnStart>(OnPlayerTurn);
        EventManager.Remove<BattleEventDefine.OnBattleEnd>(OnEnd);
    }

    // ══════════════ 回合制 ══════════════
    private void StartTurn()
    {
        EventManager.AddEvent<BattleEventDefine.OnPlayerTurnStart>(OnPlayerTurn);
        EventManager.AddEvent<BattleEventDefine.OnBattleEnd>(OnEnd);

        _units = new List<BattleUnit>
        {
            MakeUnit("飞月",   1024, CampType.Player, 300, 80, 12, new[] { 1001 }),
            MakeUnit("苏婉清", 1013, CampType.Player, 300, 60, 10, new[] { 1002 }),
            MakeUnit("吞天狼", 1038, CampType.Enemy,  300, 40, 8,  new[] { 2001 }, true),
        };

        _turn = new BattleTurnSystem();
        _turn.StartBattleInit(_units, new BattleContext(), new SkillLogicConfig());
        Debug.Log("======== 回合制战斗开始 ========");
    }

    // 自动替玩家:选第一个技能 → 打第一个存活敌人 → 结束回合
    private void OnPlayerTurn(BattleEventDefine.OnPlayerTurnStart e)
    {
        var caster = e.Unit;
        var skills = caster.skillCom.GetAllSkills();
        if (skills.Count == 0) { _turn.OnPlayerEndTurn(); return; }

        int skillId = skills[0].skillData.ID;
        var enemy = _units.Find(u => u.Camp == CampType.Enemy && u.attCom.CurHp > 0);
        _turn.OnPlayerSelectSkill(skillId, caster);
        if (enemy != null) _turn.OnPlayerSelectTarget(enemy);
        _turn.OnPlayerEndTurn();
    }

    private void OnEnd(BattleEventDefine.OnBattleEnd e) => Debug.Log($"======== 战斗结束: {(e.Win ? "我方胜" : "我方败")} ========");

    // ══════════════ ARPG ══════════════
    private void StartArpg()
    {
        var caster = MakeUnit("飞月",   1024, CampType.Player, 300, 80, 12, new[] { 3001, 3002 });
        var target = MakeUnit("吞天狼", 1038, CampType.Enemy,  300, 40, 8,  new int[0], true);
        _units = new List<BattleUnit> { caster, target };

        _arpg = new BattleARPGSystem();
        _arpg.Initialize(caster, new SkillLogicConfig());
        // 订阅 ARPG 三个流程事件 → 打 log
        _arpg.OnSkillCastStart    += s      => Debug.Log($"[ARPG] ▶ 开始释放【{s.skillData.Name}】");
        _arpg.OnSkillCastComplete += (s, ok) => Debug.Log($"[ARPG] ■ 释放结束【{s.skillData.Name}】 success={ok}");
        _arpg.OnSkillInterrupted  += s      => Debug.Log($"[ARPG] ✖ 被打断【{s.skillData.Name}】");

        Debug.Log("======== ARPG 演示开始 ========");
        StartCoroutine(ArpgScript(caster, target));
    }

    // 脚本化时间线:演示 正常释放 / 引导被打断 / GCD连发进队列
    private IEnumerator ArpgScript(BattleUnit caster, BattleUnit target)
    {
        yield return new WaitForSeconds(0.5f);

        Debug.Log("—— 场景1:正常释放主动技能(前摇→命中→后摇)——");
        Debug.Log("  TryCast 结果=" + _arpg.TryCastSkill(3001, target));
        yield return new WaitForSeconds(1.5f);   // 等它整段跑完

        Debug.Log("—— 场景2:释放引导技能,中途被打断 ——");
        Debug.Log("  TryCast 结果=" + _arpg.TryCastSkill(3002, target));
        yield return new WaitForSeconds(0.7f);   // 引导进行中…
        Debug.Log("  (受击硬直!)调用打断");
        _arpg.InterruptCurrentSkill();
        yield return new WaitForSeconds(0.6f);

        Debug.Log("—— 场景3:GCD期间连按两次(第二次应进队列)——");
        Debug.Log("  第一次=" + _arpg.TryCastSkill(3001, target));
        Debug.Log("  第二次=" + _arpg.TryCastSkill(3001, target));   // 正在施法/GCD → 进队列
        yield return new WaitForSeconds(3f);     // 等队列里那个也自动接上跑完

        Debug.Log("======== ARPG 演示结束 ========");
    }

    // ── 公共 ──
    private void OnLog(BattleEventDefine.OnBattleLog e) => Debug.Log("[战斗] " + e.Text);

    private void OnBuff(BattleEventDefine.OnBuffChanged e)
    {
        var names = new List<string>();
        foreach (var b in e.Unit.buffCom.buffList) names.Add(b.Type.ToString());
        Debug.Log($"[Buff] {e.Unit.Name} 当前buff: [{string.Join(", ", names)}]");
    }

    private BattleUnit MakeUnit(string name, int roleId, CampType camp, int hp, int atk, int spd, int[] skillIds, bool isBoss = false)
    {
        var u = new BattleUnit { Name = name, RoleId = roleId, Camp = camp, IsBoss = isBoss };
        u.attCom.MaxHp = hp; u.attCom.CurHp = hp;
        u.attCom.Atk = atk;  u.attCom.Def = 10;
        u.attCom.Speed = spd;
        foreach (var id in skillIds)
        {
            var data = BattleRuntime.Config.GetSkill(id);
            if (data != null) u.skillCom.AddSkill(data);
        }
        return u;
    }
}


/// <summary>假数据源(Demo 专用):技能/buff 写死,替代读鲁班。真实项目换成 LubanConfigProvider。</summary>
public class DemoConfigProvider : IBattleConfigProvider
{
    private readonly Dictionary<int, SkillData> _skills = new()
    {
        // 回合制技能
        { 1001, new SkillData { ID = 1001, Name = "流云斩", targetType = BattlerTargetType.EnemyOne, power = 100, mp_Cost = 1 } },
        { 1002, new SkillData { ID = 1002, Name = "易伤斩", targetType = BattlerTargetType.EnemyOne, power = 90,  mp_Cost = 1 } },
        { 2001, new SkillData { ID = 2001, Name = "撕咬",   targetType = BattlerTargetType.EnemyOne, power = 80,  mp_Cost = 0 } },
        // ARPG技能:主动(有前后摇)+ 引导(castTime长,能被打断)
        { 3001, new SkillData { ID = 3001, Name = "疾风斩", skillType = SkillType.Active,     targetType = BattlerTargetType.EnemyOne, power = 100, mp_Cost = 0, preCastTime = 0.5f, postCastTime = 0.3f } },
        { 3002, new SkillData { ID = 3002, Name = "风刃引导", skillType = SkillType.Channeling, targetType = BattlerTargetType.EnemyOne, power = 60,  mp_Cost = 0, preCastTime = 0.2f, castTime = 1.5f, postCastTime = 0.2f } },
    };

    private readonly Dictionary<int, SkillLogicType[]> _logics = new()
    {
        { 1001, new[] { SkillLogicType.Attack } },
        { 1002, new[] { SkillLogicType.Attack } },
        { 2001, new[] { SkillLogicType.Attack } },
        { 3001, new[] { SkillLogicType.Attack } },
        { 3002, new[] { SkillLogicType.Attack } },
    };

    private readonly Dictionary<int, int[]> _skillBuffs = new()
    {
        { 1002, new[] { 301 } },   // 易伤斩 → 挂易伤
    };

    private readonly Dictionary<int, BuffData> _buffs = new()
    {
        { 301, new BuffData { id = 301, buffName = "易伤", buffType = BuffType.AttrMod, attrType = AttrModType.DmgTakenPct, value = 20, RoundTimes = 2, maxStack = 1, isDebuff = true } },
    };

    public SkillData GetSkill(int skillId) => _skills.TryGetValue(skillId, out var s) ? s : null;
    public BuffData GetBuff(int buffId) => _buffs.TryGetValue(buffId, out var b) ? b : null;
    public IReadOnlyList<SkillLogicType> GetSkillLogics(int skillId) => _logics.TryGetValue(skillId, out var l) ? l : null;
    public IReadOnlyList<int> GetSkillBuffIds(int skillId) => _skillBuffs.TryGetValue(skillId, out var b) ? b : null;
}
