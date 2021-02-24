using System.Collections.Generic;
using System.Linq;
using System.Text;
using Clio.Utilities;
using ff14bot;
using ff14bot.Enums;
using ff14bot.Helpers;
using ff14bot.Managers;
using ff14bot.Navigation;
using ff14bot.Objects;
using TreeSharp;
using Action = TreeSharp.Action;

namespace Kupo.Helpers
{
    public static class Spell
    {

        #region delegates
        public delegate T Selection<out T>(object context);
        #endregion


        #region PullApply

        public static Composite PullApply(string name)
        {
            return PullApply(r => name);
        }


        /// <summary>
        /// Uses Spell.Apply to pull enemy target
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Composite PullApply(Selection<string> name)
        {
            return new PrioritySelector(ctx => ActionManager.InSpellInRangeLOS(name(ctx), Core.Target),
                   new Decorator(ctx => ctx != null && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement | CapabilityFlags.Facing),
                       new PrioritySelector(

                   new Decorator(r => ((SpellRangeCheck)r == SpellRangeCheck.ErrorNotInRange), new Action(r => Navigator.MoveTo(Core.Target.Location))),
                   new Decorator(r => ((SpellRangeCheck)r == SpellRangeCheck.ErrorNotInFront), new Action(r => Core.Target.Face())),
                   Spell.Apply(name)


                   )));
        }

        #endregion
        #region PullCast

        public static Composite PullCast(string name)
        {
            return PullCast(r => name);
        }


        /// <summary>
        /// Uses Spell.Apply to pull enemy target
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Composite PullCast(Selection<string> name)
        {
            return new PrioritySelector(ctx => ActionManager.InSpellInRangeLOS(name(ctx), Core.Target),
                   new Decorator(ctx => ctx != null && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement | CapabilityFlags.Facing),
                       new PrioritySelector(


                   new Decorator(r => ((SpellRangeCheck)r == SpellRangeCheck.ErrorNotInRange || (SpellRangeCheck)r == SpellRangeCheck.ErrorNotInLineOfSight), new Action(r => Navigator.MoveTo(Core.Target.Location))),
                   new Decorator(r => ((SpellRangeCheck)r == SpellRangeCheck.ErrorNotInFront), new Action(r => Core.Target.Face())),
                   Spell.Cast(name)


                   )));
        }

        public static Composite PullCast(Selection<string> name, Selection<bool> reqs)
        {
            return new Decorator(r=> reqs(r),new PrioritySelector(ctx => ActionManager.InSpellInRangeLOS(name(ctx), Core.Target),
                   new Decorator(ctx => ctx != null && !RoutineManager.IsAnyDisallowed(CapabilityFlags.Movement | CapabilityFlags.Facing),
                       new PrioritySelector(
                   new Decorator(r => ((SpellRangeCheck)r == SpellRangeCheck.ErrorNotInRange), new Action(r => Navigator.MoveTo(Core.Target.Location))),
                   new Decorator(r => ((SpellRangeCheck)r == SpellRangeCheck.ErrorNotInFront), new Action(r => Core.Target.Face())),
                   Spell.Cast(name)


                   ))));
        }

        

        #endregion

        #region Cast - by name

        /// <summary>
        ///   Creates a behavior to cast a spell by name. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <returns>.</returns>
        public static Composite Cast(string name)
        {
            return Cast(sp => name, onUnit => Core.Target);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name resolved during tree execution (rather than creation) on the current target.  
        ///   Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 11/25/2012.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <returns>.</returns>
        public static Composite Cast(Selection<string> name)
        {
            return Cast(name,onUnit => Core.Target);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name, on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <returns>.</returns>
        public static Composite Cast(string name, Selection<GameObject> onUnit)
        {
            return Cast(sp => name, onUnit);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name, with special requirements. Returns RunStatus.Success if successful,
        ///   RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Cast(string name, Selection<bool> requirements)
        {
            return Cast(sp => name, requirements);
        }



        /// <summary>
        ///   Creates a behavior to cast a spell by name, on a specific unit. Returns
        ///   RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 5/2/2011.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Cast(string name, Selection<bool> requirements, Selection<GameObject> onUnit)
        {
            return Cast(sp => name, requirements, onUnit);
        }



        /// <summary>
        ///   Creates a behavior to cast a spell by name resolved during tree execution (rather than creation) on a specific unit. 
        ///   Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 11/25/2012.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <returns>.</returns>
        public static Composite Cast(Selection<string> name, Selection<GameObject> onUnit)
        {
            return Cast(name, onUnit, req => true);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name resolved during tree execution (rather than creation), with special requirements, 
        ///   on the current target. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 11/25/2012.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Cast(Selection<string> name, Selection<bool> requirements)
        {
            return Cast(name, requirements, onUnit => Core.Target);
        }

        /// <summary>
        ///   Creates a behavior to cast a spell by name resolved during tree execution (rather than creation), with special requirements, 
        ///   on a specific unit. Returns RunStatus.Success if successful, RunStatus.Failure otherwise.
        /// </summary>
        /// <remarks>
        ///   Created 11/25/2012.
        /// </remarks>
        /// <param name = "name">The name.</param>
        /// <param name = "onUnit">The on unit.</param>
        /// <param name = "requirements">The requirements.</param>
        /// <returns>.</returns>
        public static Composite Cast(Selection<string> name, Selection<GameObject> onUnit, Selection<bool> requirements)
        {
            return Cast(name, requirements, onUnit);
        }


        public static Composite Cast(Selection<string> spell, Selection<bool> reqs, Selection<GameObject> onTarget, bool ignoreCanCast = false)
        {
            return
                new Decorator(
                    ret =>
                    {
                        SpellData data;
                        var spellName = spell(ret);
                        if (ActionManager.CurrentActions.TryGetValue(spellName, out data))
                        {
                            // Check reqs if its there.
                            if (reqs != null && !reqs(ret))
                                return false;


                            if (!ignoreCanCast && !ActionManager.CanCast(spellName, onTarget != null ? onTarget(ret) : Core.Player.CurrentTarget))
                                return false;

                            var castTime = data.AdjustedCastTime.TotalSeconds > 0;
                            //Spells with a cast time should not be cast when moving unless we are autonomus inwhich case we want to stop
                            if (castTime)
                            {
                                if (BotManager.Current.IsAutonomous)
                                {
                                    return true;
                                }
                                return !MovementManager.IsMoving;
                            }
                            return true;
                        }
                        return false;
                    },
                    new Action(ret =>
                    {
                        if (MovementManager.IsMoving)
                            Navigator.PlayerMover.MoveStop();
                        var spellName = spell(ret);

                        Logger.Write("Casting " + spellName);
                        ActionManager.DoAction(spellName, (onTarget != null ? onTarget(ret) : Core.Player.CurrentTarget));
                    }));
        }



        public static Composite CastLocation(Selection<string> spell, Selection<bool> reqs, Selection<Vector3> location, bool ignoreCanCast = false)
        {
            return
                new Decorator(
                    ret =>
                    {
                        SpellData data;
                        var spellName = spell(ret);
                        if (ActionManager.CurrentActions.TryGetValue(spellName, out data))
                        {
                            // Check reqs if its there.
                            if (reqs != null && !reqs(ret))
                                return false;

                            if (!ignoreCanCast && !ActionManager.CanCastLocation(spellName, location(ret)))
                                return false;

                            var castTime = data.AdjustedCastTime.TotalSeconds > 0;
                            //Spells with a cast time should not be cast when moving unless we are autonomus inwhich case we want to stop
                            if (castTime)
                            {
                                if (BotManager.Current.IsAutonomous)
                                {
                                    return true;
                                }
                                return !MovementManager.IsMoving;
                            }
                            return true;
                        }
                        return false;
                    },
                    new Action(ret =>
                    {
                        if (MovementManager.IsMoving)
                            Navigator.PlayerMover.MoveStop();
                        var spellName = spell(ret);
                        var loc = location(ret);
                        Logger.Write("Casting {0} at {1}", spellName, loc);
                        //ActionManager.DoAction(spell(ret), (onTarget != null ? onTarget(ret) : Core.Player.CurrentTarget));
                        ActionManager.DoActionLocation(spellName, loc);
                    }));
        }

        #endregion


        #region apply


        public static Composite Apply(string name)
        {
            return Apply(sp => name, null, null, 0, false);
        }
        public static Composite Apply(string name, Selection<bool> requirements)
        {
            return Apply(sp => name, requirements, null, 0, false);
        }
        public static Composite Apply(string name, Selection<bool> requirements, Selection<GameObject> onUnit)
        {
            return Apply(sp => name, requirements, onUnit, 0, false);
        }
        public static Composite Apply(string name, Selection<GameObject> onUnit)
        {
            return Apply(sp => name, null, onUnit, 0, false);
        }

        public static Composite Apply(Selection<string> spell, Selection<bool> reqs = null, Selection<GameObject> onTarget = null, int msLeft = 0, bool ignoreCanCast = false)
        {
            return new Decorator(ret =>
            {
                SpellData data;
                var spellName = spell(ret);
                if (ActionManager.CurrentActions.TryGetValue(spellName, out data))
                {
                    // Check reqs if its there.
                    if (reqs != null && !reqs(ret))
                        return false;

                    // Specific target, or just our general target.
                    GameObject target = onTarget != null ? onTarget(ret) : Core.Player.CurrentTarget;


                    if (target == null)
                        return false;


                    if (!ignoreCanCast && !ActionManager.CanCast(spellName, target))
                        return false;

                    if (KupoExtensions.DoubleCastPreventionDict.Contains(target, spellName))
                    {
                        return false;
                    }


                    if (!target.HasAura(spellName, true, msLeft))
                    {
                        var castTime = data.AdjustedCastTime.TotalSeconds > 0;
                        //Spells with a cast time should not be cast when moving unless we are autonomus inwhich case we want to stop
                        if (castTime)
                        {
                            if (BotManager.Current.IsAutonomous)
                            {
                                return true;
                            }
                            return !MovementManager.IsMoving;
                        }
                    }
                    else
                    {
                        return false;
                    }


                    return true;
                }


                return false;
            },
                new Action(ret =>
                {
                    //var castingSpell = Core.Player.SpellCastInfo;


                    if (MovementManager.IsMoving)
                        Navigator.PlayerMover.MoveStop();


                    var unit = (onTarget != null ? onTarget(ret) : Core.Player.CurrentTarget);
                    Logger.Write("Applying " + spell(ret));
                    if (ActionManager.DoAction(spell(ret), unit))
                    {
                        KupoExtensions.UpdateDoubleCastDict(spell(ret), unit);
                    }
                }));
        }
        #endregion

    }
}
