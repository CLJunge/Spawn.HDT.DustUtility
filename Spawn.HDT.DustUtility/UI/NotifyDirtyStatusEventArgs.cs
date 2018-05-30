﻿#region Using
using System.ComponentModel;
#endregion

namespace Spawn.HDT.DustUtility.UI
{
    public class NotifyDirtyStatusEventArgs : PropertyChangedEventArgs
    {
        #region Properties
        #region IsDirty
        public bool IsDirty { get; private set; }
        #endregion
        #endregion

        #region Ctor
        public NotifyDirtyStatusEventArgs(string propertyName, bool isDirty) : base(propertyName)
        {
            IsDirty = isDirty;
        }
        #endregion
    }
}