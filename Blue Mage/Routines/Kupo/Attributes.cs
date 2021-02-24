using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Kupo
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    internal sealed class IgnoreBehaviorCountAttribute : Attribute
    {
        public IgnoreBehaviorCountAttribute(BehaviorType type)
        {
            Type = type;
        }

        public BehaviorType Type { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    internal sealed class BehaviorAttribute : Attribute
    {
        public BehaviorAttribute(BehaviorType type, GameContext context = GameContext.All, int priority = 0)
        {
            Type = type;
            SpecificContext = context;
            PriorityLevel = priority;
        }

        public BehaviorType Type { get; private set; }
        public GameContext SpecificContext { get; private set; }
        public int PriorityLevel { get; private set; }
    }
}