using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 主动技能处理器:前摇 → 主效果(复用共享 SkillLogic 真打伤害) → 后摇。
/// 复用 SkillLogicConfig + SkillLogicFactory,和回合制走同一套核心 —— ARPG 只是换个"释放流程"。
/// </summary>
public class ARPG_ActiveSkillProcessor : SkillProcessor
{
    private readonly SkillLogicConfig _logicConfig;

    public ARPG_ActiveSkillProcessor(SkillLogicConfig logicConfig)
    {
        _logicConfig = logicConfig;
    }

    public override async UniTask ExecutePreCast(SkillInfo skill, BattleUnit caster, BattleUnit target)
    {
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"{caster.Name}【{skill.skillData.Name}】前摇…" });
        int ms = Mathf.RoundToInt(skill.skillData.preCastTime * 1000);
        if (ms > 0) await UniTask.Delay(ms);
    }

    public override async UniTask<bool> ExecuteCast(SkillInfo skill, BattleUnit caster, BattleUnit target)
    {
        // ★复用共享 SkillLogic:查Config → 取Factory → 执行(真打伤害/回血),和回合制同一套
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
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"{caster.Name}【{skill.skillData.Name}】命中 {(target != null ? target.Name : "无")}" });
        return true;
    }

    public override async UniTask ExecutePostCast(SkillInfo skill, BattleUnit caster, BattleUnit target)
    {
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"{caster.Name}【{skill.skillData.Name}】后摇…" });
        int ms = Mathf.RoundToInt(skill.skillData.postCastTime * 1000);
        if (ms > 0) await UniTask.Delay(ms);
    }

    public override void Interrupt()
        => EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = "主动技能被打断" });
}
