using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using Kupo.Helpers;
using TreeSharp;
using Action = TreeSharp.Action;
using BardSong = ff14bot.Managers.ActionResourceManager.Bard.BardSong;

namespace Kupo.Rotations
{
    public class ArcherBard : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override float PullRange
        {
            get { return 20; }
        }

        public override ClassJobType[] Class
        {
            get { return new ClassJobType[] {ClassJobType.Archer, ClassJobType.Bard,}; }
        }

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return SummonChocobo();
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Cast("Second Wind", r => Core.Player.CurrentHealthPercent < 40, r => Core.Player)
            );
        }

        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(
                SummonChocobo(),
                Spell.Apply("Barrage", s=> Core.Me),
                Spell.Apply("Raging Strikes", s=>!Core.Me.HasAura("Barrage"), s=>Core.Me)
            );
        }

        [Behavior(BehaviorType.Rest)]
        public Composite CreateBasicRest()
        {
            return new PrioritySelector(
                    Spell.Cast("Foe Requiem", r => Core.Me.HasAura("Foe Requiem")),
                    DefaultRestBehavior(r => Core.Player.CurrentTPPercent)
                );
        }

        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
                        return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null  && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement), new PrioritySelector(
                        CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit"),
                Spell.PullCast("Heavy Shot")
                )));
        }

        public bool CanIronJaws
        {
            get
            {
                var target = Core.Target as BattleCharacter;
                if (target == null)
                    return false;

                Aura Windbite = target.GetAuraByName("Windbite");
                Aura VenomousBite = target.GetAuraByName("Venomous Bite");

                if (Windbite != null && VenomousBite != null)
                {
                    return Windbite.TimespanLeft < TimeSpan.FromMilliseconds(5000) || VenomousBite.TimespanLeft < TimeSpan.FromMilliseconds(5000);
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
                            CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, PullRange, true, "Moving to unit")
                        )),
                        Spell.Cast("Barrage", r=> Core.Me.HasAura("Straighter Shot")),
                        Spell.Cast("Empyreal Arrow", r=> Core.Player.HasAura("Barrage")),
                        
                        Spell.Cast("Straight Shot", r => !Core.Player.HasAura("Straight Shot", true, 3000)),
                        Spell.Cast("Misery's End"),
                        Spell.Cast("Bloodletter"),

                        Spell.Cast("Mage's Ballad", s => ActionResourceManager.Bard.ActiveSong == BardSong.None),
                        Spell.Cast("Army's Paeon", s => ActionResourceManager.Bard.ActiveSong == BardSong.None),
                        Spell.Cast("The Wanderer's Minuet", s => ActionResourceManager.Bard.ActiveSong == BardSong.None),
                        Spell.Cast("Battle Voice", s => ActionResourceManager.Bard.ActiveSong != BardSong.None, s => Core.Me),
                        Spell.Cast("Pitch Perfect", s => ActionResourceManager.Bard.ActiveSong == BardSong.WanderersMinuet, s => Core.Me),

                        Spell.Cast("Sidewinder", s => Core.Target.HasAura("Windbite") && Core.Target.HasAura("Venomous Bite")),

                        Spell.Cast("Iron Jaws", r => CanIronJaws),
                        Spell.Cast("Straight Shot", r => Core.Player.HasAura("Straighter Shot")),
                        Spell.Apply(r => "Windbite", msLeft: 3000),
                        Spell.Apply(r=>"Venomous Bite",msLeft:3000),
                        Spell.Cast("Heavy Shot", r => true)
                        )));
        }
    }
}