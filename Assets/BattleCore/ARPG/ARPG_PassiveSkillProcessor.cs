using Cysharp.Threading.Tasks;

/// <summary>
/// 被动技能处理器:无前后摇,主动释放时直接跑一次效果(被动的常态触发一般靠 buff 的时机接口,
/// 这里只处理"手动激活被动"这种少见情况)。多数被动其实走 buff 系统,不走这里。
/// </summary>
public class ARPG_PassiveSkillProcessor : SkillProcessor
{
    private readonly SkillLogicConfig _logicConfig;

    public ARPG_PassiveSkillProcessor(SkillLogicConfig logicConfig)
    {
        _logicConfig = logicConfig;
    }

    public override UniTask ExecutePreCast(SkillInfo skill, BattleUnit caster, BattleUnit target) => UniTask.CompletedTask;

    public override async UniTask<bool> ExecuteCast(SkillInfo skill, BattleUnit caster, BattleUnit target)
    {
        EventManager.SendMessage(new BattleEventDefine.OnBattleLog { Text = $"{caster.Name}【{skill.skillData.Name}】被动触发" });
        var logics = _logicConfig?.GetLogics(skill.skillData.ID);
        if (logics != null)
        {
            foreach (var type in logics)
            {
                var logic = SkillLogicFactory.Get(type);
                if (logic == null) continue;
                await logic.Execute(caster, target ?? caster, skill);
            }
        }
        return true;
    }

    public override UniTask ExecutePostCast(SkillInfo skill, BattleUnit caster, BattleUnit target) => UniTask.CompletedTask;
}
