using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading;
using robotManager;
using robotManager.FiniteStateMachine;
using robotManager.Helpful;
using wManager.Wow.Class;
using wManager.Wow.Helpers;
using wManager.Wow.ObjectManager;
using wManager.Wow.Bot.States;
using wManager.Events;
using robotManager.MemoryClass;
using wManager.Wow;
using wManager.Wow.Enums;
using System.Threading.Tasks;
using robotManager.Events;
using wManager;

public class Main : ICustomClass
{
    public float Range => Frostbolt.IsKnown() ? Frostbolt.Spell.MaxRange : 30;


    private bool _isLaunched;

    private List<RotationSpell> PartyBuffs = new List<RotationSpell>{
        new RotationSpell("Arcane Intellect"),
        new RotationSpell("Dampen Magic"),
    };

    private List<RotationSpell> Buffs = new List<RotationSpell>{
        new RotationSpell("Arcane Intellect"),
        new RotationSpell("Ice Barrier"),
        new RotationSpell("Dampen Magic"),
    };

    private static WoWLocalPlayer Me = ObjectManager.Me;

    private static RotationSpell Frostbolt = new RotationSpell("Frostbolt");
    private static RotationSpell Armor = new RotationSpell("Frost Armor");
    private static RotationSpell IceArmor = new RotationSpell("Ice Armor");

    private static String _LastSpell;
    private static DateTime _LastCastTimeStamp = DateTime.Now;


    private List<RotationStep> RotationActions = new List<RotationStep>{
        new RotationStep(new RotationSpell("Shoot"), 0.9f, (s, t) => (t.HealthPercent <= 10 || Me.ManaPercentage <= 10) && !Me.CastingSpell("Evocation") && CombatUtil.CanWand(), CombatUtil.BotTarget),
        new RotationStep(new RotationRawAction(() => ItemsManager.UseItem(5513)) , 1f, (s, t) => ItemsManager.HasItemById(5513) && Me.ManaPercentage <= 30, CombatUtil.FindMe),
        new RotationStep(new RotationLua("PetAttack();") , 1.5f, (s, t) => ItemsManager.HasItemById(5513) && Me.ManaPercentage <= 30, CombatUtil.FindMe),
        new RotationStep(new RotationSpell("Cone of Cold"), 2f, (s, t) => _LastSpell == "Frostbolt" && _LastCastTimeStamp.AddMilliseconds(500) > DateTime.Now && t.HaveAnyDebuff("Freeze", "Frostbite", "Frost Nova") && t.GetDistance <= 10, CombatUtil.BotTarget),
        new RotationStep(new RotationSpell("Frostbolt"), 3f, (s, t) => true, CombatUtil.BotTarget),
        new RotationStep(new RotationSpell("Fireball"), 4f, (s, t) => !Frostbolt.IsKnown(), CombatUtil.BotTarget),
        new RotationStep(new RotationSpell("Shoot"), 5f, (s, t) => !Frostbolt.CanCast() && !Me.IsCast && !Me.CastingSpell("Evocation") && CombatUtil.CanWand(), CombatUtil.BotTarget),
    };

    public void Initialize()
    {
        wManagerSetting.CurrentSetting.UseLuaToMove = true;
        wManagerSetting.CurrentSetting.RestingMana = true;

        _isLaunched = true;

        VanillaFightclassSetting.Load();
        CombatUtil.Start();

        RotationActions.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        UpdateRanks();

        FiniteStateMachineEvents.OnAfterRunState += SpellUpdateHandler;
        EventsLuaWithArgs.OnEventsLuaWithArgs += CastingEventHandler;

        Logging.Write("Loaded VanillaFightClass");

        Rotation();
    }
    
    private void SpellUpdateHandler(Engine engine, State state)
    {
        if (state.GetType() == typeof(Trainers))
        {
            UpdateRanks();
        }
    }

    private void CastingEventHandler(LuaEventsId id, List<string> args)
    {
        switch (id)
        {
            case LuaEventsId.SPELLCAST_START:
                _LastSpell = args[0];
                _LastCastTimeStamp = DateTime.Now.AddMilliseconds(double.Parse(args[1]));
                break;
            case LuaEventsId.SPELLCAST_DELAYED:
                _LastCastTimeStamp = _LastCastTimeStamp.AddMilliseconds(double.Parse(args[0]));
                break;
        }
    }

    private void Rotation()
    {
        while (_isLaunched)
        {
            try
            {
                if (Conditions.InGameAndConnectedAndAliveAndProductStartedNotInPause && !ObjectManager.Me.IsDead)
                {
                    UseBuffs();

                    if((VanillaFightclassSetting.CurrentSetting.FrameLock || CombatUtil.GetGlobalCooldown() == 0) && (Fight.InFight || Conditions.IsAttackedAndCannotIgnore) && ObjectManager.Target.IsAttackable)
                    {
                        var watch = System.Diagnostics.Stopwatch.StartNew();
                        
                        if (VanillaFightclassSetting.CurrentSetting.FrameLock && Hook.AllowFrameLock && !Memory.WowMemory.FrameIsLocked)
                        {
                            Memory.WowMemory.LockFrame();
                        }

                        foreach (var step in RotationActions)
                        {
                            if (step.ExecuteStep())
                            {
                                break;
                            }
                        }

                        if (Hook.AllowFrameLock && Memory.WowMemory.FrameIsLocked)
                        {
                            Memory.WowMemory.UnlockFrame(true);
                        }
                        
                        watch.Stop();
                        if (watch.ElapsedMilliseconds > 100)
                        {
                            Logging.WriteFight("Iteration took " + watch.ElapsedMilliseconds + "ms");
                        }
                    } 
                }
            }catch(Exception e)
            {
                Logging.WriteError("VanillaFightClass ERROR:" + e);
            }
            Thread.Sleep(VanillaFightclassSetting.CurrentSetting.FrameLock ? 100 : (int)(CombatUtil.GetGlobalCooldown() * 1000));
        }
    }

    private void UpdateRanks()
    {
        if (IceArmor.IsKnown())
        {
            Armor = IceArmor;
        }
    }

    private void UseBuffs()
    {
        if (ObjectManager.Me.IsMounted || ObjectManager.Me.InCombat || Fight.InFight)
            return;

        Buffs.ForEach(CombatUtil.CastBuff);
        if (ObjectManager.Me.HaveBuff("Preparation"))
        {
            ObjectManager.GetObjectWoWPlayer().ForEach(o => {
                PartyBuffs.ForEach(b => CombatUtil.CastBuff(b, o));
            });
        }

        CombatUtil.CastBuff(Armor, Me);
    }

    public void Dispose()
    {
        _isLaunched = false;

        if (Hook.AllowFrameLock && Memory.WowMemory.FrameIsLocked)
        {
            Memory.WowMemory.UnlockFrame(true);
        }

        CombatUtil.Stop();

        FiniteStateMachineEvents.OnAfterRunState -= SpellUpdateHandler;

        Logging.Write("Unloaded Vanilla Fightclass");
    }

    public void ShowConfiguration()
    {
        VanillaFightclassSetting.Load();
        VanillaFightclassSetting.CurrentSetting.ToForm();
        VanillaFightclassSetting.CurrentSetting.Save();
    }

}

/*
 * SETTINGS
*/

[Serializable]
public class VanillaFightclassSetting : Settings
{

    [Setting]
    [DefaultValue(true)]
    [Category("General")]
    [DisplayName("Framelock")]
    [Description("Lock frames before each combat rotation (can help if it skips spells)")]
    public bool FrameLock { get; set; }

    public VanillaFightclassSetting()
    {
        FrameLock = true;
    }

    public static VanillaFightclassSetting CurrentSetting { get; set; }

    public bool Save()
    {
        try
        {
            return Save(AdviserFilePathAndName("CustomClass-VanillaFightClass", ObjectManager.Me.Name + "." + Usefuls.RealmName));
        }
        catch (Exception e)
        {
            Logging.WriteError("VanillaFightClass > Save(): " + e);
            return false;
        }
    }

    public static bool Load()
    {
        try
        {
            if (File.Exists(AdviserFilePathAndName("CustomClass-VanillaFightClass", ObjectManager.Me.Name + "." + Usefuls.RealmName)))
            {
                CurrentSetting =
                    Load<VanillaFightclassSetting>(AdviserFilePathAndName("CustomClass-VanillaFightClass",
                                                                 ObjectManager.Me.Name + "." + Usefuls.RealmName));
                return true;
            }
            CurrentSetting = new VanillaFightclassSetting();
        }
        catch (Exception e)
        {
            Logging.WriteError("VanillaFightClass > Load(): " + e);
        }
        return false;
    }
}