using SongLoaderPlugin;

namespace TwitchIntegrationPlugin
{
    public class QueuedSong
    {
        public string   SongName    { get; }
        public string   BeatName    { get; }
        public string   AuthName    { get; }
        public float    Bpm         { get; }
        public string   Id          { get; }
        public string   DownloadUrl { get; }
        public string   RequestedBy { get; }
        public string   CoverUrl    { get; }
        public string   SongSubName { get; }

        public QueuedSong(string songname, string beatname, string authname, string bpm, string id, string songSubName, string dlUrl, string requestedBy, string coverUrl)
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
        }
    
        public bool CompareSongs(CustomSongInfo song)
        {
            if(SongName == song.songName && AuthName == song.songAuthorName && Bpm == song.beatsPerMinute)
            {
                return true;
            }
            return false;
        }
    }
    
}
