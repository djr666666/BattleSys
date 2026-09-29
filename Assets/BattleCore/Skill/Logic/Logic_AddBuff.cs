using Cysharp.Threading.Tasks;

/// <summary>
/// [已废弃/占位] buff 施加已移到 SkillBuffMediator(中转站,监听 OnSkillExecuted 挂关联buff)。
/// 保留空实现只为不破坏 SkillLogicFactory 的注册;技能不再通过 Logic 直接挂 buff。
/// (将来若不用中转站,可把挂 buff 逻辑写回这里。)
/// </summary>
public class Logic_AddBuff : ISkillLogic
{
    public async UniTask Execute(BattleUnit caster, BattleUnit target, SkillInfo skill)
    {
        await UniTask.CompletedTask;
    }
}
