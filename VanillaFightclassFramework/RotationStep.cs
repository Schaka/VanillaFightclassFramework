using System;
using robotManager.Helpful;
using wManager.Wow.Class;
using wManager.Wow.ObjectManager;
using System.Threading;
using wManager.Wow.Helpers;

class RotationStep
{
    public RotationAction Action;
    public float Priority;
    public Func<RotationAction, WoWUnit, bool> Predicate;
    public Func<Func<WoWUnit, bool>, WoWUnit> TargetFinder;
    public bool Force = false;
    public bool CheckRange = true;

    public RotationStep(RotationAction spell, float priority, Func<RotationAction, WoWUnit, bool> predicate)
    {
        this.Action = spell;
        this.Priority = priority;
        this.Predicate = predicate;
        this.TargetFinder = CombatUtil.FindFriend;
    }

    public RotationStep(RotationAction spell, float priority, Func<RotationAction, WoWUnit, bool> predicate, Func<Func<WoWUnit, bool>, WoWUnit> targetFinder)
        : this(spell, priority, predicate)
    {
        this.TargetFinder = targetFinder;
    }

    public RotationStep(RotationAction spell, float priority, Func<RotationAction, WoWUnit, bool> predicate, Func<Func<WoWUnit, bool>, WoWUnit> targetFinder, bool force = false, bool checkRange = true)
        : this(spell, priority, predicate, targetFinder)
    {
        this.Force = force;
        this.CheckRange = checkRange;
    }

    public bool ExecuteStep()
    {
        //predicate is executed separately from targetfinder predicate
        //this way we can select one target, then check which spell to cast on the target
        //as opposed to finding a target to cast a specific spell on (not the desired result)
        Func<WoWUnit, bool> targetFinderPredicate = CheckRange ? (Func<WoWUnit, bool>)((u) => u.GetDistance <= Action.Range()) : ((u) => true);
        WoWUnit target = TargetFinder(targetFinderPredicate);
        if (target != null && Predicate(Action, target))
        {
            return Action.Execute(target, Force);
        }
        return false;
    }
}
