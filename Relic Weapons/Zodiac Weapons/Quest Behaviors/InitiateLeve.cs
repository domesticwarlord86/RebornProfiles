using System.Windows.Media;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using Buddy.Coroutines;
using Clio.Utilities;
using Clio.XmlEngine;
using ff14bot.Behavior;
using ff14bot.Managers;
using ff14bot.RemoteWindows;
using ff14bot.Enums;
using ff14bot.Helpers;
using TreeSharp;

namespace ff14bot.NeoProfiles
{
	[XmlElement("InitiateLeve")]
	public class InitiateLeveTag : ProfileBehavior
	{
		private bool _done = false;

		[XmlAttribute("LeveId")]
		public int LeveId { get; set; }

		protected override void OnStart()
		{
			_done = false;
		}

        public override bool IsDone 
		{ 
			get 
			{ 
				return _done; 
			} 
		}

        protected override void OnDone()
        {
		}

		protected override Composite CreateBehavior() 
		{
			return
				new Decorator(
					ret => !_done,
					new ActionRunCoroutine(r => InitiateLeve()));
         }

        protected override void OnResetCachedDone()
		{
			_done = false;
		}

		protected async Task<bool> InitiateLeve()
		{
			// if (Core.Player.IsMounted)
			// {
				// ActionManager.Dismount();
				// await Coroutine.Wait(20000, () => !Core.Player.IsMounted);
				// await Coroutine.Sleep(500);
			// }
			var patternFinder = new GreyMagic.PatternFinder(Core.Memory);
			IntPtr SearchResult = patternFinder.Find("48 8D 05 ? ? ? ? 48 89 54 24 ? 48 89 03 Add 3 TraceRelative");
			int agent = AgentModule.FindAgentIdByVtable(SearchResult);
			AgentModule.ToggleAgentInterfaceById(agent); 
			await Coroutine.Sleep(500);
			AtkAddonControl windowByName = RaptureAtkUnitManager.GetWindowByName("JournalDetail");
			while (windowByName == null)
			{
				await Coroutine.Sleep(500);
				windowByName = RaptureAtkUnitManager.GetWindowByName("JournalDetail");
			}
			if (windowByName != null)
			{
				var leves = LeveManager.Leves;
				if (leves.Length > 0)
				{
					foreach(ff14bot.Managers.LeveWork leve in leves)	
					{
						if(leve.GlobalId == LeveId && leve.Step == 1)
						{
							ulong globalId = (ulong) leve.GlobalId;
							windowByName.SendAction(3,3,0xD,3,globalId,3,2); //Set Quest
							await Coroutine.Sleep(200);
							windowByName.SendAction(2,3,4,4,globalId); //Initiate
							if (await Coroutine.Wait(10000, () => SelectYesno.IsOpen))
							{
								SelectYesno.ClickYes();
							}
							await Coroutine.Sleep(2000);
							RaptureAtkUnitManager.GetWindowByName("GuildLeveDifficulty").SendAction(1, 3, 0);
							await Coroutine.Sleep(3000);
							break;
						}
					}
				}
				windowByName = RaptureAtkUnitManager.GetWindowByName("JournalDetail");
				if (windowByName != null)
				{
					AgentModule.ToggleAgentInterfaceById(agent); 
				}
			}

			return _done = true;
		}
	}
}
	