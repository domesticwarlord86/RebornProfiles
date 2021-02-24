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
    internal class BlackMage : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] { ClassJobType.BlackMage }; }
        }

        public override float PullRange
        {
            get { return 20; }
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
            return 
                new PrioritySelector(
                    Spell.Cast("Transpose", r => !Core.Me.InCombat && ActionResourceManager.BlackMage.AstralStacks > 0),
                DefaultRestBehavior(r => Core.Player.CurrentManaPercent)
                );
        }


        private readonly string[] PullSpells = new[] { "Thunder II", "Thunder", "Fire", "Blizzard" };
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
    Spell.PullApply(r => BestPullSpell)
    )));
        }

        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Apply("Manaward", r => Core.Player.CurrentHealthPercent <= 80),
                Spell.Apply("Manawall", r => Core.Player.CurrentHealthPercent <= 80),
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


                        Spell.Apply("Swiftcast", r => (ActionResourceManager.BlackMage.Enochian && ActionManager.LastSpell.Name == "Fire IV") || (Core.Me.ClassLevel < 60 && Core.Me.CurrentManaPercent > 80), r => Core.Me),
                        Spell.Cast("Diversion", r => ActionManager.LastSpell.Name == "Swiftcast"),

                        Spell.Apply("Sharpcast", r => ActionManager.LastSpell.Name == "Fire IV" && ActionResourceManager.BlackMage.AstralStacks == 3, r => Core.Me),

                        Spell.Cast("Fire", r => ActionResourceManager.BlackMage.StackTimer < TimeSpan.FromMilliseconds(3000)),
                        Spell.Cast("Fire IV", r => ActionResourceManager.BlackMage.Enochian && Core.Me.CurrentMana > 80),
                        Spell.Cast("Blizzard IV", r => ActionResourceManager.BlackMage.Enochian && ActionManager.LastSpell.Name == "Enochian"),
                        Spell.Cast("Enochian", r => ActionResourceManager.BlackMage.UmbralStacks == 3 && Core.Player.CurrentManaPercent > 80),


                        
                        

                        //Need to check for insta procs first and foremost
                        Spell.Apply("Thunder III", r => Core.Player.HasAura("Thundercloud")),
                        Spell.Cast("Fire III", r => Core.Player.HasAura("Firestarter")),

                        //If we're low on mana we need to make sure we get it back
                        Spell.Cast("Blizzard III", r => Core.Player.CurrentMana < 638 && Core.Player.ClassLevel >= 38),
                        Spell.Cast("Convert", r => Core.Player.CurrentMana < 79 && Core.Player.ClassLevel >= 30),
                        Spell.Cast("Transpose", r => Core.Player.CurrentManaPercent <= 10 && Core.Player.ClassLevel < 38),
                //79 Mana is how much Blizzard III is with AstralFire ... don't want to be stuck with no mana

                        //Spell.Apply("Thunder II", r => !Core.Target.HasAura("Thunder")), //thunder 2 is now an AOE
                        Spell.Apply("Thunder"),
                        Spell.Cast("Fire III",
                            r =>
                                ActionResourceManager.BlackMage.AstralStacks == 0 &&
                                Core.Player.CurrentManaPercent > 85),
                
                        //cast blizzard 
                        Spell.Cast("Blizzard", r => ActionResourceManager.BlackMage.AstralStacks == 0 && Core.Player.CurrentManaPercent < 80),
                        //Bread and butter Fire spam
                        Spell.Cast("Fire"),
                        //for when we don't have fire
                        Spell.Cast("Blizzard")
                        )));
        }
    }
}