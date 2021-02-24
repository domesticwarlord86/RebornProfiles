using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using ff14bot.Enums;
using ff14bot.Helpers;
using TreeSharp;
using System.Windows.Media;

namespace Kupo
{
    public static class CompositeBuilder
    {
        /// <summary>
        /// allows generic behaviors to query current type of behavior
        /// during behavior construction
        /// </summary>
        public static BehaviorType CurrentBehaviorType { get; set; }

        public static bool SilentBehaviorCreation { get; set; }


        internal static List<MethodInfo> _methods = new List<MethodInfo>();

        public static Composite GetComposite(KupoRoutine whom, BehaviorType behavior, GameContext context, out int behaviourCount, bool silent = false)
        {
            if (context == GameContext.None)
            {
                // None is an invalid context, but rather than stopping bot wait it out with donothing logic
                //Logging.Write(Colors.White, "No Active Context -{0}{1} for{2} set to DoNothingBehavior temporarily", wowClass.ToString().CamelToSpaced(), behavior.ToString().CamelToSpaced(), spec.ToString().CamelToSpaced());
                behaviourCount = 1;
                return null; //NoContextAvailable.CreateDoNothingBehavior();
            }

            SilentBehaviorCreation = silent;
            behaviourCount = 0;
            _methods.Clear();
            if (_methods.Count <= 0)
            {
                _methods.AddRange(whom.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance).Where(
                    mi => !mi.IsGenericMethod && mi.GetParameters().Length == 0).Where(
                        mi => mi.ReturnType.IsAssignableFrom(typeof (Composite))));

                //Logging.Write("Kupo Behaviors: Added " + _methods.Count + " behaviors");
            }

            var matchedMethods = new Dictionary<BehaviorAttribute, Composite>();

            foreach (MethodInfo mi in _methods)
            {
                // If the behavior is set as ignore. Don't use it? Duh?
                if (mi.GetCustomAttributes(typeof (IgnoreBehaviorCountAttribute), false).Any())
                    continue;

                // If there's no behavior attrib, then move along.
                foreach (var a in mi.GetCustomAttributes(typeof (BehaviorAttribute), false))
                {
                    var attribute = a as BehaviorAttribute;
                    if (attribute == null)
                        continue;

                    // Check if our behavior matches with what we want. If not, don't add it!
                    if (IsMatchingMethod(attribute, behavior, context))
                    {
                        //if (!silent)
                        //    Logging.Write(LogLevel.Diagnostic,"{0} {1} {2}", attribute.PriorityLevel.ToString(), behavior.ToString(), mi.Name);

                        CurrentBehaviorType = behavior;

                        // if it blows up here, you defined a method with the exact same attribute and priority as one already found

                        // wrap in trace class
                        var vv = mi.Invoke(whom, null);
                        var comp = vv as Composite;

                        matchedMethods.Add(attribute, comp);

                        CurrentBehaviorType = 0;
                    }
                }
            }
            // If we found no methods, rofls!
            if (matchedMethods.Count <= 0)
            {
                return null;
            }

            var result = new PrioritySelector();
            foreach (var kvp in matchedMethods.OrderByDescending(mm => mm.Key.PriorityLevel))
            {
                result.AddChild(kvp.Value);
                behaviourCount++;
            }

            return result;
        }

        private static bool IsMatchingMethod(BehaviorAttribute attribute, BehaviorType behavior, GameContext context)
        {
            if ((attribute.Type & behavior) == 0)
                return false;
            if ((attribute.SpecificContext & context) == 0)
                return false;

            return true;
        }
    }
}