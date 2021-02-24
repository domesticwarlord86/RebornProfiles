using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Kupo.Helpers;
using TreeSharp;

namespace Kupo.Rotations
{

    internal class Rogue : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] {ClassJobType.Rogue}; }
        }

        public override float PullRange
        {
            get { return 2.5f; }
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
                SummonChocobo()
                );
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return DefaultRestBehavior(r => Core.Player.CurrentTPPercent);
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Cast("Bloodbath", r => Core.Player.CurrentHealthPercent < 60, r => Core.Player),
                Spell.Cast("Second Wind", r => Core.Player.CurrentHealthPercent < 40, r => Core.Player)
            );
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
                        Spell.PullCast("Spinning Edge")

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
                        Spell.Cast("Assassinate"),
                        Spell.Cast("Aeolian Edge", r => ActionManager.LastSpell.Name == "Gust Slash"),
                        Spell.Cast("Gust Slash", r => ActionManager.LastSpell.Name == "Spinning Edge"),
                        Spell.Apply("Mutilate"),
                        Spell.Cast("Spinning Edge", r => true)

                        )));
        }

    }

}
