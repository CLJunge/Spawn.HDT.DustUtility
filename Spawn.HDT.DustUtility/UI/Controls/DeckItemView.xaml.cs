﻿namespace Spawn.HDT.DustUtility.UI.Controls
{
    public partial class DeckItemView
    {
        #region Ctor
        public DeckItemView()
        {
            InitializeComponent();

            DustUtilityPlugin.Logger.Log(Logging.LogLevel.Debug, "Initialized new 'DeckItemView' instance");
        }
        #endregion
    }
}