using System;
using SongLoaderPlugin;

namespace TwitchIntegrationPlugin.Serializables
{
    [Serializable]
    public struct QueuedSong
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

        public bool CompareSongs(CustomSongInfo song)
        {
            return SongName == song.songName && AuthName == song.songAuthorName && Bpm == song.beatsPerMinute;
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