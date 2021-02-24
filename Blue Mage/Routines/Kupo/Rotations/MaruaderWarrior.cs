using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
    public class MaruaderWarrior : KupoRoutine
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
            get { return new [] {ClassJobType.Marauder, ClassJobType.Warrior,}; }
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
                new Decorator(ctx => ctx != null,new PrioritySelector(
                    new Decorator(ctx => !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement), new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit")
                    )),
                    Spell.PullCast("Heavy Swing")
                )));
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                //Spell.Apply("Foresight", r => Core.Player.CurrentHealthPercent <= 70,r=>Core.Player),
                //Spell.Apply("Bloodbath", r => Core.Player.CurrentHealthPercent <= 50),
                Spell.Cast("Berserk", r => Core.Player.CurrentHealthPercent <= 80 || GameObjectManager.NumberOfAttackers >= 2),
                Spell.Cast("Rampart", r => Core.Player.CurrentHealthPercent <= 60),
                Spell.Cast("Inner Beast", r => Core.Player.CurrentHealthPercent <= 60 && Core.Player.HasAura("Infuriated")),
                Spell.Cast("Convalescence", r => Core.Player.CurrentHealthPercent <= 50),
                Spell.Apply("Thrill of Battle", r => Core.Player.CurrentHealthPercent <= 30),
                Spell.Cast("Equilibrium", r => Core.Me.CurrentHealthPercent < 75)
                );
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
                        
                        Spell.Apply("Defiance", ctx => Core.Player),


                        Spell.Cast("Upheaval", r => ActionResourceManager.Warrior.BeastGauge >= 20 || Core.Player.HasAura("Inner Release")),
                        Spell.Cast("Inner Beast", r => ActionResourceManager.Warrior.BeastGauge >= 50 || Core.Player.HasAura("Inner Release")),

                        Spell.Cast("Vengeance", r => GameObjectManager.NumberOfAttackers > 1 || Core.Me.CurrentHealthPercent < 50),
                        //Spell.Cast("Steel Cyclone", r => ActionResourceManager.Warrior.BeastGauge >= 50 && GameObjectManager.NumberOfAttackers > 1),
                        
                        Spell.Cast("Infuriate", r=> ActionResourceManager.Warrior.BeastGauge < 50 && Core.Me.InCombat),
                        
                        Spell.Cast("Storm's Eye", r => ActionManager.LastSpell.Name == "Maim" && !Core.Player.HasAura("Storm's Eye",true,5000)),
                        Spell.Cast("Storm's Path", r => ActionManager.LastSpell.Name == "Maim"),
                        Spell.Cast("Maim", r => ActionManager.LastSpell.Name == "Heavy Swing"),
                        Spell.Cast("Heavy Swing", r => true)
                        )));
        }
    }
}