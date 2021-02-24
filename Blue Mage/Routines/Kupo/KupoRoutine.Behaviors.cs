using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ff14bot.Behavior;
using TreeSharp;

namespace Kupo
{
    public abstract partial class KupoRoutine
    {
        private Composite _combatBehavior;
        private Composite _combatBuffsBehavior;
        private Composite _healBehavior;
        private Composite _preCombatBuffsBehavior;
        private Composite _pullBehavior;
        private Composite _restBehavior;

        public override Composite CombatBehavior
        {
            get { return _combatBehavior; }
        }

        public override Composite CombatBuffBehavior
        {
            get { return _combatBuffsBehavior; }
        }

        public override Composite HealBehavior
        {
            get { return _healBehavior; }
        }

        public override Composite PreCombatBuffBehavior
        {
            get { return _preCombatBuffsBehavior; }
        }

        public override Composite PullBehavior
        {
            get { return _pullBehavior; }
        }

        public override Composite RestBehavior
        {
            get { return _restBehavior; }
        }


        public bool RebuildBehaviors(bool silent = false)
        {
            DetermineCurrentContext();
            InitBehaviors();

            if (!EnsureComposite(silent, true, CurrentContext, BehaviorType.Combat))
            {
                return false;
            }

            if (!EnsureComposite(silent, true, CurrentContext, BehaviorType.Pull))
            {
                return false;
            }


            // These are optional. If they're not implemented, we shouldn't stop because of it.
            EnsureComposite(silent, false, CurrentContext, BehaviorType.Rest);
            EnsureComposite(silent, false, CurrentContext, BehaviorType.CombatBuffs);
            EnsureComposite(silent, false, CurrentContext, BehaviorType.Heal);
            EnsureComposite(silent, false, CurrentContext, BehaviorType.PreCombatBuffs);


            return true;
        }

        /// <summary>
        /// initialize all base behaviors.  replaceable portion which will vary by context is represented by a single
        /// HookExecutor that gets assigned elsewhere (typically EnsureComposite())
        /// </summary>
        private void InitBehaviors()
        {
            // we only do this one time
            if (_restBehavior != null)
                return;

            // note regarding behavior intros....
            // WAIT: Rest and PreCombatBuffs should wait on gcd/cast in progress (return RunStatus.Success)
            // SKIP: PullBuffs, CombatBuffs, and Heal should fall through if gcd/cast in progress (wrap in decorator)
            // HANDLE: Pull and Combat should wait or skip as needed in class specific manner required

            _combatBehavior = new HookExecutor(HookName(BehaviorType.Combat));
            _restBehavior = new HookExecutor(HookName(BehaviorType.Rest));
            _healBehavior = new HookExecutor(HookName(BehaviorType.Heal));
            _preCombatBuffsBehavior = new HookExecutor(HookName(BehaviorType.PreCombatBuffs));
            _pullBehavior = new HookExecutor(HookName(BehaviorType.Pull));
            _combatBuffsBehavior = new HookExecutor(HookName(BehaviorType.CombatBuffs));
        }

        /// <summary>
        /// Ensures we have a composite for the given BehaviorType.  
        /// </summary>
        /// <param name="error">true: report error if composite not found, false: allow null composite</param>
        /// <param name="type">BehaviorType that should be loaded</param>
        /// <returns>true: composite loaded and saved to hook, false: failure</returns>
        private bool EnsureComposite(bool silent, bool error, GameContext context, BehaviorType type)
        {
            int count = 0;
            Composite composite;

            // Logger.WriteDebug("Creating " + type + " behavior.");

            composite = CompositeBuilder.GetComposite(this, type, context, out count);

            // handle those composites we need to default if not found
            //if (composite == null && type == BehaviorType.Rest)
            //    composite = Helpers.Rest.CreateDefaultRestBehaviour();

            if ((composite == null || count <= 0) && error)
            {
                //StopBot(string.Format("Singular does not support {0} for this {1} {2} in {3} context!", type, StyxWoW.Me.Class, TalentManager.CurrentSpec, context));
                return false;
            }

            // replace hook we created during initialization
            TreeHooks.Instance.ReplaceHook(HookName(type), composite ?? new ActionAlwaysFail());

            return composite != null;
        }


        internal static string HookName(string name)
        {
            return "Kupo." + name;
        }

        internal static string HookName(BehaviorType typ)
        {
            return "Kupo." + typ.ToString();
        }
    }
}