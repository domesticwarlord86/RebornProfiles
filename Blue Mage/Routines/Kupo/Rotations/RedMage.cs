using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using Kupo;
using Kupo.Helpers;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Kupo.Rotations
{
    internal class RedMage : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] { ClassJobType.RedMage }; }
        }

        public override float PullRange
        {
            get { return 20; }
        }

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return SummonChocobo();
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return
                DefaultRestBehavior(r => Core.Player.CurrentManaPercent);
        }


        private readonly string[] PullSpells = new[] { "Jolt II", "Jolt", "Riposte" };
        private string _BestPullSpell;
        private uint _level;
        private string BestPullSpell
        {
            get
            {

                if (_level != Core.Player.ClassLevel)
                {
                    _level = Core.Player.ClassLevel;
                    _BestPullSpell = null;
                }

                if (string.IsNullOrEmpty(_BestPullSpell))
                {
                    foreach (var spell in PullSpells.Where(ActionManager.HasSpell))
                    {
                        _BestPullSpell = spell;
                        break;
                    }

                }

                return _BestPullSpell;
            }
        }


        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
    new Decorator(ctx => ctx != null, new PrioritySelector(
                    EnsureTarget,
                    new Decorator(ctx => !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement), new PrioritySelector(
            CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
            CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                    )),
            Spell.PullCast(r => BestPullSpell)
    )));
        }


        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(
                //Dem deepz
                SummonChocobo(),
                Spell.Apply("Swiftcast", r=> !Core.Me.HasAura("Dualcast") && (ActionResourceManager.RedMage.BlackMana < 80 || ActionResourceManager.RedMage.WhiteMana < 80))
                );
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Cast("Erase", r => Core.Player.CurrentHealthPercent <= 40, r => Core.Player)
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

                        Spell.Cast("Corps-a-corps", r => Core.Target.Distance2D(Core.Me) > 15f && ActionResourceManager.RedMage.BlackMana > 80 && ActionResourceManager.RedMage.WhiteMana > 80),

                        Spell.PullCast(s => "Enchanted Redoublement", r=> ActionManager.LastSpell.Name == "Enchanted Zwerchhau"),
                        Spell.PullCast(s => "Enchanted Redoublement", r => ActionManager.LastSpell.Name == "Enchanted Riposte"),
                        Spell.PullCast(s => "Enchanted Riposte", r => ActionResourceManager.RedMage.BlackMana >= 80 && ActionResourceManager.RedMage.WhiteMana >= 80),

                        Spell.Cast("Displacement", r=> Core.Target.Distance2D(Core.Me) <= 15f && ActionResourceManager.RedMage.BlackMana < 80 || ActionResourceManager.RedMage.WhiteMana < 80),

                        Spell.Cast("Fleche"),

                        //black has priority because we get it at a lower level.
                        Spell.Cast("Veraero", r => ActionResourceManager.RedMage.WhiteMana < ActionResourceManager.RedMage.BlackMana && Core.Me.HasAura("Dualcast")),
                        Spell.Cast("Verthunder", r => ActionResourceManager.RedMage.BlackMana <= ActionResourceManager.RedMage.WhiteMana && Core.Me.HasAura("Dualcast")),

                        Spell.Cast("Verfire", r => Core.Me.HasAura("Verfire Ready")),
                        Spell.Cast("Verstone", r => Core.Me.HasAura("Verstone Ready")),
                        
                        Spell.Cast("Jolt"),
                        Spell.PullCast(s => "Riposte", r => Core.Me.ClassLevel == 1)
                        )));
        }
    }
}