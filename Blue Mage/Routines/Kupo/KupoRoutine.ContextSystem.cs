using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using Kupo.Helpers;

namespace Kupo
{

    #region Nested type: GameContextEventArg

    public class GameContextEventArg : EventArgs
    {
        public readonly GameContext CurrentContext;
        public readonly GameContext PreviousContext;

        public GameContextEventArg(GameContext currentContext, GameContext prevContext)
        {
            CurrentContext = currentContext;
            PreviousContext = prevContext;
        }
    }

    #endregion

    public partial class KupoRoutine
    {
        public event EventHandler<GameContextEventArg> OnGameContextChanged;
        private GameContext _lastContext = GameContext.None;
        private uint _lastMapId = 0;

        internal GameContext ForcedContext { get; set; }
        public GameContext CurrentContext { get; set; }


        internal readonly List<ClassJobType> Healers = new List<ClassJobType>() {ClassJobType.WhiteMage, ClassJobType.Conjurer, ClassJobType.Scholar, ClassJobType.Astrologian};

        internal void UpdateContextStateValues()
        {
            if (CurrentContext == GameContext.Instances || CurrentContext == GameContext.PvP)
            {
                if (Healers.Contains(Core.Player.CurrentJob))
                {
                    WantHealing = true;
                    return;
                }
            }
            WantHealing = false;
        }

        internal void UpdateContext()
        {
            DetermineCurrentContext();
            //Logging.Write("Old context:{0} CurrentContext:{1}", _lastContext, CurrentContext);
            // Can't update the context when it doesn't exist.
            if (CurrentContext == GameContext.None)
                return;


            if (CurrentContext != _lastContext)
            {
                Logger.Write("Old context:{0} CurrentContext:{1}", _lastContext, CurrentContext);
                // store values that require scanning lists
                UpdateContextStateValues();
                //DescribeContext();
                if (OnGameContextChanged != null)
                {
                    try
                    {
                        OnGameContextChanged(this, new GameContextEventArg(CurrentContext, _lastContext));
                    }
                    catch
                    {
                        // Eat any exceptions thrown.
                    }
                }


                _lastContext = CurrentContext;
                _lastMapId = WorldManager.ZoneId;
            }
            else if (_lastMapId != WorldManager.ZoneId)
            {
                //DescribeContext();
                _lastMapId = WorldManager.ZoneId;
            }
        }

        private void DetermineCurrentContext()
        {
            CurrentContext = _DetermineCurrentContext();
        }

        private GameContext _DetermineCurrentContext()
        {
            if (!Core.IsInGame)
                return GameContext.None;

            if (ForcedContext != GameContext.None)
                return ForcedContext;

            if (WorldManager.InPvP)
            {
                return GameContext.PvP;
            }

            if (PartyManager.IsInParty && DutyManager.InInstance)
            {
                return GameContext.Instances;
            }

            return GameContext.Normal;
        }
    }
}