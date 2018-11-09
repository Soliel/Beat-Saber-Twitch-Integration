using System;
using SimpleJSON;
using SongLoaderPlugin;

namespace TwitchIntegrationPlugin.Serializables
{
    [Serializable]
    public class QueuedSong
    {
        public string SongName;
        public string BeatName;
        public string AuthName;
        public float Bpm;
        public string Id;
        public string DownloadUrl;
        public string RequestedBy;
        public string CoverUrl;
        public string SongSubName;
        public string SongHash;

        //public event Action DequeuedCallback;

        public QueuedSong(string songname, string beatname, string authname, string bpm, string id, string songSubName, string dlUrl, string requestedBy, string coverUrl, string songHash)
        {
            SongName = songname;
            BeatName = beatname;
            AuthName = authname;
            Id = id;
            Bpm = float.Parse(bpm, System.Globalization.CultureInfo.InvariantCulture);
            SongSubName = songSubName;
            DownloadUrl = dlUrl;
            RequestedBy = requestedBy;
            CoverUrl = coverUrl;
            SongHash = songHash;
        }

        public QueuedSong()
        {
            SongName = "";
            BeatName = "";
            AuthName = "";
            Id = "";
            Bpm = 0;
            SongSubName = "";
            DownloadUrl = "";
            CoverUrl = "";
            SongHash = "";
        }

        public void InvokeDequeueCallback()
        {
            //DequeuedCallback?.Invoke();
        }

        public bool CompareSongs(CustomSongInfo song)
        {
            return SongName == song.songName && AuthName == song.songAuthorName && Bpm == song.beatsPerMinute;
        }

        public JSONNode ToJsonNode()
        {
            JSONNode node = new JSONObject();
            node["songname"] = SongName;
            node["beatname"] = BeatName;
            node["authname"] = AuthName;
            node["id"] = Id;
            node["bpm"] = Bpm;
            node["songsubname"] = SongSubName;
            node["downloadurl"] = DownloadUrl;
            node["coverurl"] = CoverUrl;
            node["songhash"] = SongHash.ToUpper();
            return node;
        }

        public static QueuedSong FromJsonNode(JSONNode node)
        {
            QueuedSong song = new QueuedSong
            {
                SongName = node["songname"],
                BeatName = node["beatname"],
                AuthName = node["authname"],
                Id = node["id"],
                Bpm = node["bpm"],
                SongSubName = node["songsubname"],
                DownloadUrl = node["downloadurl"],
                CoverUrl = node["coverurl"],
                SongHash = node["songhash"].Value.ToUpper()
            };
            return song;
        }

        public override string ToString()
        {
            string Test(string value)
            {
                return value.Replace("\"", "\\\"");
            }

            return "{" +
                   "\"songName\": \"" + Test(SongName) + "\", " +
                   "\"name\": \"" + Test(BeatName) + "\", " +
                   "\"authorName\": \"" + Test(AuthName) + "\", " +
                   "\"bpm\": " + Bpm + ", " +
                   "\"id\": \"" + Test(Id) + "\", " +
                   "\"downloadUrl\": \"" + Test(DownloadUrl) + "\", " +
                   "\"requestedBy\": \"" + Test(RequestedBy) + "\", " +
                   "\"coverUrl\": \"" + Test(CoverUrl) + "\", " +
                   "\"songSubName\": \"" + Test(SongSubName) + "\""
                   + "}";
        }
    }
}