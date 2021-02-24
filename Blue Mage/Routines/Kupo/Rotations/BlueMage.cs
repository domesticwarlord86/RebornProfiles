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
    public class BlueMage : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override float PullRange
        {
            get { return 5f; }
        }

        public override ClassJobType[] Class
        {
            get { return new [] {ClassJobType.BlueMage}; }
        }
        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(SummonChocobo());
        }
        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return new PrioritySelector(
				Spell.Cast("Toad Oil", r=> !Core.Me.HasAura("Toad Oil"))
				//Spell.Cast("Bristle", r=> !Core.Me.HasAura("Boost")),
				//Spell.Cast("Ice Spikes", r => !Core.Me.HasAura("Ice Spikes"))
			);
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentManaPercent);
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
					Spell.PullCast("Sticky Tongue"),
                    Spell.PullCast("Water Cannon")
                )));
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Cast("White Wind", r => Core.Me.CurrentHealthPercent <= 80)
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
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange, true, "Moving to unit")
                        )),
						//Spell.Apply("Off-guard", r => !Core.Me.TargetCharacter.HasAura("Off-guard")),
						//Spell.Cast("Faze", r => !Core.Me.TargetCharacter.IsCasting),
						Spell.Cast("Toad Oil", r=> !Core.Me.HasAura("Toad Oil")),
						Spell.Cast("Abyssal Transfixion", r => true),
						Spell.Cast("1000 Needles", r => true),
                        Spell.Cast("Blood Drain", r => Core.Player.CurrentManaPercent < 60),
						//Spell.Apply("Bad Breath", r=> !Core.Me.TargetCharacter.HasAura("Poison")),
                        Spell.Cast("Water Cannon", r => true)
                        )));
        }
    }
}