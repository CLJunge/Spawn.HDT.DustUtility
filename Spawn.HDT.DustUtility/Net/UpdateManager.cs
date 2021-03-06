﻿#region Using
using Spawn.HDT.DustUtility.Logging;
using Spawn.HDT.DustUtility.Properties;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
#endregion

namespace Spawn.HDT.DustUtility.Net
{
    public static class UpdateManager
    {
        #region Static Fields
        private static Version NewVersionFormat => new Version(1, 6, 1);

        private static readonly Regex s_versionRegex;
        private static readonly Regex s_updateTextRegex;

        private static WebClient s_webClient;

        private static bool s_blnIsChecking;
        #endregion

        #region Static Properties
        #region Info
        public static UpdateInfo Info { get; private set; }
        #endregion
        #endregion

        #region Custom Events
        public static event DownloadProgressChangedEventHandler DownloadProgressChanged;

        public static event DownloadDataCompletedEventHandler DownloadCompleted;
        #endregion

        #region Static Ctor
        static UpdateManager()
        {
            s_versionRegex = new Regex("[0-9]\\.[0-9]{1,2}\\.?[0-9]{0,2}");
            s_updateTextRegex = new Regex("<div class=\"markdown-body\">\\s*?<p>(?<Content>.*)</p>\\s*?</div>");
        }
        #endregion

        #region CheckForUpdatesAsync
        public static async Task<bool> CheckForUpdatesAsync()
        {
            bool blnRet = false;

            if (!s_blnIsChecking)
            {
                s_blnIsChecking = true;

                Info = null;

                try
                {
                    DustUtilityPlugin.Logger.Log(LogLevel.Trace, "Checking GitHub for updates...");

                    HttpWebRequest request = WebRequest.CreateHttp($"{Settings.Default.GitHubBaseUrl}/releases/latest");

                    using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
                    {
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            Match versionMatch = s_versionRegex.Match(response.ResponseUri.AbsoluteUri);

                            if (versionMatch.Success)
                            {
                                Version newVersion = new Version(versionMatch.Value);

#if DEBUG
                                blnRet = newVersion > new Version(0, 0);
#else
                                blnRet = newVersion > System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
#endif

                                if (blnRet)
                                {
                                    Info = new UpdateInfo(newVersion);

                                    string strResult;

                                    using (WebClient webClient = new WebClient())
                                        strResult = await webClient.DownloadStringTaskAsync(response.ResponseUri);

                                    //prepare for regex check
                                    strResult = strResult.Trim().Replace("\n", string.Empty).Replace("\r", string.Empty);

                                    Match updateTextMatch = s_updateTextRegex.Match(strResult);

                                    if (updateTextMatch.Success)
                                        Info.ReleaseNotes = updateTextMatch.Groups["Content"].Value.Replace("<br>", Environment.NewLine);

                                    DustUtilityPlugin.Logger.Log(LogLevel.Trace, "Update available");
                                }
                                else
                                {
                                    DustUtilityPlugin.Logger.Log(LogLevel.Trace, "No update available");
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //No internet connection or github down
                    DustUtilityPlugin.Logger.Log(LogLevel.Error, $"Couldn't perform update check: {ex}");
                }

                s_blnIsChecking = false;
            }

            return blnRet;
        }
        #endregion

        #region Download
        public static void Download(Version version)
        {
            string strVersionString = version.ToString(3);

            if (version < NewVersionFormat)
                strVersionString = version.ToString(2);

            using (s_webClient = new WebClient())
            {
                s_webClient.DownloadProgressChanged += (s, e) => DownloadProgressChanged?.Invoke(s, e);

                s_webClient.DownloadDataCompleted += (s, e) =>
                {
                    if (!e.Cancelled)
                        DownloadCompleted?.Invoke(s, e);
                    else
                        DustUtilityPlugin.Logger.Log(LogLevel.Warning, "User canceled download");
                };

                s_webClient.DownloadDataAsync(new Uri($"{Settings.Default.GitHubBaseUrl}/releases/download/{strVersionString}/Spawn.HDT.DustUtility.zip"));
            }
        }
        #endregion

        #region CancelDownload
        public static void CancelDownload() => s_webClient?.CancelAsync();
        #endregion
    }
}