using System.Collections.Generic;

/// <summary>类型 → 干活的人 的花名册。
///   BuffComponent=包工头(管背包/tick/增删)  Factory=派工台(按buffType给工人)  Mod_=工人(真干活)</summary>
public static class BuffModuleFactory
{
    private static readonly Dictionary<BuffType, object> _mods = new Dictionary<BuffType, object>
    {
        { BuffType.AttrMod,    new Mod_Attr()      },
        { BuffType.SwordPower, new Mod_SwordPower() },
        { BuffType.Dot,        new Mod_Dot()       },   // 持续扣血
        { BuffType.Cleanse,    new Mod_Cleanse()   },   // 净化(清负面)
        // TODO: Hot/Shield/Stun/DmgMark 后续补
    };

    public static object Get(BuffType type)
    {
        _mods.TryGetValue(type, out var m);
        return m;
    }
}