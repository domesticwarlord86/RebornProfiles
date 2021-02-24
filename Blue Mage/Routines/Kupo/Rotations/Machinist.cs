using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Managers;
using ff14bot.Objects;
using Kupo.Helpers;
using Newtonsoft.Json;
using TreeSharp;

namespace Kupo.Rotations
{

    internal class Machinist : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] { ClassJobType.Machinist }; }
        }

        public override float PullRange
        {
            get { return 20f; }
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

                        Spell.Cast("Slug Shot", r => Core.Player.HasAura("Enhanced Slug Shot")),
                        Spell.Cast("Split Shot")
                        )));
        }

        private bool HotShot
        {
            get
            {
                Aura hotshot;
                if (!Core.Player.HasAura("Hot Shot", out hotshot,msLeft:6000))
                    return true;

                return false;
            }
        }

        //private int AmmunitionCount
        //{
        //    get
        //    {
        //        return ActionResourceManager.Machinist.Ammo;
        //    }
        //}


        private readonly SpellData Reload = DataManager.GetSpellData(2867);
        private bool QuickReload
        {
            get
            {
                var cd = Reload.Cooldown.TotalSeconds;
                if (cd <55 && cd > 5)
                    return true;

                return false;
            }
        }

        private Random _random = new Random();
        private Vector3 CalculatePoint()
        {
            int attempts = 0;
            var loc = Core.Player.Location;
            Start:

            //Generate a random point around the player in a circle
            var angle = _random.NextDouble() * Math.PI * 2;
            var radius = Math.Sqrt(_random.NextDouble()) * 3;
            var x = loc.X + radius * Math.Cos(angle);
            var z = loc.Z + radius * Math.Sin(angle);
            
            //See if the location isnt too far heightwise from the player

            var top = new Vector3((float)x, loc.Y +3, (float)z);
            var bottom = new Vector3((float)x, -100, (float)z);
            Vector3 hit;
            if (WorldManager.Raycast(top, bottom, out hit))
            {
                if (Math.Abs(hit.Y - loc.Y) < 3)
                {
                    return hit;
                }
            }

            attempts++;
            if (attempts > 3)
            {
                return loc;
            }
            goto Start;
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

                        Spell.CastLocation(r => "Rook Autoturret", r => GameObjectManager.CurrentPet == null || GameObjectManager.CurrentPet.Location.Distance2DSqr(Core.Player.Location) > 30 * 30, r => CalculatePoint()),

                        //Spell.Cast("Gauss Barrel", r => !ActionResourceManager.Machinist.GaussBarrel),
                        Spell.Apply("Wildfire"),

                        
                        //Spell.Cast("Reload", r => AmmunitionCount <= 1, r => Core.Player),
                        //Spell.Cast("Quick Reload", r => QuickReload && AmmunitionCount < 3, r => Core.Player),

                        Spell.Cast("Gauss Round"),
                        Spell.Cast("Barrel Stabilizer", r => ActionResourceManager.Machinist.Heat < 25),
                        Spell.Cast("Cooldown", r => ActionResourceManager.Machinist.Heat > 50),

                        Spell.Cast("Rapid Fire"),

                        Spell.Cast("Reassemble", r=> Core.Player.HasAura("Cleaner Shot") || Core.Player.HasAura("Enhanced Slug Shot"), r => Core.Player),

                        Spell.Cast("Clean Shot", r => Core.Player.HasAura("Cleaner Shot")),
                        Spell.Cast("Slug Shot", r => Core.Player.HasAura("Enhanced Slug Shot")),

                        Spell.Cast("Heartbreak"),

                        Spell.Cast("Invigorate", r => Core.Player.CurrentTP < 500),

                        Spell.Apply(r => "Lead Shot",msLeft:3000),
                        Spell.Cast("Hot Shot", r => HotShot),

                        Spell.Cast("Heated Split Shot"),
                        Spell.Cast("Split Shot")

                        )));
        }

    }

}
