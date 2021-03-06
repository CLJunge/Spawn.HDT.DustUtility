﻿namespace Spawn.HDT.DustUtility.UI.Windows
{
    public partial class CardSelectionWindow
    {
        #region Ctor
        public CardSelectionWindow()
        {
            InitializeComponent();

            DustUtilityPlugin.Logger.Log(Logging.LogLevel.Debug, "Created new 'CardSelectionWindow' instance");
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
        private void OnClosing(object sender, System.ComponentModel.CancelEventArgs e) => DustUtilityPlugin.CardSelection.SaveSelection();
        #endregion
        #endregion
    }
}