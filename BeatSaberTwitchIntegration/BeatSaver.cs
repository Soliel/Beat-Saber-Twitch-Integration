using System;
using System.Text;
using AsyncTwitch;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using TwitchIntegrationPlugin.Serializables;

namespace TwitchIntegrationPlugin
{
    public class BeatSaver
    {
        private const string BeatSaverUrl = "https://beatsaver.com";

        public static QueuedSong GetSongFromBeatSaver(string query, bool isTextQuery, string requestedBy)
        {
            QueuedSong resultSong = new QueuedSong();
            string apiPath = isTextQuery ? "{0}/api/songs/search/all/{1}" : "{0}/api/songs/detail/{1}";

            UnityWebRequest www = UnityWebRequest.Get(string.Format(apiPath, BeatSaverUrl, query));
            bool timeout = false;
            float time = 0f;

            UnityWebRequestAsyncOperation asyncRequest = www.SendWebRequest();

            while (!asyncRequest.isDone || asyncRequest.progress < 1f)
            {
                time += Time.deltaTime;

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if ((time >= 5f) && asyncRequest.progress == 0f) continue;
                www.Abort();
                timeout = true;
            }

            if (www.isNetworkError || www.isHttpError || timeout)
            {
                www.Abort();
                TwitchConnection.Instance.SendChatMessage("Error downloading song metadata.");
                return resultSong;
            }
            else
            {
                byte[] data = www.downloadHandler.data;
                string responseString = Encoding.UTF8.GetString(data);

                if (responseString == "{}" || responseString.Length == 0 || responseString == "{\"songs\":[],\"total\":0}")
                {
                    TwitchConnection.Instance.SendChatMessage("Invalid Request");
                    return resultSong;
                }

                JSONNode node = JSON.Parse(responseString);
                node = isTextQuery ? node["songs"][0] : node["song"];

                resultSong = new QueuedSong(
                    node["songName"],
                    node["name"],
                    node["authorName"],
                    node["bpm"],
                    node["key"],
                    node["songSubName"],
                    node["downloadUrl"],
                    requestedBy,
                    node["coverUrl"],
                    node["hashMd5"]);
            }
            Console.WriteLine(resultSong);
            return resultSong;
        }
    }
}