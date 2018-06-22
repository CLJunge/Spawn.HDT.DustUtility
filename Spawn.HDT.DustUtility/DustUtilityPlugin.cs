﻿#region Using
using CommonServiceLocator;
using GalaSoft.MvvmLight.Ioc;
using Hearthstone_Deck_Tracker.API;
using Hearthstone_Deck_Tracker.Plugins;
using Hearthstone_Deck_Tracker.Utility.Extensions;
using Hearthstone_Deck_Tracker.Utility.Toasts;
using MahApps.Metro.Controls.Dialogs;
using Spawn.HDT.DustUtility.AccountManagement;
using Spawn.HDT.DustUtility.CardManagement.Offline;
using Spawn.HDT.DustUtility.Logging;
using Spawn.HDT.DustUtility.Net;
using Spawn.HDT.DustUtility.UI.Controls.Toasts;
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
        #region Constants
        public const string DecksFlyoutName = "DecksFlyout";
        public const string HistoryFlyoutName = "HistoryFlyout";

        #endregion

        #region Static Fields
        private static bool s_blnInitialized;
        private static bool s_blnIsOffline = true;
        private static bool s_blnCheckedForUpdates;
#if DEBUG
        private static readonly IAccount s_mockAcc;
#endif
        #endregion

        #region Static Properties
        #region Logger
        public static Logger Logger { get; }
        #endregion

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
        public static bool IsOffline
        {
            get => s_blnIsOffline;
            private set
            {
                if (s_blnIsOffline != value)
                {
                    s_blnIsOffline = value;

                    IsOfflineChanged?.Invoke(null, EventArgs.Empty);
                }
                else { }
            }
        }
        #endregion

        #region MainWindow
        public static MainWindow MainWindow { get; private set; }
        #endregion

        #region SettingsDialog
        public static SettingsDialogView SettingsDialog { get; private set; }
        #endregion

        #region HideMainWindowOnClose
        public static bool HideMainWindowOnClose { get; private set; }
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
        public string Description => "Utility tool for collection management. Check GitHub readme for a detailed description and list of all features.";
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
            Logger = new Logger("dustutility", Path.Combine(DataDirectory, "Logs"));

            CreateContainer();

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

            Config.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(Config.CheckForUpdates)))
                {
                    s_blnCheckedForUpdates = !Config.CheckForUpdates;
                }
                else { }
            };

#if DEBUG
            s_mockAcc = new MockAccount();
#endif
        }
        #endregion

        #region Custom Events
        #region [STATIC] IsOfflineChanged
        public static event EventHandler IsOfflineChanged;
        #endregion
        #endregion

        #region IPlugin Methods
        #region OnLoad
        public void OnLoad()
        {
            s_blnInitialized = false;

            HideMainWindowOnClose = true;

            CreateContainer();

            IsOfflineChanged += OnIsOfflineChanged;

            if (MainWindow == null)
            {
                MainWindow = new MainWindow();
            }
            else { }

            GameEvents.OnModeChanged.Add(OnModeChanged);

            Task.Run(() => UpdateDataFiles()).ContinueWith(t =>
            {
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
            HideMainWindowOnClose = false;

            MainWindow?.Close();
            MainWindow = null;

            Config.Save();

            CurrentAccount.SavePreferences();

            ServiceLocator.SetLocatorProvider(null);

            SimpleIoc.Default.Reset();
        }
        #endregion

        #region OnUpdate
        public void OnUpdate()
        {
            IsOffline = !Core.Game.IsRunning;
        }
        #endregion
        #endregion

        #region Events
        #region OnMenuItemClick
        private async void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (s_blnInitialized)
            {
                if (!IsOffline || Config.OfflineMode)
                {
                    if (!CurrentAccount.IsValid)
                    {
                        IAccount selectedAcc = await SelectAccountAsync(false);

                        if (selectedAcc != null)
                        {
                            UpdatedAccountInstance(selectedAcc);
                        }
                        else { }
                    }
                    else { }

                    if (CurrentAccount.IsValid)
                    {
                        await ShowMainWindowAsync();
                    }
                    else { }
                }
                else if (!Config.OfflineMode)
                {
                    MessageBox.Show("Hearthstone isn't running!", Name, MessageBoxButton.OK, MessageBoxImage.Warning);

                    Logger.Log(LogLevel.Warning, "Hearthstone isn't running");
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

                Logger.Log(LogLevel.Warning, "Plugin is not initialized");
            }
        }
        #endregion

        #region OnIsOfflineChanged
        private async void OnIsOfflineChanged(object sender, EventArgs e)
        {
            Logger.Log(LogLevel.Debug, $"Switched to {(IsOffline ? "offline" : "online")} mode");

            IAccount loggedInAcc = await Account.GetLoggedInAccountAsync();

            if (loggedInAcc != null)
            {
                if (!IsOffline)
                {
                    UpdatedAccountInstance(loggedInAcc);

                    Cache.ClearCache();
                }
                else { }

                await ServiceLocator.Current.GetInstance<MainViewModel>().InitializeAsync();
            }
            else if (!IsOffline && MainWindow?.Visibility == Visibility.Visible)
            {
                await MainWindow?.ShowMessageAsync(string.Empty, "Couldn't retrieve the currently logged in account! Closing window...");

                MainWindow?.Close();
            }
            else
            {
                await ServiceLocator.Current.GetInstance<MainViewModel>().InitializeAsync();
            }

            if (IsOffline)
            {
                ShowToastNotification("Current Mode: Offline");
            }
            else
            {
                ShowToastNotification("Current Mode: Online");
            }
        }
        #endregion

        #region OnModeChanged
        private async void OnModeChanged(Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode mode)
        {
            switch (mode)
            {
                case Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode.HUB:
                case Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode.COLLECTIONMANAGER:
                case Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode.TOURNAMENT:
                    if (Config.OfflineMode)
                    {
                        await Cache.SaveAll(CurrentAccount);
                    }
                    else { }
                    break;
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

            retVal.Click += OnMenuItemClick;

            return retVal;
        }
        #endregion

        #region ShowSettingsDialog
        private async void ShowSettingsDialog()
        {
            Logger.Log(LogLevel.Debug, "Opening settings dialog");

            SettingsDialog = new SettingsDialogView()
            {
                Owner = Core.MainWindow
            };

            await ServiceLocator.Current.GetInstance<SettingsDialogViewModel>().InitializeAsync();

            SettingsDialog.ShowDialog();
        }
        #endregion

        #region UpdateDataFiles
        private void UpdateDataFiles()
        {
            Logger.Log(LogLevel.Debug, "Updating data files");

            if (Config.Version == 1)
            {
                MoveAccountFiles();

                UpdateHistoryFiles();

                MoveBackupFiles();

                Config.Version = 2;

                Logger.Log(LogLevel.Debug, "Update finished");
            }
            else
            {
                Logger.Log(LogLevel.Debug, "No file update required");
            }
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

                    if (idNode != null && countNode != null && isGoldenNode != null)
                    {
                        CachedHistoryCard card = new CachedHistoryCard
                        {
                            Id = idNode.InnerText,
                            Count = Convert.ToInt32(countNode.InnerText),
                            IsGolden = Convert.ToBoolean(isGoldenNode.InnerText),
                            Date = (timestampNode != null ? DateTime.Parse(timestampNode.InnerText) : DateTime.Now)
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
        #region CreateContainer
        public static void CreateContainer()
        {
            if (!ServiceLocator.IsLocationProviderSet)
            {
                ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

                if (GalaSoft.MvvmLight.ViewModelBase.IsInDesignModeStatic)
                {
#if DEBUG
                    SimpleIoc.Default.Register(() => s_mockAcc);
#endif
                }
                else
                {
#if DEBUG
                    SimpleIoc.Default.Register(() => s_mockAcc);
#else
                    SimpleIoc.Default.Register<IAccount>(() => Account.Empty);
#endif
                }

                SimpleIoc.Default.Register(() => Configuration.Load());

                SimpleIoc.Default.Register<MainViewModel>();
                SimpleIoc.Default.Register<CardSelectionWindowViewModel>();

                SimpleIoc.Default.Register<HistoryFlyoutViewModel>();
                SimpleIoc.Default.Register<UpdateFlyoutViewModel>();
                SimpleIoc.Default.Register<DecksFlyoutViewModel>();
                SimpleIoc.Default.Register<DeckListFlyoutViewModel>();
                SimpleIoc.Default.Register<SearchParametersFlyoutViewModel>();
                SimpleIoc.Default.Register<SortOrderFlyoutViewModel>();
                SimpleIoc.Default.Register<CollectionInfoFlyoutViewModel>();

                SimpleIoc.Default.Register<AccountSelectorDialogViewModel>();
                SimpleIoc.Default.Register<SettingsDialogViewModel>();
                SimpleIoc.Default.Register<SortOrderItemSelectorDialogViewModel>();
            }
            else { }
        }
        #endregion

        #region ShowMainWindowAsync
        private static async Task ShowMainWindowAsync()
        {
            Logger.Log(LogLevel.Info, $"Opening main window for {CurrentAccount.AccountString}");

            MainWindow?.Show();

            await ServiceLocator.Current.GetInstance<MainViewModel>().InitializeAsync();

            BringWindowToFront(MainWindow);
        }
        #endregion

        #region SelectAccountAsync
        private static async Task<IAccount> SelectAccountAsync(bool blnIsSwitching)
        {
            IAccount retVal = null;

            if (!IsOffline && !blnIsSwitching)
            {
                retVal = await Account.GetLoggedInAccountAsync();
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
                    Window owner = MainWindow;

                    if (!blnIsSwitching)
                    {
                        owner = Core.MainWindow;
                    }
                    else { }

                    await ServiceLocator.Current.GetInstance<AccountSelectorDialogViewModel>().InitializeAsync();

                    AccountSelectorDialogView dialog = new AccountSelectorDialogView()
                    {
                        Owner = owner
                    };

                    if (dialog.ShowDialog().Value)
                    {
                        retVal = Account.Parse(ServiceLocator.Current.GetInstance<AccountSelectorDialogViewModel>().SelectedAccountString);

                        Config.LastSelectedAccount = retVal.AccountString;
                    }
                    else { }
                }
                else
                {
                    MessageBox.Show("No account(s) available!"
                        + Environment.NewLine + Environment.NewLine +
                        "Collection and decks have to be stored locally for an account to be available.", "Dust Utility", MessageBoxButton.OK, MessageBoxImage.Warning);

                    Logger.Log(LogLevel.Warning, "No accounts available");
                }
            }

            if (retVal != null)
            {
                Logger.Log(LogLevel.Debug, $"Account: {retVal.AccountString}");
            }
            else
            {
                Logger.Log(LogLevel.Debug, $"No account selected");
            }

            return retVal;
        }
        #endregion

        #region GetAccounts
        public static IAccount[] GetAccounts()
        {
            List<IAccount> lstRet = new List<IAccount>();

#if DEBUG
            lstRet.Add(s_mockAcc);
#endif

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
        public static async Task<bool> SwitchAccount()
        {
            bool blnRet = false;

            if (MainWindow != null)
            {
                Logger.Log(LogLevel.Debug, "Switching account...");

                IAccount oldAcc = CurrentAccount;

                IAccount selectedAcc = await SelectAccountAsync(true);

                if (selectedAcc == null)
                {
                    selectedAcc = oldAcc;
                }
                else { }

                if (!selectedAcc.Equals(oldAcc))
                {
                    UpdatedAccountInstance(selectedAcc);

                    blnRet = true;
                }
                else { }

                ShowMainWindowAsync().Forget();

                Logger.Log(LogLevel.Debug, $"Switched account: Old={oldAcc.AccountString} New={selectedAcc.AccountString}");
            }
            else { }

            return blnRet;
        }
        #endregion

        #region UpdatedAccountInstance
        private static void UpdatedAccountInstance(IAccount account)
        {
            CurrentAccount.SavePreferences();

            //Remove current account instance
            SimpleIoc.Default.Unregister<IAccount>();

            //And readd it
            SimpleIoc.Default.Register(() => account);
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
            if (window != null)
            {
                window.Activate();
                window.Topmost = true;
                window.Topmost = false;
                window.Focus();
            }
            else { }
        }
        #endregion

        #region GetCollectionWrapper
        public static List<HearthMirror.Objects.Card> GetCollectionWrapper()
        {
            List<HearthMirror.Objects.Card> lstRet = null;

            if (!IsOffline)
            {
                lstRet = HearthMirror.Reflection.GetCollection()?.Where(c => c.Count > 0).ToList();
            }
            else { }

            return lstRet;
        }
        #endregion

        #region ShowToastNotification
        public static void ShowToastNotification(string strMessage)
        {
            if (Config.ShowNotifications)
            {
                Hearthstone_Deck_Tracker.Core.MainWindow?.Dispatcher.Invoke(() =>
                    {
                        NotificationToast toast = new NotificationToast(strMessage);

                        ToastManager.ShowCustomToast(toast);
                    });
            }
            else { }
        }
        #endregion

        #region PerformUpdateCheckAsync
        public static async Task PerformUpdateCheckAsync()
        {
            if (!s_blnCheckedForUpdates)
            {
                if (Config.CheckForUpdates && await UpdateManager.CheckForUpdatesAsync())
                {
                    MainWindow?.Dispatcher.Invoke(() =>
                    {
                        ServiceLocator.Current.GetInstance<MainViewModel>().OpenFlyoutCommand.Execute(MainWindow.UpdateFlyout);
                    });

                    if (MainWindow == null || MainWindow?.Visibility != Visibility.Visible)
                    {
                        ShowToastNotification("New Update Available!");
                    }
                    else { }
                }
                else { }

                s_blnCheckedForUpdates = true;
            }
            else { }
        }
        #endregion
        #endregion
    }
}