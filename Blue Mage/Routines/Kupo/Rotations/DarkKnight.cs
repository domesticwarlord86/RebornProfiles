using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using Kupo.Helpers;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Kupo.Rotations
{
    public class DarkKnight : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override float PullRange
        {
            get { return 2.5f; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] { ClassJobType.DarkKnight }; }
        }

        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(SummonChocobo());
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentTPPercent);
        }

        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
                        return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement), new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit"),

                        Spell.PullCast("Unmend"),
                        Spell.Cast("Souleater", r => ActionManager.LastSpell.Name == "Syphon Strike"),
                        Spell.Cast("Syphon Strike", r => ActionManager.LastSpell.Name == "Hard Slash"),
                        Spell.Cast("Hard Slash")
                )));
        }



        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        new Decorator(ctx => !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement), new PrioritySelector(
                            CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                            CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit")
                        )),


                        Spell.Cast("Interject",r=> shouldInterject),
                        //Darkside activation and MP Control to keep it up as long as possible
                        //Spell.Apply("Darkside", r => Core.Player.CurrentManaPercent >= 85, r => Core.Player),						
                        //Spell.Cast("Blood Weapon", r => Core.Player.CurrentManaPercent <= 75, r => Core.Player),
                        //Spell.Cast("Blood Price", r => Core.Player.CurrentManaPercent <= 50, r => Core.Player),


                        Spell.Cast("Bloodspiller"),

                        Spell.Cast("Edge of Darkness", r => ActionResourceManager.DarkKnight.DarksideRemaining.TotalSeconds < 5 || Core.Player.CurrentMana > 6000),
                        Spell.Cast("Flood of Darkness", r => ActionResourceManager.DarkKnight.DarksideRemaining.TotalSeconds < 5),

                        Spell.Cast("Abyssal Drain", r => Core.Player.CurrentHealthPercent <= 70),

                        //Check health and see if we need to reduce the dmg for 20 secs
                        Spell.Cast("Shadowskin", r => Core.Player.CurrentHealthPercent <= 70, r => Core.Player),

                        Spell.Cast("Blood Weapon", r => Core.Player.CurrentMana < 9000, r=>Core.Player),
                        Spell.Cast("Carve and Spit", r => Core.Player.CurrentManaPercent <= 90),


                        //Basic Combat Combo Routine
                        Spell.Cast("Souleater", r => ActionManager.LastSpell.Name == "Syphon Strike"),
                        Spell.Cast("Syphon Strike", r => ActionManager.LastSpell.Name == "Hard Slash"),
                        Spell.Cast("Hard Slash")




                        )));
        }
    }
}