using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Buddy.Coroutines;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using Kupo.Helpers;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Kupo.Rotations
{
    /// <summary>
    /// Big thanks to 'hkme' on the forums for the initial version of this
    /// </summary>
    public class Dancer : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name => "Kupo [" + GetType().Name + "]";

        public override float PullRange => 14.9f;

        public override ClassJobType[] Class => new[] { ClassJobType.Dancer };

        [Behavior(BehaviorType.PreCombatBuffs)]
        public Composite CreateBasicPreCombatBuffs()
        {
            return SummonChocobo();
        }

        [Behavior(BehaviorType.CombatBuffs)]
        public Composite CreateCombatBuff()
        {
            return new PrioritySelector(
                Spell.Cast("Curing Waltz", r => Core.Me.CurrentHealthPercent < 25, s => Core.Me),
                Spell.Cast("Second Wind", r => Core.Me.CurrentHealthPercent < 15, s => Core.Me),
                SummonChocobo()
            );
        }

        [Behavior(BehaviorType.Pull)]
        public Composite CreateBasicPull()
        {
            return DancerCombat();
        }

        private const uint Lv1Skill = 15989;
        private readonly SpellData lvl1skillSpellData = DataManager.GetSpellData(Lv1Skill);
        private bool OffGcdNonClipping()
        {
            return lvl1skillSpellData.Cooldown.TotalMilliseconds > 1000;
        }



        [Behavior(BehaviorType.Heal)]
        public Composite CreateBasicHeal()
        {
            return new PrioritySelector(
                Spell.Cast("Second Wind", r => Core.Player.CurrentHealthPercent < 40, r => Core.Player)
            );
        }


        private async Task<bool> PerformDance()
        {
            var currentStep = ActionResourceManager.Dancer.CurrentStep;
            var danceCd = DataManager.GetSpellData(Emboite);
            if (danceCd == null || danceCd.Cooldown.TotalMilliseconds > 100)
                return false;

            switch (currentStep)
            {
                case ActionResourceManager.Dancer.DanceStep.Finish when Core.Me.HasMyAura("Standard Step"):
                    return ActionManager.DoAction("Double Standard Finish", Core.Me);
                case ActionResourceManager.Dancer.DanceStep.Finish when Core.Me.HasMyAura("Technical Step"):
                    return ActionManager.DoAction("Quadruple Technical Finish", Core.Me);
                case ActionResourceManager.Dancer.DanceStep.Emboite:
                    return ActionManager.DoAction("Emboite", Core.Me);
                case ActionResourceManager.Dancer.DanceStep.Entrechat:
                    return ActionManager.DoAction("Entrechat", Core.Me);
                case ActionResourceManager.Dancer.DanceStep.Jete:
                    return ActionManager.DoAction("Jete", Core.Me);
                case ActionResourceManager.Dancer.DanceStep.Pirouette:
                    return ActionManager.DoAction("Pirouette", Core.Me);
                default:
                    return false;
            }
        }

        private bool HaveFlourishExpiring(float seconds)
        {
            foreach (var aura in Core.Me.Auras)
            {
                if (aura.Caster != null && aura.Caster.IsMe && aura.Name.Contains("Flourishing") && aura.TimeLeft < seconds)
                    return true;
            }
            return false;
        }

        private bool AoeCondition()
        {
            var enemyCount = GameObjectManager.GetObjectsOfType<BattleCharacter>().Count(r => r.IsVisible && r.CanAttack && r.Distance() - r.CombatReach < 5 && r.CurrentHealth > 0);
            return enemyCount > 1;
        }


        [Behavior(BehaviorType.Combat)]
        public Composite CreateBasicCombat()
        {
            return DancerCombat();
        }

        private Composite DancerCombat()
        {
            return new PrioritySelector(ctx => Core.Player.CurrentTarget as BattleCharacter,
                new Decorator(ctx => ctx != null,
                    new PrioritySelector(
                        new Decorator(ctx => !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement) && BotManager.Current.IsAutonomous, new PrioritySelector(
                            CommonBehaviors.MoveToLos(ctx => ctx as GameObject),
                            CommonBehaviors.MoveAndStop(ctx => ((GameObject)ctx).Location, r => PullRange + Core.Target.CombatReach, true, "Moving to unit by CR")
                        )),

                        // interrupt

                        // buff before dance
                        Spell.Cast("Closed Position", r => !Core.Me.HasMyAura("Closed Position") && !IsDancing() && DancePartner != null, r => DancePartner),
                        Spell.Cast("Devilment", r => !IsDancing(), r => Core.Me),

                        // dance
                        new Decorator(r => IsDancing(), new ActionRunCoroutine(r => PerformDance())),

                        new Decorator(r => !IsDancing(), new PrioritySelector(
                            // Gcd
                            Spell.Cast("Saber Dance", r => ActionResourceManager.Dancer.Esprit >= 90), // 600 target AOE
                            Spell.Cast("Technical Step", r => !HaveFlourishExpiring(7), r => Core.Me),
                            Spell.Cast("Standard Step", r => !HaveFlourishExpiring(5), r => Core.Me),
                            Spell.Cast("Bloodshower", r => Core.Me.HasAura("Flourishing Shower") && AoeCondition(), r => Core.Me), // 300 AOE
                            Spell.Cast("Rising Windmill", r => Core.Me.HasAura("Flourishing Windmill") && AoeCondition(), r => Core.Me), // 250 AOE
                            Spell.Cast("Bladeshower", r => ActionManager.LastSpell.Name == "Windmill" && AoeCondition(), r => Core.Me), // 200 AOE
                            Spell.Cast("Fountainfall", r => Core.Me.HasAura("Flourishing Fountain")), // 350
                            Spell.Cast("Bloodshower", r => Core.Me.HasAura("Flourishing Shower") && TargetDistance < 5f, r => Core.Me), // 300 Single
                            Spell.Cast("Reverse Cascade", r => Core.Me.HasAura("Flourishing Cascade")), // 300
                            Spell.Cast("Windmill", r => AoeCondition(), r => Core.Me), // 150 AOE
                            Spell.Cast("Rising Windmill", r => Core.Me.HasAura("Flourishing Windmill") && TargetDistance < 5f, r => Core.Me), // 250 Single
                            Spell.Cast("Fountain", r => ActionManager.LastSpell.Name == "Cascade"), // 250
                            Spell.Cast("Cascade", r => true), // 200

                            // oGcd
                            Spell.Cast("Fan Dance III", r => Core.Me.HasAura("Flourishing Fan Dance") && OffGcdNonClipping()), // 200 target AOE
                            Spell.Cast("Fan Dance II", r => ActionResourceManager.Dancer.FourFoldFeathers > 3 && AoeCondition() && OffGcdNonClipping(), r => Core.Me), // 100 AOE, need Fourfold Feathers
                            Spell.Cast("Fan Dance", r => ActionResourceManager.Dancer.FourFoldFeathers > 3 && OffGcdNonClipping()), // 150, need Fourfold Feathers
                            Spell.Cast("Flourish", r => !HaveFlourishExpiring(16) && OffGcdNonClipping(), r => Core.Me)
                        )))));
        }

        private static float TargetDistance
        {
            get
            {
                var target = Core.Target;
                if (target == null || !target.IsValid) return float.MaxValue;
                return target.Distance() - target.CombatReach;
            }
        }

        /// <summary>
        /// Prefer jobs that are dps roles first
        /// </summary>
        private static HashSet<ClassJobType> invalidJobs = new HashSet<ClassJobType>() {ClassJobType.Gunbreaker,ClassJobType.DarkKnight,ClassJobType.Astrologian,ClassJobType.WhiteMage,ClassJobType.Conjurer,ClassJobType.Scholar,ClassJobType.Warrior,ClassJobType.Paladin,ClassJobType.Gladiator,};

        private static GameObject DancePartner
        {
            get
            {
                GameObject danceTarget = null;
                if (PartyManager.IsInParty)
                {
                    var targets = PartyManager.VisibleMembers.Where(r => r.BattleCharacter.Distance() < 30).Select(r=>r.BattleCharacter);
                    var battleCharacters = targets as BattleCharacter[] ?? targets.ToArray();
                    danceTarget = battleCharacters.FirstOrDefault(r => !invalidJobs.Contains(r.CurrentJob));
                    if (danceTarget == null)
                        danceTarget = battleCharacters.FirstOrDefault();

                }
                else if (ChocoboManager.Summoned)
                {
                    danceTarget = ChocoboManager.Object;
                }
                   

                return danceTarget;
            }
        }

        private static bool IsDancing()
        {
            return Core.Me.HasAura("Standard Step") || Core.Me.HasAura("Technical Step");
        }

        private const uint Emboite = 15999;
    }
}