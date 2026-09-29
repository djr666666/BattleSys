using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
// ↑ UniTask:本文件暂时没用到，先注释掉(新项目若没装 UniTask，留着会报错)。
//   后续做"异步出招流程"(await 前摇/命中/后摇)时会用到，到时装好 UniTask 再打开。

/// <summary>
/// 战斗单位本体:代表战斗里的"一个人"(飞月、吞天狼、召唤物…)。
/// 它自己只管"身份 + 分类 + 关系"，具体数值/buff/技能拆成三个组件挂着(组件化)。
///   attCom  属性(血攻防速)   buffCom  身上的buff   skillCom  拥有的技能
/// 战斗逻辑一律面向 BattleUnit 操作，不认识具体角色 → 换角色/换游戏都不用改逻辑。
/// </summary>
public class BattleUnit
{
    // ── 身份 ──
    public string Name;          // 名字(招式横幅/UI显示)
    public int Uid;              // 战斗内唯一编号(区分同名单位)
    public int RoleId;           // 角色配表id,按它取立绘/模型/头像

    public GameObject obj;       // 场景里这个单位的模型物体(Spine/预制体)

    // ── 关系 ──
    public BattleUnit Caster;    // 谁创建我的(召唤物记来源)
    public BattleUnit Master;    // 我真正归属谁

    // ── 分类标签(多维度:各管一件事,互不覆盖) ──
    public BattleUnitType UnitType;  // 是什么:玩家/怪/召唤/NPC
    public CampType Camp;            // 哪一方:我方/敌方
    public bool IsBoss;              // 是不是Boss(敌方内部再分)
    public bool AiBailoutUsed;       // AI:保底大招是否已放过(游戏专属,将来抽干净时可移走)

    // ── 组件(单位由这几个部件拼成) ──
    public AttributeComponent attCom;   // 属性
    public BuffComponent buffCom;       // buff
    public SkillComponent skillCom;     // 技能

    // 构造即组装:一创建就带齐三个组件,保证单位是"完整"的,外面拿到就能用
    public BattleUnit()
    {
        attCom   = new AttributeComponent();
        buffCom  = new BuffComponent();
        skillCom = new SkillComponent();
    }

    // 每帧驱动各组件(实时制/DoT/冷却按秒走时用;回合制里大多空转)
    public void Update(float deltaTime)
    {
        attCom?.Update(deltaTime);
        buffCom?.Update(deltaTime);
        skillCom?.Update(deltaTime);
    }
}
