using System.Security.Cryptography.X509Certificates;
using SongLoaderPlugin;

namespace TwitchIntegrationPlugin
{
    public struct QueuedSong
    {
        public string SongName { get; }
        public string BeatName { get; }
        public string AuthName { get; }
        public float Bpm { get; }
        public string Id { get; }
        public string DownloadUrl { get; }
        public string RequestedBy { get; }
        public string CoverUrl { get; }
        public string SongSubName { get; }
        public string SongHash { get; }


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