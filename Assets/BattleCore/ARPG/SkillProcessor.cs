using Cysharp.Threading.Tasks;

/// <summary>
/// ARPG 技能处理器基类:定义一次技能释放的三段(前摇→主效果→后摇)+ 打断。
/// 按技能类型(主动/引导/被动)分不同子类。目标用 BattleUnit(需要 GameObject 就取 target.obj)。
/// 全 UniTask 异步:前后摇可 await 真实时间、可被取消(打断)。
/// </summary>
public abstract class SkillProcessor
{
    /// <summary>前摇:抬手/蓄力,期间可被打断。</summary>
    public abstract UniTask ExecutePreCast(SkillInfo skill, BattleUnit caster, BattleUnit target);

    /// <summary>主效果:真正生效(复用共享 SkillLogic 打伤害/回血/挂buff)。返回是否成功。</summary>
    public abstract UniTask<bool> ExecuteCast(SkillInfo skill, BattleUnit caster, BattleUnit target);

    /// <summary>后摇:收招,期间可被打断。</summary>
    public abstract UniTask ExecutePostCast(SkillInfo skill, BattleUnit caster, BattleUnit target);

    /// <summary>被打断时清理。</summary>
    public virtual void Interrupt() { }
}
