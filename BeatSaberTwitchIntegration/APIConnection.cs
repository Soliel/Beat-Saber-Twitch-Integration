using System;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.IO;
using SimpleJSON;
using System.Text;

namespace TwitchIntegrationPlugin
{
    class APIConnection
    {
        private const String BEATSAVER = "https://beatsaver.com/";

        public static QueuedSong GetSongFromBeatSaver(Boolean isTextSearch, String queryString, String requestedBy)
        {
            String apiPath;
            if (isTextSearch)
            {
                apiPath = "{0}/api/songs/search/all/{1}";
            }
            else
                apiPath = "{0}/api/songs/detail/{1}";

            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(String.Format(apiPath, BEATSAVER, queryString));
            webrequest.Method = "GET";
            webrequest.ContentType = "application/json";
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
            HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();
            Encoding enc = Encoding.GetEncoding("utf-8");
            StreamReader responseStream = new StreamReader(webresponse.GetResponseStream(), enc);
            string result = string.Empty;
            result = responseStream.ReadToEnd();
            webresponse.Close();

            JSONNode node = JSON.Parse(result);

            if (isTextSearch)
            {
                string songName = node["songs"][0]["songName"];
                string beatName = node["songs"][0]["name"];
                string authorName = node["songs"][0]["authorName"];
                string bpm = node["songs"][0]["bpm"];
                string key = node["songs"][0]["key"];
                string downloadUrl = node["songs"][0]["downloadUrl"];
                string coverUrl = node["songs"][0]["coverUrl"];
                return new QueuedSong(songName, beatName, authorName, bpm, key, downloadUrl, requestedBy, coverUrl);
            } 
            else
            {
                string songName = node["song"]["songName"];
                string beatName = node["song"]["name"];
                string authorName = node["song"]["authorName"];
                string bpm = node["song"]["bpm"];
                string key = node["song"]["key"];
                string downloadUrl = node["song"]["downloadUrl"];
                string coverUrl = node["song"]["coverUrl"];
                return new QueuedSong(songName, beatName, authorName, bpm, key, downloadUrl, requestedBy, coverUrl);
            }
        }

        private static bool MyRemoteCertificateValidationCallback(System.Object sender,
        X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            bool isOk = true;
            // If there are errors in the certificate chain,
            // look at each error to determine the cause.
            if (sslPolicyErrors != SslPolicyErrors.None)
            {
                for (int i = 0; i < chain.ChainStatus.Length; i++)
                {
                    if (chain.ChainStatus[i].Status == X509ChainStatusFlags.RevocationStatusUnknown)
                    {
                        continue;
                    }
                    chain.ChainPolicy.RevocationFlag = X509RevocationFlag.EntireChain;
                    chain.ChainPolicy.RevocationMode = X509RevocationMode.Online;
                    chain.ChainPolicy.UrlRetrievalTimeout = new TimeSpan(0, 1, 0);
                    chain.ChainPolicy.VerificationFlags = X509VerificationFlags.AllFlags;
                    bool chainIsValid = chain.Build((X509Certificate2)certificate);
                    if (!chainIsValid)
                    {
                        isOk = false;
                        break;
                    }
                }
            }
            return isOk;
        }
    }
}
