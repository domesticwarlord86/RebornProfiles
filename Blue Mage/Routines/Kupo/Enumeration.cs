using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Kupo
{
    [Flags]
    public enum GameContext
    {
        None = 0,
        Normal = 0x1,
        Instances = 0x2,
        PvP = 0x4,

        All = Normal | Instances | PvP,
    }

    [Flags]
    public enum BehaviorType
    {
        Rest = 0x1,
        PreCombatBuffs = 0x2,
        Pull = 0x4,
        Heal = 0x8,
        CombatBuffs = 0x10,
        Combat = 0x20,

        // this is no guarantee that the bot is in combat
        InCombat = Heal | CombatBuffs | Combat,
        // this is no guarantee that the bot is out of combat
        OutOfCombat = Rest | PreCombatBuffs,

        All = Rest | PreCombatBuffs | Pull | Heal | CombatBuffs | Combat,
    }
}