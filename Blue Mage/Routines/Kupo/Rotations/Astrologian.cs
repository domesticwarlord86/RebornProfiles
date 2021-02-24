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
    public class Astrologian : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] { ClassJobType.Astrologian }; }
        }

        public override float PullRange
        {
            get { return 20; }
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
    new Decorator(ctx => ctx != null, new PrioritySelector(
new Decorator(ctx => !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement), new PrioritySelector(
             CommonBehaviors.MoveToLos(ctx => ctx as BattleCharacter),
            CommonBehaviors.MoveAndStop(ctx => (ctx as BattleCharacter).Location, PullRange, true, "Moving to unit")
)),
            Spell.PullCast(s => ActionManager.HasSpell("CombustII") ? "Combust II" : "Combust"),
            Spell.PullCast("Malefic")
    )));
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
                SummonChocobo(),
                Spell.Apply("Protect", r => Core.Player)
                );
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                //If we have a Enhanced Benefic II and we have Benefic II use it!
                Spell.Cast("Aspected Benefic", r => !Core.Player.HasAura("Aspected Benefic") && Core.Player.CurrentHealthPercent <= 80, r => Core.Player),
                Spell.Cast("Benefic II", r => Core.Player.HasAura("Enhanced Benefic II"), r => Core.Player),
                Spell.Cast("Benefic", r => Core.Player.CurrentHealthPercent <= 40, r => Core.Player),
                Spell.Cast("Essential Dignity", r => Core.Player.CurrentHealthPercent <= 20, r => Core.Player)
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
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                        )),

                        Spell.Apply("Cleric Stance", r => Core.Player),
                        Spell.Apply("Diurnal Sect", r => Core.Player),    
                        
                        Spell.Cast("Luminiferous Aether", r => (Core.Player.MaxMana - Core.Player.CurrentMana > 900), r => Core.Player),
                       
                        Spell.Apply(s =>  ActionManager.HasSpell("Combust II") ? "Combust II" : "Combust", null, r => Core.Player.CurrentTarget),
                        
                        Spell.Cast("Malefic III"),
                        Spell.Cast("Malefic II"),
                        Spell.Cast("Malefic")
                        )));
        }
    }
}