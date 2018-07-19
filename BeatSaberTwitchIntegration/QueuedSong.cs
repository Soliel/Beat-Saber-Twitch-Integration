using System;
using SongLoaderPlugin;

namespace TwitchIntegrationPlugin
{
    public class QueuedSong
    {
        public string   _songName { get; }
        public string   _beatName { get; }
        public string   _authName { get; }
        //public string[] _diffLevels { get; }
        public float    _bpm { get; }
        public string   _id { get; }
        public string  _downloadUrl { get; }
        public string _requestedBy { get; }
        public string  _coverUrl { get; }

        public QueuedSong(String songname, String beatname, String authname, String bpm, String id, String dlURL, String requestedBy, String coverURL)
        {
            _songName = songname;
            _beatName = beatname;
            _authName = authname;
            _id = id;
            _bpm = float.Parse(bpm, System.Globalization.CultureInfo.InvariantCulture);
            _downloadUrl = dlURL;
            _requestedBy = requestedBy;
            _coverUrl = coverURL;
        }
    
        public bool CompareSongs(CustomSongInfo song)
        {
            if(_songName == song.songName && _authName == song.authorName && _bpm == song.beatsPerMinute)
            {
                return true;
            }
            return false;
        }
    }
    
}
