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
            get { return 25f; }
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
				/*
        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return new PrioritySelector(
				Spell.Cast("Toad Oil", r=> !Core.Me.HasAura("Toad Oil")),
				//Spell.Cast("Bristle", r=> !Core.Me.HasAura("Boost")),
				Spell.Cast("Ice Spikes", r => !Core.Me.HasAura("Ice Spikes"))
			);
        }*/

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
										Spell.PullCast(r => "Sticky Tongue", r => ActionManager.LastSpell.Name != "Sticky Tongue" && Core.Target.Distance2D(Core.Me) >= 5f),
                    Spell.PullCast(r => "Water Cannon", r => ActionManager.LastSpell.Name != "Water Cannon" && !ActionManager.HasSpell("Sticky Tongue"))
                )));
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Cast("White Wind", r => Core.Me.CurrentHealthPercent <= 60),
								Spell.Cast("Exuviation", r => Core.Me.HasAura("Paralysis"))
                );
        }
				
        protected bool shouldFishSlap
        {
            get
            {
                var target = Core.Target as BattleCharacter;
                if (target != null)
                {
                    if (target.SpellCastInfo.IsCasting && target.SpellCastInfo.Interruptible &&
                        //Don't kick as soon as possible, kick with the reaction speed of a fast human
                        target.SpellCastInfo.CurrentCastTime.TotalMilliseconds >= 500)
                        return true;
                }

                return false;
            }
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
						//CombatBuffs							
						Spell.Cast("Toad Oil", r=> !Core.Me.HasAura("Toad Oil")),						
						
						//Interrupt
						Spell.Cast("Flying Sardine", r => shouldFishSlap),
						
						//Basic Combat
						Spell.Cast("Triple Trident", r => Core.Me.HasAura("Tingling") && Core.Me.HasAura("Harmonized")),						
						Spell.Cast("Whistle", r=> ActionManager.LastSpell.Name == "Tingle"),						
						Spell.Cast("Tingle", r => DataManager.GetSpellData(23264).Cooldown.TotalMilliseconds == 0 && Core.Me.CurrentTarget.CurrentHealthPercent > 30 && Core.Target.Distance2D(Core.Me) <= 6f),
						Spell.Cast("Lucid Dreaming", r => ActionManager.HasSpell("Lucid Dreaming") && Core.Player.CurrentManaPercent < 50),						
						Spell.Cast("Blood Drain", r => Core.Player.CurrentManaPercent < 20),						
						Spell.Cast("Off-guard", r => Core.Me.CurrentTarget.CurrentHealthPercent < 50),
						Spell.Cast("Bad Breath", r=> ActionManager.LastSpell.Name != "Bad Breath" && Core.Me.IsFacing(Core.Target) && Core.Target.Distance2D(Core.Me) <= 8f && !Core.Target.HasAura("Poison") && Core.Me.CurrentTarget.CurrentHealthPercent > 40),
						Spell.Cast("Plaincracker", r => GameObjectManager.NumberOfAttackers >= 2 && Core.Target.Distance2D(Core.Me) <= 4f),
						Spell.Cast("Whistle", r=> !Core.Me.HasAura("Boost") && !Core.Me.HasAura("Harmonized") && !Core.Target.HasAura("Off-guard") && ActionManager.HasSpell("Abyssal Transfixion")),											
						Spell.Cast("Abyssal Transfixion", r => true),
						Spell.Cast("1000 Needles", r => Core.Me.CurrentTarget.CurrentHealthPercent > 75 && ActionManager.LastSpell.Name != "1000 Needles"),					
            Spell.Cast("Water Cannon", r => true)
                        )));
        }
    }
}