/// <summary>
/// 单位类型:标记这个单位"是什么种类",和阵营(哪一方)是两个独立维度。
///   Camp   回答"哪一方"(Player / Enemy)
///   UnitType 回答"是什么"(玩家/怪/召唤物/NPC)
/// 一个单位两个都要:比如召唤物 = Camp:Player + UnitType:Summon。
/// 分开的好处:AI、结算、表现可以按"种类"区别对待,又不影响敌我判断。
/// </summary>
public enum BattleUnitType
{
    Player,    // 玩家角色(我方主战单位)
    Monster,   // 敌方怪物
    Summon,    // 召唤物(可能属我方,也可能属敌方 → 靠 Camp 区分)
    NPC,       // 中立/剧情单位,一般不参与战斗结算
}
