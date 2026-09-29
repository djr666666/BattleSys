/// <summary>
/// Cleanse(净化/驱散)模块。
/// 时机:IOnCreate —— 挂上的瞬间,清除目标身上的负面buff(data.isDebuff==true)。
/// 清几个:模板 value>0 表示清 value 个,否则(<=0)全清。
/// 实际清除交给 BuffComponent.RemoveDebuffs(它负责找 isDebuff 的、安全移除)。
///
/// 注意:净化自己一般不需要常驻,配成 RoundTimes=0 或很短即可(清完就走)。
/// 因为在 AddBuff 里 OnCreate 先于"把自己加入列表"执行,所以净化不会误清到自己。
/// 无状态:全场共享一个实例。
/// </summary>
public class Mod_Cleanse : IOnCreate
{
    public void OnCreate(BuffInfo buff, BattleUnit target)
    {
        if (target?.buffCom == null || buff?.data == null) return;

        int count = buff.data.value > 0 ? buff.data.value : -1;   // >0=清几个, 否则全清
        target.buffCom.RemoveDebuffs(count);
    }
}
