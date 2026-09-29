/// <summary>buff 行为模块接口(对标 ISkillLogic):每个接口 = 一个触发时机(工序)。
/// 一个工人(Mod_)只实现它关心的时机;Component 用 (Get(type) as IOnXxx)?. 自动筛选谁响应。</summary>

public interface IOnCreate { void OnCreate(BuffInfo buff, BattleUnit target); }   // 挂上/叠层
public interface IOnRemove { void OnRemove(BuffInfo buff, BattleUnit target); }   // 移除(还属性)
public interface IOnTick { void OnTick(BuffInfo buff, BattleUnit target); }     // 每隔N回合(DoT/HoT)
public interface IOnHit { void OnHit(BuffInfo buff, BattleUnit attacker, DamageInfo dmg); }  // 造成伤害时
public interface IOnBehurt { void OnBehurt(BuffInfo buff, BattleUnit target, DamageInfo dmg); } // 受伤时(反弹/护盾)
public interface IOnKill { void OnKill(BuffInfo buff, BattleUnit victim); }     // 自己死亡时
public interface IOnBeKill { void OnBeKill(BuffInfo buff, BattleUnit killer, BattleUnit victim); } // 击杀敌人时
public interface IOnCasterCast { void OnCasterCast(BuffInfo buff, BattleUnit caster, bool isDamageSkill); } // 放完技能(如剑势清零)
