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
        private const string Beatsaver = "https://beatsaver.com/";

        public static QueuedSong GetSongFromBeatSaver(Boolean isTextSearch, String queryString, String requestedBy)
        {
            var apiPath = isTextSearch ? "{0}/api/songs/search/all/{1}" : "{0}/api/songs/detail/{1}";

            var webrequest = (HttpWebRequest)WebRequest.Create(String.Format(apiPath, Beatsaver, queryString));
            webrequest.Method = "GET";
            webrequest.ContentType = "application/json";
            ServicePointManager.ServerCertificateValidationCallback = MyRemoteCertificateValidationCallback;
            var webresponse = (HttpWebResponse)webrequest.GetResponse();
            var enc = Encoding.GetEncoding("utf-8");
            var responseStream = new StreamReader(webresponse.GetResponseStream() ?? throw new NoNullAllowedException(), enc);
            var result = responseStream.ReadToEnd();
            webresponse.Close();

            if (result == "{\"songs\":[],\"total\":0}")
                return null;

            var node = JSON.Parse(result);

            if (isTextSearch)
            {
                string songName = node["songs"][0]["songName"];
                string beatName = node["songs"][0]["name"];
                string authorName = node["songs"][0]["authorName"];
                string bpm = node["songs"][0]["bpm"];
                string key = node["songs"][0]["key"];
                string songSubName = node["songs"][0]["songSubName"];
                string downloadUrl = node["songs"][0]["downloadUrl"];
                string coverUrl = node["songs"][0]["coverUrl"];
                return new QueuedSong(songName, beatName, authorName, bpm, key, songSubName, downloadUrl, requestedBy, coverUrl);
            }
            else
            {
                string songName = node["song"]["songName"];
                string beatName = node["song"]["name"];
                string authorName = node["song"]["authorName"];
                string bpm = node["song"]["bpm"];
                string key = node["song"]["key"];
                string songSubName = node["song"]["songSubName"];
                string downloadUrl = node["song"]["downloadUrl"];
                string coverUrl = node["song"]["coverUrl"];
                return new QueuedSong(songName, beatName, authorName, bpm, key, songSubName, downloadUrl, requestedBy, coverUrl);
            }
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
                var chainIsValid = chain.Build((X509Certificate2)certificate);
                if (chainIsValid) continue;
                isOk = false;
                break;
            }
            return isOk;
        }
    }
}