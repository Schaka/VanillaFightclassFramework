using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using wManager.Wow.ObjectManager;

interface RotationAction
{
    bool Execute(WoWUnit target, bool force = false);

    float Range();
}

