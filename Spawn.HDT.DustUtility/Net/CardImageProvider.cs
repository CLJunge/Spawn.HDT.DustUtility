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
        #region Constants
        private const string BaseUrl = "https://omgvamp-hearthstone-v1.p.mashape.com";
        #endregion

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
                try
                {
                    HttpWebRequest request = CreateCardDataRequest(strCardId);

                    HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;

                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string strJson;

                        using (Stream responseStream = response.GetResponseStream())
                        {
                            using (StreamReader reader = new StreamReader(responseStream))
                            {
                                strJson = await reader.ReadToEndAsync();
                            }
                        }

                        if (!string.IsNullOrEmpty(strJson))
                        {
                            JToken cardData = JsonConvert.DeserializeObject<JArray>(strJson)[0];

                            string strUrl = cardData.Value<string>("img");

                            if (blnPremium)
                            {
                                strUrl = cardData.Value<string>("imgGold");
                            }
                            else { }

                            HttpWebRequest imageRequest = CreateImageRequest(strUrl);

                            HttpWebResponse imageResponse = await imageRequest.GetResponseAsync() as HttpWebResponse;

                            if (imageResponse.StatusCode == HttpStatusCode.OK)
                            {
                                using (Stream responseStream = imageResponse.GetResponseStream())
                                {
                                    retVal = new MemoryStream();

                                    await responseStream.CopyToAsync(retVal);
                                }

                                retVal.Position = 0;
                            }
                            else { }
                        }
                        else { }
                    }
                    else { }
                }
                catch
                {
                    Logger.Default.Log(LogLevel.Warning, "Couldn't load card image!");
                }
            }
            else { }

            return retVal;
        }
        #endregion

        #region GetBitmapAsync
        public static async Task<Bitmap> GetBitmapAsync(string strCardId, bool blnPremium)
        {
            Stream imageStream = await GetStreamAsync(strCardId, blnPremium);

            return Image.FromStream(imageStream) as Bitmap;
        }
        #endregion

        #region Requests
        #region Json Data
        #region CreateCardDataRequest
        private static HttpWebRequest CreateCardDataRequest(string strCardId)
        {
            return CreateJsonDataRequest($"/cards/{strCardId}");
        }
        #endregion

        #region CreateCardBackImageDataRequest
        private static HttpWebRequest CreateCardBackImageDataRequest()
        {
            return CreateJsonDataRequest("/cardbacks");
        }
        #endregion

        #region CreateJsonDataRequest
        private static HttpWebRequest CreateJsonDataRequest(string strUrl)
        {
            HttpWebRequest retVal = WebRequest.CreateHttp($"{BaseUrl}{strUrl}");

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