namespace ExBuddy.OrderBotTags.Behaviors
{
    using Buddy.Coroutines;
    using Clio.Utilities;
    using Clio.XmlEngine;
    using ExBuddy.Attributes;
    using ExBuddy.Helpers;
    using ExBuddy.Interfaces;
    using ExBuddy.Windows;
    using ff14bot;
    using ff14bot.Behavior;
    using ff14bot.Enums;
    using ff14bot.Helpers;
    using ff14bot.Managers;
    using ff14bot.RemoteWindows;
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Windows.Media;

    [LoggerName("ExGuildLeve")]
    [XmlElement("ExTurnInGuildLeve")]
    public class ExTurnInGuildLeveTag : ExProfileBehavior, IInteractWithNpc
    {
        private readonly Stopwatch interactTimeout = new Stopwatch();

        private bool checkedTransport;

        private uint iconStringIndex = 9001;

        private bool turnedItemsIn;

        [DefaultValue(true)]
        [XmlAttribute("AcceptTransport")]
        public bool AcceptTransport { get; set; }

        [DefaultValue("Collect Reward.")]
        [XmlAttribute("CollectRewardText")]
        public string CollectRewardText { get; set; }

        [XmlAttribute("HqOnly")]
        public bool HqOnly { get; set; }

        [XmlAttribute("MultipleQuests")]
        public bool MultipleQuests { get; set; }

        [XmlAttribute("NqOnly")]
        public bool NqOnly { get; set; }

        [DefaultValue(60)]
        [XmlAttribute("Timeout")]
        public int Timeout { get; set; }

        [DefaultValue("Yes.")]
        [XmlAttribute("YesText")]
        public string YesText { get; set; }

        protected override Color Info
        {
            get { return Colors.Plum; }
        }

        protected override void DoReset()
        {
            interactTimeout.Reset();
            turnedItemsIn = false;
            checkedTransport = false;
            iconStringIndex = 9001;
        }

        protected override async Task<bool> Main()
        {
            if (interactTimeout.Elapsed.TotalSeconds > Timeout)
            {
                Logger.Error(Localization.Localization.ExTurnInGuildLeve_TurninTimeout);
                isDone = true;
                return true;
            }

            if (!checkedTransport)
            {
                checkedTransport = true;

                StatusText = Localization.Localization.ExTurnInGuildLeve_CheckTransport;

                var selectYesnoCountWindow = new SelectYesnoCount();
                if (await selectYesnoCountWindow.Refresh(2000))
                {
                    StatusText = Localization.Localization.ExTurnInGuildLeve_SelectTransport;

                    if (AcceptTransport)
                    {
                        selectYesnoCountWindow.Yes();
                        await Coroutine.Wait(5000, () => CommonBehaviors.IsLoading);
                        await Coroutine.Wait(System.Threading.Timeout.Infinite, () => !CommonBehaviors.IsLoading);
                    }
                    else
                    {
                        await selectYesnoCountWindow.CloseInstance();
                    }

                    return true;
                }
            }

            // Movement
            if (ExProfileBehavior.Me.Distance(Location) > 3.5)
            {
                StatusText = Localization.Localization.ExTurnInGuildLeve_Move + NpcId;

                await Location.MoveTo(radius: 3.4f, name: " NpcId: " + NpcId);
                return true;
            }

            if (!interactTimeout.IsRunning)
            {
                interactTimeout.Restart();
            }

            // Interact
            if (Core.Target == null && ExProfileBehavior.Me.Distance(Location) <= 3.5)
            {
                return await InteractWithNpc();
            }

            if (Talk.DialogOpen)
            {
                await HandleTalk();
            }

            if (SelectIconString.IsOpen)
            {
                if (iconStringIndex == 9001)
                {
                    iconStringIndex = (uint)SelectIconString.Lines().Count - 1;
                }

                // We will just click the last quest and decrement until we have either no quests left or none to turn in.
                SelectIconString.ClickSlot(--iconStringIndex);
                await Coroutine.Sleep(500);

                if (iconStringIndex == uint.MaxValue)
                {
                    Logger.Warn(Localization.Localization.ExTurnInGuildLeve_NothingToTurnin);

                    isDone = true;
                    return true;
                }

                return true;
            }

            if (SelectString.IsOpen)
            {
                var lines = SelectString.Lines();

                // If Collect Reward exists, we click that; otherwise we will click Close. (-1 as uint = uint.MaxValue)
                var index = (uint)lines.IndexOf(CollectRewardText, StringComparer.InvariantCultureIgnoreCase);

                if (index != uint.MaxValue)
                {
                    Logger.Info(Localization.Localization.ExTurnInGuildLeve_CollectReward, WorldManager.EorzaTime);
                    SelectString.ClickSlot(index);
					if (await Coroutine.Wait(1000, () => Talk.DialogOpen))
					{
						await HandleTalk();
					}
                    await Coroutine.Yield();
                    return true;
                }

                // If yes is an option, click it to turn in more items.(crafting)
                index = (uint)lines.IndexOf(YesText, StringComparer.InvariantCultureIgnoreCase);

                if (index != uint.MaxValue)
                {
                    Logger.Info(Localization.Localization.ExTurnInGuildLeve_TurninMore, WorldManager.EorzaTime);
                    SelectString.ClickSlot(index);
                    await Coroutine.Yield();
                    return true;
                }

                Logger.Warn(Localization.Localization.ExTurnInGuildLeve_NoRewardsLeft);
                isDone = true;
                SelectString.ClickSlot(index);
                await Coroutine.Yield();
                return true;
            }

            if (Request.IsOpen)
            {
                var itemCount = Memory.Request.ItemsToTurnIn.Length;

                var itemId = Memory.Request.ItemId1;

                IEnumerable<BagSlot> itemSlots =
                    InventoryManager.FilledInventoryAndArmory
                        .Where(
                            bs => bs.RawItemId == itemId
                                  && bs.BagId != InventoryBagId.EquippedItems
                                  && !Blacklist.Contains((uint)bs.Pointer.ToInt64(), BlacklistFlags.Loot)).ToArray();

                if (HqOnly)
                {
                    itemSlots = itemSlots.Where(bs => bs.IsHighQuality);
                }

                if (NqOnly)
                {
                    itemSlots = itemSlots.Where(bs => !bs.IsHighQuality);
                }

                var items = itemSlots.Take(itemCount).ToArray();

                if (items.Length == 0)
                {
                    Logger.Warn(Localization.Localization.ExTurnInGuildLeve_NoItemToTurnin, HqOnly, NqOnly, itemId);
                    isDone = true;
                    return true;
                }

                StatusText = Localization.Localization.ExTurnInGuildLeve_TurnIn;

                var isHq = items.Any(i => i.IsHighQuality);
                var itemName = items[0].EnglishName;
                var requestAttempts = 0;
                while (Request.IsOpen && requestAttempts++ < 5 && Behaviors.ShouldContinue)
                {
                    foreach (var item in items)
                    {
                        item.Handover();
                        await Coroutine.Yield();
                    }

                    await Coroutine.Wait(1000, () => Request.HandOverButtonClickable);

                    if (Request.HandOverButtonClickable)
                    {
                        Request.HandOver();

                        if (isHq)
                        {
                            await Coroutine.Wait(2000, () => !Request.IsOpen && SelectYesno.IsOpen);
                        }
                        else
                        {
                            await Coroutine.Wait(2000, () => !Request.IsOpen);
                        }
                    }
                }

                turnedItemsIn = true;

                if (SelectYesno.IsOpen)
                {
                    SelectYesno.ClickYes();
                    await Coroutine.Yield();
                    Logger.Info(Localization.Localization.ExTurnInGuildLeve_TurnInHq, itemName, WorldManager.EorzaTime);
                }
                else
                {
                    Logger.Info(Localization.Localization.ExTurnInGuildLeve_TurnInNq, itemName, WorldManager.EorzaTime);
                }

                await HandleTalk();

                await Coroutine.Wait(2000, () => JournalResult.IsOpen);
                return true;
            }

            if (JournalResult.IsOpen)
            {
                await Coroutine.Wait(2000, () => JournalResult.ButtonClickable);
                JournalResult.Complete();
                Logger.Info(Localization.Localization.ExTurnInGuildLeve_Complete, WorldManager.EorzaTime);

                await Coroutine.Wait(2000, () => !JournalResult.IsOpen);
                await HandleTalk();

                return true;
            }

            if (!await WaitForOpenWindow())
            {
                if (MultipleQuests)
                {
                    Logger.Info(Localization.Localization.ExTurnInGuildLeve_OpenWindow);
                    CloseWindows();
                    ExProfileBehavior.Me.ClearTarget();
                }
                else
                {
                    isDone = true;
                }
            }

            return true;
        }

        protected override void OnDone()
        {
            interactTimeout.Stop();

            CloseWindows();
        }

        private void CloseWindows()
        {
            if (SelectYesno.IsOpen)
            {
                SelectYesno.ClickNo();
            }

            if (Request.IsOpen)
            {
                Request.Cancel();
            }

            if (JournalResult.IsOpen)
            {
                JournalResult.Decline();
            }

            if (SelectString.IsOpen)
            {
                SelectString.ClickSlot(uint.MaxValue);
            }

            if (SelectIconString.IsOpen)
            {
                SelectIconString.ClickSlot(uint.MaxValue);
            }

            if (Shop.Open)
            {
                Shop.Close();
            }
        }

        private async Task<bool> HandleTalk(int interval = 100)
        {
            await Coroutine.Wait(1000, () => Talk.DialogOpen);

            var ticks = 0;
            while (ticks++ < 50 && Talk.DialogOpen && Behaviors.ShouldContinue)
            {
                Talk.Next();
                await Coroutine.Sleep(interval);
            }

            return await WaitForOpenWindow();
        }

        private async Task<bool> InteractWithNpc()
        {
            var ticks = 0;
            while (ticks++ < 3 && !SelectIconString.IsOpen && !SelectString.IsOpen && !Request.IsOpen && !JournalResult.IsOpen
                   && Behaviors.ShouldContinue)
            {
                await this.Interact();

                if (!await HandleTalk() && turnedItemsIn)
                {
                    Logger.Warn(Localization.Localization.ExTurnInGuildLeve_NoMoreQuests);
                    isDone = true;
                    return true;
                }
            }

            if (ticks > 3)
            {
                Logger.Warn(Localization.Localization.ExTurnInGuildLeve_NoMoveQuests2);
                isDone = true;
                return true;
            }

            return true;
        }

        private async Task<bool> WaitForOpenWindow()
        {
            return
                await
                    Coroutine.Wait(3000, () => SelectIconString.IsOpen || SelectString.IsOpen || Request.IsOpen || JournalResult.IsOpen);
        }

        #region IInteractWithNpc Members

        [XmlAttribute("NpcLocation")]
        public Vector3 Location { get; set; }

        [XmlAttribute("NpcId")]
        public uint NpcId { get; set; }

        #endregion IInteractWithNpc Members
    }
}