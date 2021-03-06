﻿#region Using
using Spawn.HDT.DustUtility.Logging;
using Spawn.HDT.DustUtility.UI;
#endregion

namespace Spawn.HDT.DustUtility.Messaging
{
    public class RemoveCardItemMessage
    {
        #region Properties
        #region FlyoutName
        public string FlyoutName { get; private set; }
        #endregion

        #region EventArgs
        public CardItemEventArgs EventArgs { get; private set; }
        #endregion
        #endregion

        #region Ctor
        public RemoveCardItemMessage(string flyoutName, CardItemEventArgs eventArgs)
        {
            FlyoutName = flyoutName;
            EventArgs = eventArgs;

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Created new 'RemoveCardItemMessage' instance");
        }
        #endregion
    }
}