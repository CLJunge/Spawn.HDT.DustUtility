﻿#region Using
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Spawn.HDT.DustUtility.Logging;
using Spawn.HDT.DustUtility.Properties;
using System.Drawing;
using System.IO;
using System.Net;
using System.Threading.Tasks;
#endregion

namespace Spawn.HDT.DustUtility.Net
{
    public static class CardImageProvider
    {
        #region Static Properties
#if DEBUG
        private static string ApiKey => Settings.Default.TestingApiKey;
#else
        private static string ApiKey => Settings.Default.ProductionApiKey;
#endif
        #endregion

        #region GetStreamAsync
        public static async Task<Stream> GetStreamAsync(string strCardId, bool blnPremium)
        {
            Stream retVal = null;

            if (!string.IsNullOrEmpty(strCardId))
            {
                DustUtilityPlugin.Logger.Log(LogLevel.Debug, $"Downloading card image... (Id={strCardId}, Premium={blnPremium})");

                try
                {
                    HttpWebRequest cardDatarequest = CreateCardDataRequest(strCardId);

                    HttpWebResponse cardDataResponse = await cardDatarequest.GetResponseAsync() as HttpWebResponse;

                    if (cardDataResponse.StatusCode == HttpStatusCode.OK)
                    {
                        string strJson = string.Empty;

                        Stream cardDataResponseStream = null;

                        try
                        {
                            cardDataResponseStream = cardDataResponse.GetResponseStream();

                            using (StreamReader reader = new StreamReader(cardDataResponseStream))
                                strJson = await reader.ReadToEndAsync();
                        }
                        finally
                        {
                            cardDataResponseStream?.Dispose();
                        }

                        if (!string.IsNullOrEmpty(strJson))
                        {
                            JToken cardData = JsonConvert.DeserializeObject<JArray>(strJson)[0];

                            string strUrl = cardData.Value<string>("img");

                            if (blnPremium)
                                strUrl = cardData.Value<string>("imgGold");

                            HttpWebRequest imageRequest = CreateImageRequest(strUrl);

                            HttpWebResponse imageResponse = await imageRequest.GetResponseAsync() as HttpWebResponse;

                            if (imageResponse.StatusCode == HttpStatusCode.OK)
                            {
                                retVal = new MemoryStream();

                                using (Stream responseStream = imageResponse.GetResponseStream())
                                    await responseStream.CopyToAsync(retVal);

                                retVal.Position = 0;
                            }
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    DustUtilityPlugin.Logger.Log(LogLevel.Error, $"Couldn't load card image: {ex}");
                }
            }

            return retVal;
        }
        #endregion

        #region GetBitmapAsync
        public static async Task<Bitmap> GetBitmapAsync(string strCardId, bool blnPremium) => Image.FromStream(await GetStreamAsync(strCardId, blnPremium)) as Bitmap;
        #endregion

        #region Requests
        #region Json Data
        #region CreateCardDataRequest
        private static HttpWebRequest CreateCardDataRequest(string strCardId)
        {
            return CreateJsonDataRequest($"/cards/{strCardId}");
        }
        #endregion

        #region CreateJsonDataRequest
        private static HttpWebRequest CreateJsonDataRequest(string strUrl)
        {
            HttpWebRequest retVal = WebRequest.CreateHttp($"{Settings.Default.ApiBaseUrl}{strUrl}");

            retVal.Accept = "application/json";

            retVal.Headers.Add("X-Mashape-Key", ApiKey);

            return retVal;
        }
        #endregion
        #endregion

        #region CreateImageRequest
        private static HttpWebRequest CreateImageRequest(string strUrl)
        {
            HttpWebRequest retVal = WebRequest.CreateHttp(strUrl);

            string strExtension = Path.GetExtension(strUrl).Remove(0, 1);

            retVal.Accept = $"image/{strExtension}";

            return retVal;
        }
        #endregion
        #endregion
    }
}