using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using robotManager.Helpful;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using wManager.Wow.Bot.States;
using wManager.Events;
using robotManager.MemoryClass;
using wManager.Wow;
using wManager.Wow.Enums;


class RotationSpell : RotationAction
{
    public readonly Spell Spell;
    private readonly string _name;
    private int? _rank;

    public RotationSpell(string name, int? rank = null)
    {
        this._name = name;
        this._rank = rank;
        this.Spell = new Spell(name);
    }

    public bool IsUsable()
    {
        string luaString = @"
        local i = 1
        local rank = {1};
        local lastIndex = 0;
        local lastRank = ""Rank 1"";
        while true do
            local spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL);
            if not spellName then
                break;
            end
   
            -- use spellName and spellRank here
            if(spellName == ""{0}"" and (rank == 0 or (rank > 0 and (spellRank == ""Rank {1}"" or spellRank == """" or spellRank == ""Summon"")))) then
                --DEFAULT_CHAT_FRAME:AddMessage('isUsable: ' .. spellName);
                lastIndex = i;
                lastRank = spellRank;
            end

            i = i + 1;
        end

        if (lastIndex ~= 0) then
            --DEFAULT_CHAT_FRAME:AddMessage('isUsable: ' .. '{0}' .. lastRank);
            PickupSpell(lastIndex, BOOKTYPE_SPELL);
            PlaceAction(1);
            ClearCursor();
            return IsUsableAction(1);
        end
        
        return false;";
        return Lua.LuaDoString<bool>(Extensions.FormatLua(luaString, Spell.Name, LuaRank()));
    }

    public bool CanCast()
    {
        return IsUsable() && GetCooldown() == 0;
    }

    public float GetCooldown()
    {
        return Spell.GetCooldown();
        /*string luaString = @"
        cooldownLeft = 0;

        local start, duration, enabled = GetSpellCooldown(""{0}"");
        if enabled == 1 and start > 0 and duration > 0 then
            cooldownLeft = duration - (GetTime() - start)
        end";
        return Lua.LuaDoString<float>(string.Format(luaString, FullName()), "cooldownLeft");*/
    }

    public void UpdateRank(int rank)
    {
        _rank = rank;
    }
    
    public int GetHighestRankLua()
    {
        string luaString = @"
        local i = 1;
        local List = {{}};
        local spellName, spellRank;            

        while true do
            spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL);
            if not spellName then return table.getn(List) end
    
            if spellName == ""{0}"" then
                _,_,spellRank = string.find(spellRank, "" (%d+)$"");
                spellRank = tonumber(spellRank);
                if not spellRank then return 0 end
                List[spellRank] = i;
            end
            i = i + 1;
        end";
        return Lua.LuaDoString<int>(Extensions.FormatLua(luaString, _name));
    }

    public int GetRank()
    {
        return _rank ?? 1;
    }

    private int LuaRank()
    {
        return _rank ?? 0;
    }


    public string FullName()
    {
        return _rank != null ? (_name + "(Rank " + _rank + ")") : _name;
    }

    public bool IsKnown()
    {
        //not a great runtime solution, but spellbook should get updated on newly learned spells
        //this SHOULD check if we have the rank available
        string luaString = @"
        local i = 1
        while true do
            local spellName, spellRank = GetSpellName(i, BOOKTYPE_SPELL);
            if not spellName then
                break;
            end
   
            -- use spellName and spellRank here
            if(spellName == ""{0}"" and (spellRank == ""Rank {1}"" or spellRank == """" or spellRank == ""Summon"")) then
                --DEFAULT_CHAT_FRAME:AddMessage('know spell: ' .. spellName);
                return true;
            end

            i = i + 1;
        end
        return false;";
        return Lua.LuaDoString<bool>(Extensions.FormatLua(luaString, _name, GetRank()));
    }
    
    public bool Execute(WoWUnit target, bool force)
    {
        bool success = CombatUtil.CastSpell(this, target, force);
        if (success)
            Logging.WriteFight("Fightclass successfully casted: " + this.FullName());
        return success;
    }

    public float Range()
    {
        return Spell.MaxRange;
    }

    public override int GetHashCode()
    {
        return _name.GetHashCode() + _rank.GetHashCode();
    }

    public override bool Equals(object obj)
    {
        RotationSpell otherObj = (RotationSpell)obj;
        return _name.Equals(otherObj._name) && _rank == otherObj._rank;
    }
    
}