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
    public class Gunbreaker : KupoRoutine
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
            get { return new[] { ClassJobType.Gunbreaker, }; }
        }

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return SummonChocobo();
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentTPPercent);
        }


        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(SummonChocobo());
        }

        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        new Decorator(ctx => !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement), new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit")
                        )),
                        Spell.PullCast(r => "Solid Barrel", r => ActionManager.LastSpell.Name == "Brutal Shell"),
                        Spell.PullCast(r => "Brutal Shell", r => ActionManager.LastSpell.Name == "Keen Edge"),
                        Spell.PullCast("Keen Edge")
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

                        Spell.Cast("Interject", r => shouldInterject),

                        Spell.Cast(r => "Wicked Talon", r => ActionResourceManager.Gunbreaker.SecondaryComboStage == 2),
                        Spell.Cast(r => "Savage Claw", r => ActionResourceManager.Gunbreaker.SecondaryComboStage == 1),
                        Spell.Cast(r => "Gnashing Fang", r => ActionResourceManager.Gunbreaker.Cartridge > 0 && ActionResourceManager.Gunbreaker.SecondaryComboStage == 0),

                        Spell.Cast(r => "Solid Barrel", r => ActionManager.LastSpell.Name == "Brutal Shell"),
                        Spell.Cast(r => "Brutal Shell", r => ActionManager.LastSpell.Name == "Keen Edge"),
                        Spell.Cast("Sonic Break"),
                        Spell.Cast("Keen Edge")
                        )));
        }
    }
}