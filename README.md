# BattleSys · Unity 战斗框架

一套**数据驱动、事件解耦**的 Unity 战斗框架。**回合制**与 **ARPG** 两套驱动**共用同一份**技能 / Buff / 伤害内核 —— 换玩法不换核心。
纯逻辑、零表现耦合(不绑美术/动画/配表方案),可以只靠 Console 日志完整跑一场战斗。

> 从一个商业修仙 RPG 项目里抽出的战斗内核,做了彻底解耦,供学习与复用。

---

## ✨ 特性

- **双驱动共用核心**:回合制(`BattleTurnSystem`)+ ARPG(`BattleARPGSystem`)打出的伤害/Buff 完全一致,只是"怎么发动"不同。
- **组件化组合(ECS 思想启发)**:一个 `BattleUnit` = 拼装 `attCom`(属性)+ `buffCom`(Buff)+ `skillCom`(技能),组合优于继承。
- **面向接口 + 工厂**:技能 `ISkillLogic`、Buff `IBuffModule`,类型 → 工人;加新技能/Buff **只加文件不改老码**(开闭原则)。
- **事件驱动(发布-订阅)**:逻辑只发事件,表现层监听 —— 逻辑与 UI/特效彻底解耦。
- **配表解耦(依赖倒置)**:框架不认识任何配表,数据全走 `IBattleConfigProvider` 接口,数据源(Luban / JSON / ScriptableObject)由使用者实现。
- **纯逻辑可跑**:无需 UI/美术,代码造单位 + 假数据源即可跑出完整战斗日志。

---

## 🧩 架构分层

```
表现层(使用者自己接)          监听事件做 UI/动画/特效/音效
        ▲  事件(EventManager)
─────────┼──────────────────────────────────────────
驱动层    │   BattleTurnSystem(回合制)   BattleARPGSystem(ARPG,前后摇/打断/GCD/连招)
─────────┼──────────────────────────────────────────
内核层    │   SkillLogic(伤害/治疗/挂Buff) · Buff 系统 · DamageCalculator · 属性
(共用)    │   SkillBuffMediator(技能↔Buff 中转站)
─────────┼──────────────────────────────────────────
数据层    │   SkillData / BuffData(POCO)  ← IBattleConfigProvider 提供
─────────┴──────────────────────────────────────────
              使用者实现 Provider,内部读 Luban / JSON / SO
```

**设计铁律**:
- 出招只走 `ExecuteSkill`,改血只走 `DamageCalculator`(唯一咽喉,好挂钩子)。
- 逻辑不认识具体角色/UI/配表,一律面向 `BattleUnit` + 事件 + 接口。

---

## 📁 目录结构

```
Assets/
├── BattleCore/            战斗框架本体(将来可整体做成 UPM 包)
│   ├── Common/            枚举:CampType / BattleUnitType
│   ├── Skill/             SkillData / SkillComponent / SkillBuffMediator
│   │   └── Logic/         ISkillLogic + Logic_Attack/Heal/Fireball/AddBuff + Factory + SkillLogicConfig
│   ├── Buff/              BuffData / BuffComponent
│   │   └── Modules/       IBuffModule + Mod_Attr/SwordPower/Dot/Cleanse + Factory
│   ├── Core/              BattleUnit / AttributeComponent / DamageCalculator
│   ├── Turn/              BattleContext / BattleTurnSystem
│   ├── ARPG/              SkillProcessor + ARPG_Active/Channeling/Passive + BattleARPGSystem
│   ├── Config/            IBattleConfigProvider(数据入口抽象)
│   └── Events/            BattleEventDefine
├── EventManager/          通用事件系统(独立库,见下方依赖)
└── BattleSample/          示例:BattleDemo(回合制/ARPG 一键跑)+ 假数据源
```

---

## 📦 依赖

- **[UniTask](https://github.com/Cysharp/UniTask)**(必需):异步出招流程。已在 `Packages/manifest.json` 以 git 方式引入。
- **[EventListeningSystem](https://github.com/djr666666/EventListeningSystem)**:简单好用的事件系统(本仓库 `Assets/EventManager` 内含一份)。

---

## 🚀 快速开始

### 1. 直接跑 Demo(最快)
打开工程 → 新建空场景 → 空物体挂 `BattleDemo` → Inspector 选 **Mode(Turn / ARPG)** → Play,看 Console 日志。

### 2. 接入你自己的数据(实现 Provider)
框架不读配表,你写一个 `IBattleConfigProvider` 实现,内部读你的数据源:

```csharp
public class MyConfigProvider : IBattleConfigProvider
{
    public SkillData GetSkill(int id) { /* 读你的表 → 填 SkillData */ }
    public BuffData  GetBuff(int id)  { /* 读你的表 → 填 BuffData  */ }
    public IReadOnlyList<SkillLogicType> GetSkillLogics(int id) { /* 技能→Logic列表 */ }
    public IReadOnlyList<int> GetSkillBuffIds(int id)           { /* 技能→关联buff */ }
}

// 开战前注册一次:
BattleRuntime.Config = new MyConfigProvider();
```

### 3. 跑一场回合制
```csharp
var mediator = new SkillBuffMediator(); mediator.Init();   // 技能自动挂buff

var units = /* 造若干 BattleUnit,设 attCom 数值 + skillCom.AddSkill */;
var turn = new BattleTurnSystem();
turn.StartBattleInit(units, new BattleContext(), new SkillLogicConfig());
// 监听 BattleEventDefine.OnBattleLog / OnUnitHpChanged / OnBattleEnd 做表现
```

### 4. ARPG(实时,前后摇/打断/GCD/连招)
```csharp
var arpg = new BattleARPGSystem();
arpg.Initialize(caster, new SkillLogicConfig());
// 每帧: arpg.Update(Time.deltaTime);
arpg.TryCastSkill(skillId, target);   // 释放
arpg.InterruptCurrentSkill();         // 打断
```

---

## 🧠 用到的设计思想

| 思想 | 体现 |
|---|---|
| 组件化组合(ECS 思想启发) | 单位 = 属性/Buff/技能 组件拼装 |
| 面向接口 + 工厂 | `ISkillLogic` / `IBuffModule`,类型→工人 |
| 开闭原则 | 加技能/Buff 只加文件不改老码 |
| 事件驱动 | `EventManager` 发布-订阅,逻辑↔表现解耦 |
| 依赖倒置 | 框架定 `IBattleConfigProvider`,数据源由使用者实现 |
| 数据/行为/驱动 三层分离 | POCO 数据 · 行为模块 · 回合/ARPG 驱动各自独立 |

---

## 🗺 Roadmap

- [x] 回合制驱动
- [x] ARPG 驱动(前后摇/打断/GCD/连招队列)
- [x] Buff 系统(属性修改 / 剑势叠层 / 持续伤害 / 净化)
- [x] 技能↔Buff 中转站、配表接口解耦
- [ ] 更多 Buff 模块(护盾/眩晕/反弹/吸血…)
- [ ] 战棋(SRPG)驱动(格子 + 寻路 + 按距离选目标)
- [ ] 打包成 UPM 包 + 可视化技能编辑器

---

## 📄 License

MIT
