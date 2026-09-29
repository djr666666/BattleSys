using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
/// 属性修改模块:挂上加、移除减对应属性累加器(狂暴/降防/易伤/减伤/受治/增伤)。
/// 只写"属性累加器",不直接改血/算伤害——DamageCalculator 读累加器时才用到(buff与伤害解耦)。
/// 无状态:全场共享一个实例。
/// </summary>
public class Mod_Attr : IOnCreate, IOnRemove
{
    public void OnCreate(BuffInfo buff, BattleUnit target) => Apply(buff, target, +1);
    public void OnRemove(BuffInfo buff, BattleUnit target) => Apply(buff, target, -buff.curStack);

    private void Apply(BuffInfo buff, BattleUnit t, int mul)
    {
        if (t?.attCom == null) return;


        int v = buff.effectValue * mul;
        switch (buff.data.attrType)
        {
            case AttrModType.Atk: t.attCom.AtkPctMod += v; break;
            case AttrModType.Def: t.attCom.DefPctMod += v; break;
            case AttrModType.DmgTakenPct: t.attCom.DmgTakenPctMod += v; break;
            case AttrModType.HealTakenPct: t.attCom.HealTakenPctMod += v; break;
            case AttrModType.DmgDealtPct: t.attCom.DmgDealtPctMod += v; break;
        }
    }
}
