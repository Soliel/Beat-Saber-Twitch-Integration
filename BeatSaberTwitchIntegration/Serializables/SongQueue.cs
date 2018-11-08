using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace TwitchIntegrationPlugin.Serializables
{
    [Serializable]
    public class SongQueue
    {
        [SerializeField]
        private List<QueuedSong> _songQueue = new List<QueuedSong>();


        public SongQueue()
        {
            _songQueue = new List<QueuedSong>();
        }

        public void SaveSongQueue()
        {
            using (FileStream fs = new FileStream("UserData/TwitchIntegrationSavedQueue.json", FileMode.Create, FileAccess.Write))
            {
                byte[] Buffer = Encoding.ASCII.GetBytes(JsonUtility.ToJson(this, true));
                fs.Write(Buffer, 0, Buffer.Length);
            }
        }

        public void LoadSongQueue()
        {
            using (FileStream fs = new FileStream("UserData/TwitchIntegrationSavedQueue.json", FileMode.OpenOrCreate,
                FileAccess.ReadWrite))
            {
                if (fs.Length == 0) return;
                byte[] readBuffer = new byte[fs.Length];
                fs.Read(readBuffer, 0, (int) fs.Length);
                string readString = Encoding.ASCII.GetString(readBuffer);
                _songQueue = JsonUtility.FromJson<SongQueue>(readString).GetSongList();
            }
        }

        public void AddSongToQueue(QueuedSong song)
        {
            _songQueue.Add(song);
        }

        public QueuedSong PopQueuedSong()
        {
            QueuedSong returnSong = _songQueue[0];
            _songQueue.RemoveAt(0);
            returnSong.InvokeDequeueCallback();

            return returnSong;
        }

        public QueuedSong PeekQueuedSong()
        {
            return _songQueue[0];
        }

        public void RemoveSongFromQueue(QueuedSong song)
        {
            song.InvokeDequeueCallback();
            _songQueue.Remove(song);
        }

        public bool IsSongInQueue(QueuedSong song)
        {
            return _songQueue.Contains(song);
        }

        public bool DoesQueueHaveSongs()
        {
            return _songQueue.Count == 0 ? true : false;
        }

        public List<QueuedSong> GetSongList()
        {
            return _songQueue;
        }
    }
}
