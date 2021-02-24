using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Buddy.Coroutines;
using Clio.Utilities;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Objects;
using Kupo.Helpers;
using Kupo.Settings;
using Newtonsoft.Json;
using TreeSharp;

namespace Kupo.Rotations
{
    

    internal class Ninja : KupoRoutine
    {
        //DEVELOPERS REPLACE GetType().Name WITH YOUR CR'S NAME.
        public override string Name
        {
            get { return "Kupo [" + GetType().Name + "]"; }
        }

        public override ClassJobType[] Class
        {
            get { return new[] { ClassJobType.Ninja }; }
        }

        public override void OnInitialize()
        {
            WindowSettings = new NinjaSettings();
            settings = WindowSettings as NinjaSettings;

        }


        private NinjaSettings settings;


        public override float PullRange
        {
            get { return 2.5f; }
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


        
        private readonly SpellData Ten = DataManager.GetSpellData(2259);
        private readonly SpellData Chi = DataManager.GetSpellData(2261);
        private readonly SpellData Jin = DataManager.GetSpellData(2263);

        private readonly SpellData Ninjutsu = DataManager.GetSpellData(2260);

        private readonly SpellData Kassatsu = DataManager.GetSpellData(2264);

        private readonly SpellData Trick_Attack = DataManager.GetSpellData(2258);
        private readonly SpellData TenChiJin = DataManager.GetSpellData(7403);

        public static HashSet<uint> OverrideBackstabIds = new HashSet<uint>()
        {
            3240//Cloud of darkness
        };


        private const int HutonRecast = 20000;
        private async Task<bool> DoNinjutsu()
        {
            
            var localPlayer = Core.Player;
            //Exit early if player was inputting something
            if (localPlayer.HasAura("Mudra"))
                return true;
            if (ActionManager.CanCastOrQueue(Jin, null))
            {

                //"Ten Chi Jin"
                //Aura doesn't appear right away, so check the cooldown on the TCJ since that seems to activate quickly
                if (localPlayer.HasAura(1186) || TenChiJin.Cooldown.TotalSeconds >= 95)
                {
                    Logger.Write("Inside TCJ - aura check");
                    await HandleTCJ();
                    return true;
                }


                if (ActionResourceManager.Ninja.HutonTimer.TotalMilliseconds == 0)
                {
                    await CastHuton();
                    return false;
                }

                var curTarget = Core.Target as BattleCharacter;
                if (curTarget == null)
                    return false;

                if (curTarget.TimeToDeath() <= 3)
                    return false;


                if (settings.UseAoe)
                {
                    if (EnemiesNearPlayer(5f, r => r.TimeToDeath() > 3) >= 2)
                    {
                        await CastDoton();
                        return false;
                    }
                }
 

                //Suiton
                var taCD = Trick_Attack.Cooldown;
                //We can start casting suiton before trick attack is ready cause its going to take some time
                if (taCD.TotalMilliseconds <= 1300)
                {
                    if (!await CastSuiton())
                        return false;

                    if (!await CastTrickAttack())
                        return false;



                    var charges = Kassatsu.Charges;
                    if ((charges < 1) || !Core.Player.HasTarget)
                        return false;



                    Again:
                    if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Kassatsu, null)))
                    {
                        if (Core.Player.ClassLevel >= 74)
                        {
                            await CastHyoshoRanyu();
                        }
                        else
                        {
                            await CastRaiton();
                        }

                        charges--;
                        if (charges >= 1)
                            goto Again;

                    }


                    return false;

                }

                if (taCD.TotalSeconds >= 20)
                {
                    await CastRaiton();
                }


                return false;
            }



            if (ActionManager.CanCastOrQueue(Chi, null))
            {
                await CastRaiton();
                return false;
            }

            if (ActionManager.CanCastOrQueue(Ten, null))
            {
                await Coroutine.Wait(5000, () => ActionManager.DoAction(Ten, null));
                await CastNinjutsu();
                return false;
            }




            return false;
        }

        private async Task HandleTCJ()
        {
            var huton = ActionResourceManager.Ninja.HutonTimer.TotalMilliseconds > 0;
            if (huton)
            {
                if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Ten, null)))
                {
                    await Coroutine.Wait(5000, () => Core.Player.HasAura("Mudra"));

                    if (await CastNinjutsuTCJ())
                    {
                        if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Chi, null)))
                        {

                            if (await awaitMudraChange() == false)
                                return;

                            if (await CastNinjutsuTCJ())
                            {
                                if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Jin, null)))
                                {
                                    if (await awaitMudraChange() == false)
                                        return;
                                    await CastNinjutsuTCJ();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Jin, null)))
                {
                    await Coroutine.Wait(5000, () => Core.Player.HasAura("Mudra"));

                    if (await CastNinjutsuTCJ())
                    {
                        if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Chi, null)))
                        {

                            if (await awaitMudraChange() == false)
                                return;

                            if (await CastNinjutsuTCJ())
                            {
                                if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Ten, null)))
                                {
                                    if (await awaitMudraChange() == false)
                                        return;
                                    await CastNinjutsuTCJ();
                                }
                            }
                        }
                    }
                }
            }

        }

        private async Task<bool> awaitMudraChange()
        {
            var mudraState = mudraValue;
            var localPlayer = Core.Player;


            while (localPlayer.HasAura("Mudra"))
            {
                await Coroutine.Yield();
                var newState = mudraValue;
                if (newState == -1)
                {
                    Logger.Write("mudrachange negative one");
                    return false;
                }
                    
                
                if (newState > 0 && newState != mudraState)
                {
                    //await Coroutine.Sleep(500);
                    Logger.Write("mudrachange true");
                    return true;
                }
            }
            Logger.Write("mudrachange false");
            return false;
        }

        private async Task<bool> CastNinjutsuTCJ()
        {
            var mudraState = mudraValue;
            var localPlayer = Core.Player;
            Logging.Write("Inside CastNinjutsuTCJ(), {0}", localPlayer.HasAura("Mudra"));
            while (localPlayer.HasAura("Mudra"))
            {
                Logging.Write("Inside CastNinjutsuTCJ loop");
                if (localPlayer.HasTarget)
                {
                    if (ActionManager.DoAction(Ninjutsu, Core.Target))
                    {
                        return true;
                        //await Coroutine.Yield();
                        //var newState = mudraValue;
                        //if (newState == -1)
                        //    return false;
                        //
                        //if (newState > 0 && newState != mudraState)
                        //{
                        //    await Coroutine.Sleep(500);
                        //    return true;
                        //}


                    }
                }
                await Coroutine.Yield();
            }

            return false;
            //if (await Coroutine.Wait(5000, () => Core.Player.HasAura("Mudra")))
            //{
            //    bool possibly = false;
            //
            //    var mudraState = mudraValue;
            //    while (Core.Player.HasAura("Mudra"))
            //    {
            //        if (Core.Player.HasTarget)
            //        {
            //            if (ActionManager.DoAction(Ninjutsu, Core.Target))
            //            {
            //                possibly = true;
            //            }
            //        }
            //        //This needs to be replaced with the changes made to combatassist, player could be not incombat now
            //        if (!Core.Player.InCombat)
            //            return false;
            //
            //        await Coroutine.Yield();
            //    }
            //    await Coroutine.Wait(5000, () => Ninjutsu.Cooldown.TotalSeconds > 10);
            //    return possibly;
            //}
            //return false;
        }

        private int mudraValue
        {
            get
            {
                var mudraBuff = Core.Player.GetAuraById(496);
                if (mudraBuff != null)
                {
                    return (int)mudraBuff.Value;
                }

                return -1;
            }
        }

        private async Task CastHuton()
        {
            if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Jin, null)))
            {
                if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Chi, null)))
                {
                    if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Ten, null)))
                    {
                        await CastNinjutsuSelf();
                    }
                }
            }
        }

        private async Task<bool> CastHyoshoRanyu()
        {
            if (!await Coroutine.Wait(5000, () => ActionManager.DoAction(Chi, null))) return false;
            if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Jin, null)))
            {
                return await CastNinjutsu();
            }
            return false;
        }


        private async Task<bool> CastTrickAttack()
        {

            while (Core.Player.HasAura("Suiton"))
            {
                if (Core.Player.HasTarget)
                {
                    //if (OverrideBackstabIds.Contains(Core.Target.NpcId) || Core.Target.IsBehind)
                    //{
                    //    ActionManager.DoAction(Trick_Attack, Core.Target);
                    //}
                    //else if (BotManager.Current.IsAutonomous || Core.Target.IsFacing(Core.Player))
                    //{
                    //    ActionManager.DoAction(Sneak_Attack, Core.Target);
                    //}

                    if (!Core.Player.HasAura("True North"))
                        ActionManager.DoAction("True North", Core.Player);
                    ActionManager.DoAction(Trick_Attack, Core.Target);


                }
                if (!Core.Player.InCombat)
                    return false;

                await Coroutine.Yield();
            }

            if (!BotManager.Current.IsAutonomous)
            {
                return await Coroutine.Wait(2000, () => Core.Target != null && Core.Target.IsValid && Core.Target.HasAura("Vulnerability Up"));
            }

            return false;
        }


        private async Task<bool> CastDoton()
        {
            if (!await Coroutine.Wait(5000, () => ActionManager.DoAction(Ten, null))) 
                return false;
            if (!await Coroutine.Wait(5000, () => ActionManager.DoAction(Jin, null)))
                return false;
            
            if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Chi, null)))
            {
                return await CastNinjutsu();
            }

            //if (!await Coroutine.Wait(5000, () => ActionManager.DoAction(Chi, null)))
            //    return false;
            //
            //if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Ten, null)))
            //{
            //    return await CastNinjutsu();
            //}


            return false;
        }

        private async Task<bool> CastRaiton()
        {
            if (!await Coroutine.Wait(5000, () => ActionManager.DoAction(Ten, null))) return false;
            if (await Coroutine.Wait(5000, () => ActionManager.DoAction(Chi, null)))
            {
                return await CastNinjutsu();
            }
            return false;
        }

        private async Task<bool> CastSuiton()
        {
            if (!await Coroutine.Wait(5000, () => ActionManager.DoAction(Ten, null))) return false;
            if (!await Coroutine.Wait(5000, () => ActionManager.DoAction(Chi, null))) return false;
            if (!await Coroutine.Wait(5000, () => ActionManager.DoAction(Jin, null))) return false;


            if (await CastNinjutsu())
            {
                return await Coroutine.Wait(5000, () => Core.Player.HasAura("Suiton"));
            }

            return false;
        }


        private async Task<bool> CastNinjutsuSelf()
        {
            var playerObject = Core.Player;
            if (await Coroutine.Wait(5000, () => playerObject.HasAura("Mudra")))
            {
                bool possibly = false;
                while (playerObject.HasAura("Mudra"))
                {

                    if (ActionManager.DoAction(Ninjutsu, playerObject))
                    {
                        possibly = true;
                    }

                    await Coroutine.Yield();
                }
                await Coroutine.Wait(5000, () => Ninjutsu.Cooldown.TotalSeconds > 10);
                return possibly;
            }
            return false;
        }


        private async Task<bool> CastNinjutsu()
        {
            if (await Coroutine.Wait(5000, () => Core.Player.HasAura("Mudra")))
            {
                bool possibly = false;
                while (Core.Player.HasAura("Mudra"))
                {
                    if (Core.Player.HasTarget)
                    {
                        if (ActionManager.DoAction(Ninjutsu, Core.Target))
                        {
                            possibly = true;
                        }
                    }
                    //This needs to be replaced with the changes made to combatassist, player could be not incombat now
                    if (!Core.Player.InCombat)
                        return false;

                    await Coroutine.Yield();
                }
                await Coroutine.Wait(5000, () => Ninjutsu.Cooldown.TotalSeconds > 10);
                return possibly;
            }
            return false;
        }


        private bool HasBleedingDebuff(BattleCharacter r)
        {
            return r.HasAura("Storm's Eye", false, 5000) || r.HasAura("Dancing Edge", false, 5000);
        }


        public bool ShouldArmorCrush
        {
            get
            {

                TimeSpan huton = ActionResourceManager.Ninja.HutonTimer;
                if (huton.TotalMilliseconds > 0)
                {
                    if (huton.TotalMilliseconds <= HutonRecast)
                    {
                        return true;
                    }
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
                        CommonBehaviors.MoveAndStop(ctx => (ctx as GameObject).Location, ctx => Core.Player.CombatReach + PullRange + (ctx as GameObject).CombatReach, true, "Moving to unit")
                        )),

                        //Spell.Apply("Kiss of the Wasp", r => (Core.Player.ClassLevel >= 36 || !ActionManager.HasSpell("Kiss of the Viper")), r => Core.Player),
                        //Spell.Apply("Kiss of the Viper", r => ActionManager.HasSpell("Kiss of the Viper") && Core.Player.ClassLevel < 36, r => Core.Player),
                        
                        new Decorator(r=> settings.UseAoe && EnemiesNearPlayer(8f) >= 3, new PrioritySelector(
                                                    Spell.Cast("Hellfrog Medium"),
                                                    Spell.Cast("Hakke Mujinsatsu", r => ActionManager.LastSpell.Name == "Death Blossom", r => Core.Player),
                                                    Spell.Cast("Death Blossom",r=>Core.Player),

                                                    new ActionAlwaysSucceed()
                            )),



                        new ActionRunCoroutine(r => DoNinjutsu()),
                        Spell.Cast(r => "Armor Crush", r => ShouldArmorCrush && ActionManager.LastSpell.Name == "Gust Slash"),
                        Spell.Cast("Bunshin", r=>Core.Player),
                        Spell.Cast("Bhavacakra"),
                        Spell.Cast("Trick Attack", r => Core.Player.HasAura("Suiton") && (r as BattleCharacter).IsBehind),
                        Spell.Cast("Assassinate"),


                        

                        //Spell.Apply(r => "Dancing Edge", r => !ShouldArmorCrush && !HasBleedingDebuff((r as BattleCharacter)) && HutonWrapper && ActionManager.LastSpell.Name == "Gust Slash", msLeft: 5000),
                        Spell.Apply(r => "Shadow Fang", r => !ShouldArmorCrush  && ActionManager.LastSpell.Name == "Spinning Edge", msLeft: 3000),


                        Spell.Cast("Aeolian Edge", r => ActionManager.LastSpell.Name == "Gust Slash"),
                        Spell.Cast("Gust Slash", r => ActionManager.LastSpell.Name == "Spinning Edge"),
                        
                        Spell.Cast("Mug"),
                        Spell.Cast("Dream Within a Dream",r => (r as BattleCharacter).TimeToDeath() > 3),
                        Spell.Cast("Spinning Edge")

                        )));
        }

    }

}
