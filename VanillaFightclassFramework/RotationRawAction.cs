using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wManager.Wow.ObjectManager;

class RotationRawAction : RotationAction
{
    private readonly Action _rotationAction;
    private readonly float _actionRange;

    public RotationRawAction(Action RotationAction, float ActionRange = 30)
    {
        this._rotationAction = RotationAction;
        this._actionRange = ActionRange;
    }

    public bool Execute(WoWUnit target, bool force = false)
    {
        _rotationAction.Invoke();
        return true;
    }

    public float Range()
    {
        return _actionRange;
    }
}

