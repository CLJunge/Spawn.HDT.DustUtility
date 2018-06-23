﻿#region Using
using CommonServiceLocator;
using Spawn.HDT.DustUtility.UI.ViewModels;
#endregion

namespace Spawn.HDT.DustUtility.UI.Windows
{
    public partial class MainWindow
    {
        #region Ctor
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region Events
        #region OnRemoveCardItem
        private void OnRemoveCardItem(object sender, CardItemEventArgs e) => DustUtilityPlugin.CardSelection.OnRemoveCardItem(e);
        #endregion

        #region OnItemDropped
        private async void OnItemDropped(object sender, CardItemEventArgs e) => await DustUtilityPlugin.CardSelection.OnItemDropped(e);
        #endregion

        #region OnClosing
        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            DustUtilityPlugin.CardSelection.OnClosing(sender, e);

            ServiceLocator.Current.GetInstance<MainViewModel>().OnClosing(sender, e);
        }
        #endregion
        #endregion
    }
}