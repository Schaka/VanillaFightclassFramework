using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;

class RotationLua : RotationAction
{
    private readonly string _luaAction;
    private readonly float _actionRange;

    public RotationLua(string Lua, float Range = 30)
    {
        this._luaAction = Lua;
        this._actionRange = Range;
    }

    public bool Execute(WoWUnit target, bool force = false)
    {
        if(force && ObjectManager.Me.IsCast)
        {
            Lua.LuaDoString("SpellStopCasting();");   
        }
        Lua.LuaDoString(_luaAction);

        return true;
    }

    public float Range()
    {
        return _actionRange;
    }
}

