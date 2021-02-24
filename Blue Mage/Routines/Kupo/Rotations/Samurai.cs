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
using SamResource = ff14bot.Managers.ActionResourceManager.Samurai;
using Iaijutsu = ff14bot.Managers.ActionResourceManager.Samurai.Iaijutsu;

namespace Kupo.Rotations
{
    internal class Samurai : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] { ClassJobType.Samurai }; }
        }

        public override float PullRange
        {
            get { return 2.5f; }
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
                DefaultRestBehavior(r => Core.Player.CurrentTPPercent);
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
            Spell.PullCast(r => "Hakaze")
    )));
        }


        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateBasicCombatBuffs()
        {
            return new PrioritySelector(SummonChocobo());
        }



        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Cast("Bloodbath", r => Core.Player.CurrentHealthPercent < 60, r => Core.Player),
                Spell.Cast("Second Wind", r => Core.Player.CurrentHealthPercent < 40, r => Core.Player)
            );
        }

        private static SpellData Guren = DataManager.GetSpellData("Hissatsu: Guren");

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


                        ///spenders
                        Spell.Cast("Hissatsu: Guren", r => SamResource.Kenki >= 50),
                        Spell.Cast("Hissatsu: Shinten", r => (SamResource.Kenki >= 40 || (ActionManager.LastSpell.Name == "Hissatsu: Shinten" && SamResource.Kenki >= 25)) && Guren.Cooldown > TimeSpan.Zero),
                        Spell.Cast("Hissatsu: Kaiten", r => SamResource.Kenki>= 10 && ((SamResource.Sen.HasFlag(Iaijutsu.Getsu) && !Core.Target.HasAura("Higanbana")) || SamResource.Sen == (Iaijutsu.Getsu | Iaijutsu.Setsu | Iaijutsu.Ka)) && Core.Target.HasAura("Higanbana") ),
                        Spell.Cast("Higanbana", r=> !Core.Target.HasAura("Higanbana") && SamResource.Sen.HasFlag(Iaijutsu.Getsu)),
                        
                        Spell.Cast("Midare Setsugekka", r=> (SamResource.Sen == (Iaijutsu.Getsu | Iaijutsu.Setsu | Iaijutsu.Ka)) && Core.Target.HasAura("Higanbana")),
                        Spell.Cast("Hagakure", r=> SamResource.Kenki <= 10  && SamResource.Sen == (Iaijutsu.Getsu | Iaijutsu.Setsu | Iaijutsu.Ka)),

                        //generators
                        // generate all 3
                        Spell.Cast("Meikyo Shisui"),
                        Spell.Cast("Yukikaze", r => Core.Me.HasAura("Meikyo Shisui") && !SamResource.Sen.HasFlag(Iaijutsu.Setsu)),
                        Spell.Cast("Gekko", r => Core.Me.HasAura("Meikyo Shisui") && !SamResource.Sen.HasFlag(Iaijutsu.Getsu)),
                        Spell.Cast("Kasha", r => Core.Me.HasAura("Meikyo Shisui") && !SamResource.Sen.HasFlag(Iaijutsu.Ka)),


                        //getsu
                        Spell.Cast("Jinpu", r=> ActionManager.LastSpell.Name == "Hakaze" && !SamResource.Sen.HasFlag(Iaijutsu.Getsu)),
                        Spell.Cast("Gekko", r => ActionManager.LastSpell.Name == "Jinpu"),

                        //setsu
                        Spell.Cast("Yukikaze", r => ActionManager.LastSpell.Name == "Hakaze" && !SamResource.Sen.HasFlag(Iaijutsu.Setsu)),

                        //Ka
                        Spell.Cast("Shifu", r => ActionManager.LastSpell.Name == "Hakaze" && !SamResource.Sen.HasFlag(Iaijutsu.Ka)),
                        Spell.Cast("Kasha", r => ActionManager.LastSpell.Name == "Shifu"),


                        Spell.PullCast("Hakaze")
                    )));
        }
    }
}