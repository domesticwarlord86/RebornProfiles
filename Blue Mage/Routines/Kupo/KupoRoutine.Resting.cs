using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ff14bot;
using ff14bot.Behavior;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using Kupo.Helpers;
using TreeSharp;
using Action = System.Action;

namespace Kupo
{
    partial class KupoRoutine
    {
        public static bool Resting = false;

		private static readonly HashSet<ClassJobType> energyClassJobTypes = new HashSet<ClassJobType>()
		{
		    ClassJobType.Gladiator,
            ClassJobType.Pugilist,
            ClassJobType.Marauder,
            ClassJobType.Lancer,
            ClassJobType.Archer,
            ClassJobType.Paladin,
            ClassJobType.Monk,
            ClassJobType.Warrior,
            ClassJobType.Dragoon,
            ClassJobType.Bard,
            ClassJobType.Rogue,
            ClassJobType.Ninja,
            ClassJobType.DarkKnight
		};

        private static readonly HashSet<ClassJobType> mpClassJobTypes = new HashSet<ClassJobType>()
        {
            ClassJobType.Conjurer,
            ClassJobType.Thaumaturge,
            ClassJobType.WhiteMage,
            ClassJobType.BlackMage,
            ClassJobType.Arcanist,
            ClassJobType.Summoner,
            ClassJobType.Scholar,
            ClassJobType.Astrologian
        };

        private enum EnergyType
        {
            None,
			MP
        }

        //private Func<float> CurrentEnergy;
        private EnergyType CurrentEnergyType;
        private void SetupEnergyFunction()
        {
            if (mpClassJobTypes.Contains(Core.Player.CurrentJob))
            {
                CurrentEnergyType= EnergyType.MP;
            }
            else
            {
                CurrentEnergyType = EnergyType.None;
            }
        }
        
        private async Task<bool> Rest(Selection<float> CurrentEnergy)
        {
			Rest:
            if (Resting)
            {
                Logger.WriteNoSpam("Waiting to recover our health/mana back");
                if (MovementManager.IsMoving)
                {
                    Navigator.PlayerMover.MoveStop();
                }

                if (Core.Player.CurrentHealthPercent < WindowSettings.RestHealthDone || CurrentEnergy(null) < WindowSettings.RestEnergyDone)
                {
                    return true;
                }
                Resting = false;
                return false;
            }

            if (Core.Player.CurrentHealthPercent < WindowSettings.RestHealth)
            {
                Resting = true;
				goto Rest;
            }


            
            if (CurrentEnergyType != EnergyType.None && CurrentEnergy(null) < WindowSettings.RestEnergy)
            {
                Resting = true;
                goto Rest;
            }


            return false;
        }

        protected Composite DefaultRestBehavior(Selection<float> energy)
        {

            return new ActionRunCoroutine(r => Rest(energy));
            /*return new PrioritySelector(
                new Decorator(r => Resting, new PrioritySelector(
                    new FailLogger(r => ),
                    new Decorator(r => MovementManager.IsMoving, new Action(r => Navigator.PlayerMover.MoveStop())),
                    new Decorator(r => Core.Player.CurrentHealthPercent < WindowSettings.RestHealthDone, new ActionAlwaysSucceed()),
                    new Decorator(r => CurrentEnergy(r) < WindowSettings.RestEnergyDone, new ActionAlwaysSucceed()),
                    new Action(r => Resting = false)
                    )),
                new Decorator(
                    r =>
                        CurrentEnergy(r) < WindowSettings.RestEnergy || Core.Player.CurrentHealthPercent < WindowSettings.RestHealth, new Action(r => Resting = true))
                );*/
        }


    }
}
