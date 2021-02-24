using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Helpers;
using ff14bot.Objects;
using Kupo.Settings;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Kupo.Helpers
{
    public static class Units
    {

        public static bool HasAura(this GameObject unit, string spellname, bool isMyAura = false, int msLeft = 0)
        {
            var unitasc = (unit as Character);
            if (unit == null || unitasc == null || !unitasc.IsValid)
            {
                return false;
            }

            IEnumerable<Aura> auras;
            if (isMyAura)
            {
                var objectId = Core.Player.ObjectId;
                auras = unitasc.CharacterAuras.Where(r => r.CasterId == objectId && r.Name == spellname);
            }
            else
            {
                auras = unitasc.CharacterAuras.Where(r => r.Name == spellname);
            }

            return auras.Any(aura => aura.TimespanLeft.TotalMilliseconds >= msLeft);
        }

        public static bool HasAura(this GameObject unit, string spellname, out Aura resultingAura, bool isMyAura = false, int msLeft = 0)
        {
            resultingAura = null;
            Character unitasc = (unit as Character);
            if (unit == null || unitasc == null || !unitasc.IsValid)
            {
                return false;
            }
			
			
            IEnumerable<Aura> auras;
            if (isMyAura)
            {
                var objectId = Core.Player.ObjectId;
                auras = unitasc.CharacterAuras.Where(r => r.CasterId == objectId && r.Name == spellname);
            }
            else
            {
                auras = unitasc.CharacterAuras.Where(r => r.Name == spellname);
                
            }


            resultingAura = auras.FirstOrDefault(aura => aura.TimespanLeft.TotalMilliseconds >= msLeft);
            return resultingAura != null;
        }


        public static bool ShowPlayerNames = false;

        public static string SafeName(this GameObject obj)
        {
            if (obj.IsMe)
            {
                return "Me";
            }

            string name;
            var character = obj as BattleCharacter;
            if (character != null)
            {
                name = character.CanAttack ? "Enemy." : "Ally.";
                name += ShowPlayerNames ? character.Name : character.CurrentJob.ToString();
            }
            else
            {
                name = obj.Name;
            }

            return name + "." + obj.ObjectId;
        }


    }

    // following class should probably be in Unit, but made a separate 
    // .. extension class to separate the private property names.
    // --
    // credit to Handnavi.  the following is a wrapping of his code
    public static class TimeToDeathExtension
    {

        public struct TimeToDeathStruct
        {
            public uint Guid { get; set; }  // guid of mob

            public uint FirstLife;         // life of mob when first seen
            public uint FirstLifeMax;      // max life of mob when first seen
            public int FirstTime;          // time mob was first seen
            public uint CurrentLife;       // life of mob now
            public int CurrentTime;        // time now
        }

        public static Dictionary<BattleCharacter, TimeToDeathStruct> TTDStorage = new Dictionary<BattleCharacter, TimeToDeathStruct>();


        /// <summary>
        /// seconds until the target dies.  first call initializes values. subsequent
        /// return estimate or indeterminateValue if death can't be calculated
        /// </summary>
        /// <param name="target">unit to monitor</param>
        /// <param name="indeterminateValue">return value if death cannot be calculated ( -1 or int.MaxValue are common)</param>
        /// <returns>number of seconds </returns>
        public static long TimeToDeath(this BattleCharacter target, long indeterminateValue = -1)
        {
            if (target == null || !target.IsValid || !target.IsAlive)
            {
                //Logging.Write("TimeToDeath: {0} (GUID: {1}, Entry: {2}) is dead!", target.SafeName(), target.Guid, target.Entry);
                return 0;
            }

            //Striking dummy's all have an id of 541
            if (target.NpcId == 541)
            {
                return 111;     // pick a magic number since training dummies dont die
            }


            TimeToDeathStruct ttds;
            if (!TTDStorage.TryGetValue(target, out ttds))
            {
                var tmpTime =  ConvDate2Timestam(DateTime.Now);
                ttds = new TimeToDeathStruct
                {
                    Guid = target.ObjectId,
                    FirstLife = target.CurrentHealth,
                    FirstLifeMax = target.MaxHealth,
                    FirstTime = tmpTime,
                    CurrentTime = tmpTime,
                    CurrentLife = target.CurrentHealth
                };
                TTDStorage[target] = ttds;
                //No point going anyfurther on the first trip
                return indeterminateValue;
            }

            ttds.CurrentLife = target.CurrentHealth;
            ttds.CurrentTime = ConvDate2Timestam(DateTime.Now);


            int timeDiff = ttds.CurrentTime - ttds.FirstTime;
            uint hpDiff = ttds.FirstLife - ttds.CurrentLife;
            if (hpDiff > 0)
            {
                /*
                * Rule of three (Dreisatz):
                * If in a given timespan a certain value of damage is done, what timespan is needed to do 100% damage?
                * The longer the timespan the more precise the prediction
                * time_diff/hp_diff = x/first_life_max
                * x = time_diff*first_life_max/hp_diff
                * 
                * For those that forgot, http://mathforum.org/library/drmath/view/60822.html
                */
                long fullTime = timeDiff * ttds.FirstLifeMax / hpDiff;
                long pastFirstTime = (ttds.FirstLifeMax - ttds.FirstLife) * timeDiff / hpDiff;
                long calcTime = ttds.FirstTime - pastFirstTime + fullTime - ttds.CurrentTime;
                if (calcTime < 1) calcTime = 1;
                //calc_time is a int value for time to die (seconds) so there's no need to do SecondsToTime(calc_time)
                long timeToDie = calcTime;
                //Logging.Write("TimeToDeath: {0} (GUID: {1}) dies in {2}", target, target.ObjectId, timeToDie);
                return timeToDie;
            }
            if (hpDiff <= 0)
            {
                //unit was healed,resetting the initial values
                ttds.Guid = target.ObjectId;
                ttds.FirstLife = target.CurrentHealth;
                ttds.FirstLifeMax = target.MaxHealth;
                ttds.FirstTime = ConvDate2Timestam(DateTime.Now);
                //Lets do a little trick and calculate with seconds / u know Timestamp from unix? we'll do so too
                //Logging.Write("TimeToDeath: {0} (GUID: {1}, Entry: {2}) was healed, resetting data.", target.SafeName(), target.Guid, target.Entry);
                return indeterminateValue;
            }
            if (ttds.CurrentLife == ttds.FirstLifeMax)
            {
                //Logging.Write("TimeToDeath: {0} (GUID: {1}, Entry: {2}) is at full health.", target.SafeName(), target.Guid, target.Entry);
                return indeterminateValue;
            }
            //Logging.Write("TimeToDeath: {0} (GUID: {1}, Entry: {2}) no damage done, nothing to calculate.", target.SafeName(), target.Guid, target.Entry);
            return indeterminateValue;
        }


        private static readonly DateTime timeOrigin = new DateTime(2012, 1, 1); // Refernzdatum (festgelegt)

        private static int ConvDate2Timestam(DateTime time)
        {
#if PREV
                DateTime baseLine = new DateTime(1970, 1, 1); // Refernzdatum (festgelegt)
                DateTime date2 = time; // jetztiges Datum / Uhrzeit
                var ts = new TimeSpan(date2.Ticks - baseLine.Ticks); // das Delta ermitteln
                // Das Delta als gesammtzahl der sekunden ist der Timestamp
                return (Convert.ToInt32(ts.TotalSeconds));
#else
            return (int)(time - timeOrigin).TotalSeconds;
#endif
        }

        /// <summary>
        /// creates behavior to write timetodeath values to debug log.  only
        /// evaluated if Singular Debug setting is enabled
        /// </summary>
        /// <returns></returns>
        public static Composite CreateWriteDebugTimeToDeath()
        {
            return new Action(
                ret =>
                {
                    if (KupoSettings.Instance.Debug && Core.Me.HasTarget && Core.Target is BattleCharacter)
                    {
                        long timeNow = ((BattleCharacter)Core.Target).TimeToDeath();
                        if (timeNow != lastReportedTime)
                        {
                            lastReportedTime = timeNow;
                            Logger.WriteFile("TimeToDeath: {0} (GUID: {1}, Entry: {2}) dies in {3}",
                                Core.Me.CurrentTarget.SafeName(),
                                Core.Me.CurrentTarget.ObjectId,
                                Core.Me.CurrentTarget.NpcId,
                                lastReportedTime);
                        }
                    }

                    return RunStatus.Failure;
                });

        }

        private static long lastReportedTime = -111;

    }

}
