using System;
using System.Data;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;
using SimpleJSON;
using System.Text;

namespace TwitchIntegrationPlugin
{
    class ApiConnection
    {
        private const string BeatSaver = "https://beatsaver.com";

        public static QueuedSong GetSongFromBeatSaver(bool isTextSearch, string queryString, string requestedBy)
        {
            var apiPath = isTextSearch ? "{0}/api/songs/search/all/{1}" : "{0}/api/songs/detail/{1}";

            var webRequest = (HttpWebRequest) WebRequest.Create(string.Format(apiPath, BeatSaver, queryString));
            webRequest.Method = "GET";
            webRequest.ContentType = "application/json";
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;

            string result;
            try
            {
                var enc = Encoding.GetEncoding("utf-8");
                var webResponse = (HttpWebResponse) webRequest.GetResponse();
                var responseStream = new StreamReader(webResponse.GetResponseStream() ?? throw new NoNullAllowedException(), enc);
                result = responseStream.ReadToEnd();
                webResponse.Close();
            }
            catch (WebException)
            {
                // todo log error? think is 404
                return null;
            }

            if (result.Length == 0 || result == "{\"songs\":[],\"total\":0}")
            {
                return null;
            }

            var node = JSON.Parse(result);
            node = isTextSearch ? node["songs"][0] : node["song"];

            return new QueuedSong(
                node["songName"],
                node["name"],
                node["authorName"],
                node["bpm"],
                node["key"],
                node["songSubName"],
                node["downloadUrl"],
                requestedBy,
                node["coverUrl"],
                node["hashMd5"]
            );
        }

        private static bool MyRemoteCertificateValidationCallback(object sender,
            X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            var isOk = true;
            // If there are errors in the certificate chain,
            // look at each error to determine the cause.
            if (sslPolicyErrors == SslPolicyErrors.None) return true;
            foreach (var t in chain.ChainStatus)
            {
                if (t.Status == X509ChainStatusFlags.RevocationStatusUnknown)
                {
                    continue;
                }

                chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;

                var chainIsValid = chain.Build((X509Certificate2) certificate);
                if (chainIsValid) continue;

                isOk = false;
                break;
            }

            return isOk;
        }
    }
}