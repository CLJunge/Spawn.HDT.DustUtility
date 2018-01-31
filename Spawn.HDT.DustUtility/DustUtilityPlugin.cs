﻿#region Using
using GalaSoft.MvvmLight.Ioc;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Logging;
using Microsoft.Practices.ServiceLocation;
using Spawn.HDT.DustUtility.AccountManagement;
using Spawn.HDT.DustUtility.CardManagement;
using Spawn.HDT.DustUtility.CardManagement.Offline;
using Spawn.HDT.DustUtility.Services;
using Spawn.HDT.DustUtility.Services.Providers;
using Spawn.HDT.DustUtility.UI.Dialogs;
using Spawn.HDT.DustUtility.UI.ViewModels;
using Spawn.HDT.DustUtility.UI.Windows;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml;
#endregion

namespace Spawn.HDT.DustUtility
{
    public class DustUtilityPlugin : IPlugin
    {
        #region Static Fields
        private static MainWindow s_mainWindow;
        private static bool s_blnInitialized;
        #endregion

        #region Static Properties
        #region DataDirectory
        public static string DataDirectory => GetDataDirectory();
        #endregion

        #region BackupsDirectory
        public static string BackupsDirectory => GetDataDirectory("Backups");
        #endregion

        #region AccountsDirectory
        public static string AccountsDirectory => GetDataDirectory("Accounts");
        #endregion

        #region IsOffline
        public static bool IsOffline { get; private set; }
        #endregion

        #region MainWindow
        public static MainWindow MainWindow => s_mainWindow;
        #endregion

        #region CurrentAccount
        public static IAccount CurrentAccount => ServiceLocator.Current.GetInstance<IAccount>();
        #endregion

        #region Config
        public static Configuration Config => ServiceLocator.Current.GetInstance<Configuration>();
        #endregion

        #region NumericRegex
        public static Regex NumericRegex => new Regex("^(0|[1-9][0-9]*)$");
        #endregion

        #region RarityBrushes
        public static Dictionary<int, SolidColorBrush> RarityBrushes { get; }
        #endregion
        #endregion

        #region Properties
        #region Name
        public string Name => "Dust Utility";
        #endregion

        #region Description
        public string Description => "Utility tool for dust management. Check GitHub readme for a detailed description and a list of all features.";
        #endregion

        #region ButtonText
        public string ButtonText => "Settings...";
        #endregion

        #region Author
        public string Author => "CLJunge";
        #endregion

        #region Version
        public Version Version => new Version(Assembly.GetExecutingAssembly().GetName().Version.ToString(3));
        #endregion

        #region MenuItem
        public MenuItem MenuItem => CreateMenuItem();
        #endregion
        #endregion

        #region Static Ctor
        static DustUtilityPlugin()
        {
#if DEBUG
            if (!GalaSoft.MvvmLight.ViewModelBase.IsInDesignModeStatic)
            {
                ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
                SimpleIoc.Default.Register<IAccount>(() => new MockAccount());
            }
            else { }
#else
            ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);
            SimpleIoc.Default.Register<IAccount>(() => Account.Empty);
#endif

            SimpleIoc.Default.Register<IDialogService, DialogServiceProvider>();
            SimpleIoc.Default.Register<ICardsManager, CardsManager>();
            SimpleIoc.Default.Register(() => Configuration.Load());

            RarityBrushes = new Dictionary<int, SolidColorBrush>
            {
                //Common
                { 1, new SolidColorBrush(Color.FromRgb(38, 168, 16)) },
                //Rare
                //s_dColors.Add(3, new SolidColorBrush(Color.FromRgb(18, 92, 204)));
                { 3, new SolidColorBrush(Color.FromRgb(30, 113, 255)) },
                //Epic
                //s_dColors.Add(4, new SolidColorBrush(Color.FromRgb(135, 14, 186)));
                { 4, new SolidColorBrush(Color.FromRgb(163, 58, 234)) },
                //Legendary
                //s_dColors.Add(5, new SolidColorBrush(Color.FromRgb(226, 119, 24)));
                { 5, new SolidColorBrush(Color.FromRgb(255, 153, 0)) }
            };
        }
        #endregion

        #region IPlugin Methods
        #region OnLoad
        public void OnLoad()
        {
            s_blnInitialized = false;

            Task.Run(() => UpdatePluginFiles()).ContinueWith(t =>
            {
                if (Config.OfflineMode && Core.Game.IsRunning)
                {
                    Cache.StartTimer();
                }
                else { }

                s_blnInitialized = true;
            });
        }
        #endregion

        #region OnButtonPress
        public void OnButtonPress()
        {
            ShowSettingsDialog();
        }
        #endregion

        #region OnUnload
        public void OnUnload()
        {
            if (Cache.TimerEnabled)
            {
                Cache.StopTimer();
            }
            else { }

            Config.Save();

            SimpleIoc.Default.Reset();

            ServiceLocator.SetLocatorProvider(null);
        }
        #endregion

        #region OnUpdate
        public void OnUpdate()
        {
            IsOffline = !Core.Game.IsRunning && Config.OfflineMode;

            if (Config.OfflineMode && (Core.Game.IsRunning && !Cache.TimerEnabled))
            {
                Cache.StartTimer();
            }
            else if (!Core.Game.IsRunning && Cache.TimerEnabled)
            {
                Cache.StopTimer();
            }
            else { }
        }
        #endregion

        #region OnClick
        private void OnClick(object sender, RoutedEventArgs e)
        {
            if (s_blnInitialized)
            {
                if (Core.Game.IsRunning || Config.OfflineMode)
                {
                    if (!CurrentAccount.IsValid)
                    {
                        IAccount selectedAcc = SelectAccount(false);

                        if (selectedAcc != null)
                        {
                            UpdatedAccountInstance(selectedAcc);
                        }
                        else { }
                    }
                    else { }

                    if (CurrentAccount.IsValid)
                    {
                        OpenMainWindow();
                    }
                    else { }
                }
                else if (!Config.OfflineMode)
                {
                    MessageBox.Show("Hearthstone isn't running!", Name, MessageBoxButton.OK, MessageBoxImage.Warning);

                    Log.WriteLine("Hearthstone isn't running", LogType.Warning);
                }
                else { }
            }
            else
            {
                string strMessage = "Plugin is still initializing...";

                if (Config.Version == 1)
                {
                    strMessage = $"{strMessage} (Updating account files to new format)";
                }
                else { }

                MessageBox.Show(strMessage, Name, MessageBoxButton.OK, MessageBoxImage.Information);

                Log.WriteLine("Plugin is not initialized", LogType.Info);
            }
        }
        #endregion
        #endregion

        #region CreateMenuItem
        private MenuItem CreateMenuItem()
        {
            MenuItem retVal = new MenuItem()
            {
                Header = Name,
                Icon = new Image()
                {
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri("/Spawn.HDT.DustUtility;component/Resources/icon.png", UriKind.Relative))
                }
            };

            retVal.Click += OnClick;

            return retVal;
        }
        #endregion

        #region ShowSettingsDialog
        private void ShowSettingsDialog()
        {
            Log.WriteLine("Opening settings dialog", LogType.Debug);

            using (IDialogService dialogService = ServiceLocator.Current.GetInstance<IDialogService>())
            {
                if (dialogService.ShowDialog<SettingsDialogView>(Core.MainWindow))
                {
                    if (Config.OfflineMode && Core.Game.IsRunning)
                    {
                        if (!Cache.TimerEnabled)
                        {
                            Cache.StartTimer();
                        }
                        else
                        {
                            //Reinitialize timer with new interval
                            Cache.StopTimer();

                            Cache.StartTimer();
                        }
                    }
                    else if (!Config.OfflineMode && Cache.TimerEnabled)
                    {
                        Cache.StopTimer();
                    }
                    else { }

                    ReloadFlyoutViews();
                }
                else { }
            }
        }
        #endregion

        #region UpdatePluginFiles
        private void UpdatePluginFiles()
        {
            Log.WriteLine("Updating plugin files", LogType.Debug);

            if (Config.Version == 1)
            {
                MoveAccountFiles();

                UpdateHistoryFiles();

                MoveBackupFiles();

                Config.Version = 2;
            }
            else { }

            Log.WriteLine("Finished updating plugin files", LogType.Debug);
        }
        #endregion

        #region MoveAccountFiles
        private void MoveAccountFiles()
        {
            string[] vFiles = Directory.GetFiles(DataDirectory);

            if (vFiles.Length >= 1)
            {
                string[] vTypes = new string[]
                {
                    Account.CollectionString,
                    Account.DecksString,
                    Account.HistoryString,
                    Account.PreferencesString
                };

                for (int i = 0; i < vTypes.Length; i++)
                {
                    string[] vChunk = vFiles.Where(s => s.Contains($"_{vTypes[i]}.xml")).ToArray();

                    for (int j = 0; j < vChunk.Length; j++)
                    {
                        FileInfo fileInfo = new FileInfo(vChunk[j]);

                        string strTargetPath = Path.Combine(AccountsDirectory, fileInfo.Name);

                        if (File.Exists(strTargetPath))
                        {
                            File.Delete(strTargetPath);
                        }
                        else { }

                        fileInfo.MoveTo(strTargetPath);
                    }
                }
            }
            else { }
        }
        #endregion

        #region UpdateHistoryFiles
        private void UpdateHistoryFiles()
        {
            string[] vFiles = Directory.GetFiles(AccountsDirectory, $"*_{Account.HistoryString}.xml");

            Parallel.For(0, vFiles.Length, i =>
            {
                XmlDocument document = new XmlDocument();

                document.Load(vFiles[i]);

                List<CachedHistoryCard> lstHistory = new List<CachedHistoryCard>();

                for (int j = 0; j < document.DocumentElement.ChildNodes.Count; j++)
                {
                    XmlNode cardNode = document.DocumentElement.ChildNodes[j];

                    XmlNode idNode = cardNode.SelectSingleNode("Id");
                    XmlNode countNode = cardNode.SelectSingleNode("Count");
                    XmlNode isGoldenNode = cardNode.SelectSingleNode("IsGolden");
                    XmlNode timestampNode = cardNode.SelectSingleNode("Timestamp");

                    if (idNode != null && countNode != null && isGoldenNode != null && timestampNode != null)
                    {
                        CachedHistoryCard card = new CachedHistoryCard
                        {
                            Id = idNode.InnerText,
                            Count = Convert.ToInt32(countNode.InnerText),
                            IsGolden = Convert.ToBoolean(isGoldenNode.InnerText),
                            Date = DateTime.Parse(timestampNode.InnerText)
                        };

                        lstHistory.Add(card);
                    }
                    else { }
                }

                FileManager.Write(vFiles[i], lstHistory);
            });
        }
        #endregion

        #region MoveBackupFiles
        private void MoveBackupFiles()
        {
            string strOldDir = Path.Combine(DataDirectory, "Backup");

            if (Directory.Exists(strOldDir))
            {
                Directory.Move(strOldDir, Path.Combine(DataDirectory, "Backups"));
            }
            else { }
        }
        #endregion

        #region Static Methods
        #region OpenMainWindow
        private static void OpenMainWindow()
        {
            if (s_mainWindow == null)
            {
                Log.WriteLine($"Opening main window for {CurrentAccount.AccountString}", LogType.Info);

                //s_mainView = new MainWindowView(s_account, GetAccounts().Length > 1);
                s_mainWindow = new MainWindow();

                s_mainWindow.Closed += (s, e) =>
                {
                    CurrentAccount.SavePreferences();

                    s_mainWindow = null;
                };

                ServiceLocator.Current.GetInstance<MainViewModel>().Initialize();

                s_mainWindow.Show();
            }
            else
            {
                BringWindowToFront(s_mainWindow);
            }
        }
        #endregion

        #region SelectAccount
        private static IAccount SelectAccount(bool blnIsSwitching)
        {
            IAccount retVal = null;

            if (Core.Game.IsRunning && !blnIsSwitching)
            {
                retVal = Account.LoggedInAccount;
            }
            else
            {
                IAccount[] vAccounts = GetAccounts();

                if (vAccounts.Length == 1)
                {
                    retVal = vAccounts[0];
                }
                else if (vAccounts.Length > 1)
                {
                    Window owner = s_mainWindow;

                    if (!blnIsSwitching)
                    {
                        owner = Core.MainWindow;
                    }
                    else { }

                    using (IDialogService dialogService = ServiceLocator.Current.GetInstance<IDialogService>())
                    {
                        if (dialogService.ShowDialog<AccountSelectorDialogView>(owner))
                        {
                            retVal = Account.Parse(dialogService.GetDialogResult<string>());

                            Config.LastSelectedAccount = retVal.AccountString;
                        }
                        else { }
                    }
                }
                else
                {
                    retVal = Account.Empty;
                }
            }

            if (retVal != null)
            {
                Log.WriteLine($"Account: {retVal.AccountString}", LogType.Debug);
            }
            else
            {
                Log.WriteLine($"No account selected", LogType.Debug);
            }

            return retVal;
        }
        #endregion

        #region GetAccounts
        public static IAccount[] GetAccounts()
        {
            List<IAccount> lstRet = new List<IAccount>();

            if (Directory.Exists(DataDirectory))
            {
                string[] vFiles = Directory.GetFiles(AccountsDirectory, $"*_{Account.CollectionString}.xml");

                for (int i = 0; i < vFiles.Length; i++)
                {
                    string strCollectionFileName = vFiles[i];

                    string strDecksFileName = strCollectionFileName.Replace($"_{Account.CollectionString}", $"_{Account.DecksString}");

                    if (File.Exists(strDecksFileName))
                    {
                        string strAccountString = Path.GetFileNameWithoutExtension(strCollectionFileName).Replace($"_{Account.CollectionString}", string.Empty);

                        lstRet.Add(Account.Parse(strAccountString));
                    }
                    else { }
                }
            }
            else { }

            return lstRet.ToArray();
        }
        #endregion

        #region SwitchAccount
        public static void SwitchAccount()
        {
            if (s_mainWindow != null)
            {
                Log.WriteLine("Switching account...", LogType.Debug);

                IAccount oldAcc = CurrentAccount;

                IAccount selectedAcc = SelectAccount(true);

                if (selectedAcc == null)
                {
                    selectedAcc = oldAcc;
                }
                else { }

                if (!selectedAcc.Equals(oldAcc))
                {
                    s_mainWindow.Close();

                    UpdatedAccountInstance(selectedAcc);

                    ReloadFlyoutViews();
                }
                else { }

                OpenMainWindow();

                Log.WriteLine($"Switched account: Old={oldAcc.AccountString} New={selectedAcc.AccountString}", LogType.Debug);
            }
            else { }
        }
        #endregion

        #region UpdatedAccountInstance
        private static void UpdatedAccountInstance(IAccount account)
        {
            //Remove current account instance and card manager
            SimpleIoc.Default.Unregister<IAccount>();
            SimpleIoc.Default.Unregister<ICardsManager>();

            //And readd them
            SimpleIoc.Default.Register(() => account);
            SimpleIoc.Default.Register<ICardsManager, CardsManager>();
        }
        #endregion

        #region ReloadFlyoutViews
        private static void ReloadFlyoutViews()
        {
            ServiceLocator.Current.GetInstance<HistoryFlyoutViewModel>().ReloadRequired = true;
            ServiceLocator.Current.GetInstance<DecksFlyoutViewModel>().ReloadRequired = true;
            ServiceLocator.Current.GetInstance<SearchParametersFlyoutViewModel>().ReloadRequired = true;
        }
        #endregion

        #region GetDataDirectory
        private static string GetDataDirectory(string strFolder = "")
        {
            string strRet = Path.Combine(Hearthstone_Deck_Tracker.Config.Instance.DataDir, "DustUtility", strFolder);

            if (!Directory.Exists(strRet))
            {
                Directory.CreateDirectory(strRet);
            }
            else { }

            return strRet;
        }
        #endregion

        #region GetFullFileName
        public static string GetFullFileName(IAccount account, string strType)
        {
            string strRet = string.Empty;

            if (!account.IsEmpty)
            {
                strRet = Path.Combine(DataDirectory, "Accounts", $"{account.AccountString}_{strType}.xml");
            }
            else
            {
                strRet = Path.Combine(DataDirectory, "Accounts", $"{strType}.xml");
            }

            return strRet;
        }
        #endregion

        #region BringWindowToFront
        public static void BringWindowToFront(Window window)
        {
            window.Activate();
            window.Topmost = true;
            window.Topmost = false;
            window.Focus();
        }
        #endregion
        #endregion
    }
}