﻿#region Using
using GalaSoft.MvvmLight.CommandWpf;
using GalaSoft.MvvmLight.Messaging;
#if DEBUG
using Hearthstone_Deck_Tracker.Utility.Extensions;
#endif
using Spawn.HDT.DustUtility.CardManagement.Offline;
using Spawn.HDT.DustUtility.Hearthstone;
using Spawn.HDT.DustUtility.Logging;
using Spawn.HDT.DustUtility.Messaging;
using Spawn.HDT.DustUtility.UI.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
#endregion

namespace Spawn.HDT.DustUtility.UI.ViewModels
{
    public class HistoryFlyoutViewModel : ViewModelBase
    {
        #region Properties
        #region CanNotifyDirtyStatus
        public override bool CanNotifyDirtyStatus => false;
        #endregion

        #region CardItems
        public ObservableCollection<CardItemModel> CardItems { get; set; }
        #endregion

        #region ClearHistoryCommand
        public ICommand ClearHistoryCommand => new RelayCommand(ClearHistory, (() => CardItems.Count > 0));
        #endregion
        #endregion

        #region Ctor
        public HistoryFlyoutViewModel()
        {
            CardItems = new ObservableCollection<CardItemModel>();

#if DEBUG
            if (IsInDesignMode)
                InitializeAsync().Forget();
#endif

            Messenger.Default.Register<RemoveCardItemMessage>(this, RemoveCardItem);

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Created new 'HistoryFlyoutViewModel' instance");
        }
        #endregion

        #region Dtor
        ~HistoryFlyoutViewModel() => Messenger.Default.Unregister(this);
        #endregion

        #region InitializeAsync
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            if (ReloadRequired)
            {
                CardItems.Clear();

                List<CachedHistoryCard> lstHistory = DustUtilityPlugin.CurrentAccount.GetHistory();

                for (int i = 0; i < lstHistory.Count; i++)
                {
                    CardWrapper wrapper = new CardWrapper(lstHistory[i]);

                    CardItemModel cardItem = new CardItemModel(wrapper)
                    {
                        ColoredCount = true
                    };

                    if (cardItem.Count > 0)
                        cardItem.Dust = wrapper.RawCard.GetCraftingCost();

                    CardItems.Add(cardItem);
                }

                if (lstHistory.Count > 0)
                    DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Loaded history: {lstHistory.Count} entries");
                else
                    DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"No history available");

                ReloadRequired = false;
            }

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Finished initializing");
        }
        #endregion

        #region RemoveCardItem
        private void RemoveCardItem(RemoveCardItemMessage message)
        {
            if ((message.FlyoutName?.Equals(DustUtilityPlugin.HistoryFlyoutName) ?? false) && message.EventArgs?.RowIndex > -1)
            {
                CardItems.RemoveAt(message.EventArgs.RowIndex);

                HistoryManager.RemoveItemAt(DustUtilityPlugin.CurrentAccount, message.EventArgs.RowIndex);
            }
        }
        #endregion

        #region ClearHistory
        private void ClearHistory()
        {
            CardItems.Clear();

            HistoryManager.ClearHistory(DustUtilityPlugin.CurrentAccount);

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Cleared view model");
        }
        #endregion
    }
}