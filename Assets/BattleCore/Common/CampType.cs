/// <summary>
/// 阵营:标记一个战斗单位属于"哪一方"。
/// 战斗里判断敌我(能不能打、找目标、AI选敌)都靠它,
/// 一句 if (a.Camp != b.Camp) 就能分清对手。
/// 铁律:这个枚举只回答"哪一方",别的概念(是不是Boss/召唤)另开枚举。
/// </summary>
public enum CampType
{
    Player,   // 我方
    Enemy,    // 敌方
    // Neutral,   // (可选)中立:野怪,谁都能打/谁都不主动打
}
