/// <summary>
/// Buff 模板数据(享元/共享):描述"这种 buff 长什么样"的固定配置。
/// 和 SkillData 一样是"模板",全项目一种 buff 共一份;真正挂身上的是下面的 BuffInfo。
/// </summary>
public class BuffData
{
    // 基础信息
    public int id;
    public string buffName;
    public string buffDes;
    public string buffIcon;

    // 核心:类型 + 数值
    public BuffType buffType;      // buff 行为类型(决定走哪个模块)
    public AttrModType attrType;   // 属性修改类型(仅 AttrMod 用)
    public int value;              // 基础数值(如攻击+X% / 每层增伤X%)

    // ── 规则字段(引擎留了钩子,值待配表加列;现默认不生效)──
    public int maxStack;           // 最大层数
    public int priority;           // 优先级(互斥取舍/结算排序)
    public int mutexGroup;         // 互斥组ID:同组不能共存(0=不互斥)
    public bool isDebuff;          // 是否负面(可被净化/驱散)

    public string[] tags;          // 标签(如 毒=伤害型+毒属型)
    public BuffState state = BuffState.Started;  // buff 状态

    // ── 回合制信息 ──
    public bool isLoop;            // 是否永久触发
    public int RoundTimes;         // 持续回合数(0=永久,直到消耗/清除)
    public int tickRound;          // 每隔几回合触发一次(DoT/HoT)
    public int RoundTimerLater;    // 几回合后才开始触发

    // ── 实时制遗留字段(回合制不用,保留供扩展) ──
    public float duration;         // 持续时间(秒)
    public int tickTime;           // 每隔几秒触发
    public float durationLater;    // 多久后开始触发

    public BuffUpdateTimeEnum buffUpdateTimeEnum;   // 刷新方式:叠加/刷新/保持
    public BuffRemoveStackEnum buffRemoveStackEnum; // 清除方式:清空/减层
}


/// <summary>
/// 运行时"一条真正挂在身上的 buff"实例:每挂一次 new 一个(每人各一份)。
/// data 指向共享模板(1份),其余是这一条自己的、会变的状态。对称 SkillInfo。
/// </summary>
[System.Serializable]
public class BuffInfo
{
    public BuffData data;        // 共享模板(对称 SkillInfo.skillData)
    public BattleUnit caster;    // buff 施加者
    public BattleUnit target;    // buff 承受者(触发时直接用,不用再查)
    public int curStack = 1;     // 当前层数
    public int roundsLeft;       // 剩余回合(data.RoundTimes>0 时有效)
    public int tickCounter;      // 距下次 tick 的回合计数
    public int effectValue;      // 实际生效数值(如易伤=剑势层×data.value);默认=data.value

    public int Id => data != null ? data.id : 0;
    public BuffType Type => data != null ? data.buffType : BuffType.None;
}