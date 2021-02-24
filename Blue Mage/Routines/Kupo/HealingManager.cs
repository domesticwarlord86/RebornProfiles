using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ff14bot;
using ff14bot.Managers;
using ff14bot.NeoProfiles;
using ff14bot.Objects;

namespace Kupo
{
    public class HealTargeting : Targeting
    {
        private static HealTargeting _instance;

        /// <summary> Default constructor. </summary>
        public HealTargeting()
        {
            Provider = new HealTargetingProvider();
        }

        /// <summary> Gets the instance. </summary>
        /// <value> The instance. </value>
        public static HealTargeting Instance
        {
            get { return _instance ?? (_instance = new HealTargeting()); }
        }
    }


    public class HealTargetingProvider : ITargetingProvider
    {
        public sealed class TargetPriority
        {
            public BattleCharacter Object;
            public double Score;
        }

        protected List<BattleCharacter> GetInitialObjectList()
        {
            List<BattleCharacter> heallist = new List<BattleCharacter>();


            if (PartyManager.IsInParty)
            {
                heallist.AddRange(PartyManager.VisibleMembers.Select(r => r.GameObject as BattleCharacter));
            }
            else
            {
                heallist.Add(Core.Player);
            }
            if (GameObjectManager.CurrentPet != null)
            {
                heallist.Add(GameObjectManager.CurrentPet);
            }


            return heallist;
        }

        public List<BattleCharacter> GetObjectsByWeight()
        {
            var wecareabout = GetInitialObjectList();

            List<TargetPriority> wrapped = wecareabout.Select(n => new TargetPriority {Object = n,}).ToList();
            DefaultTargetWeight(wrapped);

            return new List<BattleCharacter>(wrapped.OrderByDescending(s => s.Score).Select(s => s.Object).ToList());
        }

        private void DefaultTargetWeight(List<TargetPriority> units)
        {
            var myLoc = Core.Player.Location;

            foreach (TargetPriority prio in units)
            {
                var u = prio.Object;
                if (u == null || !u.IsValid)
                {
                    prio.Score = -9999f;
                    continue;
                }

                // The more health they have, the lower the score.
                // This should give -500 for units at 100%
                // And -50 for units at 10%
                try
                {
                    prio.Score = u.IsAlive ? 500f : -500f;
                    prio.Score -= u.CurrentHealthPercent*5;

                    // If they're out of range, give them a bit lower score.
                    if (u.Location.DistanceSqr(myLoc) > 40*40)
                    {
                        prio.Score -= 50f;
                    }

                    // If they're out of LOS, again, lower score!
                    if (!u.InLineOfSight())
                    {
                        prio.Score -= 100f;
                    }

                    /* Give tanks more weight. If the tank dies, we all die. KEEP HIM UP.
                    if (tanks.Contains(u.Guid) && u.HealthPercent != 100)
                    {
                        prio.Score += 100f;
                    }*/

                    /* Give flag carriers more weight in battlegrounds. We need to keep them alive!
                    if (inBg && u.IsPlayer && u.Auras.Keys.Any(a => a.ToLowerInvariant().Contains("flag")))
                    {
                        prio.Score += 100f;
                    }*/
                }
                catch (System.AccessViolationException)
                {
                    prio.Score = -9999f;
                    continue;
                }
            }
        }
    }
}