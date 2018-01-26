﻿using Spawn.HDT.DustUtility.Mvvm;
using Spawn.HDT.DustUtility.Services;
using System.Collections.ObjectModel;

namespace Spawn.HDT.DustUtility.ViewModel
{
    public class AccountSelectorDialogViewModel : ViewModelBase, IDialogResultService<string>
    {
        #region Member Variables
        private string m_selectedAccountString;
        #endregion

        #region Properties
        #region Accounts
        public ObservableCollection<Account> Accounts { get; set; }
        #endregion

        #region SelectedAccountString
        public string SelectedAccountString
        {
            get => m_selectedAccountString;
            set => Set(ref m_selectedAccountString, value);
        }
        #endregion
        #endregion

        #region Ctor
        public AccountSelectorDialogViewModel()
        {
            WindowTitle = "Dust Utility - Select account...";

            Accounts = new ObservableCollection<Account>(DustUtilityPlugin.GetAccounts());

            if (!string.IsNullOrEmpty(DustUtilityPlugin.Config.LastSelectedAccount))
            {
                SelectedAccountString = DustUtilityPlugin.Config.LastSelectedAccount;
            }
            else
            {
                SelectedAccountString = Accounts[0].AccountString;
            }
        }
        #endregion

        #region GetDialogResult
        public string GetDialogResult() => SelectedAccountString;
        #endregion
    }
}