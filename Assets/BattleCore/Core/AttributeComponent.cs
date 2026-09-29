using System;

/// <summary>
/// 属性组件:一个战斗单位的【所有数值】都装在这里(血/护盾/攻/防/速/暴击/命中闪避 + buff百分比修正)。
///
/// 它是"单位"的一个部件 —— BattleUnit 会持有一个 AttributeComponent(通常叫 attCom)。
/// 为什么单独拆成一个组件、不直接把这些字段塞进 BattleUnit?
///   1) 职责单一:BattleUnit 管"我是谁"(阵营/类型/模型)，属性归属性，各管一摊，好维护;
///   2) 复用:伤害计算、buff、UI 血条都只跟 attCom 打交道，不用认识整个 BattleUnit;
///   3) 扩展:以后加属性只动这一个文件。
///
/// 谁在用它:
///   - DamageCalculator 改血(读 MaxHp/Def/各种 PctMod);
///   - Buff 的 AttrMod 模块 加减那几个 PctMod;
///   - 回合系统 读 Speed 排出手顺序;
///   - UI 读 CurHp/MaxHp 画血条。
/// </summary>
public class AttributeComponent
{
    // 生命 / 护盾
    public int CurHp;        // 当前生命
    public int MaxHp;        // 最大生命
    public int CurShield;    // 当前护盾
    public int MaxShield;    // 最大护盾(没上限可不用)

    // 基础战斗属性
    public int Atk;          // 攻击
    public int Def;          // 防御

    // 速度:用"属性"包一层,变了就发事件(回合系统靠速度排序,速度一变要重排)
    public event Action OnSpeedChanged;
    private int _speed;
    public int Speed
    {
        get => _speed;
        set { _speed = value; OnSpeedChanged?.Invoke(); }
    }

    public int Crit;         // 暴击率
    public int CritDamage;   // 暴击伤害
    public int Hit;          // 命中
    public int Dodge;        // 闪避

    // ── buff 百分比修正累加器(AttrMod 模块加减，伤害/治疗计算时读) ──
    public int AtkPctMod;        // 攻击 ±%
    public int DefPctMod;        // 防御 ±%
    public int DmgTakenPctMod;   // 受伤系数 ±%(易伤+/减伤-)
    public int DmgDealtPctMod;   // 输出伤害系数 ±%
    public int HealTakenPctMod;  // 受治疗系数 ±%

    public void Update(float deltaTime) { }   // 预留(实时制/DoT 走秒时用)
}