using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 火球/元素弹模板:直接伤害 + 额外元素系数。和 Logic_Attack 区别是走"法术"(暂用 ×1.5 代替,
/// 以后 AttributeComponent 加 MagicAtk 再替换)。适用火球/冰锥/雷击等直伤法术。
/// </summary>
public class Logic_Fireball : ISkillLogic
{
    public async UniTask Execute(BattleUnit caster, BattleUnit target, SkillInfo skill)
    {
        if (target == null) { Debug.LogWarning("[Logic_Fireball] 目标为空"); return; }

        int baseDamage  = DamageCalculator.Calculate(caster, target, skill);
        int magicDamage = Mathf.RoundToInt(baseDamage * 1.5f);   // 魔法系数(临时)
        DamageCalculator.ApplyDamage(target, magicDamage);

        await UniTask.CompletedTask;
    }
}
