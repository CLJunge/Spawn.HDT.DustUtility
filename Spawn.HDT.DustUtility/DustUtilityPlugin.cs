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
using Spawn.HDT.DustUtility.Properties;
using Spawn.HDT.DustUtility.UI;
using Spawn.HDT.DustUtility.UI.Controls.Toasts;
using Spawn.HDT.DustUtility.UI.Dialogs;
using Spawn.HDT.DustUtility.UI.ViewModels;
using Spawn.HDT.DustUtility.UI.Windows;
using Spawn.HDT.DustUtility.Utilities;
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
        public const string SearchParametersFlyoutName = "SearchParametersFlyout";
        private const int AccountWaitTimeout = 10;
        #endregion

        #region Static Fields
#pragma warning disable S1450 // Private fields only used as local variables in methods should become local variables
        private static Configuration s_config;
        private static CardSelectionManager s_cardSelection;
#pragma warning restore S1450 // Private fields only used as local variables in methods should become local variables
        private static bool s_blnIsOffline = true;
        private bool? m_blnPreviousIsOfflineValue = true;
        private static bool s_blnCheckedForUpdates;
        private static UpdateWindow s_updateDialog;
        #endregion

        #region Member Variables
        private bool m_blnInitialized;
        private DateTime m_dtLastSaveTimestamp = DateTime.MinValue;
        private bool m_blnForceSave;
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
        public static Configuration Config => s_config ?? (s_config = Configuration.Load());
        #endregion

        #region CardSelection
        public static CardSelectionManager CardSelection => s_cardSelection ?? (s_cardSelection = new CardSelectionManager());
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
#pragma warning disable S3963 // "static" fields should be initialized inline
        static DustUtilityPlugin()
        {
            Logger = new Logger(logDirectory: Path.Combine(DataDirectory, "Logs"));

#if DEBUG
            Logger.DebugMode = true;
#endif

            Logger.Log(LogLevel.All, $"---- Dust Utility {Assembly.GetExecutingAssembly().GetName().Version.ToString(3)} ----");

            CreateContainer();

            RarityBrushes = new Dictionary<int, SolidColorBrush>
            {
                //Common
                { 1, new SolidColorBrush(Color.FromRgb(38, 168, 16)) },
                //Rare
                { 3, new SolidColorBrush(Color.FromRgb(30, 113, 255)) },
                //Epic
                { 4, new SolidColorBrush(Color.FromRgb(163, 58, 234)) },
                //Legendary
                { 5, new SolidColorBrush(Color.FromRgb(255, 153, 0)) }
            };

            Config.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName.Equals(nameof(Config.CheckForUpdates)))
                    s_blnCheckedForUpdates = !Config.CheckForUpdates;
            };

            Logger.Log(LogLevel.Debug, "Initialized 'DustUtilityPlugin'");
        }
#pragma warning restore S3963 // "static" fields should be initialized inline
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
            m_blnInitialized = false;

            LoadPlugin();
        }
        #endregion

        #region OnButtonPress
        public void OnButtonPress() => ShowSettingsDialog().Forget();
        #endregion

        #region OnUnload
        public void OnUnload() => UnloadPlugin();
        #endregion

        #region OnUpdate
        public void OnUpdate() => IsOffline = !Core.Game.IsRunning;
        #endregion
        #endregion

        #region LoadPlugin
        private void LoadPlugin()
        {
            Logger.Log(LogLevel.Trace, "Loading plugin...");

            HideMainWindowOnClose = true;

            CreateContainer();

            IsOfflineChanged += OnIsOfflineChanged;

            if (MainWindow == null)
                MainWindow = new MainWindow();

            GameEvents.OnModeChanged.Add(OnModeChanged);

            Task.Run(() => UpdateDataFiles()).ContinueWith(t =>
            {
                m_blnInitialized = true;

                Logger.Log(LogLevel.Trace, "Plugin loaded sucessfully");
            });
        }
        #endregion

        #region UnloadPlugin
        private void UnloadPlugin()
        {
            HideMainWindowOnClose = false;

            MainWindow?.Close();
            MainWindow = null;

            Config.Save();

            CurrentAccount.SavePreferences();

            ServiceLocator.SetLocatorProvider(null);

            SimpleIoc.Default.Reset();

            Logger.Log(LogLevel.Trace, "Unloaded plugin");
        }
        #endregion

        #region Events
        #region OnMenuItemClick
        private async void OnMenuItemClick(object sender, RoutedEventArgs e)
        {
            if (m_blnInitialized)
            {
                if (!IsOffline || Config.OfflineMode)
                {
                    if (!CurrentAccount.IsValid)
                    {
                        IAccount selectedAcc = await SelectAccountAsync(false);

                        if (selectedAcc != null)
                            UpdatedAccountInstance(selectedAcc);
                    }

                    if (CurrentAccount.IsValid)
                        await ShowMainWindowAsync();
                }
                else if (!Config.OfflineMode)
                {
                    MessageBox.Show("Hearthstone is not running!", Name, MessageBoxButton.OK, MessageBoxImage.Warning);

                    Logger.Log(LogLevel.Warning, "Hearthstone is not running");
                }
            }
            else
            {
                string strMessage = "Plugin is still initializing...";

                if (Config.Version == 1)
                    strMessage = $"{strMessage} (Updating account files to new format)";

                MessageBox.Show(strMessage, Name, MessageBoxButton.OK, MessageBoxImage.Information);

                Logger.Log(LogLevel.Warning, "Plugin is not initialized");
            }
        }
        #endregion

        #region OnIsOfflineChanged
        private async void OnIsOfflineChanged(object sender, EventArgs e)
        {
            if (IsOffline != m_blnPreviousIsOfflineValue)
            {
                if (Config.OfflineMode)
                {
                    Logger.Log(LogLevel.Debug, $"Switched to {(IsOffline ? "offline" : "online")} mode");

                    int nCounter = 0;
                    IAccount loggedInAcc;

                    do
                    {
                        await Task.Delay(200);

                        loggedInAcc = await Account.GetLoggedInAccountAsync();

                        nCounter += 1;
                    }
                    while (loggedInAcc == null && nCounter <= AccountWaitTimeout);

#pragma warning disable S2583 // Conditionally executed blocks should be reachable
                    if (loggedInAcc?.IsValid ?? false)
#pragma warning restore S2583 // Conditionally executed blocks should be reachable
                    {
                        if (!IsOffline)
                        {
                            UpdatedAccountInstance(loggedInAcc);

                            Cache.ClearCache();
                        }

                        await ServiceLocator.Current.GetInstance<MainViewModel>().InitializeAsync();
                    }
                    else if (!IsOffline && MainWindow?.Visibility == Visibility.Visible)
                    {
                        await MainWindow?.ShowMessageAsync(string.Empty, "Couldn't get the currently logged in account! Closing window...");

                        MainWindow?.Close();
                    }
                    else
                    {
                        await ServiceLocator.Current.GetInstance<MainViewModel>().InitializeAsync();
                    }

                    if (!IsOffline)
                    {
                        ServiceLocator.Current.GetInstance<MainViewModel>().TryUpdateDecksButton(false);

                        m_blnForceSave = true;
                    }

                    ShowToastNotification($"Current Mode: {(IsOffline ? "Offline" : "Online")}");
                }
                else if (IsOffline)
                {
                    MainWindow?.Close();
                }

                m_blnPreviousIsOfflineValue = IsOffline;
            }
        }
        #endregion

        #region OnModeChanged
#pragma warning disable S3168 // "async" methods should not return "void"
        private async void OnModeChanged(Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode mode)
#pragma warning restore S3168 // "async" methods should not return "void"
        {
            switch (mode)
            {
                case Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode.HUB:
                case Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode.TOURNAMENT:
                case Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode.COLLECTIONMANAGER:
                    if (Config.OfflineMode
                        && ((DateTime.Now - m_dtLastSaveTimestamp).Seconds >= GetSaveDelay()
                        || m_blnForceSave)
                        && await Cache.SaveAllAsync(Config.EnableHistory))
                    {
                        m_blnForceSave = false;

                        m_dtLastSaveTimestamp = DateTime.Now;
                    }

                    if (mode == Hearthstone_Deck_Tracker.Enums.Hearthstone.Mode.TOURNAMENT)
                        ServiceLocator.Current.GetInstance<MainViewModel>().TryUpdateDecksButton(true);

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
                    Source = new System.Windows.Media.Imaging.BitmapImage(new Uri(Settings.Default.IconResourcePath, UriKind.Relative))
                }
            };

            retVal.Click += OnMenuItemClick;

            Logger.Log(LogLevel.Debug, "Created menu item");

            return retVal;
        }
        #endregion

        #region ShowSettingsDialog
        private async Task ShowSettingsDialog()
        {
            Logger.Log(LogLevel.Debug, "Opening settings dialog");

            SettingsDialog = new SettingsDialogView()
            {
                Owner = Core.MainWindow
            };

            await ServiceLocator.Current.GetInstance<SettingsDialogViewModel>().InitializeAsync();

            if (SettingsDialog.ShowDialog().Value)
                ServiceLocator.Current.GetInstance<MainViewModel>().InitializeAsync().Forget();
        }
        #endregion

        #region UpdateDataFiles
        private void UpdateDataFiles()
        {
            if (Config.Version == 1)
            {
                Logger.Log(LogLevel.Debug, "Updating data files");

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
                            File.Delete(strTargetPath);

                        fileInfo.MoveTo(strTargetPath);
                    }
                }

                Logger.Log(LogLevel.Debug, "Moved account files to new directory");
            }
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
                }

                FileHelper.Write(vFiles[i], lstHistory);
            });

            Logger.Log(LogLevel.Debug, "Updated history files");
        }
        #endregion

        #region MoveBackupFiles
        private void MoveBackupFiles()
        {
            string strOldDir = Path.Combine(DataDirectory, "Backup");

            if (Directory.Exists(strOldDir))
            {
                Directory.Move(strOldDir, Path.Combine(DataDirectory, "Backups"));

                Logger.Log(LogLevel.Debug, "Moved backups to new directory");
            }
        }
        #endregion

        #region Static Methods
        #region CreateContainer
        public static void CreateContainer()
        {
            if (!ServiceLocator.IsLocationProviderSet)
            {
                ServiceLocator.SetLocatorProvider(() => SimpleIoc.Default);

                SimpleIoc.Default.Register<IAccount>(() => Account.Empty);

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

                Logger.Log(LogLevel.Debug, "Created IOC container");
            }
        }
        #endregion

        #region ShowMainWindowAsync
        private static async Task ShowMainWindowAsync()
        {
            Logger.Log(LogLevel.Trace, $"Opening main window for {CurrentAccount.DisplayString}");

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
                        owner = Core.MainWindow;

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
                }
                else
                {
                    MessageBox.Show("No account(s) available!"
                        + Environment.NewLine + Environment.NewLine +
                        "Collection has to be saved locally for an account to be available.", "Dust Utility", MessageBoxButton.OK, MessageBoxImage.Warning);

                    Logger.Log(LogLevel.Warning, "No accounts available");
                }
            }

            if (retVal != null)
                Logger.Log(LogLevel.Debug, $"Account: {retVal.DisplayString}");
            else
                Logger.Log(LogLevel.Debug, $"No account selected");

            return retVal;
        }
        #endregion

        #region GetAccounts
        public static IAccount[] GetAccounts()
        {
            List<IAccount> lstRet = new List<IAccount>();

            Logger.Log(LogLevel.Debug, "Loading accounts...");

            if (Directory.Exists(DataDirectory))
            {
                string[] vFiles = Directory.GetFiles(AccountsDirectory, $"*_{Account.CollectionString}.xml");

                for (int i = 0; i < vFiles.Length; i++)
                    lstRet.Add(Account.Parse(Path.GetFileNameWithoutExtension(vFiles[i]).Replace($"_{Account.CollectionString}", string.Empty)));
            }

            Logger.Log(LogLevel.Debug, $"Loaded {lstRet.Count} account(s)");

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
                    selectedAcc = oldAcc;

                if (!selectedAcc.Equals(oldAcc))
                {
                    UpdatedAccountInstance(selectedAcc);

                    if (IsOffline)
                        Cache.ClearCache();

                    await ShowMainWindowAsync();

                    blnRet = true;
                }

                Logger.Log(LogLevel.Debug, $"Switched account: Old={oldAcc.DisplayString} New={selectedAcc.DisplayString}");
            }

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

            Logger.Log(LogLevel.Debug, "Updated account instance");
        }
        #endregion

        #region GetDataDirectory
        private static string GetDataDirectory(string strFolder = "")
        {
            string strRet = Path.Combine(Hearthstone_Deck_Tracker.Config.Instance.DataDir, "DustUtility", strFolder);

            if (!Directory.Exists(strRet))
                Directory.CreateDirectory(strRet);

            return strRet;
        }
        #endregion

        #region GetFullFileName
        public static string GetFullFileName(IAccount account, string strType) => !account.IsEmpty
                ? Path.Combine(AccountsDirectory, $"{account.AccountString}_{strType}.xml")
                : Path.Combine(AccountsDirectory, $"{strType}.xml");
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
        }
        #endregion

        #region GetCollectionWrapper
        public static List<HearthMirror.Objects.Card> GetCollectionWrapper()
        {
            List<HearthMirror.Objects.Card> lstRet = null;

            if (!IsOffline)
            {
                lstRet = HearthMirror.Reflection.GetCollection()?.Where(c => c.Count > 0 && c.Id != null && HearthDb.Cards.Collectible.ContainsKey(c.Id)).ToList();
#if DEBUG
                Logger.Log(LogLevel.All, $"Count: {lstRet?.Count}");
#endif
            }

            return lstRet;
        }
        #endregion

        #region ShowToastNotification
        public static void ShowToastNotification(string strMessage, Action onClick = null)
        {
            if (Config.ShowNotifications)
            {
                Hearthstone_Deck_Tracker.Core.MainWindow?.Dispatcher.Invoke(() =>
                {
                    NotificationToast toast = new NotificationToast(strMessage);

                    if (onClick != null)
                        toast.Click += (s, e) => onClick();

                    ToastManager.ShowCustomToast(toast);
                });

                Logger.Log(LogLevel.Debug, $"Display toast notification ('{strMessage}')");
            }
        }
        #endregion

        #region CheckForUpdatesAsync
        public static async Task CheckForUpdatesAsync()
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
                        ShowToastNotification("New Update Available!", () =>
                        {
                            s_updateDialog = new UpdateWindow();
                            s_updateDialog.UpdateFlyoutView.DownloadProgressBar.Margin = new Thickness();

                            s_updateDialog.Show();
                        });
                    }
                }

                s_blnCheckedForUpdates = true;
            }
            else
            {
                Logger.Log(LogLevel.Trace, "Already checked for updates");
            }
        }
        #endregion

        #region GetSaveDelay
        private int GetSaveDelay()
        {
            int nRet = 0;

            switch (Config.SaveDelayUnit)
            {
                case TimeUnit.Seconds:
                    nRet = Config.SaveDelay;
                    break;

                case TimeUnit.Minutes:
                    nRet = Config.SaveDelay * 60;
                    break;
            }

            return nRet;
        }
        #endregion

        #region CloseUpdateDialog
        public static void CloseUpdateDialog()
        {
            if (s_updateDialog != null)
            {
                s_updateDialog.Close();
                s_updateDialog = null;

                Logger.Log(LogLevel.Debug, "Closed update dialog");
            }
        }
        #endregion
        #endregion
    }
}