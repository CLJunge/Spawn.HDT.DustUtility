﻿#region Using
using Hearthstone_Deck_Tracker.Utility.Toasts;
using Spawn.HDT.DustUtility.Logging;
using System;
using System.Windows.Input;
#endregion

namespace Spawn.HDT.DustUtility.UI.Controls.Toasts
{
    public partial class NotificationToast
    {
        #region Ctor
        public NotificationToast() => InitializeComponent();

        public NotificationToast(string message)
            : this()
        {
            MessageTextBox.Text = message;

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Created new 'NotificationToast' instance (Message={MessageTextBox.Text})");
        }
        #endregion

        #region Custom Events
        public event EventHandler<MouseButtonEventArgs> Click;
        #endregion

        #region Events
        #region OnMouseLeftButtonUp
        private void OnMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToastManager.ForceCloseToast(this);

            DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Closed toast (Message={MessageTextBox.Text})");

            if (Click != null)
            {
                Click(this, e);

                DustUtilityPlugin.Logger.Log(LogLevel.Debug, "Executed 'OnClick' action");
            }
        }
        #endregion

        #region OnMouseEnter
        private void OnMouseEnter(object sender, MouseEventArgs e)
        {
            if (Cursor != Cursors.Wait)
                Cursor = Cursors.Hand;
        }
        #endregion

        #region OnMouseLeave
        private void OnMouseLeave(object sender, MouseEventArgs e)
        {
            if (Cursor != Cursors.Wait)
                Cursor = Cursors.Arrow;
        }
        #endregion
        #endregion
    }
}