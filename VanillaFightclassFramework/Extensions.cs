using System;
using System.Collections.Generic;
using System.Linq;
using robotManager.Helpful;
using System.Text;
using wManager.Wow.ObjectManager;
using wManager.Wow.Helpers;
using wManager.Wow.Class;

public static class Extensions
{
    public static bool HasDebuffType(this WoWUnit unit, string type)
    {
        string luaString = @"hasDebuff = false;
        for i=1,40 do
	        local _, _ debuffType = UnitDebuff(""{1}"", i);
            if debuffType == ""{0}"" then
                hasDebuff = true
                break;
            end
        end";
        return Lua.LuaDoString<bool>(FormatLua(luaString, type, CombatUtil.GetLuaId(unit)), "hasDebuff");
    }

    public static bool HaveDebuff(this WoWUnit unit, string name)
    {
        return unit.HaveBuff(name);
    }

    public static bool IsCasting(this WoWUnit unit)
    {
        return unit.IsCast;
    }

    public static bool CastingSpell(this WoWUnit unit, params string[] names)
    {
        foreach(string name in names){
            if(name == unit.CastingSpell.Name)
            {
                return true;
            }
        }
        return false;
    }

    public static bool HasMana(this WoWUnit unit)
    {
        string luaString = @"return (UnitPowerType(""{0}"") == 0 and UnitMana(""{0}"") > 1)";
        return Lua.LuaDoString<bool>(FormatLua(luaString, CombatUtil.GetLuaId(unit)));
    }

    public static bool HaveAnyDebuff(this WoWUnit unit, params string[] names)
    {
        foreach(string name in names)
        {
            if (unit.HaveBuff(name))
            {
                return true;
            }
        }
        return false;
    }

    public static bool IsCreatureType(this WoWUnit unit, string type)
    {
        string luaString = @"isCreatureType = (UnitCreatureType(""{1}"") == ""{0}"" or false);";
        return Lua.LuaDoString<bool>(FormatLua(luaString, type, CombatUtil.GetLuaId(unit)), "isCreatureType");
    }

    public static bool HaveAllDebuffs(this WoWUnit unit, params string[] names)
    {
        foreach(string name in names)
        {
            if (!unit.HaveBuff(name))
            {
                return false;
            }
        }
        return true;
    }

    public static bool IsPlayer(this WoWUnit unit)
    {
        string luaString = @"isPlayer = (UnitIsPlayer(""{0}"") == 1)";
        return Lua.LuaDoString<bool>(FormatLua(luaString, CombatUtil.GetLuaId(unit)), "isPlayer");
    }

    public static bool AffectingCombat(this WoWLocalPlayer me)
    {
        return Lua.LuaDoString<bool>(@"return (UnitAffectingCombat(""player"") ~= nil)");
    }

    public static float GetCooldown(this Spell spell)
    {
        string luaString = @"
        cooldownLeft = 0;
        local i = 1
        while true do
            local spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL);
            if not spellName then
                break;
            end
   
            -- use spellName and spellRank here
            if(spellName == ""{0}"") then
                local start, duration, enabled = GetSpellCooldown(i, BOOKTYPE_SPELL);
                if enabled == 1 and start > 0 and duration > 0 then
                    cooldownLeft = duration - (GetTime() - start)
                    --DEFAULT_CHAT_FRAME:AddMessage(""cooldown left on "" .. "" .. spellName .. "" .. cooldownLeft);
                end
            end

            i = i + 1;
        end";
        return Lua.LuaDoString<float>(FormatLua(luaString, spell.Name), "cooldownLeft");
    }

    public static string FormatLua(string str, params object[] names)
    {
        return string.Format(str, names.Select(s => s.ToString().Replace("'", "\\'").Replace("\"", "\\\"")).ToArray());
    }

    private static string LuaAndCondition(string[] names, string varname)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var name in names)
        {
            sb.Append(" and " + varname + " == \"" + name + "\"");
        }
        return sb.ToString().Substring(5);
    }

    private static string LuaOrCondition(string[] names, string varname)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var name in names)
        {
            sb.Append(" or " + varname + " == \"" + name + "\"");
        }
        return sb.ToString().Substring(4);
    }

    private static string LuaTable(string[] names)
    {
        string returnValue = "{";
        foreach (var name in names)
        {
            returnValue += "[\"" + name + "\"] = false,";
        }
        return returnValue += "};";
    }
}
