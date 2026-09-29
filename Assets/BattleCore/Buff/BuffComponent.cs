using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ═══════════════ Buff 容器 BuffComponent(总纲) ═══════════════
/// 挂在 BattleUnit 上的"buff 背包"——管理这个单位身上所有 buff 实例(BuffInfo)。
/// 角色对称:SkillComponent 管技能,BuffComponent 管 buff。
///
/// 它负责什么("包工头",只管管理,不管具体效果):
///   1) 增删:AddBuff(挂/叠层) / RemoveBuff / ClearAllBuffs / RemoveDebuffs(净化)
///   2) 回合推进:TickRound —— DoT/HoT 周期触发 + 回合到期移除
///   3) 战斗时机分发:TriggerOnHit/OnBehurt/OnKill/OnBeKill/OnCasterCast
///      —— 在对应时机遍历身上 buff,让各自的模块(Mod_)响应
///   4) 查询:HasBuff / GetBuff / GetBuffByType
///   变化后发 OnBuffChanged 通知 UI 刷图标。
///
/// 它【不】写具体效果:加多少攻、扣多少血,都在 Modules/ 里的 Mod_ 工人。
/// 分工:BuffComponent=包工头(什么时候开工) / Factory=派工台(按类型给工人) / Mod_=工人(真干活)。
/// ═════════════════════════════════════════════════════════════
/// </summary>
public class BuffComponent
{
    // 这个单位当前挂着的所有 buff(用链表:增删频繁、不需要随机下标)
    public LinkedList<BuffInfo> buffList = new LinkedList<BuffInfo>();
    // "待删清单":遍历中不能直接删,先记下来遍历完再删(避免改集合崩溃)
    private readonly List<BuffInfo> _toRemove = new List<BuffInfo>();

    // 回合制不需要每帧驱动;留空实现,避免 BattleUnit.Update 调用报错
    public void Update(float deltaTime) { }

    /// <summary>回合驱动:该单位回合开始时调。处理 tick(DoT/HoT) + 回合到期。</summary>
    public void TickRound(BattleUnit owner)
    {
        _toRemove.Clear();
        foreach (var b in buffList)
        {
            // 周期触发(持续扣血/回血)
            if (b.data.tickRound > 0)
            {
                b.tickCounter--;
                if (b.tickCounter <= 0)
                {
                    (BuffModuleFactory.Get(b.Type) as IOnTick)?.OnTick(b, owner);
                    b.tickCounter = b.data.tickRound;
                }
            }
            // 回合到期(RoundTimes>0 才计时;0=永久,直到被消耗/清除)
            if (b.data.RoundTimes > 0)
            {
                b.roundsLeft--;
                if (b.roundsLeft <= 0) _toRemove.Add(b);
            }
        }
        foreach (var b in _toRemove) RemoveBuff(b);
        _toRemove.Clear();
    }

    /// <summary>加 buff:已有同 id 则叠层(未满),否则新挂。挂上/叠层都会触发对应模块的 OnCreate。</summary>
    public void AddBuff(BuffInfo info)
    {
        if (info?.data == null) return;
        if (!ResolveMutex(info)) return;   // 互斥检查(P1,当前恒放行)

        var mod = BuffModuleFactory.Get(info.Type);
        var exist = FindBuff(info.Id);

        if (exist != null)
        {
            if (exist.curStack < exist.data.maxStack)
            {
                exist.curStack++;
                // 刷新持续时间
                if (exist.data.buffUpdateTimeEnum == BuffUpdateTimeEnum.Add)
                    exist.roundsLeft += exist.data.RoundTimes;
                else if (exist.data.buffUpdateTimeEnum == BuffUpdateTimeEnum.Replace)
                    exist.roundsLeft = exist.data.RoundTimes;
                // 叠层再触发一次 OnCreate(属性类=再加一份)
                (mod as IOnCreate)?.OnCreate(exist, exist.target);
            }
        }
        else
        {
            info.roundsLeft = info.data.RoundTimes;
            info.tickCounter = info.data.tickRound;
            (mod as IOnCreate)?.OnCreate(info, info.target);
            buffList.AddLast(info);
        }
        NotifyChanged(info.target);
    }

    /// <summary>
    /// 互斥/优先级检查:AddBuff 前调。同 mutexGroup 已有时按 priority 取舍。
    /// 返回 true=放行。当前是骨架,恒放行(P1 再实现)。
    /// </summary>
    private bool ResolveMutex(BuffInfo incoming)
    {
        if (incoming?.data == null || incoming.data.mutexGroup == 0) return true;
        // TODO(P1): 找同组 buff → 比 priority → 高的留。现暂放行。
        return true;
    }

    /// <summary>
    /// 净化/驱散:移除 count 个负面 buff(count<=0 表示全清)。Mod_Cleanse / 净化技能调这个。
    /// 返回实际清掉数量。用"待删清单"安全移除,不在遍历中直接删。
    /// </summary>
    public int RemoveDebuffs(int count = -1)
    {
        _toRemove.Clear();
        foreach (var b in buffList)
        {
            if (b.data != null && b.data.isDebuff)
            {
                _toRemove.Add(b);
                if (count > 0 && _toRemove.Count >= count) break;   // 够数就停
            }
        }
        foreach (var b in _toRemove) RemoveBuff(b);
        int removed = _toRemove.Count;
        _toRemove.Clear();
        return removed;
    }

    /// <summary>移除一条 buff:触发它的 OnRemove(还属性等),再从列表删,发变化通知。</summary>
    public void RemoveBuff(BuffInfo info)
    {
        if (info == null || !buffList.Contains(info)) return;
        (BuffModuleFactory.Get(info.Type) as IOnRemove)?.OnRemove(info, info.target);
        buffList.Remove(info);
        NotifyChanged(info.target);
    }

    // buff 变化 → 通知 UI 刷图标(单向:buff 不认识 UI,只发事件)
    private static void NotifyChanged(BattleUnit unit)
    {
        if (unit != null)
            EventManager.SendMessage(new BattleEventDefine.OnBuffChanged { Unit = unit });
    }

    // ── 战斗时机分发(AttackLogic / DamageCalculator 在对应时机调)──
    public void TriggerOnHit(BattleUnit attacker, DamageInfo dmg)
    {
        foreach (var b in buffList)
            (BuffModuleFactory.Get(b.Type) as IOnHit)?.OnHit(b, attacker, dmg);
    }
    public void TriggerOnBehurt(BattleUnit target, DamageInfo dmg)
    {
        foreach (var b in buffList)
            (BuffModuleFactory.Get(b.Type) as IOnBehurt)?.OnBehurt(b, target, dmg);
    }
    public void TriggerOnKill(BattleUnit victim)
    {
        foreach (var b in buffList)
            (BuffModuleFactory.Get(b.Type) as IOnKill)?.OnKill(b, victim);
    }
    public void TriggerOnBeKill(BattleUnit killer, BattleUnit victim)
    {
        foreach (var b in buffList)
            (BuffModuleFactory.Get(b.Type) as IOnBeKill)?.OnBeKill(b, killer, victim);
    }
    /// <summary>施法者放完技能 → 通知身上 buff 自管(如剑势放完伤害技清零)。
    /// 用快照遍历,允许模块在回调里移除自己。</summary>
    public void TriggerOnCasterCast(BattleUnit caster, bool isDamageSkill)
    {
        foreach (var b in new List<BuffInfo>(buffList))
            (BuffModuleFactory.Get(b.Type) as IOnCasterCast)?.OnCasterCast(b, caster, isDamageSkill);
    }

    // ── 查询 ──
    public bool HasBuff(int id) => FindBuff(id) != null;
    public BuffInfo GetBuff(int id) => FindBuff(id);

    private BuffInfo FindBuff(int id)
    {
        foreach (var b in buffList) if (b.Id == id) return b;
        return null;
    }

    public BuffInfo GetBuffByType(BuffType type)
    {
        foreach (var b in buffList) if (b.Type == type) return b;
        return null;
    }
    public void RemoveBuffByType(BuffType type)
    {
        var b = GetBuffByType(type);
        if (b != null) RemoveBuff(b);
    }

    public void ClearAllBuffs()
    {
        foreach (var b in buffList)
            (BuffModuleFactory.Get(b.Type) as IOnRemove)?.OnRemove(b, b.target);
        buffList.Clear();
    }
}
