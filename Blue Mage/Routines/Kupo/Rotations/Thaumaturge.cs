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
    internal class Thaumaturge : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] {ClassJobType.Thaumaturge}; }
        }

        public override float PullRange
        {
            get { return 20; }
        }

        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(SummonChocobo(),
            Spell.Apply("Raging Strikes"),
            Spell.Apply("Swiftcast")

            );
        }

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return SummonChocobo();
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentManaPercent);
        }


        private string[] PullSpells = new[] {"Thunder II", "Thunder"};
        private string _BestPullSpell;

        private string BestPullSpell
        {
            get
            {
                if (string.IsNullOrEmpty(_BestPullSpell))
                {
                    foreach (var spell in PullSpells)
                    {
                        if (ActionManager.HasSpell(spell))
                        {
                            _BestPullSpell = spell;
                            return spell;
                        }
                    }
                    _BestPullSpell = "Blizzard";
                    return "Blizzard";
                }
                else
                {
                    return _BestPullSpell;
                }
            }
        }


        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,new PrioritySelector(
                EnsureTarget,
                new Decorator(ctx => !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement), new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                )),
                Spell.PullApply(BestPullSpell)
                )));
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Apply("Manaward", r => Core.Player.CurrentHealthPercent <= 80),
                Spell.Apply("Manawall", r => Core.Player.CurrentHealthPercent <= 80),
                Spell.Cast("Physick", r => Core.Player.CurrentHealthPercent <= 40, r => Core.Player)
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
                        EnsureTarget,
                        Spell.Apply("Thunder"),
                        Spell.Cast("Blizzard", r => Core.Player.CurrentManaPercent <= 60),
                        Spell.Cast("Fire"),
                        Spell.Cast("Blizzard", r => !ActionManager.HasSpell("Fire"))
                        )));
        }
    }
}