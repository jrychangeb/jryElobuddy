using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAC_Vayne.Util
{
    public enum EnumSelectorMode
    {
        AutoPriority = 0,
        NearMouse = 1,
        Closest = 2,
        LeastHealth = 3,
        MostAttackDamage = 4,
        MostAbilityPower = 5,
        LessAttacksToKill = 6
    }
}
