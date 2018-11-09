using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SimpleJSON;

namespace TwitchIntegrationPlugin.Serializables
{
    [Serializable]
    public class SongQueue
    {

        public List<QueuedSong> SongQueueList;


        public SongQueue()
        {
            SongQueueList = new List<QueuedSong>();
        }

        public void SaveSongQueue()
        {
            using (FileStream fs = new FileStream("UserData/TwitchIntegrationSavedQueue.json", FileMode.Create, FileAccess.Write))
            {
                JSONNode arrayNode = new JSONArray();
                foreach (QueuedSong queuedSong in SongQueueList)
                {
                    arrayNode.Add(queuedSong.ToJsonNode());
                }

                byte[] buffer = Encoding.ASCII.GetBytes(arrayNode.ToString());
                fs.Write(buffer, 0, buffer.Length);
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
                JSONNode node = JSON.Parse(readString);

                foreach (KeyValuePair<string, JSONNode> queuedSong in node)
                {
                    QueuedSong song = QueuedSong.FromJsonNode(queuedSong.Value);
                    SongQueueList.Add(song);
                }
            }
        }

        public void AddSongToQueue(QueuedSong song)
        {
            SongQueueList.Add(song);
        }

        public QueuedSong PopQueuedSong()
        {
            QueuedSong returnSong = SongQueueList[0];
            SongQueueList.RemoveAt(0);
            try
            {
                if (StaticData.UserRequestCount.ContainsKey(returnSong.RequestedBy))
                    StaticData.UserRequestCount[returnSong.RequestedBy]--;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }


            

            return returnSong;
        }

        public QueuedSong PeekQueuedSong()
        {
            return SongQueueList[0];
        }

        public void RemoveSongFromQueue(QueuedSong song)
        {
            if(StaticData.UserRequestCount.ContainsKey(song.RequestedBy))
                StaticData.UserRequestCount[song.RequestedBy]--;
            SongQueueList.Remove(song);
        }

        public bool IsSongInQueue(QueuedSong song)
        {
            return SongQueueList.Exists(x => x.Id == song.Id);
        }

        public bool DoesQueueHaveSongs()
        {
            return SongQueueList.Count != 0;
        }

        public List<QueuedSong> GetSongList()
        {
            return SongQueueList;
        }
    }
}
