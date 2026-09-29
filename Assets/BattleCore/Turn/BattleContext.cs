using UnityEngine;

/// <summary>
/// 战斗上下文:一场战斗的"全局共享状态" —— 不属于任何单个单位的东西都放这。
/// 一场战斗一个 BattleContext,回合系统和各逻辑都读它。
/// 对比:AttributeComponent 装"某个单位自己的"数值(血攻防);BattleContext 装"整场共享的"(回合/资源/当前谁在动)。
///
/// 说明:目前字段偏【回合制】(Turn 回合数、CurActor 当前行动者)。
///       ARPG 实时制没有"回合",到时另建/扩展自己的上下文,内核(技能/buff/伤害)照样共用。
/// </summary>
public class BattleContext
{
    // ── 战斗信息 ──
    public int Turn;                  // 当前第几回合(回合制)

    // ── 全队共享资源(不是某个人的,是整队的)──
    public int LingLi         = 4;    // 灵力:放技能消耗,全队共享,初始 4
    public int LingLiMax      = 10;   // 灵力上限
    public int IntentPoint    = 0;    // 意境点:攒够开契界,全队共享。正式=0(之前临时测试给过8)
    public int IntentPointMax = 10;   // 意境点上限

    // ── 当前行为(UI 用来高亮角色/目标、刷技能面板)──
    public BattleUnit CurActor;       // 当前行动者
    public BattleUnit CurTarget;      // 当前目标

    // 领域(游戏专属:布置后影响全局规则)。纯框架可删,先留着不影响。
    public GameObject Domain;
}
