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
    public class Dragoon : KupoRoutine
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
            get { return new[] { ClassJobType.Dragoon }; }
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Cast("Bloodbath", r => Core.Player.CurrentHealthPercent < 60, r => Core.Player),
                Spell.Cast("Second Wind", r => Core.Player.CurrentHealthPercent < 40, r => Core.Player)
            );
        }

        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(SummonChocobo());
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

                        Spell.PullCast(r => "Full Thrust", r => ActionManager.LastSpell.Name == "Vorpal Thrust"),
                        Spell.PullCast(r => "Vorpal Thrust", r => ActionManager.LastSpell.Name == "True Thrust"),
                        Spell.PullCast("True Thrust")
            )));
        }


        private readonly SpellData Jump = DataManager.GetSpellData("Jump");

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
                        Spell.Cast("Heavy Thrust", r =>  !Core.Player.HasAura("Heavy Thrust",true,3000)),
                        
                        Spell.Cast("BattleLitany"),

                        Spell.Apply(r => "Chaos Thrust", r => ActionManager.LastSpell.Name == "Disembowel", msLeft: 3000),
                        Spell.Cast(r => "Disembowel", r => ActionManager.LastSpell.Name == "Impulse Drive"),
                        Spell.Cast("Impulse Drive", r => !Core.Player.CurrentTarget.HasAura("Chaos Thrust", true, 6500) && ActionManager.HasSpell("Disembowel")),
                        Spell.Cast("Full Thrust", r => ActionManager.LastSpell.Name == "Vorpal Thrust"),
                        Spell.Cast("Vorpal Thrust", r => ActionManager.LastSpell.Name == "True Thrust"),
                        //Spell.Apply("Phlebotomize", r => (r as BattleCharacter).TimeToDeath() > 8 && ActionManager.LastSpell.Name != "Impulse Drive" && ActionManager.LastSpell.Name != "Disembowel"),
                        Spell.Cast("True Thrust", r => true),
                        
                        Spell.Cast("Dragonfire Dive"),
                        Spell.Cast("Fang and Claw", r => ActionResourceManager.Dragoon.Timer != TimeSpan.Zero),
                        Spell.Cast("Geirskogul", r => ActionResourceManager.Dragoon.Timer != TimeSpan.Zero),
                        Spell.Cast("Blood of the Dragon"),
                        Spell.Cast("Invigorate", r => Core.Player.CurrentTP < 500),
                        Spell.Cast("Spineshatter Dive"),
                        Spell.Cast("Jump", r => Core.Player.ClassLevel < 45)
                        )));
        }
    }
}