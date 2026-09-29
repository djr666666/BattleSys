using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 引导技能处理器:短前摇 → 进入引导(持续 castTime,期间可被打断)→ 后摇。
/// 引导期间每段可反复生效(这里简化为引导时长内跑一次 SkillLogic;要按秒多次tick可再扩展)。
/// </summary>
public class ARPG_ChannelingSkillProcessor : SkillProcessor
{
    private readonly SkillLogicConfig _logicConfig;
    private bool _isChanneling;

    public ARPG_ChannelingSkillProcessor(SkillLogicConfig logicConfig)
    {
        _logicConfig = logicConfig;
    }

    public override async UniTask ExecutePreCast(SkillInfo skill, BattleUnit caster, BattleUnit target)
    {
        int ms = Mathf.RoundToInt(skill.skillData.preCastTime * 1000);
        if (ms > 0) await UniTask.Delay(ms);
    }

    public override async UniTask<bool> ExecuteCast(SkillInfo skill, BattleUnit caster, BattleUnit target)
    {
        _isChanneling = true;
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"{caster.Name}【{skill.skillData.Name}】开始引导…" });

        var logics = _logicConfig?.GetLogics(skill.skillData.ID);
        if (logics != null)
        {
            foreach (var type in logics)
            {
                var logic = SkillLogicFactory.Get(type);
                if (logic == null) continue;
                await logic.Execute(caster, target, skill);
            }
        }

        int ms = Mathf.RoundToInt(skill.skillData.castTime * 1000);   // 引导持续
        if (ms > 0) await UniTask.Delay(ms);

        _isChanneling = false;
        return true;
    }

    public override async UniTask ExecutePostCast(SkillInfo skill, BattleUnit caster, BattleUnit target)
    {
        int ms = Mathf.RoundToInt(skill.skillData.postCastTime * 1000);
        if (ms > 0) await UniTask.Delay(ms);
    }

    public override void Interrupt()
    {
        if (!_isChanneling) return;
        _isChanneling = false;
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = "引导技能被打断" });
    }
}
